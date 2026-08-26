using Microsoft.Win32;
using NPOI.XSSF.UserModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.ZuliConverter;
using static VIBN_Tools.GlobalClasses.Interfaces;
using MvvmBase = VIBN_Tools.GlobalClasses.MvvmBase;
using Path = System.IO.Path;
using RobotType = VIBN_Tools.GlobalClasses.RobotType;

namespace VIBN_Tools.Application.VM
{
    /// <summary>
    /// Owns the import options and conversion command for ZuLi/interface data.
    /// Format-specific writing belongs to the converter strategies, not to the
    /// WPF view model.
    /// </summary>
    public class ZuliConverterPageVM : MvvmBase
    {

        //===========================================================================================================================
        // B I N D I N G S   -   I M P O R T   F I L E
        //===========================================================================================================================

        // Textbox Filename Interface Information
        private string _fileNameZuli;
        public string FileNameZuli
        {
            get { return _fileNameZuli; }
            set
            {
                _fileNameZuli = value;
                OnPropertyChanged();
            }
        }


        // Button Open Container XML
        public ICommand OpenZuliFile => GetCommandBindingAsync(Open_ZuliFile);


        private bool _enableOpenZuli;
        public bool EnableOpenZuliFile
        {
            get { return _enableOpenZuli; }
            set
            {
                _enableOpenZuli = value;
                OnPropertyChanged();
            }
        }



        //===========================================================================================================================
        // B I N D I N G S   -   O P T I O N S
        //===========================================================================================================================

        public ObservableCollection<OptionsViewModelBase> OptionFields { get; }
        private OptionsViewModel<ApplicationType> _outputApplicationOption { get; set; }
        private OptionsViewModel<LanguageType> _languageOption { get; set; }
        private OptionsViewModel<RobotType> _robotoption { get; set; }


        public ObservableCollection<LanguageType> AvailableLanguages { get; set; }








        //===========================================================================================================================
        // B I N D I N G S   -   S T A T U S   I N F O R M A T I O N
        //===========================================================================================================================

        public ObservableCollection<StatusViewModel> StatusFields { get; }


        // Button Open Container XML
        public ICommand CreateImportFile { get; }


        private bool _isBusyCreateImportFile;
        public bool IsBusyCreateImportFile
        {
            get { return _isBusyCreateImportFile; }
            set
            {
                _isBusyCreateImportFile = value;
                OnPropertyChanged();
            }
        }







        //===========================================================================================================================
        // P R O P E R T I E S   O F   V I E W - M O D E L
        //===========================================================================================================================

        public List<IZuliToInterface> ZuliLines { get; set; }

        public List<IZuliToInterface> PlcLines { get; set; }
        public List<IZuliToInterface> RobotLines { get; set; }




        private XSSFWorkbook _excelImport;




        private readonly Dictionary<(ApplicationType? App, Type LineType, RobotType?), IZuliExportStrategyBase> _exportStrategies = new Dictionary<(ApplicationType? App, Type LineType, RobotType?), IZuliExportStrategyBase>()
        {
            // PLC
            { (ApplicationType.FeScreenSim, typeof(BeckhoffZuliLine), null), new BeckhoffExportStrategyFeScreenSim() },
            { (ApplicationType.FeScreenSim, typeof(SiemensZuliLine), null), new SiemensExportStrategyFeScreenSim() },
            { (ApplicationType.FeScreenSim, typeof(TiaPlcTagLine), null), new TiaExportStrategyFeScreenSim() },

            { (ApplicationType.ProcessSimulate, typeof(BeckhoffZuliLine), null), new BeckhoffExportStrategyProcessSimulate() },

            // Robot ABB
            { (ApplicationType.FeScreenSim, typeof(BeckhoffZuliLine), RobotType.ABB), new BeckhoffExportStrategyFeScreenSimAbb() },
            { (ApplicationType.FeScreenSim, typeof(SiemensZuliLine), RobotType.ABB), new SiemensExportStrategyFeScreenSimAbb() },
            { (ApplicationType.FeScreenSim, typeof(TiaPlcTagLine), RobotType.ABB), new TiaExportStrategyFeScreenSimAbb() },


            // Robot Fanuc
            { (ApplicationType.FeScreenSim, typeof(BeckhoffZuliLine), RobotType.Fanuc), new BeckhoffExportStrategyFeScreenSimFanuc() },
            { (ApplicationType.FeScreenSim, typeof(SiemensZuliLine), RobotType.Fanuc), new SiemensExportStrategyFeScreenSimFanuc() },
            { (ApplicationType.FeScreenSim, typeof(TiaPlcTagLine), RobotType.Fanuc), new TiaExportStrategyFeScreenSimFanuc() },


            // Robot KUKA
            { (ApplicationType.FeScreenSim, typeof(BeckhoffZuliLine), RobotType.Kuka), new BeckhoffExportStrategyFeScreenSimKuka() },
            { (ApplicationType.FeScreenSim, typeof(SiemensZuliLine), RobotType.Kuka), new SiemensExportStrategyFeScreenSimKuka() },
            { (ApplicationType.FeScreenSim, typeof(TiaPlcTagLine), RobotType.Kuka), new TiaExportStrategyFeScreenSimKuka() },

        };















        //===========================================================================================================================
        // C O N S T R U C T O R
        //===========================================================================================================================

        public ZuliConverterPageVM()
        {
            ZuliLines = new List<IZuliToInterface>();
            PlcLines = new List<IZuliToInterface>();
            RobotLines = new List<IZuliToInterface>();

            AvailableLanguages = new ObservableCollection<LanguageType>();

            // Command Initialise
            CreateImportFile = new AsyncCommandHandler(
                para => Create_ImportFile(para),
                para => CanCreateImportFile());


            // Initialise Option Fields
            _outputApplicationOption = new OptionsViewModel<ApplicationType>()
            {
                Label = "Output Application:",
                ViewElement = ViewElement.Combobox,
                Items = new ObservableCollection<ApplicationType>(Enum.GetValues<ApplicationType>()),
                ViewElementEnabled = false,
                ValueObject = null,
            };
            _outputApplicationOption.PropertyChanged += Option_PropertyChanged;

            _languageOption = new OptionsViewModel<LanguageType>()
            {
                Label = "Language:",
                ViewElement = ViewElement.Combobox,
                Items = new ObservableCollection<LanguageType>(Enum.GetValues<LanguageType>()),
                ViewElementEnabled = false,
                ValueObject = null,
            };
            _languageOption.PropertyChanged += Option_PropertyChanged;

            _robotoption = new OptionsViewModel<RobotType>()
            {
                Label = "Robot Type:",
                ViewElement = ViewElement.Combobox,
                Items = new ObservableCollection<RobotType>(Enum.GetValues<RobotType>()),
                ViewElementEnabled = false,
                ValueObject = null,
            };
            _robotoption.PropertyChanged += Option_PropertyChanged;

            OptionFields = new ObservableCollection<OptionsViewModelBase>()
            {
                _outputApplicationOption,
                _languageOption,
                _robotoption,
            };

            StatusFields = new ObservableCollection<StatusViewModel>()
            {
                new StatusViewModel(){Label = "Type of Import-File:", ViewElement = ViewElement.Textblock},
                new StatusViewModel(){Label = "Total Zuli lines read in:", ViewElement = ViewElement.Textblock},
                new StatusViewModel(){Label = "PLC Signals:", ViewElement = ViewElement.Textblock},
                new StatusViewModel(){Label = "Robot Signals:", ViewElement = ViewElement.Textblock},
            };

        }








        //===========================================================================================================================
        // M E T H O D S
        //===========================================================================================================================

        private async Task<bool> Open_ZuliFile(object parameter)
        {
            _outputApplicationOption.ViewElementEnabled = false;
            _languageOption.ViewElementEnabled = false;
            _robotoption.ViewElementEnabled = false;

            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Filter = "Excel files (*.xls;*.xlsx;*.xlsm)|*xls;*.xlsx;*.xlsm|XML files (*.xml)|*.xml|All files (*.*)|*.*";
            openFile.Title = "Select Interface Information File";

            if (openFile.ShowDialog() == true)
            {
                FileNameZuli = openFile.FileName;
                _excelImport = ExcelHelper.OpenExcelWorkbook(FileNameZuli);

                var matchingDef = ZuliConverterService.DetectZuliType(_excelImport);

                if (matchingDef != null)
                {
                    ZuliLines = ZuliConverterService.Parse(_excelImport, matchingDef);

                    // Update available languages
                    CheckAvailableLanguages();
                }


                // Separate into Plc- and Robot-Lines
                if (matchingDef is IZuliTypeDefinition<BeckhoffZuliLine>)
                {
                    RobotLines = ZuliLines
                        .OfType<BeckhoffZuliLine>()
                        .Where(x => x.InstallationLocation.Contains("ROB"))
                        .Cast<IZuliToInterface>()
                        .ToList();

                    PlcLines = ZuliLines.Except(RobotLines).ToList();
                }
                else if (matchingDef is IZuliTypeDefinition<SiemensZuliLine>)
                {
                    RobotLines = ZuliLines
                        .OfType<SiemensZuliLine>()
                        .Where(x => !x.Address.Contains(".") && !x.Address.Contains('B') && !x.Address.Contains('W') && !x.Address.Contains('D') && x.Address != string.Empty)
                        .Cast<IZuliToInterface>()
                        .ToList();

                    PlcLines = ZuliLines.Except(RobotLines).ToList();
                }
                else if (matchingDef is IZuliTypeDefinition<TiaPlcTagLine>)
                {
                    RobotLines = ZuliLines
                        .OfType<TiaPlcTagLine>()
                        .Where(x => !x.Address.Contains(".") && !x.Address.Contains('B') && !x.Address.Contains('W') && !x.Address.Contains('D') && x.Address != string.Empty)
                        .Cast<IZuliToInterface>()
                        .ToList();

                    PlcLines = ZuliLines.Except(RobotLines).ToList();
                }

                // Update Options
                if (ZuliLines.Count > 0)
                {
                    _outputApplicationOption.ViewElementEnabled = true;
                    _languageOption.ViewElementEnabled = true;
                }
                if (RobotLines.Count > 0)
                {
                    _robotoption.ViewElementEnabled = true;
                }

                // Update Status Info
                UpdateStatusValues();

                // Evaluate Export Button
                RaiseCreateImportFileCanExecuteChanged();

                //FileLoaded = true;
                return true;
            }
            return false;
        }


        private async Task<bool> Create_ImportFile(object parameter)
        {
            IsBusyCreateImportFile = true;

            var oldFileName = Path.GetFileNameWithoutExtension(FileNameZuli);
            var plcFileName = $"PLC-Interface_{oldFileName}.xlsx";
            var robotFileName = $"Robot-Interface_{oldFileName}.xlsx";

            // PLC-Export
            if (PlcLines.Any())
            {
                ExportLines(PlcLines, plcFileName, null);
            }

            // Robot-Export
            if (RobotLines.Any())
            {
                ExportLines(RobotLines, robotFileName, _robotoption.Value);
            }

            IsBusyCreateImportFile = false;
            return true;

        }







        //===========================================================================================================================
        // M E T H O D S   ( H E L P E R S )
        //===========================================================================================================================



        private void CheckAvailableLanguages()
        {
            AvailableLanguages.Clear();

            foreach (var entry in LanguageSelectionMap.Mapping)
            {
                if (ZuliLines.Any(l => !string.IsNullOrWhiteSpace(entry.Value(l))))
                {
                    AvailableLanguages.Add(entry.Key);
                }
            }

            // Update items of language option
            _languageOption.Items = new ObservableCollection<LanguageType>(AvailableLanguages);

            // Reset selection if current value is no longer valid
            if (!_languageOption.Items.Contains(_languageOption.Value ?? default))
                _languageOption.Value = null;


        }




        /// <summary>
        /// Function updates the Status Information values
        /// </summary>
        private void UpdateStatusValues()
        {
            var firstLine = ZuliLines.FirstOrDefault();
            if (firstLine != null)
            {
                StatusFields[0].ValueObject = GetZuliDisplayName(firstLine.GetType());
            }

            StatusFields[1].ValueObject = ZuliLines.Count();
            StatusFields[2].ValueObject = PlcLines.Count();
            StatusFields[3].ValueObject = RobotLines.Count();
        }

        private static string GetZuliDisplayName(Type type)
        {
            var attr = type.GetCustomAttributes(typeof(ZuliDisplayNameAttribute), false)
                           .FirstOrDefault() as ZuliDisplayNameAttribute;

            return attr?.DisplayName ?? type.Name; // Fallback: Class name
        }






        private bool CanCreateImportFile()
        {
            bool baseConditions = ZuliLines.Count > 0
                && _outputApplicationOption?.Value != null
                && _languageOption?.Value != null;

            if (RobotLines == null || RobotLines.Count() == 0)
            {
                return baseConditions;
            }

            return baseConditions && _robotoption?.Value != null;
        }

        private void RaiseCreateImportFileCanExecuteChanged()
        {
            (CreateImportFile as AsyncCommandHandler)?.RaiseCanExecuteChanged();
        }

        private void Option_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Value")
                RaiseCreateImportFileCanExecuteChanged();
        }





        private bool ExportLines(IEnumerable<IZuliToInterface> lines, string defaultFileName, RobotType? robotType = null)
        {
            var firstLine = lines.FirstOrDefault();
            if (firstLine == null)
                return false;

            var key = (_outputApplicationOption.Value, firstLine.GetType(), robotType);

            if (!_exportStrategies.TryGetValue(key, out var strategy))
            {
                MessageBox.Show($"No Export-Strategy found for Application \"{_outputApplicationOption.Value}\", " +
                                $"Robot-Type \"{robotType?.ToString()}\" and Zuli-Type \"{GetZuliDisplayName(firstLine.GetType())}\"");

                return false;
            }


            using (var workbook = strategy.CreateWorkbook())
            {
                var sheet = workbook.GetSheetAt(0);
                int rowIndex = 1;

                foreach (var line in lines)
                {
                    var row = sheet.CreateRow(rowIndex++);
                    strategy.WriteLineToExcel(row, line, _languageOption.Value!.Value);
                }

                var saveDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                    Title = "Select Location to Save File",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    FileName = defaultFileName
                };

                if (saveDialog.ShowDialog() != true)
                    return false;

                using (var fs = File.Open(saveDialog.FileName, FileMode.Create, FileAccess.Write))
                {
                    workbook.Write(fs);
                }
            }

            return true;
        }



    }













    //public class OptionsViewModel : MvvmBase
    //{
    //    public string Label { get; set; }
    //    public ViewElement ViewElement { get; set; }


    //    private bool _viewElementEnabled;
    //    public bool ViewElementEnabled
    //    {
    //        get { return _viewElementEnabled; }
    //        set
    //        {
    //            _viewElementEnabled = value;
    //            OnPropertyChanged();
    //        }
    //    }


    //    private object _value;
    //    public object Value
    //    {
    //        get { return _value; }
    //        set
    //        {
    //            _value = value;
    //            OnPropertyChanged();
    //        }
    //    }
    //    public ObservableCollection<string> Items { get; set; } // for Combobox
    //}


    //public enum ViewElement
    //{
    //    Textblock,
    //    Textbox,
    //    Combobox,
    //    Checkbox,
    //}







}
