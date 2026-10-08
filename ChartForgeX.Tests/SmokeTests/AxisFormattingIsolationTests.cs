using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.Rendering;

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

        var secondary = Chart.Create()
            .WithSize(700, 280)
            .WithValueFormatter(_ => "generic-secondary")
            .ConfigureYAxis(axis => axis.LabelFormatter = _ => "primary-fixed")
            .WithTickLabelStyle(style => style.WithWeight("650").WithItalic())
            .WithSecondaryYAxis("Rate")
            .AddLine("Rate", Points(20, 40, 60));
        secondary.Series[0].UseSecondaryYAxis();
        var secondaryPrepared = PreparedFamily(secondary);
        Assert(secondaryPrepared.Regions.Any(region => region.Role == "axis-secondary-y-label" && region.Label!.StartsWith("generic-secondary (", StringComparison.Ordinal)),
            "Secondary tick regions should retain the complete fallback-formatted value.");
        var secondarySvg = secondary.ToSvg();
        Assert(secondarySvg.Contains(">generic-secondary</text>", StringComparison.Ordinal), "Secondary y-axis ticks should fall back to the generic value formatter when the axis has no dedicated formatter.");
        Assert(secondarySvg.Contains("data-cfx-role=\"axis-secondary-y-label\"", StringComparison.Ordinal) && secondarySvg.Contains("font-weight=\"650\"", StringComparison.Ordinal) && secondarySvg.Contains("font-style=\"italic\"", StringComparison.Ordinal), "Secondary SVG axis ticks should honor the shared tick-label typography style.");
        var secondaryPng = secondary.ToPng();
        secondary.WithValueFormatter(_ => "changed-secondary");
        Assert(!secondaryPng.AsSpan().SequenceEqual(secondary.ToPng()), "Secondary PNG ticks should use the same generic formatter fallback as SVG output.");

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
        var shortAxis = PreparedAxis(shortChart);
        var longAxis = PreparedAxis(longChart);
        Assert(longAxis.Start.X > shortAxis.Start.X + 20, "Long primary y-axis labels should reserve more plot space with the same shared native geometry.");
        var longPng = longChart.ToPng();
        longChart.Options.YAxis.ShowLine = false;
        Assert(!longPng.AsSpan().SequenceEqual(longChart.ToPng()), "The configured axis rule should paint native raster pixels at its measured position.");

        const string sample = "MMMMMMMMiiiiiiii";
        foreach (var family in new[] { "serif", "monospace" }) {
            var chart = FormattedAxisChart(_ => sample).WithSize(900, 280).WithTickLabelStyle(style => style.WithFontFamily(family).WithItalic());
            var context = VisualExportRequest.ForChart(chart).Context;
            var scene = chart.Prepare(context).Scene;
            var rule = scene.Nodes.OfType<VisualSceneLine>().Single(node => node.Role == "axis-y");
            var ticks = scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "axis-y-label").ToArray();
            Assert(ticks.Length > 0 && ticks.All(tick => tick.Text.Style.Font.Family == family && tick.Text.Style.Font.Italic),
                "Axis labels should retain the requested font family and italic style in the shared prepared run.");
            var widest = ticks.Max(tick => tick.Text.Metrics.Width);
            Assert(Math.Abs(rule.Start.X - chart.Options.Padding.Left - context.Theme.Spacing - widest) < .001,
                "Axis reservation should use the actual retained font metrics rather than a renderer-specific fixed gutter.");
            Assert(ticks.All(tick => tick.X + tick.Text.Metrics.Width <= rule.Start.X - context.Theme.Spacing + .001),
                "Prepared tick ink should fit completely within the measured strip beside the plot.");
            Assert(chart.ToPng().Length > 64, "Font-specific axis strips should render native PNG output.");
        }

        static VisualSceneLine PreparedAxis(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context).Scene.Nodes
            .OfType<VisualSceneLine>().Single(node => node.Role == "axis-y");
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
