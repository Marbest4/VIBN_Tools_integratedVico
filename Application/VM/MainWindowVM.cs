using System.Security.Principal;
using VIBN_Tools.Core.ViCo;
using VIBN_Tools.GlobalClasses;

namespace VIBN_Tools.Application.VM;

/// <summary>
/// Application-wide startup state.  It loads the dynamic workstation directory
/// and the same central role list that is used by ViCo administration.
/// </summary>
public sealed class MainWindowVM : MvvmBase
{
    private bool _canUseLevel7Features;
    private bool _canUseLevel8Features;
    private string _currentLevel = "Nicht erkannt";

    /// <summary>CAD Wizard, Container Generation and Container2Fee.</summary>
    public bool CanUseLevel7Features
    {
        get => _canUseLevel7Features;
        private set
        {
            _canUseLevel7Features = value;
            OnPropertyChanged();
        }
    }

    /// <summary>AI-Test and the Kanbanize card workspace.</summary>
    public bool CanUseLevel8Features
    {
        get => _canUseLevel8Features;
        private set
        {
            _canUseLevel8Features = value;
            OnPropertyChanged();
        }
    }

    public string CurrentLevel
    {
        get => _currentLevel;
        private set
        {
            _currentLevel = value;
            OnPropertyChanged();
        }
    }

    public async Task InitializeAsync()
    {
        var workstationsTask = InitializeWorkstationsAsync();
        var rolesTask = InitializeRolesAsync();
        await Task.WhenAll(workstationsTask, rolesTask);
    }

    private static async Task InitializeWorkstationsAsync()
    {
        try
        {
            await ViCoFeatureBootstrapper.InitializeWorkstationDirectoryAsync();
            ApplicationLogService.Instance.Information(
                "Arbeitsstationen",
                $"{ViCoFeatureBootstrapper.WorkstationDirectory.Entries.Count - 1} PCs aus dem ViCo-Cache geladen.");
        }
        catch (Exception exception)
        {
            ApplicationLogService.Instance.Error(
                "Arbeitsstationen",
                "Die gemeinsame PC-Liste konnte beim Start nicht geladen werden.",
                exception);
        }
    }

    private async Task InitializeRolesAsync()
    {
        var currentUser = WindowsIdentity.GetCurrent().Name;
        try
        {
            var roles = ViCoFeatureBootstrapper.UserRoleStore.IsConfigured
                ? await ViCoFeatureBootstrapper.UserRoleStore.LoadAsync()
                : ViCoRolePolicy.ApplyMandatoryRoles(Array.Empty<ViCoUserRole>());
            var persistedLevel = roles.FirstOrDefault(role =>
                WindowsUserIdentity.Equals(role.UserName, currentUser))?.Level;
            ApplyRole(ViCoRolePolicy.GetEffectiveLevel(currentUser, persistedLevel));
        }
        catch (Exception exception)
        {
            // The mandatory system administrator remains usable even while a
            // shared drive is temporarily unavailable; every other user gets
            // no privileged navigation until the role store can be read again.
            ApplyRole(ViCoRolePolicy.GetEffectiveLevel(currentUser, null));
            ApplicationLogService.Instance.Error(
                "Rollenverwaltung",
                "Die zentrale Rollenliste konnte beim Start nicht geladen werden.",
                exception);
        }
    }

    private void ApplyRole(string level)
    {
        CurrentLevel = level;
        var numericLevel = ViCoRolePolicy.ParseLevel(level);
        CanUseLevel7Features = numericLevel >= 7;
        CanUseLevel8Features = numericLevel >= 8;
    }
}
