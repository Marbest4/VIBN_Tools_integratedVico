using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using VIBN_Tools.Core.Kanbanize;

namespace VIBN_Tools.Infrastructure.Kanbanize;

/// <summary>
/// HTTP adapter for the Businessmap/Kanbanize v2 card endpoints. It deliberately
/// contains no license model: the feature only reads board positions and creates cards.
/// </summary>
public sealed class KanbanizeCardApiService : IKanbanizeCardService
{
    private const string DefaultApiBaseUrl = "https://grobgroup.kanbanize.com/api/v2";
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _apiBaseUrl;

    public KanbanizeCardApiService(HttpClient httpClient, string? apiKey, string? apiBaseUrl = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _apiKey = apiKey?.Trim() ?? string.Empty;
        _apiBaseUrl = (apiBaseUrl ?? DefaultApiBaseUrl).TrimEnd('/');
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<IReadOnlyList<KanbanizeBoardInfo>> LoadBoardsAsync(CancellationToken cancellationToken = default)
    {
        using var document = await GetJsonAsync("/boards", cancellationToken);
        return GetDataElements(document.RootElement)
            .Select(element => new KanbanizeBoardInfo(
                ReadInt(element, "board_id", "id"),
                ReadString(element, "name"),
                ReadString(element, "description")))
            .Where(board => board.Id > 0 && board.Name.Length > 0)
            .OrderBy(board => board.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<KanbanizeBoardStructure> LoadBoardStructureAsync(
        int boardId,
        CancellationToken cancellationToken = default)
    {
        if (boardId <= 0)
            throw new ArgumentOutOfRangeException(nameof(boardId));

        var lanesTask = GetJsonAsync($"/boards/{boardId}/lanes", cancellationToken);
        var columnsTask = GetJsonAsync($"/boards/{boardId}/columns", cancellationToken);
        await Task.WhenAll(lanesTask, columnsTask);
        using var lanesDocument = await lanesTask;
        using var columnsDocument = await columnsTask;

        var lanes = GetDataElements(lanesDocument.RootElement)
            .Select(element => new KanbanizeLaneInfo(
                ReadInt(element, "lane_id", "id"),
                ReadInt(element, "workflow_id"),
                ReadString(element, "name")))
            .Where(lane => lane.Id > 0)
            .OrderBy(lane => lane.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var columns = GetDataElements(columnsDocument.RootElement)
            .Select(element => new KanbanizeColumnInfo(
                ReadInt(element, "column_id", "id"),
                ReadInt(element, "workflow_id"),
                ReadString(element, "name")))
            .Where(column => column.Id > 0)
            .OrderBy(column => column.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new KanbanizeBoardStructure(lanes, columns);
    }

    public async Task<KanbanizeCreatedCard> CreateCardAsync(
        KanbanizeCardDraft draft,
        CancellationToken cancellationToken = default)
    {
        var validationError = KanbanizeCardDraftPolicy.Validate(draft);
        if (validationError is not null)
            throw new ArgumentException(validationError, nameof(draft));
        EnsureConfigured();

        var payload = new Dictionary<string, object?>
        {
            ["lane_id"] = draft.LaneId,
            ["column_id"] = draft.ColumnId,
            ["title"] = draft.Title.Trim(),
            ["description"] = draft.Description?.Trim() ?? string.Empty,
            ["priority"] = draft.Priority
        };
        if (!string.IsNullOrWhiteSpace(draft.CustomId))
            payload["custom_id"] = draft.CustomId.Trim();
        if (draft.Deadline is not null)
            payload["deadline"] = draft.Deadline.Value.UtcDateTime.ToString("O");

        using var request = CreateRequest(HttpMethod.Post, "/cards");
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(responseText))
            return new KanbanizeCreatedCard(0, draft.Title.Trim());

        using var document = JsonDocument.Parse(responseText);
        var card = GetPayloadObject(document.RootElement);
        var cardId = ReadInt(card, "card_id", "id");
        return new KanbanizeCreatedCard(cardId, ReadString(card, "title", draft.Title.Trim()));
    }

    private async Task<JsonDocument> GetJsonAsync(string relativeUrl, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        using var request = CreateRequest(HttpMethod.Get, relativeUrl);
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativeUrl)
    {
        var request = new HttpRequestMessage(method, _apiBaseUrl + relativeUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.TryAddWithoutValidation("apikey", _apiKey);
        return request;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        var detail = ExtractErrorDetail(responseText);
        throw new HttpRequestException(
            $"Kanbanize API returned {(int)response.StatusCode} ({response.ReasonPhrase})." +
            (detail.Length == 0 ? string.Empty : $" {detail}"));
    }

    private static IEnumerable<JsonElement> GetDataElements(JsonElement root)
    {
        var data = root;
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var directData))
            data = directData;
        if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("data", out var nestedData))
            data = nestedData;

        return data.ValueKind == JsonValueKind.Array
            ? data.EnumerateArray().ToArray()
            : Array.Empty<JsonElement>();
    }

    private static JsonElement GetPayloadObject(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var data))
            return data;
        return root;
    }

    private static int ReadInt(JsonElement element, params string[] names)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return 0;
        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var value))
                continue;
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
                return number;
            if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out number))
                return number;
        }
        return 0;
    }

    private static string ReadString(JsonElement element, string name, string fallback = "") =>
        element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null
            ? value.ToString().Trim()
            : fallback;

    private static string ExtractErrorDetail(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
            return string.Empty;
        try
        {
            using var document = JsonDocument.Parse(responseText);
            var payload = GetPayloadObject(document.RootElement);
            if (payload.ValueKind != JsonValueKind.Object)
                return responseText.Trim()[..Math.Min(300, responseText.Trim().Length)];
            foreach (var name in new[] { "message", "error", "errors" })
            {
                if (payload.TryGetProperty(name, out var value))
                    return value.ToString().Trim()[..Math.Min(300, value.ToString().Trim().Length)];
            }
        }
        catch (JsonException)
        {
            // A non-JSON proxy response still yields a useful HTTP status above.
        }
        return responseText.Trim()[..Math.Min(300, responseText.Trim().Length)];
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Kanbanize API access is not configured.");
    }
}
