# Architecture and first-release scope

## Projects

- `src/XartZans.WinManager.App`: WPF shell, views, MVVM view models, Windows system
  snapshot and WMI hardware services, external JSON localization, and file logger.
- `src/XartZans.WinManager.Core`: operating-system-independent metric calculation
  and formatting logic.
- `tests/XartZans.WinManager.Core.Tests`: unit tests for core calculations.

The Windows x64 package is self-contained. `installer/XartZans-WinManager.iss`
builds a per-user Setup EXE from the publish folder; GitHub Actions attaches it to
releases when a `v*` tag is pushed.

The UI observes immutable system snapshots. Windows API, WMI, and sensor reads run
away from the WPF dispatcher. The System Information screen refreshes every two
seconds. There are no administrator prompts or system-changing operations in this
first release.

## Current data sources

- CPU: `GetSystemTimes` delta calculation.
- Memory: `GlobalMemoryStatusEx`.
- System drive: `DriveInfo` for the Windows system volume.
- Network: aggregate byte counters for active, non-loopback, non-tunnel interfaces.
- OS and processor model: Windows version and registry values.
- Graphics adapter model: primary display adapter via `EnumDisplayDevices`.

WMI inventory reads processor details, computer and OS identification, video
adapter names and driver versions, memory modules, motherboard, BIOS, and physical
disk model/status. For each memory module, the SMBIOS maximum data rate is displayed
in MT/s separately from the configured clock in MHz. The SMBIOS Type 17 Speed field
is documented by DMTF as the device's maximum capable data rate; the configured
speed is a separate field. A disk WMI status is not a SMART health test.
LibreHardwareMonitor supplies real GPU load and CPU, GPU, and memory temperature
readings when the device exposes them. The report exporter includes the full WMI
inventory, current usage readings, and every readable hardware sensor. Sensor access
can depend on hardware, firmware, and permissions; the app does not elevate or
install a driver, and unavailable values stay unavailable instead of receiving
estimates.

Windows release availability dates are kept in `src/XartZans.WinManager.App/Data/windows-release-dates.json`
and sourced from Microsoft's Windows release information pages. Unknown releases
remain unavailable rather than being guessed.

## Localization and theme

Turkish and English ship as JSON catalogs. On first start they are copied to the
user-writable `%LOCALAPPDATA%\XartZans\WinManager\Languages` folder. The animated
language menu reloads the catalog list when opened, so users can add or remove
catalogs without rebuilding the app. Missing keys fall back to English. Black and
white palettes are selected from the Tools > Themes page and saved per user.
LibreHardwareMonitorLib is used under MPL-2.0; details and
the license text are included in `THIRD-PARTY-NOTICES.md` and `licenses/`.
