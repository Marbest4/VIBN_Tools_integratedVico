# User guide

## Project Settings

The PC dropdown and the ViCo PC search use the same dynamic workstation directory. It is populated from the latest Kanbanize cache when the application starts and refreshed after every online Kanbanize update. `localhost` remains available for local FEE work.

Select a PC, choose whether FEE data should be loaded, then select **Connect**. The status appears directly below the controls. Detailed failures are available in the global **Diagnoseprotokoll** at the bottom of the application.

## ViCo PC and project search

Use **Projekt** to find the workstation belonging to a project, or **PC** to search by workstation name. The result table shows:

- project status: `[B]` Backlog, `[P]` Planning, `[W]` Working, `[D]` Done;
- workstation and the Remote Desktop user from the Kanbanize card;
- projects assigned to the workstation;
- automation software stated in Kanbanize: TIA Portal, Beckhoff TwinCAT and/or Rockwell Studio 5000;
- FEE and LAN/hardware information;
- the number of matching robot software cards and their names/status in the tooltip;
- cached live availability of the workstation.

Software is labelled as **installed** only if the source card explicitly says so. Otherwise it is shown as **laut Kanbanize angegeben**.

Select a row and project to open Remote Desktop, TeamViewer, the workstation project share, simulation project, PLC project or planning directory. The Kanbanize user has priority for Remote Desktop.

## Projects, transfer and TIA Portal

**Projekte & Favoriten** locates simulation projects and stores compatible ViCo favorites. **Transfer** copies selected project items with bounded parallelism. **TIA Portal** communicates with the separate TIA Bridge process and preserves the original import/export and axis-library workflows.

## Administration

The page displays the current Windows identity, recognized ViCo level, today's Outlook meetings and the latest available update. License levels 8 and 9 can update license entries. If administration is unavailable, the page distinguishes an unrecognized user from an inaccessible network path or incompatible license data; details are written to the diagnosis log.

## Diagnosis log

Expand **Diagnoseprotokoll** at the bottom of any page. It retains the latest 500 entries in memory and offers copy, clear and log-folder actions. File logs rotate daily and retain 14 archives. Passwords, API keys and license keys must not be logged.
