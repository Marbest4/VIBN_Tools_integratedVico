namespace VIBN_Tools.Core.ViCo;

/// <summary>
/// One persisted ViCo role assignment.  Roles are intentionally independent of
/// licenses: they solely control which tool areas are visible and manageable.
/// </summary>
public sealed record ViCoUserRole(string UserName, string Level, string Source);

/// <summary>Validated, complete role-set change ready for an atomic save.</summary>
public sealed record ViCoUserRoleChangePlan(
    bool IsValid,
    string Message,
    IReadOnlyList<ViCoUserRole> Roles,
    IReadOnlyList<string> Level9Users);

/// <summary>
/// Central authorization policy for VIBN Tools.  The policy is deliberately
/// free of licensing semantics, and is shared by the UI, persistence layer and
/// automated checks.
/// </summary>
public static class ViCoRolePolicy
{
    /// <summary>The operational emergency administrator required by policy.</summary>
    public const string MandatoryLevel9User = "lutzma";

    /// <summary>
    /// Minimum number of distinct Level9 administrators.  Keep this value at
    /// two so a single account can never become an administrative bottleneck.
    /// </summary>
    public const int MinimumLevel9Users = 2;

    public static string GetEffectiveLevel(string userName, string? persistedLevel) =>
        IsMandatoryLevel9User(userName) ? "Level9" : NormalizeLevel(persistedLevel);

    public static bool IsMandatoryLevel9User(string userName) =>
        WindowsUserIdentity.Equals(userName, MandatoryLevel9User);

    public static int ParseLevel(string? value) =>
        value is not null &&
        value.StartsWith("Level", StringComparison.OrdinalIgnoreCase) &&
        int.TryParse(value[5..], out var level) &&
        level is >= 0 and <= 9
            ? level
            : -1;

    /// <summary>Normalizes an editable level without silently granting access.</summary>
    public static string NormalizeLevel(string? level) =>
        ParseLevel(level) is var numeric && numeric >= 0
            ? $"Level{numeric}"
            : "Nicht erkannt";

    /// <summary>
    /// Applies immutable policy accounts to a read model.  It does not persist
    /// data itself; the caller may decide whether the central role store is
    /// currently writable.
    /// </summary>
    public static IReadOnlyList<ViCoUserRole> ApplyMandatoryRoles(IEnumerable<ViCoUserRole> roles)
    {
        ArgumentNullException.ThrowIfNull(roles);
        var normalized = ToDictionary(roles);
        normalized[MandatoryLevel9User] = new ViCoUserRole(
            MandatoryLevel9User,
            "Level9",
            "Systemrichtlinie");
        return normalized.Values
            .OrderBy(role => role.UserName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Validates a complete proposed role set before it is written.  Validation
    /// happens before the atomic file replace, so an interrupted or invalid UI
    /// change can never leave fewer than two Level9 administrators behind.
    /// </summary>
    public static ViCoUserRoleChangePlan PlanSave(IEnumerable<ViCoUserRole> roles)
    {
        ArgumentNullException.ThrowIfNull(roles);
        var normalized = ToDictionary(roles);
        normalized[MandatoryLevel9User] = new ViCoUserRole(
            MandatoryLevel9User,
            "Level9",
            "Systemrichtlinie");

        var invalid = normalized.Values.FirstOrDefault(role => ParseLevel(role.Level) < 0);
        if (invalid is not null)
        {
            return Invalid($"Die Stufe für {invalid.UserName} ist ungültig.", normalized.Values);
        }

        var level9Users = normalized.Values
            .Where(role => string.Equals(role.Level, "Level9", StringComparison.OrdinalIgnoreCase))
            .Select(role => WindowsUserIdentity.Normalize(role.UserName))
            .Where(userName => userName.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(userName => userName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (level9Users.Length < MinimumLevel9Users)
        {
            return new ViCoUserRoleChangePlan(
                false,
                $"Mindestens {MinimumLevel9Users} unterschiedliche Benutzer müssen Level9 besitzen.",
                normalized.Values.OrderBy(role => role.UserName, StringComparer.OrdinalIgnoreCase).ToArray(),
                level9Users);
        }

        return new ViCoUserRoleChangePlan(
            true,
            $"Die Level9-Mindestbesetzung ist mit {level9Users.Length} Benutzern erfüllt.",
            normalized.Values.OrderBy(role => role.UserName, StringComparer.OrdinalIgnoreCase).ToArray(),
            level9Users);
    }

    private static ViCoUserRoleChangePlan Invalid(string message, IEnumerable<ViCoUserRole> roles) =>
        new(
            false,
            message,
            roles.OrderBy(role => role.UserName, StringComparer.OrdinalIgnoreCase).ToArray(),
            Array.Empty<string>());

    private static Dictionary<string, ViCoUserRole> ToDictionary(IEnumerable<ViCoUserRole> roles)
    {
        var result = new Dictionary<string, ViCoUserRole>(StringComparer.OrdinalIgnoreCase);
        foreach (var role in roles)
        {
            var userName = WindowsUserIdentity.Normalize(role.UserName);
            if (userName.Length == 0)
                continue;

            result[userName] = new ViCoUserRole(
                userName,
                NormalizeLevel(role.Level),
                string.IsNullOrWhiteSpace(role.Source) ? "roles.json" : role.Source);
        }

        return result;
    }
}

/// <summary>Persistence boundary for the central role list.</summary>
public interface IViCoUserRoleStore
{
    bool IsConfigured { get; }

    Task<IReadOnlyList<ViCoUserRole>> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Saves a complete, policy-validated role set atomically.</summary>
    Task SaveAsync(IReadOnlyCollection<ViCoUserRole> roles, CancellationToken cancellationToken = default);
}
