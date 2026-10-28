# Project status - desktop 3.3.0

The deliverable is a Windows desktop portfolio simulator using fictional civilian scenarios. This checklist describes that deliverable; it does not claim every research/deployment idea in the original blueprints is complete.

## Delivered

- [x] Deterministic simulation lifecycle, finite supply, restock and shortages.
- [x] Fleet of 1-8 vehicles, shared reservations and priority dispatch.
- [x] Offline map, selection, pan/zoom, scenario editor and bounded JSON/XML import/export.
- [x] Bounded GeoJSON inspection and integrated road-derived scenarios.
- [x] Shared road incidents and explicit junction-based rerouting.
- [x] EF Core/SQLite persistence, checked history, replay and supported legacy fixtures.
- [x] A/B comparison, PDF/HTML/CSV reports and background report export with cancellation.
- [x] Background completion preview, diagnostics and reproducible engine/replay profiling.
- [x] Local SignalR controller/observer roles, deduplication and restart/reconnect recovery.
- [x] Bounded synthetic forecast experiment with separated training/holdout groups and baseline comparison.
- [x] Release build, 212 console checks, lessons and actual WPF interaction/reconnect checks.
- [x] Self-contained Windows package, manifest/checksum generation and reusable extracted-package verification.
- [x] Screenshot gallery, architecture, reviewer path, demo instructions and Bangla interview preparation.

## Future work and external acceptance

| Item | Current boundary | What would establish completion |
|---|---|---|
| Real-world routing/forecast efficacy | Synthetic exercises and generated data | Licensed representative field data, agreed metrics and external evaluation |
| General GIS import | Bounded WGS84 GeoJSON subset | Define supported formats/projections and test fixtures; integrate a suitable adapter |
| Scale/performance | Small demo; engine/replay profile available | Agree workload/latency budgets, measure SQL/UI/network and optimize identified bottlenecks |
| Internet/cloud deployment | Loopback-only local host | Hosting target, identity/authentication model, transport configuration and operational review |
| Signed installer | Unsigned ZIP with optional per-user install | Signing certificate and distribution workflow |
| Broad installation/accessibility/DPI acceptance | Local Windows and isolated-package tests | Clean physical/VM environments, keyboard/screen-reader audit and monitor/DPI matrix |
| Recorded live demo | Written reproducible demo and actual screenshots | Record and review a live run; publish a video link |

These future items are not required to run the local portfolio demo and are not represented as finished in the CV or release description.

## Verify and review

Use [verification](verification.md), [the reviewer guide](REVIEWER-GUIDE.md), [the demo script](demo-script.md) and [interview preparation](INTERVIEW-PREP.bn.md). Each release includes a checksum; package checks emit a separate machine-readable verification artifact beside the ZIP.
