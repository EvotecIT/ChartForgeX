using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Paint-only tiny-flow omission preserves proportional allocation, raw totals and retained color facts.</summary>
public sealed class SankeyTinyFlowTests {
    [Fact]
    public void OmittedTinyRibbonsRetainEndpointSlotsNodesConservationAndIndependentColorDomain() {
        var chart = Model(999, 1); var source = SankeyFlowColorTests.Prepare(chart);
        var raw = chart.Series[0].FlowLinks.ToArray();
        chart.ConfigureSankey(options => options.RibbonVisibilityThreshold = 2);
        chart.Series[0].WithFlowStyle("small", new ChartFlowStyle(fillPattern: ChartFillPattern.Crosshatch,
            outlineStroke: new ChartStrokeStyle(ChartColor.Black, 3, dash: new[] { 2d, 2d })));
        var omitted = SankeyFlowColorTests.Prepare(chart); var small = SankeyFlowColorTests.Link(omitted, "small");
        Assert.Equal("below-paint-threshold", (string?)small.Attribute("data-cfx-geometry-status"));
        Assert.Empty(small.Descendants()); Assert.Equal("100", (string?)small.Attribute("data-cfx-color-value"));
        Assert.Equal(raw, chart.Series[0].FlowLinks);
        Assert.Equal(source.Regions.Select(region => region.Bounds), omitted.Regions.Select(region => region.Bounds));
        Assert.Equal(SankeyFlowColorTests.Ribbons(source)[0].Commands, Assert.Single(SankeyFlowColorTests.Ribbons(omitted)).Commands);
        Assert.All(SankeyFlowColorTests.Links(source), link => {
            var id = (string)link.Attribute("data-cfx-target-id")!;
            Assert.Equal((string?)link.Attribute("data-cfx-width"), (string?)SankeyFlowColorTests.Link(omitted, id).Attribute("data-cfx-width"));
        });
        var nodes = SankeyFlowColorTests.Role(omitted, "sankey-node");
        Assert.Equal("1000", (string?)nodes[0].Attribute("data-cfx-outgoing"));
        var root = omitted.Regions.Single(region => region.Id == "series-0-node-r");
        var widths = SankeyFlowColorTests.Links(omitted).Select(link => Number(link, "width")).ToArray();
        Assert.Equal(root.Bounds.Height, widths.Sum(), 10); Assert.Equal(999, widths[0] / widths[1], 10);
        chart.WithLegend(true); var keyed = SankeyFlowColorTests.Prepare(chart);
        var key = Assert.Single(SankeyFlowColorTests.Role(keyed, "sankey-color-scale"));
        Assert.Equal("-5", (string?)key.Attribute("data-cfx-min-value")); Assert.Equal("100", (string?)key.Attribute("data-cfx-max-value"));
        var snapshot = omitted.ToSvg(); chart.Options.Sankey.RibbonVisibilityThreshold = 0;
        Assert.Equal(snapshot, omitted.ToSvg()); Assert.Equal(2, SankeyFlowColorTests.Ribbons(SankeyFlowColorTests.Prepare(chart)).Length);
    }

    [Fact]
    public void UnrepresentablePositiveWidthKeepsItsRawFlowIdentityAndReportsPrecisionCollapse() {
        var chart = Model(1e308, double.Epsilon).WithLegend(true);
        var prepared = SankeyFlowColorTests.Prepare(chart); var tiny = SankeyFlowColorTests.Link(prepared, "small");
        Assert.Equal(double.Epsilon.ToString("R", CultureInfo.InvariantCulture), (string?)tiny.Attribute("data-cfx-value"));
        Assert.Equal("0", (string?)tiny.Attribute("data-cfx-width"));
        Assert.Equal("precision-collapse", (string?)tiny.Attribute("data-cfx-geometry-status")); Assert.Empty(tiny.Descendants());
        Assert.Equal("r", (string?)tiny.Attribute("data-cfx-source")); Assert.Equal("small", (string?)tiny.Attribute("data-cfx-target"));
        Assert.Equal("100", (string?)tiny.Attribute("data-cfx-color-value"));
        Assert.Contains(prepared.Regions, region => region.Id == "series-0-link-small" && region.Label?.Contains("100", StringComparison.Ordinal) == true);
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "sankey.precision-collapse");
        Assert.Single(SankeyFlowColorTests.Ribbons(prepared)); Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(360, false)] [InlineData(360, true)] [InlineData(800, false)] [InlineData(800, true)]
    public void CompactAndWideTinyFlowCapturesRetainReadableFactsWithoutInflatingRibbons(int width, bool dark) {
        var chart = Model(999, 1).WithSize(width, 400).WithDataLabels(true).WithLegend(true)
            .WithHeader(true).WithTitle("Large and small request flows")
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        chart.ConfigureSankey(options => { options.RibbonVisibilityThreshold = 2; options.ColorDimension.Name = "Latency (ms)"; });
        var prepared = SankeyFlowColorTests.Prepare(chart);
        Assert.Equal("below-paint-threshold", (string?)SankeyFlowColorTests.Link(prepared, "small").Attribute("data-cfx-geometry-status"));
        Assert.Equal(2, SankeyFlowColorTests.Links(prepared).Length); Assert.Equal(3, SankeyFlowColorTests.Role(prepared, "sankey-node").Length);
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        ScatterBubbleColorRenderingTests.Capture(prepared, "sankey-tiny-flow-" + width + "-" + (dark ? "dark" : "light"));
    }

    [Theory]
    [InlineData(-1)] [InlineData(double.NaN)] [InlineData(double.PositiveInfinity)]
    public void InvalidVisibilityThresholdLeavesThePreviousSnapshotInstalled(double invalid) {
        var chart = Model(999, 1); var original = chart.Options.Sankey;
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.ConfigureSankey(options => options.RibbonVisibilityThreshold = invalid));
        Assert.Same(original, chart.Options.Sankey); Assert.Equal(0, original.RibbonVisibilityThreshold);
    }

    private static Chart Model(double large, double small) => Chart.Create().WithSize(800, 400).WithHeader(false).WithLegend(false).WithDataLabels(false)
        .AddSankey("Requests", new[] { new ChartNode("r", "Received"), new ChartNode("large", "Automatic"), new ChartNode("small", "Manual") },
            new[] { new ChartFlowLink("large", "r", "large", large, -5), new ChartFlowLink("small", "r", "small", small, 100) })
        .ConfigureSankey(options => { options.FlowColorMode = ChartSankeyFlowColorMode.Quantitative;
            options.ColorScale = ChartColorScale.Sequential(SankeyFlowColorTests.Low, SankeyFlowColorTests.High); });
    private static double Number(XElement element, string name) => double.Parse(element.Attribute("data-cfx-" + name)!.Value, CultureInfo.InvariantCulture);
}
