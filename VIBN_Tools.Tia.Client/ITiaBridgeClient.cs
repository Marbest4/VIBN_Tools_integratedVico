using VIBN_Tools.Tia.Contracts;

namespace VIBN_Tools.Tia.Client;

public interface ITiaBridgeClient : IAsyncDisposable
{
    bool IsConnected { get; }

    Task ConnectAsync(CancellationToken cancellationToken = default);

    Task<bool> PingAsync(CancellationToken cancellationToken = default);

    Task SelectVersionAsync(string version, CancellationToken cancellationToken = default);

    Task AttachAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TiaPlcInfo>> ListPlcsAsync(CancellationToken cancellationToken = default);

    Task SelectPlcAsync(int plcIndex, CancellationToken cancellationToken = default);

    /// <summary>Lists configured TIA device items and their input/output byte offsets.</summary>
    Task<IReadOnlyList<TiaHardwareModuleInfo>> ListHardwareAsync(CancellationToken cancellationToken = default);

    Task<TiaProjectTree> ListProgramBlocksAsync(CancellationToken cancellationToken = default);

    Task<TiaProjectTree> ListDataTypesAsync(CancellationToken cancellationToken = default);

    Task ImportBlockAsync(string folderPath, string filePath, CancellationToken cancellationToken = default);

    Task ExportBlockAsync(string folderPath, string blockName, string filePath, CancellationToken cancellationToken = default);

    Task ImportDataTypeAsync(string folderPath, string filePath, CancellationToken cancellationToken = default);

    Task ExportDataTypeAsync(string folderPath, string dataTypeName, string filePath, CancellationToken cancellationToken = default);

    Task CreateBlockFolderAsync(string parentPath, string name, CancellationToken cancellationToken = default);

    Task CreateDataTypeFolderAsync(string parentPath, string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TiaAxisInfo>> ConfigureAxesAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(CancellationToken cancellationToken = default);
}
