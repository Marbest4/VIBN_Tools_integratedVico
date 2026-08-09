using VIBN_Tools.ContainerGeneration.BusinessLogic.ExcelData;

namespace VIBN_Tools.ContainerGeneration.BusinessLogic.ZuLiData.Interfaces
{
    /// <summary>
    /// Define the interface for a rule set which is used to read data from a excel worksheet.
    /// </summary>
    public interface IWorksheetRuleSet
    {
        /// <summary>
        /// Gets or sets the worksheet number (containing the relevant data).
        /// </summary>
        public int WorksheetNumber { get; }

        /// <summary>
        /// Gets or sets the row which contains the column header (of the relevant data table).
        /// </summary>
        public int ColumnHeaderRow { get; }

        /// <summary>
        /// Gets or sets a list of <see cref="ColumnDefinition"/> specifying the columns containing the relevant data.
        /// </summary>
        public List<ColumnDefinition> ColumnDefinitions { get; }
    }
}
