namespace VIBN_Tools.Core.ViCo;

public sealed record ViCoLicenseEntry(string UserName, string Level, string SourceFile);

public interface IViCoLicenseService
{
    bool IsConfigured { get; }

    Task<IReadOnlyList<ViCoLicenseEntry>> LoadApprovedAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ViCoLicenseEntry>> LoadRequestsAsync(CancellationToken cancellationToken = default);

    Task RequestCurrentUserAsync(CancellationToken cancellationToken = default);

    Task SetLevelAsync(string userName, string level, CancellationToken cancellationToken = default);
}

public sealed record UpcomingMeeting(string Subject, DateTime Start, DateTime End);

public interface IUpcomingMeetingService
{
    Task<IReadOnlyList<UpcomingMeeting>> LoadTodayAsync(CancellationToken cancellationToken = default);
}

public sealed record ViCoUpdateInfo(string Version, string SourceDirectory, string ExecutablePath);

public interface IViCoUpdateService
{
    Task<ViCoUpdateInfo?> FindLatestAsync(CancellationToken cancellationToken = default);
}
