# Verification / 2.3 source

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
| Local WPF | Actual buttons/bindings/timer, map/inspector sync, replay seek, reset, close; default/minimum window rendering; stock/travel demo, moving cargo, shortage controls, restock completion and read-only logistics replay; actual GIS viewer import/selection/error preservation and asynchronous completion preview |
| Two WPF clients | Incident sync, observer write disabled, stale state, automatic reconnect after actual host restart |

**101 console checks**, plus the interactive WPF flows. These are executable checks; run the script rather than `dotnet test`. GitHub Actions runs build/console/integration validation with `-SkipUi`; inspect the actual workflow result for each commit.

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

No fresh physical machine, all-DPI/multi-monitor matrix, complete accessibility audit, signed installer, high-load benchmark, internet hosting or real-world exercise efficacy has been established. Initial SQLite schema bootstrap is not a general migration framework. Local UI persistence/replay is synchronous; large histories are not the target. The host is loopback-only, single-room, full-snapshot synchronization. Reconnection eventually stops after configured SignalR retries; restart the client if the host remains unavailable.

PDF uses ASCII labels with Unicode replacements; HTML/CSV preserve full Unicode. Checksum validation detects inconsistent saved data, not adversarial rewriting of all files by a privileged local user.

## Profiling

Run `scripts/profile.ps1` to write `artifacts/performance.json`. Three warmups, fifteen measured serial samples, 1000 ticks or verified commands per sample, processor/runtime/OS and hash included. This measures engine and replay only; SQL, UI and network are excluded. Diagnostics export is a single process observation, not this repeated profile.

[Observed local sample, 25 October 2026](evidence/performance-2026-10-25.json): median 0.1941 ms per 1000 engine ticks and 4.0988 ms per 1000 verified replay commands on the recorded Windows x64 / Intel environment. Includes final hash checks and three warmups; these small, uncontended samples do not measure user-visible latency or promise performance on another machine.

Editor WPF flow: open via Tools, reject invalid capacity, add/remove rows, save/reload XML, create a new run without changing the old scenario, discard a draft and retain input after a failed creation callback.
