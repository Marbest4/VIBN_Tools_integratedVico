using VIBN_Tools.ContainerGeneration.BusinessLogic.ZuLiData.Interfaces;

namespace VIBN_Tools.ContainerGeneration.BusinessLogic.ExcelData
{
    /// <summary>
    /// Define a rule set which is used to read data from a excel worksheet.
    /// </summary>
    public class WorksheetRuleSet : IWorksheetRuleSet
    {
        public int WorksheetNumber { get; set; }

        public int ColumnHeaderRow { get; set; }

        public List<ColumnDefinition> ColumnDefinitions { get; set; } = new();
    }
}
