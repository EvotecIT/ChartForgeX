using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyC4SharedPolicyReviewCausalTests {
    [Theory]
    [InlineData("operator", "cloud")]
    [InlineData("a", "east")]
    [InlineData("b", "d")]
    public void BoundedNestedGridRoutesClearUnrelatedNativeCardInteriors(string sourceId, string targetId) {
        var source = File.ReadAllText(Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", "c4-layout-config-native.mmd"));
        var chart = MermaidC4ConfigurationDepthTests.Parse(source).ToTopologyChart(
            MermaidC4ConfigurationDepthTests.Options(false, 390).C4);
        if (sourceId == "b") chart.AddEdge("review-node-route", sourceId, targetId, "Copies");
        var prepared = chart.Prepare();
        var facts = prepared.Visual.CapturedSemanticSnapshot!;
        Capture(prepared, "c4-review-bounded-route-" + sourceId + "-" + targetId);
        var edge = Assert.Single(facts.Edges, item => item.SourceId == sourceId && item.TargetId == targetId);
        Assert.True(edge.ResolvedRoute.Count >= 2);
        foreach (var node in facts.Nodes.Where(node => node.Id != sourceId && node.Id != targetId))
            Assert.False(TopologyAlignmentReviewCausalTests.CrossesBody(edge, node),
                sourceId + "-to-" + targetId + " route crosses unrelated " + node.Id + " body.");
        Assert.Equal(TopologyEdgeRouting.Orthogonal, chart.Edges.Single(item => item.SourceNodeId == sourceId && item.TargetNodeId == targetId).Routing);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(18)]
    public void ShortIconCaptionHonorsAuthoredPreferredSizeBeforeNativePreparation(double fontSize) {
        var chart = IconChart("A");
        chart.Nodes[0].TextStyle = new TextStyleOverride { FontSize = fontSize };
        var prepared = chart.Prepare(IconOptions());
        Capture(prepared, "c4-review-icon-size-" + fontSize);
        var caption = Assert.Single(prepared.Visual.Scene.Nodes.OfType<VisualSceneText>(), item => item.Role == "topology-node-label");
        Assert.Equal("A", string.Concat(caption.Text.Lines.Select(line => line.Text)));
        // Per-node typography is a base size; the established Icon role keeps its 10.5/11 ratio.
        Assert.Equal(fontSize * (10.5 / 11), caption.Text.Size, 6);
        var plate = Assert.Single(prepared.Visual.Scene.Nodes.OfType<VisualSceneRectangle>(), item => item.Role == "topology-node-icon-label");
        Assert.True(plate.Bounds.Width >= caption.Text.Metrics.Width + 12 - .000001);
        Assert.True(plate.Bounds.Height >= caption.Text.Metrics.LineHeight - .000001);
        Assert.Equal(fontSize, chart.Nodes[0].TextStyle!.FontSize);
    }

    [Fact]
    public void IconFamilyOverrideUsesThePaintedNativeFaceForItsPlateAndFootprint() {
        var family = BrowserProofFonts.Family;
        var chart = IconChart("Wide WWW").WithTheme(VisualTheme.Graphite().WithFontFamily("monospace"));
        chart.Nodes[0].TextStyle = new TextStyleOverride { FontFamily = family };
        var prepared = chart.Prepare(IconOptions());
        Capture(prepared, "c4-review-icon-family");
        var caption = Assert.Single(prepared.Visual.Scene.Nodes.OfType<VisualSceneText>(), item => item.Role == "topology-node-label");
        var plate = Assert.Single(prepared.Visual.Scene.Nodes.OfType<VisualSceneRectangle>(), item => item.Role == "topology-node-icon-label");
        Assert.Equal(family, caption.Text.Style.Font.Family);
        Assert.Equal("Wide WWW", string.Concat(caption.Text.Lines.Select(line => line.Text)));
        Assert.Equal(Math.Max(34, caption.Text.Metrics.Width + 12), plate.Bounds.Width, 6);
        Assert.Equal(Math.Max(15, caption.Text.Metrics.LineHeight), plate.Bounds.Height, 6);
        Assert.True(plate.Bounds.Right <= prepared.Width);
        Assert.True(plate.Bounds.Bottom <= prepared.Height);
        var saved = prepared.Visual.CapturedSemanticSnapshot!;
        Assert.Equal(family, Assert.Single(saved.Nodes).Topology!.TextStyle!.FontFamily);
        chart.Nodes[0].TextStyle!.FontFamily = "Changed after preparation";
        Assert.Equal(family, caption.Text.Style.Font.Family);
    }

    [Fact]
    public void EmptyIconTextOverrideRetainsTheExistingNoOverrideNativeBytes() {
        var chart = IconChart("Ordinary API");
        var ordinary = chart.Prepare(IconOptions());
        chart.Nodes[0].TextStyle = new TextStyleOverride();
        var empty = chart.Prepare(IconOptions());
        var caption = Assert.Single(ordinary.Visual.Scene.Nodes.OfType<VisualSceneText>(), item => item.Role == "topology-node-label");
        Assert.Equal(VisualTheme.Graphite().Typography.DataLabelSize * (10.5 / 11), caption.Text.Size, 6);
        Assert.Equal(ordinary.ToSvg(), empty.ToSvg());
        Assert.Equal(ordinary.ToPng(), empty.ToPng());
        Assert.Equal(ordinary.Visual.CapturedSemanticSnapshot!.Nodes.Select(node => (node.Id, node.X, node.Y, node.Width, node.Height)),
            empty.Visual.CapturedSemanticSnapshot!.Nodes.Select(node => (node.Id, node.X, node.Y, node.Width, node.Height)));
        Capture(ordinary, "c4-review-icon-default-control");
    }

    private static TopologyChart IconChart(string label) {
        var chart = TopologyChart.Create().WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family))
            .WithViewport(600, 300, 28).WithLayout(TopologyLayoutMode.GroupGrid)
            .AddAutoNode("icon", label, width: 80, height: 70);
        chart.Nodes[0].DisplayMode = TopologyNodeDisplayMode.Icon;
        chart.Nodes[0].PreserveDisplayModeSize = true;
        chart.Nodes[0].ShowStatusBadge = false;
        return chart;
    }

    private static TopologyRenderOptions IconOptions() => new() {
        IncludeTitle = false, IncludeLegend = false, IncludeIconLabels = true,
        TextMeasurementMode = TextMeasurementMode.InstalledFonts, FitContentToViewport = false
    };

    private static void Capture(PreparedTopology prepared, string name) {
        var png = prepared.ToPng();
        var image = RasterImageDecoder.Decode(png);
        Assert.Equal((int)Math.Ceiling(prepared.Width), image.Width);
        Assert.Equal((int)Math.Ceiling(prepared.Height), image.Height);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } directory) return;
        if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Capture directory must be absolute.");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, name);
        File.WriteAllText(path + ".svg", prepared.ToSvg());
        File.WriteAllBytes(path + ".native.png", png);
        // This capture records native geometry; wire admission has a separate bounded-font contract.
        File.WriteAllText(path + ".json", System.Text.Json.JsonSerializer.Serialize(prepared.Analyze()));
        TopologyPortableFontObservation.Capture(name, prepared.Visual);
    }
}
