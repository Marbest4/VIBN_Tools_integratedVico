using System.Collections.ObjectModel;
using System.Windows.Input;
using VIBN_Tools.Core.ViCo;
using VIBN_Tools.GlobalClasses;

namespace VIBN_Tools.Application.VM;

public sealed class ViCoSearchPageVM : MvvmBase
{
    private readonly IViCoWorkstationCatalog _catalog;
    private readonly IViCoWorkstationSearch _search;
    private readonly Func<CancellationToken, Task<IViCoRelatedPathResolver>> _pathResolverFactory;
    private readonly INetworkAvailabilityService _network;
    private readonly IRemoteDesktopService _remoteDesktop;
    private readonly IExternalPathLauncher _launcher;
    private readonly IViCoOnlineRefreshService _onlineRefresh;
    private IReadOnlyList<ViCoWorkstation> _allWorkstations = Array.Empty<ViCoWorkstation>();
    private IViCoRelatedPathResolver? _pathResolver;
    private bool _initialized;

    public ViCoSearchPageVM(
        IViCoWorkstationCatalog catalog,
        IViCoWorkstationSearch search,
        Func<CancellationToken, Task<IViCoRelatedPathResolver>> pathResolverFactory,
        INetworkAvailabilityService network,
        IRemoteDesktopService remoteDesktop,
        IExternalPathLauncher launcher,
        IViCoOnlineRefreshService onlineRefresh)
    {
        _catalog = catalog;
        _search = search;
        _pathResolverFactory = pathResolverFactory;
        _network = network;
        _remoteDesktop = remoteDesktop;
        _launcher = launcher;
        _onlineRefresh = onlineRefresh;

        RefreshCommand = GetCommandBindingAsync(RefreshAsync);
        RefreshOnlineCommand = GetCommandBindingAsync(RefreshOnlineAsync);
        ConnectRemoteCommand = GetCommandBinding(ConnectRemote);
        OpenTeamViewerCommand = GetCommandBinding(() => _launcher.Open("https://web.teamviewer.com/remote-support?tab=Sessions"));
        OpenPcProjectsCommand = GetCommandBinding(() => OpenRelated(ViCoRelatedPathKind.WorkstationProjects));
        OpenSimulationCommand = GetCommandBinding(() => OpenRelated(ViCoRelatedPathKind.Simulation));
        OpenCommissioningCommand = GetCommandBinding(() => OpenRelated(ViCoRelatedPathKind.Commissioning));
        OpenPlanningCommand = GetCommandBinding(() => OpenRelated(ViCoRelatedPathKind.Planning));
    }

    public ObservableCollection<ViCoWorkstation> Results { get; } = new();

    public ObservableCollection<string> Projects { get; } = new();

    public ICommand RefreshCommand { get; }

    public ICommand RefreshOnlineCommand { get; }

    public bool CanRefreshOnline => _onlineRefresh.IsConfigured;

    public ICommand ConnectRemoteCommand { get; }

    public ICommand OpenTeamViewerCommand { get; }

    public ICommand OpenPcProjectsCommand { get; }

    public ICommand OpenSimulationCommand { get; }

    public ICommand OpenCommissioningCommand { get; }

    public ICommand OpenPlanningCommand { get; }

    public int MonitorCount => _remoteDesktop.MonitorCount;

    public bool HasMonitor2 => MonitorCount >= 2;

    public bool HasMonitor3 => MonitorCount >= 3;

    public bool HasMonitor4 => MonitorCount >= 4;

    public bool UseMonitor1 { get; set; } = true;

    public bool UseMonitor2 { get; set; }

    public bool UseMonitor3 { get; set; }

    public bool UseMonitor4 { get; set; }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            OnPropertyChanged();
            ApplySearch();
        }
    }

    private ViCoSearchMode _searchMode = ViCoSearchMode.Project;
    public ViCoSearchMode SearchMode
    {
        get => _searchMode;
        set
        {
            _searchMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsProjectMode));
            OnPropertyChanged(nameof(IsWorkstationMode));
            ApplySearch();
        }
    }

    public bool IsProjectMode
    {
        get => SearchMode == ViCoSearchMode.Project;
        set { if (value) SearchMode = ViCoSearchMode.Project; }
    }

    public bool IsWorkstationMode
    {
        get => SearchMode == ViCoSearchMode.Workstation;
        set { if (value) SearchMode = ViCoSearchMode.Workstation; }
    }

    private ViCoWorkstation? _selectedWorkstation;
    public ViCoWorkstation? SelectedWorkstation
    {
        get => _selectedWorkstation;
        set
        {
            _selectedWorkstation = value;
            OnPropertyChanged();
            Projects.Clear();
            if (value is not null)
            {
                foreach (var project in value.Projects)
                    Projects.Add(project);
                SelectedProject = Projects.FirstOrDefault();
                _ = RefreshAvailabilityAsync(value);
            }
        }
    }

    private string? _selectedProject;
    public string? SelectedProject
    {
        get => _selectedProject;
        set
        {
            _selectedProject = value;
            OnPropertyChanged();
        }
    }

    private bool _isOnline;
    public bool IsOnline
    {
        get => _isOnline;
        private set
        {
            _isOnline = value;
            OnPropertyChanged();
        }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            _isBusy = value;
            OnPropertyChanged();
        }
    }

    private string _statusText = "ViCo-Suche ist bereit.";
    public string StatusText
    {
        get => _statusText;
        private set
        {
            _statusText = value;
            OnPropertyChanged();
        }
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;
        _initialized = true;
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (IsBusy)
            return;
        IsBusy = true;
        StatusText = "PC- und Projektdaten werden geladen …";
        try
        {
            var catalogTask = _catalog.LoadAsync();
            var resolverTask = _pathResolverFactory(CancellationToken.None);
            await Task.WhenAll(catalogTask, resolverTask);
            var snapshot = await catalogTask;
            _pathResolver = await resolverTask;
            _allWorkstations = snapshot.Workstations;
            ApplySearch();
            StatusText = snapshot.Warnings.Count == 0
                ? $"{_allWorkstations.Count} Arbeitsstationen geladen."
                : $"{_allWorkstations.Count} Arbeitsstationen geladen; {snapshot.Warnings.Count} Datenquelle(n) nicht erreichbar.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshOnlineAsync()
    {
        if (IsBusy || !_onlineRefresh.IsConfigured)
        {
            StatusText = "Kanbanize-Zugriff ist auf diesem Rechner nicht konfiguriert.";
            return;
        }

        IsBusy = true;
        StatusText = "Kanbanize-Daten werden aktualisiert …";
        try
        {
            await _onlineRefresh.RefreshAsync();
        }
        finally
        {
            IsBusy = false;
        }
        await RefreshAsync();
    }

    private void ApplySearch()
    {
        var selected = SelectedWorkstation?.PcName;
        Results.Clear();
        foreach (var item in _search.Search(_allWorkstations, SearchText, SearchMode))
            Results.Add(item);
        SelectedWorkstation = Results.FirstOrDefault(item =>
            string.Equals(item.PcName, selected, StringComparison.OrdinalIgnoreCase));
    }

    private async Task RefreshAvailabilityAsync(ViCoWorkstation workstation)
    {
        IsOnline = await _network.PingAsync(workstation.PcName);
    }

    private void ConnectRemote()
    {
        if (SelectedWorkstation is null)
            return;
        var monitors = new[] { UseMonitor1, UseMonitor2, UseMonitor3, UseMonitor4 }
            .Select((selected, index) => (selected, index))
            .Where(value => value.selected)
            .Select(value => value.index)
            .ToArray();
        _remoteDesktop.Connect(SelectedWorkstation.PcName, SelectedWorkstation.UserName, monitors);
    }

    private void OpenRelated(ViCoRelatedPathKind kind)
    {
        if (SelectedWorkstation is null || _pathResolver is null)
            return;
        var project = SelectedProject ?? SearchText;
        var path = _pathResolver.Resolve(SelectedWorkstation, project, kind);
        if (string.IsNullOrWhiteSpace(path))
        {
            StatusText = "Für die Auswahl wurde kein passender Pfad gefunden.";
            return;
        }
        _launcher.Open(path);
    }
}
