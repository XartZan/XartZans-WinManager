using System.Collections.ObjectModel;
using XartZans.WinManager.App.Services;

namespace XartZans.WinManager.App.ViewModels;

public sealed class ThemesViewModel : ObservableObject, IDisposable
{
    public ThemesViewModel()
    {
        Themes =
        [
            new ThemeOption("dark", "ThemeBlackTitle", "ThemeBlackDescription", "#0B0E14", "#151A23", "#F0F2F8", "#A7AEFF"),
            new ThemeOption("light", "ThemeWhiteTitle", "ThemeWhiteDescription", "#F3F5FA", "#FFFFFF", "#1B2430", "#6058C7")
        ];
        SelectThemeCommand = new RelayCommand(parameter =>
        {
            if (parameter is string code)
            {
                ThemeService.ApplyTheme(code);
            }
        });

        RefreshState();
        ThemeService.ThemeChanged += OnThemeChanged;
        LocalizationService.LanguageChanged += OnLanguageChanged;
    }

    public ObservableCollection<ThemeOption> Themes { get; }
    public RelayCommand SelectThemeCommand { get; }

    private void RefreshState()
    {
        foreach (var theme in Themes)
        {
            theme.IsSelected = string.Equals(theme.Code, ThemeService.CurrentThemeCode, StringComparison.OrdinalIgnoreCase);
            theme.RefreshLanguage();
        }
    }

    private void OnThemeChanged(object? sender, EventArgs e) => RefreshState();

    private void OnLanguageChanged(object? sender, EventArgs e) => RefreshState();

    public void Dispose()
    {
        ThemeService.ThemeChanged -= OnThemeChanged;
        LocalizationService.LanguageChanged -= OnLanguageChanged;
    }
}
