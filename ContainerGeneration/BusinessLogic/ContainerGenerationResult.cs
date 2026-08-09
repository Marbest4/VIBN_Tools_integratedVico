using System.Xml.Linq;
using VIBN_Tools.ContainerGeneration.BusinessLogic.ContainerData;

namespace VIBN_Tools.ContainerGeneration.BusinessLogic;

/// <summary>
/// Complete input for one deterministic generation run.
/// </summary>
public sealed record ContainerGenerationRequest(
    IReadOnlyList<ContainerEntry> Signals,
    XDocument Requirements,
    IReadOnlyList<GroupingRule> GroupingRules,
    SubstitutionRule? SubstitutionRule,
    bool IgnoreCase = true,
    bool UseFilterList = true);

public sealed record ContainerGenerationStatistics(
    int TotalSignals,
    int FilteredSignals,
    int MatchedSignals,
    int UnassignedSignals,
    int GeneratedContainers)
{
    public double MatchRate => TotalSignals - FilteredSignals > 0
        ? (double)MatchedSignals / (TotalSignals - FilteredSignals)
        : 0;
}

/// <summary>
/// Explicit result of a generation run. Entries are copies and do not alias
/// the imported signal list.
/// </summary>
public sealed record ContainerGenerationResult(
    IReadOnlyList<ComponentContainer> Containers,
    IReadOnlyList<ContainerEntry> UnassignedSignals,
    IReadOnlyList<ContainerEntry> FilteredSignals,
    ContainerGenerationStatistics Statistics);
