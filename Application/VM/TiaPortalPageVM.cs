using System.Collections.ObjectModel;
using System.Windows.Input;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.Core.ViCo;
using VIBN_Tools.Tia.Client;
using VIBN_Tools.Tia.Contracts;

namespace VIBN_Tools.Application.VM;

/// <summary>
/// UI coordinator for TIA Portal operations. Every Siemens Openness call is
/// delegated through the isolated named-pipe bridge so an Openness failure does
/// not terminate the WPF host process.
/// </summary>
public sealed class TiaPortalPageVM : MvvmBase, IAsyncDisposable
{
    private readonly ITiaBridgeClient _client;
    private readonly ITiaLibraryService _libraryService;
    private readonly IFolderSelectionService _folderSelection;
    private readonly IApplicationLog _log;
    private bool _isBusy;
    private string? _selectedVersion;
    private TiaPlcInfo? _selectedPlc;
    private string _statusText;

    public TiaPortalPageVM(
        ITiaBridgeClient client,
        ITiaLibraryService libraryService,
        IFolderSelectionService folderSelection,
        IReadOnlyList<string> installedVersions,
        IApplicationLog? log = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _libraryService = libraryService ?? throw new ArgumentNullException(nameof(libraryService));
        _folderSelection = folderSelection ?? throw new ArgumentNullException(nameof(folderSelection));
        _log = log ?? NullApplicationLog.Instance;

        foreach (var version in installedVersions)
            InstalledVersions.Add(version);

        _selectedVersion = InstalledVersions.FirstOrDefault();
        _statusText = InstalledVersions.Count == 0
            ? "Keine unterstützte TIA-Portal-Installation gefunden."
            : "TIA Bridge ist bereit.";

        ConnectCommand = GetCommandBindingAsync(ConnectAsync);
        SelectPlcCommand = GetCommandBindingAsync(SelectPlcAsync);
        LoadHardwareCommand = GetCommandBindingAsync(LoadHardwareAsync);
        LoadBlocksCommand = GetCommandBindingAsync(LoadBlocksAsync);
        LoadDataTypesCommand = GetCommandBindingAsync(LoadDataTypesAsync);
        ConfigureAxesCommand = GetCommandBindingAsync(ConfigureAxesAsync);
        SaveCommand = GetCommandBindingAsync(SaveAsync);
        BrowseImportCommand = GetCommandBinding(BrowseImport);
        BrowseExportCommand = GetCommandBinding(BrowseExport);
        ImportLibraryCommand = GetCommandBindingAsync(ImportLibraryAsync);
        ExportLibraryCommand = GetCommandBindingAsync(ExportLibraryAsync);
    }

    public ObservableCollection<string> InstalledVersions { get; } = new();

    public ObservableCollection<TiaPlcInfo> Plcs { get; } = new();

    public ObservableCollection<TiaProgramItemInfo> ProgramItems { get; } = new();

    public ObservableCollection<TiaAxisInfo> Axes { get; } = new();

    public ObservableCollection<TiaHardwareModuleInfo> HardwareModules { get; } = new();

    public ICommand ConnectCommand { get; }

    public ICommand SelectPlcCommand { get; }

    public ICommand LoadHardwareCommand { get; }

    public ICommand LoadBlocksCommand { get; }

    public ICommand LoadDataTypesCommand { get; }

    public ICommand ConfigureAxesCommand { get; }

    public ICommand SaveCommand { get; }

    public ICommand BrowseImportCommand { get; }

    public ICommand BrowseExportCommand { get; }

    public ICommand ImportLibraryCommand { get; }

    public ICommand ExportLibraryCommand { get; }

    private string _libraryPath = string.Empty;
    public string LibraryPath
    {
        get => _libraryPath;
        set
        {
            _libraryPath = value;
            OnPropertyChanged();
        }
    }

    private string _exportPath = string.Empty;
    public string ExportPath
    {
        get => _exportPath;
        set
        {
            _exportPath = value;
            OnPropertyChanged();
        }
    }

    private string _libraryName = "VICOBIB";
    public string LibraryName
    {
        get => _libraryName;
        set
        {
            _libraryName = value;
            OnPropertyChanged();
        }
    }

    private bool _configureAxesDuringImport;
    public bool ConfigureAxesDuringImport
    {
        get => _configureAxesDuringImport;
        set
        {
            _configureAxesDuringImport = value;
            OnPropertyChanged();
        }
    }

    private int _operationProgress;
    public int OperationProgress
    {
        get => _operationProgress;
        private set
        {
            _operationProgress = value;
            OnPropertyChanged();
        }
    }

    public string? SelectedVersion
    {
        get => _selectedVersion;
        set
        {
            if (string.Equals(_selectedVersion, value, StringComparison.Ordinal))
                return;

            _selectedVersion = value;
            OnPropertyChanged();
        }
    }

    public TiaPlcInfo? SelectedPlc
    {
        get => _selectedPlc;
        set
        {
            if (ReferenceEquals(_selectedPlc, value))
                return;

            _selectedPlc = value;
            OnPropertyChanged();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            _isBusy = value;
            OnPropertyChanged();
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set
        {
            _statusText = value;
            OnPropertyChanged();
        }
    }

    public ValueTask DisposeAsync() => _client.DisposeAsync();

    private async Task ConnectAsync()
    {
        if (IsBusy || string.IsNullOrWhiteSpace(SelectedVersion))
            return;

        await RunBusyAsync("Verbindung zu TIA Portal wird hergestellt …", async () =>
        {
            await _client.ConnectAsync();
            if (!await _client.PingAsync())
                throw new InvalidOperationException("TIA Bridge antwortet nicht.");

            await _client.SelectVersionAsync(SelectedVersion);
            await _client.AttachAsync();

            var plcs = await _client.ListPlcsAsync();
            Replace(Plcs, plcs);
            SelectedPlc = Plcs.FirstOrDefault();
            HardwareModules.Clear();
            StatusText = $"Mit TIA Portal {SelectedVersion} verbunden; {Plcs.Count} PLC(s) gefunden.";
        });
    }

    private async Task SelectPlcAsync()
    {
        if (SelectedPlc is null)
            return;

        await RunBusyAsync("PLC wird ausgewählt …", async () =>
        {
            await _client.SelectPlcAsync(SelectedPlc.Index);
            ProgramItems.Clear();
            Axes.Clear();
            HardwareModules.Clear();
            StatusText = $"PLC '{SelectedPlc.Name}' ist ausgewählt.";
        });
    }

    private async Task LoadHardwareAsync()
    {
        if (SelectedPlc is null)
            return;

        await RunBusyAsync("TIA-Hardwarekonfiguration wird gelesen …", async () =>
        {
            // Select again so the bridge state always follows the combobox
            // selection, even if the user skipped the explicit select button.
            await _client.SelectPlcAsync(SelectedPlc.Index);
            var modules = await _client.ListHardwareAsync();
            Replace(HardwareModules, modules);
            var addressed = HardwareModules.Count(module =>
                module.InputStartByte >= 0 || module.OutputStartByte >= 0);
            StatusText = $"{HardwareModules.Count} TIA-Hardwareelement(e) geladen; " +
                         $"{addressed} mit E-/A-Adresse.";
        });
    }

    private Task LoadBlocksAsync() => LoadTreeAsync(
        "Programmbausteine werden geladen …",
        _client.ListProgramBlocksAsync,
        "Programmbausteine");

    private Task LoadDataTypesAsync() => LoadTreeAsync(
        "Datentypen werden geladen …",
        _client.ListDataTypesAsync,
        "Datentypen");

    private async Task LoadTreeAsync(
        string busyText,
        Func<CancellationToken, Task<TiaProjectTree>> loader,
        string description)
    {
        await RunBusyAsync(busyText, async () =>
        {
            var tree = await loader(CancellationToken.None);
            Replace(ProgramItems, tree.Items);
            StatusText = $"{tree.Items.Count} {description} geladen.";
        });
    }

    private async Task ConfigureAxesAsync()
    {
        await RunBusyAsync("Achsen werden für die Simulation konfiguriert …", async () =>
        {
            var axes = await _client.ConfigureAxesAsync();
            Replace(Axes, axes);
            StatusText = $"{axes.Count} Achse(n) konfiguriert.";
        });
    }

    private async Task SaveAsync()
    {
        await RunBusyAsync("TIA-Projekt wird gespeichert …", async () =>
        {
            await _client.SaveAsync();
            StatusText = "TIA-Projekt gespeichert.";
        });
    }

    private void BrowseImport()
    {
        var selected = _folderSelection.SelectFolder("ViCo-Bibliothek auswählen", LibraryPath);
        if (selected is not null)
            LibraryPath = selected;
    }

    private void BrowseExport()
    {
        var selected = _folderSelection.SelectFolder("Exportziel auswählen", ExportPath);
        if (selected is not null)
            ExportPath = selected;
    }

    private async Task ImportLibraryAsync()
    {
        if (string.IsNullOrWhiteSpace(LibraryPath) || string.IsNullOrWhiteSpace(SelectedVersion))
            return;

        await RunBusyAsync("ViCo-Bibliothek wird importiert …", async () =>
        {
            OperationProgress = 0;
            var progress = new Progress<TiaLibraryProgress>(UpdateLibraryProgress);
            await _libraryService.ImportAsync(
                LibraryPath,
                ConfigureAxesDuringImport,
                SelectedVersion,
                progress);
            OperationProgress = 100;
            StatusText = "ViCo-Bibliothek wurde importiert und das TIA-Projekt gespeichert.";
        });
    }

    private async Task ExportLibraryAsync()
    {
        if (string.IsNullOrWhiteSpace(ExportPath) ||
            string.IsNullOrWhiteSpace(LibraryName) ||
            string.IsNullOrWhiteSpace(SelectedVersion))
        {
            return;
        }

        await RunBusyAsync("ViCo-Bibliothek wird exportiert …", async () =>
        {
            OperationProgress = 0;
            var progress = new Progress<TiaLibraryProgress>(UpdateLibraryProgress);
            var path = await _libraryService.ExportAsync(
                LibraryName,
                ExportPath,
                SelectedVersion,
                progress);
            OperationProgress = 100;
            StatusText = $"ViCo-Bibliothek exportiert: {path}";
        });
    }

    private void UpdateLibraryProgress(TiaLibraryProgress progress)
    {
        OperationProgress = progress.Total == 0
            ? 0
            : (int)Math.Round(progress.Completed * 100d / progress.Total);
        StatusText = progress.Operation;
    }

    private async Task RunBusyAsync(string status, Func<Task> action)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        StatusText = status;
        try
        {
            await action();
        }
        catch (OperationCanceledException)
        {
            StatusText = "TIA-Vorgang wurde abgebrochen.";
            _log.Warning("TIA Portal", StatusText);
        }
        catch (Exception exception)
        {
            // Commands are invoked from async-void WPF command bindings. By
            // handling bridge failures here, the user gets a clear status and
            // a diagnostic entry instead of an unhandled runtime exception.
            StatusText = $"TIA-Vorgang fehlgeschlagen: {exception.Message}";
            _log.Error("TIA Portal", StatusText, exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values)
    {
        target.Clear();
        foreach (var value in values)
            target.Add(value);
    }
}
