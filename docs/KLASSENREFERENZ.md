# Klassen- und Verzeichnisreferenz

## Anwendung und Navigation

| Klasse/Datei | Aufgabe |
| --- | --- |
| `Application/View/MainWindow.xaml` | Hauptnavigation, zentrierte Reiterbeschriftungen und Level7-/Level8-Sichtbarkeit |
| `Application/VM/MainWindowVM.cs` | lädt Arbeitsplätze/Rollen beim Start und berechnet Hauptreiter-Berechtigungen |
| `Application/ViCoFeatureBootstrapper.cs` | Composition Root für ViCo, Kanbanize, RDP, Rollen und TIA-Bridge |
| `Application/ApplicationLogService.cs` | zentraler Anwendungslog für Status, Warnungen und Fehler |
| `Application/View/DiagnosticsPanel.xaml` | sichtbares Diagnosefenster im Hauptfenster |

## ViCo-Modelle und Fachregeln (`VIBN_Tools.Core/ViCo`)

| Datei/Typ | Aufgabe |
| --- | --- |
| `Workstations.cs` | PC-, Projekt-, Konfigurations-, RDP- und Dienstverträge; `ViCoWorkstation` berechnet Status/Projektübersicht |
| `UserRoles.cs` | `ViCoUserRole`, `ViCoRolePolicy`, `IViCoUserRoleStore`, feste `lutzma`-Rolle und Zwei-Level9-Invariante |
| `ProjectCatalog.cs` | Projekt-/Favoritenmodelle und Suchverträge |
| `Diagnostics.cs` | neutraler Logvertrag `IApplicationLog` |
| `Administration.cs` | Outlook-/Updateverträge für die ViCo-Verwaltung |

## ViCo-Infrastruktur (`VIBN_Tools.Infrastructure/ViCo`)

| Datei/Typ | Aufgabe |
| --- | --- |
| `LegacyWorkstationCatalog.cs` | liest kompatible Cachedateien und die strukturierte `KONFIGURATION`-Karte; Kanbanize-Benutzer hat Vorrang |
| `KanbanizeRefreshService.cs` | lädt Arbeitsplätze/Robotik aus Kanbanize und ersetzt Caches atomar |
| `WorkstationBoardCache.cs` | typisierte Cacheform mit Karten- und Unteraufgaben-IDs |
| `KanbanizeWorkstationConfigurationService.cs` | aktualisiert/ergänzt Standard-Unteraufgaben und legt eine fehlende KONFIGURATION-Karte nur auf expliziten Befehl an |
| `DesktopWorkstationServices.cs` | Ping, temporärer Credential-Manager-Eintrag, `.rdp`-Start und read-only `quser`-Abfrage mit Fehlerdiagnose |
| `RemoteDesktopProfileBuilder.cs` | reine, testbare `.rdp`-Profilbildung für automatische oder abgefragte Anmeldungen |
| `JsonViCoUserRoleStore.cs` | atomare `roles.json`-Ablage |
| `LegacyRoleMigrationReader.cs` | einmaliger Nur-Lese-Import älterer Zuordnungen |
| `BoundedFileCopyService.cs` | begrenzte parallele Dateiübertragung |

## ViCo-ViewModels (`Application/VM`)

| Klasse | Aufgabe |
| --- | --- |
| `ViCoSearchPageVM` | Suche, Refresh, Pfadauflösung, Online-/Session-Abfragen, RDP sowie KONFIGURATION speichern/anlegen |
| `ViCoWorkstationRowVM` | Präsentation einer Tabellenzeile: Farben, Erreichbarkeit, RDP-Sitzung und Konfigurationsspalten |
| `ViCoConfigurationFieldVM` | Änderungsnachverfolgung einer vorhandenen Konfigurations-Unteraufgabe |
| `ViCoPageVM` | Projekte und Favoriten |
| `ViCoCopyPageVM` | Transfer zwischen Quell- und Zielpfaden |
| `ViCoWorkspacePageVM` | Sichtbarkeit des Verwaltungsreiters ab Level8 |
| `ViCoAdministrationPageVM` | Rollen, Termine, Versionen; nur Level9 schreibt Rollen |
| `TiaPortalPageVM` | PLC-, Library-, Achsen- und Hardwareansicht mit abgefangenen Bridge-Fehlern |

## Kanbanize

| Datei/Typ | Aufgabe |
| --- | --- |
| `Core/Kanbanize/CardCreation.cs` | neutrale Karten-/Boardmodelle und `IKanbanizeCardService` |
| `Core/Kanbanize/VibnWorkplaceSynchronization.cs` | Auswahl-, Zeitplan-, Konflikt- und Idempotenzregel |
| `Infrastructure/Kanbanize/KanbanizeCardApiService.cs` | v2-HTTP-Abfragen, Kartencreate und minimale Deadline-/Start-Patches |
| `Application/VM/KanbanizeCardPageVM.cs` | Boards, Auswahl und manuelle Kartenerstellung |
| `Application/VM/VibnWorkplaceSynchronizationVM.cs` | Vorschauzeilen, Zähler und bewusste Synchronisierung |
| `Application/View/KanbanizeCardPage.xaml` | UI mit Trennung zwischen Automatik und eigener Karte |

## TIA-Bridge

| Projekt/Datei | Aufgabe |
| --- | --- |
| `VIBN_Tools.Tia.Contracts` | serialisierbare DTOs, `TiaCommands`, Request/Response-Umschläge |
| `VIBN_Tools.Tia.Client` | typed Named-Pipe-Client, Bridge-Start, Library-Import/-Export |
| `VIBN_Tools.TiaBridge/Bridge/TiaCommandDispatcher.cs` | ordnet Protokollkommandos Sessionmethoden zu |
| `VIBN_Tools.TiaBridge/Openness/TiaOpennessSession.cs` | versionsgebundene Siemens-Openness-Implementierung; `ListHardware` liest Modul- und E/A-Adressdaten |
| `Application/VM/TiaPortalPageVM.cs` | WPF-Steuerung und protokollierter Fehlerpfad |

## Special Devices und bestehende VIBN-Bereiche

| Bereich | Einstiegspunkt |
| --- | --- |
| TIA-Hardware nach Special Devices | `SpecialDevicePageVM.cs`, `SpecialDeviceHardwareImportVM.cs`, `SpecialDevices/DeviceFactory.cs` |
| CAD Wizard | `CadWizardPageVM.cs` |
| Zuli Converter | `ZuliConverterPageVM.cs` |
| Container Generation | `ContainerGenerationPageVM.cs`, `ContainerGeneration/` |
| Container2Fee | `ContainerToFeePageVM.cs`, `ContainerToFee/` |
| Model Validation | `ModelValidationPageVM.cs` |
| Model Control | `ModelControlPageVM.cs` |
| Interface Operation | `InterfaceOperationPageVM.cs` |

Bestehende große VIBN-ViewModels werden nicht durch neue ViCo-Logik vergrößert. Neue Integrationslogik gehört in die oben genannten modulierten Klassen und Dienste.
