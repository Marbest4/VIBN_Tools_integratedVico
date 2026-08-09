using ClosedXML.Excel;

namespace VIBN_Tools.ContainerGeneration.BusinessLogic.ExcelData
{
    /// <summary>
    /// Define column relevant properties used when reading data from a Excel worksheet.
    /// </summary>
    public class ColumnDefinition
    {
        /// <summary>
        /// Gets or sets the column name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the target property name to which the column refers to.
        /// </summary>
        public string TargetProperty { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating if the column is required or not.
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if duplicate entries in the column are allowed or not.
        /// </summary>
        public bool AllowDuplicates { get; set; }

        /// <summary>
        /// Gets or sets a value specifying the expected data type of the column.
        /// </summary>
        public XLDataType DataType { get; set; }
    }
}
