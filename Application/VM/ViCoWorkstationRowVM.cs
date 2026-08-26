using VIBN_Tools.Core.ViCo;
using VIBN_Tools.GlobalClasses;

namespace VIBN_Tools.Application.VM;

/// <summary>
/// Presentation-only state for one searchable ViCo workstation row. Keeping
/// cell formatting and live availability state out of the search coordinator
/// makes the latter responsible only for loading and actions.
/// </summary>
public sealed class ViCoWorkstationRowVM : MvvmBase
{
    public ViCoWorkstationRowVM(ViCoWorkstation model)
    {
        Model = model;
    }

    public ViCoWorkstation Model { get; private set; }
    public string PcName => Model.PcName;
    public string DisplayName => Model.DisplayName;
    public string UserName => Model.UserName;
    public string Status => Model.Status;

    /// <summary>
    /// Keeps the operational state visually scannable without putting WPF
    /// brushes into the view model. Free workstations are green; planning or
    /// active work makes a workstation occupied and therefore red.
    /// </summary>
    public string StatusBackground => Status switch
    {
        "Frei" => "#FFC6EFCE",
        "Belegt" => "#FFFFC7CE",
        _ => "#FFF3F5F7"
    };

    public string ProjectSummary => Model.ProjectSummary;
    public string AdditionalProjects => Model.AdditionalProjects;
    public string SoftwareInformation => Model.SoftwareInformation;
    public IReadOnlyList<AutomationSoftwareInfo> SoftwareDetails => Model.AutomationSoftware;
    public string FeeInformation => Model.FeeInformation;
    public string HardwareInformation => Model.HardwareInformation;
    public int RobotCount => Model.RobotCount;
    public string RobotSummary => Model.RobotSummary;
    public IReadOnlyList<string> Details => Model.Details;
    public string ConfigurationSoftware => Model.WorkstationConfiguration.Software.Value;
    public string ConfigurationLocation => Model.WorkstationConfiguration.Location.Value;
    public string ConfigurationProjectIp => Model.WorkstationConfiguration.ProjectIp.Value;
    public string ConfigurationOther => Model.WorkstationConfiguration.Other.Value;
    public string ConfigurationStatus => Model.HasConfigurationCard
        ? "Vorhanden"
        : "Konfigurationskarte fehlt!";
    public string ConfigurationStatusBackground => Model.HasConfigurationCard
        ? "#FFC6EFCE"
        : "#FFFFC7CE";

    private bool _isOnline;

    /// <summary>Used by the view to suppress remote/path actions for offline PCs.</summary>
    public bool IsOnline
    {
        get => _isOnline;
        private set
        {
            _isOnline = value;
            OnPropertyChanged();
        }
    }

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
        IsOnline = isOnline;
        OnlineStatus = isOnline ? "Online" : "Offline";
        OnlineStatusBackground = isOnline ? "#FFC6EFCE" : "#FFFFC7CE";
        if (!isOnline)
            SetRemoteSessionOffline();
    }

    private string _remoteSessionStatus = "Wird geprüft …";
    public string RemoteSessionStatus
    {
        get => _remoteSessionStatus;
        private set
        {
            _remoteSessionStatus = value;
            OnPropertyChanged();
        }
    }

    private string _lastRemoteLogon = "Wird geprüft …";
    public string LastRemoteLogon
    {
        get => _lastRemoteLogon;
        private set
        {
            _lastRemoteLogon = value;
            OnPropertyChanged();
        }
    }

    private string _remoteSessionDiagnostic = string.Empty;
    public string RemoteSessionDiagnostic
    {
        get => _remoteSessionDiagnostic;
        private set
        {
            _remoteSessionDiagnostic = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Maps a remote query result to concise, user-facing grid values.</summary>
    public void SetRemoteSession(ViCoRemoteSessionInfo info)
    {
        if (!info.IsAvailable)
        {
            RemoteSessionDiagnostic = info.DiagnosticMessage;
            var permissionFailure = info.DiagnosticMessage.Contains("Berechtigung", StringComparison.OrdinalIgnoreCase) ||
                                    info.DiagnosticMessage.Contains("Access is denied", StringComparison.OrdinalIgnoreCase);
            RemoteSessionStatus = permissionFailure ? "Nicht abrufbar (Rechte)" : "Nicht abrufbar";
            LastRemoteLogon = permissionFailure ? "Nicht abrufbar (Rechte)" : "Nicht abrufbar";
            return;
        }

        RemoteSessionDiagnostic = string.Empty;

        RemoteSessionStatus = string.IsNullOrWhiteSpace(info.ActiveUser)
            ? "Keine aktive Sitzung"
            : $"Aktiv: {info.ActiveUser}";
        LastRemoteLogon = info.LastLogonAt is null
            ? "Keine Anmeldung gefunden"
            : $"{info.LastLogonUser} – {info.LastLogonAt.Value.LocalDateTime:dd.MM.yyyy HH:mm}";
    }

    private void SetRemoteSessionOffline()
    {
        RemoteSessionStatus = "Offline";
        LastRemoteLogon = "—";
        RemoteSessionDiagnostic = "Der PC ist offline.";
    }

    /// <summary>Updates only the editable KONFIGURATION projection after a successful save.</summary>
    public void UpdateConfiguration(ViCoWorkstationConfiguration configuration)
    {
        var userName = string.IsNullOrWhiteSpace(configuration.User.Value)
            ? Model.UserName
            : configuration.User.Value.Trim();
        Model = Model with { UserName = userName, Configuration = configuration };
        OnPropertyChanged(nameof(UserName));
        OnPropertyChanged(nameof(ConfigurationSoftware));
        OnPropertyChanged(nameof(ConfigurationLocation));
        OnPropertyChanged(nameof(ConfigurationProjectIp));
        OnPropertyChanged(nameof(ConfigurationOther));
        OnPropertyChanged(nameof(ConfigurationStatus));
        OnPropertyChanged(nameof(ConfigurationStatusBackground));
    }
}

/// <summary>
/// Editable presentation state for one existing KONFIGURATION subtask. It
/// tracks its original value so the UI never sends unchanged board fields.
/// </summary>
public sealed class ViCoConfigurationFieldVM : MvvmBase
{
    private string _value;
    private string _originalValue;

    public ViCoConfigurationFieldVM(ViCoConfigurationField field)
    {
        Key = field.Key;
        SubtaskId = field.SubtaskId;
        _value = field.Value;
        _originalValue = field.Value;
    }

    public string Key { get; }

    public int SubtaskId { get; }

    public bool CanSave => SubtaskId > 0;

    public string Value
    {
        get => _value;
        set
        {
            if (string.Equals(_value, value, StringComparison.Ordinal))
                return;
            _value = value ?? string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsChanged));
        }
    }

    public bool IsChanged => !string.Equals(_originalValue, Value, StringComparison.Ordinal);

    public ViCoConfigurationField ToField() => new(Key, Value, SubtaskId);

    public void AcceptSavedValue()
    {
        _originalValue = Value;
        OnPropertyChanged(nameof(IsChanged));
    }
}
