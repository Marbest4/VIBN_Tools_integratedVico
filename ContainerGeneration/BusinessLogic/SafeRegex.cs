using System.Text.RegularExpressions;

namespace VIBN_Tools.ContainerGeneration.BusinessLogic;

public static class SafeRegex
{
    public static TimeSpan DefaultTimeout { get; } = TimeSpan.FromSeconds(1);

    public static Regex Create(
        string pattern,
        RegexOptions options = RegexOptions.None) =>
        new(
            pattern,
            options | RegexOptions.CultureInvariant,
            DefaultTimeout);
}
