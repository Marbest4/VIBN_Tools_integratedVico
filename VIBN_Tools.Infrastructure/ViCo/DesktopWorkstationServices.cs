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

    public WindowsRemoteDesktopService(string workingDirectory)
    {
        Directory.CreateDirectory(workingDirectory);
        _rdpFile = Path.Combine(workingDirectory, "ViCo.rdp");
    }

    public int MonitorCount => Math.Max(1, GetSystemMetrics(MonitorMetric));

    public void Connect(string hostName, string userName, IReadOnlyCollection<int> monitorIndexes)
    {
        if (string.IsNullOrWhiteSpace(hostName))
            throw new ArgumentException("A workstation is required.", nameof(hostName));

        var monitors = monitorIndexes
            .Where(index => index >= 0 && index < MonitorCount)
            .Distinct()
            .OrderBy(index => index)
            .ToArray();
        if (monitors.Length == 0)
            monitors = new[] { 0 };

        var lines = new List<string>
        {
            "screen mode id:i:2",
            "session bpp:i:32",
            "compression:i:1",
            "keyboardhook:i:2",
            "networkautodetect:i:1",
            "bandwidthautodetect:i:1",
            "displayconnectionbar:i:1",
            "redirectclipboard:i:1",
            "autoreconnection enabled:i:1",
            $"full address:s:{hostName}",
            $"username:s:{userName}",
            "prompt for credentials:i:1",
            "administrative session:i:0",
            "enablecredsspsupport:i:1",
            "redirectprinters:i:0",
            "redirectcomports:i:0",
            "redirectsmartcards:i:0",
            "drivestoredirect:s:"
        };

        if (monitors.Length == MonitorCount)
            lines.Add("use multimon:i:1");
        else if (monitors.Length > 1)
        {
            lines.Add($"selectedmonitors:s:{string.Join(',', monitors)}");
            lines.Add("use multimon:i:1");
        }
        else
        {
            lines.Add("use multimon:i:0");
        }

        File.WriteAllLines(_rdpFile, lines, Encoding.Unicode);
        Process.Start(new ProcessStartInfo("mstsc.exe", $"\"{_rdpFile}\"") { UseShellExecute = true });
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

        var key = ExtractProjectKey(project);
        return kind switch
        {
            ViCoRelatedPathKind.Simulation => Find(_projects, project),
            ViCoRelatedPathKind.Commissioning => Find(_commissioning, key),
            ViCoRelatedPathKind.Planning => FindContains(_planning, key),
            _ => null
        };
    }

    public static async Task<ViCoRelatedPathResolver> CreateAsync(
        string simulationRoot,
        string cacheRoot,
        CancellationToken cancellationToken = default)
    {
        var projectService = new FileSystemProjectCatalogService(new ViCoPathsOptions(simulationRoot, string.Empty));
        var catalog = await projectService.LoadAsync(cancellationToken);
        var projects = catalog.Projects.ToDictionary(
            item => item.DisplayName,
            item => item.FullPath,
            StringComparer.OrdinalIgnoreCase);
        var commissioning = await LoadPairsAsync(cacheRoot, "ComissioningFoldersName.txt", "ComissioningFoldersPath.txt", cancellationToken);
        var planning = await LoadPairsAsync(cacheRoot, "PlanningFoldersName.txt", "PlanningFoldersPath.txt", cancellationToken);
        return new ViCoRelatedPathResolver(simulationRoot, projects, commissioning, planning);
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

    private static string? Find(IReadOnlyDictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var path) ? path : null;

    private static string? FindContains(IReadOnlyDictionary<string, string> values, string key) =>
        values.FirstOrDefault(pair => pair.Key.Contains(key, StringComparison.OrdinalIgnoreCase)).Value;

    private static string ExtractProjectKey(string project)
    {
        var first = project.Split('/')[0];
        var parts = first.Split('_', '-');
        return parts.FirstOrDefault(part => part.Length >= 3) ?? first;
    }
}
