using Microsoft.ML;
using Microsoft.ML.Data;

namespace VIBN_Tools.ContainerGeneration.AI
{
    public sealed class PredictionService
    {
        private readonly MLContext _ml = new MLContext(seed: 42);
        private PredictionEngine<SignalInput, SignalPrediction>? _engine;
        private ITransformer? _model;

        private string[] _slotNames = Array.Empty<string>();

        public void Load(string modelPath)
        {
            _model = _ml.Model.Load(modelPath, out var inputSchema);
            if (_model == null)
                throw new InvalidOperationException($"Konnte Modell nicht laden: {modelPath}");

            _engine = _ml.Model.CreatePredictionEngine<SignalInput, SignalPrediction>(_model);

            // Output-Schema aus der Modell-Pipeline holen
            var outputSchema = _model.GetOutputSchema(inputSchema);

            // === 1) SlotNames von der Score-Spalte lesen (Standard bei Multiclass) ===
            var scoreCol = outputSchema.GetColumnOrNull(nameof(SignalPrediction.Score));
            if (scoreCol.HasValue)
            {
                try
                {
                    VBuffer<ReadOnlyMemory<char>> slotNamesBuf = default;
                    scoreCol.Value.Annotations.GetValue("SlotNames", ref slotNamesBuf);

                    var arr = slotNamesBuf.DenseValues().Select(x => x.ToString()).ToArray();
                    if (arr.Length > 0)
                    {
                        _slotNames = arr;
                        return; // fertig
                    }
                }
                catch
                {
                    // weiter unten Fallback
                }
            }

            // === 2) Fallback: KeyValues von PredictedLabel lesen ===
            var predCol = outputSchema.GetColumnOrNull(nameof(SignalPrediction.PredictedLabel));
            if (predCol.HasValue)
            {
                try
                {
                    VBuffer<ReadOnlyMemory<char>> keys = default;
                    predCol.Value.Annotations.GetValue("KeyValues", ref keys);

                    var arr = keys.DenseValues().Select(k => k.ToString()).ToArray();
                    if (arr.Length > 0)
                    {
                        _slotNames = arr;
                        return;
                    }
                }
                catch
                {
                    // Letzter Fallback → keine Label-Namen
                }
            }

            // Wenn beides nicht klappt → array leer → Fallback-Labels "#0,#1..."
            _slotNames = Array.Empty<string>();
        }

        public SignalPrediction Predict(string signalText, string? componentType = null, string? slotName = "")
        {
            if (_engine == null)
                throw new InvalidOperationException("Model not loaded.");

            var input = new SignalInput
            {
                SignalText = Normalize(signalText),
                ComponentType = componentType ?? "",
                SlotName = slotName ?? ""  // <- wichtig für Pipeline-Schema!
            };

            var pred = _engine.Predict(input);

            pred.Confidence = pred.Score?.Length > 0
                ? pred.Score.Max()
                : 0f;

            return pred;
        }

        public IReadOnlyList<(string label, float score)> GetTopK(float[] score, int k = 3)
        {
            if (score == null || score.Length == 0)
                return Array.Empty<(string, float)>();

            // korrekte Labels, wenn vorhanden:
            if (_slotNames.Length == score.Length)
            {
                return score
                    .Select((s, idx) => (label: _slotNames[idx], score: s))
                    .OrderByDescending(t => t.score)
                    .Take(k)
                    .ToList();
            }

            // Fallback: "#0" etc.
            return score
                .Select((s, idx) => (label: $"#{idx}", score: s))
                .OrderByDescending(t => t.score)
                .Take(k)
                .ToList();
        }

        private static string Normalize(string s) =>
            (s ?? "").ToLowerInvariant()
                     .Replace("ä", "ae")
                     .Replace("ö", "oe")
                     .Replace("ü", "ue")
                     .Replace("ß", "ss");
    }

    public sealed class SignalInput
    {
        public string SignalText { get; set; } = "";
        public string ComponentType { get; set; } = "";
        public string SlotName { get; set; } = "";  // <- notwendig für Input-Schema
    }

    public sealed class SignalPrediction
    {
        public string PredictedLabel { get; set; } = "";
        public float[] Score { get; set; } = Array.Empty<float>();
        [NoColumn] public float Confidence { get; set; }
    }
}