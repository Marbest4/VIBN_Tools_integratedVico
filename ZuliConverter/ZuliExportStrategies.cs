using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Windows;
using VIBN_Tools.GlobalClasses;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.ZuliConverter
{

    #region Export to fe.screen-sim (PLC)
    //===========================================================================================================================
    // E X P O R T   T O   F E . S C R E E N - S I M   ( P L C )
    //===========================================================================================================================

    public class BeckhoffExportStrategyFeScreenSim : IZuliExportStrategy<BeckhoffZuliLine>
    {
        public string ApplicationName => ApplicationType.FeScreenSim.ToString();

        public string RobotType => null;

        public XSSFWorkbook CreateWorkbook() => ExcelHelper.CreateFeeImportWorkbook();

        public void WriteLineToExcel(IRow row, BeckhoffZuliLine line, LanguageType selectedLanguage)
        {
            if (line.Symbolic == String.Empty)
            {
                MessageBox.Show($"Symbolic of Signal with Text '{line.TextLanguage1}' is empty");
                return;
            }

            var text = LanguageSelectionMap.Mapping[selectedLanguage](line);

            var path = line.Symbolic.StartsWith("i")
                        ? $"GVL_IO_Import.Igb{line.Symbolic.Substring(1)}"
                        : $"GVL_IO_Import.Ogb{line.Symbolic.Substring(1)}";

            var comment = $"{line.TextLanguage1}; sensor/actor-BMK: {line.DeviceIdSimple}; e-plan page: {line.PositioningConnectedDevice}";
            var type = char.ToUpper(line.DataType[0]) + line.DataType.Substring(1).ToLower();
            var usage = line.Symbolic.StartsWith("i") ? "Write" : "Read";

            row.CreateCell(0).SetCellValue(text);
            row.CreateCell(2).SetCellValue(path);
            row.CreateCell(3).SetCellValue(comment);
            row.CreateCell(4).SetCellValue(type);
            row.CreateCell(5).SetCellValue(usage);
        }

        void IZuliExportStrategyBase.WriteLineToExcel(IRow row, IZuliToInterface line, LanguageType selectedLanguage) => WriteLineToExcel(row, (BeckhoffZuliLine)line, selectedLanguage);
    }




    public class SiemensExportStrategyFeScreenSim : IZuliExportStrategy<SiemensZuliLine>
    {
        public string ApplicationName => ApplicationType.FeScreenSim.ToString();

        public string RobotType => null;

        public XSSFWorkbook CreateWorkbook() => ExcelHelper.CreateFeeImportWorkbook();

        public void WriteLineToExcel(IRow row, SiemensZuliLine line, LanguageType selectedLanguage)
        {
            var text = LanguageSelectionMap.Mapping[selectedLanguage](line);

            var usage = (line.Address.StartsWith("I") || line.Address.StartsWith("E")) ? "Write" : "Read";

            row.CreateCell(0).SetCellValue(text);
            row.CreateCell(1).SetCellValue(line.Address);
            row.CreateCell(3).SetCellValue(line.Symbolic);
            row.CreateCell(4).SetCellValue(line.DataType);
            row.CreateCell(5).SetCellValue(usage);
        }

        void IZuliExportStrategyBase.WriteLineToExcel(IRow row, IZuliToInterface line, LanguageType selectedLanguage) => WriteLineToExcel(row, (SiemensZuliLine)line, selectedLanguage);
    }




    public class TiaExportStrategyFeScreenSim : IZuliExportStrategy<TiaPlcTagLine>
    {
        public string ApplicationName => ApplicationType.FeScreenSim.ToString();

        public string RobotType => null;

        public XSSFWorkbook CreateWorkbook() => ExcelHelper.CreateFeeImportWorkbook();

        public void WriteLineToExcel(IRow row, TiaPlcTagLine line, LanguageType selectedLanguage)
        {
            var text = LanguageSelectionMap.Mapping[selectedLanguage](line);

            var usage = (line.Address.StartsWith("I") || line.Address.StartsWith("E")) ? "Write" : "Read";

            row.CreateCell(0).SetCellValue(text);
            row.CreateCell(1).SetCellValue(line.Address);
            row.CreateCell(3).SetCellValue(line.Symbolic);
            row.CreateCell(4).SetCellValue(line.DataType);
            row.CreateCell(5).SetCellValue(usage);
        }

        void IZuliExportStrategyBase.WriteLineToExcel(IRow row, IZuliToInterface line, LanguageType selectedLanguage) => WriteLineToExcel(row, (TiaPlcTagLine)line, selectedLanguage);
    }
    #endregion



    #region Export to fe.screen-sim (ABB)
    //===========================================================================================================================
    // E X P O R T   T O   F E . S C R E E N - S I M   ( A B B )
    //===========================================================================================================================

    public class BeckhoffExportStrategyFeScreenSimAbb : IZuliExportStrategy<BeckhoffZuliLine>
    {
        public string ApplicationName => ApplicationType.FeScreenSim.ToString();

        public string RobotType => GlobalClasses.RobotType.ABB.DisplayName();

        public XSSFWorkbook CreateWorkbook() => ExcelHelper.CreateFeeImportWorkbook();

        public void WriteLineToExcel(IRow row, BeckhoffZuliLine line, LanguageType selectedLanguage)
        {
            var text = LanguageSelectionMap.Mapping[selectedLanguage](line);

            if ((line.PlcAddress.Contains("PEW") || line.PlcAddress.Contains("PAW")) &&
                int.TryParse(line.PlcAddress.Substring(3), out int addressNumber))
            {
                int word = (addressNumber - 1) / 16;
                string address = $"Word{word}";
                string usage = line.PlcAddress.Contains("E") ? "Write" : "Read";

                row.CreateCell(0).SetCellValue(text);
                row.CreateCell(1).SetCellValue(address);
                row.CreateCell(3).SetCellValue(line.Symbolic ?? String.Empty);
                row.CreateCell(4).SetCellValue(line.DataType);
                row.CreateCell(5).SetCellValue(usage);

            }
            else
            {
                var usage = line.PlcAddress.StartsWith("I") ? "Write" : "Read";

                row.CreateCell(0).SetCellValue(text);
                row.CreateCell(1).SetCellValue(line.PlcAddress.Substring(1));
                row.CreateCell(3).SetCellValue(line.Symbolic ?? String.Empty);
                row.CreateCell(4).SetCellValue(line.DataType);
                row.CreateCell(5).SetCellValue(usage);
            }

        }

        void IZuliExportStrategyBase.WriteLineToExcel(IRow row, IZuliToInterface line, LanguageType selectedLanguage) => WriteLineToExcel(row, (BeckhoffZuliLine)line, selectedLanguage);
    }




    public class SiemensExportStrategyFeScreenSimAbb : IZuliExportStrategy<SiemensZuliLine>
    {
        public string ApplicationName => ApplicationType.FeScreenSim.ToString();

        public string RobotType => GlobalClasses.RobotType.ABB.DisplayName();

        public XSSFWorkbook CreateWorkbook() => ExcelHelper.CreateFeeImportWorkbook();

        public void WriteLineToExcel(IRow row, SiemensZuliLine line, LanguageType selectedLanguage)
        {
            var text = LanguageSelectionMap.Mapping[selectedLanguage](line);

            var usage = line.Address.StartsWith("E") ? "Write" : "Read";

            row.CreateCell(0).SetCellValue(text);
            row.CreateCell(1).SetCellValue(line.Address.Substring(1));
            row.CreateCell(3).SetCellValue(line.Symbolic ?? String.Empty);
            row.CreateCell(4).SetCellValue(line.DataType);
            row.CreateCell(5).SetCellValue(usage);

        }

        void IZuliExportStrategyBase.WriteLineToExcel(IRow row, IZuliToInterface line, LanguageType selectedLanguage) => WriteLineToExcel(row, (SiemensZuliLine)line, selectedLanguage);
    }




    public class TiaExportStrategyFeScreenSimAbb : IZuliExportStrategy<TiaPlcTagLine>
    {
        public string ApplicationName => ApplicationType.FeScreenSim.ToString();

        public string RobotType => GlobalClasses.RobotType.ABB.DisplayName();

        public XSSFWorkbook CreateWorkbook() => ExcelHelper.CreateFeeImportWorkbook();

        public void WriteLineToExcel(IRow row, TiaPlcTagLine line, LanguageType selectedLanguage)
        {
            var text = LanguageSelectionMap.Mapping[selectedLanguage](line);

            var usage = line.Address.StartsWith("E") ? "Write" : "Read";

            row.CreateCell(0).SetCellValue(text);
            row.CreateCell(1).SetCellValue(line.Address.Substring(1));
            row.CreateCell(3).SetCellValue(line.Symbolic ?? String.Empty);
            row.CreateCell(4).SetCellValue(line.DataType);
            row.CreateCell(5).SetCellValue(usage);

        }

        void IZuliExportStrategyBase.WriteLineToExcel(IRow row, IZuliToInterface line, LanguageType selectedLanguage) => WriteLineToExcel(row, (TiaPlcTagLine)line, selectedLanguage);
    }
    #endregion



    #region Export to fe.screen-sim (Fanuc)
    //===========================================================================================================================
    // E X P O R T   T O   F E . S C R E E N - S I M   ( F A N U C )
    //===========================================================================================================================

    public class BeckhoffExportStrategyFeScreenSimFanuc : IZuliExportStrategy<BeckhoffZuliLine>
    {
        public string ApplicationName => ApplicationType.FeScreenSim.ToString();

        public string RobotType => GlobalClasses.RobotType.Fanuc.DisplayName();

        public XSSFWorkbook CreateWorkbook() => ExcelHelper.CreateFeeImportWorkbook();

        public void WriteLineToExcel(IRow row, BeckhoffZuliLine line, LanguageType selectedLanguage)
        {
            var text = LanguageSelectionMap.Mapping[selectedLanguage](line);

            var address = line.PlcAddress.StartsWith("I") ?
                                 $"DIN[{line.PlcAddress.Substring(1)}]" :
                                 $"DOUT[{line.PlcAddress.Substring(1)}]";

            var usage = line.PlcAddress.StartsWith("I") ? "Write" : "Read";

            row.CreateCell(0).SetCellValue(text);
            row.CreateCell(1).SetCellValue(address);
            row.CreateCell(3).SetCellValue(line.Symbolic ?? String.Empty);
            row.CreateCell(4).SetCellValue(line.DataType);
            row.CreateCell(5).SetCellValue(usage);
        }

        void IZuliExportStrategyBase.WriteLineToExcel(IRow row, IZuliToInterface line, LanguageType selectedLanguage) => WriteLineToExcel(row, (BeckhoffZuliLine)line, selectedLanguage);
    }




    public class SiemensExportStrategyFeScreenSimFanuc : IZuliExportStrategy<SiemensZuliLine>
    {
        public string ApplicationName => ApplicationType.FeScreenSim.ToString();

        public string RobotType => GlobalClasses.RobotType.Fanuc.DisplayName();

        public XSSFWorkbook CreateWorkbook() => ExcelHelper.CreateFeeImportWorkbook();

        public void WriteLineToExcel(IRow row, SiemensZuliLine line, LanguageType selectedLanguage)
        {
            var text = LanguageSelectionMap.Mapping[selectedLanguage](line);

            var address = line.Address.StartsWith("E") ?
                                $"DIN[{line.Address.Substring(1)}]" :
                                $"DOUT[{line.Address.Substring(1)}]";

            var usage = line.Address.StartsWith("E") ? "Write" : "Read";

            row.CreateCell(0).SetCellValue(text);
            row.CreateCell(1).SetCellValue(address);
            row.CreateCell(3).SetCellValue(line.Symbolic ?? String.Empty);
            row.CreateCell(4).SetCellValue(line.DataType);
            row.CreateCell(5).SetCellValue(usage);


        }

        void IZuliExportStrategyBase.WriteLineToExcel(IRow row, IZuliToInterface line, LanguageType selectedLanguage) => WriteLineToExcel(row, (SiemensZuliLine)line, selectedLanguage);
    }




    public class TiaExportStrategyFeScreenSimFanuc : IZuliExportStrategy<TiaPlcTagLine>
    {
        public string ApplicationName => ApplicationType.FeScreenSim.ToString();

        public string RobotType => GlobalClasses.RobotType.Fanuc.DisplayName();

        public XSSFWorkbook CreateWorkbook() => ExcelHelper.CreateFeeImportWorkbook();

        public void WriteLineToExcel(IRow row, TiaPlcTagLine line, LanguageType selectedLanguage)
        {
            var text = LanguageSelectionMap.Mapping[selectedLanguage](line);

            var address = line.Address.StartsWith("E") ?
                                $"DIN[{line.Address.Substring(1)}]" :
                                $"DOUT[{line.Address.Substring(1)}]";

            var usage = line.Address.StartsWith("E") ? "Write" : "Read";

            row.CreateCell(0).SetCellValue(text);
            row.CreateCell(1).SetCellValue(address);
            row.CreateCell(3).SetCellValue(line.Symbolic ?? String.Empty);
            row.CreateCell(4).SetCellValue(line.DataType);
            row.CreateCell(5).SetCellValue(usage);


        }

        void IZuliExportStrategyBase.WriteLineToExcel(IRow row, IZuliToInterface line, LanguageType selectedLanguage) => WriteLineToExcel(row, (TiaPlcTagLine)line, selectedLanguage);
    }
    #endregion



    #region Export to fe.screen-sim (Kuka)
    //===========================================================================================================================
    // E X P O R T   T O   F E . S C R E E N - S I M   ( K U K A )
    //===========================================================================================================================

    public class BeckhoffExportStrategyFeScreenSimKuka : IZuliExportStrategy<BeckhoffZuliLine>
    {
        public string ApplicationName => ApplicationType.FeScreenSim.ToString();

        public string RobotType => GlobalClasses.RobotType.Kuka.DisplayName();

        public XSSFWorkbook CreateWorkbook() => ExcelHelper.CreateFeeImportWorkbook();

        public void WriteLineToExcel(IRow row, BeckhoffZuliLine line, LanguageType selectedLanguage)
        {
            var text = LanguageSelectionMap.Mapping[selectedLanguage](line);

            var address = line.PlcAddress.StartsWith("I") ?
                                 $"$IN[{line.PlcAddress.Substring(1)}]" :
                                 $"$OUT[{line.PlcAddress.Substring(1)}]";

            var usage = line.PlcAddress.StartsWith("I") ? "Write" : "Read";

            row.CreateCell(0).SetCellValue(text);
            row.CreateCell(1).SetCellValue(address);
            row.CreateCell(3).SetCellValue(line.Symbolic ?? String.Empty);
            row.CreateCell(4).SetCellValue(line.DataType);
            row.CreateCell(5).SetCellValue(usage);
        }

        void IZuliExportStrategyBase.WriteLineToExcel(IRow row, IZuliToInterface line, LanguageType selectedLanguage) => WriteLineToExcel(row, (BeckhoffZuliLine)line, selectedLanguage);
    }




    public class SiemensExportStrategyFeScreenSimKuka : IZuliExportStrategy<SiemensZuliLine>
    {
        public string ApplicationName => ApplicationType.FeScreenSim.ToString();

        public string RobotType => GlobalClasses.RobotType.Kuka.DisplayName();

        public XSSFWorkbook CreateWorkbook() => ExcelHelper.CreateFeeImportWorkbook();

        public void WriteLineToExcel(IRow row, SiemensZuliLine line, LanguageType selectedLanguage)
        {
            var text = LanguageSelectionMap.Mapping[selectedLanguage](line);

            var address = line.Address.StartsWith("E") ?
                                $"$IN[{line.Address.Substring(1)}]" :
                                $"$OUT[{line.Address.Substring(1)}]";

            var usage = line.Address.StartsWith("E") ? "Write" : "Read";

            row.CreateCell(0).SetCellValue(text);
            row.CreateCell(1).SetCellValue(address);
            row.CreateCell(3).SetCellValue(line.Symbolic ?? String.Empty);
            row.CreateCell(4).SetCellValue(line.DataType);
            row.CreateCell(5).SetCellValue(usage);

        }

        void IZuliExportStrategyBase.WriteLineToExcel(IRow row, IZuliToInterface line, LanguageType selectedLanguage) => WriteLineToExcel(row, (SiemensZuliLine)line, selectedLanguage);
    }




    public class TiaExportStrategyFeScreenSimKuka : IZuliExportStrategy<TiaPlcTagLine>
    {
        public string ApplicationName => ApplicationType.FeScreenSim.ToString();

        public string RobotType => GlobalClasses.RobotType.Kuka.DisplayName();

        public XSSFWorkbook CreateWorkbook() => ExcelHelper.CreateFeeImportWorkbook();

        public void WriteLineToExcel(IRow row, TiaPlcTagLine line, LanguageType selectedLanguage)
        {
            var text = LanguageSelectionMap.Mapping[selectedLanguage](line);

            var address = line.Address.StartsWith("E") ?
                                $"$IN[{line.Address.Substring(1)}]" :
                                $"$OUT[{line.Address.Substring(1)}]";

            var usage = line.Address.StartsWith("E") ? "Write" : "Read";

            row.CreateCell(0).SetCellValue(text);
            row.CreateCell(1).SetCellValue(address);
            row.CreateCell(3).SetCellValue(line.Symbolic ?? String.Empty);
            row.CreateCell(4).SetCellValue(line.DataType);
            row.CreateCell(5).SetCellValue(usage);

        }

        void IZuliExportStrategyBase.WriteLineToExcel(IRow row, IZuliToInterface line, LanguageType selectedLanguage) => WriteLineToExcel(row, (TiaPlcTagLine)line, selectedLanguage);
    }
    #endregion





    #region Export to ProcessSimulate (PLC)
    //===========================================================================================================================
    // E X P O R T   T O   P R O C E S S S I M U L A T E   ( P L C )
    //===========================================================================================================================

    public class BeckhoffExportStrategyProcessSimulate : IZuliExportStrategy<BeckhoffZuliLine>
    {
        public string ApplicationName => ApplicationType.ProcessSimulate.ToString();

        public string RobotType => throw new NotImplementedException();

        public XSSFWorkbook CreateWorkbook() => ExcelHelper.CreatePcsImportWorkbook();

        public void WriteLineToExcel(IRow row, BeckhoffZuliLine line, LanguageType selectedLanguage)
        {
            try
            {
                var text = LanguageSelectionMap.Mapping[selectedLanguage](line);

                bool isInput = line.Symbolic.StartsWith("i");
                string channel = ConvertChannelDesignation(line.ChannelDesignation, isInput);
                string deviceId = line.DeviceIdSimple.Replace("-", "_");

                var signalName = isInput ?
                    $"GVL_Hardware.{deviceId}.{channel}.HardwareDriverSim.Inputs.Input" :
                    $"GVL_Hardware.{deviceId}.{channel}.HardwareDriverSim.Outputs.Output";

                var type = char.ToUpper(line.DataType[0]) + line.DataType.Substring(1).ToLower();
                var address = "No Address";
                var format = line.Symbolic.StartsWith("i") ? "I" : "Q";
                var comment = $"// {line.SymbolicDetermined} - {text}";

                row.CreateCell(0).SetCellValue(signalName);
                row.CreateCell(1).SetCellValue(type);
                row.CreateCell(2).SetCellValue(address);
                row.CreateCell(3).SetCellValue(format);
                row.CreateCell(4).SetCellValue(comment);
            }
            catch (Exception)
            {

                MessageBox.Show($"Exception aufgetreten. Datatype: {line.DataType}, Symbolic: {line.Symbolic}, Channel: {line.ChannelDesignation}, device ID: {line.DeviceIdSimple}, symbolicDet: {line.SymbolicDetermined}");
            }


        }

        void IZuliExportStrategyBase.WriteLineToExcel(IRow row, IZuliToInterface line, LanguageType selectedLanguage) => WriteLineToExcel(row, (BeckhoffZuliLine)line, selectedLanguage);


        private static string ConvertChannelDesignation(string designation, bool isInput)
        {
            if (string.IsNullOrWhiteSpace(designation)) return designation;

            bool parsed = int.TryParse(designation, out var designationNumber);

            if (designation.Length == 1 && parsed)
            {
                return (isInput ? "DigitalInput" : "DigitalOutput") + (designationNumber + 1).ToString(); ;
            }
            else
            {
                designation = designation.Trim();
                designation = designation.Substring("Channel ".Length);

                return (isInput ? "DigitalInput" : "DigitalOutput") + designation;
            }


        }
    }




    public class SiemensExportStrategyProcessSimulate : IZuliExportStrategy<SiemensZuliLine>
    {
        public string ApplicationName => ApplicationType.ProcessSimulate.ToString();

        public string RobotType => null;

        public XSSFWorkbook CreateWorkbook() => ExcelHelper.CreatePcsImportWorkbook();



        public void WriteLineToExcel(IRow row, SiemensZuliLine line, LanguageType selectedLanguage)
        {
            throw new NotImplementedException();
        }

        void IZuliExportStrategyBase.WriteLineToExcel(IRow row, IZuliToInterface line, LanguageType selectedLanguage) => WriteLineToExcel(row, (SiemensZuliLine)line, selectedLanguage);
    }




    public class TiaExportStrategyProcessSimulate : IZuliExportStrategy<TiaPlcTagLine>
    {
        public string ApplicationName => ApplicationType.ProcessSimulate.ToString();

        public string RobotType => null;

        public XSSFWorkbook CreateWorkbook() => ExcelHelper.CreatePcsImportWorkbook();



        public void WriteLineToExcel(IRow row, TiaPlcTagLine line, LanguageType selectedLanguage)
        {
            throw new NotImplementedException();
        }

        void IZuliExportStrategyBase.WriteLineToExcel(IRow row, IZuliToInterface line, LanguageType selectedLanguage) => WriteLineToExcel(row, (TiaPlcTagLine)line, selectedLanguage);
    }

    #endregion








}
