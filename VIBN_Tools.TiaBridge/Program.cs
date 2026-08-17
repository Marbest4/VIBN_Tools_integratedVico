using VIBN_Tools.TiaBridge.Bridge;
using VIBN_Tools.TiaBridge.Openness;

namespace VIBN_Tools.TiaBridge;

internal static class Program
{
    private const string DefaultPipeName = "TIA_PIPE";

    private static int Main(string[] args)
    {
        var pipeName = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
            ? args[0]
            : DefaultPipeName;

        try
        {
            using (var session = new TiaOpennessSession())
            {
                var dispatcher = new TiaCommandDispatcher(session);
                var server = new TiaBridgeServer(pipeName, dispatcher);
                server.Run();
            }

            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}
