namespace VIBN_Tools.Core.ViCo;

public sealed record ViCoLicenseEntry(string UserName, string Level, string SourceFile);

/// <summary>A single persisted license-level change.</summary>
public sealed record LicenseLevelChange(string UserName, string Level);

/// <summary>Validated result of a complete license change request.</summary>
public sealed record LicenseChangePlan(
    bool IsValid,
    string Message,
    IReadOnlyList<LicenseLevelChange> Changes,
    IReadOnlyList<string> ResultingLevel9Users);

/// <summary>
/// Protects the ViCo administration from ending with a single Level9 account.
/// Change <see cref="MinimumLevel9Users"/> only after the operational policy has changed.
/// </summary>
public static class LicenseAdministrationPolicy
{
    /// <summary>
    /// Minimum number of distinct Windows identities that must retain Level9.
    /// This is the single source of truth for the UI, save operation and tests.
    /// </summary>
    public const int MinimumLevel9Users = 2;

    /// <summary>
    /// Calculates the resulting license state before any file is written.
    /// </summary>
    public static LicenseChangePlan PlanChange(
        IEnumerable<ViCoLicenseEntry> currentEntries,
        string userName,
        string level,
        string? additionalLevel9User = null)
    {
        ArgumentNullException.ThrowIfNull(currentEntries);
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(level))
            return Invalid("Benutzer und Lizenzlevel müssen ausgewählt sein.");

        var levels = currentEntries
            .Where(entry => !string.Equals(entry.Level, "Requested", StringComparison.OrdinalIgnoreCase))
            .GroupBy(entry => WindowsUserIdentity.Normalize(entry.UserName), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Key.Length > 0)
            .ToDictionary(
                group => group.Key,
                group => group.First().Level,
                StringComparer.OrdinalIgnoreCase);

        var normalizedUser = WindowsUserIdentity.Normalize(userName);
        levels[normalizedUser] = level;

        var changes = new List<LicenseLevelChange>();
        if (!string.IsNullOrWhiteSpace(additionalLevel9User))
        {
            var normalizedAdditional = WindowsUserIdentity.Normalize(additionalLevel9User);
            if (normalizedAdditional.Length > 0 &&
                !string.Equals(normalizedAdditional, normalizedUser, StringComparison.OrdinalIgnoreCase))
            {
                levels[normalizedAdditional] = "Level9";
                changes.Add(new LicenseLevelChange(additionalLevel9User, "Level9"));
            }
        }

        var resultingLevel9Users = levels
            .Where(item => string.Equals(item.Value, "Level9", StringComparison.OrdinalIgnoreCase))
            .Select(item => item.Key)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (resultingLevel9Users.Length < MinimumLevel9Users)
        {
            return new LicenseChangePlan(
                false,
                $"Mindestens {MinimumLevel9Users} unterschiedliche Benutzer müssen Level9 besitzen. " +
                "Wähle bei Bedarf einen zusätzlichen Level9-Benutzer aus.",
                Array.Empty<LicenseLevelChange>(),
                resultingLevel9Users);
        }

        changes.Add(new LicenseLevelChange(userName, level));
        changes = changes
            .GroupBy(change => WindowsUserIdentity.Normalize(change.UserName), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .OrderByDescending(change => string.Equals(change.Level, "Level9", StringComparison.OrdinalIgnoreCase))
            .ToList();

        return new LicenseChangePlan(
            true,
            $"Die Level9-Mindestbesetzung ist mit {resultingLevel9Users.Length} Benutzern erfüllt.",
            changes,
            resultingLevel9Users);
    }

    private static LicenseChangePlan Invalid(string message) =>
        new(false, message, Array.Empty<LicenseLevelChange>(), Array.Empty<string>());
}

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
