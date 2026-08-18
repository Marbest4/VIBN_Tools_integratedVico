namespace VIBN_Tools.Core.ViCo;

public enum ViCoSearchMode
{
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

public sealed record ViCoWorkstation(
    string DisplayName,
    string PcName,
    string UserName,
    string TiaInformation,
    string FeeInformation,
    string HardwareInformation,
    IReadOnlyList<string> Projects,
    IReadOnlyList<string> Details)
{
    public string ProjectSummary => string.Join(" | ", Projects.Take(3));

    public string AdditionalProjects => Projects.Count > 3 ? $"+{Projects.Count - 3}" : string.Empty;

    public string Status => string.Join(", ", Details
        .Select(ProjectIdentity.GetStatus)
        .Where(value => value.Length > 0)
        .Distinct(StringComparer.OrdinalIgnoreCase));

    public int RobotCount => Details.Count(value =>
        value.StartsWith("Robot ", StringComparison.OrdinalIgnoreCase));
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

public interface IRemoteCredentialStore
{
    void Save(string hostName, string userName);

    void RemoveLater(string hostName, TimeSpan delay);
}

public interface INetworkAvailabilityService
{
    Task<bool> PingAsync(string hostName, CancellationToken cancellationToken = default);
}

public interface IRemoteDesktopService
{
    int MonitorCount { get; }

    void Connect(string hostName, string userName, IReadOnlyCollection<int> monitorIndexes);
}

public interface IViCoOnlineRefreshService
{
    bool IsConfigured { get; }

    Task RefreshAsync(CancellationToken cancellationToken = default);
}

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
}
