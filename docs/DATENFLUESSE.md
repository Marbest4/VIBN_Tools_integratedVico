# Datenflüsse

## Arbeitsplatzbestand und Konfiguration

```mermaid
flowchart LR
    K[Kanbanize Arbeitsplätze-Board] --> R[KanbanizeRefreshService]
    R --> C[atomare Cachedateien]
    R --> S[WorkstationBoardCache.json]
    C --> P[LegacyWorkstationCatalog]
    S --> P
    P --> D[WorkstationDirectory]
    P --> V[ViCoSearchPageVM]
    D --> PS[Project Settings Dropdown]
```

`WorkstationBoardCache.json` bewahrt Karten- und Unteraufgaben-IDs der `KONFIGURATION`-Karte. `LegacyWorkstationCatalog` verbindet sie über die Lane mit dem Arbeitsplatz. Der Wert `USER:` hat Vorrang vor älteren Textkarten; damit nutzen ViCo und Project Settings dieselbe dynamische PC-Benutzer-Zuordnung.

Beim Speichern der Konfiguration läuft der Datenfluss nur in die Gegenrichtung der vorhandenen Unteraufgabe:

```mermaid
sequenceDiagram
    participant U as Benutzer
    participant VM as ViCoSearchPageVM
    participant C as Konfigurationsadapter
    participant K as Kanbanize
    U->>VM: Wert ändern und Speichern
    VM->>VM: geänderte und fehlende Standardfelder auswählen
    VM->>C: Karte + Subtask-ID + KEY: Wert
    C->>K: PATCH /cards/{card}/subtasks/{subtask}
    opt Standard-Unteraufgabe fehlt
        C->>K: POST /cards/{card}/subtasks
    end
    K-->>C: Erfolg/Fehler
    C-->>VM: Ergebnis
```

Andere normale Kartenfelder werden nie geschrieben. Fehlt die komplette Karte, läuft ein separater ausdrücklicher Ablauf über `POST /cards` mit Titel `KONFIGURATION` und anschließend fünf Standard-Unteraufgaben.

## Remote Desktop und Sitzungsauskunft

```mermaid
sequenceDiagram
    participant U as Benutzer
    participant VM as ViCoSearchPageVM
    participant N as NetworkAvailabilityService
    participant Q as WindowsRemoteSessionService
    participant R as WindowsRemoteDesktopService
    U->>VM: Arbeitsplatz auswählen
    VM->>N: Ping, begrenzt parallel
    alt online
        VM->>Q: quser /server, read-only
        Q-->>VM: Sitzung oder Nicht abrufbar
        U->>VM: Remote Desktop
        VM->>R: automatische Anmeldung
    else offline
        VM-->>U: Aktionen ausgeblendet
    end
```

Die alternative Schaltfläche „RDP mit Anmeldedaten“ ruft denselben RDP-Adapter mit `prompt for credentials:i:1` ohne temporären Eintrag auf. Der normale Start liest `VIBN_RDP_PASSWORD`, erzeugt `TERMSRV/<PC>` unmittelbar vor `mstsc` und löscht den Eintrag nach 20 Sekunden. Das Kennwort landet nie im `.rdp`-Profil, Cache oder Log.

## Kanbanize VIBN → Arbeitsplätze

```mermaid
flowchart TD
    S[VIBN-Grundinbetriebnahme-Karte] --> V{zulässig?}
    V -- nein --> X[ausgeschlossen]
    V -- ja --> F[Terminformel je Quellkarte]
    F --> M{Zielkarte mit custom_id?}
    M -- keine --> C[POST neue verknüpfte Karte]
    M -- genau eine --> D{Start und Deadline gleich?}
    D -- nein --> P[PATCH nur Startfeld 508 + Deadline]
    D -- ja --> U[unverändert]
    M -- mehrere --> K[Konflikt, keine Änderung]
```

Die Formel ist Start = Quell-Deadline − 14 Tage, Ende = Deadline derselben Quellkarte + 56 Tage. Eine fehlende Quell-Deadline oder mehrere passende Zielkarten sind Konflikte ohne Schreiboperation.

## TIA-Hardware und Special Devices

```mermaid
flowchart LR
    T[TIA Portal / Openness] --> B[TiaBridge]
    B --> P[Named-Pipe Client]
    P --> H[TiaHardwareModuleInfo]
    H --> R[TiaHardwareDeviceRowVM]
    R --> Q[Benutzer prüft Logik + E/A-Bytes]
    Q --> W[Special-Device-Warteschlange]
    W --> F[serielle FEE-Erzeugung]
```

Bis zur letzten Aktion ist der Ablauf read-only. Die TIA-Bridge liest Modul, Typ, Slot und Eingangs-/Ausgangsbyte. Erst das bestätigte Erzeugen verändert die FEE-Simulation.

## Rollen

```mermaid
flowchart LR
    J[Windows-Benutzer] --> R[roles.json]
    R --> P[ViCoRolePolicy]
    P --> M[MainWindowVM]
    P --> V[ViCoWorkspacePageVM]
    P --> A[ViCoAdministrationPageVM]
```

Die gleiche Policy regelt Hauptreiter, Verwaltungsreiter und Schreibrecht. Beim Speichern validiert sie `lutzma` als Level9 und mindestens zwei unterschiedliche Level9-Benutzer.
