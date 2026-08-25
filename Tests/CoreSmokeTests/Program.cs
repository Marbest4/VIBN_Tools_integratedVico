using VIBN_Tools.Core.ViCo;
using VIBN_Tools.Core.Kanbanize;
using VIBN_Tools.Infrastructure.Kanbanize;
using VIBN_Tools.Infrastructure.ViCo;
using VIBN_Tools.Tia.Client;
using VIBN_Tools.Tia.Contracts;
using System.IO.Pipes;
using System.Net;
using System.Net.Http;
using System.Text.Json;

var temporaryRoot = Path.Combine(Path.GetTempPath(), $"vibn-vico-tests-{Guid.NewGuid():N}");

try
{
    Directory.CreateDirectory(temporaryRoot);
    Console.WriteLine("Running project catalog and search smoke test...");
    await VerifyProjectCatalogAndSearchAsync(temporaryRoot);
    Console.WriteLine("Running favorites compatibility smoke test...");
    await VerifyFavoritesCompatibilityAsync(temporaryRoot);
    Console.WriteLine("Running bounded file copy smoke test...");
    await VerifyFileCopyAsync(temporaryRoot);
    Console.WriteLine("Running legacy workstation catalog smoke test...");
    await VerifyLegacyWorkstationCatalogAsync(temporaryRoot);
    Console.WriteLine("Running workstation occupancy and unified search smoke test...");
    VerifyWorkstationOccupancyAndUnifiedSearch();
    Console.WriteLine("Running shared workstation directory smoke test...");
    await VerifyWorkstationDirectoryAsync();
    Console.WriteLine("Running ViCo project identity and path smoke test...");
    VerifyProjectIdentityAndPaths(temporaryRoot);
    Console.WriteLine("Running Remote Desktop profile smoke test...");
    VerifyRemoteDesktopProfile();
    Console.WriteLine("Running Level9 administration policy smoke test...");
    VerifyLicenseAdministrationPolicy();
    Console.WriteLine("Running Kanbanize card draft policy smoke test...");
    VerifyKanbanizeCardDraftPolicy();
    Console.WriteLine("Running idempotent VIBN workplace synchronization smoke test...");
    await VerifyVibnWorkplaceSynchronizationAsync();
    Console.WriteLine("Running narrow Kanbanize HTTP write-scope smoke test...");
    await VerifyKanbanizeHttpWriteScopeAsync();
    if (OperatingSystem.IsWindows())
    {
        Console.WriteLine("Running legacy license and update smoke test...");
        await VerifyAdministrationServicesAsync(temporaryRoot);
        Console.WriteLine("Running administration identity smoke test...");
        await VerifyAdministrationIdentityAsync();
    }
    Console.WriteLine("Running TIA library workflow smoke test...");
    await VerifyTiaLibraryWorkflowAsync(temporaryRoot);
    Console.WriteLine("Running typed TIA pipe protocol smoke test...");
    await VerifyTypedTiaPipeProtocolAsync();
    Console.WriteLine("All ViCo core smoke tests passed.");
    return 0;
}
finally
{
    if (Directory.Exists(temporaryRoot))
        Directory.Delete(temporaryRoot, recursive: true);
}

static async Task VerifyProjectCatalogAndSearchAsync(string temporaryRoot)
{
    var projectPath = Path.Combine(temporaryRoot, "Area", "GM1234_Line", "05-130");
    Directory.CreateDirectory(projectPath);

    var options = new ViCoPathsOptions(temporaryRoot, Path.Combine(temporaryRoot, "favorites.txt"));
    var catalog = await new FileSystemProjectCatalogService(options).LoadAsync();

    Assert(catalog.Projects.Count == 1, "The project catalog should contain one project.");
    Assert(catalog.Projects[0].DisplayName == "GM1234/05-130", "Unexpected display name.");

    var results = new ProjectSearchService().Search(catalog.Projects, "gm1234/05130");
    Assert(results.Count == 1 && results[0].FullPath == projectPath, "Normalized search failed.");
}

static async Task VerifyFavoritesCompatibilityAsync(string temporaryRoot)
{
    var favoritesPath = Path.Combine(temporaryRoot, "favorites", "Favorites_2.txt");
    var repository = new LegacyTextFavoritesRepository(favoritesPath);
    var expected = new[] { new FavoriteEntry("Project", @"C:\Projects\Project") };

    await repository.SaveAsync(expected);
    var actual = await repository.LoadAsync();

    Assert(actual.SequenceEqual(expected), "Legacy favorites roundtrip failed.");
}

static async Task VerifyFileCopyAsync(string temporaryRoot)
{
    var sourceDirectory = Path.Combine(temporaryRoot, "copy-source");
    var destinationDirectory = Path.Combine(temporaryRoot, "copy-destination");
    Directory.CreateDirectory(sourceDirectory);
    await File.WriteAllTextAsync(Path.Combine(sourceDirectory, "data.txt"), "VIBN");

    var service = new BoundedFileCopyService();
    await service.CopyAsync(new[] { new FileCopyItem(sourceDirectory, destinationDirectory) });

    Assert(
        await File.ReadAllTextAsync(Path.Combine(destinationDirectory, "data.txt")) == "VIBN",
        "File copy failed.");
}

static async Task VerifyLegacyWorkstationCatalogAsync(string temporaryRoot)
{
    var cache = Path.Combine(temporaryRoot, "server-cache");
    Directory.CreateDirectory(cache);
    await File.WriteAllLinesAsync(
        Path.Combine(cache, "AllPCLaneInfosWithChilds.txt"),
        new[] { "lane-1", "GM12345 Tool PC" });
    await File.WriteAllLinesAsync(
        Path.Combine(cache, "AllCardsOfPCsV2.txt"),
        new[]
        {
            "#Working#[GM9000/01-001] Demo", "lane-1",
            "Remote user: ZKDS-Simulation-P01", "lane-1",
            "TIA V18", "lane-1",
            "Beckhoff TwinCAT 3 angegeben", "lane-1",
            "Rockwell Studio 5000 V35 installiert", "lane-1",
            "FEE 5.0", "lane-1",
            "LAN Industrial", "lane-1"
        });
    await File.WriteAllLinesAsync(
        Path.Combine(cache, "AllRobyCards.txt"),
        new[] { "[GM9000/01-001][R01] Software Robotik", "column-working" });
    await File.WriteAllLinesAsync(
        Path.Combine(cache, "AllRobyCardsRobyName.txt"),
        new[] { "R01", "column-working" });
    await File.WriteAllLinesAsync(
        Path.Combine(cache, "AllRobyColumns.txt"),
        new[] { "column-working", "In Arbeit" });

    var snapshot = await new LegacyWorkstationCatalog(cache).LoadAsync();
    Assert(snapshot.Workstations.Count == 1, "Legacy workstation catalog should contain one workstation.");
    var workstation = snapshot.Workstations[0];
    Assert(workstation.PcName == "GM12345", "Workstation name parsing failed.");
    Assert(workstation.UserName == "zkds-simulation-p01", "Kanbanize user parsing failed.");
    Assert(workstation.Status == "Belegt", "Active Kanbanize cards must mark the workstation as occupied.");
    Assert(workstation.Projects.Count == 1, "Project card parsing failed.");
    Assert(workstation.AutomationSoftware.Count == 3, "TIA, Beckhoff and Rockwell should be detected.");
    Assert(workstation.SoftwareInformation.Contains("TwinCAT", StringComparison.OrdinalIgnoreCase),
        "Beckhoff software information is missing.");
    Assert(workstation.RobotCount == 1 && workstation.RobotDetails[0].Name == "R01",
        "Robot name, status or deduplication failed.");
    Assert(new ViCoWorkstationSearch().Search(snapshot.Workstations, "GM9000", ViCoSearchMode.Project).Count == 1,
        "Project-oriented workstation search failed.");
}

static void VerifyWorkstationOccupancyAndUnifiedSearch()
{
    var free = new ViCoWorkstation(
        "GM10001 Free PC",
        "GM10001",
        "zkds-free",
        string.Empty,
        string.Empty,
        string.Empty,
        new[] { "[B] GM1000/01-001", "[D] GM1000/01-002" },
        new[] { "[B] GM1000/01-001", "[D] GM1000/01-002" });
    var occupied = new ViCoWorkstation(
        "GM10002 Busy PC",
        "GM10002",
        "zkds-busy",
        string.Empty,
        string.Empty,
        string.Empty,
        new[] { "[P] GM2000/01-001", "[D] GM2000/01-002" },
        new[] { "[P] GM2000/01-001", "[D] GM2000/01-002" });

    Assert(free.Status == "Frei", "Backlog/done-only cards must mark a workstation as free.");
    Assert(occupied.Status == "Belegt", "Planning must take precedence over done cards.");

    var search = new ViCoWorkstationSearch();
    Assert(search.Search(new[] { free, occupied }, "zkds-busy", ViCoSearchMode.All).Single() == occupied,
        "Unified search must find a Kanbanize user without selecting a separate mode.");
    Assert(search.Search(new[] { free, occupied }, "GM1000/01-001", ViCoSearchMode.All).Single() == free,
        "Unified search must continue to find project numbers.");
}

static async Task VerifyWorkstationDirectoryAsync()
{
    var workstation = new ViCoWorkstation(
        "GM12345 Tool PC",
        "GM12345",
        "kanbanize-user",
        "TIA Portal V18",
        string.Empty,
        string.Empty,
        Array.Empty<string>(),
        Array.Empty<string>());
    var directory = new WorkstationDirectory(new SnapshotCatalog(workstation));
    await directory.RefreshAsync();

    Assert(directory.PcNames.SequenceEqual(new[] { "localhost", "GM12345" }),
        "The shared workstation directory did not expose the dynamic PC list.");
    Assert(directory.FindUser("gm12345") == "kanbanize-user",
        "Kanbanize user priority in the shared workstation directory failed.");
}

static void VerifyProjectIdentityAndPaths(string temporaryRoot)
{
    var simulationRoot = Path.Combine(temporaryRoot, "simulation");
    var simulationPath = Path.Combine(simulationRoot, "Area", "GM_GU1660_Line", "05-130");
    Directory.CreateDirectory(simulationPath);

    var resolver = new ViCoRelatedPathResolver(
        simulationRoot,
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["GM_GU1660/05-130"] = simulationPath
        },
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["GU1660"] = @"\\server\plc\GU1660"
        },
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Customer_GU1660_Planning"] = @"\\server\planning\GU1660"
        });
    var workstation = new ViCoWorkstation(
        "GM12345 Tool PC",
        "GM12345",
        "zkds-simulation-p01",
        "TIA V18",
        "FEE 5",
        "LAN",
        new[] { "[W] GM_GU1660/05-130 Customer" },
        Array.Empty<string>());

    var projectCard = workstation.Projects[0];
    Assert(resolver.Resolve(workstation, projectCard, ViCoRelatedPathKind.Simulation) == simulationPath,
        "Status-tolerant simulation path resolution failed.");
    Assert(resolver.Resolve(workstation, projectCard, ViCoRelatedPathKind.Commissioning) == @"\\server\plc\GU1660",
        "Commissioning path resolution failed.");
    Assert(resolver.Resolve(workstation, projectCard, ViCoRelatedPathKind.Planning) == @"\\server\planning\GU1660",
        "Planning path resolution failed.");

    var workstationPath = resolver.Resolve(workstation, projectCard, ViCoRelatedPathKind.WorkstationProject);
    Assert(workstationPath == Path.Combine(@"\\GM12345\_Projekte$", "Area", "GM_GU1660_Line", "05-130"),
        "Workstation project path mapping failed.");

    var structureRoot = Path.Combine(temporaryRoot, "project-structure");
    new StandardProjectStructureService().EnsureCreated(structureRoot);
    Assert(Directory.Exists(Path.Combine(structureRoot, "02_SimulationProject")),
        "Standard project structure creation failed.");
}

static void VerifyRemoteDesktopProfile()
{
    var lines = RemoteDesktopProfileBuilder.Build(
        "GM12345",
        "zkds-simulation-p01",
        new[] { 0, 2 },
        3);
    Assert(lines.Contains("username:s:zkds-simulation-p01"),
        "The normalized Kanbanize user was not written to the RDP profile.");
    Assert(lines.Contains("prompt for credentials:i:0"),
        "The RDP profile must retain the original one-click credential behavior.");
    Assert(lines.Contains("selectedmonitors:s:0,2"),
        "Selected RDP monitors were not preserved.");
}

static async Task VerifyAdministrationServicesAsync(string temporaryRoot)
{
    var approved = Path.Combine(temporaryRoot, "licenses", "approved");
    var requests = Path.Combine(temporaryRoot, "licenses", "requests");
    var licenses = new LegacyLicenseService(
        approved,
        requests,
        "12345678901234567890123456789012");
    await licenses.SetLevelAsync(@"grob\user", "Level8");
    var entries = await licenses.LoadApprovedAsync();
    Assert(entries.Count == 1 && entries[0].Level == "Level8", "Legacy license roundtrip failed.");
    Assert(WindowsUserIdentity.Equals(@"grob\user", "user"),
        "Domain-qualified and short Windows users should identify the same license.");

    var version = Path.Combine(temporaryRoot, "versions", "V1.2.3", "publish");
    Directory.CreateDirectory(version);
    await File.WriteAllTextAsync(Path.Combine(version, "VICO_V2.exe"), string.Empty);
    var update = await new FileSystemViCoUpdateService(Path.Combine(temporaryRoot, "versions")).FindLatestAsync();
    Assert(update?.Version == "1.2.3", "ViCo update discovery failed.");
}

static void VerifyLicenseAdministrationPolicy()
{
    var oneLevel9 = new[]
    {
        new ViCoLicenseEntry(@"grob\lutzma", "Level9", "memory"),
        new ViCoLicenseEntry("admin-b", "Level8", "memory")
    };
    var promotion = LicenseAdministrationPolicy.PlanChange(oneLevel9, "admin-b", "Level9");
    Assert(promotion.IsValid && promotion.ResultingLevel9Users.Count == 2,
        "Promoting a second distinct Level9 user must be accepted.");

    var twoLevel9 = new[]
    {
        new ViCoLicenseEntry(@"grob\lutzma", "Level9", "memory"),
        new ViCoLicenseEntry("admin-a", "Level9", "memory"),
        new ViCoLicenseEntry("admin-c", "Level8", "memory")
    };
    var unsafeDowngrade = LicenseAdministrationPolicy.PlanChange(twoLevel9, "admin-a", "Level8");
    Assert(!unsafeDowngrade.IsValid,
        "Downgrading to a single Level9 user must be rejected.");

    var safeReplacement = LicenseAdministrationPolicy.PlanChange(
        twoLevel9,
        "admin-a",
        "Level8",
        "admin-c");
    Assert(safeReplacement.IsValid && safeReplacement.ResultingLevel9Users.Count == 2,
        "Replacing a Level9 user atomically must be accepted.");
    Assert(safeReplacement.Changes[0] == new LicenseLevelChange("admin-c", "Level9"),
        "The replacement promotion must be persisted before the downgrade.");

    var duplicateIdentity = new[]
    {
        new ViCoLicenseEntry(@"grob\lutzma", "Level9", "memory"),
        new ViCoLicenseEntry("LUTZMA", "Level9", "memory")
    };
    var duplicatePlan = LicenseAdministrationPolicy.PlanChange(duplicateIdentity, "lutzma", "Level9");
    Assert(!duplicatePlan.IsValid,
        "Domain-qualified and short names of the same account must count only once.");

    Assert(LicenseAdministrationPolicy.GetEffectiveLevel(@"grob\lutzma", null) == "Level9",
        "lutzma must be an effective Level9 administrator even before the compatible store is refreshed.");
    var mandatoryUserDowngrade = LicenseAdministrationPolicy.PlanChange(twoLevel9, "lutzma", "Level8");
    Assert(!mandatoryUserDowngrade.IsValid,
        "The mandatory lutzma Level9 assignment must not be downgradable through the UI.");
}

static void VerifyKanbanizeCardDraftPolicy()
{
    var valid = new KanbanizeCardDraft(1541, 28125, 29373, "Neue Karte", "Beschreibung", 3, "GM1234", null);
    Assert(KanbanizeCardDraftPolicy.Validate(valid) is null,
        "A complete Kanbanize card draft should be valid.");
    Assert(KanbanizeCardDraftPolicy.Validate(valid with { Title = " " }) is not null,
        "A Kanbanize card title must be required.");
    Assert(KanbanizeCardDraftPolicy.Validate(valid with { Priority = 5 }) is not null,
        "Kanbanize card priority must be bounded.");
}

static async Task VerifyVibnWorkplaceSynchronizationAsync()
{
    var sourceDeadline = new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);
    var service = new MemoryKanbanizeCardService(
        new[]
        {
            new KanbanizeCardInfo(101, 1392, 10, 20, "[VIBN] Grundinbetriebnahme GM1000", null, sourceDeadline),
            new KanbanizeCardInfo(102, 1392, 10, 20, "[VIBN] Grundinbetriebnahme GM2000", null, sourceDeadline),
            new KanbanizeCardInfo(105, 1392, 10, 20, "[VIBN] Grundinbetriebnahme GM5000", null, sourceDeadline),
            new KanbanizeCardInfo(106, 1392, 10, 20, "[VIBN] Grundinbetriebnahme GM6000", null, sourceDeadline),
            new KanbanizeCardInfo(107, 1392, 10, 20, "[VIBN] Grundinbetriebnahme Vorlage", null, sourceDeadline),
            new KanbanizeCardInfo(108, 1392, 10, 25236, "[VIBN] Grundinbetriebnahme Archiv", null, sourceDeadline)
        },
        new[]
        {
            new KanbanizeCardInfo(201, 1541, 28125, 29373, "Bestehende Karte", "102", sourceDeadline.AddDays(-3)),
            new KanbanizeCardInfo(205, 1541, 28125, 29373, "Bereits aktuell", "105", sourceDeadline),
            new KanbanizeCardInfo(206, 1541, 28125, 29373, "Doppelte Eins", "106", sourceDeadline),
            new KanbanizeCardInfo(207, 1541, 28125, 29373, "Doppelte Zwei", "106", sourceDeadline)
        });
    var synchronization = new VibnWorkplaceSynchronizationService(service);
    var settings = new VibnWorkplaceSynchronizationSettings(1392, 1541, 28125, 29373, 3, true);

    var preview = await synchronization.PreviewAsync(settings);
    Assert(preview.CreateCount == 1 && preview.DeadlineUpdateCount == 1 && preview.UnchangedCount == 1,
        "The preview must distinguish missing, stale and already-current target cards.");
    Assert(preview.ConflictCount == 1 && preview.ExcludedSourceCardCount == 2,
        "Duplicate target IDs must be reported and template/archive source cards excluded.");

    var withoutDeadlineSync = await synchronization.PreviewAsync(settings with { SynchronizeDeadlines = false });
    Assert(withoutDeadlineSync.DeadlineUpdateCount == 0,
        "Deadline synchronization must be explicitly suppressible without affecting duplicate detection.");

    var result = await synchronization.SynchronizeAsync(settings);
    Assert(result.CreatedCount == 1 && result.DeadlineUpdateCount == 1 && result.Failures.Count == 0,
        "Synchronization should create the missing target and adjust only its stale deadline.");
    Assert(service.GeneratedCards.Single().SourceCardId == 101 &&
           service.GeneratedCards.Single().Title == "*[Gen]* GM1000",
        "A generated card must preserve the legacy title marker and source identity.");
    Assert(service.DeadlineChanges.SequenceEqual(new[] { new DeadlineChange(201, sourceDeadline) }),
        "Only the existing target deadline may be changed; no other target field is updated.");

    var repeat = await synchronization.SynchronizeAsync(settings);
    Assert(repeat.CreatedCount == 0 && repeat.DeadlineUpdateCount == 0,
        "A second synchronization must not create duplicates or repeat unchanged deadline updates.");
}

static async Task VerifyKanbanizeHttpWriteScopeAsync()
{
    var deadline = new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);
    using var handler = new RecordingHttpMessageHandler();
    handler.EnqueueJson("""
        {"data":{"data":[{"card_id":101,"board_id":1392,"lane_id":10,"column_id":20,"title":"[VIBN] Grundinbetriebnahme GM1000","custom_id":null,"deadline":"2026-09-15T12:00:00.0000000Z"}],"pagination":{"all_pages":1}}}
        """);
    handler.EnqueueJson("""
        {"data":{"card_id":9001,"title":"*[Gen]* GM1000"}}
        """);
    handler.EnqueueJson("{}");
    using var httpClient = new HttpClient(handler);
    var api = new KanbanizeCardApiService(httpClient, "test-only-key", "https://example.test/api/v2");

    var cards = await api.LoadCardsAsync(1392);
    await api.CreateGeneratedCardAsync(new KanbanizeGeneratedCardDraft(
        101,
        28125,
        29373,
        "*[Gen]* GM1000",
        3,
        deadline));
    await api.UpdateDeadlineAsync(9001, deadline);

    Assert(cards.Count == 1 && string.IsNullOrEmpty(cards[0].CustomId),
        "The card reader must preserve the source card fields used for duplicate detection.");
    Assert(handler.Requests.Count == 3, "The API adapter should make one read and two narrowly scoped writes.");
    Assert(handler.Requests[0].RelativeUrl.Contains("per_page=1000", StringComparison.Ordinal) &&
           handler.Requests[0].RelativeUrl.Contains("custom_id", StringComparison.Ordinal),
        "The synchronization reader must request all relevant card identity fields.");
    Assert(handler.Requests.All(request => request.ApiKey == "test-only-key"),
        "Every Kanbanize request must carry the configured API key.");

    using var createPayload = JsonDocument.Parse(handler.Requests[1].Body);
    var create = createPayload.RootElement;
    Assert(create.GetProperty("lane_id").GetInt32() == 28125 &&
           create.GetProperty("column_id").GetInt32() == 29373 &&
           create.GetProperty("custom_id").GetString() == "101",
        "A generated workplace card must retain the selected destination and source identity.");
    Assert(create.GetProperty("links_to_existing_cards_to_add_or_update")[0]
               .GetProperty("linked_card_id").GetInt32() == 101,
        "A generated workplace card must retain the parent link to its source card.");
    Assert(!create.TryGetProperty("actual_end_time", out _) &&
           !create.TryGetProperty("description", out _),
        "The synchronization must not add unrelated card fields when creating a workplace card.");

    using var patchPayload = JsonDocument.Parse(handler.Requests[2].Body);
    var patchFields = patchPayload.RootElement.EnumerateObject().Select(property => property.Name).ToArray();
    Assert(patchFields.SequenceEqual(new[] { "deadline" }, StringComparer.Ordinal) &&
           patchPayload.RootElement.GetProperty("deadline").GetString() == deadline.UtcDateTime.ToString("O"),
        "The deadline sync must PATCH only the deadline field of an existing target card.");
}

static async Task VerifyAdministrationIdentityAsync()
{
    var licenses = new MemoryLicenseService(new ViCoLicenseEntry(@"grob\user", "Level9", "memory"));
    var viewModel = new VIBN_Tools.Application.VM.ViCoAdministrationPageVM(
        licenses,
        new EmptyMeetingService(),
        new EmptyUpdateService(),
        new NoOpPathLauncher(),
        "user");
    await viewModel.InitializeAsync();

    Assert(viewModel.CurrentLevel == "Level9" && viewModel.CanManageLicenses,
        "A domain-qualified Level9 license should enable administration for the short Windows user.");
    Assert(licenses.SavedChanges.Any(change =>
            WindowsUserIdentity.Equals(change.UserName, "lutzma") &&
            string.Equals(change.Level, "Level9", StringComparison.OrdinalIgnoreCase)),
        "Opening administration must persist the mandatory lutzma Level9 assignment when possible.");
}

static async Task VerifyTiaLibraryWorkflowAsync(string temporaryRoot)
{
    var library = Path.Combine(temporaryRoot, "library");
    var programFolder = Path.Combine(library, "_Programm", "VICOBIB", "Nested");
    var typeFolder = Path.Combine(library, "_Datatype", "VICOBIB");
    Directory.CreateDirectory(programFolder);
    Directory.CreateDirectory(typeFolder);
    await File.WriteAllTextAsync(Path.Combine(programFolder, "FB.xml"), "block");
    await File.WriteAllTextAsync(Path.Combine(programFolder, "FB_IDB.xml"), "idb");
    await File.WriteAllTextAsync(Path.Combine(typeFolder, "Type.xml"), "type");

    var client = new FakeTiaBridgeClient();
    var service = new TiaLibraryService(client);
    await service.ImportAsync(library, configureAxes: true, "V18");

    Assert(client.Saved, "TIA library import should save the project.");
    Assert(client.ImportedBlocks.Count == 4, "TIA block and generated axis imports are incomplete.");
    Assert(client.ImportedBlocks[^1].File.EndsWith("FB_IDB.xml", StringComparison.OrdinalIgnoreCase),
        "TIA instance DB should be imported last.");
    Assert(client.ImportedDataTypes.Count == 1, "TIA data type import is incomplete.");

    client.Blocks.Items.Add(new TiaProgramItemInfo { Name = "FB", FolderPath = "VICOBIB/Nested" });
    client.DataTypes.Items.Add(new TiaProgramItemInfo { Name = "Type", FolderPath = "VICOBIB" });
    var exportPath = await service.ExportAsync("VICOBIB", Path.Combine(temporaryRoot, "export"), "V18");
    Assert(File.Exists(Path.Combine(exportPath, "_Programm", "VICOBIB", "Nested", "FB.xml")),
        "TIA block export structure is incorrect.");
    Assert(File.Exists(Path.Combine(exportPath, "_Datatype", "VICOBIB", "Type.xml")),
        "TIA data type export structure is incorrect.");
}

static async Task VerifyTypedTiaPipeProtocolAsync()
{
    var pipeName = $"vibn-tia-test-{Guid.NewGuid():N}";
    var serverTask = Task.Run(async () =>
    {
        await using var server = new NamedPipeServerStream(
            pipeName,
            PipeDirection.InOut,
            1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous);

        await server.WaitForConnectionAsync();
        using var reader = new StreamReader(server, leaveOpen: true);
        using var writer = new StreamWriter(server, leaveOpen: true) { AutoFlush = true };

        for (var requestIndex = 0; requestIndex < 2; requestIndex++)
        {
            var requestLine = await reader.ReadLineAsync();
            var request = JsonSerializer.Deserialize<TiaRequestEnvelope>(requestLine!);
            var expectedCommand = requestIndex == 0 ? TiaCommands.Ping : TiaCommands.Close;
            Assert(request?.Command == expectedCommand, $"Typed TIA pipe command '{expectedCommand}' was not received.");

            var response = new TiaResponseEnvelope
            {
                RequestId = request!.RequestId,
                Success = true,
                PayloadJson = JsonSerializer.Serialize(requestIndex == 0 ? "pong" : null)
            };

            await writer.WriteLineAsync(JsonSerializer.Serialize(response));
        }
    });

    var options = new TiaBridgeClientOptions(
        pipeName,
        ConnectTimeout: TimeSpan.FromSeconds(5),
        RequestTimeout: TimeSpan.FromSeconds(5));
    var client = new NamedPipeTiaBridgeClient(options);
    try
    {
        await client.ConnectAsync().WaitAsync(TimeSpan.FromSeconds(6));
        Assert(await client.PingAsync(), "Typed TIA pipe response failed.");
    }
    finally
    {
        await client.DisposeAsync();
    }

    await serverTask.WaitAsync(TimeSpan.FromSeconds(5));
}

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

sealed class SnapshotCatalog(params ViCoWorkstation[] workstations) : IViCoWorkstationCatalog
{
    public Task<ViCoWorkstationSnapshot> LoadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new ViCoWorkstationSnapshot(workstations, Array.Empty<string>()));
}

sealed record CapturedHttpRequest(HttpMethod Method, string RelativeUrl, string ApiKey, string Body);

/// <summary>
/// In-memory HTTP boundary for payload tests. It ensures the Kanbanize adapter
/// can be verified without any network call or mutation of a real board.
/// </summary>
sealed class RecordingHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new();

    public List<CapturedHttpRequest> Requests { get; } = new();

    public void EnqueueJson(string json) =>
        _responses.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var body = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken);
        var apiKey = request.Headers.TryGetValues("apikey", out var values)
            ? values.SingleOrDefault() ?? string.Empty
            : string.Empty;
        Requests.Add(new CapturedHttpRequest(
            request.Method,
            request.RequestUri?.PathAndQuery ?? string.Empty,
            apiKey,
            body));

        if (_responses.Count == 0)
            throw new InvalidOperationException("No mocked Kanbanize response was provided.");
        return _responses.Dequeue();
    }
}

sealed record DeadlineChange(int CardId, DateTimeOffset? Deadline);

sealed class MemoryKanbanizeCardService : IKanbanizeCardService
{
    private readonly List<KanbanizeCardInfo> _sourceCards;
    private readonly List<KanbanizeCardInfo> _targetCards;
    private int _nextCardId = 9000;

    public MemoryKanbanizeCardService(
        IEnumerable<KanbanizeCardInfo> sourceCards,
        IEnumerable<KanbanizeCardInfo> targetCards)
    {
        _sourceCards = sourceCards.ToList();
        _targetCards = targetCards.ToList();
    }

    public bool IsConfigured => true;

    public List<KanbanizeGeneratedCardDraft> GeneratedCards { get; } = new();

    public List<DeadlineChange> DeadlineChanges { get; } = new();

    public Task<IReadOnlyList<KanbanizeBoardInfo>> LoadBoardsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<KanbanizeBoardInfo>>(Array.Empty<KanbanizeBoardInfo>());

    public Task<KanbanizeBoardStructure> LoadBoardStructureAsync(int boardId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new KanbanizeBoardStructure(Array.Empty<KanbanizeLaneInfo>(), Array.Empty<KanbanizeColumnInfo>()));

    public Task<IReadOnlyList<KanbanizeCardInfo>> LoadCardsAsync(int boardId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<KanbanizeCardInfo>>(
            (boardId == 1392 ? _sourceCards : _targetCards).ToArray());

    public Task<KanbanizeCreatedCard> CreateCardAsync(KanbanizeCardDraft draft, CancellationToken cancellationToken = default) =>
        Task.FromResult(new KanbanizeCreatedCard(++_nextCardId, draft.Title));

    public Task<KanbanizeCreatedCard> CreateGeneratedCardAsync(
        KanbanizeGeneratedCardDraft draft,
        CancellationToken cancellationToken = default)
    {
        GeneratedCards.Add(draft);
        var created = new KanbanizeCardInfo(
            ++_nextCardId,
            1541,
            draft.TargetLaneId,
            draft.TargetColumnId,
            draft.Title,
            draft.SourceCardId.ToString(),
            draft.Deadline);
        _targetCards.Add(created);
        return Task.FromResult(new KanbanizeCreatedCard(created.Id, created.Title));
    }

    public Task UpdateDeadlineAsync(int cardId, DateTimeOffset? deadline, CancellationToken cancellationToken = default)
    {
        var index = _targetCards.FindIndex(card => card.Id == cardId);
        if (index < 0)
            throw new InvalidOperationException("Target card not found.");
        _targetCards[index] = _targetCards[index] with { Deadline = deadline };
        DeadlineChanges.Add(new DeadlineChange(cardId, deadline));
        return Task.CompletedTask;
    }
}

sealed class MemoryLicenseService : IViCoLicenseService
{
    private readonly ViCoLicenseEntry[] _entries;

    public MemoryLicenseService(params ViCoLicenseEntry[] entries)
    {
        _entries = entries;
    }

    public bool IsConfigured => true;

    public List<LicenseLevelChange> SavedChanges { get; } = new();

    public Task<IReadOnlyList<ViCoLicenseEntry>> LoadApprovedAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ViCoLicenseEntry>>(_entries);

    public Task<IReadOnlyList<ViCoLicenseEntry>> LoadRequestsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ViCoLicenseEntry>>(Array.Empty<ViCoLicenseEntry>());

    public Task RequestCurrentUserAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SetLevelAsync(string userName, string level, CancellationToken cancellationToken = default)
    {
        SavedChanges.Add(new LicenseLevelChange(userName, level));
        return Task.CompletedTask;
    }
}

sealed class EmptyMeetingService : IUpcomingMeetingService
{
    public Task<IReadOnlyList<UpcomingMeeting>> LoadTodayAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<UpcomingMeeting>>(Array.Empty<UpcomingMeeting>());
}

sealed class EmptyUpdateService : IViCoUpdateService
{
    public Task<ViCoUpdateInfo?> FindLatestAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<ViCoUpdateInfo?>(null);
}

sealed class NoOpPathLauncher : IExternalPathLauncher
{
    public void Open(string path)
    {
    }
}
