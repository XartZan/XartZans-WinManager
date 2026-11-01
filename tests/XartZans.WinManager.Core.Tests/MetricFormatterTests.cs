using XartZans.WinManager.Core.Formatting;

namespace XartZans.WinManager.Core.Tests;

public sealed class MetricFormatterTests
{
    [Fact]
    public void FormatBytes_UsesReadableUnit()
    {
        var formatted = MetricFormatter.FormatBytes(1536);

        Assert.StartsWith("1", formatted);
        Assert.EndsWith("KB", formatted);
    }

    [Fact]
    public void FormatRate_MarksUnavailableMeasurements()
    {
        Assert.Equal("—", MetricFormatter.FormatRate(null));
    }

    [Fact]
    public void FormatPercent_ClampsOutOfRangeValues()
    {
        Assert.Equal("100%", MetricFormatter.FormatPercent(150));
        Assert.Equal("0%", MetricFormatter.FormatPercent(-3));
    }
}
