using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;
using static ChartForgeX.Tests.ScatterBubbleColorRenderingTests;

namespace ChartForgeX.Tests;

/// <summary>Independent observations share captured color text while Y, Size, paint and raw facts retain their policies.</summary>
public sealed class IndependentColorDimensionTests {
    [Theory]
    [InlineData(ChartSeriesKind.Scatter, "uniform", 360, false)]
    [InlineData(ChartSeriesKind.Scatter, "positioned", 800, true)]
    [InlineData(ChartSeriesKind.Scatter, "discrete", 800, false)]
    [InlineData(ChartSeriesKind.Bubble, "uniform", 800, true)]
    [InlineData(ChartSeriesKind.Bubble, "positioned", 360, false)]
    [InlineData(ChartSeriesKind.Bubble, "discrete", 800, true)]
    public void CulturalColorCaptionsLeaveYSizePaintAndSourceFactsIndependent(ChartSeriesKind kind, string mode, int width, bool dark) {
        var chart = ColorDimensionPresentationFixtures.Create(kind, mode, width, dark);
        var series = chart.Series[0]; var bubble = kind == ChartSeriesKind.Bubble;
        if (mode == "uniform") series.WithColorScale(ChartColorScale.Sequential(Low, High).WithValueRange(0, 20));
        var ordinary = XDocument.Parse(Prepare(chart).ToSvg());
        series.ConfigureColorDimension(options => {
            options.Name = "Temperature";
            options.ValueFormat = ChartValueFormat.Number("0.000 '°C'", CultureInfo.GetCultureInfo("fr-FR"));
        });
        var prepared = Prepare(chart); var svg = XDocument.Parse(prepared.ToSvg()); var points = Role(svg, "point");
        Assert.Equal(new[] { "7.125", "0", null }, points.Select(point => (string?)point.Attribute("data-cfx-color-value")));
        Assert.Equal("7,125 °C", (string?)points[0].Attribute("data-cfx-formatted-color-value"));
        Assert.Equal("0,000 °C", (string?)points[1].Attribute("data-cfx-formatted-color-value"));
        Assert.Equal("Temperature", (string?)points[0].Attribute("data-cfx-label-color"));
        Assert.Equal("18", (string?)points[0].Attribute("data-cfx-y"));
        Assert.Equal(bubble ? "9.0 units" : "18.0 units", (string?)points[0].Attribute("data-cfx-label"));
        Assert.Contains("Temperature: 7,125 °C", prepared.Regions.Single(region => region.Id == "series-0-point-0").Label);
        Assert.Null(points[2].Attribute("data-cfx-formatted-color-value"));
        Assert.Equal("true", (string?)points[2].Attribute("data-cfx-color-missing"));
        if (bubble) {
            Assert.Equal("9", (string?)points[0].Attribute("data-cfx-size"));
            Assert.Equal("0", (string?)points[0].Attribute("data-cfx-source-point"));
            Assert.Equal("1", (string?)points[0].Attribute("data-cfx-source-count"));
        } else Assert.Equal("0", (string?)points[0].Attribute("data-cfx-source-point"));
        Assert.Equal(Role(ordinary, bubble ? "bubble" : "marker").Select(mark => mark.RenderedColor("fill")),
            Role(svg, bubble ? "bubble" : "marker").Select(mark => mark.RenderedColor("fill")));
        var key = Role(svg, "color-scale-0").Single();
        Assert.Contains("°C", key.Value); Assert.DoesNotContain("units", key.Value);
        if (mode == "discrete") Assert.Equal("5", (string?)Role(svg, "color-scale-0-step")[0].Attribute("data-cfx-upper-bound"));
        else Assert.All(Role(svg, "color-scale-0-step"), sample => {
            Assert.Equal("Temperature", (string?)sample.Attribute("data-cfx-label-value"));
            Assert.EndsWith("°C", (string?)sample.Attribute("data-cfx-formatted-value"));
        });
        if (mode == "positioned") {
            var anchor = Role(svg, "color-scale-0-stop-source")[1];
            Assert.Equal("8", (string?)anchor.Attribute("data-cfx-value"));
            Assert.Equal("8,000 °C", (string?)anchor.Attribute("data-cfx-formatted-value"));
        }
        Capture(prepared, $"dimension-{kind}-{mode}-{width}-{(dark ? "dark" : "light")}");
    }

    [Theory]
    [InlineData(ChartSeriesKind.Treemap, false)]
    [InlineData(ChartSeriesKind.Sunburst, true)]
    public void HierarchyColorPolicyLeavesSizesTotalsAndSecondaryContextIndependent(ChartSeriesKind kind, bool discrete) {
        var chart = Chart.Create().WithSize(800, 500).WithDataLabels(false).WithValueFormat(ChartValueFormat.Number("0.0 'items'"));
        var items = new[] { new ChartHierarchyItem("root", "Root", colorValue: 0),
            new ChartHierarchyItem("a", "A", "root", 9, 7.125), new ChartHierarchyItem("b", "B", "root", 4) };
        var contexts = new List<ChartSunburstLabelContext>();
        if (kind == ChartSeriesKind.Sunburst) chart.AddSunburst("Samples", items)
            .ConfigureSunburst(options => options.SecondaryLabelFormatter = value => { contexts.Add(value); return value.FormattedValue + " secondary"; });
        else chart.AddTreemap("Samples", items);
        var scale = discrete ? ChartColorScale.Discrete(new[] { new ChartColorBand(5, Low), new ChartColorBand(null, High) })
            : ChartColorScale.Sequential(new[] { new ChartColorStop(0, Low), new ChartColorStop(8, Empty), new ChartColorStop(20, High) });
        var options = kind == ChartSeriesKind.Sunburst ? chart.Options.Sunburst.ColorDimension : chart.Options.Treemap.ColorDimension;
        if (kind == ChartSeriesKind.Sunburst) chart.Options.Sunburst.ColorScale = scale; else chart.Options.Treemap.ColorScale = scale;
        options.Name = "Temperature"; options.ValueFormat = ChartValueFormat.Number("0.000 '°C'", CultureInfo.GetCultureInfo("fr-FR"));
        var prepared = Prepare(chart); var svg = XDocument.Parse(prepared.ToSvg());
        var nodes = svg.Descendants().Where(node => (string?)node.Attribute("data-cfx-target-kind") == "node")
            .ToDictionary(node => (string)node.Attribute("data-cfx-target-id")!);
        Assert.Equal("13", (string?)nodes["root"].Attribute("data-cfx-value"));
        Assert.Equal("13.0 items", (string?)nodes["root"].Attribute("data-cfx-formatted-value"));
        Assert.Equal("7.125", (string?)nodes["a"].Attribute("data-cfx-color-value"));
        Assert.Equal("7,125 °C", (string?)nodes["a"].Attribute("data-cfx-formatted-color-value"));
        Assert.Null(nodes["b"].Attribute("data-cfx-color-value")); Assert.Equal("true", (string?)nodes["b"].Attribute("data-cfx-color-missing"));
        var prefix = kind == ChartSeriesKind.Sunburst ? "sunburst" : "treemap";
        var key = Role(svg, prefix + "-color-scale").Single();
        Assert.Contains("Temperature", key.Value); Assert.Contains("°C", key.Value); Assert.DoesNotContain("items", key.Value);
        if (kind == ChartSeriesKind.Sunburst) {
            Assert.Equal(3, contexts.Count); Assert.All(contexts, value => Assert.EndsWith("items", value.FormattedValue));
            Assert.Equal(13, contexts.Single(value => value.Item.Id == "root").Value);
            Assert.Contains("13.0 items secondary", prepared.Regions.Single(region => region.Id == "series-0-node-root").Label);
        }
        Capture(prepared, "dimension-" + prefix);
    }
}
