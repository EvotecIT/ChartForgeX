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
        Assert(bars.Contains("data-cfx-role=\"bar\" data-cfx-series=\"1\" data-cfx-point=\"1\" data-cfx-x=\"2\" data-cfx-y=\"25\" data-cfx-base=\"55\"", System.StringComparison.Ordinal), "Bars should expose point x, value, and stacked base metadata.");

        var horizontal = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithStackedBars()
            .AddHorizontalBar("Complete", Points(40, 55))
            .AddHorizontalBar("Partial", Points(15, 20))
            .ToSvg();
        Assert(horizontal.Contains("data-cfx-role=\"horizontal-bar\" data-cfx-series=\"1\" data-cfx-point=\"1\" data-cfx-category=\"2\" data-cfx-value=\"20\" data-cfx-base=\"55\"", System.StringComparison.Ordinal), "Horizontal bars should expose category, value, and stacked base metadata.");

        var lines = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .AddLine("Trend", Points(4, 8, 12))
            .ToSvg();
        Assert(lines.Contains("data-cfx-role=\"line\" data-cfx-series=\"0\" data-cfx-point-count=\"3\"", System.StringComparison.Ordinal), "Lines should expose point count metadata.");
        Assert(lines.Contains("data-cfx-role=\"line-marker\" data-cfx-series=\"0\" data-cfx-point=\"2\" data-cfx-x=\"3\" data-cfx-y=\"12\"", System.StringComparison.Ordinal), "Line markers should expose point x/y metadata.");

        var scatter = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .AddScatter("Observed", new[] {
                new ChartPoint(1, 42),
                new ChartPoint(2, 58)
            })
            .ToSvg();
        Assert(scatter.Contains("data-cfx-role=\"scatter-point\" data-cfx-series=\"0\" data-cfx-point=\"1\" data-cfx-x=\"2\" data-cfx-y=\"58\"", System.StringComparison.Ordinal), "Scatter points should expose point x/y metadata.");

        var lollipop = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .AddLollipop("Coverage", Points(74, 88))
            .ToSvg();
        Assert(lollipop.Contains("data-cfx-role=\"lollipop-marker\" data-cfx-series=\"0\" data-cfx-point=\"1\" data-cfx-x=\"2\" data-cfx-y=\"88\"", System.StringComparison.Ordinal), "Lollipop markers should expose point x/y metadata.");

        var rangeBar = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .AddRangeBar("Observed", new[] {
                new ChartInterval(1, 32, 44),
                new ChartInterval(2, 38, 58)
            })
            .ToSvg();
        Assert(rangeBar.Contains("data-cfx-role=\"range-bar\" data-cfx-series=\"0\" data-cfx-point=\"1\" data-cfx-x=\"2\" data-cfx-start=\"38\" data-cfx-end=\"58\"", System.StringComparison.Ordinal), "Range bars should expose x, start, and end metadata.");
    }
}
