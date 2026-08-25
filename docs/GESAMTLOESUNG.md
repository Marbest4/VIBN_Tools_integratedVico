# VIBN Tools – Gesamtübersicht der Solution

Dieses Handbuch beschreibt die vollständige Desktopanwendung, nicht nur ViCo. Die bestehende VIBN-Funktionalität bleibt in ihren eigenen Reitern erhalten; ViCo und Kanbanize ergänzen sie um Projekt-, PC- und Arbeitsvorbereitung.

## Gemeinsamer Arbeitsablauf

1. In **Project Settings** das lokale oder entfernte FEE-/Simulationsprojekt verbinden.
2. Den passenden Fachreiter wählen, Eingabedaten laden und die Vorschau bzw. Validierung prüfen.
3. Eine schreibende Aktion erst nach Prüfung der Auswahl ausführen.
4. Statuszeile und das globale **Diagnoseprotokoll** bei Abweichungen prüfen.

Die Funktionen greifen teilweise auf dasselbe verbundene FEE-Modell zu. Ein Wechsel des Reiters trennt eine bestehende Verbindung nicht automatisch.

## Hauptreiter und Bedienung

| Reiter | Zweck | Typischer Ablauf | Technischer Einstieg |
|---|---|---|---|
| **Project Settings** | FEE-/Simulationsprojekt auswählen und verbinden | PC wählen, Optionen festlegen, **Connect**; erst nach bestätigter Meldung arbeiten | `Settings/`, `Application/VM/SettingsPageVM.cs` |
| **Kanbanize Karten** | VIBN-Karten sicher ins Arbeitsplätze-Board übernehmen oder einzelne Karten anlegen | Zuerst **Boards aktualisieren**; im Automatikreiter prüfen und bewusst synchronisieren; eigene Karten im zweiten Reiter | `VIBN_Tools.Core/Kanbanize/`, `VIBN_Tools.Infrastructure/Kanbanize/` |
| **ViCo** | Arbeitsplätze, Projekte, Transfer, TIA und Verwaltung bündeln | PC/Projekt suchen, passende Aktion wählen; Transfer/TIA/Verwaltung nach Bedarf öffnen | `VIBN_Tools.Core/ViCo/`, `VIBN_Tools.Infrastructure/ViCo/` |
| **CAD Wizard** | CAD-nahe FEE-Strukturen aufbauen | Joints, Sensoren, Förderer oder Templates erzeugen; leere Knoten bzw. Namen anschließend prüfen | `CAD Wizard/`, `CadWizardPageVM.cs` |
| **Zuli Converter** | ZuLi-/Schnittstellendaten in das gewählte Zielformat überführen | Quelldatei öffnen, Optionen prüfen, Importdatei erzeugen | `ZuliConverter/`, `ZuliConverterPageVM.cs` |
| **Container Generation** | Container aus Interface- und Anforderungsdaten erzeugen, prüfen und exportieren | Interface/Requirements laden, Einstellungen laden, generieren, Workspace prüfen, exportieren | `ContainerGeneration/`, `ContainerGenerationPageVM.cs` |
| **Container2Fee** | Container-XML den FEE-Objekten zuordnen und ins Modell übertragen | XML laden, Simulationsobjekte suchen, Zuordnungen prüfen, Generierung starten | `ContainerToFee/`, `ContainerToFeePageVM.cs` |
| **Special Devices** | Spezialgeräte aus dem Katalog konfigurieren und anlegen | Hersteller/Typ wählen, Werte prüfen, hinzufügen bzw. erzeugen | `SpecialDevices/`, `SpecialDevicePageVM.cs` |
| **Model Validation** | Modellregeln ausführen und Befunde strukturieren | FEE-Daten aktualisieren, Gruppen/Filter prüfen, Befunde bestätigen oder korrigieren | `ModelValidation/`, `ModelValidationPageVM.cs` |
| **Model Control** | Roboter, Achsen und steuerbare FEE-Objekte bedienen | FEE-Daten aktualisieren, Objekt auswählen, Bewegung/Operation bewusst auslösen | `ModelControl/`, `RobotControl/`, `ModelControlPageVM.cs` |
| **Interface Operation** | FEE-Schnittstellen und Signale verbinden bzw. zusammenführen | beide Schnittstellen auswählen, Signale filtern, Vorschau prüfen, verbinden/übernehmen | `InterfaceOperation/`, `InterfaceOperationPageVM.cs` |
| **AI-Test** | Trainingsdaten, Vorhersagen, Konflikte und Auswertung der Containerzuordnung bearbeiten | Trainingsordner prüfen, trainieren/auswerten, Konflikte korrigieren, Berichte exportieren | `ContainerGeneration/AI/`, `AITrainingTestPageVM.cs` |

Der ausgeblendete Reiter **MiniTools** ist keine aktive Produktfunktion.

## Kanbanize: zwei bewusst getrennte Arbeitsweisen

### VIBN → Arbeitsplätze

Dieser Reiter bildet den früheren Canbanize-Automatikablauf in sicherer Form ab:

1. Quelle und Zielboard, Ziel-Lane und Zielspalte prüfen. Die historischen Standardwerte werden nur als Vorauswahl verwendet.
2. **Prüfen** lädt aktuelle Karten beider Boards und zeigt ausschließlich eine Vorschau.
3. Die Automation berücksichtigt nur aktive VIBN-Karten mit `Grundinbetriebnahme`; `Vorlage` und die historische Archivspalte werden ausgeschlossen.
4. **Synchronisieren** erstellt fehlende Zielkarten mit Quellkarten-ID als `custom_id` und Elternverknüpfung.
5. Bei genau einer vorhandenen Zielkarte mit derselben Quellkarten-ID wird nur deren Deadline angeglichen.

Es gibt keine Lösch-, Verschiebe-, Umbenennungs-, Beschreibungs- oder Lizenzaktion. Mehrere Zielkarten mit identischer Quellkarten-ID gelten als Konflikt und werden nicht verändert. Ein zweiter Lauf erzeugt deshalb keine Duplikate.

### Eigene Karte

Die manuelle Kartenerstellung bleibt vollständig unabhängig: Board, Lane, Spalte, Titel, externe ID, Priorität, Deadline und Beschreibung werden bewusst vom Benutzer gewählt. Sie hat keine Lizenzanfragefunktion.

## ViCo im Überblick

| Unterbereich | Funktion |
|---|---|
| **Übersicht & Verbindung** | einheitliche Suche nach PC, Benutzer, GM/GU oder Projekt; Remote Desktop, TeamViewer sowie Projekt-/Simulation-/SPS-/Planungspfade |
| **Projekte & Favoriten** | Simulationsprojektablage durchsuchen, Ordner öffnen und Favoriten im kompatiblen Format verwalten |
| **Transfer** | Projektbestandteile mit begrenzter Parallelität zwischen Verzeichnissen kopieren |
| **TIA Portal** | über separaten Bridge-Prozess mit TIA Openness arbeiten; Bibliotheken importieren/exportieren und Achsen konfigurieren |
| **Verwaltung** | Termine, ViCo-Version und Lizenzbestand; sichtbar ab Level7, Bearbeitung ab Level8 |

Die ViCo-Belegung ist **grün** bei `Frei` und **rot** bei `Belegt`. Planung oder In Arbeit hat Vorrang vor Backlog/Erledigt. Der Online-Status ist separat und prüft nur die Ping-Erreichbarkeit.

## Solution- und Codeaufteilung

```text
VIBN_Tools (WPF-Host)
├─ Application/             Views, ViewModels, Navigation, Diagnoseprotokoll
├─ Settings/                FEE-Verbindung und Projekteinstellungen
├─ CAD Wizard/              CAD-basierte Generierungshilfen
├─ ZuliConverter/           ZuLi-Konvertierung und Exportstrategien
├─ ContainerGeneration/     Import, Regeln, Workspace, Export, KI
├─ ContainerToFee/          Container-Modelle, Factories und FEE-Übertragung
├─ SpecialDevices/          Geräte-Katalog, Factory und konkrete Gerätetypen
├─ ModelValidation/         Regeln, Change-Routing und Befunde
├─ ModelControl/            Achsen, Objekte, Roboter und Bewegungsabläufe
├─ InterfaceOperation/      Schnittstellen- und Signaloperationen
├─ RobotControl/            wiederverwendbare Roboterbewegungen
└─ GlobalClasses/           gemeinsame MVVM-, FEE-SDK- und Hilfstypen

VIBN_Tools.Core             fachliche ViCo-/Kanbanize-Verträge und Regeln
VIBN_Tools.Infrastructure   Datei-, Netzwerk-, Windows- und Kanbanize-Adapter
VIBN_Tools.Tia.Contracts    serialisierbare Named-Pipe-DTOs
VIBN_Tools.Tia.Client       Client zur isolierten TIA Bridge
VIBN_Tools.TiaBridge        separater Openness-Prozess
Tests/                      Core-, WPF-, Golden-Master- und Compile-Tests
```

`Application/ViCoFeatureBootstrapper.cs` ist der Composition Root für ViCo, TIA und Kanbanize. Neue externe Dienste werden dort über ein Core-Interface mit ihrer Infrastrukturimplementierung verbunden. Die bestehenden VIBN-Reiter verwenden historisch gewachsene Dienste direkt; neue Umbauten sollen schrittweise über klare Verträge und Tests erfolgen.

## Wo erweitere ich was?

| Bedarf | Richtiger Ort |
|---|---|
| neue Oberfläche oder Hauptreiter | `Application/View/` und passendes `Application/VM/`; Navigation in `MainWindow.xaml` |
| neue reine ViCo-/Kanbanize-Regel | `VIBN_Tools.Core/ViCo` bzw. `VIBN_Tools.Core/Kanbanize` |
| HTTP-, UNC-, Windows- oder Dateizugriff | `VIBN_Tools.Infrastructure` oder klar abgegrenzter bestehender Dienst |
| neue TIA-Operation | Contracts → Client → Bridge Dispatcher → Openness-Session |
| neues Spezialgerät | Gerätetyp in `SpecialDevices/Devices`, danach `DeviceCatalog`/`DeviceFactory` |
| neuer Container | passende Containerbasis oder Factory in `ContainerToFee`; Generierungsregel in `ContainerGeneration/BusinessLogic` |
| neue Modellprüfung | `ModelValidation` als isolierte Validierungsregel statt in der View |
| neue UI-Aktion | Command im passenden ViewModel, verständliche Statusmeldung und Diagnoseeintrag |

## Kommentare, Lesbarkeit und Tests

Öffentliche Fachverträge, nicht offensichtliche Sicherheits-/Nebenläufigkeitsregeln und externe Systemgrenzen besitzen XML- oder Inline-Kommentare. Historisch gewachsener Code wird nicht mit redundantem Zeilenkommentar überdeckt; die Klassenreferenz und sprechende Typen erklären die Verantwortung.

Für jede neue Fachregel gehört mindestens ein Test nach `Tests/CoreSmokeTests`. XAML-Bindings auf schreibgeschützte Werte müssen `Mode=OneWay` verwenden. Der WPF-Startup-Test schützt zusätzlich gegen XAML- und Binding-Ausnahmen.
