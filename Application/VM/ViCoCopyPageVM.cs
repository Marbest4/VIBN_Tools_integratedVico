using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using VIBN_Tools.Core.ViCo;
using VIBN_Tools.GlobalClasses;

namespace VIBN_Tools.Application.VM;

public sealed record CopySourceEntry(
    string Name,
    string FullPath,
    bool IsDirectory,
    bool CopyContentsToDestination = false);

public sealed class ViCoCopyPageVM : MvvmBase, IDisposable
{
    private readonly IFileCopyService _copyService;
    private readonly IFolderSelectionService _folderSelection;
    private readonly ViCoWorkspaceContext _workspaceContext;
    private readonly IProjectStructureService _projectStructure;
    private CancellationTokenSource? _copyCancellation;

    public ViCoCopyPageVM(
        IFileCopyService copyService,
        IFolderSelectionService folderSelection,
        ViCoWorkspaceContext workspaceContext,
        IProjectStructureService projectStructure)
    {
        _copyService = copyService;
        _folderSelection = folderSelection;
        _workspaceContext = workspaceContext;
        _projectStructure = projectStructure;

        AddFolderCommand = GetCommandBinding(AddFolder);
        AddFilesCommand = GetCommandBinding(AddFiles);
        RemoveCommand = GetCommandBinding(RemoveSelected);
        BrowseDestinationCommand = GetCommandBinding(BrowseDestination);
        TransferCommand = GetCommandBindingAsync(TransferAsync);
        CancelCommand = GetCommandBinding(Cancel);
        ApplySelectionCommand = GetCommandBinding(ApplyWorkspaceSelection);
        AddProjectContentCommand = GetCommandBinding(AddProjectContent);
    }

    public ObservableCollection<CopySourceEntry> Sources { get; } = new();

    public ICommand AddFolderCommand { get; }

    public ICommand AddFilesCommand { get; }

    public ICommand RemoveCommand { get; }

    public ICommand BrowseDestinationCommand { get; }

    public ICommand TransferCommand { get; }

    public ICommand CancelCommand { get; }

    public ICommand ApplySelectionCommand { get; }

    public ICommand AddProjectContentCommand { get; }

    private bool _isServerToPc = true;
    public bool IsServerToPc
    {
        get => _isServerToPc;
        set
        {
            _isServerToPc = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DirectionText));
            ApplyWorkspaceSelection();
        }
    }

    public string DirectionText => IsServerToPc ? "Server → PC" : "PC → Server";

    private string _sourceRootPath = string.Empty;
    public string SourceRootPath
    {
        get => _sourceRootPath;
        private set
        {
            _sourceRootPath = value;
            OnPropertyChanged();
        }
    }

    private string _selectionSummary = "Noch keine Auswahl aus der ViCo-Übersicht übernommen.";
    public string SelectionSummary
    {
        get => _selectionSummary;
        private set
        {
            _selectionSummary = value;
            OnPropertyChanged();
        }
    }

    private CopySourceEntry? _selectedSource;
    public CopySourceEntry? SelectedSource
    {
        get => _selectedSource;
        set
        {
            _selectedSource = value;
            OnPropertyChanged();
        }
    }

    private string _destinationPath = string.Empty;
    public string DestinationPath
    {
        get => _destinationPath;
        set
        {
            _destinationPath = value;
            OnPropertyChanged();
        }
    }

    private int _progressPercent;
    public int ProgressPercent
    {
        get => _progressPercent;
        private set
        {
            _progressPercent = value;
            OnPropertyChanged();
        }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            _isBusy = value;
            OnPropertyChanged();
        }
    }

    private string _statusText = "Dateien oder Ordner für die Übertragung auswählen.";
    public string StatusText
    {
        get => _statusText;
        private set
        {
            _statusText = value;
            OnPropertyChanged();
        }
    }

    public void Dispose()
    {
        _copyCancellation?.Cancel();
        _copyCancellation?.Dispose();
    }

    private void AddFolder()
    {
        var path = _folderSelection.SelectFolder("Quellordner auswählen", SourceRootPath);
        if (path is not null)
            AddSource(path, true);
    }

    private void AddFiles()
    {
        foreach (var path in _folderSelection.SelectFiles("Quelldateien auswählen", SourceRootPath))
            AddSource(path, false);
    }

    private void AddSource(string path, bool isDirectory)
    {
        if (Sources.Any(item => string.Equals(item.FullPath, path, StringComparison.OrdinalIgnoreCase)))
            return;

        var name = isDirectory
            ? new DirectoryInfo(path).Name
            : Path.GetFileName(path);
        Sources.Add(new CopySourceEntry(name, path, isDirectory));
        StatusText = $"{Sources.Count} Element(e) vorgemerkt.";
    }

    private void RemoveSelected()
    {
        if (SelectedSource is null)
            return;
        Sources.Remove(SelectedSource);
        SelectedSource = null;
    }

    private void BrowseDestination()
    {
        var path = _folderSelection.SelectFolder("Zielordner auswählen", DestinationPath);
        if (path is not null)
            DestinationPath = path;
    }

    public void ApplyWorkspaceSelection()
    {
        var selection = _workspaceContext.CurrentSelection;
        if (!selection.IsComplete)
        {
            SelectionSummary = "Bitte zuerst in der ViCo-Übersicht einen PC und ein Projekt auswählen.";
            return;
        }

        SourceRootPath = IsServerToPc
            ? selection.ServerProjectPath
            : selection.WorkstationProjectPath;
        DestinationPath = IsServerToPc
            ? selection.WorkstationProjectPath
            : selection.ServerProjectPath;
        SelectionSummary = $"{selection.PcName} · {selection.Project} · {DirectionText}";
        StatusText = "Auswahl übernommen. Elemente auswählen oder den gesamten Projektinhalt hinzufügen.";
    }

    private void AddProjectContent()
    {
        if (string.IsNullOrWhiteSpace(SourceRootPath) || !Directory.Exists(SourceRootPath))
        {
            StatusText = "Der Quellprojektpfad ist nicht erreichbar.";
            return;
        }

        if (Sources.Any(item => item.CopyContentsToDestination))
            return;
        Sources.Add(new CopySourceEntry("Gesamter Projektinhalt", SourceRootPath, true, true));
        StatusText = "Der gesamte Projektinhalt wurde vorgemerkt.";
    }

    private async Task TransferAsync()
    {
        if (IsBusy || Sources.Count == 0 || string.IsNullOrWhiteSpace(DestinationPath))
            return;
        var selection = _workspaceContext.CurrentSelection;
        if (IsServerToPc &&
            selection.IsComplete &&
            string.Equals(DestinationPath, selection.WorkstationProjectPath, StringComparison.OrdinalIgnoreCase))
        {
            _projectStructure.EnsureCreated(DestinationPath);
        }
        else if (!Directory.Exists(DestinationPath))
        {
            Directory.CreateDirectory(DestinationPath);
        }

        _copyCancellation?.Dispose();
        _copyCancellation = new CancellationTokenSource();
        IsBusy = true;
        ProgressPercent = 0;
        StatusText = "Übertragung wird vorbereitet …";

        try
        {
            var items = Sources.Select(source => new FileCopyItem(
                source.FullPath,
                source.CopyContentsToDestination
                    ? DestinationPath
                    : Path.Combine(DestinationPath, source.Name))).ToArray();
            var progress = new Progress<FileCopyProgress>(value =>
            {
                ProgressPercent = value.Percent;
                StatusText = string.IsNullOrWhiteSpace(value.CurrentFile)
                    ? "Übertragung abgeschlossen."
                    : $"{value.Percent}% – {Path.GetFileName(value.CurrentFile)}";
            });

            await _copyService.CopyAsync(items, progress, _copyCancellation.Token);
        }
        catch (OperationCanceledException)
        {
            StatusText = "Übertragung abgebrochen.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Cancel() => _copyCancellation?.Cancel();
}
