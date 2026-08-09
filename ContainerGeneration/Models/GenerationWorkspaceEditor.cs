using VIBN_Tools.ContainerGeneration.BusinessLogic.ContainerData;

namespace VIBN_Tools.ContainerGeneration.Models;

public sealed record UnassignEntryResult(
    bool WasAssigned,
    int RemovedDuplicateOccurrences,
    int RemovedEmptyContainers);

/// <summary>
/// Centralized, identity-based operations for the editable generation
/// workspace. An imported signal may exist in exactly one location.
/// </summary>
public static class GenerationWorkspaceEditor
{
    public static UnassignEntryResult MoveToUnassigned(
        ContainerEntry entry,
        string? restoredSignal,
        IList<ContainerData> containers,
        IList<ContainerEntry> unassigned,
        IList<ContainerEntry> filtered)
    {
        if (!string.IsNullOrWhiteSpace(restoredSignal))
            entry.Signal = restoredSignal;

        return MoveToOpenList(
            entry,
            containers,
            unassigned,
            filtered,
            unassigned);
    }

    public static UnassignEntryResult MoveToFiltered(
        ContainerEntry entry,
        IList<ContainerData> containers,
        IList<ContainerEntry> unassigned,
        IList<ContainerEntry> filtered) =>
        MoveToOpenList(
            entry,
            containers,
            unassigned,
            filtered,
            filtered);

    public static void MoveToContainer(
        ContainerEntry entry,
        ContainerData target,
        IList<ContainerData> containers,
        IList<ContainerEntry> unassigned,
        IList<ContainerEntry> filtered)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(target);

        foreach (var container in containers.ToList())
        {
            var matches = container.DataList
                .Where(candidate => SameSource(candidate, entry))
                .ToList();
            foreach (var match in matches)
                container.DataList.Remove(match);
        }

        RemoveAllMatches(unassigned, entry);
        RemoveAllMatches(filtered, entry);

        if (!containers.Contains(target))
            containers.Add(target);
        target.DataList.Add(entry);

        RemoveEmptyContainers(containers, target);
    }

    private static UnassignEntryResult MoveToOpenList(
        ContainerEntry entry,
        IList<ContainerData> containers,
        IList<ContainerEntry> unassigned,
        IList<ContainerEntry> filtered,
        IList<ContainerEntry> target)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(containers);
        ArgumentNullException.ThrowIfNull(unassigned);
        ArgumentNullException.ThrowIfNull(filtered);

        var wasAssigned = false;
        var removedOccurrences = 0;

        foreach (var container in containers.ToList())
        {
            var matches = container.DataList
                .Where(candidate => SameSource(candidate, entry))
                .ToList();

            if (matches.Count == 0)
                continue;

            wasAssigned = true;
            foreach (var match in matches)
            {
                container.DataList.Remove(match);
                removedOccurrences++;
            }

            container.Validate();
            container.RefreshReimportStatus();
        }

        var openListOccurrences =
            unassigned.Count(candidate => SameSource(candidate, entry)) +
            filtered.Count(candidate => SameSource(candidate, entry));
        RemoveAllMatches(unassigned, entry);
        RemoveAllMatches(filtered, entry);
        target.Add(entry);

        entry.Slot = string.Empty;

        var removedEmptyContainers = RemoveEmptyContainers(containers);

        return new UnassignEntryResult(
            wasAssigned,
            Math.Max(0, removedOccurrences - 1) + openListOccurrences,
            removedEmptyContainers);
    }

    private static int RemoveEmptyContainers(
        IList<ContainerData> containers,
        ContainerData? except = null)
    {
        var removed = 0;
        for (var index = containers.Count - 1; index >= 0; index--)
        {
            if (ReferenceEquals(containers[index], except) ||
                containers[index].DataList.Count != 0)
            {
                continue;
            }

            containers.RemoveAt(index);
            removed++;
        }

        return removed;
    }

    public static bool SameSource(ContainerEntry left, ContainerEntry right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (ReferenceEquals(left, right))
            return true;

        if (!string.IsNullOrWhiteSpace(left.SignalId) &&
            !string.IsNullOrWhiteSpace(right.SignalId))
        {
            return string.Equals(
                left.SignalId,
                right.SignalId,
                StringComparison.Ordinal);
        }

        return string.Equals(
            GenerationWorkspaceReconciler.CreatePrimaryKey(left),
            GenerationWorkspaceReconciler.CreatePrimaryKey(right),
            StringComparison.Ordinal);
    }

    public static void RemoveAllMatches(
        IList<ContainerEntry> entries,
        ContainerEntry reference)
    {
        for (var index = entries.Count - 1; index >= 0; index--)
        {
            if (SameSource(entries[index], reference))
                entries.RemoveAt(index);
        }
    }
}
