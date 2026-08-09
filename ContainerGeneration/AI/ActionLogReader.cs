using System.IO;
using System.Text.Json;

namespace VIBN_Tools.ContainerGeneration.AI;

/// <summary>
/// Liest JSONL-ActionLogs und wandelt sie in Trainingszeilen um.
///
/// VERBESSERUNGEN:
///  - ComponentType wird jetzt aus dem Log übernommen (war vorher verloren!).
///    UserActionEvent muss dazu ComponentType enthalten (siehe ActionLogger).
///  - Ungültige/leere Zeilen werden still übersprungen (Exception-Handling).
/// </summary>
public static class ActionLogReader
{
    public static IEnumerable<TrainingRow> ToTrainingRows(IEnumerable<string> files)
    {
        foreach (var f in files)
        {
            if (!File.Exists(f)) continue;

            foreach (var line in File.ReadLines(f))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                UserActionEvent? evt = null;
                try { evt = JsonSerializer.Deserialize<UserActionEvent>(line); }
                catch { /* fehlerhafte Zeile überspringen */ }

                if (evt is null) continue;
                if (string.IsNullOrWhiteSpace(evt.SignalText)) continue;
                if (string.IsNullOrWhiteSpace(evt.ToSlot)) continue;

                yield return new TrainingRow
                {
                    SignalText    = evt.SignalText,
                    SlotName      = evt.ToSlot,
                    ComponentType = evt.ComponentType ?? "",   // ← neu: Feature erhalten!
                    ComponentName = evt.ToContainer
                };
            }
        }
    }
}
