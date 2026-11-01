namespace XartZans.WinManager.App.Services;

public sealed record HardwareInventory(
    ProcessorHardware? Processor,
    ComputerSystemHardware? ComputerSystem,
    OperatingSystemHardware? OperatingSystem,
    IReadOnlyList<GraphicsAdapterHardware> GraphicsAdapters,
    IReadOnlyList<MemoryModuleHardware> MemoryModules,
    MotherboardHardware? Motherboard,
    BiosHardware? Bios,
    IReadOnlyList<PhysicalDiskHardware> PhysicalDisks,
    DateTimeOffset ScannedAt);

public sealed record ProcessorHardware(string? Name, uint? Cores, uint? LogicalProcessors, uint? MaximumClockMhz);

public sealed record ComputerSystemHardware(string? Manufacturer, string? Model, ulong? InstalledMemoryBytes);

public sealed record OperatingSystemHardware(string? Name, string? Version, string? Build, string? Architecture, DateOnly? ReleaseDate);

public sealed record GraphicsAdapterHardware(string? Name, string? DriverVersion);

public sealed record MemoryModuleHardware(
    string? Location,
    string? Manufacturer,
    string? PartNumber,
    string? Model,
    ulong? CapacityBytes,
    uint? MaximumDataRateMtps,
    uint? ConfiguredClockMhz);

public sealed record MotherboardHardware(string? Manufacturer, string? Product, string? Version);

public sealed record BiosHardware(string? Manufacturer, string? Version, string? ReleaseDate);

public sealed record PhysicalDiskHardware(string? Model, ulong? CapacityBytes, string? InterfaceType, string? WindowsWmiStatus);
