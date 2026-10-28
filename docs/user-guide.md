# AEGIS-SIM user guide

## Local runs

Launch `Start-Local.cmd` from an extracted Windows package, or run `dotnet run --project src/Aegis.Desktop.Wpf` on Windows with the .NET 10 SDK. A new data directory creates a demo run. Later starts open the most recent saved history as read-only replay.

Use **Reset** to create a fresh run with a new ID. **Start**, **Pause** and **Step** control the local exercise. Completion waits for all vehicles to return. Local playback speed is available under Tools at 1x, 2x and 4x; it changes pacing, not logical results or recorded history.

The default database is `%LOCALAPPDATA%\AEGIS-SIM\local\runs.db`. Use `--data-dir <directory>` for an isolated workspace.

## Scenario editing and import

**Tools / Scenario editor** opens a separate draft for capacities, shelter names/demand/positions, finite inventory, travel settings, fleet size and priorities. Validate and save JSON/XML, or create a separate run. Invalid input cannot replace saved history. Shared multiplayer scenarios and compiled road exercises cannot be edited here.

Sample scenarios are under `assets/scenarios/`. JSON/XML input is bounded and versioned. XML DTD/external resolution is disabled. Scenario validation checks names, quantities, coordinates and supported combinations.

## Stock, vehicles and priority

Dispatch reserves cargo against both depot stock and outstanding shelter demand. Arrival delivers it; the vehicle returns before another dispatch. Movement precedes dispatch, and a vehicle cannot redispatch on its return-arrival tick.

- **Stock + travel demo:** starts with 30 packets; shortage occurs at tick 16 with 18 outstanding. **Restock +20** permits completion at tick 24, leaving 2 in stock.
- **Tools / Three-vehicle demo:** three capacity-10 vehicles deliver 48 packets and all return at tick 8.
- **Tools / Priority + limited stock demo:** higher eligible ranks are served first; ties keep scenario order. With 20 packets, C receives 18 and B receives 2, while A waits. Priority is static and does not redirect cargo already assigned.

A shelter route closure pauses outbound service for that shelter. Shared road closures have separate behavior described below.

## Road network exercises

Open **Tools / Road network demo**, or import `assets/maps/road-exercise.geojson`. The default exercise delivers 30 packets and returns both vehicles at tick 14. Positive road lengths feed deterministic Dijkstra paths. Default travel ticks are `ceil(routeMeters / metersPerTick)` in each direction. Vehicle positions follow saved projected road geometry.

The GeoJSON root `exercise` object defines name, capacity, stock, fleet size and meters per tick. Point features identify depot/junction/shelter nodes; road LineStrings identify endpoints with `from`/`to`. The sample and bounded import format are described in [map provenance](../assets/maps/README.md). Inputs are fictional, regional WGS84 coordinates. Road distances use a spherical approximation; display projection is separate from distance calculation.

### Assigned-trip routing

This is the default mode. Close `road-01` for five ticks before dispatch: new trips choose the open detour via `road-99`. Closed edges appear red. Unreachable shelters do not reserve cargo; if no eligible open path exists, vehicles wait at the depot.

Once assigned, a trip keeps its route and duration. Closing any edge in that assigned path pauses it in place, outbound or returning. Expiry resumes the same trip. The return follows the assigned outbound path. Renewal cannot shorten the existing closure.

### Junction routing

Start a fresh road demo and select **Use junction routing** while every vehicle is at the depot. The mode is persisted and remains enabled for that run; Reset creates a new run with it disabled.

Step three times, select `road-03`, close it for five ticks, then Step again. The vehicle serving B chooses `road-02` from the reached node, preserving its destination and reserved cargo.

An entered road finishes even if it subsequently closes. At each reached node, the vehicle selects a shortest open path toward its shelter or depot. With no open path, it waits at that node without losing cargo. Outbound shelter closures still pause service. Return paths can differ from outbound paths.

Each tick advances within at most one road; unused movement is discarded at a node. Duration therefore rounds upward per edge in this explicit mode. It is a discrete training model without traffic, congestion or collision avoidance.

## Saved history, comparison and reports

**Saved runs / A/B** lists local histories. Open one for read-only replay; use the timeline to reconstruct a selected tick. Rapid seeks cancel superseded reconstruction and prevent stale results from replacing a newer state.

Ctrl+select two compatible runs to compare. Seeded baseline and closure runs finish in 6 and 8 logical ticks with 48 packets delivered in both. A/B comparisons require compatible scenario definitions.

**Export report** captures the full selected run, even when replay shows an earlier tick. PDF/HTML/CSV metrics are derived from verified replay. Fleet exports add per-vehicle CSV. Only one report export runs at a time; **Tools / Cancel report export** cancels reconstruction/staging. Handled publication failures attempt to restore previous output; process termination or power loss is outside the rollback guarantee. PDF labels use ASCII replacements; HTML/CSV preserve Unicode.

## Offline GIS, preview and diagnostics

- **Offline GeoJSON layers:** load `assets/maps/synthetic-region.geojson` and inspect features, layers and geometry. This separate viewer does not modify simulation routes, demand or saved hashes. Import bounds are documented in [map provenance](../assets/maps/README.md).
- **Completion preview:** calculates a continuation on a separate copy from the current or selected replay state. It assumes no new future interventions and does not append history. It reports completion, supply exhaustion or a tick limit.
- **Export diagnostics:** saves verified identity/runtime facts without credentials or database paths. `scripts/profile.ps1` measures engine/replay timings; it does not measure SQL, UI or network latency.

## Synthetic forecast lab

**Tools / Synthetic forecast lab** generates 800 schema-2 initial states and evaluates 7-nearest-neighbour regression. The deterministic split has 627 training groups and 173 holdout groups; scaling and the constant baseline use training rows only.

Recorded holdout MAE is 6.62 ticks versus 25.78 for the baseline; worst error is 62.55 ticks. Export includes the model, predictions, split and dataset hash. Only the bounded generated initial-state domain is supported; incidents, fleet, roads and mid-run inference are excluded. This does not establish real-world predictive accuracy. Use exact completion preview for a known exercise history.

## Local controller and observer

Run `Start-Multiplayer.cmd` from the package, or `scripts/start-multiplayer.ps1` from source. The local authoritative host advances the shared run; only the controller may submit commands. The observer is read-only, enforced by the host as well as the UI.

The demo uses loopback addresses. Credentials are stored under `%LOCALAPPDATA%\AEGIS-SIM\host\session-secrets.json`; do not share that file. Reset creates a new shared ID while retaining earlier runs. Restarting the host reconstructs saved state, and clients reconnect/resynchronize. SignalR retry attempts are finite; restart a client if the host remains unavailable beyond that retry period.

This is not an internet deployment or a production identity/authentication system.

## Verification and troubleshooting

Run `scripts/verify.ps1` for locked restore, Release build, executable checks and interactive WPF flows. Use `-SkipUi` for headless CI. The seven reference-engine checks contain self-contained test fixtures under `tests/Aegis.ReferenceEngine.Checks/Fixtures/`; they do not depend on tutorial projects.

For a Windows ZIP, run `python scripts/verify-package.py <archive.zip>` beside its SHA-256 file. This validates isolated extraction, startup, seeded replay, tools, installation and host health. See [verification](verification.md) and [architecture](architecture.md) for evidence, tradeoffs and limits.
