using Microsoft.ML;
using Microsoft.ML.Trainers.LightGbm;

namespace VIBN_Tools.ContainerGeneration.AI
{
    /// <summary>
    /// Führt das ML.NET-Training durch.
    ///
    /// VERBESSERUNGEN gegenüber Vorgänger:
    ///  - Pipeline-Definition an einer einzigen Stelle (BuildPipeline), nicht doppelt
    ///  - Normalize() kommt jetzt aus TextNormalizer (shared), nicht mehr lokal
    ///  - Korrekturen (Improve-Einträge) werden standardmäßig 5× dupliziert,
    ///    damit Benutzer-Feedback stärker gewichtet wird als Bulk-XML-Daten
    ///  - EvaluationService und TrainingService nutzen jetzt denselben Trainer-Typ
    /// </summary>
    public sealed class TrainingService
    {
        private readonly MLContext _ml = new MLContext(seed: 42);

        // -----------------------------------------------------------------------
        // Internes Schema
        // -----------------------------------------------------------------------
        private sealed class TrainInput
        {
            public string SignalText    { get; set; } = "";
            public string SlotName     { get; set; } = "";
            public string ComponentType { get; set; } = "";
        }

        // -----------------------------------------------------------------------
        // Haupt-Training
        // -----------------------------------------------------------------------

        /// <summary>
        /// Trainiert das Modell aus normalisierten Trainingsdaten.
        /// </summary>
        /// <param name="rows">Alle Trainingsdaten (XML + Logs + Corrections)</param>
        /// <param name="correctionRows">
        ///   Benutzer-Korrekturen (Improve). Diese werden <paramref name="correctionWeight"/>-fach
        ///   in den Trainings-Pool aufgenommen, damit sie das Modell stärker beeinflussen.
        /// </param>
        /// <param name="outputModelPath">Pfad zur neuen .zip-Modelldatei</param>
        /// <param name="useLightGbm">true = LightGBM (empfohlen), false = SDCA (Fallback)</param>
        /// <param name="correctionWeight">
        ///   Wie oft Korrekturen dupliziert werden (Standard 5).
        ///   Erhöhen, wenn Korrekturen noch nicht greifen; verringern bei Overfitting.
        /// </param>
        /// <param name="progress">Optionaler Log-Fortschritt</param>
        public void TrainFromRows(
            IEnumerable<TrainingRow> rows,
            IEnumerable<TrainingRow>? correctionRows,
            string outputModelPath,
            bool useLightGbm,
            int correctionWeight = 5,
            IProgress<string>? progress = null)
        {
            // ── 1) Daten aufbereiten ────────────────────────────────────────────
            var all = PrepareInputs(rows).ToList();

            // Korrekturen mehrfach hinzufügen → stärkere Gewichtung
            if (correctionRows != null)
            {
                var corrList = PrepareInputs(correctionRows).ToList();
                for (int i = 0; i < correctionWeight; i++)
                    all.AddRange(corrList);

                if (corrList.Count > 0)
                    progress?.Report($"Korrekturen ({corrList.Count}) wurden {correctionWeight}× gewichtet hinzugefügt.");
            }

            if (all.Count < 10)
                throw new InvalidOperationException("Zu wenige Trainingsdaten (<10).");

            progress?.Report($"Training: {all.Count} Zeilen (inkl. Gewichtung) geladen.");

            // ── 2) Datensatz ────────────────────────────────────────────────────
            var data = _ml.Data.LoadFromEnumerable(all);

            // ── 3) Pipeline ─────────────────────────────────────────────────────
            string trainerName = useLightGbm ? "LightGBM" : "SDCA";
            progress?.Report($"Verwende Trainer: {trainerName}");

            var pipeline = BuildPipeline(useLightGbm);

            // ── 4) Trainieren ───────────────────────────────────────────────────
            progress?.Report("Trainiere Modell …");
            var model = pipeline.Fit(data);

            // ── 5) Speichern ────────────────────────────────────────────────────
            progress?.Report($"Speichere Modell nach: {outputModelPath}");
            _ml.Model.Save(model, data.Schema, outputModelPath);
            progress?.Report("Training abgeschlossen.");
        }

        // -----------------------------------------------------------------------
        // Feature-Importance per Ablation
        // -----------------------------------------------------------------------

        /// <summary>
        /// Berechnet Feature-Importance durch Ablation:
        /// Ein Feature wird auf leer gesetzt und der Genauigkeitsverlust gemessen.
        /// </summary>
        public List<(string FeatureName, double Score)> GetFeatureImportanceAblation(
            IEnumerable<TrainingRow> rows,
            bool useLightGbm,
            IProgress<string>? progress = null)
        {
            var result = new List<(string FeatureName, double Score)>();

            var all = PrepareInputs(rows).ToList();
            if (all.Count < 10)
            {
                progress?.Report("FeatureImportance: Zu wenige Zeilen (<10) – abgebrochen.");
                return result;
            }

            var data     = _ml.Data.LoadFromEnumerable(all);
            var pipeline = BuildPipeline(useLightGbm);
            var model    = pipeline.Fit(data);

            double baseAcc = EvalAccuracy(model, data);

            // SignalText abklemmen
            var noText   = all.Select(t => new TrainInput { SignalText = "", SlotName = t.SlotName, ComponentType = t.ComponentType }).ToList();
            double impText = Math.Max(0, baseAcc - EvalAccuracy(model, _ml.Data.LoadFromEnumerable(noText)));

            // ComponentType abklemmen
            var noType   = all.Select(t => new TrainInput { SignalText = t.SignalText, SlotName = t.SlotName, ComponentType = "" }).ToList();
            double impType = Math.Max(0, baseAcc - EvalAccuracy(model, _ml.Data.LoadFromEnumerable(noType)));

            progress?.Report($"FeatureImportance (Ablation): SignalText={impText:0.000}, ComponentType={impType:0.000}");

            result.Add(("SignalText",    impText));
            result.Add(("ComponentType", impType));
            return result;
        }

        // -----------------------------------------------------------------------
        // Interne Hilfsmethoden
        // -----------------------------------------------------------------------

        /// <summary>
        /// Baut die ML-Pipeline. Einzige Definition – keine Duplikate mehr.
        /// </summary>
        private IEstimator<ITransformer> BuildPipeline(bool useLightGbm)
        {
            IEstimator<ITransformer> trainer = useLightGbm
                ? (IEstimator<ITransformer>)_ml.MulticlassClassification.Trainers.LightGbm(
                    labelColumnName:   "Label",
                    featureColumnName: "Features")
                : _ml.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                    labelColumnName:   "Label",
                    featureColumnName: "Features");

            return _ml.Transforms.Conversion.MapValueToKey("Label", nameof(TrainInput.SlotName))
                .Append(_ml.Transforms.Text.NormalizeText("TextNorm", nameof(TrainInput.SignalText)))
                .Append(_ml.Transforms.Text.FeaturizeText("TextFeats", "TextNorm"))
                .Append(_ml.Transforms.Categorical.OneHotEncoding("TypeFeat", nameof(TrainInput.ComponentType)))
                .Append(_ml.Transforms.Concatenate("Features", "TextFeats", "TypeFeat"))
                .Append(trainer)
                .Append(_ml.Transforms.Conversion.MapKeyToValue("PredictedLabel"));
        }

        private IEnumerable<TrainInput> PrepareInputs(IEnumerable<TrainingRow> rows)
            => rows
                .Where(r => !string.IsNullOrWhiteSpace(r.SignalText) &&
                            !string.IsNullOrWhiteSpace(r.SlotName))
                .Select(r => new TrainInput
                {
                    SignalText    = TextNormalizer.Normalize(r.SignalText),
                    SlotName      = r.SlotName,
                    ComponentType = r.ComponentType ?? ""
                });

        private double EvalAccuracy(ITransformer model, IDataView data)
        {
            var scored  = model.Transform(data);
            var metrics = _ml.MulticlassClassification.Evaluate(
                data:                     scored,
                labelColumnName:          "Label",
                scoreColumnName:          "Score",
                predictedLabelColumnName: "PredictedLabel");
            return metrics.MicroAccuracy;
        }
    }
}
