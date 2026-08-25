# Kanbanize Karten

## Zweck und Umfang

Der Reiter **Kanbanize Karten** ist ein eigenständiger Arbeitsablauf zum Erstellen einzelner Karten. Er wurde anhand des früheren `Canbanize-Tool`-Prinzips neu aufgebaut, verwendet aber keine Lizenzdateien, keine Lizenzanfragen und keine Sammelerstellung/-löschung.

## Bedienablauf

1. **Boards aktualisieren** lädt die Boards, für die der API-Schlüssel Zugriff hat.
2. Board auswählen. Lanes und Spalten werden parallel geladen.
3. Lane auswählen. Nur Spalten desselben Workflows bleiben auswählbar.
4. Titel eingeben. Externe ID, Beschreibung und Deadline sind optional.
5. Priorität 1–4 wählen und **Karte erstellen** drücken.

Die Auswahl bleibt nach einer erfolgreichen Erstellung erhalten, Titel, externe ID und Beschreibung werden geleert. So lassen sich mehrere Karten am selben Ziel anlegen, ohne dass ein zweiter Klick versehentlich dieselbe Karte erstellt.

## Daten und Berechtigungen

| Feld | Verhalten |
|---|---|
| Board / Lane / Spalte | Pflicht; IDs stammen ausschließlich aus der Live-API |
| Titel | Pflicht, maximal 255 Zeichen |
| Externe ID | optional; wird als `custom_id` gesendet |
| Priorität | Pflichtwert 1 (hoch) bis 4 (niedrig) |
| Deadline | optional; wird nur bei aktivierter Checkbox gesendet |
| Beschreibung | optional; wird unverändert als Kartenbeschreibung gesendet |

Der Zugang verwendet `VIBN_VICO_KANBANIZE_API_KEY`, denselben Schlüssel wie die ViCo-Online-Aktualisierung. Die API-Berechtigung des zugehörigen Kanbanize-Benutzers muss **Create Card** für das gewählte Board erlauben. Die Anwendung übermittelt nie API-Schlüssel an die Benutzeroberfläche oder das Log.

## Quellcode-Wegweiser

| Teil | Datei |
|---|---|
| Validierung und Schnittstelle | `VIBN_Tools.Core/Kanbanize/CardCreation.cs` |
| HTTP-Adapter | `VIBN_Tools.Infrastructure/Kanbanize/KanbanizeCardApiService.cs` |
| UI-Zustand und Commands | `Application/VM/KanbanizeCardPageVM.cs` |
| XAML-Oberfläche | `Application/View/KanbanizeCardPage.xaml` |
| Zusammensetzung | `Application/ViCoFeatureBootstrapper.cs` |

## Fehlerfälle

- **Boards konnten nicht geladen werden:** API-Schlüssel, Netzwerk, Konto-/Boardberechtigung und Diagnoseprotokoll prüfen.
- **Keine passende Spalte:** Lane und Spalte liegen in verschiedenen Workflows oder das Board enthält keine zugängliche Position.
- **Karte konnte nicht erstellt werden:** Statusmeldung enthält keine Schlüssel; im Diagnoseprotokoll stehen HTTP-Status und technische Ursache.
- **Karte wird doppelt angelegt:** Nach Erfolg wird der Titel geleert. Nur nach bewusster erneuter Eingabe kann erneut erstellt werden.
