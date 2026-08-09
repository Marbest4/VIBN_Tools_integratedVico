using ClosedXML.Excel;
using VIBN_Tools.ContainerGeneration.BusinessLogic.ContainerData;
using VIBN_Tools.ContainerGeneration.BusinessLogic.ExcelData;
using VIBN_Tools.ContainerGeneration.BusinessLogic.ZuLiData.Interfaces;
using VIBN_Tools.ContainerGeneration.Utils;

namespace VIBN_Tools.ContainerGeneration.BusinessLogic.ZuLiData
{
    /// <summary>
    /// Abstract base class for reading ZuLi data from a file.
    /// </summary>
    public abstract class AZuLiReader
    {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Reads ZuLi data from the specified file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>A result containing the list of container entries.</returns>
        public abstract Result<List<ContainerEntry>> ReadFromFile(string path);

        /// <summary>
        /// Asynchronously reads ZuLi data from the specified file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>A task representing the asynchronous operation, with a result containing the list of container entries.</returns>
        public abstract Task<Result<List<ContainerEntry>>> ReadFromFileAsync(string path);

        /// <summary>
        /// Reads ZuLi data from the specified file using the provided worksheet rule set.
        /// </summary>
        /// <typeparam name="T">The type of container entry.</typeparam>
        /// <param name="path">The path to the file.</param>
        /// <param name="worksheetRuleSet">The worksheet rule set used for reading data.</param>
        /// <returns>A result containing the list of container entries.</returns>
        public Result<List<T>> ReadFromFile<T>(string path, IWorksheetRuleSet worksheetRuleSet)
        {
            var result = ExcelReader.Read(path);

            if (!result.IsSuccess)
                return Result<List<T>>.Failure(result.ErrorMessage);

            using var workbook = result.Value;
            if (worksheetRuleSet.WorksheetNumber < 1 ||
                worksheetRuleSet.WorksheetNumber > workbook.Worksheets.Count)
            {
                string message = $"Requested worksheet {worksheetRuleSet.WorksheetNumber} is not available";
                Logger.Error(message);
                return Result<List<T>>.Failure(message);
            }

            var worksheet = workbook.Worksheet(worksheetRuleSet.WorksheetNumber);
            return ReadData<T>(worksheet, worksheetRuleSet);
        }

        /// <summary>
        /// Reads data from the specified worksheet using the provided worksheet rule set.
        /// </summary>
        /// <typeparam name="T">The type of container entry.</typeparam>
        /// <param name="worksheet">The worksheet to read data from.</param>
        /// <param name="worksheetRuleSet">The worksheet rule set used for reading data.</param>
        /// <returns>A result containing the list of container entries.</returns>
        public virtual Result<List<T>> ReadData<T>(IXLWorksheet worksheet, IWorksheetRuleSet worksheetRuleSet)
        {
            List<T> entries = new List<T>();

            // 1) Spalten im Header mappen
            var mappingResult = getColumnMapping(worksheet, worksheetRuleSet.ColumnHeaderRow, worksheetRuleSet.ColumnDefinitions);
            if (!mappingResult.IsSuccess)
                return Result<List<T>>.Failure("Could not find required columns"); // Log bereits in getColumnMapping
            var columnMapping = mappingResult.Value; // Dictionary<IXLCell headerCell, ColumnDefinition def>

            // 2) Nach TargetProperty gruppieren (z. B. "Address" => ["Path", "Address"])
            var groupsByTarget = columnMapping
                .GroupBy(kvp => kvp.Value.TargetProperty)
                .ToDictionary(g => g.Key, g => g.ToList());

            // 3) Zeilen iterieren
            var rows = worksheet.RowsUsed(r => r.RowNumber() > worksheetRuleSet.ColumnHeaderRow);

            foreach (var row in rows)
            {
                bool invalidRow = false;
                if (Activator.CreateInstance(typeof(T)) is not T instance)
                    return Result<List<T>>.Failure(
                        $"Could not create an instance of {typeof(T).FullName}.");

                foreach (var targetGroup in groupsByTarget)
                {
                    string targetProperty = targetGroup.Key;
                    var candidates = targetGroup.Value; // List<(IXLCell headerCell, ColumnDefinition def)>

                    // (Optional) deterministische Reihenfolge, falls mehrere Kandidaten in einer Zeile befüllt sind:
                    // Hier "Path" vor "Address"; wenn beide leer, bleibt value null.
                    string[] preferredNames = new[] { "Path", "Address" };
                    candidates = candidates
                        .OrderBy(c =>
                        {
                            int idx = Array.IndexOf(preferredNames, c.Value.Name);
                            return idx >= 0 ? idx : int.MaxValue;
                        })
                        .ThenBy(c => c.Key.Address.ColumnNumber)
                        .ToList();

                    // 3a) Erste nicht-leere Zelle wählen
                    string? valueToSet = null;

                    foreach (var cand in candidates)
                    {
                        var cell = row.Cell(cand.Key.Address.ColumnNumber);
                        var text = cell.Value.ToString();

                        // nur nicht-leere Zellen berücksichtigen
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            // Formatvalidierung nur für die gewählte Zelle
                            if (cell.DataType != cand.Value.DataType)
                            {
                                Logger.Warn("Cell (column {columnName} row {rowIndex}) does not have the correct format. Found: {currentFormat} expected: {expectedFormat}",
                                    cand.Value.Name, cell.Address.RowNumber, cell.DataType, cand.Value.DataType);
                                invalidRow = true; // Zeile verwerfen (oder alternativ: continue und nächsten Kandidaten testen)
                            }
                            else
                            {
                                valueToSet = text;
                            }
                            break; // wir berücksichtigen genau EINE Spalte pro TargetProperty
                        }
                    }

                    // 3b) Required-Check pro Zeile: wenn kein Wert gewählt und Gruppe enthält ein Required
                    bool isRequired = candidates.Any(c => c.Value.IsRequired);
                    if (valueToSet == null && isRequired)
                    {
                        Logger.Warn("Row {rowIndex}: required property {targetProperty} has no value in any of its mapped columns.",
                            row.RowNumber(), targetProperty);
                        invalidRow = true;
                    }

                    // 3c) Property setzen (falls verfügbar)
                    var property = typeof(T).GetProperty(targetProperty);
                    if (property != null)
                    {
                        if (valueToSet != null)
                            property.SetValue(instance, valueToSet);
                    }
                    else
                    {
                        throw new ArgumentException($"Type {typeof(T)} has no property {targetProperty}!");
                    }
                }

                if (!invalidRow && instance != null)
                    entries.Add(instance);
            }

            return Result<List<T>>.Success(entries);
        }

        /// <summary>
        /// Maps columns in the worksheet to the internal column definitions.
        /// </summary>
        /// <param name="worksheet">The worksheet to map columns from.</param>
        /// <param name="columnHeaderRow">The row number of the column headers.</param>
        /// <param name="columnDefinitions">The list of column definitions.</param>
        /// <returns>A result containing the column mapping.</returns>
        private Result<Dictionary<IXLCell, ColumnDefinition>> getColumnMapping(IXLWorksheet worksheet, int columnHeaderRow, List<ColumnDefinition> columnDefinitions)
        {
            Dictionary<IXLCell, ColumnDefinition> columnMapping = new Dictionary<IXLCell, ColumnDefinition>();
            foreach (var columnDefinition in columnDefinitions)
            {
                var column = worksheet
                    .Row(columnHeaderRow)
                    .Cells()
                    .FirstOrDefault(cell => string.Equals(
                        cell.Value.ToString().Trim(),
                        columnDefinition.Name,
                        StringComparison.OrdinalIgnoreCase));
                if (column == null)
                {
                    if (!columnDefinition.IsRequired)
                        Logger.Info("Optional column {columnName} does not exist", columnDefinition.Name);
                }
                else
                    columnMapping.Add(column, columnDefinition);
            }

            // Several source formats use alternative columns for the same
            // target property (for example Address or Path). A target is valid
            // when at least one of its supported columns exists.
            foreach (var requiredTarget in columnDefinitions
                         .Where(definition => definition.IsRequired)
                         .Select(definition => definition.TargetProperty)
                         .Distinct(StringComparer.Ordinal))
            {
                if (columnMapping.Values.Any(definition =>
                        string.Equals(
                            definition.TargetProperty,
                            requiredTarget,
                            StringComparison.Ordinal)))
                    continue;

                var alternatives = columnDefinitions
                    .Where(definition => definition.TargetProperty == requiredTarget)
                    .Select(definition => definition.Name);
                Logger.Error(
                    "None of the required columns for {targetProperty} exist. Expected one of: {columnNames}",
                    requiredTarget,
                    string.Join(", ", alternatives));
                return Result<Dictionary<IXLCell, ColumnDefinition>>.Failure("Excel file invalid");
            }

            return Result<Dictionary<IXLCell, ColumnDefinition>>.Success(columnMapping);
        }
    }
}
