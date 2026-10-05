using ChartForgeX;
using ChartForgeX.Core;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void PieAndDonutSvgExposeSliceMetadata() {
        var pie = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithXLabels("Passed", "Failed")
            .AddPie("Results", Points(75, 25))
            .ToSvg();
        Assert(pie.Contains("data-cfx-role=\"pie-slice\" data-cfx-point=\"0\" data-cfx-label=\"Passed\" data-cfx-value=\"75\" data-cfx-percent=\"0.75\"", System.StringComparison.Ordinal), "Pie slices should expose label, value, and percent metadata.");

        var donut = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithXLabels("Passed", "Failed")
            .AddDonut("Results", Points(75, 25))
            .ToSvg();
        Assert(donut.Contains("data-cfx-role=\"donut-slice\" data-cfx-point=\"0\" data-cfx-label=\"Passed\" data-cfx-value=\"75\" data-cfx-percent=\"0.75\"", System.StringComparison.Ordinal), "Donut slices should expose label, value, and percent metadata.");
        Assert(donut.Contains("data-cfx-role=\"donut-total-label\"", System.StringComparison.Ordinal), "Donuts should expose center total role metadata.");

        var positionedLegend = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithLegendPosition(ChartLegendPosition.TopRight)
            .WithXLabels("Passed", "Failed", "Skipped")
            .AddDonut("Results", Points(70, 20, 10));
        var positionedLegendSvg = positionedLegend.ToSvg();
        Assert(positionedLegendSvg.Contains("data-cfx-role=\"slice-legend\" data-cfx-position=\"TopRight\"", System.StringComparison.Ordinal), "Pie and donut slice legends should expose their configured legend position.");
        Assert(positionedLegendSvg.Contains("data-cfx-role=\"slice-legend-percent\" data-cfx-point=\"1\"", System.StringComparison.Ordinal), "Slice legends should keep per-slice percent labels.");
        Assert(positionedLegend.ToPng().Length > 64, "Positioned slice legends should render PNG output.");
    }
}
