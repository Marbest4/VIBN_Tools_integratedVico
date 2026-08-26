using System.Text.RegularExpressions;
using System.Text.Json;
using VIBN_Tools.Core.ViCo;

namespace VIBN_Tools.Infrastructure.ViCo;

/// <summary>Parses the compatible Kanbanize cache files into neutral workstation models.</summary>
public sealed class LegacyWorkstationCatalog : IViCoWorkstationCatalog
{
    private readonly string _cacheRoot;

    public LegacyWorkstationCatalog(string cacheRoot)
    {
        _cacheRoot = cacheRoot;
    }

    public async Task<ViCoWorkstationSnapshot> LoadAsync(CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();
        var lanes = await ReadLinesAsync("AllPCLaneInfosWithChilds.txt", warnings, cancellationToken);
        var cards = await ReadLinesAsync("AllCardsOfPCsV2.txt", warnings, cancellationToken);
        var robotCards = await ReadLinesAsync("AllRobyCards.txt", warnings, cancellationToken);
        var robotNames = await ReadLinesAsync("AllRobyCardsRobyName.txt", warnings, cancellationToken);
        var robotColumns = await ReadLinesAsync("AllRobyColumns.txt", warnings, cancellationToken);
        var boardData = await ReadBoardDataAsync(warnings, cancellationToken);
        var combined = CombineLegacyCards(lanes, cards);
        return new ViCoWorkstationSnapshot(
            ParseWorkstations(
                combined,
                robotCards,
                robotNames,
                robotColumns,
                boardData.Configurations,
                boardData.ConfigurationColumns),
            warnings);
    }

    private async Task<IReadOnlyList<string>> ReadLinesAsync(
        string name,
        ICollection<string> warnings,
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(_cacheRoot, name);
        try
        {
            return File.Exists(path)
                ? await File.ReadAllLinesAsync(path, cancellationToken)
                : Array.Empty<string>();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            warnings.Add($"{path}: {exception.Message}");
            return Array.Empty<string>();
        }
    }

    private async Task<CachedBoardData> ReadBoardDataAsync(
        ICollection<string> warnings,
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(_cacheRoot, "WorkstationBoardCache.json");
        if (!File.Exists(path))
            return CachedBoardData.Empty;

        try
        {
            await using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite);
            var cache = await JsonSerializer.DeserializeAsync<WorkstationBoardCache>(
                stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken) ?? new WorkstationBoardCache();
            var configurations = cache.Cards
                .Where(card => IsConfigurationTitle(card.Title))
                .Where(card => card.Id > 0 && card.LaneId.Length > 0)
                .GroupBy(card => card.LaneId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => BuildConfiguration(group.OrderByDescending(card => card.Subtasks.Count).First()),
                    StringComparer.OrdinalIgnoreCase);
            var columns = cache.Cards
                .Where(card => card.Id > 0 && card.LaneId.Length > 0)
                .GroupBy(card => card.LaneId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    ResolveConfigurationColumn,
                    StringComparer.OrdinalIgnoreCase);
            return new CachedBoardData(configurations, columns);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            warnings.Add($"{path}: {exception.Message}");
            return CachedBoardData.Empty;
        }
    }

    private static int ResolveConfigurationColumn(IEnumerable<WorkstationCardCacheEntry> cards)
    {
        var columnIds = cards
            .Select(card => int.TryParse(card.ColumnId, out var columnId) ? columnId : 0)
            .Where(columnId => columnId > 0)
            .ToArray();
        if (columnIds.Any(columnId => columnId is >= 29368 and <= 29371))
            return 29368;
        if (columnIds.Any(columnId => columnId is >= 29373 and <= 29376))
            return 29373;
        return columnIds.FirstOrDefault();
    }

    private static IReadOnlyList<string> CombineLegacyCards(
        IReadOnlyList<string> lanes,
        IReadOnlyList<string> cards)
    {
        var combined = new List<string>();
        for (var laneIndex = 1; laneIndex < lanes.Count; laneIndex++)
        {
            var title = lanes[laneIndex];
            if (title.Contains("data", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Lane", StringComparison.OrdinalIgnoreCase) ||
                 (!title.Contains("GM", StringComparison.OrdinalIgnoreCase) &&
                  !title.Contains("GU", StringComparison.OrdinalIgnoreCase) &&
                  !title.Contains("Tool", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var laneId = lanes[laneIndex - 1];
            var matchingCards = new List<string>();
            for (var cardIndex = 1; cardIndex < cards.Count; cardIndex++)
            {
                if (string.Equals(cards[cardIndex], laneId, StringComparison.Ordinal))
                    matchingCards.Add(cards[cardIndex - 1]);
            }

            if (matchingCards.Count == 0)
                continue;

            combined.Add("NEW_Lane");
            combined.Add(title);
            combined.Add($"__lane-id__:{laneId}");
            combined.AddRange(matchingCards);
        }

        if (combined.Count > 0)
            combined.Add("NEW_Lane");
        return combined;
    }

    private static IReadOnlyList<ViCoWorkstation> ParseWorkstations(
        IReadOnlyList<string> combined,
        IReadOnlyList<string> robotCards,
        IReadOnlyList<string> robotNames,
        IReadOnlyList<string> robotColumns,
        IReadOnlyDictionary<string, ViCoWorkstationConfiguration> configurations,
        IReadOnlyDictionary<string, int> configurationColumns)
    {
        var result = new List<ViCoWorkstation>();
        for (var index = 0; index < combined.Count; index++)
        {
            if (!string.Equals(combined[index], "NEW_Lane", StringComparison.Ordinal) || index + 1 >= combined.Count)
                continue;

            var displayName = Clean(combined[++index]);
            var details = new List<string>();
            var laneId = string.Empty;
            while (index + 1 < combined.Count &&
                   !string.Equals(combined[index + 1], "NEW_Lane", StringComparison.Ordinal))
            {
                var detail = Clean(combined[++index]);
                if (detail.StartsWith("__lane-id__:", StringComparison.OrdinalIgnoreCase))
                {
                    laneId = detail["__lane-id__:".Length..].Trim();
                    continue;
                }
                details.Add(detail);
            }

            var pcName = ExtractPcName(displayName);
            var laneCards = details
                .Where(detail => !IsConfigurationCard(detail))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var configuration = configurations.TryGetValue(laneId, out var configured)
                ? configured
                : ViCoWorkstationConfiguration.Empty;
            foreach (var field in configuration.Fields.Where(field => !string.IsNullOrWhiteSpace(field.Value)))
                details.Add($"KONFIGURATION / {field.Key}: {field.Value}");

            var cardUser = ExtractUserName(configuration.User.Value);
            if (cardUser.Length == 0)
                cardUser = configuration.User.Value.Trim();
            var user = !string.IsNullOrWhiteSpace(cardUser)
                ? cardUser
                : details
                .Select(ExtractUserName)
                .FirstOrDefault(value => value.Length > 0) ?? string.Empty;
            var software = ParseSoftware(new[] { configuration.Software.Value });
            var softwareSummary = configuration.Software.Value.Trim();
            var fee = string.Join(" | ", details.Where(value => value.Contains("FEE", StringComparison.OrdinalIgnoreCase)));
            var hardware = details.FirstOrDefault(value => value.Contains("LAN", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
            // The compact project column is operational: only planning and
            // work in progress belong there. Backlog/done cards remain in
            // Details and are therefore still visible in the expanded view.
            var projects = laneCards
                .Where(card => ProjectIdentity.GetStatus(card) is "Planung" or "In Arbeit")
                .ToArray();
            var robots = FindRobotInformation(laneCards, robotCards, robotNames, robotColumns);
            foreach (var robot in robots)
                details.Add($"Robot: {robot.Name} – {robot.Status}");

            result.Add(new ViCoWorkstation(
                displayName,
                pcName,
                user,
                softwareSummary,
                fee,
                hardware,
                projects,
                details,
                software,
                robots,
                configuration,
                int.TryParse(laneId, out var numericLaneId) ? numericLaneId : 0,
                configurationColumns.TryGetValue(laneId, out var configurationColumnId)
                    ? configurationColumnId
                    : 0));
        }

        return result;
    }

    private static IReadOnlyList<ViCoRobotInfo> FindRobotInformation(
        IReadOnlyCollection<string> projects,
        IReadOnlyList<string> robotCards,
        IReadOnlyList<string> robotNames,
        IReadOnlyList<string> robotColumns)
    {
        var columnNames = Enumerable.Range(0, robotColumns.Count / 2)
            .ToDictionary(
                index => Clean(robotColumns[index * 2]),
                index => Clean(robotColumns[index * 2 + 1]),
                StringComparer.OrdinalIgnoreCase);
        var robots = new List<ViCoRobotInfo>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index + 1 < robotCards.Count; index += 2)
        {
            var projectKey = Clean(robotCards[index]);
            var normalizedRobotProject = ProjectIdentity.Normalize(
                projectKey.Replace("Software Robotik", string.Empty, StringComparison.OrdinalIgnoreCase));
            var robotMachineKey = ProjectIdentity.MachineKey(projectKey);
            if (!projects.Any(project =>
                    (normalizedRobotProject.Length > 0 &&
                     ProjectIdentity.Normalize(project).Contains(normalizedRobotProject, StringComparison.OrdinalIgnoreCase)) ||
                    (robotMachineKey.Length > 0 &&
                     string.Equals(ProjectIdentity.MachineKey(project), robotMachineKey, StringComparison.OrdinalIgnoreCase))))
                continue;
            var columnId = Clean(robotCards[index + 1]);
            var columnName = columnNames.TryGetValue(columnId, out var name) ? name : columnId;
            var robotName = index < robotNames.Count
                ? Clean(robotNames[index])
                : ExtractRobotName(projectKey);
            if (string.IsNullOrWhiteSpace(robotName) || robotName.All(char.IsDigit))
                robotName = ExtractRobotName(projectKey);
            if (string.IsNullOrWhiteSpace(robotName))
                robotName = $"Roboter {robots.Count + 1}";
            var identity = $"{ProjectIdentity.Normalize(projectKey)}|{ProjectIdentity.Normalize(robotName)}";
            if (seen.Add(identity))
                robots.Add(new ViCoRobotInfo(robotName, columnName, projectKey));
        }
        return robots;
    }

    private static IReadOnlyList<AutomationSoftwareInfo> ParseSoftware(IEnumerable<string> details)
    {
        var result = new List<AutomationSoftwareInfo>();
        foreach (var detail in details)
        {
            AddSoftware(result, detail, AutomationPlatform.SiemensTiaPortal,
                "TIA Portal", "TIA Portal", "TIA");
            AddSoftware(result, detail, AutomationPlatform.BeckhoffTwinCat,
                "Beckhoff TwinCAT", "Beckhoff", "TwinCAT");
            AddSoftware(result, detail, AutomationPlatform.RockwellStudio5000,
                "Rockwell Studio 5000", "Rockwell", "Studio 5000", "RSLogix");
        }

        return result
            .GroupBy(item => item.Platform)
            .Select(group => group.First())
            .ToArray();
    }

    private static void AddSoftware(
        ICollection<AutomationSoftwareInfo> target,
        string detail,
        AutomationPlatform platform,
        string name,
        params string[] markers)
    {
        if (!markers.Any(marker => detail.Contains(marker, StringComparison.OrdinalIgnoreCase)))
            return;
        var version = Regex.Match(detail, @"\b(?:V(?:ersion)?\s*)?\d{1,2}(?:\.\d+)?\b", RegexOptions.IgnoreCase);
        var displayName = version.Success ? $"{name} {version.Value.Trim()}" : name;
        var state = Regex.IsMatch(detail, @"\binstall(?:iert|ed|ation)\b", RegexOptions.IgnoreCase)
            ? SoftwareEvidenceState.Installed
            : SoftwareEvidenceState.Specified;
        target.Add(new AutomationSoftwareInfo(platform, displayName, detail, state));
    }

    private static string ExtractRobotName(string title)
    {
        var bracketValues = Regex.Matches(title, @"\[([^\]]+)\]")
            .Select(match => match.Groups[1].Value.Trim())
            .Where(value => value.Length > 0)
            .ToArray();
        if (bracketValues.Length > 1)
            return bracketValues[^1];
        var cleaned = title.Replace("Software Robotik", string.Empty, StringComparison.OrdinalIgnoreCase);
        return ProjectIdentity.CleanDisplay(cleaned).Trim(' ', '-', ':', '|');
    }

    private static bool IsConfigurationCard(string value)
    {
        var title = Regex.Replace(value, @"^\s*\[[BPWD]\]\s*", string.Empty);
        return IsConfigurationTitle(title);
    }

    private static bool IsConfigurationTitle(string value) =>
        string.Equals(value.Trim().TrimEnd(':'), "KONFIGURATION", StringComparison.OrdinalIgnoreCase);

    private static ViCoWorkstationConfiguration BuildConfiguration(WorkstationCardCacheEntry card)
    {
        return new ViCoWorkstationConfiguration(
            card.Id,
            FindConfigurationField(card.Subtasks, "USER"),
            FindConfigurationField(card.Subtasks, "STANDORT"),
            FindConfigurationField(card.Subtasks, "SW", "SOFTWARE"),
            FindConfigurationField(card.Subtasks, "PROJEKT-IP", "PROJEKTIP"),
            FindConfigurationField(card.Subtasks, "SONSTIGES"));
    }

    private static ViCoConfigurationField FindConfigurationField(
        IEnumerable<WorkstationSubtaskCacheEntry> subtasks,
        params string[] keys)
    {
        var match = subtasks.FirstOrDefault(subtask => keys.Any(key =>
            string.Equals(ExtractConfigurationKey(subtask.Description), key, StringComparison.OrdinalIgnoreCase)));
        var key = keys[0];
        return match is null
            ? new ViCoConfigurationField(key, string.Empty, 0)
            : new ViCoConfigurationField(key, ExtractConfigurationValue(match.Description), match.Id);
    }

    private static string ExtractConfigurationKey(string description)
    {
        var separator = description.IndexOf(':');
        var label = separator < 0 ? description : description[..separator];
        return label.Trim()
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();
    }

    private static string ExtractConfigurationValue(string description)
    {
        var separator = description.IndexOf(':');
        return separator < 0 ? string.Empty : description[(separator + 1)..].Trim();
    }

    private static string ExtractPcName(string value)
    {
        var match = Regex.Match(value, @"\b(?:GM|GU)[A-Z0-9]{4,8}\b", RegexOptions.IgnoreCase);
        return match.Success ? match.Value.ToUpperInvariant() : value.Trim();
    }

    internal static string ExtractUserName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var match = Regex.Match(
            value,
            @"(?<![A-Z0-9])ZKDS-[A-Z0-9._-]+",
            RegexOptions.IgnoreCase);
        if (match.Success)
            return match.Value.TrimEnd('.', ',', ';', ':').ToLowerInvariant();

        match = Regex.Match(
            value,
            @"(?<![A-Z0-9])ZKDS[ _-]+SIMULATION[ _-]+P\d{1,2}",
            RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return Regex.Replace(match.Value, "[ _]+", "-")
                .ToLowerInvariant();
        }

        var matches = Regex.Matches(
            value,
            @"(?<![A-Z0-9])ZK[A-Z0-9._-]{2,}",
            RegexOptions.IgnoreCase);
        return matches
            .Select(candidate => candidate.Value.TrimEnd('.', ',', ';', ':'))
            .Where(candidate => candidate.Length > 4)
            .OrderByDescending(candidate => candidate.Length)
            .Select(candidate => candidate.ToLowerInvariant())
            .FirstOrDefault() ?? string.Empty;
    }

    private static string Clean(string value) =>
        value.Replace("\\", string.Empty)
            .Replace("\"", string.Empty)
            .Replace("#Backlog#", "[B] ")
            .Replace("#Planning#", "[P] ")
            .Replace("#Working#", "[W] ")
            .Replace("#Done#", "[D] ")
            .Trim();

    private sealed record CachedBoardData(
        IReadOnlyDictionary<string, ViCoWorkstationConfiguration> Configurations,
        IReadOnlyDictionary<string, int> ConfigurationColumns)
    {
        public static CachedBoardData Empty { get; } = new(
            new Dictionary<string, ViCoWorkstationConfiguration>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase));
    }
}

/// <summary>Searches workstation data without exposing cache parsing details to the UI.</summary>
public sealed class ViCoWorkstationSearch : IViCoWorkstationSearch
{
    public IReadOnlyList<ViCoWorkstation> Search(
        IEnumerable<ViCoWorkstation> workstations,
        string query,
        ViCoSearchMode mode)
    {
        var normalized = Normalize(query);
        if (normalized.Length == 0)
            return workstations.OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase).ToArray();

        return workstations
            .Where(workstation => Matches(workstation, normalized, mode))
            .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool Matches(ViCoWorkstation workstation, string normalizedQuery, ViCoSearchMode mode)
    {
        if (mode == ViCoSearchMode.Project)
            return workstation.Projects.Any(project => Normalize(project).Contains(normalizedQuery, StringComparison.Ordinal));
        if (mode == ViCoSearchMode.Workstation)
            return Normalize(workstation.DisplayName + workstation.PcName + workstation.UserName)
                .Contains(normalizedQuery, StringComparison.Ordinal);

        // The single visible search intentionally covers every useful identifier
        // so operators do not need to decide up front whether a value is a PC,
        // project number or Kanbanize user.
        var searchable = string.Join(" ", new[]
        {
            workstation.DisplayName,
            workstation.PcName,
            workstation.UserName,
            string.Join(" ", workstation.Projects),
            string.Join(" ", workstation.Details)
        });
        return Normalize(searchable).Contains(normalizedQuery, StringComparison.Ordinal);
    }

    private static string Normalize(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
