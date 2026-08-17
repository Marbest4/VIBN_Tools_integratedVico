# ViCo- und TIA-Integration

## Zielarchitektur

Die vorhandene WPF-Anwendung bleibt der Desktop-Host und stellt die gemeinsame Navigation bereit.
Die integrierten Funktionen sind in klar getrennte Schichten aufgeteilt:

- `VIBN_Tools.Core`: UI-unabhängige ViCo-Modelle und Schnittstellen.
- `VIBN_Tools.Infrastructure`: Dateisystem, Favoriten, Pfadöffnung und begrenztes paralleles Kopieren.
- `VIBN_Tools.Tia.Contracts`: versionsneutrale DTOs und Befehle für die Prozessgrenze.
- `VIBN_Tools.Tia.Client`: asynchroner, typisierter Named-Pipe-Client der WPF-Anwendung.
- `VIBN_Tools.TiaBridge`: separater .NET-Framework-4.8-Prozess für Siemens TIA Openness.
- `Application/View`: WPF-Oberflächen innerhalb der bestehenden VIBN-Tools-Navigation.

Die Prozessgrenze ist beabsichtigt: Das WPF-Hauptprogramm kann modern bleiben, während die klassische
TIA-Openness-API weiterhin in dem von Siemens unterstützten .NET-Framework-Prozess geladen wird.

## Enthaltener Funktionsstand

| ViCo-Referenzfunktion | Integration im WPF-Host |
| --- | --- |
| PC Search / Project Search | Gemeinsame Suche mit PC-, Benutzer-, TIA-, FEE-, LAN-, Projekt- und Robotikdaten |
| Kanbanize-Aktualisierung | Asynchroner API-Abruf mit atomarer Aktualisierung der kompatiblen Server-Caches |
| PC-Erreichbarkeit | Asynchroner Ping mit kurzem Timeout |
| Remote Connection | RDP-Datei mit Auswahl von bis zu vier lokalen Monitoren |
| TeamViewer | Öffnen der bisherigen TeamViewer-Weboberfläche |
| PC-, Simulation-, PLC- und Planungsordner | Auflösung und Öffnen der bisherigen UNC-/Cachepfade |
| Copy Overlay | Dateien/Ordner, Mehrfachauswahl, Zielwahl, begrenzte Parallelität, Fortschritt und Abbruch |
| Favourites | Rückwärtskompatibles Textformat, Projekt-/Datei-/Programm-/Ordnerfavoriten, Bearbeiten und Löschen |
| TIA Portal | V15–V22, Attach, PLC-Auswahl, Bausteine, Datentypen, Achsen und Speichern |
| Import Custom ViCo | Rekursiver Import von `_Programm` und `_Datatype`, Ordneranlage und IDBs zuletzt |
| Export Custom ViCo | Rekursiver Export mit Erhalt der TIA-Ordnerstruktur |
| Integrate ViCo Axis Config | Achsparametrierung sowie Erzeugung und Import von AxisDB/AxisFC |
| Lizenzierung | Bestehendes AES-Dateiformat, Anfragen, Levelanzeige und Level-8-Verwaltung |
| Outlook HUD | Anzeige der noch anstehenden Termine des aktuellen Tages |
| Updateprüfung | Erkennung der neuesten Version im bestehenden Versionsverzeichnis |

Nicht als Produktionsfunktionen übernommen wurden ausschließlich die im Referenzprojekt nicht erreichbaren
Demo-/Testseiten `Window3`, `Window99` und `ProgramTest`.

## Build

Die neue Lösung ist `VIBN_Tools_App.sln`. Beim Build des WPF-Hauptprojekts wird die TIA Bridge mitgebaut
und nach `TiaBridge` unterhalb des Anwendungsausgabeverzeichnisses kopiert.

Benötigte externe Abhängigkeiten:

1. Der interne NuGet-Feed mit `Grob.UX` muss in der Benutzer- oder Firmen-NuGet-Konfiguration verfügbar sein.
2. Die fe.screen-sim-SDK-DLLs werden standardmäßig unter
   `C:\Program Files\fe.screen-sim V5\5.0.11.48415` erwartet. Ein anderer Installationspfad kann über
   `FEE_SCREEN_SIM_ROOT` gesetzt werden.
3. Für einen TIA-End-to-End-Test muss TIA Portal inklusive Public API/Openness installiert sein und der
   ausführende Windows-Benutzer die erforderlichen Siemens-Berechtigungen besitzen.

Die unabhängig testbaren Komponenten lassen sich so prüfen:

```powershell
dotnet build Tests/CoreSmokeTests/VIBN_Tools.Core.SmokeTests.csproj --configuration Release --no-restore -warnaserror
dotnet run --project Tests/CoreSmokeTests/VIBN_Tools.Core.SmokeTests.csproj --configuration Release --no-build
dotnet build VIBN_Tools.TiaBridge/VIBN_Tools.TiaBridge.csproj --configuration Release --no-restore -warnaserror
```

## Noch bewusst ausstehend

- End-to-End-Tests gegen die echten ViCo-Netzpfade, fe.screen-sim und eine laufende TIA-Instanz.
- Bedien- und Layoutprüfung auf einem produktiven Desktop mit Outlook und mehreren Monitoren.
- Sicherheitsreview als eigener letzter Schritt, wie vereinbart.

Die beiden bestehenden geheimen Werte aus dem ViCo-Referenzcode wurden nicht erneut in Quellcode kopiert.
Bis zum Sicherheitsreview werden sie lokal über `VIBN_VICO_KANBANIZE_API_KEY` und
`VIBN_VICO_LICENSE_KEY` bereitgestellt.
