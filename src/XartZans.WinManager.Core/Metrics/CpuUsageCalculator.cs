namespace XartZans.WinManager.Core.Metrics;

public readonly record struct CpuTimeSample(ulong Idle, ulong Kernel, ulong User)
{
    public ulong Total => Kernel + User;
}

public static class CpuUsageCalculator
{
    public static double? Calculate(CpuTimeSample previous, CpuTimeSample current)
    {
        if (current.Total <= previous.Total || current.Idle < previous.Idle)
        {
            return null;
        }

        var totalDelta = current.Total - previous.Total;
        var idleDelta = current.Idle - previous.Idle;
        if (idleDelta > totalDelta)
        {
            return null;
        }

        return Math.Clamp((double)(totalDelta - idleDelta) / totalDelta * 100, 0, 100);
    }
}
