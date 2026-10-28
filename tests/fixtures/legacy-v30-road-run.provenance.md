# Original engine 3.0 road archive

`legacy-v30-road-run.json` was generated on 28 October 2026 by referencing `Aegis.Simulation.dll` and `Aegis.Infrastructure.dll` from the preceding local package `artifacts/report-export-20261028/app`, before the engine 3.1 source increment. The generator asserted engine 3.0.0, imported the shipped road-exercise GeoJSON and appended 14 Step commands using that binary. The final state hash is `4964D3B767E9A6F2D6C46D56DD0BD1672807D21757480C3670E599D898D7642E`.

The checked-in fixture is self-contained; CI does not require that local package. The release checks replay every original command checksum and verify a SQL save/load round trip. This is original binary command/hash provenance, not an original 3.0 SQL database fixture.
