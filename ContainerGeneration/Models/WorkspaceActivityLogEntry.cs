namespace VIBN_Tools.ContainerGeneration.Models;

/// <summary>
/// A user-facing audit entry for one fachliche workspace operation.
/// Entries are stored with saved workspaces, but are not exported into
/// the production container XML.
/// </summary>
public sealed class WorkspaceActivityLogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Category { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;

    public string TimeText => Timestamp.ToString("dd.MM.yyyy HH:mm:ss");
}
