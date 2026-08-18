namespace VIBN_Tools.Core.ViCo;

public sealed class ViCoWorkspaceContext
{
    private readonly object _gate = new();

    public ViCoTransferSelection CurrentSelection { get; private set; } = ViCoTransferSelection.Empty;

    public void Update(
        ViCoWorkstation workstation,
        string project,
        string? simulationPath,
        string? workstationProjectPath)
    {
        lock (_gate)
        {
            CurrentSelection = new ViCoTransferSelection(
                workstation.PcName,
                ProjectIdentity.CleanDisplay(project),
                simulationPath ?? string.Empty,
                workstationProjectPath ?? string.Empty);
        }
    }
}

public sealed record ViCoTransferSelection(
    string PcName,
    string Project,
    string ServerProjectPath,
    string WorkstationProjectPath)
{
    public static ViCoTransferSelection Empty { get; } = new(string.Empty, string.Empty, string.Empty, string.Empty);

    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(PcName) &&
        !string.IsNullOrWhiteSpace(Project) &&
        !string.IsNullOrWhiteSpace(ServerProjectPath) &&
        !string.IsNullOrWhiteSpace(WorkstationProjectPath);
}

public interface IProjectStructureService
{
    void EnsureCreated(string projectDirectory);
}
