using System.Windows.Media;
using XartZans.WinManager.App.Services;

namespace XartZans.WinManager.App.ViewModels;

public sealed class ThemeOption : ObservableObject
{
    private bool _isSelected;

    public ThemeOption(string code, string titleKey, string descriptionKey, string background, string panel, string text, string accent)
    {
        Code = code;
        TitleKey = titleKey;
        DescriptionKey = descriptionKey;
        PreviewBackground = CreateBrush(background);
        PreviewPanel = CreateBrush(panel);
        PreviewText = CreateBrush(text);
        PreviewAccent = CreateBrush(accent);
    }

    public string Code { get; }
    public string TitleKey { get; }
    public string DescriptionKey { get; }
    public string Title => LocalizationService.Get(TitleKey);
    public string Description => LocalizationService.Get(DescriptionKey);
    public Brush PreviewBackground { get; }
    public Brush PreviewPanel { get; }
    public Brush PreviewText { get; }
    public Brush PreviewAccent { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public void RefreshLanguage()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Description));
    }

    private static Brush CreateBrush(string color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)!);
        brush.Freeze();
        return brush;
    }
}
