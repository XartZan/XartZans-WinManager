using XartZans.WinManager.Core.SystemInformation;

namespace XartZans.WinManager.Core.Tests;

public sealed class WindowsReleaseDateResolverTests
{
    private static readonly IReadOnlyDictionary<string, string> ReleaseDates = new Dictionary<string, string>
    {
        ["26H2"] = "2026-09-29",
        ["25H2"] = "2025-09-30"
    };

    private static readonly IReadOnlyDictionary<string, string> BuildVersions = new Dictionary<string, string>
    {
        ["26300"] = "26H2",
        ["26200"] = "25H2"
    };

    [Fact]
    public void Resolve_UsesKnownDisplayVersion()
    {
        var date = WindowsReleaseDateResolver.Resolve("26H2", 26300, ReleaseDates, BuildVersions);

        Assert.Equal(new DateOnly(2026, 9, 29), date);
    }

    [Fact]
    public void Resolve_FallsBackToBuildWhenDisplayVersionIsUnknown()
    {
        var date = WindowsReleaseDateResolver.Resolve("future-version", 26200, ReleaseDates, BuildVersions);

        Assert.Equal(new DateOnly(2025, 9, 30), date);
    }

    [Fact]
    public void Resolve_ReturnsNullForUnknownBuildAndRelease()
    {
        var date = WindowsReleaseDateResolver.Resolve("future-version", 99999, ReleaseDates, BuildVersions);

        Assert.Null(date);
    }
}
