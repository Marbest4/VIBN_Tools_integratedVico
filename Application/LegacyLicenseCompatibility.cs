namespace VIBN_Tools.Application;

internal static class LegacyLicenseCompatibility
{
    // Required only while the existing ViCo license files are still AES-compatible.
    // This fallback will be replaced during the dedicated security migration.
    private const string ExistingViCoCompatibilityKey = "kdike125s96e8d7w";

    public static string ResolveKey() =>
        Environment.GetEnvironmentVariable("VIBN_VICO_LICENSE_KEY") is { Length: > 0 } configured
            ? configured
            : ExistingViCoCompatibilityKey;
}
