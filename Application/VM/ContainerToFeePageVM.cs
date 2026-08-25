using Microsoft.Win32;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using VIBN_Tools.ContainerToFee;
using VIBN_Tools.ContainerToFee.General;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;
using VIBN_Tools.Settings;
using static VIBN_Tools.ContainerToFee.ContainerToFeeService;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.Application.VM
{
    /// <summary>
    /// Guides the controlled transfer from container XML to existing FEE
    /// objects. It owns selection/progress state while factories and services
    /// perform the actual model mapping.
    /// </summary>
    public class ContainerToFeePageVM : MvvmBase
    {


        //===========================================================================================================================
        // B I N D I N G S   -   R E A D   I N   D A T A
        //===========================================================================================================================

        // Textbox Filename Container XML
        private string _fileNameContainerXML;
        public string FileNameContainerXml
        {
            get { return _fileNameContainerXML; }
            set
            {
                _fileNameContainerXML = value;
                OnPropertyChanged();
            }
        }

        // Button Open Container XML
        public ICommand OpenContainerXml => GetCommandBindingAsync(Open_ContainerXml);

        private bool _enableOpenContainerXml;
        public bool EnableOpenContainerXml
        {
            get { return _enableOpenContainerXml; }
            set
            {
                _enableOpenContainerXml = value;
                OnPropertyChanged();
            }
        }



        // Button Search SimObjects
        public ICommand SearchSimObjects => GetCommandBindingAsync(Search_SimObjects);

        public bool CanExecuteSearchSimObjects => Connection.IsConnected && ListAllContainers.Count > 0;


        private bool _isProcessingSearchSimObjects;
        public bool IsProcessingSearchSimObjects
        {
            get => _isProcessingSearchSimObjects;
            set
            {
                _isProcessingSearchSimObjects = value;
                OnPropertyChanged(nameof(CanExecuteGeneration));
            }
        }


        //===========================================================================================================================
        // B I N D I N G S   -   F I N D   &   A S S I G N   S I M O B J E C T S
        //===========================================================================================================================

        // Enable GroupBox
        private bool _enableFindAssignSimObjects;

        public bool EnableFindAssignSimObjects
        {
            get { return _enableFindAssignSimObjects; }
            set
            {
                _enableFindAssignSimObjects = value;
                OnPropertyChanged();
            }
        }

        // Infotext
        private string _selectionInfoText;
        public string SelectionInfoText
        {
            get { return _selectionInfoText; }
            set
            {
                _selectionInfoText = value;
                OnPropertyChanged();
            }
        }



        // Textbox Filter SimObjects
        private string _filterText;
        public string FilterText
        {
            get { return _filterText; }
            set
            {
                _filterText = value;
                OnPropertyChanged();

                // Start Debounce
                _debounceTimer.Stop();
                _debounceTimer.Start();
            }
        }

        // Checkbox Show Assigned SimObjects
        private bool _checkboxShowAssigned;
        public bool CheckboxShowAssigned
        {
            get { return _checkboxShowAssigned; }
            set
            {
                _checkboxShowAssigned = value;
                OnPropertyChanged();
                SelectableSimObjectsView.Refresh();
            }
        }

        // Button Refresh SimObjects
        public ICommand RefreshSimObjectList => GetCommandBindingAsync(Refresh_SimObjectList);


        // Listbox SimObject Selection
        public ICollectionView SelectableSimObjectsView { get; }

        public ObservableCollection<FeeAbstractObject> SelectedSimObjects { get; set; } = new ObservableCollection<FeeAbstractObject>();


        // Selection Mode in Listbox
        public bool IsMultiSelectionEnabled => CurrentTarget?.AllowMultiSelect ?? true;
        public SelectionMode SelectionModeValue => IsMultiSelectionEnabled ? SelectionMode.Extended : SelectionMode.Single;




        // Button Create SimObject
        public ICommand CreateSimObject => GetCommandBindingAsync(Create_SimObject);

        // Button Skip SimObject
        public ICommand SkipSimObjectSelection => GetCommandBindingAsync(Skip_SimObjectSelection);

        // Button Select SimObjects
        public ICommand SelectSimObjects => GetCommandBindingAsync(Select_SimObjects);

        // Button Back to last SimObjects
        public ICommand GoBackToLastSelection => GetCommandBindingAsync(Back_SimObjectSelection);

        // Enable Button Go back
        private bool _enableGoBackToLast;
        public bool EnableGoBackToLast
        {
            get { return _enableGoBackToLast; }
            set
            {
                _enableGoBackToLast = value;
                OnPropertyChanged();
            }
        }


        // Button Cancel SkimObject Selection
        public ICommand CancelSimObjectSelection => GetCommandBindingAsync(Cancel_SimObjectSelection);



        //===========================================================================================================================
        // B I N D I N G S   -   S T A T U S   I N F O   &   S T A R T   G E N E R A T I O N
        //===========================================================================================================================

        // Collection of Status Information
        private ObservableCollection<StatEntry> _statusInfoCollection;
        public ObservableCollection<StatEntry> StatusInfoCollection
        {
            get { return _statusInfoCollection; }
            set
            {
                _statusInfoCollection = value;
                OnPropertyChanged();
            }
        }

        // Button Start Generation
        public ICommand StartGeneration => GetCommandBindingAsync(Start_Generation);



        // Button Start Generation IsBusy
        private bool _isBusyStartGeneration;
        public bool IsBusyStartGeneration
        {
            get { return _isBusyStartGeneration; }
            set
            {
                _isBusyStartGeneration = value;
                OnPropertyChanged();
            }
        }

        public bool CanExecuteGeneration => Connection.IsConnected && !IsProcessingSearchSimObjects && (ListAllContainers.Count > 0 || ListUnknownSignals.Count > 0);







        //===========================================================================================================================
        // P R O P E R T I E S   O F   V I E W - M O D E L
        //===========================================================================================================================

        // FEE Conection State
        public FeeConnectionService Connection => Services.Connection;

        // Collection of mappable SimObjects
        public ObservableCollection<FeeAbstractObject> MappableSimObjects { get; set; }


        // Lists of Containers and Signals
        public ObservableCollection<ContainerBaseClass> ListAllContainers { get; set; }

        // Current Container Reference of Iteration
        private ISimObjectFindOrSelect _currentContainer;
        public ISimObjectFindOrSelect CurrentContainer
        {
            get { return _currentContainer; }
            set
            {
                _currentContainer = value;
                OnPropertyChanged();
                SelectableSimObjectsView.Refresh();
                UpdateSelectionInfoText();
            }
        }

        // Curretn Target Reference of Iteration
        private SimObjectTarget _currentTarget;
        public SimObjectTarget CurrentTarget
        {
            get { return _currentTarget; }
            set
            {
                _currentTarget = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsMultiSelectionEnabled));
                OnPropertyChanged(nameof(SelectionModeValue));

                SelectableSimObjectsView.Refresh();
                UpdateSelectionInfoText();
            }
        }

        public ObservableCollection<FeeInterfaceSignal> ListUnknownSignals { get; set; }


        // DispatcherTimer for filtering SimObjects with Debounce
        private readonly DispatcherTimer _debounceTimer;


        //private int _currentContainerIndex = -1;
        private int _currentSelectionStepIndex;
        private List<SimObjectSelectionStep> _selectionSteps;






        private int _skipCounter;
        public bool _isSimObjectSearchCancelled;
        private int _generatedSignalsCount;


        // Undo Stack for go-back operations
        private Stack<ContainerSnapshot> _snapshotHistory = new Stack<ContainerSnapshot>();





        //===========================================================================================================================
        // C O N S T R U C T O R
        //===========================================================================================================================

        public ContainerToFeePageVM()
        {
            ListAllContainers = new ObservableCollection<ContainerBaseClass>();
            ListUnknownSignals = new ObservableCollection<FeeInterfaceSignal>();

            MappableSimObjects = new ObservableCollection<FeeAbstractObject>();

            // Set CollectionViewer and add sorting
            SelectableSimObjectsView = CollectionViewSource.GetDefaultView(MappableSimObjects);
            SelectableSimObjectsView.Filter = FilterSelectableSimObjects;
            SelectableSimObjectsView.SortDescriptions.Clear();
            SelectableSimObjectsView.SortDescriptions.Add(new SortDescription(nameof(FeeAbstractObject.Name), ListSortDirection.Ascending));

            // Set natural string compare for sorting
            var customView = SelectableSimObjectsView as ListCollectionView;
            if (customView != null) { customView.CustomSort = new NaturalStringComparer(); }

            // Update Filter View when MappableSimObjects is changed
            MappableSimObjects.CollectionChanged += (sender, eventArgs) => { SelectableSimObjectsView.Refresh(); };


            // Configure and initialise Debounce-Timer
            _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _debounceTimer.Tick += (sender, eventArgs) =>
            {
                _debounceTimer.Stop();
                SelectableSimObjectsView.Refresh();
            };

            // Initial state of View
            EnableOpenContainerXml = true;
            CheckboxShowAssigned = false;
            //EnableProgressActive = Visibility.Hidden;
            IsBusyStartGeneration = false;

            // Initialise Status Information
            StatusInfoCollection = new ObservableCollection<StatEntry>
            {
                new StatEntry{Label = "Found Containers:"},
                new StatEntry{Label = "Found Signals:"},
                new StatEntry{Label = "Assigned SimObjects"},
                new StatEntry{Label = "Skipped Objects"},
                new StatEntry{Label = "Generated Signals"},
            };

            // Listen for changes to activate Button Generate
            Connection.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(FeeConnectionService.IsConnected))
                {
                    OnPropertyChanged(nameof(CanExecuteGeneration));
                    OnPropertyChanged(nameof(CanExecuteSearchSimObjects));
                }
            };
            ListAllContainers.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(CanExecuteGeneration));
                OnPropertyChanged(nameof(CanExecuteSearchSimObjects));
            };

            ListUnknownSignals.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(CanExecuteGeneration));
            };


            // Init for selection steps
            _selectionSteps = new List<SimObjectSelectionStep>();
            _currentSelectionStepIndex = -1;

        }




        //===========================================================================================================================
        // M E T H O D S   ( B U T T O N S )
        //===========================================================================================================================

        /// <summary>
        /// Function opens the container.xml file and reads in the container data
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns>true if opening file was successful</returns>
        private async Task<bool> Open_ContainerXml(object parameter)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
            openFile.Title = "Select Container XML";

            if (openFile.ShowDialog() == true)
            {
                FileNameContainerXml = openFile.FileName;

                // Clear lists of Container Data and unknown Signals and store new data
                ListAllContainers.Clear();
                ListUnknownSignals.Clear();
                _generatedSignalsCount = 0;

                (var containers, var unknownSignals) = ReadInContainerXmlData(FileNameContainerXml);

                LinkAddonContainers(containers);

                foreach (var c in containers)
                {
                    ListAllContainers.Add(c);

                    //// Add Fault CabinetElement for sensor containers
                    //if (c is Sensor_Container sensor)
                    //{
                    //    var faultSimContainer = sensor.CreateFaultSimCabinetElementContainer();
                    //    ListAllContainers.Add(faultSimContainer);
                    //}
                }

                foreach (var s in unknownSignals)
                    ListUnknownSignals.Add(s);


                // Clear Assignment Information of MappableSimObjects
                foreach (var simObject in MappableSimObjects.OfType<IAssignableSimObject>())
                {
                    simObject.AssignedContainer = null;
                }

                // Update Status Information Collection
                UpdateStatusValues();


                return true;
            }
            return false;
        }


        /// <summary>
        /// Function maps SimObjects to corresponding containers or updates the listbox to wait for user selection of SimObjects
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        private async Task<bool> Search_SimObjects(object parameter)
        {

            IsProcessingSearchSimObjects = true;

            // Reset Skip Counter and Skip All flag
            _skipCounter = 0;
            _isSimObjectSearchCancelled = false;

            await UpdateSimObjectsFromSimulationAsync();
            SelectableSimObjectsView.Refresh();

            // Create Compare Instances
            var typeComparer = new NaturalStringComparer<ContainerBaseClass>(x => x.GetType().Name);
            var componentComparer = new NaturalStringComparer<ContainerBaseClass>(x => x.ComponentName);

            // Sort list of all containers
            var sorted = ListAllContainers.OrderBy(x => x, typeComparer)
                                                 .ThenBy(x => x, componentComparer)
                                                 .ToList();
            ListAllContainers.Clear();

            foreach (var item in sorted)
                ListAllContainers.Add(item);


            // Find SimObject by name if existing
            foreach (var container in ListAllContainers)
            {
                if (container is ISimObjectFindOrSelect soFindSelect)
                {
                    soFindSelect.FindSimObjects(MappableSimObjects);
                }
            }

            _selectionSteps = ListAllContainers.OfType<ISimObjectFindOrSelect>().SelectMany(container =>
                container.GetSimObjectTargets().Select(target => new SimObjectSelectionStep
                {
                    Container = container,
                    Target = target,
                }))
                .ToList();


            // Select SimObjects from ListBox entries
            _currentSelectionStepIndex = -1;
            SelectNextSimObject();

            return true;
        }


        /// <summary>
        /// Function iterates through all container elements and generates it in the simulation
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        private async Task<bool> Start_Generation(object parameter)
        {
            // Enable/Disable View Elements
            EnableOpenContainerXml = false;
            IsBusyStartGeneration = true;

            // Reset Skip Counter and Skip All flag
            _skipCounter = 0;
            _isSimObjectSearchCancelled = false;


            // Sort list of all containers
            var sorted = ListAllContainers.OrderBy(x => x.GetType().Name)
                                                 .ThenBy(x => x.ComponentName)
                                                 .ToList();
            ListAllContainers.Clear();

            foreach (var item in sorted)
                ListAllContainers.Add(item);


            // Define Interfaces
            FeeInterface InterfaceContainerToFee = new FeeInterface()
            {
                Name = $"Auto Generated (at {DateTime.Now.ToString("dd.MM.yyyy HH:mm")})",
            };
            FeeInterface InterfaceUnknownSignals = new FeeInterface()
            {
                Name = $"Unknown Signals (generated at {DateTime.Now.ToString("dd.MM.yyyy HH:mm")})",
            };


            if (ListAllContainers.Count > 0)
            {
                // Create BasicFrame for Logics etc.
                FeeBasicFrame BasicFrameContainerToFee = new FeeBasicFrame()
                {
                    Name = $"Auto Generated (at {DateTime.Now.ToString("dd.MM.yyyy HH:mm")})",
                };
                await BasicFrameContainerToFee.CreateAsync();
                await BasicFrameContainerToFee.SendAndWaitAsync();

                // Create Interface for container signals
                await InterfaceContainerToFee.CreateInterfaceAsync();

                await CreateAllContainersAsync(ListAllContainers, InterfaceContainerToFee, BasicFrameContainerToFee);
            }

            if (ListUnknownSignals.Count > 0)
            {
                // Create Interface for unknown signals
                await InterfaceUnknownSignals.CreateInterfaceAsync();

                // Create unknown signals
                await Parallel.ForEachAsync(ListUnknownSignals, async (signal, ct) =>
                {
                    await signal.CreateSignalAsync(InterfaceUnknownSignals);
                });

                //foreach (var signal in ListUnknownSignals)
                //{
                //    await signal.CreateSignalAsync(InterfaceUnknownSignals);
                //}
            }


            _generatedSignalsCount = (await FeeInterface.GetAllSignalsFromInterfaceAsync(InterfaceContainerToFee.Guid)).Count + (await FeeInterface.GetAllSignalsFromInterfaceAsync(InterfaceUnknownSignals.Guid)).Count;

            // Delete SimObjects - CHECK LATER IF STILL NEEDED AFTER GENERATION
            MappableSimObjects.Clear();

            // Write Status Information and update view
            UpdateStatusValues();

            EnableOpenContainerXml = true;
            //EnableProgressActive = Visibility.Hidden;
            IsBusyStartGeneration = false;


            return true;
        }


        /// <summary>
        /// Update MappableSimObjects Collection with actual objects from the simulation
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        private async Task Refresh_SimObjectList(object parameter)
        {
            SaveContainerSnapshot();

            if (EnableFindAssignSimObjects)
            {
                await UpdateSimObjectsFromSimulationAsync();
                SelectableSimObjectsView.Refresh();
            }

        }


        /// <summary>
        /// Function sets flag to create the corresponding SimObject in the current container
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        private async Task Create_SimObject(object parameter)
        {
            SaveContainerSnapshot();

            if (CurrentContainer is ICreatableContainer container)
            {
                container.IsCreationRequested = true;
            }
            SelectNextSimObject();
        }


        /// <summary>
        /// Function skips the current SmObject selection
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        private async Task Skip_SimObjectSelection(object parameter)
        {
            SaveContainerSnapshot();

            _skipCounter++;
            SelectNextSimObject();
        }


        /// <summary>
        /// Function gets the selected SimObjects from the Listbox and assignes it to the current container.
        /// It also removes the selected SimObject from a previously container if it was wrongly assigned to it.
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        private async Task Select_SimObjects(object parameter)
        {
            SaveContainerSnapshot();

            if (parameter is IList selectedItems && CurrentContainer != null && CurrentTarget != null)
            {
                var selectedSimObjects = selectedItems.OfType<FeeAbstractObject>();
                var objectsToAssign = new List<FeeAbstractObject>();

                foreach (var simObject in selectedSimObjects)
                {
                    if (simObject is IAssignableSimObject assignableObject)
                    {
                        foreach (var target in ListAllContainers.OfType<ISimObjectFindOrSelect>().SelectMany(x => x.GetSimObjectTargets()).Where(x => x != CurrentTarget))
                        {
                            var filtered = target.GetObjects().Where(x => x != simObject).ToList();

                            target.AssignObjects(filtered);
                        }

                        // Assignment to new container
                        objectsToAssign.Add(simObject);
                        assignableObject.AssignedContainer = CurrentContainer;
                    }

                }

                CurrentTarget.AssignObjects(objectsToAssign);


                // Enable View elements
                EnableFindAssignSimObjects = false;

                SelectNextSimObject();
            }

        }

        /// <summary>
        /// Goes back to the last object
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        private async Task Back_SimObjectSelection(object parameter)
        {
            Undo();
        }


        /// <summary>
        /// Cancels the selection process of SimObjects
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        private async Task Cancel_SimObjectSelection(object parameter)
        {
            _isSimObjectSearchCancelled = true;
            SelectNextSimObject();
        }



        //===========================================================================================================================
        // M E T H O D S   ( H E L P E R S )
        //===========================================================================================================================




        /// <summary>
        /// Helper Function for searching SimObjects. Prepares the collection for selecting a SimObject by the user
        /// </summary>        
        private void SelectNextSimObject()
        {
            do
            {
                // Update Status Information Collection
                UpdateStatusValues();

                _currentSelectionStepIndex++;
                if (_currentSelectionStepIndex >= _selectionSteps.Count || _isSimObjectSearchCancelled)
                {
                    CurrentContainer = null;
                    CurrentTarget = null;

                    // Enable Buttons on end of SimObject search
                    EnableOpenContainerXml = true;
                    //EnableSearchSimObjects = Connection.IsConnected && ListAllContainers.Count > 0;
                    EnableFindAssignSimObjects = false;

                    IsProcessingSearchSimObjects = false;

                    return;
                }

                var step = _selectionSteps[_currentSelectionStepIndex];

                if (!HasAssignedSimObjects(step.Target))
                {
                    CurrentContainer = step.Container;
                    CurrentTarget = step.Target;

                    EnableFindAssignSimObjects = true;

                    return; //Wait for user interaction
                }

            }
            // Next iteration if Container had SimObject already assigned
            while (_currentSelectionStepIndex < _selectionSteps.Count);

        }


        // Function to check if any object of MappableSimObjects has this container already assigned to it
        //private bool HasAssignedSimObjects(ISimObjectFindOrSelect soFindSelect)
        //{
        //    return MappableSimObjects.OfType<IAssignableSimObject>().Any(obj => obj.AssignedContainer == soFindSelect);
        //}

        private bool HasAssignedSimObjects(SimObjectTarget target)
        {
            return target.GetObjects().Any();
        }



        /// <summary>
        /// Function reads in SimObjects from simulation and adds or removes new or deleted objects from MappableSimObjects collection
        /// </summary>
        /// <returns></returns>
        private async Task UpdateSimObjectsFromSimulationAsync()
        {
            var newSimObjects = await GetSimObjectsFromSimultionAsync();

            // Add or Update SimObject
            foreach (var newObj in newSimObjects)
            {
                var existingObject = MappableSimObjects.FirstOrDefault(x => x.GuidString == newObj.GuidString);
                // There is an SimObject which Guid is already in MappableSimObjects
                if (existingObject != null)
                {
                    // Update Properties if wanted
                    existingObject.Name = newObj.Name;
                }
                else
                {
                    MappableSimObjects.Add(newObj);
                }
            }

            // Remove SimObjects from MappableSimObjects that are no longer in the simulation
            var guidsInSimulation = newSimObjects.Select(x => x.GuidString).ToHashSet();
            for (int i = 0; i < MappableSimObjects.Count; i++)
            {
                if (!guidsInSimulation.Contains(MappableSimObjects[i].GuidString))
                    MappableSimObjects.RemoveAt(i);
            }

            var sorted = MappableSimObjects.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();

            MappableSimObjects.Clear();

            foreach (var item in sorted)
                MappableSimObjects.Add(item);

        }



        /// <summary>
        /// Function sets the Info text to show the user, which container SimObjects need to be selected
        /// </summary>
        private void UpdateSelectionInfoText()
        {
            if (CurrentContainer != null && CurrentTarget != null)
            {
                string componentName = (CurrentContainer as ContainerBaseClass)?.ComponentName ?? "Unknown Component";

                SelectionInfoText = $"Please Select SimulationObjects for component:   ''{componentName}''   ({CurrentTarget.DisplayNameWithSelection})";
            }
            else
            {
                SelectionInfoText = string.Empty;
            }
        }



        /// <summary>
        /// Function updates the Status Information values
        /// </summary>
        private void UpdateStatusValues()
        {
            StatusInfoCollection[0].Value = ListAllContainers.Count.ToString();
            StatusInfoCollection[1].Value = (ListAllContainers.Sum(x => x.CountNonNullSignals()) + ListUnknownSignals.Count).ToString();
            StatusInfoCollection[2].Value = MappableSimObjects.Where(x => x is IAssignableSimObject assignableObject && assignableObject.AssignedContainer != null).Count().ToString();
            StatusInfoCollection[3].Value = _skipCounter.ToString();
            StatusInfoCollection[4].Value = _generatedSignalsCount.ToString();
        }



        /// <summary>
        /// Funtion filters the Listbox view
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private bool FilterSelectableSimObjects(object obj)
        {
            if (obj is FeeAbstractObject simObject)
            {
                // 1. Filter: Select allowed types from CurrentContainer SimObject Lists
                if (CurrentContainer == null || CurrentTarget == null)
                {
                    return false;
                }

                if (!CurrentTarget.AllowedType.IsAssignableFrom(simObject.GetType()))
                {
                    return false;
                }

                // 2. Filter: Checkbox Show Assigned
                if (!CheckboxShowAssigned && simObject is IAssignableSimObject assignableObject && assignableObject.AssignedContainer != null) return false;

                // 3. Filter: Text filter
                if (!String.IsNullOrWhiteSpace(FilterText) && (simObject.Name == null || !simObject.Name.Contains(FilterText, StringComparison.OrdinalIgnoreCase))) return false;
            }

            return true;
        }




        private void SaveContainerSnapshot()
        {
            if (CurrentContainer != null && CurrentTarget != null)
            {
                _snapshotHistory.Push(new ContainerSnapshot
                {
                    SelectionStepIndex = _currentSelectionStepIndex,
                    Container = CurrentContainer,
                    Target = CurrentTarget,
                    AssignedSimObjects = CurrentTarget.GetObjects().ToList(),
                    IsCreationRequested = (CurrentContainer as ICreatableContainer)?.IsCreationRequested ?? false,
                });

                EnableGoBackToLast = true;
            }
        }


        private void Undo()
        {
            if (_snapshotHistory.Count == 0)
            {
                EnableGoBackToLast = false;
                return;
            }

            var snapshot = _snapshotHistory.Pop();

            _currentSelectionStepIndex = snapshot.SelectionStepIndex;
            CurrentContainer = snapshot.Container;
            CurrentTarget = snapshot.Target;

            if (CurrentContainer is ICreatableContainer creatable)
            {
                creatable.IsCreationRequested = snapshot.IsCreationRequested;
            }

            // Remove current assign of target
            foreach (var simObject in CurrentTarget.GetObjects())
            {
                if (simObject is IAssignableSimObject assignable)
                {
                    assignable.AssignedContainer = null;
                }
            }

            // Reset target
            CurrentTarget.AssignObjects(Enumerable.Empty<FeeAbstractObject>());
            CurrentTarget.AssignObjects(snapshot.AssignedSimObjects);

            // Restore Assigned container
            foreach (var simObject in snapshot.AssignedSimObjects)
            {
                if (simObject is IAssignableSimObject assignable)
                {
                    assignable.AssignedContainer = CurrentContainer;
                }
            }


            EnableFindAssignSimObjects = true;
            UpdateStatusValues();

            SelectableSimObjectsView.Refresh();

            EnableGoBackToLast = _snapshotHistory.Count > 0;

        }


    }




    public class StatEntry : MvvmBase
    {
        public string Label { get; set; }

        private string _value;
        public string Value
        {
            get { return _value; }
            set
            {
                _value = value;
                OnPropertyChanged();
            }
        }
    }
}
