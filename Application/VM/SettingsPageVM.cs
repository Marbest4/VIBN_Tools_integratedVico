using System.Collections.ObjectModel;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;
using NPOI.POIFS.Crypt;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;
using VIBN_Tools.KanbanizeService;
using VIBN_Tools.Settings;
using static VIBN_Tools.Settings.ProjectSettings;

namespace VIBN_Tools.Application.VM
{
    public class SettingsPageVM : MvvmBase
    {

        //===========================================================================================================================
        // B I N D I N G S   -   P R O J E C T   S E T T I N G S
        //===========================================================================================================================

        // Array with TemplateType values
        public Array TemplateTypes => Enum.GetValues(typeof(TemplateType));

        private readonly ProjectSettings _projectSettings;
        private readonly FeeConnectionService _connectionService;
        private readonly FeeObjectService _feeObjectService;

        public TemplateType SelectedTemplate
        {
            get => _projectSettings.SelectedTemplate;
            set
            {
                _projectSettings.SelectedTemplate = value;
                OnPropertyChanged();
            }
        }






        //===========================================================================================================================
        // B I N D I N G S   -   R E M O T E   &   C O N N E C T I O N
        //===========================================================================================================================

        // Checkbox for using localhost
        private bool _checkboxUseLocalhost;
        public bool CheckboxUseLocalhost
        {
            get { return _checkboxUseLocalhost; }
            set
            {
                if (_checkboxUseLocalhost == value) return;

                _checkboxUseLocalhost = value;
                OnPropertyChanged();

                if (_isServerChangeActive) return;

                if (value)
                {
                    _isServerChangeActive = true;
                    SelectedServer = ServerNames[0].ToString();
                    _isServerChangeActive = false;
                }
            }
        }


        private string _selectedServer;
        public string SelectedServer
        {
            get { return _selectedServer; }
            set
            {
                if (_selectedServer == value) return;

                _selectedServer = value;
                OnPropertyChanged();

                if (_isServerChangeActive) return;

                if (!string.Equals(value, ServerNames[0].ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    _isServerChangeActive = true;
                    CheckboxUseLocalhost = false;
                    _isServerChangeActive = false;
                }

                //CheckServerAsync();
            }
        }

        private bool _isServerChangeActive = false;

        private bool _changeServerFromCheckBox = false;
        private bool _changeServerFromComboBox = false;

        public ObservableCollection<string> ServerNames { get; set; }

        private bool _isServerReachable;
        public bool IsServerReachable
        {
            get { return _isServerReachable; }
            set
            {
                _isServerReachable = value;
                OnPropertyChanged();
            }
        }



        // Toggle Buttons Displays
        private bool _useDisplay1;
        public bool UseDisplay1
        {
            get { return _useDisplay1; }
            set
            {
                _useDisplay1 = value;
                OnPropertyChanged();
                UpdateUsedDisplays();
            }
        }

        private bool _useDisplay2;
        public bool UseDisplay2
        {
            get { return _useDisplay2; }
            set
            {
                _useDisplay2 = value;
                OnPropertyChanged();
                UpdateUsedDisplays();
            }
        }

        private bool _useDisplay3;
        public bool UseDisplay3
        {
            get { return _useDisplay3; }
            set
            {
                _useDisplay3 = value;
                OnPropertyChanged();
                UpdateUsedDisplays();
            }
        }

        private bool _useDisplay4;
        public bool UseDisplay4
        {
            get { return _useDisplay4; }
            set
            {
                _useDisplay4 = value;
                OnPropertyChanged();
                UpdateUsedDisplays();
            }
        }


        public int UsedDisplays { get; set; }









        //===========================================================================================================================
        // B I N D I N G S   -   S I M U L A T I O N   ( F E E )
        //===========================================================================================================================

        public ICommand ConnectToFee => GetCommandBindingAsync(Connect_ToFee);
        public ICommand DisconnectFromFee => GetCommandBindingAsync(Disconnect_FromFee);
        public FeeConnectionService Connection => Services.Connection;


        public ICommand CreateProjectBase => GetCommandBindingAsync(Create_ProjectBase);


        private string _connectedServer;
        public string ConnectedServer
        {
            get { return _connectedServer; }
            set
            {
                _connectedServer = value;
                OnPropertyChanged();
            }
        }

        private string _feeObjectStatus;
        public string FeeObjectStatus
        {
            get { return _feeObjectStatus; }
            set
            {
                _feeObjectStatus = value;
                OnPropertyChanged();
            }
        }


        private bool _loadFeeData;
        public bool LoadFeeData
        {
            get { return _loadFeeData; }
            set 
            { 
                _loadFeeData = value; 
                OnPropertyChanged();
            }
        }







        public ICommand TestButton => GetCommandBindingAsync(Test_Button);
        private KanbanizeService.KanbanizeService _kanbanizeService = new KanbanizeService.KanbanizeService();

        public ObservableCollection<KanbanizeBoard> Boards {  get; set; } = new ObservableCollection<KanbanizeBoard>();




        private async Task Test_Button()
        {
            var startTime = DateTime.Now;

            var boards = await _kanbanizeService.LoadBoardsAsync();
            Boards.Clear();
            foreach (var item in boards)
                Boards.Add(item);

            var cardsTask = _kanbanizeService.LoadAllCardsAsync();

            var boardTasks = Boards.Select(async board =>
            {
                var workflowsTask = _kanbanizeService.LoadWorkflowsAsync(board.Id);
                var lanesTask = _kanbanizeService.LoadLanesAsync(board.Id);
                var columnsTask = _kanbanizeService.LoadColumnsAsync(board.Id);

                await Task.WhenAll(workflowsTask, lanesTask, columnsTask);

                board.Workflows = workflowsTask.Result;
                board.AllColumns = columnsTask.Result;

                var lanes = lanesTask.Result;

                foreach (var workflow in board.Workflows)
                {
                    workflow.Lanes = lanes.Where(l => l.WorkflowId == workflow.Id).OrderBy(l => l.Position).ToList();
                    workflow.Columns = board.AllColumns.Where(c => c.WorkflowId == workflow.Id).ToList();
                }
            });

            var allTasks = new List<Task>();
            allTasks.AddRange(boardTasks);
            allTasks.Add(cardsTask);

            await Task.WhenAll(allTasks);


            // Map Cards
            var allCards = cardsTask.Result;

            foreach(var board in Boards)
            {
                var boardCards = allCards.Where(c => c.BoardId == board.Id);

                foreach (var card in boardCards)
                {
                    var lane = board.Workflows.SelectMany(w => w.Lanes).FirstOrDefault(l => l.Id == card.LaneId);
                    lane?.Cards.Add(card);

                    var column = board.AllColumns.FirstOrDefault(c => c.Id == card.ColumnId);
                    card.Section = column?.Section;
                }
            }

            

            var operationTime = (DateTime.Now - startTime).TotalSeconds;

            MessageBox.Show($"Total seconds reading Kanbanize Data: {operationTime}s");

        }







        //===========================================================================================================================
        // C O N S T R U C T O R
        //===========================================================================================================================

        public SettingsPageVM(ProjectSettings projectSettings, FeeConnectionService connectionService)
        {
            ServerNames = new ObservableCollection<string>(RemoteConnection.ServerUserNames.Select(x => x.Server));
            _projectSettings = projectSettings;
            _connectionService = connectionService;

            Services.FeeObjects.FeeObjectsUpdated += OnFeeObjectsLoaded;

            CheckboxUseLocalhost = true;

            ConnectedServer = "---";

            Connection.Connected += OnConnected;

            LoadFeeData = false;
        }















        //===========================================================================================================================
        // M E T H O D S   ( B U T T O N S )
        //===========================================================================================================================

        private async Task Connect_ToFee(object parameter)
        {
            _connectionService.LoadFeeDataOnConnect = LoadFeeData;

            Services.ApiInstance.Connect(SelectedServer, "admin", "admin");
        }




        private async Task Disconnect_FromFee(object parameter)
        {
            Services.ApiInstance.Disconnect();

            ConnectedServer = "---";
        }


        private async Task Create_ProjectBase(object parameter)
        {
            await CreateFeeSimulationBaseAsync();

            await ImportFeeLogicAndCabinetFilesAsync();
        }



        







        //===========================================================================================================================
        // M E T H O D S   ( H E L P E R S )
        //===========================================================================================================================



        private void UpdateUsedDisplays()
        {
            UsedDisplays = new[] { UseDisplay1, UseDisplay2, UseDisplay3, UseDisplay4 }.Count(x => x);
        }

        private async Task CheckServerAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedServer))
            {
                IsServerReachable = false;
                return;
            }

            IsServerReachable = await RemoteConnection.CheckServerReachableAsync(SelectedServer);
        }


        private void OnFeeObjectsLoaded(object sender, FeeObjectsUpdatedEventargs e)
        {
            FeeObjectStatus = $"Total time reading Fee data: {e.ElapsedTime.TotalSeconds.ToString("F2")}s";
        }







        //===========================================================================================================================
        // E V E N T S
        //===========================================================================================================================

        private void OnConnected()
        {
            ConnectedServer = SelectedServer;
        }

    }
}
