using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
//using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using FS.SDK.Scene.Objects;
using Microsoft.Win32;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;
using VIBN_Tools.ModelControl;

namespace VIBN_Tools.Application.VM
{
    /// <summary>
    /// Coordinates interactive control of robots, axes and selected FEE
    /// objects. Motion implementations stay in ModelControl/RobotControl so
    /// UI state is not coupled to device-specific SDK calls.
    /// </summary>
    public class ModelControlPageVM : MvvmBase
    {

        #region Constructor & View-Model Properties
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///                                                                                                                      ///   
        ///     C O N S T R U C T O R   &   V I E W   M O D E L   P R O P E R T I E S                                            ///
        ///                                                                                                                      ///
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        //===========================================================================================================================
        // C O N S T R U C T O R
        //===========================================================================================================================

        public ModelControlPageVM()
        {
            // Fee Objects
            if (Services.FeeObjects.AllFeeObjects?.Any() == true)
            {
                OnFeeObjectsChanged(this, new FeeObjectsUpdatedEventargs());
            }

            // Trigger for Fee Objects updated
            Services.FeeObjects.FeeObjectsUpdated += OnFeeObjectsChanged;



            // Robot Control
            EnableOpenRobotCsv = true;

            _robotMotionController = new ModelControlMotionService();

            _robotMotionController.StatusChanged += OnStatusChanged;
            _robotMotionController.JointValuesUpdated += OnJointValuesUpdated;
            _robotMotionController.MovingStateChanged += OnRobotMovingStateChanged;


            // Axis Control
            EnableOpenAxisCompositionCsv = true;

            CompositionData = new ObservableCollection<AxisCompositionData>();
            CompositionPositions = new ObservableCollection<AxisCompositionPositionsData>();

            AxisSelections = new ObservableCollection<AxisSelectionViewModel>();
            SelectableAxisCompositionJoints = new ObservableCollection<FeeJoint>();

            _axisCompositionMotionController = new ModelControlMotionService();
            _axisCompositionMotionController.StatusChanged += OnStatusChanged;
            _axisCompositionMotionController.JointValuesUpdated += OnJointValuesUpdated;
            _axisCompositionMotionController.MovingStateChanged += OnAxisCompositionMovingStateChanged;


            // Object Control
            ControllableObjects = new ObservableCollection<FeeAbstractObject>();

            ExpanderView = CollectionViewSource.GetDefaultView(ControllableObjects);
            ExpanderView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(FeeAbstractObject.FeeType)));
            ExpanderView.SortDescriptions.Add(new SortDescription(nameof(FeeAbstractObject.Name), ListSortDirection.Ascending));
            ExpanderView.Filter = FilterControllableObjects;

            // Configure and initialise Debounce-Timer
            _debounceTimerObjectControlFilter = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _debounceTimerObjectControlFilter.Tick += (sender, eventArgs) =>
            {
                _debounceTimerObjectControlFilter.Stop();
                ExpanderView.Refresh();
            };



        }


        //===========================================================================================================================
        // P R O P E R T I E S   O F   V I E W - M O D E L
        //===========================================================================================================================

        // General
        private bool _isBusyUpdatingFeeData;
        public bool IsBusyUpdatingFeeData
        {
            get { return _isBusyUpdatingFeeData; }
            set
            {
                _isBusyUpdatingFeeData = value;
                OnPropertyChanged();
            }
        }

        // Robot Control
        private readonly ModelControlMotionService _robotMotionController;


        // Object Control
        private readonly ModelControlMotionService _axisCompositionMotionController;



        //===========================================================================================================================
        // F U N C T I O N S   O F   V I E W - M O D E L
        //===========================================================================================================================

        private async Task Reload_FeeDataAsync()
        {
            IsBusyUpdatingFeeData = true;

            await Services.FeeObjects.UpdateFeeDataAsync();
        }


        private void OnFeeObjectsChanged(object sender, FeeObjectsUpdatedEventargs e)
        {
            UpdateSimRobotsList();
            UpdateControllableObjectsList();
            UpdateSelectableAxisList();

            IsBusyUpdatingFeeData = false;
        }




        //===========================================================================================================================
        // E V E N T S
        //===========================================================================================================================

        private void OnStatusChanged(string message, Severity severity)
        {
            SetStatus(message, severity);
        }

        private void OnJointValuesUpdated(double[] values)
        {
            ActualPositionJ1 = Safe(values, 0);
            ActualPositionJ2 = Safe(values, 1);
            ActualPositionJ3 = Safe(values, 2);
            ActualPositionJ4 = Safe(values, 3);
            ActualPositionJ5 = Safe(values, 4);
            ActualPositionJ6 = Safe(values, 5);
            ActualPositionJ7 = Safe(values, 6);
        }

        private float Safe(double[] arr, int index)
        {
            return index < arr.Length ? (float)Math.Round(arr[index], 3) : 0f;
        }



        #endregion





        #region Robot Control
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///                                                                                                                      ///   
        ///     R O B O T   C O N T R O L   -   B I N D I N G S   &   F U N C T I O N S                                          ///
        ///                                                                                                                      ///
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        //===========================================================================================================================
        // F I L E   I M P O R T
        //===========================================================================================================================

        // Textbox Filename Robot CSV
        private string _fileNameRobotCsv;
        public string FileNameRobotCsv
        {
            get { return _fileNameRobotCsv; }
            set
            {
                _fileNameRobotCsv = value;
                OnPropertyChanged();
            }
        }

        private bool _enableOpenRobotCsv;
        public bool EnableOpenRobotCsv
        {
            get { return _enableOpenRobotCsv; }
            set
            {
                _enableOpenRobotCsv = value;
                OnPropertyChanged();
            }
        }


        // Button Open Robot CSV
        public ICommand OpenRobotCsv => GetCommandBindingAsync(Open_RobotCsv);





        //===========================================================================================================================
        // R O B O T   C O N T R O L
        //===========================================================================================================================


        public ObservableCollection<RobotControlData> RobotsData { get; } = new();
        public ObservableCollection<SimRobotDefinition> SimRobots { get; set; } = new();



        private RobotControlData _selectedRobot;
        public RobotControlData SelectedRobot
        {
            get => _selectedRobot;
            set
            {
                if (_selectedRobot != value)
                {
                    _selectedRobot = value;
                    OnPropertyChanged(nameof(SelectedRobot));
                }
            }
        }

        private RobotControlPath _selectedPath;
        public RobotControlPath SelectedPath
        {
            get => _selectedPath;
            set
            {
                if (_selectedPath != value)
                {
                    _selectedPath = value;
                    OnPropertyChanged(nameof(SelectedPath));
                }
            }
        }

        private RobotControlPosition _selectedPosition;
        public RobotControlPosition SelectedPosition
        {
            get => _selectedPosition;
            set
            {
                if (_selectedPosition != value)
                {
                    _selectedPosition = value;
                    OnPropertyChanged(nameof(SelectedPosition));
                }
            }
        }


        private SimRobotDefinition _selectedSimRobot;
        public SimRobotDefinition SelectedSimRobot
        {
            get => _selectedSimRobot;
            set
            {
                if (_selectedSimRobot != value)
                {
                    _selectedSimRobot = value;
                    OnPropertyChanged(nameof(SelectedSimRobot));
                    _ = RefreshSimRobotValues();
                }
            }
        }

        private int _velocityPercentageRobot;

        public int VelocityPercentageRobot
        {
            get => _velocityPercentageRobot;
            set
            {
                _velocityPercentageRobot = value;
            }
        }


        private bool _isRobotMoving;
        public bool IsRobotMoving
        {
            get => _isRobotMoving;
            set
            {
                _isRobotMoving = value;
                OnPropertyChanged();
            }
        }

        private bool _driveSinglePositions;
        public bool DriveSinglePosition
        {
            get { return _driveSinglePositions; }
            set
            {
                _driveSinglePositions = value;
                OnPropertyChanged();
            }
        }


        // Button Update Sim Robots
        public ICommand UpdateSimRobotData => GetCommandBindingAsync(Reload_FeeDataAsync);

        // Button Move to Position
        public ICommand MoveRobotToPosition => GetCommandBindingAsync(MoveRobot_ToPosition);

        // Button Stop Movement 
        public ICommand StopRobotMovement => GetCommandBinding(Stop_RobotMovement);



        //===========================================================================================================================
        // M E T H O D S   ( B U T T O N S )
        //===========================================================================================================================

        private async Task<bool> Open_RobotCsv(object parameter)
        {

            var fileDialog = new OpenFileDialog
            {
                Title = "Load Robot Data",
                Filter = "CSV/TSV/Textdateien|*.csv;*.tsv;*.txt|Alle Dateien|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (fileDialog.ShowDialog() != true)
            {
                return false;
            }

            FileNameRobotCsv = fileDialog.FileName;

            try
            {
                var result = await RobotControlService.ParseRobotCsvAsync(FileNameRobotCsv);

                RobotsData.Clear();
                foreach (var robot in result)
                    RobotsData.Add(robot);

                // initialize sim robots when csv parse was successful
                await Reload_FeeDataAsync();


                return true;
            }
            catch (FormatException)
            {
                MessageBox.Show("The CSV file format is invalid or missing required columns.", "CSV Import", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading CSV:\n{ex.Message}", "CSV Import", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }


        }


        private async Task MoveRobot_ToPosition(object parameter)
        {
            if (SelectedRobot is null)
            {
                SetStatus("No physical robot selected.", Severity.Error);
                return;
            }

            if (SelectedPath is null)
            {
                SetStatus("No path selected.", Severity.Error);
                return;
            }

            if (SelectedPosition is null)
            {
                SetStatus("No target position selected.", Severity.Warning);
                return;
            }

            if (SelectedSimRobot is null)
            {
                SetStatus("No simulation robot selected.", Severity.Error);
                return;
            }

            if (VelocityPercentageRobot <= 0)
            {
                SetStatus("Velocity is 0%. Set a value > 0.", Severity.Warning);
                return;
            }

            await _robotMotionController.MoveRobotToPositionAsync(SelectedRobot, SelectedPath, SelectedPosition, SelectedSimRobot, VelocityPercentageRobot, DriveSinglePosition);

        }


        private void Stop_RobotMovement(object parameter)
        {
            _robotMotionController.Cancel();
        }



        //===========================================================================================================================
        // M E T H O D S   ( H E L P E R S )
        //===========================================================================================================================

        private void UpdateSimRobotsList()
        {
            //await Services.FeeObjects.UpdateFeeDataAsync();

            var robots = RobotControlService.GetSimRobots();

            SimRobots.Clear();
            foreach (var robot in robots)
                SimRobots.Add(robot);
        }


        public async Task RefreshSimRobotValues()
        {
            if (SelectedSimRobot == null)
                return;

            var values = await ModelControlMotionService.ReadCurrentJointValuesAsync(SelectedSimRobot.Joints);
            OnJointValuesUpdated(values);
        }



        //===========================================================================================================================
        // E V E N T S
        //===========================================================================================================================

        private void OnRobotMovingStateChanged(bool isMoving)
        {
            IsRobotMoving = isMoving;
        }


        #endregion





        #region Axis Control
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///                                                                                                                      ///   
        ///     A X I S   C O N T R O L   -   B I N D I N G S   &   F U N C T I O N S                                            ///
        ///                                                                                                                      ///
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



        //===========================================================================================================================
        // F I L E   I M P O R T
        //===========================================================================================================================

        // Textbox Filename Robot CSV
        private string _fileNameAxisCompositionCsv;
        public string FileNameAxisCompositionCsv
        {
            get { return _fileNameAxisCompositionCsv; }
            set
            {
                _fileNameAxisCompositionCsv = value;
                OnPropertyChanged();
            }
        }

        private bool _enableOpenAxisCompositionCsv;
        public bool EnableOpenAxisCompositionCsv
        {
            get { return _enableOpenAxisCompositionCsv; }
            set
            {
                _enableOpenAxisCompositionCsv = value;
                OnPropertyChanged();
            }
        }


        // Button Open Robot CSV
        public ICommand OpenAxisCompositionCsv => GetCommandBindingAsync(Open_AxisCompositionCsv);




        //===========================================================================================================================
        // C O M P O S I T I O N   D A T A
        //===========================================================================================================================

        public ObservableCollection<AxisCompositionData> CompositionData { get; private set; }

        public ObservableCollection<AxisCompositionPositionsData> CompositionPositions { get; private set; }

        public ObservableCollection<AxisSelectionViewModel> AxisSelections { get; private set; }
        public ObservableCollection<FeeJoint> SelectableAxisCompositionJoints { get; private set; }



        private AxisCompositionData _selectedComposition;
        public AxisCompositionData SelectedComposition
        {
            get { return _selectedComposition; }
            set
            {
                _selectedComposition = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedComposition));

                UpdateCompositionPositions();
                UpdateAxisSelections();
            }
        }

        private AxisCompositionPositionsData _selectedCompositionPosition;
        public AxisCompositionPositionsData SelectedCompositionPosition
        {
            get { return _selectedCompositionPosition; }
            set
            {
                _selectedCompositionPosition = value;
                OnPropertyChanged();
            }
        }


        public bool HasSelectedComposition => SelectedComposition != null;



        private bool _isAxisCompositionMoving;
        public bool IsAxisCompositionMoving
        {
            get => _isAxisCompositionMoving;
            set
            {
                _isAxisCompositionMoving = value;
                OnPropertyChanged();
            }
        }

        private int _velocityPercentageAxisComposition;

        public int VelocityPercentageAxisComposition
        {
            get => _velocityPercentageAxisComposition;
            set
            {
                _velocityPercentageAxisComposition = value;
            }
        }



        public ICommand UpdateSelectableJoints => GetCommandBindingAsync(Reload_FeeDataAsync);
        public ICommand MoveAxisComposition => GetCommandBindingAsync(MoveAxisCompositiont_ToPosition);

        public ICommand StopAxisCompositionMovement => GetCommandBinding(Stop_AxisCompositionMovement);









        //===========================================================================================================================
        // M E T H O D S   ( B U T T O N S )
        //===========================================================================================================================

        private async Task<bool> Open_AxisCompositionCsv(object parameter)
        {
            var fileDialog = new OpenFileDialog
            {
                Title = "Load Axis Composition File",
                Filter = "CSV|*.csv|Alle Dateien|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (fileDialog.ShowDialog() != true)
            {
                return false;
            }

            FileNameAxisCompositionCsv = fileDialog.FileName;

            var data = AxisControlService.ParseAxisCompositionData(FileNameAxisCompositionCsv);
            var sorted = data.OrderBy(x => x.CompositionName);

            CompositionData.Clear();
            foreach (var item in sorted)
                CompositionData.Add(item);

            return true;


        }


        private async Task MoveAxisCompositiont_ToPosition(object parameter)
        {

            if (SelectedCompositionPosition is null)
            {
                SetStatus("No target position selected.", Severity.Warning);
                return;
            }

            if (VelocityPercentageAxisComposition <= 0)
            {
                SetStatus("Velocity is 0%. Set a value > 0.", Severity.Warning);
                return;
            }

            var joints = AxisSelections.Select(x => x.SelectedJoint).ToList();

            await _axisCompositionMotionController.MoveAxisCompositionToPositionAsync(SelectedCompositionPosition, joints, VelocityPercentageAxisComposition);

        }


        private void Stop_AxisCompositionMovement(object parameter)
        {
            _axisCompositionMotionController.Cancel();
        }






        //===========================================================================================================================
        // M E T H O D S   ( H E L P E R S )
        //===========================================================================================================================

        private void UpdateCompositionPositions()
        {
            CompositionPositions.Clear();

            if (SelectedComposition == null)
                return;

            foreach (var pos in SelectedComposition.PositionsData)
                CompositionPositions.Add(pos);

        }    



        private void UpdateAxisSelections()
        {
            AxisSelections.Clear();

            if (SelectedComposition == null)
                return;

            var firstPos = SelectedComposition.PositionsData.FirstOrDefault();
            if (firstPos == null)
                return;

            int axisCount = firstPos.AxisValues.Length;

            for (int i = 1; i <= axisCount; i++)
            {
                var axis = new AxisSelectionViewModel(i, SelectableAxisCompositionJoints, IsJointAvailable);

                axis.SelectionChanged += OnAxisSelectionChanged;

                AxisSelections.Add(axis);
            }
        }


        private void UpdateSelectableAxisList()
        {
            SelectableAxisCompositionJoints.Clear();

            foreach (var obj in Services.FeeObjects.AllFeeObjects)
            {
                if (obj is FeeJoint joint)
                    SelectableAxisCompositionJoints.Add(joint);
            }
        }


        private bool IsJointAvailable(AxisSelectionViewModel axisSel, FeeJoint joint)
        {
            var usedJoints = AxisSelections.Where(a => a != axisSel)
                                           .Select(a => a.SelectedJoint)
                                           .Where(j => j != null);

            return !usedJoints.Contains(joint);
        }



        private void OnAxisSelectionChanged(object sender, EventArgs e)
        {
            foreach (var axis in AxisSelections)
                axis.OnPropertyChanged(nameof(axis.Options));
        }



        //===========================================================================================================================
        // E V E N T S
        //===========================================================================================================================

        private void OnAxisCompositionMovingStateChanged(bool isMoving)
        {
            IsAxisCompositionMoving = isMoving;
        }



        #endregion





        #region Object Control
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///                                                                                                                      ///   
        ///     O B J E C T   C O N T R O L   -   B I N D I N G S   &   F U N C T I O N S                                        ///
        ///                                                                                                                      ///
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        //===========================================================================================================================
        // F I L T E R   /   S E L E C T I O N
        //===========================================================================================================================

        private string _objectControlFilterText;
        public string ObjectControlFilterText
        {
            get { return _objectControlFilterText; }
            set
            {
                _objectControlFilterText = value;
                OnPropertyChanged();

                // Start Debounce
                _debounceTimerObjectControlFilter.Stop();
                _debounceTimerObjectControlFilter.Start();
            }
        }

        private bool _showOnlySelected;
        public bool ShowOnlySelected
        {
            get { return _showOnlySelected; }
            set
            {
                _showOnlySelected = value;
                OnPropertyChanged();
                ExpanderView.Refresh();
            }
        }


        // DispatcherTimer for filtering SimObjects with Debounce
        private readonly DispatcherTimer _debounceTimerObjectControlFilter;




        //===========================================================================================================================
        // E X P A N D E R   V I E W 
        //===========================================================================================================================

        public ICollectionView ExpanderView { get; }

        public ObservableCollection<FeeAbstractObject> ControllableObjects { get; }

        public ICommand UpdateControllableObjects => GetCommandBindingAsync(Reload_FeeDataAsync);




        //===========================================================================================================================
        // T R I G G E R   E X P A N D E R   E L E M E N T   F U N C T I O N S
        //===========================================================================================================================

        public ICommand OnButtonPressRelease => GetCommandBindingAsync(async obj =>
        {
            if (obj is FeeButton feeObject)
            {
                await feeObject.SetPressReleaseAsync();
            }
        });

        public ICommand OnConveyorSetVelocity => GetCommandBindingAsync(async obj =>
        {
            if (obj is FeeSurface feeObject)
            {
                await feeObject.SetVelocity();
            }
        });

        public ICommand OnJointMoveToPosition => GetCommandBindingAsync(async obj =>
        {
            if (obj is FeeJoint feeObject)
            {
                await feeObject.SetManualVelocityPositionAsync();
            }
        });

        public ICommand OnPickAndPlaceChange => GetCommandBindingAsync(async obj =>
        {
            if (obj is FeePickAndPlace feeObject)
            {
                await feeObject.SetPickDropAsync();
            }
        });

        public ICommand OnReparent => GetCommandBindingAsync(async obj =>
        {
            if (obj is FeeReparenter feeObject)
            {
                await feeObject.SetReparentAsync();
            }
        });

        //public ICommand OnSensorDetect => GetCommandBindingAsync(async obj =>
        //{
        //    if (obj is FeeSensor feeObject)
        //    {
        //        await feeObject.SetDetection();
        //    }
        //});

        public ICommand OnStopperOpenClose => GetCommandBindingAsync(async obj =>
        {
            if (obj is FeeFloor feeObject)
            {
                await feeObject.SetCollision();
            }
        });










        //===========================================================================================================================
        // M E T H O D S   ( H E L P E R S )
        //===========================================================================================================================

        private bool FilterControllableObjects(object obj)
        {
            var item = (FeeAbstractObject)obj;

            if (ShowOnlySelected && !item.IsSelected)
                return false;

            if (!string.IsNullOrWhiteSpace(ObjectControlFilterText) && !item.Name.Contains(ObjectControlFilterText, StringComparison.OrdinalIgnoreCase))
                return false;

            return true;

        }


        private void UpdateControllableObjectsList()
        {
            ControllableObjects.Clear();

            foreach (var obj in Services.FeeObjects.AllFeeObjects)
            {
                //if (obj.FeeType == nameof(Button))
                //    ControllableObjects.Add(obj);

                if (obj.FeeType == nameof(Floor) && obj is FeeFloor floor && floor.UseCollisionSlot)
                    ControllableObjects.Add(obj);

                if (obj.FeeType == nameof(MotionJoint))
                    ControllableObjects.Add(obj);

                if (obj.FeeType == nameof(PickAndPlace))
                    ControllableObjects.Add(obj);

                if (obj.FeeType == nameof(Reparenter))
                    ControllableObjects.Add(obj);

                if (obj.FeeType == nameof(Surface))
                    ControllableObjects.Add(obj);

                if (obj.FeeType == nameof(SafetySensor))
                    ControllableObjects.Add(obj);

            }

            ExpanderView.Refresh();
        }



        #endregion







        #region Status Information
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///                                                                                                                      ///   
        ///     S T A T U S   I N F R O M A T I O N   -   B I N D I N G S   &   F U N C T I O N S                                ///
        ///                                                                                                                      ///
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        //===========================================================================================================================
        // A C T U A L   P O S I T I O N
        //===========================================================================================================================

        private float _actualPositionJ1;
        public float ActualPositionJ1
        {
            get { return _actualPositionJ1; }
            set
            {
                _actualPositionJ1 = value;
                OnPropertyChanged();
            }
        }

        private float _actualPositionJ2;
        public float ActualPositionJ2
        {
            get { return _actualPositionJ2; }
            set
            {
                _actualPositionJ2 = value;
                OnPropertyChanged();
            }
        }

        private float _actualPositionJ3;
        public float ActualPositionJ3
        {
            get { return _actualPositionJ3; }
            set
            {
                _actualPositionJ3 = value;
                OnPropertyChanged();
            }
        }

        private float _actualPositionJ4;
        public float ActualPositionJ4
        {
            get { return _actualPositionJ4; }
            set
            {
                _actualPositionJ4 = value;
                OnPropertyChanged();
            }
        }

        private float _actualPositionJ5;
        public float ActualPositionJ5
        {
            get { return _actualPositionJ5; }
            set
            {
                _actualPositionJ5 = value;
                OnPropertyChanged();
            }
        }

        private float _actualPositionJ6;
        public float ActualPositionJ6
        {
            get { return _actualPositionJ6; }
            set
            {
                _actualPositionJ6 = value;
                OnPropertyChanged();
            }
        }

        private float _actualPositionJ7;
        public float ActualPositionJ7
        {
            get { return _actualPositionJ7; }
            set
            {
                _actualPositionJ7 = value;
                OnPropertyChanged();
            }
        }



        //===========================================================================================================================
        // M O V E M E N T   /   S T A T U S
        //===========================================================================================================================

        


        private string _statusText;
        public string StatusText
        {
            get { return _statusText; }
            set
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }

        private Severity _statusSeverity = Severity.Info;
        public Severity StatusSeverity
        {
            get => _statusSeverity;
            set
            {
                if (_statusSeverity != value)
                {
                    _statusSeverity = value;
                    OnPropertyChanged(nameof(StatusSeverity));
                }
            }
        }



        //===========================================================================================================================
        // M E T H O D S   ( H E L P E R S )
        //===========================================================================================================================

        private void SetStatus(string message, Severity severity = Severity.Info)
        {
            StatusText = $"[{DateTime.Now:HH:mm:ss}]\n{message}";
            StatusSeverity = severity;
        }


        #endregion


















































        //===========================================================================================================================
        // PREPARE FOR AXIS CONTROL
        //===========================================================================================================================

        public List<string> AllJoints { get; } = new List<string> { "J1", "J2", "J3", "J4", "J5", "J6" };

        public string SelectedJoint1 { get; set; }
        public string SelectedJoint2 { get; set; }
        public string SelectedJoint3 { get; set; }
        public string SelectedJoint4 { get; set; }
        public string SelectedJoint5 { get; set; }
        public string SelectedJoint6 { get; set; }


        public IEnumerable<string> JointOptions1 => GetFilteredOptions(SelectedAxis1);
        public IEnumerable<string> JointOptions2 => GetFilteredOptions(SelectedAxis1);
        public IEnumerable<string> JointOptions3 => GetFilteredOptions(SelectedAxis1);
        public IEnumerable<string> JointOptions4 => GetFilteredOptions(SelectedAxis1);
        public IEnumerable<string> JointOptions5 => GetFilteredOptions(SelectedAxis1);
        public IEnumerable<string> JointOptions6 => GetFilteredOptions(SelectedAxis1);



        private IEnumerable<string> GetFilteredOptions(string currentSelection)
        {
            var selected = new[]
            {
                SelectedJoint1, SelectedJoint2, SelectedJoint3,
                SelectedJoint4, SelectedJoint5, SelectedJoint6
            }.Where(x => x != null && x != currentSelection);

            return AllJoints.Except(selected);
        }

        private string _selectedAxis1;
        public string SelectedAxis1
        {
            get => _selectedAxis1;
            set
            {
                _selectedAxis1 = value;
                OnPropertyChanged();
                RefreshJointOptions();
            }
        }


        private void RefreshJointOptions()
        {
            OnPropertyChanged(nameof(JointOptions1));
            OnPropertyChanged(nameof(JointOptions2));
            OnPropertyChanged(nameof(JointOptions3));
            OnPropertyChanged(nameof(JointOptions4));
            OnPropertyChanged(nameof(JointOptions5));
            OnPropertyChanged(nameof(JointOptions6));
        }

    }
}
