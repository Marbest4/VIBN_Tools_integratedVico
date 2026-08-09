namespace VIBN_Tools.ContainerGeneration.AI
{
    /// <summary>
    /// Zentraler Text-Normalisierer für alle ML-Komponenten.
    /// Vorher war Normalize() in TrainingService, PredictionService, EvaluationService
    /// und TrainingDataAnalyzer je separat definiert – Inkonsistenz-Risiko!
    /// Ab sofort: eine einzige Quelle der Wahrheit.
    /// </summary>
    public static class TextNormalizer
    {
        /// <summary>
        /// Normalisiert einen Signal-Text für das ML-Modell.
        /// Umlaute werden transkribiert (ä→ae usw.), Leerzeichen getrimmt,
        /// alles kleingeschrieben. So matchen Training und Prediction identisch.
        /// </summary>
        public static string Normalize(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;

            return s.Trim()
                    .ToLowerInvariant()
                    .Replace("ä", "ae")
                    .Replace("ö", "oe")
                    .Replace("ü", "ue")
                    .Replace("ß", "ss");
        }
    }
}
