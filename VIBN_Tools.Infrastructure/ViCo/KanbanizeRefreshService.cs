using System.Net.Http.Headers;
using System.Text.Json;
using VIBN_Tools.Core.ViCo;

namespace VIBN_Tools.Infrastructure.ViCo;

public sealed class KanbanizeRefreshService : IViCoOnlineRefreshService
{
    private const string ApiBase = "https://grobgroup.kanbanize.com/api/v2";
    private static readonly JsonSerializerOptions CacheJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _cacheRoot;

    public KanbanizeRefreshService(HttpClient httpClient, string? apiKey, string cacheRoot)
    {
        _httpClient = httpClient;
        _apiKey = apiKey ?? string.Empty;
        _cacheRoot = cacheRoot;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Kanbanize API access is not configured.");

        var lanesTask = GetJsonAsync("/boards/1541/lanes", cancellationToken);
        var cardsTask = GetJsonAsync(
            "/cards?board_ids=1541&per_page=1000&fields=card_id,lane_id,column_id,title,subtasks",
            cancellationToken);
        var robotCardsTask = GetJsonAsync("/cards?board_ids=846&per_page=1000&fields=card_id,title,column_id", cancellationToken);
        var robotColumnsTask = GetJsonAsync("/boards/846/columns?fields=column_id,name", cancellationToken);
        await Task.WhenAll(lanesTask, cardsTask, robotCardsTask, robotColumnsTask);
        using var lanes = await lanesTask;
        using var cards = await cardsTask;
        using var robotCards = await robotCardsTask;
        using var robotColumns = await robotColumnsTask;

        var laneLines = new List<string>();
        var structuredLanes = new List<WorkstationLaneCacheEntry>();
        foreach (var lane in EnumerateObjects(lanes.RootElement))
        {
            if (!TryGetScalar(lane, "lane_id", out var id) || !TryGetScalar(lane, "name", out var name))
                continue;
            laneLines.Add(id);
            laneLines.Add(name);
            structuredLanes.Add(new WorkstationLaneCacheEntry { Id = id, Name = name });
        }

        var cardLines = new List<string>();
        foreach (var card in EnumerateObjects(cards.RootElement))
        {
            if (!TryGetScalar(card, "lane_id", out var laneId) || !TryGetScalar(card, "title", out var title))
                continue;
            var status = TryGetScalar(card, "column_id", out var columnId) ? MapStatus(columnId) : string.Empty;
            cardLines.Add(status + title);
            cardLines.Add(laneId);
        }

        Directory.CreateDirectory(_cacheRoot);
        await WriteAtomicallyAsync(
            Path.Combine(_cacheRoot, "AllPCLaneInfosWithChilds.txt"),
            laneLines,
            cancellationToken);
        await WriteAtomicallyAsync(
            Path.Combine(_cacheRoot, "AllCardsOfPCsV2.txt"),
            cardLines,
            cancellationToken);
        await WriteJsonAtomicallyAsync(
            Path.Combine(_cacheRoot, "WorkstationBoardCache.json"),
            new WorkstationBoardCache
            {
                Lanes = structuredLanes
                    .GroupBy(lane => lane.Id, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .ToList(),
                Cards = GetCardEntries(cards.RootElement)
            },
            cancellationToken);

        var robotCardLines = new List<string>();
        var robotNameLines = new List<string>();
        var knownRobotCards = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var card in EnumerateObjects(robotCards.RootElement))
        {
            if (!TryGetScalar(card, "title", out var title) ||
                !title.Contains("Software Robotik", StringComparison.OrdinalIgnoreCase) ||
                !TryGetScalar(card, "column_id", out var columnId))
            {
                continue;
            }
            var identity = TryGetScalar(card, "card_id", out var cardId) ? cardId : $"{title}|{columnId}";
            if (!knownRobotCards.Add(identity))
                continue;
            robotCardLines.Add(title);
            robotCardLines.Add(columnId);
            robotNameLines.Add(ExtractRobotName(title));
            robotNameLines.Add(columnId);
        }

        var robotColumnLines = new List<string>();
        foreach (var column in EnumerateObjects(robotColumns.RootElement))
        {
            if (!TryGetScalar(column, "column_id", out var columnId) ||
                !TryGetScalar(column, "name", out var name))
            {
                continue;
            }
            robotColumnLines.Add(columnId);
            robotColumnLines.Add(name);
        }

        await WriteAtomicallyAsync(Path.Combine(_cacheRoot, "AllRobyCards.txt"), robotCardLines, cancellationToken);
        await WriteAtomicallyAsync(Path.Combine(_cacheRoot, "AllRobyCardsRobyName.txt"), robotNameLines, cancellationToken);
        await WriteAtomicallyAsync(Path.Combine(_cacheRoot, "AllRobyColumns.txt"), robotColumnLines, cancellationToken);
    }

    private async Task<JsonDocument> GetJsonAsync(string relativeUrl, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ApiBase + relativeUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.TryAddWithoutValidation("apikey", _apiKey);
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    private static IEnumerable<JsonElement> EnumerateObjects(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            yield return root;
            foreach (var property in root.EnumerateObject())
            {
                foreach (var child in EnumerateObjects(property.Value))
                    yield return child;
            }
        }
        else if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                foreach (var child in EnumerateObjects(item))
                    yield return child;
            }
        }
    }

    /// <summary>
    /// Reads only actual board cards (rather than recursively walking every
    /// JSON object) so configuration subtasks retain their parent card ID and
    /// can later be edited without touching any other card field.
    /// </summary>
    private static List<WorkstationCardCacheEntry> GetCardEntries(JsonElement root)
    {
        var data = root;
        if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("data", out var nested))
            data = nested;
        if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("data", out nested))
            data = nested;
        if (data.ValueKind != JsonValueKind.Array)
            return new List<WorkstationCardCacheEntry>();

        return data.EnumerateArray()
            .Select(card => new WorkstationCardCacheEntry
            {
                Id = TryGetInt(card, "card_id", "id"),
                LaneId = TryGetScalar(card, "lane_id", out var laneId) ? laneId : string.Empty,
                ColumnId = TryGetScalar(card, "column_id", out var columnId) ? columnId : string.Empty,
                Title = TryGetScalar(card, "title", out var title) ? title : string.Empty,
                Subtasks = GetSubtasks(card)
            })
            .Where(card => card.Id > 0 && card.LaneId.Length > 0)
            .GroupBy(card => card.Id)
            .Select(group => group.First())
            .ToList();
    }

    private static List<WorkstationSubtaskCacheEntry> GetSubtasks(JsonElement card)
    {
        if (card.ValueKind != JsonValueKind.Object ||
            !card.TryGetProperty("subtasks", out var subtasks) ||
            subtasks.ValueKind != JsonValueKind.Array)
        {
            return new List<WorkstationSubtaskCacheEntry>();
        }

        return subtasks.EnumerateArray()
            .Where(subtask => subtask.ValueKind == JsonValueKind.Object)
            .Select(subtask => new WorkstationSubtaskCacheEntry
            {
                Id = TryGetInt(subtask, "subtask_id", "id"),
                Description = TryGetScalar(subtask, "description", out var description)
                    ? description
                    : string.Empty
            })
            .Where(subtask => subtask.Id > 0 && subtask.Description.Length > 0)
            .ToList();
    }

    private static bool TryGetScalar(JsonElement value, string name, out string result)
    {
        result = string.Empty;
        if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(name, out var property))
            return false;
        if (property.ValueKind is JsonValueKind.Object or JsonValueKind.Array or JsonValueKind.Null)
            return false;
        result = property.ToString();
        return true;
    }

    private static int TryGetInt(JsonElement value, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGetScalar(value, name, out var raw) || !int.TryParse(raw, out var number))
                continue;
            return number;
        }
        return 0;
    }

    private static string MapStatus(string columnId) => columnId switch
    {
        "29373" or "29368" => "#Backlog#",
        "29374" or "29369" => "#Planning#",
        "29375" or "29370" => "#Working#",
        "29376" or "29371" => "#Done#",
        _ => string.Empty
    };

    private static string ExtractRobotName(string title)
    {
        var values = System.Text.RegularExpressions.Regex.Matches(title, @"\[([^\]]+)\]")
            .Select(match => match.Groups[1].Value.Trim())
            .Where(value => value.Length > 0)
            .ToArray();
        if (values.Length > 1)
            return values[^1];
        return title.Replace("Software Robotik", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim(' ', '-', ':', '|');
    }

    private static async Task WriteAtomicallyAsync(
        string destination,
        IEnumerable<string> lines,
        CancellationToken cancellationToken)
    {
        var temporary = destination + ".tmp";
        await File.WriteAllLinesAsync(temporary, lines, cancellationToken);
        File.Move(temporary, destination, overwrite: true);
    }

    private static async Task WriteJsonAtomicallyAsync(
        string destination,
        WorkstationBoardCache value,
        CancellationToken cancellationToken)
    {
        var temporary = destination + ".tmp";
        await using (var stream = new FileStream(
            temporary,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 16 * 1024,
            useAsync: true))
        {
            await JsonSerializer.SerializeAsync(stream, value, CacheJsonOptions, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }
        File.Move(temporary, destination, overwrite: true);
    }
}
