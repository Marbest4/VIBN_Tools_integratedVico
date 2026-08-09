using System.IO;
using System.Xml.Linq;
using VIBN_Tools.ContainerGeneration.Utils;

namespace VIBN_Tools.ContainerGeneration.BusinessLogic.RequirementsXml
{
    /// <summary>
    /// Class to represent a default AutoCreate XML file.
    /// </summary>
    public class RequirementsXml : IRequirementsXml
    {
        public string XmlSchema { get; private set; }

        /// <summary>
        /// Gets the XML document.
        /// </summary
        public XDocument Document { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the XML document is initialized.
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequirementsXml"/> class.
        /// </summary>
        public RequirementsXml()
        {
            XmlSchema = ResourceHandler.AUTOCREATE_SCHEMA;
            Document = new XDocument();
            IsInitialized = false;
        }

        public Result<XDocument> ReadFromFile(string filePath) => ReadFromFileInternal(filePath);
        public async Task<Result<XDocument>> ReadFromFileAsync(string filePath) => await Task.Run(() => ReadFromFileInternal(filePath));

        private Result<XDocument> ReadFromFileInternal(string filePath)
        {
            try
            {
                Result<XDocument> result = XmlHandler.Read(filePath);
                if (!result.IsSuccess)
                    return result;

                using Stream stream = ResourceHandler.GetEmbeddedResourceStream(ResourceHandler.AUTOCREATE_SCHEMA);
                if (!XmlHandler.Validate(result.Value, stream))
                    return Result<XDocument>.Failure("AutoCreate schema validation failed");

                Document = result.Value;
                IsInitialized = true;
                return Result<XDocument>.Success(Document);
            }
            catch (Exception ex)
            {
                return Result<XDocument>.Failure($"A unexpected failure occured: {ex.Message}");
            }
        }

        public int? GetMinSignals(string componentType)
        {
            var component = Document.Descendants("Component").FirstOrDefault(c => c.Attribute("type")?.Value == componentType);
            var minSignal = component?.Attribute("minSignals")?.Value.ToString();
            if (minSignal != null)
            {
                if (int.TryParse(minSignal, out int ConvertedInt))
                {
                    return ConvertedInt;
                }
            }
            return null;
        }

        public int? GetMaxSignals(string componentType)
        {
            var component = Document.Descendants("Component").FirstOrDefault(c => c.Attribute("type")?.Value == componentType);
            var maxSignal = component?.Attribute("maxSignals")?.Value.ToString();
            if (maxSignal != null)
            {
                if (int.TryParse(maxSignal, out int ConvertedInt))
                {
                    return ConvertedInt;
                }
            }
            return null;
        }

        public List<string> GetSlotNames(string componentType)
        {
            var component = Document.Descendants("Component").FirstOrDefault(c => c.Attribute("type")?.Value == componentType);
            var slots = component?.Descendants("Slot");
            return slots?
                .Select(slot => slot.Attribute("name")?.Value)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? [];
        }

        public List<string> GetComponentTypes()
        {
            return Document
                .Descendants("Component")
                .Select(component => component.Attribute("type")?.Value)
                .Where(type => !string.IsNullOrWhiteSpace(type))
                .Select(type => type!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

    }
}
