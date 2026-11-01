using System.Globalization;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using XartZans.WinManager.App.Services;
using XartZans.WinManager.Core.Formatting;

namespace XartZans.WinManager.App.ViewModels;

public sealed class DashboardViewModel : ObservableObject, IDisposable
{
    private readonly SystemSnapshotService _snapshotService;
    private readonly WmiHardwareInventoryService _hardwareInventoryService;
    private readonly HardwareTemperatureService _temperatureService;
    private readonly IAppLogger _logger;
    private readonly DispatcherTimer _timer;
    private bool _isRefreshing;
    private bool _hasSnapshot;
    private SystemSnapshot? _lastSnapshot;
    private HardwareInventory? _hardwareInventory;
    private HardwareSensorSnapshot? _lastSensorSnapshot;
    private bool _isMonitoringAvailable = true;
    private bool _hasCpuUsage;
    private bool _hasMemoryUsage;
    private bool _hasDiskUsage;
    private double _cpuUsagePercent;
    private double _memoryUsagePercent;
    private double _diskUsagePercent;
    private string _cpuValueText = string.Empty;
    private string _memoryValueText = string.Empty;
    private string _memoryDetailText = string.Empty;
    private string _diskValueText = string.Empty;
    private string _diskDetailText = string.Empty;
    private string _downloadText = string.Empty;
    private string _uploadText = string.Empty;
    private string _computerName = string.Empty;
    private string _operatingSystem = string.Empty;
    private string _processorModel = string.Empty;
    private string _graphicsModel = string.Empty;
    private double? _graphicsUsagePercent;
    private string _lastUpdatedText = "—";
    private string _cpuTemperatureText = string.Empty;
    private string _gpuTemperatureText = string.Empty;
    private string _memoryTemperatureText = string.Empty;
    private string _temperatureUpdatedText = string.Empty;
    private string _computerModelSummaryText = string.Empty;
    private string _motherboardSummaryText = string.Empty;
    private string _memoryModulesSummaryText = string.Empty;
    private string _operatingSystemReleaseDateText = string.Empty;

    public DashboardViewModel(
        SystemSnapshotService snapshotService,
        WmiHardwareInventoryService hardwareInventoryService,
        HardwareTemperatureService temperatureService,
        IAppLogger logger)
    {
        _snapshotService = snapshotService;
        _hardwareInventoryService = hardwareInventoryService;
        _temperatureService = temperatureService;
        _logger = logger;
        RefreshLanguage();
        LocalizationService.LanguageChanged += OnLanguageChanged;

        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();
        _ = RefreshAsync();
        _ = RefreshHardwareSummaryAsync();
    }

    public bool IsMonitoringAvailable
    {
        get => _isMonitoringAvailable;
        private set
        {
            if (SetProperty(ref _isMonitoringAvailable, value))
            {
                OnPropertyChanged(nameof(MonitoringStatusText));
                OnPropertyChanged(nameof(MonitoringStatusBrush));
            }
        }
    }

    public string MonitoringStatusText => LocalizationService.Get(
        IsMonitoringAvailable ? "MonitoringActive" : "MonitoringUnavailable");

    public Brush MonitoringStatusBrush => (Brush)Application.Current.FindResource(
        IsMonitoringAvailable ? "PositiveBrush" : "WarningBrush");

    public bool HasCpuUsage
    {
        get => _hasCpuUsage;
        private set => SetProperty(ref _hasCpuUsage, value);
    }

    public bool HasMemoryUsage
    {
        get => _hasMemoryUsage;
        private set => SetProperty(ref _hasMemoryUsage, value);
    }

    public bool HasDiskUsage
    {
        get => _hasDiskUsage;
        private set => SetProperty(ref _hasDiskUsage, value);
    }

    public double CpuUsagePercent
    {
        get => _cpuUsagePercent;
        private set => SetProperty(ref _cpuUsagePercent, value);
    }

    public double MemoryUsagePercent
    {
        get => _memoryUsagePercent;
        private set => SetProperty(ref _memoryUsagePercent, value);
    }

    public double DiskUsagePercent
    {
        get => _diskUsagePercent;
        private set => SetProperty(ref _diskUsagePercent, value);
    }

    public string CpuValueText
    {
        get => _cpuValueText;
        private set => SetProperty(ref _cpuValueText, value);
    }

    public string MemoryValueText
    {
        get => _memoryValueText;
        private set => SetProperty(ref _memoryValueText, value);
    }

    public string MemoryDetailText
    {
        get => _memoryDetailText;
        private set => SetProperty(ref _memoryDetailText, value);
    }

    public string DiskValueText
    {
        get => _diskValueText;
        private set => SetProperty(ref _diskValueText, value);
    }

    public string DiskDetailText
    {
        get => _diskDetailText;
        private set => SetProperty(ref _diskDetailText, value);
    }

    public string DownloadText
    {
        get => _downloadText;
        private set => SetProperty(ref _downloadText, value);
    }

    public string UploadText
    {
        get => _uploadText;
        private set => SetProperty(ref _uploadText, value);
    }

    public string ComputerName
    {
        get => _computerName;
        private set => SetProperty(ref _computerName, value);
    }

    public string OperatingSystem
    {
        get => _operatingSystem;
        private set => SetProperty(ref _operatingSystem, value);
    }

    public string ProcessorModel
    {
        get => _processorModel;
        private set => SetProperty(ref _processorModel, value);
    }

    public string GraphicsModel
    {
        get => _graphicsModel;
        private set => SetProperty(ref _graphicsModel, value);
    }

    public string GraphicsUsageText => _graphicsUsagePercent is double usage
        ? MetricFormatter.FormatPercent(usage)
        : LocalizationService.Get("GraphicsUsageUnavailable");

    public double GraphicsUsagePercent => _graphicsUsagePercent ?? 0;

    public bool HasGraphicsUsage => _graphicsUsagePercent.HasValue;

    public string CpuTemperatureText
    {
        get => _cpuTemperatureText;
        private set => SetProperty(ref _cpuTemperatureText, value);
    }

    public string GpuTemperatureText
    {
        get => _gpuTemperatureText;
        private set => SetProperty(ref _gpuTemperatureText, value);
    }

    public string MemoryTemperatureText
    {
        get => _memoryTemperatureText;
        private set => SetProperty(ref _memoryTemperatureText, value);
    }

    public string TemperatureUpdatedText
    {
        get => _temperatureUpdatedText;
        private set => SetProperty(ref _temperatureUpdatedText, value);
    }

    public string ComputerModelSummaryText
    {
        get => _computerModelSummaryText;
        private set => SetProperty(ref _computerModelSummaryText, value);
    }

    public string MotherboardSummaryText
    {
        get => _motherboardSummaryText;
        private set => SetProperty(ref _motherboardSummaryText, value);
    }

    public string MemoryModulesSummaryText
    {
        get => _memoryModulesSummaryText;
        private set => SetProperty(ref _memoryModulesSummaryText, value);
    }

    public string OperatingSystemReleaseDateText
    {
        get => _operatingSystemReleaseDateText;
        private set => SetProperty(ref _operatingSystemReleaseDateText, value);
    }

    public string LastUpdatedText
    {
        get => _lastUpdatedText;
        private set => SetProperty(ref _lastUpdatedText, value);
    }

    private async void OnTimerTick(object? sender, EventArgs e) => await RefreshAsync();

    private async Task RefreshAsync()
    {
        if (_isRefreshing)
        {
            return;
        }

        _isRefreshing = true;
        try
        {
            var (snapshot, sensors) = await Task.Run(() =>
            {
                var currentSnapshot = _snapshotService.Capture();
                var currentSensors = _temperatureService.Capture();
                return (currentSnapshot, currentSensors);
            });
            ApplySnapshot(snapshot);
            ApplySensors(sensors);
            IsMonitoringAvailable = true;
        }
        catch (Exception exception)
        {
            IsMonitoringAvailable = false;
            _logger.Error("Failed to collect a system snapshot.", exception);
            if (!_hasSnapshot)
            {
                SetUnavailableValues();
            }

            if (_lastSensorSnapshot is null)
            {
                SetUnavailableSensors();
            }
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private void ApplySnapshot(SystemSnapshot snapshot)
    {
        _lastSnapshot = snapshot;
        if (snapshot.CpuUsagePercent is double cpu)
        {
            HasCpuUsage = true;
            CpuUsagePercent = cpu;
            CpuValueText = MetricFormatter.FormatPercent(cpu);
        }
        else if (_hasSnapshot)
        {
            HasCpuUsage = false;
            CpuValueText = LocalizationService.Get("MetricUnavailable");
        }
        else
        {
            CpuValueText = LocalizationService.Get("MetricWarmingUp");
        }

        HasMemoryUsage = snapshot.TotalMemoryBytes > 0;
        MemoryUsagePercent = HasMemoryUsage
            ? (double)snapshot.UsedMemoryBytes / snapshot.TotalMemoryBytes * 100
            : 0;
        MemoryValueText = HasMemoryUsage
            ? MetricFormatter.FormatPercent(MemoryUsagePercent)
            : LocalizationService.Get("MetricUnavailable");
        MemoryDetailText = HasMemoryUsage
            ? $"{MetricFormatter.FormatBytes(snapshot.UsedMemoryBytes)} / {MetricFormatter.FormatBytes(snapshot.TotalMemoryBytes)}"
            : LocalizationService.Get("MetricUnavailable");

        HasDiskUsage = snapshot.SystemDrive is { TotalBytes: > 0 };
        if (snapshot.SystemDrive is { } drive)
        {
            DiskUsagePercent = drive.UsagePercent ?? 0;
            DiskValueText = MetricFormatter.FormatPercent(drive.UsagePercent);
            DiskDetailText = $"{drive.Name}  ·  {LocalizationService.Get("DiskFree")}: {MetricFormatter.FormatBytes(drive.FreeBytes)}";
        }
        else
        {
            DiskUsagePercent = 0;
            DiskValueText = LocalizationService.Get("MetricUnavailable");
            DiskDetailText = LocalizationService.Get("MetricUnavailable");
        }

        DownloadText = snapshot.DownloadBytesPerSecond is null && !_hasSnapshot
            ? LocalizationService.Get("MetricWarmingUp")
            : MetricFormatter.FormatRate(snapshot.DownloadBytesPerSecond);
        UploadText = snapshot.UploadBytesPerSecond is null && !_hasSnapshot
            ? LocalizationService.Get("MetricWarmingUp")
            : MetricFormatter.FormatRate(snapshot.UploadBytesPerSecond);
        ComputerName = snapshot.ComputerName;
        OperatingSystem = snapshot.OperatingSystem;
        ProcessorModel = string.IsNullOrWhiteSpace(snapshot.ProcessorModel)
            ? LocalizationService.Get("MetricUnavailable")
            : snapshot.ProcessorModel;
        GraphicsModel = string.IsNullOrWhiteSpace(snapshot.GraphicsModel)
            ? LocalizationService.Get("MetricUnavailable")
            : snapshot.GraphicsModel;
        LastUpdatedText = snapshot.CapturedAt.LocalDateTime.ToString("HH:mm:ss");
        _hasSnapshot = true;
    }

    private async Task RefreshHardwareSummaryAsync()
    {
        try
        {
            var inventory = await Task.Run(() => _hardwareInventoryService.Read());
            _hardwareInventory = inventory;
            ApplyHardwareSummary(inventory);
        }
        catch (Exception exception)
        {
            _logger.Error("Could not collect the dashboard hardware summary.", exception);
            ComputerModelSummaryText = LocalizationService.Get("MetricUnavailable");
            MotherboardSummaryText = LocalizationService.Get("MetricUnavailable");
            MemoryModulesSummaryText = LocalizationService.Get("MetricUnavailable");
            OperatingSystemReleaseDateText = LocalizationService.Get("ReleaseDateUnavailable");
        }
    }

    private void ApplyHardwareSummary(HardwareInventory inventory)
    {
        var unavailable = LocalizationService.Get("MetricUnavailable");
        var computer = inventory.ComputerSystem;
        ComputerModelSummaryText = JoinAvailable(computer?.Manufacturer, computer?.Model) is { Length: > 0 } model
            ? $"{ComputerName} · {model}"
            : ComputerName;

        var motherboard = inventory.Motherboard;
        MotherboardSummaryText = ValueOrUnavailable(
            JoinAvailable(motherboard?.Manufacturer, motherboard?.Product, motherboard?.Version), unavailable);

        var modules = inventory.MemoryModules
            .Select(module =>
            {
                var model = JoinAvailable(module.Manufacturer, module.PartNumber, module.Model);
                var capacity = module.CapacityBytes is > 0 ? MetricFormatter.FormatBytes(module.CapacityBytes.Value) : unavailable;
                var speed = module.MaximumDataRateMtps is > 0
                    ? $" · {LocalizationService.Get("MemoryMaxSpeedShort")}: {module.MaximumDataRateMtps.Value.ToString("N0", CultureInfo.CurrentCulture)} MT/s"
                    : string.Empty;
                return $"{ValueOrUnavailable(module.Location, LocalizationService.Get("MemoryLocation"))}: {ValueOrUnavailable(model, unavailable)} · {capacity}{speed}";
            })
            .Take(6)
            .ToArray();
        MemoryModulesSummaryText = modules.Length > 0
            ? string.Join(Environment.NewLine, modules)
            : unavailable;

        OperatingSystemReleaseDateText = inventory.OperatingSystem?.ReleaseDate is DateOnly releaseDate
            ? releaseDate.ToString("d", CultureInfo.CurrentCulture)
            : LocalizationService.Get("ReleaseDateUnavailable");
    }

    private void ApplySensors(HardwareSensorSnapshot snapshot)
    {
        _lastSensorSnapshot = snapshot;
        _graphicsUsagePercent = snapshot.GpuUsagePercent;
        OnPropertyChanged(nameof(GraphicsUsageText));
        OnPropertyChanged(nameof(GraphicsUsagePercent));
        OnPropertyChanged(nameof(HasGraphicsUsage));
        CpuTemperatureText = FormatTemperatureReadings(snapshot.Cpu);
        GpuTemperatureText = FormatTemperatureReadings(snapshot.Gpu);
        MemoryTemperatureText = FormatTemperatureReadings(snapshot.Memory);
        TemperatureUpdatedText = $"{LocalizationService.Get("LastUpdated")}: {snapshot.CapturedAt.LocalDateTime:HH:mm:ss}";
    }

    private static string FormatTemperatureReadings(IReadOnlyList<TemperatureSensorReading> readings)
    {
        if (readings.Count == 0)
        {
            return LocalizationService.Get("TemperatureUnavailable");
        }

        return string.Join(" · ", readings.Take(4).Select(reading =>
            $"{reading.Name}: {reading.Celsius.ToString("0.#", CultureInfo.CurrentCulture)} °C"));
    }

    private static string JoinAvailable(params string?[] values) =>
        string.Join(" ", values.Where(value => !string.IsNullOrWhiteSpace(value)));

    private static string ValueOrUnavailable(string? value, string unavailable) =>
        string.IsNullOrWhiteSpace(value) ? unavailable : value;

    private void RefreshLanguage()
    {
        OnPropertyChanged(nameof(MonitoringStatusText));
        OnPropertyChanged(nameof(GraphicsUsageText));
        if (_lastSnapshot is { } snapshot)
        {
            ApplySnapshot(snapshot);
        }
        else
        {
            if (!HasCpuUsage)
            {
                CpuValueText = LocalizationService.Get(_hasSnapshot ? "MetricUnavailable" : "MetricWarmingUp");
            }

            if (!HasMemoryUsage)
            {
                MemoryValueText = LocalizationService.Get("MetricUnavailable");
                MemoryDetailText = LocalizationService.Get("MetricUnavailable");
            }

            if (!HasDiskUsage)
            {
                DiskValueText = LocalizationService.Get("MetricUnavailable");
                DiskDetailText = LocalizationService.Get("MetricUnavailable");
            }

            if (!_hasSnapshot)
            {
                DownloadText = LocalizationService.Get("MetricWarmingUp");
                UploadText = LocalizationService.Get("MetricWarmingUp");
            }
        }

        if (_lastSensorSnapshot is { } sensors)
        {
            ApplySensors(sensors);
        }

        if (_hardwareInventory is { } inventory)
        {
            ApplyHardwareSummary(inventory);
        }
    }

    private void SetUnavailableValues()
    {
        CpuValueText = LocalizationService.Get("MetricUnavailable");
        MemoryValueText = LocalizationService.Get("MetricUnavailable");
        MemoryDetailText = LocalizationService.Get("MetricUnavailable");
        DiskValueText = LocalizationService.Get("MetricUnavailable");
        DiskDetailText = LocalizationService.Get("MetricUnavailable");
        DownloadText = LocalizationService.Get("MetricUnavailable");
        UploadText = LocalizationService.Get("MetricUnavailable");
        ProcessorModel = LocalizationService.Get("MetricUnavailable");
        GraphicsModel = LocalizationService.Get("MetricUnavailable");
        OperatingSystem = LocalizationService.Get("MetricUnavailable");
        ComputerName = LocalizationService.Get("MetricUnavailable");
        SetUnavailableSensors();
    }

    private void SetUnavailableSensors()
    {
        var unavailable = LocalizationService.Get("TemperatureUnavailable");
        _graphicsUsagePercent = null;
        OnPropertyChanged(nameof(GraphicsUsageText));
        OnPropertyChanged(nameof(GraphicsUsagePercent));
        OnPropertyChanged(nameof(HasGraphicsUsage));
        CpuTemperatureText = unavailable;
        GpuTemperatureText = unavailable;
        MemoryTemperatureText = unavailable;
        TemperatureUpdatedText = unavailable;
    }

    private void OnLanguageChanged(object? sender, EventArgs e) => RefreshLanguage();

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
        LocalizationService.LanguageChanged -= OnLanguageChanged;
        _temperatureService.Dispose();
    }
}
