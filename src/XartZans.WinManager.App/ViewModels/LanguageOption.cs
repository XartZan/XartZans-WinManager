namespace XartZans.WinManager.App.ViewModels;

public sealed class LanguageOption(string code, string name) : ObservableObject
{
    private bool _isSelected;

    public string Code { get; } = code;
    public string Name { get; } = name;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
