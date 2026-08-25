using Microsoft.Win32;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using VIBN_Tools.ContainerGeneration.BusinessLogic;
using VIBN_Tools.ContainerGeneration.BusinessLogic.ContainerData;
using VIBN_Tools.ContainerGeneration.BusinessLogic.RequirementsXml;
using VIBN_Tools.ContainerGeneration.BusinessLogic.ZuLiData;
using VIBN_Tools.ContainerGeneration.BusinessLogic.ZuLiData.Interfaces;
using VIBN_Tools.ContainerGeneration.Models;
using VIBN_Tools.ContainerGeneration.Utils;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.ContainerGeneration.AI;

namespace VIBN_Tools.Application.VM
{
    /// <summary>
    /// Coordinates the container-generation workspace: input files, settings,
    /// generation, validation, review, undo/redo and export. The domain rules
    /// remain in <c>ContainerGeneration/BusinessLogic</c>.
    /// </summary>
    public class ContainerGenerationPageVM : MvvmBase
    {



        //===========================================================================================================================
        // B I N D I N G S   -   B U T T O N S   /   C O M M A N D S
        //===========================================================================================================================

        public ICommand OpenInterfaceFile => GetCommandBindingAsync(Open_InterfaceFile);
        public ICommand OpenRequirementsXml => GetCommandBindingAsync(Open_RequirementsXml);

        public ICommand LoadSettings => GetCommandBindingAsync(Load_Settings);
        public ICommand SaveSettings => GetCommandBinding(Save_Settings);

        public ICommand GenerateContainers => GetCommandBindingAsync(Generate_Containers);
        public ICommand ValidateWorkspace => GetCommandBinding(Validate_Workspace);
        public ICommand ExportContainers => GetCommandBinding(Export_Containers);

        public ICommand LoadData => GetCommandBinding(Load_Data);

        public ICommand SaveData => GetCommandBinding(Save_Data);




        public ICommand DeleteItem => GetCommandBinding(Delete_Item);
        public ICommand UndoLastAction => GetCommandBinding(Undo_LastAction);
        public ICommand RedoLastAction => GetCommandBinding(Redo_LastAction);
        public ICommand ClearActivityLog => GetCommandBinding(Clear_ActivityLog);
        public ICommand ApplyReimportSelection => GetCommandBinding(Apply_ReimportSelection);
        public ICommand CancelReimportSelection => GetCommandBinding(Cancel_ReimportSelection);
        public ICommand SelectAllReimportChanges => GetCommandBinding(
            () => SetAllReimportChanges(true));
        public ICommand SelectNoReimportChanges => GetCommandBinding(
            () => SetAllReimportChanges(false));



        /// <summary>
        /// Validate if a container generation is possible. Causes the corresponding button to be enabled or not.
        /// </summary>
        public bool CanGenerate => Zuli.Items.Count > 0 && RequirementsFile.IsInitialized && !WasGenerated;


        private bool _wasgenerated;
        public bool WasGenerated
        {
            get { return _wasgenerated; }
            set 
            { 
                _wasgenerated = value; 
                OnPropertyChanged(nameof(CanGenerate));
            }
        }



        private bool _isBusyGenerateContainers;
        public bool IsBusyGenerateContainers
        {
            get { return _isBusyGenerateContainers; }
            set { _isBusyGenerateContainers = value; OnPropertyChanged(); }
        }


        /// <summary>
        /// Validate if a loading data is possible. Causes the corresponding button to be enabled or not.
        /// </summary>
        public bool CanLoadData => RequirementsFile.IsInitialized;




        public ICommand ContainerDataGridPreviewKeyDown => GetCommandBinding(ContainerGrid_OnPreviewKeyDown);
        public ICommand ContainerDataGridDragOver => GetCommandBinding(ContainerGrid_OnDragOver);
        public ICommand DataGridDrop => GetCommandBinding(Datagrid_OnDrop);
        public ICommand DataGridMouseMove => GetCommandBinding(Datagrid_OnMouseMove);




        //===========================================================================================================================
        // B I N D I N G S   -   D A T A G R I D   E N T R I E S
        //===========================================================================================================================

        /// <summary>
        /// Gets or sets the filtered entries list displayed on the filtered entries grid. Notifies the UI on change.
        /// </summary
        private ObservableCollection<ContainerEntry> _filteredEntries = [];
        public ObservableCollection<ContainerEntry> FilteredEntries
        {
            get => _filteredEntries;
            set
            {
                _filteredEntries = value;
                OnPropertyChanged(nameof(FilteredEntries));
            }
        }

        /// <summary>
        /// Gets or sets the currently selected element from the unassigned entries grid. Notifies the UI on change.
        /// </summary
        private ContainerEntry? _selectedFilteredEntry;
        public ContainerEntry? SelectedFilteredEntry
        {
            get => _selectedFilteredEntry;
            set
            {
                _selectedFilteredEntry = value;
                OnPropertyChanged(nameof(SelectedFilteredEntry));
            }
        }



        /// <summary>
        /// Gets or sets the unassigned entries list displayed on the unassigned entries grid. Notifies the UI on change.
        /// </summary
        private ObservableCollection<ContainerEntry> _unassignedEntries = [];
        public ObservableCollection<ContainerEntry> UnassignedEntries
        {
            get => _unassignedEntries;
            set
            {
                _unassignedEntries.CollectionChanged -= UpdateUICount;
                _unassignedEntries = value;
                _unassignedEntries.CollectionChanged += UpdateUICount;
                OnPropertyChanged(nameof(UnassignedEntries));
            }
        }

        /// <summary>
        /// Gets or sets the currently selected element from the unassigned entries grid. Notifies the UI on change.
        /// </summary
        private ContainerEntry? _selectedUnassignedEntry;
        public ContainerEntry? SelectedUnassignedEntry
        {
            get => _selectedUnassignedEntry;
            set
            {
                _selectedUnassignedEntry = value;
                OnPropertyChanged(nameof(SelectedUnassignedEntry));
            }
        }



        /// <summary>
        /// Gets or sets the container list displayed on the container grid. Notifies the UI on change.
        /// </summary
        private ObservableCollection<ContainerData> _containerList = [];
        public ObservableCollection<ContainerData> ContainerList
        {
            get => _containerList;
            set
            {
                _containerList.CollectionChanged -= UpdateUICount;
                _containerList = value;
                _containerList.CollectionChanged += UpdateUICount;
                OnPropertyChanged(nameof(ContainerList));
            }
        }

        /// <summary>
        /// Gets or sets the currently selected element from the container grid. Notifies the UI on change.
        /// </summary
        private List<ContainerData> _selectedContainers = [];
        public List<ContainerData> SelectedContainers
        {
            get => _selectedContainers;
            set
            {
                _selectedContainers = value;
                OnPropertyChanged(nameof(SelectedContainers));
            }
        }


        public ICommand SelectionChangedExecuted => GetCommandBinding(SelectionChanged_Executed);







        //===========================================================================================================================
        // B I N D I N G S   -   F I L T E R   T E X T
        //===========================================================================================================================


        /// <summary>
        /// Gets or sets the search text for container grid. Notifies the UI on change.
        /// </summary
        private string _searchTextContainer = string.Empty;
        public string SearchTextContainer
        {
            get => _searchTextContainer;
            set
            {
                _searchTextContainer = value;
                OnPropertyChanged();

                // Start Debounce
                _debounceTimerContainerData.Stop();
                _debounceTimerContainerData.Start();

            }
        }

        /// <summary>
        /// Gets or sets the search text for unassigned entries grid. Notifies the UI on change.
        /// </summary
        private string _searchTextUnassignedEntries = string.Empty;
        public string SearchTextUnassignedEntries
        {
            get => _searchTextUnassignedEntries;
            set
            {
                _searchTextUnassignedEntries = value;
                OnPropertyChanged();

                // Start Debounce
                _debounceTimerUnassignedData.Stop();
                _debounceTimerUnassignedData.Start();

            }
        }

        /// <summary>
        /// Gets or sets the search text for filtered entries grid. Notifies the UI on change.
        /// </summary
        private string _searchTextFilteredEntries = string.Empty;
        public string SearchTextFilteredEntries
        {
            get => _searchTextFilteredEntries;
            set
            {
                _searchTextFilteredEntries = value;
                OnPropertyChanged();

                // Start Debounce
                _debounceTimerFilteredData.Stop();
                _debounceTimerFilteredData.Start();

            }
        }



        //===========================================================================================================================
        // P R O P E R T I E S   O F   V I E W - M O D E L
        //===========================================================================================================================

        private ContainerGenerationSettings _settings = new();
        public ContainerGenerationSettings Settings
        {
            get { return _settings; }
            set
            {
                _settings = value;
                OnPropertyChanged();
            }
        }


        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();



        // DispatcherTimer for filtering SimObjects with Debounce
        // ── ActionLogger: protokolliert jede Drag-and-Drop-Aktion sofort auf Disk ──
        // Speicherort: {ExeOrdner}\vibn_ai_data\actions\YYYYMMDD.jsonl
        // Die Logs werden beim naechsten Training/Check automatisch eingelesen.
        private readonly ActionLogger _actionLogger = new ActionLogger();
        private GenerationWorkspaceSnapshot? _pendingReimportSnapshot;
        private readonly Dictionary<ContainerEntry, EventHandler> _slotChangedHandlers = new();
        private readonly Dictionary<ContainerEntry, EventHandler<SignalClearedEventArgs>>
            _signalClearedHandlers = new();
        private readonly Dictionary<ContainerEntry, EventHandler<WorkspaceValueChangingEventArgs>>
            _entryChangingHandlers = new();
        private readonly Dictionary<ContainerData, EventHandler<WorkspaceValueChangingEventArgs>>
            _containerChangingHandlers = new();
        private readonly Dictionary<ContainerData, PropertyChangedEventHandler>
            _containerPropertyChangedHandlers = new();
        private readonly Dictionary<ContainerData, System.Collections.Specialized.NotifyCollectionChangedEventHandler>
            _containerEntryCollectionHandlers = new();
        private readonly Dictionary<ContainerEntry, string> _lastKnownSlot = new();
        private readonly List<WorkspaceUndoState> _undoHistory = [];
        private readonly List<WorkspaceUndoState> _redoHistory = [];
        private const int MaximumUndoActions = 20;
        private const int MaximumActivityLogEntries = 250;
        private bool _suppressUndoCapture;
        private bool _isRestoringWorkspace;

        private List<ContainerData>? _pendingGeneratedContainers;
        private List<ContainerEntry>? _pendingGeneratedUnassigned;
        private List<ContainerEntry>? _pendingGeneratedFiltered;
        private ReimportSummary? _pendingReimportSummary;

        public ObservableCollection<ReimportDifference> PendingReimportChanges { get; } = [];
        public ObservableCollection<WorkspaceActivityLogEntry> ActivityLog { get; } = [];

        public bool HasPendingReimportChanges => PendingReimportChanges.Count > 0;
        public bool CanUndo => _undoHistory.Count > 0;
        public bool CanRedo => _redoHistory.Count > 0;
        public string UndoDescription =>
            CanUndo
                ? $"Rückgängig: {_undoHistory[^1].Description}"
                : "Keine Änderung zum Rückgängigmachen";
        public string RedoDescription =>
            CanRedo
                ? $"Wiederholen: {_redoHistory[^1].Description}"
                : "Keine Änderung zum Wiederholen";
        public bool HasActivityLog => ActivityLog.Count > 0;
        public string ActivityLogHeader =>
            $"Aktivitätsprotokoll ({ActivityLog.Count})";
        public string PendingReimportSelectionSummary =>
            PendingReimportChanges.Count == 0
                ? string.Empty
                : $"{PendingReimportChanges.Count} Unterschiede erkannt: " +
                  $"{PendingReimportChanges.Count(change => change.IsAccepted)} werden übernommen, " +
                  $"{PendingReimportChanges.Count(change => !change.IsAccepted)} werden nicht übernommen.";

        private string _reimportNotice = string.Empty;
        public string ReimportNotice
        {
            get => _reimportNotice;
            private set
            {
                if (string.Equals(_reimportNotice, value, StringComparison.Ordinal))
                    return;

                _reimportNotice = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasReimportNotice));
                OnPropertyChanged(nameof(ShowReimportNotice));
            }
        }

        public bool HasReimportNotice => !string.IsNullOrWhiteSpace(ReimportNotice);
        public bool ShowReimportNotice =>
            HasReimportNotice && !HasPendingReimportChanges;

        public ObservableCollection<WorkspaceReviewFilterOption> ReviewFilterOptions { get; } =
        [
            new(WorkspaceReviewFilter.All, "Alle"),
            new(WorkspaceReviewFilter.NeedsReview, "Prüfen"),
            new(WorkspaceReviewFilter.Changed, "Geändert / neu"),
            new(WorkspaceReviewFilter.ManuallyEdited, "Manuell bearbeitet"),
            new(WorkspaceReviewFilter.Unchecked, "Nicht abgehakt"),
            new(WorkspaceReviewFilter.Invalid, "Ungültig")
        ];

        private WorkspaceReviewFilterOption? _selectedReviewFilter;
        public WorkspaceReviewFilterOption? SelectedReviewFilter
        {
            get => _selectedReviewFilter;
            set
            {
                if (ReferenceEquals(_selectedReviewFilter, value))
                    return;

                _selectedReviewFilter = value;
                OnPropertyChanged();
                RefreshAllWorkspaceFilters();
            }
        }

        private readonly DispatcherTimer _debounceTimerContainerData;
        private readonly DispatcherTimer _debounceTimerUnassignedData;
        private readonly DispatcherTimer _debounceTimerFilteredData;





        /// <summary>
        /// Gets the count of the currently assigned signals
        /// </summary>
        public int AssignedSignals
        {
            get
            {
                return ContainerList.SelectMany(x => x.DataList).Count();
            }
        }

        /// <summary>
        /// Gets the percent of signals assigned
        /// </summary>
        public double PercentComplete
        {
            get
            {
                int Signals = AssignedSignals;

                int Total = Signals + UnassignedEntries.Count;
                if (Total > 0)
                {
                    return 1.0 * Signals / Total;
                }
                else
                {
                    return 0;
                }
            }
        }


        /// <summary>
        /// Gets or sets the status text message. Notifies the UI on change.
        /// </summary
        private string _statusText = string.Empty;
        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                OnPropertyChanged(nameof(StatusText));
            }
        }



        /// <summary>
        /// Collection of component types. Contains all types which were found after reading a AutoCreateFile.
        /// </summary>
        private ObservableCollection<string> _componentTypes = [];
        public ObservableCollection<string> ComponentTypes
        {
            get => _componentTypes;
            set
            {
                _componentTypes = value;
                OnPropertyChanged();
            }
        }


        /// <summary>
        /// List of container entries read from the ZuLi.
        /// </summary>
        public IZuLiData<ContainerEntry> Zuli { get; private set; }

        /// <summary>
        /// Instance of a AutoCreate XML.
        /// </summary>
        public IRequirementsXml RequirementsFile { get; private set; }

        /// <summary>
        /// Module to generate container data.
        /// </summary>
        public ContainerGenerator ContainerGenerator { get; private set; }









        //===========================================================================================================================
        // C O N S T R U C T O R
        //===========================================================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerGenerationPageVM"/> class.
        /// This constructor sets up the default settings and initializes various components.
        /// </summary>
        public ContainerGenerationPageVM()
        {
            Settings = new ContainerGenerationSettings();

            FilteredEntries = new ObservableCollection<ContainerEntry>();
            UnassignedEntries = new ObservableCollection<ContainerEntry>();
            ContainerList = new ObservableCollection<ContainerData>();
            ContainerList.CollectionChanged += ContainerList_CollectionChanged;

            SelectedContainers = new List<ContainerData>();

            ComponentTypes = new ObservableCollection<string>();


            // Configure and initialise Debounce-Timer
            _debounceTimerContainerData = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _debounceTimerContainerData.Tick += (sender, eventArgs) =>
            {
                _debounceTimerContainerData.Stop();
                FilterContainerGrid();
            };

            _debounceTimerUnassignedData = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _debounceTimerUnassignedData.Tick += (sender, eventArgs) =>
            {
                _debounceTimerUnassignedData.Stop();
                FilterUnassignedEntriesGrid();
            };

            _debounceTimerFilteredData = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _debounceTimerFilteredData.Tick += (sender, eventArgs) =>
            {
                _debounceTimerFilteredData.Stop();
                FilterFilteredEntriesGrid();
            };

            SelectedReviewFilter = ReviewFilterOptions[0];


            StatusText = string.Empty;

            Zuli = new ZuLiDefault();

            RequirementsFile = new RequirementsXml();
            RequirementsProvider.RequirementsFile = this.RequirementsFile;


            ContainerGenerator = new ContainerGenerator();

            LoadDefaultSettings();

            AddActivity(
                "System",
                "Container-Generator geöffnet",
                "Neue Bearbeitungssitzung gestartet.");


        }





        //===========================================================================================================================
        // M E T H O D S   ( B U T T O N S )
        //===========================================================================================================================

        /// <summary>
        /// Opens and processes an ZuLi Excel file.
        /// This method opens a file dialog to select an a ZuLi Excel file, reads the file asynchronously and
        /// updates the status text based on the result.
        /// </summary>
        private async Task Open_InterfaceFile(object parameter)
        {
            string excelFilter = "Excel Files (*.xls;*.xlsx;*.xlsm)|*.xls;*.xlsx;*.xlsm";
            string filePath = SystemDialog.OpenSelectFileDialog(excelFilter);
            if (string.IsNullOrEmpty(filePath))
                return;

            var importMode = AskForImportMode("ZuLi");
            if (importMode == ImportMode.Cancel)
                return;

            var importedZuli = new ZuLiDefault();
            var result = await importedZuli.ReadFromFileAsync(filePath);
            if (!result.IsSuccess)
            {
                StatusText = result.ErrorMessage;
                return;
            }

            Zuli = importedZuli;
            EnsureSignalIds(Zuli.Items);
            Settings.PathZuli = filePath;
            CommitSuccessfulImport(importMode, "ZuLi");
            OnPropertyChanged(nameof(CanGenerate));
        }

        private async Task Open_RequirementsXml(object parameter)
        {
            string xmlFilter = "XML File (*.xml)|*.xml";
            string filePath = SystemDialog.OpenSelectFileDialog(xmlFilter);
            if (string.IsNullOrEmpty(filePath))
                return;

            var importMode = AskForImportMode("Requirements-XML");
            if (importMode == ImportMode.Cancel)
                return;

            var importedRequirements = new RequirementsXml();
            var result = await importedRequirements.ReadFromFileAsync(filePath);
            if (result.IsSuccess)
            {
                RequirementsFile = importedRequirements;
                RequirementsProvider.RequirementsFile = RequirementsFile;
                Settings.PathRequirementsXml = filePath;
                ComponentTypes.Clear();
                var components = RequirementsFile.GetComponentTypes().OrderBy(x => x).ToList();
                foreach (var component in components)
                {
                    ComponentTypes.Add(component);
                }

                CommitSuccessfulImport(importMode, "Requirements-XML");
            }
            else
            {
                StatusText = result.ErrorMessage;
                return;
            }

            OnPropertyChanged(nameof(CanGenerate));
        }


        /// <summary>
        /// Loads a saved UI settings XML and initializes the corresponding data immediately.
        /// </summary>
        private async Task Load_Settings(object parameter)
        {
            string xmlFilter = "XML File (*.xml)|*.xml";
            string filePath = SystemDialog.OpenSelectFileDialog(xmlFilter);
            if (string.IsNullOrEmpty(filePath))
                return;

            var importMode = AskForImportMode("Projekt-Einstellungen");
            if (importMode == ImportMode.Cancel)
                return;

            var resultSettings = XmlHandler.Read(filePath);
            if (!resultSettings.IsSuccess)
            {
                StatusText = resultSettings.ErrorMessage;
                return;
            }

            var previousSettings = Settings.GetSettings();
            if (!Settings.SetSettings(resultSettings.Value))
            {
                StatusText = "Die Projekteinstellungen konnten nicht übernommen werden.";
                return;
            }

            var importedZuli = new ZuLiDefault();
            var importedRequirements = new RequirementsXml();

            var resultZuli = await importedZuli.ReadFromFileAsync(Settings.PathZuli);
            if (!resultZuli.IsSuccess)
            {
                Settings.SetSettings(previousSettings);
                StatusText = resultZuli.ErrorMessage;
                return;
            }

            var resultRequirements = await importedRequirements.ReadFromFileAsync(Settings.PathRequirementsXml);
            if (!resultRequirements.IsSuccess)
            {
                Settings.SetSettings(previousSettings);
                StatusText = resultRequirements.ErrorMessage;
                return;
            }

            Zuli = importedZuli;
            EnsureSignalIds(Zuli.Items);
            RequirementsFile = importedRequirements;
            RequirementsProvider.RequirementsFile = RequirementsFile;

            ComponentTypes.Clear();
            foreach (var component in RequirementsFile.GetComponentTypes().OrderBy(x => x))
                ComponentTypes.Add(component);

            CommitSuccessfulImport(importMode, "Projekt-Einstellungen");
            OnPropertyChanged(nameof(CanGenerate));
        }


        /// <summary>
        /// Exports all UI settings required for a container generation to a XML file.  
        /// </summary>
        private void Save_Settings(object parameter)
        {
            string xmlFilter = "XML File (*.xml)|*.xml";
            string fileName = $"{Path.GetFileNameWithoutExtension(Settings.PathZuli)}.xml";
            string filePath = SystemDialog.OpenSaveFileDialog(xmlFilter, fileName, "Settings");
            if (!string.IsNullOrEmpty(filePath))
            {
                var result = XmlHandler.Write(Settings.GetSettings(), filePath);
                if (!result.IsSuccess)
                {
                    StatusText = result.ErrorMessage;
                }
                else
                {
                    AddActivity(
                        "Datei",
                        "Generator-Einstellungen gespeichert",
                        filePath);
                }
            }
        }





        /// <summary>
        /// Generates container data based on grouping and substitution rules.
        /// This method clears existing data, generates grouping and substitution rules, and uses these rules to generate container data asynchronously.
        /// It then validates the generated containers and updates the unassigned entries.
        /// </summary>
        private async Task Generate_Containers(object parameter)
        {
            IsBusyGenerateContainers = true;
            try
            {
                var resultGrouping = Settings.GenerateGroupingRules();
                var resultSubstitution = Settings.GenerateSubstitutionRule();

                if (resultGrouping.IsSuccess)
                {
                    if (resultSubstitution.IsSuccess)
                    {
                        var generationResult = await ContainerGenerator.GenerateAsync(
                            new ContainerGenerationRequest(
                                Zuli.Items,
                                RequirementsFile.Document,
                                resultGrouping.Value,
                                resultSubstitution.Value,
                                ContainerGenerator.IgnoreCase,
                                ContainerGenerator.UseFilterList));

                        var generatedContainers = ContainerData.FromList(
                            generationResult.Containers.ToList());
                        var generatedUnassigned = generationResult.UnassignedSignals.ToList();
                        var generatedFiltered = generationResult.FilteredSignals.ToList();
                        EnsureSignalIds(
                            generatedContainers.SelectMany(container => container.DataList)
                                .Concat(generatedUnassigned)
                                .Concat(generatedFiltered));

                        foreach (var container in generatedContainers)
                            ConfigureContainer(container);

                        string completionStatus;

                        if (_pendingReimportSnapshot is not null)
                        {
                            var summary = GenerationWorkspaceReconciler.Reconcile(
                                _pendingReimportSnapshot,
                                generatedContainers,
                                generatedUnassigned,
                                generatedFiltered,
                                RequirementsFile);

                            foreach (var container in generatedContainers)
                                ConfigureContainer(container);

                            if (summary.Differences.Count > 0)
                            {
                                _pendingGeneratedContainers = generatedContainers;
                                _pendingGeneratedUnassigned = generatedUnassigned;
                                _pendingGeneratedFiltered = generatedFiltered;
                                _pendingReimportSummary = summary;

                                PendingReimportChanges.Clear();
                                foreach (var difference in summary.Differences)
                                {
                                    PendingReimportChanges.Add(difference);
                                    difference.PropertyChanged +=
                                        PendingReimportChange_PropertyChanged;
                                }

                                OnPropertyChanged(nameof(HasPendingReimportChanges));
                                OnPropertyChanged(nameof(ShowReimportNotice));
                                OnPropertyChanged(nameof(PendingReimportSelectionSummary));
                                ReimportNotice =
                                    $"Reimport-Vorschau erzeugt: {summary.Differences.Count} Unterschiede. " +
                                    "Der sichtbare Arbeitsstand wurde noch nicht verändert.";
                                AddActivity(
                                    "Reimport",
                                    "Vergleich erzeugt",
                                    $"{summary.Differences.Count} Unterschiede erkannt; " +
                                    $"{summary.PreservedAssignments} unveränderte Zuordnungen erkannt. " +
                                    "Noch keine Änderung am Arbeitsstand.");
                                StatusText =
                                    $"{summary.Differences.Count} Änderungen wurden erkannt. " +
                                    "Bitte jede Änderung prüfen und anschließend „Auswahl anwenden“ wählen. " +
                                    "Der bisherige Arbeitsstand bleibt bis dahin unverändert.";
                                WasGenerated = false;
                                return;
                            }

                            completionStatus = summary.ToStatusText();
                            ReimportNotice =
                                "Reimport geprüft: Es wurden keine einzeln zu entscheidenden fachlichen Unterschiede erkannt.";
                            AddActivity(
                                "Reimport",
                                "Ohne Unterschiede übernommen",
                                summary.ToStatusText());
                        }
                        else
                        {
                            ReimportNotice = string.Empty;
                            completionStatus =
                                $"Generierung abgeschlossen: {generationResult.Statistics.GeneratedContainers} Container, " +
                                $"{generationResult.Statistics.MatchedSignals} zugeordnet, " +
                                $"{generationResult.Statistics.UnassignedSignals} nicht zugeordnet.";
                        }

                        // Generation and reconciliation operate on local collections.
                        // Only a completely successful result replaces the visible work.
                        ReplaceWorkspace(
                            generatedContainers,
                            generatedUnassigned,
                            generatedFiltered);
                        ReattachAllSlotChangedHandlers();
                        _pendingReimportSnapshot = null;
                        StatusText = completionStatus;
                        WasGenerated = true;
                        AddActivity(
                            "Generierung",
                            "Arbeitsstand erzeugt",
                            completionStatus);

                    }
                    else
                    {
                        StatusText = resultSubstitution.ErrorMessage;
                        WasGenerated = false;
                    }
                }
                else
                {
                    StatusText = resultGrouping.ErrorMessage;
                    WasGenerated = false;
                }
            }
            catch (RegexMatchTimeoutException ex)
            {
                Logger.Error(ex, "Regex timeout during container generation.");
                StatusText =
                    "Die Generierung wurde abgebrochen, weil ein regulärer Ausdruck zu lange benötigt. " +
                    "Bitte Regex vereinfachen oder genauer eingrenzen. Der bisherige Arbeitsstand blieb erhalten.";
                WasGenerated = false;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Container generation failed.");
                StatusText =
                    $"Die Generierung konnte nicht abgeschlossen werden: {ex.Message}. " +
                    "Der bisherige Arbeitsstand blieb erhalten.";
                WasGenerated = false;
            }
            finally
            {
                IsBusyGenerateContainers = false;
            }
        }


        /// <summary>
        /// Exports the container data to an XML file.
        /// This method opens a save file dialog, converts the container list to a list of component containers,
        /// adds unassigned entries, and writes the data to an XML file.
        /// </summary>
        private void Export_Containers(object parameter)
        {
            var summary = CreateValidationSummary();
            StatusText = summary.ToStatusText();
            var confirmation = MessageBox.Show(
                summary.ToDisplayText() +
                Environment.NewLine +
                Environment.NewLine +
                (summary.HasWarnings
                    ? "Es bestehen Prüfhinweise. Soll trotzdem exportiert werden?"
                    : "Die Prüfung war ohne Hinweis. Soll jetzt exportiert werden?"),
                "Prüfzusammenfassung vor Export",
                MessageBoxButton.YesNo,
                summary.HasWarnings ? MessageBoxImage.Warning : MessageBoxImage.Information);

            if (confirmation != MessageBoxResult.Yes)
            {
                AddActivity(
                    "Prüfung",
                    "Export nach Prüfung abgebrochen",
                    summary.ToStatusText());
                if (summary.HasWarnings)
                {
                    SelectedReviewFilter = ReviewFilterOptions.First(
                        option => option.Value == WorkspaceReviewFilter.NeedsReview);
                }
                return;
            }

            string xmlFilter = "XML File (*.xml)|*.xml";
            string fileName = $"{Path.GetFileNameWithoutExtension(Settings.PathZuli)}.xml";
            string filePath = SystemDialog.OpenSaveFileDialog(xmlFilter, fileName, "Container");
            if (!string.IsNullOrEmpty(filePath))
            {
                List<ComponentContainer> exportContainerList = ContainerData.ToComponentContainerList(ContainerList);
                // Add unknown container type with all unassigned entries
                exportContainerList.Add(new ComponentContainer { Component = "unknown", Type = "unknown", DataList = UnassignedEntries });

                Result<string> result = XmlHandler.WriteContainerXml(exportContainerList, filePath, Path.GetFileName(Settings.PathRequirementsXml), Path.GetFileName(Settings.PathZuli));
                if (!result.IsSuccess)
                {
                    StatusText = result.ErrorMessage;
                }
                else
                {
                    AddActivity(
                        "Export",
                        "Container-XML exportiert",
                        $"{filePath} | {ContainerList.Count} Container, " +
                        $"{AssignedSignals} zugeordnete und {UnassignedEntries.Count} nicht zugeordnete Signale.");
                    StatusText =
                        $"Export abgeschlossen. {summary.ToStatusText()}";
                }
            }
        }

        private void Validate_Workspace(object parameter)
        {
            var summary = CreateValidationSummary();
            StatusText = summary.ToStatusText();
            AddActivity(
                "Prüfung",
                "Prüfzusammenfassung erstellt",
                summary.ToStatusText());
            MessageBox.Show(
                summary.ToDisplayText(),
                "Prüfzusammenfassung",
                MessageBoxButton.OK,
                summary.HasWarnings ? MessageBoxImage.Warning : MessageBoxImage.Information);
        }

        private WorkspaceValidationSummary CreateValidationSummary()
        {
            return WorkspaceValidationAnalyzer.Analyze(
                ContainerList,
                UnassignedEntries,
                FilteredEntries);
        }


        private void Save_Data(object parameter)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "xml (*.xml)|*.xml";
            saveFileDialog.Title = "Choose save location";

            if (saveFileDialog.ShowDialog() == true)
            {
                //Get the path of specified file
                string[] FoundFiles = saveFileDialog.FileNames;
                if (FoundFiles.Length == 1)
                {
                    AddActivity(
                        "Datei",
                        "Arbeitsstand gespeichert",
                        FoundFiles[0]);
                    SavedData CreatedSaveData = new SavedData
                    {
                        ContainerList = ContainerList.ToList(),
                        FilteredEntries = FilteredEntries.ToList(),
                        UnassignedEntries = UnassignedEntries.ToList(),
                        ActivityLog = ActivityLog.ToList(),
                        FilePath = FoundFiles[0],
                    };

                    CreatedSaveData.CaptureEntryStates();
                    CreatedSaveData.SetSettings();
                }
            }

        }

        private void Load_Data(object parameter)
        {

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "xml (*.xml)|*.xml";
            openFileDialog.Multiselect = false;
            openFileDialog.Title = "Select the data to load";

            if (openFileDialog.ShowDialog() == true)
            {
                //Get the path of specified file
                string[] FoundFiles = openFileDialog.FileNames;
                if (FoundFiles.Length == 1)
                {
                    try
                    {
                        SavedData loadedData = SavedData.DeserializeProject(FoundFiles[0]);
                        foreach (var container in loadedData.ContainerList)
                            ConfigureContainer(container);

                        // Replace the visible workspace only after the complete file
                        // has been read and validated. A broken file therefore cannot
                        // destroy the current work.
                        if (HasWorkspaceData)
                            CaptureUndo("Gespeicherten Arbeitsstand laden");
                        ReplaceWorkspace(
                            loadedData.ContainerList,
                            loadedData.UnassignedEntries,
                            loadedData.FilteredEntries);

                        _pendingReimportSnapshot = null;
                        ClearPendingReimportResult();
                        ReattachAllSlotChangedHandlers();
                        ActivityLog.Clear();
                        foreach (var logEntry in loadedData.ActivityLog
                                     .OrderByDescending(entry => entry.Timestamp)
                                     .Take(MaximumActivityLogEntries))
                        {
                            ActivityLog.Add(logEntry);
                        }
                        NotifyActivityLogChanged();
                        AddActivity(
                            "Datei",
                            "Arbeitsstand geladen",
                            FoundFiles[0]);
                        WasGenerated = true;
                        StatusText = "Gespeicherter Arbeitsstand wurde geladen.";
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Could not load workspace from {FilePath}.", FoundFiles[0]);
                        MessageBox.Show(
                            "Die Datei konnte nicht gelesen werden. Der aktuelle Arbeitsstand wurde nicht verändert.",
                            "Arbeitsstand laden",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }



        }

        private void Apply_ReimportSelection(object parameter)
        {
            if (_pendingReimportSummary is null ||
                _pendingGeneratedContainers is null ||
                _pendingGeneratedUnassigned is null ||
                _pendingGeneratedFiltered is null)
            {
                return;
            }

            CaptureUndo("Reimport-Auswahl anwenden");
            var decisions = PendingReimportChanges.ToList();
            var accepted = PendingReimportChanges.Count(change => change.IsAccepted);
            var rejected = PendingReimportChanges.Count - accepted;
            var newSignals = decisions.Count(
                change => change.Kind == ReimportChangeKind.NewFromSource);
            var changedSignals = decisions.Count(
                change => change.Kind == ReimportChangeKind.SourceChanged);

            _suppressUndoCapture = true;
            try
            {
                GenerationWorkspaceReconciler.ApplyDecisions(
                    _pendingReimportSummary,
                    _pendingGeneratedContainers,
                    _pendingGeneratedUnassigned,
                    _pendingGeneratedFiltered);

                foreach (var container in _pendingGeneratedContainers)
                    ConfigureContainer(container);

                ReplaceWorkspace(
                    _pendingGeneratedContainers,
                    _pendingGeneratedUnassigned,
                    _pendingGeneratedFiltered);
                ReattachAllSlotChangedHandlers();
            }
            finally
            {
                _suppressUndoCapture = false;
            }

            _pendingReimportSnapshot = null;
            ClearPendingReimportResult();
            WasGenerated = true;
            RefreshAllWorkspaceFilters();
            foreach (var decision in decisions)
            {
                AddActivity(
                    "Reimport",
                    decision.IsAccepted
                        ? $"{decision.Category} übernommen"
                        : $"{decision.Category}: bisherigen Stand beibehalten",
                    $"Signal {decision.Signal} – {decision.ExactDifference}. " +
                    $"Auswirkung: {decision.DecisionEffect}.");
            }
            AddActivity(
                "Reimport",
                "Auswahl angewendet",
                $"{accepted} Änderungen übernommen; {rejected} Änderungen nicht übernommen. " +
                $"{ContainerList.Count} Container und {AssignedSignals} zugeordnete Signale im resultierenden Arbeitsstand.");

            ReimportNotice =
                $"Arbeitsstand wurde am {DateTime.Now:dd.MM.yyyy HH:mm} durch den Reimport geändert: " +
                $"{newSignals} neue und {changedSignals} in der Quelle geänderte Signale erkannt; " +
                $"{accepted} Änderungen übernommen, {rejected} Änderungen nicht übernommen.";
            StatusText =
                $"Reimport übernommen: {newSignals} neue, {changedSignals} geänderte Signale; " +
                $"{accepted} Änderungen angenommen, " +
                $"{rejected} Änderungen verworfen beziehungsweise bisheriger Stand beibehalten.";
        }

        private void Cancel_ReimportSelection(object parameter)
        {
            var differenceCount = PendingReimportChanges.Count;
            ClearPendingReimportResult();
            WasGenerated = false;
            ReimportNotice =
                $"Reimport-Vorschau mit {differenceCount} Unterschieden wurde verworfen; " +
                "der Arbeitsstand wurde nicht verändert.";
            AddActivity(
                "Reimport",
                "Vorschau verworfen",
                $"{differenceCount} erkannte Unterschiede; keine Änderung am Arbeitsstand.");
            StatusText =
                "Die Reimport-Vorschau wurde verworfen. Der bisherige Arbeitsstand blieb unverändert.";
        }

        private void SetAllReimportChanges(bool isAccepted)
        {
            foreach (var change in PendingReimportChanges)
                change.IsAccepted = isAccepted;

            AddActivity(
                "Reimport",
                isAccepted ? "Alle Änderungen ausgewählt" : "Alle Änderungen abgewählt",
                $"{PendingReimportChanges.Count} Vergleichszeilen aktualisiert.");
        }

        private void ClearPendingReimportResult()
        {
            foreach (var difference in PendingReimportChanges)
                difference.PropertyChanged -= PendingReimportChange_PropertyChanged;

            _pendingGeneratedContainers = null;
            _pendingGeneratedUnassigned = null;
            _pendingGeneratedFiltered = null;
            _pendingReimportSummary = null;
            PendingReimportChanges.Clear();
            OnPropertyChanged(nameof(HasPendingReimportChanges));
            OnPropertyChanged(nameof(ShowReimportNotice));
            OnPropertyChanged(nameof(PendingReimportSelectionSummary));
        }

        private void PendingReimportChange_PropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(ReimportDifference.IsAccepted) or
                nameof(ReimportDifference.DecisionEffect))
            {
                OnPropertyChanged(nameof(PendingReimportSelectionSummary));
            }
        }

        private void Undo_LastAction(object parameter)
        {
            if (_undoHistory.Count == 0)
                return;

            var state = _undoHistory[^1];
            _undoHistory.RemoveAt(_undoHistory.Count - 1);
            _redoHistory.Add(
                WorkspaceUndoState.Capture(
                    state.Description,
                    ContainerList,
                    UnassignedEntries,
                    FilteredEntries));
            TrimHistory(_redoHistory);
            _isRestoringWorkspace = true;
            _suppressUndoCapture = true;
            try
            {
                ReplaceWorkspace(
                    state.Containers,
                    state.Unassigned,
                    state.Filtered);
                foreach (var container in ContainerList)
                    ConfigureContainer(container);
                ReattachAllSlotChangedHandlers();
            }
            finally
            {
                _suppressUndoCapture = false;
                _isRestoringWorkspace = false;
            }

            WasGenerated = HasWorkspaceData;
            RefreshAllWorkspaceFilters();
            NotifyUndoStateChanged();
            if (state.Description.Contains("Reimport", StringComparison.OrdinalIgnoreCase))
            {
                ReimportNotice =
                    "Die letzte Übernahme des Reimports wurde rückgängig gemacht.";
            }
            AddActivity(
                "Rückgängig",
                state.Description,
                "Der vorherige Arbeitsstand wurde vollständig wiederhergestellt.");
            StatusText = $"Rückgängig ausgeführt: {state.Description}.";
        }

        private void Redo_LastAction(object parameter)
        {
            if (_redoHistory.Count == 0)
                return;

            var state = _redoHistory[^1];
            _redoHistory.RemoveAt(_redoHistory.Count - 1);
            _undoHistory.Add(
                WorkspaceUndoState.Capture(
                    state.Description,
                    ContainerList,
                    UnassignedEntries,
                    FilteredEntries));
            TrimHistory(_undoHistory);

            _isRestoringWorkspace = true;
            _suppressUndoCapture = true;
            try
            {
                ReplaceWorkspace(
                    state.Containers,
                    state.Unassigned,
                    state.Filtered);
                foreach (var container in ContainerList)
                    ConfigureContainer(container);
                ReattachAllSlotChangedHandlers();
            }
            finally
            {
                _suppressUndoCapture = false;
                _isRestoringWorkspace = false;
            }

            WasGenerated = HasWorkspaceData;
            RefreshAllWorkspaceFilters();
            NotifyUndoStateChanged();
            if (state.Description.Contains("Reimport", StringComparison.OrdinalIgnoreCase))
            {
                ReimportNotice =
                    "Die zuvor rückgängig gemachte Reimport-Übernahme wurde wiederholt.";
            }
            AddActivity(
                "Wiederholen",
                state.Description,
                "Der rückgängig gemachte Arbeitsstand wurde erneut angewendet.");
            StatusText = $"Wiederholen ausgeführt: {state.Description}.";
        }

        private void CaptureUndo(string description)
        {
            if (_suppressUndoCapture || _isRestoringWorkspace)
                return;

            _undoHistory.Add(
                WorkspaceUndoState.Capture(
                    description,
                    ContainerList,
                    UnassignedEntries,
                    FilteredEntries));

            TrimHistory(_undoHistory);
            _redoHistory.Clear();

            NotifyUndoStateChanged();
        }

        private void RunWorkspaceAction(
            string description,
            Action action,
            string? details = null)
        {
            var before =
                $"{ContainerList.Count} Container, {AssignedSignals} zugeordnet, " +
                $"{UnassignedEntries.Count} nicht zugeordnet, {FilteredEntries.Count} gefiltert";
            CaptureUndo(description);
            _suppressUndoCapture = true;
            try
            {
                action();
            }
            finally
            {
                _suppressUndoCapture = false;
            }

            RefreshAllWorkspaceFilters();
            var after =
                $"{ContainerList.Count} Container, {AssignedSignals} zugeordnet, " +
                $"{UnassignedEntries.Count} nicht zugeordnet, {FilteredEntries.Count} gefiltert";
            AddActivity(
                "Bearbeitung",
                description,
                $"{(string.IsNullOrWhiteSpace(details) ? string.Empty : details + " | ")}" +
                $"Bestand vorher: {before}; danach: {after}.");
        }

        private void NotifyUndoStateChanged()
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(UndoDescription));
            OnPropertyChanged(nameof(CanRedo));
            OnPropertyChanged(nameof(RedoDescription));
        }

        private static void TrimHistory(List<WorkspaceUndoState> history)
        {
            if (history.Count > MaximumUndoActions)
                history.RemoveAt(0);
        }

        private void AddActivity(string category, string action, string details)
        {
            ActivityLog.Insert(
                0,
                new WorkspaceActivityLogEntry
                {
                    Timestamp = DateTime.Now,
                    Category = category,
                    Action = action,
                    Details = details
                });

            while (ActivityLog.Count > MaximumActivityLogEntries)
                ActivityLog.RemoveAt(ActivityLog.Count - 1);

            NotifyActivityLogChanged();
        }

        private void Clear_ActivityLog(object parameter)
        {
            ActivityLog.Clear();
            NotifyActivityLogChanged();
            StatusText = "Aktivitätsprotokoll wurde geleert.";
        }

        private void NotifyActivityLogChanged()
        {
            OnPropertyChanged(nameof(HasActivityLog));
            OnPropertyChanged(nameof(ActivityLogHeader));
        }






        //===========================================================================================================================
        // M E T H O D S   ( H E L P E R S )
        //===========================================================================================================================

        private enum ImportMode
        {
            NewProject,
            Reimport,
            Cancel
        }

        private bool HasWorkspaceData =>
            ContainerList.Count > 0 ||
            UnassignedEntries.Count > 0 ||
            FilteredEntries.Count > 0;

        private ImportMode AskForImportMode(string sourceName)
        {
            if (!HasWorkspaceData && _pendingReimportSnapshot is null)
                return ImportMode.NewProject;

            var result = MessageBox.Show(
                $"{sourceName} einlesen:\n\n" +
                "Ja = bestehende Arbeit erneut prüfen. Die neue Datei wird eingelesen, " +
                "manuelle beziehungsweise bestehende Zuordnungen werden beim nächsten Generieren abgeglichen und gekennzeichnet.\n\n" +
                "Nein = neues Projekt. Die bisherigen Arbeitsergebnisse werden nach erfolgreichem Einlesen verworfen.\n\n" +
                "Abbrechen = nichts ändern.",
                "Importart auswählen",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            return result switch
            {
                MessageBoxResult.Yes => ImportMode.Reimport,
                MessageBoxResult.No => ImportMode.NewProject,
                _ => ImportMode.Cancel
            };
        }

        private void CommitSuccessfulImport(ImportMode importMode, string sourceName)
        {
            _redoHistory.Clear();
            NotifyUndoStateChanged();

            if (importMode == ImportMode.Reimport)
            {
                ClearPendingReimportResult();
                _pendingReimportSnapshot ??= GenerationWorkspaceReconciler.Capture(
                    ContainerList,
                    UnassignedEntries,
                    FilteredEntries);

                ReimportNotice =
                    $"{sourceName} wurde als Reimport eingelesen. Erst die nächste Generierung erzeugt den Vergleich.";
                AddActivity(
                    "Import",
                    $"{sourceName} als Reimport eingelesen",
                    "Bestehender Arbeitsstand wurde als Vergleichsbasis gesichert.");
                StatusText =
                    $"{sourceName} wurde neu eingelesen. Beim nächsten Generieren werden " +
                    "bestehende Zuordnungen abgeglichen und Änderungen gekennzeichnet.";
            }
            else
            {
                _pendingReimportSnapshot = null;
                ClearPendingReimportResult();
                ClearData();
                _undoHistory.Clear();
                _redoHistory.Clear();
                NotifyUndoStateChanged();
                ReimportNotice = string.Empty;
                AddActivity(
                    "Import",
                    $"{sourceName} als neues Projekt eingelesen",
                    "Vorheriger Container-Arbeitsstand wurde geleert.");
                StatusText = $"{sourceName} wurde als neues Projekt eingelesen.";
            }

            WasGenerated = false;
        }

        private void ReplaceWorkspace(
            IEnumerable<ContainerData> containers,
            IEnumerable<ContainerEntry> unassigned,
            IEnumerable<ContainerEntry> filtered)
        {
            ClearData();

            foreach (var container in containers)
            {
                EnsureSignalIds(container.DataList);
                ContainerList.Add(container);
            }

            foreach (var entry in unassigned)
            {
                entry.EnsureSignalId();
                UnassignedEntries.Add(entry);
            }

            foreach (var entry in filtered)
            {
                entry.EnsureSignalId();
                FilteredEntries.Add(entry);
            }
        }

        private static void EnsureSignalIds(IEnumerable<ContainerEntry> entries)
        {
            foreach (var entry in entries)
                entry.EnsureSignalId();
        }

        private void ConfigureContainer(ContainerData container)
        {
            container.Slots.Clear();
            foreach (var slot in RequirementsFile.GetSlotNames(container.Type))
                container.Slots.Add(slot);

            container.MinSignals = RequirementsFile.GetMinSignals(container.Type);
            container.MaxSignals = RequirementsFile.GetMaxSignals(container.Type);
            container.Validate();
            container.RefreshReimportStatus();
        }

        /// <summary>
        /// Clears the grid data for container as well as unassigned entries.
        /// </summary>
        private void ClearData()
        {
            foreach (var handler in _slotChangedHandlers)
                handler.Key.SlotChanged -= handler.Value;
            foreach (var handler in _signalClearedHandlers)
                handler.Key.SignalCleared -= handler.Value;
            foreach (var handler in _entryChangingHandlers)
                handler.Key.WorkspaceValueChanging -= handler.Value;
            foreach (var handler in _containerChangingHandlers)
                handler.Key.WorkspaceValueChanging -= handler.Value;
            foreach (var handler in _containerPropertyChangedHandlers)
                handler.Key.PropertyChanged -= handler.Value;
            foreach (var handler in _containerEntryCollectionHandlers)
                handler.Key.DataList.CollectionChanged -= handler.Value;

            _slotChangedHandlers.Clear();
            _signalClearedHandlers.Clear();
            _entryChangingHandlers.Clear();
            _containerChangingHandlers.Clear();
            _containerPropertyChangedHandlers.Clear();
            _containerEntryCollectionHandlers.Clear();
            _lastKnownSlot.Clear();
            ContainerList.Clear();
            UnassignedEntries.Clear();
            FilteredEntries.Clear();
        }


        /// <summary>
        /// Set default settings to initialize the view at startup.
        /// </summary>
        private void LoadDefaultSettings()
        {
            StatusText = "Ready to start...";
            //Settings.PathZuli = "Path to ZuLi file (.xlsx)";
            //Settings.PathRequirementsXml = "Path to AutoCreate file (.xml)";

            Settings.GroupByComponent = true;
            Settings.GroupByType = false;
            Settings.GroupById = false;
            Settings.GroupByAddress = false;

            Settings.SelectedOption = ContainerGeneration.Models.ContainerGenerationSettings.ComponentOption;
        }



        /// <summary>
        /// Deletes the specified container entry and adds it to the unassigned entries.
        /// This method finds the container that contains the entry, removes the entry from the container,
        /// and adds it to the unassigned entries list.
        /// </summary>
        /// <param name="item">The container entry to delete.</param>
        private void Delete_Item(object parameter)
        {
            if (parameter is ContainerEntry item)
            {
                var sourceContainer = ContainerList.FirstOrDefault(
                    container => container.DataList.Contains(item));
                if (sourceContainer is null)
                {
                    Logger.Warn("Could not remove entry {EntryId}: no source container found.", item.ID);
                    return;
                }

                RunWorkspaceAction(
                    "Signal aus Container entfernen",
                    () =>
                    {
                        var result = GenerationWorkspaceEditor.MoveToUnassigned(
                            item,
                            item.Signal,
                            ContainerList,
                            UnassignedEntries,
                            FilteredEntries);
                        MarkAsManual(item, "Manuell aus dem Container entfernt.");
                        if (result.RemovedDuplicateOccurrences > 0)
                        {
                            Logger.Warn(
                                "Removed {DuplicateCount} duplicate workspace occurrences for entry {EntryId}.",
                                result.RemovedDuplicateOccurrences,
                                item.ID);
                        }
                    },
                    $"Signal „{item.Signal}“ aus Container „{sourceContainer.Component}“");
            }
        }



        /// <summary>
        /// Filters the unassigned entries grid based on the search text.
        /// </summary>
        /// <remarks>
        /// This method filters the <see cref="ContainerList"/> collection based on the <see cref="SearchTextContainer"/>.
        /// If the search text is empty or less than 2 characters, the filter is cleared. Otherwise, a short delay is introduced
        /// to keep the input responsive before starting the search.
        /// The filter checks for matches in various properties of the <see cref="ContainerData"/>.
        /// </remarks>
        private void FilterContainerGrid()
        {
            var view = CollectionViewSource.GetDefaultView(ContainerList);
            var search = SearchTextContainer?.Trim() ?? string.Empty;
            var reviewFilter = SelectedReviewFilter?.Value ?? WorkspaceReviewFilter.All;
            ApplyWorkspaceFilter(
                view,
                string.IsNullOrEmpty(search) && reviewFilter == WorkspaceReviewFilter.All
                    ? null
                    : item =>
                        item is ContainerData container &&
                        (string.IsNullOrEmpty(search) ||
                         ContainerWorkspaceSearch.Matches(container, search)) &&
                        MatchesReviewFilter(container, reviewFilter),
                "Container");
        }


        /// <summary>
        /// Filters the unassigned entries grid based on the search text.
        /// </summary>
        /// <remarks>
        /// This method filters the <see cref="UnassignedEntries"/> collection based on the <see cref="SearchTextUnassignedEntries"/>.
        /// If the search text is empty or less than 2 characters, the filter is cleared. Otherwise, a short delay is introduced
        /// to keep the input responsive before starting the search.
        /// The filter checks for matches in various properties of the <see cref="ContainerEntry"/>.
        /// </remarks>
        private void FilterUnassignedEntriesGrid()
        {
            var viewUnassigned = CollectionViewSource.GetDefaultView(UnassignedEntries);
            var search = SearchTextUnassignedEntries?.Trim() ?? string.Empty;
            var reviewFilter = SelectedReviewFilter?.Value ?? WorkspaceReviewFilter.All;
            ApplyWorkspaceFilter(
                viewUnassigned,
                string.IsNullOrEmpty(search) && reviewFilter == WorkspaceReviewFilter.All
                    ? null
                    : item =>
                        item is ContainerEntry entry &&
                        (string.IsNullOrEmpty(search) ||
                         ContainerWorkspaceSearch.Matches(entry, search)) &&
                        MatchesReviewFilter(entry, reviewFilter),
                "nicht zugeordnete Signale");

        }

        /// <summary>
        /// Filters the filtered entries grid based on the search text.
        /// </summary>
        /// <remarks>
        /// This method filters the <see cref="FilteredEntries"/> collection based on the <see cref="SearchTextFilteredEntries"/>.
        /// If the search text is empty or less than 2 characters, the filter is cleared. Otherwise, a short delay is introduced
        /// to keep the input responsive before starting the search.
        /// The filter checks for matches in various properties of the <see cref="ContainerEntry"/>.
        /// </remarks>
        private void FilterFilteredEntriesGrid()
        {
            var viewFiltered = CollectionViewSource.GetDefaultView(FilteredEntries);
            var search = SearchTextFilteredEntries?.Trim() ?? string.Empty;
            var reviewFilter = SelectedReviewFilter?.Value ?? WorkspaceReviewFilter.All;
            ApplyWorkspaceFilter(
                viewFiltered,
                string.IsNullOrEmpty(search) && reviewFilter == WorkspaceReviewFilter.All
                    ? null
                    : item =>
                        item is ContainerEntry entry &&
                        (string.IsNullOrEmpty(search) ||
                         ContainerWorkspaceSearch.Matches(entry, search)) &&
                        MatchesReviewFilter(entry, reviewFilter),
                "gefilterte Signale");
        }

        private void RefreshAllWorkspaceFilters()
        {
            if (_debounceTimerContainerData is null)
                return;

            FilterContainerGrid();
            FilterUnassignedEntriesGrid();
            FilterFilteredEntriesGrid();
        }

        private static bool MatchesReviewFilter(
            ContainerData container,
            WorkspaceReviewFilter filter) =>
            filter switch
            {
                WorkspaceReviewFilter.NeedsReview => container.RequiresReview,
                WorkspaceReviewFilter.Changed => container.HasDetectedChanges,
                WorkspaceReviewFilter.ManuallyEdited =>
                    container.DataList.Any(entry => entry.IsManuallyEdited),
                WorkspaceReviewFilter.Unchecked =>
                    !container.ManuallyChecked &&
                    (!container.IsValid || container.HasDetectedChanges),
                WorkspaceReviewFilter.Invalid => !container.IsValid,
                _ => true
            };

        private static bool MatchesReviewFilter(
            ContainerEntry entry,
            WorkspaceReviewFilter filter) =>
            filter switch
            {
                WorkspaceReviewFilter.NeedsReview =>
                    entry.ReviewState is ContainerEntryReviewState.NeedsReview or
                        ContainerEntryReviewState.SourceChanged or
                        ContainerEntryReviewState.NewFromSource or
                        ContainerEntryReviewState.NewlyRecognized ||
                    string.IsNullOrWhiteSpace(entry.Signal),
                WorkspaceReviewFilter.Changed =>
                    entry.ReviewState is not ContainerEntryReviewState.None and
                        not ContainerEntryReviewState.Preserved,
                WorkspaceReviewFilter.ManuallyEdited => entry.IsManuallyEdited,
                WorkspaceReviewFilter.Unchecked =>
                    entry.ReviewState is ContainerEntryReviewState.NeedsReview or
                        ContainerEntryReviewState.SourceChanged or
                        ContainerEntryReviewState.NewFromSource or
                        ContainerEntryReviewState.NewlyRecognized,
                WorkspaceReviewFilter.Invalid => string.IsNullOrWhiteSpace(entry.Signal),
                _ => true
            };

        private void ApplyWorkspaceFilter(
            ICollectionView view,
            Predicate<object>? filter,
            string area)
        {
            try
            {
                if (view is IEditableCollectionView editableView)
                {
                    if (editableView.IsEditingItem)
                        editableView.CommitEdit();
                    if (editableView.IsAddingNew)
                        editableView.CommitNew();
                }

                using (view.DeferRefresh())
                    view.Filter = filter;
            }
            catch (InvalidOperationException ex)
            {
                Logger.Warn(ex, "Could not refresh {FilterArea} filter.", area);
                StatusText =
                    $"Der Filter für {area} konnte während einer laufenden Bearbeitung nicht aktualisiert werden. " +
                    "Bitte Eingabe abschließen und erneut versuchen.";
            }
        }



        /// <summary>
        /// Finds the first ancestor of the specified type in the visual tree.
        /// </summary>
        /// <typeparam name="T">The type of the ancestor to find. Must be a subclass of DependencyObject.</typeparam>
        /// <param name="current">The starting point in the visual tree to begin the search.</param>
        /// <returns>
        /// The first ancestor of type <typeparamref name="T"/> found in the visual tree, or <c>null</c> if no ancestor of the specified type is found.
        /// </returns>
        /// <remarks>
        /// This method traverses up the visual tree starting from the given <paramref name="current"/> object.
        /// It checks each parent object to see if it matches the specified type <typeparamref name="T"/>.
        /// If a match is found, the ancestor is returned; otherwise, the search continues up the tree.
        /// </remarks>
        private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T type)
                {
                    return type;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }


        /// <summary>
        /// Event handler for the log received event. Updates the status text if a log event was received.
        /// </summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="message">Log message.</param>
        private void CustomTarget_LogReceived(object? sender, string message)
        {
            StatusText = message;
        }


        //===========================================================================================================================
        // E V E N T S
        //===========================================================================================================================

        private void UpdateUICount(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (sender is IEnumerable<ContainerData> ContainerList)
            {
                foreach (ContainerData t_Data in ContainerList)
                {
                    t_Data.DataList.CollectionChanged -= UpdateUICount;
                    t_Data.DataList.CollectionChanged += UpdateUICount;
                }
            }

            OnPropertyChanged(nameof(AssignedSignals));
            OnPropertyChanged(nameof(PercentComplete));
        }


        /// <summary>
        /// Handles the preview key down event to add data to unassigned entries when the delete key is pressed.
        /// This method checks if the delete key is pressed and if the selected container is in the container list,
        /// then adds the data list of the selected container to the unassigned entries.
        /// </summary>
        /// <param name="e">The <see cref="KeyEventArgs"/> instance containing the event data.</param>
        public void ContainerGrid_OnPreviewKeyDown(object parameter)
        {
            if (parameter is not KeyEventArgs e)
                return;

            if (e.Key == Key.Delete)
            {
                var containersToClear = SelectedContainers
                    .Where(container => container is not null && ContainerList.Contains(container))
                    .ToList();
                if (containersToClear.Count == 0)
                    return;

                var signalCount = containersToClear.Sum(container => container.DataList.Count);
                RunWorkspaceAction(
                    "Ausgewählte Container leeren",
                    () =>
                    {
                        foreach (var selectedContainer in containersToClear)
                        {
                            foreach (var item in selectedContainer.DataList.ToList())
                            {
                                GenerationWorkspaceEditor.MoveToUnassigned(
                                    item,
                                    item.Signal,
                                    ContainerList,
                                    UnassignedEntries,
                                    FilteredEntries);
                                MarkAsManual(
                                    item,
                                    "Manuell aus dem Container entfernt.");
                            }
                        }
                    },
                    $"{containersToClear.Count} Container mit {signalCount} Signalen");

                e.Handled = true;
            }
        }

        private void SelectionChanged_Executed(object parameter)
        {
            this.SelectedContainers.Clear();
            if (parameter is IList ConvertedList)
            {
                foreach (var SelectedData in ConvertedList)
                {
                    if (SelectedData is ContainerData ConvertedData)
                    {
                        this.SelectedContainers.Add(ConvertedData);
                    }
                }
            }
        }


        /// <summary>
        /// Handles the type selection change event. After assigning a new type its corresponding slot data will be updated.
        /// </summary>
        /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
        public void OnTypeSelectionChanged(SelectionChangedEventArgs e)
        {
            if (e != null && e.AddedItems.Count > 0)
            {
                if (((ComboBox)e.Source).DataContext is ContainerData data)
                {
                    data.Slots.Clear();

                    foreach (var slot in RequirementsFile.GetSlotNames(data.Type))
                        data.Slots.Add(slot);

                    data.MinSignals = RequirementsFile.GetMinSignals(data.Type);
                    data.MaxSignals = RequirementsFile.GetMaxSignals(data.Type);
                    foreach (var entry in data.DataList)
                        MarkAsManual(entry, "Containertyp wurde manuell geändert.");
                    data.Validate();
                }
            }
        }


        /// <summary>
        /// Event that will be fired once loading the view is completed.
        /// </summary>
        /// <param name="view">The main view.</param>
        public void OnViewLoaded()
        {
            var config = NLog.LogManager.Configuration;
            if (config == null)
            {
                Logger.Error("NLog configuration not loaded");
                return;
            }

            var customTarget = config.FindTargetByName<CustomLoggerTarget>("CustomLog");
            if (customTarget == null)
            {
                Logger.Error("CustomLog target not found in NLog configuration.");
                return;
            }

            customTarget.LogReceived += CustomTarget_LogReceived;
        }



        /// <summary>
        /// Handles the mouse move event to initiate a drag-and-drop operation.
        /// </summary>
        /// <param name="e">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        /// <remarks>
        /// This method checks if the left mouse button is pressed and if the source of the event is a <see cref="DataGrid"/>.
        /// If so, it attempts to find the <see cref="DataGridRow"/> that is the ancestor of the original source of the event.
        /// If a valid <see cref="DataGridRow"/> is found, it retrieves the corresponding data item and checks if it matches the
        /// <see cref="SelectedUnassignedEntry"/>. If all conditions are met, it initiates a drag-and-drop operation with the data item.
        /// </remarks>
        private void Datagrid_OnMouseMove(object parameter)
        {
            if (parameter is not MouseEventArgs e)
                return;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (e.Source is not DataGrid dataGrid)
                    return;

                var dataGridRow = FindAncestor<DataGridRow>((DependencyObject)e.OriginalSource);
                if (dataGridRow == null)
                    return;

                var data = (ContainerEntry)dataGrid.ItemContainerGenerator.ItemFromContainer(dataGridRow);
                if (data == null)
                    return;

                if (!data.Equals(SelectedUnassignedEntry) && !data.Equals(SelectedFilteredEntry))
                    return;

                var dataObj = new DataObject(data);
                dataObj.SetData("DragSource", dataGrid);
                DragDrop.DoDragDrop(dataGrid, dataObj, DragDropEffects.Move);
            }
        }

        /// <summary>
        /// Handles the drag-over event during a drag-and-drop operation.
        /// </summary>
        /// <param name="e">The DragEventArgs containing the event data.</param>
        /// <remarks>
        /// This method sets the effect of the drag-and-drop operation to indicate a move action
        /// and marks the event as handled to prevent further processing.
        /// </remarks>
        private void ContainerGrid_OnDragOver(object parameter)
        {
            if (parameter is DragEventArgs e)
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles the drop event for a drag-and-drop operation involving a DataGrid.
        /// </summary>
        /// <param name="e">The DragEventArgs containing the event data.</param>
        private void Datagrid_OnDrop(object parameter)
        {
            if (parameter is not DragEventArgs e)
                return;
            if (e.Source is not DataGrid DropDataGrid)
                return;
            if (e.Data.GetData(typeof(ContainerEntry)) is not ContainerEntry data)
                return;
            if (e.Data.GetData("DragSource") is not DataGrid)
                return;

            // Get the target row
            if (DropDataGrid.ItemsSource == FilteredEntries)
            {
                RunWorkspaceAction(
                    "Signal als gefiltert einordnen",
                    () =>
                    {
                        GenerationWorkspaceEditor.MoveToFiltered(
                            data,
                            ContainerList,
                            UnassignedEntries,
                            FilteredEntries);
                        MarkAsManual(data, "Manuell als gefiltert eingeordnet.");
                    },
                    $"Signal „{data.Signal}“");
            }
            else if (DropDataGrid.ItemsSource == UnassignedEntries)
            {
                RunWorkspaceAction(
                    "Signal als nicht zugeordnet einordnen",
                    () =>
                    {
                        GenerationWorkspaceEditor.MoveToUnassigned(
                            data,
                            data.Signal,
                            ContainerList,
                            UnassignedEntries,
                            FilteredEntries);
                        MarkAsManual(data, "Manuell als nicht zugeordnet eingeordnet.");
                    },
                    $"Signal „{data.Signal}“");
            }
            else
            {
                var targetRow = FindAncestor<DataGridRow>((DependencyObject)e.OriginalSource);
                if (targetRow == null)
                    return;

                var targetDescription = targetRow.Item is ContainerData target
                    ? $"Container „{target.Component}“ ({target.Type})"
                    : "neuer Container";
                RunWorkspaceAction(
                    "Signal einem Container zuordnen",
                    () => MoveData(targetRow, data),
                    $"Signal „{data.Signal}“ → {targetDescription}");
            }
        }


        /// <summary>
        ///  Logic to move data between collections (e.g. after a DragAndDrop operation).
        /// </summary>
        /// <param name="targetRow">Row in the target grid.</param>
        /// <param name="data">Data entry to add.</param>
        private void MoveData(DataGridRow targetRow, ContainerEntry data)
        {
            if (targetRow.Item is ContainerData targetData)
            {
                // Quell-Container ermitteln (fuer Log: welcher Container verliert das Signal?)
                var sourceContainer = ContainerList.FirstOrDefault(c => c.DataList.Contains(data));

                if (sourceContainer == targetData)
                    return;

                if (sourceContainer != null && sourceContainer != targetData)
                {
                    _actionLogger.LogRemoved(sourceContainer.Component, sourceContainer.Type, data);
                }

                GenerationWorkspaceEditor.MoveToContainer(
                    data,
                    targetData,
                    ContainerList,
                    UnassignedEntries,
                    FilteredEntries);
                AttachSlotChangedHandler(targetData, data);
                MarkAsManual(data, "Manuell einem Container zugeordnet.");
                targetData.ManuallyChecked = false;

                // ActionLog: Signal wurde von sourceContainer nach targetData verschoben
                _actionLogger.LogAdded(
                    containerName: targetData.Component,
                    componentType: targetData.Type,
                    entry: data,
                    ruleSuggestion: data.Slot,
                    mlTop1: null,
                    mlScore: null);

            }
            else if (targetRow.Item == CollectionView.NewItemPlaceholder)
            {
                ContainerData CreatedContainerData = new ContainerData();
                GenerationWorkspaceEditor.MoveToContainer(
                    data,
                    CreatedContainerData,
                    ContainerList,
                    UnassignedEntries,
                    FilteredEntries);
                AttachSlotChangedHandler(CreatedContainerData, data);
                MarkAsManual(data, "Manuell einem neuen Container zugeordnet.");
                CreatedContainerData.ManuallyChecked = false;

                _actionLogger.LogAdded(
                    containerName: CreatedContainerData.Component,
                    componentType: CreatedContainerData.Type,
                    entry: data,
                    ruleSuggestion: data.Slot,
                    mlTop1: null,
                    mlScore: null);

            }
        }

        private void ContainerList_CollectionChanged(
            object? sender,
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems is not null)
            {
                foreach (ContainerData container in e.OldItems)
                    UnsubscribeContainer(container);
            }

            if (e.NewItems is not null)
            {
                foreach (ContainerData container in e.NewItems)
                    SubscribeContainer(container);
            }

            UpdateUICount(sender, e);
        }

        private void SubscribeContainer(ContainerData container)
        {
            if (!_containerChangingHandlers.ContainsKey(container))
            {
                EventHandler<WorkspaceValueChangingEventArgs> changingHandler =
                    (sender, args) => OnWorkspaceValueChanging(sender, args);
                _containerChangingHandlers[container] = changingHandler;
                container.WorkspaceValueChanging += changingHandler;
            }

            if (!_containerPropertyChangedHandlers.ContainsKey(container))
            {
                PropertyChangedEventHandler propertyHandler = (_, args) =>
                {
                    if (args.PropertyName is nameof(ContainerData.RequiresReview) or
                        nameof(ContainerData.HasDetectedChanges) or
                        nameof(ContainerData.IsValid) or
                        nameof(ContainerData.ManuallyChecked))
                    {
                        if (!_suppressUndoCapture &&
                            SelectedReviewFilter?.Value != WorkspaceReviewFilter.All)
                        {
                            // A container can emit several dependent review
                            // notifications for one edit. Debouncing collapses
                            // them into one container-view refresh.
                            _debounceTimerContainerData.Stop();
                            _debounceTimerContainerData.Start();
                        }
                    }
                };
                _containerPropertyChangedHandlers[container] = propertyHandler;
                container.PropertyChanged += propertyHandler;
            }

            if (!_containerEntryCollectionHandlers.ContainsKey(container))
            {
                System.Collections.Specialized.NotifyCollectionChangedEventHandler collectionHandler =
                    (_, args) =>
                    {
                        if (args.NewItems is not null)
                        {
                            foreach (ContainerEntry entry in args.NewItems)
                                AttachSlotChangedHandler(container, entry);
                        }
                    };
                _containerEntryCollectionHandlers[container] = collectionHandler;
                container.DataList.CollectionChanged += collectionHandler;
            }

            foreach (var entry in container.DataList)
                AttachSlotChangedHandler(container, entry);
        }

        private void UnsubscribeContainer(ContainerData container)
        {
            if (_containerChangingHandlers.Remove(container, out var changingHandler))
                container.WorkspaceValueChanging -= changingHandler;
            if (_containerPropertyChangedHandlers.Remove(container, out var propertyHandler))
                container.PropertyChanged -= propertyHandler;
            if (_containerEntryCollectionHandlers.Remove(container, out var collectionHandler))
                container.DataList.CollectionChanged -= collectionHandler;
        }

        private void OnWorkspaceValueChanging(
            object? sender,
            WorkspaceValueChangingEventArgs args)
        {
            var isUserChange = !_suppressUndoCapture && !_isRestoringWorkspace;
            var description = args.PropertyName switch
            {
                nameof(ContainerEntry.Signal) => "Signalname ändern",
                nameof(ContainerEntry.Slot) => "Slot ändern",
                nameof(ContainerEntry.ID) => "Quell-ID ändern",
                nameof(ContainerEntry.Address) => "Signaladresse ändern",
                nameof(ContainerEntry.DataType) => "Datentyp ändern",
                nameof(ContainerEntry.Note) => "Notiz ändern",
                nameof(ContainerData.Component) => "Containername ändern",
                nameof(ContainerData.Type) => "Containertyp ändern",
                nameof(ContainerData.ManuallyChecked) => "Prüfstatus ändern",
                _ => "Arbeitsstand ändern"
            };

            CaptureUndo(description);
            if (isUserChange)
            {
                var details =
                    args.PropertyName == nameof(ContainerEntry.Signal) &&
                    string.IsNullOrWhiteSpace(args.NewValue?.ToString())
                        ? $"Die Zuordnung von Signal „{FormatActivityValue(args.PreviousValue)}“ " +
                          "wird entfernt; der Signalname bleibt in „Nicht zugeordnet“ erhalten."
                        : $"{args.PropertyName}: „{FormatActivityValue(args.PreviousValue)}“ → " +
                          $"„{FormatActivityValue(args.NewValue)}“";
                AddActivity("Direkte Änderung", description, details);
            }

            var previousSuppression = _suppressUndoCapture;
            _suppressUndoCapture = true;
            try
            {
                if (sender is ContainerEntry entry)
                {
                    MarkAsManual(entry, $"{description}.");
                    var owner = ContainerList.FirstOrDefault(
                        container => container.DataList.Contains(entry));
                    if (owner is not null)
                        owner.ManuallyChecked = false;
                }
                else if (sender is ContainerData container &&
                         args.PropertyName != nameof(ContainerData.ManuallyChecked))
                {
                    container.ManuallyChecked = false;
                    foreach (var containerEntry in container.DataList)
                        MarkAsManual(containerEntry, $"{description}.");
                }
            }
            finally
            {
                _suppressUndoCapture = previousSuppression;
            }
        }

        private static string FormatActivityValue(object? value)
        {
            var text = value?.ToString();
            return string.IsNullOrWhiteSpace(text) ? "leer" : text.Trim();
        }

        /// <summary>
        /// Haengt den SlotChanged-Handler an einen ContainerEntry.
        /// Wird aufgerufen wenn ein Entry einem Container hinzugefuegt wird.
        /// Verhindert Mehrfach-Registrierung durch Abmelden vor Anmelden.
        /// </summary>
        private void AttachSlotChangedHandler(ContainerData container, ContainerEntry entry)
        {
            if (_slotChangedHandlers.TryGetValue(entry, out var existingHandler))
                entry.SlotChanged -= existingHandler;

            _lastKnownSlot[entry] = entry.Slot;
            EventHandler handler = (_, _) => OnEntrySlotChanged(container, entry);
            _slotChangedHandlers[entry] = handler;
            entry.SlotChanged += handler;

            if (_signalClearedHandlers.TryGetValue(entry, out var existingSignalHandler))
                entry.SignalCleared -= existingSignalHandler;

            EventHandler<SignalClearedEventArgs> signalHandler =
                (_, args) => OnEntrySignalCleared(container, entry, args);
            _signalClearedHandlers[entry] = signalHandler;
            entry.SignalCleared += signalHandler;

            if (_entryChangingHandlers.TryGetValue(entry, out var existingChangingHandler))
                entry.WorkspaceValueChanging -= existingChangingHandler;

            EventHandler<WorkspaceValueChangingEventArgs> changingHandler =
                (sender, args) => OnWorkspaceValueChanging(sender, args);
            _entryChangingHandlers[entry] = changingHandler;
            entry.WorkspaceValueChanging += changingHandler;
        }

        private void OnEntrySignalCleared(
            ContainerData container,
            ContainerEntry entry,
            SignalClearedEventArgs args)
        {
            var previousSuppression = _suppressUndoCapture;
            _suppressUndoCapture = true;
            try
            {
            if (_slotChangedHandlers.TryGetValue(entry, out var slotHandler))
            {
                entry.SlotChanged -= slotHandler;
                _slotChangedHandlers.Remove(entry);
            }

            if (_signalClearedHandlers.TryGetValue(entry, out var signalHandler))
            {
                entry.SignalCleared -= signalHandler;
                _signalClearedHandlers.Remove(entry);
            }

            _lastKnownSlot.Remove(entry);

            var result = GenerationWorkspaceEditor.MoveToUnassigned(
                entry,
                args.PreviousSignal,
                ContainerList,
                UnassignedEntries,
                FilteredEntries);
            MarkAsManual(
                entry,
                "Signalzuordnung wurde durch Leeren der Signalzelle entfernt.");
            container.ManuallyChecked = false;
            Logger.Info(
                "Signal {EntryId} was moved from container {Container} to unassigned; {DuplicateCount} duplicates removed.",
                entry.ID,
                container.Component,
                result.RemovedDuplicateOccurrences);
            StatusText =
                $"Signal „{entry.Signal}“ wurde aus dem Container entfernt und einmalig als nicht zugeordnet eingetragen.";
            }
            finally
            {
                _suppressUndoCapture = previousSuppression;
            }
        }

        private void OnEntrySlotChanged(ContainerData container, ContainerEntry entry)
        {
            var previousSuppression = _suppressUndoCapture;
            _suppressUndoCapture = true;
            try
            {
            _lastKnownSlot.TryGetValue(entry, out var oldSlot);
            _lastKnownSlot[entry] = entry.Slot;

            MarkAsManual(entry, "Slot wurde manuell geändert.");
            container.ManuallyChecked = false;
            container.RefreshReimportStatus();

            if (!string.IsNullOrEmpty(entry.Slot) && oldSlot != entry.Slot)
            {
                _actionLogger.LogSlotChange(
                    containerName: container.Component,
                    componentType: container.Type,
                    entry: entry,
                    oldSlot: oldSlot ?? "",
                    mlTop1: null,
                    mlScore: null);
            }
            }
            finally
            {
                _suppressUndoCapture = previousSuppression;
            }
        }

        private void ReattachAllSlotChangedHandlers()
        {
            foreach (var container in ContainerList)
            {
                foreach (var entry in container.DataList)
                    AttachSlotChangedHandler(container, entry);
            }
        }

        private static void MarkAsManual(ContainerEntry entry, string message)
        {
            entry.IsManuallyEdited = true;
            entry.ReviewState = ContainerEntryReviewState.ManuallyEdited;
            entry.ReviewMessage = message;
        }





    }
}
