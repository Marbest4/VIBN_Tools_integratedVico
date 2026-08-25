using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using System.Windows.Input;
using VIBN_Tools.Core.ViCo;
using VIBN_Tools.GlobalClasses;

namespace VIBN_Tools.Application.VM;

/// <summary>Presentation-only state for one searchable ViCo workstation row.</summary>
public sealed class ViCoWorkstationRowVM : MvvmBase
{
    public ViCoWorkstationRowVM(ViCoWorkstation model)
    {
        Model = model;
    }

    public ViCoWorkstation Model { get; }
    public string PcName => Model.PcName;
    public string DisplayName => Model.DisplayName;
    public string UserName => Model.UserName;
    public string Status => Model.Status;
    public string ProjectSummary => Model.ProjectSummary;
    public string AdditionalProjects => Model.AdditionalProjects;
    public string SoftwareInformation => Model.SoftwareInformation;
    public IReadOnlyList<AutomationSoftwareInfo> SoftwareDetails => Model.AutomationSoftware;
    public string FeeInformation => Model.FeeInformation;
    public string HardwareInformation => Model.HardwareInformation;
    public int RobotCount => Model.RobotCount;
    public string RobotSummary => Model.RobotSummary;
    public IReadOnlyList<string> Details => Model.Details;

    private string _onlineStatus = "Wird geprüft …";
    public string OnlineStatus
    {
        get => _onlineStatus;
        private set
        {
            _onlineStatus = value;
            OnPropertyChanged();
        }
    }

    private string _onlineStatusBackground = "#FFF3F5F7";
    public string OnlineStatusBackground
    {
        get => _onlineStatusBackground;
        private set
        {
            _onlineStatusBackground = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Updates the compact availability cell without putting WPF types into the view model.</summary>
    public void SetOnline(bool isOnline)
    {
        OnlineStatus = isOnline ? "Online" : "Offline";
        OnlineStatusBackground = isOnline ? "#FFC6EFCE" : "#FFFFC7CE";
    }
}

/// <summary>
/// Coordinates unified workstation search, cache refresh, online availability
/// and the actions that open a selected workstation/project.
/// </summary>
public sealed class ViCoSearchPageVM : MvvmBase, IDisposable
{
    private readonly IViCoWorkstationCatalog _catalog;
    private readonly IViCoWorkstationSearch _search;
    private readonly Func<CancellationToken, Task<IViCoRelatedPathResolver>> _pathResolverFactory;
    private readonly INetworkAvailabilityService _network;
    private readonly IRemoteDesktopService _remoteDesktop;
    private readonly IExternalPathLauncher _launcher;
    private readonly IViCoOnlineRefreshService _onlineRefresh;
    private readonly ViCoWorkspaceContext _workspaceContext;
    private readonly Action<IEnumerable<ViCoWorkstation>> _synchronizeWorkstations;
    private readonly IApplicationLog _log;
    private IReadOnlyList<ViCoWorkstation> _allWorkstations = Array.Empty<ViCoWorkstation>();
    private IViCoRelatedPathResolver? _pathResolver;
    private CancellationTokenSource? _availabilityCancellation;
    private CancellationTokenSource? _searchDebounceCancellation;
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private readonly ConcurrentDictionary<string, (bool IsOnline, DateTimeOffset CheckedAt)> _availabilityCache =
        new(StringComparer.OrdinalIgnoreCase);
    private bool _initialized;

    public ViCoSearchPageVM(
        IViCoWorkstationCatalog catalog,
        IViCoWorkstationSearch search,
        Func<CancellationToken, Task<IViCoRelatedPathResolver>> pathResolverFactory,
        INetworkAvailabilityService network,
        IRemoteDesktopService remoteDesktop,
        IExternalPathLauncher launcher,
        IViCoOnlineRefreshService onlineRefresh,
        ViCoWorkspaceContext workspaceContext,
        Action<IEnumerable<ViCoWorkstation>> synchronizeWorkstations,
        IApplicationLog? log = null)
    {
        _catalog = catalog;
        _search = search;
        _pathResolverFactory = pathResolverFactory;
        _network = network;
        _remoteDesktop = remoteDesktop;
        _launcher = launcher;
        _onlineRefresh = onlineRefresh;
        _workspaceContext = workspaceContext;
        _synchronizeWorkstations = synchronizeWorkstations;
        _log = log ?? NullApplicationLog.Instance;

        RefreshCommand = GetCommandBindingAsync(RefreshFromBestAvailableSourceAsync);
        ConnectRemoteCommand = GetCommandBinding(ConnectRemote);
        OpenTeamViewerCommand = GetCommandBinding(() => _launcher.Open("https://web.teamviewer.com/remote-support?tab=Sessions"));
        OpenPcProjectsCommand = GetCommandBinding(() => OpenRelated(ViCoRelatedPathKind.WorkstationProjects));
        OpenSimulationCommand = GetCommandBinding(() => OpenRelated(ViCoRelatedPathKind.Simulation));
        OpenCommissioningCommand = GetCommandBinding(() => OpenRelated(ViCoRelatedPathKind.Commissioning));
        OpenPlanningCommand = GetCommandBinding(() => OpenRelated(ViCoRelatedPathKind.Planning));
    }

    public ObservableCollection<ViCoWorkstationRowVM> Results { get; } = new();
    public ObservableCollection<string> Projects { get; } = new();
    public ICommand RefreshCommand { get; }
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
            _ = ApplySearchDebouncedAsync();
        }
    }

    private ViCoSearchMode _searchMode = ViCoSearchMode.All;
    public ViCoSearchMode SearchMode
    {
        get => _searchMode;
        set
        {
            _searchMode = value;
            OnPropertyChanged();
            ApplySearch();
        }
    }

    private ViCoWorkstationRowVM? _selectedWorkstation;
    public ViCoWorkstationRowVM? SelectedWorkstation
    {
        get => _selectedWorkstation;
        set
        {
            _selectedWorkstation = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedRemoteUser));
            Projects.Clear();
            if (value is not null)
            {
                foreach (var project in value.Model.Projects)
                    Projects.Add(project);
                SelectedProject = Projects.FirstOrDefault();
            }
            else
            {
                SelectedProject = null;
            }
            UpdatePathInformation();
        }
    }

    public string SelectedRemoteUser => SelectedWorkstation?.UserName ?? string.Empty;

    private string? _selectedProject;
    public string? SelectedProject
    {
        get => _selectedProject;
        set
        {
            _selectedProject = value;
            OnPropertyChanged();
            UpdatePathInformation();
        }
    }

    private string _pathInformation = "PC und Projekt auswählen.";
    public string PathInformation
    {
        get => _pathInformation;
        private set
        {
            _pathInformation = value;
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
        await RefreshCachedDataAsync();
        if (_onlineRefresh.IsConfigured)
            _ = RunPeriodicRefreshAsync(_lifetimeCancellation.Token);
    }

    public void Dispose()
    {
        _availabilityCancellation?.Cancel();
        _availabilityCancellation?.Dispose();
        _searchDebounceCancellation?.Cancel();
        _searchDebounceCancellation?.Dispose();
        _lifetimeCancellation.Cancel();
        _lifetimeCancellation.Dispose();
    }

    /// <summary>
    /// Uses a configured online source first, then always reloads the resulting
    /// local cache. Without an API key this still provides a useful cache refresh.
    /// </summary>
    private async Task RefreshFromBestAvailableSourceAsync()
    {
        if (IsBusy)
            return;
        if (!_onlineRefresh.IsConfigured)
        {
            await RefreshCachedDataAsync();
            return;
        }

        IsBusy = true;
        StatusText = "Kanbanize-Daten werden aktualisiert …";
        var onlineUpdateSucceeded = false;
        try
        {
            await _onlineRefresh.RefreshAsync();
            onlineUpdateSucceeded = true;
            _log.Information("Kanbanize", "PC-, Projekt- und Robotikdaten wurden aktualisiert.");
        }
        catch (Exception exception)
        {
            _log.Error("Kanbanize", "Die Online-Aktualisierung ist fehlgeschlagen; der vorhandene Cache wird verwendet.", exception);
        }
        finally
        {
            IsBusy = false;
        }

        await RefreshCachedDataAsync(onlineUpdateSucceeded
            ? null
            : "Online-Aktualisierung fehlgeschlagen; vorhandener Cache wurde geladen.");
    }

    /// <summary>Reads the existing cache and rebuilds search/path state without a network write.</summary>
    private async Task RefreshCachedDataAsync(string? completionMessage = null)
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
            _synchronizeWorkstations(_allWorkstations);
            ApplySearch();
            StatusText = completionMessage ?? (snapshot.Warnings.Count == 0
                ? $"{_allWorkstations.Count} Arbeitsstationen geladen. Kanbanize-Benutzer wurden synchronisiert."
                : $"{_allWorkstations.Count} Arbeitsstationen geladen; {snapshot.Warnings.Count} Datenquelle(n) nicht erreichbar.");
            _log.Information("ViCo-Suche", StatusText);
            foreach (var warning in snapshot.Warnings)
                _log.Warning("ViCo-Suche", "Eine Datenquelle konnte nicht gelesen werden.", warning);
        }
        catch (Exception exception)
        {
            StatusText = "PC- und Projektdaten konnten nicht geladen werden.";
            _log.Error("ViCo-Suche", StatusText, exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RunPeriodicRefreshAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                if (IsBusy)
                    continue;
                try
                {
                    await _onlineRefresh.RefreshAsync(cancellationToken);
                    await RefreshCachedDataAsync();
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    StatusText = $"Kanbanize-Aktualisierung fehlgeschlagen: {exception.Message}";
                    _log.Error("Kanbanize", "Die periodische Aktualisierung ist fehlgeschlagen.", exception);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Application shutdown.
        }
    }

    private void ApplySearch()
    {
        var selected = SelectedWorkstation?.PcName;
        Results.Clear();
        foreach (var item in _search.Search(_allWorkstations, SearchText, SearchMode))
            Results.Add(new ViCoWorkstationRowVM(item));
        SelectedWorkstation = Results.FirstOrDefault(item =>
            string.Equals(item.PcName, selected, StringComparison.OrdinalIgnoreCase)) ?? Results.FirstOrDefault();
        StartAvailabilityRefresh();
    }

    private async Task ApplySearchDebouncedAsync()
    {
        _searchDebounceCancellation?.Cancel();
        _searchDebounceCancellation?.Dispose();
        _searchDebounceCancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCancellation.Token);
        try
        {
            await Task.Delay(300, _searchDebounceCancellation.Token);
            ApplySearch();
        }
        catch (OperationCanceledException)
        {
            // A newer search text superseded this update.
        }
    }

    private void StartAvailabilityRefresh()
    {
        _availabilityCancellation?.Cancel();
        _availabilityCancellation?.Dispose();
        _availabilityCancellation = new CancellationTokenSource();
        _ = RefreshAvailabilityAsync(Results.ToArray(), _availabilityCancellation.Token);
    }

    private async Task RefreshAvailabilityAsync(
        IReadOnlyCollection<ViCoWorkstationRowVM> rows,
        CancellationToken cancellationToken)
    {
        using var throttle = new SemaphoreSlim(8);
        var tasks = rows.Select(async row =>
        {
            var acquired = false;
            try
            {
                if (_availabilityCache.TryGetValue(row.PcName, out var cached) &&
                    DateTimeOffset.Now - cached.CheckedAt < TimeSpan.FromSeconds(30))
                {
                    row.SetOnline(cached.IsOnline);
                    return;
                }
                await throttle.WaitAsync(cancellationToken);
                acquired = true;
                var isOnline = await _network.PingAsync(row.PcName, cancellationToken);
                _availabilityCache[row.PcName] = (isOnline, DateTimeOffset.Now);
                row.SetOnline(isOnline);
            }
            catch (OperationCanceledException)
            {
                // A new search superseded this availability scan.
            }
            finally
            {
                if (acquired)
                    throttle.Release();
            }
        });
        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            // A new search superseded this availability scan.
        }
    }

    private void ConnectRemote()
    {
        if (SelectedWorkstation is null)
            return;
        if (string.IsNullOrWhiteSpace(SelectedWorkstation.UserName))
        {
            StatusText = "Die Kanbanize-Karte enthält keinen gültigen Remote-Benutzer.";
            _log.Warning("Remote Desktop", StatusText);
            return;
        }

        var monitors = new[] { UseMonitor1, UseMonitor2, UseMonitor3, UseMonitor4 }
            .Select((selected, index) => (selected, index))
            .Where(value => value.selected)
            .Select(value => value.index)
            .ToArray();
        try
        {
            _remoteDesktop.Connect(SelectedWorkstation.PcName, SelectedWorkstation.UserName, monitors);
            StatusText = $"Remote Desktop wird als {SelectedWorkstation.UserName} gestartet.";
            _log.Information("Remote Desktop", $"Verbindung zu {SelectedWorkstation.PcName} als {SelectedWorkstation.UserName} gestartet.");
        }
        catch (Exception exception)
        {
            StatusText = $"Remote Desktop konnte nicht gestartet werden: {exception.Message}";
            _log.Error("Remote Desktop", $"Verbindung zu {SelectedWorkstation.PcName} konnte nicht gestartet werden.", exception);
        }
    }

    private void OpenRelated(ViCoRelatedPathKind kind)
    {
        if (SelectedWorkstation is null || _pathResolver is null)
            return;
        var project = SelectedProject ?? SearchText;
        var path = _pathResolver.Resolve(SelectedWorkstation.Model, project, kind);
        if (string.IsNullOrWhiteSpace(path))
        {
            StatusText = "Für die Auswahl wurde kein passender Pfad gefunden.";
            return;
        }
        _launcher.Open(path);
        StatusText = $"Geöffnet: {path}";
        _log.Information("ViCo-Pfade", StatusText);
    }

    private void UpdatePathInformation()
    {
        if (SelectedWorkstation is null || _pathResolver is null || string.IsNullOrWhiteSpace(SelectedProject))
        {
            PathInformation = "PC und Projekt auswählen.";
            return;
        }

        var workstation = SelectedWorkstation.Model;
        var simulation = _pathResolver.Resolve(workstation, SelectedProject, ViCoRelatedPathKind.Simulation);
        var commissioning = _pathResolver.Resolve(workstation, SelectedProject, ViCoRelatedPathKind.Commissioning);
        var planning = _pathResolver.Resolve(workstation, SelectedProject, ViCoRelatedPathKind.Planning);
        var workstationProject = _pathResolver.Resolve(workstation, SelectedProject, ViCoRelatedPathKind.WorkstationProject);
        PathInformation = string.Join(Environment.NewLine, new[]
        {
            Describe("PC-Projekt", workstationProject),
            Describe("Simulation", simulation),
            Describe("PLC", commissioning),
            Describe("Planung", planning)
        });
        _workspaceContext.Update(workstation, SelectedProject, simulation, workstationProject);
    }

    private static string Describe(string label, string? path) =>
        string.IsNullOrWhiteSpace(path) ? $"{label}: nicht gefunden" : $"{label}: {path}";
}
