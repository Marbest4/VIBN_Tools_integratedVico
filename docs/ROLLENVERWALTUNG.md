# Rollenverwaltung

## Zweck

VIBN Tools verwendet Rollen ausschließlich zur Steuerung der Tool-Berechtigungen. Es gibt keine Lizenzanfrage, keine Ablaufdaten und keine Lizenzkostenlogik. Die Rollenliste wird zentral in `roles.json` gespeichert und kann über die Umgebungsvariable `VIBN_VICO_ROLES_FILE` auf einen anderen freigegebenen Pfad gelegt werden.

## Berechtigungsmatrix

| Bereich | erforderliche Rolle |
| --- | --- |
| normale VIBN-Reiter, Project Settings, ViCo, Zuli Converter, Special Devices, Model Validation, Model Control, Interface Operation | alle Benutzer |
| CAD Wizard, Container Generation, Container2Fee | Level7 oder höher |
| Kanbanize Karten, AI-Test | Level8 oder höher |
| ViCo → Verwaltung anzeigen | Level8 oder höher |
| Benutzer hinzufügen, entfernen oder Stufe ändern | Level9 |

Die Sichtbarkeit wird beim Start aus derselben Rollenliste bestimmt, die auch der Verwaltungsreiter verwendet. Ein nicht erkannter Benutzer erhält keine Level7-/Level8-Reiter.

## Verbindliche Level9-Regel

`lutzma` ist systemweit immer Level9. Dieser Eintrag ist sichtbar, kann aber nicht herabgestuft oder entfernt werden.

Zusätzlich müssen nach jeder gespeicherten Änderung mindestens zwei unterschiedliche Benutzer Level9 haben. Die Prüfung läuft zentral in `ViCoRolePolicy.PlanSave`; sie wird daher nicht durch eine XAML-Einstellung oder durch einen einzelnen Button umgangen.

Wenn nur `lutzma` Level9 ist, kann ein Level9-Administrator zuerst einen zweiten Benutzer mit Level9 anlegen. Erst dann wird die zentrale Rollenliste geschrieben. Eine Herabstufung oder Entfernung, die wieder nur einen Level9-Benutzer zurückließe, wird mit einer verständlichen Statusmeldung abgewiesen.

## Bedienung im Verwaltungsreiter

1. ViCo öffnen und **Verwaltung** wählen. Der Reiter ist ab Level8 sichtbar.
2. Mit **Aktualisieren** die Rollenliste einlesen.
3. Als Level9 im Bereich **Benutzer- und Rollenverwaltung** Benutzername und Stufe wählen und **Benutzer hinzufügen** klicken.
4. Für eine Änderung den Benutzer in der Tabelle markieren, eine neue Stufe wählen und **Stufe speichern** klicken.
5. Vor dem Entfernen oder Herabstufen eines Level9-Benutzers prüfen, dass danach mindestens zwei andere/eindeutige Level9-Zuordnungen erhalten bleiben.

Die Rollen-Datei wird atomar ersetzt. Ein abgebrochener Schreibvorgang kann daher keine teilweise geschriebene JSON-Datei erzeugen.

## Wo wird die Mindestanzahl geändert?

Die fachliche Konstante steht ausschließlich hier:

```csharp
VIBN_Tools.Core/ViCo/UserRoles.cs
ViCoRolePolicy.MinimumLevel9Users
```

Der aktuelle Wert ist `2`. Eine Änderung daran ist eine organisatorische Entscheidung und muss zusammen mit `VerifyRoleAdministrationPolicy` in `Tests/CoreSmokeTests/Program.cs` geprüft werden.

## Historische Daten

Falls noch keine `roles.json` existiert, kann die Anwendung einmalig frühere verschlüsselte Zuordnungen lesen und in das neue, reine Rollenformat überführen. Danach wird kein altes Lizenzformat mehr geschrieben oder für Anfragen verwendet. Der Migrationspfad ist bewusst von der laufenden Rollenlogik getrennt.
