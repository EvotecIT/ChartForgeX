using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Equal RGB tokens must retain the producer's meaning across prepared family exports.</summary>
public sealed partial class V2PreparedFamilyPaintTests {
    private static readonly ChartColor Shared = ChartColor.FromHex("#2864B4");

    [Theory]
    [InlineData(ChartSeriesKind.Radar, "radar-area", "fill")]
    [InlineData(ChartSeriesKind.Polar, "polar-line", "stroke")]
    [InlineData(ChartSeriesKind.PolarArea, "polar-area-segment", "fill")]
    [InlineData(ChartSeriesKind.Circle, "circle-value", "fill")]
    [InlineData(ChartSeriesKind.Bullet, "bullet-value", "fill")]
    [InlineData(ChartSeriesKind.ProgressBar, "progress-fill", "fill")]
    [InlineData(ChartSeriesKind.Tree, "tree-node-mark", "fill")]
    [InlineData(ChartSeriesKind.Treemap, "treemap-tile-mark", "fill")]
    [InlineData(ChartSeriesKind.Sunburst, "sunburst-segment-mark", "fill")]
    [InlineData(ChartSeriesKind.Sankey, "sankey-node-mark", "fill")]
    [InlineData(ChartSeriesKind.Funnel, "funnel-segment", "fill")]
    [InlineData(ChartSeriesKind.Pictorial, "pictorial-fill", "fill")]
    [InlineData(ChartSeriesKind.WordCloud, "word-cloud-text", "fill")]
    public void PreparedMarksKeepStateAndAuthoredSeriesDistinctWithEqualRgb(ChartSeriesKind kind, string role, string attribute) {
        var chart = V2GalleryModels.Create(kind);
        foreach (var series in chart.Series) series.StateRole = ChartSeriesState.Danger;
        var context = Context(); var variables = Variables();
        var prepared = chart.Prepare(context);
        Assert.All(Paints(prepared, variables, role, attribute), paint => Assert.Contains("var(--status,", paint));
        foreach (var series in chart.Series) series.Color = Shared;
        var explicitSeries = chart.Prepare(context);
        Assert.All(Paints(explicitSeries, variables, role, attribute), paint => Assert.Contains("var(--series,", paint));
    }

    [Fact]
    public void DottedDefaultsAreStatusesAndExplicitPointOverridesRetainSeriesRole() {
        var chart = Chart.Create().AddDottedMap("Locations", new[] { new ChartMapPoint("First", 0, 20), new ChartMapPoint("Second", 10, 30) })
            .AddMapRouteBetweenPoints("Connection", "First", "Second");
        chart.Series[0].WithPointColor(0, Shared);
        var prepared = chart.Prepare(Context()); var variables = Variables();
        var points = Paints(prepared, variables, "dotted-map-point", "fill");
        Assert.Contains("var(--series,", points[0]); Assert.Contains("var(--status,", points[1]);
        Assert.All(Paints(prepared, variables, "dotted-map-connector", "stroke"), paint => Assert.Contains("var(--status,", paint));
        var rgba = prepared.ToRgba(new VisualRenderOptions(supersampling: 1)).Pixels;
        chart.Options.SvgColorVariables = variables;
        Assert.Equal(rgba, chart.Prepare(Context()).ToRgba(new VisualRenderOptions(supersampling: 1)).Pixels);
    }

    [Fact]
    public void SankeyNodeStateOverridesAndFunnelContrastInkUseTheirAuthoredRoles() {
        var sankey = V2GalleryModels.Create(ChartSeriesKind.Sankey);
        sankey.Series[0].WithNodeState(sankey.Series[0].Nodes[0].Id, ChartSeriesState.Danger);
        var nodePaints = Paints(sankey.Prepare(Context()), Variables(), "sankey-node-mark", "fill");
        Assert.All(nodePaints, paint => Assert.Contains("var(--status,", paint));
        sankey.Series[0].WithPointColor(1, Shared);
        nodePaints = Paints(sankey.Prepare(Context()), Variables(), "sankey-node-mark", "fill");
        Assert.Contains("var(--status,", nodePaints[0]); Assert.Contains("var(--series,", nodePaints[1]);
        var funnel = V2GalleryModels.Create(ChartSeriesKind.Funnel).WithDataLabels(); funnel.Series[0].Color = Shared;
        var variables = Variables().AddInk("--series-ink", Shared, ChartColor.White, SvgColorRole.Series);
        Assert.All(Paints(funnel.Prepare(Context()), variables, "funnel-label", "fill"), paint => Assert.Contains("var(--series-ink,", paint));
        funnel.Options.DataLabelStyle.Color = Shared;
        Assert.All(Paints(funnel.Prepare(Context()), variables, "funnel-label", "fill"), paint => Assert.Contains("var(--text,", paint));
    }

    [Theory]
    [InlineData(ChartSeriesKind.RegionMap, "region-map-region")]
    [InlineData(ChartSeriesKind.TileMap, "tile-map-region")]
    public void MapScalesRetainStateBlendOperandsAndExplicitPointColors(ChartSeriesKind kind, string role) {
        var chart = V2GalleryModels.Create(kind); chart.Series[0].StateRole = ChartSeriesState.Danger;
        chart.Series[0].WithPointColor(0, Shared);
        var prepared = chart.Prepare(Context(showLegend: true));
        var document = XDocument.Parse(prepared.ToSvg(new VisualSvgOptions(colorVariables: Variables())));
        string PointFill(string point) => document.Descendants().Single(element =>
            (string?)element.Attribute("data-cfx-point") == point && (string?)element.Attribute("data-cfx-empty") == "false")
            .Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == role).Attribute("fill")!.Value;
        Assert.Contains("var(--series,", PointFill("0"));
        Assert.Contains("var(--status,", PointFill("1"));
        Assert.Contains("var(--surface,", PointFill("1"));
        Assert.All(Paints(prepared, Variables(), "map-scale-step", "fill"), paint => Assert.Contains("var(--status,", paint));
        Assert.All(Paints(prepared, Variables(), "map-scale-no-data", "fill"), paint => Assert.Contains("var(--surface,", paint));
    }

    private static VisualRenderContext Context(bool showLegend = false) {
        var tokens = new VisualDesignTokens { Background = Shared, Surface = Shared, Foreground = Shared, MutedForeground = Shared, Border = Shared, Palette = new[] { Shared } };
        tokens.Status.Critical = new VisualTokenColor(Shared, ChartColor.White);
        tokens.Status.Medium = new VisualTokenColor(Shared, ChartColor.White);
        tokens.Status.Pass = new VisualTokenColor(Shared, ChartColor.White);
        tokens.Status.Neutral = new VisualTokenColor(Shared, ChartColor.White);
        return new VisualRenderContext(layout: new(new(800, 520)), theme: new VisualTheme(tokens, tokens), frame: new(showLegend: showLegend));
    }

    private static SvgColorVariables Variables() => new SvgColorVariables().Add("--surface", Shared, SvgColorRole.Surface)
        .Add("--status", Shared, SvgColorRole.Status).Add("--series", Shared, SvgColorRole.Series).Add("--text", Shared, SvgColorRole.Text);

    private static string[] Paints(PreparedVisual prepared, SvgColorVariables variables, string role, string attribute) {
        var document = XDocument.Parse(prepared.ToSvg(new VisualSvgOptions(colorVariables: variables)));
        var paints = document.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role)
            .SelectMany(element => element.Attributes(attribute).Concat(element.Descendants().Attributes(attribute)))
            .Select(paint => paint.Value).ToArray();
        Assert.NotEmpty(paints); return paints;
    }
}
