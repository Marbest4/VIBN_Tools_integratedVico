# Architecture and data sources

## Boundaries

- `Application`: WPF views, view models, composition root and application log.
- `VIBN_Tools.Core`: platform-neutral ViCo/Kanbanize models, policies and interfaces.
- `VIBN_Tools.Infrastructure`: filesystem, cache, HTTP, Windows RDP/session and JSON role adapters.
- `VIBN_Tools.Tia.Contracts`: serializable protocol DTOs.
- `VIBN_Tools.Tia.Client`: typed named-pipe client.
- `VIBN_Tools.TiaBridge`: isolated Siemens Openness process.

## Source of truth matrix

| Data | Source | Rule |
| --- | --- | --- |
| Workstations and user assignment | Kanbanize workstation cache | `KONFIGURATION / USER` overrides older card text |
| Workstation configuration | `KONFIGURATION` card and card-level subtasks endpoint | update existing standard subtasks; explicitly create missing subtask/card |
| Online state | bounded ICMP ping | offline suppresses remote/path actions |
| Remote session / last logon | read-only `quser` | lack of permission means “Not available”, not offline |
| Workplace card schedule | VIBN source + single VIBN template deadline | source −14 days, template +56 days |
| Authorization | central `roles.json` | `lutzma` is Level9; at least two Level9 users on save |
| TIA hardware | selected PLC via Openness | read-only module and byte address data before FEE creation |

## Reliability and performance

- Core policies are testable without live services.
- Cache files and role files are written atomically.
- Workstation ping and remote-session queries have separate bounded concurrency.
- Kanbanize synchronization is idempotent through source `custom_id` and uses narrow payloads.
- TIA stays outside the WPF process and bridge failures are caught at view-model boundaries.
- WPF grids use virtualization and deferred tab templates are covered by a UI startup test.

## Remote Desktop credential boundary

The `.rdp` profile contains only host, Kanbanize-selected user, monitor selection and prompt mode. The automatic action reads `VIBN_RDP_PASSWORD` from the signed-in user's environment, creates `TERMSRV/<host>` through `cmdkey`, launches `mstsc`, and removes that entry after 20 seconds. The password is never part of source, cache, role data, RDP file or Kanbanize payloads. The prompted action does not create a credential entry.
