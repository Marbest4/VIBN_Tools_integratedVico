# Entwicklerhandbuch

## Architekturziel

Die Anwendung folgt für den integrierten ViCo-Bereich einer geschichteten MVVM-Struktur. Die bestehende VIBN-Funktionalität bleibt im WPF-Host und wird nicht unnötig verändert. Neue ViCo-Fachlogik gehört nicht in Code-behind oder in eine große ViewModel-Klasse, sondern in kleine Core-Dienste mit Interfaces und austauschbare Infrastrukturadapter.

Die vollständige Landkarte aller historischen und neuen Module steht in [Gesamtübersicht der Solution](GESAMTLOESUNG.md). Dieses Dokument beschreibt die verbindlichen Erweiterungsregeln für die gesamte Lösung.

```text
WPF View
   ↓ Binding / Command
Application ViewModel
   ↓ Interface
Core: Modelle, Verträge, Fachregeln
   ↑ Implementierung
Infrastructure: Datei, Netz, Windows, Kanbanize, Outlook

TIA ViewModel → TIA Client → Named Pipe → TIA Bridge → Siemens Openness
```

## Projekte und Abhängigkeitsrichtung

| Projekt | Darf kennen | Darf nicht enthalten |
|---|---|---|
| `VIBN_Tools.Core` | .NET-Basistypen | WPF, UNC-spezifische Implementierungen, TIA-Assemblies |
| `VIBN_Tools.Infrastructure` | Core | UI-Zustand oder View-Logik |
| `VIBN_Tools.Tia.Contracts` | serialisierbare DTOs | Siemens Openness oder WPF |
| `VIBN_Tools.Tia.Client` | Contracts | WPF-Controls, konkrete ViewModels |
| `VIBN_Tools.TiaBridge` | Contracts, Siemens Openness | Hauptanwendungs-UI |
| WPF-Host `VIBN_Tools` | alle benötigten Module | neue fachliche Regeln im Code-behind |

Der zentrale Composition Root ist `Application/ViCoFeatureBootstrapper.cs`. Nur dort werden Core-Verträge mit konkreten Infrastrukturklassen verbunden. Dadurch bleiben ViewModels testbar und Datenquellen austauschbar.

## Start- und Navigationsablauf

1. `App.xaml` lädt Ressourcen und startet `MainWindow`.
2. `Application/View/MainWindow.xaml` definiert die Hauptreiter.
3. Die ViCo- und Kanbanize-Views beziehen ihre ViewModels über `ViCoFeatureBootstrapper`.
4. `InitializeWorkstationDirectoryAsync` lädt einmal den gemeinsamen PC-Bestand.
5. Project Settings und ViCo Search verwenden dieselbe `IWorkstationDirectory`-Instanz.
6. Views rufen asynchrone `InitializeAsync`-Methoden erst auf, wenn sie benötigt werden.

Der Hauptreiter **Kanbanize Karten** ist kein Unterteil von ViCo-Lizenzen. Die manuelle Kartenerstellung folgt `KanbanizeCardPage` → `KanbanizeCardPageVM` → `IKanbanizeCardService` → `KanbanizeCardApiService`. Die VIBN-Übernahme ist bewusst getrennt: `VibnWorkplaceSynchronizationVM` → `IVibnWorkplaceSynchronizationService` → `VibnWorkplaceSynchronizationService` → `IKanbanizeCardService`. Der API-Schlüssel wird nur im HTTP-Header verwendet; das Modul enthält keine Lizenz- oder Anfrageklassen.

Die Synchronisierung darf nur eine fehlende Zielkarte erstellen oder die Deadline einer eindeutig verknüpften Zielkarte patchen. Delete-, Move-, Archive-, Titel- und Beschreibungsendpunkte gehören ausdrücklich nicht in diesen Ablauf.

## Programmierregeln

- Views enthalten Layout und Binding, aber keine Fachlogik.
- ViewModels koordinieren UI-Zustand und Dienste; sie greifen nicht direkt auf UNC-Dateien oder HTTP zu.
- Core-Dienste treffen fachliche Entscheidungen und sind ohne WPF testbar.
- Infrastructure implementiert Betriebssystem-, Netzwerk- und Dateizugriffe.
- Read-only-Properties in DataGrid-Spalten müssen ausdrücklich `Mode=OneWay` binden.
- Lang laufende Arbeit ist `async`; keine Netzwerk- oder Dateisuche auf dem UI-Thread.
- Collections für große Tabellen behalten Virtualisierung und werden nicht bei jedem Tastendruck vollständig neu aufgebaut.
- Kommentare erklären das **Warum** einer nicht offensichtlichen Entscheidung. Namen, kleine Methoden und Tests dokumentieren das **Was**.
- Keine Kennwörter, Schlüssel oder Tokens in Code, Statusmeldungen oder Logs ergänzen.

## Eine Funktion erweitern

### Neue ViCo-Datenquelle

1. Interface und neutrale Modelle in `VIBN_Tools.Core/ViCo/` ergänzen.
2. Adapter in `VIBN_Tools.Infrastructure/ViCo/` implementieren.
3. Adapter ausschließlich in `ViCoFeatureBootstrapper` verdrahten.
4. ViewModel über das Interface erweitern.
5. View mit OneWay-Bindings und Commands ergänzen.
6. fachliche Randfälle in `Tests/CoreSmokeTests/Program.cs` prüfen.

### Neue ViCo-Aktion

1. Prüfen, ob die Aktion zu Suche, Projekt/Favoriten, Transfer, TIA oder Verwaltung gehört.
2. Kleine Core-Abstraktion anlegen, falls externe Zustände betroffen sind.
3. Command im passenden ViewModel anlegen; Aktivierbarkeit aus explizitem Zustand ableiten.
4. Fehler in eine verständliche Statusmeldung und in `IApplicationLog` schreiben.
5. XAML nur an das Command binden; keine Aktion im Click-Handler implementieren.

### Neue Kanbanize-Kartenfunktion

1. Neutrales Modell oder Validierung in `VIBN_Tools.Core/Kanbanize/` ergänzen.
2. HTTP-Vertrag in `IKanbanizeCardService` halten; neue Endpunkte nur in `VIBN_Tools.Infrastructure/Kanbanize/` implementieren.
3. Alle externen Schreibvorgänge vor dem Senden validieren und nach Erfolg Status/Log schreiben.
4. Board-IDs nie fest in XAML oder ViewModel schreiben; aus der API laden.
5. Keine Lizenzfelder, -anfragen oder Schlüsselanzeige in das Modul aufnehmen.

### VIBN-Kartenübernahme ändern

1. Die Filter- und Idempotenzregeln ausschließlich in `VibnWorkplaceSynchronizationPolicy` ändern.
2. Vor dem Hinzufügen eines Schreibvorgangs prüfen, ob er mit dem Grundsatz „neue Karte oder Deadline, nichts sonst“ vereinbar ist.
3. Jede neue Aktion als Vorschauposition modellieren und im Core-Smoketest absichern.
4. Mehrdeutige Zielzuordnungen immer als Konflikt behandeln, niemals automatisch auflösen.
5. API-Payloads so klein halten, dass `UpdateDeadlineAsync` wirklich nur `deadline` sendet.

### Neue TIA-Operation

1. Request/Response in `VIBN_Tools.Tia.Contracts` ergänzen.
2. Client-Vertrag und Named-Pipe-Aufruf ergänzen.
3. Dispatch in `TiaCommandDispatcher` ergänzen.
4. Openness-Aufruf hinter `ITiaOpennessSession` implementieren.
5. Protokolltest und Workflowtest ergänzen.

Damit bleibt die Hauptanwendung unabhängig von einer konkreten TIA-Version und ein Bridge-Fehler beendet nicht den UI-Prozess.

### Neues spezielles Gerät

Geräteklasse im passenden Unterordner von `SpecialDevices` ergänzen und über `DeviceFactory`/`DeviceCatalog` registrieren. Gemeinsames Verhalten gehört in Basistypen oder Dienste, nicht als Kopie in jede Geräteklasse.

### Neue Container-Art

Die passende Basisklasse im Bereich `ContainerToFee` bzw. den Generatorvertrag in `ContainerGeneration` verwenden. Serialisierung, FEE-Zugriff und UI-Auswahl getrennt halten. Für formatabhängige Varianten eine Strategie statt großer `switch`-Blöcke einsetzen.

## Build und Tests

Voraussetzungen sind Windows, .NET 8 SDK, Grob.UX, fe.screen-sim V5 und für Live-TIA die jeweilige TIA-/Openness-Installation.

```powershell
dotnet restore VIBN_Tools_App.sln --configfile NuGet.Config
dotnet build VIBN_Tools_App.sln --configuration Release --no-restore
dotnet run --project Tests/CoreSmokeTests/VIBN_Tools.Core.SmokeTests.csproj --configuration Release
```

Vor einer Veröffentlichung zusätzlich die WPF-Start-/Interaktionstests und die manuelle [Release-Abnahme](ACCEPTANCE_CHECKLIST.md) ausführen. Live-UNC-, Outlook-, Kanbanize- und TIA-Tests benötigen die jeweilige Unternehmensumgebung und können nicht vollständig durch lokale Smoke-Tests ersetzt werden.

## Review-Checkliste

- Zuständigkeit der geänderten Klasse ist weiterhin eindeutig.
- Neue externe Abhängigkeit besitzt ein Core-Interface.
- Keine synchrone I/O im UI-Thread.
- Keine TwoWay-Bindung auf eine schreibgeschützte Eigenschaft.
- Abbruch, leere Daten und nicht erreichbare Netzwerkpfade sind behandelt.
- Benutzerzuordnung stammt aus `WorkstationDirectory`, nicht aus einer neuen festen Tabelle.
- Level9-Änderungen laufen über `LicenseAdministrationPolicy`.
- `lutzma` bleibt über `MandatoryLevel9User` auf Level9; der Verwaltungsreiter ist ab Level7 sichtbar.
- Kanbanize-Karten nutzen `IKanbanizeCardService` und enthalten keine Lizenzlogik.
- VIBN-Synchronisierung verwendet die Quellkarten-ID als `custom_id`, erstellt keine Duplikate und verändert bestehende Karten nur an der Deadline.
- Fehler sind bedienbar formuliert und technisch protokolliert.
- Mindestens ein automatisierter Test schützt die neue Fachregel.
