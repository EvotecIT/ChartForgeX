using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void YAxisFormattingStaysIsolatedFromGenericValues() {
        var chart = Chart.Create()
            .WithSize(420, 280)
            .WithValueFormatter(_ => "generic-value")
            .ConfigureYAxis(axis => axis.LabelFormatter = _ => "axis-only")
            .WithDataLabels()
            .AddLine("Values", Points(10, 20, 30));

        var svg = chart.ToSvg();
        Assert(svg.Contains(">axis-only</text>", StringComparison.Ordinal), "Primary y-axis ticks should use their axis formatter.");
        Assert(svg.Contains(">generic-value</text>", StringComparison.Ordinal), "Cartesian data labels should keep the generic value formatter.");
        Assert(chart.ToPng().Length > 64, "Independent y-axis and generic value formatters should render through the PNG path.");

        var pieSvg = Chart.Create()
            .WithValueFormatter(_ => "generic-pie-value")
            .ConfigureYAxis(axis => axis.LabelFormatter = _ => "axis-only")
            .WithDataLabels()
            .WithPieSliceLabelContent(ChartPieSliceLabelContent.Value)
            .AddPie("Share", Points(70, 30))
            .ToSvg();
        Assert(pieSvg.Contains("generic-pie-value", StringComparison.Ordinal), "Non-axis chart values should keep the generic formatter.");
        Assert(!pieSvg.Contains("axis-only", StringComparison.Ordinal), "Y-axis-only formatting should not leak into non-axis chart values.");
    }

    private static void PrimaryYAxisFormattingReservesPngPlotSpace() {
        var shortChart = FormattedAxisChart(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture));
        var longChart = FormattedAxisChart(value => "$" + value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " milliseconds");
        var shortPixels = ReadPngRgba(shortChart.ToPng(), out var width, out _);
        var longPixels = ReadPngRgba(longChart.ToPng(), out _, out _);
        var shortAxis = FindNearColorBounds(shortPixels, width, 255, 0, 255, 4);
        var longAxis = FindNearColorBounds(longPixels, width, 255, 0, 255, 4);

        Assert(!shortAxis.IsEmpty && !longAxis.IsEmpty, "PNG y-axis reserve proof should find both configured axis rules.");
        Assert(longAxis.Left > shortAxis.Left + 20, "Long primary y-axis labels should move the PNG plot right even when no secondary axis is present.");
    }

    private static Chart FormattedAxisChart(Func<double, string> formatter) {
        var theme = ChartTheme.ReportLight();
        theme.Axis = ChartColor.FromHex("#FF00FF");
        var chart = Chart.Create()
            .WithSize(420, 280)
            .WithTheme(theme)
            .WithGrid(false)
            .WithLegend(false)
            .ConfigureYAxis(axis => axis.LabelFormatter = formatter)
            .AddLine("Latency", Points(1000000, 1120000, 1080000));
        chart.Options.XAxis.ShowLine = false;
        return chart;
    }
}
