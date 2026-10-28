using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Independent Sankey color facts and endpoint policies retain proportional weights and detached native output.</summary>
public sealed class SankeyFlowColorTests {
    internal static readonly ChartColor Low = ChartColor.FromHex("#2166AC"), High = ChartColor.FromHex("#B2182B"), Missing = ChartColor.FromHex("#9CA3AF");

    [Fact]
    public void DefaultSourceOutputIsStableAndTargetPaintUsesTheSelectedEndpointWithoutMovingFlows() {
        var chart = Model(null, null, null); var series = chart.Series[0];
        series.WithPointColor(0, Low).WithPointColor(1, High).WithPointColor(2, Missing);
        series.WithNodeState("a", ChartSeriesState.Danger).WithPointFillPattern(1, ChartFillPattern.Crosshatch);
        var before = Prepare(chart); var svg = before.ToSvg(); var png = before.ToPng();
        chart.ConfigureSankey(options => options.FlowColorMode = ChartSankeyFlowColorMode.Source);
        Assert.Equal(svg, Prepare(chart).ToSvg()); Assert.Equal(png, Prepare(chart).ToPng());
        chart.ConfigureSankey(options => options.FlowColorMode = ChartSankeyFlowColorMode.Target);
        var target = Prepare(chart); var paths = Ribbons(target);
        Assert.Equal(ChartColorMath.WithOpacity(High, .35), paths[0].Fill);
        Assert.Equal(ChartColorMath.WithOpacity(Missing, .35), paths[1].Fill);
        Assert.Equal("Danger", (string?)Link(target, "p0").Attribute("data-cfx-state"));
        Assert.Contains(Link(target, "p0").Descendants(), node => (string?)node.Attribute("data-cfx-role") == "sankey-ribbon-pattern");
        for (int index = 0; index < paths.Length; index++) Assert.Equal(Ribbons(before)[index].Commands, paths[index].Commands);
        Assert.Equal(before.Regions.Select(region => region.Bounds), target.Regions.Select(region => region.Bounds));
        series.WithFlowStyle("p0", new ChartFlowStyle(fill: ChartColor.Black, fillPattern: ChartFillPattern.None)).WithFlowState("p0", ChartSeriesState.None);
        var overridden = Prepare(chart);
        Assert.Equal(ChartColorMath.WithOpacity(ChartColor.Black, .35), Ribbons(overridden)[0].Fill);
        Assert.Equal("None", (string?)Link(overridden, "p0").Attribute("data-cfx-state"));
        Assert.DoesNotContain(Link(overridden, "p0").Descendants(), node => (string?)node.Attribute("data-cfx-role") == "sankey-ribbon-pattern");
    }

    [Fact]
    public void SignedZeroAndMissingMeasurementsMapIndependentlyOfWeightAndManualPaint() {
        var chart = Model(-10, 0, null); var source = Prepare(chart); var series = chart.Series[0];
        chart.ConfigureSankey(options => { options.FlowColorMode = ChartSankeyFlowColorMode.Quantitative;
            options.ColorScale = ChartColorScale.Sequential(Low, High).WithNoDataColor(Missing); });
        var prepared = Prepare(chart); var paths = Ribbons(prepared);
        Assert.Equal(ChartColorMath.WithOpacity(Low, .35), paths[0].Fill);
        Assert.Equal(ChartColorMath.WithOpacity(High, .35), paths[1].Fill);
        Assert.Equal(ChartColorMath.WithOpacity(Missing, .35), paths[2].Fill);
        Assert.Equal(new[] { "-10", "0", null }, Links(prepared).Select(link => (string?)link.Attribute("data-cfx-color-value")));
        Assert.Equal("true", (string?)Link(prepared, "p2").Attribute("data-cfx-color-missing"));
        for (int index = 0; index < paths.Length; index++) Assert.Equal(Ribbons(source)[index].Commands, paths[index].Commands);
        series.Color = Missing; Assert.All(Ribbons(Prepare(chart)), path => Assert.Equal(ChartColorMath.WithOpacity(Missing, .35), path.Fill));
        series.WithPointColor(0, High); Assert.All(Ribbons(Prepare(chart)), path => Assert.Equal(ChartColorMath.WithOpacity(High, .35), path.Fill));
        series.WithFlowStyle("p0", new ChartFlowStyle(fill: Low, fillOpacity: .6));
        Assert.Equal(ChartColorMath.WithOpacity(Low, .6), Ribbons(Prepare(chart))[0].Fill);
        Assert.Equal(new[] { 4d, 3d, 3d }, series.FlowLinks.Select(link => link.Value));
        Assert.Equal("-10", (string?)Link(Prepare(chart), "p0").Attribute("data-cfx-color-value"));
    }

    [Fact]
    public void NestedConfigurationAndPreparationCaptureIndependentColorOptionsAndFormatter() {
        var chart = Model(-10, 0, 10); var calls = new Dictionary<double, int>();
        chart.ConfigureSankey(options => { options.FlowColorMode = ChartSankeyFlowColorMode.Quantitative;
            options.ColorDimension.Name = "Latency"; options.ColorDimension.ValueFormat = ChartValueFormat.Custom(value => {
                calls[value] = calls.TryGetValue(value, out var count) ? count + 1 : 1; return value.ToString("R", CultureInfo.InvariantCulture) + " ms";
            }); });
        var original = chart.Options.Sankey;
        Assert.Throws<InvalidOperationException>(() => chart.ConfigureSankey(options => { options.ColorDimension.Name = "Rejected"; throw new InvalidOperationException(); }));
        Assert.Same(original, chart.Options.Sankey); Assert.Equal("Latency", chart.Options.Sankey.ColorDimension.Name);
        ChartSankeyOptions? retained = null;
        chart.ConfigureSankey(options => { retained = options; options.ColorDimension.Name = "Delay"; });
        retained!.ColorDimension.Name = "Uncommitted";
        Assert.Equal("Delay", chart.Options.Sankey.ColorDimension.Name);
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng(); var count = calls.Values.Sum();
        Assert.All(calls.Values, value => Assert.Equal(1, value));
        chart.Options.Sankey.ColorDimension.ValueFormat = ChartValueFormat.Custom(_ => throw new InvalidOperationException("Mutable model formatter"));
        chart.Series[0].ReplaceFlowData(chart.Series[0].Nodes, chart.Series[0].FlowLinks.Select(flow => new ChartFlowLink(flow.Id, flow.SourceId, flow.TargetId, flow.Value, 99)));
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng()); Assert.Equal(count, calls.Values.Sum());
        Assert.Equal("Delay", (string?)Link(prepared, "p0").Attribute("data-cfx-label-color"));
    }

    [Theory]
    [InlineData(360, false)] [InlineData(360, true)] [InlineData(800, false)] [InlineData(800, true)]
    public void NumericKeyAndNativeExportsRetainAllColorFactsAcrossCompactWideAndBothThemes(int width, bool dark) {
        var chart = Model(-10, 0, null).WithSize(width, 400).WithDataLabels(true).WithLegend(true)
            .WithHeader(true).WithTitle("Request volume and latency").WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        chart.ConfigureSankey(options => { options.FlowColorMode = ChartSankeyFlowColorMode.Quantitative;
            options.ColorDimension.Name = "Latency (ms)"; options.ColorScale = ChartColorScale.Sequential(Low, High).WithNoDataColor(Missing); });
        var prepared = Prepare(chart);
        ScatterBubbleColorRenderingTests.Capture(prepared, "sankey-flow-color-" + width + "-" + (dark ? "dark" : "light"));
        var key = Role(prepared, "sankey-color-scale").Single();
        Assert.Equal("-10", (string?)key.Attribute("data-cfx-min-value")); Assert.Equal("0", (string?)key.Attribute("data-cfx-max-value"));
        var captions = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "sankey-color-scale-label").ToArray();
        Assert.Equal(3, captions.Length); // Both numeric endpoints and the authored missing-value key remain visible.
        var low = Assert.Single(captions, node => node.Id == "sankey-color-scale-label-low");
        var high = Assert.Single(captions, node => node.Id == "sankey-color-scale-label-high");
        var missing = Assert.Single(captions, node => node.Id == "sankey-color-scale-label-missing");
        Assert.Equal(chart.Options.Labels.NoData, string.Concat(missing.Text.Lines.Select(line => line.Text)));
        Assert.Contains("-10", string.Concat(low.Text.Lines.Select(line => line.Text)));
        Assert.EndsWith("0", string.Concat(high.Text.Lines.Select(line => line.Text)));
        var lowBounds = CartesianReversalLabelTests.Bounds(low); var highBounds = CartesianReversalLabelTests.Bounds(high);
        Assert.True(lowBounds.Width > 0 && highBounds.Width > 0 && lowBounds.Right <= highBounds.Left,
            "Compact and wide automatic keys must paint distinct, nonoverlapping numeric endpoints.");
        Assert.Equal(3, Links(prepared).Length); Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        var png = RasterImageDecoder.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        Assert.Equal(width, png.Width); Assert.Equal(400, png.Height);
    }

    [Fact]
    public void InvalidPoliciesAreRejectedWithoutChangingTheInstalledConfiguration() {
        var chart = Model(null, null, null); var original = chart.Options.Sankey;
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.ConfigureSankey(options => options.FlowColorMode = (ChartSankeyFlowColorMode)99));
        Assert.Same(original, chart.Options.Sankey);
        chart.Options.Sankey.ColorScale = ChartColorScale.Sequential(Low, High);
        Assert.Throws<InvalidOperationException>(() => Prepare(chart));
        chart.ConfigureSankey(options => options.FlowColorMode = ChartSankeyFlowColorMode.Quantitative);
        Assert.NotEmpty(Prepare(chart).ToPng());
    }

    internal static Chart Model(params double?[] values) => Chart.Create().WithSize(800, 460).WithHeader(false).WithLegend(false).WithDataLabels(false)
        .AddSankey("Requests", new[] { new ChartNode("r", "Received"), new ChartNode("a", "Automatic"), new ChartNode("b", "Manual"), new ChartNode("c", "Review") },
            values.Select((value, index) => new ChartFlowLink("p" + index, "r", new[] { "a", "b", "c" }[index], index == 0 ? 4 : 3, value)));
    internal static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    internal static VisualScenePath[] Ribbons(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "sankey-ribbon").ToArray();
    internal static XElement[] Role(PreparedVisual prepared, string role) => XDocument.Parse(prepared.ToSvg()).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == role).ToArray();
    internal static XElement[] Links(PreparedVisual prepared) => Role(prepared, "sankey-link");
    internal static XElement Link(PreparedVisual prepared, string id) => Links(prepared).Single(node => (string?)node.Attribute("data-cfx-target-id") == id);
}
