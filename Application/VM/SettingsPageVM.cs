using System.Collections.ObjectModel;
using System.Net.Http;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;
using NPOI.POIFS.Crypt;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;
using VIBN_Tools.KanbanizeService;
using VIBN_Tools.Settings;
using VIBN_Tools.Core.ViCo;
using static VIBN_Tools.Settings.ProjectSettings;

namespace VIBN_Tools.Application.VM
{
    /// <summary>Coordinates project settings and confirms FEE connections before showing them as active.</summary>
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
        private readonly IWorkstationDirectory _workstations;
        private readonly IApplicationLog _log;

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
                    SelectedServer = "localhost";
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

                if (!string.Equals(value, "localhost", StringComparison.OrdinalIgnoreCase))
                {
                    _isServerChangeActive = true;
                    CheckboxUseLocalhost = false;
                    _isServerChangeActive = false;
                }

                _ = CheckServerAsync(value);
            }
        }

        private bool _isServerChangeActive = false;

        private bool _changeServerFromCheckBox = false;
        private bool _changeServerFromComboBox = false;

        public ObservableCollection<string> ServerNames => _workstations.PcNames;

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

        private string _connectionStatus = "Noch keine Verbindung aufgebaut.";
        public string ConnectionStatus
        {
            get => _connectionStatus;
            private set
            {
                _connectionStatus = value;
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

        public SettingsPageVM(
            ProjectSettings projectSettings,
            FeeConnectionService connectionService,
            IWorkstationDirectory workstations,
            IApplicationLog? log = null)
        {
            _projectSettings = projectSettings;
            _connectionService = connectionService;
            _workstations = workstations;
            _log = log ?? NullApplicationLog.Instance;

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
            if (string.IsNullOrWhiteSpace(SelectedServer))
            {
                ConnectionStatus = "Bitte zuerst einen PC auswählen.";
                _log.Warning("Project Settings", ConnectionStatus);
                return;
            }

            _connectionService.LoadFeeDataOnConnect = LoadFeeData;
            var stopwatch = Stopwatch.StartNew();
            ConnectionStatus = $"Verbindung zu {SelectedServer} wird aufgebaut …";
            // Clear stale UI state before the SDK confirms the new endpoint.
            ConnectedServer = "---";
            _log.Information("Project Settings", ConnectionStatus);
            try
            {
                if (_connectionService.IsConnected)
                {
                    Services.ApiInstance.Disconnect();
                    if (!await _connectionService.WaitForDisconnectedAsync(TimeSpan.FromSeconds(3)))
                    {
                        stopwatch.Stop();
                        ConnectionStatus = "Die bestehende FEE-Verbindung konnte nicht sauber getrennt werden.";
                        _log.Warning("Project Settings", ConnectionStatus);
                        return;
                    }
                }

                Services.ApiInstance.Connect(SelectedServer, "admin", "admin");
                var connected = await _connectionService.WaitForConnectedAsync(TimeSpan.FromSeconds(10));
                stopwatch.Stop();
                if (!connected)
                {
                    await DisconnectAfterFailedConnectionAsync();
                    ConnectedServer = "---";
                    ConnectionStatus = $"Verbindung zu {SelectedServer} konnte nicht bestätigt werden (Zeitüberschreitung).";
                    _log.Warning("Project Settings", ConnectionStatus);
                    return;
                }

                ConnectedServer = SelectedServer;
                ConnectionStatus = $"Mit {SelectedServer} verbunden ({stopwatch.Elapsed.TotalSeconds:F1} s).";
                _log.Information("Project Settings", ConnectionStatus);
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                await DisconnectAfterFailedConnectionAsync();
                ConnectedServer = "---";
                ConnectionStatus = $"Verbindung zu {SelectedServer} fehlgeschlagen.";
                _log.Error("Project Settings", ConnectionStatus, exception);
            }
        }




        private async Task Disconnect_FromFee(object parameter)
        {
            Services.ApiInstance.Disconnect();

            ConnectedServer = "---";
            ConnectionStatus = "Verbindung getrennt.";
            _log.Information("Project Settings", ConnectionStatus);
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

        private async Task CheckServerAsync(string serverName)
        {
            if (string.IsNullOrWhiteSpace(serverName))
            {
                IsServerReachable = false;
                return;
            }

            var reachable = await RemoteConnection.CheckServerReachableAsync(serverName);
            if (!string.Equals(SelectedServer, serverName, StringComparison.OrdinalIgnoreCase))
                return;
            IsServerReachable = reachable;
            if (!reachable)
                _log.Warning("Project Settings", $"{serverName} antwortet nicht auf Ping. Ein Verbindungsversuch bleibt möglich.");
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
            // The periodic service only raises this after the SDK reports
            // NetworkState.Connected; still avoid displaying an empty endpoint.
            if (!string.IsNullOrWhiteSpace(SelectedServer))
                ConnectedServer = SelectedServer;
        }

        private async Task DisconnectAfterFailedConnectionAsync()
        {
            try
            {
                Services.ApiInstance.Disconnect();
                await _connectionService.WaitForDisconnectedAsync(TimeSpan.FromSeconds(2));
            }
            catch
            {
                // The original connection exception is more useful to the caller.
            }
        }

    }
}
