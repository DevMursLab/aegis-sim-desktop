# 2.3 source - local scenario editor

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
