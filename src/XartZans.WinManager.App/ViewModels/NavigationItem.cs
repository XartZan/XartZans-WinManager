using XartZans.WinManager.App.Services;

namespace XartZans.WinManager.App.ViewModels;

public sealed class NavigationItem : ObservableObject, IDisposable
{
    private bool _isSelected;

    public NavigationItem(string titleKey, string glyph, bool isImplemented)
    {
        TitleKey = titleKey;
        Glyph = glyph;
        IsImplemented = isImplemented;
        RefreshLanguage();
        LocalizationService.LanguageChanged += OnLanguageChanged;
    }

    public string TitleKey { get; }
    public string Glyph { get; }
    public bool IsImplemented { get; }
    public string Title { get; private set; } = string.Empty;
    public string Status => LocalizationService.Get(IsImplemented ? "StatusActive" : "StatusPreparing");

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public void RefreshLanguage()
    {
        Title = LocalizationService.Get(TitleKey);
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Status));
    }

    private void OnLanguageChanged(object? sender, EventArgs e) => RefreshLanguage();

    public void Dispose() => LocalizationService.LanguageChanged -= OnLanguageChanged;
}

public sealed class NavigationGroup(string titleKey, IReadOnlyList<NavigationItem> items) : ObservableObject
{
    public string TitleKey { get; } = titleKey;
    public IReadOnlyList<NavigationItem> Items { get; } = items;
    public string Title => LocalizationService.Get(TitleKey);

    public void RefreshLanguage() => OnPropertyChanged(nameof(Title));
}
