using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class PreparedChordTests {
    internal static VisualRenderContext Context(int width = 720, int height = 460, VisualThemeMode mode = VisualThemeMode.Light) =>
        new(new VisualLayoutOptions(new VisualSize(width, height)), theme: VisualTheme.Graphite(), themeMode: mode, frame: new VisualFrame(showLegend: false));
    internal static XElement[] Role(PreparedVisual visual, string role) => XDocument.Parse(visual.ToSvg()).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == role).ToArray();
    private static double Number(XElement node, string name) => double.Parse(node.Attribute("data-cfx-" + name)!.Value, CultureInfo.InvariantCulture);
    private static Chart Independent(double first, double second) => Chart.Create().AddChord("Weights", new[] {
        new ChartNode("a", "Support"), new ChartNode("b", "Support"), new ChartNode("c", "Other"), new ChartNode("d", "Other")
    }, new[] { new ChartFlowLink("ab", "a", "b", first), new ChartFlowLink("cd", "c", "d", second) });

    [Theory]
    [InlineData(double.Epsilon, double.Epsilon * 2)]
    [InlineData(1e-310, 2e-310)]
    [InlineData(5e-8, 5e-7)]
    [InlineData(1e308, 1e308)]
    public void ChordNormalizesBeforeGlobalSummationAndPreservesFiniteWeightProportions(double first, double second) {
        var prepared = Independent(first, second).Prepare(Context());
        var links = Role(prepared, "chord-link"); var nodes = Role(prepared, "chord-node");
        Assert.Equal(new[] { first, second }, links.Select(link => Number(link, "value")));
        Assert.Equal(second / first, Number(links[1], "source-sweep") / Number(links[0], "source-sweep"), 10);
        Assert.Equal(new[] { first, first, second, second }, nodes.Select(node => Number(node, "value")));
        Assert.Equal((360 - 4 * 3) * Math.PI / 180, nodes.Sum(node => Number(node, "sweep")), 12);
        foreach (var link in links) Assert.Equal(Number(link, "source-sweep"), Number(link, "target-sweep"));
        Assert.Equal(2, prepared.Scene.Nodes.OfType<VisualScenePath>().Count(node => node.Role == "chord-ribbon"));
        Assert.Equal(4, prepared.Scene.Nodes.OfType<VisualSceneSlice>().Count(node => node.Role == "chord-node-mark"));
        Assert.DoesNotContain(prepared.Diagnostics, item => item.Code == "chord.precision-collapse");
        Assert.All(prepared.Regions, region => { Assert.True(double.IsFinite(region.Bounds.X)); Assert.True(double.IsFinite(region.Bounds.Width)); });
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Fact]
    public void ReciprocalParallelAndSelfRibbonsKeepDistinctSlotsIdsAndActualDirection() {
        var chart = Chart.Create().AddChord("Transfers", new[] { new ChartNode("a", "Support"), new ChartNode("b", "Support") }, new[] {
            new ChartFlowLink("forward", "a", "b", 7), new ChartFlowLink("reverse", "b", "a", 3),
            new ChartFlowLink("parallel", "a", "b", 2), new ChartFlowLink("self", "a", "a", 1)
        });
        var prepared = chart.Prepare(Context()); var links = Role(prepared, "chord-link"); var nodes = Role(prepared, "chord-node");
        Assert.Equal(new[] { "forward", "reverse", "parallel", "self" }, links.Select(link => (string?)link.Attribute("data-cfx-target-id")));
        Assert.Equal(new[] { "a", "b", "a", "a" }, links.Select(link => (string?)link.Attribute("data-cfx-source")));
        Assert.Equal(new[] { "b", "a", "b", "a" }, links.Select(link => (string?)link.Attribute("data-cfx-target")));
        Assert.Equal(new[] { 7d, 3, 2, 1 }, links.Select(link => Number(link, "value")));
        Assert.All(links, link => { Assert.Equal("polar", (string?)link.Attribute("data-cfx-coordinate-system")); Assert.Null(link.Attribute("data-cfx-point")); });
        Assert.NotEqual(Number(links[0], "source-start-angle"), Number(links[2], "source-start-angle"));
        Assert.NotEqual(Number(links[3], "source-start-angle"), Number(links[3], "target-start-angle"));
        Assert.Equal(4, Number(nodes[0], "incoming")); Assert.Equal(10, Number(nodes[0], "outgoing")); Assert.Equal(14, Number(nodes[0], "value"));
        Assert.Equal(Number(nodes[0], "sweep"), links.Where(link => (string?)link.Attribute("data-cfx-source") == "a").Sum(link => Number(link, "source-sweep"))
            + links.Where(link => (string?)link.Attribute("data-cfx-target") == "a").Sum(link => Number(link, "target-sweep")), 12);
        Assert.Equal(4, prepared.Scene.Nodes.Count(node => node.Role == "chord-target-cue"));
    }

    [Fact]
    public void ZeroAndPrecisionCollapsedFactsRemainSemanticWithoutMinimumGeometry() {
        var zero = Chart.Create().AddChord("Zero", new[] { new ChartNode("a", "A"), new ChartNode("b", "B") }, new[] { new ChartFlowLink("zero", "a", "b", 0) }).Prepare(Context());
        Assert.Single(Role(zero, "chord-link")); Assert.Equal(2, Role(zero, "chord-node").Length);
        Assert.DoesNotContain(zero.Scene.Nodes, node => node.Role == "chord-ribbon" || node.Role == "chord-node-mark");
        Assert.Contains(zero.Diagnostics, item => item.Code == "chord.no-positive-flow");
        Assert.All(zero.Regions, region => { Assert.Equal(0, region.Bounds.Width); Assert.Equal(0, region.Bounds.Height); });
        var empty = Chart.Create().AddChord("Empty", Array.Empty<ChartNode>(), Array.Empty<ChartFlowLink>()).Prepare(Context());
        Assert.Contains(empty.Diagnostics, item => item.Code == "chord.no-positive-flow");
        var tiny = Independent(double.Epsilon, 1e308).Prepare(Context());
        var collapsed = Role(tiny, "chord-link")[0];
        Assert.Equal(double.Epsilon, Number(collapsed, "value")); Assert.Equal("precision-collapse", (string?)collapsed.Attribute("data-cfx-geometry-status"));
        Assert.Contains(tiny.Diagnostics, item => item.Code == "chord.precision-collapse");
        Assert.Single(tiny.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "chord-ribbon");
    }

    [Theory]
    [InlineData(320, 280, VisualThemeMode.Light)]
    [InlineData(320, 280, VisualThemeMode.Dark)]
    [InlineData(720, 460, VisualThemeMode.Light)]
    [InlineData(720, 460, VisualThemeMode.Dark)]
    public void ConfiguredChordUsesOneBoundedNativeSceneForSvgAndRaster(int width, int height, VisualThemeMode mode) {
        var chart = V2GalleryModels.Create(ChartSeriesKind.Chord, "options"); var prepared = chart.Prepare(Context(width, height, mode));
        var nodes = Role(prepared, "chord-node"); var links = Role(prepared, "chord-link");
        Assert.Equal(4, nodes.Length); Assert.Equal(9, links.Length);
        Assert.Equal((300 - 4 * 5) * Math.PI / 180, nodes.Sum(node => Number(node, "sweep")), 11);
        Assert.Equal(-120 * Math.PI / 180, Number(nodes[0], "start-angle"), 12);
        Assert.Equal(.09, 1 - Number(nodes[0], "inner-radius") / Number(nodes[0], "outer-radius"), 12);
        Assert.All(prepared.Regions, region => {
            Assert.InRange(region.Bounds.Left, 24 - .1, width - 24 + .1); Assert.InRange(region.Bounds.Right, 24 - .1, width - 24 + .1);
            Assert.InRange(region.Bounds.Top, 24 - .1, height - 24 + .1); Assert.InRange(region.Bounds.Bottom, 24 - .1, height - 24 + .1);
        });
        var pixels = prepared.ToRgba(new VisualRenderOptions(supersampling: 1)); Assert.Equal(width, pixels.Width); Assert.Equal(height, pixels.Height);
        Assert.Equal(8, prepared.Scene.Nodes.Count(node => node.Role == "chord-ribbon"));
        Assert.DoesNotContain("<script", prepared.ToSvg(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ChordOptionsAndStylingAreAppliedBeforeThePreparedSnapshotIsDetached() {
        var chart = Independent(3, 7).ConfigureChord(options => { options.LabelContent = ChartChordLabelContent.None; options.DirectionCue = ChartChordDirectionCue.None; options.RibbonOpacity = .2; });
        var color = ChartColor.FromHex("#243B53");
        chart.Series[0].WithNodeState("a", ChartSeriesState.Warning).WithPointColor(2, color).WithPointFillPattern(2, ChartFillPattern.Crosshatch);
        var context = Context(); var prepared = chart.Prepare(context);
        var fills = prepared.Scene.Nodes.OfType<VisualSceneSlice>().Where(node => node.Role == "chord-node-mark").Select(node => node.Fill).ToArray();
        Assert.Equal(context.Theme.Resolve(context.ThemeMode).Status.Medium.Fill, fills[0]); Assert.Equal(color, fills[2]);
        Assert.Contains(prepared.Scene.Nodes, node => node.Role == "chord-node-pattern");
        Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role == "chord-target-cue" || node.Role == "chord-node-label");
        var ribbon = prepared.Scene.Nodes.OfType<VisualScenePath>().First(node => node.Role == "chord-ribbon");
        Assert.Equal(ChartColorMath.WithOpacity(fills[0]!.Value, .2), ribbon.Fill);
        var svg = prepared.ToSvg("stable"); var png = prepared.ToPng(new VisualRenderOptions(supersampling: 1));
        chart.Options.Chord.StartAngleDegrees = 30; chart.Options.Chord.RibbonOpacity = .9;
        chart.Series[0].WithNodeState("a", ChartSeriesState.Danger); chart.Series[0].Color = ChartColor.Black;
        chart.Options.ValueFormatter = _ => "changed"; chart.Series[0].DataLabelStyle.FontSize = 30;
        Assert.Equal(svg, prepared.ToSvg("stable")); Assert.Equal(png, prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Fact]
    public void SvgSerializationEscapesAuthoredIdentityAndPreservesRawWeights() {
        var chart = Chart.Create().AddChord("Flows <&>", new[] { new ChartNode("a<&>", "Support <&>"), new ChartNode("b\"'", "Support <&>") },
            new[] { new ChartFlowLink("link<&>\"", "a<&>", "b\"'", 3.125) });
        var prepared = chart.Prepare(Context()); var link = Assert.Single(Role(prepared, "chord-link"));
        Assert.Equal("link<&>\"", (string?)link.Attribute("data-cfx-target-id"));
        Assert.Equal("a<&>", (string?)link.Attribute("data-cfx-source")); Assert.Equal("b\"'", (string?)link.Attribute("data-cfx-target"));
        Assert.Equal("Support <&>", (string?)link.Attribute("data-cfx-source-label")); Assert.Equal(3.125, Number(link, "value"));
        Assert.Null(link.Attribute("data-cfx-source-point")); Assert.Equal("0", (string?)link.Attribute("data-cfx-source-link-index"));
        Assert.Equal(prepared.ToSvg("fixed"), prepared.ToSvg("fixed"));
    }

    [Fact]
    public void GapsThatConsumeTheCircularSpanFailBeforeReturningAFalseScene() {
        var chart = Independent(1, 1).ConfigureChord(options => { options.SweepAngleDegrees = 20; options.NodeGapDegrees = 5; });
        Assert.Throws<InvalidOperationException>(() => chart.Prepare(Context()));
    }

    [Fact]
    public void DistinctAnglesWithCollapsedNativeEndpointsAndThicknessAreDiagnosed() {
        var chart = Chart.Create().AddChord("Precision", new[] { new ChartNode("a", "Tiny"), new ChartNode("b", "B"), new ChartNode("c", "C") },
            new[] { new ChartFlowLink("tiny-self", "a", "a", 1e-20), new ChartFlowLink("large", "b", "c", 1) }).ConfigureChord(options => options.StartAngleDegrees = 0);
        var prepared = chart.Prepare(Context()); var tiny = Role(prepared, "chord-link")[0];
        Assert.True(Number(tiny, "source-sweep") > 0);
        Assert.Equal("precision-collapse", (string?)tiny.Attribute("data-cfx-geometry-status"));
        Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "chord-ribbon");
        Assert.Contains(prepared.Diagnostics, item => item.Code == "chord.precision-collapse");
        chart.Options.Chord.NodeThicknessRatio = double.Epsilon;
        var thin = chart.Prepare(Context());
        Assert.DoesNotContain(thin.Scene.Nodes, node => node.Role == "chord-node-mark");
        Assert.Contains(thin.Diagnostics, item => item.Code == "chord.precision-collapse");
    }
}
