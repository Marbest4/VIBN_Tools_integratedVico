namespace VIBN_Tools.Core.ViCo;

public enum ViCoProjectKind
{
    Simulation,
    Commissioning,
    Planning
}

public sealed record ProjectLocation(
    string DisplayName,
    string FullPath,
    ViCoProjectKind Kind = ViCoProjectKind.Simulation);

public sealed record ProjectCatalogSnapshot(
    IReadOnlyList<ProjectLocation> Projects,
    IReadOnlyList<string> Warnings,
    DateTimeOffset LoadedAt);

public interface IProjectCatalogService
{
    Task<ProjectCatalogSnapshot> LoadAsync(CancellationToken cancellationToken = default);
}

public interface IProjectSearchService
{
    IReadOnlyList<ProjectLocation> Search(
        IEnumerable<ProjectLocation> projects,
        string? query,
        int maximumResults = 250);
}

public sealed class ProjectSearchService : IProjectSearchService
{
    public IReadOnlyList<ProjectLocation> Search(
        IEnumerable<ProjectLocation> projects,
        string? query,
        int maximumResults = 250)
    {
        ArgumentNullException.ThrowIfNull(projects);

        if (maximumResults <= 0)
            return Array.Empty<ProjectLocation>();

        var materialized = projects as IReadOnlyList<ProjectLocation> ?? projects.ToArray();
        if (string.IsNullOrWhiteSpace(query))
            return materialized.Take(maximumResults).ToArray();

        var queryParts = query
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Normalize)
            .Where(part => part.Length > 0)
            .ToArray();

        if (queryParts.Length == 0)
            return materialized.Take(maximumResults).ToArray();

        return materialized
            .Select(project => new
            {
                Project = project,
                Display = Normalize(project.DisplayName),
                Path = Normalize(project.FullPath)
            })
            .Where(candidate => queryParts.All(part =>
                candidate.Display.Contains(part, StringComparison.Ordinal) ||
                candidate.Path.Contains(part, StringComparison.Ordinal)))
            .OrderByDescending(candidate =>
                string.Equals(candidate.Display, Normalize(query), StringComparison.Ordinal))
            .ThenBy(candidate => candidate.Project.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Take(maximumResults)
            .Select(candidate => candidate.Project)
            .ToArray();
    }

    private static string Normalize(string value)
    {
        Span<char> buffer = stackalloc char[value.Length];
        var length = 0;

        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character))
                buffer[length++] = char.ToLowerInvariant(character);
        }

        return new string(buffer[..length]);
    }
}
