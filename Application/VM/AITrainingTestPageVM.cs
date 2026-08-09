using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using VIBN_Tools.Application.View;
using VIBN_Tools.ContainerGeneration.AI;
using VIBN_Tools.GlobalClasses;

namespace VIBN_Tools.Application.VM
{
    /// <summary>
    /// ViewModel für das KI-Training-/Test-Panel.
    ///
    /// VERBESSERUNGEN gegenüber Vorgänger:
    ///  - Train() und QuickTrain() waren nahezu identisch (~200 Zeilen doppelter Code).
    ///    Jetzt zusammengeführt in RunTrainAsync() → eine einzige Quelle der Wahrheit.
    ///  - Training läuft jetzt asynchron (Task.Run) → UI friert nicht mehr ein.
    ///  - EvaluationService bekommt jetzt useLightGbm übergeben (vorher immer SDCA).
    ///  - Corrections werden separat an TrainingService übergeben für korrekte Gewichtung.
    ///  - Normalize() aus TextNormalizer (shared).
    /// </summary>
    public class AITrainingTestPageVM : MvvmBase
    {
        // ===========================
        // Commands
        // ===========================
        public ICommand TrainCommand    => GetCommandBinding(_ => _ = RunTrainAsync(addXml: true));
        public ICommand CheckCommand    => GetCommandBinding(Check);

        public ICommand ExportConflictsCsvCommand   => GetCommandBinding(_ => ExportConflictsCsv());
        public ICommand ExportModelEntriesCsvCommand => GetCommandBinding(_ => ExportModelEntriesCsv());
        public ICommand ExportEvaluationCsvCommand  => GetCommandBinding(_ => ExportEvaluationCsv());
        public ICommand ExportAnalysisCsvCommand    => GetCommandBinding(_ => ExportAnalysisCsv());

        public ICommand OpenConflictCommand         => GetCommandBinding(OpenConflict);
        public ICommand ImproveConflictCommand      => GetCommandBinding(_ => ImproveSelectedConflict());
        public ICommand OpenNoiseFilterEditorCommand => GetCommandBinding(_ => OpenNoiseFilterEditor());
        public ICommand ToggleInfoCommand           => GetCommandBinding(_ => IsInfoOpen = !IsInfoOpen);
        public ICommand OpenTrainingFolderCommand   => GetCommandBinding(_ => OpenTrainingFolder());
        public ICommand RefreshAnalysisCommand      => GetCommandBinding(_ => RunAnalysis());
        public ICommand ImproveMultipleCommand      => GetCommandBinding(ImproveMultiple);

        // ===========================
        // Services
        // ===========================
        private readonly TrainingDatasetStore  _store      = new();
        private readonly TrainingService       _trainer    = new();
        private readonly PredictionService     _predictor  = new();
        private readonly EvaluationService     _evaluator  = new();
        private readonly TrainingDataAnalyzer  _analyzer   = new();
        private readonly ActionLogNoiseFilter  _noiseFilter = new();
        private readonly ComponentTypeNormalizer _typeNorm  = new();

        // ===========================
        // Settings
        // ===========================
        private double _confidenceThreshold = 0.85;
        public double ConfidenceThreshold
        {
            get => _confidenceThreshold;
            set
            {
                if (Math.Abs(_confidenceThreshold - value) > 1e-6)
                {
                    _confidenceThreshold = value;
                    OnPropertyChanged();
                    RecalculateUncertainFlags();
                }
            }
        }

        private bool _useLightGbm = true;
        public bool UseLightGbm
        {
            get => _useLightGbm;
            set { _useLightGbm = value; OnPropertyChanged(); }
        }

        private bool _autoTrainAfterImprove = true;
        public bool AutoTrainAfterImprove
        {
            get => _autoTrainAfterImprove;
            set { _autoTrainAfterImprove = value; OnPropertyChanged(); }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            private set { _isBusy = value; OnPropertyChanged(); }
        }

        // ===========================
        // Logging
        // ===========================
        private string _logText = "";
        public string LogText
        {
            get => _logText;
            set { _logText = value; OnPropertyChanged(); }
        }

        private void Log(string msg)
        {
            // UI-Thread-sicher (wird aus Task.Run aufgerufen)
            System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                LogText += msg + Environment.NewLine;
            });
            Debug.WriteLine(msg);
        }

        // ===========================
        // Trainingsdaten
        // ===========================
        public ObservableCollection<ModelEntry> ModelEntries { get; } = new();

        public class ModelEntry
        {
            public string SignalText    { get; set; } = "";
            public string SlotName     { get; set; } = "";
            public string ComponentType { get; set; } = "";
        }

        // ===========================
        // Konflikte
        // ===========================
        public ObservableCollection<ConflictRow> Conflicts  { get; } = new();
        public ObservableCollection<string>      KnownSlots { get; } = new();
        public ObservableCollection<string>      KnownTypes { get; } = new();

        private void RefreshKnownSlotsFromRows(IEnumerable<TrainingRow> rows)
        {
            var slots = rows
                .Select(r => r.SlotName)
                .Concat(LoadCorrections().Select(c => c.SlotName))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .ToList();

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                KnownSlots.Clear();
                foreach (var s in slots) KnownSlots.Add(s);
            });
        }

        private void RefreshKnownSlotsFromConflicts()
        {
            var slots = Conflicts
                .Select(c => c.SollSlot)
                .Concat(Conflicts.Select(c => c.PredictedSlot))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .ToList();
            KnownSlots.Clear();
            foreach (var s in slots) KnownSlots.Add(s);
        }

        private void RefreshKnownTypesFromRows(IEnumerable<TrainingRow> rows)
        {
            var types = rows
                .Select(r => r.ComponentType)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .ToList();

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                KnownTypes.Clear();
                foreach (var t in types) KnownTypes.Add(t);
            });
        }

        private void RefreshKnownTypesFromConflicts()
        {
            var types = Conflicts
                .Select(c => c.Type)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .ToList();
            KnownTypes.Clear();
            foreach (var t in types) KnownTypes.Add(t);
        }

        public class ConflictRow
        {
            public string   Signal         { get; set; } = "";
            public string   Type           { get; set; } = "";
            public string   SollType       { get; set; } = "";  // editierbar: korrigierter Typ für Training
            public string   SollSlot       { get; set; } = "";
            public string   PredictedSlot  { get; set; } = "";
            public double   Confidence     { get; set; }
            public string   Top3           { get; set; } = "";
            public string[] SmartSuggestions { get; set; } = Array.Empty<string>();
            public bool     IsConflict     { get; set; }
            public bool     IsUncertain    { get; set; }
        }

        private ConflictRow? _selectedConflict;
        public ConflictRow? SelectedConflict
        {
            get => _selectedConflict;
            set { _selectedConflict = value; OnPropertyChanged(); }
        }

        private int _conflictsCount;
        public int ConflictsCount
        {
            get => _conflictsCount;
            set { _conflictsCount = value; OnPropertyChanged(); }
        }

        private int _uncertainCount;
        public int UncertainCount
        {
            get => _uncertainCount;
            set { _uncertainCount = value; OnPropertyChanged(); }
        }

        private string _lastCheckedXmlPath = "";

        // ===========================
        // Evaluation
        // ===========================
        private string _evaluationSummary = "Noch keine Evaluation durchgeführt.";
        public string EvaluationSummary
        {
            get => _evaluationSummary;
            set { _evaluationSummary = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ConfusionCellVM>    ConfusionMatrix  { get; } = new();
        public ObservableCollection<string>             ConfusionLabels  { get; } = new();
        public ObservableCollection<ConfusionCell2D>    ConfusionCells2D { get; } = new();
        public ObservableCollection<FeatureImportanceItem> FeatureImportance { get; } = new();

        private int _maxConfusionCount;
        public int MaxConfusionCount
        {
            get => _maxConfusionCount;
            set { _maxConfusionCount = value; OnPropertyChanged(); }
        }

        public class ConfusionCellVM  { public string Actual { get; set; } = ""; public string Predicted { get; set; } = ""; public int Count { get; set; } }
        public class ConfusionCell2D  { public string Actual { get; set; } = ""; public string Predicted { get; set; } = ""; public int Count { get; set; } }
        public class FeatureImportanceItem { public string Feature { get; set; } = ""; public double Score { get; set; } }

        // ===========================
        // Analyse
        // ===========================
        public ObservableCollection<NameCountItem>       SlotDistribution          { get; } = new();
        public ObservableCollection<NameCountItem>       ComponentTypeDistribution { get; } = new();
        public ObservableCollection<InconsistentSignalVM> InconsistentSignals      { get; } = new();
        public ObservableCollection<BadWordStatVM>       BadWordStats              { get; } = new();
        public ObservableCollection<BadWordSampleVM>     BadWordSamples            { get; } = new();

        public class NameCountItem         { public string Name  { get; set; } = ""; public int Count    { get; set; } }
        public class InconsistentSignalVM  { public string Signal { get; set; } = ""; public string Slots { get; set; } = ""; }
        public class BadWordStatVM         { public string Word  { get; set; } = ""; public int Count    { get; set; }  public double Percent { get; set; } }
        public class BadWordSampleVM       { public string Signal { get; set; } = ""; public string BadWord { get; set; } = ""; }

        private int _badWordTotalRows;
        public int BadWordTotalRows
        {
            get => _badWordTotalRows;
            private set { _badWordTotalRows = value; OnPropertyChanged(); }
        }

        private List<string> _evaluationWarnings = new();
        public List<string> EvaluationWarnings
        {
            get => _evaluationWarnings;
            set { _evaluationWarnings = value; OnPropertyChanged(); }
        }

        // ===========================
        // Info Overlay
        // ===========================
        private bool _isInfoOpen;
        public bool IsInfoOpen
        {
            get => _isInfoOpen;
            set { _isInfoOpen = value; OnPropertyChanged(); }
        }

        // ===========================
        // Ctor
        // ===========================
        public AITrainingTestPageVM()
        {
            try { RunAnalysis(); } catch { /* ignorieren */ }
        }

        // ===========================
        // XML Loader
        // ===========================
        private List<(string Signal, string Slot, string Type)> LoadXmlEntries(string xmlPath)
        {
            var doc = XDocument.Load(xmlPath);
            return (from container in doc.Descendants("Container")
                    let type = (string?)container.Element("Type")
                    from entry in container.Descendants("Entry")
                    let signal = (string?)entry.Element("Signal")
                    let slot   = (string?)entry.Element("Slot")
                    select (Signal: signal ?? "", Slot: slot ?? "", Type: type ?? ""))
                   .ToList();
        }

        // ===========================
        // TRAIN / QUICK-TRAIN  ← zusammengeführt
        // ===========================

        /// <summary>
        /// Führt den kompletten Trainingsablauf asynchron durch.
        /// Ersetzt die vorherigen Train() und QuickTrain()-Methoden, die ~identical waren.
        /// </summary>
        /// <param name="addXml">
        ///   true  = Benutzer wählt eine neue XML → wird dem Trainingspool hinzugefügt.
        ///   false = Auto-Train nach Improve (kein neues XML nötig).
        /// </param>
        private async Task RunTrainAsync(bool addXml)
        {
            if (IsBusy)
            {
                Log("Training läuft bereits – bitte warten.");
                return;
            }

            try
            {
                IsBusy = true;

                string? xmlFile = null;
                if (addXml)
                {
                    xmlFile = SystemDialog.OpenSelectFileDialog("XML Files|*.xml");
                    if (string.IsNullOrEmpty(xmlFile))
                    {
                        Log("Training abgebrochen: keine XML ausgewählt.");
                        return;
                    }
                }

                await Task.Run(() =>
                {
                    var progress = new Progress<string>(msg => Log(msg));
                    string prefix = addXml ? "" : "[Auto-Train] ";

                    if (xmlFile != null)
                    {
                        Log($"{prefix}Füge XML zum Trainingspool hinzu:\n{xmlFile}");
                        var duplicate = _store.AddXml(xmlFile, progress);
                        if (duplicate != null)
                        {
                            Log($"{prefix}HINWEIS: Identische XML bereits im Pool als '{duplicate}' – Training verwendet den bestehenden Eintrag.");
                        }
                        // Zeige alle XMLs im Pool im Log
                        var poolXmls = _store.ListXmls();
                        Log($"{prefix}Trainingspool ({poolXmls.Count} XMLs):");
                        foreach (var xml in poolXmls)
                            Log($"  • {System.IO.Path.GetFileName(xml)}");
                    }

                    var logs = ModelPaths.AllActionLogs();  // Alle Logs (nicht nur 30 Tage)
                    var rows = _store.BuildRowsFromAllXmlsAndLogs(logs, progress);

                    foreach (var r in rows)
                        r.ComponentType = _typeNorm.Normalize(r.ComponentType);

                    var rawRows = rows.ToList();

                    // Noise-Filter (Soft-Cap, kein hartes Deduplizieren mehr)
                    var filtered = _noiseFilter.Filter(rawRows).ToList();

                    // Corrections SEPARAT übergeben → TrainingService gewichtet sie höher
                    var corrections = LoadCorrections();
                    foreach (var c in corrections)
                        c.ComponentType = _typeNorm.Normalize(c.ComponentType);

                    // UI: Trainingsdaten anzeigen
                    var allForUi = filtered.Concat(corrections).ToList();
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ModelEntries.Clear();
                        foreach (var r in allForUi)
                            ModelEntries.Add(new ModelEntry
                            {
                                SignalText    = r.SignalText,
                                SlotName      = r.SlotName,
                                ComponentType = r.ComponentType
                            });
                    });

                    RefreshKnownSlotsFromRows(filtered);
                    RefreshKnownTypesFromRows(filtered);

                    Log($"{prefix}Trainingsdaten: {filtered.Count} gefiltert + {corrections.Count} Korrekturen");

                    // Trainieren (Corrections separat für Gewichtung)
                    string newModel = ModelPaths.NewVersion();
                    _trainer.TrainFromRows(
                        filtered,
                        corrections,
                        newModel,
                        UseLightGbm,
                        correctionWeight: 5,
                        progress);
                    ModelPaths.SwitchToLatest(newModel);

                    Log($"{prefix}Modell aktiv: {ModelPaths.CurrentModel}");

                    // Evaluation – jetzt mit korrektem Trainer
                    var eval = _evaluator.Evaluate(
                        filtered.Concat(corrections.SelectMany(c => Enumerable.Repeat(c, 5))),
                        UseLightGbm,
                        0.2,
                        progress);

                    string evalSummary = string.IsNullOrEmpty(eval.Note)
                        ? $"MicroAcc={eval.MicroAccuracy:P2} | MacroAcc={eval.MacroAccuracy:P2} | Top3={eval.Top3Accuracy:P2} | LogLoss={eval.LogLoss:0.000}"
                        : eval.Note;

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        EvaluationSummary = evalSummary;
                        UpdateConfusionHeatmap(eval);
                    });

                    Log($"{prefix}Evaluation: {evalSummary}");

                    // Analyse
                    RunAnalysis(rawRows);

                    // Feature Importance
                    var fi = _trainer.GetFeatureImportanceAblation(filtered, UseLightGbm, progress);
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        FeatureImportance.Clear();
                        foreach (var f in fi.OrderByDescending(x => x.Score))
                            FeatureImportance.Add(new FeatureImportanceItem { Feature = f.FeatureName, Score = f.Score });
                    });

                    Log($"{prefix}Training abgeschlossen.");
                });
            }
            catch (Exception ex)
            {
                Log($"FEHLER beim Training: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ===========================
        // CHECK
        // ===========================
        private void Check(object _)
        {
            try
            {
                if (!File.Exists(ModelPaths.CurrentModel))
                {
                    Log("Kein Modell vorhanden. Bitte zuerst Train ausführen!");
                    return;
                }

                string xmlFile = SystemDialog.OpenSelectFileDialog("XML Files|*.xml");
                if (string.IsNullOrEmpty(xmlFile))
                {
                    Log("Check abgebrochen: keine XML ausgewählt.");
                    return;
                }

                _lastCheckedXmlPath = xmlFile;
                _predictor.Load(ModelPaths.CurrentModel);
                Log($"Modell geladen:\n{ModelPaths.CurrentModel}");

                var entries = LoadXmlEntries(xmlFile)
                    .Where(e => !string.IsNullOrWhiteSpace(e.Signal))
                    .ToList();

                Log($"Gefundene Einträge in XML: {entries.Count}");

                Conflicts.Clear();
                int conflicts = 0, uncertain = 0;

                foreach (var e in entries)
                {
                    var pred = _predictor.Predict(e.Signal, e.Type, e.Slot);
                    var topK = _predictor.GetTopK(pred.Score, 3);

                    bool isConflict = !string.Equals(pred.PredictedLabel, e.Slot, StringComparison.OrdinalIgnoreCase);
                    bool isUncertain = pred.Confidence < ConfidenceThreshold;

                    if (isConflict)      conflicts++;
                    else if (isUncertain) uncertain++;

                    Conflicts.Add(new ConflictRow
                    {
                        Signal           = e.Signal,
                        Type             = e.Type,
                        SollType         = e.Type,  // Startwert = XML-Typ, editierbar per Dropdown
                        SollSlot         = e.Slot,
                        PredictedSlot    = pred.PredictedLabel,
                        Confidence       = pred.Confidence,
                        Top3             = string.Join(", ", topK.Select(t => $"{t.label} ({t.score:P0})")),
                        SmartSuggestions = topK.Select(t => t.label).ToArray(),
                        IsConflict       = isConflict,
                        IsUncertain      = !isConflict && isUncertain
                    });
                }

                ConflictsCount = conflicts;
                UncertainCount = uncertain;
                RefreshKnownSlotsFromConflicts();
                RefreshKnownTypesFromConflicts();

                Log($"=== KI-CHECK abgeschlossen: Konflikte={conflicts}, Unsicher={uncertain} ===");
            }
            catch (Exception ex)
            {
                Log($"FEHLER beim KI-Check: {ex.Message}");
            }
        }

        // ===========================
        // Improve
        // ===========================
        private void ImproveSelectedConflict()
        {
            try
            {
                if (SelectedConflict == null)
                {
                    MessageBox.Show("Bitte zuerst einen Konflikt auswählen.", "Improve");
                    return;
                }
                SaveCorrection(SelectedConflict);
                Log($"Improve: '{SelectedConflict.Signal}' → Slot='{SelectedConflict.SollSlot}', Type='{SelectedConflict.SollType}' gespeichert.");

                if (AutoTrainAfterImprove)
                    _ = RunTrainAsync(addXml: false);
            }
            catch (Exception ex)
            {
                Log($"Improve fehlgeschlagen: {ex.Message}");
            }
        }

        private void ImproveMultiple(object parameter)
        {
            try
            {
                var sel = ExtractSelection(parameter);
                if (sel.Count == 0 && SelectedConflict != null)
                    sel = new List<ConflictRow> { SelectedConflict };

                if (sel.Count == 0)
                {
                    MessageBox.Show("Bitte mindestens einen Konflikt auswählen.", "Improve");
                    return;
                }

                foreach (var row in sel) SaveCorrection(row);
                Log($"Improve: {sel.Count} Korrekturen gespeichert.");

                if (AutoTrainAfterImprove)
                    _ = RunTrainAsync(addXml: false);
            }
            catch (Exception ex)
            {
                Log($"Improve (Mehrfach) fehlgeschlagen: {ex.Message}");
            }
        }

        private void SaveCorrection(ConflictRow row)
        {
            AppendCorrection(new TrainingRow
            {
                SignalText    = row.Signal,
                SlotName      = row.SollSlot,
                ComponentType = _typeNorm.Normalize(row.SollType),
                ComponentName = "",
                SignalId      = "",
                Address       = ""
            });
        }

        private static List<ConflictRow> ExtractSelection(object parameter)
        {
            var result = new List<ConflictRow>();
            if (parameter is System.Collections.IList ilist)
                foreach (var item in ilist)
                    if (item is ConflictRow r) result.Add(r);
            return result;
        }

        // ===========================
        // Corrections (CSV)
        // ===========================
        private string CorrectionsFilePath => ModelPaths.CorrectionsFile;

        private void AppendCorrection(TrainingRow row)
        {
            Directory.CreateDirectory(_store.RootFolder);
            bool newFile = !File.Exists(CorrectionsFilePath);
            using var sw = new StreamWriter(CorrectionsFilePath, append: true, Encoding.UTF8);
            if (newFile)
                sw.WriteLine("SignalText;SlotName;ComponentType;ComponentName;SignalId;Address");
            sw.WriteLine($"{Csv(row.SignalText)};{Csv(row.SlotName)};{Csv(row.ComponentType)};{Csv(row.ComponentName)};{Csv(row.SignalId)};{Csv(row.Address)}");
        }

        private List<TrainingRow> LoadCorrections()
        {
            var list = new List<TrainingRow>();
            if (!File.Exists(CorrectionsFilePath)) return list;

            foreach (var line in File.ReadAllLines(CorrectionsFilePath, Encoding.UTF8).Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var c = SplitCsv(line);
                list.Add(new TrainingRow
                {
                    SignalText    = c.ElementAtOrDefault(0) ?? "",
                    SlotName      = c.ElementAtOrDefault(1) ?? "",
                    ComponentType = c.ElementAtOrDefault(2) ?? "",
                    ComponentName = c.ElementAtOrDefault(3) ?? "",
                    SignalId      = c.ElementAtOrDefault(4) ?? "",
                    Address       = c.ElementAtOrDefault(5) ?? ""
                });
            }
            return list;
        }

        // ===========================
        // Confusion Heatmap
        // ===========================
        private void UpdateConfusionHeatmap(EvaluationResult eval)
        {
            if (eval == null) return;
            ConfusionMatrix.Clear();
            ConfusionLabels.Clear();
            ConfusionCells2D.Clear();

            int max = 0;
            var labelSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in eval.Confusion)
            {
                labelSet.Add(kv.Key.actual);
                labelSet.Add(kv.Key.predicted);
                if (kv.Value > max) max = kv.Value;
            }

            var labels = labelSet.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
            foreach (var l in labels) ConfusionLabels.Add(l);

            foreach (var kv in eval.Confusion)
                ConfusionMatrix.Add(new ConfusionCellVM { Actual = kv.Key.actual, Predicted = kv.Key.predicted, Count = kv.Value });

            var lookup = eval.Confusion.ToDictionary(kv => kv.Key, kv => kv.Value);
            foreach (var a in labels)
                foreach (var p in labels)
                    ConfusionCells2D.Add(new ConfusionCell2D
                    {
                        Actual    = a,
                        Predicted = p,
                        Count     = lookup.TryGetValue((a, p), out var val) ? val : 0
                    });

            MaxConfusionCount = max;
            OnPropertyChanged(nameof(ConfusionLabels));
            OnPropertyChanged(nameof(ConfusionCells2D));
        }

        // ===========================
        // Analyse + BadWord
        // ===========================
        private void RunAnalysis(List<TrainingRow>? rawRows = null)
        {
            try
            {
                if (rawRows == null)
                {
                    var logs = ModelPaths.AllActionLogs();  // Alle Logs (nicht nur 30 Tage)
                    var tmp  = _store.BuildRowsFromAllXmlsAndLogs(logs, null);
                    foreach (var r in tmp) r.ComponentType = _typeNorm.Normalize(r.ComponentType);
                    rawRows = tmp.ToList();
                }

                RunBadWordAnalysis(rawRows);

                var filtered = _noiseFilter.Filter(rawRows).ToList();
                filtered.AddRange(LoadCorrections());

                var analysis = _analyzer.Analyze(filtered);

                void Dispatch(Action a) => System.Windows.Application.Current.Dispatcher.Invoke(a);

                Dispatch(() =>
                {
                    SlotDistribution.Clear();
                    foreach (var c in analysis.SlotDistribution)
                        SlotDistribution.Add(new NameCountItem { Name = c.Name, Count = c.Count });

                    ComponentTypeDistribution.Clear();
                    foreach (var c in analysis.ComponentTypeDistribution)
                        ComponentTypeDistribution.Add(new NameCountItem { Name = c.Name, Count = c.Count });

                    InconsistentSignals.Clear();
                    foreach (var inc in analysis.InconsistentSignals)
                        InconsistentSignals.Add(new InconsistentSignalVM { Signal = inc.Signal, Slots = string.Join(", ", inc.Slots) });
                });

                // Warnungen
                var warnings = new List<string>();
                int totalRows = analysis.SlotDistribution.Sum(s => s.Count);

                if (totalRows < 50)
                    warnings.Add($"Sehr kleiner Datensatz ({totalRows} Zeilen): Evaluation kann zu optimistisch wirken.");

                foreach (var sl in analysis.SlotDistribution)
                {
                    if (sl.Count < 5)  warnings.Add($"Slot '{sl.Name}' kommt nur {sl.Count}× vor → Lernproblem wahrscheinlich.");
                    else if (sl.Count < 10) warnings.Add($"Slot '{sl.Name}' kommt nur {sl.Count}× vor → bitte mehr Trainingsdaten sammeln.");
                }

                foreach (var sl in analysis.SlotDistribution)
                {
                    double share = totalRows > 0 ? (double)sl.Count / totalRows : 0;
                    if (share > 0.40)
                        warnings.Add($"Slot '{sl.Name}' dominiert mit {share:P0} → Modell könnte verzerrt werden.");
                }

                foreach (var w in warnings) Log($"[WARNUNG] {w}");
                Dispatch(() => EvaluationWarnings = warnings);

                Log($"Analyse: Rows={analysis.TotalRows}, Slots={analysis.SlotDistribution.Count}, Inkonsistente={analysis.InconsistentSignals.Count}");
            }
            catch (Exception ex)
            {
                Log($"Analyse fehlgeschlagen: {ex.Message}");
            }
        }

        private void RunBadWordAnalysis(List<TrainingRow> rawRows)
        {
            try
            {
                var cfg   = NoiseFilterConfig.Load();
                var bads  = new HashSet<string>(cfg?.BadWords ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
                BadWordTotalRows = rawRows?.Count ?? 0;

                var counter = bads.ToDictionary(b => b, b => 0, StringComparer.OrdinalIgnoreCase);
                System.Windows.Application.Current.Dispatcher.Invoke(() => BadWordSamples.Clear());

                if (rawRows != null && bads.Count > 0)
                {
                    foreach (var r in rawRows)
                    {
                        var txt = r.SignalText ?? "";
                        if (txt.Length == 0) continue;
                        foreach (var bw in bads)
                        {
                            if (txt.IndexOf(bw, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                counter[bw]++;
                                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                {
                                    if (BadWordSamples.Count < 200)
                                        BadWordSamples.Add(new BadWordSampleVM { Signal = txt, BadWord = bw });
                                });
                                break;
                            }
                        }
                    }
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    BadWordStats.Clear();
                    if (BadWordTotalRows > 0)
                        foreach (var kv in counter.OrderByDescending(k => k.Value).Where(k => k.Value > 0))
                            BadWordStats.Add(new BadWordStatVM { Word = kv.Key, Count = kv.Value, Percent = (double)kv.Value / BadWordTotalRows });
                });
            }
            catch (Exception ex) { Log($"BadWord-Analyse fehlgeschlagen: {ex.Message}"); }
        }

        // ===========================
        // NoiseFilter Editor
        // ===========================
        // ===========================
        // Trainingsordner im Explorer öffnen
        // ===========================
        private void OpenTrainingFolder()
        {
            try
            {
                var path = _store.RootFolder;
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName        = path,
                    UseShellExecute = true   // öffnet den Windows Explorer
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Konnte Ordner nicht öffnen: {ex.Message}", "Fehler");
            }
        }

        private void OpenNoiseFilterEditor()
        {
            try
            {
                var dlg = new NoiseFilterEditor { Owner = System.Windows.Application.Current.MainWindow };
                if (dlg.ShowDialog() == true)
                {
                    _noiseFilter.Reload();
                    RunAnalysis();
                    Log("Noise-Filter aktualisiert → Analyse neu berechnet.");
                }
            }
            catch (Exception ex) { MessageBox.Show($"Noise-Filter Editor Fehler: {ex.Message}", "Fehler"); }
        }

        // ===========================
        // OpenConflict (Notepad++)
        // ===========================
        private void OpenConflict(object parameter)
        {
            if (parameter is not ConflictRow row) return;
            if (string.IsNullOrWhiteSpace(_lastCheckedXmlPath) || !File.Exists(_lastCheckedXmlPath))
            {
                MessageBox.Show("Die geprüfte XML-Datei ist nicht mehr verfügbar.", "Öffnen");
                return;
            }
            try
            {
                int line = FindFirstLineNumber(_lastCheckedXmlPath, row.Signal);
                var npp  = FindNotepadPlusPlus();

                if (!string.IsNullOrEmpty(npp) && (npp.Equals("notepad++.exe", StringComparison.OrdinalIgnoreCase) || File.Exists(npp)) && line > 0)
                {
                    Process.Start(new ProcessStartInfo { FileName = npp, Arguments = $"-n {line} \"{_lastCheckedXmlPath}\"", UseShellExecute = false });
                    Log($"Notepad++ geöffnet @ Zeile {line}");
                }
                else
                {
                    Process.Start(new ProcessStartInfo { FileName = _lastCheckedXmlPath, UseShellExecute = true });
                    Log("XML geöffnet (Standardeditor).");
                }
                Clipboard.SetText(row.Signal);
            }
            catch (Exception ex) { MessageBox.Show($"Konnte XML nicht öffnen: {ex.Message}", "Öffnen"); }
        }

        private static int FindFirstLineNumber(string filePath, string search)
        {
            int line = 0;
            foreach (var l in File.ReadLines(filePath, Encoding.UTF8))
            {
                line++;
                if (!string.IsNullOrEmpty(search) && l.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                    return line;
            }
            return -1;
        }

        private static string FindNotepadPlusPlus()
        {
            var candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),   "Notepad++", "notepad++.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Notepad++", "notepad++.exe"),
                "notepad++.exe"
            };
            foreach (var c in candidates)
                if (c.Equals("notepad++.exe", StringComparison.OrdinalIgnoreCase) || File.Exists(c))
                    return c;
            return "";
        }

        // ===========================
        // Exporte
        // ===========================
        private void ExportConflictsCsv()
        {
            if (Conflicts.Count == 0) { MessageBox.Show("Keine Konfliktdaten vorhanden.", "Export"); return; }
            var dlg = new SaveFileDialog { Title = "Konflikte exportieren", Filter = "CSV|*.csv", FileName = $"conflicts_{DateTime.Now:yyyyMMdd_HHmm}.csv" };
            if (dlg.ShowDialog() != true) return;
            using var sw = new StreamWriter(dlg.FileName, false, Encoding.UTF8);
            sw.WriteLine("Signal;Type;SollSlot;Predicted;Confidence;IsConflict;IsUncertain;Top3");
            foreach (var r in Conflicts)
                sw.WriteLine($"{Csv(r.Signal)};{Csv(r.Type)};{Csv(r.SollSlot)};{Csv(r.PredictedSlot)};{r.Confidence:0.000};{r.IsConflict};{r.IsUncertain};{Csv(r.Top3)}");
            Log($"Konflikte exportiert: {dlg.FileName}");
        }

        private void ExportModelEntriesCsv()
        {
            if (ModelEntries.Count == 0) { MessageBox.Show("Keine Trainingsdaten vorhanden.", "Export"); return; }
            var dlg = new SaveFileDialog { Title = "Trainingsdaten exportieren", Filter = "CSV|*.csv", FileName = $"trainingdata_{DateTime.Now:yyyyMMdd_HHmm}.csv" };
            if (dlg.ShowDialog() != true) return;
            using var sw = new StreamWriter(dlg.FileName, false, Encoding.UTF8);
            sw.WriteLine("Signal;Slot;Type");
            foreach (var r in ModelEntries)
                sw.WriteLine($"{Csv(r.SignalText)};{Csv(r.SlotName)};{Csv(r.ComponentType)}");
            Log($"Trainingsdaten exportiert: {dlg.FileName}");
        }

        private void ExportEvaluationCsv()
        {
            if (ConfusionMatrix.Count == 0) { MessageBox.Show("Keine Evaluationsdaten vorhanden.", "Export"); return; }
            var dlg = new SaveFileDialog { Title = "Evaluation exportieren", Filter = "CSV|*.csv", FileName = $"evaluation_{DateTime.Now:yyyyMMdd_HHmm}.csv" };
            if (dlg.ShowDialog() != true) return;
            using var sw = new StreamWriter(dlg.FileName, false, Encoding.UTF8);
            sw.WriteLine($"Summary;{Csv(EvaluationSummary)}");
            sw.WriteLine();
            sw.WriteLine("Actual;Predicted;Count");
            foreach (var c in ConfusionMatrix.OrderBy(v => v.Actual).ThenBy(v => v.Predicted))
                sw.WriteLine($"{Csv(c.Actual)};{Csv(c.Predicted)};{c.Count}");
            Log($"Evaluation exportiert: {dlg.FileName}");
        }

        private void ExportAnalysisCsv()
        {
            var dlg = new SaveFileDialog { Title = "Analyse exportieren", Filter = "CSV|*.csv", FileName = $"analysis_{DateTime.Now:yyyyMMdd_HHmm}.csv" };
            if (dlg.ShowDialog() != true) return;
            using var sw = new StreamWriter(dlg.FileName, false, Encoding.UTF8);
            sw.WriteLine("=== Slot Distribution ==="); sw.WriteLine("Slot;Count");
            foreach (var s in SlotDistribution) sw.WriteLine($"{Csv(s.Name)};{s.Count}");
            sw.WriteLine();
            sw.WriteLine("=== ComponentType Distribution ==="); sw.WriteLine("Type;Count");
            foreach (var s in ComponentTypeDistribution) sw.WriteLine($"{Csv(s.Name)};{s.Count}");
            sw.WriteLine();
            sw.WriteLine("=== Inconsistent Signals ==="); sw.WriteLine("Signal;Slots");
            foreach (var i in InconsistentSignals) sw.WriteLine($"{Csv(i.Signal)};{Csv(i.Slots)}");
            sw.WriteLine();
            sw.WriteLine("=== BadWord Statistik ==="); sw.WriteLine("BadWord;Count;Percent");
            foreach (var bw in BadWordStats) sw.WriteLine($"{Csv(bw.Word)};{bw.Count};{bw.Percent:P2}");
            Log($"Analyse exportiert: {dlg.FileName}");
        }

        // ===========================
        // Helpers
        // ===========================
        private void RecalculateUncertainFlags()
        {
            int conflicts = 0, uncertain = 0;
            foreach (var r in Conflicts)
            {
                r.IsUncertain = !r.IsConflict && r.Confidence < ConfidenceThreshold;
                if (r.IsConflict)      conflicts++;
                else if (r.IsUncertain) uncertain++;
            }
            ConflictsCount = conflicts;
            UncertainCount = uncertain;
            OnPropertyChanged(nameof(Conflicts));
        }

        private static string Csv(string? s)
        {
            if (s == null) return "";
            bool needsQuotes = s.Contains(';') || s.Contains('"') || s.Contains('\n') || s.Contains('\r');
            s = s.Replace("\"", "\"\"");
            return needsQuotes ? $"\"{s}\"" : s;
        }

        private static string[] SplitCsv(string line)
        {
            var list   = new List<string>();
            var sb     = new StringBuilder();
            bool quoted = false;
            foreach (var ch in line)
            {
                if (ch == '"') { quoted = !quoted; continue; }
                if (ch == ';' && !quoted) { list.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(ch);
            }
            list.Add(sb.ToString());
            return list.ToArray();
        }
    }
}
