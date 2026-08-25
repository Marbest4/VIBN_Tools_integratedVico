namespace VIBN_Tools.Core.Kanbanize;

/// <summary>
/// Immutable target selection for the VIBN-to-workplace workflow. Board, lane
/// and column are selected from live Kanbanize data in the UI; no location is
/// silently moved after a card has been created.
/// </summary>
public sealed record VibnWorkplaceSynchronizationSettings(
    int SourceBoardId,
    int TargetBoardId,
    int TargetLaneId,
    int TargetColumnId,
    int Priority,
    bool SynchronizeDeadlines);

/// <summary>Classifies exactly what a preview or synchronization will do.</summary>
public enum VibnWorkplaceSynchronizationAction
{
    Create,
    UpdateDeadline,
    Unchanged,
    Conflict
}

/// <summary>One source card and the single allowed action for its target card.</summary>
public sealed record VibnWorkplaceSynchronizationItem(
    VibnWorkplaceSynchronizationAction Action,
    KanbanizeCardInfo SourceCard,
    KanbanizeCardInfo? TargetCard,
    string Message);

/// <summary>
/// Read-only result of comparing virtual-commissioning cards with generated
/// workplace cards. Previewing never writes to Kanbanize.
/// </summary>
public sealed record VibnWorkplaceSynchronizationPreview(
    IReadOnlyList<VibnWorkplaceSynchronizationItem> Items,
    int ExcludedSourceCardCount)
{
    public int CreateCount => Items.Count(item => item.Action == VibnWorkplaceSynchronizationAction.Create);

    public int DeadlineUpdateCount => Items.Count(item => item.Action == VibnWorkplaceSynchronizationAction.UpdateDeadline);

    public int UnchangedCount => Items.Count(item => item.Action == VibnWorkplaceSynchronizationAction.Unchanged);

    public int ConflictCount => Items.Count(item => item.Action == VibnWorkplaceSynchronizationAction.Conflict);

    public bool HasChanges => CreateCount > 0 || DeadlineUpdateCount > 0;
}

/// <summary>Outcome of an explicit write operation, including isolated per-card failures.</summary>
public sealed record VibnWorkplaceSynchronizationResult(
    VibnWorkplaceSynchronizationPreview Preview,
    int CreatedCount,
    int DeadlineUpdateCount,
    IReadOnlyList<string> Failures);

/// <summary>
/// Coordinates safe, idempotent replication of VIBN commissioning cards into
/// the workplace board. It never deletes, moves, renames or changes existing
/// target cards; only a deadline of an unambiguous generated card may change.
/// </summary>
public interface IVibnWorkplaceSynchronizationService
{
    bool IsConfigured { get; }

    Task<VibnWorkplaceSynchronizationPreview> PreviewAsync(
        VibnWorkplaceSynchronizationSettings settings,
        CancellationToken cancellationToken = default);

    Task<VibnWorkplaceSynchronizationResult> SynchronizeAsync(
        VibnWorkplaceSynchronizationSettings settings,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Pure business policy preserved from the historic Canbanize tool. It keeps
/// the legacy card selection/title marker but replaces fragile string parsing
/// with typed, testable rules.
/// </summary>
public static class VibnWorkplaceSynchronizationPolicy
{
    public const int DefaultSourceBoardId = 1392;
    public const int DefaultTargetBoardId = 1541;
    public const int ExcludedArchiveColumnId = 25236;
    public const string RequiredSourceTitleFragment = "Grundinbetriebnahme";
    public const string ExcludedSourceTitleFragment = "Vorlage";

    /// <summary>Only genuine virtual-commissioning cards from active source columns are synchronized.</summary>
    public static bool IsEligibleSourceCard(KanbanizeCardInfo card) =>
        card.ColumnId != ExcludedArchiveColumnId &&
        card.Title.Contains(RequiredSourceTitleFragment, StringComparison.OrdinalIgnoreCase) &&
        !card.Title.Contains(ExcludedSourceTitleFragment, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Retains the recognizable title convention of the preceding tool without
    /// ever changing an existing target title during later synchronizations.
    /// </summary>
    public static string GetGeneratedTitle(string sourceTitle) =>
        sourceTitle.Replace(
            "[VIBN] Grundinbetriebnahme",
            "*[Gen]*",
            StringComparison.OrdinalIgnoreCase);

    /// <summary>Compares instants with second precision to avoid timezone/JSON round-trip noise.</summary>
    public static bool HasEquivalentDeadline(DateTimeOffset? left, DateTimeOffset? right)
    {
        if (left is null || right is null)
            return left is null && right is null;

        return Math.Abs((left.Value.UtcDateTime - right.Value.UtcDateTime).TotalSeconds) < 1;
    }

    public static string? Validate(VibnWorkplaceSynchronizationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.SourceBoardId <= 0 || settings.TargetBoardId <= 0)
            return "Quell- und Zielboard müssen ausgewählt sein.";
        if (settings.TargetLaneId <= 0 || settings.TargetColumnId <= 0)
            return "Ziel-Lane und Zielspalte müssen ausgewählt sein.";
        if (settings.Priority < KanbanizeCardDraftPolicy.MinimumPriority ||
            settings.Priority > KanbanizeCardDraftPolicy.MaximumPriority)
        {
            return $"Die Priorität muss zwischen {KanbanizeCardDraftPolicy.MinimumPriority} und {KanbanizeCardDraftPolicy.MaximumPriority} liegen.";
        }

        return null;
    }
}

/// <summary>
/// Core orchestrator for the VIBN workplace synchronization. It takes a fresh
/// snapshot for every explicit run, serializes local runs and touches external
/// state through <see cref="IKanbanizeCardService"/> only.
/// </summary>
public sealed class VibnWorkplaceSynchronizationService : IVibnWorkplaceSynchronizationService
{
    private readonly IKanbanizeCardService _cards;
    private readonly SemaphoreSlim _synchronizationGate = new(1, 1);

    public VibnWorkplaceSynchronizationService(IKanbanizeCardService cards)
    {
        _cards = cards ?? throw new ArgumentNullException(nameof(cards));
    }

    public bool IsConfigured => _cards.IsConfigured;

    public Task<VibnWorkplaceSynchronizationPreview> PreviewAsync(
        VibnWorkplaceSynchronizationSettings settings,
        CancellationToken cancellationToken = default) =>
        BuildPreviewAsync(settings, cancellationToken);

    public async Task<VibnWorkplaceSynchronizationResult> SynchronizeAsync(
        VibnWorkplaceSynchronizationSettings settings,
        CancellationToken cancellationToken = default)
    {
        var validationError = VibnWorkplaceSynchronizationPolicy.Validate(settings);
        if (validationError is not null)
            throw new ArgumentException(validationError, nameof(settings));

        await _synchronizationGate.WaitAsync(cancellationToken);
        try
        {
            // A new snapshot immediately before writing prevents repeat clicks
            // from creating a second target card for the same source card.
            var preview = await BuildPreviewAsync(settings, cancellationToken);
            var createdCount = 0;
            var deadlineUpdateCount = 0;
            var failures = new List<string>();

            foreach (var item in preview.Items)
            {
                try
                {
                    switch (item.Action)
                    {
                        case VibnWorkplaceSynchronizationAction.Create:
                            await _cards.CreateGeneratedCardAsync(
                                new KanbanizeGeneratedCardDraft(
                                    item.SourceCard.Id,
                                    settings.TargetLaneId,
                                    settings.TargetColumnId,
                                    VibnWorkplaceSynchronizationPolicy.GetGeneratedTitle(item.SourceCard.Title),
                                    settings.Priority,
                                    item.SourceCard.Deadline),
                                cancellationToken);
                            createdCount++;
                            break;

                        case VibnWorkplaceSynchronizationAction.UpdateDeadline:
                            await _cards.UpdateDeadlineAsync(
                                item.TargetCard!.Id,
                                item.SourceCard.Deadline,
                                cancellationToken);
                            deadlineUpdateCount++;
                            break;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    failures.Add($"{item.SourceCard.Title} (Quellkarte {item.SourceCard.Id}): {exception.Message}");
                }
            }

            return new VibnWorkplaceSynchronizationResult(
                preview,
                createdCount,
                deadlineUpdateCount,
                failures);
        }
        finally
        {
            _synchronizationGate.Release();
        }
    }

    private async Task<VibnWorkplaceSynchronizationPreview> BuildPreviewAsync(
        VibnWorkplaceSynchronizationSettings settings,
        CancellationToken cancellationToken)
    {
        var validationError = VibnWorkplaceSynchronizationPolicy.Validate(settings);
        if (validationError is not null)
            throw new ArgumentException(validationError, nameof(settings));

        var sourceTask = _cards.LoadCardsAsync(settings.SourceBoardId, cancellationToken);
        var targetTask = _cards.LoadCardsAsync(settings.TargetBoardId, cancellationToken);
        await Task.WhenAll(sourceTask, targetTask);
        var sourceCards = await sourceTask;
        var targetCards = await targetTask;

        var eligibleSourceCards = sourceCards
            .Where(VibnWorkplaceSynchronizationPolicy.IsEligibleSourceCard)
            .OrderBy(card => card.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(card => card.Id)
            .ToArray();
        var targetCardsBySourceId = targetCards
            .Where(card => !string.IsNullOrWhiteSpace(card.CustomId))
            .GroupBy(card => card.CustomId!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);
        var items = new List<VibnWorkplaceSynchronizationItem>(eligibleSourceCards.Length);

        foreach (var sourceCard in eligibleSourceCards)
        {
            var sourceId = sourceCard.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (!targetCardsBySourceId.TryGetValue(sourceId, out var matchingTargets))
            {
                items.Add(new VibnWorkplaceSynchronizationItem(
                    VibnWorkplaceSynchronizationAction.Create,
                    sourceCard,
                    null,
                    "Neue verknüpfte Arbeitsplatzkarte erstellen."));
                continue;
            }

            if (matchingTargets.Length != 1)
            {
                items.Add(new VibnWorkplaceSynchronizationItem(
                    VibnWorkplaceSynchronizationAction.Conflict,
                    sourceCard,
                    null,
                    $"{matchingTargets.Length} Zielkarten verwenden dieselbe Quellkarten-ID; keine Änderung durchgeführt."));
                continue;
            }

            var targetCard = matchingTargets[0];
            if (settings.SynchronizeDeadlines &&
                !VibnWorkplaceSynchronizationPolicy.HasEquivalentDeadline(sourceCard.Deadline, targetCard.Deadline))
            {
                items.Add(new VibnWorkplaceSynchronizationItem(
                    VibnWorkplaceSynchronizationAction.UpdateDeadline,
                    sourceCard,
                    targetCard,
                    "Nur die Deadline der vorhandenen Zielkarte an die Quelle anpassen."));
            }
            else
            {
                items.Add(new VibnWorkplaceSynchronizationItem(
                    VibnWorkplaceSynchronizationAction.Unchanged,
                    sourceCard,
                    targetCard,
                    "Bereits verknüpft; keine Änderung erforderlich."));
            }
        }

        return new VibnWorkplaceSynchronizationPreview(
            items,
            sourceCards.Count - eligibleSourceCards.Length);
    }
}
