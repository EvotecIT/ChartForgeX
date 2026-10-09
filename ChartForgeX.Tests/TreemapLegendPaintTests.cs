using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TreemapLegendPaintTests {
    [Fact]
    public void LeafKeysRetainNumericStatusExplicitAndMissingPaintProvenance() {
        var red = ChartColor.FromRgb(255, 0, 0);
        var blue = ChartColor.FromRgb(0, 0, 255);
        var missing = ChartColor.FromRgb(13, 47, 81);
        var chart = Chart.Create().WithPointLegend().AddTreemap("Colors", new[] {
            new ChartTreemapItem("low", "Low", value: 4, colorValue: 0),
            new ChartTreemapItem("explicit", "Explicit", value: 3, colorValue: 100),
            new ChartTreemapItem("missing", "Missing", value: 2)
        }).ConfigureTreemap(options => {
            options.ColorScale = ChartColorScale.Sequential(red, blue).WithValueRange(0, 100).WithNoDataColor(missing);
            options.ShowColorScaleLegend = false;
        });
        chart.Series[0].WithPointColor(1, blue);
        var variables = new SvgColorVariables().Add("--host-low", red, SvgColorRole.Ramp)
            .Add("--host-explicit", blue, SvgColorRole.Series).Add("--host-missing", missing, SvgColorRole.Ramp);
        var xml = XDocument.Parse(chart.Prepare(VisualExportRequest.ForChart(chart).Context)
            .ToSvg(new VisualSvgOptions("paint", variables)));
        MatchingPaint(xml, "low", "--host-low");
        MatchingPaint(xml, "explicit", "--host-explicit");
        MatchingPaint(xml, "missing", "--host-missing");

        var status = Chart.Create().WithPointLegend().WithLegend().AddTreemap("States", new[] {
            new ChartTreemapItem("warning", "Warning", value: 1)
        });
        status.Series[0].WithNodeState("warning", ChartSeriesState.Warning);
        var context = VisualExportRequest.ForChart(status).Context;
        var warning = context.Theme.Resolve(context.ThemeMode).Status.Medium.Fill;
        xml = XDocument.Parse(status.Prepare(context).ToSvg(new VisualSvgOptions("status",
            new SvgColorVariables().Add("--host-warning", warning, SvgColorRole.Status))));
        MatchingPaint(xml, "warning", "--host-warning");
        Assert.Equal("Warning", (string?)Key(xml, "warning").Attribute("data-cfx-state"));
    }

    [Fact]
    public void HierarchySeriesKeyRetainsItsCanonicalStatusPaint() {
        var chart = Chart.Create().WithLegend().AddTreemap("Allocation", new[] {
            new ChartTreemapItem("one", "One", value: 1)
        });
        chart.Series[0].StateRole = ChartSeriesState.Warning;
        var context = VisualExportRequest.ForChart(chart).Context;
        var warning = context.Theme.Resolve(context.ThemeMode).Status.Medium.Fill;
        var xml = XDocument.Parse(chart.Prepare(context).ToSvg(new VisualSvgOptions("series",
            new SvgColorVariables().Add("--host-warning", warning, SvgColorRole.Status))));
        var mark = xml.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "treemap-tile-mark");
        var swatch = xml.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "legend-swatch");
        Assert.Contains("var(--host-warning,", (string?)mark.Attribute("fill"));
        Assert.Equal((string?)mark.Attribute("fill"), (string?)swatch.Attribute("fill"));
    }

    private static XElement Key(XDocument xml, string id) => xml.Descendants().Single(element =>
        (string?)element.Attribute("data-cfx-role") == "legend-entry" && (string?)element.Attribute("data-cfx-legend-target-id") == id);

    private static void MatchingPaint(XDocument xml, string id, string variable) {
        var node = xml.Descendants().Single(element => (string?)element.Attribute("data-cfx-target-id") == id);
        var mark = node.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "treemap-tile-mark");
        var swatch = Key(xml, id).Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "legend-swatch");
        Assert.Contains("var(" + variable + ",", (string?)mark.Attribute("fill"));
        Assert.Equal((string?)mark.Attribute("fill"), (string?)swatch.Attribute("fill"));
    }
}
