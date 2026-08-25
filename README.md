# VIBN Tools mit integriertem ViCo

Windows-Desktopanwendung für Arbeitsabläufe der virtuellen Inbetriebnahme. Die bestehende VIBN-Tools-Oberfläche bleibt der Host; ViCo und die isolierte TIA Bridge sind als modulare Funktionen integriert.

## Dokumentation

Der vollständige Einstieg für Anwender, Entwickler und Betrieb befindet sich im [Dokumentationsindex](docs/README.md).

- [Benutzerhandbuch](docs/BENUTZERHANDBUCH.md)
- [Kanbanize Karten](docs/KANBANIZE_KARTEN.md)
- [Entwicklerhandbuch](docs/ENTWICKLERHANDBUCH.md)
- [Klassenreferenz](docs/KLASSENREFERENZ.md)
- [Quellcode-Dokumentation](docs/QUELLCODE_DOKUMENTATION.md)
- [Konfiguration und Fehlersuche](docs/KONFIGURATION_UND_BETRIEB.md)
- [Lizenzverwaltung und Level9-Regel](docs/LIZENZVERWALTUNG.md)
- [Datenflüsse](docs/DATENFLUESSE.md)
- [Release-Abnahme](docs/ACCEPTANCE_CHECKLIST.md)

## Build

Voraussetzungen:

- Windows mit .NET 8 SDK
- konfigurierte Grob.UX-Paketquelle
- fe.screen-sim V5 SDK; bei abweichender Installation `FEE_SCREEN_SIM_ROOT` setzen
- Siemens TIA Portal/Openness für Live-TIA-Abläufe
- Zugriff auf die konfigurierten GROB-Netzwerkpfade für Live-ViCo-Daten

```powershell
dotnet restore VIBN_Tools_App.sln --configfile NuGet.Config
dotnet build VIBN_Tools_App.sln --configuration Release --no-restore
```

Lokale Kernprüfungen:

```powershell
dotnet run --project Tests/CoreSmokeTests/VIBN_Tools.Core.SmokeTests.csproj --configuration Release
```

Schlüssel und Anmeldedaten dürfen nie in das Anwendungsprotokoll geschrieben werden. Die aktuelle Kompatibilität für Legacy-Lizenzen und den bisherigen Remote-Desktop-Ablauf ist für die spätere Sicherheitsphase dokumentiert.
