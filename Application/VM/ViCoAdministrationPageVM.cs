using System.Collections.ObjectModel;
using System.Windows.Input;
using VIBN_Tools.Core.ViCo;
using VIBN_Tools.GlobalClasses;

namespace VIBN_Tools.Application.VM;

/// <summary>
/// Coordinates the ViCo administration dashboard and the central, license-free
/// user-role list. Viewing the page requires Level8; changing role data is
/// reserved for Level9 administrators.
/// </summary>
public sealed class ViCoAdministrationPageVM : MvvmBase
{
    private readonly IViCoUserRoleStore _roles;
    private readonly IUpcomingMeetingService _meetings;
    private readonly IViCoUpdateService _updates;
    private readonly IExternalPathLauncher _launcher;
    private readonly string _currentUser;
    private readonly IApplicationLog _log;
    private bool _initialized;

    public ViCoAdministrationPageVM(
        IViCoUserRoleStore roles,
        IUpcomingMeetingService meetings,
        IViCoUpdateService updates,
        IExternalPathLauncher launcher,
        string currentUser,
        IApplicationLog? log = null)
    {
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _meetings = meetings ?? throw new ArgumentNullException(nameof(meetings));
        _updates = updates ?? throw new ArgumentNullException(nameof(updates));
        _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
        _currentUser = WindowsUserIdentity.Normalize(currentUser);
        _log = log ?? NullApplicationLog.Instance;

        foreach (var level in Enumerable.Range(0, 10).Select(value => $"Level{value}"))
            RoleLevels.Add(level);

        SelectedLevel = "Level0";
        RefreshCommand = GetCommandBindingAsync(RefreshAsync);
        AddUserCommand = GetCommandBindingAsync(AddUserAsync);
        SaveRoleCommand = GetCommandBindingAsync(SaveSelectedRoleAsync);
        RemoveUserCommand = GetCommandBindingAsync(RemoveSelectedUserAsync);
        OpenUpdateCommand = GetCommandBinding(OpenUpdate);
    }

    public ObservableCollection<UpcomingMeeting> Meetings { get; } = new();

    public ObservableCollection<ViCoUserRole> RoleEntries { get; } = new();

    public ObservableCollection<string> RoleLevels { get; } = new();

    public ICommand RefreshCommand { get; }

    public ICommand AddUserCommand { get; }

    public ICommand SaveRoleCommand { get; }

    public ICommand RemoveUserCommand { get; }

    public ICommand OpenUpdateCommand { get; }

    public bool IsRoleStoreConfigured => _roles.IsConfigured;

    public string CurrentUser => _currentUser;

    private ViCoUserRole? _selectedRole;
    public ViCoUserRole? SelectedRole
    {
        get => _selectedRole;
        set
        {
            _selectedRole = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanEditSelectedRole));
            if (value is not null)
                SelectedLevel = value.Level;
        }
    }

    private string _newUserName = string.Empty;
    public string NewUserName
    {
        get => _newUserName;
        set
        {
            _newUserName = value;
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
            OnPropertyChanged(nameof(CanManageUsers));
            OnPropertyChanged(nameof(CanEditSelectedRole));
        }
    }

    /// <summary>Only Level9 may add, remove or change user roles.</summary>
    public bool CanManageUsers => ViCoRolePolicy.ParseLevel(CurrentLevel) >= 9;

    /// <summary>
    /// The mandatory break-glass account is visible but its Level9 assignment
    /// is a system policy, not an editable user setting.
    /// </summary>
    public bool CanEditSelectedRole =>
        CanManageUsers &&
        SelectedRole is not null &&
        !ViCoRolePolicy.IsMandatoryLevel9User(SelectedRole.UserName);

    public int Level9UserCount => RoleEntries
        .Where(role => string.Equals(role.Level, "Level9", StringComparison.OrdinalIgnoreCase))
        .Select(role => WindowsUserIdentity.Normalize(role.UserName))
        .Where(user => user.Length > 0)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Count();

    public string Level9CoverageText =>
        $"Level9-Benutzer: {Level9UserCount} / mindestens {ViCoRolePolicy.MinimumLevel9Users}";

    private string _roleStatus = "Rollen wurden noch nicht geprüft.";
    public string RoleStatus
    {
        get => _roleStatus;
        private set
        {
            _roleStatus = value;
            OnPropertyChanged();
        }
    }

    private string _statusText = "ViCo-Verwaltung ist bereit.";
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
        StatusText = "Kalender-, Rollen- und Versionsinformationen werden geladen …";
        var meetingsTask = TryLoadMeetingsAsync();
        var updateTask = TryLoadUpdateAsync();
        var rolesTask = TryLoadRolesAsync();
        await Task.WhenAll(meetingsTask, updateTask, rolesTask);

        Replace(Meetings, await meetingsTask);
        LatestUpdate = await updateTask;
        Replace(RoleEntries, await rolesTask);
        OnPropertyChanged(nameof(Level9UserCount));
        OnPropertyChanged(nameof(Level9CoverageText));

        var persistedLevel = RoleEntries.FirstOrDefault(role =>
            WindowsUserIdentity.Equals(role.UserName, _currentUser))?.Level;
        CurrentLevel = ViCoRolePolicy.GetEffectiveLevel(_currentUser, persistedLevel);
        RoleStatus = CurrentLevel == "Nicht erkannt"
            ? $"Für {_currentUser} wurde keine Rollenfreigabe gefunden."
            : ViCoRolePolicy.IsMandatoryLevel9User(_currentUser)
                ? $"{_currentUser} ist gemäß Systemrichtlinie Level9."
                : $"{_currentUser} wurde mit {CurrentLevel} erkannt.";
        StatusText = "ViCo-Verwaltung aktualisiert.";
        _log.Information("Verwaltung", RoleStatus);
    }

    private async Task AddUserAsync()
    {
        if (!CanManageUsers)
        {
            DenyRoleChange();
            return;
        }

        var userName = WindowsUserIdentity.Normalize(NewUserName);
        if (userName.Length == 0 || string.IsNullOrWhiteSpace(SelectedLevel))
        {
            StatusText = "Benutzername und Stufe müssen angegeben werden.";
            return;
        }
        if (RoleEntries.Any(role => WindowsUserIdentity.Equals(role.UserName, userName)))
        {
            StatusText = $"{userName} ist bereits in der Rollenliste vorhanden.";
            return;
        }

        await SaveRolesAsync(
            RoleEntries.Append(new ViCoUserRole(userName, SelectedLevel, "roles.json")),
            $"{userName} wurde zur Rollenliste hinzugefügt.");
        NewUserName = string.Empty;
    }

    private async Task SaveSelectedRoleAsync()
    {
        if (!CanManageUsers)
        {
            DenyRoleChange();
            return;
        }
        if (SelectedRole is null || string.IsNullOrWhiteSpace(SelectedLevel))
        {
            StatusText = "Zuerst einen Benutzer und eine Stufe auswählen.";
            return;
        }
        if (ViCoRolePolicy.IsMandatoryLevel9User(SelectedRole.UserName))
        {
            StatusText = $"{ViCoRolePolicy.MandatoryLevel9User} bleibt gemäß Systemrichtlinie Level9.";
            return;
        }

        var updated = RoleEntries.Select(role => WindowsUserIdentity.Equals(role.UserName, SelectedRole.UserName)
            ? role with { Level = SelectedLevel }
            : role);
        await SaveRolesAsync(updated, $"Die Stufe für {SelectedRole.UserName} wurde gespeichert.");
    }

    private async Task RemoveSelectedUserAsync()
    {
        if (!CanManageUsers)
        {
            DenyRoleChange();
            return;
        }
        if (SelectedRole is null)
        {
            StatusText = "Zuerst einen Benutzer auswählen.";
            return;
        }
        if (ViCoRolePolicy.IsMandatoryLevel9User(SelectedRole.UserName))
        {
            StatusText = $"{ViCoRolePolicy.MandatoryLevel9User} ist ein verpflichtender Level9-Benutzer und kann nicht entfernt werden.";
            return;
        }

        await SaveRolesAsync(
            RoleEntries.Where(role => !WindowsUserIdentity.Equals(role.UserName, SelectedRole.UserName)),
            $"{SelectedRole.UserName} wurde aus der Rollenliste entfernt.");
    }

    private async Task SaveRolesAsync(IEnumerable<ViCoUserRole> roles, string successMessage)
    {
        try
        {
            var proposedRoles = roles.ToArray();
            var plan = ViCoRolePolicy.PlanSave(proposedRoles);
            if (!plan.IsValid)
            {
                StatusText = plan.Message;
                _log.Warning("Verwaltung", StatusText);
                return;
            }

            await _roles.SaveAsync(plan.Roles);
            await RefreshAsync();
            StatusText = successMessage;
            _log.Information("Verwaltung", successMessage);
        }
        catch (Exception exception)
        {
            StatusText = "Die Rollenänderung konnte nicht gespeichert werden.";
            _log.Error("Verwaltung", StatusText, exception);
        }
    }

    private void DenyRoleChange()
    {
        StatusText = "Benutzerverwaltung ist ausschließlich mit Level9 möglich.";
        _log.Warning("Verwaltung", StatusText);
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

    private async Task<ViCoUpdateInfo?> TryLoadUpdateAsync()
    {
        try
        {
            return await _updates.FindLatestAsync();
        }
        catch (Exception exception)
        {
            _log.Error("Verwaltung", "Versionsinformationen konnten nicht geladen werden.", exception);
            return null;
        }
    }

    private async Task<IReadOnlyList<ViCoUserRole>> TryLoadRolesAsync()
    {
        if (!_roles.IsConfigured)
        {
            RoleStatus = "Der zentrale Rollenpfad ist nicht konfiguriert.";
            return ViCoRolePolicy.ApplyMandatoryRoles(Array.Empty<ViCoUserRole>());
        }

        try
        {
            return await _roles.LoadAsync();
        }
        catch (Exception exception)
        {
            RoleStatus = "Die zentrale Rollenliste ist nicht erreichbar.";
            _log.Error("Verwaltung", RoleStatus, exception);
            return ViCoRolePolicy.ApplyMandatoryRoles(Array.Empty<ViCoUserRole>());
        }
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values)
    {
        target.Clear();
        foreach (var value in values)
            target.Add(value);
    }
}
