using XartZans.WinManager.Core.Metrics;

namespace XartZans.WinManager.Core.Tests;

public sealed class CpuUsageCalculatorTests
{
    [Fact]
    public void Calculate_ReturnsBusyTimePercentage()
    {
        var previous = new CpuTimeSample(Idle: 100, Kernel: 200, User: 100);
        var current = new CpuTimeSample(Idle: 400, Kernel: 1_000, User: 500);

        var usage = CpuUsageCalculator.Calculate(previous, current);

        Assert.Equal(75, usage);
    }

    [Fact]
    public void Calculate_ReturnsNullWhenThereIsNoElapsedTime()
    {
        var sample = new CpuTimeSample(Idle: 100, Kernel: 200, User: 100);

        Assert.Null(CpuUsageCalculator.Calculate(sample, sample));
    }

    [Fact]
    public void Calculate_ReturnsNullWhenCountersMoveBackwards()
    {
        var previous = new CpuTimeSample(Idle: 100, Kernel: 200, User: 100);
        var current = new CpuTimeSample(Idle: 90, Kernel: 300, User: 200);

        Assert.Null(CpuUsageCalculator.Calculate(previous, current));
    }
}
