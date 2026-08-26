# Konfiguration, Betrieb und Fehlersuche

## Voraussetzungen

- Windows-Desktop mit .NET 8 Runtime;
- Grob.UX und das kompatible FEE-/fe.screen-sim-SDK;
- Zugriff auf die vorgesehenen UNC-Pfade für ViCo-Caches, Projekte, Rollen und Versionen;
- Kanbanize-/Businessmap-Zugriff für Live-Aktualisierung und Kartenfunktionen;
- für Live-TIA: passende lokale Siemens-TIA-Portal-/Openness-Installation und Berechtigung;
- optional Outlook für Termine im Verwaltungsreiter.

Ohne Unternehmensnetz startet die Oberfläche weiterhin. Live-Daten, Kartenaktionen, Netzpfade oder TIA-Operationen können dann erwartbar nicht verfügbar sein und werden protokolliert.

## Zentrale Konfigurationsstellen

| Wert | Ort | Zweck |
| --- | --- | --- |
| Kanbanize API-Schlüssel | `VIBN_VICO_KANBANIZE_API_KEY` | bevorzugter Live-Zugang für ViCo und Kartenreiter |
| RDP-Kennwort | `VIBN_RDP_PASSWORD` | lokale Benutzervariable für den kurzlebigen `TERMSRV/<PC>`-Eintrag |
| Rollen-Datei | `VIBN_VICO_ROLES_FILE` | optionaler zentraler Pfad zu `roles.json` |
| ViCo-Pfade | `VIBN_Tools.Infrastructure/ViCo/ViCoPathsOptions.cs` | Caches, Projekte, Versionen und Standard-Arbeitsordner |
| TIA-Bridge | `Application/ViCoFeatureBootstrapper.cs` | Bridge-Executable, Pipe pro Prozess, lokale Versionserkennung |
| Logging | `ApplicationLogService` / vorhandene Log-Konfiguration | sichtbares Diagnosepanel und Logdatei |

API-Schlüssel und Kennwörter gehören nicht in Quellcode, Screenshots, Tickets oder das Diagnoseprotokoll.

Einmalige Einrichtung für den aktuell angemeldeten Windows-Benutzer (Platzhalter ersetzen, danach das Tool neu starten):

```powershell
[Environment]::SetEnvironmentVariable('VIBN_VICO_KANBANIZE_API_KEY', '<BUSINESSMAP-API-KEY>', 'User')
[Environment]::SetEnvironmentVariable('VIBN_RDP_PASSWORD', '<REMOTE-PASSWORT>', 'User')
```

Alternativ kann `Configure-VIBN-Tools.cmd` im Projektstamm gestartet werden.
Der Assistent fragt beide Werte verdeckt ab und speichert sie für den aktuellen
Windows-Benutzer. Er enthält selbst weder API-Key noch Kennwort. Danach VIBN
Tools und gegebenenfalls Visual Studio vollständig neu starten.

Kanbanize/Businessmap verwendet hier keinen Benutzerpasswort-Login, sondern den API-Key im Header `apikey`. Ein abgelaufener, rotierter oder für das Board nicht berechtigter Key führt zu 401/403; eine 400-Feldvalidierung ist dagegen ein Abfragefehler. Der Refresh wiederholt nur sichere GET-Anfragen bei Netzwerk-, 408-, 429- und 5xx-Fehlern.

## Datenquellen und Aktualisierung

| Information | Quelle | Aktualisierung |
| --- | --- | --- |
| PCs, Benutzer, Projekte, Software und KONFIGURATION | Arbeitsplätze-Board / strukturierter Cache | Start, Daten aktualisieren und periodisch |
| Robotik-Informationen | Robotik-Board / Cache | Start, Daten aktualisieren und periodisch |
| Project-Settings-Dropdown | gemeinsames `WorkstationDirectory` plus Ping | beim Start, Filter und manueller Aktualisierung |
| Rollen | `roles.json` | Anwendungstart und Verwaltungs-Refresh |
| Kartenpositionen/Karten | Kanbanize v2 | ausdrücklich durch Kartenreiter |
| TIA-Versionen | lokale Siemens-PublicAPI-Pfade | beim Öffnen der TIA-Ansicht |

## Häufige Fehler

### PC fehlt im Project-Settings-Dropdown

1. **Liste aktualisieren** drücken.
2. Filter löschen oder präzisieren.
3. Prüfen, ob der PC aktuell auf Ping antwortet – Offline-PCs erscheinen absichtlich nicht.
4. In ViCo **Daten aktualisieren** drücken und das Diagnoseprotokoll auf Cache-/Kanbanize-Fehler prüfen.

### Connect zeigt trotzdem nicht „verbunden“

Das ist korrekt, wenn FEE die Verbindung nicht bestätigt. Die Anwendung setzt `Connected to` erst nach `WaitForConnectedAsync`. Status-/Logmeldung prüfen, Servernamen und FEE-Service kontrollieren und danach erneut verbinden.

### RDP-Sitzung steht auf „Nicht abrufbar“

Der PC kann trotzdem online sein. Der aktuell angemeldete Benutzer darf die Remote-Terminalsitzungen nicht abfragen oder `quser` erreicht den Zielcomputer nicht. Mit einem Konto mit ausreichender administrativer Berechtigung starten oder die Remote-Abfrageberechtigung prüfen. Der RDP-Start selbst bleibt davon unabhängig.

Ein lokaler oder Domänen-Administrator funktioniert nur, wenn dieses Konto auch auf dem Ziel-PC autorisiert ist und Remote Desktop Services/RPC durch die Firewall erreichbar sind. Es gibt keinen sicheren Workaround, der diese Rechte umgeht. „Letzte Anmeldung“ bezeichnet die jüngste von `quser` noch gelistete aktive/getrennte Terminalsitzung; bereits abgemeldete Sitzungen sind ohne Zugriff auf das Windows-Sicherheitsereignisprotokoll nicht verlässlich bestimmbar.

### Remote Desktop verwendet einen falschen Benutzer

Die `USER:`-Unteraufgabe der `KONFIGURATION`-Karte hat Vorrang. Der normale Remote-Button erzeugt `TERMSRV/<PC-Name>` mit diesem Benutzer nur für den Start und entfernt den Eintrag nach 20 Sekunden; der zweite Button zeigt den Windows-Anmeldedialog.

### Automatische Remote-Anmeldung ist noch nicht eingerichtet

Die Benutzervariable `VIBN_RDP_PASSWORD` fehlt oder ist leer. Sie mit dem oben dokumentierten PowerShell-Befehl setzen und das Tool neu starten. Der separate Dialog-Button funktioniert ohne diese Variable.

### Konfigurationswerte lassen sich nicht speichern

Vorhandene Standard-Unteraufgaben werden per PATCH gespeichert; fehlende Standard-Unteraufgaben werden per POST an `/cards/{card}/subtasks` ergänzt. Fehlt die gesamte Karte, kann sie nur über **Standardkarte anlegen** bewusst erzeugt werden.

### Kanbanize meldet 400 bei `fields`

`subtasks`, Positionsfelder und Custom Fields sind in dieser Instanz keine zulässigen Werte des Kartenparameters `fields`. Die Arbeitsplatzabfrage verzichtet deshalb auf `fields`, paginiert über alle Seiten und verwendet `expand=subtasks`. Für jede gefundene KONFIGURATION-Karte wird zusätzlich der autoritative Endpunkt `/cards/{card_id}/subtasks` gelesen und mit der Expansion zusammengeführt. Dadurch werden auch direkt in der Businessmap-Oberfläche erzeugte oder nur teilweise expandierte Unteraufgaben übernommen.

### Kanbanize-Vorschau zeigt Konflikt

Keinen Synchronisieren-Lauf erzwingen. Prüfen, ob die betroffene Quellkarte eine Deadline hat und nicht mehrere Zielkarten dieselbe Quell-ID oder denselben generierten Titel tragen. Eine separate Vorlagenkarte ist nicht erforderlich; Konflikte führen bewusst zu keiner Änderung.

### TIA Bridge verbindet sich nicht oder Hardware bleibt leer

1. Exakt passende TIA-Version auswählen und das Projekt vollständig öffnen. Openness arbeitet nicht im Versions-Kompatibilitätsmodus.
2. Bei mehreren TIA-Fenstern nur das gewünschte Projekt geöffnet lassen. Die Bridge priorisiert `ProjectPath`, wartet bei großen Projekten bis zu 90 Sekunden auf die Openness-Projektfreigabe und unterstützt sowohl `Projects` als auch eine bereits geöffnete `LocalSessions[n].Project`-Sitzung.
3. Gruppe `Siemens TIA Openness`, TIA-Funktionsrecht **Edit project via Openness API**, Firewallfreigabe und installierte Optionen/HSPs prüfen.
4. Im Diagnosepanel die Bridge-Fehler lesen.
5. Für Special Devices Gerätename/Gerätekopf, Modul/Typ, Firmware, E-/A-Adressbereich, Byte-Längen und Logik kontrollieren. Die Hardwareabfrage durchläuft alle `Project.Devices` und liest `DeviceItems` sowie `Addresses` auch über deren explizite Openness-Schnittstellen, weil dezentrale PROFINET-/PROFIBUS-Geräte nicht unterhalb des PLC-Racks liegen. Bei Modulen, die keine Openness-Adresse bereitstellen, fehlende Adressen manuell ergänzen.
6. Die Bridge läuft als 64-Bit-fähiger .NET-Framework-Prozess. Nach einer Änderung der lokalen Gruppe `Siemens TIA Openness` Windows ab- und wieder anmelden; ein bloßer Neustart des Tools aktualisiert das Windows-Gruppentoken nicht.

Unterstützt werden lokal erkannte PublicAPI-Installationen V15 bis V22. Es wird immer die Assembly der ausgewählten Version geladen; V20 verwendet ausschließlich `Portal V20/PublicAPI/V20/Siemens.Engineering.dll`.

### XamlParseException oder Binding-Fehler

Nicht mit einem erneuten Schreibvorgang fortfahren. Status/Stacktrace sichern, den WPF-UI-Smoke-Test ausführen und die betroffene View/Property in der [Klassenreferenz](KLASSENREFERENZ.md) nachschlagen. Die integrierten Views sind auf schreibgeschützte Anzeige-Bindings abgesichert.

## Performance-Leitplanken

- Tabellen verwenden Zeilen- und Spaltenvirtualisierung.
- PC-Pings sind auf acht, RDP-Sitzungsabfragen auf vier parallele Abfragen begrenzt.
- Such- und Filtereingaben werden entprellt; ein neuer Filter bricht die vorherige Prüfung ab.
- Kanbanize-Caches werden atomar geschrieben; die UI liest stabile Snapshots.
- Dateiübertragungen und FEE-Geräteerzeugungen sind begrenzt/serialisiert.
- Keine rekursiven Netzwerkscans oder TIA-/Outlook-Aufrufe im UI-Thread ergänzen.

## Warnungsstrategie

Der historische WPF-Bestand wurde vor Nullable-Referenztypen entwickelt. Im Hauptprojekt gilt deshalb `Nullable=annotations`; neue Integrationsprojekte bleiben `Nullable=enable`, Core und Infrastructure bauen Warnungen als Fehler. Externe `MSB3277`-Hinweise entstehen durch Versionsunterschiede zwischen geliefertem FEE-SDK und .NET 8 und dürfen nur durch ein abgestimmtes SDK-Upgrade, nicht durch blindes Suppressieren, beseitigt werden.

## Veröffentlichung

1. Release-Build ausführen.
2. Core- und UI-Smoke-Tests ausführen.
3. [ACCEPTANCE_CHECKLIST.md](ACCEPTANCE_CHECKLIST.md) auf einem echten GROB-Desktop abarbeiten.
4. Keine Cachedateien, Rollen-Dateien mit Realbenutzern, API-Schlüssel oder RDP-Credentials einchecken.
