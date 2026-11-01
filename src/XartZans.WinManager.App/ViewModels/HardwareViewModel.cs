using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Win32;
using XartZans.WinManager.App.Services;
using XartZans.WinManager.Core.Formatting;

namespace XartZans.WinManager.App.ViewModels;

public sealed class HardwareViewModel : ObservableObject, IDisposable
{
    private readonly WmiHardwareInventoryService _inventoryService;
    private readonly DashboardViewModel _dashboard;
    private readonly SystemInformationReportService _reportService;
    private readonly IAppLogger _logger;
    private CancellationTokenSource? _refreshCancellation;
    private HardwareInventory? _inventory;
    private bool _isLoading;
    private bool _hasLoaded;
    private bool _isWmiUnavailable;
    private bool _isExportingReport;
    private string? _reportStatusKey;
    private string _scanStatusText = string.Empty;
    private string _processorNameText = string.Empty;
    private string _processorDetailsText = string.Empty;
    private string _computerModelText = string.Empty;
    private string _operatingSystemText = string.Empty;
    private string _operatingSystemDetailsText = string.Empty;
    private string _operatingSystemReleaseDateText = string.Empty;
    private string _architectureText = string.Empty;
    private string _memorySummaryText = string.Empty;
    private string _memorySpeedSummaryText = string.Empty;
    private string _motherboardManufacturerText = string.Empty;
    private string _motherboardProductText = string.Empty;
    private string _motherboardVersionText = string.Empty;
    private string _biosManufacturerText = string.Empty;
    private string _biosVersionText = string.Empty;
    private string _biosDateText = string.Empty;

    public HardwareViewModel(
        WmiHardwareInventoryService inventoryService,
        DashboardViewModel dashboard,
        SystemInformationReportService reportService,
        IAppLogger logger)
    {
        _inventoryService = inventoryService;
        _dashboard = dashboard;
        _reportService = reportService;
        _logger = logger;
        RefreshLanguage();
        LocalizationService.LanguageChanged += OnLanguageChanged;
        _dashboard.PropertyChanged += OnDashboardPropertyChanged;
        RefreshCommand = new RelayCommand(_ => StartRefresh());
        ExportReportCommand = new RelayCommand(async _ => await ExportReportAsync(), _ => !IsExportingReport);
        _ = RefreshAsync();
    }

    public ObservableCollection<GraphicsAdapterRow> GraphicsAdapters { get; } = [];
    public ObservableCollection<MemoryModuleRow> MemoryModules { get; } = [];
    public ObservableCollection<PhysicalDiskRow> PhysicalDisks { get; } = [];
    public RelayCommand RefreshCommand { get; }
    public RelayCommand ExportReportCommand { get; }
    public bool HasGraphicsAdapters => GraphicsAdapters.Count > 0;
    public bool HasMemoryModules => MemoryModules.Count > 0;
    public bool HasPhysicalDisks => PhysicalDisks.Count > 0;
    public bool NoGraphicsAdapters => _hasLoaded && !HasGraphicsAdapters;
    public bool NoMemoryModules => _hasLoaded && !HasMemoryModules;
    public bool NoPhysicalDisks => _hasLoaded && !HasPhysicalDisks;

    public bool HasReportStatus => !string.IsNullOrWhiteSpace(_reportStatusKey);

    public string ReportStatusText => string.IsNullOrWhiteSpace(_reportStatusKey)
        ? string.Empty
        : LocalizationService.Get(_reportStatusKey);

    public bool IsExportingReport
    {
        get => _isExportingReport;
        private set
        {
            if (SetProperty(ref _isExportingReport, value))
            {
                ExportReportCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(ShowLoadedContent));
            }
        }
    }

    public bool ShowLoadedContent => _hasLoaded && !IsLoading;

    public bool IsWmiUnavailable => _isWmiUnavailable;

    public string ScanStatusText
    {
        get => _scanStatusText;
        private set => SetProperty(ref _scanStatusText, value);
    }

    public string ProcessorNameText
    {
        get => _processorNameText;
        private set => SetProperty(ref _processorNameText, value);
    }

    public string ProcessorDetailsText
    {
        get => _processorDetailsText;
        private set => SetProperty(ref _processorDetailsText, value);
    }

    public string ComputerModelText
    {
        get => _computerModelText;
        private set => SetProperty(ref _computerModelText, value);
    }

    public string OperatingSystemText
    {
        get => _operatingSystemText;
        private set => SetProperty(ref _operatingSystemText, value);
    }

    public string OperatingSystemDetailsText
    {
        get => _operatingSystemDetailsText;
        private set => SetProperty(ref _operatingSystemDetailsText, value);
    }

    public string OperatingSystemReleaseDateText
    {
        get => _operatingSystemReleaseDateText;
        private set => SetProperty(ref _operatingSystemReleaseDateText, value);
    }

    public string ArchitectureText
    {
        get => _architectureText;
        private set => SetProperty(ref _architectureText, value);
    }

    public string MemorySummaryText
    {
        get => _memorySummaryText;
        private set => SetProperty(ref _memorySummaryText, value);
    }

    public string MemorySpeedSummaryText
    {
        get => _memorySpeedSummaryText;
        private set => SetProperty(ref _memorySpeedSummaryText, value);
    }

    public string MotherboardManufacturerText
    {
        get => _motherboardManufacturerText;
        private set => SetProperty(ref _motherboardManufacturerText, value);
    }

    public string MotherboardProductText
    {
        get => _motherboardProductText;
        private set => SetProperty(ref _motherboardProductText, value);
    }

    public string MotherboardVersionText
    {
        get => _motherboardVersionText;
        private set => SetProperty(ref _motherboardVersionText, value);
    }

    public string BiosManufacturerText
    {
        get => _biosManufacturerText;
        private set => SetProperty(ref _biosManufacturerText, value);
    }

    public string BiosVersionText
    {
        get => _biosVersionText;
        private set => SetProperty(ref _biosVersionText, value);
    }

    public string BiosDateText
    {
        get => _biosDateText;
        private set => SetProperty(ref _biosDateText, value);
    }

    public string GraphicsUsageStatus => _dashboard.GraphicsUsageText;

    public string WmiUnavailableMessage => LocalizationService.Get(
        _isWmiUnavailable ? "HardwareWmiUnavailable" : "HardwareDataReadError");

    private async Task RefreshAsync()
    {
        if (IsLoading)
        {
            return;
        }

        _refreshCancellation?.Dispose();
        _refreshCancellation = new CancellationTokenSource();
        var cancellationToken = _refreshCancellation.Token;
        IsLoading = true;
        ScanStatusText = LocalizationService.Get("HardwareLoading");

        try
        {
            var inventory = await Task.Run(() => _inventoryService.Read(cancellationToken), cancellationToken);
            _inventory = inventory;
            _isWmiUnavailable = IsInventoryEmpty(inventory);
            _hasLoaded = true;
            ApplyInventory(inventory);
            OnPropertyChanged(nameof(ShowLoadedContent));
            OnPropertyChanged(nameof(IsWmiUnavailable));
            OnPropertyChanged(nameof(WmiUnavailableMessage));
        }
        catch (OperationCanceledException)
        {
            // Closing the app cancels the remaining WMI queries.
        }
        catch (Exception exception)
        {
            _logger.Error("Could not read the hardware inventory.", exception);
            _isWmiUnavailable = true;
            ScanStatusText = LocalizationService.Get("HardwareDataReadError");
            _hasLoaded = false;
            OnPropertyChanged(nameof(WmiUnavailableMessage));
            OnPropertyChanged(nameof(IsWmiUnavailable));
            OnPropertyChanged(nameof(ShowLoadedContent));
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyInventory(HardwareInventory inventory)
    {
        var unavailable = LocalizationService.Get("MetricUnavailable");
        var processor = inventory.Processor;
        ProcessorNameText = ValueOrUnavailable(processor?.Name, unavailable);
        ProcessorDetailsText = string.Join(" · ", new[]
        {
            processor?.Cores is uint cores ? $"{LocalizationService.Get("ProcessorCores")}: {cores}" : null,
            processor?.LogicalProcessors is uint logical ? $"{LocalizationService.Get("ProcessorThreads")}: {logical}" : null,
            processor?.MaximumClockMhz is uint frequency && frequency > 0
                ? $"{LocalizationService.Get("ProcessorMaxFrequency")}: {frequency.ToString("N0", CultureInfo.CurrentCulture)} MHz"
                : null
        }.Where(value => value is not null));
        if (string.IsNullOrWhiteSpace(ProcessorDetailsText))
        {
            ProcessorDetailsText = unavailable;
        }

        ComputerModelText = ValueOrUnavailable(
            JoinAvailable(inventory.ComputerSystem?.Manufacturer, inventory.ComputerSystem?.Model), unavailable);
        OperatingSystemText = ValueOrUnavailable(inventory.OperatingSystem?.Name, unavailable);
        ArchitectureText = ValueOrUnavailable(inventory.OperatingSystem?.Architecture, unavailable);
        var operatingSystemDetails = new[]
        {
            inventory.OperatingSystem?.Version,
            inventory.OperatingSystem?.Build is { Length: > 0 } build
                ? $"{LocalizationService.Get("OperatingSystemBuild")}: {build}"
                : null
        }.Where(value => !string.IsNullOrWhiteSpace(value));
        OperatingSystemDetailsText = ValueOrUnavailable(string.Join(" · ", operatingSystemDetails), unavailable);
        OperatingSystemReleaseDateText = inventory.OperatingSystem?.ReleaseDate is DateOnly releaseDate
            ? releaseDate.ToString("d", CultureInfo.CurrentCulture)
            : LocalizationService.Get("ReleaseDateUnavailable");

        var installedMemory = inventory.MemoryModules
            .Where(module => module.CapacityBytes is > 0)
            .Aggregate(0UL, (total, module) => total + module.CapacityBytes!.Value);
        if (installedMemory == 0)
        {
            installedMemory = inventory.ComputerSystem?.InstalledMemoryBytes ?? 0;
        }

        MemorySummaryText = installedMemory > 0
            ? $"{MetricFormatter.FormatBytes(installedMemory)} · {LocalizationService.Get("MemoryModules")}: {inventory.MemoryModules.Count}"
            : unavailable;
        var configuredSpeeds = inventory.MemoryModules
            .Where(module => module.ConfiguredClockMhz is > 0)
            .Select(module => module.ConfiguredClockMhz!.Value)
            .Distinct()
            .Select(speed => $"{speed.ToString("N0", CultureInfo.CurrentCulture)} MHz");
        MemorySpeedSummaryText = ValueOrUnavailable(string.Join(", ", configuredSpeeds), unavailable);

        var board = inventory.Motherboard;
        MotherboardManufacturerText = ValueOrUnavailable(board?.Manufacturer, unavailable);
        MotherboardProductText = ValueOrUnavailable(board?.Product, unavailable);
        MotherboardVersionText = ValueOrUnavailable(board?.Version, unavailable);

        var bios = inventory.Bios;
        BiosManufacturerText = ValueOrUnavailable(bios?.Manufacturer, unavailable);
        BiosVersionText = ValueOrUnavailable(bios?.Version, unavailable);
        BiosDateText = ValueOrUnavailable(bios?.ReleaseDate, unavailable);

        GraphicsAdapters.Clear();
        foreach (var adapter in inventory.GraphicsAdapters)
        {
            GraphicsAdapters.Add(new GraphicsAdapterRow(
                ValueOrUnavailable(adapter.Name, unavailable),
                ValueOrUnavailable(adapter.DriverVersion, unavailable)));
        }

        MemoryModules.Clear();
        foreach (var module in inventory.MemoryModules)
        {
            var partModel = JoinAvailable(module.Manufacturer, module.PartNumber, module.Model);
            var speed = module.MaximumDataRateMtps is > 0
                ? $"{module.MaximumDataRateMtps.Value.ToString("N0", CultureInfo.CurrentCulture)} MT/s"
                : unavailable;
            var configuredSpeed = module.ConfiguredClockMhz is > 0
                ? $"{module.ConfiguredClockMhz.Value.ToString("N0", CultureInfo.CurrentCulture)} MHz"
                : unavailable;
            MemoryModules.Add(new MemoryModuleRow(
                ValueOrUnavailable(module.Location, unavailable),
                ValueOrUnavailable(partModel, unavailable),
                module.CapacityBytes is > 0 ? MetricFormatter.FormatBytes(module.CapacityBytes.Value) : unavailable,
                speed,
                configuredSpeed));
        }

        PhysicalDisks.Clear();
        foreach (var disk in inventory.PhysicalDisks)
        {
            PhysicalDisks.Add(new PhysicalDiskRow(
                ValueOrUnavailable(disk.Model, unavailable),
                disk.CapacityBytes is > 0 ? MetricFormatter.FormatBytes(disk.CapacityBytes.Value) : unavailable,
                ValueOrUnavailable(disk.InterfaceType, unavailable),
                ValueOrUnavailable(disk.WindowsWmiStatus, unavailable)));
        }

        OnPropertyChanged(nameof(HasGraphicsAdapters));
        OnPropertyChanged(nameof(HasMemoryModules));
        OnPropertyChanged(nameof(HasPhysicalDisks));
        OnPropertyChanged(nameof(NoGraphicsAdapters));
        OnPropertyChanged(nameof(NoMemoryModules));
        OnPropertyChanged(nameof(NoPhysicalDisks));
        ScanStatusText = $"{LocalizationService.Get("HardwareLastScan")} · {inventory.ScannedAt.LocalDateTime:HH:mm:ss}";
        OnPropertyChanged(nameof(GraphicsUsageStatus));
    }

    private void RefreshLanguage()
    {
        OnPropertyChanged(nameof(GraphicsUsageStatus));
        OnPropertyChanged(nameof(WmiUnavailableMessage));
        OnPropertyChanged(nameof(ReportStatusText));
        if (_inventory is { } inventory)
        {
            ApplyInventory(inventory);
        }
        else if (IsLoading)
        {
            ScanStatusText = LocalizationService.Get("HardwareLoading");
        }
    }

    private static bool IsInventoryEmpty(HardwareInventory inventory) =>
        inventory.Processor is null && inventory.ComputerSystem is null && inventory.OperatingSystem is null &&
        inventory.GraphicsAdapters.Count == 0 && inventory.MemoryModules.Count == 0 &&
        inventory.Motherboard is null && inventory.Bios is null && inventory.PhysicalDisks.Count == 0;

    private static string JoinAvailable(params string?[] values) =>
        string.Join(" ", values.Where(value => !string.IsNullOrWhiteSpace(value)));

    private static string ValueOrUnavailable(string? value, string unavailable) =>
        string.IsNullOrWhiteSpace(value) ? unavailable : value;

    private void OnLanguageChanged(object? sender, EventArgs e) => RefreshLanguage();

    private void OnDashboardPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DashboardViewModel.GraphicsUsageText))
        {
            OnPropertyChanged(nameof(GraphicsUsageStatus));
        }
    }

    private async Task ExportReportAsync()
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var dialog = new SaveFileDialog
        {
            Title = LocalizationService.Get("ReportExport"),
            Filter = "Text report (*.txt)|*.txt",
            DefaultExt = ".txt",
            AddExtension = true,
            OverwritePrompt = true,
            InitialDirectory = Directory.Exists(documents) ? documents : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            FileName = $"XartZan-System-Info-{DateTime.Now:yyyyMMdd-HHmmss}.txt"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        IsExportingReport = true;
        SetReportStatus("ReportExporting");
        try
        {
            var report = await _reportService.CreateAsync();
            await File.WriteAllTextAsync(dialog.FileName, report, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
            SetReportStatus("ReportCreated");
        }
        catch (Exception exception)
        {
            _logger.Error("Could not export the system information report.", exception);
            SetReportStatus("ReportFailed");
        }
        finally
        {
            IsExportingReport = false;
        }
    }

    private void SetReportStatus(string? resourceKey)
    {
        _reportStatusKey = resourceKey;
        OnPropertyChanged(nameof(ReportStatusText));
        OnPropertyChanged(nameof(HasReportStatus));
    }

    private void StartRefresh() => _ = RefreshAsync();

    public void Dispose()
    {
        LocalizationService.LanguageChanged -= OnLanguageChanged;
        _dashboard.PropertyChanged -= OnDashboardPropertyChanged;
        _refreshCancellation?.Cancel();
        _refreshCancellation?.Dispose();
    }
}

public sealed record GraphicsAdapterRow(string Name, string DriverVersion);

public sealed record MemoryModuleRow(string Location, string Model, string Capacity, string SmbiosMaximumSpeed, string ConfiguredSpeed);

public sealed record PhysicalDiskRow(string Model, string Capacity, string InterfaceType, string WindowsWmiStatus);
