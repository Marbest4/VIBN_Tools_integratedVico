namespace VIBN_Tools.ContainerGeneration.AI;

public sealed record AiIssue(
    string Container,
    string SignalId,
    string SignalText,
    string RuleSlot,
    string MlSlot,
    float Confidence
);