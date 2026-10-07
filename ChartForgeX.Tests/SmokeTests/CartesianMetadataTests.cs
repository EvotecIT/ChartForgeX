using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void CartesianSvgElementsExposePointMetadata() {
        var bars = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithStackedBars()
            .AddBar("Passed", Points(40, 55))
            .AddBar("Warnings", Points(15, 25))
            .ToSvg();
        CartesianMetadata(CartesianPoint(bars, 1, 1), ("x", "2"), ("y", "25"), ("base", "55"));

        var horizontal = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithStackedBars()
            .AddHorizontalBar("Complete", Points(40, 55))
            .AddHorizontalBar("Partial", Points(15, 20))
            .ToSvg();
        CartesianMetadata(CartesianPoint(horizontal, 1, 1), ("category", "2"), ("value", "20"), ("base", "55"));

        var lines = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .AddLine("Trend", Points(4, 8, 12))
            .ToSvg();
        Assert(CountOccurrences(lines, "data-cfx-role=\"point\"") == 3, "Lines retain every point even when markers are hidden.");
        CartesianMetadata(CartesianPoint(lines, 0, 2), ("x", "3"), ("y", "12"));

        var scatter = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .AddScatter("Observed", new[] {
                new ChartPoint(1, 42),
                new ChartPoint(2, 58)
            })
            .ToSvg();
        CartesianMetadata(CartesianPoint(scatter, 0, 1), ("x", "2"), ("y", "58"));

        var lollipop = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .AddLollipop("Coverage", Points(74, 88))
            .ToSvg();
        CartesianMetadata(CartesianPoint(lollipop, 0, 1), ("x", "2"), ("y", "88"));

        var rangeBar = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .AddRangeBar("Observed", new[] {
                new ChartInterval(1, 32, 44),
                new ChartInterval(2, 38, 58)
            })
            .ToSvg();
        CartesianMetadata(CartesianPoint(rangeBar, 0, 1), ("x", "2"), ("start", "38"), ("end", "58"));
    }
}
