using System.Globalization;
using System.Management;

namespace XartZans.WinManager.App.Services;

public sealed class WmiHardwareInventoryService(IAppLogger logger)
{
    private readonly WindowsReleaseDateService _releaseDates = new();

    public HardwareInventory Read(CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Hardware inventory is available only on Windows.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var processor = ReadClass(
            "Win32_Processor",
            ["Name", "NumberOfCores", "NumberOfLogicalProcessors", "MaxClockSpeed"],
            item => new ProcessorHardware(
                ReadText(item, "Name"),
                ReadUInt32(item, "NumberOfCores"),
                ReadUInt32(item, "NumberOfLogicalProcessors"),
                ReadUInt32(item, "MaxClockSpeed"))
        ).FirstOrDefault();

        cancellationToken.ThrowIfCancellationRequested();
        var computer = ReadClass(
            "Win32_ComputerSystem",
            ["Manufacturer", "Model", "TotalPhysicalMemory"],
            item => new ComputerSystemHardware(
                ReadText(item, "Manufacturer"),
                ReadText(item, "Model"),
                ReadUInt64(item, "TotalPhysicalMemory"))
        ).FirstOrDefault();

        cancellationToken.ThrowIfCancellationRequested();
        var operatingSystem = ReadClass(
            "Win32_OperatingSystem",
            ["Caption", "Version", "BuildNumber", "OSArchitecture"],
            item => new OperatingSystemHardware(
                ReadText(item, "Caption"),
                ReadText(item, "Version"),
                ReadText(item, "BuildNumber"),
                ReadText(item, "OSArchitecture"),
                null)
        ).FirstOrDefault() is { } system
            ? system with { ReleaseDate = _releaseDates.GetInstalledReleaseDate() }
            : null;

        cancellationToken.ThrowIfCancellationRequested();
        var graphics = ReadClass(
            "Win32_VideoController",
            ["Name", "DriverVersion"],
            item => new GraphicsAdapterHardware(ReadText(item, "Name"), ReadText(item, "DriverVersion")));

        cancellationToken.ThrowIfCancellationRequested();
        var memory = ReadClass(
            "Win32_PhysicalMemory",
            ["DeviceLocator", "Manufacturer", "PartNumber", "Model", "Capacity", "Speed", "ConfiguredClockSpeed"],
            item => new MemoryModuleHardware(
                ReadText(item, "DeviceLocator"),
                ReadText(item, "Manufacturer"),
                ReadText(item, "PartNumber"),
                ReadText(item, "Model"),
                ReadUInt64(item, "Capacity"),
                ReadUInt32(item, "Speed"),
                ReadUInt32(item, "ConfiguredClockSpeed")));

        cancellationToken.ThrowIfCancellationRequested();
        var motherboard = ReadClass(
            "Win32_BaseBoard",
            ["Manufacturer", "Product", "Version"],
            item => new MotherboardHardware(
                ReadText(item, "Manufacturer"),
                ReadText(item, "Product"),
                ReadText(item, "Version"))
        ).FirstOrDefault();

        cancellationToken.ThrowIfCancellationRequested();
        var bios = ReadClass(
            "Win32_BIOS",
            ["Manufacturer", "SMBIOSBIOSVersion", "ReleaseDate"],
            item => new BiosHardware(
                ReadText(item, "Manufacturer"),
                ReadText(item, "SMBIOSBIOSVersion"),
                ReadWmiDate(item, "ReleaseDate"))
        ).FirstOrDefault();

        cancellationToken.ThrowIfCancellationRequested();
        var disks = ReadClass(
            "Win32_DiskDrive",
            ["Model", "Size", "InterfaceType", "Status"],
            item => new PhysicalDiskHardware(
                ReadText(item, "Model"),
                ReadUInt64(item, "Size"),
                ReadText(item, "InterfaceType"),
                ReadText(item, "Status")));

        return new HardwareInventory(processor, computer, operatingSystem, graphics, memory, motherboard, bios, disks, DateTimeOffset.Now);
    }

    private IReadOnlyList<T> ReadClass<T>(string className, string[] properties, Func<ManagementBaseObject, T> map)
    {
        try
        {
            var query = $"SELECT {string.Join(", ", properties)} FROM {className}";
            using var searcher = new ManagementObjectSearcher(query);
            using var results = searcher.Get();
            var items = new List<T>();
            foreach (ManagementObject item in results)
            {
                using (item)
                {
                    items.Add(map(item));
                }
            }

            return items;
        }
        catch (Exception exception)
        {
            logger.Error($"WMI hardware query failed for {className}.", exception);
            return [];
        }
    }

    private static string? ReadText(ManagementBaseObject item, string propertyName)
    {
        var value = item[propertyName]?.ToString()?.Trim();
        return string.IsNullOrWhiteSpace(value) ||
               value.Equals("To be filled by O.E.M.", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("Default string", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("System Product Name", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("Unknown", StringComparison.OrdinalIgnoreCase)
            ? null
            : value;
    }

    private static uint? ReadUInt32(ManagementBaseObject item, string propertyName)
    {
        try
        {
            var value = item[propertyName];
            return value is null ? null : Convert.ToUInt32(value, CultureInfo.InvariantCulture);
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
        {
            return null;
        }
    }

    private static ulong? ReadUInt64(ManagementBaseObject item, string propertyName)
    {
        try
        {
            var value = item[propertyName];
            return value is null ? null : Convert.ToUInt64(value, CultureInfo.InvariantCulture);
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
        {
            return null;
        }
    }

    private static string? ReadWmiDate(ManagementBaseObject item, string propertyName)
    {
        var value = ReadText(item, propertyName);
        if (value is null)
        {
            return null;
        }

        try
        {
            return ManagementDateTimeConverter.ToDateTime(value).ToString("yyyy-MM-dd", CultureInfo.CurrentCulture);
        }
        catch (Exception exception) when (exception is FormatException or ArgumentOutOfRangeException or ManagementException)
        {
            return null;
        }
    }
}
