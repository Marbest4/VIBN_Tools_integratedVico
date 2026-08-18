using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using NLog;
using VIBN_Tools.Core.ViCo;

namespace VIBN_Tools.Application;

public sealed class ApplicationLogService : IApplicationLog
{
    private const int MaximumVisibleEntries = 500;
    private static readonly Logger Logger = LogManager.GetLogger("VIBN_Tools.Diagnostics");

    public static ApplicationLogService Instance { get; } = new();

    private ApplicationLogService()
    {
    }

    public ObservableCollection<ApplicationLogEntry> Entries { get; } = new();

    public string LogDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GROB",
        "VIBN_Tools",
        "Logs");

    public void Information(string area, string message)
    {
        Logger.Info("[{Area}] {Message}", area, message);
        Add(new ApplicationLogEntry(DateTimeOffset.Now, ApplicationLogLevel.Information, area, message));
    }

    public void Warning(string area, string message, string details = "")
    {
        Logger.Warn("[{Area}] {Message} {Details}", area, message, details);
        Add(new ApplicationLogEntry(DateTimeOffset.Now, ApplicationLogLevel.Warning, area, message, details));
    }

    public void Error(string area, string message, Exception exception)
    {
        Logger.Error(exception, "[{Area}] {Message}", area, message);
        Add(new ApplicationLogEntry(
            DateTimeOffset.Now,
            ApplicationLogLevel.Error,
            area,
            message,
            exception.Message));
    }

    public void Clear()
    {
        RunOnUiThread(Entries.Clear);
    }

    private void Add(ApplicationLogEntry entry)
    {
        RunOnUiThread(() =>
        {
            Entries.Add(entry);
            while (Entries.Count > MaximumVisibleEntries)
                Entries.RemoveAt(0);
        });
    }

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
            action();
        else
            dispatcher.BeginInvoke(action);
    }
}
