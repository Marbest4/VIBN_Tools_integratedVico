using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;
using VIBN_Tools.ModelValidation;

namespace VIBN_Tools.Application.VM
{
    /// <summary>
    /// Presents grouped validation findings for the connected FEE model and
    /// delegates rule evaluation/change tracking to the ModelValidation layer.
    /// </summary>
    public class ModelValidationPageVM : MvvmBase
    {

        //===========================================================================================================================
        // P R O P E R T I E S   O F   V I E W - M O D E L
        //===========================================================================================================================

        public ObservableCollection<ValidationGroupViewModel> ValidationGroups { get; set; }

        public double NameColumnWidth { get; set; }
        public double IssueColumnWidth { get; set; }


        private bool _showGuidColumn;
        public bool ShowGuidColumn
        {
            get { return _showGuidColumn; }
            set 
            { 
                _showGuidColumn = value;
                OnPropertyChanged();
            }
        }


        private string _selectedTabName;
        public string SelectedTabName
        {
            get { return _selectedTabName; }
            set 
            { 
                _selectedTabName = value;
                OnPropertyChanged();
            }
        }


        private ValidationGroupViewModel _selectedTab;
        public ValidationGroupViewModel SelectedTab
        {
            get { return _selectedTab; }
            set 
            { 
                _selectedTab = value;
                SelectedTabName = value?.GroupName;
                OnPropertyChanged();
            }
        }


        private string _filterText;
        public string FilterText
        {
            get { return _filterText; }
            set 
            { 
                _filterText = value; 
                OnPropertyChanged();

                // Start debounce
                _debounceTimer.Stop();
                _debounceTimer.Start();

            }
        }




        // DispatcherTimer for filtering objects with Debounce
        private readonly DispatcherTimer _debounceTimer;







        //===========================================================================================================================
        // B I N D I N G S   -   B U T T O N S
        //===========================================================================================================================

        // Button Get SceneObject Data
        public ICommand UpdateFeeObjectData => GetCommandBindingAsync(Update_FeeObjectData);


        public ICommand ExpandAll => GetCommandBinding(() =>
        {
            foreach (var group in ValidationGroups)
                group.IsExpanded = true;
        });

        public ICommand CollapseAll => GetCommandBinding(() =>
        {
            foreach (var group in ValidationGroups)
                group.IsExpanded = false;
        });




        public ICommand AcknowledgeObject => GetCommandBinding(Acknowledge_Object);


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




        //===========================================================================================================================
        // C O N S T R U C T O R
        //===========================================================================================================================

        public ModelValidationPageVM()
        {
            ValidationGroups = new ObservableCollection<ValidationGroupViewModel>();

            // Build ValidationGroups if already data existing
            if (Services.FeeObjects.AllFeeObjects?.Any() == true)
            {
                OnFeeObjectsUpdated(this, new FeeObjectsUpdatedEventargs());
            }

            // Build ValidationGroups if ObjectsUpdated Trigger is set
            Services.FeeObjects.FeeObjectsUpdated += OnFeeObjectsUpdated;


            // Configure and initialise Debounce-Timer
            _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _debounceTimer.Tick += (sender, eventArgs) =>
            {
                _debounceTimer.Stop();

                foreach(var group in ValidationGroups)
                    group.ApplyFilter(FilterText);
            };
        }



        //===========================================================================================================================
        // M E T H O D S
        //===========================================================================================================================

        public async Task Update_FeeObjectData(object arg)
        {
            IsBusyUpdatingFeeData = true;

            await Services.FeeObjects.UpdateFeeDataAsync();

        }








        private void OnFeeObjectsUpdated(object sender, FeeObjectsUpdatedEventargs e)
        {
            var allFeeObjects = Services.FeeObjects.AllFeeObjects;

            if (allFeeObjects == null || allFeeObjects.Count == 0)
                return;

            // Store old tab 
            var oldTabName = SelectedTabName;

            ValidationGroups.Clear();

            // Add all objects group
            //var groupAll = new ValidationGroupViewModel()
            //{
            //    GroupName = "All Objects",
            //    IsAllObjectsGroup = true,
            //};
            //ValidationGroups.Add(groupAll);
            

            var definitions = new (string GroupName, Func<ObservableCollection<FeeAbstractObject>> ItemsFactory)[]
            {
                ("Button", () => new ObservableCollection<FeeAbstractObject>(allFeeObjects.OfType<FeeButton>())),
                ("Conveyor", () => new ObservableCollection<FeeAbstractObject>(allFeeObjects.OfType<FeeSurface>())),
                ("DetectionFlag", () => new ObservableCollection<FeeAbstractObject>(allFeeObjects.OfType<FeeDetectionFlag>())),
                ("Inserter", () => new ObservableCollection<FeeAbstractObject>(allFeeObjects.OfType<FeeInserter>())),
                ("Interface", () => new ObservableCollection<FeeAbstractObject>(allFeeObjects.OfType<FeeInterface>())),
                ("Signals", () => new ObservableCollection<FeeAbstractObject>(allFeeObjects.OfType<FeeInterface>().SelectMany(x => x.Signals))),
                ("Joint", () => new ObservableCollection<FeeAbstractObject>(allFeeObjects.OfType<FeeJoint>())),
                ("Label", () => new ObservableCollection<FeeAbstractObject>(allFeeObjects.OfType<FeeLabel>())),
                ("Logic", () => new ObservableCollection<FeeAbstractObject>(allFeeObjects.OfType<FeeLogic>())),
                ("Pick & Place", () => new ObservableCollection<FeeAbstractObject>(allFeeObjects.OfType<FeePickAndPlace>())),
                ("Reading/Writing Unit", () => new ObservableCollection<FeeAbstractObject>(allFeeObjects.Where(x => x is FeeReadingUnit || x is FeeWritingUnit))),
                ("Remover", () => new ObservableCollection<FeeAbstractObject>(allFeeObjects.OfType<FeeRemover>())),
                ("Reparenter", () => new ObservableCollection<FeeAbstractObject>(allFeeObjects.OfType<FeeReparenter>())),
                ("Sensor", () => new ObservableCollection<FeeAbstractObject>(allFeeObjects.OfType<FeeSensor>())),
                ("Stopper", () => new ObservableCollection<FeeAbstractObject>(allFeeObjects.OfType<FeeFloor>())),
                ("Workpiece", () => new ObservableCollection<FeeAbstractObject>(allFeeObjects.OfType<FeeFloor>().Where(x => x.Parent is FeeDetectionFlag detectFlag && detectFlag.Name == "Workpieces").ToList())),
            };

            foreach (var def in definitions)
            {
                var items = def.ItemsFactory();
                if (items.Any())
                {
                    var group = new ValidationGroupViewModel(items)
                    {
                        GroupName = def.GroupName,
                    };

                    group.ApplyFilter(FilterText);

                    ValidationGroups.Add(group);
                }
            }

            // Add marks group
            var groupMarks = new ValidationGroupViewModel(new ObservableCollection<FeeAbstractObject>(allFeeObjects.Where(x => x is FeePickAndPlace || x is FeeDecoration || x is FeeSensor || (x is FeeDetectionFlag flag && flag.IsWorkpiece))))
            {
                GroupName = "Marks",
                IsMarksGroup = true,
            };
            groupMarks.ApplyFilter(FilterText);
            ValidationGroups.Add(groupMarks);


            // Restore tab index
            var restoredTab = ValidationGroups.FirstOrDefault(g => g.GroupName == oldTabName);
            SelectedTab = restoredTab ?? ValidationGroups.FirstOrDefault();


            CalculateColumnWidths();

            IsBusyUpdatingFeeData = false;
        }



        public void Acknowledge_Object(object parameter)
        {
            if (parameter is FeeAbstractObject feeObj)
            {
                foreach (var issue in feeObj.PlausibilityIssues)
                {
                    issue.IsAcknowledged = true;
                    feeObj.NotifyIssueStateChanged();
                }
            }
        }








        //===========================================================================================================================
        // M E T H O D S   ( M A N U A L   C O N T R O L )
        //===========================================================================================================================

        //public void Set_Visibility(object parameter)
        //{
        //    if(parameter is FeeAbstractObject feeObj)
        //    {
        //        // Set Property in FEE
        //
        //        .Object.CreateObject(feeObj.Type, feeObj.Guid);
        //        App.
        //
        //
        //        .SetProperty(feeObj.Guid, nameof(ModelComponent.IsComponentActive), feeObj.Visible, "Model");
        //        App.ApiInstance.Object.Send(feeObj.Guid);
        //    }
        //}






        //===========================================================================================================================
        // M E T H O D S   ( H E L P E R S )
        //===========================================================================================================================


        private void CalculateColumnWidths()
        {
            var allNames = ValidationGroups
                .Where(g => !g.IsAllObjectsGroup)
                .SelectMany(g => g.Items)
                .Select(i => i.Name)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList();

            if (!allNames.Any())
            {
                NameColumnWidth = 200;      //Fallback
                return;
            }

            string longestName = allNames.OrderByDescending(n => n.Length).First();
            NameColumnWidth = longestName.Length * 7 + 30;


            var allIssues = ValidationGroups
                .Where(g => !g.IsAllObjectsGroup)
                .SelectMany(g => g.Items)
                .SelectMany(i => i.PlausibilityIssues)
                .Select(i => i.Message)
                .Where(m => !string.IsNullOrEmpty(m))
                .ToList();

            if (!allIssues.Any())
            {
                IssueColumnWidth = 500;     //Fallback
                return;
            }

            string longestIssue = allIssues.OrderByDescending(i => i.Length).First();
            IssueColumnWidth = longestIssue.Length * 7 + 30;
        }









    }

}
