using ClosedXML.Excel;

namespace VIBN_Tools.ContainerGeneration.Utils
{
    /// <summary>
    /// Static class containing read methods for Excel files relevant to the application.
    /// </summary>
    public static class ExcelReader
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Reads an Excel file from the specified file path.
        /// This method reads the Excel file, logs the success or failure, and returns the result.
        /// </summary>
        /// <param name="excelPath">The path to the Excel file.</param>
        /// <returns>A <see cref="Result{T}"/> containing the Excel workbook or an error message.</returns>
        public static Result<IXLWorkbook> Read(string excelPath)
        {
            try
            {
                XLWorkbook workbook = new(excelPath);
                Logger.Info("Excel document at {path} read successfully", excelPath);
                return Result<IXLWorkbook>.Success(workbook);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while reading {path}", excelPath);
                return Result<IXLWorkbook>.Failure(ex.Message);
            }
        }
    }
}
