namespace VIBN_Tools.ContainerGeneration.AI
{
    /// <summary>
    /// Entfernt fehlerhafte ActionLog-Datensätze.
    ///
    /// VERBESSERUNGEN gegenüber Vorgänger:
    ///  - Keine vollständige Deduplizierung mehr (war ein ML-Fehler!).
    ///    Vorher: GroupBy(...).Select(g => g.First()) entfernte ALLE Duplikate.
    ///    Problem: Wenn ein Signal korrekt 20× mit dem gleichen Slot vorkommt,
    ///    ist das wertvolle Frequenzinformation für das Modell → darf nicht entfernt werden.
    ///    Neu: Soft-Cap pro (Signal, Slot)-Kombination (Standard: max. 20 Exemplare),
    ///    damit einzelne Einträge nicht den gesamten Trainings-Pool dominieren.
    ///
    /// Weiterhin gefiltert:
    ///  - Leere oder extrem kurze/lange Signale
    ///  - Ungültige Slot-Namen
    ///  - "BadWords" aus NoiseFilterConfig.json
    /// </summary>
    public sealed class ActionLogNoiseFilter
    {
        private HashSet<string> _badWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "test", "debug", "fehler", "xxx", "asdf"
        };

        /// <summary>
        /// Maximale Anzahl identischer (Signal, Slot, Type)-Kombinationen.
        /// Verhindert, dass häufig verwendete Einträge alles andere dominieren.
        /// </summary>
        public int MaxDuplicatesPerGroup { get; set; } = 20;

        public ActionLogNoiseFilter() => Reload();

        public void Reload()
        {
            try   { LoadFromConfig(NoiseFilterConfig.Load()); }
            catch { /* alte Defaults behalten */ }
        }

        public void LoadFromConfig(NoiseFilterConfig cfg)
        {
            if (cfg?.BadWords != null)
                _badWords = new HashSet<string>(cfg.BadWords, StringComparer.OrdinalIgnoreCase);
        }

        public IEnumerable<TrainingRow> Filter(IEnumerable<TrainingRow> rows)
        {
            return rows
                .Where(r => !string.IsNullOrWhiteSpace(r.SignalText))
                .Where(r => r.SignalText.Length > 2 && r.SignalText.Length < 200)
                .Where(r => !_badWords.Any(b => r.SignalText.Contains(b, StringComparison.OrdinalIgnoreCase)))
                .Where(r => IsValidSlot(r.SlotName))
                // SOFT-CAP: Max N Exemplare pro identischer Kombination
                // (Statt früher: .Select(g => g.First()) → nur 1 Exemplar!)
                .GroupBy(r => (
                    TextNormalizer.Normalize(r.SignalText),
                    r.SlotName?.ToLowerInvariant() ?? "",
                    r.ComponentType?.ToLowerInvariant() ?? ""))
                .SelectMany(g => g.Take(MaxDuplicatesPerGroup));
        }

        private static bool IsValidSlot(string slot)
        {
            if (string.IsNullOrWhiteSpace(slot)) return false;
            if (slot.Length < 2 || slot.Length > 50) return false;
            return slot.All(c => char.IsLetterOrDigit(c) || c == '_');
        }
    }
}
