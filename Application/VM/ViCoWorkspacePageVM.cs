using VIBN_Tools.Core.ViCo;
using VIBN_Tools.GlobalClasses;

namespace VIBN_Tools.Application.VM;

/// <summary>
/// Supplies only workspace-level state. Individual ViCo pages retain their own
/// view models, while this class controls whether administration is visible.
/// </summary>
public sealed class ViCoWorkspacePageVM : MvvmBase
{
    private readonly IViCoLicenseService _licenses;
    private readonly string _currentUser;
    private readonly IApplicationLog _log;
    private bool _initialized;

    public ViCoWorkspacePageVM(IViCoLicenseService licenses, string currentUser, IApplicationLog? log = null)
    {
        _licenses = licenses ?? throw new ArgumentNullException(nameof(licenses));
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

        if (!_licenses.IsConfigured)
        {
            AccessStatus = "Verwaltung ist ausgeblendet: Lizenzspeicher nicht konfiguriert.";
            return;
        }

        try
        {
            var entries = await _licenses.LoadApprovedAsync();
            var persistedLevel = entries.FirstOrDefault(entry =>
                WindowsUserIdentity.Equals(entry.UserName, _currentUser))?.Level;
            var effectiveLevel = LicenseAdministrationPolicy.GetEffectiveLevel(_currentUser, persistedLevel);
            CanViewAdministration = LicenseAdministrationPolicy.ParseLevel(effectiveLevel) >= 7;
            AccessStatus = CanViewAdministration
                ? $"Verwaltung ist mit {effectiveLevel} verfügbar."
                : "Verwaltung ist erst ab Level7 sichtbar.";
            _log.Information("ViCo", AccessStatus);
        }
        catch (Exception exception)
        {
            AccessStatus = "Verwaltung ist ausgeblendet: Lizenzdaten konnten nicht gelesen werden.";
            _log.Error("ViCo", AccessStatus, exception);
        }
    }
}
