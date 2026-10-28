# Three-minute review demo

Prepare: verify the source, then run the seed tool or extract a packaged release. Use `Start-Seeded-Demo.cmd` for a prepared pair of runs. Use an interactive Windows desktop; keep the host local.

| Time | Action | Evidence |
|---|---|---|
| 0:00–0:20 | Open the app; identify the synthetic scenario, run ID and map units | Clear scope and observable state |
| 0:20–0:50 | Reset to a fresh run, select A, close its route, Start | Incident alters availability; other shelters receive supply |
| 0:50–1:10 | Pause, Step, Resume; observe waiting ticks and completion | Valid lifecycle and consistent map/table/history |
| 1:10–1:40 | Close/reopen app, open saved history, seek replay ticks | Durable SQL storage and reproducible state, not a video |
| 1:40–2:05 | Select baseline and changed runs with Ctrl+click; Compare | 6 versus 8 ticks; same scenario and delivered quantity |
| 2:05–2:25 | Export PDF/HTML/CSV; open a report | Numbers and state hash come from verified replay |
| 2:25–2:50 | Open controller/observer demo; inject an incident | Host authority and observer read-only UI/server boundary |
| 2:50–3:00 | Show verification command and successful CI | Reproducible review path |

For a longer technical review, stop/restart the host while clients stay open. Watch stale/reconnecting state and snapshot recovery. The automated two-window smoke test exercises this actual interruption.

## Questions to explain using the source

- Why does the engine avoid DateTime/random I/O?
- Why commit before broadcasting a successful command?
- How does command identity differ from run version and room revision?
- Why are loaded runs read-only, and why does Reset create a new ID?
- How do map coordinates differ from screen coordinates and GPS?
- Why is mean service tick not real-world waiting time?
- Which parts would need changing before internet deployment or large scenarios?

State the limits plainly: legacy instant-visit scenarios, configurable stock/travel and bounded road exercises coexist. Distances are a spherical approximation; local demo tokens and synchronous small-run persistence target a portfolio demo. The separate forecast lab uses generated data and does not establish field-validated predictive accuracy.

## Optional stock/travel extension (2.1 source)

Select **Stock + travel demo**. Step twice: stock is reserved and cargo is moving, but delivered remains zero. Continue to tick 16: 30 delivered, 18 pending, SupplyExhausted. Restock +20, then step to completion at tick 24: 48 delivered, 2 in stock and vehicle at depot. Save/reload or replay the run to inspect the same inventory history. Baseline 6/8 comparisons above remain schema 1.

## Offline review tools (2.2 desktop)

Open Tools / Completion preview before stepping: observe the same deterministic endpoint without changing commands. Open Tools / Offline GeoJSON layers and load assets/maps/synthetic-region.geojson; select the fictional depot and toggle areas/lines/points. These geographical layers are a separate inspection view. Finally export diagnostics; show engine version, verified hash and the explicit timing caveat.

## Road exercise (3.0 release)

Open Tools / Road network demo. Select Road Shelter-B and Step twice: cargo is reserved, two markers follow the imported road, and the inspector shows 890 m / 3 ticks each way. Completion preview predicts tick 14. Continue until all 30 packets arrive and both vehicles return at tick 14. Replay tick 2 to reproduce the same road positions; export HTML to inspect source SHA-256 and route distances. Explain static paths, priority order, and the difference between a shelter service closure and a shared road-edge closure.

The generic editor is disabled for this exercise. Change metersPerTick in a copy of assets/maps/road-exercise.geojson and import it as a new run. Original saved history retains its frozen paths. Existing legacy SQLite databases are backed up before the first versioned upgrade; subsequent starts do not repeat that backup.

## Shared road incidents (3.1 source)

Start a new Road network demo. In the inspector select road-01 and choose Close road / 5 ticks, then Step twice. Show the red closed shared edge and vehicles following the long road-99 detour. Select a dispatch event to inspect the chosen road IDs, distance and travel duration. Finish the run, open its saved read-only replay and export HTML/vehicle CSV to inspect incidents and routing provenance. A road closed after dispatch pauses the assigned trip in place; the model reroutes new dispatches only.

## Junction diversion (3.2 source)

Start a fresh Road network demo and press Use junction routing. Step three times; both vehicles reach the first junction. Select road-03, close it for five ticks and Step again. B's vehicle goes right on road-02 instead of entering road-03; its shelter destination and reserved load are preserved. Inspect the movement event's junction-path reason. Seek the saved replay to tick 3 and verify the exact junction position, then tick 4 for the diversion. HTML explains per-edge timing; vehicle CSV retains path decisions. Closing a currently occupied edge lets the vehicle finish it, then it reconsiders the route at the next node.
