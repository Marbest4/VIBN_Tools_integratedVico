using System.Xml.Serialization;

namespace VIBN_Tools.ContainerGeneration.BusinessLogic.ContainerData
{
    /// <summary>
    /// Represents the root structure for serializing a component containers as CAAMergeResult.
    /// </summary>
    [XmlRoot("CAAMergeResult")]
    public class CAAMergeResult
    {
        /// <summary>
        /// Gets or sets the version (e.g. program version). Serializes to an attribute.
        /// </summary>
        [XmlAttribute("version")]
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the creation time. Serializes to an attribute.
        /// </summary>
        [XmlAttribute("createdAt")]
        public string CreatedAt { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the AutoCreate XML file name. Serializes to an attribute.
        /// </summary>
        [XmlAttribute("autoCreateFile")]
        public string AutoCreateFile { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ZuLi Excel file name. Serializes to an attribute.
        /// </summary>
        [XmlAttribute("zuli")]
        public string Zuli { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a list of corresponding <see cref="ComponentContainer"/>. Serializes to an Array. 
        /// </summary>
        [XmlArray("ContainerList")]
        [XmlArrayItem("Container")]
        public required List<ComponentContainer> ContainerList { get; set; }
    }
}
