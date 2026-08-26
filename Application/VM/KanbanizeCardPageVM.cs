using System.Collections.ObjectModel;
using System.Windows.Input;
using VIBN_Tools.Core.Kanbanize;
using VIBN_Tools.Core.ViCo;
using VIBN_Tools.GlobalClasses;

namespace VIBN_Tools.Application.VM;

/// <summary>
/// Coordinates shared board data and the optional manual portion of the
/// Kanbanize page. The VIBN workplace automation is delegated to its own view
/// model; neither workflow contains license or permission-request logic.
/// </summary>
public sealed class KanbanizeCardPageVM : MvvmBase, IDisposable
{
    private readonly IKanbanizeCardService _cards;
    private readonly IApplicationLog _log;
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private CancellationTokenSource? _structureCancellation;
    private IReadOnlyList<KanbanizeColumnInfo> _boardColumns = Array.Empty<KanbanizeColumnInfo>();
    private bool _initialized;

    public KanbanizeCardPageVM(
        IKanbanizeCardService cards,
        IVibnWorkplaceSynchronizationService workplaceSynchronization,
        IApplicationLog? log = null)
    {
        _cards = cards ?? throw new ArgumentNullException(nameof(cards));
        _log = log ?? NullApplicationLog.Instance;
        WorkplaceSynchronization = new VibnWorkplaceSynchronizationVM(
            _cards,
            workplaceSynchronization,
            _log);
        foreach (var priority in Enumerable.Range(
                     KanbanizeCardDraftPolicy.MinimumPriority,
                     KanbanizeCardDraftPolicy.MaximumPriority - KanbanizeCardDraftPolicy.MinimumPriority + 1))
        {
            Priorities.Add(priority);
        }

        ReloadBoardsCommand = GetCommandBindingAsync(LoadBoardsAsync);
        CreateCardCommand = GetCommandBindingAsync(CreateCardAsync);
    }

    public ObservableCollection<KanbanizeBoardInfo> Boards { get; } = new();

    /// <summary>
    /// Separate workflow for the idempotent VIBN-to-workplace automation. The
    /// existing properties in this view model remain exclusively responsible
    /// for optional, manually created cards.
    /// </summary>
    public VibnWorkplaceSynchronizationVM WorkplaceSynchronization { get; }

    public ObservableCollection<KanbanizeLaneInfo> Lanes { get; } = new();

    public ObservableCollection<KanbanizeColumnInfo> Columns { get; } = new();

    public ObservableCollection<int> Priorities { get; } = new();

    public ICommand ReloadBoardsCommand { get; }

    public ICommand CreateCardCommand { get; }

    public bool IsConfigured => _cards.IsConfigured;

    private KanbanizeBoardInfo? _selectedBoard;
    public KanbanizeBoardInfo? SelectedBoard
    {
        get => _selectedBoard;
        set
        {
            if (ReferenceEquals(_selectedBoard, value))
                return;
            _selectedBoard = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanCreate));
            _ = LoadBoardStructureAsync(value);
        }
    }

    private KanbanizeLaneInfo? _selectedLane;
    public KanbanizeLaneInfo? SelectedLane
    {
        get => _selectedLane;
        set
        {
            if (ReferenceEquals(_selectedLane, value))
                return;
            _selectedLane = value;
            OnPropertyChanged();
            RefreshColumnsForSelectedLane();
            OnPropertyChanged(nameof(CanCreate));
        }
    }

    private KanbanizeColumnInfo? _selectedColumn;
    public KanbanizeColumnInfo? SelectedColumn
    {
        get => _selectedColumn;
        set
        {
            if (ReferenceEquals(_selectedColumn, value))
                return;
            _selectedColumn = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanCreate));
        }
    }

    private string _title = string.Empty;
    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanCreate));
        }
    }

    private string _description = string.Empty;
    public string Description
    {
        get => _description;
        set
        {
            _description = value;
            OnPropertyChanged();
        }
    }

    private string _customId = string.Empty;
    public string CustomId
    {
        get => _customId;
        set
        {
            _customId = value;
            OnPropertyChanged();
        }
    }

    private int _selectedPriority = 3;
    public int SelectedPriority
    {
        get => _selectedPriority;
        set
        {
            _selectedPriority = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanCreate));
        }
    }

    private bool _hasDeadline;
    public bool HasDeadline
    {
        get => _hasDeadline;
        set
        {
            _hasDeadline = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanCreate));
        }
    }

    private DateTime? _deadline = DateTime.Today;
    public DateTime? Deadline
    {
        get => _deadline;
        set
        {
            _deadline = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanCreate));
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
            OnPropertyChanged(nameof(CanCreate));
        }
    }

    private string _statusText = "Kanbanize-Kartenerstellung ist bereit.";
    public string StatusText
    {
        get => _statusText;
        private set
        {
            _statusText = value;
            OnPropertyChanged();
        }
    }

    public bool CanCreate =>
        IsConfigured &&
        !IsBusy &&
        SelectedBoard is not null &&
        SelectedLane is not null &&
        SelectedColumn is not null &&
        !string.IsNullOrWhiteSpace(Title);

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;
        _initialized = true;
        await LoadBoardsAsync();
    }

    public void Dispose()
    {
        WorkplaceSynchronization.Dispose();
        _structureCancellation?.Cancel();
        _structureCancellation?.Dispose();
        _lifetimeCancellation.Cancel();
        _lifetimeCancellation.Dispose();
    }

    private async Task LoadBoardsAsync()
    {
        if (IsBusy)
            return;
        if (!IsConfigured)
        {
            StatusText = "Kanbanize-Zugriff ist nicht konfiguriert. Bitte den API-Schlüssel bereitstellen.";
            _log.Warning("Kanbanize Karten", StatusText);
            return;
        }

        IsBusy = true;
        StatusText = "Kanbanize-Boards werden geladen …";
        try
        {
            var boards = await _cards.LoadBoardsAsync(_lifetimeCancellation.Token);
            Replace(Boards, boards);
            WorkplaceSynchronization.SetBoards(boards);
            SelectedBoard = Boards.FirstOrDefault(board =>
                                board.Id == VibnWorkplaceSynchronizationPolicy.DefaultTargetBoardId) ??
                            Boards.FirstOrDefault(board =>
                                (board.Name + " " + board.Description).Contains(
                                    "arbeitsplatz",
                                    StringComparison.OrdinalIgnoreCase)) ??
                            Boards.FirstOrDefault();
            StatusText = boards.Count == 0
                ? "Es wurden keine zugänglichen Kanbanize-Boards gefunden."
                : $"{boards.Count} Kanbanize-Board(s) geladen.";
            _log.Information("Kanbanize Karten", StatusText);
        }
        catch (OperationCanceledException)
        {
            // View was closed while the request was in flight.
        }
        catch (Exception exception)
        {
            StatusText = "Kanbanize-Boards konnten nicht geladen werden.";
            _log.Error("Kanbanize Karten", StatusText, exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadBoardStructureAsync(KanbanizeBoardInfo? board)
    {
        _structureCancellation?.Cancel();
        _structureCancellation?.Dispose();
        _structureCancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCancellation.Token);
        var cancellationToken = _structureCancellation.Token;

        Lanes.Clear();
        Columns.Clear();
        _boardColumns = Array.Empty<KanbanizeColumnInfo>();
        SelectedLane = null;
        SelectedColumn = null;
        if (board is null || !IsConfigured)
            return;

        IsBusy = true;
        StatusText = $"Zielpositionen für {board.Name} werden geladen …";
        try
        {
            var structure = await _cards.LoadBoardStructureAsync(board.Id, cancellationToken);
            if (!ReferenceEquals(SelectedBoard, board))
                return;

            Replace(Lanes, structure.Lanes);
            _boardColumns = structure.Columns;
            SelectedLane = Lanes.FirstOrDefault(lane =>
                               string.Equals(lane.Name.Trim(), "angelegt", StringComparison.OrdinalIgnoreCase)) ??
                           Lanes.FirstOrDefault(lane =>
                               lane.Name.Contains("angelegt", StringComparison.OrdinalIgnoreCase)) ??
                           Lanes.FirstOrDefault();
            RefreshColumnsForSelectedLane();
            StatusText = $"{Lanes.Count} Lane(s) und {Columns.Count} passende Spalte(n) geladen.";
        }
        catch (OperationCanceledException)
        {
            // A newer board selection superseded this request.
        }
        catch (Exception exception)
        {
            StatusText = "Kanbanize-Zielpositionen konnten nicht geladen werden.";
            _log.Error("Kanbanize Karten", StatusText, exception);
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested)
                IsBusy = false;
        }
    }

    private void RefreshColumnsForSelectedLane()
    {
        var matchingColumns = SelectedLane is null
            ? _boardColumns
            : _boardColumns.Where(column => column.WorkflowId == SelectedLane.WorkflowId).ToArray();
        Replace(Columns, matchingColumns);
        SelectedColumn = Columns.FirstOrDefault(column =>
                             string.Equals(column.Name.Trim(), "Backlog", StringComparison.OrdinalIgnoreCase)) ??
                         Columns.FirstOrDefault(column =>
                             column.Name.Contains("Backlog", StringComparison.OrdinalIgnoreCase)) ??
                         Columns.FirstOrDefault();
    }

    private async Task CreateCardAsync()
    {
        if (SelectedBoard is null || SelectedLane is null || SelectedColumn is null)
        {
            StatusText = "Bitte Board, Lane und Spalte auswählen.";
            return;
        }

        DateTimeOffset? deadline = HasDeadline && Deadline is not null
            ? new DateTimeOffset(Deadline.Value.Date, TimeZoneInfo.Local.GetUtcOffset(Deadline.Value.Date))
            : null;
        var draft = new KanbanizeCardDraft(
            SelectedBoard.Id,
            SelectedLane.Id,
            SelectedColumn.Id,
            Title,
            Description,
            SelectedPriority,
            CustomId,
            deadline);
        var validationError = KanbanizeCardDraftPolicy.Validate(draft);
        if (validationError is not null)
        {
            StatusText = validationError;
            return;
        }

        IsBusy = true;
        StatusText = "Kanbanize-Karte wird erstellt …";
        try
        {
            var created = await _cards.CreateCardAsync(draft, _lifetimeCancellation.Token);
            var cardReference = created.Id > 0 ? $" (ID {created.Id})" : string.Empty;
            StatusText = $"Karte \"{created.Title}\" wurde erstellt{cardReference}.";
            _log.Information("Kanbanize Karten", StatusText);

            // Keep the chosen destination for consecutive cards, but prevent a
            // second click from accidentally creating the same title again.
            Title = string.Empty;
            Description = string.Empty;
            CustomId = string.Empty;
        }
        catch (OperationCanceledException)
        {
            // View was closed while the request was in flight.
        }
        catch (Exception exception)
        {
            StatusText = "Kanbanize-Karte konnte nicht erstellt werden.";
            _log.Error("Kanbanize Karten", StatusText, exception);
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
