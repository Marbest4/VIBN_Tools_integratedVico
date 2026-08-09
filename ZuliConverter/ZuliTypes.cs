using NPOI.SS.UserModel;
using VIBN_Tools.GlobalClasses;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.ZuliConverter
{

    public class BeckhoffZuliDefinition : IZuliTypeDefinition<BeckhoffZuliLine>
    {
        public string TypeName => "Beckhoff Zuli";
        public int SheetIndex => 1;
        public int HeaderRow => 5;
        public int FirstDataRow => 9;

        private string _scriptVersion;


        public bool Matches(IWorkbook workbook)
        {
            for (int i = 0; i < workbook.NumberOfSheets; i++)
            {
                var sheet = workbook.GetSheetAt(i);
                if (sheet.SheetName == "_GrobM_Eplan_Beckhoff_Zuli_")
                {
                    _scriptVersion = sheet.GetRow(1).GetCell(16, MissingCellPolicy.CREATE_NULL_AS_BLANK).ToString();
                    return true;
                }
            }
            return false;
        }

        public IZuliToInterface ParseRowGeneric(IRow row, DataFormatter formatter, IFormulaEvaluator evaluator) => ParseRow(row, formatter, evaluator);


        public BeckhoffZuliLine ParseRow(IRow row, DataFormatter formatter, IFormulaEvaluator evaluator)
        {
            var line = new BeckhoffZuliLine();
            if (_scriptVersion == "3.0")
            {
                line.Symbolic = ExcelHelper.GetCellString(row, 7, formatter, evaluator);
                line.SymbolicDetermined = ExcelHelper.GetCellString(row, 10, formatter, evaluator);
                line.DataType = ExcelHelper.GetCellString(row, 9, formatter, evaluator);
                line.DeviceIdSimple = ExcelHelper.GetCellString(row, 3, formatter, evaluator);
                line.PositioningIoAssembly = ExcelHelper.GetCellString(row, 11, formatter, evaluator);
                line.PositioningConnectedDevice = ExcelHelper.GetCellString(row, 12, formatter, evaluator);
                line.InstallationLocation = ExcelHelper.GetCellString(row, 2, formatter, evaluator);
                line.PlcAddress = ExcelHelper.GetCellString(row, 6, formatter, evaluator);
                line.TextLanguage1 = ExcelHelper.GetCellString(row, 17, formatter, evaluator);
                line.TextLanguage2 = ExcelHelper.GetCellString(row, 18, formatter, evaluator);
                line.TextLanguage3 = ExcelHelper.GetCellString(row, 19, formatter, evaluator);
                line.TextLanguage4 = ExcelHelper.GetCellString(row, 20, formatter, evaluator);
            }
            else if (_scriptVersion == "3.1")
            {
                line.Symbolic = ExcelHelper.GetCellString(row, 11, formatter, evaluator);
                line.SymbolicDetermined = ExcelHelper.GetCellString(row, 14, formatter, evaluator);
                line.DataType = ExcelHelper.GetCellString(row, 13, formatter, evaluator);
                line.DeviceId = ExcelHelper.GetCellString(row, 5, formatter, evaluator);
                line.DeviceIdSimple = ExcelHelper.GetCellString(row, 4, formatter, evaluator);
                line.PositioningIoAssembly = ExcelHelper.GetCellString(row, 15, formatter, evaluator);
                line.PositioningConnectedDevice = ExcelHelper.GetCellString(row, 16, formatter, evaluator);
                line.ChannelDesignation = ExcelHelper.GetCellString(row, 10, formatter, evaluator);
                line.InstallationLocation = ExcelHelper.GetCellString(row, 3, formatter, evaluator);
                line.PlcAddress = ExcelHelper.GetCellString(row, 9, formatter, evaluator);
                line.TextLanguage1 = ExcelHelper.GetCellString(row, 21, formatter, evaluator);
                line.TextLanguage2 = ExcelHelper.GetCellString(row, 22, formatter, evaluator);
                line.TextLanguage3 = ExcelHelper.GetCellString(row, 23, formatter, evaluator);
                line.TextLanguage4 = ExcelHelper.GetCellString(row, 24, formatter, evaluator);
            }
            if (VerifyLine(line))
            {
                return line;
            }

            return null;

        }

        public bool VerifyLine(IZuliToInterface line)
        {
            if (line is BeckhoffZuliLine zl)
            {
                if (zl.DataType == String.Empty)
                {
                    zl.DataType = "BOOL";
                }
                return !ExcludeTextsGerman.Any(ex => zl.TextLanguage1 == ex) && !ExcludeTextsEnglish.Any(ex => zl.TextLanguage1 == ex) && !ExcludeContainsTexts.Any(ex => zl.TextLanguage1.Contains(ex));
            }
            return false;
        }


        private static readonly List<string> ExcludeTextsGerman = new List<string>()
        {
            "Reserve",
            "RESERVE",
            "",
            "DP",
            "DPP",
            "Feldbus Kopfmodul",
            "n.c.",
            "Sender",
            "sender",
            "Transmitter",
            "transmitter",
            "DVI",
        };

        private static readonly List<string> ExcludeTextsEnglish = new List<string>()
        {
            "Not Used",
            "Spare",
            "SPARE"

        };

        private static readonly List<string> ExcludeContainsTexts = new List<string>()
        {
            "MSO",
            "Sender",
            "sender",
        };
    }


    [ZuliDisplayName("Beckhoff Zuli")]
    public class BeckhoffZuliLine : IZuliToInterface
    {
        public string Symbolic { get; set; }
        public string SymbolicDetermined { get; set; }
        public string DataType { get; set; }
        public string DeviceId { get; set; }                    // BMW Bezeichnung
        public string DeviceIdSimple { get; set; }              // nur übergeordnete BMW Bezeichnung
        public string PositioningIoAssembly { get; set; }
        public string PositioningConnectedDevice { get; set; }  // E-Plan Seite
        public string ChannelDesignation { get; set; }
        public string InstallationLocation { get; set; }
        public string PlcAddress { get; set; }
        public string TextLanguage1 { get; set; }
        public string TextLanguage2 { get; set; }
        public string TextLanguage3 { get; set; }
        public string TextLanguage4 { get; set; }

    }








    public class SiemensZuliDefinition : IZuliTypeDefinition<SiemensZuliLine>
    {

        public string TypeName => "Siemens Zuli";
        public int SheetIndex => 1;
        public int HeaderRow => 0;
        public int FirstDataRow => 1;


        public bool Matches(IWorkbook workbook)
        {
            for (int i = 0; i < workbook.NumberOfSheets; i++)
            {
                var sheet = workbook.GetSheetAt(i);
                if (sheet.SheetName.Contains("Zuli"))
                {
                    return true;
                }
            }
            return false;
        }


        public IZuliToInterface ParseRowGeneric(IRow row, DataFormatter formatter, IFormulaEvaluator evaluator) => ParseRow(row, formatter, evaluator);

        public SiemensZuliLine ParseRow(IRow row, DataFormatter formatter, IFormulaEvaluator evaluator)
        {
            var line = new SiemensZuliLine
            {
                Symbolic = ExcelHelper.GetCellString(row, 2, formatter, evaluator),
                Address = ExcelHelper.GetCellString(row, 0, formatter, evaluator)
                                    .Replace("%", "")
                                    .Replace(" ", "")
                                    .Replace("O", "A")
                                    .Replace("Q", "A")
                                    .Replace("I", "E"),
                DataType = ExcelHelper.GetCellString(row, 11, formatter, evaluator),
                DeviceId = ExcelHelper.GetCellString(row, 1, formatter, evaluator),
                DeviceIdSimple = ExcelHelper.GetCellString(row, 9, formatter, evaluator),
                TextLanguage1 = ExcelHelper.GetCellString(row, 4, formatter, evaluator),
                TextLanguage2 = ExcelHelper.GetCellString(row, 5, formatter, evaluator),
                TextLanguage3 = ExcelHelper.GetCellString(row, 6, formatter, evaluator),
                TextLanguage4 = ExcelHelper.GetCellString(row, 7, formatter, evaluator),
            };

            if (VerifyLine(line))
            {
                return line;
            }

            return null;

        }



        public bool VerifyLine(IZuliToInterface line)
        {
            if (line is SiemensZuliLine zl)
            {
                if (zl.DataType == String.Empty)
                {
                    zl.DataType = "BOOL";
                }
                return !ExcludeTextsGerman.Any(ex => zl.TextLanguage1 == ex) && !ExcludeTextsEnglish.Any(ex => zl.TextLanguage1 == ex) && !ExcludeContainsTexts.Any(ex => zl.TextLanguage1.Contains(ex));
            }
            return false;
        }


        private static readonly List<string> ExcludeTextsGerman = new List<string>()
        {
            "Reserve",
            "RESERVE",
            "",
        };

        private static readonly List<string> ExcludeTextsEnglish = new List<string>()
        {
            "Not Used",
            "Spare",
            "SPARE"

        };

        private static readonly List<string> ExcludeContainsTexts = new List<string>()
        {
            "MSO",
        };

    }



    [ZuliDisplayName("Siemens Zuli")]
    public class SiemensZuliLine : IZuliToInterface
    {
        public string Symbolic { get; set; }
        public string DeviceId { get; set; }            // BMK mit Pin
        public string DeviceIdSimple { get; set; }      // BMK für Beschilderung
        public string Address { get; set; }
        public string DataType { get; set; }
        public string TextLanguage1 { get; set; }
        public string TextLanguage2 { get; set; }
        public string TextLanguage3 { get; set; }
        public string TextLanguage4 { get; set; }


    }






    public class TiaPlcTagsDefinition : IZuliTypeDefinition<TiaPlcTagLine>
    {

        public string TypeName => "Tia Plc Tags";
        public int SheetIndex => 0;
        public int HeaderRow => 0;
        public int FirstDataRow => 1;


        public bool Matches(IWorkbook workbook)
        {
            for (int i = 0; i < workbook.NumberOfSheets; i++)
            {
                var sheet = workbook.GetSheetAt(i);
                if (sheet.SheetName.Contains("PLC Tags"))
                {
                    return true;
                }
            }
            return false;
        }


        public IZuliToInterface ParseRowGeneric(IRow row, DataFormatter formatter, IFormulaEvaluator evaluator) => ParseRow(row, formatter, evaluator);

        public TiaPlcTagLine ParseRow(IRow row, DataFormatter formatter, IFormulaEvaluator evaluator)
        {
            var line = new TiaPlcTagLine()
            {
                Symbolic = ExcelHelper.GetCellString(row, 0, formatter, evaluator),
                Address = ExcelHelper.GetCellString(row, 3, formatter, evaluator)
                                    .Replace("%", "")
                                    .Replace(" ", "")
                                    .Replace("O", "A")
                                    .Replace("Q", "A")
                                    .Replace("I", "E"),
                DataType = ExcelHelper.GetCellString(row, 2, formatter, evaluator),
                TextLanguage1 = ExcelHelper.GetCellString(row, 4, formatter, evaluator),
                TagTable = ExcelHelper.GetCellString(row, 1, formatter, evaluator),

            };

            if (VerifyLine(line))
            {
                return line;
            }

            return null;

        }



        public bool VerifyLine(IZuliToInterface line)
        {
            if (line is TiaPlcTagLine tl)
            {
                if (tl.DataType == String.Empty)
                {
                    tl.DataType = "BOOL";
                }
                return !ExcludeTextsGerman.Any(ex => tl.TextLanguage1 == ex) && !ExcludeTextsEnglish.Any(ex => tl.TextLanguage1 == ex) && !ExcludeContainsTexts.Any(ex => tl.TextLanguage1.Contains(ex));
            }
            return false;
        }


        private static readonly List<string> ExcludeTextsGerman = new List<string>()
        {
            "Reserve",
            "RESERVE",
            "",
        };

        private static readonly List<string> ExcludeTextsEnglish = new List<string>()
        {
            "Not Used",
            "Spare",
            "SPARE"

        };

        private static readonly List<string> ExcludeContainsTexts = new List<string>()
        {
            "MSO",
        };

    }



    [ZuliDisplayName("Tia Plc Tags")]
    public class TiaPlcTagLine : IZuliToInterface
    {

        public string Symbolic { get; set; }
        public string TextLanguage1 { get; set; }
        public string TextLanguage2 { get; set; }
        public string TextLanguage3 { get; set; }
        public string TextLanguage4  { get; set; }

        public string TagTable { get; set; }            // Name der Variablentabelle
        public string DataType { get; set; }
        public string Address { get; set; }


    }




}