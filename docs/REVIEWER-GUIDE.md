# Review AEGIS-SIM in five minutes

AEGIS-SIM is an independent portfolio project for fictional civilian response exercises. The strongest review path is to connect visible behavior to the code and its verification.

## 1. See the application

Open the [screenshot gallery](../README.md#application-gallery), then follow the [three-minute demo](demo-script.md). The screenshots show the actual Windows application; the repository banner is decorative artwork.

The published [3.3.1 Windows download](https://github.com/DevMursLab/aegis-sim-desktop/releases/tag/v3.3.1) includes its runtime, editor, fleet, priorities, road incidents, junction diversion, synthetic forecast lab and report-export improvements. Desktop version is 3.3.1 / simulation engine 3.2.0. The package is verified from an isolated ZIP extraction, including fresh/seeded startup, custom-directory installation, installed startup, host health and packaged tools. This is a local Windows check, not a clean physical machine audit.

## 2. Reproduce a result

On Windows with .NET 10:

```powershell
dotnet run --project src/Aegis.Tools -- --seed .\artifacts\review-demo
dotnet run --project src/Aegis.Desktop.Wpf -- --data-dir .\artifacts\review-demo
```

Compare the seeded baseline and closure histories: 6 versus 8 logical ticks, with 48 packets delivered in both. For the road exercise, open Tools / Road network demo. Enable junction routing while all vehicles are at the depot, step three times, close road-03 and inspect the diversion. See [the full exercise](user-guide.md#junction-routing).

## 3. Inspect engineering decisions

| Review question | Code to inspect |
|---|---|
| Where are simulation rules kept? | [SimulationSession.cs](../src/Aegis.Simulation/SimulationSession.cs), [FleetSession.cs](../src/Aegis.Simulation/FleetSession.cs) |
| How are open routes selected? | [RoadRouting.cs](../src/Aegis.Simulation/RoadRouting.cs), [JunctionRouting.cs](../src/Aegis.Simulation/JunctionRouting.cs) |
| How is state exposed to WPF? | [RunViewModel.cs](../src/Aegis.Presentation/RunViewModel.cs) |
| How are saved commands and checkpoints verified? | [RunHistory.cs](../src/Aegis.Simulation/RunHistory.cs), [RunDatabase.cs](../src/Aegis.Infrastructure/RunDatabase.cs) |
| How do reports use verified history? | [Reports.cs](../src/Aegis.Infrastructure/Reports.cs), [PdfReport.cs](../src/Aegis.Infrastructure/PdfReport.cs) |
| How is the forecast experiment evaluated? | [ForecastLab.cs](../src/Aegis.Simulation/ForecastLab.cs), [evaluation artifact](evidence/forecast-evaluation-2026-10-28.json) |

Read [architecture](architecture.md) for the command-to-persistence-to-publication flow. Tests include tampered history, stock conservation, old-engine fixtures, network roles, duplicate commands and an actual host restart.

## 4. Run verification

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\verify.ps1 -SkipUi
```

The 28 October 2026 source audit passed a locked restore, Release build with zero warnings/errors, 212 console checks and three lessons. A separate fresh WPF smoke run also passed with no binding errors, including two-window reconnect and the forecast lab. Omit `-SkipUi` on an interactive Windows desktop to include these flows. See [verification](verification.md) for evidence and limits.

## 5. Assess the boundaries

The model uses logical ticks and synthetic demand. Local networking is loopback-only. General GIS, real traffic prediction, field-validated ML, cloud hosting, a signed installer and broad accessibility/scale audits remain future work. The forecast lab covers only generated schema-2 initial states; exact completion preview is used for known exercise histories.
