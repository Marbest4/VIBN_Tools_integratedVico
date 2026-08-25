namespace VIBN_Tools.Tia.Contracts;

public static class TiaCommands
{
    public const string Ping = "system.ping";
    public const string SelectVersion = "session.select-version";
    public const string Attach = "session.attach";
    public const string ListPlcs = "project.list-plcs";
    public const string SelectPlc = "project.select-plc";
    public const string ListHardware = "project.list-hardware";
    public const string ListProgramBlocks = "program.list-blocks";
    public const string ListDataTypes = "program.list-data-types";
    public const string ImportBlock = "program.import-block";
    public const string ExportBlock = "program.export-block";
    public const string ImportDataType = "program.import-data-type";
    public const string ExportDataType = "program.export-data-type";
    public const string CreateBlockFolder = "program.create-block-folder";
    public const string CreateDataTypeFolder = "program.create-data-type-folder";
    public const string ConfigureAxes = "technology.configure-axes";
    public const string Save = "project.save";
    public const string Close = "system.close";
}

public sealed class TiaRequestEnvelope
{
    public string RequestId { get; set; } = string.Empty;

    public string Command { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = "{}";
}

public sealed class TiaResponseEnvelope
{
    public string RequestId { get; set; } = string.Empty;

    public bool Success { get; set; }

    public string PayloadJson { get; set; } = "null";

    public TiaError? Error { get; set; }
}

public sealed class TiaError
{
    public string Code { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? Details { get; set; }
}
