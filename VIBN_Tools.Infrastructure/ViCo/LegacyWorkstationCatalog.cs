using System.Text.RegularExpressions;
using VIBN_Tools.Core.ViCo;

namespace VIBN_Tools.Infrastructure.ViCo;

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
        var robotColumns = await ReadLinesAsync("AllRobyColumns.txt", warnings, cancellationToken);
        var combined = CombineLegacyCards(lanes, cards);
        return new ViCoWorkstationSnapshot(ParseWorkstations(combined, robotCards, robotColumns), warnings);
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
            combined.AddRange(matchingCards);
        }

        if (combined.Count > 0)
            combined.Add("NEW_Lane");
        return combined;
    }

    private static IReadOnlyList<ViCoWorkstation> ParseWorkstations(
        IReadOnlyList<string> combined,
        IReadOnlyList<string> robotCards,
        IReadOnlyList<string> robotColumns)
    {
        var result = new List<ViCoWorkstation>();
        for (var index = 0; index < combined.Count; index++)
        {
            if (!string.Equals(combined[index], "NEW_Lane", StringComparison.Ordinal) || index + 1 >= combined.Count)
                continue;

            var displayName = Clean(combined[++index]);
            var details = new List<string>();
            while (index + 1 < combined.Count &&
                   !string.Equals(combined[index + 1], "NEW_Lane", StringComparison.Ordinal))
            {
                details.Add(Clean(combined[++index]));
            }

            var pcName = ExtractPcName(displayName);
            var user = details
                .Select(ExtractUserName)
                .FirstOrDefault(value => value.Length > 0) ?? string.Empty;
            var tia = details.FirstOrDefault(value => value.Contains("TIA", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
            var fee = string.Join(" | ", details.Where(value => value.Contains("FEE", StringComparison.OrdinalIgnoreCase)));
            var hardware = details.FirstOrDefault(value => value.Contains("LAN", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
            var projects = details
                .Where(IsProjectCard)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            AddRobotInformation(details, projects, robotCards, robotColumns);

            result.Add(new ViCoWorkstation(
                displayName,
                pcName,
                user,
                tia,
                fee,
                hardware,
                projects,
                details));
        }

        return result;
    }

    private static void AddRobotInformation(
        ICollection<string> details,
        IReadOnlyCollection<string> projects,
        IReadOnlyList<string> robotCards,
        IReadOnlyList<string> robotColumns)
    {
        var columnNames = Enumerable.Range(0, robotColumns.Count / 2)
            .ToDictionary(
                index => Clean(robotColumns[index * 2]),
                index => Clean(robotColumns[index * 2 + 1]),
                StringComparer.OrdinalIgnoreCase);
        var robotNumber = 0;
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
            details.Add($"Robot {++robotNumber}: {columnName}");
        }
    }

    private static bool IsProjectCard(string value) =>
        (value.Contains("GM", StringComparison.OrdinalIgnoreCase) ||
         value.Contains("GU", StringComparison.OrdinalIgnoreCase)) &&
        !value.Contains("ZKDS", StringComparison.OrdinalIgnoreCase) &&
        !value.Contains("LAN", StringComparison.OrdinalIgnoreCase) &&
        !value.Contains("TIA", StringComparison.OrdinalIgnoreCase) &&
        !value.Contains("FEE", StringComparison.OrdinalIgnoreCase);

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
}

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
            .Where(workstation => mode == ViCoSearchMode.Workstation
                ? Normalize(workstation.DisplayName + workstation.PcName).Contains(normalized, StringComparison.Ordinal)
                : workstation.Projects.Any(project => Normalize(project).Contains(normalized, StringComparison.Ordinal)))
            .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string Normalize(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
