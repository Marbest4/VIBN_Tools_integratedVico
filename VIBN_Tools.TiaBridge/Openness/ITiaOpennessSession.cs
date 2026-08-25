using VIBN_Tools.Tia.Contracts;

namespace VIBN_Tools.TiaBridge.Openness;

public interface ITiaOpennessSession : IDisposable
{
    void SelectVersion(string version);

    void Attach();

    IReadOnlyList<TiaPlcInfo> ListPlcs();

    void SelectPlc(int plcIndex);

    IReadOnlyList<TiaHardwareModuleInfo> ListHardware();

    TiaProjectTree ListProgramBlocks();

    TiaProjectTree ListDataTypes();

    void ImportBlock(TiaTransferPayload payload);

    void ExportBlock(TiaTransferPayload payload);

    void ImportDataType(TiaTransferPayload payload);

    void ExportDataType(TiaTransferPayload payload);

    void CreateBlockFolder(TiaFolderPayload payload);

    void CreateDataTypeFolder(TiaFolderPayload payload);

    IReadOnlyList<TiaAxisInfo> ConfigureAxes();

    void Save();
}
