using System.Collections.ObjectModel;
using System.Windows.Input;
using VIBN_Tools.Core.Kanbanize;
using VIBN_Tools.Core.ViCo;
using VIBN_Tools.GlobalClasses;

namespace VIBN_Tools.Application.VM;

/// <summary>Display-only row for a safe VIBN workplace synchronization preview.</summary>
public sealed class VibnWorkplaceSynchronizationRowVM : MvvmBase
{
    private readonly Action _selectionChanged;
    private bool _isSelected;

    public VibnWorkplaceSynchronizationRowVM(
        VibnWorkplaceSynchronizationItem item,
        Action? selectionChanged = null)
    {
        _selectionChanged = selectionChanged ?? (() => { });
        Action = item.Action;
        SourceTitle = item.SourceCard.Title;
        SourceCardId = item.SourceCard.Id;
        SourceDeadline = FormatDeadline(item.SourceCard.Deadline);
        CalculatedStart = FormatDeadline(item.Schedule?.StartDate);
        CalculatedEnd = FormatDeadline(item.Schedule?.EndDate);
        TargetCardId = item.TargetCard?.Id.ToString() ?? "—";
        TargetStart = FormatDeadline(item.TargetCard?.StartDate);
        TargetDeadline = FormatDeadline(item.TargetCard?.Deadline);
        Details = item.Message;
    }

    public VibnWorkplaceSynchronizationAction Action { get; }

    public bool CanSynchronize =>
        Action is VibnWorkplaceSynchronizationAction.Create or
            VibnWorkplaceSynchronizationAction.UpdateDeadline;

    /// <summary>Only explicitly checked preview rows may be written.</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            var normalized = CanSynchronize && value;
            if (_isSelected == normalized)
                return;
            _isSelected = normalized;
            OnPropertyChanged();
            _selectionChanged();
        }
    }

    public string ActionText => Action switch
    {
        VibnWorkplaceSynchronizationAction.Create => "Neu",
        VibnWorkplaceSynchronizationAction.UpdateDeadline => "Zeitplan",
        VibnWorkplaceSynchronizationAction.Unchanged => "Unverändert",
        VibnWorkplaceSynchronizationAction.Conflict => "Konflikt",
        _ => Action.ToString()
    };

    public string ActionBackground => Action switch
    {
        VibnWorkplaceSynchronizationAction.Create => "#FFDDEBF7",
        VibnWorkplaceSynchronizationAction.UpdateDeadline => "#FFFFF2CC",
        VibnWorkplaceSynchronizationAction.Unchanged => "#FFE2F0D9",
        VibnWorkplaceSynchronizationAction.Conflict => "#FFFFC7CE",
        _ => "#FFF3F5F7"
    };

    public string SourceTitle { get; }

    public int SourceCardId { get; }

    public string SourceDeadline { get; }

    public string CalculatedStart { get; }

    public string CalculatedEnd { get; }

    public string TargetCardId { get; }

    public string TargetStart { get; }

    public string TargetDeadline { get; }

    public string Details { get; }

    private static string FormatDeadline(DateTimeOffset? deadline) =>
        deadline?.ToLocalTime().ToString("dd.MM.yyyy HH:mm") ?? "—";
}

/// <summary>
/// Owns only the VIBN-to-workplace replication workflow. Keeping it separate
/// from manual card creation makes its deliberately narrow write scope easy to
/// audit: create a missing linked card or patch its calculated schedule,
/// nothing else.
/// </summary>
public sealed class VibnWorkplaceSynchronizationVM : MvvmBase, IDisposable
{
    private readonly IKanbanizeCardService _cards;
    private readonly IVibnWorkplaceSynchronizationService _synchronization;
    private readonly IApplicationLog _log;
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private CancellationTokenSource? _structureCancellation;
    private IReadOnlyList<KanbanizeColumnInfo> _targetBoardColumns = Array.Empty<KanbanizeColumnInfo>();
    private VibnWorkplaceSynchronizationPreview? _preview;
    private VibnWorkplaceSynchronizationSettings? _previewSettings;

    public VibnWorkplaceSynchronizationVM(
        IKanbanizeCardService cards,
        IVibnWorkplaceSynchronizationService synchronization,
        IApplicationLog? log = null)
    {
        _cards = cards ?? throw new ArgumentNullException(nameof(cards));
        _synchronization = synchronization ?? throw new ArgumentNullException(nameof(synchronization));
        _log = log ?? NullApplicationLog.Instance;
        foreach (var priority in Enumerable.Range(
                     KanbanizeCardDraftPolicy.MinimumPriority,
                     KanbanizeCardDraftPolicy.MaximumPriority - KanbanizeCardDraftPolicy.MinimumPriority + 1))
        {
            Priorities.Add(priority);
        }

        PreviewCommand = GetCommandBindingAsync(LoadPreviewAsync);
        SynchronizeCommand = GetCommandBindingAsync(SynchronizeAsync);
    }

    public ObservableCollection<KanbanizeBoardInfo> Boards { get; } = new();

    public ObservableCollection<KanbanizeLaneInfo> TargetLanes { get; } = new();

    public ObservableCollection<KanbanizeColumnInfo> TargetColumns { get; } = new();

    public ObservableCollection<int> Priorities { get; } = new();

    public ObservableCollection<VibnWorkplaceSynchronizationRowVM> PreviewItems { get; } = new();

    public ICommand PreviewCommand { get; }

    public ICommand SynchronizeCommand { get; }

    public bool IsConfigured => _synchronization.IsConfigured;

    private KanbanizeBoardInfo? _selectedSourceBoard;
    public KanbanizeBoardInfo? SelectedSourceBoard
    {
        get => _selectedSourceBoard;
        set
        {
            if (ReferenceEquals(_selectedSourceBoard, value))
                return;
            _selectedSourceBoard = value;
            OnPropertyChanged();
            InvalidatePreview();
        }
    }

    private KanbanizeBoardInfo? _selectedTargetBoard;
    public KanbanizeBoardInfo? SelectedTargetBoard
    {
        get => _selectedTargetBoard;
        set
        {
            if (ReferenceEquals(_selectedTargetBoard, value))
                return;
            _selectedTargetBoard = value;
            OnPropertyChanged();
            InvalidatePreview();
            _ = LoadTargetStructureAsync(value);
        }
    }

    private KanbanizeLaneInfo? _selectedTargetLane;
    public KanbanizeLaneInfo? SelectedTargetLane
    {
        get => _selectedTargetLane;
        set
        {
            if (ReferenceEquals(_selectedTargetLane, value))
                return;
            _selectedTargetLane = value;
            OnPropertyChanged();
            RefreshTargetColumns();
            InvalidatePreview();
        }
    }

    private KanbanizeColumnInfo? _selectedTargetColumn;
    public KanbanizeColumnInfo? SelectedTargetColumn
    {
        get => _selectedTargetColumn;
        set
        {
            if (ReferenceEquals(_selectedTargetColumn, value))
                return;
            _selectedTargetColumn = value;
            OnPropertyChanged();
            InvalidatePreview();
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
            InvalidatePreview();
        }
    }

    private bool _synchronizeDeadlines = true;
    public bool SynchronizeDeadlines
    {
        get => _synchronizeDeadlines;
        set
        {
            _synchronizeDeadlines = value;
            OnPropertyChanged();
            InvalidatePreview();
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
            OnPropertyChanged(nameof(CanPreview));
            OnPropertyChanged(nameof(CanSynchronize));
        }
    }

    public bool CanPreview => IsConfigured && !IsBusy && CreateSettings() is not null;

    public bool CanSynchronize =>
        !IsBusy &&
        _preview is { HasChanges: true } &&
        _previewSettings is not null &&
        _previewSettings == CreateSettings() &&
        PreviewItems.Any(item => item.CanSynchronize && item.IsSelected);

    public int CreateCount => _preview?.CreateCount ?? 0;

    public int DeadlineUpdateCount => _preview?.DeadlineUpdateCount ?? 0;

    public int UnchangedCount => _preview?.UnchangedCount ?? 0;

    public int ConflictCount => _preview?.ConflictCount ?? 0;

    public int ExcludedSourceCardCount => _preview?.ExcludedSourceCardCount ?? 0;

    private string _statusText = "VIBN-Synchronisierung ist bereit. Zuerst prüfen, danach bewusst synchronisieren.";
    public string StatusText
    {
        get => _statusText;
        private set
        {
            _statusText = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Receives the board list loaded once by the parent page. The historic
    /// board IDs are only defaults; users can select another permitted board.
    /// </summary>
    public void SetBoards(IEnumerable<KanbanizeBoardInfo> boards)
    {
        var values = boards?.ToArray() ?? Array.Empty<KanbanizeBoardInfo>();
        Replace(Boards, values);

        var sourceBoard = SelectPreferredBoard(
            values,
            VibnWorkplaceSynchronizationPolicy.DefaultSourceBoardId,
            "virtuell");
        var targetBoard = SelectPreferredBoard(
            values,
            VibnWorkplaceSynchronizationPolicy.DefaultTargetBoardId,
            "arbeitsplatz");
        SelectedSourceBoard = sourceBoard;
        SelectedTargetBoard = targetBoard;
        if (sourceBoard is null || targetBoard is null)
            StatusText = "Quell- und Zielboard aus der Liste auswählen und anschließend prüfen.";
    }

    public void Dispose()
    {
        _structureCancellation?.Cancel();
        _structureCancellation?.Dispose();
        _lifetimeCancellation.Cancel();
        _lifetimeCancellation.Dispose();
    }

    private async Task LoadTargetStructureAsync(KanbanizeBoardInfo? board)
    {
        _structureCancellation?.Cancel();
        _structureCancellation?.Dispose();
        _structureCancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCancellation.Token);
        var cancellationToken = _structureCancellation.Token;

        TargetLanes.Clear();
        TargetColumns.Clear();
        _targetBoardColumns = Array.Empty<KanbanizeColumnInfo>();
        SelectedTargetLane = null;
        SelectedTargetColumn = null;
        if (board is null || !IsConfigured)
            return;

        IsBusy = true;
        StatusText = $"Zielpositionen für {board.Name} werden geladen …";
        try
        {
            var structure = await _cards.LoadBoardStructureAsync(board.Id, cancellationToken);
            if (!ReferenceEquals(SelectedTargetBoard, board))
                return;

            Replace(TargetLanes, structure.Lanes);
            _targetBoardColumns = structure.Columns;
            SelectedTargetLane = TargetLanes.FirstOrDefault(lane => lane.Id == 28125) ??
                                 TargetLanes.FirstOrDefault(lane => lane.Name.Contains("Tool", StringComparison.OrdinalIgnoreCase)) ??
                                 TargetLanes.FirstOrDefault();
            RefreshTargetColumns();
            StatusText = "Zielpositionen geladen. Änderungen werden erst nach ‚Prüfen‘ und ‚Synchronisieren‘ ausgeführt.";
        }
        catch (OperationCanceledException)
        {
            // A newer target selection superseded this request.
        }
        catch (Exception exception)
        {
            StatusText = "Zielpositionen konnten nicht geladen werden.";
            _log.Error("Kanbanize Synchronisierung", StatusText, exception);
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested)
                IsBusy = false;
        }
    }

    private void RefreshTargetColumns()
    {
        var matchingColumns = SelectedTargetLane is null
            ? _targetBoardColumns
            : _targetBoardColumns.Where(column => column.WorkflowId == SelectedTargetLane.WorkflowId).ToArray();
        Replace(TargetColumns, matchingColumns);
        SelectedTargetColumn = TargetColumns.FirstOrDefault(column => column.Id == 29373) ??
                               TargetColumns.FirstOrDefault(column => column.Name.Contains("Backlog", StringComparison.OrdinalIgnoreCase)) ??
                               TargetColumns.FirstOrDefault();
    }

    private async Task LoadPreviewAsync()
    {
        var settings = CreateSettings();
        if (settings is null)
        {
            StatusText = "Quellboard, Zielboard, Ziel-Lane und Zielspalte auswählen.";
            return;
        }

        IsBusy = true;
        StatusText = "VIBN-Karten und Arbeitsplatzkarten werden verglichen …";
        try
        {
            var preview = await _synchronization.PreviewAsync(settings, _lifetimeCancellation.Token);
            ApplyPreview(preview, settings);
            StatusText = DescribePreview(preview);
            _log.Information("Kanbanize Synchronisierung", StatusText);
        }
        catch (OperationCanceledException)
        {
            // View was closed while the preview was loading.
        }
        catch (Exception exception)
        {
            StatusText = "VIBN-Synchronisierung konnte nicht geprüft werden.";
            _log.Error("Kanbanize Synchronisierung", StatusText, exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SynchronizeAsync()
    {
        var settings = CreateSettings();
        if (settings is null || _previewSettings is null || _previewSettings != settings)
        {
            StatusText = "Die Auswahl hat sich geändert. Bitte zuerst erneut prüfen.";
            return;
        }
        if (_preview is not { HasChanges: true })
        {
            StatusText = "Es liegen keine sicheren Änderungen vor.";
            return;
        }

        IsBusy = true;
        StatusText = "VIBN-Karten werden sicher synchronisiert …";
        try
        {
            var selectedSourceIds = PreviewItems
                .Where(item => item.CanSynchronize && item.IsSelected)
                .Select(item => item.SourceCardId)
                .ToArray();
            if (selectedSourceIds.Length == 0)
            {
                StatusText = "Mindestens eine Änderung in der Vorschau markieren.";
                return;
            }
            var result = await _synchronization.SynchronizeAsync(
                settings,
                selectedSourceIds,
                _lifetimeCancellation.Token);
            var refreshedPreview = await _synchronization.PreviewAsync(settings, _lifetimeCancellation.Token);
            ApplyPreview(refreshedPreview, settings);

            foreach (var failure in result.Failures)
                _log.Warning("Kanbanize Synchronisierung", failure);
            StatusText = $"Synchronisierung abgeschlossen: {result.CreatedCount} neu, " +
                         $"{result.DeadlineUpdateCount} Zeitplan(e) angepasst, {result.Failures.Count} Fehler. " +
                         DescribePreview(refreshedPreview);
            _log.Information("Kanbanize Synchronisierung", StatusText);
        }
        catch (OperationCanceledException)
        {
            // View was closed while the synchronization was running.
        }
        catch (Exception exception)
        {
            StatusText = "VIBN-Synchronisierung konnte nicht ausgeführt werden.";
            _log.Error("Kanbanize Synchronisierung", StatusText, exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private VibnWorkplaceSynchronizationSettings? CreateSettings() =>
        SelectedSourceBoard is null || SelectedTargetBoard is null ||
        SelectedTargetLane is null || SelectedTargetColumn is null
            ? null
            : new VibnWorkplaceSynchronizationSettings(
                SelectedSourceBoard.Id,
                SelectedTargetBoard.Id,
                SelectedTargetLane.Id,
                SelectedTargetColumn.Id,
                SelectedPriority,
                SynchronizeDeadlines);

    private void ApplyPreview(
        VibnWorkplaceSynchronizationPreview preview,
        VibnWorkplaceSynchronizationSettings settings)
    {
        _preview = preview;
        _previewSettings = settings;
        Replace(PreviewItems, preview.Items.Select(item =>
            new VibnWorkplaceSynchronizationRowVM(item, OnPreviewSelectionChanged)));
        OnPropertyChanged(nameof(CreateCount));
        OnPropertyChanged(nameof(DeadlineUpdateCount));
        OnPropertyChanged(nameof(UnchangedCount));
        OnPropertyChanged(nameof(ConflictCount));
        OnPropertyChanged(nameof(ExcludedSourceCardCount));
        OnPropertyChanged(nameof(CanSynchronize));
    }

    private void InvalidatePreview()
    {
        _preview = null;
        _previewSettings = null;
        PreviewItems.Clear();
        OnPropertyChanged(nameof(CreateCount));
        OnPropertyChanged(nameof(DeadlineUpdateCount));
        OnPropertyChanged(nameof(UnchangedCount));
        OnPropertyChanged(nameof(ConflictCount));
        OnPropertyChanged(nameof(ExcludedSourceCardCount));
        OnPropertyChanged(nameof(CanPreview));
        OnPropertyChanged(nameof(CanSynchronize));
    }

    private void OnPreviewSelectionChanged()
    {
        OnPropertyChanged(nameof(CanSynchronize));
        var selectedCount = PreviewItems.Count(item => item.CanSynchronize && item.IsSelected);
        StatusText = selectedCount == 0
            ? "Die gewünschten Änderungen in der Vorschau markieren."
            : $"{selectedCount} Änderung(en) für die Synchronisierung markiert.";
    }

    private static KanbanizeBoardInfo? SelectPreferredBoard(
        IEnumerable<KanbanizeBoardInfo> boards,
        int preferredId,
        string nameFragment) =>
        boards.FirstOrDefault(board => board.Id == preferredId) ??
        boards.FirstOrDefault(board =>
            (board.Name + " " + board.Description).Contains(nameFragment, StringComparison.OrdinalIgnoreCase));

    private static string DescribePreview(VibnWorkplaceSynchronizationPreview preview) =>
        $"Prüfung: {preview.CreateCount} neu, {preview.DeadlineUpdateCount} Zeitplan(e), " +
        $"{preview.UnchangedCount} unverändert, {preview.ConflictCount} Konflikt(e), " +
        $"{preview.ExcludedSourceCardCount} Quelle(n) ausgeschlossen.";

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values)
    {
        target.Clear();
        foreach (var value in values)
            target.Add(value);
    }
}
