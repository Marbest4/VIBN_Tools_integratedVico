using VIBN_Tools.ContainerGeneration.BusinessLogic.ContainerData;
using VIBN_Tools.ContainerGeneration.BusinessLogic.RequirementsXml;
using VIBN_Tools.GlobalClasses;

namespace VIBN_Tools.ContainerGeneration.Models;

public enum WorkspaceEntryLocation
{
    Container,
    Unassigned,
    Filtered
}

public sealed record WorkspaceEntrySnapshot(
    string PrimaryKey,
    string SignalId,
    string Id,
    string Address,
    string Signal,
    string DataType,
    string SourceFingerprint,
    WorkspaceEntryLocation Location,
    string ContainerId,
    string ContainerName,
    string ComponentType,
    string Slot,
    string Note,
    bool WasManuallyEdited,
    ContainerEntryReviewState ReviewState,
    string ReviewMessage);

public sealed class GenerationWorkspaceSnapshot
{
    public IReadOnlyList<WorkspaceEntrySnapshot> Entries { get; }

    public GenerationWorkspaceSnapshot(IReadOnlyList<WorkspaceEntrySnapshot> entries)
    {
        Entries = entries;
    }
}

public enum ReimportChangeKind
{
    NewFromSource,
    SourceChanged,
    RuleSuggestionChanged,
    NewlyRecognized,
    RemovedFromSource
}

public sealed class ReimportDifference : NotifyBase
{
    private bool _isAccepted;

    public ReimportDifference(
        ReimportChangeKind kind,
        string category,
        string signal,
        string previousValue,
        string detectedValue,
        bool isAccepted,
        WorkspaceEntrySnapshot? previousEntry = null,
        WorkspaceEntrySnapshot? detectedEntry = null,
        ContainerEntry? targetEntry = null)
    {
        Kind = kind;
        Category = category;
        Signal = signal;
        PreviousValue = previousValue;
        DetectedValue = detectedValue;
        _isAccepted = isAccepted;
        PreviousEntry = previousEntry;
        DetectedEntry = detectedEntry;
        TargetEntry = targetEntry;
        ExactDifference = BuildExactDifference(kind, previousEntry, detectedEntry);
    }

    public ReimportChangeKind Kind { get; }
    public string Category { get; }
    public string Signal { get; }
    public string PreviousValue { get; }
    public string DetectedValue { get; }
    public string ExactDifference { get; }
    public string DecisionEffect => GetDecisionEffect(Kind, IsAccepted);

    public bool IsAccepted
    {
        get => _isAccepted;
        set
        {
            if (SetPropertyChange(ref _isAccepted, value))
                OnPropertyChanged(nameof(DecisionEffect));
        }
    }

    internal WorkspaceEntrySnapshot? PreviousEntry { get; }
    internal WorkspaceEntrySnapshot? DetectedEntry { get; }
    internal ContainerEntry? TargetEntry { get; set; }

    public string ToDisplayText() =>
        $"{Category}: {Signal} | bisher: {PreviousValue} | neu erkannt: {DetectedValue}";

    private static string BuildExactDifference(
        ReimportChangeKind kind,
        WorkspaceEntrySnapshot? previous,
        WorkspaceEntrySnapshot? detected)
    {
        if (kind == ReimportChangeKind.NewFromSource && detected is not null)
        {
            return $"Neues Signal: {DescribeSourceFields(detected)}; " +
                   $"Erkannt als: {DescribeLocation(detected)}";
        }

        if (kind == ReimportChangeKind.RemovedFromSource && previous is not null)
        {
            return $"Fehlt vollständig in der neuen Quelle. Bisher: " +
                   $"{DescribeSourceFields(previous)}; {DescribeLocation(previous)}";
        }

        if (previous is null || detected is null)
            return "Kein feldgenauer Vergleich verfügbar.";

        var changes = new List<string>();
        if (kind == ReimportChangeKind.SourceChanged)
        {
            AddFieldChange(changes, "ID", previous.Id, detected.Id);
            AddFieldChange(changes, "Adresse", previous.Address, detected.Address);
            AddFieldChange(changes, "Signalname", previous.Signal, detected.Signal);
            AddFieldChange(changes, "Datentyp", previous.DataType, detected.DataType);
        }
        else
        {
            AddFieldChange(changes, "Container", previous.ContainerName, detected.ContainerName);
            AddFieldChange(changes, "Containertyp", previous.ComponentType, detected.ComponentType);
            AddFieldChange(changes, "Slot", previous.Slot, detected.Slot);

            var previousLocation = DescribeLocation(previous);
            var detectedLocation = DescribeLocation(detected);
            AddFieldChange(changes, "Zuordnung", previousLocation, detectedLocation);
        }

        return changes.Count == 0
            ? "Die Zuordnung wurde anhand eines anderen Erkennungskriteriums bewertet; die sichtbaren Felder sind gleich."
            : string.Join(" | ", changes);
    }

    private static string GetDecisionEffect(
        ReimportChangeKind kind,
        bool isAccepted) =>
        (kind, isAccepted) switch
        {
            (ReimportChangeKind.NewFromSource, true) =>
                "Neues Signal in den Arbeitsstand übernehmen",
            (ReimportChangeKind.NewFromSource, false) =>
                "Neues Signal nicht übernehmen",
            (ReimportChangeKind.SourceChanged, true) =>
                "Neu eingelesene Quelldaten verwenden",
            (ReimportChangeKind.SourceChanged, false) =>
                "Bisherige Quelldaten beibehalten",
            (ReimportChangeKind.RuleSuggestionChanged, true) =>
                "Neue Container-/Slot-Erkennung verwenden",
            (ReimportChangeKind.RuleSuggestionChanged, false) =>
                "Bisherige Container-/Slot-Zuordnung beibehalten",
            (ReimportChangeKind.NewlyRecognized, true) =>
                "Neu erkannte Zuordnung übernehmen",
            (ReimportChangeKind.NewlyRecognized, false) =>
                "Signal in der bisherigen offenen Liste belassen",
            (ReimportChangeKind.RemovedFromSource, true) =>
                "Signal aus dem Arbeitsstand entfernen",
            (ReimportChangeKind.RemovedFromSource, false) =>
                "Signal trotz fehlender Quelle beibehalten",
            _ => string.Empty
        };

    private static void AddFieldChange(
        ICollection<string> changes,
        string field,
        string previous,
        string detected)
    {
        if (string.Equals(previous, detected, StringComparison.Ordinal))
            return;

        changes.Add(
            $"{field}: „{FormatValue(previous)}“ → „{FormatValue(detected)}“");
    }

    private static string DescribeSourceFields(WorkspaceEntrySnapshot entry) =>
        $"ID={FormatValue(entry.Id)}, Adresse={FormatValue(entry.Address)}, " +
        $"Signal={FormatValue(entry.Signal)}, Datentyp={FormatValue(entry.DataType)}";

    private static string DescribeLocation(WorkspaceEntrySnapshot entry) =>
        entry.Location switch
        {
            WorkspaceEntryLocation.Container =>
                $"Container {FormatValue(entry.ContainerName)} / " +
                $"{FormatValue(entry.ComponentType)} / Slot {FormatValue(entry.Slot)}",
            WorkspaceEntryLocation.Filtered => "Gefilterte Signale",
            _ => "Nicht zugeordnete Signale"
        };

    private static string FormatValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "leer" : value.Trim();
}

public sealed record ReimportSummary(
    int PreservedAssignments,
    int NewlyRecognized,
    int NewEntries,
    int NeedsReview,
    int RemovedEntries)
{
    public IReadOnlyList<string> RemovedEntryDescriptions { get; init; } = Array.Empty<string>();
    public IReadOnlyList<ReimportDifference> Differences { get; init; } =
        Array.Empty<ReimportDifference>();

    public string ToStatusText()
    {
        var text =
            $"Reimport abgeschlossen: {PreservedAssignments} Zuordnungen übernommen, " +
            $"{NewlyRecognized} neu erkannt, {NewEntries} neue Signale, " +
            $"{NeedsReview} zu prüfen, {RemovedEntries} nicht mehr in der Quelle.";

        if (RemovedEntryDescriptions.Count > 0)
            text += $" Entfernt: {string.Join(", ", RemovedEntryDescriptions.Take(5))}" +
                    (RemovedEntryDescriptions.Count > 5 ? " …" : string.Empty);

        return text;
    }

    public string ToComparisonText(int maximumDetails = 12)
    {
        var lines = new List<string>
        {
            "Vergleich zwischen bestehendem Modell und neuer Erkennung:",
            string.Empty,
            $"{PreservedAssignments} bestehende Zuordnungen werden beibehalten.",
            $"{NewlyRecognized} zuvor offene Signale wurden neu erkannt.",
            $"{NewEntries} Signale sind neu in der Quelle.",
            $"{RemovedEntries} bisherige Signale fehlen in der neuen Quelle.",
            $"{NeedsReview} Einträge müssen geprüft werden."
        };

        if (Differences.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("Änderungen und neue Vorschläge:");
            lines.AddRange(Differences
                .Take(Math.Max(0, maximumDetails))
                .Select(difference => "• " + difference.ToDisplayText()));

            if (Differences.Count > maximumDetails)
                lines.Add($"• … {Differences.Count - maximumDetails} weitere Änderungen");
        }
        else
        {
            lines.Add(string.Empty);
            lines.Add("Es wurden keine fachlichen Unterschiede erkannt.");
        }

        lines.Add(string.Empty);
        lines.Add(
            "Beim Übernehmen bleiben bestehende manuelle Zuordnungen erhalten; " +
            "abweichende neue Vorschläge werden als „Prüfen“ gekennzeichnet.");
        return string.Join(Environment.NewLine, lines);
    }
}

/// <summary>
/// Captures an edited generation workspace and overlays its confirmed work on
/// a freshly generated result. The new import remains authoritative for source
/// fields; previous assignments are preserved where a stable signal match exists.
/// </summary>
public static class GenerationWorkspaceReconciler
{
    public static GenerationWorkspaceSnapshot Capture(
        IEnumerable<ContainerData> containers,
        IEnumerable<ContainerEntry> unassigned,
        IEnumerable<ContainerEntry> filtered)
    {
        var entries = new List<WorkspaceEntrySnapshot>();

        foreach (var container in containers)
        {
            entries.AddRange(container.DataList.Select(entry =>
                CreateSnapshot(entry, WorkspaceEntryLocation.Container, container)));
        }

        entries.AddRange(unassigned.Select(entry =>
            CreateSnapshot(entry, WorkspaceEntryLocation.Unassigned, null)));
        entries.AddRange(filtered.Select(entry =>
            CreateSnapshot(entry, WorkspaceEntryLocation.Filtered, null)));

        return new GenerationWorkspaceSnapshot(
            entries
                .GroupBy(entry => entry.PrimaryKey, StringComparer.Ordinal)
                .Select(group => group
                    .OrderByDescending(entry => entry.WasManuallyEdited)
                    .ThenByDescending(entry =>
                        entry.ReviewState ==
                        ContainerEntryReviewState.ManuallyEdited)
                    .ThenBy(entry => entry.Location)
                    .First())
                .ToList());
    }

    public static ReimportSummary Reconcile(
        GenerationWorkspaceSnapshot snapshot,
        IList<ContainerData> containers,
        IList<ContainerEntry> unassigned,
        IList<ContainerEntry> filtered,
        IRequirementsXml requirements)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(requirements);

        var matcher = new SnapshotMatcher(snapshot.Entries);
        var currentEntries = EnumerateCurrentEntries(containers, unassigned, filtered).ToList();
        var preserved = 0;
        var recognized = 0;
        var added = 0;
        var needsReview = 0;
        var differences = new List<ReimportDifference>();

        foreach (var current in currentEntries)
        {
            var detected = CreateSnapshot(
                current.Entry,
                current.Location,
                current.Container);
            var previous = matcher.Find(current.Entry);
            if (previous is null)
            {
                var newDifference = new ReimportDifference(
                    ReimportChangeKind.NewFromSource,
                    "Neu in Quelle",
                    DescribeSignal(current.Entry),
                    "nicht vorhanden",
                    DescribeCurrentAssignment(current),
                    true,
                    detectedEntry: detected,
                    targetEntry: current.Entry);
                differences.Add(newDifference);
                Mark(
                    current.Entry,
                    ContainerEntryReviewState.NewFromSource,
                    newDifference.ExactDifference);
                added++;
                continue;
            }

            // The freshly generated entry receives the persistent workspace
            // identity of the matching signal before it is moved or edited.
            current.Entry.SignalId = previous.SignalId;
            detected = detected with { SignalId = previous.SignalId };

            var sourceChanged = SourceFieldsChanged(previous, current.Entry);

            ReimportDifference? sourceDifference = null;
            if (sourceChanged)
            {
                sourceDifference = new ReimportDifference(
                    ReimportChangeKind.SourceChanged,
                    "Quelldaten geändert",
                    DescribeSignal(current.Entry),
                    DescribeSource(previous),
                    DescribeSource(current.Entry),
                    false,
                    previous,
                    detected,
                    current.Entry);
                differences.Add(sourceDifference);
            }

            if (previous.Location == WorkspaceEntryLocation.Container)
            {
                var assignmentChanged = !AssignmentMatches(previous, current);
                if (assignmentChanged)
                {
                    differences.Add(new ReimportDifference(
                        ReimportChangeKind.RuleSuggestionChanged,
                        "Regelvorschlag geändert",
                        DescribeSignal(current.Entry),
                        DescribePreviousAssignment(previous),
                        DescribeCurrentAssignment(current),
                        false,
                        previous,
                        detected,
                        current.Entry));
                }

                var target = FindOrCreateContainer(containers, previous);
                MoveToContainer(
                    current.Entry,
                    target,
                    containers,
                    unassigned,
                    filtered);

                current.Entry.Slot = previous.Slot;
                if (!string.IsNullOrWhiteSpace(previous.Note))
                    current.Entry.Note = previous.Note;

                current.Entry.IsManuallyEdited = previous.WasManuallyEdited;

                var validSlots = requirements.GetSlotNames(target.Type);
                var slotNoLongerValid =
                    !string.IsNullOrWhiteSpace(previous.Slot) &&
                    !validSlots.Contains(previous.Slot, StringComparer.OrdinalIgnoreCase);

                if (sourceDifference is not null)
                {
                    var additionalReason = slotNoLongerValid
                        ? " Der bisherige Slot ist in der aktuellen Requirements-Datei nicht mehr vorhanden."
                        : assignmentChanged
                            ? " Die neue Regelerkennung schlägt zusätzlich eine andere Zuordnung vor."
                            : string.Empty;
                    Mark(
                        current.Entry,
                        ContainerEntryReviewState.SourceChanged,
                        $"Quelle geändert: {sourceDifference.ExactDifference}.{additionalReason}");
                    needsReview++;
                }
                else if (previous.ReviewState == ContainerEntryReviewState.NeedsReview)
                {
                    Mark(
                        current.Entry,
                        ContainerEntryReviewState.NeedsReview,
                        string.IsNullOrWhiteSpace(previous.ReviewMessage)
                            ? "Diese Zuordnung war bereits als zu prüfen gekennzeichnet."
                            : previous.ReviewMessage);
                    needsReview++;
                }
                else if (slotNoLongerValid || assignmentChanged)
                {
                    var reason = slotNoLongerValid
                        ? "Der bisherige Slot ist in der aktuellen Requirements-Datei nicht mehr vorhanden."
                        : "Die neue Regelerkennung schlägt eine andere Zuordnung vor; die bisherige Zuordnung wurde beibehalten.";

                    Mark(current.Entry, ContainerEntryReviewState.NeedsReview, reason);
                    needsReview++;
                }
                else
                {
                    Mark(
                        current.Entry,
                        ContainerEntryReviewState.Preserved,
                        "Vorherige Container- und Slotzuordnung wurde übernommen.");
                    preserved++;
                }

                continue;
            }

            if (previous.WasManuallyEdited)
            {
                MoveToList(
                    current.Entry,
                    previous.Location,
                    containers,
                    unassigned,
                    filtered);
                current.Entry.Slot = previous.Slot;
                current.Entry.IsManuallyEdited = true;
                if (sourceDifference is not null)
                {
                    Mark(
                        current.Entry,
                        ContainerEntryReviewState.SourceChanged,
                        $"Quelle geändert: {sourceDifference.ExactDifference}. " +
                        "Die vorherige manuelle Einordnung wurde beibehalten.");
                    needsReview++;
                }
                else
                {
                    Mark(
                        current.Entry,
                        ContainerEntryReviewState.Preserved,
                        "Die vorherige manuelle Einordnung wurde übernommen.");
                    preserved++;
                }
                continue;
            }

            if (sourceDifference is not null)
            {
                Mark(
                    current.Entry,
                    ContainerEntryReviewState.SourceChanged,
                    $"Quelle geändert: {sourceDifference.ExactDifference}. " +
                    (current.Location == WorkspaceEntryLocation.Container
                        ? "Das geänderte Signal wurde neu erkannt; die Zuordnung muss geprüft werden."
                        : "Das Signal ist weiterhin nicht eindeutig zugeordnet."));
                needsReview++;
            }
            else if (previous.ReviewState == ContainerEntryReviewState.NeedsReview)
            {
                Mark(
                    current.Entry,
                    ContainerEntryReviewState.NeedsReview,
                    string.IsNullOrWhiteSpace(previous.ReviewMessage)
                        ? "Dieses Signal war bereits als zu prüfen gekennzeichnet."
                        : previous.ReviewMessage);
                needsReview++;
            }
            else if (current.Location == WorkspaceEntryLocation.Container)
            {
                differences.Add(new ReimportDifference(
                    ReimportChangeKind.NewlyRecognized,
                    "Neu erkannt",
                    DescribeSignal(current.Entry),
                    DescribePreviousAssignment(previous),
                    DescribeCurrentAssignment(current),
                    true,
                    previous,
                    detected,
                    current.Entry));
                Mark(
                    current.Entry,
                    ContainerEntryReviewState.NewlyRecognized,
                    "Das Regelwerk konnte dieses zuvor offene Signal jetzt zuordnen.");
                recognized++;
            }
        }

        for (var index = containers.Count - 1; index >= 0; index--)
        {
            if (containers[index].DataList.Count == 0)
                containers.RemoveAt(index);
        }

        foreach (var container in containers)
        {
            container.Validate();
            container.RefreshReimportStatus();
        }

        var removedEntries = matcher.UnmatchedEntries;
        foreach (var removedEntry in removedEntries)
        {
            // Never silently discard entries that are absent from a new source.
            // The modeller decides about every removal in the comparison view.
            var restoredEntry = RestoreSnapshot(
                removedEntry,
                containers,
                unassigned,
                filtered);
            Mark(
                restoredEntry,
                ContainerEntryReviewState.NeedsReview,
                removedEntry.WasManuallyEdited
                    ? "Manuell angelegtes Signal fehlt in der neuen Quelle und wurde beibehalten."
                    : "Signal fehlt in der neuen Quelle. Entfernung bitte einzeln bestätigen.");

            differences.Add(new ReimportDifference(
                ReimportChangeKind.RemovedFromSource,
                "Nicht mehr in Quelle",
                DescribeSignal(removedEntry),
                DescribePreviousAssignment(removedEntry),
                "nicht vorhanden",
                false,
                previousEntry: removedEntry,
                targetEntry: restoredEntry));
        }

        return new ReimportSummary(
            preserved,
            recognized,
            added,
            needsReview,
            matcher.UnmatchedCount)
        {
            RemovedEntryDescriptions = removedEntries
                .Select(entry =>
                    !string.IsNullOrWhiteSpace(entry.Id)
                        ? entry.Id
                        : !string.IsNullOrWhiteSpace(entry.Address)
                            ? entry.Address
                            : entry.Signal)
                .ToList(),
            Differences = differences
        };
    }

    public static void ApplyDecisions(
        ReimportSummary summary,
        IList<ContainerData> containers,
        IList<ContainerEntry> unassigned,
        IList<ContainerEntry> filtered)
    {
        ArgumentNullException.ThrowIfNull(summary);

        foreach (var difference in summary.Differences)
        {
            var target = difference.TargetEntry;

            switch (difference.Kind)
            {
                case ReimportChangeKind.NewFromSource:
                    if (!difference.IsAccepted && target is not null)
                        RemoveFromWorkspace(target, containers, unassigned, filtered);
                    break;

                case ReimportChangeKind.SourceChanged:
                    if (!difference.IsAccepted &&
                        target is not null &&
                        difference.PreviousEntry is not null)
                    {
                        RestoreSourceFields(target, difference.PreviousEntry);
                    }
                    break;

                case ReimportChangeKind.RuleSuggestionChanged:
                    if (difference.IsAccepted &&
                        target is not null &&
                        difference.DetectedEntry is not null)
                    {
                        MoveToSnapshotLocation(
                            target,
                            difference.DetectedEntry,
                            containers,
                            unassigned,
                            filtered);
                    }
                    break;

                case ReimportChangeKind.NewlyRecognized:
                    if (!difference.IsAccepted &&
                        target is not null &&
                        difference.PreviousEntry is not null)
                    {
                        MoveToSnapshotLocation(
                            target,
                            difference.PreviousEntry,
                            containers,
                            unassigned,
                            filtered);
                    }
                    break;

                case ReimportChangeKind.RemovedFromSource:
                    if (difference.IsAccepted && target is not null)
                        RemoveFromWorkspace(target, containers, unassigned, filtered);
                    break;
            }

        }

        // One signal can have several simultaneous differences (for example
        // address and rule assignment). Apply one combined, persistent review
        // state only after all decisions have been executed so no later
        // difference overwrites the exact field-level explanation.
        foreach (var differenceGroup in summary.Differences
                     .Where(difference => difference.TargetEntry is not null)
                     .GroupBy(difference => difference.TargetEntry!))
        {
            var target = differenceGroup.Key;
            var differences = differenceGroup.ToList();
            if (!ContainsEntry(target, containers, unassigned, filtered))
                continue;

            Mark(
                target,
                DetermineReviewStateAfterDecision(differences),
                string.Join(
                    Environment.NewLine,
                    differences
                        .Select(difference =>
                            $"{difference.Category}: {difference.ExactDifference}. " +
                            $"Entscheidung: {difference.DecisionEffect}.")
                        .Distinct(StringComparer.Ordinal)));

            var owner = containers.FirstOrDefault(
                container => container.DataList.Contains(target));
            if (owner is not null)
                owner.ManuallyChecked = false;
        }

        for (var index = containers.Count - 1; index >= 0; index--)
        {
            if (containers[index].DataList.Count == 0)
                containers.RemoveAt(index);
            else
            {
                containers[index].Validate();
                containers[index].RefreshReimportStatus();
            }
        }
    }

    private static ContainerEntryReviewState DetermineReviewStateAfterDecision(
        IReadOnlyCollection<ReimportDifference> differences)
    {
        if (differences.Any(difference =>
                difference.Kind == ReimportChangeKind.NewFromSource &&
                difference.IsAccepted))
        {
            return ContainerEntryReviewState.NewFromSource;
        }

        if (differences.Any(difference =>
                difference.Kind == ReimportChangeKind.SourceChanged))
        {
            return ContainerEntryReviewState.SourceChanged;
        }

        if (differences.Any(difference =>
                difference.Kind == ReimportChangeKind.NewlyRecognized &&
                difference.IsAccepted))
        {
            return ContainerEntryReviewState.NewlyRecognized;
        }

        if (differences.Any(difference =>
                difference.Kind is ReimportChangeKind.RuleSuggestionChanged or
                    ReimportChangeKind.RemovedFromSource ||
                (difference.Kind == ReimportChangeKind.NewlyRecognized &&
                 !difference.IsAccepted)))
        {
            return ContainerEntryReviewState.NeedsReview;
        }

        return ContainerEntryReviewState.Preserved;
    }

    public static string CreatePrimaryKey(ContainerEntry entry)
    {
        var id = Normalize(entry.ID);
        var address = Normalize(entry.Address);

        if (id.Length > 0 || address.Length > 0)
            return $"id:{id}|address:{address}";

        return $"signal:{Normalize(entry.Signal)}|type:{Normalize(entry.DataType)}";
    }

    public static string CreateSourceFingerprint(ContainerEntry entry) =>
        string.Join(
            "|",
            Normalize(entry.ID),
            Normalize(entry.Address),
            Normalize(entry.DataType),
            Normalize(entry.Signal));

    private static WorkspaceEntrySnapshot CreateSnapshot(
        ContainerEntry entry,
        WorkspaceEntryLocation location,
        ContainerData? container)
    {
        return new(
            CreatePrimaryKey(entry),
            entry.EnsureSignalId(),
            entry.ID,
            entry.Address,
            entry.Signal,
            entry.DataType,
            CreateSourceFingerprint(entry),
            location,
            container?.Id ?? string.Empty,
            container?.Component ?? string.Empty,
            container?.Type ?? string.Empty,
            entry.Slot,
            entry.Note,
            entry.IsManuallyEdited,
            entry.ReviewState,
            entry.ReviewMessage);
    }

    private static IEnumerable<CurrentEntry> EnumerateCurrentEntries(
        IEnumerable<ContainerData> containers,
        IEnumerable<ContainerEntry> unassigned,
        IEnumerable<ContainerEntry> filtered)
    {
        foreach (var container in containers)
        {
            foreach (var entry in container.DataList)
                yield return new CurrentEntry(entry, WorkspaceEntryLocation.Container, container);
        }

        foreach (var entry in unassigned)
            yield return new CurrentEntry(entry, WorkspaceEntryLocation.Unassigned, null);

        foreach (var entry in filtered)
            yield return new CurrentEntry(entry, WorkspaceEntryLocation.Filtered, null);
    }

    private static ContainerData FindOrCreateContainer(
        IList<ContainerData> containers,
        WorkspaceEntrySnapshot previous)
    {
        var container = containers.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, previous.ContainerId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(candidate.Component, previous.ContainerName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(candidate.Type, previous.ComponentType, StringComparison.OrdinalIgnoreCase));

        if (container is not null)
            return container;

        container = new ContainerData
        {
            Id = previous.ContainerId,
            Component = previous.ContainerName,
            Type = previous.ComponentType
        };
        containers.Add(container);
        return container;
    }

    private static ContainerEntry RestoreSnapshot(
        WorkspaceEntrySnapshot snapshot,
        IList<ContainerData> containers,
        IList<ContainerEntry> unassigned,
        IList<ContainerEntry> filtered)
    {
        var entry = new ContainerEntry
        {
            SignalId = snapshot.SignalId,
            ID = snapshot.Id,
            Address = snapshot.Address,
            Signal = snapshot.Signal,
            DataType = snapshot.DataType,
            Slot = snapshot.Slot,
            Note = snapshot.Note,
            IsManuallyEdited = snapshot.WasManuallyEdited,
            ReviewState = snapshot.ReviewState,
            ReviewMessage = snapshot.ReviewMessage
        };

        MoveToSnapshotLocation(entry, snapshot, containers, unassigned, filtered);
        return entry;
    }

    private static void RestoreSourceFields(
        ContainerEntry entry,
        WorkspaceEntrySnapshot snapshot)
    {
        entry.ID = snapshot.Id;
        entry.Address = snapshot.Address;
        entry.Signal = snapshot.Signal;
        entry.DataType = snapshot.DataType;
        entry.Note = snapshot.Note;
    }

    private static void MoveToSnapshotLocation(
        ContainerEntry entry,
        WorkspaceEntrySnapshot snapshot,
        IList<ContainerData> containers,
        IList<ContainerEntry> unassigned,
        IList<ContainerEntry> filtered)
    {
        entry.Slot = snapshot.Slot;
        if (snapshot.Location == WorkspaceEntryLocation.Container)
        {
            var target = FindOrCreateContainer(containers, snapshot);
            MoveToContainer(entry, target, containers, unassigned, filtered);
            return;
        }

        MoveToList(
            entry,
            snapshot.Location,
            containers,
            unassigned,
            filtered);
    }

    private static void RemoveFromWorkspace(
        ContainerEntry entry,
        IEnumerable<ContainerData> containers,
        IList<ContainerEntry> unassigned,
        IList<ContainerEntry> filtered)
    {
        foreach (var container in containers)
            container.DataList.Remove(entry);

        unassigned.Remove(entry);
        filtered.Remove(entry);
    }

    private static bool ContainsEntry(
        ContainerEntry entry,
        IEnumerable<ContainerData> containers,
        IEnumerable<ContainerEntry> unassigned,
        IEnumerable<ContainerEntry> filtered) =>
        containers.Any(container => container.DataList.Contains(entry)) ||
        unassigned.Contains(entry) ||
        filtered.Contains(entry);

    private static void MoveToContainer(
        ContainerEntry entry,
        ContainerData target,
        IEnumerable<ContainerData> containers,
        IList<ContainerEntry> unassigned,
        IList<ContainerEntry> filtered)
    {
        foreach (var container in containers)
        {
            var duplicates = container.DataList
                .Where(candidate =>
                    GenerationWorkspaceEditor.SameSource(candidate, entry))
                .ToList();
            foreach (var duplicate in duplicates)
                container.DataList.Remove(duplicate);
        }

        GenerationWorkspaceEditor.RemoveAllMatches(unassigned, entry);
        GenerationWorkspaceEditor.RemoveAllMatches(filtered, entry);

        target.DataList.Add(entry);
    }

    private static void MoveToList(
        ContainerEntry entry,
        WorkspaceEntryLocation targetLocation,
        IEnumerable<ContainerData> containers,
        IList<ContainerEntry> unassigned,
        IList<ContainerEntry> filtered)
    {
        foreach (var container in containers)
        {
            var matches = container.DataList
                .Where(candidate =>
                    GenerationWorkspaceEditor.SameSource(candidate, entry))
                .ToList();
            foreach (var match in matches)
                container.DataList.Remove(match);
        }

        GenerationWorkspaceEditor.RemoveAllMatches(unassigned, entry);
        GenerationWorkspaceEditor.RemoveAllMatches(filtered, entry);

        var target = targetLocation == WorkspaceEntryLocation.Filtered ? filtered : unassigned;
        target.Add(entry);
    }

    private static bool AssignmentMatches(
        WorkspaceEntrySnapshot previous,
        CurrentEntry current) =>
        current.Location == WorkspaceEntryLocation.Container &&
        current.Container is not null &&
        string.Equals(
            previous.ContainerName,
            current.Container.Component,
            StringComparison.OrdinalIgnoreCase) &&
        string.Equals(
            previous.ComponentType,
            current.Container.Type,
            StringComparison.OrdinalIgnoreCase) &&
        string.Equals(
            previous.Slot,
            current.Entry.Slot,
            StringComparison.OrdinalIgnoreCase);

    private static bool SourceFieldsChanged(
        WorkspaceEntrySnapshot previous,
        ContainerEntry current) =>
        !string.Equals(previous.Id, current.ID, StringComparison.Ordinal) ||
        !string.Equals(previous.Address, current.Address, StringComparison.Ordinal) ||
        !string.Equals(previous.Signal, current.Signal, StringComparison.Ordinal) ||
        !string.Equals(previous.DataType, current.DataType, StringComparison.Ordinal);

    private static string DescribePreviousAssignment(WorkspaceEntrySnapshot entry) =>
        entry.Location switch
        {
            WorkspaceEntryLocation.Container =>
                $"{entry.ContainerName} / {entry.ComponentType} / {entry.Slot}",
            WorkspaceEntryLocation.Filtered => "gefiltert",
            _ => "nicht zugeordnet"
        };

    private static string DescribeCurrentAssignment(CurrentEntry entry) =>
        entry.Location switch
        {
            WorkspaceEntryLocation.Container when entry.Container is not null =>
                $"{entry.Container.Component} / {entry.Container.Type} / {entry.Entry.Slot}",
            WorkspaceEntryLocation.Filtered => "gefiltert",
            _ => "nicht zugeordnet"
        };

    private static string DescribeSource(WorkspaceEntrySnapshot entry) =>
        $"ID={entry.Id}, Adresse={entry.Address}, Signal={entry.Signal}, Typ={entry.DataType}";

    private static string DescribeSource(ContainerEntry entry) =>
        $"ID={entry.ID}, Adresse={entry.Address}, Signal={entry.Signal}, Typ={entry.DataType}";

    private static string DescribeSignal(ContainerEntry entry) =>
        !string.IsNullOrWhiteSpace(entry.Signal)
            ? entry.Signal
            : !string.IsNullOrWhiteSpace(entry.ID)
                ? entry.ID
                : entry.Address;

    private static string DescribeSignal(WorkspaceEntrySnapshot entry) =>
        !string.IsNullOrWhiteSpace(entry.Signal)
            ? entry.Signal
            : !string.IsNullOrWhiteSpace(entry.Id)
                ? entry.Id
                : entry.Address;

    private static void Mark(
        ContainerEntry entry,
        ContainerEntryReviewState state,
        string message)
    {
        entry.ReviewState = state;
        entry.ReviewMessage = message;
    }

    private static string Normalize(string? value) =>
        (value ?? string.Empty).Trim().ToUpperInvariant();

    private sealed record CurrentEntry(
        ContainerEntry Entry,
        WorkspaceEntryLocation Location,
        ContainerData? Container);

    private sealed class SnapshotMatcher
    {
        private readonly IReadOnlyList<WorkspaceEntrySnapshot> _entries;
        private readonly HashSet<int> _used = new();

        public SnapshotMatcher(IReadOnlyList<WorkspaceEntrySnapshot> entries)
        {
            _entries = entries;
        }

        public int UnmatchedCount => _entries.Count - _used.Count;
        public IReadOnlyList<WorkspaceEntrySnapshot> UnmatchedEntries =>
            Enumerable.Range(0, _entries.Count)
                .Where(index => !_used.Contains(index))
                .Select(index => _entries[index])
                .ToList();

        public WorkspaceEntrySnapshot? Find(ContainerEntry entry)
        {
            if (!string.IsNullOrWhiteSpace(entry.SignalId))
            {
                var signalIdMatch = FindUnique(candidate =>
                    string.Equals(
                        candidate.SignalId,
                        entry.SignalId,
                        StringComparison.Ordinal));
                if (signalIdMatch is not null)
                    return signalIdMatch;
            }

            var primary = CreatePrimaryKey(entry);
            var match = FindFirst(candidate =>
                string.Equals(candidate.PrimaryKey, primary, StringComparison.Ordinal));

            if (match is not null)
                return match;

            if (!string.IsNullOrWhiteSpace(entry.ID))
            {
                match = FindUnique(candidate =>
                    string.Equals(candidate.Id, entry.ID, StringComparison.OrdinalIgnoreCase));
                if (match is not null)
                    return match;
            }

            if (!string.IsNullOrWhiteSpace(entry.Address))
            {
                match = FindUnique(candidate =>
                    string.Equals(candidate.Address, entry.Address, StringComparison.OrdinalIgnoreCase));
                if (match is not null)
                    return match;
            }

            return FindUnique(candidate =>
                string.Equals(candidate.Signal, entry.Signal, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(candidate.DataType, entry.DataType, StringComparison.OrdinalIgnoreCase));
        }

        private WorkspaceEntrySnapshot? FindFirst(Func<WorkspaceEntrySnapshot, bool> predicate)
        {
            for (var index = 0; index < _entries.Count; index++)
            {
                if (_used.Contains(index) || !predicate(_entries[index]))
                    continue;

                _used.Add(index);
                return _entries[index];
            }

            return null;
        }

        private WorkspaceEntrySnapshot? FindUnique(Func<WorkspaceEntrySnapshot, bool> predicate)
        {
            var matches = Enumerable.Range(0, _entries.Count)
                .Where(index => !_used.Contains(index) && predicate(_entries[index]))
                .Take(2)
                .ToList();

            if (matches.Count != 1)
                return null;

            _used.Add(matches[0]);
            return _entries[matches[0]];
        }
    }
}
