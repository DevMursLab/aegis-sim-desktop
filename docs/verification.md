# Verification

## Current source

These are executable console checks, not an xUnit/`dotnet test` suite.

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\verify.ps1 -SkipUi
```

The script performs a locked restore with NuGet audit enabled, builds 15 projects in Release with warnings treated as errors, and runs **212 checks**. [Windows CI for the reference-test cleanup](https://github.com/DevMursLab/aegis-sim-desktop/actions/runs/40721310979) passed. Local locked restore, warning-free Release build and the seven relocated reference-engine checks passed. Standalone tutorial projects are not required.

| Group | Count | Coverage |
|---|---:|---|
| Reference engine | 7 | Delivery bounds, determinism, validation and completion using self-contained fixtures |
| Desktop | 20 | Lifecycle, commands, reset, conservation, map transforms, selection and notifications |
| Integration | 22 | Scenario interchange, SQL/replay integrity, save failure, reports, process reload and live clients |
| Logistics | 20 | Stock/cargo, travel, restock, closures, original histories and host roles |
| Quality | 32 | GeoJSON bounds/geometry, preview, cancellation, diagnostics privacy and editor validation |
| Fleet | 20 | Reservations, simultaneous arrivals, 1-8 vehicle matrix, replay/SQL and synchronization |
| Priority | 16 | Scarcity, stable ties, eligibility, no redirection, traces, compatibility and reports |
| Release | 75 | Roads/junctions, legacy archives, database upgrades, reports/cancellation, forecasts, replay races and host restart |
| **Total** | **212** | |

Omit `-SkipUi` on an interactive Windows desktop to include actual WPF flows. Recorded WPF verification covers buttons/bindings/timer, pause/resume/reset, map/inspector, editor/GIS/fleet/priority/roads, timeline cancellation, forecast lab, playback pacing and two-window host restart/reconnect, with no binding errors. Reference-test relocation and subsequent documentation edits do not change runtime code; the interactive suite was not rerun for these source-maintenance edits.

## Package verification

```powershell
python .\scripts\verify-package.py .\artifacts\AEGIS-SIM-3.3.1-win-x64.zip
```

The 3.3.1 package passed checksum/ZIP integrity, required-file and credential-filename checks, exclusion of personal planning/preparation material, isolated extraction, fresh/seeded startup, packaged profiling/forecast tools, custom-directory installation, installed startup and road-host health. Exact source commit, SHA-256 and results are in the [release verification asset](https://github.com/DevMursLab/aegis-sim-desktop/releases/tag/v3.3.1). These use the same Windows environment with isolated data, not a clean physical-machine audit.

## Reproducible outcomes

- Default demands 25, 5 and 18 with capacity 10: baseline tick 6, 48 delivered. A five-tick shelter closure gives tick 8, 48 delivered, two waiting ticks and 75% active ticks.
- Finite-stock demo: 30 delivered and 18 pending at tick 16; restock 20 permits completion at tick 24 with two in stock and no cargo.
- Three-vehicle demo: 48 delivered and every vehicle returned at tick 8.
- Default imported-road demo: 30 delivered and every vehicle returned at tick 14. Explicit junction mode uses separate per-edge rounding.

These are deterministic synthetic observations, not field efficacy results. See [demo walkthrough](demo-script.md).

## Dated evidence

These artifacts record earlier observations, not a claim that every commit reran every check:

- [Original 3.3.0 audit](evidence/source-audit-2026-10-28.json): 212 checks and the then-included three console lessons. Those standalone projects have since been removed; their seven model checks remain as reference fixtures.
- [Junction routing](evidence/junction-routing-2026-10-28.json): earlier 201-check increment and WPF exercise.
- [Shared roads](evidence/dynamic-roads-2026-10-28.json): earlier 181-check increment.
- [Synthetic forecast evaluation](evidence/forecast-evaluation-2026-10-28.json): training/holdout groups, model, predictions, metrics and dataset hash.
- [Timing sample](evidence/performance-2026-10-25.json): environment, warmups, repeated engine/replay samples and hashes. SQL/UI/network latency is excluded.

## Limits

No broad physical-machine/monitor/DPI matrix, complete accessibility audit, signed installer, internet deployment, high-load acceptance or field validation has been established. Live local persistence remains synchronous. The host is loopback-only and single-room; reconnect attempts are finite. Report rollback covers handled failures rather than power loss or process termination. PDF labels use ASCII replacements; HTML/CSV preserve Unicode. Checksums detect inconsistency, not privileged rewriting of all data.
