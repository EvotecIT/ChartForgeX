using System.Diagnostics;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using Xunit;
using Xunit.Abstractions;

namespace ChartForgeX.Tests;

/// <summary>
/// Catches superlinear static export. Four times the points may cost at most about twice the linear expectation,
/// so machine speed and ordinary noise cancel out while a quadratic step (about 16x) fails.
/// </summary>
[Collection(nameof(RenderingBudgetCollection))]
public sealed class StaticExportScalingTests {
    private const int SmallPoints = 2_000;
    private const int Scale = 4;
    private const double TimeRatioLimit = 8;
    private const double AllocationRatioLimit = 6;
    private readonly ITestOutputHelper _output;

    public StaticExportScalingTests(ITestOutputHelper output) => _output = output;

    [Theory]
    [InlineData("svg")]
    [InlineData("png")]
    public void RawLineExportCostGrowsLinearlyWithPointCount(string format) {
        Func<Chart, int> export = format == "svg" ? chart => chart.ToSvg().Length : chart => chart.ToPng().Length;
        var small = Measure(SmallPoints, export);
        var large = Measure(SmallPoints * Scale, export);
        double timeRatio = large.Milliseconds / Math.Max(small.Milliseconds, 1), allocationRatio = (double)large.Allocated / small.Allocated;
        _output.WriteLine($"{format}: {SmallPoints} points {small.Milliseconds:F1} ms / {small.Allocated} bytes; "
            + $"{SmallPoints * Scale} points {large.Milliseconds:F1} ms / {large.Allocated} bytes; ratios {timeRatio:F2}x time, {allocationRatio:F2}x allocation.");
        Assert.True(allocationRatio <= AllocationRatioLimit, $"{format}: {Scale}x points allocated {allocationRatio:F2}x (limit {AllocationRatioLimit}x).");
        Assert.True(timeRatio <= TimeRatioLimit, $"{format}: {Scale}x points took {timeRatio:F2}x as long (limit {TimeRatioLimit}x).");
    }

    // The best of three runs removes scheduling noise; allocation on this thread is deterministic for one model.
    private static (double Milliseconds, long Allocated) Measure(int points, Func<Chart, int> export) {
        var chart = Chart.Create().WithSize(1200, 600).WithTitle("Export scaling").WithLegend(false)
            .AddLine("Signal", Enumerable.Range(0, points).Select(index => new ChartPoint(index, Math.Sin(index * .013) * 40 + index % 17)));
        Assert.True(export(chart) > 0);
        var best = double.MaxValue;
        long allocated = 0;
        for (var run = 0; run < 3; run++) {
            var before = GC.GetAllocatedBytesForCurrentThread();
            var watch = Stopwatch.StartNew();
            export(chart);
            watch.Stop();
            allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            best = Math.Min(best, watch.Elapsed.TotalMilliseconds);
        }
        return (best, allocated);
    }
}
