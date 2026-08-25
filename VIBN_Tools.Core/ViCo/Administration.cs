namespace VIBN_Tools.Core.ViCo;

/// <summary>Read-only Outlook item displayed in the ViCo administration view.</summary>
public sealed record UpcomingMeeting(string Subject, DateTime Start, DateTime End);

public interface IUpcomingMeetingService
{
    Task<IReadOnlyList<UpcomingMeeting>> LoadTodayAsync(CancellationToken cancellationToken = default);
}

/// <summary>Latest deployable ViCo version discovered on the shared update path.</summary>
public sealed record ViCoUpdateInfo(string Version, string SourceDirectory, string ExecutablePath);

public interface IViCoUpdateService
{
    Task<ViCoUpdateInfo?> FindLatestAsync(CancellationToken cancellationToken = default);
}
