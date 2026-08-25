namespace VIBN_Tools.Application;

/// <summary>
/// Compatibility key lookup used exclusively while importing the predecessor's
/// encrypted role assignments into roles.json. It has no effect after the JSON
/// role file exists.
/// </summary>
internal static class LegacyRoleMigrationCompatibility
{
    private const string ExistingRoleMigrationKey = "kdike125s96e8d7w";

    public static string ResolveKey() =>
        Environment.GetEnvironmentVariable("VIBN_VICO_ROLE_MIGRATION_KEY") is { Length: > 0 } configured
            ? configured
            : ExistingRoleMigrationKey;
}
