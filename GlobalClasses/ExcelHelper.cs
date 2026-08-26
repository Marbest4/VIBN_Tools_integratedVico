using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.IO;
using System.Windows;

namespace VIBN_Tools.GlobalClasses
{
    public static class ExcelHelper
    {


        public static XSSFWorkbook OpenExcelWorkbook(string FileName)
        {
            //using var fs = new FileStream(FileName, FileMode.Open, FileAccess.Read);
            //return new XSSFWorkbook(fs);

            try
            {
                using (FileStream fs = new FileStream(FileName, FileMode.Open, FileAccess.Read))
                {
                    return new XSSFWorkbook(fs);
                }
                ;
            }
            catch (FileNotFoundException e)
            {
                MessageBox.Show(e.ToString());
                return null;
            }
        }



        public static string GetCellString(IRow row, int columnIndex, DataFormatter formatter, IFormulaEvaluator evaluator)
        {
            if (row == null) return string.Empty;

            var cell = row.GetCell(columnIndex, MissingCellPolicy.CREATE_NULL_AS_BLANK);

            string value = formatter.FormatCellValue(cell, evaluator) ?? string.Empty;

            if (value == "###")
            {
                return String.Empty;
            }

            return value;
        }


        public static XSSFWorkbook CreateFeeImportWorkbook()
        {
            // Create new Workbook
            XSSFWorkbook workbook = new XSSFWorkbook();
            ISheet sheetInterfaceImport = workbook.CreateSheet("InterfaceSimExport");

            if (sheetInterfaceImport != null)
            {
                // Create Cell Style for first row
                ICellStyle boldFontCellStyle = workbook.CreateCellStyle();
                IFont boldFont = workbook.CreateFont();
                boldFont.IsBold = true;
                boldFontCellStyle.SetFont(boldFont);

                // Set Column width
                sheetInterfaceImport.SetColumnWidth(0, 17000);
                sheetInterfaceImport.SetColumnWidth(1, 1800);
                sheetInterfaceImport.SetColumnWidth(2, 16000);
                sheetInterfaceImport.SetColumnWidth(3, 15000);
                sheetInterfaceImport.SetColumnWidth(4, 1800);
                sheetInterfaceImport.SetColumnWidth(5, 1800);

                // Create Header Row
                var headerRow = sheetInterfaceImport.CreateRow(0);
                headerRow.CreateCell(0).SetCellValue("Tag");
                headerRow.CreateCell(1).SetCellValue("Address");
                headerRow.CreateCell(2).SetCellValue("Path");
                headerRow.CreateCell(3).SetCellValue("Comment");
                headerRow.CreateCell(4).SetCellValue("Type");
                headerRow.CreateCell(5).SetCellValue("Usage");

                // Set Cellstyle for Header Row
                headerRow.GetCell(0).CellStyle = boldFontCellStyle;
                headerRow.GetCell(1).CellStyle = boldFontCellStyle;
                headerRow.GetCell(2).CellStyle = boldFontCellStyle;
                headerRow.GetCell(3).CellStyle = boldFontCellStyle;
                headerRow.GetCell(4).CellStyle = boldFontCellStyle;
                headerRow.GetCell(5).CellStyle = boldFontCellStyle;
            }

            return workbook;

        }

        public static XSSFWorkbook CreatePcsImportWorkbook()
        {
            // Create new Workbook
            XSSFWorkbook workbook = new XSSFWorkbook();
            ISheet sheetInterfaceImport = workbook.CreateSheet("Sheet1");

            if (sheetInterfaceImport != null)
            {
                // Create Cell Style for first row
                ICellStyle boldFontCellStyle = workbook.CreateCellStyle();
                IFont boldFont = workbook.CreateFont();
                boldFont.IsBold = true;
                boldFontCellStyle.SetFont(boldFont);


                // Set Column width
                sheetInterfaceImport.SetColumnWidth(0, 17000);
                sheetInterfaceImport.SetColumnWidth(1, 1800);
                sheetInterfaceImport.SetColumnWidth(2, 1800);
                sheetInterfaceImport.SetColumnWidth(3, 1800);
                sheetInterfaceImport.SetColumnWidth(4, 17000);

                // Create Header Row
                var headerRow = sheetInterfaceImport.CreateRow(0);
                headerRow.CreateCell(0).SetCellValue("Signal Name");
                headerRow.CreateCell(1).SetCellValue("Type");
                headerRow.CreateCell(2).SetCellValue("Address");
                headerRow.CreateCell(3).SetCellValue("IEC Format");
                headerRow.CreateCell(4).SetCellValue("Comment");

                // Set Cellstyle for Header Row
                headerRow.GetCell(0).CellStyle = boldFontCellStyle;
                headerRow.GetCell(1).CellStyle = boldFontCellStyle;
                headerRow.GetCell(2).CellStyle = boldFontCellStyle;
                headerRow.GetCell(3).CellStyle = boldFontCellStyle;
                headerRow.GetCell(4).CellStyle = boldFontCellStyle;
            }

            return workbook;

        }



    }

}
