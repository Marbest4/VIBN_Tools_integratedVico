using System.Xml.Linq;

namespace VIBN_Tools.ContainerGeneration.AI;

public static class ExportXmlParser
{
    public static IEnumerable<TrainingRow> Read(string exportXmlPath)
    {
        var doc = XDocument.Load(exportXmlPath);
        var containers = doc.Root?.Element("ContainerList")?.Elements("Container") ?? Enumerable.Empty<XElement>();
        foreach (var c in containers)
        {
            var component = c.Element("Component")?.Value?.Trim();
            var type = c.Element("Type")?.Value?.Trim();
            var entries = c.Element("DataList")?.Elements("Entry") ?? Enumerable.Empty<XElement>();
            foreach (var e in entries)
            {
                yield return new TrainingRow
                {
                    ComponentName = component,
                    ComponentType = type,
                    SignalId = e.Element("ID")?.Value?.Trim(),
                    Address = e.Element("Address")?.Value?.Trim(),
                    SignalText = e.Element("Signal")?.Value?.Trim(),
                    SlotName = e.Element("Slot")?.Value?.Trim(),
                };
            }
        }
    }
}

public sealed class TrainingRow
{
    public string SignalText { get; set; }
    public string SlotName { get; set; }          // Label
    public string ComponentType { get; set; }     // Feature (optional)
    public string ComponentName { get; set; }     // Feature (optional)
    public string SignalId { get; set; }          // Feature (optional)
    public string Address { get; set; }           // Feature (optional)
}