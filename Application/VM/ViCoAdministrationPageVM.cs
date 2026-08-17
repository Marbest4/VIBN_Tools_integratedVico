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
    private bool _initialized;

    public ViCoAdministrationPageVM(
        IViCoLicenseService licenses,
        IUpcomingMeetingService meetings,
        IViCoUpdateService updates,
        IExternalPathLauncher launcher,
        string currentUser)
    {
        _licenses = licenses;
        _meetings = meetings;
        _updates = updates;
        _launcher = launcher;
        _currentUser = currentUser.ToLowerInvariant();

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

    private ViCoLicenseEntry? _selectedLicense;
    public ViCoLicenseEntry? SelectedLicense
    {
        get => _selectedLicense;
        set
        {
            _selectedLicense = value;
            OnPropertyChanged();
            if (value is not null)
                SelectedLevel = value.Level;
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
        CurrentLevel = LicenseEntries.FirstOrDefault(entry =>
            string.Equals(entry.UserName, _currentUser, StringComparison.OrdinalIgnoreCase))?.Level ?? "Nicht erkannt";
        StatusText = "ViCo-Dashboard aktualisiert.";
    }

    private async Task RequestLicenseAsync()
    {
        if (!_licenses.IsConfigured)
        {
            StatusText = "Lizenzkompatibilität ist nicht konfiguriert.";
            return;
        }
        await _licenses.RequestCurrentUserAsync();
        StatusText = "Lizenzanfrage wurde abgelegt.";
    }

    private async Task SaveLicenseAsync()
    {
        if (!CanManageLicenses || SelectedLicense is null || string.IsNullOrWhiteSpace(SelectedLevel))
            return;
        await _licenses.SetLevelAsync(SelectedLicense.UserName, SelectedLevel);
        await RefreshAsync();
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
        catch
        {
            return Array.Empty<UpcomingMeeting>();
        }
    }

    private async Task<IReadOnlyList<ViCoLicenseEntry>> TryLoadLicensesAsync()
    {
        if (!_licenses.IsConfigured)
            return Array.Empty<ViCoLicenseEntry>();
        try
        {
            var approvedTask = _licenses.LoadApprovedAsync();
            var requestsTask = _licenses.LoadRequestsAsync();
            await Task.WhenAll(approvedTask, requestsTask);
            return (await approvedTask)
                .Concat(await requestsTask)
                .GroupBy(entry => entry.UserName, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.OrderBy(entry => entry.Level == "Requested" ? 1 : 0).First())
                .OrderBy(entry => entry.UserName, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch
        {
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
