using System.Collections.ObjectModel;
using XartZans.WinManager.App.Services;

namespace XartZans.WinManager.App.ViewModels;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private object _currentPage;
    private NavigationItem _selectedItem;
    private ModuleInfoViewModel? _activeModule;
    private readonly WmiHardwareInventoryService _hardwareInventoryService;
    private readonly HardwareTemperatureService _temperatureService;
    private readonly SystemInformationReportService _reportService;
    private readonly IAppLogger _logger;
    private HardwareViewModel? _hardwareViewModel;

    public MainViewModel(SystemSnapshotService snapshotService, IAppLogger logger)
    {
        var dashboard = new NavigationItem("NavDashboard", "\uE80F", isImplemented: true);
        var hardware = new NavigationItem("NavHardware", "\uE7F4", isImplemented: true);
        var repair = new NavigationItem("NavRepair", "\uE90F", isImplemented: false);
        var storage = new NavigationItem("NavStorage", "\uE958", isImplemented: false);
        var windows = new NavigationItem("NavWindows", "\uE713", isImplemented: false);
        var bios = new NavigationItem("NavBios", "\uE8D7", isImplemented: false);
        var security = new NavigationItem("NavSecurity", "\uE72E", isImplemented: false);
        var reports = new NavigationItem("NavReports", "\uE9D9", isImplemented: false);
        var themes = new NavigationItem("NavThemes", "\uE790", isImplemented: true);

        NavigationGroups = new ObservableCollection<NavigationGroup>
        {
            new("NavGroupGeneral", [dashboard]),
            new("NavGroupSystem", [hardware]),
            new("NavGroupTools", [repair, storage, windows, bios, reports, themes]),
            new("NavGroupSecurity", [security])
        };
        NavigationItems = NavigationGroups.SelectMany(group => group.Items).ToArray();
        _logger = logger;
        _hardwareInventoryService = new WmiHardwareInventoryService(logger);
        _temperatureService = new HardwareTemperatureService(logger);
        _reportService = new SystemInformationReportService(_hardwareInventoryService, snapshotService, _temperatureService, logger);
        Dashboard = new DashboardViewModel(snapshotService, _hardwareInventoryService, _temperatureService, logger);
        Themes = new ThemesViewModel();
        SelectLanguageCommand = new RelayCommand(parameter =>
        {
            if (parameter is LanguageOption language)
            {
                LocalizationService.SelectLanguage(language.Code);
            }
        });
        OpenLanguagesFolderCommand = new RelayCommand(_ => LocalizationService.OpenLanguagesFolder());
        RefreshLanguagesCommand = new RelayCommand(_ => LocalizationService.ReloadCatalogs());
        SelectNavigationCommand = new RelayCommand(parameter =>
        {
            if (parameter is NavigationItem item)
            {
                NavigateTo(item);
            }
        });

        _selectedItem = dashboard;
        _selectedItem.IsSelected = true;
        _currentPage = Dashboard;
        LocalizationService.LanguageChanged += OnLanguageChanged;
    }

    public ObservableCollection<NavigationGroup> NavigationGroups { get; }
    public IReadOnlyList<NavigationItem> NavigationItems { get; }
    public DashboardViewModel Dashboard { get; }
    public ThemesViewModel Themes { get; }
    public ObservableCollection<LanguageOption> Languages => LocalizationService.Languages;
    public string SelectedLanguageLabel => LocalizationService.SelectedLanguageLabel;
    public string SelectedLanguageName => LocalizationService.SelectedLanguageName;
    public RelayCommand SelectLanguageCommand { get; }
    public RelayCommand OpenLanguagesFolderCommand { get; }
    public RelayCommand RefreshLanguagesCommand { get; }
    public RelayCommand SelectNavigationCommand { get; }

    public object CurrentPage
    {
        get => _currentPage;
        private set => SetProperty(ref _currentPage, value);
    }

    private void NavigateTo(NavigationItem item)
    {
        _activeModule?.Dispose();
        _activeModule = null;
        _selectedItem.IsSelected = false;
        item.IsSelected = true;
        _selectedItem = item;
        if (item.TitleKey == "NavHardware")
        {
            _hardwareViewModel ??= new HardwareViewModel(_hardwareInventoryService, Dashboard, _reportService, _logger);
            CurrentPage = _hardwareViewModel;
        }
        else if (item.TitleKey == "NavThemes")
        {
            CurrentPage = Themes;
        }
        else if (item.IsImplemented)
        {
            CurrentPage = Dashboard;
        }
        else
        {
            _activeModule = new ModuleInfoViewModel(item);
            CurrentPage = _activeModule;
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(SelectedLanguageLabel));
        OnPropertyChanged(nameof(SelectedLanguageName));
        foreach (var group in NavigationGroups)
        {
            group.RefreshLanguage();
        }
    }

    public void Dispose()
    {
        LocalizationService.LanguageChanged -= OnLanguageChanged;
        _activeModule?.Dispose();
        _hardwareViewModel?.Dispose();
        Themes.Dispose();
        foreach (var item in NavigationItems)
        {
            item.Dispose();
        }
        Dashboard.Dispose();
    }
}
