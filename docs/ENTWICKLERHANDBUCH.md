# Entwicklerhandbuch

## Architekturregel

Neue ViCo-, Kanbanize- und TIA-Funktionen folgen dieser Richtung:

```text
View (XAML) → ViewModel → Core-Modell/Interface → Infrastructure-Adapter
TIA: ViewModel → Tia.Client → Named Pipe → TiaBridge → Siemens Openness
```

`VIBN_Tools.Core` darf keine WPF-, HTTP-, Windows- oder Siemens-Abhängigkeit erhalten. `Infrastructure` implementiert Core-Verträge. `Application/VM` koordiniert Bedienung und Status, enthält aber keine Transportformate oder Geschäftsregeln. Die bestehende VIBN-Logik wird nur dort angefasst, wo eine neue Integration sie benötigt.

## Einstieg in den Code

1. `Application/View/MainWindow.xaml` zeigt alle Hauptreiter und die Rollen-Sichtbarkeit.
2. `Application/VM/MainWindowVM.cs` lädt dynamische Arbeitsplätze und die zentrale Rollenliste.
3. `Application/ViCoFeatureBootstrapper.cs` verbindet Core-Interfaces mit konkreten Infrastrukturdiensten.
4. Für den gewünschten Funktionsbereich die Tabelle in [KLASSENREFERENZ.md](KLASSENREFERENZ.md) verwenden.
5. Vor einer Änderung die korrespondierenden Smoke-Tests in `Tests/CoreSmokeTests/Program.cs` lesen.

## Erweiterungsmuster

### Neue ViCo-Arbeitsplatzinformation

1. Feld als neutrales Modell oder Vertrag in `VIBN_Tools.Core/ViCo` definieren.
2. Cache-/Kanbanize-Parsing in `LegacyWorkstationCatalog` bzw. `KanbanizeRefreshService` ergänzen.
3. Nur wenn editierbar: vorhandene Feld-/Subtask-ID im Core-Modell bewahren und einen eng begrenzten Infrastruktur-Write implementieren.
4. Anzeige in `ViCoWorkstationRowVM` und XAML ergänzen.
5. Parser- und Write-Scope-Test hinzufügen.

Die `KONFIGURATION`-Bearbeitung ist das Referenzmuster: vorhandene Unteraufgaben werden per PATCH geändert, fehlende Standard-Unteraufgaben per POST ergänzt. Eine fehlende Karte wird nur nach dem expliziten UI-Befehl standardisiert erstellt; normale Karten bleiben unberührt.

### Neue RDP-/Windows-Aktion

Zuerst ein Interface in `Workstations.cs` ergänzen. Danach eine konkrete Implementierung in `DesktopWorkstationServices.cs` schreiben und sie im Bootstrapper registrieren. Keine `Process.Start`-Aufrufe direkt aus einem ViewModel einfügen. Offline-Schutz und Fehlerprotokoll gehören in das ViewModel.

Ein RDP-Profil darf ausschließlich Ziel-PC, Benutzer, Monitorwahl und Abfragemodus enthalten. Der einzige Kennwortprovider ist `VIBN_RDP_PASSWORD`; `WindowsTemporaryRemoteCredentialStore` reicht den Wert über `ProcessStartInfo.ArgumentList` an `cmdkey`, protokolliert ihn nie und entfernt den Zieleintrag verzögert. Keine zweite Passwortquelle und kein Literal ergänzen.

`quser /server:<PC>` besitzt keinen sicheren Rechte-Bypass. Fehler 5 wird als Berechtigungsdiagnose an die Oberfläche gereicht. Alternative Implementierungen dürfen keine Credentials auslesen oder Berechtigungen umgehen.

### Neue Kanbanize-Funktion

1. Modell, Validierung und Fachregel in `VIBN_Tools.Core/Kanbanize`.
2. Eventuelle HTTP-Operation als schmalen Member von `IKanbanizeCardService` formulieren.
3. Den v2-Adapter in `VIBN_Tools.Infrastructure/Kanbanize/KanbanizeCardApiService.cs` umsetzen.
4. Payload auf das fachlich erlaubte Minimum begrenzen.
5. Einen `RecordingHttpMessageHandler`-Test hinzufügen, der Methode, URL und JSON-Felder prüft.

Keine generische „Update alles“-Methode einführen: Gerade beim Arbeitsplatz-Board ist der eng begrenzte Write-Scope Teil der Fachanforderung.

### Neue TIA-Operation

Die Reihenfolge ist verbindlich:

1. DTO und Kommandoname in `VIBN_Tools.Tia.Contracts`.
2. Member in `ITiaBridgeClient` und `NamedPipeTiaBridgeClient`.
3. Dispatch im `TiaCommandDispatcher`.
4. Implementierung in `ITiaOpennessSession` und `TiaOpennessSession`.
5. ViewModel-Command, Status und XAML.
6. Protocol-/Fake-Test in `Tests/CoreSmokeTests`.

Die Hauptanwendung darf keine Siemens-Openness-Assembly direkt laden. TIA-Fehler sind im ViewModel zu fangen und über `IApplicationLog` zu dokumentieren.

### Neues Special Device

1. konkrete Geräteklasse unter `SpecialDevices/Devices` ergänzen;
2. in `DeviceCatalog` und `DeviceFactory` registrieren;
3. optional eine konservative TIA-Erkennung in `SpecialDeviceLogicOption.Suggest` hinzufügen;
4. zuerst nur in die Warteschlange übernehmen, FEE-Erzeugung erst nach Benutzerprüfung starten.

### Neue Rolle oder Reiterberechtigung

Rollenlogik liegt allein in `ViCoRolePolicy`. Sichtbarkeiten liegen in `MainWindowVM`/`MainWindow.xaml` bzw. `ViCoWorkspacePageVM`. Die Regel darf nicht als Zeichenvergleich in mehreren XAML-Dateien dupliziert werden.

## Nebenläufigkeit und UI-Stabilität

- Netzwerk-, Datei-, Kanbanize- und TIA-Arbeit niemals im UI-Thread ausführen.
- Fan-out begrenzen: Arbeitsplatz-Pings sind auf acht, RDP-Sitzungsabfragen auf vier parallele Anfragen begrenzt.
- Bei Benutzerfiltern Abbruchtokens/Debounce einsetzen.
- Schreiboperationen, die dieselbe externe Ressource betreffen, serialisieren oder idempotent machen.
- Fehler einer optionalen Detailabfrage dürfen nie den gesamten Tabellen-Refresh abbrechen.
- Beim Binden von WPF-Eigenschaften `OneWay` einsetzen, wenn keine Quelle geschrieben werden darf. Das verhindert die früheren schreibgeschützten `PropertyPathWorker`-Fehler.

## Tests

| Test | Ziel |
| --- | --- |
| `Tests/CoreSmokeTests` | Modelle, Parser, Rollen, RDP-Profil, Kanbanize-Idempotenz, schmale HTTP-Payloads, TIA-Library und Named-Pipe-Protokoll |
| `Tests/UiStartupSmokeTests` | Instanziierung integrierter WPF-Views, deferred Tabs, DataGrid-/ComboBox-Bindings und Screenshot-Erzeugung |
| manuelle Abnahme | reale UNC-Pfade, echte Kanbanize-Berechtigung, FEE, Outlook, RDP und TIA Openness |

Vor dem Commit mindestens Core-Smoke, WPF-UI-Smoke und einen Release-Build ausführen. Für reale Systeme zusätzlich [ACCEPTANCE_CHECKLIST.md](ACCEPTANCE_CHECKLIST.md) abarbeiten.

## Kommentare und Lesbarkeit

XML-Kommentare erklären öffentliche Modelle, Grenzen und Invarianten. Kommentare innerhalb einer Methode erklären ausschließlich nicht offensichtliche Entscheidungen, beispielsweise Timeout-, Cache- oder Datenintegritätsgründe. Sie dürfen keinen Code in eigenen Worten wiederholen.

Neue Klassen sollen eine eng abgegrenzte Aufgabe haben. Wenn eine ViewModel-Datei mehrere eigenständige Präsentationsmodelle enthält, diese in getrennte Dateien auslagern – beispielsweise `ViCoWorkstationRowVM` gegenüber `ViCoSearchPageVM`.
