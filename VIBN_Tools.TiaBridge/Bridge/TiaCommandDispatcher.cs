using Newtonsoft.Json;
using VIBN_Tools.Tia.Contracts;
using VIBN_Tools.TiaBridge.Openness;

namespace VIBN_Tools.TiaBridge.Bridge;

public sealed class TiaCommandDispatcher
{
    private readonly ITiaOpennessSession _session;

    public TiaCommandDispatcher(ITiaOpennessSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public TiaDispatchResult Dispatch(TiaRequestEnvelope request)
    {
        if (string.IsNullOrWhiteSpace(request.Command))
            throw new ArgumentException("Command is missing.", nameof(request));

        switch (request.Command)
        {
            case TiaCommands.Ping:
                return TiaDispatchResult.From("pong");

            case TiaCommands.SelectVersion:
                _session.SelectVersion(Read<TiaVersionPayload>(request).Version);
                return TiaDispatchResult.Empty();

            case TiaCommands.Attach:
                _session.Attach();
                return TiaDispatchResult.Empty();

            case TiaCommands.ListPlcs:
                return TiaDispatchResult.From(_session.ListPlcs());

            case TiaCommands.SelectPlc:
                _session.SelectPlc(Read<TiaPlcSelectionPayload>(request).PlcIndex);
                return TiaDispatchResult.Empty();

            case TiaCommands.ListHardware:
                return TiaDispatchResult.From(_session.ListHardware());

            case TiaCommands.ListProgramBlocks:
                return TiaDispatchResult.From(_session.ListProgramBlocks());

            case TiaCommands.ListDataTypes:
                return TiaDispatchResult.From(_session.ListDataTypes());

            case TiaCommands.ImportBlock:
                _session.ImportBlock(Read<TiaTransferPayload>(request));
                return TiaDispatchResult.Empty();

            case TiaCommands.ExportBlock:
                _session.ExportBlock(Read<TiaTransferPayload>(request));
                return TiaDispatchResult.Empty();

            case TiaCommands.ImportDataType:
                _session.ImportDataType(Read<TiaTransferPayload>(request));
                return TiaDispatchResult.Empty();

            case TiaCommands.ExportDataType:
                _session.ExportDataType(Read<TiaTransferPayload>(request));
                return TiaDispatchResult.Empty();

            case TiaCommands.CreateBlockFolder:
                _session.CreateBlockFolder(Read<TiaFolderPayload>(request));
                return TiaDispatchResult.Empty();

            case TiaCommands.CreateDataTypeFolder:
                _session.CreateDataTypeFolder(Read<TiaFolderPayload>(request));
                return TiaDispatchResult.Empty();

            case TiaCommands.ConfigureAxes:
                return TiaDispatchResult.From(_session.ConfigureAxes());

            case TiaCommands.Save:
                _session.Save();
                return TiaDispatchResult.Empty();

            case TiaCommands.Close:
                return TiaDispatchResult.Close();

            default:
                throw new ArgumentException($"Unknown TIA command: {request.Command}");
        }
    }

    private static T Read<T>(TiaRequestEnvelope request)
    {
        return JsonConvert.DeserializeObject<T>(request.PayloadJson)
            ?? throw new ArgumentException($"Payload for '{request.Command}' is invalid.");
    }
}

public sealed class TiaDispatchResult
{
    private TiaDispatchResult(object? payload, bool closeBridge)
    {
        Payload = payload;
        CloseBridge = closeBridge;
    }

    public object? Payload { get; }

    public bool CloseBridge { get; }

    public static TiaDispatchResult From(object? payload) => new(payload, false);

    public static TiaDispatchResult Empty() => new(null, false);

    public static TiaDispatchResult Close() => new(null, true);
}
