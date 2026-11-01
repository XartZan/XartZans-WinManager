using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using XartZans.WinManager.Core.Metrics;

namespace XartZans.WinManager.App.Services;

public sealed class SystemSnapshotService
{
    private readonly object _captureSync = new();
    private CpuTimeSample? _previousCpuSample;
    private NetworkCounters? _previousNetworkCounters;

    public SystemSnapshot Capture()
    {
        lock (_captureSync)
        {
            return CaptureCore();
        }
    }

    private SystemSnapshot CaptureCore()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("System monitoring is available only on Windows.");
        }

        var now = DateTimeOffset.Now;
        var cpuUsage = ReadCpuUsage();
        var memory = ReadMemory();
        var network = ReadNetworkRates();

        return new SystemSnapshot(
            Environment.MachineName,
            ReadOperatingSystem(),
            ReadProcessorModel(),
            ReadGraphicsModel(),
            cpuUsage,
            memory.Used,
            memory.Total,
            ReadSystemDrive(),
            network.Download,
            network.Upload,
            now);
    }

    private double? ReadCpuUsage()
    {
        if (!GetSystemTimes(out var idle, out var kernel, out var user))
        {
            return null;
        }

        var current = new CpuTimeSample(ToUInt64(idle), ToUInt64(kernel), ToUInt64(user));
        var usage = _previousCpuSample is { } previous
            ? CpuUsageCalculator.Calculate(previous, current)
            : null;
        _previousCpuSample = current;
        return usage;
    }

    private static (ulong Used, ulong Total) ReadMemory()
    {
        var status = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
        if (!GlobalMemoryStatusEx(ref status))
        {
            return (0, 0);
        }

        return (status.TotalPhysical - status.AvailablePhysical, status.TotalPhysical);
    }

    private (double? Download, double? Upload) ReadNetworkRates()
    {
        try
        {
            ulong received = 0;
            ulong sent = 0;
            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel ||
                    networkInterface.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }

                try
                {
                    var statistics = networkInterface.GetIPStatistics();
                    received += (ulong)Math.Max(0, statistics.BytesReceived);
                    sent += (ulong)Math.Max(0, statistics.BytesSent);
                }
                catch (NetworkInformationException)
                {
                    // A single unavailable adapter should not hide counters from other adapters.
                }
            }

            var current = new NetworkCounters(received, sent, Stopwatch.GetTimestamp());
            var previous = _previousNetworkCounters;
            _previousNetworkCounters = current;
            if (previous is null || current.Received < previous.Received || current.Sent < previous.Sent)
            {
                return (null, null);
            }

            var elapsed = Stopwatch.GetElapsedTime(previous.Timestamp, current.Timestamp).TotalSeconds;
            return elapsed <= 0
                ? (null, null)
                : ((current.Received - previous.Received) / elapsed, (current.Sent - previous.Sent) / elapsed);
        }
        catch (NetworkInformationException)
        {
            return (null, null);
        }
    }

    private static DriveSnapshot? ReadSystemDrive()
    {
        try
        {
            var systemRoot = Path.GetPathRoot(Environment.SystemDirectory);
            var drive = DriveInfo.GetDrives().FirstOrDefault(candidate =>
                candidate.IsReady && candidate.DriveType == DriveType.Fixed &&
                string.Equals(candidate.Name, systemRoot, StringComparison.OrdinalIgnoreCase));
            if (drive is null || drive.TotalSize < 0 || drive.AvailableFreeSpace < 0)
            {
                return null;
            }

            var total = (ulong)drive.TotalSize;
            var free = (ulong)drive.AvailableFreeSpace;
            return new DriveSnapshot(drive.Name.TrimEnd(Path.DirectorySeparatorChar), total - free, total, free);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string ReadOperatingSystem()
    {
        var version = Environment.OSVersion.Version;
        var windowsName = version.Build >= 22000 ? "Windows 11" : "Windows 10";
        var product = Registry.GetValue(
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion",
            "EditionID",
            null) as string;
        var displayVersion = Registry.GetValue(
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion",
            "DisplayVersion",
            null) as string;

        var details = new[] { windowsName, product, displayVersion, $"build {version.Build}" }
            .Where(value => !string.IsNullOrWhiteSpace(value));
        return string.Join(" · ", details);
    }

    private static string? ReadProcessorModel()
    {
        try
        {
            return Registry.GetValue(
                @"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0",
                "ProcessorNameString",
                null) as string;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string? ReadGraphicsModel()
    {
        try
        {
            for (uint index = 0; index < 16; index++)
            {
                var device = DisplayDevice.Create();
                if (!EnumDisplayDevices(null, index, ref device, 0))
                {
                    break;
                }

                if ((device.StateFlags & 0x4) != 0 && !string.IsNullOrWhiteSpace(device.DeviceString))
                {
                    return device.DeviceString.TrimEnd('\0');
                }
            }
        }
        catch (EntryPointNotFoundException)
        {
            // Keep unavailable hardware details empty rather than presenting a guess.
        }

        return null;
    }

    private static ulong ToUInt64(System.Runtime.InteropServices.ComTypes.FILETIME value) =>
        ((ulong)(uint)value.dwHighDateTime << 32) | (uint)value.dwLowDateTime;

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(
        out System.Runtime.InteropServices.ComTypes.FILETIME idleTime,
        out System.Runtime.InteropServices.ComTypes.FILETIME kernelTime,
        out System.Runtime.InteropServices.ComTypes.FILETIME userTime);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayDevices(string? device, uint deviceNumber, ref DisplayDevice displayDevice, uint flags);

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DisplayDevice
    {
        public uint Size;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceString;
        public uint StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceKey;

        public static DisplayDevice Create() => new()
        {
            Size = (uint)Marshal.SizeOf<DisplayDevice>(),
            DeviceName = new string('\0', 32),
            DeviceString = new string('\0', 128),
            DeviceId = new string('\0', 128),
            DeviceKey = new string('\0', 128)
        };
    }

    private sealed record NetworkCounters(ulong Received, ulong Sent, long Timestamp);
}
