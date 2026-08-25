using VIBN_Tools.Core.ViCo;
using VIBN_Tools.GlobalClasses;

namespace VIBN_Tools.Application.VM;

/// <summary>
/// Supplies only workspace-level access state. Individual ViCo pages retain
/// their own view models; this class determines whether the Level8+ management
/// page may be shown.
/// </summary>
public sealed class ViCoWorkspacePageVM : MvvmBase
{
    private readonly IViCoUserRoleStore _roles;
    private readonly string _currentUser;
    private readonly IApplicationLog _log;
    private bool _initialized;

    public ViCoWorkspacePageVM(IViCoUserRoleStore roles, string currentUser, IApplicationLog? log = null)
    {
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _currentUser = currentUser ?? string.Empty;
        _log = log ?? NullApplicationLog.Instance;
    }

    private bool _canViewAdministration;
    public bool CanViewAdministration
    {
        get => _canViewAdministration;
        private set
        {
            _canViewAdministration = value;
            OnPropertyChanged();
        }
    }

    private string _accessStatus = "Verwaltungsberechtigung wird geprüft …";
    public string AccessStatus
    {
        get => _accessStatus;
        private set
        {
            _accessStatus = value;
            OnPropertyChanged();
        }
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;
        _initialized = true;

        try
        {
            var roles = _roles.IsConfigured
                ? await _roles.LoadAsync()
                : ViCoRolePolicy.ApplyMandatoryRoles(Array.Empty<ViCoUserRole>());
            var persistedLevel = roles.FirstOrDefault(role =>
                WindowsUserIdentity.Equals(role.UserName, _currentUser))?.Level;
            var effectiveLevel = ViCoRolePolicy.GetEffectiveLevel(_currentUser, persistedLevel);
            CanViewAdministration = ViCoRolePolicy.ParseLevel(effectiveLevel) >= 8;
            AccessStatus = CanViewAdministration
                ? $"Verwaltung ist mit {effectiveLevel} verfügbar."
                : "Verwaltung ist erst ab Level8 sichtbar.";
            _log.Information("ViCo", AccessStatus);
        }
        catch (Exception exception)
        {
            var fallbackLevel = ViCoRolePolicy.GetEffectiveLevel(_currentUser, null);
            CanViewAdministration = ViCoRolePolicy.ParseLevel(fallbackLevel) >= 8;
            AccessStatus = CanViewAdministration
                ? "Verwaltung ist mit der Systemrolle verfügbar; die zentrale Rollenliste ist nicht erreichbar."
                : "Verwaltung ist ausgeblendet: Die zentrale Rollenliste konnte nicht gelesen werden.";
            _log.Error("ViCo", AccessStatus, exception);
        }
    }
}
