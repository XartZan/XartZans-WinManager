using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using XartZans.WinManager.Core.SystemInformation;

namespace XartZans.WinManager.App.Services;

public sealed class WindowsReleaseDateService
{
    private readonly Lazy<WindowsReleaseCatalog?> _catalog;

    public WindowsReleaseDateService()
    {
        _catalog = new Lazy<WindowsReleaseCatalog?>(LoadCatalog, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public DateOnly? GetInstalledReleaseDate()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        try
        {
            const string registryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion";
            var displayVersion = Registry.GetValue(registryPath, "DisplayVersion", null) as string;
            displayVersion ??= Registry.GetValue(registryPath, "ReleaseId", null) as string;
            var buildText = Registry.GetValue(registryPath, "CurrentBuildNumber", null) as string
                ?? Registry.GetValue(registryPath, "CurrentBuild", null) as string;

            var build = int.TryParse(buildText, out var parsedBuild) ? parsedBuild : Environment.OSVersion.Version.Build;
            var productKey = build >= 22000 ? "windows11" : "windows10";
            var releaseVersion = string.IsNullOrWhiteSpace(displayVersion)
                ? null
                : displayVersion.Trim();

            var catalog = _catalog.Value;
            if (catalog?.Releases is not { } allReleases ||
                !allReleases.TryGetValue(productKey, out var releases) ||
                catalog.BuildVersions is not { } buildVersions)
            {
                return null;
            }

            return WindowsReleaseDateResolver.Resolve(releaseVersion, build, releases, buildVersions);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return null;
        }
    }

    private static WindowsReleaseCatalog? LoadCatalog()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "windows-release-dates.json");
        try
        {
            using var stream = File.OpenRead(path);
            return JsonSerializer.Deserialize<WindowsReleaseCatalog>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private sealed record WindowsReleaseCatalog(
        Dictionary<string, Dictionary<string, string>>? Releases,
        Dictionary<string, string>? BuildVersions,
        Dictionary<string, string>? Sources);
}
