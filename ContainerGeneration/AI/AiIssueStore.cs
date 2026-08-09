namespace VIBN_Tools.ContainerGeneration.AI;

public sealed class AiIssueStore
{
    private readonly List<AiIssue> _issues = new();
    public IReadOnlyList<AiIssue> Issues => _issues;
    public void Clear() => _issues.Clear();
    public void Add(AiIssue issue) => _issues.Add(issue);
}