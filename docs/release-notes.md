# 2.5 source - static shelter priorities

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

The supplied blueprint is broader than this tested portfolio release. Multiple concurrent vehicles, visual scenario editing, priority allocation, persisted layer-to-scenario associations, general GIS formats/projections, an evaluated ML model, database migrations and large-history UI performance remain future work. The implementation currently uses one vehicle and a fixed dispatch order. GeoJSON inspection is view-only, not a geographical simulation adapter.

Cloud deployment and signed installation are separate deployment work requiring a hosting target and signing certificate. Clean physical-machine installation, a comprehensive accessibility/DPI audit and a recorded demonstration have not been established. There is no claim that all blueprint acceptance gates are complete.

## Reviewer path

Run the stock/travel exercise, inspect a GeoJSON layer, open a completion preview, export diagnostics, replay saved history, and run the same verification script as CI. See [demo script](demo-script.md) and [verification](verification.md) for exact commands and reproducible outcomes.
