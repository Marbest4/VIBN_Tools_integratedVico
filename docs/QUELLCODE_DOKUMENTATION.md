# Quellcode-Dokumentation und Kommentierregeln

## Was im Code dokumentiert ist

Neue und geänderte öffentliche Klassen, Interfaces, Fachregeln und nicht offensichtliche Abläufe besitzen XML-Kommentare oder kurze Inline-Kommentare. Die Kommentare beantworten insbesondere, warum eine Grenze, Reihenfolge oder asynchrone Behandlung notwendig ist.

| Bereich | Kommentierte Kernpunkte |
|---|---|
| `LicenseAdministrationPolicy` | Mindestbesetzung, festes Level9-Konto `lutzma`, effektive Lizenzstufe und sicherer Änderungsplan |
| `ViCoAdministrationPageVM` | persistente Hochstufung von `lutzma`, Schreibreihenfolge beim Levelwechsel |
| `ViCoWorkspacePageVM` | Sichtbarkeit des Verwaltungsreiters ab Level7 |
| `ViCoSearchPageVM` | einheitliche Aktualisierung, Cache-/Online-Verhalten, Online-Farblogik |
| `ProjectIdentity` | Priorität Belegt vor Frei bei gemischten Karten |
| `FeeConnectionService` | tatsächliche FEE-Verbindungsbestätigung nach `Connect` |
| `KanbanizeCardDraftPolicy` | validierbare Eingabegrenzen vor einem externen Schreibvorgang |
| `KanbanizeCardApiService` | v2-HTTP-Grenze, API-Schlüssel nur im Header, keine Lizenzlogik |
| `KanbanizeCardPageVM` | Abbruch, Workflow-Spaltenfilter, Reset nach erfolgreicher Erstellung |

## Lesereihenfolge für neue Entwickler

1. [Klassenreferenz](KLASSENREFERENZ.md) für Zuständigkeiten lesen.
2. View (`.xaml`) und ViewModel (`.cs`) gemeinsam betrachten.
3. Das verwendete Core-Interface suchen.
4. Die Infrastrukturimplementierung und `ViCoFeatureBootstrapper` prüfen.
5. Den zugehörigen Smoke-Test lesen.

## Konventionen

- XML-Kommentare stehen auf öffentlichen Verträgen und Fachregeln.
- Inline-Kommentare stehen nur an Stellen, deren Motivation aus dem Code nicht offensichtlich ist, etwa Cache-Fallback, Schreibreihenfolge oder Abbruchbehandlung.
- Keine Kommentare schreiben, die lediglich den Code wiederholen.
- Jeder externe Vorgang läuft über ein Interface und wird in Statuszeile sowie Diagnoseprotokoll nachvollziehbar.
- Neue Bindings zu schreibgeschützten Werten immer explizit `Mode=OneWay` setzen.

Die bestehende, historisch gewachsene VIBN-Funktionalität wurde bewusst nicht zeilenweise mit Redundanz-Kommentaren überzogen. Ihre Zuständigkeiten sind in der Klassenreferenz beschrieben; neue Umbauten sollen schrittweise in kleine Dienste mit Tests überführt werden.
