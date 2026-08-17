using VIBN_Tools.Core.ViCo;

namespace VIBN_Tools.Infrastructure.ViCo;

public sealed class FileSystemProjectCatalogService : IProjectCatalogService
{
    private readonly ViCoPathsOptions _options;

    public FileSystemProjectCatalogService(ViCoPathsOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public Task<ProjectCatalogSnapshot> LoadAsync(CancellationToken cancellationToken = default) =>
        Task.Run(() => Load(cancellationToken), cancellationToken);

    private ProjectCatalogSnapshot Load(CancellationToken cancellationToken)
    {
        var projects = new List<ProjectLocation>();
        var warnings = new List<string>();

        if (!Directory.Exists(_options.SimulationProjectsRoot))
        {
            warnings.Add($"Projektstamm ist nicht erreichbar: {_options.SimulationProjectsRoot}");
            return new ProjectCatalogSnapshot(projects, warnings, DateTimeOffset.Now);
        }

        foreach (var areaDirectory in EnumerateDirectoriesSafe(_options.SimulationProjectsRoot, warnings))
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var gmDirectory in EnumerateDirectoriesSafe(areaDirectory, warnings))
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var projectDirectory in EnumerateDirectoriesSafe(gmDirectory, warnings))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    projects.Add(new ProjectLocation(
                        CreateDisplayName(gmDirectory, projectDirectory),
                        projectDirectory));
                }
            }
        }

        var ordered = projects
            .GroupBy(project => project.FullPath, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(project => project.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new ProjectCatalogSnapshot(ordered, warnings, DateTimeOffset.Now);
    }

    private static IEnumerable<string> EnumerateDirectoriesSafe(
        string path,
        ICollection<string> warnings)
    {
        try
        {
            return Directory.EnumerateDirectories(path).ToArray();
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or DirectoryNotFoundException)
        {
            warnings.Add($"Verzeichnis konnte nicht gelesen werden: {path} ({exception.Message})");
            return Array.Empty<string>();
        }
    }

    private static string CreateDisplayName(string gmDirectory, string projectDirectory)
    {
        var gmFolderName = Path.GetFileName(gmDirectory);
        var segments = gmFolderName.Split('_', StringSplitOptions.RemoveEmptyEntries);
        var gmNumber = segments.Length > 0 ? segments[0] : gmFolderName;

        if (gmNumber.Length < 3 && segments.Length > 1)
            gmNumber = $"{gmNumber}_{segments[1]}";

        return $"{gmNumber}/{Path.GetFileName(projectDirectory).Replace('/', '-')}";
    }
}
