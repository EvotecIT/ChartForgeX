using System.Diagnostics;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;
using Xunit.Abstractions;

namespace ChartForgeX.Tests;

[Collection(nameof(RenderingBudgetCollection))]
public sealed class DenseLineRasterBudgetTests {
    private readonly ITestOutputHelper _output;
    public DenseLineRasterBudgetTests(ITestOutputHelper output) => _output = output;

    // Tens of thousands of raw points squeeze about seventy segments into each device pixel column.
    // Scanning that stroke outline as one shape grew with the square of its density (about 18 s here);
    // the PNG must stay a small multiple of the second it takes now.
    [Fact]
    public void DenseRawLinePngStaysWithinRasterBudget() {
        var prepared = Chart.Create().WithSize(1200, 600).WithTitle("Dense raw line").WithLegend(false)
            .AddLine("Signal", Signal(80_000))
            .Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(1200, 600))));
        var watch = Stopwatch.StartNew();
        var png = prepared.ToPng();
        watch.Stop();
        _output.WriteLine($"80k-point line PNG: {watch.ElapsedMilliseconds} ms, {png.Length} bytes; budget 6000 ms.");
        Assert.True(png.Length > 1_000);
        Assert.True(watch.ElapsedMilliseconds <= 6_000, $"Dense line PNG took {watch.ElapsedMilliseconds} ms.");
    }

    private static IEnumerable<ChartPoint> Signal(int count) {
        uint state = 12345;
        for (var i = 0; i < count; i++) {
            state = unchecked(state * 1664525u + 1013904223u);
            var noise = ((state >> 8) % 1000) / 1000.0 - 0.5;
            yield return new ChartPoint(i, Math.Sin(i * 6.0 / count * Math.PI) * 40 + Math.Sin(i * 0.37) * 6 + noise * 8 + 100);
        }
    }
}
