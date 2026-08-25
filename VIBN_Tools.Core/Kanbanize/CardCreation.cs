namespace VIBN_Tools.Core.Kanbanize;

/// <summary>Selectable Kanbanize board without UI- or transport-specific state.</summary>
public sealed record KanbanizeBoardInfo(int Id, string Name, string Description)
{
    public string DisplayName => string.IsNullOrWhiteSpace(Description)
        ? Name
        : $"{Name} ({Description})";
}

/// <summary>A lane in a board workflow where a new card can be created.</summary>
public sealed record KanbanizeLaneInfo(int Id, int WorkflowId, string Name);

/// <summary>A workflow column where a new card can be created.</summary>
public sealed record KanbanizeColumnInfo(int Id, int WorkflowId, string Name);

/// <summary>All selectable positions of one Kanbanize board.</summary>
public sealed record KanbanizeBoardStructure(
    IReadOnlyList<KanbanizeLaneInfo> Lanes,
    IReadOnlyList<KanbanizeColumnInfo> Columns);

/// <summary>Input required to create exactly one Kanbanize card.</summary>
public sealed record KanbanizeCardDraft(
    int BoardId,
    int LaneId,
    int ColumnId,
    string Title,
    string Description,
    int Priority,
    string? CustomId,
    DateTimeOffset? Deadline);

/// <summary>Minimal result returned after Kanbanize has accepted a card.</summary>
public sealed record KanbanizeCreatedCard(int Id, string Title);

/// <summary>
/// Boundary for the card-creation feature. Implementations may use HTTP, while
/// view models remain testable and independent of Kanbanize response formats.
/// </summary>
public interface IKanbanizeCardService
{
    bool IsConfigured { get; }

    Task<IReadOnlyList<KanbanizeBoardInfo>> LoadBoardsAsync(CancellationToken cancellationToken = default);

    Task<KanbanizeBoardStructure> LoadBoardStructureAsync(int boardId, CancellationToken cancellationToken = default);

    Task<KanbanizeCreatedCard> CreateCardAsync(KanbanizeCardDraft draft, CancellationToken cancellationToken = default);
}

/// <summary>
/// Validates a card before an external write is attempted. Keeping this policy in
/// Core makes the same checks usable by the WPF UI and automated tests.
/// </summary>
public static class KanbanizeCardDraftPolicy
{
    public const int MinimumPriority = 1;
    public const int MaximumPriority = 4;

    public static string? Validate(KanbanizeCardDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        if (draft.BoardId <= 0 || draft.LaneId <= 0 || draft.ColumnId <= 0)
            return "Board, Lane und Spalte müssen ausgewählt sein.";
        if (string.IsNullOrWhiteSpace(draft.Title))
            return "Ein Kartentitel ist erforderlich.";
        if (draft.Title.Trim().Length > 255)
            return "Der Kartentitel darf höchstens 255 Zeichen enthalten.";
        if (draft.Priority < MinimumPriority || draft.Priority > MaximumPriority)
            return $"Die Priorität muss zwischen {MinimumPriority} (hoch) und {MaximumPriority} (niedrig) liegen.";

        return null;
    }
}
