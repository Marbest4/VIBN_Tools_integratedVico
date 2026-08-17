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
    WorkstationProjects
}

public sealed record ViCoWorkstation(
    string DisplayName,
    string PcName,
    string UserName,
    string TiaInformation,
    string FeeInformation,
    string HardwareInformation,
    IReadOnlyList<string> Projects,
    IReadOnlyList<string> Details);

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

    void Connect(string hostName, string userName, IReadOnlyCollection<int> monitorIndexes);
}

public interface IViCoOnlineRefreshService
{
    bool IsConfigured { get; }

    Task RefreshAsync(CancellationToken cancellationToken = default);
}
