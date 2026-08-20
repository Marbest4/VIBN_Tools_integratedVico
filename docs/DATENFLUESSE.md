# Datenflüsse

## PC- und Benutzerbestand

```mermaid
flowchart LR
    K["Kanbanize Boards 1541 / 846"] --> R["KanbanizeRefreshService"]
    R --> C["atomare Cachedateien auf Server"]
    C --> L["LegacyWorkstationCatalog"]
    L --> D["WorkstationDirectory"]
    D --> S["Project Settings Dropdown"]
    D --> V["ViCo PC-/Projektsuche"]
```

Beim Start wird der vorhandene Cache gelesen. Eine Online-Aktualisierung lädt PC- und Robotikdaten, ersetzt die Cachedateien und synchronisiert danach das gemeinsame Verzeichnis. Kanbanize ist bei widersprüchlichen Benutzern die maßgebliche Quelle.

## Remote-Verbindung

```mermaid
sequenceDiagram
    participant U as Benutzer
    participant VM as ViCoSearchPageVM
    participant C as WorkstationDirectory/Kanbanize
    participant R as WindowsRemoteDesktopService
    participant W as Windows RDP
    U->>VM: PC/Projekt auswählen und Remote starten
    VM->>C: priorisierten PC-Benutzer übernehmen
    VM->>R: PC, Benutzer und Monitore
    R->>R: RDP-Profil erstellen / Credentials bereitstellen
    R->>W: mstsc starten
```

## TIA-Operation

```mermaid
sequenceDiagram
    participant UI as TiaPortalPageVM
    participant Client as NamedPipeTiaBridgeClient
    participant Bridge as TiaBridgeServer
    participant TIA as TiaOpennessSession
    UI->>Client: typisierter Befehl
    Client->>Bridge: JSON Request über Named Pipe
    Bridge->>TIA: versionsgebundene Openness-Operation
    TIA-->>Bridge: Ergebnis oder Fehler
    Bridge-->>Client: JSON Response
    Client-->>UI: DTO oder TiaBridgeException
```

## Lizenzänderung

```mermaid
flowchart TD
    A["Ausgewählter Benutzer + Ziellevel"] --> P["LicenseAdministrationPolicy.PlanChange"]
    E["Optionaler Ersatzbenutzer"] --> P
    P --> Q{"Danach mindestens 2 eindeutige Level9?"}
    Q -- Nein --> X["Abbruch + Status + Diagnoseprotokoll"]
    Q -- Ja --> H["Ersatz zuerst auf Level9 schreiben"]
    H --> Z["gewählte Änderung schreiben"]
    Z --> N["Lizenzbestand neu laden"]
```

## Diagnose

ViewModels melden bedienbare Texte an `IApplicationLog`. `ApplicationLogService` hält höchstens 500 sichtbare Einträge und leitet dieselben Ereignisse an NLog weiter. Dadurch bleibt die Oberfläche begrenzt, während rotierende Dateien eine spätere Analyse erlauben.
