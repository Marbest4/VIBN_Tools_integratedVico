using Microsoft.ML;
using Microsoft.ML.Data;

namespace VIBN_Tools.ContainerGeneration.AI
{
    /// <summary>
    /// Evaluiert die Modellgüte auf einem Hold-out-Testset (Train-Test-Split).
    ///
    /// VERBESSERUNGEN:
    ///  - Verwendet jetzt denselben Trainer wie TrainingService (useLightGbm-Parameter).
    ///    Vorher wurde in EvaluationService immer SDCA verwendet, egal was der
    ///    Benutzer eingestellt hatte → Evaluation war inkonsistent mit dem echten Modell.
    ///  - Normalize() kommt aus TextNormalizer (shared).
    /// </summary>
    public sealed class EvaluationService
    {
        private readonly MLContext _ml = new MLContext(seed: 42);

        private sealed class EvalInput
        {
            public string SignalText    { get; set; } = "";
            public string SlotName     { get; set; } = "";
            public string ComponentType { get; set; } = "";
        }

        private sealed class ScoredRow
        {
            public string  SlotName       { get; set; } = "";
            public string  PredictedLabel { get; set; } = "";
            public float[] Score          { get; set; } = Array.Empty<float>();
        }

        /// <summary>
        /// Führt eine Train-Test-Evaluation durch.
        /// </summary>
        /// <param name="rows">Alle Trainingsdaten</param>
        /// <param name="useLightGbm">Muss identisch mit TrainingService sein!</param>
        /// <param name="testFraction">Anteil für Testset (Standard 0.2 = 20 %)</param>
        /// <param name="progress">Optionaler Fortschrittslogger</param>
        public EvaluationResult Evaluate(
            IEnumerable<TrainingRow> rows,
            bool useLightGbm,
            double testFraction = 0.2,
            IProgress<string>? progress = null)
        {
            var dataList = rows
                .Where(r => !string.IsNullOrWhiteSpace(r.SignalText) &&
                            !string.IsNullOrWhiteSpace(r.SlotName))
                .Select(r => new EvalInput
                {
                    SignalText    = TextNormalizer.Normalize(r.SignalText),
                    SlotName      = r.SlotName,
                    ComponentType = r.ComponentType ?? ""
                })
                .ToList();

            if (dataList.Count < 10)
                return new EvaluationResult { Note = "Zu wenige Daten für sinnvolle Evaluation (<10)." };

            progress?.Report($"Evaluation: {dataList.Count} Zeilen, Testanteil: {testFraction:P0}");

            var data = _ml.Data.LoadFromEnumerable(dataList);

            // ── Pipeline: identisch zu TrainingService, gleicher Trainer ───────
            IEstimator<ITransformer> trainer = useLightGbm
                ? (IEstimator<ITransformer>)_ml.MulticlassClassification.Trainers.LightGbm(
                    labelColumnName:   "Label",
                    featureColumnName: "Features")
                : _ml.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                    labelColumnName:   "Label",
                    featureColumnName: "Features");

            var pipeline =
                _ml.Transforms.Conversion.MapValueToKey("Label", "SlotName")
                .Append(_ml.Transforms.Text.NormalizeText("TextNorm", "SignalText"))
                .Append(_ml.Transforms.Text.FeaturizeText("TextFeats", "TextNorm"))
                .Append(_ml.Transforms.Categorical.OneHotEncoding("TypeFeat", "ComponentType"))
                .Append(_ml.Transforms.Concatenate("Features", "TextFeats", "TypeFeat"))
                .Append(trainer)
                .Append(_ml.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            var split  = _ml.Data.TrainTestSplit(data, testFraction: testFraction, seed: 42);
            var model  = pipeline.Fit(split.TrainSet);
            var scored = model.Transform(split.TestSet);

            var metrics = _ml.MulticlassClassification.Evaluate(
                data:                     scored,
                labelColumnName:          "Label",
                scoreColumnName:          "Score",
                predictedLabelColumnName: "PredictedLabel");

            // SlotNames für Score-Vektor
            string[] slotNames = Array.Empty<string>();
            var scoreCol = scored.Schema.GetColumnOrNull("Score");
            if (scoreCol.HasValue)
            {
                try
                {
                    VBuffer<ReadOnlyMemory<char>> buf = default;
                    scoreCol.Value.Annotations.GetValue("SlotNames", ref buf);
                    slotNames = buf.DenseValues().Select(v => v.ToString()).ToArray();
                }
                catch { /* Fallback bleibt leer */ }
            }

            var testRows = _ml.Data.CreateEnumerable<ScoredRow>(scored, reuseRowObject: false).ToList();

            // Confusion + Top-3
            var confusion = new Dictionary<(string actual, string predicted), int>(StringTupleComparer.OrdinalIgnoreCase);
            int top3Hits  = 0;

            foreach (var r in testRows)
            {
                var pred = r.PredictedLabel ?? "";
                var act  = r.SlotName ?? "";
                confusion[(act, pred)] = confusion.TryGetValue((act, pred), out var c) ? c + 1 : 1;

                if (r.Score != null && r.Score.Length > 0 && slotNames.Length == r.Score.Length)
                {
                    var top3 = r.Score
                        .Select((s, idx) => (label: slotNames[idx], score: s))
                        .OrderByDescending(t => t.score)
                        .Take(3)
                        .Select(t => t.label)
                        .ToArray();
                    if (top3.Contains(act, StringComparer.OrdinalIgnoreCase))
                        top3Hits++;
                }
            }

            var result = new EvaluationResult
            {
                MicroAccuracy   = metrics.MicroAccuracy,
                MacroAccuracy   = metrics.MacroAccuracy,
                LogLoss         = metrics.LogLoss,
                PerClassLogLoss = metrics.PerClassLogLoss?.ToArray() ?? Array.Empty<double>(),
                Confusion       = confusion,
                Top3Accuracy    = testRows.Count > 0 ? (double)top3Hits / testRows.Count : 0.0
            };

            progress?.Report(
                $"Eval ({(useLightGbm ? "LightGBM" : "SDCA")}): " +
                $"MicroAcc={result.MicroAccuracy:P2}, " +
                $"MacroAcc={result.MacroAccuracy:P2}, " +
                $"Top3={result.Top3Accuracy:P2}, " +
                $"LogLoss={result.LogLoss:0.000}");

            return result;
        }
    }

    public sealed class EvaluationResult
    {
        public string Note          { get; set; } = "";
        public double MicroAccuracy  { get; set; }
        public double MacroAccuracy  { get; set; }
        public double LogLoss        { get; set; }
        public double Top3Accuracy   { get; set; }
        public double[] PerClassLogLoss { get; set; } = Array.Empty<double>();
        public Dictionary<(string actual, string predicted), int> Confusion { get; set; } = new();
    }

    internal sealed class StringTupleComparer : IEqualityComparer<(string a, string b)>
    {
        public static readonly StringTupleComparer OrdinalIgnoreCase = new();

        public bool Equals((string a, string b) x, (string a, string b) y)
            => string.Equals(x.a, y.a, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.b, y.b, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string a, string b) obj)
            => StringComparer.OrdinalIgnoreCase.GetHashCode(obj.a) * 397
             ^ StringComparer.OrdinalIgnoreCase.GetHashCode(obj.b);
    }
}
