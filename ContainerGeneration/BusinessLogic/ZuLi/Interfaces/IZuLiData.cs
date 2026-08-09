using VIBN_Tools.ContainerGeneration.Utils;

namespace VIBN_Tools.ContainerGeneration.BusinessLogic.ZuLiData.Interfaces
{
    /// <summary>
    /// Defines the interface for ZuLi data operations.
    /// </summary>
    /// <typeparam name="T">The type of container entry.</typeparam>
    public interface IZuLiData<T>
    {
        /// <summary>
        /// Gets the worksheet rule set used for reading data.
        /// </summary>
        public IWorksheetRuleSet worksheetRuleSet { get; }

        /// <summary>
        /// Gets the list of container entries.
        /// </summary>
        public List<T> Items { get; }

        // <summary>
        /// Reads ZuLi data from the specified file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>A result containing the list of container entries.</returns>
        public Result<List<T>> ReadFromFile(string path);

        /// <summary>
        /// Asynchronously reads ZuLi data from the specified file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>A task representing the asynchronous operation, with a result containing the list of container entries.</returns>
        public Task<Result<List<T>>> ReadFromFileAsync(string path);
    }
}
