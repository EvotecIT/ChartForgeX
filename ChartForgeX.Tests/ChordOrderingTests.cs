using System.Collections;
using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ChordOrderingTests {
    private static VisualRenderContext Context(int width = 800, VisualThemeMode mode = VisualThemeMode.Light) =>
        new(new VisualLayoutOptions(new VisualSize(width, 520)), theme: VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family),
            themeMode: mode, frame: new VisualFrame(showLegend: false));
    private static XElement[] Role(PreparedVisual visual, string role) => PreparedChordTests.Role(visual, role);
    private static double Number(XElement node, string key) => double.Parse(node.Attribute("data-cfx-" + key)!.Value, CultureInfo.InvariantCulture);
    private static string Id(XElement node) => node.Attribute("data-cfx-target-id")!.Value;

    [Theory]
    [InlineData(390, VisualThemeMode.Light)]
    [InlineData(390, VisualThemeMode.Dark)]
    [InlineData(800, VisualThemeMode.Light)]
    [InlineData(800, VisualThemeMode.Dark)]
    public void ExplicitDisplayOrderRetainsSourceTraversalSlotsWeightsColorsAndDash(int width, VisualThemeMode mode) {
        var chart = ChordDepthRecipe.Create(width == 390 ? "compact" : "wide", mode);
        var source = chart.Series[0]; var nodes = source.Nodes.ToArray(); var flows = source.FlowLinks.ToArray();
        var visual = chart.Prepare(Context(width, mode)); var n = Role(visual, "chord-node"); var f = Role(visual, "chord-link");
        Assert.Equal(nodes.Select(item => item.Id), n.Select(Id)); Assert.Equal(flows.Select(item => item.Id), f.Select(Id));
        Assert.Equal(nodes, source.Nodes); Assert.Equal(flows, source.FlowLinks);
        Assert.Equal(new[] { "api", "data", "web", "ops" }, n.Where(item => Number(item, "sweep") > 0).OrderBy(item => Number(item, "start-angle")).Select(Id));
        var byId = f.ToDictionary(Id, StringComparer.Ordinal);
        Assert.True(Number(byId["api-priority"], "source-start-angle") < Number(byId["api-data"], "source-start-angle"));
        Assert.True(Number(byId["api-data"], "target-start-angle") < Number(byId["data-self"], "target-start-angle"));
        Assert.True(Number(byId["data-self"], "target-start-angle") < Number(byId["api-priority"], "target-start-angle"));
        Assert.NotEqual(Number(byId["data-self"], "source-start-angle"), Number(byId["data-self"], "target-start-angle"));
        Assert.Equal("zero", byId["zero"].Attribute("data-cfx-geometry-status")!.Value);
        Assert.Equal(0, Number(n.Single(item => Id(item) == "idle"), "value"));
        foreach (var flow in f) Assert.Equal(Number(flow, "source-sweep"), Number(flow, "target-sweep"));
        foreach (var node in n) Assert.Equal(Number(node, "sweep"), f.Where(flow => (string?)flow.Attribute("data-cfx-source") == Id(node)).Sum(flow => Number(flow, "source-sweep"))
            + f.Where(flow => (string?)flow.Attribute("data-cfx-target") == Id(node)).Sum(flow => Number(flow, "target-sweep")), 11);
        var priority = visual.Scene.Nodes.OfType<VisualScenePath>().Single(item => item.Role == "chord-ribbon" && item.DashOffset == 1.5);
        Assert.Equal(new[] { 5d, 3d }, priority.Dash); Assert.Equal(2, priority.StrokeWidth);
        var baseline = ChordDepthRecipe.Create(width == 390 ? "compact-source" : "source", mode).Prepare(Context(width, mode));
        Assert.Equal(baseline.Scene.Nodes.OfType<VisualSceneSlice>().Where(item => item.Role == "chord-node-mark").Select(item => item.Fill),
            visual.Scene.Nodes.OfType<VisualSceneSlice>().Where(item => item.Role == "chord-node-mark").Select(item => item.Fill));
        Save("chord-order-" + width + "-" + mode, visual);
    }

    [Fact]
    public void OrderingCopiesPermutationsAndPreparedGeometryIgnoresLaterModelAndOptionsEdits() {
        var nodes = new[] { "data", "api", "ops", "web", "idle" };
        var flows = new[] { "zero", "data-self", "ops-web", "web-ops", "api-priority", "api-data" };
        var ordering = new ChartChordOrdering(nodes, flows, flows); nodes[0] = "changed"; flows[0] = "changed";
        Assert.Equal("data", ordering.NodeIds![0]); Assert.Equal("zero", ordering.OutgoingFlowIds![0]);
        Assert.Throws<NotSupportedException>(() => ((IList<string>)ordering.NodeIds!)[0] = "changed");
        var chart = ChordDepthRecipe.Create().ConfigureChord(options => options.Ordering = ordering);
        var visual = chart.Prepare(Context()); var svg = visual.ToSvg("detached"); var png = visual.ToPng(new VisualRenderOptions(supersampling: 1));
        chart.ConfigureChord(options => { options.Ordering = null; options.RibbonTension = 1; });
        chart.Series[0].ReplaceFlowData(Array.Empty<ChartNode>(), Array.Empty<ChartFlowLink>());
        Assert.Equal(svg, visual.ToSvg("detached")); Assert.Equal(png, visual.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("blank")]
    [InlineData("unknown-node")]
    [InlineData("missing-isolated")]
    [InlineData("unknown-flow")]
    [InlineData("missing-zero")]
    public void InvalidCompletePermutationsFailWithoutRewritingCapturedSource(string invalid) {
        if (invalid == "duplicate") { Assert.Throws<ArgumentException>(() => new ChartChordOrdering(new[] { "api", "api" })); return; }
        if (invalid == "blank") { Assert.Throws<ArgumentException>(() => new ChartChordOrdering(outgoingFlowIds: new[] { " " })); return; }
        var chart = ChordDepthRecipe.Create(); var source = chart.Series[0]; var nodes = source.Nodes.ToArray(); var flows = source.FlowLinks.ToArray();
        chart.Options.Chord.Ordering = invalid switch {
            "unknown-node" => new ChartChordOrdering(new[] { "api", "web", "ops", "data", "ghost" }),
            "missing-isolated" => new ChartChordOrdering(new[] { "api", "web", "ops", "data" }),
            "unknown-flow" => new ChartChordOrdering(outgoingFlowIds: flows.Select(flow => flow.Id == "zero" ? "ghost" : flow.Id)),
            _ => new ChartChordOrdering(incomingFlowIds: flows.Where(flow => flow.Id != "zero").Select(flow => flow.Id))
        };
        var error = Assert.Throws<InvalidOperationException>(() => chart.Prepare(Context()));
        Assert.Contains(invalid.StartsWith("unknown", StringComparison.Ordinal) ? "unknown ID" : "missing captured ID", error.Message);
        Assert.Equal(nodes, source.Nodes); Assert.Equal(flows, source.FlowLinks);
    }

    [Fact]
    public void EmptyAndAllZeroInputsStillValidateOrdersAndRefillUsesAnExplicitNewPermutation() {
        var raw = Chart.Create().AddSeries(new ChartSeries(new ChartSeriesId("raw-empty"), "Raw empty", ChartSeriesKind.Chord, Array.Empty<ChartPoint>()));
        raw.Options.Chord.Ordering = new ChartChordOrdering(new[] { "ghost" });
        Assert.Throws<InvalidOperationException>(() => raw.Prepare(Context()));
        raw.Options.Chord.Ordering = null;
        Assert.Contains(raw.Prepare(Context()).Diagnostics, item => item.Code == "chord.no-positive-flow");
        var chart = Chart.Create().AddChord("Empty", Array.Empty<ChartNode>(), Array.Empty<ChartFlowLink>())
            .ConfigureChord(options => options.Ordering = new ChartChordOrdering(Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>()));
        Assert.Contains(chart.Prepare(Context()).Diagnostics, item => item.Code == "chord.no-positive-flow");
        var source = chart.Series[0]; source.ReplaceFlowData(new[] { new ChartNode("a", "A"), new ChartNode("b", "B") }, new[] { new ChartFlowLink("zero", "a", "b", 0) });
        Assert.Throws<InvalidOperationException>(() => chart.Prepare(Context()));
        chart.ConfigureChord(options => options.Ordering = new ChartChordOrdering(new[] { "b", "a" }, new[] { "zero" }, new[] { "zero" }));
        var zero = chart.Prepare(Context()); Assert.Equal(2, Role(zero, "chord-node").Length); Assert.Single(Role(zero, "chord-link"));
        Assert.DoesNotContain(zero.Scene.Nodes, item => item.Role == "chord-ribbon");
        source.ReplaceFlowData(source.Nodes, new[] { new ChartFlowLink("zero", "a", "b", 4) });
        Assert.Single(chart.Prepare(Context()).Scene.Nodes, item => item.Role == "chord-ribbon");
        source.ReplaceFlowData(Array.Empty<ChartNode>(), Array.Empty<ChartFlowLink>());
        Assert.Throws<InvalidOperationException>(() => chart.Prepare(Context()));
        chart.Options.Chord.Ordering = null;
        Assert.Contains(chart.Prepare(Context()).Diagnostics, item => item.Code == "chord.no-positive-flow");
    }

    internal static void Save(string name, PreparedVisual visual) {
        var directory = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Captures need an absolute task-owned path.");
        Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, name + ".svg"), visual.ToSvg(name));
        File.WriteAllBytes(Path.Combine(directory, name + ".png"), visual.ToPng(new VisualRenderOptions(supersampling: 1)));
    }
}
