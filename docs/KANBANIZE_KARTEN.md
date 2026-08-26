# Kanbanize Karten

## Zwei getrennte Funktionen

Der Hauptreiter **Kanbanize Karten** enthält bewusst zwei voneinander unabhängige Abläufe:

1. **VIBN → Arbeitsplätze** liest virtuelle Inbetriebnahme-Karten und erstellt bzw. aktualisiert ihre eindeutig verknüpfte Arbeitsplatzkarte.
2. **Eigene Karte** erstellt auf ausdrücklichen Benutzerwunsch eine normale Kanbanize-Karte.

Beide Abläufe enthalten keine Lizenzanfrage- oder Lizenzdatenlogik.

## VIBN → Arbeitsplätze

### Voraussetzungen

- API-Zugriff kann Boards, Lanes, Spalten und Karten lesen.
- Im Zielboard darf der API-Zugriff Karten erstellen und die beiden geplanten Terminwerte bestehender generierter Karten ändern.
- Quellboard, Zielboard, Ziel-Lane und Zielspalte müssen gewählt sein.

### Bedienablauf

1. **Boards aktualisieren** drücken.
2. Quellboard „Virtuelle Inbetriebnahme“ sowie Zielboard „Arbeitsplätze“ und die gewünschte Zielposition auswählen.
3. **Prüfen** drücken und jede Zeile der Vorschau lesen.
4. Ausschließlich die gewünschten Zeilen in der Spalte **Sync** markieren.
5. Nur wenn Vorschau und Zielposition fachlich korrekt sind, **Synchronisieren** drücken. Nicht markierte Zeilen werden garantiert nicht geschrieben.

### Auswahlregel

Eine Quellkarte ist zulässig, wenn ihr Titel `Grundinbetriebnahme` enthält, sie nicht `Vorlage` heißt und nicht in der Archivspalte liegt. Eine zusätzliche Vorlagenkarte ist für die Synchronisierung nicht erforderlich.

### Terminregel

| Zielwert | Berechnung |
| --- | --- |
| Start | Deadline der konkreten Quellkarte minus 14 Tage |
| Ende/Deadline | Deadline derselben VIBN-Quellkarte plus 56 Tage |

Der Start wird im bestehenden Startdatums-Custom-Field des Arbeitsplätze-Boards (Feld-ID `508`) gespeichert. Das Ende wird als reguläre Kanbanize-Deadline gespeichert. `actual_end_time` wird ausdrücklich nicht geschrieben, weil es einen tatsächlichen Abschluss statt eines Plantermins beschreibt.

Fehlt die Deadline einer Quellkarte, zeigt die Vorschau für genau diese Karte einen Konflikt. Andere gültige, markierte Karten können weiterhin synchronisiert werden.

### Duplikat- und Änderungsregel

Die Zielkarte speichert die Quellkarten-ID als `custom_id` und Parent-Link. Dadurch erkennt ein zweiter Lauf zuverlässig dieselbe Karte.

| Situation | Verhalten |
| --- | --- |
| keine Zielkarte mit Quell-ID | neue verknüpfte Zielkarte an ausgewählter Zielposition erstellen |
| genau eine Zielkarte mit abweichendem Zeitplan | nur Startdatumsfeld und Deadline patchen |
| genau eine Zielkarte mit gleichem Zeitplan | unverändert |
| mehrere Zielkarten mit gleicher Quell-ID | Konflikt, keinerlei Änderung |
| fehlende Quell-Deadline | Konflikt für diese Quellkarte, keinerlei Änderung |

Die Automatik verschiebt, löscht, benennt, beschreibt oder priorisiert keine vorhandene Karte. Jede Einzelausnahme wird in der Vorschau und im Diagnoseprotokoll sichtbar.

## Eigene Karte

Im Reiter **Eigene Karte** kann der Benutzer Board, Lane, Spalte, Titel, Beschreibung, Priorität, externe ID und Deadline wählen. Das Arbeitsplätze-Board, die Lane **Angelegt** und die Spalte **Backlog** werden – sofern über den API-Benutzer erreichbar – als Standard vorausgewählt. Der Entwurf wird vor dem HTTP-Aufruf validiert. Die manuelle Erstellung verwendet keine VIBN-ID und beeinflusst die Synchronisierung nicht.

## Tests und Codewegweiser

| Bereich | Datei |
| --- | --- |
| Modelle und HTTP-Vertrag | `VIBN_Tools.Core/Kanbanize/CardCreation.cs` |
| Auswahl-, Termin- und Duplikatregel | `VIBN_Tools.Core/Kanbanize/VibnWorkplaceSynchronization.cs` |
| Kanbanize-v2-Adapter | `VIBN_Tools.Infrastructure/Kanbanize/KanbanizeCardApiService.cs` |
| WPF-Vorschau und Status | `Application/VM/VibnWorkplaceSynchronizationVM.cs` |
| Kartenreiter | `Application/View/KanbanizeCardPage.xaml` |
| Fach- und Payloadtests | `Tests/CoreSmokeTests/Program.cs` – `VerifyVibnWorkplaceSynchronizationAsync` und `VerifyKanbanizeHttpWriteScopeAsync` |
