# VIBN Tools with integrated ViCo

Windows desktop application for virtual commissioning workflows. The existing VIBN Tools UI remains the host application; ViCo and the isolated TIA Bridge are integrated as modular features.

## Build

Prerequisites:

- Windows desktop with .NET 8 SDK
- the configured Grob.UX package source
- fe.screen-sim V5 SDK; set `FEE_SCREEN_SIM_ROOT` if it is not installed at the default path
- Siemens TIA Portal/Openness for live TIA workflows
- access to the documented GROB network paths for live ViCo data

```powershell
dotnet restore VIBN_Tools_App.sln --configfile NuGet.Config
dotnet build VIBN_Tools_App.sln --configuration Release --no-restore
```

Run the local core verification with:

```powershell
dotnet run --project Tests/CoreSmokeTests/VIBN_Tools.Core.SmokeTests.csproj --configuration Release
```

## Documentation

- [User guide](docs/USER_GUIDE.md)
- [Architecture and data sources](docs/ARCHITECTURE.md)
- [Release acceptance checklist](docs/ACCEPTANCE_CHECKLIST.md)
- [Integration notes](INTEGRATION.md)

Secrets and credentials must never be written to the application log. The current legacy license and Remote Desktop compatibility are intentionally documented for replacement in the dedicated security phase.
