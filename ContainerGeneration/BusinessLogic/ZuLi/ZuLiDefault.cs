using ClosedXML.Excel;
using VIBN_Tools.ContainerGeneration.BusinessLogic.ContainerData;
using VIBN_Tools.ContainerGeneration.BusinessLogic.ExcelData;
using VIBN_Tools.ContainerGeneration.BusinessLogic.ZuLiData.Interfaces;
using VIBN_Tools.ContainerGeneration.Utils;

namespace VIBN_Tools.ContainerGeneration.BusinessLogic.ZuLiData
{
    /// <summary>
    /// Represents the default implementation for reading ZuLi data from a file.
    /// </summary>
    public class ZuLiDefault : AZuLiReader, IZuLiData<ContainerEntry>
    {
        /// <summary>
        /// Gets the worksheet rule set used for reading data.
        /// </summary>
        public IWorksheetRuleSet worksheetRuleSet { get; private set; }

        /// <summary>
        /// Gets the list of container entries read from the file.
        /// </summary
        public List<ContainerEntry> Items { get; private set; }


        /// <summary>
        /// Mapping function for TargetProperty to read-in both address and path
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public string MapToTargetProperty(string columnName)
        {
            return columnName switch
            {
                "Address" => "Address",
                "Path" => "Address",            // mapping "Path" to "Address"
                _ => columnName
            };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZuLiDefault"/> class.
        /// </summary>
        public ZuLiDefault()
        {
            worksheetRuleSet = new WorksheetRuleSet
            {
                WorksheetNumber = 1,
                ColumnHeaderRow = 1,
                ColumnDefinitions = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "Tag", TargetProperty = MapToTargetProperty("Signal"), IsRequired = true, AllowDuplicates=true, DataType = XLDataType.Text },
                    new ColumnDefinition { Name = "Comment", TargetProperty = MapToTargetProperty("ID"), IsRequired = false, AllowDuplicates=false, DataType = XLDataType.Text },
                    new ColumnDefinition { Name = "Address", TargetProperty = MapToTargetProperty("Address"), IsRequired = true, AllowDuplicates=false, DataType = XLDataType.Text },
                    new ColumnDefinition { Name = "Path", TargetProperty = MapToTargetProperty("Address"), IsRequired = false, AllowDuplicates=false, DataType = XLDataType.Text },
                    new ColumnDefinition { Name = "Type", TargetProperty = MapToTargetProperty("DataType"), IsRequired = false, AllowDuplicates=false, DataType = XLDataType.Text }
                }
                //ColumnDefinitions = new List<ColumnDefinition>
                //{
                //    new ColumnDefinition { Name = "Tag", TargetProperty = "Signal", IsRequired = true, AllowDuplicates=true, DataType = XLDataType.Text },
                //    new ColumnDefinition { Name = "Comment", TargetProperty = "ID", IsRequired = true, AllowDuplicates=false, DataType = XLDataType.Text },
                //    new ColumnDefinition { Name = "Address", TargetProperty = "Address", IsRequired = true, AllowDuplicates=false, DataType = XLDataType.Text },
                //    new ColumnDefinition { Name = "Type", TargetProperty = "DataType", IsRequired = false, AllowDuplicates=false, DataType = XLDataType.Text }
                //}
            };
            Items = [];
        }

        /// <summary>
        /// Reads ZuLi data from the specified file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>A result containing the list of container entries.</returns>
        public override Result<List<ContainerEntry>> ReadFromFile(string path) => ReadFromFileInternal(path);

        /// <summary>
        /// Asynchronously reads ZuLi data from the specified file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>A task representing the asynchronous operation, with a result containing the list of container entries.</returns
        public override async Task<Result<List<ContainerEntry>>> ReadFromFileAsync(string path) => await Task.Run(() => ReadFromFileInternal(path));

        /// <summary>
        /// Internal method to read ZuLi data from the specified file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>A result containing the list of container entries.</returns>
        private Result<List<ContainerEntry>> ReadFromFileInternal(string path)
        {
            try
            {
                var result = base.ReadFromFile<ContainerEntry>(path, worksheetRuleSet);
                if (result.IsSuccess)
                {
                    var importedItems = result.Value.Where(item => !item.IsEmpty()).ToList();
                    foreach (var item in importedItems)
                    {
                        // Some supported PLC interface exports contain only
                        // Tag + Path and have no separate Comment/ID column.
                        // The signal text is a deterministic fallback; Address
                        // remains the primary stable identity where available.
                        if (string.IsNullOrWhiteSpace(item.ID))
                            item.ID = item.Signal;
                    }

                    Items = importedItems;
                    return Result<List<ContainerEntry>>.Success(importedItems);
                }

                return result;
            }
            catch (Exception ex)
            {
                return Result<List<ContainerEntry>>.Failure($"A unexpected failure occured: {ex.Message}");
            }
        }
    }
}
