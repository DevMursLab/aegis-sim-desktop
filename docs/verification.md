# Verification / current source

## Source audit - 28 October 2026

Current desktop version: **3.3.0**; simulation engine: **3.2.0**. A fresh `scripts/verify.ps1 -SkipUi` run passed locked dependency restore, Release build with **zero warnings and zero errors**, **212 executable console checks** and all three lessons. Counts: lesson engine 7, desktop logic 20, integration 22, logistics 20, quality 32, fleet 20, priority 16, release 75. The release group includes eight synthetic forecast checks and report cancellation / playback pacing checks. A separate fresh `dotnet run --project tests/Aegis.Wpf.Smoke -c Release --no-build` run also passed actual WPF interaction flows, forecast lab, playback pacing and two-window host restart/reconnect, with no binding errors. Junction, dynamic-road and forecast screenshots were refreshed from this run. Earlier evidence is retained below.

## Earlier 3.2 verification

Validation, 28 October 2026 (3.2): the standard verify.ps1 run passed locked dependency restore with NuGet audit enabled, a Release build with zero warnings/errors, all **201 console checks**, all three lessons and actual WPF flows with no binding errors. The release group contains 64 checks, including 18 new junction checks and two new live host checks: role-enforced junction diversion and an actual host restart preserving an occupied road, mode and checksum. Original engine 3.1 incident history retains its tick-26 hash and old waiting rules. After placing the new mode button alongside road controls to preserve event-history space, the affected projects were rebuilt warning-free and the entire WPF suite passed again.

Junction coverage includes occupied-edge completion after closure, exact node arrival, diversion without position jumps, already-traversed edges, no-path expiry, return paths, shelter closures, per-edge tick rounding, 1/2/8-vehicle conservation, enable/reset constraints, legacy version gates, immutable exposed geometry, preview/replay/checkpoints, SQL tampering, unranked report traces, deterministic road-order handling and restock. Desktop automation exercises the actual enable/road buttons, exact junction position, diversion geometry and read-only seek. Screenshot: docs/images/junction-routing.png. This is a discrete training model: entered edges finish, paths change at nodes, and unused tick budget is discarded at nodes.

Validation, 28 October 2026 (3.1): the complete standard verify.ps1 run passed locked restore with NuGet audit enabled, a Release build with zero warnings/errors, all **181 console checks**, all three lessons and the actual WPF suite. Fifteen new dynamic-road checks plus a live controller/observer check cover alternate paths, unreachable demand, expiry, outbound/return waiting, conservation, renewal/reset, immutable returned geometry, metrics, preview, report traces, SQL tampering, original engine 3.0 binary compatibility and read-only desktop controls. WPF verifies the actual road picker/button, closed-edge binding, alternate moving geometry, replay protection and non-road command disablement. The initial top-row layout reduced the minimum-window map area; moving the controls into the inspector fixed that check, and the complete suite passed afterward.

The road demo without incidents finishes at tick 14. Closing road-01 at tick zero for five ticks chooses the long road-99 detour and finishes all 30 deliveries/returns at tick 24. This is a deterministic training-model result. Assigned trips pause for any closed edge anywhere on their path; this is not edge-by-edge occupancy or in-flight diversion.

Validation, 28 October 2026: locked restore with NuGet audit enabled and the full Release build passed with zero warnings/errors. All 165 console checks, three console lessons and the actual WPF suite passed, including frozen full-run background export, overlap rejection, reset, failure/retry and two-client host restart/reconnect. The initial new rollback check used a validation-only exception helper and stopped on the expected IOException; after correcting that check, the Release group was rebuilt and all 28 checks passed, followed by WPF and lessons. Other console groups had already passed in the standard verification run.

Report publication follow-up, 28 October 2026: reports are generated in a private staging directory before publication. Handled publication failures roll back replaced files and remove newly published files; originals are retained with a recovery path if rollback also fails. A destination-directory conflict check verifies rollback and successful retry. Publication is not an atomic multi-file transaction: concurrent readers may see intermediate files, and process termination or power loss can interrupt publication. Concurrent exporters targeting the same run/folder are not supported.

Report optimization follow-up, 26 October 2026: the standard verification script restores with the dependency audit enabled and builds with zero warnings/errors. Report generation shares one verified reconstruction across metrics, PDF, HTML and CSV. Two additional release checks cover known outcomes across legacy, logistics, fleet, priority and geographic scenarios, and corrupt-history rejection. That follow-up contained 164 console checks; publication rollback brought it to 165.

Source-change verification, 26 October 2026: warning-free Release build with locked dependencies, all 162 console checks, and all three console lessons passed. NuGet's vulnerability endpoint was unreachable even on an escalated retry, so this build used `-p:NuGetAudit=false`; it does not establish a current vulnerability audit. The actual WPF suite passed, including full-run background export while viewing partial replay, reset during export, overlapping-export rejection, failure/retry, and two-window host restart/reconnect. Network console checks completed successfully with escalated local networking after a sandboxed attempt stalled.

Local environment: Windows x64, .NET SDK **10.0.402**, 25 October 2026.

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\verify.ps1
```

| Check group | Coverage |
|---|---|
| Release build | All projects; warnings treated as errors; dependency audit enabled; pinned lock files |
| Lesson 03 | 7 checks: delivery bounds, determinism, input validation and completion |
| Desktop/model/map | 20 checks: lifecycle, commands, reset, conservation, map transforms/hit-testing and notifications |
| Integration | 22 checks: scenario interchange, SQL/replay integrity, storage failure, metrics/reports, independent process reload and actual SignalR host/client behavior |
| Logistics | 20 checks: cargo/stock conservation, travel boundaries, shortage/restock, closure, JSON/XML, legacy 2.0 golden history/SQL, persistence, replay, metrics/reports and live host roles |
| Quality | 32 checks: GeoJSON validation/projection, geometry bounds, deterministic completion/shortage/replay preview, cancellation, tick limit, diagnostics privacy and 12 editor checks (validation, isolation, culture, row bounds, JSON/XML interchange) |
| Fleet | 20 checks: concurrent reservation/arrival, exhaustion/restock, closures, travel boundaries, 1-8 vehicle matrix, SQL/checksum/replay, original engine 2.1 fixture, editor, reports and actual host/observer |
| Priority | 16 checks: scarcity, stable ties, closures, cargo reservation, no redirection, validation, ranked state hashes, JSON/XML, editor, SQL traces/tampering, original 2.4 SQL fixture, culture matrix, reports and live controller/observer |
| Release | 64 checks: spherical road lengths, stable shortest paths, geometry validation, road motion, source hashes, snapshot isolation, XML/JSON, legacy 2.5 SQL, database backup/idempotency/rollback/future-version refusal, service closures actual geographic host/observer three report reuse/integrity/publication checks and five asynchronous replay checks (endpoint, latest seek, corrupt history, reset race and cancellation); 15 dynamic-road checks and an additional live host/observer road-incident check; 18 junction checks, live junction-role synchronization and a real host restart retaining in-flight edge progress |
| Local WPF | Actual buttons/bindings/timer, map/inspector sync, replay seek, reset, close; default/minimum window rendering; stock/travel demo, moving cargo, shortage controls, restock completion and read-only logistics replay; actual GIS viewer import/selection/error preservation and asynchronous completion preview |
| Two WPF clients | Incident sync, observer write disabled, stale state, automatic reconnect after actual host restart |

**201 console checks**, plus the interactive WPF flows. These are executable checks; run the script rather than `dotnet test`. GitHub Actions runs build/console/integration validation with `-SkipUi`; inspect the actual workflow result for each commit.

## Observed reproducible result

Default scenario: demands 25, 5, 18; vehicle capacity 10. Baseline completes in **6 ticks**, delivers **48**, remaining **0**. Closing Shelter-A for five ticks produces **8 ticks**, **48 delivered**, **2 waiting ticks**, **75% active ticks**, mean service tick **219/48 ≈4.56**. Baseline mean service tick is **163/48 ≈3.40**. These are deterministic model results, not real-world rescue claims.

Schema 2 demo: initial stock 30; shortage at tick 16 with 30 delivered; restock 20; completion at tick 24 with 48 delivered, 2 in stock and no cargo. Each of the six trips costs one dispatch tick, two outbound ticks and one return tick.

The committed legacy fixture was extracted from the original v2.0 package database. Its original hash, commands, events and checkpoint must remain byte-compatible after loading in 2.1.

## Reliability checks

- Persisted history reloads in an independent process with the same final state hash.
- Commands, normalized typed events and checkpoints are compared to replay on load.
- Invalid/truncated history and tampered checksums/SQL are rejected.
- A forced save failure leaves the displayed run unadvanced.
- Invalid XML/JSON, oversized input, unknown schema and external-entity XML fail safely.
- Observer writes and unauthenticated reads are denied by the live host.
- Duplicate network commands are idempotent; stale expected versions fail.
- Controller reset is idempotent, creates a new shared ID and preserves the prior SQL run.
- PDF pages open successfully; a 64-shelter report paginates to four pages. Sample PDF pages were rendered and inspected locally.
- HTML labels and CSV spreadsheet prefixes are escaped.

## Release package

`scripts/package.ps1` publishes self-contained Windows x64 desktop/host/tools, seeds two runs, exports PDF/HTML/CSV, includes scenarios/learning docs/license notices and generates a ZIP plus SHA-256. The per-user installer is optional. See release assets for the downloadable archive.

## Practical limits

No fresh physical machine, all-DPI/multi-monitor matrix, complete accessibility audit, signed installer, high-load benchmark, internet hosting or real-world exercise efficacy has been established. SQLite schema version 1 has an explicit legacy upgrade with a consistent pre-upgrade backup and transaction rollback; it is not a general migration framework. Live command persistence is synchronous; saved-history replay and source-build report export run in the background. Large histories are not the target. The host is loopback-only, single-room, full-snapshot synchronization. Reconnection eventually stops after configured SignalR retries; restart the client if the host remains unavailable.

PDF uses ASCII labels with Unicode replacements; HTML/CSV preserve full Unicode. Checksum validation detects inconsistent saved data, not adversarial rewriting of all files by a privileged local user.

## Profiling

Run `scripts/profile.ps1` to write `artifacts/performance.json`. Three warmups, fifteen measured serial samples, 1000 ticks or verified commands per sample, processor/runtime/OS and hash included. This measures engine and replay only; SQL, UI and network are excluded. Diagnostics export is a single process observation, not this repeated profile.

[Observed local sample, 25 October 2026](evidence/performance-2026-10-25.json): median 0.1941 ms per 1000 engine ticks and 4.0988 ms per 1000 verified replay commands on the recorded Windows x64 / Intel environment. Includes final hash checks and three warmups; these small, uncontended samples do not measure user-visible latency or promise performance on another machine.

Editor WPF flow: open via Tools, reject invalid capacity, add/remove rows, save/reload XML, create a new run without changing the old scenario, discard a draft and retain input after a failed creation callback.

Fleet WPF: launch via Tools, render three moving markers, bind per-vehicle rows, finish at tick 8, validate fleet count in the editor, start four vehicles and reconstruct read-only simultaneous arrival at replay tick 3.

Priority WPF: launch the limited-stock demo, inspect urgent-shelter results and selected dispatch reason, edit a real priority grid cell, reject rank 10, accept rank 8 in a new run and reconstruct read-only priority history.


Road WPF: open the shipped demo, bind six roads, disable incompatible generic editing, verify movement follows road geometry, complete 30 packets at tick 14 and reconstruct the same map at replay tick 2. Screenshot: `docs/images/road-exercise.png`.

The 3.0 release package is checked separately for ZIP integrity, packaged startup, per-user installation and installed startup. These checks use this Windows environment, not a clean physical machine.


3.0.1 WPF additionally exercises dispatcher activity during background replay and the actual timeline slider with rapid requests; the latest requested tick wins and controls remain read-only. Initial database work and history listing/verification/comparison run off the dispatcher. SQL operations finish in their worker if the history window closes; their results are discarded. Live command persistence and final event/map publication still use the UI thread; source-build report export runs in the background. No large-history throughput claim is made.
