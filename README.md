# VIBN Tools

VIBN Tools ist die WPF-Desktopanwendung für Modellierung, virtuelle Inbetriebnahme und die dazugehörigen ViCo-Arbeitsabläufe. Die vorhandenen VIBN-Werkzeuge bleiben erhalten. ViCo, Kanbanize und die TIA-Bridge sind als getrennte, testbare Module integriert.

## Einstieg

- [Anwenderhandbuch](docs/BENUTZERHANDBUCH.md) – vollständige Bedienung aller Reiter, Beispiele und Screenshots.
- [Gesamtübersicht der Solution](docs/GESAMTLOESUNG.md) – Funktionslandkarte und Zuständigkeiten.
- [Kanbanize Karten](docs/KANBANIZE_KARTEN.md) – sichere VIBN-zu-Arbeitsplätze-Synchronisierung und manuelle Karten.
- [Rollenverwaltung](docs/ROLLENVERWALTUNG.md) – Level, Sichtbarkeiten und die Level9-Mindestregel.
- [Konfiguration und Betrieb](docs/KONFIGURATION_UND_BETRIEB.md) – Voraussetzungen, Datenquellen, Protokolle und Fehlersuche.
- [Entwicklerhandbuch](docs/ENTWICKLERHANDBUCH.md) und [Klassenreferenz](docs/KLASSENREFERENZ.md) – Architektur, Erweiterungspunkte und Codewegweiser.

## Wichtige Eigenschaften

- ViCo verwendet einen gemeinsamen, dynamischen PC-/Benutzerbestand aus Kanbanize; es gibt keine fest kompilierte PC-Benutzer-Zuordnung.
- Die ViCo-Übersicht zeigt alle Karten der jeweiligen Arbeitsplatz-Lane. Die Zustandskennung `[B]`, `[P]`, `[W]` oder `[D]` bleibt sichtbar; die `KONFIGURATION`-Karte wird separat angezeigt und bearbeitet.
- Der normale Button **Remote Desktop** legt den lokalen Credential-Manager-Eintrag aus `VIBN_RDP_PASSWORD` nur für den Start an und entfernt ihn nach 20 Sekunden. **RDP mit Anmeldedaten** öffnet den Windows-Anmeldedialog ohne diesen temporären Eintrag.
- Kanbanize synchronisiert keine Duplikate und ändert bei vorhandenen generierten Karten ausschließlich den berechneten Starttermin und die Deadline.
- Die TIA-Openness-Kommunikation läuft in einem separaten Bridge-Prozess; ein TIA-Fehler beendet nicht die WPF-Anwendung.
- Rollen ersetzen Lizenzanfragen. Level7 schaltet CAD Wizard, Container Generation und Container2Fee frei; Level8 zusätzlich AI-Test und Kanbanize; die ViCo-Verwaltung ist ab Level8 sichtbar und ab Level9 schreibbar.

## Build und lokale Prüfungen

Die vollständige Solution ist `VIBN_Tools_App.sln`. Für einen Build werden Windows, .NET 8, Grob.UX und das FEE-SDK benötigt. Für reale TIA-Funktionen muss außerdem eine unterstützte Siemens-TIA-Portal-/Openness-Installation vorhanden sein.

```powershell
dotnet build VIBN_Tools_App.sln --configuration Release
dotnet run --project Tests/CoreSmokeTests/VIBN_Tools.Core.SmokeTests.csproj --configuration Release
dotnet run --project Tests/UiStartupSmokeTests/VIBN_Tools.UiStartup.SmokeTests.csproj --configuration Release
```

Die lokalen Smoke-Tests verwenden keine produktiven Kanbanize-Boards und keine realen TIA-Projekte. Die zusätzliche Live-Abnahme ist in [ACCEPTANCE_CHECKLIST.md](docs/ACCEPTANCE_CHECKLIST.md) beschrieben.
