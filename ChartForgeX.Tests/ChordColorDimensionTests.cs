using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Independent flow measurements retain weighted geometry, authored identity and native paint contracts.</summary>
public sealed partial class ChordColorDimensionTests {
    private static readonly ChartColor Low = ChartColor.FromHex("#2166AC"), High = ChartColor.FromHex("#B2182B");

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.PositiveInfinity)]
    public void IndependentFlowColorRequiresFiniteMeasurements(double invalid) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartFlowLink("flow", "a", "b", 1, invalid));

    [Fact]
    public void NullColorPreservesLegacyOutputAndSignedZeroColorDoesNotBecomeWeight() {
        var legacy = Chart.Create().AddChord("Transfers", Nodes(), new[] { new ChartFlowLink("flow", "a", "b", 7) });
        var nullable = Chart.Create().AddChord("Transfers", Nodes(), new[] { new ChartFlowLink("flow", "a", "b", 7, null) });
        Assert.Equal(Prepare(legacy).ToSvg(), Prepare(nullable).ToSvg());
        Assert.Equal(Prepare(legacy).ToPng(), Prepare(nullable).ToPng());
        var zero = new ChartFlowLink("zero", "a", "b", 7, 0); var negative = new ChartFlowLink("negative", "a", "b", 2, -12);
        Assert.Equal(7, zero.Value); Assert.Equal((double?)0, zero.ColorValue); Assert.Equal((double?)-12, negative.ColorValue);
        var sankey = Chart.Create().AddSankey("Independent color fact", Nodes(), new[] { zero });
        Assert.Equal((double?)0, Assert.Single(sankey.Series[0].FlowLinks).ColorValue);
    }

    [Fact]
    public void ColorOnlyChangesPreserveParallelReciprocalSelfAndZeroFlowGeometry() {
        var ordinary = ChordRecipe.Create("wide"); var measured = ChordRecipe.Create("color-dimension");
        var before = Prepare(ordinary); var after = Prepare(measured);
        Assert.Equal(ordinary.Series[0].FlowLinks.Select(flow => (flow.Id, flow.SourceId, flow.TargetId, flow.Value)),
            measured.Series[0].FlowLinks.Select(flow => (flow.Id, flow.SourceId, flow.TargetId, flow.Value)));
        Assert.Equal(9, Links(after).Length);
        var source = measured.Series[0].FlowLinks;
        Assert.Equal(source.Select(flow => flow.Id), Links(after).Select(link => (string?)link.Attribute("data-cfx-target-id")));
        foreach (var role in new[] { "chord-ribbon", "chord-target-cue" }) {
            var oldPaths = before.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == role).ToArray();
            var newPaths = after.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == role).ToArray();
            Assert.Equal(oldPaths.Length, newPaths.Length);
            for (var index = 0; index < oldPaths.Length; index++) Assert.Equal(oldPaths[index].Commands, newPaths[index].Commands);
        }
        foreach (var role in new[] { "chord-link", "chord-node" }) {
            var oldFacts = PreparedChordTests.Role(before, role); var newFacts = PreparedChordTests.Role(after, role);
            for (var index = 0; index < oldFacts.Length; index++) foreach (var name in new[] {
                "target-id", "value", "source", "target", "source-link-index", "source-start-angle", "source-sweep", "target-start-angle", "target-sweep",
                "incoming", "outgoing", "start-angle", "sweep", "center-x", "center-y", "inner-radius", "outer-radius", "geometry-status"
            }) Assert.Equal((string?)oldFacts[index].Attribute("data-cfx-" + name), (string?)newFacts[index].Attribute("data-cfx-" + name));
        }
        Assert.Equal(before.Regions.Select(region => region.Bounds), after.Regions.Select(region => region.Bounds));
        foreach (var flow in source) {
            var link = Link(after, flow.Id);
            Assert.Equal("transfer-source", (string?)link.Attribute("data-cfx-series-key"));
            Assert.Equal(flow.ColorValue?.ToString("R", CultureInfo.InvariantCulture), (string?)link.Attribute("data-cfx-color-value"));
            Assert.Equal(flow.ColorValue.HasValue ? null : "true", (string?)link.Attribute("data-cfx-color-missing"));
        }
        Assert.Equal("0", (string?)Link(after, "zero-transfer").Attribute("data-cfx-value"));
        Assert.Equal("210", (string?)Link(after, "zero-transfer").Attribute("data-cfx-color-value"));
        Assert.Empty(Link(after, "zero-transfer").Descendants());
        Assert.All(PreparedChordTests.Role(after, "chord-node"), node => Assert.Null(node.Attribute("data-cfx-color-value")));
    }

    [Fact]
    public void SemanticOnlyFlowsStillContributeTheirOwnColorDomainWithoutInventingGeometry() {
        var series = ChartSeries.CreateChord(new ChartSeriesId("precision-colors"), "Transfers", new[] {
            new ChartNode("a", "A"), new ChartNode("b", "B"), new ChartNode("c", "C"), new ChartNode("d", "D")
        }, new[] {
            new ChartFlowLink("tiny", "a", "b", double.Epsilon, 100), new ChartFlowLink("large", "c", "d", 1e308, 10),
            new ChartFlowLink("zero", "a", "b", 0, -50), new ChartFlowLink("missing", "b", "a", 0, null)
        });
        series.ShowInLegend = false;
        var chart = Chart.Create().AddSeries(series).ConfigureChord(options => options.ColorScale = ChartColorScale.Sequential(Low, High));
        var prepared = Prepare(chart, legend: true); var key = PreparedChordTests.Role(prepared, "chord-color-scale").Single();
        Assert.Equal("-50", (string?)key.Attribute("data-cfx-min-value")); Assert.Equal("100", (string?)key.Attribute("data-cfx-max-value"));
        Assert.Equal("precision-collapse", (string?)Link(prepared, "tiny").Attribute("data-cfx-geometry-status"));
        Assert.Equal("100", (string?)Link(prepared, "tiny").Attribute("data-cfx-color-value"));
        Assert.Empty(Link(prepared, "tiny").Descendants()); Assert.Empty(Link(prepared, "zero").Descendants());
        Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "chord-ribbon");
        Assert.Equal("true", (string?)Link(prepared, "missing").Attribute("data-cfx-color-missing"));
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "chord.precision-collapse");
    }

    [Fact]
    public void NumericPaintRetainsManualPrecedenceStateAlphaPatternStrokeAndDomainFacts() {
        var chart = Model(new double?[] { -10, 0, 10, 30, null }); var series = chart.Series[0];
        series.WithNodeState("a", ChartSeriesState.Danger).WithFlowState("priority", ChartSeriesState.Info);
        var initial = Prepare(chart, legend: true);
        Assert.Equal(ChartColorMath.WithOpacity(Low, .35), Ribbons(initial)[0].Fill);
        Assert.Equal("Danger", (string?)Link(initial, "forward").Attribute("data-cfx-state"));
        var variables = new SvgColorVariables().Add("--measured", Low, SvgColorRole.Ramp).Add("--manual", Low, SvgColorRole.Series);
        var numericSvg = XDocument.Parse(initial.ToSvg(new VisualSvgOptions("color", variables)));
        Assert.Contains("var(--measured,", numericSvg.Descendants().Single(node => (string?)node.Attribute("data-cfx-target-id") == "forward")
            .Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "chord-ribbon").Attribute("fill")!.Value, StringComparison.Ordinal);
        series.Color = ChartColor.FromHex("#365F63");
        Assert.Equal(ChartColorMath.WithOpacity(series.Color.Value, .35), Ribbons(Prepare(chart))[0].Fill);
        var sourceColor = ChartColor.FromHex("#7356BD"); series.WithPointColor(0, sourceColor);
        Assert.Equal(ChartColorMath.WithOpacity(sourceColor, .35), Ribbons(Prepare(chart))[0].Fill);
        var authored = ChartColor.FromRgba(41, 94, 131, 200);
        series.WithFlowStyle("forward", new ChartFlowStyle(fill: authored, fillOpacity: .25, fillPattern: ChartFillPattern.Crosshatch,
            stroke: ChartColor.Black, strokeWidth: 2, strokeOpacity: .4));
        var after = Prepare(chart, legend: true); var ribbon = Ribbons(after)[0];
        Assert.Equal(ChartColorMath.WithOpacity(authored, .25), ribbon.Fill); Assert.Equal(ChartColorMath.WithOpacity(ChartColor.Black, .4), ribbon.Stroke);
        Assert.Equal(2, ribbon.StrokeWidth);
        var pattern = after.Scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role == "chord-ribbon-pattern").ToArray();
        Assert.NotEmpty(pattern); Assert.All(pattern, node => Assert.Equal(ChartColorMath.WithOpacity(authored, .25), node.Stroke));
        Assert.Equal(authored, after.Scene.Nodes.OfType<VisualScenePath>().First(node => node.Role == "chord-target-cue").Fill);
        var key = PreparedChordTests.Role(after, "chord-color-scale").Single();
        Assert.Equal("-10", (string?)key.Attribute("data-cfx-min-value")); Assert.Equal("30", (string?)key.Attribute("data-cfx-max-value"));
        Assert.Equal("30", (string?)Link(after, "self").Attribute("data-cfx-color-value"));
        Assert.Equal("Danger", (string?)Link(after, "forward").Attribute("data-cfx-state"));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("fixed-missing")]
    [InlineData("constant")]
    public void MissingAndConstantColorKeysDoNotInferFromTransferWeights(string mode) {
        var chart = Model(Enumerable.Repeat<double?>(mode == "constant" ? 7 : null, 5).ToArray());
        chart.Options.Chord.ColorDimension.LegendStyle = ChartColorLegendStyle.Gradient;
        if (mode == "fixed-missing") chart.Options.Chord.ColorScale = ChartColorScale.Sequential(Low, High).WithValueRange(0, 10);
        var prepared = Prepare(chart, legend: true); var key = PreparedChordTests.Role(prepared, "chord-color-scale").Single();
        Assert.Equal(new[] { "7", "2", "3", "1", "0" }, Links(prepared).Select(link => (string?)link.Attribute("data-cfx-value")));
        if (mode == "missing") {
            Assert.Null(key.Attribute("data-cfx-min-value")); Assert.Null(key.Attribute("data-cfx-max-value"));
            Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role == "chord-color-scale-gradient");
        } else {
            Assert.Equal(mode == "constant" ? "7" : "0", (string?)key.Attribute("data-cfx-min-value"));
            Assert.Equal(mode == "constant" ? "7" : "10", (string?)key.Attribute("data-cfx-max-value"));
            if (mode == "constant") Assert.Equal(Low, prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Single(node => node.Role == "chord-color-scale-gradient").Fill);
            else Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGradient>());
        }
        Assert.All(Links(prepared), link => Assert.Equal(mode == "constant" ? "7" : null, (string?)link.Attribute("data-cfx-color-value")));
        if (mode != "constant") Assert.All(Links(prepared), link => Assert.Equal("true", (string?)link.Attribute("data-cfx-color-missing")));
    }

    internal static Chart Model(double?[] values) {
        var ids = new[] { "forward", "priority", "return", "self", "zero" }; var weights = new[] { 7d, 2, 3, 1, 0 };
        var series = ChartSeries.CreateChord(new ChartSeriesId("color-flows"), "Transfers", Nodes(), ids.Select((id, index) =>
            new ChartFlowLink(id, index == 2 ? "b" : "a", index is 2 or 3 ? "a" : "b", weights[index], values[index])));
        series.ShowInLegend = false;
        return Chart.Create().AddSeries(series).ConfigureChord(options => {
            options.ColorScale = ChartColorScale.Sequential(Low, High); options.ColorDimension.Name = "Delay";
            options.ColorDimension.ValueFormat = ChartValueFormat.Number("0.0 'ms'", CultureInfo.InvariantCulture);
        });
    }
    private static ChartNode[] Nodes() => new[] { new ChartNode("a", "Support"), new ChartNode("b", "Support") };
    private static PreparedVisual Prepare(Chart chart, bool legend = false) => chart.Prepare(new VisualRenderContext(
        new VisualLayoutOptions(new VisualSize(720, 460)), VisualTheme.Graphite(), VisualThemeMode.Light, new VisualFrame(showLegend: legend),
        new FontSpec { Family = "Carlito", FilePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf") }));
    private static XElement[] Links(PreparedVisual prepared) => PreparedChordTests.Role(prepared, "chord-link");
    private static XElement Link(PreparedVisual prepared, string id) => Links(prepared).Single(node => (string?)node.Attribute("data-cfx-target-id") == id);
    private static VisualScenePath[] Ribbons(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "chord-ribbon").ToArray();
}
