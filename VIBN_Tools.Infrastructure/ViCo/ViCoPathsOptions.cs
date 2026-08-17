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

    public string ApprovedLicensesRoot { get; init; } =
        @"\\grob.local\grob\GM\KO\EL\ALLG\Abtlg\ZD\Simulation\Dokumentation\vico\lic";

    public string LicenseRequestsRoot { get; init; } =
        @"\\grob.local\grob\GM\ALLG\Transfer\Allgemein\tmp\Vico\lic";

    public string VersionsRoot { get; init; } =
        @"\\grob.local\grob\GM\KO\EL\ALLG\Abtlg\ZD\Simulation\Dokumentation\vico\versions";

    public static ViCoPathsOptions CreateDefault()
    {
        return new ViCoPathsOptions(
            @"\\grob.local\grob\GM\KO\ALLG\PRJ\El_Prj_Um\_Simulation\Projekte",
            @"C:\Treiber\VICO_Tool\Favorites_2.txt");
    }
}
