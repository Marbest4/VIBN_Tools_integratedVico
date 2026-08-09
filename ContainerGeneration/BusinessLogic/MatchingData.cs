using VIBN_Tools.ContainerGeneration.BusinessLogic.ContainerData;

namespace VIBN_Tools.ContainerGeneration.BusinessLogic
{
    /// <summary>
    /// Represents matching data with component and container information.
    /// </summary>
    /// <param name="componentName">The name of the component.</param>
    /// <param name="componentType">The type of the component.</param>
    /// <param name="containerName">The name of the container.</param>
    /// <param name="containerEntry">The container entry data.</param>
    /// <param name="keyData">The key data dictionary (from which the ContainerName was created).</param
    public class MatchingData(string componentName, string componentType, string containerName, int? minSignals, int? maxSignals, ContainerEntry containerEntry, Dictionary<string, bool> keyData)
    {
        /// <summary>
        /// Gets or sets the name of the component.
        /// </summary>
        public string ComponentName { get; set; } = componentName;

        /// <summary>
        /// Gets or sets the type of the component.
        /// </summary>
        public string ComponentType { get; set; } = componentType;

        /// <summary>
        /// Gets or sets the name of the container.
        /// </summary>
        public string ContainerName { get; set; } = containerName;

        /// <summary>
        /// Gets or sets the minimum number of signals.
        /// </summary>
        public int? MinSignals { get; set; } = minSignals;

        /// <summary>
        /// Gets or sets the max number of signals.
        /// </summary>
        public int? MaxSignals { get; set; } = maxSignals;

        /// <summary>
        /// Gets or sets the container entry.
        /// </summary>
        public ContainerEntry ContainerEntry { get; set; } = containerEntry;

        /// <summary>
        /// Gets or sets the key data dictionary.
        /// </summary>
        public Dictionary<string, bool> KeyData { get; set; } = keyData;
    }
}
