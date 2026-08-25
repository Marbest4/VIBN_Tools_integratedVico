# Klassen- und Verzeichnisreferenz

Diese Referenz erklärt Zuständigkeiten und sinnvolle Erweiterungspunkte. Sie ersetzt keine API-Dokumentation jeder einzelnen Anweisung; der Code ist bewusst über sprechende Typen, kleine Methoden, Verträge und Tests dokumentiert.

## WPF-Host und gemeinsame Anwendung

| Datei/Klasse | Zuständigkeit | Hier erweitern, wenn … |
|---|---|---|
| `Application/View/MainWindow.xaml` | Fensterrahmen, Hauptnavigation, Diagnoseleiste | ein Hauptreiter hinzukommt |
| `Application/VM/MainWindowVM.cs` | globaler Fensterzustand | wirklich fensterweiter Zustand benötigt wird |
| `Application/ViCoFeatureBootstrapper.cs` | Composition Root für alle ViCo-Abhängigkeiten | ein Interface eine neue Implementierung erhält |
| `ApplicationLogService` | begrenzter UI-Logpuffer und NLog-Weitergabe | eine anwendungsweite Logausgabe benötigt wird |
| `DiagnosticsPanelVM` / `DiagnosticsPanel.xaml` | Log anzeigen, kopieren, leeren, Ordner öffnen | Diagnosebedienung ergänzt wird |
| `IFolderSelectionService`, `WpfFolderSelectionService` | UI-unabhängiger Vertrag und WPF-Ordnerdialog | ViewModels einen Ordner auswählen lassen sollen |
| `ViCoRemoteCredentialStore` | kompatible Bereitstellung der RDP-Anmeldedaten | der spätere sichere Credential-Speicher migriert wird |
| `LegacyLicenseCompatibility` | Auflösung des vorhandenen Kompatibilitätsschlüssels | das Lizenzformat in der Sicherheitsphase migriert wird |
| `KanbanizeCardPage` / `KanbanizeCardPageVM` | Host für optionale manuelle Karten und VIBN-Synchronisierung, ohne Lizenzworkflow | Kartenreiter oder gemeinsame Board-Ladelogik ergänzt werden |
| `VibnWorkplaceSynchronizationVM` | Auswahl, Vorschau, Status und bewusste Ausführung der VIBN→Arbeitsplätze-Automatik | Bedienung, Zielauswahl oder Vorschautabelle ergänzt werden |

Zu jedem `*PageVM` gehört in `Application/View/` eine gleichnamige XAML-View. `.xaml.cs` beschränkt sich auf Initialisierung und UI-Lebenszyklus; Fachlogik gehört in ViewModel/Core.

## ViCo-ViewModels

| Klasse | Aufgabe |
|---|---|
| `ViCoSearchPageVM` | Suchmodus, Filter, Ergebnisauswahl, Pfadauflösung, Online-Aktualisierung und Remote-/Öffnen-Commands |
| `ViCoWorkstationRowVM` | darstellbarer Zustand einer PC-Zeile einschließlich gecachtem Online-Status |
| `ViCoPageVM` | Simulationsprojektkatalog, Suche, Ordner öffnen und Favoriten laden/speichern |
| `ViCoCopyPageVM` | Transferauswahl, Fortschritt, Abbruch und gemeinsamer Workspace-Kontext |
| `TiaPortalPageVM` | Bridge-Lebenszyklus, TIA-/PLC-Auswahl, Baumdaten und Import-/Export-Workflows |
| `ViCoAdministrationPageVM` | Outlook, Update, Lizenzanzeige und Ausführung validierter Lizenzänderungspläne |
| `ViCoWorkspacePageVM` | prüft das aktuelle Level und blendet Verwaltung unter Level7 aus |
| `KanbanizeCardPageVM` | Boards, Lanes, Workflow-Spalten, manueller Kartendraft und asynchrone Erstellung |
| `VibnWorkplaceSynchronizationVM` | VIBN-Quelle/Zielpositionen, sichere Vorschau, Konfliktanzeige und Synchronisierungscommand |

## `VIBN_Tools.Core/ViCo`

### `Workstations.cs`

| Typ | Aufgabe |
|---|---|
| `ViCoWorkstation` | unveränderliches PC-Modell mit Benutzer, Projekten, Software, FEE, Hardware und Robotern |
| `AutomationSoftwareInfo` | Plattform, Quelle und Aussagegrad `angegeben`/`installiert` |
| `ViCoRobotInfo` | Name, Status und Quellkarte eines Robotiktreffers |
| `IViCoWorkstationCatalog` | liest den PC-Bestand |
| `IViCoWorkstationSearch` | filtert den Bestand nach Modus und Suchtext |
| `IViCoRelatedPathResolver` | löst Simulations-, SPS-, Planungs- und PC-Projektpfade auf |
| `INetworkAvailabilityService` | asynchroner Erreichbarkeitstest |
| `IRemoteDesktopService` | startet RDP ohne Windows-Details im ViewModel |
| `IViCoOnlineRefreshService` | aktualisiert die Online-Datenquelle/Cache-Dateien |
| `WorkstationDirectory` | zentrale, beobachtbare PC-/Benutzerquelle für Settings und ViCo |
| `WindowsUserIdentity` | normalisiert und vergleicht Windows-Benutzernamen |
| `ProjectIdentity` | vereinheitlicht Projektnummern und Statusmarker |

### Weitere Core-Dateien

| Datei | Zentrale Typen und Aufgabe |
|---|---|
| `ProjectCatalog.cs` | `ProjectLocation`, Katalog-/Suchverträge und reine `ProjectSearchService`-Suche |
| `Favorites.cs` | `FavoriteEntry` und Persistenzvertrag |
| `FileCopy.cs` | Kopierauftrag, Fortschritt und Verträge für Kopieren/Öffnen |
| `WorkspaceContext.cs` | geteilte ViCo-Auswahl zwischen Suche, Projekten und Transfer; Projektstrukturvertrag |
| `Administration.cs` | Lizenz-, Termin- und Updateverträge sowie `LicenseAdministrationPolicy` |
| `Kanbanize/CardCreation.cs` | Board-/Positionsmodelle, Kartenentwurf, Validierung und HTTP-unabhängiger Kartenvertrag |
| `Kanbanize/VibnWorkplaceSynchronization.cs` | Quellfilter, Idempotenz über `custom_id`, Vorschau, Konfliktschutz und erlaubte Schreibaktionen |
| `Diagnostics.cs` | Logmodell, Level, `IApplicationLog` und Null-Implementierung für Tests |

Core ist der richtige Ort für Regeln, die unabhängig davon gelten, ob Daten aus Dateien, HTTP oder später einer Datenbank kommen.

## `VIBN_Tools.Infrastructure/ViCo`

| Klasse | Aufgabe / Datenquelle |
|---|---|
| `ViCoPathsOptions` | zentrale Standardpfade, Arbeitsordner und maximale Kopierparallelität |
| `LegacyWorkstationCatalog` | parst die kompatiblen Kanbanize-Cachedateien in Core-Modelle |
| `ViCoWorkstationSearch` | konkrete Suchimplementierung für PC-/Projektabfragen |
| `KanbanizeRefreshService` | lädt Boards 1541 und 846 und ersetzt Cachedateien atomar |
| `NetworkAvailabilityService` | begrenzte, gecachte Ping-Abfrage |
| `WindowsRemoteDesktopService` | erzeugt/startet RDP und setzt den priorisierten Benutzer |
| `RemoteDesktopProfileBuilder` | reine Erzeugung des `.rdp`-Inhalts, dadurch separat testbar |
| `ViCoRelatedPathResolver` | indiziert und findet verwandte Projektpfade |
| `StandardProjectStructureService` | beschreibt auswählbare Standardordner für den Transfer |
| `FileSystemProjectCatalogService` | scannt Simulationsprojekte asynchron |
| `LegacyTextFavoritesRepository` | liest/schreibt das bestehende Favoritenformat |
| `BoundedFileCopyService` | kopiert Dateien mit begrenzter Parallelität und Fortschritt |
| `LegacyLicenseService` | liest/schreibt vorhandene verschlüsselte Lizenzdateien |
| `OutlookMeetingService` | liest heutige Termine über Outlook-Interop |
| `FileSystemViCoUpdateService` | findet die neueste Version im Versionsverzeichnis |
| `WindowsPathLauncher` | öffnet Datei-/Ordnerziele über Windows |
| `KanbanizeCardApiService` | lädt Boards/Positionen/Karten, erstellt manuelle oder VIBN-verknüpfte Karten und patcht gezielt Deadlines; keine Lizenzlogik |

Parseränderungen für das vorhandene Cacheformat gehören in `LegacyWorkstationCatalog`; Änderungen an der Kanbanize-API-Abfrage in `KanbanizeRefreshService`. Diese Trennung verhindert, dass die UI vom Transportformat abhängt.

## TIA-Teilprojekte

| Klasse/Datei | Aufgabe |
|---|---|
| `TiaProtocol.cs` | Befehlsnamen, Request-/Response-Hüllen und strukturierte Fehler |
| `TiaDtos.cs` | ausschließlich serialisierbare Prozessgrenzen-DTOs |
| `ITiaBridgeClient` | asynchroner Vertrag der Hauptanwendung |
| `NamedPipeTiaBridgeClient` | startet/überwacht Bridge und überträgt JSON über Named Pipes |
| `TiaBridgeClientOptions` | Pipe, Executable und Zeitlimits |
| `TiaLibraryService` | höherer Import-/Export- und Achsbibliotheksworkflow |
| `TiaBridgeServer` | Named-Pipe-Server im separaten Prozess |
| `TiaCommandDispatcher` | ordnet protokollierte Befehle Openness-Operationen zu |
| `ITiaOpennessSession` | testbare Grenze zu Siemens Openness |
| `TiaOpennessSession` | versionsgebundene echte TIA-Implementierung |

## Bestehende VIBN-Funktionsbereiche

| Verzeichnis / ViewModel | Inhalt und Erweiterungspunkt |
|---|---|
| `Settings`, `SettingsPageVM` | Projekt-/FEE-Verbindung und gemeinsame Einstellungen; PC-Quelle nicht duplizieren |
| `Settings/FeeConnectionService` | überwacht SDK-Zustand und bestätigt Connect erst bei `NetworkState.Connected` |
| `CAD Wizard`, `CadWizardPageVM` | CAD-Mapping und CAD-Arbeitsablauf |
| `ZuliConverter`, `ZuliConverterPageVM` | Konvertierungsservice und formatabhängige Strategien |
| `ContainerGeneration`, `ContainerGenerationPageVM` | Generator, Einstellungen, Workspace und KI-Zuordnung; neue Generatorlogik als Dienst |
| `ContainerToFee`, `ContainerToFeePageVM` | Containerbasistypen und konkrete FEE-Abbildungen; neue Container von passender Basis ableiten |
| `SpecialDevices`, `SpecialDevicePageVM` | Gerätekatalog, Factory und Gerätetypen; neue Geräte registrieren statt UI-Sonderfall |
| `ModelValidation`, `ModelValidationPageVM` | Validatoren und Befunddarstellung; neue Regel als Validator ergänzen |
| `ModelControl`, `ModelControlPageVM` | Axis-/Object-/Robot-/Motion-Dienste; gerätespezifisches Verhalten in Dienst kapseln |
| `InterfaceOperation`, `InterfaceOperationPageVM` | Schnittstellenoperationen und UI-Koordination |
| `RobotControl` | wiederverwendbare Robotersteuerung |
| `AITrainingTestPageVM`, `ContainerGeneration/AI` | Trainings- und Testablauf für KI-Zuordnung |
| `GlobalClasses` | FEE-SDK-Zugriff, gemeinsame Domänen-/MVVM-Hilfen; nur wirklich modulübergreifenden Code ergänzen |
| `KanbanizeService` | bestehender Kanbanize-Kompatibilitätszugriff; neue ViCo-Aktualisierung bevorzugt über Core-Interface |
| `RemoteService`, `TiaService` | Legacy-Servicegrenzen; bei neuer ViCo-Logik nicht direkt aus Views verwenden |

Einige ursprüngliche ViewModels sind historisch groß. Sie wurden bei der ViCo-Integration bewusst nicht funktional umgebaut, um Regressionen in den laufenden VIBN-Werkzeugen zu vermeiden. Neue Logik dort sollte schrittweise in Services/Strategien extrahiert und mit Golden-Master- oder Fachtests abgesichert werden.

## Tests

| Bereich | Zweck |
|---|---|
| `Tests/CoreSmokeTests` | Parser, einheitliche Suche, Belegung, Favoriten, Transfer, RDP-Profil, Level9-Regel, Kartenvalidierung und TIA-Protokoll |
| `Tests/UiStartupSmokeTests` | WPF-Start, Ressourcen und wichtige Interaktionen/Bindings |
| `Tests/GoldenMaster` | Schutz bestehender generierter Ergebnisse |
| `Tests/ContainerGenerationCompile` | Kompilierbarkeit generierter Containerlogik |
