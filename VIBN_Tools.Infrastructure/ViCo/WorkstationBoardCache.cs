namespace VIBN_Tools.Infrastructure.ViCo;

/// <summary>
/// Durable, typed cache of the workspace board.  It sits alongside the legacy
/// text files so existing installations keep their established cache fallback
/// while newer UI features can use card and subtask identifiers safely.
/// </summary>
internal sealed class WorkstationBoardCache
{
    public int SchemaVersion { get; set; } = 1;

    public List<WorkstationLaneCacheEntry> Lanes { get; set; } = new();

    public List<WorkstationCardCacheEntry> Cards { get; set; } = new();
}

internal sealed class WorkstationLaneCacheEntry
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}

internal sealed class WorkstationCardCacheEntry
{
    public int Id { get; set; }

    public string LaneId { get; set; } = string.Empty;

    public string ColumnId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public List<WorkstationSubtaskCacheEntry> Subtasks { get; set; } = new();
}

internal sealed class WorkstationSubtaskCacheEntry
{
    public int Id { get; set; }

    public string Description { get; set; } = string.Empty;
}
