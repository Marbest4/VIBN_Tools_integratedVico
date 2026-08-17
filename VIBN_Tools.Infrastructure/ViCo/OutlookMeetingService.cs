using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using VIBN_Tools.Core.ViCo;

namespace VIBN_Tools.Infrastructure.ViCo;

[SupportedOSPlatform("windows")]
public sealed class OutlookMeetingService : IUpcomingMeetingService
{
    public Task<IReadOnlyList<UpcomingMeeting>> LoadTodayAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run<IReadOnlyList<UpcomingMeeting>>(() => LoadOnSta(cancellationToken), cancellationToken);
    }

    private static IReadOnlyList<UpcomingMeeting> LoadOnSta(CancellationToken cancellationToken)
    {
        IReadOnlyList<UpcomingMeeting>? result = null;
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                result = ReadMeetings(cancellationToken);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null)
            throw failure;
        return result ?? Array.Empty<UpcomingMeeting>();
    }

    private static IReadOnlyList<UpcomingMeeting> ReadMeetings(CancellationToken cancellationToken)
    {
        var outlookType = Type.GetTypeFromProgID("Outlook.Application")
            ?? throw new InvalidOperationException("Microsoft Outlook is not installed.");
        dynamic? application = null;
        dynamic? session = null;
        dynamic? calendar = null;
        dynamic? items = null;
        try
        {
            application = Activator.CreateInstance(outlookType)
                ?? throw new InvalidOperationException("Microsoft Outlook could not be started.");
            session = application.GetNamespace("MAPI");
            session.Logon("", "", false, false);
            calendar = session.GetDefaultFolder(9);
            items = calendar.Items;
            items.IncludeRecurrences = true;
            items.Sort("[Start]");

            var start = DateTime.Today;
            var end = start.AddDays(1);
            var now = DateTime.Now;
            var meetings = new List<UpcomingMeeting>();
            foreach (dynamic item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                DateTime itemStart = item.Start;
                if (itemStart >= end)
                    break;
                if (itemStart >= now && itemStart >= start)
                    meetings.Add(new UpcomingMeeting(Convert.ToString(item.Subject) ?? string.Empty, itemStart, (DateTime)item.End));
                Release(item);
            }
            return meetings;
        }
        finally
        {
            try { session?.Logoff(); } catch { }
            Release(items);
            Release(calendar);
            Release(session);
            Release(application);
        }
    }

    private static void Release(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
            Marshal.FinalReleaseComObject(value);
    }
}

public sealed class FileSystemViCoUpdateService : IViCoUpdateService
{
    private readonly string _versionsRoot;

    public FileSystemViCoUpdateService(string versionsRoot)
    {
        _versionsRoot = versionsRoot;
    }

    public Task<ViCoUpdateInfo?> FindLatestAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            if (!Directory.Exists(_versionsRoot))
                return null;
            return Directory.EnumerateDirectories(_versionsRoot)
                .Select(path => new
                {
                    Path = path,
                    Version = ParseVersion(Path.GetFileName(path)),
                    Executable = FindExecutable(path)
                })
                .Where(item => item.Version is not null && item.Executable is not null)
                .OrderByDescending(item => item.Version)
                .Select(item => new ViCoUpdateInfo(item.Version!.ToString(), item.Path, item.Executable!))
                .FirstOrDefault();
        }, cancellationToken);
    }

    private static Version? ParseVersion(string value)
    {
        var normalized = value.TrimStart('V', 'v').Replace('_', '.');
        return Version.TryParse(normalized, out var version) ? version : null;
    }

    private static string? FindExecutable(string root) =>
        Directory.EnumerateFiles(root, "*.exe", SearchOption.AllDirectories)
            .FirstOrDefault(path => Path.GetFileName(path).Contains("VICO", StringComparison.OrdinalIgnoreCase));
}
