using VIBN_Tools.ContainerGeneration.BusinessLogic.ContainerData;

namespace VIBN_Tools.ContainerGeneration.Models;

public static class ContainerWorkspaceSearch
{
    public static bool Matches(ContainerData? container, string? searchText)
    {
        if (container is null)
            return false;

        var search = searchText?.Trim();
        if (string.IsNullOrEmpty(search))
            return true;

        return Contains(container.Component, search) ||
               Contains(container.Type, search) ||
               container.DataList.Any(entry => Matches(entry, search));
    }

    public static bool Matches(ContainerEntry? entry, string? searchText)
    {
        if (entry is null)
            return false;

        var search = searchText?.Trim();
        if (string.IsNullOrEmpty(search))
            return true;

        return Contains(entry.SignalId, search) ||
               Contains(entry.ID, search) ||
               Contains(entry.Signal, search) ||
               Contains(entry.Slot, search) ||
               Contains(entry.Address, search) ||
               Contains(entry.DataType, search) ||
               Contains(entry.Note, search);
    }

    private static bool Contains(string? value, string search) =>
        !string.IsNullOrEmpty(value) &&
        value.Contains(search, StringComparison.OrdinalIgnoreCase);
}
