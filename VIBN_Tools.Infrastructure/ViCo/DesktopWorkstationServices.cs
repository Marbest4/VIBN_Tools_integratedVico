using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using VIBN_Tools.Core.ViCo;

namespace VIBN_Tools.Infrastructure.ViCo;

public sealed class NetworkAvailabilityService : INetworkAvailabilityService
{
    public async Task<bool> PingAsync(string hostName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(hostName))
            return false;

        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(hostName, TimeSpan.FromSeconds(1), cancellationToken: cancellationToken);
            return reply.Status == IPStatus.Success;
        }
        catch (PingException)
        {
            return false;
        }
    }
}

public sealed class WindowsRemoteDesktopService : IRemoteDesktopService
{
    private const int MonitorMetric = 80;
    private readonly string _rdpFile;
    private readonly IRemoteCredentialStore? _credentials;

    public WindowsRemoteDesktopService(string workingDirectory, IRemoteCredentialStore? credentials = null)
    {
        Directory.CreateDirectory(workingDirectory);
        _rdpFile = Path.Combine(workingDirectory, "ViCo.rdp");
        _credentials = credentials;
    }

    public int MonitorCount => Math.Max(1, GetSystemMetrics(MonitorMetric));

    public void Connect(string hostName, string userName, IReadOnlyCollection<int> monitorIndexes)
    {
        var lines = RemoteDesktopProfileBuilder.Build(hostName, userName, monitorIndexes, MonitorCount);
        File.WriteAllLines(_rdpFile, lines, Encoding.Unicode);
        _credentials?.Save(hostName, userName);
        try
        {
            Process.Start(new ProcessStartInfo("mstsc.exe", $"\"{_rdpFile}\"") { UseShellExecute = true });
        }
        finally
        {
            _credentials?.RemoveLater(hostName, TimeSpan.FromSeconds(10));
        }
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);
}

public sealed class ViCoRelatedPathResolver : IViCoRelatedPathResolver
{
    private readonly string _simulationRoot;
    private readonly IReadOnlyDictionary<string, string> _projects;
    private readonly IReadOnlyDictionary<string, string> _commissioning;
    private readonly IReadOnlyDictionary<string, string> _planning;

    public ViCoRelatedPathResolver(
        string simulationRoot,
        IReadOnlyDictionary<string, string> projects,
        IReadOnlyDictionary<string, string> commissioning,
        IReadOnlyDictionary<string, string> planning)
    {
        _simulationRoot = simulationRoot;
        _projects = projects;
        _commissioning = commissioning;
        _planning = planning;
    }

    public string? Resolve(ViCoWorkstation workstation, string project, ViCoRelatedPathKind kind)
    {
        if (kind == ViCoRelatedPathKind.WorkstationProjects)
        {
            var preferred = $@"\\{workstation.PcName}\_Projekte$";
            if (Directory.Exists(preferred))
                return preferred;
            var driveD = $@"\\{workstation.PcName}\D$";
            return Directory.Exists(driveD) ? driveD : $@"\\{workstation.PcName}\C$";
        }

        var simulationPath = FindProject(_projects, project);
        if (kind == ViCoRelatedPathKind.WorkstationProject)
        {
            if (string.IsNullOrWhiteSpace(simulationPath))
                return null;

            var relativePath = Path.GetRelativePath(_simulationRoot, simulationPath);
            return Path.Combine($@"\\{workstation.PcName}\_Projekte$", relativePath);
        }

        var key = ProjectIdentity.MachineKey(project);
        return kind switch
        {
            ViCoRelatedPathKind.Simulation => simulationPath,
            ViCoRelatedPathKind.Commissioning => FindByMachine(_commissioning, key, false),
            ViCoRelatedPathKind.Planning => FindByMachine(_planning, key, true),
            _ => null
        };
    }

    public static async Task<ViCoRelatedPathResolver> CreateAsync(
        string simulationRoot,
        string cacheRoot,
        CancellationToken cancellationToken = default,
        string? commissioningRoot = null,
        string? planningRoot = null)
    {
        var projectService = new FileSystemProjectCatalogService(new ViCoPathsOptions(simulationRoot, string.Empty));
        var catalog = await projectService.LoadAsync(cancellationToken);
        var projects = catalog.Projects.ToDictionary(
            item => item.DisplayName,
            item => item.FullPath,
            StringComparer.OrdinalIgnoreCase);
        var commissioningTask = LoadPairsOrScanAsync(
            cacheRoot,
            "ComissioningFoldersName.txt",
            "ComissioningFoldersPath.txt",
            commissioningRoot,
            3,
            false,
            cancellationToken);
        var planningTask = LoadPairsOrScanAsync(
            cacheRoot,
            "PlanningFoldersName.txt",
            "PlanningFoldersPath.txt",
            planningRoot,
            2,
            true,
            cancellationToken);
        await Task.WhenAll(commissioningTask, planningTask);
        var commissioning = await commissioningTask;
        var planning = await planningTask;
        return new ViCoRelatedPathResolver(simulationRoot, projects, commissioning, planning);
    }

    private static async Task<IReadOnlyDictionary<string, string>> LoadPairsOrScanAsync(
        string cacheRoot,
        string namesFile,
        string pathsFile,
        string? liveRoot,
        int depth,
        bool skipUnderscoreDirectories,
        CancellationToken cancellationToken)
    {
        var cached = await LoadPairsAsync(cacheRoot, namesFile, pathsFile, cancellationToken);
        var cachePath = Path.Combine(cacheRoot, pathsFile);
        var cacheIsCurrent = File.Exists(cachePath) && File.GetLastWriteTime(cachePath).Date == DateTime.Today;
        if (cacheIsCurrent || string.IsNullOrWhiteSpace(liveRoot) || !Directory.Exists(liveRoot))
            return cached;

        try
        {
            var scanned = await Task.Run(
                () => ScanDirectories(liveRoot, depth, skipUnderscoreDirectories, cancellationToken),
                cancellationToken);
            return scanned.Count > 0 ? scanned : cached;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or DirectoryNotFoundException)
        {
            return cached;
        }
    }

    private static IReadOnlyDictionary<string, string> ScanDirectories(
        string root,
        int depth,
        bool skipUnderscoreDirectories,
        CancellationToken cancellationToken)
    {
        IEnumerable<string> current = new[] { root };
        for (var level = 0; level < depth; level++)
        {
            var next = new List<string>();
            foreach (var parent in current)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    next.AddRange(Directory.EnumerateDirectories(parent).Where(path =>
                        !skipUnderscoreDirectories || level != 0 ||
                        !Path.GetFileName(path).StartsWith('_')));
                }
                catch (Exception exception) when (
                    exception is IOException or UnauthorizedAccessException or DirectoryNotFoundException)
                {
                    // Other branches remain usable when one customer folder is inaccessible.
                }
            }
            current = next;
        }

        return current
            .GroupBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<IReadOnlyDictionary<string, string>> LoadPairsAsync(
        string root,
        string namesFile,
        string pathsFile,
        CancellationToken cancellationToken)
    {
        var namesPath = Path.Combine(root, namesFile);
        var pathsPath = Path.Combine(root, pathsFile);
        if (!File.Exists(namesPath) || !File.Exists(pathsPath))
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var names = await File.ReadAllLinesAsync(namesPath, cancellationToken);
        var paths = await File.ReadAllLinesAsync(pathsPath, cancellationToken);
        return Enumerable.Range(0, Math.Min(names.Length, paths.Length))
            .GroupBy(index => names[index], StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => paths[group.First()], StringComparer.OrdinalIgnoreCase);
    }

    private static string? FindProject(IReadOnlyDictionary<string, string> values, string project)
    {
        if (string.IsNullOrWhiteSpace(project))
            return null;

        var normalized = ProjectIdentity.Normalize(project);
        var machineKey = ProjectIdentity.MachineKey(project);
        return values
            .Select(pair => new
            {
                pair.Value,
                Normalized = ProjectIdentity.Normalize(pair.Key),
                MachineKey = ProjectIdentity.MachineKey(pair.Key)
            })
            .Where(candidate =>
                normalized.Contains(candidate.Normalized, StringComparison.OrdinalIgnoreCase) ||
                candidate.Normalized.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                (machineKey.Length > 0 &&
                 string.Equals(candidate.MachineKey, machineKey, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(candidate =>
                normalized.Contains(candidate.Normalized, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(candidate => candidate.Normalized.Length)
            .Select(candidate => candidate.Value)
            .FirstOrDefault();
    }

    private static string? FindByMachine(
        IReadOnlyDictionary<string, string> values,
        string machineKey,
        bool allowContains)
    {
        if (machineKey.Length == 0)
            return null;

        var exact = values.FirstOrDefault(pair =>
            string.Equals(ProjectIdentity.MachineKey(pair.Key), machineKey, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(ProjectIdentity.Normalize(pair.Key), machineKey, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(exact.Value))
            return exact.Value;

        return allowContains
            ? values.FirstOrDefault(pair =>
                ProjectIdentity.Normalize(pair.Key).Contains(machineKey, StringComparison.OrdinalIgnoreCase)).Value
            : null;
    }
}

public sealed class StandardProjectStructureService : IProjectStructureService
{
    private static readonly string[] ProjectFolders =
    {
        "00_Documents",
        "01_CAD",
        "02_SimulationProject",
        "03_WorkpieceTemplates",
        "04_Robot",
        "05_PLC",
        "06_Video"
    };

    public void EnsureCreated(string projectDirectory)
    {
        if (string.IsNullOrWhiteSpace(projectDirectory))
            throw new ArgumentException("A project directory is required.", nameof(projectDirectory));

        Directory.CreateDirectory(projectDirectory);
        foreach (var folder in ProjectFolders)
            Directory.CreateDirectory(Path.Combine(projectDirectory, folder));
    }
}
