## Source maintenance - reference tests

Removes the standalone lessons and lesson launcher from the source tree. The seven reference-engine checks retain their small model as self-contained test fixtures under `tests/Aegis.ReferenceEngine.Checks/Fixtures/`. Solution, CI and verification references are updated. Simulation behavior and the 212-check total are unchanged; the Windows application remains desktop 3.3.1 / engine 3.2.0.

# Desktop 3.3.1 - documentation and distribution cleanup

Replaces the long landing page with a focused feature overview, five application captures, setup instructions and links to technical documentation. Personal learning/interview material and planning PDFs are excluded from the current source tree; a user guide documents the application workflows. Reference console samples remain because they are exercised by verification. Simulation engine 3.2.0, routing rules, persistence formats and application behavior are unchanged.

The Windows package carries the technical user guide instead of personal preparation notes. Older releases are historical artifacts and are not rewritten. Package verification targets the new documentation layout.

# Desktop 3.3.0 / engine 3.2.0

Adds a separate synthetic forecast lab: deterministic 7-nearest-neighbour regression over 800 generated schema-2 initial states, group-separated holdout evaluation, training-only scaling and an exported model/evaluation artifact. Holdout MAE is 6.62 logical ticks versus 25.78 for the training-mean baseline; worst error is 62.55 ticks. This does not support fleet, roads, incidents or real-world forecasting. Exact completion preview remains available for known histories.

Report export supports cancellation during reconstruction; cancelled work preserves existing output. Playback speed changes pacing without changing recorded history. Current source verification passes 212 console checks, three lessons and a warning-free Release build. Interactive UI evidence is documented separately. The repository now includes a reviewer guide, feature gallery and source/package version distinction. The 3.3.0 self-contained Windows package ships these additions together with road incidents and junction routing. Packaging writes a source/version manifest and SHA-256 sidecar; the reusable package verifier checks an isolated extraction, seeded and fresh startup, installation, host health and packaged tools.

# Included in 3.3.0 - engine 3.2 junction routing

Road exercises can persist EnableJunctionRouting while all vehicles are at the depot. The mode is irreversible within a run; Reset creates a fresh run with it disabled. Vehicles finish any road they have entered, even if that road subsequently closes. At each reached node, deterministic shortest-path routing excludes currently closed edges and targets the original shelter or, when returning, the depot. A disconnected vehicle waits at its node without losing cargo or delivering twice. Shelter closure still pauses outbound service.

Movement advances up to metersPerTick inside one road per tick. It stops at the next node and discards unused tick budget; per-edge rounding replaces the default whole-route rounding in this explicit mode. Dispatch still takes a separate tick, no vehicle redispatches on its return-arrival tick, shared demand stays reserved, and completion waits for every return. TicksRemaining/Duration describe the current edge in junction mode; at a node both are zero. RoadJourney retains the last reached node, target node, entered road, oriented geometry and remaining meters.

Engine 3.2 reads original 3.1 road-incident histories with their old hashes and whole-trip waiting behavior. New optional journey fields are omitted when null; the mode has a separate checksum/checkpoint branch. Older engine documents cannot accept EnableJunctionRouting. The original 3.1 fixture came from the preceding packaged DLLs and retains its tick-26 final hash. No database schema change is needed.

Desktop controls, actual map motion, read-only replay, preview, HTML/PDF/CSV, SQL and controller/observer state reconstruct the same model. Tests include a real host restart with a diverted vehicle. This is a discrete training model without congestion, collision avoidance or a real-world travel-time claim. Included in desktop release 3.3.0.

# Included in 3.3.0 - shared road incidents

Road exercises accept replayable `CloseRoad` commands with a road ID and 1-100 logical ticks. New dispatches recompute a deterministic shortest open path, excluding roads closed through the current tick. Highest eligible priority wins; unreachable shelters do not reserve cargo. If no shelter is reachable, vehicles wait without consuming stock. Renewing an incident extends or preserves its expiry.

Assigned trips retain their geometry and duration. Any closed road in that assigned path pauses the whole trip in place, outbound or returning; expiry resumes it. Return uses the same chosen route. This is dispatch-time rerouting, not edge-by-edge movement or in-flight diversion. Default scenarios without incidents keep their old behavior and hashes. Engine 3.1 reads original 2.0/2.1/2.4/2.5/3.0 histories; road incidents cannot be inserted into older engine archives. No schema migration is required.

Desktop road selection, red closed edges, alternate vehicle geometry, read-only replay, SQL checkpoints, completion preview, controller/observer and HTML/vehicle-CSV traces share the same model. The original engine 3.0 road fixture was generated with the preceding packaged DLLs. New console and WPF checks cover closures and compatibility. Included in desktop release 3.3.0.

# Included in 3.3.0 - responsive report export

Source builds generate verified PDF/HTML/CSV reports on a worker task. The full run is captured before exporting, so reset, replay seeking and incoming commands cannot change the requested report. Overlapping report exports are refused; errors release the guard for retry. Completion identifies the exported run. Live persistence remains synchronous, and cancellation is supported during reconstruction/staging; multi-file publication is not crash-atomic. All formats are staged before publishing; handled publication errors restore previous reports and remove partial new files. If rollback also fails, recoverable originals remain in the staging folder identified by the error. Process termination, power loss and concurrent exporters to the same destination are outside this rollback guarantee. Included in desktop release 3.3.0.

Report export now verifies the command history once and reuses that reconstruction for metrics and all output formats, replacing four full replays. Standalone PDF export uses one replay instead of two. Checks compare metrics across all five scenario generations and reject corrupt history at both public report entry points. This reduces duplicated work without changing simulation rules, archive hashes or report calculations; it is not a measured speedup claim.

# 3.0.1 - responsive saved history and replay

Desktop startup database work, saved-run listing, SQL history validation and A/B calculation now run on worker tasks. Timeline reconstruction is asynchronous and cooperatively cancellable between commands. Superseded seeks cannot publish stale results; reset/new scenarios cancel pending replay; corrupt input leaves the prior display intact. Closing the history window discards pending SQL results. Shared simulation rules, engine 3.0.0 and persisted hash formats are unchanged.

162 console checks plus real WPF flows cover this patch, including a 20,000-command cancellation/reset fixture and actual slider updates. The patch is a responsiveness improvement, not a large-scale throughput guarantee. Final map/event publication, live command persistence and report export are still synchronous. The self-contained 3.0.1 Windows package contains these desktop changes; 3.0.0 remains the previous release.

## Historical 3.0.0 - integrated roads and a complete Windows portfolio package

This release ships the previously source-only editor, fleet and priorities together with road-derived travel. A bounded WGS84 GeoJSON exercise compiles to schema 4: deterministic shortest paths, spherical road lengths rounded up per edge, projected road geometry, source hash and both-direction travel ticks. The map, replay, preview, host/observer and HTML report share the frozen compiled scenario. The supplied two-vehicle exercise delivers 30 packets and returns at tick 14.

SQLite now records schema version 1 and a migration ledger. Legacy upgrades first create a consistent SQLite backup, then perform additive changes and event backfill transactionally. Failed backfill rolls back; future or unrelated schemas are refused. The app reports the upgrade backup path. Original engine 2.0, 2.1, 2.4 and 2.5 fixtures retain their hashes and SQL payloads. New engine histories use 3.0.0.

Verification covers 157 executable console checks, warning-free Release build, interactive WPF flows and two-window reconnect. The self-contained Windows x64 ZIP includes its runtime, local/seeded/multiplayer launchers, optional per-user installer, sample GeoJSON and Bangla lessons 1-17. Extract it and run `Start-Local.cmd`; select Tools / Road network demo.

Road paths are static, positive-weight and undirected. Service closures freeze assigned outbound trips, not shared road edges. Schema 4 derives return time from the outbound path; its serialized returnTicks=0 is an unused placeholder. Geometry is edited in source GeoJSON, not the generic editor. The separate layer inspector remains view-only. Deployment and broad acceptance limits below still apply.

## Historical 2.5 source - static shelter priorities

Fleet scenarios accept ranks 0-9. Dispatch selects the highest-priority open shelter with unreserved demand; equal ranks retain list order. Closed routes are skipped. Existing cargo is not reassigned, and ranks are static per scenario. Scarcity can defer lower-priority shelters; no fairness or real-world urgency guarantee is claimed.

Editor, map, inspector, selected dispatch explanation, JSON/XML, replay and reports expose priorities. Priority CSV exports include engine-generated reasons. A 20-packet demo serves C with 18 and B with 2, then exhausts after the final return at tick 4. Exact restock of 28 permits completion at tick 12.

Engine 2.5 reads original 2.0/2.1/2.4 histories. Zero priority and null reasons preserve old serialization and hashes. Original 2.4 SQL fixture coverage checks commands, events and checkpoints. Nonzero priority cannot masquerade as an older engine document. 137 executable console checks and extended WPF flows cover the increment.

This source increment does not publish a new binary; the downloadable 2.2.0 ZIP lacks the editor, fleet and priorities. Remaining: geographic simulation integration, database migration/scale work and deployment/acceptance gates below.

## Historical 2.4 source - shared-stock multi-vehicle simulation

Schema 3 supports 1-8 identical-capacity vehicles, stable vehicle IDs and shared stock. Movement precedes dispatch; only vehicles at depot at tick start may dispatch. Outstanding cargo is reserved against shelter demand, preventing double allocation. All vehicles return before completion or supply exhaustion. Closure freezes affected outbound vehicles only. Dispatch order remains shelter-list order; priority allocation is future work.

A tick stores an aggregate event containing ordered per-vehicle actions, so SQLite keeps its existing (RunId, Tick) key. Fleet state enters hashes/checkpoints. Engine 2.4 reads original engine 2.0/2.1 archives with their existing hashes and normalized SQL payloads. New histories require 2.4.

Map markers, vehicle event rows, editor count, JSON/XML, preview, metrics, reports and local host/observer all understand fleet state. Reports add a vehicle CSV; movement counts sum vehicle-ticks. 121 console checks plus interactive flows verify the increment. Demo: three vehicles deliver 48 in 8 ticks; the same one-vehicle settings take 24 ticks.

No new binary release was published for this source increment; the downloadable 2.2.0 ZIP lacks the editor and fleet model. Remaining: priority dispatch, geographic simulation integration, migration/scale work and deployment/acceptance gates below.

## Historical 2.3 source - local scenario editor

Adds a separate draft editor for schema 1/2 scenarios: editable shelter table, bounded numeric fields, stock/travel settings, validation, JSON/XML save and creation of a new saved run. Original scenario/history remains available. Editor is local only. Failed validation or storage keeps the draft open.

101 console checks and extended real WPF flows cover this increment. The simulation engine stays 2.1.0; persisted hash semantics do not change. The published 2.2.0 binary remains the previous release and does not contain the editor.

Remaining: multiple vehicles, priority dispatch, geographical simulation integration, migration/performance work and the deployment/acceptance items below.

## Historical 2.2.0 — offline inspection and planning

This desktop release combines the 2.1 finite-stock/travel increment with three review tools:

- Offline GeoJSON viewer: regional WGS84 FeatureCollections with Point, LineString and simple Polygon geometry; layer toggles, feature inspection and existing pan/zoom controls. Validation failures preserve the previous good layer.
- Completion preview: background continuation of a verified copy from the live state or selected replay tick. Reports completion, supply exhaustion or the model tick limit without submitting commands.
- Diagnostics export and reproducible engine/replay profiling, with explicit measurement boundaries.

The simulation engine remains **2.1.0** because this release does not change its rules or hash contract. Original 2.0 and 2.1 histories remain supported. Desktop/package version is 2.2.0. No trained model, new routing algorithm or geographical travel physics is implied.

Verification: 89 executable console checks, real WPF interactions for the new tools, stock/travel controls, replay and two-client reconnect. CI runs the console checks on Windows; interactive checks run locally.

## Remaining work and evidence limits

The supplied blueprint is broader than this tested portfolio release. The 3.0 package includes local editing, multiple vehicles, priority dispatch, persisted road-to-scenario associations and a tested versioned database upgrade. General GIS formats/projections, evaluated real-world routing/traffic behavior, an evaluated ML model and large-history UI performance remain future work.

Cloud deployment and signed installation are separate deployment work requiring a hosting target and signing certificate. Clean physical-machine installation, a comprehensive accessibility/DPI audit and a recorded demonstration have not been established. There is no claim that all blueprint acceptance gates are complete.

## Reviewer path

Run the stock/travel exercise, inspect a GeoJSON layer, open a completion preview, export diagnostics, replay saved history, and run the same verification script as CI. See [demo script](demo-script.md) and [verification](verification.md) for exact commands and reproducible outcomes.
