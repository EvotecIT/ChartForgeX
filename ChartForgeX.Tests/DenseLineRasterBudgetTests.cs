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
    // Scanning that stroke outline as one shape grew with the square of its density (about 28 s here);
    // the PNG must stay a small multiple of the second it takes now.
    // The time budget is Category=Performance, so normal CI on shared runners leaves it out.
    [Fact]
    [Trait("Category", "Performance")]
    public void DenseRawLinePngStaysWithinRasterBudget() {
        var prepared = Prepare();
        var watch = Stopwatch.StartNew();
        var png = prepared.ToPng();
        watch.Stop();
        _output.WriteLine($"80k-point line PNG: {watch.ElapsedMilliseconds} ms, {png.Length} bytes; budget 6000 ms.");
        Assert.True(watch.ElapsedMilliseconds <= 6_000, $"Dense line PNG took {watch.ElapsedMilliseconds} ms.");
    }

    [Fact]
    public void DenseRawLineRendersToPng() => Assert.True(Prepare().ToPng().Length > 1_000);

    private static PreparedVisual Prepare() => Chart.Create().Size(1200, 600).Title("Dense raw line").Legend(false)
        .AddLine("Signal", Signal(80_000))
        .Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(1200, 600))));

    private static IEnumerable<ChartPoint> Signal(int count) {
        uint state = 12345;
        for (var i = 0; i < count; i++) {
            state = unchecked(state * 1664525u + 1013904223u);
            var noise = ((state >> 8) % 1000) / 1000.0 - 0.5;
            yield return new ChartPoint(i, Math.Sin(i * 6.0 / count * Math.PI) * 40 + Math.Sin(i * 0.37) * 6 + noise * 8 + 100);
        }
    }
}
