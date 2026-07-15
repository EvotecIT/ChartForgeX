using ChartForgeX;
using ChartForgeX.Core;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void RadarHonorsAxesAndGridVisibility() {
        var axesOff = RadarSample()
            .WithAxes(false)
            .ToSvg();
        Assert(axesOff.Contains("data-cfx-role=\"radar-area\"", System.StringComparison.Ordinal), "Radar polygons should still render when axes are disabled.");
        Assert(!axesOff.Contains("data-cfx-role=\"radar-axis-label\"", System.StringComparison.Ordinal), "Radar category labels should hide when axes are disabled.");
        Assert(!axesOff.Contains("data-cfx-role=\"radar-ring-label\"", System.StringComparison.Ordinal), "Radar ring labels should hide when axes are disabled.");

        var gridOff = RadarSample()
            .WithGrid(false)
            .ToSvg();
        Assert(gridOff.Contains("data-cfx-role=\"radar-outline\"", System.StringComparison.Ordinal), "Radar outlines should still render when grid is disabled.");
        Assert(!gridOff.Contains("data-cfx-role=\"radar-ring\"", System.StringComparison.Ordinal), "Radar rings should hide when grid is disabled.");
        Assert(!gridOff.Contains("data-cfx-role=\"radar-spoke\"", System.StringComparison.Ordinal), "Radar spokes should hide when grid is disabled.");
        Assert(RadarSample().WithAxes(false).WithGrid(false).ToPng().Length > 64, "Compact radar options should render valid PNG output.");
        var positionedLegend = RadarSample().WithLegendPosition(ChartLegendPosition.Right);
        Assert(positionedLegend.ToSvg().Contains("data-cfx-role=\"legend\" data-cfx-position=\"Right\"", System.StringComparison.Ordinal), "Radar charts should use the shared positioned legend.");
        Assert(positionedLegend.ToPng().Length > 64, "Positioned radar legends should render valid PNG output.");

        var axisTicks = RadarSample();
        axisTicks.Options.XAxis.TickCount = 2;
        axisTicks.Options.YAxis.TickCount = 10;
        Assert(CountOccurrences(axisTicks.ToSvg(), "data-cfx-role=\"radar-ring\"") == 10, "Radar value rings should use the y-axis tick count independently from the category x-axis.");
        Assert(axisTicks.ToPng().Length > 64, "Radar y-axis tick counts should render through the PNG path.");
    }

    private static void RadarNegativeExplicitMaximumInfersCompatibleMinimum() {
        var chart = Chart.Create()
            .WithSize(520, 360)
            .WithXLabels("A", "B", "C")
            .AddRadar("Debt", Points(-12, -10.5, -11));
        chart.Options.YAxisMaximum = -10;

        var svg = chart.ToSvg();

        Assert(svg.Contains("data-cfx-role=\"radar-area\"", System.StringComparison.Ordinal), "Radar charts with negative explicit maximums should still render polygons.");
        Assert(!svg.Contains("NaN", System.StringComparison.Ordinal) && !svg.Contains("Infinity", System.StringComparison.Ordinal), "Radar charts with negative explicit maximums should avoid invalid SVG coordinates.");
        Assert(chart.ToPng().Length > 64, "Radar charts with negative explicit maximums should render valid PNG output.");
    }

    private static Chart RadarSample() => Chart.Create()
        .WithSize(520, 360)
        .WithXLabels("Coverage", "Policy", "Alerts", "Response")
        .AddRadar("Current", Points(92, 74, 88, 81));
}
