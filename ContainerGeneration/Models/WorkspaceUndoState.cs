using VIBN_Tools.ContainerGeneration.BusinessLogic.ContainerData;

namespace VIBN_Tools.ContainerGeneration.Models;

/// <summary>
/// A complete, independent copy of the editable container workspace.
/// Keeping full states makes undo deterministic across moves, deletes,
/// slot edits and automatic removal of empty containers.
/// </summary>
public sealed class WorkspaceUndoState
{
    public string Description { get; }
    public IReadOnlyList<ContainerData> Containers { get; }
    public IReadOnlyList<ContainerEntry> Unassigned { get; }
    public IReadOnlyList<ContainerEntry> Filtered { get; }

    private WorkspaceUndoState(
        string description,
        IReadOnlyList<ContainerData> containers,
        IReadOnlyList<ContainerEntry> unassigned,
        IReadOnlyList<ContainerEntry> filtered)
    {
        Description = description;
        Containers = containers;
        Unassigned = unassigned;
        Filtered = filtered;
    }

    public static WorkspaceUndoState Capture(
        string description,
        IEnumerable<ContainerData> containers,
        IEnumerable<ContainerEntry> unassigned,
        IEnumerable<ContainerEntry> filtered) =>
        new(
            description,
            containers.Select(CloneContainer).ToList(),
            unassigned.Select(entry => entry.Clone()).ToList(),
            filtered.Select(entry => entry.Clone()).ToList());

    private static ContainerData CloneContainer(ContainerData source)
    {
        var clone = new ContainerData
        {
            Id = source.Id,
            Component = source.Component,
            Type = source.Type,
            MinSignals = source.MinSignals,
            MaxSignals = source.MaxSignals,
            ManuallyChecked = source.ManuallyChecked
        };

        clone.Slots.Clear();
        foreach (var slot in source.Slots)
            clone.Slots.Add(slot);

        foreach (var entry in source.DataList)
            clone.DataList.Add(entry.Clone());

        clone.Validate();
        clone.RefreshReimportStatus();
        return clone;
    }
}
