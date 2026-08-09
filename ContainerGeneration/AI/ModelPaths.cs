using System.IO;
using System.Security.Cryptography;

namespace VIBN_Tools.ContainerGeneration.AI;

/// <summary>
/// Zentrale Pfadverwaltung fuer alle KI-relevanten Dateien.
///
/// SPEICHERSTRATEGIE:
///   Alle Dateien liegen RELATIV zum Startordner der .exe:
///   {ExeDir}\vibn_ai_data\
///       ├── models\          → trainierte Modelle
///       ├── actions\         → ActionLogs (JSONL, eine Datei pro Tag)
///       ├── training_pool\   → hochgeladene Trainings-XMLs
///       └── Corrections.csv  → manuelle Korrekturen aus Improve
///
///   Vorteil: Alle KI-Daten sind am selben Ort wie das Tool.
///   Kein Suchen in AppData. Einfach sichern, einfach verschieben.
///
/// AENDERUNGEN gegenueber Original:
///   - Pfadbasis: AppData → ExeDir\vibn_ai_data\
///   - ActionLogs: nur 30 Tage → ALLE vorhandenen Logs
///   - Neu: AllActionLogs() gibt alle JSONL-Dateien zurueck
///   - Neu: XmlHash() prueft ob eine XML schon im Pool liegt
/// </summary>
public static class ModelPaths
{
    // ════════════════════════════════════════════════════════════════
    //  BASISPFAD – relativ zur laufenden .exe
    // ════════════════════════════════════════════════════════════════
    /// <summary>
    /// Basisordner: Verzeichnis der laufenden .exe + \vibn_ai_data
    /// Beispiel: D:\VIBN_Tools\vibn_ai_data\
    /// </summary>
    public static string BaseDir => Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "vibn_ai_data");

    public static string ModelsDir  => Path.Combine(BaseDir, "models");
    public static string ActionsDir => Path.Combine(BaseDir, "actions");

    // TrainingDatasetStore nutzt TrainingPoolDir als RootFolder
    public static string TrainingPoolDir => Path.Combine(BaseDir, "training_pool");

    public static string CurrentModel   => Path.Combine(ModelsDir, "model_current.zip");
    public static string CorrectionsFile => Path.Combine(BaseDir,  "Corrections.csv");

    // ════════════════════════════════════════════════════════════════
    //  MODELL-VERSIONIERUNG
    // ════════════════════════════════════════════════════════════════
    public static string NewVersion()
    {
        Directory.CreateDirectory(ModelsDir);
        return Path.Combine(ModelsDir, $"model_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip");
    }

    public static void SwitchToLatest(string newPath)
    {
        Directory.CreateDirectory(ModelsDir);
        File.Copy(newPath, CurrentModel, overwrite: true);
    }

    // ════════════════════════════════════════════════════════════════
    //  ACTIONLOGS – ALLE (nicht nur letzte 30 Tage)
    // ════════════════════════════════════════════════════════════════
    /// <summary>
    /// Gibt ALLE vorhandenen ActionLog-JSONL-Dateien zurueck.
    /// Vorher: nur die letzten 30 Tage. Jetzt: vollstaendige Historie.
    /// </summary>
    public static IEnumerable<string> AllActionLogs()
    {
        if (!Directory.Exists(ActionsDir)) yield break;
        foreach (var f in Directory.GetFiles(ActionsDir, "*.jsonl")
                                   .OrderBy(f => f))   // chronologisch
            yield return f;
    }

    /// <summary>
    /// Optional: Nur Logs der letzten N Tage (Fallback fuer Tests).
    /// Im Produktivbetrieb AllActionLogs() verwenden.
    /// </summary>
    public static IEnumerable<string> ActionLogsLastDays(int days)
    {
        if (!Directory.Exists(ActionsDir)) yield break;
        var minDate = DateTime.UtcNow.AddDays(-days).Date;
        foreach (var f in Directory.GetFiles(ActionsDir, "*.jsonl"))
            if (File.GetLastWriteTimeUtc(f).Date >= minDate) yield return f;
    }

    // ════════════════════════════════════════════════════════════════
    //  XML-DUPLIKAT-ERKENNUNG
    // ════════════════════════════════════════════════════════════════
    /// <summary>
    /// Berechnet den MD5-Hash einer Datei (schnell, reicht fuer Duplikat-Check).
    /// </summary>
    public static string FileHash(string path)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(path);
        var hash = md5.ComputeHash(stream);
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// Prueft ob eine XML mit demselben Inhalt bereits im Trainingspool liegt.
    /// Gibt den Namen der Duplikat-Datei zurueck, oder null wenn kein Duplikat.
    /// </summary>
    public static string? FindDuplicateXml(string candidatePath, string poolDir)
    {
        if (!Directory.Exists(poolDir)) return null;
        var candidateHash = FileHash(candidatePath);

        foreach (var existing in Directory.GetFiles(poolDir, "*.xml"))
        {
            if (FileHash(existing) == candidateHash)
                return Path.GetFileName(existing);
        }
        return null;
    }

    // ════════════════════════════════════════════════════════════════
    //  VERZEICHNISSE SICHERSTELLEN
    // ════════════════════════════════════════════════════════════════
    public static void EnsureAllDirectories()
    {
        Directory.CreateDirectory(ModelsDir);
        Directory.CreateDirectory(ActionsDir);
        Directory.CreateDirectory(TrainingPoolDir);
    }
}
