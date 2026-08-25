# Screenshots

Die Bilder in diesem Ordner stammen aus `Tests/UiStartupSmokeTests` mit synthetischen Testdaten. Sie enthalten keine echte Kanbanize-, FEE-, TIA-, Benutzer- oder Kennwortinformation.

Zum Aktualisieren:

```powershell
$env:VIBN_CAPTURE_UI_PREVIEW = '1'
dotnet run --project Tests/UiStartupSmokeTests/VIBN_Tools.UiStartup.SmokeTests.csproj --configuration Release
```

Die erzeugten Vorschauen liegen zunächst im Test-Ausgabeordner. Vor dem Ersetzen der Dokumentationsbilder muss die Darstellung visuell geprüft werden.
