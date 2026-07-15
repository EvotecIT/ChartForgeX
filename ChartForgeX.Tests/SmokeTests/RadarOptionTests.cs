using System.Globalization;
using System.Linq;
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

        var baselinePng = RadarSample().ToPng();
        var hiddenValueAxis = RadarSample().ConfigureYAxis(axis => axis.Visible = false);
        var hiddenValueAxisSvg = hiddenValueAxis.ToSvg();
        Assert(!hiddenValueAxisSvg.Contains("data-cfx-role=\"radar-ring-label\"", System.StringComparison.Ordinal), "Radar ring labels should follow Y-axis visibility.");
        Assert(hiddenValueAxisSvg.Contains("data-cfx-role=\"radar-axis-label\"", System.StringComparison.Ordinal), "Hiding the radar value axis should preserve visible category labels.");
        Assert(!baselinePng.SequenceEqual(hiddenValueAxis.ToPng()), "PNG radar ring labels should follow Y-axis visibility.");

        var hiddenCategoryAxis = RadarSample().ConfigureXAxis(axis => axis.Visible = false);
        var hiddenCategoryAxisSvg = hiddenCategoryAxis.ToSvg();
        Assert(!hiddenCategoryAxisSvg.Contains("data-cfx-role=\"radar-axis-label\"", System.StringComparison.Ordinal), "Radar category labels should follow X-axis visibility.");
        Assert(hiddenCategoryAxisSvg.Contains("data-cfx-role=\"radar-ring-label\"", System.StringComparison.Ordinal), "Hiding radar categories should preserve visible value-axis labels.");
        Assert(!baselinePng.SequenceEqual(hiddenCategoryAxis.ToPng()), "PNG radar category labels should follow X-axis visibility.");

        var positionedLegend = RadarSample().WithLegendPosition(ChartLegendPosition.Right);
        Assert(positionedLegend.ToSvg().Contains("data-cfx-role=\"legend\" data-cfx-position=\"Right\"", System.StringComparison.Ordinal), "Radar charts should use the shared positioned legend.");
        Assert(positionedLegend.ToPng().Length > 64, "Positioned radar legends should render valid PNG output.");

        var axisTicks = RadarSample();
        axisTicks.Options.XAxis.TickCount = 2;
        axisTicks.Options.YAxis.TickCount = 10;
        Assert(CountOccurrences(axisTicks.ToSvg(), "data-cfx-role=\"radar-ring\"") == 10, "Radar value rings should use the y-axis tick count independently from the category x-axis.");
        Assert(axisTicks.ToPng().Length > 64, "Radar y-axis tick counts should render through the PNG path.");

        var formattedRings = RadarSample().ConfigureYAxis(axis => {
            axis.WithBounds(0, 100);
            axis.TickCount = 6;
            axis.Labels.Add(new ChartAxisLabel(20, "explicit-ring"));
            axis.LabelFormatter = value => "ring-" + value.ToString("0", CultureInfo.InvariantCulture);
        });
        var formattedSvg = formattedRings.ToSvg();
        Assert(formattedSvg.Contains(">explicit-ring</text>", System.StringComparison.Ordinal), "Radar rings should honor explicit y-axis labels.");
        Assert(formattedSvg.Contains(">ring-40</text>", System.StringComparison.Ordinal), "Radar rings should honor generated y-axis label formatters.");
        var formattedPng = formattedRings.ToPng();
        formattedRings.Options.YAxis.Labels.Clear();
        formattedRings.Options.YAxis.LabelFormatter = _ => "different-ring";
        Assert(!formattedPng.SequenceEqual(formattedRings.ToPng()), "PNG radar rings should respond to y-axis label formatting.");
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
