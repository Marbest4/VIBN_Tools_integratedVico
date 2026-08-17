using System.IO.Pipes;
using Newtonsoft.Json;
using VIBN_Tools.Tia.Contracts;

namespace VIBN_Tools.TiaBridge.Bridge;

public sealed class TiaBridgeServer
{
    private readonly string _pipeName;
    private readonly TiaCommandDispatcher _dispatcher;

    public TiaBridgeServer(string pipeName, TiaCommandDispatcher dispatcher)
    {
        _pipeName = string.IsNullOrWhiteSpace(pipeName)
            ? throw new ArgumentException("A pipe name is required.", nameof(pipeName))
            : pipeName;
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    public void Run()
    {
        Console.WriteLine($"TIA Bridge is listening on '{_pipeName}'.");

        while (true)
        {
            using (var server = new NamedPipeServerStream(
                       _pipeName,
                       PipeDirection.InOut,
                       1,
                       PipeTransmissionMode.Byte,
                       PipeOptions.None))
            {
                server.WaitForConnection();
                Console.WriteLine("TIA Bridge client connected.");

                using (var reader = new StreamReader(server))
                using (var writer = new StreamWriter(server) { AutoFlush = true })
                {
                    while (server.IsConnected)
                    {
                        var requestLine = reader.ReadLine();
                        if (requestLine == null)
                            break;

                        var shouldClose = HandleRequest(requestLine, writer);
                        if (shouldClose)
                            return;
                    }
                }
            }

            Console.WriteLine("TIA Bridge client disconnected.");
        }
    }

    private bool HandleRequest(string requestLine, TextWriter writer)
    {
        TiaRequestEnvelope? request = null;

        try
        {
            request = JsonConvert.DeserializeObject<TiaRequestEnvelope>(requestLine)
                ?? throw new InvalidDataException("Request envelope is empty.");

            var result = _dispatcher.Dispatch(request);
            writer.WriteLine(JsonConvert.SerializeObject(new TiaResponseEnvelope
            {
                RequestId = request.RequestId,
                Success = true,
                PayloadJson = JsonConvert.SerializeObject(result.Payload)
            }));

            return result.CloseBridge;
        }
        catch (Exception exception)
        {
            writer.WriteLine(JsonConvert.SerializeObject(new TiaResponseEnvelope
            {
                RequestId = request?.RequestId ?? string.Empty,
                Success = false,
                Error = new TiaError
                {
                    Code = MapErrorCode(exception),
                    Message = exception.Message,
                    Details = exception.ToString()
                }
            }));

            return false;
        }
    }

    private static string MapErrorCode(Exception exception)
    {
        if (exception is ArgumentException)
            return "request.invalid";
        if (exception is FileNotFoundException)
            return "file.not-found";
        if (exception is InvalidOperationException)
            return "session.invalid-state";

        return "bridge.unhandled";
    }
}
