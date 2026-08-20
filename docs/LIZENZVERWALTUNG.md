# Lizenzverwaltung

## Verhalten

Die Verwaltung liest genehmigte Lizenzdateien und Anfragen über `IViCoLicenseService`. Der aktuelle Windows-Benutzer wird normalisiert; `GROB\Max.Mustermann` und `max.mustermann` zählen deshalb als dasselbe Konto.

Level8 und Level9 dürfen in der aktuellen Kompatibilitätslogik Einträge verwalten. Unabhängig davon gilt die fachliche Invariante:

> Nach jeder gespeicherten Änderung müssen mindestens zwei unterschiedliche Benutzer Level9 besitzen.

Eine einzelne Person kann nicht durch unterschiedliche Schreibweisen doppelt gezählt werden. Eine unzulässige Änderung wird vor jedem Dateizugriff abgebrochen und im Diagnoseprotokoll vermerkt.

## Bedienung

- Zweiten Administrator hochstufen: Benutzer markieren, `Level9` wählen, speichern.
- Einen von genau zwei Level9-Benutzern herabstufen: Zielbenutzer und neues Level wählen, zusätzlich den Ersatz im Dropdown **Zusätzlicher Level9-Benutzer** auswählen, speichern.
- Der Ersatz wird zuerst hochgestuft. Erst danach wird der bisherige Benutzer herabgestuft. So erzeugt auch ein teilweiser Schreibfehler nicht absichtlich einen Zustand mit nur einem Level9-Benutzer.

## Wo wird die Mindestanzahl geändert?

Die Regel liegt zentral in:

```text
VIBN_Tools.Core/ViCo/Administration.cs
LicenseAdministrationPolicy.MinimumLevel9Users
```

Aktueller Wert:

```csharp
public const int MinimumLevel9Users = 2;
```

Dieser Wert ist absichtlich **nicht** in XAML oder im ViewModel dupliziert. Oberfläche, Statusanzeige und Speichervalidierung beziehen sich auf dieselbe Core-Regel. Bei einer fachlich freigegebenen Änderung muss außerdem `VerifyLicenseAdministrationPolicy` in `Tests/CoreSmokeTests/Program.cs` angepasst werden.

## Zuständige Klassen

| Klasse | Aufgabe |
|---|---|
| `LicenseAdministrationPolicy` | berechnet und validiert den vollständigen Änderungsplan |
| `LicenseChangePlan` | Ergebnis mit Gültigkeit, Meldung, Schreibreihenfolge und resultierenden Level9-Benutzern |
| `ViCoAdministrationPageVM` | Auswahl, Anzeige, Aufruf der Richtlinie und sequenzielles Speichern |
| `LegacyLicenseService` | kompatibles Lesen/Schreiben der vorhandenen verschlüsselten Dateien |
| `WindowsUserIdentity` | vereinheitlicht Domain- und Kurzschreibweisen |
| `ViCoAdministrationPage.xaml` | Auswahlfelder, Deckungsanzeige und Speicheraktion |

Die vorhandene Verschlüsselung ist ein Kompatibilitätsmechanismus und ersetzt kein modernes Berechtigungsbackend. Eine Migration der Sicherheit ist weiterhin ein eigener, späterer Arbeitsschritt.
