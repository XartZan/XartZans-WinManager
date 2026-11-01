using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using XartZans.WinManager.App.ViewModels;

namespace XartZans.WinManager.App.Services;

public static class LocalizationService
{
    private const long MaximumCatalogSize = 256 * 1024;
    private const int MaximumCatalogEntries = 512;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private static readonly Dictionary<string, LanguageCatalog> Catalogs = new(StringComparer.OrdinalIgnoreCase);
    private static ResourceDictionary? _activeDictionary;
    private static IReadOnlyDictionary<string, string>? _packagedEnglishStrings;
    private static string? _selectedCode;
    private static string _languagesDirectory = string.Empty;
    private static string _settingsDirectory = string.Empty;

    public static event EventHandler? LanguageChanged;

    public static ObservableCollection<LanguageOption> Languages { get; } = [];

    public static string SelectedLanguageCode => _selectedCode ?? "tr";

    public static string SelectedLanguageName =>
        Catalogs.TryGetValue(SelectedLanguageCode, out var catalog) ? catalog.Name : SelectedLanguageCode;

    public static string SelectedLanguageLabel => SelectedLanguageCode.ToUpperInvariant();

    public static string LanguagesDirectory => _languagesDirectory;

    public static void Initialize()
    {
        var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _settingsDirectory = Path.Combine(localData, "XartZans", "WinManager");
        _languagesDirectory = Path.Combine(_settingsDirectory, "Languages");

        try
        {
            Directory.CreateDirectory(_languagesDirectory);
            SynchronizePackagedFiles();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            App.Logger.Error("Could not prepare the user language catalog folder; using packaged catalogs.", exception);
            _languagesDirectory = Path.Combine(AppContext.BaseDirectory, "Languages");
        }

        ReloadCatalogs();
    }

    public static string Get(string key) =>
        Application.Current?.TryFindResource(key)?.ToString() ?? key;

    public static void SelectLanguage(string code)
    {
        if (!Catalogs.TryGetValue(code, out var catalog) || string.Equals(_selectedCode, catalog.Code, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        ApplyCatalog(catalog);
        SavePreferredLanguage(catalog.Code);
        LanguageChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void ReloadCatalogs()
    {
        var preferredCode = _selectedCode ?? ReadPreferredLanguage();
        Catalogs.Clear();
        LoadCatalogFiles();

        if (Catalogs.Count == 0)
        {
            RestoreFallbackCatalog();
            LoadCatalogFiles();
        }

        if (Catalogs.Count == 0)
        {
            throw new InvalidOperationException("No valid language catalog is available.");
        }

        Languages.Clear();
        foreach (var catalog in Catalogs.Values.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            Languages.Add(new LanguageOption(catalog.Code, catalog.Name));
        }

        var selected = Catalogs.TryGetValue(preferredCode ?? string.Empty, out var preferred)
            ? preferred
            : Catalogs.TryGetValue("tr", out var turkish)
                ? turkish
                : Catalogs.Values.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase).First();

        var selectionChanged = !string.Equals(_selectedCode, selected.Code, StringComparison.OrdinalIgnoreCase);
        ApplyCatalog(selected);
        if (selectionChanged)
        {
            SavePreferredLanguage(selected.Code);
        }
        LanguageChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void OpenLanguagesFolder()
    {
        try
        {
            Directory.CreateDirectory(_languagesDirectory);
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                UseShellExecute = false,
                ArgumentList = { _languagesDirectory }
            });
        }
        catch (Exception exception)
        {
            App.Logger.Error("Could not open the language catalog folder.", exception);
        }
    }

    private static void SynchronizePackagedFiles()
    {
        var packagedDirectory = Path.Combine(AppContext.BaseDirectory, "Languages");
        if (!Directory.Exists(packagedDirectory))
        {
            return;
        }

        var packagedHashes = ReadCatalogManifest();
        foreach (var packagedFile in Directory.EnumerateFiles(packagedDirectory, "*.json", SearchOption.TopDirectoryOnly))
        {
            var code = Path.GetFileNameWithoutExtension(packagedFile);
            var destination = Path.Combine(_languagesDirectory, Path.GetFileName(packagedFile));
            var packagedHash = ComputeFileHash(packagedFile);

            if (!File.Exists(destination))
            {
                if (!packagedHashes.ContainsKey(code))
                {
                    File.Copy(packagedFile, destination);
                }
            }
            else if (packagedHashes.TryGetValue(code, out var previousPackagedHash) &&
                     string.Equals(ComputeFileHash(destination), previousPackagedHash, StringComparison.OrdinalIgnoreCase) &&
                     !string.Equals(previousPackagedHash, packagedHash, StringComparison.OrdinalIgnoreCase))
            {
                // Refresh an untouched shipped catalog; preserve files the user edited.
                File.Copy(packagedFile, destination, overwrite: true);
            }

            MergeMissingCatalogStrings(packagedFile, destination, code);
            packagedHashes[code] = packagedHash;
        }

        var readmePath = Path.Combine(packagedDirectory, "README.md");
        var destinationReadme = Path.Combine(_languagesDirectory, "README.md");
        if (File.Exists(readmePath) && !File.Exists(destinationReadme))
        {
            File.Copy(readmePath, destinationReadme);
        }

        WriteCatalogManifest(packagedHashes);
    }

    private static void MergeMissingCatalogStrings(string packagedPath, string destinationPath, string code)
    {
        try
        {
            var packaged = JsonNode.Parse(File.ReadAllText(packagedPath));
            var userCopy = JsonNode.Parse(File.ReadAllText(destinationPath));
            if (packaged?["strings"] is not JsonObject packagedStrings ||
                userCopy?["strings"] is not JsonObject userStrings)
            {
                return;
            }

            var changed = false;
            foreach (var (key, packagedValue) in packagedStrings)
            {
                if (!userStrings.ContainsKey(key))
                {
                    userStrings[key] = packagedValue?.DeepClone();
                    changed = true;
                    continue;
                }

                if (TryGetLegacyValue(code, key, out var previousValue, out var currentValue) &&
                    userStrings[key] is JsonValue currentNode &&
                    currentNode.TryGetValue<string>(out var currentText) &&
                    string.Equals(currentText, previousValue, StringComparison.Ordinal))
                {
                    userStrings[key] = currentValue;
                    changed = true;
                }
            }

            if (changed)
            {
                File.WriteAllText(destinationPath, userCopy.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
            }
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException or InvalidOperationException)
        {
            App.Logger.Information($"Kept the existing '{code}' language catalog because its missing keys could not be updated.");
        }
    }

    private static bool TryGetLegacyValue(string code, string key, out string previousValue, out string currentValue)
    {
        previousValue = string.Empty;
        currentValue = string.Empty;
        if (string.Equals(code, "tr", StringComparison.OrdinalIgnoreCase))
        {
            (previousValue, currentValue) = key switch
            {
                "GraphicsUsageUnavailable" => ("GPU yükü ölçümü bu sürümde sunulmuyor", "GPU yükü sensör verisi alınamıyor"),
                "GraphicsUsageDetails" => ("Bu değer henüz Windows API'lerinden okunmuyor; tahmini değer gösterilmez.", "Yük, ekran kartının sağladığı donanım sensöründen okunur."),
                "HardwareSensorsNote" => ("Sıcaklık, fan hızı ve GPU yükü bu sürümde okunmuyor; tahmini değer gösterilmez.", "GPU yükü ve sıcaklıklar desteklenen donanım sensörlerinden okunur. Fan bilgisi henüz gösterilmiyor."),
                "MemoryMaxSpeed" => ("Azami hız (SMBIOS)", "Azami veri hızı (MT/s)"),
                "MemoryMaxSpeedShort" => ("SMBIOS", "Azami hız"),
                _ => (string.Empty, string.Empty)
            };
        }
        else if (string.Equals(code, "en", StringComparison.OrdinalIgnoreCase))
        {
            (previousValue, currentValue) = key switch
            {
                "GraphicsUsageUnavailable" => ("GPU load measurement is not available in this version", "GPU load sensor data is unavailable"),
                "GraphicsUsageDetails" => ("This value is not collected from Windows yet; no estimated value is shown.", "Load is read from a hardware sensor exposed by the graphics adapter."),
                "HardwareSensorsNote" => ("Temperature, fan speed, and GPU load are not read in this version; no values are estimated.", "GPU load and temperature come from supported hardware sensors. Fan readings are not shown yet."),
                "MemoryMaxSpeed" => ("Maximum speed (SMBIOS)", "Maximum data rate (MT/s)"),
                "MemoryMaxSpeedShort" => ("SMBIOS", "Max rate"),
                _ => (string.Empty, string.Empty)
            };
        }

        return previousValue.Length > 0;
    }

    private static Dictionary<string, string> ReadCatalogManifest()
    {
        var manifest = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var path = Path.Combine(_settingsDirectory, "catalogs.manifest.json");
        try
        {
            if (!File.Exists(path))
            {
                return manifest;
            }

            using var stream = File.OpenRead(path);
            var values = JsonSerializer.Deserialize<Dictionary<string, string>>(stream, JsonOptions);
            if (values is not null)
            {
                foreach (var (code, hash) in values)
                {
                    manifest[code] = hash;
                }
            }
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            App.Logger.Information("The optional packaged language catalog manifest could not be read.");
        }

        return manifest;
    }

    private static void WriteCatalogManifest(IReadOnlyDictionary<string, string> manifest)
    {
        try
        {
            Directory.CreateDirectory(_settingsDirectory);
            var path = Path.Combine(_settingsDirectory, "catalogs.manifest.json");
            File.WriteAllText(path, JsonSerializer.Serialize(manifest));
            File.SetAttributes(path, FileAttributes.Hidden);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            App.Logger.Information("The optional packaged language catalog manifest could not be saved.");
        }
    }

    private static string ComputeFileHash(string path) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path)));

    private static void LoadCatalogFiles()
    {
        foreach (var filePath in Directory.EnumerateFiles(_languagesDirectory, "*.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var catalog = ReadCatalog(filePath);
                if (catalog is null)
                {
                    continue;
                }

                catalog = MergePackagedCatalogStrings(catalog, filePath);

                if (!Catalogs.TryAdd(catalog.Code, catalog))
                {
                    App.Logger.Information($"Skipped duplicate language catalog: {Path.GetFileName(filePath)}");
                }
            }
            catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException or InvalidDataException)
            {
                App.Logger.Error($"Skipped invalid language catalog '{Path.GetFileName(filePath)}'.", exception);
            }
        }
    }

    private static LanguageCatalog MergePackagedCatalogStrings(LanguageCatalog userCatalog, string userPath)
    {
        var packagedPath = Path.Combine(AppContext.BaseDirectory, "Languages", Path.GetFileName(userPath));
        if (!File.Exists(packagedPath) ||
            string.Equals(Path.GetFullPath(packagedPath), Path.GetFullPath(userPath), StringComparison.OrdinalIgnoreCase))
        {
            return userCatalog;
        }

        try
        {
            var packagedCatalog = ReadCatalog(packagedPath);
            if (packagedCatalog is null || !string.Equals(packagedCatalog.Code, userCatalog.Code, StringComparison.OrdinalIgnoreCase))
            {
                return userCatalog;
            }

            var mergedStrings = new Dictionary<string, string>(packagedCatalog.Strings, StringComparer.Ordinal);
            foreach (var (key, userValue) in userCatalog.Strings)
            {
                if (!TryGetLegacyValue(userCatalog.Code, key, out var previousValue, out _) ||
                    !string.Equals(userValue, previousValue, StringComparison.Ordinal))
                {
                    mergedStrings[key] = userValue;
                }
            }

            return new LanguageCatalog(userCatalog.Code, userCatalog.Name, mergedStrings);
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException or InvalidDataException)
        {
            App.Logger.Information($"Using the existing '{userCatalog.Code}' language catalog because packaged defaults could not be loaded.");
            return userCatalog;
        }
    }

    private static LanguageCatalog? ReadCatalog(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length is <= 0 or > MaximumCatalogSize)
        {
            throw new InvalidDataException("Language catalog file size is outside the supported range.");
        }

        using var stream = File.OpenRead(filePath);
        var source = JsonSerializer.Deserialize<LanguageCatalogFile>(stream, JsonOptions);
        var fileCode = Path.GetFileNameWithoutExtension(filePath);
        if (source is null ||
            !IsValidCode(fileCode) ||
            !string.Equals(source.Code, fileCode, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(source.Name) ||
            source.Name.Length > 48 ||
            source.Strings is null ||
            source.Strings.Count > MaximumCatalogEntries)
        {
            throw new InvalidDataException("Language catalog metadata is incomplete or invalid.");
        }

        var strings = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in source.Strings)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Length > 100 || value is null || value.Length > 2_000)
            {
                continue;
            }

            strings[key] = value;
        }

        if (strings.Count == 0)
        {
            throw new InvalidDataException("Language catalog contains no usable strings.");
        }

        return new LanguageCatalog(fileCode, source.Name.Trim(), strings);
    }

    private static bool IsValidCode(string code) =>
        code.Length is >= 2 and <= 16 &&
        code.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');

    private static void RestoreFallbackCatalog()
    {
        var packagedFallback = Path.Combine(AppContext.BaseDirectory, "Languages", "tr.json");
        var destination = Path.Combine(_languagesDirectory, "tr.json");
        if (File.Exists(packagedFallback) &&
            !string.Equals(Path.GetFullPath(packagedFallback), Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(packagedFallback, destination, overwrite: true);
        }
    }

    private static void ApplyCatalog(LanguageCatalog selected)
    {
        var merged = new ResourceDictionary();
        var fallbackStrings = Catalogs.TryGetValue("en", out var english)
            ? english.Strings
            : GetPackagedEnglishStrings();
        if (fallbackStrings is not null)
        {
            foreach (var (key, value) in fallbackStrings)
            {
                merged[key] = value;
            }
        }

        foreach (var (key, value) in selected.Strings)
        {
            merged[key] = value;
        }

        var dictionaries = Application.Current.Resources.MergedDictionaries;
        if (_activeDictionary is not null)
        {
            dictionaries.Remove(_activeDictionary);
        }

        dictionaries.Insert(0, merged);
        _activeDictionary = merged;
        _selectedCode = selected.Code;

        foreach (var language in Languages)
        {
            language.IsSelected = string.Equals(language.Code, selected.Code, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string? ReadPreferredLanguage()
    {
        try
        {
            var settingsPath = Path.Combine(_settingsDirectory, "language.txt");
            return File.Exists(settingsPath) ? File.ReadAllText(settingsPath).Trim() : null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            App.Logger.Error("Could not read the preferred language setting.", exception);
            return null;
        }
    }

    private static void SavePreferredLanguage(string code)
    {
        try
        {
            Directory.CreateDirectory(_settingsDirectory);
            File.WriteAllText(Path.Combine(_settingsDirectory, "language.txt"), code);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            App.Logger.Error("Could not save the preferred language setting.", exception);
        }
    }

    private static IReadOnlyDictionary<string, string>? GetPackagedEnglishStrings()
    {
        if (_packagedEnglishStrings is not null)
        {
            return _packagedEnglishStrings;
        }

        var path = Path.Combine(AppContext.BaseDirectory, "Languages", "en.json");
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            _packagedEnglishStrings = ReadCatalog(path)?.Strings;
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException or InvalidDataException)
        {
            App.Logger.Error("Could not load the packaged English fallback catalog.", exception);
        }

        return _packagedEnglishStrings;
    }

    private sealed record LanguageCatalog(string Code, string Name, IReadOnlyDictionary<string, string> Strings);

    private sealed record LanguageCatalogFile(string? Code, string? Name, Dictionary<string, string?>? Strings);
}
