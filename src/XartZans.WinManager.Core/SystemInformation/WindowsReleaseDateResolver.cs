using System.Globalization;

namespace XartZans.WinManager.Core.SystemInformation;

public static class WindowsReleaseDateResolver
{
    public static DateOnly? Resolve(
        string? displayVersion,
        int build,
        IReadOnlyDictionary<string, string> releaseDates,
        IReadOnlyDictionary<string, string> buildVersions)
    {
        ArgumentNullException.ThrowIfNull(releaseDates);
        ArgumentNullException.ThrowIfNull(buildVersions);

        var releaseVersion = string.IsNullOrWhiteSpace(displayVersion) ? null : displayVersion.Trim();
        if (releaseVersion is null || !releaseDates.ContainsKey(releaseVersion))
        {
            buildVersions.TryGetValue(build.ToString(CultureInfo.InvariantCulture), out releaseVersion);
        }

        return releaseVersion is not null &&
               releaseDates.TryGetValue(releaseVersion, out var dateText) &&
               DateOnly.TryParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
    }
}
