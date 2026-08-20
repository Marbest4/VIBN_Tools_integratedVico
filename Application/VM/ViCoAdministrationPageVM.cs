using System.Collections.ObjectModel;
using System.Windows.Input;
using VIBN_Tools.Core.ViCo;
using VIBN_Tools.GlobalClasses;

namespace VIBN_Tools.Application.VM;

public sealed class ViCoAdministrationPageVM : MvvmBase
{
    private readonly IViCoLicenseService _licenses;
    private readonly IUpcomingMeetingService _meetings;
    private readonly IViCoUpdateService _updates;
    private readonly IExternalPathLauncher _launcher;
    private readonly string _currentUser;
    private readonly IApplicationLog _log;
    private bool _initialized;

    public ViCoAdministrationPageVM(
        IViCoLicenseService licenses,
        IUpcomingMeetingService meetings,
        IViCoUpdateService updates,
        IExternalPathLauncher launcher,
        string currentUser,
        IApplicationLog? log = null)
    {
        _licenses = licenses;
        _meetings = meetings;
        _updates = updates;
        _launcher = launcher;
        _currentUser = currentUser.ToLowerInvariant();
        _log = log ?? NullApplicationLog.Instance;

        foreach (var level in Enumerable.Range(0, 10).Select(value => $"Level{value}").Append("denied"))
            LicenseLevels.Add(level);

        RefreshCommand = GetCommandBindingAsync(RefreshAsync);
        RequestLicenseCommand = GetCommandBindingAsync(RequestLicenseAsync);
        SaveLicenseCommand = GetCommandBindingAsync(SaveLicenseAsync);
        OpenUpdateCommand = GetCommandBinding(OpenUpdate);
    }

    public ObservableCollection<UpcomingMeeting> Meetings { get; } = new();

    public ObservableCollection<ViCoLicenseEntry> LicenseEntries { get; } = new();

    public ObservableCollection<string> LicenseLevels { get; } = new();

    public ICommand RefreshCommand { get; }

    public ICommand RequestLicenseCommand { get; }

    public ICommand SaveLicenseCommand { get; }

    public ICommand OpenUpdateCommand { get; }

    public bool IsLicenseConfigured => _licenses.IsConfigured;

    public string CurrentUser => _currentUser;

    private ViCoLicenseEntry? _selectedLicense;
    public ViCoLicenseEntry? SelectedLicense
    {
        get => _selectedLicense;
        set
        {
            _selectedLicense = value;
            OnPropertyChanged();
            AdditionalLevel9License = null;
            if (value is not null)
                SelectedLevel = value.Level;
        }
    }

    private ViCoLicenseEntry? _additionalLevel9License;
    public ViCoLicenseEntry? AdditionalLevel9License
    {
        get => _additionalLevel9License;
        set
        {
            _additionalLevel9License = value;
            OnPropertyChanged();
        }
    }

    private string? _selectedLevel;
    public string? SelectedLevel
    {
        get => _selectedLevel;
        set
        {
            _selectedLevel = value;
            OnPropertyChanged();
        }
    }

    private ViCoUpdateInfo? _latestUpdate;
    public ViCoUpdateInfo? LatestUpdate
    {
        get => _latestUpdate;
        private set
        {
            _latestUpdate = value;
            OnPropertyChanged();
        }
    }

    private string _currentLevel = "Nicht erkannt";
    public string CurrentLevel
    {
        get => _currentLevel;
        private set
        {
            _currentLevel = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanManageLicenses));
        }
    }

    public bool CanManageLicenses => ParseLevel(CurrentLevel) >= 8;

    public int Level9UserCount => LicenseEntries
        .Where(entry => string.Equals(entry.Level, "Level9", StringComparison.OrdinalIgnoreCase))
        .Select(entry => WindowsUserIdentity.Normalize(entry.UserName))
        .Where(user => user.Length > 0)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Count();

    public string Level9CoverageText =>
        $"Level9-Benutzer: {Level9UserCount} / mindestens {LicenseAdministrationPolicy.MinimumLevel9Users}";

    private string _licenseStatus = "Lizenzdaten wurden noch nicht geprüft.";
    public string LicenseStatus
    {
        get => _licenseStatus;
        private set
        {
            _licenseStatus = value;
            OnPropertyChanged();
        }
    }

    private string _statusText = "ViCo-Dashboard ist bereit.";
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
        StatusText = "Kalender, Lizenz- und Versionsinformationen werden geladen …";
        var meetingTask = TryLoadMeetingsAsync();
        var updateTask = _updates.FindLatestAsync();
        var licenseTask = TryLoadLicensesAsync();
        await Task.WhenAll(meetingTask, updateTask, licenseTask);

        Replace(Meetings, await meetingTask);
        LatestUpdate = await updateTask;
        Replace(LicenseEntries, await licenseTask);
        OnPropertyChanged(nameof(Level9UserCount));
        OnPropertyChanged(nameof(Level9CoverageText));
        CurrentLevel = LicenseEntries.FirstOrDefault(entry =>
            WindowsUserIdentity.Equals(entry.UserName, _currentUser))?.Level ?? "Nicht erkannt";
        LicenseStatus = CurrentLevel == "Nicht erkannt"
            ? $"Für {WindowsUserIdentity.Normalize(_currentUser)} wurde keine Freigabe gefunden."
            : $"{WindowsUserIdentity.Normalize(_currentUser)} wurde mit {CurrentLevel} erkannt.";
        StatusText = "ViCo-Dashboard aktualisiert.";
        _log.Information("Verwaltung", LicenseStatus);
    }

    private async Task RequestLicenseAsync()
    {
        if (!_licenses.IsConfigured)
        {
            StatusText = "Lizenzkompatibilität ist nicht konfiguriert.";
            _log.Warning("Verwaltung", StatusText);
            return;
        }
        try
        {
            await _licenses.RequestCurrentUserAsync();
            StatusText = "Lizenzanfrage wurde abgelegt.";
            _log.Information("Verwaltung", StatusText);
        }
        catch (Exception exception)
        {
            StatusText = "Lizenzanfrage konnte nicht gespeichert werden.";
            _log.Error("Verwaltung", StatusText, exception);
        }
    }

    private async Task SaveLicenseAsync()
    {
        if (!CanManageLicenses || SelectedLicense is null || string.IsNullOrWhiteSpace(SelectedLevel))
            return;

        var plan = LicenseAdministrationPolicy.PlanChange(
            LicenseEntries,
            SelectedLicense.UserName,
            SelectedLevel,
            AdditionalLevel9License?.UserName);
        if (!plan.IsValid)
        {
            StatusText = plan.Message;
            _log.Warning("Verwaltung", StatusText);
            return;
        }

        try
        {
            // Promotions are persisted before a possible downgrade. Even a
            // partial write therefore cannot intentionally leave only one Level9 user.
            foreach (var change in plan.Changes)
                await _licenses.SetLevelAsync(change.UserName, change.Level);

            _log.Information(
                "Verwaltung",
                $"{string.Join(", ", plan.Changes.Select(change => $"{change.UserName}={change.Level}"))}. {plan.Message}");
            await RefreshAsync();
            StatusText = plan.Message;
        }
        catch (Exception exception)
        {
            StatusText = "Das Lizenzlevel konnte nicht gespeichert werden.";
            _log.Error("Verwaltung", StatusText, exception);
        }
    }

    private void OpenUpdate()
    {
        if (LatestUpdate is not null)
            _launcher.Open(LatestUpdate.SourceDirectory);
    }

    private async Task<IReadOnlyList<UpcomingMeeting>> TryLoadMeetingsAsync()
    {
        try
        {
            return await _meetings.LoadTodayAsync();
        }
        catch (Exception exception)
        {
            _log.Error("Verwaltung", "Outlook-Termine konnten nicht geladen werden.", exception);
            return Array.Empty<UpcomingMeeting>();
        }
    }

    private async Task<IReadOnlyList<ViCoLicenseEntry>> TryLoadLicensesAsync()
    {
        if (!_licenses.IsConfigured)
        {
            LicenseStatus = "Der ViCo-Kompatibilitätsschlüssel ist nicht konfiguriert.";
            return Array.Empty<ViCoLicenseEntry>();
        }
        try
        {
            var approvedTask = _licenses.LoadApprovedAsync();
            var requestsTask = _licenses.LoadRequestsAsync();
            await Task.WhenAll(approvedTask, requestsTask);
            return (await approvedTask)
                .Concat(await requestsTask)
                .GroupBy(entry => WindowsUserIdentity.Normalize(entry.UserName), StringComparer.OrdinalIgnoreCase)
                .Select(group => group.OrderBy(entry => entry.Level == "Requested" ? 1 : 0).First())
                .OrderBy(entry => entry.UserName, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (Exception exception)
        {
            LicenseStatus = "Lizenzdateien oder Netzwerkpfad sind nicht erreichbar.";
            _log.Error("Verwaltung", LicenseStatus, exception);
            return Array.Empty<ViCoLicenseEntry>();
        }
    }

    private static int ParseLevel(string value) =>
        value.StartsWith("Level", StringComparison.OrdinalIgnoreCase) &&
        int.TryParse(value[5..], out var level)
            ? level
            : -1;

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values)
    {
        target.Clear();
        foreach (var value in values)
            target.Add(value);
    }
}
