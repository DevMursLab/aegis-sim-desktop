# AEGIS-SIM Desktop 3.3.0

Windows x64 desktop simulator with its runtime included. Simulation engine: **3.2.0**.

## New since 3.0.1

- Shared road closures with deterministic open-route selection for new dispatches.
- Explicit junction routing: finish an entered road, then select a new open path at a reached node, outbound or returning.
- Separate synthetic forecast lab: 800 generated schema-2 starts, 627 training and 173 holdout groups, training-only scaling and exportable model/evaluation. Holdout MAE 6.62 ticks versus 25.78 for the constant baseline; worst error 62.55 ticks. No real-world accuracy claim.
- Background verified report export with cancellation, frozen-run inputs and rollback after handled publication failures.
- Local playback pacing at 1x, 2x and 4x without modifying recorded history.
- Reviewer guide, screenshot gallery, source/package manifest, checksums, reusable package verifier and Bangla interview preparation.

## Run

1. Download the Windows x64 ZIP and its `.sha256` file.
2. Verify the hash with `Get-FileHash <archive.zip> -Algorithm SHA256`.
3. Extract the complete ZIP to a writable folder.
4. Run `Start-Seeded-Demo.cmd` or `Start-Local.cmd`.
5. Optional: run `Install.ps1` for a per-user installation, without administrator rights.

No SDK, cloud account or external map tiles are required. For junction routing, open Tools / Road network demo, enable the mode while all vehicles are at the depot, step three times, select road-03 and close it. Inspect the detour and then the saved replay.

## Verification and scope

The source verification covers a locked restore, a Release build with zero warnings/errors, **212 console checks**, three lessons, actual WPF flows and controller/observer host restart/reconnect with no binding errors. Release package verification additionally checks ZIP integrity and checksum, isolated extraction, fresh and seeded startup, packaged profiling/forecast tools, custom-directory installation, installed startup and road-host health. See the accompanying `.verification.json` asset for the exact package hash and source commit.

This is an independent portfolio simulator with fictional civilian data. It does not reproduce an institution's internal system. Networking is local/loopback-only. No real-traffic, high-load, clean-physical-machine, signed-installer or comprehensive accessibility/DPI claim is made. Original supported engine histories remain readable; older binaries cannot open new engine 3.2 histories.
