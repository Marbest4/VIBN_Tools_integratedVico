namespace VIBN_Tools.Tia.Contracts;

public sealed class EmptyPayload
{
    public static EmptyPayload Instance { get; } = new();

    private EmptyPayload()
    {
    }
}

public sealed class TiaVersionPayload
{
    public string Version { get; set; } = string.Empty;
}

public sealed class TiaPlcSelectionPayload
{
    public int PlcIndex { get; set; }
}

public sealed class TiaFolderPayload
{
    public string ParentPath { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}

public sealed class TiaTransferPayload
{
    public string FolderPath { get; set; } = string.Empty;

    public string ItemName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;
}

public sealed class TiaPlcInfo
{
    public int Index { get; set; }

    public string Name { get; set; } = string.Empty;

    public string TypeIdentifier { get; set; } = string.Empty;
}

public sealed class TiaProjectTree
{
    public List<TiaFolderInfo> Folders { get; set; } = new();

    public List<TiaProgramItemInfo> Items { get; set; } = new();
}

public sealed class TiaFolderInfo
{
    public string Name { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;
}

public sealed class TiaProgramItemInfo
{
    public string Name { get; set; } = string.Empty;

    public string FolderPath { get; set; } = string.Empty;
}

public sealed class TiaAxisInfo
{
    public string Name { get; set; } = string.Empty;

    public string TechnologyType { get; set; } = string.Empty;
}

/// <summary>
/// Read-only hardware/module information discovered through TIA Openness.
/// Addresses are byte offsets as reported by the configured input/output
/// address objects; a negative value means that the module has no address of
/// that IO type.
/// </summary>
public sealed class TiaHardwareModuleInfo
{
    public int DeviceIndex { get; set; }

    public int Slot { get; set; } = -1;

    public string DeviceName { get; set; } = string.Empty;

    public string ModuleName { get; set; } = string.Empty;

    public string ModuleType { get; set; } = string.Empty;

    public string TypeIdentifier { get; set; } = string.Empty;

    public string FirmwareVersion { get; set; } = string.Empty;

    public int InputStartByte { get; set; } = -1;

    public int InputLength { get; set; }

    public int InputEndByte => InputStartByte >= 0 && InputLength > 0
        ? InputStartByte + InputLength - 1
        : -1;

    public string InputAddressRange => FormatAddressRange(InputStartByte, InputEndByte);

    public int OutputStartByte { get; set; } = -1;

    public int OutputLength { get; set; }

    public int OutputEndByte => OutputStartByte >= 0 && OutputLength > 0
        ? OutputStartByte + OutputLength - 1
        : -1;

    public string OutputAddressRange => FormatAddressRange(OutputStartByte, OutputEndByte);

    private static string FormatAddressRange(int start, int end) => start < 0
        ? "—"
        : end > start ? $"{start}–{end}" : start.ToString();
}
