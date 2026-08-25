# Konfiguration, Betrieb und Fehlersuche

## Voraussetzungen

- Windows-Desktop mit .NET 8 Runtime;
- Grob.UX und das kompatible FEE-/fe.screen-sim-SDK;
- Zugriff auf die vorgesehenen UNC-Pfade für ViCo-Caches, Projekte, Rollen und Versionen;
- Kanbanize-/Businessmap-Zugriff für Live-Aktualisierung und Kartenfunktionen;
- für Live-TIA: passende lokale Siemens-TIA-Portal-/Openness-Installation und Berechtigung;
- optional Outlook für Termine im Verwaltungsreiter.

Ohne Unternehmensnetz startet die Oberfläche weiterhin. Live-Daten, Kartenaktionen, Netzpfade oder TIA-Operationen können dann erwartbar nicht verfügbar sein und werden protokolliert.

## Zentrale Konfigurationsstellen

| Wert | Ort | Zweck |
| --- | --- | --- |
| Kanbanize API-Schlüssel | `VIBN_VICO_KANBANIZE_API_KEY` | bevorzugter Live-Zugang für ViCo und Kartenreiter |
| Rollen-Datei | `VIBN_VICO_ROLES_FILE` | optionaler zentraler Pfad zu `roles.json` |
| ViCo-Pfade | `VIBN_Tools.Infrastructure/ViCo/ViCoPathsOptions.cs` | Caches, Projekte, Versionen und Standard-Arbeitsordner |
| TIA-Bridge | `Application/ViCoFeatureBootstrapper.cs` | Bridge-Executable, Pipe pro Prozess, lokale Versionserkennung |
| Logging | `ApplicationLogService` / vorhandene Log-Konfiguration | sichtbares Diagnosepanel und Logdatei |

API-Schlüssel und Kennwörter gehören nicht in Quellcode, Screenshots, Tickets oder das Diagnoseprotokoll.

## Datenquellen und Aktualisierung

| Information | Quelle | Aktualisierung |
| --- | --- | --- |
| PCs, Benutzer, Projekte, Software und KONFIGURATION | Arbeitsplätze-Board / strukturierter Cache | Start, Daten aktualisieren und periodisch |
| Robotik-Informationen | Robotik-Board / Cache | Start, Daten aktualisieren und periodisch |
| Project-Settings-Dropdown | gemeinsames `WorkstationDirectory` plus Ping | beim Start, Filter und manueller Aktualisierung |
| Rollen | `roles.json` | Anwendungstart und Verwaltungs-Refresh |
| Kartenpositionen/Karten | Kanbanize v2 | ausdrücklich durch Kartenreiter |
| TIA-Versionen | lokale Siemens-PublicAPI-Pfade | beim Öffnen der TIA-Ansicht |

## Häufige Fehler

### PC fehlt im Project-Settings-Dropdown

1. **Liste aktualisieren** drücken.
2. Filter löschen oder präzisieren.
3. Prüfen, ob der PC aktuell auf Ping antwortet – Offline-PCs erscheinen absichtlich nicht.
4. In ViCo **Daten aktualisieren** drücken und das Diagnoseprotokoll auf Cache-/Kanbanize-Fehler prüfen.

### Connect zeigt trotzdem nicht „verbunden“

Das ist korrekt, wenn FEE die Verbindung nicht bestätigt. Die Anwendung setzt `Connected to` erst nach `WaitForConnectedAsync`. Status-/Logmeldung prüfen, Servernamen und FEE-Service kontrollieren und danach erneut verbinden.

### RDP-Sitzung steht auf „Nicht abrufbar“

Der PC kann trotzdem online sein. Der aktuell angemeldete Benutzer darf die Remote-Terminalsitzungen nicht abfragen oder `quser` erreicht den Zielcomputer nicht. Mit einem Konto mit ausreichender administrativer Berechtigung starten oder die Remote-Abfrageberechtigung prüfen. Der RDP-Start selbst bleibt davon unabhängig.

### Remote Desktop verwendet einen falschen Benutzer

Die `USER:`-Unteraufgabe der `KONFIGURATION`-Karte hat Vorrang. In ViCo den angezeigten Remote-Benutzer prüfen, Konfiguration gegebenenfalls bearbeiten und speichern, anschließend die Daten aktualisieren. Der normale Remote-Button verwendet nur die lokale Windows-RDP-Anmeldung; der zweite Button zeigt den Windows-Anmeldedialog. Bei geänderter Zuordnung den gespeicherten Eintrag für `TERMSRV/<PC-Name>` in der Windows-Anmeldeinformationsverwaltung entfernen und einmal über den Dialog-Button mit **Anmeldedaten speichern** neu anmelden.

### Automatische Remote-Anmeldung ist noch nicht eingerichtet

Für den aktuellen Windows-Benutzer existiert noch keine gespeicherte RDP-Anmeldung für den Ziel-PC. **Remote Desktop mit Anmeldedaten** wählen, die Anmeldung eingeben und im Windows-Dialog **Anmeldedaten speichern** aktivieren. Danach verwendet der normale Button diesen lokalen Eintrag automatisch. Das Tool speichert oder verteilt kein Passwort.

### Konfigurationswerte lassen sich nicht speichern

Nur vorhandene Unteraufgaben mit einer gültigen ID sind schreibbar. Fehlt beispielsweise `PROJEKT-IP:`, zeigt die Zeile sich schreibgeschützt. Das Tool erstellt sie nicht automatisch. Zusätzlich benötigt der Kanbanize-Zugang Bearbeitungsrechte für Unteraufgaben der betreffenden Karte.

### Kanbanize-Vorschau zeigt Konflikt

Keinen Synchronisieren-Lauf erzwingen. Prüfen, ob genau eine datierte `Grundinbetriebnahme ... Vorlage`-Karte existiert, die Quellkarte eine Deadline hat und nicht mehrere Zielkarten dieselbe Quell-ID tragen. Konflikte führen bewusst zu keiner Änderung.

### TIA Bridge verbindet sich nicht oder Hardware bleibt leer

1. Passende TIA-Version installieren und Projekt öffnen.
2. TIA-Version, PLC und Openness-Berechtigung prüfen.
3. Im Diagnosepanel die Bridge-Fehler lesen.
4. Für Special Devices sowohl Eingangs- als auch Ausgangsbyte und Logik kontrollieren; fehlende Adressen müssen manuell ergänzt werden.

### XamlParseException oder Binding-Fehler

Nicht mit einem erneuten Schreibvorgang fortfahren. Status/Stacktrace sichern, den WPF-UI-Smoke-Test ausführen und die betroffene View/Property in der [Klassenreferenz](KLASSENREFERENZ.md) nachschlagen. Die integrierten Views sind auf schreibgeschützte Anzeige-Bindings abgesichert.

## Performance-Leitplanken

- Tabellen verwenden Zeilen- und Spaltenvirtualisierung.
- PC-Pings sind auf acht, RDP-Sitzungsabfragen auf vier parallele Abfragen begrenzt.
- Such- und Filtereingaben werden entprellt; ein neuer Filter bricht die vorherige Prüfung ab.
- Kanbanize-Caches werden atomar geschrieben; die UI liest stabile Snapshots.
- Dateiübertragungen und FEE-Geräteerzeugungen sind begrenzt/serialisiert.
- Keine rekursiven Netzwerkscans oder TIA-/Outlook-Aufrufe im UI-Thread ergänzen.

## Veröffentlichung

1. Release-Build ausführen.
2. Core- und UI-Smoke-Tests ausführen.
3. [ACCEPTANCE_CHECKLIST.md](ACCEPTANCE_CHECKLIST.md) auf einem echten GROB-Desktop abarbeiten.
4. Keine Cachedateien, Rollen-Dateien mit Realbenutzern, API-Schlüssel oder RDP-Credentials einchecken.
