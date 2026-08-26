using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using System.Windows.Input;
using VIBN_Tools.Core.ViCo;
using VIBN_Tools.GlobalClasses;

namespace VIBN_Tools.Application.VM;

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
    private readonly IRemoteSessionService _remoteSessions;
    private readonly IExternalPathLauncher _launcher;
    private readonly IViCoOnlineRefreshService _onlineRefresh;
    private readonly IViCoWorkstationConfigurationService _configurationService;
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
    private readonly ConcurrentDictionary<string, (ViCoRemoteSessionInfo Info, DateTimeOffset CheckedAt)> _remoteSessionCache =
        new(StringComparer.OrdinalIgnoreCase);
    private bool _initialized;

    public ViCoSearchPageVM(
        IViCoWorkstationCatalog catalog,
        IViCoWorkstationSearch search,
        Func<CancellationToken, Task<IViCoRelatedPathResolver>> pathResolverFactory,
        INetworkAvailabilityService network,
        IRemoteDesktopService remoteDesktop,
        IRemoteSessionService remoteSessions,
        IExternalPathLauncher launcher,
        IViCoOnlineRefreshService onlineRefresh,
        IViCoWorkstationConfigurationService configurationService,
        ViCoWorkspaceContext workspaceContext,
        Action<IEnumerable<ViCoWorkstation>> synchronizeWorkstations,
        IApplicationLog? log = null)
    {
        _catalog = catalog;
        _search = search;
        _pathResolverFactory = pathResolverFactory;
        _network = network;
        _remoteDesktop = remoteDesktop;
        _remoteSessions = remoteSessions;
        _launcher = launcher;
        _onlineRefresh = onlineRefresh;
        _configurationService = configurationService;
        _workspaceContext = workspaceContext;
        _synchronizeWorkstations = synchronizeWorkstations;
        _log = log ?? NullApplicationLog.Instance;

        RefreshCommand = GetCommandBindingAsync(RefreshFromBestAvailableSourceAsync);
        ConnectRemoteCommand = GetCommandBinding(ConnectRemote);
        ConnectRemoteWithPromptCommand = GetCommandBinding(ConnectRemoteWithPrompt);
        SaveConfigurationCommand = GetCommandBindingAsync(SaveConfigurationAsync);
        CreateConfigurationCommand = GetCommandBindingAsync(CreateConfigurationAsync);
        OpenPcProjectsCommand = GetCommandBinding(() => OpenRelated(ViCoRelatedPathKind.WorkstationProjects));
        OpenSimulationCommand = GetCommandBinding(() => OpenRelated(ViCoRelatedPathKind.Simulation));
        OpenCommissioningCommand = GetCommandBinding(() => OpenRelated(ViCoRelatedPathKind.Commissioning));
        OpenPlanningCommand = GetCommandBinding(() => OpenRelated(ViCoRelatedPathKind.Planning));
    }

    public ObservableCollection<ViCoWorkstationRowVM> Results { get; } = new();
    public ObservableCollection<string> Projects { get; } = new();
    public ObservableCollection<ViCoConfigurationFieldVM> ConfigurationFields { get; } = new();
    public ICommand RefreshCommand { get; }
    public ICommand ConnectRemoteCommand { get; }
    public ICommand ConnectRemoteWithPromptCommand { get; }
    public ICommand SaveConfigurationCommand { get; }
    public ICommand CreateConfigurationCommand { get; }
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
            OnPropertyChanged(nameof(CanUseSelectedWorkstationActions));
            OnPropertyChanged(nameof(IsSelectedWorkstationOffline));
            OnPropertyChanged(nameof(CanEditConfiguration));
            OnPropertyChanged(nameof(CanCreateConfiguration));
            OnPropertyChanged(nameof(HasSelectedConfigurationCard));
            OnPropertyChanged(nameof(IsSelectedConfigurationMissing));
            Projects.Clear();
            ConfigurationFields.Clear();
            if (value is not null)
            {
                foreach (var project in value.Model.Projects)
                    Projects.Add(project);
                foreach (var field in value.Model.WorkstationConfiguration.Fields)
                    ConfigurationFields.Add(new ViCoConfigurationFieldVM(field));
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

    /// <summary>Offline PCs cannot execute RDP or path actions and expose no action buttons.</summary>
    public bool CanUseSelectedWorkstationActions => SelectedWorkstation?.IsOnline == true;

    public bool IsSelectedWorkstationOffline =>
        SelectedWorkstation is not null && !SelectedWorkstation.IsOnline;

    /// <summary>An existing configuration card can be edited; missing standard subtasks are added on save.</summary>
    public bool CanEditConfiguration =>
        _configurationService.IsConfigured &&
        SelectedWorkstation?.Model.WorkstationConfiguration.IsEditable == true;

    public bool CanCreateConfiguration =>
        _configurationService.IsConfigured &&
        SelectedWorkstation is not null &&
        !SelectedWorkstation.Model.HasConfigurationCard &&
        SelectedWorkstation.Model.KanbanizeLaneId > 0 &&
        SelectedWorkstation.Model.ConfigurationColumnId > 0;

    public bool HasSelectedConfigurationCard => SelectedWorkstation?.Model.HasConfigurationCard == true;

    public bool IsSelectedConfigurationMissing =>
        SelectedWorkstation is not null && !SelectedWorkstation.Model.HasConfigurationCard;

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
        using var pingThrottle = new SemaphoreSlim(8);
        using var sessionThrottle = new SemaphoreSlim(4);
        var tasks = rows.Select(async row =>
        {
            try
            {
                bool isOnline;
                if (_availabilityCache.TryGetValue(row.PcName, out var cached) &&
                    DateTimeOffset.Now - cached.CheckedAt < TimeSpan.FromSeconds(30))
                {
                    isOnline = cached.IsOnline;
                }
                else
                {
                    // Do not keep one of the limited ping slots while the
                    // optional, slower RDP-session query is running. This is
                    // significant for desktop users with many workstations.
                    await pingThrottle.WaitAsync(cancellationToken);
                    try
                    {
                        isOnline = await _network.PingAsync(row.PcName, cancellationToken);
                        _availabilityCache[row.PcName] = (isOnline, DateTimeOffset.Now);
                    }
                    finally
                    {
                        pingThrottle.Release();
                    }
                }

                row.SetOnline(isOnline);
                if (isOnline)
                    await RefreshRemoteSessionAsync(row, sessionThrottle, cancellationToken);
                NotifySelectedWorkstationAvailabilityChanged(row);
            }
            catch (OperationCanceledException)
            {
                // A new search superseded this availability scan.
            }
            catch (Exception exception)
            {
                // Availability is a best-effort enhancement. A malformed
                // hostname or transient network failure must not abort the
                // refresh for the other workstations.
                row.SetOnline(false);
                row.SetRemoteSession(ViCoRemoteSessionInfo.NotAvailable);
                NotifySelectedWorkstationAvailabilityChanged(row);
                _log.Warning("Verfügbarkeit", $"Status für {row.PcName} konnte nicht ermittelt werden.", exception.Message);
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

    private async Task RefreshRemoteSessionAsync(
        ViCoWorkstationRowVM row,
        SemaphoreSlim throttle,
        CancellationToken cancellationToken)
    {
        if (_remoteSessionCache.TryGetValue(row.PcName, out var cached) &&
            DateTimeOffset.Now - cached.CheckedAt < TimeSpan.FromMinutes(2))
        {
            row.SetRemoteSession(cached.Info);
            return;
        }

        var acquired = false;
        try
        {
            await throttle.WaitAsync(cancellationToken);
            acquired = true;
            var info = await _remoteSessions.GetSessionInfoAsync(row.PcName, cancellationToken);
            _remoteSessionCache[row.PcName] = (info, DateTimeOffset.Now);
            row.SetRemoteSession(info);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            // The session query is optional. It must never change the actual
            // network availability state or block all remaining PCs.
            row.SetRemoteSession(ViCoRemoteSessionInfo.NotAvailable);
            _log.Warning("Remote-Sitzung", $"Sitzungsstatus für {row.PcName} ist nicht abrufbar.", exception.Message);
        }
        finally
        {
            if (acquired)
                throttle.Release();
        }
    }

    private void NotifySelectedWorkstationAvailabilityChanged(ViCoWorkstationRowVM row)
    {
        if (ReferenceEquals(row, SelectedWorkstation))
        {
            OnPropertyChanged(nameof(CanUseSelectedWorkstationActions));
            OnPropertyChanged(nameof(IsSelectedWorkstationOffline));
        }
    }

    private void ConnectRemote()
    {
        StartRemote(promptForCredentials: false);
    }

    private void ConnectRemoteWithPrompt()
    {
        StartRemote(promptForCredentials: true);
    }

    private void StartRemote(bool promptForCredentials)
    {
        if (SelectedWorkstation is null)
            return;
        if (!CanUseSelectedWorkstationActions)
        {
            StatusText = "Der PC ist offline. Remote- und Pfadaktionen sind ausgeblendet.";
            return;
        }
        if (!promptForCredentials && string.IsNullOrWhiteSpace(SelectedWorkstation.UserName))
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
            if (promptForCredentials)
            {
                _remoteDesktop.ConnectWithCredentialPrompt(
                    SelectedWorkstation.PcName,
                    SelectedWorkstation.UserName,
                    monitors);
                StatusText = "Remote Desktop wird mit Windows-Anmeldedialog gestartet.";
                _log.Information("Remote Desktop", $"Anmeldedialog für {SelectedWorkstation.PcName} gestartet.");
            }
            else
            {
                _remoteDesktop.Connect(SelectedWorkstation.PcName, SelectedWorkstation.UserName, monitors);
                StatusText = $"Remote Desktop wird automatisch als {SelectedWorkstation.UserName} gestartet.";
                _log.Information("Remote Desktop", $"Automatische Verbindung zu {SelectedWorkstation.PcName} als {SelectedWorkstation.UserName} gestartet.");
            }
        }
        catch (Exception exception)
        {
            StatusText = $"Remote Desktop konnte nicht gestartet werden: {exception.Message}";
            _log.Error("Remote Desktop", $"Verbindung zu {SelectedWorkstation.PcName} konnte nicht gestartet werden.", exception);
        }
    }

    private async Task SaveConfigurationAsync()
    {
        if (!CanEditConfiguration || SelectedWorkstation is null)
        {
            StatusText = "Für diesen Arbeitsplatz ist keine bearbeitbare KONFIGURATION-Karte vorhanden.";
            return;
        }

        var changedFields = ConfigurationFields
            .Where(field => field.IsChanged || !field.CanSave)
            .Select(field => field.ToField())
            .ToArray();
        if (changedFields.Length == 0)
        {
            StatusText = "Keine geänderten KONFIGURATION-Werte zum Speichern vorhanden.";
            return;
        }

        try
        {
            var currentConfiguration = SelectedWorkstation.Model.WorkstationConfiguration;
            await _configurationService.SaveFieldsAsync(
                currentConfiguration.CardId,
                changedFields,
                _lifetimeCancellation.Token);

            var configuration = BuildUpdatedConfiguration(currentConfiguration, ConfigurationFields);
            SelectedWorkstation.UpdateConfiguration(configuration);
            _allWorkstations = _allWorkstations
                .Select(workstation => string.Equals(
                    workstation.PcName,
                    SelectedWorkstation.PcName,
                    StringComparison.OrdinalIgnoreCase)
                    ? SelectedWorkstation.Model
                    : workstation)
                .ToArray();
            _synchronizeWorkstations(_allWorkstations);
            foreach (var field in ConfigurationFields)
                field.AcceptSavedValue();
            OnPropertyChanged(nameof(SelectedRemoteUser));
            StatusText = $"{changedFields.Length} KONFIGURATION-Wert(e) wurden in Kanbanize gespeichert.";
            _log.Information("Kanbanize", StatusText);
        }
        catch (OperationCanceledException)
        {
            // Application shutdown cancels only the pending external request.
        }
        catch (Exception exception)
        {
            StatusText = "KONFIGURATION-Werte konnten nicht gespeichert werden.";
            _log.Error("Kanbanize", StatusText, exception);
        }
    }

    private async Task CreateConfigurationAsync()
    {
        if (IsBusy)
            return;
        if (!CanCreateConfiguration || SelectedWorkstation is null)
        {
            StatusText = "KONFIGURATION kann nicht angelegt werden: Lane, Zielspalte oder Kanbanize-Zugriff fehlt.";
            return;
        }

        var pcName = SelectedWorkstation.PcName;
        try
        {
            IsBusy = true;
            StatusText = "Standardisierte KONFIGURATION-Karte wird angelegt …";
            var cardId = await _configurationService.CreateStandardAsync(
                SelectedWorkstation.Model.KanbanizeLaneId,
                SelectedWorkstation.Model.ConfigurationColumnId,
                ConfigurationFields.Select(field => field.ToField()).ToArray(),
                _lifetimeCancellation.Token);
            await _onlineRefresh.RefreshAsync(_lifetimeCancellation.Token);
            IsBusy = false;
            await RefreshCachedDataAsync($"KONFIGURATION-Karte {cardId} wurde angelegt und neu geladen.");
            SelectedWorkstation = Results.FirstOrDefault(row =>
                string.Equals(row.PcName, pcName, StringComparison.OrdinalIgnoreCase));
            _log.Information("Kanbanize", $"KONFIGURATION-Karte {cardId} für {pcName} wurde angelegt.");
        }
        catch (OperationCanceledException)
        {
            // Application shutdown cancels only the pending external request.
        }
        catch (Exception exception)
        {
            StatusText = $"KONFIGURATION-Karte konnte nicht angelegt werden: {exception.Message}";
            _log.Error("Kanbanize", "KONFIGURATION-Karte konnte nicht angelegt werden.", exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static ViCoWorkstationConfiguration BuildUpdatedConfiguration(
        ViCoWorkstationConfiguration current,
        IEnumerable<ViCoConfigurationFieldVM> fields)
    {
        var byKey = fields.ToDictionary(field => field.Key, field => field.ToField(), StringComparer.OrdinalIgnoreCase);
        return new ViCoWorkstationConfiguration(
            current.CardId,
            byKey["USER"],
            byKey["STANDORT"],
            byKey["SW"],
            byKey["PROJEKT-IP"],
            byKey["SONSTIGES"]);
    }

    private void OpenRelated(ViCoRelatedPathKind kind)
    {
        if (SelectedWorkstation is null || _pathResolver is null)
            return;
        if (!CanUseSelectedWorkstationActions)
        {
            StatusText = "Der PC ist offline. Remote- und Pfadaktionen sind ausgeblendet.";
            return;
        }
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
