using XartZans.WinManager.App.Services;

namespace XartZans.WinManager.App.ViewModels;

public sealed class ModuleInfoViewModel : ObservableObject, IDisposable
{
    private readonly NavigationItem _navigationItem;

    public ModuleInfoViewModel(NavigationItem navigationItem)
    {
        _navigationItem = navigationItem;
        RefreshLanguage();
        LocalizationService.LanguageChanged += OnLanguageChanged;
    }

    public string Title => _navigationItem.Title;
    public string Status => LocalizationService.Get("StatusPreparing");
    public string Description => LocalizationService.Get("ModulePreparingDescription");
    public string NextStep => LocalizationService.Get("ModuleNextStep");

    private void RefreshLanguage()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(NextStep));
    }

    private void OnLanguageChanged(object? sender, EventArgs e) => RefreshLanguage();

    public void Dispose() => LocalizationService.LanguageChanged -= OnLanguageChanged;
}
