using System.Globalization;

namespace XartZans.WinManager.Core.Formatting;

public static class MetricFormatter
{
    private static readonly string[] ByteUnits = ["B", "KB", "MB", "GB", "TB"];

    public static string FormatBytes(ulong bytes)
    {
        var value = (double)bytes;
        var unit = 0;
        while (value >= 1024 && unit < ByteUnits.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return string.Create(CultureInfo.CurrentCulture, $"{value:0.#} {ByteUnits[unit]}");
    }

    public static string FormatRate(double? bytesPerSecond)
    {
        if (bytesPerSecond is null || !double.IsFinite(bytesPerSecond.Value) || bytesPerSecond < 0)
        {
            return "—";
        }

        var value = bytesPerSecond.Value;
        var unit = "B/s";
        if (value >= 1024 * 1024)
        {
            value /= 1024 * 1024;
            unit = "MB/s";
        }
        else if (value >= 1024)
        {
            value /= 1024;
            unit = "KB/s";
        }

        return string.Create(CultureInfo.CurrentCulture, $"{value:0.#} {unit}");
    }

    public static string FormatPercent(double? percent)
    {
        if (percent is not double value || !double.IsFinite(value))
        {
            return "—";
        }

        return string.Create(CultureInfo.CurrentCulture, $"{Math.Clamp(value, 0, 100):0}%");
    }
}
