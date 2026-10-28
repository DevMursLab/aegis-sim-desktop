# AEGIS-SIM Desktop 3.3.1

Windows x64 desktop application with its runtime included. Simulation engine: **3.2.0**.

## Changes

This documentation/distribution patch removes personal learning and interview notes from the package, adds a technical user guide and simplifies the repository landing page. Simulation rules, history formats and functionality are unchanged from 3.3.0: shared road incidents, junction routing, fleet priorities, synthetic forecast lab, cancellable report export and local playback controls remain available.

## Run

Download the ZIP and checksum, verify with `Get-FileHash <archive.zip> -Algorithm SHA256`, extract the complete archive, then run `Start-Seeded-Demo.cmd` or `Start-Local.cmd`. No SDK is required. Optional `Install.ps1` provides a per-user installation without administrator rights. See `docs/user-guide.md`.

## Verification and boundaries

The application is covered by 212 console checks, three reference samples, warning-free Release builds and WPF/controller-observer reconnect checks. The accompanying `.verification.json` records the exact package hash and source commit and checks ZIP integrity, isolated startup, seeded replay, tools, installation and host health.

Independent portfolio software using fictional civilian data. Local/loopback networking only. No field-validated forecasting, real traffic, high-load, clean-physical-machine, signed-installer or comprehensive accessibility/DPI claim. Supported older histories remain readable; old binaries cannot open new engine 3.2 histories.
