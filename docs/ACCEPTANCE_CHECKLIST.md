# Release acceptance checklist

Run this checklist on a GROB desktop with VPN/network access, fe.screen-sim, Outlook and at least one supported TIA Portal version.

## Automated baseline

- Release build of `VIBN_Tools_App.sln` succeeds.
- Core smoke tests pass with warnings treated as errors.
- UI startup smoke test initializes every integrated WPF view without a XAML or binding exception.

## Project Settings

- The PC dropdown contains exactly `localhost` plus the PCs visible in ViCo search.
- Updating Kanbanize changes both lists without restarting.
- FEE connect and disconnect work for localhost and one remote PC.
- A failed connection creates a readable Project Settings log entry.

## ViCo search and remote access

- Project and PC searches find known cards.
- TIA, Beckhoff and Rockwell cards appear in the Software column.
- Robot count, robot names and robot status match Kanbanize.
- `[B]`, `[P]`, `[W]` and `[D]` are displayed with the documented meaning.
- Remote Desktop uses the Kanbanize user and starts without another workflow step.
- All project-path buttons open the expected directories.
- Belegung ist bei Frei grün und bei Belegt rot; der Online-Ping bleibt separat grün/rot.

## Kanbanize

- **Prüfen** im VIBN→Arbeitsplätze-Reiter erzeugt keine Karte und zeigt neue, unveränderte, Deadline- und Konfliktfälle korrekt.
- Eine fehlende VIBN-Karte erzeugt genau eine verknüpfte Zielkarte mit Quell-ID als `custom_id`.
- Ein wiederholter Lauf erzeugt keine Duplikate.
- Bei einer abweichenden Deadline wird nur die Deadline der eindeutigen Zielkarte angepasst.
- Mehrdeutige `custom_id`-Zuordnungen bleiben unverändert und werden als Konflikt angezeigt.
- Eine manuell erstellte Karte im zweiten Kanbanize-Reiter funktioniert unabhängig von der Synchronisierung.

## Transfer and TIA

- A representative project copy completes and preserves its directory structure.
- TIA Bridge connects to every supported installed version.
- Library import, export and axis generation complete on a disposable test project.

## Administration

- The current Windows user and expected license level are shown.
- A Level8/Level9 user can change a disposable test entry and reload it.
- Outlook meetings and latest update load when their sources are available.
- Missing network access is shown as a diagnostic error instead of an empty unexplained page.

Record application version, machine, operator, date and deviations before release approval.
