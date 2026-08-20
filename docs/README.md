# VIBN Tools – Dokumentation

Diese Seite ist der Einstiegspunkt für Anwender, Entwickler und den Betrieb.

## Für Anwender

- [Benutzerhandbuch](BENUTZERHANDBUCH.md): Oberfläche, Reiter, typische Arbeitsabläufe und Bedeutung der Anzeigen.
- [Lizenzverwaltung](LIZENZVERWALTUNG.md): Berechtigungen und die Mindestbesetzung mit zwei Level9-Benutzern.
- [Konfiguration, Betrieb und Fehlersuche](KONFIGURATION_UND_BETRIEB.md): Voraussetzungen, Datenquellen, Protokolle und bekannte Abhängigkeiten.

## Für Entwickler

- [Entwicklerhandbuch](ENTWICKLERHANDBUCH.md): Architektur, Startablauf, MVVM-Regeln, Build, Tests und Erweiterungsrezepte.
- [Klassenreferenz](KLASSENREFERENZ.md): Zuständigkeit und Erweiterungspunkt der wichtigen Klassen und Verzeichnisse.
- [Datenflüsse](DATENFLUESSE.md): Ablauf von PC-Synchronisation, Remote-Verbindung, TIA Bridge und Lizenzänderung.
- [Release-Abnahme](ACCEPTANCE_CHECKLIST.md): Prüfliste vor einer Veröffentlichung.

Die älteren englischen Dokumente [USER_GUIDE.md](USER_GUIDE.md) und [ARCHITECTURE.md](ARCHITECTURE.md) bleiben als kompakte Referenz erhalten. Bei Widersprüchen sind der aktuelle Code und diese deutschen Dokumente maßgeblich.

## Schnellnavigation im Quellcode

| Gesucht | Einstieg |
|---|---|
| Aufbau der Hauptnavigation | `Application/View/MainWindow.xaml` |
| Zusammensetzung der ViCo-Dienste | `Application/ViCoFeatureBootstrapper.cs` |
| ViCo-Oberflächenlogik | `Application/VM/ViCo*PageVM.cs`, `Application/VM/TiaPortalPageVM.cs` |
| ViCo-Fachregeln und Verträge | `VIBN_Tools.Core/ViCo/` |
| Netzwerk-, Datei- und Windows-Anbindung | `VIBN_Tools.Infrastructure/ViCo/` |
| TIA-Prozessgrenze | `VIBN_Tools.Tia.Contracts/`, `VIBN_Tools.Tia.Client/`, `VIBN_Tools.TiaBridge/` |
| Level9-Mindestanzahl | `VIBN_Tools.Core/ViCo/Administration.cs`, `LicenseAdministrationPolicy.MinimumLevel9Users` |
| Automatisierte Kernprüfungen | `Tests/CoreSmokeTests/Program.cs` |
