namespace VIBN_Tools.Tia.Client;

public sealed class TiaBridgeException : Exception
{
    public TiaBridgeException(string code, string message, string? details = null)
        : base(message)
    {
        Code = code;
        Details = details;
    }

    public string Code { get; }

    public string? Details { get; }
}
