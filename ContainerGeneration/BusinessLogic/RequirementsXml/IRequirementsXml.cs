using System.Xml.Linq;
using VIBN_Tools.ContainerGeneration.Utils;

namespace VIBN_Tools.ContainerGeneration.BusinessLogic.RequirementsXml
{
    /// <summary>
    /// Defines the interface for AutoCreateXml modules.
    /// </summary>
    public interface IRequirementsXml
    {
        /// <summary>
        /// Gets the XML schema used for validation.
        /// </summary>
        public string XmlSchema { get; }

        /// <summary>
        /// Gets the XML document.
        /// </summary
        public XDocument Document { get; }

        /// <summary>
        /// Gets a value indicating whether the XML document is initialized.
        /// </summary>
        public bool IsInitialized { get; }

        /// <summary>
        /// Reads an AutoCreate XML document from the specified file.
        /// </summary>
        /// <param name="filePath">The path to the file.</param>
        /// <returns>A result containing the AutoCreate XML document.</returns>
        public Result<XDocument> ReadFromFile(string filePath);

        /// <summary>
        /// Asynchronously reads an AutoCreate XML document from the specified file.
        /// </summary>
        /// <param name="filePath">The path to the file.</param>
        /// <returns>A task representing the asynchronous operation, with a result containing the AutoCreate XML document.</returns>
        public Task<Result<XDocument>> ReadFromFileAsync(string filePath);

        /// <summary>
        /// Gets the minimum number of signals for the specified component type.
        /// </summary>
        /// <param name="componentType">The type of the component.</param>
        /// <returns>A list of slot names.</returns>
        public int? GetMinSignals(string componentName);

        /// <summary>
        /// Gets the maximum number of signals for the specified component type.
        /// </summary>
        /// <param name="componentType">The type of the component.</param>
        /// <returns>A list of slot names.</returns>
        public int? GetMaxSignals(string componentName);

        /// <summary>
        /// Gets the slot names for the specified component type.
        /// </summary>
        /// <param name="componentType">The type of the component.</param>
        /// <returns>A list of slot names.</returns>
        public List<string> GetSlotNames(string componentName);

        /// <summary>
        /// Gets the component types in the AutoCreate XML document.
        /// </summary>
        /// <returns>A list of component types.</returns>
        public List<string> GetComponentTypes();
    }
}
