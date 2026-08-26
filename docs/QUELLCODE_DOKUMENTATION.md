# Quellcode-Dokumentation und Kommentierregeln

## Dokumentierte Schwerpunkte

Die neuen und geänderten Integrationsklassen besitzen XML-Kommentare an ihren öffentlichen Grenzen. Die Kommentare erklären vor allem:

| Klasse/Datei | dokumentierte Entscheidung |
| --- | --- |
| `ViCoRolePolicy` | feste Level9-Rolle und atomar validierte Mindestbesetzung |
| `ViCoWorkstation`, `ViCoConfigurationField` | Statusregel und sichere Zuordnung von Kanbanize-Unteraufgaben |
| `ViCoSearchPageVM` | begrenzte Ping-/RDP-Abfragen, Offline-Schutz sowie Bearbeiten/standardisiertes Anlegen der Konfiguration |
| `WindowsRemoteSessionService` | read-only Abfrage und „Nicht abrufbar“ bei fehlender Berechtigung |
| `VibnWorkplaceSynchronizationPolicy` | quellkartenbezogene Terminformel, Konflikt- und Duplikatschutz |
| `KanbanizeCardApiService` | minimaler HTTP-Write-Scope ohne Fremdfelder |
| `TiaHardwareModuleInfo`, `TiaOpennessSession` | read-only Hardwaredaten und Byteadress-Semantik |
| `SpecialDeviceHardwareImportVM` | Prüfzone zwischen TIA-Erkennung und FEE-Schreibvorgang |

## Lesereihenfolge für neue Entwickler

1. [GESAMTLOESUNG.md](GESAMTLOESUNG.md) lesen.
2. `MainWindow.xaml` und `ViCoFeatureBootstrapper.cs` öffnen.
3. Für eine ViCo-Änderung zuerst Core-Modelle, dann ViewModel und Infrastrukturadapter lesen.
4. Für Kanbanize `VibnWorkplaceSynchronization.cs` vor `KanbanizeCardApiService.cs` lesen.
5. Für TIA immer Contracts → Client → Dispatcher → Openness → ViewModel verfolgen.
6. Den passenden Smoke-Test lesen und erweitern, bevor eine externe Schreiboperation geändert wird.

## Regeln für neue Kommentare

- Öffentliche Modelle, Interfaces und Seiten-ViewModels: XML-`summary` mit Aufgabe und Grenze.
- Nicht offensichtliche Algorithmen: Kommentar zum Grund, nicht zum offensichtlichen Ablauf.
- Externe Schreiboperationen: ausdrücklich dokumentieren, welche Felder verändert werden dürfen.
- Cache, Timeout, Parallelitätslimit und Fallback: Begründung am Code.
- Keine auskommentierten alten Implementierungen als Dokumentation behalten; Historie gehört in Git und in dieses Handbuch.

## Qualitätsregeln

- Keine WPF-Typen in Core-Modellen.
- Keine HTTP-/Windows-/TIA-Details in XAML oder reinen ViewModels.
- Keine TwoWay-Bindung auf schreibgeschützte Anzeigeeigenschaften.
- Keine ungebremsten Netzwerkfan-outs.
- Keine ungetestete Änderung an Rollen, Kanbanize-Payloads oder TIA-Protokoll.
- Keine neue Lizenzanfrage- oder Lizenzschreiblogik.

## Prüfpfad für eine Änderung

1. Fachregel mit einem Core-Test abdecken.
2. Adapterpayload oder Dateischreibscope mit einem Fake/Recording-Test abdecken.
3. WPF-View im UI-Smoke-Test instanziieren; bei einem neuen deferred Tab gezielt auswählen.
4. Release-Build und Live-Abnahme durchführen.
