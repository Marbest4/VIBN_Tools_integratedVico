namespace VIBN_Tools.Core.ViCo;

public enum ApplicationLogLevel
{
    Information,
    Warning,
    Error
}

public sealed record ApplicationLogEntry(
    DateTimeOffset Timestamp,
    ApplicationLogLevel Level,
    string Area,
    string Message,
    string Details = "");

public interface IApplicationLog
{
    void Information(string area, string message);

    void Warning(string area, string message, string details = "");

    void Error(string area, string message, Exception exception);
}

public sealed class NullApplicationLog : IApplicationLog
{
    public static NullApplicationLog Instance { get; } = new();

    private NullApplicationLog()
    {
    }

    public void Information(string area, string message)
    {
    }

    public void Warning(string area, string message, string details = "")
    {
    }

    public void Error(string area, string message, Exception exception)
    {
    }
}
