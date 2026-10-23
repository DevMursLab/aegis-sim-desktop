# AEGIS-SIM Desktop

A Windows desktop learning project for **synthetic civilian supply-response simulation**, built with C#, .NET 10, WPF and MVVM.

![Actual desktop workspace paused during a delivery run](docs/images/desktop-preview.png)

## Current features

- Start, Pause, Resume, manual Step and Reset.
- One vehicle visit per logical tick; ordered shelter service with capacity and demand bounds.
- Observable run status, tick, remaining demand, shelter table and delivery history.
- Offline synthetic planar map with click selection, zoom/pan, keyboard controls and Fit map.
- Map, inspector, completion bar and table share the same simulation state; last delivery is highlighted.
- Repeatable runs, scenario validation and protected lifecycle transitions.
- Three independent console lessons plus Bangla WPF/MVVM and offline-map lessons.

**Status: early desktop learning preview.** GIS import/projections, incidents, persistence, replay, multiplayer, authentication, forecasts and an installer are planned. The map uses a fixed synthetic demo layout in local meters, not real geography. Links are illustrative and do not calculate routes. History is in memory and clears on reset or exit. The vehicle gets a fresh load each visit; stock and travel time are not modeled.

## Run

Requirements: **Windows and .NET 10 SDK**. A runtime alone is insufficient. No paid API, database setup, map service or third-party runtime package is required for this preview.

From the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-lesson.ps1 -Lesson 05
```

Or use the SDK directly:

```powershell
dotnet run --project src/Aegis.Desktop.Wpf
```

The script supports a user-local SDK at `%LOCALAPPDATA%\Microsoft\dotnet`, otherwise using PATH. `global.json` selects a stable .NET 10 feature band.

Try **Step → Start → Pause → Step → Resume → Completed → Reset**. Default shelter demands are 25, 5 and 18, with vehicle capacity 10. Six visits deliver all 48 packets. The timer schedules approximately one tick per second; virtual results do not depend on wall-clock time.

Select a shelter on the map or from the dropdown. Scroll to zoom, drag to pan, or use the zoom/Fit buttons. With map focus, arrow keys pan, `+`/`-` zoom and `Home` fits the view. The amber marker indicates the last delivery visit, not continuous vehicle movement.

## Verify

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\verify.ps1
```

Builds the full solution in Release with warnings treated as errors, runs **27 executable console checks**, runs all console lessons, and opens a real WPF window for button, binding, map and timer validation. These are executable checks, not an xUnit or `dotnet test` suite.

For environments without an interactive desktop:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\verify.ps1 -SkipUi
```

[GitHub Actions](https://github.com/DevMursLab/aegis-sim-desktop/actions/workflows/build.yml) runs the Release build and console validation on Windows. Interactive UI checks run locally. See [verification evidence and limits](docs/verification.md).

## Architecture

```text
WPF View → Presentation (ViewModel + Commands) → Simulation (state + rules)
                ↑
     App composition + DispatcherTimer
```

Simulation and presentation have no WPF dependency. Map rendering and gesture handling stay in the WPF layer; code-behind handles only initialization and visual zoom/fit actions. The small demo runs on the dispatcher thread; large-scenario scheduling is future work. [Architecture and tradeoffs](docs/architecture.md).

| Folder | Contents |
|---|---|
| `src/` | Desktop preview, presentation and simulation |
| `lessons/` | Independent console exercises 01–03 |
| `tests/` | Executable model/command checks and WPF smoke runner |
| `docs/learning/` | Bangla explanations, exercises and roadmap |
| `scripts/` | Lesson launcher and full verification |
| `main/` | Current desktop planning PDF |
| `partial/` | Earlier web/Blazor planning PDF, kept as background |

## বাংলা শেখার পথ

[শুরু করো](docs/learning/START-HERE.bn.md) → [01: variable ও loop](docs/learning/01-supply-ticks.bn.md) → [02: class ও object](docs/learning/02-shelter-objects.bn.md) → [03: tick engine](docs/learning/03-tick-engine.bn.md) → [04: WPF ও MVVM](docs/learning/04-wpf-mvvm.bn.md) → [05: offline map](docs/learning/05-offline-map.bn.md)।

প্রতিটি ধাপে ফল অনুমান করো, চালাও, একটি মান বদলাও এবং নিজের ভাষায় ব্যাখ্যা করো। Console lesson চালাতে `-Lesson 01`, `02` বা `03`; Lesson 03/04/05-এর checks চালাতে সঙ্গে `-Check` দাও। Code প্রস্তুত মানে শেখা শেষ নয়; অনুশীলন করে বোঝা যাচাই করবে।

## Scope

All scenarios are fictional civilian training examples. This project does not reproduce any institution's internal military software. The PDFs are learning blueprints, not verified internal requirements.
