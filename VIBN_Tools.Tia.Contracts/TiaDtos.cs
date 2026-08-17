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
