using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using VIBN_Tools.Core.ViCo;

namespace VIBN_Tools.Infrastructure.ViCo;

/// <summary>
/// Focused Kanbanize adapter for one standardized KONFIGURATION card. Existing
/// subtasks are patched; missing standard subtasks are created. A missing card
/// is created only after the user explicitly presses the create button.
/// </summary>
public sealed class KanbanizeWorkstationConfigurationService : IViCoWorkstationConfigurationService
{
    private const string DefaultApiBase = "https://grobgroup.kanbanize.com/api/v2";
    private const int WorkplaceBoardId = 1541;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _apiBase;

    public KanbanizeWorkstationConfigurationService(
        HttpClient httpClient,
        string? apiKey,
        string? apiBase = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _apiKey = apiKey?.Trim() ?? string.Empty;
        _apiBase = (apiBase ?? DefaultApiBase).TrimEnd('/');
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task SaveFieldsAsync(
        int configurationCardId,
        IReadOnlyCollection<ViCoConfigurationField> fields,
        CancellationToken cancellationToken = default)
    {
        if (configurationCardId <= 0)
            throw new ArgumentOutOfRangeException(nameof(configurationCardId));
        ArgumentNullException.ThrowIfNull(fields);
        EnsureConfigured();

        // Always read the live subtask list before a write. Cache IDs may be
        // stale and older KONFIGURATION cards can use aliases such as
        // SOFTWARE or PROJEKTIP. Resolving them here makes saving idempotent.
        var existingSubtasks = await LoadStandardSubtasksAsync(configurationCardId, cancellationToken);
        foreach (var field in fields)
        {
            var description = $"{field.Key}: {field.Value.Trim()}";
            var liveSubtaskId = existingSubtasks.GetValueOrDefault(NormalizeConfigurationKey(field.Key));
            var subtaskId = liveSubtaskId > 0 ? liveSubtaskId : field.SubtaskId;
            var method = subtaskId > 0 ? HttpMethod.Patch : HttpMethod.Post;
            var relativeUrl = subtaskId > 0
                ? $"/cards/{configurationCardId}/subtasks/{subtaskId}"
                : $"/cards/{configurationCardId}/subtasks";
            var response = await SendJsonAsync(
                method,
                relativeUrl,
                new Dictionary<string, string> { ["description"] = description },
                $"KONFIGURATION-Unteraufgabe '{field.Key}' konnte nicht gespeichert werden",
                cancellationToken);
            if (subtaskId <= 0)
            {
                var createdId = ReadCreatedSubtaskId(response);
                if (createdId > 0)
                    existingSubtasks[field.Key] = createdId;
            }
        }
    }

    public async Task<int> CreateStandardAsync(
        int laneId,
        int columnId,
        IReadOnlyCollection<ViCoConfigurationField> fields,
        CancellationToken cancellationToken = default)
    {
        if (laneId <= 0)
            throw new ArgumentOutOfRangeException(nameof(laneId));
        if (columnId <= 0)
            throw new ArgumentOutOfRangeException(nameof(columnId));
        ArgumentNullException.ThrowIfNull(fields);
        EnsureConfigured();

        // The UI cache is deliberately only a projection. Recheck the live
        // lane immediately before POST so an older or differently expanded
        // KONFIGURATION card can never be duplicated.
        var existingCardId = await FindConfigurationCardAsync(laneId, cancellationToken);
        if (existingCardId > 0)
        {
            await SaveFieldsAsync(existingCardId, fields, cancellationToken);
            return existingCardId;
        }

        var responseBody = await SendJsonAsync(
            HttpMethod.Post,
            "/cards",
            new Dictionary<string, object?>
            {
                ["lane_id"] = laneId,
                ["column_id"] = columnId,
                ["title"] = "KONFIGURATION",
                ["description"] = "Standardisierte Arbeitsplatz-Konfiguration für VIBN Tools."
            },
            "KONFIGURATION-Karte konnte nicht angelegt werden",
            cancellationToken);
        var cardId = ReadCreatedCardId(responseBody);
        if (cardId <= 0)
            throw new InvalidDataException("Kanbanize hat nach dem Anlegen keine Karten-ID zurückgegeben.");

        await SaveFieldsAsync(cardId, fields, cancellationToken);
        return cardId;
    }

    private async Task<string> SendJsonAsync(
        HttpMethod method,
        string relativeUrl,
        object payload,
        string failureMessage,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, _apiBase + relativeUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.TryAddWithoutValidation("apikey", _apiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode)
            return body;

        var authenticationHint = response.StatusCode is System.Net.HttpStatusCode.Unauthorized or
            System.Net.HttpStatusCode.Forbidden
            ? " API-Key und Board-Berechtigung prüfen."
            : string.Empty;
        throw new HttpRequestException(
            $"{failureMessage} ({(int)response.StatusCode} {response.ReasonPhrase}).{authenticationHint} {body}".Trim());
    }

    private async Task<Dictionary<string, int>> LoadStandardSubtasksAsync(
        int cardId,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Get, $"/cards/{cardId}/subtasks");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw CreateRequestException("KONFIGURATION-Unteraufgaben konnten nicht geprüft werden", response, body);
        if (string.IsNullOrWhiteSpace(body))
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        using var document = JsonDocument.Parse(body);
        var data = UnwrapData(document.RootElement);
        if (data.ValueKind != JsonValueKind.Array)
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        return data.EnumerateArray()
            .Select(subtask => new
            {
                Id = ReadInt(subtask, "subtask_id", "id", "card_id"),
                Key = ReadConfigurationKey(subtask)
            })
            .Where(subtask => subtask.Id > 0 && subtask.Key.Length > 0)
            .GroupBy(subtask => subtask.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Id, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<int> FindConfigurationCardAsync(int laneId, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(
            HttpMethod.Get,
            $"/cards?board_ids={WorkplaceBoardId}&lane_ids={laneId}&per_page=100&fields=card_id,title");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw CreateRequestException("Vorhandene KONFIGURATION-Karte konnte nicht geprüft werden", response, body);
        if (string.IsNullOrWhiteSpace(body))
            return 0;

        using var document = JsonDocument.Parse(body);
        var data = UnwrapData(document.RootElement);
        if (data.ValueKind != JsonValueKind.Array)
            return 0;

        return data.EnumerateArray()
            .Where(card => ReadTextProperty(card, "title").Contains(
                "KONFIGURATION",
                StringComparison.OrdinalIgnoreCase))
            .Select(card => ReadInt(card, "card_id", "id"))
            .FirstOrDefault(id => id > 0);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativeUrl)
    {
        var request = new HttpRequestMessage(method, _apiBase + relativeUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.TryAddWithoutValidation("apikey", _apiKey);
        return request;
    }

    private static string ReadConfigurationKey(JsonElement subtask)
    {
        var description = ReadSubtaskText(subtask);
        if (description.Length == 0)
            return string.Empty;
        var separator = description.IndexOf(':');
        var key = separator < 0 ? description : description[..separator];
        return NormalizeConfigurationKey(key);
    }

    private static string NormalizeConfigurationKey(string key)
    {
        var normalized = key.Trim().Replace("_", string.Empty).Replace(" ", string.Empty).ToUpperInvariant();
        return normalized switch
        {
            "SOFTWARE" => "SW",
            "PROJEKTIP" => "PROJEKT-IP",
            _ => normalized
        };
    }

    private static string ReadSubtaskText(JsonElement subtask)
    {
        foreach (var propertyName in new[] { "description", "title", "name" })
        {
            var value = ReadTextProperty(subtask, propertyName);
            if (value.Length > 0)
                return value;
        }
        return string.Empty;
    }

    private static string ReadTextProperty(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value) ||
            value.ValueKind != JsonValueKind.String)
        {
            return string.Empty;
        }
        return value.GetString()?.Trim() ?? string.Empty;
    }

    private static int ReadCreatedSubtaskId(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
            return 0;
        using var document = JsonDocument.Parse(responseBody);
        return ReadInt(UnwrapData(document.RootElement), "subtask_id", "id");
    }

    private static JsonElement UnwrapData(JsonElement element)
    {
        while (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("data", out var nested))
            element = nested;
        return element;
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

    private static HttpRequestException CreateRequestException(
        string message,
        HttpResponseMessage response,
        string body)
    {
        var authenticationHint = response.StatusCode is System.Net.HttpStatusCode.Unauthorized or
            System.Net.HttpStatusCode.Forbidden
            ? " API-Key und Board-Berechtigung prüfen."
            : string.Empty;
        return new HttpRequestException(
            $"{message} ({(int)response.StatusCode} {response.ReasonPhrase}).{authenticationHint} {body}".Trim());
    }

    private static int ReadCreatedCardId(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
            return 0;
        using var document = JsonDocument.Parse(responseBody);
        var payload = document.RootElement;
        while (payload.ValueKind == JsonValueKind.Object &&
               payload.TryGetProperty("data", out var nested))
        {
            payload = nested;
        }
        if (payload.ValueKind != JsonValueKind.Object)
            return 0;
        foreach (var name in new[] { "card_id", "id" })
        {
            if (!payload.TryGetProperty(name, out var value))
                continue;
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
                return number;
            if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out number))
                return number;
        }
        return 0;
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
            throw new InvalidOperationException(
                "Kanbanize API access is not configured. VIBN_VICO_KANBANIZE_API_KEY setzen.");
    }
}
