# Changelog

All notable changes to this project are documented here. This project follows
[Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- Animated language picker backed by editable JSON catalogs in the user profile.
- Black and white themes under Tools, persisted per user.
- Hardware and system page with read-only WMI details for CPU, graphics adapters,
  memory, motherboard, BIOS, Windows, and physical disks.
- Real GPU load and CPU, GPU, and memory temperature readings when hardware exposes them.
- System information export that writes available WMI, sensor, and usage data to a text file and opens it.
- Renamed the dashboard to System Information, moved the GPU card beside CPU, and changed module badges to Active.
- Removed the refresh-interval selector; dashboard updates use a fixed two-second interval.
- Added Windows release dates, full memory part/model details, and separate SMBIOS
  maximum data rate (MT/s) and configured clock (MHz).
- Added a per-user Setup EXE with desktop/Start menu shortcuts and a GitHub Release
  workflow for version tags.

## [0.1.0] - 2026-11-01

### Added

- Initial .NET 10 WPF application shell with an MVVM structure.
- Turkish default interface and runtime Turkish/English language switch.
- Dashboard with live CPU, memory, system-drive, and network measurements.
- Windows, processor, and primary graphics adapter identification where available.
- Explicit unavailable states for GPU utilization and temperature/fan sensors.
- Local application error logging and transparent placeholders for unfinished modules.
- Windows x64 single-file publish settings, unit test project, and GitHub Actions workflow.
