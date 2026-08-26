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
    Console.WriteLine("Running Level9 role policy smoke test...");
    VerifyRoleAdministrationPolicy();
    Console.WriteLine("Running Kanbanize card draft policy smoke test...");
    VerifyKanbanizeCardDraftPolicy();
    Console.WriteLine("Running idempotent VIBN workplace synchronization smoke test...");
    await VerifyVibnWorkplaceSynchronizationAsync();
    Console.WriteLine("Running narrow Kanbanize HTTP write-scope smoke test...");
    await VerifyKanbanizeHttpWriteScopeAsync();
    Console.WriteLine("Running workstation KONFIGURATION write-scope smoke test...");
    await VerifyWorkstationConfigurationWriteScopeAsync();
    Console.WriteLine("Running Kanbanize refresh/subtask API smoke test...");
    await VerifyKanbanizeRefreshApiAsync(temporaryRoot);
    Console.WriteLine("Running role store and update smoke test...");
    await VerifyRoleStoreAndUpdateAsync(temporaryRoot);
    Console.WriteLine("Running administration identity smoke test...");
    await VerifyAdministrationIdentityAsync();
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
    await File.WriteAllTextAsync(
        Path.Combine(cache, "WorkstationBoardCache.json"),
        JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            lanes = Array.Empty<object>(),
            cards = new[]
            {
                new
                {
                    id = 901,
                    laneId = "lane-1",
                    columnId = "column-config",
                    title = "KONFIGURATION",
                    subtasks = new[]
                    {
                        new { id = 911, description = "USER: zkds-config-priority" },
                        new { id = 912, description = "STANDORT: Werk 2" },
                        new { id = 913, description = "SW: TIA V18; Beckhoff TwinCAT 3" },
                        new { id = 914, description = "PROJEKT-IP: 10.20.30.40" },
                        new { id = 915, description = "SONSTIGES: Wartungsfenster Freitag" }
                    }
                }
            }
        }));

    var snapshot = await new LegacyWorkstationCatalog(cache).LoadAsync();
    Assert(snapshot.Workstations.Count == 1, "Legacy workstation catalog should contain one workstation.");
    var workstation = snapshot.Workstations[0];
    Assert(workstation.PcName == "GM12345", "Workstation name parsing failed.");
    Assert(workstation.UserName == "zkds-config-priority", "The KONFIGURATION USER must take precedence over older card text.");
    Assert(workstation.WorkstationConfiguration.CardId == 901 &&
           workstation.WorkstationConfiguration.ProjectIp.Value == "10.20.30.40" &&
           workstation.WorkstationConfiguration.Other.Value.Contains("Freitag", StringComparison.Ordinal),
        "KONFIGURATION fields and subtask identities were not retained for safe editing.");
    Assert(workstation.Status == "Belegt", "Active Kanbanize cards must mark the workstation as occupied.");
    Assert(workstation.Projects.Count == 1 && workstation.Projects[0].Contains("GM9000", StringComparison.Ordinal) &&
           workstation.Details.Any(card => card.Contains("FEE 5.0", StringComparison.Ordinal)),
        "The compact project column must contain only planning/working cards while full details remain available.");
    Assert(workstation.AutomationSoftware.Count == 2,
        "Software must be detected only from the KONFIGURATION/SW subtask, never from older lane cards.");
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

    var configuration = new ViCoWorkstationConfiguration(
        701,
        new ViCoConfigurationField("USER", "zkds-busy", 711),
        new ViCoConfigurationField("STANDORT", "Werk München", 712),
        new ViCoConfigurationField("SW", "TIA V20", 713),
        new ViCoConfigurationField("PROJEKT-IP", "10.25.30.40", 714),
        new ViCoConfigurationField("SONSTIGES", "Prüfplatz Nord", 715));
    var configured = occupied with
    {
        SoftwareInformation = "TIA Portal V20",
        Details = occupied.Details.Concat(new[] { "nur-in-kanbanize-details" }).ToArray(),
        Configuration = configuration
    };
    foreach (var visibleValue in new[]
             {
                 "GM10002", "GM2000/01-001", "TIA V20", "München", "10.25.30.40",
                 "Prüfplatz", "zkds-busy"
             })
    {
        Assert(search.Search(new[] { configured }, visibleValue, ViCoSearchMode.All).Count == 1,
            $"Overview search did not include visible field '{visibleValue}'.");
    }
    Assert(search.Search(new[] { configured }, "nur-in-kanbanize-details", ViCoSearchMode.All).Count == 0,
        "Overview search must ignore hidden Kanbanize details and diagnostic columns.");
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
        "The RDP profile must retain the automatic Windows credential behavior.");
    Assert(lines.Contains("selectedmonitors:s:0,2"),
        "Selected RDP monitors were not preserved.");
    Assert(!lines.Any(line => line.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                             line.StartsWith("pass:", StringComparison.OrdinalIgnoreCase)),
        "RDP profiles must never contain credential material.");

    var promptedLines = RemoteDesktopProfileBuilder.Build(
        "GM12345",
        string.Empty,
        new[] { 0 },
        1,
        promptForCredentials: true);
    Assert(promptedLines.Contains("prompt for credentials:i:1"),
        "The separate RDP button must open the Windows credential dialog.");
    Assert(!promptedLines.Any(line => line.StartsWith("username:s:", StringComparison.OrdinalIgnoreCase)),
        "The prompted RDP profile must not inject an automatic user name.");
}

static async Task VerifyRoleStoreAndUpdateAsync(string temporaryRoot)
{
    var rolesFile = Path.Combine(temporaryRoot, "roles", "roles.json");
    var roles = new JsonViCoUserRoleStore(rolesFile);
    await roles.SaveAsync(new[]
    {
        new ViCoUserRole(@"grob\lutzma", "Level9", "test"),
        new ViCoUserRole(@"grob\user", "Level8", "test"),
        new ViCoUserRole("admin-b", "Level9", "test")
    });
    var entries = await roles.LoadAsync();
    Assert(entries.Count == 3 && entries.Single(entry => entry.UserName == "user").Level == "Level8",
        "The license-free role store roundtrip failed.");
    Assert(WindowsUserIdentity.Equals(@"grob\user", "user"),
        "Domain-qualified and short Windows users should identify the same role.");

    var rejected = false;
    try
    {
        await roles.SaveAsync(new[] { new ViCoUserRole("lutzma", "Level9", "test") });
    }
    catch (InvalidOperationException)
    {
        rejected = true;
    }
    Assert(rejected, "The role store must reject a role set with only one Level9 administrator.");

    var version = Path.Combine(temporaryRoot, "versions", "V1.2.3", "publish");
    Directory.CreateDirectory(version);
    await File.WriteAllTextAsync(Path.Combine(version, "VICO_V2.exe"), string.Empty);
    var update = await new FileSystemViCoUpdateService(Path.Combine(temporaryRoot, "versions")).FindLatestAsync();
    Assert(update?.Version == "1.2.3", "ViCo update discovery failed.");
}

static void VerifyRoleAdministrationPolicy()
{
    var oneLevel9 = new[]
    {
        new ViCoUserRole(@"grob\lutzma", "Level9", "memory"),
        new ViCoUserRole("admin-b", "Level8", "memory")
    };
    var promotion = ViCoRolePolicy.PlanSave(oneLevel9.Select(role =>
        WindowsUserIdentity.Equals(role.UserName, "admin-b")
            ? role with { Level = "Level9" }
            : role));
    Assert(promotion.IsValid && promotion.Level9Users.Count == 2,
        "Promoting a second distinct Level9 user must be accepted.");

    var twoLevel9 = new[]
    {
        new ViCoUserRole(@"grob\lutzma", "Level9", "memory"),
        new ViCoUserRole("admin-a", "Level9", "memory"),
        new ViCoUserRole("admin-c", "Level8", "memory")
    };
    var unsafeDowngrade = ViCoRolePolicy.PlanSave(twoLevel9.Select(role =>
        WindowsUserIdentity.Equals(role.UserName, "admin-a")
            ? role with { Level = "Level8" }
            : role));
    Assert(!unsafeDowngrade.IsValid,
        "Downgrading to a single Level9 user must be rejected.");

    var safeReplacement = ViCoRolePolicy.PlanSave(twoLevel9.Select(role =>
        WindowsUserIdentity.Equals(role.UserName, "admin-a")
            ? role with { Level = "Level8" }
            : WindowsUserIdentity.Equals(role.UserName, "admin-c")
                ? role with { Level = "Level9" }
                : role));
    Assert(safeReplacement.IsValid && safeReplacement.Level9Users.Count == 2,
        "Replacing a Level9 user atomically must be accepted.");
    Assert(safeReplacement.Roles.Single(role => role.UserName == "admin-c").Level == "Level9",
        "The replacement promotion must be included in the atomically saved role set.");

    var duplicateIdentity = new[]
    {
        new ViCoUserRole(@"grob\lutzma", "Level9", "memory"),
        new ViCoUserRole("LUTZMA", "Level9", "memory")
    };
    var duplicatePlan = ViCoRolePolicy.PlanSave(duplicateIdentity);
    Assert(!duplicatePlan.IsValid,
        "Domain-qualified and short names of the same account must count only once.");

    Assert(ViCoRolePolicy.GetEffectiveLevel(@"grob\lutzma", null) == "Level9",
        "lutzma must be an effective Level9 administrator even before the compatible store is refreshed.");
    var mandatoryUserDowngrade = ViCoRolePolicy.PlanSave(twoLevel9.Select(role =>
        WindowsUserIdentity.Equals(role.UserName, "lutzma")
            ? role with { Level = "Level8" }
            : role));
    Assert(mandatoryUserDowngrade.IsValid &&
           mandatoryUserDowngrade.Roles.Single(role => role.UserName == "lutzma").Level == "Level9",
        "The mandatory lutzma Level9 assignment must remain Level9 in every saved role set.");
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
    var expectedStart = sourceDeadline.AddDays(-14);
    var expectedEnd = sourceDeadline.AddDays(56);
    var service = new MemoryKanbanizeCardService(
        new[]
        {
            new KanbanizeCardInfo(101, 1392, 10, 20, "[VIBN] Grundinbetriebnahme GM1000", null, sourceDeadline),
            new KanbanizeCardInfo(102, 1392, 10, 20, "[VIBN] Grundinbetriebnahme GM2000", null, sourceDeadline),
            new KanbanizeCardInfo(105, 1392, 10, 20, "[VIBN] Grundinbetriebnahme GM5000", null, sourceDeadline),
            new KanbanizeCardInfo(106, 1392, 10, 20, "[VIBN] Grundinbetriebnahme GM6000", null, sourceDeadline),
            new KanbanizeCardInfo(109, 1392, 10, 20, "[VIBN] Grundinbetriebnahme GM9000", null, sourceDeadline),
            new KanbanizeCardInfo(108, 1392, 10, 25236, "[VIBN] Grundinbetriebnahme Archiv", null, sourceDeadline)
        },
        new[]
        {
            new KanbanizeCardInfo(201, 1541, 28125, 29373, "Bestehende Karte", "102", sourceDeadline.AddDays(-3), expectedStart.AddDays(-1)),
            new KanbanizeCardInfo(205, 1541, 28125, 29373, "Bereits aktuell", "105", expectedEnd, expectedStart),
            new KanbanizeCardInfo(209, 1541, 28125, 29373, "*[Gen]* GM9000", null, expectedEnd, expectedStart),
            new KanbanizeCardInfo(206, 1541, 28125, 29373, "Doppelte Eins", "106", sourceDeadline),
            new KanbanizeCardInfo(207, 1541, 28125, 29373, "Doppelte Zwei", "106", sourceDeadline)
        });
    var synchronization = new VibnWorkplaceSynchronizationService(service);
    var settings = new VibnWorkplaceSynchronizationSettings(1392, 1541, 28125, 29373, 3, true);

    var preview = await synchronization.PreviewAsync(settings);
    Assert(preview.CreateCount == 1 && preview.DeadlineUpdateCount == 1 && preview.UnchangedCount == 2,
        "The preview must distinguish missing, stale and already-current target schedules.");
    Assert(preview.Items.Single(item => item.SourceCard.Id == 109).Action == VibnWorkplaceSynchronizationAction.Unchanged,
        "A legacy generated title must prevent duplicates even if its custom source ID is absent.");
    Assert(preview.ConflictCount == 1 && preview.ExcludedSourceCardCount == 1,
        "Duplicate target IDs must be reported and archived source cards excluded.");
    Assert(preview.Items.Where(item => item.SourceCard.Deadline is not null).All(item =>
            item.Schedule is null || item.Schedule.EndDate == item.SourceCard.Deadline!.Value.AddDays(56)),
        "Every VIBN card must derive its end date from its own deadline without requiring a template card.");

    var withoutDeadlineSync = await synchronization.PreviewAsync(settings with { SynchronizeDeadlines = false });
    Assert(withoutDeadlineSync.DeadlineUpdateCount == 0,
        "Schedule synchronization must be explicitly suppressible without affecting duplicate detection.");

    var createOnly = await synchronization.SynchronizeAsync(settings, new[] { 101 });
    Assert(createOnly.CreatedCount == 1 && createOnly.DeadlineUpdateCount == 0 &&
           service.ScheduleChanges.Count == 0,
        "Only explicitly selected preview rows may be synchronized.");
    Assert(service.GeneratedCards.Single().SourceCardId == 101 &&
           service.GeneratedCards.Single().Title == "*[Gen]* GM1000" &&
           service.GeneratedCards.Single().StartDate == expectedStart &&
           service.GeneratedCards.Single().Deadline == expectedEnd,
        "A generated card must preserve its identity and receive the calculated start/end schedule.");

    var deadlineOnly = await synchronization.SynchronizeAsync(settings, new[] { 102 });
    Assert(deadlineOnly.CreatedCount == 0 && deadlineOnly.DeadlineUpdateCount == 1 &&
           deadlineOnly.Failures.Count == 0,
        "The separately selected stale schedule should be adjusted without creating another card.");
    Assert(service.ScheduleChanges.SequenceEqual(new[] { new ScheduleChange(201, expectedStart, expectedEnd) }),
        "Only the existing generated card schedule may be changed; no other target field is updated.");

    var repeatPreview = await synchronization.PreviewAsync(settings);
    var repeatSelection = repeatPreview.Items
        .Where(item => item.Action is VibnWorkplaceSynchronizationAction.Create or VibnWorkplaceSynchronizationAction.UpdateDeadline)
        .Select(item => item.SourceCard.Id)
        .ToArray();
    Assert(repeatSelection.Length == 0,
        "A repeated preview must not expose already applied changes for selection.");
    var repeat = new VibnWorkplaceSynchronizationResult(repeatPreview, 0, 0, Array.Empty<string>());
    Assert(repeat.CreatedCount == 0 && repeat.DeadlineUpdateCount == 0,
        "A second synchronization must not create duplicates or repeat unchanged schedule updates.");
}

static async Task VerifyKanbanizeHttpWriteScopeAsync()
{
    var sourceDeadline = new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);
    var start = sourceDeadline.AddDays(-14);
    var end = sourceDeadline.AddDays(56);
    using var handler = new RecordingHttpMessageHandler();
    handler.EnqueueJson("""
        {"data":{"data":[{"card_id":101,"board_id":1392,"lane_id":10,"column_id":20,"title":"[VIBN] Grundinbetriebnahme GM1000","custom_id":null,"deadline":"2026-09-15T12:00:00.0000000Z","custom_fields":[{"field_id":508,"value":"2026-09-01T12:00:00.0000000Z"}]}],"pagination":{"all_pages":1}}}
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
        end,
        start));
    await api.UpdateGeneratedScheduleAsync(9001, start, end);

    Assert(cards.Count == 1 && string.IsNullOrEmpty(cards[0].CustomId) && cards[0].StartDate == sourceDeadline.AddDays(-14),
        "The card reader must preserve the source card identity and workplace start date.");
    Assert(handler.Requests.Count == 3, "The API adapter should make one read and two narrowly scoped writes.");
    Assert(handler.Requests[0].RelativeUrl.Contains("per_page=1000", StringComparison.Ordinal) &&
           handler.Requests[0].RelativeUrl.Contains("expand=custom_fields", StringComparison.Ordinal) &&
           handler.Requests[0].RelativeUrl.Contains("fields=card_id,title,custom_id,deadline", StringComparison.Ordinal) &&
           !handler.Requests[0].RelativeUrl.Contains("column_id", StringComparison.Ordinal),
        "The synchronization reader must explicitly request deadline using only Businessmap-valid fields.");
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
    Assert(create.GetProperty("deadline").GetString() == end.UtcDateTime.ToString("O") &&
           create.GetProperty("custom_fields_to_add_or_update")[0].GetProperty("field_id").GetInt32() == 508 &&
           create.GetProperty("custom_fields_to_add_or_update")[0].GetProperty("value").GetString() == start.UtcDateTime.ToString("O"),
        "The generated card must receive only the established workplace start field and calculated end deadline.");

    using var patchPayload = JsonDocument.Parse(handler.Requests[2].Body);
    var patchFields = patchPayload.RootElement.EnumerateObject().Select(property => property.Name).ToArray();
    Assert(patchFields.SequenceEqual(new[] { "deadline", "custom_fields_to_add_or_update" }, StringComparer.Ordinal) &&
           patchPayload.RootElement.GetProperty("deadline").GetString() == end.UtcDateTime.ToString("O") &&
           patchPayload.RootElement.GetProperty("custom_fields_to_add_or_update")[0].GetProperty("field_id").GetInt32() == 508 &&
           patchPayload.RootElement.GetProperty("custom_fields_to_add_or_update")[0].GetProperty("value").GetString() == start.UtcDateTime.ToString("O"),
        "The schedule sync must PATCH only the generated start field and deadline of an existing target card.");
}

static async Task VerifyWorkstationConfigurationWriteScopeAsync()
{
    using var handler = new RecordingHttpMessageHandler();
    handler.EnqueueJson("{\"data\":[]}");
    handler.EnqueueJson("{}");
    handler.EnqueueJson("{}");
    using var httpClient = new HttpClient(handler);
    var service = new KanbanizeWorkstationConfigurationService(httpClient, "test-only-key");

    await service.SaveFieldsAsync(
        710,
        new[]
        {
            new ViCoConfigurationField("USER", "zkds-simulation-p01", 711),
            new ViCoConfigurationField("SONSTIGES", "neu anlegen", 0)
        });

    Assert(handler.Requests.Count == 3,
        "Missing IDs must first be checked, then an existing subtask patched and a genuinely missing one created.");
    Assert(handler.Requests[0].Method == HttpMethod.Get &&
           handler.Requests[0].RelativeUrl == "/api/v2/cards/710/subtasks",
        "The idempotency check must read the current card-level subtasks before creating a missing key.");
    var request = handler.Requests[1];
    Assert(request.Method == HttpMethod.Patch &&
           request.RelativeUrl == "/api/v2/cards/710/subtasks/711" &&
           request.ApiKey == "test-only-key",
        "The configuration editor must PATCH exactly its selected subtask with the configured API key.");
    using var payload = JsonDocument.Parse(request.Body);
    var fields = payload.RootElement.EnumerateObject().Select(property => property.Name).ToArray();
    Assert(fields.SequenceEqual(new[] { "description" }, StringComparer.Ordinal) &&
           payload.RootElement.GetProperty("description").GetString() == "USER: zkds-simulation-p01",
        "The configuration editor must update only the existing subtask description.");
    var createRequest = handler.Requests[2];
    Assert(createRequest.Method == HttpMethod.Post &&
           createRequest.RelativeUrl == "/api/v2/cards/710/subtasks",
        "A missing standardized configuration subtask must use the card-level subtasks endpoint.");
    using var createPayload = JsonDocument.Parse(createRequest.Body);
    Assert(createPayload.RootElement.GetProperty("description").GetString() == "SONSTIGES: neu anlegen",
        "The missing standard subtask description is incorrect.");

    using var staleHandler = new RecordingHttpMessageHandler();
    staleHandler.EnqueueJson(
        "{\"data\":{\"subtask_details\":{\"799\":{\"title\":{\"text\":\"SONSTIGES: bereits vorhanden\"}}}}}");
    staleHandler.EnqueueJson("{}");
    using var staleClient = new HttpClient(staleHandler);
    var staleService = new KanbanizeWorkstationConfigurationService(staleClient, "test-only-key");
    await staleService.SaveFieldsAsync(
        710,
        new[] { new ViCoConfigurationField("SONSTIGES", "aktualisiert", 0) });
    Assert(staleHandler.Requests[1].Method == HttpMethod.Patch &&
           staleHandler.Requests[1].RelativeUrl == "/api/v2/cards/710/subtasks/799",
        "A stale local ID must not create a duplicate subtask when the key already exists remotely.");

    using var existingCardHandler = new RecordingHttpMessageHandler();
    existingCardHandler.EnqueueJson("{\"data\":{\"data\":[{\"card_id\":720,\"title\":\"Arbeitsplatz KONFIGURATION\"}]}}");
    existingCardHandler.EnqueueJson("{\"data\":[{\"card_id\":801,\"description\":\"USER: alt\"}]}");
    existingCardHandler.EnqueueJson("{}");
    using var existingCardClient = new HttpClient(existingCardHandler);
    var existingCardService = new KanbanizeWorkstationConfigurationService(existingCardClient, "test-only-key");
    var resolvedCardId = await existingCardService.CreateStandardAsync(
        28125,
        29373,
        new[] { new ViCoConfigurationField("USER", "neu", 0) });
    Assert(resolvedCardId == 720 &&
           existingCardHandler.Requests.All(request =>
               request.Method != HttpMethod.Post || request.RelativeUrl != "/api/v2/cards") &&
           existingCardHandler.Requests[^1].RelativeUrl == "/api/v2/cards/720/subtasks/801",
        "Create must reuse and update a live KONFIGURATION card instead of creating a duplicate.");
}

static async Task VerifyKanbanizeRefreshApiAsync(string temporaryRoot)
{
    var cacheRoot = Path.Combine(temporaryRoot, "kanbanize-refresh");
    using var handler = new KanbanizeRefreshHttpMessageHandler();
    using var client = new HttpClient(handler);
    await new KanbanizeRefreshService(client, "test-only-key", cacheRoot).RefreshAsync();

    Assert(handler.Requests.Any(url => url.StartsWith("/api/v2/cards?board_ids=1541", StringComparison.Ordinal)) &&
           !handler.Requests.Any(url => url.StartsWith("/api/v2/cards?", StringComparison.Ordinal) &&
                                       url.Contains("fields=", StringComparison.OrdinalIgnoreCase)),
        "The card query must omit the API instance's incompatible fields parameter.");
    Assert(handler.Requests.Any(url => url.Contains("expand=subtasks", StringComparison.OrdinalIgnoreCase)) &&
           handler.Requests.Contains("/api/v2/cards/501/subtasks", StringComparer.Ordinal),
        "The authoritative card-level endpoint must also be read for web-created KONFIGURATION subtasks.");

    using var cache = JsonDocument.Parse(await File.ReadAllTextAsync(
        Path.Combine(cacheRoot, "WorkstationBoardCache.json")));
    var cards = cache.RootElement.GetProperty("cards");
    Assert(cards.GetArrayLength() == 2,
        "All cards returned for the workstation lane must be retained in the structured cache.");
    var configuration = cards.EnumerateArray().Single(card => card.GetProperty("id").GetInt32() == 501);
    Assert(configuration.GetProperty("subtasks").GetArrayLength() == 2 &&
           configuration.GetProperty("subtasks").EnumerateArray().Any(subtask =>
               subtask.GetProperty("description").GetString() == "SW: TIA V20"),
        "Nested/dictionary KONFIGURATION subtasks from the direct card endpoint were not cached.");
}

static async Task VerifyAdministrationIdentityAsync()
{
    var roles = new MemoryRoleStore(
        new ViCoUserRole(@"grob\lutzma", "Level9", "memory"),
        new ViCoUserRole(@"grob\user", "Level9", "memory"));
    var viewModel = new VIBN_Tools.Application.VM.ViCoAdministrationPageVM(
        roles,
        new EmptyMeetingService(),
        new EmptyUpdateService(),
        new NoOpPathLauncher(),
        "user");
    await viewModel.InitializeAsync();

    Assert(viewModel.CurrentLevel == "Level9" && viewModel.CanManageUsers,
        "A domain-qualified Level9 role should enable role administration for the short Windows user.");
    Assert(viewModel.RoleEntries.Any(role =>
            WindowsUserIdentity.Equals(role.UserName, "lutzma") &&
            string.Equals(role.Level, "Level9", StringComparison.OrdinalIgnoreCase)),
        "The mandatory lutzma Level9 role must be present in the administration view.");
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

        for (var requestIndex = 0; requestIndex < 3; requestIndex++)
        {
            var requestLine = await reader.ReadLineAsync();
            var request = JsonSerializer.Deserialize<TiaRequestEnvelope>(requestLine!);
            var expectedCommand = requestIndex switch
            {
                0 => TiaCommands.Ping,
                1 => TiaCommands.ListHardware,
                _ => TiaCommands.Close
            };
            Assert(request?.Command == expectedCommand, $"Typed TIA pipe command '{expectedCommand}' was not received.");

            var response = new TiaResponseEnvelope
            {
                RequestId = request!.RequestId,
                Success = true,
                PayloadJson = requestIndex switch
                {
                    0 => JsonSerializer.Serialize("pong"),
                    1 => JsonSerializer.Serialize(new[]
                    {
                        new TiaHardwareModuleInfo
                        {
                            Slot = 2,
                            DeviceName = "PLC test",
                            ModuleName = "DI/DO test module",
                            ModuleType = "Digital IO",
                            FirmwareVersion = "V1.0",
                            InputStartByte = 8,
                            InputLength = 12,
                            OutputStartByte = 12,
                            OutputLength = 6
                        }
                    }),
                    _ => JsonSerializer.Serialize((object?)null)
                }
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
        var hardware = await client.ListHardwareAsync();
        Assert(hardware.Count == 1 && hardware[0].InputStartByte == 8 && hardware[0].OutputStartByte == 12 &&
               hardware[0].DeviceName == "PLC test" && hardware[0].ModuleType == "Digital IO" &&
               hardware[0].FirmwareVersion == "V1.0" &&
               hardware[0].InputAddressRange == "8–19" && hardware[0].OutputAddressRange == "12–17",
            "TIA hardware configuration must survive the typed pipe boundary.");
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

sealed class KanbanizeRefreshHttpMessageHandler : HttpMessageHandler
{
    public List<string> Requests { get; } = new();

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var url = request.RequestUri?.PathAndQuery ?? string.Empty;
        Requests.Add(url);
        var json = url switch
        {
            "/api/v2/boards/1541/lanes" => "{\"data\":[{\"lane_id\":28125,\"name\":\"GM12345 Tool PC\"}]}",
            var value when value.StartsWith("/api/v2/cards?board_ids=1541", StringComparison.Ordinal) =>
                "{\"data\":{\"data\":[{\"card_id\":501,\"lane_id\":28125,\"column_id\":29373,\"title\":\"Arbeitsplatz KONFIGURATION\",\"subtasks\":[{\"card_id\":601,\"description\":\"STANDORT: Werk 1\"}]},{\"card_id\":502,\"lane_id\":28125,\"column_id\":29375,\"title\":\"GM9000/01-001\"}],\"pagination\":{\"all_pages\":1}}}",
            "/api/v2/cards/501/subtasks" =>
                "{\"data\":{\"subtasks\":{\"601\":{\"subtask_id\":601,\"description\":\"STANDORT: Werk 1\"},\"602\":{\"description\":{\"text\":\"SW: TIA V20\"}}}}}",
            var value when value.StartsWith("/api/v2/cards?board_ids=846", StringComparison.Ordinal) =>
                "{\"data\":{\"data\":[],\"pagination\":{\"all_pages\":1}}}",
            var value when value.StartsWith("/api/v2/boards/846/columns", StringComparison.Ordinal) => "{\"data\":[]}",
            _ => throw new InvalidOperationException($"Unexpected Kanbanize refresh request: {url}")
        };
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });
    }
}

sealed record ScheduleChange(int CardId, DateTimeOffset StartDate, DateTimeOffset EndDate);

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

    public List<ScheduleChange> ScheduleChanges { get; } = new();

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
            draft.Deadline,
            draft.StartDate);
        _targetCards.Add(created);
        return Task.FromResult(new KanbanizeCreatedCard(created.Id, created.Title));
    }

    public Task UpdateDeadlineAsync(int cardId, DateTimeOffset? deadline, CancellationToken cancellationToken = default)
    {
        var index = _targetCards.FindIndex(card => card.Id == cardId);
        if (index < 0)
            throw new InvalidOperationException("Target card not found.");
        _targetCards[index] = _targetCards[index] with { Deadline = deadline };
        return Task.CompletedTask;
    }

    public Task UpdateGeneratedScheduleAsync(
        int cardId,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default)
    {
        var index = _targetCards.FindIndex(card => card.Id == cardId);
        if (index < 0)
            throw new InvalidOperationException("Target card not found.");

        _targetCards[index] = _targetCards[index] with
        {
            StartDate = startDate,
            Deadline = endDate
        };
        ScheduleChanges.Add(new ScheduleChange(cardId, startDate, endDate));
        return Task.CompletedTask;
    }
}

sealed class MemoryRoleStore : IViCoUserRoleStore
{
    private IReadOnlyList<ViCoUserRole> _roles;

    public MemoryRoleStore(params ViCoUserRole[] roles)
    {
        _roles = roles;
    }

    public bool IsConfigured => true;

    public Task<IReadOnlyList<ViCoUserRole>> LoadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_roles);

    public Task SaveAsync(IReadOnlyCollection<ViCoUserRole> roles, CancellationToken cancellationToken = default)
    {
        var plan = ViCoRolePolicy.PlanSave(roles);
        if (!plan.IsValid)
            throw new InvalidOperationException(plan.Message);
        _roles = plan.Roles;
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
