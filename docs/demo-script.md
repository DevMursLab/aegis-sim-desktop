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

State the limits plainly: synthetic local-meter map, fresh load per visit, no travel-time physics, local demo tokens, synchronous small-run persistence and no predictive AI claim.

## Optional stock/travel extension (2.1 source)

Select **Stock + travel demo**. Step twice: stock is reserved and cargo is moving, but delivered remains zero. Continue to tick 16: 30 delivered, 18 pending, SupplyExhausted. Restock +20, then step to completion at tick 24: 48 delivered, 2 in stock and vehicle at depot. Save/reload or replay the run to inspect the same inventory history. Baseline 6/8 comparisons above remain schema 1.
