using System.IO;

namespace VIBN_Tools.ContainerGeneration.AI
{
    /// <summary>
    /// Verwaltet den persistenten Pool an Trainings-XMLs.
    ///
    /// PFAD (relativ zur .exe):
    ///   {ExeDir}\vibn_ai_data\training_pool\
    ///
    /// Wird von ModelPaths.TrainingPoolDir gesteuert –
    /// zentraler Konfigurationsort fuer alle Pfade.
    /// </summary>
    public sealed class TrainingDatasetStore
    {
        public string RootFolder { get; }

        public TrainingDatasetStore(string? rootOverride = null)
        {
            // Pfad kommt aus ModelPaths – dort zentral aendern
            RootFolder = rootOverride ?? ModelPaths.TrainingPoolDir;
            Directory.CreateDirectory(RootFolder);
        }

        /// <summary>
        /// Fueget eine XML zum Pool hinzu. Prueft vorher auf Duplikate.
        /// </summary>
        /// <returns>
        ///   null = erfolgreich hinzugefuegt.
        ///   string = Name der bereits vorhandenen Duplikat-Datei (keine Aktion ausgefuehrt).
        /// </returns>
        public string? AddXml(string sourcePath, IProgress<string>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
                throw new FileNotFoundException("XML nicht gefunden.", sourcePath);

            // Duplikat-Pruefung per MD5-Hash
            var duplicate = ModelPaths.FindDuplicateXml(sourcePath, RootFolder);
            if (duplicate != null)
            {
                progress?.Report($"HINWEIS: Identische XML bereits im Pool: {duplicate} – wird nicht erneut hinzugefuegt.");
                return duplicate;
            }

            var fileName   = Path.GetFileNameWithoutExtension(sourcePath);
            var ext        = Path.GetExtension(sourcePath);
            var targetName = $"{Sanitize(fileName)}_{DateTime.Now:yyyyMMdd_HHmmss}{ext}";
            var targetPath = Path.Combine(RootFolder, targetName);

            File.Copy(sourcePath, targetPath, overwrite: false);
            progress?.Report($"XML zum Trainingspool hinzugefuegt: {targetPath}");
            return null;   // kein Duplikat
        }

        public IReadOnlyList<string> ListXmls()
        {
            if (!Directory.Exists(RootFolder)) return Array.Empty<string>();
            return Directory.EnumerateFiles(RootFolder, "*.xml", SearchOption.TopDirectoryOnly)
                            .OrderBy(p => p)
                            .ToList();
        }

        /// <summary>
        /// Liest alle XMLs im Store + ActionLogs und baut kombinierte Trainingszeilen.
        /// AENDERUNG: Verwendet jetzt ModelPaths.AllActionLogs() statt nur letzter 30 Tage.
        /// </summary>
        public List<TrainingRow> BuildRowsFromAllXmlsAndLogs(
            IEnumerable<string> logs,
            IProgress<string>? progress = null)
        {
            var xmls = ListXmls();
            progress?.Report($"Trainingspool: {xmls.Count} XML-Datei(en) gefunden.");

            var all = new List<TrainingRow>();

            foreach (var xml in xmls)
            {
                var rows = ExportXmlParser.Read(xml)
                    .Where(r => !string.IsNullOrWhiteSpace(r.SlotName))
                    .Select(r => new TrainingRow
                    {
                        SignalText    = r.SignalText,
                        SlotName      = r.SlotName,
                        ComponentType = r.ComponentType ?? ""
                    });
                all.AddRange(rows);
            }

            var logList = logs.ToList();
            progress?.Report($"ActionLogs: {logList.Count} Datei(en) gefunden.");

            var corrected = ActionLogReader.ToTrainingRows(logList)
                .Where(r => !string.IsNullOrWhiteSpace(r.SlotName))
                .Select(r => new TrainingRow
                {
                    SignalText    = r.SignalText,
                    SlotName      = r.SlotName,
                    ComponentType = r.ComponentType ?? ""
                });

            var corrList = corrected.ToList();
            all.AddRange(corrList);

            progress?.Report($"Gesamt-Trainingszeilen (XML + Logs): {all.Count}");
            return all;
        }

        private static string Sanitize(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}
