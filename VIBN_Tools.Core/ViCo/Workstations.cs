using System.Collections.ObjectModel;

namespace VIBN_Tools.Core.ViCo;

public enum ViCoSearchMode
{
    /// <summary>Searches PC name, user, project and available card details together.</summary>
    All,
    Project,
    Workstation
}

public enum ViCoRelatedPathKind
{
    Simulation,
    Commissioning,
    Planning,
    WorkstationProjects,
    WorkstationProject
}

public enum AutomationPlatform
{
    SiemensTiaPortal,
    BeckhoffTwinCat,
    RockwellStudio5000
}

public enum SoftwareEvidenceState
{
    Specified,
    Installed
}

public sealed record AutomationSoftwareInfo(
    AutomationPlatform Platform,
    string DisplayName,
    string Source,
    SoftwareEvidenceState EvidenceState = SoftwareEvidenceState.Specified)
{
    public string EvidenceLabel => EvidenceState == SoftwareEvidenceState.Installed
        ? "installiert"
        : "laut Kanbanize angegeben";
}

public sealed record ViCoRobotInfo(string Name, string Status, string SourceCard);

/// <summary>
/// One editable field from the KONFIGURATION card. The subtask ID is retained
/// so the UI can update exactly that subtask and no unrelated board data.
/// </summary>
public sealed record ViCoConfigurationField(string Key, string Value, int SubtaskId)
{
    public bool CanSave => SubtaskId > 0;
}

/// <summary>
/// Structured workstation metadata extracted from the KONFIGURATION card and
/// its USER, STANDORT, SW, PROJEKT-IP and SONSTIGES subtasks.
/// </summary>
public sealed record ViCoWorkstationConfiguration(
    int CardId,
    ViCoConfigurationField User,
    ViCoConfigurationField Location,
    ViCoConfigurationField Software,
    ViCoConfigurationField ProjectIp,
    ViCoConfigurationField Other)
{
    public static ViCoWorkstationConfiguration Empty { get; } = new(
        0,
        new ViCoConfigurationField("USER", string.Empty, 0),
        new ViCoConfigurationField("STANDORT", string.Empty, 0),
        new ViCoConfigurationField("SW", string.Empty, 0),
        new ViCoConfigurationField("PROJEKT-IP", string.Empty, 0),
        new ViCoConfigurationField("SONSTIGES", string.Empty, 0));

    public IReadOnlyList<ViCoConfigurationField> Fields => new[]
    {
        User, Location, Software, ProjectIp, Other
    };

    public bool IsEditable => CardId > 0;
}

public sealed record ViCoWorkstation(
    string DisplayName,
    string PcName,
    string UserName,
    string SoftwareInformation,
    string FeeInformation,
    string HardwareInformation,
    IReadOnlyList<string> Projects,
    IReadOnlyList<string> Details,
    IReadOnlyList<AutomationSoftwareInfo>? Software = null,
    IReadOnlyList<ViCoRobotInfo>? Robots = null,
    ViCoWorkstationConfiguration? Configuration = null,
    int KanbanizeLaneId = 0,
    int ConfigurationColumnId = 0)
{
    public IReadOnlyList<AutomationSoftwareInfo> AutomationSoftware { get; } =
        Software ?? Array.Empty<AutomationSoftwareInfo>();

    public IReadOnlyList<ViCoRobotInfo> RobotDetails { get; } =
        Robots ?? Array.Empty<ViCoRobotInfo>();

    public string ProjectSummary => string.Join(" | ", Projects);

    public string AdditionalProjects => string.Empty;

    /// <summary>Raw project states retained for diagnostic/detail displays.</summary>
    public string ProjectStatusSummary => string.Join(", ", Details
        .Select(ProjectIdentity.GetStatus)
        .Where(value => value.Length > 0)
        .Distinct(StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Operational occupancy. Planning or working work takes precedence over
    /// backlog/done cards because those cards do not make a workstation free.
    /// </summary>
    public string Status => ProjectIdentity.GetOccupancyStatus(Details);

    public int RobotCount => RobotDetails.Count > 0
        ? RobotDetails.Count
        : Details.Count(value => value.StartsWith("Robot ", StringComparison.OrdinalIgnoreCase));

    public string RobotSummary => RobotDetails.Count == 0
        ? "Keine Robotik-Karte zugeordnet"
        : string.Join(" | ", RobotDetails.Select(robot => $"{robot.Name}: {robot.Status}"));

    public ViCoWorkstationConfiguration WorkstationConfiguration =>
        Configuration ?? ViCoWorkstationConfiguration.Empty;

    public bool HasConfigurationCard => WorkstationConfiguration.CardId > 0;
}

public sealed record ViCoWorkstationSnapshot(
    IReadOnlyList<ViCoWorkstation> Workstations,
    IReadOnlyList<string> Warnings);

public interface IViCoWorkstationCatalog
{
    Task<ViCoWorkstationSnapshot> LoadAsync(CancellationToken cancellationToken = default);
}

public interface IViCoWorkstationSearch
{
    IReadOnlyList<ViCoWorkstation> Search(
        IEnumerable<ViCoWorkstation> workstations,
        string query,
        ViCoSearchMode mode);
}

public interface IViCoRelatedPathResolver
{
    string? Resolve(ViCoWorkstation workstation, string project, ViCoRelatedPathKind kind);
}

public interface INetworkAvailabilityService
{
    Task<bool> PingAsync(string hostName, CancellationToken cancellationToken = default);
}

public interface IRemoteDesktopService
{
    int MonitorCount { get; }

    /// <summary>Starts RDP with locally saved Windows credentials.</summary>
    void Connect(string hostName, string userName, IReadOnlyCollection<int> monitorIndexes);

    /// <summary>Starts RDP without inserting credentials so Windows shows its sign-in dialog.</summary>
    void ConnectWithCredentialPrompt(string hostName, string userName, IReadOnlyCollection<int> monitorIndexes);
}

/// <summary>Creates and removes the short-lived Windows credential used by automatic RDP.</summary>
public interface IRemoteCredentialStore
{
    void SaveTemporary(string hostName, string userName);

    Task RemoveAfterAsync(
        string hostName,
        TimeSpan delay,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Read-only remote-session data. <see cref="IsAvailable"/> is false when
/// Windows denies the remote query or the session service cannot be reached.
/// </summary>
public sealed record ViCoRemoteSessionInfo(
    bool IsAvailable,
    string ActiveUser,
    string LastLogonUser,
    DateTimeOffset? LastLogonAt,
    string DiagnosticMessage = "")
{
    public static ViCoRemoteSessionInfo NotAvailable { get; } = new(false, string.Empty, string.Empty, null);

    public static ViCoRemoteSessionInfo Unavailable(string reason) =>
        new(false, string.Empty, string.Empty, null, reason);
}

/// <summary>Queries terminal-server/RDP session information without modifying the remote PC.</summary>
public interface IRemoteSessionService
{
    Task<ViCoRemoteSessionInfo> GetSessionInfoAsync(
        string hostName,
        CancellationToken cancellationToken = default);
}

public interface IViCoOnlineRefreshService
{
    bool IsConfigured { get; }

    Task RefreshAsync(CancellationToken cancellationToken = default);
}

/// <summary>Creates or updates the standardized KONFIGURATION card for a workstation lane.</summary>
public interface IViCoWorkstationConfigurationService
{
    bool IsConfigured { get; }

    Task SaveFieldsAsync(
        int configurationCardId,
        IReadOnlyCollection<ViCoConfigurationField> fields,
        CancellationToken cancellationToken = default);

    Task<int> CreateStandardAsync(
        int laneId,
        int columnId,
        IReadOnlyCollection<ViCoConfigurationField> fields,
        CancellationToken cancellationToken = default);
}

public sealed record WorkstationDirectoryEntry(string PcName, string UserName);

public interface IWorkstationDirectory
{
    ObservableCollection<string> PcNames { get; }

    IReadOnlyList<WorkstationDirectoryEntry> Entries { get; }

    DateTimeOffset? LastUpdated { get; }

    Task RefreshAsync(CancellationToken cancellationToken = default);

    void Synchronize(IEnumerable<ViCoWorkstation> workstations);

    string FindUser(string pcName);
}

public sealed class WorkstationDirectory : IWorkstationDirectory
{
    private readonly IViCoWorkstationCatalog _catalog;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private readonly object _sync = new();
    private IReadOnlyList<WorkstationDirectoryEntry> _entries =
        new[] { new WorkstationDirectoryEntry("localhost", string.Empty) };

    public WorkstationDirectory(IViCoWorkstationCatalog catalog)
    {
        _catalog = catalog;
        PcNames.Add("localhost");
    }

    public ObservableCollection<string> PcNames { get; } = new();

    public IReadOnlyList<WorkstationDirectoryEntry> Entries
    {
        get
        {
            lock (_sync)
                return _entries;
        }
    }

    public DateTimeOffset? LastUpdated { get; private set; }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await _refreshGate.WaitAsync(cancellationToken);
        try
        {
            var snapshot = await _catalog.LoadAsync(cancellationToken);
            Synchronize(snapshot.Workstations);
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    public void Synchronize(IEnumerable<ViCoWorkstation> workstations)
    {
        var entries = workstations
            .Where(item => !string.IsNullOrWhiteSpace(item.PcName))
            .GroupBy(item => item.PcName.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new WorkstationDirectoryEntry(
                group.Key.ToUpperInvariant(),
                group.Select(item => item.UserName.Trim())
                    .FirstOrDefault(user => !string.IsNullOrWhiteSpace(user)) ?? string.Empty))
            .OrderBy(item => item.PcName, StringComparer.OrdinalIgnoreCase)
            .Prepend(new WorkstationDirectoryEntry("localhost", string.Empty))
            .ToArray();

        lock (_sync)
            _entries = entries;

        ReplacePcNames(entries.Select(item => item.PcName));
        LastUpdated = DateTimeOffset.Now;
    }

    public string FindUser(string pcName)
    {
        lock (_sync)
        {
            return _entries.FirstOrDefault(entry =>
                string.Equals(entry.PcName, pcName, StringComparison.OrdinalIgnoreCase))?.UserName ?? string.Empty;
        }
    }

    private void ReplacePcNames(IEnumerable<string> names)
    {
        var desired = names.ToArray();
        for (var index = PcNames.Count - 1; index >= 0; index--)
        {
            if (!desired.Contains(PcNames[index], StringComparer.OrdinalIgnoreCase))
                PcNames.RemoveAt(index);
        }

        for (var index = 0; index < desired.Length; index++)
        {
            var currentIndex = PcNames.IndexOf(desired[index]);
            if (currentIndex < 0)
                PcNames.Insert(Math.Min(index, PcNames.Count), desired[index]);
            else if (currentIndex != index)
                PcNames.Move(currentIndex, index);
        }
    }
}

public static class WindowsUserIdentity
{
    public static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        var trimmed = value.Trim().Replace('/', '\\');
        var separator = trimmed.LastIndexOf('\\');
        return (separator >= 0 ? trimmed[(separator + 1)..] : trimmed).ToLowerInvariant();
    }

    public static bool Equals(string left, string right) =>
        string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);
}

/// <summary>Normalizes project identifiers and maps raw Kanbanize markers to scheduling state.</summary>
public static class ProjectIdentity
{
    private static readonly string[] StatusTokens =
    {
        "#Backlog#", "#Planning#", "#Working#", "#Done#",
        "[B]", "[P]", "[W]", "[D]"
    };

    public static string CleanDisplay(string value)
    {
        var result = value ?? string.Empty;
        foreach (var token in StatusTokens)
            result = result.Replace(token, string.Empty, StringComparison.OrdinalIgnoreCase);
        return result.Trim();
    }

    public static string Normalize(string value) =>
        new(CleanDisplay(value)
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());

    public static string MachineKey(string value)
    {
        var matches = System.Text.RegularExpressions.Regex.Matches(
            CleanDisplay(value),
            @"(?:GM|GU)[A-Z0-9]{3,8}",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return matches.Count == 0 ? string.Empty : matches[matches.Count - 1].Value.ToUpperInvariant();
    }

    public static string GetStatus(string value)
    {
        if (value.Contains("#Backlog#", StringComparison.OrdinalIgnoreCase) || value.StartsWith("[B]", StringComparison.OrdinalIgnoreCase))
            return "Backlog";
        if (value.Contains("#Planning#", StringComparison.OrdinalIgnoreCase) || value.StartsWith("[P]", StringComparison.OrdinalIgnoreCase))
            return "Planung";
        if (value.Contains("#Working#", StringComparison.OrdinalIgnoreCase) || value.StartsWith("[W]", StringComparison.OrdinalIgnoreCase))
            return "In Arbeit";
        if (value.Contains("#Done#", StringComparison.OrdinalIgnoreCase) || value.StartsWith("[D]", StringComparison.OrdinalIgnoreCase))
            return "Erledigt";
        return string.Empty;
    }

    /// <summary>
    /// Maps all cards assigned to a workstation to the single status relevant
    /// for scheduling: active planning/work means occupied, otherwise a pure
    /// backlog/done set is free.
    /// </summary>
    public static string GetOccupancyStatus(IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var states = values
            .Select(GetStatus)
            .Where(state => state.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (states.Contains("In Arbeit") || states.Contains("Planung"))
            return "Belegt";
        if (states.Contains("Backlog") || states.Contains("Erledigt"))
            return "Frei";
        return "Unbekannt";
    }
}
