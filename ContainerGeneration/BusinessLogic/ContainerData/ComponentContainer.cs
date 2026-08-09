using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace VIBN_Tools.ContainerGeneration.BusinessLogic.ContainerData
{
    /// <summary>
    /// Represents a component container. Marks properties relevant for XML export using serialization.
    /// </summary>
    public class ComponentContainer
    {
        /// <summary>
        /// Gets or sets the ID (currently not used). Serializes to an attribute.
        /// </summary>
        [XmlAttribute("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the container name. Serializes to an element.
        /// </summary>
        [XmlElement("Component")]
        public string Component { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type. Serializes to an element.
        /// </summary>
        [XmlElement("Type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the max signals. Serializes to an attribute.
        /// </summary>
        [XmlIgnore]
        public int? MaxSignals { get; set; } = null;

        /// <summary>
        /// Gets or sets the min signals. Serializes to an attribute.
        /// </summary>
        [XmlIgnore]
        public int? MinSignals { get; set; } = null;

        /// <summary>
        /// Gets or sets a list of corresponding <see cref="ContainerEntry"/>. Serializes to an Array. 
        /// </summary>
        [XmlArray("DataList")]
        [XmlArrayItem("Entry")]
        public ObservableCollection<ContainerEntry> DataList { get; set; } = new ObservableCollection<ContainerEntry>();
    }
}
