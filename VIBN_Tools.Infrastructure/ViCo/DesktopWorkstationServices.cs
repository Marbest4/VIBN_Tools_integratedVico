using System.Diagnostics;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
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

/// <summary>
/// Creates a transient RDP profile without credential material. Windows owns
/// the password in the interactive user's Credential Manager, so the normal
/// action remains automatic while the prompted action can establish or change
/// that local Windows entry.
/// </summary>
public sealed class WindowsRemoteDesktopService : IRemoteDesktopService
{
    private const int MonitorMetric = 80;
    private readonly string _rdpFile;
    private readonly IRemoteCredentialStore _credentialStore;

    public WindowsRemoteDesktopService(
        string workingDirectory,
        IRemoteCredentialStore credentialStore)
    {
        Directory.CreateDirectory(workingDirectory);
        _rdpFile = Path.Combine(workingDirectory, "ViCo.rdp");
        _credentialStore = credentialStore;
    }

    public int MonitorCount => Math.Max(1, GetSystemMetrics(MonitorMetric));

    public void Connect(string hostName, string userName, IReadOnlyCollection<int> monitorIndexes)
    {
        _credentialStore.SaveTemporary(hostName, userName);
        try
        {
            ConnectInternal(hostName, userName, monitorIndexes, promptForCredentials: false);
        }
        finally
        {
            // mstsc reads the credential immediately. The delayed removal keeps
            // it available during startup but does not leave it in Credential Manager.
            _ = _credentialStore.RemoveAfterAsync(hostName, TimeSpan.FromSeconds(20));
        }
    }

    public void ConnectWithCredentialPrompt(string hostName, string userName, IReadOnlyCollection<int> monitorIndexes)
    {
        ConnectInternal(hostName, userName, monitorIndexes, promptForCredentials: true);
    }

    private void ConnectInternal(
        string hostName,
        string userName,
        IReadOnlyCollection<int> monitorIndexes,
        bool promptForCredentials)
    {
        var lines = RemoteDesktopProfileBuilder.Build(
            hostName,
            userName,
            monitorIndexes,
            MonitorCount,
            promptForCredentials);
        File.WriteAllLines(_rdpFile, lines, Encoding.Unicode);
        Process.Start(new ProcessStartInfo("mstsc.exe", $"\"{_rdpFile}\"") { UseShellExecute = true });
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);
}

/// <summary>
/// Stores a credential only for the RDP startup window. The password comes
/// from the current user's environment and is never compiled into the tool.
/// </summary>
public sealed class WindowsTemporaryRemoteCredentialStore : IRemoteCredentialStore
{
    public const string PasswordEnvironmentVariable = "VIBN_RDP_PASSWORD";

    public void SaveTemporary(string hostName, string userName)
    {
        // Read the persistent user value first. This avoids an older value
        // inherited by a still-running Visual Studio process taking precedence
        // after the configuration assistant changed the password.
        var password = Environment.GetEnvironmentVariable(
                           PasswordEnvironmentVariable,
                           EnvironmentVariableTarget.User) ??
                       Environment.GetEnvironmentVariable(PasswordEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                $"Automatische RDP-Anmeldung ist nicht eingerichtet. Benutzervariable {PasswordEnvironmentVariable} setzen.");
        }
        if (string.IsNullOrWhiteSpace(hostName) || string.IsNullOrWhiteSpace(userName))
            throw new ArgumentException("Remote-PC und Benutzer müssen angegeben sein.");

        RunCmdKey($"TERMSRV/{hostName.Trim()}", userName.Trim(), password);
    }

    public async Task RemoveAfterAsync(
        string hostName,
        TimeSpan delay,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(hostName))
            return;
        try
        {
            await Task.Delay(delay, cancellationToken);
            RunCmdKeyDelete($"TERMSRV/{hostName.Trim()}");
        }
        catch (OperationCanceledException)
        {
            // Application shutdown can cancel delayed cleanup.
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or TimeoutException or System.ComponentModel.Win32Exception)
        {
            // Cleanup is best effort and happens after mstsc has started. A
            // failed delete must not surface as an unobserved background fault.
        }
    }

    private static void RunCmdKey(string target, string userName, string password)
    {
        using var process = CreateCmdKeyProcess();
        process.StartInfo.ArgumentList.Add($"/generic:{target}");
        process.StartInfo.ArgumentList.Add($"/user:{userName}");
        process.StartInfo.ArgumentList.Add($"/pass:{password}");
        RunAndVerify(process, "RDP-Anmeldedaten konnten nicht temporär hinterlegt werden");
    }

    private static void RunCmdKeyDelete(string target)
    {
        using var process = CreateCmdKeyProcess();
        process.StartInfo.ArgumentList.Add($"/delete:{target}");
        RunAndVerify(process, "Temporäre RDP-Anmeldedaten konnten nicht entfernt werden", ignoreNotFound: true);
    }

    private static Process CreateCmdKeyProcess() => new()
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.SystemDirectory, "cmdkey.exe"),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }
    };

    private static void RunAndVerify(Process process, string message, bool ignoreNotFound = false)
    {
        if (!process.Start())
            throw new InvalidOperationException(message + ".");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit(5000);
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException(message + ": cmdkey hat nicht rechtzeitig geantwortet.");
        }
        if (process.ExitCode != 0 && !(ignoreNotFound &&
            (output.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
             output.Contains("nicht gefunden", StringComparison.OrdinalIgnoreCase) ||
             error.Contains("not found", StringComparison.OrdinalIgnoreCase))))
        {
            throw new InvalidOperationException($"{message} (cmdkey ExitCode {process.ExitCode}).");
        }
    }
}

/// <summary>
/// Uses the Windows <c>quser</c> command to read RDP/terminal sessions from a
/// remote PC. It is intentionally read-only. When the account is not allowed
/// to query the target, the caller receives <see cref="ViCoRemoteSessionInfo.NotAvailable"/>
/// rather than a misleading online/session state.
/// </summary>
public sealed class WindowsRemoteSessionService : IRemoteSessionService
{
    private static readonly string[] ActiveStates = { "ACTIVE", "AKTIV" };

    public async Task<ViCoRemoteSessionInfo> GetSessionInfoAsync(
        string hostName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(hostName))
            return ViCoRemoteSessionInfo.NotAvailable;

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "quser.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };
        process.StartInfo.ArgumentList.Add($"/server:{hostName.Trim()}");

        try
        {
            if (!process.Start())
                return ViCoRemoteSessionInfo.NotAvailable;

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(3));
            var outputTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var errorTask = process.StandardError.ReadToEndAsync(timeout.Token);
            await process.WaitForExitAsync(timeout.Token);
            var output = await outputTask;
            var error = await errorTask;
            if (process.ExitCode != 0)
                return ViCoRemoteSessionInfo.Unavailable(DescribeFailure(error, process.ExitCode));

            return Parse(output);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryStop(process);
            return ViCoRemoteSessionInfo.Unavailable("Zeitüberschreitung bei der Remote-Sitzungsabfrage.");
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return ViCoRemoteSessionInfo.Unavailable($"Remote-Sitzungsabfrage nicht verfügbar: {exception.Message}");
        }
    }

    private static string DescribeFailure(string error, int exitCode)
    {
        if (error.Contains("Access is denied", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("Zugriff verweigert", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("Error 5", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("Fehler 5", StringComparison.OrdinalIgnoreCase))
        {
            return "Berechtigung fehlt. Mit einem für die Remotedienste des Ziel-PCs autorisierten Administratorkonto starten.";
        }

        var detail = error.Trim();
        return detail.Length == 0
            ? $"Remote-Sitzungsabfrage fehlgeschlagen (quser ExitCode {exitCode})."
            : detail;
    }

    private static ViCoRemoteSessionInfo Parse(string output)
    {
        var sessions = output
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(ParseSession)
            .Where(session => session is not null)
            .Select(session => session!)
            .ToArray();
        if (sessions.Length == 0)
            return new ViCoRemoteSessionInfo(true, string.Empty, string.Empty, null);

        var active = sessions.FirstOrDefault(session => ActiveStates.Any(state =>
            string.Equals(state, session.State, StringComparison.OrdinalIgnoreCase)));
        var latest = sessions
            .Where(session => session.LogonAt is not null)
            .OrderByDescending(session => session.LogonAt)
            .FirstOrDefault() ?? sessions[0];
        return new ViCoRemoteSessionInfo(
            true,
            active?.UserName ?? string.Empty,
            latest.UserName,
            latest.LogonAt);
    }

    private static RemoteSessionRow? ParseSession(string rawLine)
    {
        if (rawLine.Contains("USERNAME", StringComparison.OrdinalIgnoreCase) ||
            rawLine.Contains("BENUTZERNAME", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var columns = Regex.Split(rawLine.Trim().TrimStart('>'), @"\s{2,}")
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .ToArray();
        var idIndex = Array.FindIndex(columns, value => int.TryParse(value, out _));
        if (idIndex < 1 || idIndex + 1 >= columns.Length)
            return null;

        var userName = columns[0];
        var state = columns[idIndex + 1];
        var logonText = columns[^1];
        return new RemoteSessionRow(userName, state, ParseLogonTime(logonText));
    }

    private static DateTimeOffset? ParseLogonTime(string value)
    {
        var cultures = new[]
        {
            CultureInfo.CurrentCulture,
            CultureInfo.GetCultureInfo("de-DE"),
            CultureInfo.InvariantCulture
        };
        foreach (var culture in cultures)
        {
            if (DateTimeOffset.TryParse(
                    value,
                    culture,
                    DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces,
                    out var parsed))
            {
                return parsed;
            }
        }
        return null;
    }

    private static void TryStop(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // The timed-out query can have exited between the check and Kill.
        }
    }

    private sealed record RemoteSessionRow(string UserName, string State, DateTimeOffset? LogonAt);
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
