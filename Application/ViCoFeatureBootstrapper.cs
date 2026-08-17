using System.IO;
using System.Net.Http;
using System.Security.Principal;
using VIBN_Tools.Application.VM;
using VIBN_Tools.Core.ViCo;
using VIBN_Tools.Infrastructure.ViCo;
using VIBN_Tools.Tia.Client;

namespace VIBN_Tools.Application;

public static class ViCoFeatureBootstrapper
{
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
        var pipeName = $"VIBN_Tools.TiaBridge.{Environment.ProcessId}";
        var bridgeExecutable = Path.Combine(
            AppContext.BaseDirectory,
            "TiaBridge",
            "VIBN_Tools.TiaBridge.exe");

        var client = new NamedPipeTiaBridgeClient(new TiaBridgeClientOptions(
            pipeName,
            BridgeExecutablePath: bridgeExecutable));

        return new TiaPortalPageVM(
            client,
            new TiaLibraryService(client),
            new WpfFolderSelectionService(),
            FindInstalledTiaVersions());
    }

    public static ViCoCopyPageVM CreateCopyViewModel()
    {
        var options = ViCoPathsOptions.CreateDefault();
        return new ViCoCopyPageVM(
            new BoundedFileCopyService(options.MaximumParallelCopies),
            new WpfFolderSelectionService());
    }

    public static ViCoSearchPageVM CreateSearchViewModel()
    {
        var options = ViCoPathsOptions.CreateDefault();
        var remoteDesktop = new WindowsRemoteDesktopService(options.WorkingDirectory);

        return new ViCoSearchPageVM(
            new LegacyWorkstationCatalog(options.ServerCacheRoot),
            new ViCoWorkstationSearch(),
            cancellationToken => CreatePathResolverAsync(options, cancellationToken),
            new NetworkAvailabilityService(),
            remoteDesktop,
            new WindowsPathLauncher(),
            new KanbanizeRefreshService(
                new HttpClient(),
                Environment.GetEnvironmentVariable("VIBN_VICO_KANBANIZE_API_KEY"),
                options.ServerCacheRoot));
    }

    public static ViCoAdministrationPageVM CreateAdministrationViewModel()
    {
        var options = ViCoPathsOptions.CreateDefault();
        return new ViCoAdministrationPageVM(
            new LegacyLicenseService(
                options.ApprovedLicensesRoot,
                options.LicenseRequestsRoot,
                Environment.GetEnvironmentVariable("VIBN_VICO_LICENSE_KEY")),
            new OutlookMeetingService(),
            new FileSystemViCoUpdateService(options.VersionsRoot),
            new WindowsPathLauncher(),
            WindowsIdentity.GetCurrent().Name);
    }

    private static async Task<IViCoRelatedPathResolver> CreatePathResolverAsync(
        ViCoPathsOptions options,
        CancellationToken cancellationToken) =>
        await ViCoRelatedPathResolver.CreateAsync(
            options.SimulationProjectsRoot,
            options.ServerCacheRoot,
            cancellationToken);

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
