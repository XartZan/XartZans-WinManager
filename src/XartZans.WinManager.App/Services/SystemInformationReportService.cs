using System.Globalization;
using System.Text;
using XartZans.WinManager.Core.Formatting;

namespace XartZans.WinManager.App.Services;

public sealed class SystemInformationReportService(
    WmiHardwareInventoryService inventoryService,
    SystemSnapshotService snapshotService,
    HardwareTemperatureService sensorService,
    IAppLogger logger)
{
    private static readonly string[] LabelKeys =
    [
        "ReportTitle", "ReportGeneratedAt", "ReportComputerSection", "ComputerName", "ComputerModel",
        "OperatingSystem", "OperatingSystemBuild", "OperatingSystemReleaseDate", "HardwareArchitecture",
        "HardwareProcessor", "ProcessorCores", "ProcessorThreads", "ProcessorMaxFrequency", "ReportCpuLoad",
        "GraphicsStatusTitle", "GraphicsUsage", "GpuDriverVersion", "HardwareMemory", "MemoryModules", "MemoryUsage",
        "MemoryLocation", "MemoryModuleModel", "MemoryCapacity", "MemoryMaxSpeed", "MemoryConfiguredSpeed",
        "HardwareMotherboard", "BoardManufacturer", "BoardProduct", "BoardVersion", "HardwareBios",
        "BiosManufacturer", "BiosVersion", "BiosDate", "HardwareDisks", "DiskModel", "DiskSize",
        "DiskInterface", "DiskStatus", "SystemDrive", "DiskFree", "DriveUsage", "ReportNetwork", "NetworkDownload",
        "NetworkUpload", "TemperatureTitle", "ReportHardwareSensors", "HardwareSensorsNote", "MetricUnavailable",
        "MetricDisk", "TemperatureUnavailable", "ReleaseDateUnavailable"
    ];

    public Task<string> CreateAsync(CancellationToken cancellationToken = default)
    {
        var labels = LabelKeys.ToDictionary(key => key, LocalizationService.Get, StringComparer.Ordinal);
        return Task.Run(() => CreateReport(labels, cancellationToken), cancellationToken);
    }

    private string CreateReport(IReadOnlyDictionary<string, string> labels, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var inventory = inventoryService.Read(cancellationToken);
        SystemSnapshot? snapshot;
        try
        {
            snapshot = snapshotService.Capture();
        }
        catch (Exception exception)
        {
            logger.Error("Could not capture live metrics while creating the system report.", exception);
            snapshot = null;
        }

        var sensors = sensorService.Capture();
        var builder = new StringBuilder(8_192);
        builder.AppendLine(labels["ReportTitle"]);
        builder.AppendLine($"{labels["ReportGeneratedAt"]}: {DateTimeOffset.Now.LocalDateTime.ToString("G", CultureInfo.CurrentCulture)}");
        builder.AppendLine(new string('=', 64));

        AddSection(builder, labels["ReportComputerSection"]);
        AddValue(builder, labels["ComputerName"], snapshot?.ComputerName ?? Environment.MachineName, labels);
        AddValue(builder, labels["ComputerModel"], Join(inventory.ComputerSystem?.Manufacturer, inventory.ComputerSystem?.Model), labels);

        var operatingSystem = inventory.OperatingSystem;
        AddValue(builder, labels["OperatingSystem"], operatingSystem?.Name ?? snapshot?.OperatingSystem, labels);
        AddValue(builder, labels["OperatingSystemBuild"], Join(operatingSystem?.Version, operatingSystem?.Build), labels);
        AddValue(builder, labels["OperatingSystemReleaseDate"],
            operatingSystem?.ReleaseDate?.ToString("d", CultureInfo.CurrentCulture) ?? labels["ReleaseDateUnavailable"], labels);
        AddValue(builder, labels["HardwareArchitecture"], operatingSystem?.Architecture, labels);

        AddSection(builder, labels["HardwareProcessor"]);
        var processor = inventory.Processor;
        AddValue(builder, labels["HardwareProcessor"], processor?.Name ?? snapshot?.ProcessorModel, labels);
        AddValue(builder, labels["ProcessorCores"], processor?.Cores?.ToString(CultureInfo.CurrentCulture), labels);
        AddValue(builder, labels["ProcessorThreads"], processor?.LogicalProcessors?.ToString(CultureInfo.CurrentCulture), labels);
        AddValue(builder, labels["ProcessorMaxFrequency"],
            processor?.MaximumClockMhz is uint frequency && frequency > 0 ? $"{frequency:N0} MHz" : null, labels);
        AddValue(builder, labels["ReportCpuLoad"], FormatPercent(snapshot?.CpuUsagePercent), labels);

        AddSection(builder, labels["GraphicsStatusTitle"]);
        if (inventory.GraphicsAdapters.Count > 0)
        {
            foreach (var adapter in inventory.GraphicsAdapters)
            {
                AddValue(builder, labels["GraphicsStatusTitle"], adapter.Name, labels);
                AddValue(builder, labels["GpuDriverVersion"], adapter.DriverVersion, labels, indent: true);
            }
        }
        else
        {
            AddValue(builder, labels["GraphicsStatusTitle"], snapshot?.GraphicsModel, labels);
        }
        AddValue(builder, labels["GraphicsUsage"], FormatPercent(sensors.GpuUsagePercent), labels);

        AddSection(builder, labels["HardwareMemory"]);
        var totalMemory = inventory.MemoryModules
            .Where(module => module.CapacityBytes is > 0)
            .Aggregate(0UL, (total, module) => total + module.CapacityBytes!.Value);
        if (totalMemory == 0)
        {
            totalMemory = inventory.ComputerSystem?.InstalledMemoryBytes ?? 0;
        }
        AddValue(builder, labels["MemoryCapacity"], totalMemory > 0 ? MetricFormatter.FormatBytes(totalMemory) : null, labels);
        var memoryUsage = snapshot is { TotalMemoryBytes: > 0 }
            ? $"{MetricFormatter.FormatBytes(snapshot.UsedMemoryBytes)} / {MetricFormatter.FormatBytes(snapshot.TotalMemoryBytes)} ({FormatPercent((double)snapshot.UsedMemoryBytes / snapshot.TotalMemoryBytes * 100)})"
            : null;
        AddValue(builder, labels["MemoryUsage"], memoryUsage, labels);
        AddValue(builder, labels["MemoryModules"], inventory.MemoryModules.Count.ToString(CultureInfo.CurrentCulture), labels);
        foreach (var module in inventory.MemoryModules)
        {
            AddValue(builder, Join(labels["MemoryLocation"], module.Location), Join(module.Manufacturer, module.PartNumber, module.Model), labels);
            AddValue(builder, labels["MemoryCapacity"], module.CapacityBytes is > 0 ? MetricFormatter.FormatBytes(module.CapacityBytes.Value) : null, labels, indent: true);
            AddValue(builder, labels["MemoryMaxSpeed"], module.MaximumDataRateMtps is > 0 ? $"{module.MaximumDataRateMtps:N0} MT/s" : null, labels, indent: true);
            AddValue(builder, labels["MemoryConfiguredSpeed"], module.ConfiguredClockMhz is > 0 ? $"{module.ConfiguredClockMhz:N0} MHz" : null, labels, indent: true);
        }

        AddSection(builder, labels["HardwareMotherboard"]);
        AddValue(builder, labels["BoardManufacturer"], inventory.Motherboard?.Manufacturer, labels);
        AddValue(builder, labels["BoardProduct"], inventory.Motherboard?.Product, labels);
        AddValue(builder, labels["BoardVersion"], inventory.Motherboard?.Version, labels);

        AddSection(builder, labels["HardwareBios"]);
        AddValue(builder, labels["BiosManufacturer"], inventory.Bios?.Manufacturer, labels);
        AddValue(builder, labels["BiosVersion"], inventory.Bios?.Version, labels);
        AddValue(builder, labels["BiosDate"], inventory.Bios?.ReleaseDate, labels);

        AddSection(builder, labels["HardwareDisks"]);
        if (inventory.PhysicalDisks.Count == 0)
        {
            AddValue(builder, labels["HardwareDisks"], null, labels);
        }
        foreach (var disk in inventory.PhysicalDisks)
        {
            AddValue(builder, labels["DiskModel"], disk.Model, labels);
            AddValue(builder, labels["DiskSize"], disk.CapacityBytes is > 0 ? MetricFormatter.FormatBytes(disk.CapacityBytes.Value) : null, labels, indent: true);
            AddValue(builder, labels["DiskInterface"], disk.InterfaceType, labels, indent: true);
            AddValue(builder, labels["DiskStatus"], disk.WindowsWmiStatus, labels, indent: true);
        }

        AddSection(builder, labels["SystemDrive"]);
        AddValue(builder, labels["SystemDrive"], snapshot?.SystemDrive?.Name, labels);
        AddValue(builder, labels["DiskSize"], snapshot?.SystemDrive is { } drive ? MetricFormatter.FormatBytes(drive.TotalBytes) : null, labels, indent: true);
        AddValue(builder, labels["DiskFree"], snapshot?.SystemDrive is { } systemDrive ? MetricFormatter.FormatBytes(systemDrive.FreeBytes) : null, labels, indent: true);
        AddValue(builder, labels["DriveUsage"], FormatPercent(snapshot?.SystemDrive?.UsagePercent), labels, indent: true);

        AddSection(builder, labels["ReportNetwork"]);
        AddValue(builder, labels["NetworkDownload"], MetricFormatter.FormatRate(snapshot?.DownloadBytesPerSecond), labels);
        AddValue(builder, labels["NetworkUpload"], MetricFormatter.FormatRate(snapshot?.UploadBytesPerSecond), labels);

        AddSection(builder, labels["ReportHardwareSensors"]);
        if (sensors.Readings.Count == 0)
        {
            AddValue(builder, labels["ReportHardwareSensors"], labels["TemperatureUnavailable"], labels);
        }
        else
        {
            foreach (var reading in sensors.Readings)
            {
                var sensorType = reading.SensorType.ToString();
                var value = $"{reading.Value.ToString("N2", CultureInfo.CurrentCulture)}{SensorUnit(sensorType)}";
                AddValue(builder, $"{reading.Hardware} · {reading.Name} ({sensorType})", value, labels);
            }
        }

        builder.AppendLine();
        builder.AppendLine(labels["HardwareSensorsNote"]);
        return builder.ToString();
    }

    private static void AddSection(StringBuilder builder, string title)
    {
        builder.AppendLine();
        builder.AppendLine(title.ToUpper(CultureInfo.CurrentCulture));
        builder.AppendLine(new string('-', Math.Min(64, Math.Max(12, title.Length))));
    }

    private static void AddValue(StringBuilder builder, string label, string? value, IReadOnlyDictionary<string, string> labels, bool indent = false)
    {
        var printableValue = string.IsNullOrWhiteSpace(value) ? labels["MetricUnavailable"] : value;
        builder.Append(indent ? "  " : string.Empty).Append(label).Append(": ").AppendLine(printableValue);
    }

    private static string FormatPercent(double? value) => value is double percent
        ? MetricFormatter.FormatPercent(percent)
        : string.Empty;

    private static string Join(params string?[] parts) => string.Join(" ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));

    private static string SensorUnit(string sensorType) => sensorType switch
    {
        "Temperature" => " °C",
        "Load" or "Control" => " %",
        "Clock" => " MHz",
        "Fan" => " RPM",
        "Voltage" => " V",
        "Power" => " W",
        "Current" => " A",
        "Energy" => " Wh",
        "Frequency" => " Hz",
        "Throughput" => " B/s",
        "Flow" => " L/min",
        "TimeSpan" => " ms",
        _ => string.Empty
    };
}
