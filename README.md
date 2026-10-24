# AEGIS-SIM Desktop / 2.0

A **synthetic civilian response-training simulator** built with C#, .NET 10, WPF/MVVM, EF Core/SQLite and SignalR. Run an exercise, inject a route closure, persist commands/events, reconstruct history and compare measured outcomes.

![Actual controller window after a synchronized route-closure incident](docs/images/desktop-preview.png)

## A reviewable three-minute demo

1. Open the desktop app and select Shelter-A.
2. Close its route for five logical ticks and start the run.
3. Watch the map, inspector, event table and metrics change together.
4. Reopen a saved run after closing the app; seek its read-only replay timeline.
5. Compare baseline and incident runs, then export verified PDF/HTML/CSV.
6. Start a controller and observer; interrupt/restart the host and watch automatic state recovery.

The supplied seed tool produces the same comparison every time: **baseline 6 ticks / closure run 8 ticks / 48 packets delivered in both**. These are synthetic-model observations, not real-world performance claims. [Detailed demo script](docs/demo-script.md).

## Implemented

| Area | Working behavior |
|---|---|
| Desktop | Start, Pause, Resume, Step, Reset; command enablement and error feedback |
| Map | Offline local-meter vector view, selection, inspector, pan/zoom, keyboard alternatives |
| Scenarios | Bounded/versioned JSON and XML import/export; coordinates, unique names and quantities validated; XML DTD/external resolver disabled |
| Incidents | Selected shelter route closed for five ticks; availability, ordering and completion metrics change |
| Persistence | Transactional EF Core SQLite runs, unique commands, typed tick events and checksummed checkpoints |
| Replay | Durable command replay, state-hash validation, read-only timeline; original history remains immutable |
| Comparison | Compatible-run A/B ticks, deliveries, mean service tick, active-tick percentage and waiting ticks |
| Reports | Actual PDF, UTF-8 HTML and CSV generated from verified replay; HTML/CSV escaping |
| Multiplayer | Local authoritative host, separate controller/observer tokens, version checks, deduplication, full-snapshot resync and automatic reconnect |
| Packaging | Self-contained Windows x64 ZIP, seeded demonstrations, per-user installer and source verification scripts |
| Learning | Bangla lessons from variables through persistence, replay, networking and packaging |

## Run from source

Requirements: **Windows x64 and the .NET 10 SDK**. Dependencies restore from NuGet; pinned versions and lock files are committed. No paid API, cloud account, external map tiles or manually configured database is required.

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-lesson.ps1 -Lesson 05
```

Or `dotnet run --project src/Aegis.Desktop.Wpf`. On first launch a local run is created. Later launches open the most recent saved run as read-only replay; **Reset** starts a fresh run with a new ID.

Local data: `%LOCALAPPDATA%\AEGIS-SIM\local\runs.db`. Import sample files from `assets/scenarios/`. Select **Saved runs / A/B** to replay one run or Ctrl+select two compatible runs to compare. Export report saves the **full selected run**, even when the replay slider is at an earlier tick.

Seed a separate review folder:

```powershell
dotnet run --project src/Aegis.Tools -- --seed .\artifacts\review-demo
dotnet run --project src/Aegis.Desktop.Wpf -- --data-dir .\artifacts\review-demo
```

The lesson launcher also supports a user-local SDK at `%LOCALAPPDATA%\Microsoft\dotnet`.

## Two desktop clients

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\start-multiplayer.ps1
```

This starts a hidden local host plus controller and observer windows. **Only the host advances a network run.** Controller Reset starts a new shared run; previous runs remain in the host database. The script prints the host PID so it can be stopped after the demo.

The demo is **loopback-only**. Credentials live in `%LOCALAPPDATA%\AEGIS-SIM\host\session-secrets.json`; do not commit or share them. Roles are checked by the server, not just disabled in the UI. This is not an internet-deployment authentication system.

## Verify and package

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\verify.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\package.ps1
```

Verification performs a locked dependency restore, full Release build with warnings treated as errors, **49 executable console checks**, all earlier console lessons, real WPF interaction checks, and a two-window host-restart/reconnect check. Checks are console executables, not an xUnit/`dotnet test` suite.

[GitHub Actions](https://github.com/DevMursLab/aegis-sim-desktop/actions/workflows/build.yml) runs the same Release build and console/integration checks on Windows with `-SkipUi`. Interactive WPF checks run locally. [Evidence and limits](docs/verification.md).

The package includes its runtime. Extract it, then use **Start-Local.cmd**, **Start-Seeded-Demo.cmd**, or **Start-Multiplayer.cmd**. No SDK is required. Optional `Install.ps1` copies the app into the current user's Programs folder and creates a desktop shortcut. No administrator rights are required.

## Architecture and learning

```text
WPF View → Presentation / commands → Simulation / rules and replay
                  ↓                          ↑
        SQLite persistence port     SignalR authoritative host
```

Simulation and presentation do not depend on WPF. A candidate command is replayed and validated, committed to SQL, then published to the screen/clients. [Architecture](docs/architecture.md), [demo script](docs/demo-script.md), [third-party notices](THIRD-PARTY-NOTICES.md).

[বাংলা শেখার শুরু](docs/learning/START-HERE.bn.md) → [01](docs/learning/01-supply-ticks.bn.md) → [02](docs/learning/02-shelter-objects.bn.md) → [03](docs/learning/03-tick-engine.bn.md) → [04](docs/learning/04-wpf-mvvm.bn.md) → [05](docs/learning/05-offline-map.bn.md) → [06](docs/learning/06-sql-history.bn.md) → [07](docs/learning/07-replay.bn.md) → [08](docs/learning/08-multiplayer.bn.md) → [09](docs/learning/09-release.bn.md)। Code প্রস্তুত; নিজের অনুশীলন ও ব্যাখ্যা দিয়ে শেখা যাচাই করবে।

## Scope and limits

This is a **portfolio MVP**, using fictional civilian scenarios. It does not reproduce an institution's internal military software. All demand begins at tick zero; each delivery visit receives a fresh load. There is no stock/travel-time physics. Route closure restricts a shelter's availability; links do not compute geographic routes.

Coordinates are synthetic local meters, not GPS/GeoJSON/WGS84. AI forecasts, general GIS/projection import, cloud deployment, high-scale load testing, a signed installer and comprehensive accessibility/high-DPI audits are outside this release. Full snapshots and command replay favor a small, explainable demo over throughput. PDF uses ASCII labels; HTML/CSV preserve Unicode names. The initial database uses an explicit schema bootstrap/additive development upgrade; future schema evolution needs a migration plan.
