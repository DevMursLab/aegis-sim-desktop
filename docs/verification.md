# Verification record

Local validation: **24 October 2026**, Windows x64, .NET SDK **10.0.402**.

Reproduce: `powershell -ExecutionPolicy Bypass -File .\scripts\verify.ps1`.

| Check | Result |
|---|---|
| Full solution Release build, warnings as errors | Passed; 0 warnings, 0 errors |
| Lesson 03 executable checks | 7 passed |
| Desktop model, ViewModel and map checks | 20 passed |
| Console lessons 01, 02 and 03 | Ran successfully |
| Real WPF window smoke check | Passed |
| Binding errors during smoke run | None observed |

Default desktop run: 6 logical ticks, 48 delivered, zero remaining. The README screenshot is rendered from an actual paused run, showing selected shelter details and partial completion; it is not a mockup. A separate completed screenshot is also included in `docs/images/`.

## Coverage

- Command availability, duplicate start protection, manual step and automatic advance.
- Pause blocking late timer callbacks, resume and completion locking.
- Reset from running, paused and completed states, restoring demand and clearing history.
- Identical ordered delivery events after reset/rerun.
- Capacity limits and supply conservation with capacities 1, 4, 10 and 100.
- Invalid capacity, negative demand, duplicate/empty names and total-demand overflow.
- Empty scenarios, invalid transitions, external state mutation and change notifications.
- Coordinate round-trip across viewport sizes, zoom anchor and zoom bounds, pan/fit and hit-testing after transforms.
- Invalid viewport input, map/table/inspector consistency, selection persistence, and reset/completion progress.

The WPF smoke runner invokes actual button automation peers, reads bound text and table counts, tests map selection and dropdown synchronization, zoom/fit buttons and map data refresh, waits for the actual timer, checks pause/resume/completion/reset, closes while running, and fails on binding errors. Default and minimum-size window renders are inspected. Mouse/keyboard gesture handlers are not a full physical-input automation suite.

## Limits

These are console checks and a WPF smoke executable, not xUnit tests. `dotnet test` is not the verification entry point. GitHub Actions runs the build and console validation with `-SkipUi`; consult the workflow for the remote result. Local success does not establish CI success.

Fresh-machine installation, high-DPI/multi-monitor layout, accessibility tooling, large scenarios and load/performance are not yet validated. Persistence and multiplayer are not implemented. No production-readiness claim is made.
