using System.IO;
using System.Net.Http;
using System.Security.Principal;
using VIBN_Tools.Application.VM;
using VIBN_Tools.Core.Kanbanize;
using VIBN_Tools.Core.ViCo;
using VIBN_Tools.Infrastructure.Kanbanize;
using VIBN_Tools.Infrastructure.ViCo;
using VIBN_Tools.Tia.Client;
using VIBN_Tools.Settings;

namespace VIBN_Tools.Application;

public static class ViCoFeatureBootstrapper
{
    private static readonly ViCoPathsOptions SharedOptions = ViCoPathsOptions.CreateDefault();
    private static readonly ViCoWorkspaceContext WorkspaceContext = new();
    private static readonly LegacyWorkstationCatalog SharedWorkstationCatalog =
        new(SharedOptions.ServerCacheRoot);
    private static readonly IViCoUserRoleStore SharedUserRoleStore = CreateUserRoleStore();

    public static IWorkstationDirectory WorkstationDirectory { get; } =
        new WorkstationDirectory(SharedWorkstationCatalog);

    /// <summary>Single shared source of truth for all VIBN Tools role checks.</summary>
    public static IViCoUserRoleStore UserRoleStore => SharedUserRoleStore;

    public static Task InitializeWorkstationDirectoryAsync(CancellationToken cancellationToken = default) =>
        WorkstationDirectory.RefreshAsync(cancellationToken);

    public static ViCoPageVM CreateViewModel()
    {
        var options = ViCoPathsOptions.CreateDefault();

        return new ViCoPageVM(
            new FileSystemProjectCatalogService(options),
            new ProjectSearchService(),
            new LegacyTextFavoritesRepository(options.FavoritesFile),
            new WindowsPathLauncher(),
            new WpfFolderSelectionService());
    }

    public static TiaPortalPageVM CreateTiaPortalViewModel()
    {
        var client = CreateTiaBridgeClient();

        return new TiaPortalPageVM(
            client,
            new TiaLibraryService(client),
            new WpfFolderSelectionService(),
            FindInstalledTiaVersions(),
            ApplicationLogService.Instance);
    }

    /// <summary>Creates an independent bridge process for a TIA-facing page.</summary>
    public static ITiaBridgeClient CreateTiaBridgeClient()
    {
        var pipeName = $"VIBN_Tools.TiaBridge.{Environment.ProcessId}.{Guid.NewGuid():N}";
        var bridgeExecutable = Path.Combine(
            AppContext.BaseDirectory,
            "TiaBridge",
            "VIBN_Tools.TiaBridge.exe");
        return new NamedPipeTiaBridgeClient(new TiaBridgeClientOptions(
            pipeName,
            RequestTimeout: TimeSpan.FromMinutes(5),
            BridgeExecutablePath: bridgeExecutable));
    }

    public static SpecialDevicePageVM CreateSpecialDeviceViewModel() =>
        new(CreateTiaBridgeClient(), FindInstalledTiaVersions(), ApplicationLogService.Instance);

    public static ViCoCopyPageVM CreateCopyViewModel()
    {
        var options = ViCoPathsOptions.CreateDefault();
        return new ViCoCopyPageVM(
            new BoundedFileCopyService(options.MaximumParallelCopies),
            new WpfFolderSelectionService(),
            WorkspaceContext,
            new StandardProjectStructureService());
    }

    public static ViCoSearchPageVM CreateSearchViewModel()
    {
        var options = ViCoPathsOptions.CreateDefault();
        var remoteDesktop = new WindowsRemoteDesktopService(
            options.WorkingDirectory,
            new WindowsTemporaryRemoteCredentialStore());
        var apiKey = ResolveKanbanizeApiKey();

        return new ViCoSearchPageVM(
            SharedWorkstationCatalog,
            new ViCoWorkstationSearch(),
            cancellationToken => CreatePathResolverAsync(options, cancellationToken),
            new NetworkAvailabilityService(),
            remoteDesktop,
            new WindowsRemoteSessionService(),
            new WindowsPathLauncher(),
            new KanbanizeRefreshService(
                new HttpClient(),
                apiKey,
                options.ServerCacheRoot),
            new KanbanizeWorkstationConfigurationService(new HttpClient(), apiKey),
            WorkspaceContext,
            workstations => WorkstationDirectory.Synchronize(workstations),
            ApplicationLogService.Instance);
    }

    /// <summary>
    /// Creates the standalone card workflow. It shares the existing Kanbanize
    /// credentials with ViCo, but has no dependency on ViCo role visibility.
    /// </summary>
    public static KanbanizeCardPageVM CreateKanbanizeCardViewModel()
    {
        IKanbanizeCardService cards = new KanbanizeCardApiService(
            new HttpClient(),
            ResolveKanbanizeApiKey());
        return new KanbanizeCardPageVM(
            cards,
            new VibnWorkplaceSynchronizationService(cards),
            ApplicationLogService.Instance);
    }

    public static ViCoAdministrationPageVM CreateAdministrationViewModel()
    {
        var options = ViCoPathsOptions.CreateDefault();
        return new ViCoAdministrationPageVM(
            SharedUserRoleStore,
            new OutlookMeetingService(),
            new FileSystemViCoUpdateService(options.VersionsRoot),
            new WindowsPathLauncher(),
            WindowsIdentity.GetCurrent().Name,
            ApplicationLogService.Instance);
    }

    /// <summary>Creates the authorization gate for the ViCo workspace navigation.</summary>
    public static ViCoWorkspacePageVM CreateWorkspaceViewModel()
    {
        return new ViCoWorkspacePageVM(
            SharedUserRoleStore,
            WindowsIdentity.GetCurrent().Name,
            ApplicationLogService.Instance);
    }

    private static IViCoUserRoleStore CreateUserRoleStore()
    {
        return new JsonViCoUserRoleStore(
            SharedOptions.RolesFile,
            LoadLegacyRolesForOneTimeMigrationAsync);
    }

    /// <summary>
    /// Reads the predecessor's encrypted assignments once when roles.json is
    /// absent. The active application never creates requests or license files.
    /// </summary>
    private static async Task<IReadOnlyList<ViCoUserRole>> LoadLegacyRolesForOneTimeMigrationAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var legacy = new LegacyRoleMigrationReader(
                SharedOptions.LegacyRoleAssignmentsRoot,
                LegacyRoleMigrationCompatibility.ResolveKey());
            if (!legacy.CanRead)
                return Array.Empty<ViCoUserRole>();

            var assignments = await legacy.LoadAsync(cancellationToken);
            return assignments
                .Select(assignment => new ViCoUserRole(
                    assignment.UserName,
                    assignment.Level,
                    "Migration"))
                .ToArray();
        }
        catch (Exception exception)
        {
            ApplicationLogService.Instance.Warning(
                "Rollenverwaltung",
                "Historische Rollen konnten nicht migriert werden; die zentrale roles.json wird unverändert verwendet.",
                exception.Message);
            return Array.Empty<ViCoUserRole>();
        }
    }

    private static async Task<IViCoRelatedPathResolver> CreatePathResolverAsync(
        ViCoPathsOptions options,
        CancellationToken cancellationToken) =>
        await ViCoRelatedPathResolver.CreateAsync(
            options.SimulationProjectsRoot,
            options.ServerCacheRoot,
            cancellationToken,
            options.CommissioningProjectsRoot,
            options.PlanningProjectsRoot);

    private static string? ResolveKanbanizeApiKey()
    {
        return (Environment.GetEnvironmentVariable(
                    "VIBN_VICO_KANBANIZE_API_KEY",
                    EnvironmentVariableTarget.User) ??
                Environment.GetEnvironmentVariable("VIBN_VICO_KANBANIZE_API_KEY"))?.Trim();
    }

    private static IReadOnlyList<string> FindInstalledTiaVersions()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        return Enumerable.Range(15, 8)
            .Reverse()
            .Select(version => $"V{version}")
            .Where(version => File.Exists(Path.Combine(
                programFiles,
                "Siemens",
                "Automation",
                $"Portal {version}",
                "PublicAPI",
                version,
                "Siemens.Engineering.dll")))
            .ToArray();
    }
}
