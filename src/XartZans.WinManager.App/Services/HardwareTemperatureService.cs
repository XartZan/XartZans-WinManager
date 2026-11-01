using System.Collections.Immutable;
using LibreHardwareMonitor.Hardware;

namespace XartZans.WinManager.App.Services;

public sealed class HardwareTemperatureService : IDisposable
{
    private readonly object _sync = new();
    private readonly IAppLogger _logger;
    private readonly Computer _computer = new()
    {
        IsCpuEnabled = true,
        IsGpuEnabled = true,
        IsMemoryEnabled = true,
        IsMotherboardEnabled = true
    };
    private readonly UpdateVisitor _updateVisitor = new();
    private bool _opened;
    private bool _unavailable;
    private bool _disposed;

    public HardwareTemperatureService(IAppLogger logger) => _logger = logger;

    public HardwareSensorSnapshot Capture()
    {
        lock (_sync)
        {
            if (_disposed || _unavailable || !OperatingSystem.IsWindows())
            {
                return HardwareSensorSnapshot.Empty(DateTimeOffset.Now);
            }

            try
            {
                if (!_opened)
                {
                    // LibreHardwareMonitor only opens available sensor providers; this app never installs or elevates a driver.
                    _computer.Open();
                    _opened = true;
                }

                _computer.Accept(_updateVisitor);
                var cpu = new List<TemperatureSensorReading>();
                var gpu = new List<TemperatureSensorReading>();
                var memory = new List<TemperatureSensorReading>();
                var gpuLoads = new List<float>();
                var readings = new List<HardwareSensorReading>();

                foreach (var hardware in _computer.Hardware)
                {
                    CollectSensors(hardware, cpu, gpu, memory, gpuLoads, readings);
                }

                return new HardwareSensorSnapshot(
                    cpu.ToImmutableArray(),
                    gpu.ToImmutableArray(),
                    memory.ToImmutableArray(),
                    SelectGpuUsage(gpuLoads, readings),
                    readings.ToImmutableArray(),
                    DateTimeOffset.Now,
                    null);
            }
            catch (Exception exception)
            {
                _unavailable = true;
                _logger.Error("Hardware sensors could not be read. The app did not request elevation or install a driver.", exception);
                return HardwareSensorSnapshot.Empty(DateTimeOffset.Now, exception.Message);
            }
        }
    }

    private static void CollectSensors(
        IHardware hardware,
        ICollection<TemperatureSensorReading> cpu,
        ICollection<TemperatureSensorReading> gpu,
        ICollection<TemperatureSensorReading> memory,
        ICollection<float> gpuLoads,
        ICollection<HardwareSensorReading> readings)
    {
        var hardwareType = hardware.HardwareType.ToString();
        var isCpu = hardwareType.Equals("Cpu", StringComparison.OrdinalIgnoreCase);
        var isGpu = hardwareType.StartsWith("Gpu", StringComparison.OrdinalIgnoreCase);
        var isMemory = hardwareType.Equals("Memory", StringComparison.OrdinalIgnoreCase);

        foreach (var sensor in hardware.Sensors)
        {
            if (sensor.Value is not float value || !float.IsFinite(value))
            {
                continue;
            }

            if (sensor.SensorType == SensorType.Temperature)
            {
                // Ignore invalid zero/sentinel and implausible values from unsupported sensors.
                if (value is <= 0 or > 125)
                {
                    continue;
                }

                readings.Add(new HardwareSensorReading(hardware.Name, hardwareType, sensor.Name, sensor.SensorType, value));
                var reading = new TemperatureSensorReading(sensor.Name, value);
                if (isCpu)
                {
                    cpu.Add(reading);
                }
                else if (isGpu)
                {
                    gpu.Add(reading);
                }
                else if (isMemory || IsMemoryTemperatureSensor(sensor.Name))
                {
                    memory.Add(reading);
                }
            }
            else
            {
                readings.Add(new HardwareSensorReading(hardware.Name, hardwareType, sensor.Name, sensor.SensorType, value));
                if (isGpu && sensor.SensorType == SensorType.Load && value is >= 0 and <= 100)
                {
                    gpuLoads.Add(value);
                }
            }
        }

        foreach (var subHardware in hardware.SubHardware)
        {
            CollectSensors(subHardware, cpu, gpu, memory, gpuLoads, readings);
        }
    }

    private static double? SelectGpuUsage(IReadOnlyCollection<float> gpuLoads, IReadOnlyCollection<HardwareSensorReading> readings)
    {
        var preferred = readings
            .Where(reading => reading.HardwareType.StartsWith("Gpu", StringComparison.OrdinalIgnoreCase) &&
                              reading.SensorType == SensorType.Load &&
                              (reading.Name.Contains("Core", StringComparison.OrdinalIgnoreCase) ||
                               reading.Name.Contains("3D", StringComparison.OrdinalIgnoreCase)))
            .Select(reading => reading.Value)
            .ToArray();
        if (preferred.Length > 0)
        {
            return preferred.Max();
        }

        return gpuLoads.Count == 0 ? null : gpuLoads.Max();
    }

    private static bool IsMemoryTemperatureSensor(string name) =>
        name.Contains("DIMM", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("DRAM", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Memory", StringComparison.OrdinalIgnoreCase);

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_opened)
            {
                try
                {
                    _computer.Close();
                }
                catch (Exception exception)
                {
                    _logger.Error("Could not close the hardware sensor reader cleanly.", exception);
                }
            }
        }
    }

    private sealed class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer) => computer.Traverse(this);

        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (var subHardware in hardware.SubHardware)
            {
                subHardware.Accept(this);
            }
        }

        public void VisitSensor(ISensor sensor) { }

        public void VisitParameter(IParameter parameter) { }
    }
}

public sealed record TemperatureSensorReading(string Name, float Celsius);

public sealed record HardwareSensorReading(string Hardware, string HardwareType, string Name, SensorType SensorType, float Value);

public sealed record HardwareSensorSnapshot(
    IReadOnlyList<TemperatureSensorReading> Cpu,
    IReadOnlyList<TemperatureSensorReading> Gpu,
    IReadOnlyList<TemperatureSensorReading> Memory,
    double? GpuUsagePercent,
    IReadOnlyList<HardwareSensorReading> Readings,
    DateTimeOffset CapturedAt,
    string? Error)
{
    public static HardwareSensorSnapshot Empty(DateTimeOffset capturedAt, string? error = null) => new([], [], [], null, [], capturedAt, error);
}
