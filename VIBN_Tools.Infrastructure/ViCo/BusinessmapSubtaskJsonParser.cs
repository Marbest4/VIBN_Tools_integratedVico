using System.Text.Json;

namespace VIBN_Tools.Infrastructure.ViCo;

/// <summary>
/// Normalizes the different Businessmap subtask response shapes used by card
/// expansion, the card-level endpoint and older server versions.
/// </summary>
internal static class BusinessmapSubtaskJsonParser
{
    internal sealed record Entry(int Id, string Description);

    public static IReadOnlyList<Entry> Parse(JsonElement root, bool isEndpointPayload)
    {
        var containers = FindContainers(root).ToList();
        if (containers.Count == 0 && isEndpointPayload)
            containers.Add(UnwrapData(root));

        var result = new List<Entry>();
        foreach (var container in containers)
            Collect(container, result, 0);

        return result
            .Where(entry => entry.Id > 0 && entry.Description.Length > 0)
            .GroupBy(entry => entry.Id)
            .Select(group => group.Last())
            .ToArray();
    }

    private static IEnumerable<JsonElement> FindContainers(JsonElement node)
    {
        if (node.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in node.EnumerateObject())
            {
                if (property.Name.Equals("subtasks", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("subtask_details", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("subtaskdetails", StringComparison.OrdinalIgnoreCase))
                {
                    yield return property.Value;
                    continue;
                }

                foreach (var nested in FindContainers(property.Value))
                    yield return nested;
            }
        }
        else if (node.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in node.EnumerateArray())
            {
                foreach (var nested in FindContainers(item))
                    yield return nested;
            }
        }
    }

    private static void Collect(JsonElement node, ICollection<Entry> result, int dictionaryKeyId)
    {
        if (node.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in node.EnumerateArray())
                Collect(item, result, 0);
            return;
        }
        if (node.ValueKind != JsonValueKind.Object)
            return;

        var id = ReadInt(node, "subtask_id", "id", "card_id");
        if (id <= 0)
            id = dictionaryKeyId;
        var description = ReadText(node);
        if (id > 0 && description.Length > 0)
            result.Add(new Entry(id, description));

        foreach (var property in node.EnumerateObject())
        {
            var propertyId = int.TryParse(property.Name, out var parsedId) ? parsedId : 0;
            Collect(property.Value, result, propertyId);
        }
    }

    private static string ReadText(JsonElement node)
    {
        foreach (var name in new[] { "description", "title", "name" })
        {
            if (!node.TryGetProperty(name, out var value))
                continue;
            if (value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString()))
                return value.GetString()!.Trim();
            if (value.ValueKind != JsonValueKind.Object)
                continue;
            foreach (var nestedName in new[] { "value", "text", "plain_text" })
            {
                if (value.TryGetProperty(nestedName, out var nested) &&
                    nested.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(nested.GetString()))
                {
                    return nested.GetString()!.Trim();
                }
            }
        }
        return string.Empty;
    }

    private static int ReadInt(JsonElement node, params string[] names)
    {
        foreach (var name in names)
        {
            if (!node.TryGetProperty(name, out var value))
                continue;
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
                return number;
            if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out number))
                return number;
        }
        return 0;
    }

    private static JsonElement UnwrapData(JsonElement node)
    {
        for (var depth = 0; depth < 5 && node.ValueKind == JsonValueKind.Object; depth++)
        {
            if (!node.TryGetProperty("data", out var nested))
                break;
            node = nested;
        }
        return node;
    }
}
