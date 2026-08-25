# VIBN Tools – Dokumentation

Diese Seite ist der Einstiegspunkt für Anwender, Entwickler und den Betrieb.

## Für Anwender

- [Gesamtübersicht der Solution](GESAMTLOESUNG.md): alle aktiven Hauptreiter, typische Abläufe und Erweiterungspunkte.
- [Benutzerhandbuch](BENUTZERHANDBUCH.md): Oberfläche, Reiter, typische Arbeitsabläufe und Bedeutung der Anzeigen.
- [Kanbanize Karten](KANBANIZE_KARTEN.md): neue Karten erstellen, Zielposition wählen und Fehler einordnen.
- [Lizenzverwaltung](LIZENZVERWALTUNG.md): Berechtigungen und die Mindestbesetzung mit zwei Level9-Benutzern.
- [Konfiguration, Betrieb und Fehlersuche](KONFIGURATION_UND_BETRIEB.md): Voraussetzungen, Datenquellen, Protokolle und bekannte Abhängigkeiten.

## Für Entwickler

- [Gesamtübersicht der Solution](GESAMTLOESUNG.md): Landkarte aller VIBN-, ViCo-, TIA- und Testmodule.
- [Entwicklerhandbuch](ENTWICKLERHANDBUCH.md): Architektur, Startablauf, MVVM-Regeln, Build, Tests und Erweiterungsrezepte.
- [Klassenreferenz](KLASSENREFERENZ.md): Zuständigkeit und Erweiterungspunkt der wichtigen Klassen und Verzeichnisse.
- [Quellcode-Dokumentation](QUELLCODE_DOKUMENTATION.md): Kommentierkonventionen und Wegweiser zu den wichtigsten Implementierungen.
- [Datenflüsse](DATENFLUESSE.md): Ablauf von PC-Synchronisation, Remote-Verbindung, TIA Bridge und Lizenzänderung.
- [Release-Abnahme](ACCEPTANCE_CHECKLIST.md): Prüfliste vor einer Veröffentlichung.

Die älteren englischen Dokumente [USER_GUIDE.md](USER_GUIDE.md) und [ARCHITECTURE.md](ARCHITECTURE.md) bleiben als kompakte Referenz erhalten. Bei Widersprüchen sind der aktuelle Code und diese deutschen Dokumente maßgeblich.

## Schnellnavigation im Quellcode

| Gesucht | Einstieg |
|---|---|
| Aufbau der Hauptnavigation | `Application/View/MainWindow.xaml` |
| Zusammensetzung der ViCo-Dienste | `Application/ViCoFeatureBootstrapper.cs` |
| Karten in Kanbanize erstellen | `Application/VM/KanbanizeCardPageVM.cs`, `VIBN_Tools.Core/Kanbanize/`, `VIBN_Tools.Infrastructure/Kanbanize/` |
| VIBN-Karten sicher in Arbeitsplätze synchronisieren | `VibnWorkplaceSynchronizationVM.cs`, `VibnWorkplaceSynchronization.cs` |
| Alle bestehenden VIBN-Werkzeuge | `GESAMTLOESUNG.md`, jeweilige `Application/VM/*PageVM.cs` und Fachverzeichnisse |
| ViCo-Oberflächenlogik | `Application/VM/ViCo*PageVM.cs`, `Application/VM/TiaPortalPageVM.cs` |
| ViCo-Fachregeln und Verträge | `VIBN_Tools.Core/ViCo/` |
| Netzwerk-, Datei- und Windows-Anbindung | `VIBN_Tools.Infrastructure/ViCo/` |
| TIA-Prozessgrenze | `VIBN_Tools.Tia.Contracts/`, `VIBN_Tools.Tia.Client/`, `VIBN_Tools.TiaBridge/` |
| Level9-Mindestanzahl | `VIBN_Tools.Core/ViCo/Administration.cs`, `LicenseAdministrationPolicy.MinimumLevel9Users` |
| Festes Level9-Konto | `LicenseAdministrationPolicy.MandatoryLevel9User` (`lutzma`) |
| Automatisierte Kernprüfungen | `Tests/CoreSmokeTests/Program.cs` |
