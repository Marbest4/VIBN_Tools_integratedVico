# Architecture and data sources

## Boundaries

- `VIBN_Tools.Core`: immutable ViCo models, interfaces, search, workstation directory and business rules.
- `VIBN_Tools.Infrastructure`: file-system, Kanbanize, Remote Desktop, Outlook, update and legacy-license adapters.
- `VIBN_Tools.Tia.Contracts`: typed process protocol.
- `VIBN_Tools.Tia.Client`: lifecycle and named-pipe client for the bridge.
- `VIBN_Tools.TiaBridge`: isolated .NET Framework process loading the selected TIA Openness version.
- `Application`: WPF view models, views, composition and the bounded in-memory diagnosis log.

The original VIBN Tools feature classes are not refactored as part of the ViCo integration. This keeps the migration regression surface bounded.

## Data source matrix

| Information | Source | Refresh | Meaning |
|---|---|---|---|
| PCs, users, projects, FEE, LAN, software | Kanbanize board 1541 cache | startup, manual and periodic | Values stated on workstation cards |
| Robot count, name and status | Kanbanize board 846 cache | manual and periodic | Unique matching software-robotics cards |
| Online state | ICMP ping | after a debounced search, cached for 30 s | Network response only |
| Simulation/PLC/planning paths | configured UNC roots and cache indexes | view refresh | Best matching project path |
| License level | compatible encrypted files on configured UNC roots | administration refresh | Existing ViCo authorization level |
| TIA installation | local Siemens PublicAPI directories | view creation | Installed local TIA Openness versions |

`WorkstationDirectory` is the single source of truth for the Project Settings dropdown and ViCo workstation users. Kanbanize values replace older assignments; there is no compiled PC-to-user table.

## Reliability and performance

- project and network access is asynchronous;
- copy concurrency is bounded;
- DataGrid row and column virtualization remains enabled;
- availability checks are limited to eight concurrent pings, delayed by 300 ms while typing and cached for 30 seconds;
- Kanbanize cache files are replaced atomically;
- the visible diagnosis buffer is capped at 500 records and file logs rotate.

## Known follow-up: security phase

The existing ViCo encrypted license format and one-click Remote Desktop credential workflow are retained for compatibility. They must be migrated in the dedicated security phase without changing user workflows. Kanbanize/API credentials, Remote Desktop passwords and license keys must be removed from source-controlled compatibility paths as part of that phase.
