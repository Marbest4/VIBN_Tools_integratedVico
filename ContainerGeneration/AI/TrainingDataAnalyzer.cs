namespace VIBN_Tools.ContainerGeneration.AI
{
    public sealed class TrainingDataAnalyzer
    {
        public TrainingDataAnalysis Analyze(IEnumerable<TrainingRow> rows)
        {
            var list = rows.Where(r => !string.IsNullOrWhiteSpace(r.SignalText)
                                     && !string.IsNullOrWhiteSpace(r.SlotName))
                           .ToList();

            var slotDist = list
                .GroupBy(r => r.SlotName, StringComparer.OrdinalIgnoreCase)
                .Select(g => new CountItem(g.Key, g.Count()))
                .OrderByDescending(x => x.Count).ToList();

            var typeDist = list
                .GroupBy(r => r.ComponentType ?? "", StringComparer.OrdinalIgnoreCase)
                .Select(g => new CountItem(g.Key, g.Count()))
                .OrderByDescending(x => x.Count).ToList();

            // Inkonsistente Signale: gleiches Signal mapped auf mehrere Slots
            var inconsistentSignals = list
                .GroupBy(r => Normalize(r.SignalText))
                .Select(g => new { Signal = g.First().SignalText, Slots = g.Select(x => x.SlotName).Distinct(StringComparer.OrdinalIgnoreCase).ToList(), Count = g.Count() })
                .Where(x => x.Slots.Count > 1)
                .OrderByDescending(x => x.Count)
                .Take(100) // Deckelung
                .Select(x => new InconsistentSignal(x.Signal, x.Slots))
                .ToList();

            // Token-Statistik (einfach)
            var tokenFreq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in list)
            {
                foreach (var tok in Tokenize(r.SignalText))
                {
                    tokenFreq[tok] = tokenFreq.TryGetValue(tok, out var c) ? c + 1 : 1;
                }
            }
            var topTokens = tokenFreq.OrderByDescending(p => p.Value)
                                     .Take(100)
                                     .Select(p => new CountItem(p.Key, p.Value))
                                     .ToList();

            return new TrainingDataAnalysis
            {
                TotalRows = list.Count,
                SlotDistribution = slotDist,
                ComponentTypeDistribution = typeDist,
                InconsistentSignals = inconsistentSignals,
                TopTokens = topTokens
            };
        }

        private static string Normalize(string s) =>
            (s ?? "").Trim().ToLowerInvariant();

        private static IEnumerable<string> Tokenize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) yield break;
            var span = s.ToLowerInvariant();
            char[] sep = new[] { ' ', '\t', '_', '-', '/', '\\', '.', ',', ';', ':', '(', ')', '[', ']', '{', '}', '\"', '\'', '|' };
            foreach (var t in span.Split(sep, StringSplitOptions.RemoveEmptyEntries))
            {
                yield return t;
            }
        }
    }

    public sealed record CountItem(string Name, int Count);
    public sealed record InconsistentSignal(string Signal, IReadOnlyList<string> Slots);

    public sealed class TrainingDataAnalysis
    {
        public int TotalRows { get; set; }
        public List<CountItem> SlotDistribution { get; set; } = new();
        public List<CountItem> ComponentTypeDistribution { get; set; } = new();
        public List<InconsistentSignal> InconsistentSignals { get; set; } = new();
        public List<CountItem> TopTokens { get; set; } = new();
    }
}