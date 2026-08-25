namespace VIBN_Tools.Infrastructure.ViCo;

public sealed record ViCoPathsOptions(
    string SimulationProjectsRoot,
    string FavoritesFile,
    int MaximumParallelCopies = 2)
{
    public string ServerCacheRoot { get; init; } =
        @"\\grob.local\grob\GM\KO\EL\ALLG\Abtlg\ZD\Simulation\Dokumentation\vico\Server";

    public string WorkingDirectory { get; init; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GROB",
        "VIBN_Tools",
        "ViCo");

    /// <summary>
    /// Historical encrypted assignments, read only during the one-time roles
    /// migration. New installations use <see cref="RolesFile"/> exclusively.
    /// </summary>
    public string LegacyRoleAssignmentsRoot { get; init; } =
        @"\\grob.local\grob\GM\KO\EL\ALLG\Abtlg\ZD\Simulation\Dokumentation\vico\lic";

    /// <summary>
    /// Shared, human-readable role store.  The historical folder is retained
    /// solely to preserve the existing access control list during migration;
    /// the file itself is not a license store and contains only user levels.
    /// Set <c>VIBN_VICO_ROLES_FILE</c> to use another centrally managed path.
    /// </summary>
    public string RolesFile { get; init; } = Environment.GetEnvironmentVariable("VIBN_VICO_ROLES_FILE")
        ?? @"\\grob.local\grob\GM\KO\EL\ALLG\Abtlg\ZD\Simulation\Dokumentation\vico\lic\roles.json";

    public string VersionsRoot { get; init; } =
        @"\\grob.local\grob\GM\KO\EL\ALLG\Abtlg\ZD\Simulation\Dokumentation\vico\versions";

    public string CommissioningProjectsRoot { get; init; } =
        @"\\grob.local\grob\GM\KO\ALLG\PRJ\El_Project";

    public string PlanningProjectsRoot { get; init; } =
        @"\\grob.local\grob\GM\ALLG\KdProj\Kundenprojekte";

    public static ViCoPathsOptions CreateDefault()
    {
        return new ViCoPathsOptions(
            @"\\grob.local\grob\GM\KO\ALLG\PRJ\El_Prj_Um\_Simulation\Projekte",
            @"C:\Treiber\VICO_Tool\Favorites_2.txt");
    }
}
