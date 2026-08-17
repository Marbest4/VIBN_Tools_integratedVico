namespace VIBN_Tools.Tia.Client;

public sealed record TiaBridgeClientOptions(
    string PipeName,
    string ServerName = ".",
    TimeSpan? ConnectTimeout = null,
    TimeSpan? RequestTimeout = null,
    string? BridgeExecutablePath = null)
{
    public TimeSpan EffectiveConnectTimeout => ConnectTimeout ?? TimeSpan.FromSeconds(5);

    public TimeSpan EffectiveRequestTimeout => RequestTimeout ?? TimeSpan.FromMinutes(2);
}
