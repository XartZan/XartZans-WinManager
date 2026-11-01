using System.IO;
using System.Windows;

namespace XartZans.WinManager.App.Services;

public static class ThemeService
{
    private const string ThemeSettingFile = "theme.txt";
    private const string PaletteMarkerKey = "ThemePaletteMarker";
    private static string _currentThemeCode = "dark";

    public static event EventHandler? ThemeChanged;

    public static string CurrentThemeCode => _currentThemeCode;

    public static void Initialize() => ApplyTheme(ReadSavedTheme(), persist: false);

    public static void ApplyTheme(string themeCode) => ApplyTheme(themeCode, persist: true);

    private static void ApplyTheme(string themeCode, bool persist)
    {
        var normalizedCode = string.Equals(themeCode, "light", StringComparison.OrdinalIgnoreCase)
            ? "light"
            : "dark";
        var themeFile = normalizedCode == "light" ? "Light.xaml" : "Dark.xaml";
        var uri = new Uri($"/XartZans.WinManager;component/Themes/{themeFile}", UriKind.Relative);
        var palette = (ResourceDictionary)Application.LoadComponent(uri);
        var mergedDictionaries = Application.Current.Resources.MergedDictionaries;
        var previousPalette = mergedDictionaries.FirstOrDefault(dictionary => dictionary.Contains(PaletteMarkerKey));
        if (previousPalette is not null)
        {
            mergedDictionaries.Remove(previousPalette);
        }

        mergedDictionaries.Add(palette);
        _currentThemeCode = normalizedCode;
        if (persist)
        {
            SaveTheme(normalizedCode);
        }

        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    private static string ReadSavedTheme()
    {
        try
        {
            var path = GetSettingsPath();
            return File.Exists(path) && string.Equals(File.ReadAllText(path).Trim(), "light", StringComparison.OrdinalIgnoreCase)
                ? "light"
                : "dark";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return "dark";
        }
    }

    private static void SaveTheme(string themeCode)
    {
        try
        {
            var path = GetSettingsPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, themeCode);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Theme changes still apply for this session when preferences are read-only.
        }
    }

    private static string GetSettingsPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "XartZans",
        "WinManager",
        ThemeSettingFile);
}
