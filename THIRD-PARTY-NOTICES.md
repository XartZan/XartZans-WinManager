# Third-party notices

This file lists packages used by the application and its tests. The application
does not modify the third-party libraries listed below. Package versions should
be re-audited whenever dependencies change.

## Application dependencies

| Package | Version | License | Purpose |
| --- | --- | --- | --- |
| LibreHardwareMonitorLib | 0.9.6 | MPL-2.0 | Reads available CPU, GPU, memory, and motherboard sensor data. |
| BlackSharp.Core | 1.0.7 | MPL-2.0 | Transitive dependency of LibreHardwareMonitorLib. |
| DiskInfoToolkit | 1.1.2 | MPL-2.0 | Transitive dependency of LibreHardwareMonitorLib. |
| RAMSPDToolkit-NDD | 1.4.2 | MPL-2.0 | Transitive dependency of LibreHardwareMonitorLib. |
| HidSharp | 2.6.4 | Apache-2.0 | Transitive hardware-device access dependency. Its bundled notice is in `licenses/HidSharp-Apache-2.0.txt`. |
| Mono.Posix.NETStandard | 1.0.0 | See package metadata | Transitive dependency; its package metadata links to [Microsoft's license information](https://go.microsoft.com/fwlink/?linkid=869050). |
| System.Management | 10.0.12 | MIT | Windows WMI inventory. |
| System.IO.Ports | 10.0.3 | MIT | Transitive dependency. |
| System.IO.FileSystem.AccessControl | 5.0.0 | MIT | Transitive dependency. |
| Microsoft.NET.ILLink.Tasks | 10.0.13 | MIT | Automatically referenced .NET build/publish tooling. |

The MPL-2.0 text is included at `licenses/MPL-2.0.txt`. LibreHardwareMonitorLib
and its MPL-licensed dependencies are available from the
[LibreHardwareMonitor source repository](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor).
The separate `RAMSPDToolkit-NDD` package is also MPL-2.0. HidSharp's complete
package notice is included alongside the MPL text.

## Test dependencies

| Package family | Resolved versions | License |
| --- | --- | --- |
| xUnit (`xunit`, `xunit.assert`, `xunit.core`, `xunit.abstractions`, analyzers and extensibility packages) | 2.9.3; abstractions 2.0.3; analyzers 1.18.0 | Apache-2.0 |
| `xunit.runner.visualstudio` | 3.1.4 | Apache-2.0 |
| Microsoft test platform (`Microsoft.NET.Test.Sdk`, ObjectModel, TestHost, CodeCoverage) | 17.14.1 | MIT |
| `Newtonsoft.Json` | 13.0.3 | MIT |

These are development/test dependencies and are not application NuGet dependencies.

## Microsoft runtime

The Windows x64 self-contained publish includes the applicable .NET 10 runtime
and Windows Presentation Foundation files. Microsoft runtime components are
distributed under their applicable Microsoft notices and licenses; see the
[.NET license information](https://dotnet.microsoft.com/en-us/dotnet/core/license).
