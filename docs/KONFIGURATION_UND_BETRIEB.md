# Konfiguration, Betrieb und Fehlersuche

## Voraussetzungen

- Windows-Desktop mit .NET 8 SDK bzw. passender Desktop Runtime;
- erreichbare Grob.UX-NuGet-Quelle;
- fe.screen-sim V5 SDK;
- für Live-TIA: installierte passende TIA-Portal-/Openness-Version und Benutzerberechtigung;
- für Live-ViCo: Zugriff auf die konfigurierten Unternehmensnetzpfade, Kanbanize und gegebenenfalls Outlook.

Ohne Unternehmensnetz können lokale Oberfläche und Tests funktionieren, während Projektkatalog, Lizenz, Update oder Kanbanize-Daten erwartbar nicht verfügbar sind.

## Konfigurationsstellen

| Einstellung | Ort | Bedeutung |
|---|---|---|
| `FEE_SCREEN_SIM_ROOT` | Umgebungsvariable / `VIBN_Tools.csproj` | Installationswurzel des fe.screen-sim SDK; Standard ist die im Projekt angegebene V5-Version |
| `VIBN_VICO_KANBANIZE_API_KEY` | Umgebungsvariable | bevorzugter API-Schlüssel für ViCo-Online-Aktualisierung und Kanbanize-Karten |
| `VIBN_VICO_LICENSE_KEY` | Umgebungsvariable | überschreibt den Schlüssel des kompatiblen Altformats |
| Projekt-, Cache-, Lizenz-, Versionspfade | `VIBN_Tools.Infrastructure/ViCo/ViCoPathsOptions.cs` | zentrale ViCo-Verzeichnisse |
| TIA Bridge | `Application/ViCoFeatureBootstrapper.cs` | Executable-Unterordner, Pipe pro Hauptprozess, lokale TIA-Erkennung |
| Logging | `NLog.config` | Dateiname, Level, Ausgabeziele und 14 Archive |
| Kopierparallelität | `ViCoPathsOptions.MaximumParallelCopies` | Anzahl gleichzeitiger Transfers; Standard 2 |

Schlüssel nicht in neue Konfigurationsdateien im Repository schreiben. Die vorhandenen Kompatibilitäts-Fallbacks werden erst in der geplanten Sicherheitsmigration entfernt.

## Verzeichnisse zur Laufzeit

| Inhalt | Standard |
|---|---|
| Buildausgabe | `artifacts/build/<Konfiguration>/net8.0-windows/` |
| ViCo-Arbeitsdateien und RDP-Profile | `%LOCALAPPDATA%\GROB\VIBN_Tools\ViCo` |
| Diagnose-Dateien | `%LOCALAPPDATA%\GROB\VIBN_Tools\Logs` |
| Favoriten | kompatibler Pfad aus `ViCoPathsOptions.FavoritesFile` |
| PC-/Robotik-Cache | `ViCoPathsOptions.ServerCacheRoot` |

Die konkreten UNC-Standardwerte stehen nur in `ViCoPathsOptions.cs`, damit sie nicht über mehrere ViewModels verteilt werden.

## Datenquellen und Aktualität

| Daten | Quelle | Aktualisierung |
|---|---|---|
| PCs, Benutzer, Projekte, FEE, Hardware, Software | Kanbanize Board 1541 über Cache | beim Start aus Cache; online manuell/periodisch |
| Roboternamen/-status | Kanbanize Board 846 über Cache | online manuell/periodisch |
| Erreichbarkeit | ICMP-Ping | verzögert nach Suche, 30 Sekunden Cache, höchstens 8 parallel |
| Simulations-/SPS-/Planungspfade | konfigurierte Verzeichniswurzeln | beim Erzeugen/Aktualisieren des Resolvers |
| Lizenzen | kompatible verschlüsselte Dateien | beim Öffnen/Aktualisieren der Verwaltung |
| Termine | lokales Outlook-Profil | Verwaltung aktualisieren |
| TIA-Versionen | lokale Siemens-PublicAPI-Verzeichnisse | ViewModel-Erzeugung |
| Kartenpositionen / Kartenerstellung | Kanbanize v2 API | beim Öffnen/Aktualisieren des Kartenreiters bzw. auf Benutzeraktion |
| VIBN→Arbeitsplätze-Abgleich | Kanbanize Boards der gewählten Quelle und des Ziels | nur nach **Prüfen** bzw. **Synchronisieren**, immer mit frischem Snapshot |

## Häufige Fehler

### PC fehlt im Project-Settings-Dropdown

1. In ViCo **Daten aktualisieren** ausführen.
2. Prüfen, ob die PC-Karte auf Board 1541 erwartungsgemäß aufgebaut ist.
3. Diagnose auf Kanbanize-/Cachefehler prüfen.
4. Prüfen, ob das Cacheverzeichnis erreichbar und beschreibbar ist.

Settings und ViCo Search nutzen dieselbe Quelle. Ein bleibender Unterschied deutet deshalb auf noch nicht synchronisierten UI-Zustand oder einen Parsingfehler hin, nicht auf eine zweite feste PC-Liste.

### Remote Desktop verwendet falschen Benutzer

Kanbanize-Karte und Ergebniszeile prüfen. Der normalisierte Benutzer aus dieser Karte muss priorisiert werden. Danach Diagnose und erzeugtes RDP-Profil prüfen, aber keine Kennwörter weitergeben.

### Projektaktion meldet XamlParseException

Den innersten `InnerException`-Text und den gebundenen Property-Namen erfassen. Bei schreibgeschützten ViewModel-Properties muss die XAML-Bindung `Mode=OneWay` verwenden. Die UI-Startup-/Interaktionstests ausführen, weil der WPF-Stack häufig nur `PropertyPathWorker` als äußere Fehlerstelle nennt.

### Connect oder Projektpfad funktioniert nicht

Nach **Connect** wird maximal 10 Sekunden auf den tatsächlichen FEE-Zustand `Connected` gewartet. Bei Zeitüberschreitung wird die Verbindung getrennt, `Connected to` bleibt `---` und die Statuszeile meldet den Fehler. PC-/Projektselektion, Netzverbindung und den betreffenden UNC-Pfad prüfen. Die Statuszeile ist für Anwender formuliert; das Diagnoseprotokoll enthält die technische Ursache.

### Kanbanize-Synchronisierung oder Karte kann nicht ausgeführt werden

Board, Lane, Spalte und Titel prüfen. Der API-Schlüssel muss für das Board die Berechtigung **Create Card** besitzen. Für die VIBN-Synchronisierung kommen Leserechte auf Quell- und Zielboard sowie die Berechtigung zum Aktualisieren der Deadline hinzu. Die Kartenfunktion teilt den ViCo-API-Schlüssel, hat aber keine Lizenzanfrage oder Lizenzabhängigkeit. Details stehen im [Kartenhandbuch](KANBANIZE_KARTEN.md).

### TIA Bridge verbindet sich nicht

1. passende TIA-Version ist lokal installiert;
2. `TiaBridge/VIBN_Tools.TiaBridge.exe` liegt neben der Buildausgabe;
3. Openness-Berechtigung des Windows-Benutzers ist vorhanden;
4. keine andere TIA-Instanz blockiert die Operation;
5. Bridge-Fehler im Diagnoseprotokoll prüfen.

### Lizenzänderung wird abgewiesen

- aktuelle Berechtigung muss mindestens Level8 sein;
- Benutzer und Ziellevel müssen ausgewählt sein;
- nach der Änderung müssen zwei eindeutige Level9-Benutzer bestehen;
- bei Ersatz das zusätzliche Level9-Konto auswählen;
- Lizenznetzpfad und Kompatibilitätsschlüssel müssen erreichbar bzw. korrekt sein.

## Performance-Leitplanken

- Ping: maximal 8 gleichzeitig, Suche 300 ms entprellt, Ergebnis 30 Sekunden gecacht;
- Kopieren: standardmäßig maximal 2 parallele Dateioperationen;
- Tabellen: Zeilen- und Spaltenvirtualisierung nicht deaktivieren;
- Cachedateien: atomarer Austausch über temporäre Datei;
- sichtbares Log: maximal 500 Einträge;
- keine rekursiven Netzwerkscans oder Outlook-/TIA-Aufrufe im UI-Thread ergänzen.

## Veröffentlichung

1. Restore und Release-Build ohne Fehler.
2. Core- und UI-Smoke-Tests erfolgreich.
3. [Release-Abnahme](ACCEPTANCE_CHECKLIST.md) in einer repräsentativen Unternehmensumgebung.
4. TIA-Versionen, UNC-Zugriff, Remote-Verbindung und mindestens ein vollständiger ViCo-Workflow prüfen.
5. Buildartefakte versionieren/paketieren; keine lokalen Cache-, Lizenz- oder Credential-Dateien einchecken.
