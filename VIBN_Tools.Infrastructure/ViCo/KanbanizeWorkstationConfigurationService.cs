using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using VIBN_Tools.Core.ViCo;

namespace VIBN_Tools.Infrastructure.ViCo;

/// <summary>
/// Narrow Kanbanize write adapter for workstation KONFIGURATION subtasks. It
/// cannot create, move, delete or rename cards and therefore keeps all board
/// workflow data outside the edit scope.
/// </summary>
public sealed class KanbanizeWorkstationConfigurationService : IViCoWorkstationConfigurationService
{
    private const string ApiBase = "https://grobgroup.kanbanize.com/api/v2";
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public KanbanizeWorkstationConfigurationService(HttpClient httpClient, string? apiKey)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _apiKey = apiKey?.Trim() ?? string.Empty;
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

        foreach (var field in fields.Where(field => field.CanSave))
        {
            var description = $"{field.Key}: {field.Value.Trim()}";
            using var request = new HttpRequestMessage(
                HttpMethod.Patch,
                $"{ApiBase}/cards/{configurationCardId}/subtasks/{field.SubtaskId}");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.TryAddWithoutValidation("apikey", _apiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(new Dictionary<string, string> { ["description"] = description }),
                Encoding.UTF8,
                "application/json");
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                continue;

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"KONFIGURATION-Unteraufgabe '{field.Key}' konnte nicht gespeichert werden " +
                $"({(int)response.StatusCode} {response.ReasonPhrase}). {error}".Trim());
        }
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Kanbanize API access is not configured.");
    }
}
