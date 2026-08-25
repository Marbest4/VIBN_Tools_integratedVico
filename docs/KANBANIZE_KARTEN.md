# Kanbanize Karten

Der Hauptreiter **Kanbanize Karten** enthält zwei getrennte Funktionen: die sichere, automatisierte VIBN-Übernahme ins Arbeitsplätze-Board und die optionale manuelle Kartenerstellung. Beide verwenden den vorhandenen Kanbanize-/Businessmap-API-Zugang, aber keine Lizenzanfragen oder Lizenzdateien.

## VIBN → Arbeitsplätze

### Ziel

Der Ablauf ersetzt die alte Canbanize-Desktopautomatik. Er liest geeignete Karten aus dem Board **Virtuelle Inbetriebnahme** und erstellt die fehlende, verknüpfte Karte im **Arbeitsplätze**-Board. Die historischen Board-IDs `1392` und `1541` sowie Tool-Lane/Backlog-Spalte werden nur als dynamische Vorauswahl verwendet. Board, Lane und Spalte können vor der Prüfung sichtbar kontrolliert und bei Bedarf bewusst geändert werden.

### Bedienablauf

1. **Boards aktualisieren** wählen und warten, bis die Comboboxen gefüllt sind.
2. Quellboard, Zielboard, Ziel-Lane und Zielspalte kontrollieren.
3. Priorität für neu zu erzeugende Karten wählen. **Deadlines aus Quelle synchronisieren** bleibt normalerweise aktiviert.
4. **Prüfen** wählen. Diese Aktion führt ausschließlich Lesezugriffe aus.
5. Die Vorschautabelle prüfen.
6. Nur bei korrekter Vorschau **Synchronisieren** wählen.

| Vorschauaktion | Bedeutung |
|---|---|
| **Neu** | Für die Quellkarten-ID existiert noch keine Zielkarte; eine verknüpfte Karte wird erstellt. |
| **Deadline** | Genau eine vorhandene Zielkarte besitzt dieselbe Quellkarten-ID, aber eine andere Deadline; nur die Deadline wird aktualisiert. |
| **Unverändert** | Die eindeutige Zielkarte existiert bereits und benötigt keine Änderung. |
| **Konflikt** | Mehrere Zielkarten verwenden dieselbe Quellkarten-ID; es wird nichts geändert. |

### Sicherheits- und Duplikatregel

Der Idempotenzschlüssel ist die interne ID der VIBN-Quellkarte. Eine neue Zielkarte erhält diese ID als `custom_id` und als Elternverknüpfung. Ein nachfolgender Lauf liest beide Boards neu ein und erkennt die bestehende Zielkarte darüber.

Die Automatik führt nur diese zwei Schreiboperationen aus:

1. Fehlende Zielkarte erstellen.
2. Deadline einer eindeutigen, bereits verknüpften Zielkarte aktualisieren.

Sie löscht, verschiebt, archiviert, verwirft, benennt und beschreibt keine vorhandenen Karten um. Bei einer entfernten Quell-Deadline wird ausschließlich die Ziel-Deadline geleert, wenn der Deadline-Abgleich aktiviert ist. Das ist die einzige erlaubte Änderung einer vorhandenen Karte.

Geeignet sind nur Quellkarten, deren Titel `Grundinbetriebnahme` enthält. `Vorlage` und die historische Archivspalte `25236` werden ausgeschlossen. Für neue Karten wird die bekannte Titelmarkierung `[VIBN] Grundinbetriebnahme` in `*[Gen]*` überführt; bestehende Titel werden später nie angepasst.

## Eigene Karte

Die Registerkarte **Eigene Karte** ist unabhängig von der Synchronisierung.

1. Board, Lane und Spalte auswählen.
2. Titel eingeben; externe ID, Beschreibung und Deadline sind optional.
3. Priorität 1–4 wählen.
4. **Karte erstellen** wählen.

Nach Erfolg bleiben Zielposition und Priorität erhalten, Titel, externe ID und Beschreibung werden geleert. Damit kann eine weitere Karte vorbereitet werden, ohne den vorherigen Titel versehentlich erneut zu senden.

| Feld | Verhalten |
|---|---|
| Board / Lane / Spalte | Pflicht; Positionen stammen ausschließlich aus der Live-API. |
| Titel | Pflicht, maximal 255 Zeichen. |
| Externe ID | optional; wird als `custom_id` gesendet. |
| Priorität | 1 (hoch) bis 4 (niedrig). |
| Deadline | optional; wird nur bei aktivierter Checkbox gesendet. |
| Beschreibung | optional; wird als Kartenbeschreibung gesendet. |

## Berechtigungen und Fehlerfälle

`VIBN_VICO_KANBANIZE_API_KEY` ist der bevorzugte API-Schlüssel. Für die VIBN-Synchronisierung benötigt der zugehörige Kanbanize-Benutzer Lesezugriff auf Quell- und Zielboard, **Create Card** für das Ziel sowie die Berechtigung, Kartendetails/Deadlines zu aktualisieren. Der Schlüssel selbst wird nie in der Oberfläche oder im Diagnoseprotokoll angezeigt.

- **Boards/Zielpositionen konnten nicht geladen werden:** API-Schlüssel, Netzwerk und Boardzugriff prüfen.
- **Prüfung konnte nicht ausgeführt werden:** Lesezugriff auf beide Boards oder API-Antwort im Diagnoseprotokoll prüfen.
- **Synchronisierung enthält Konflikte:** doppelte Zielkarten bewusst im Board bereinigen; die Anwendung ändert sie absichtlich nicht automatisch.
- **Deadline konnte nicht aktualisiert werden:** Update-Berechtigung prüfen. Andere Kartenfelder bleiben trotzdem unverändert.
- **Karte konnte nicht erstellt werden:** Zielposition, Create-Card-Berechtigung und die technische Detailmeldung im Diagnoseprotokoll prüfen.

## Quellcode-Wegweiser

| Teil | Datei |
|---|---|
| Kartenmodelle und HTTP-Vertrag | `VIBN_Tools.Core/Kanbanize/CardCreation.cs` |
| Fachregel, Vorschau und idempotente Synchronisierung | `VIBN_Tools.Core/Kanbanize/VibnWorkplaceSynchronization.cs` |
| Kanbanize-v2-Adapter | `VIBN_Tools.Infrastructure/Kanbanize/KanbanizeCardApiService.cs` |
| UI-Zustand der Synchronisierung | `Application/VM/VibnWorkplaceSynchronizationVM.cs` |
| optionale manuelle Kartenerstellung | `Application/VM/KanbanizeCardPageVM.cs` |
| XAML-Oberfläche | `Application/View/KanbanizeCardPage.xaml` |
| Zusammensetzung/Abhängigkeiten | `Application/ViCoFeatureBootstrapper.cs` |
| automatisierte Fach- und HTTP-Nutzlasttests | `Tests/CoreSmokeTests/Program.cs`, `VerifyVibnWorkplaceSynchronizationAsync` und `VerifyKanbanizeHttpWriteScopeAsync` |
