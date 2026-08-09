using System.Collections.Generic;
using VIBN_Tools.ContainerGeneration.BusinessLogic.ContainerData;
using VIBN_Tools.ContainerGeneration.Models;

namespace VIBN_Tools.ContainerGeneration.AI;

/// <summary>
/// Beobachtet einen Container auf Drag-and-Drop- und Slot-Änderungen
/// und delegiert sie an den ActionLogger.
///
/// FIXES gegenüber Vorgänger:
///  - LogRemoved / LogAdded / LogSlotChange: neues componentType-Argument
///    wird jetzt aus container.Type befüllt.
///  - oldSlot bei SlotChanged war immer "".
///    Fix: _lastKnownSlot-Dictionary speichert den zuletzt bekannten Slot
///    pro Entry.ID. Beim Feuern von SlotChanged lesen wir den alten Wert
///    aus dem Dictionary, bevor wir ihn mit dem neuen überschreiben.
/// </summary>
public sealed class ContainerGraphObserver
{
    private readonly ActionLogger _logger;

    /// <summary>
    /// Speichert den zuletzt gesehenen Slot pro Entry-ID.
    /// So kann bei SlotChanged der „alte" Slot rekonstruiert werden,
    /// auch wenn das Event erst nach der Zuweisung feuert.
    /// </summary>
    private readonly Dictionary<string, string> _lastKnownSlot = new();

    public ContainerGraphObserver(ActionLogger logger) => _logger = logger;

    public void AttachTo(ContainerData container)
    {
        // Initialen Slot-Stand für alle vorhandenen Entries merken
        foreach (var entry in container.DataList)
            _lastKnownSlot[entry.EnsureSignalId()] = entry.Slot;

        // ── Add / Remove (Drag-and-Drop) ────────────────────────────────
        container.DataList.CollectionChanged += (s, e) =>
        {
            if (e.OldItems != null)
                foreach (ContainerEntry entry in e.OldItems)
                {
                    _logger.LogRemoved(container.Component, container.Type, entry);
                    _lastKnownSlot.Remove(entry.EnsureSignalId());
                }

            if (e.NewItems != null)
                foreach (ContainerEntry entry in e.NewItems)
                {
                    _logger.LogAdded(
                        container.Component, container.Type, entry,
                        ruleSuggestion: entry.Slot,
                        mlTop1: null, mlScore: null);
                    _lastKnownSlot[entry.EnsureSignalId()] = entry.Slot;
                }
        };

        // ── Slot-Änderungen ──────────────────────────────────────────────
        foreach (var entry in container.DataList)
            SubscribeSlotChanged(entry, container);

        container.DataList.CollectionChanged += (_, e) =>
        {
            if (e.NewItems != null)
                foreach (ContainerEntry entry in e.NewItems)
                    SubscribeSlotChanged(entry, container);

            if (e.OldItems != null)
                foreach (ContainerEntry entry in e.OldItems)
                    entry.SlotChanged -= MakeHandler(entry, container);
        };
    }

    private void SubscribeSlotChanged(ContainerEntry entry, ContainerData container)
    {
        // Initialen Slot sichern (falls noch nicht vorhanden)
        _lastKnownSlot.TryAdd(entry.EnsureSignalId(), entry.Slot);
        entry.SlotChanged += MakeHandler(entry, container);
    }

    /// <summary>
    /// Erzeugt einen EventHandler, der den alten Slot aus dem Dictionary liest,
    /// bevor er ihn mit dem neuen Wert überschreibt.
    /// WICHTIG: Das Event feuert NACH der Zuweisung (_slot ist bereits neu),
    /// daher muss der alte Wert vorher zwischengespeichert worden sein.
    /// </summary>
    private EventHandler MakeHandler(ContainerEntry entry, ContainerData container)
    {
        return Handler;

        void Handler(object? sender, EventArgs e)
        {
            var currentEntry = (ContainerEntry)sender!;
            var signalId = currentEntry.EnsureSignalId();

            // Alten Slot aus Dictionary holen (wurde beim letzten Mal gespeichert)
            var oldSlot = _lastKnownSlot.TryGetValue(signalId, out var prev)
                ? prev
                : "";

            // Neuen Slot für nächste Änderung merken
            _lastKnownSlot[signalId] = currentEntry.Slot;

            _logger.LogSlotChange(
                container.Component, container.Type, currentEntry,
                oldSlot:  oldSlot,
                mlTop1:   null,
                mlScore:  null);
        }
    }
}
