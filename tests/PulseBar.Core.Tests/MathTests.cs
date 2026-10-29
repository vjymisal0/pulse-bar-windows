using PulseBar.Core;

namespace PulseBar.Core.Tests;

public class MathTests
{
    [Theory]
    [InlineData(60, Level.Ok)]
    [InlineData(70, Level.Warn)]
    [InlineData(90, Level.Hot)]
    public void LevelBands(double value, Level expected) => Assert.Equal(expected, Levels.For(value, 70, 85));

    [Fact]
    public void LevelMissingIsOk() => Assert.Equal(Level.Ok, Levels.For(null, 70, 85));

    [Fact]
    public void RateNeedsPrevious() => Assert.Null(Rates.PerSecond(null, 100, 1));

    [Fact]
    public void RateOnCounterReset() => Assert.Null(Rates.PerSecond(500, 100, 1));

    [Fact]
    public void Rate() => Assert.Equal(200, Rates.PerSecond(100, 400, 1.5));

    [Fact]
    public void CpuPercentCountsKernelMinusIdle()
    {
        // Kernel time includes idle time: 900 kernel + 100 user = 1000 elapsed, 800 idle -> 20% busy.
        var prev = new CpuTimes(Idle: 0, Kernel: 0, User: 0);
        var cur = new CpuTimes(Idle: 800, Kernel: 900, User: 100);
        Assert.Equal(20, Rates.CpuPercent(prev, cur));
    }

    [Fact]
    public void CpuPercentNeedsPrevious() => Assert.Null(Rates.CpuPercent(null, new CpuTimes(1, 2, 3)));

    [Fact]
    public void HistoryKeepsNewest()
    {
        var h = new History(3);
        foreach (var v in new double?[] { 1, 2, 3, 4 })
            h.Add(v);
        Assert.Equal([2, 3, 4], h.Values);
    }
}
