using ChartForgeX.Markup;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyShapeContentTests {
    [Theory]
    [InlineData(TopologyNodeShape.Diamond)]
    [InlineData(TopologyNodeShape.Ellipse)]
    [InlineData(TopologyNodeShape.DoubleCircle)]
    [InlineData(TopologyNodeShape.Cylinder)]
    [InlineData(TopologyNodeShape.Cloud)]
    [InlineData(TopologyNodeShape.Parallelogram)]
    [InlineData(TopologyNodeShape.TrapezoidAlt)]
    [InlineData(TopologyNodeShape.Stadium)]
    public void RichContentGrowsBeforeRoutingAndRemainsInsideItsNativeShape(TopologyNodeShape shape) {
        var chart = TopologyChart.Create().WithViewport(800, 500, 20);
        chart.Nodes.Add(new TopologyNode { Id = "review", Label = "Approved?", Subtitle = "Human gate", Badge = "?",
            X = 40, Y = 40, Width = 130, Height = 68, Shape = shape, Status = TopologyHealthStatus.Warning,
            FitLabelToContent = true, PreserveDisplayModeSize = true });
        chart.Nodes[0].Details.Add(new TopologyNodeDetail { Label = "Owner", Value = "Platform", Color = "var(--detail, #2A4470)", Status = TopologyHealthStatus.Warning });
        var prepared = chart.Prepare(Context(), new TopologyRenderOptions { IncludeLegend = false, WrapNodeLabels = true });
        Assert.Contains(Text(prepared, "topology-node-label"), value => value == "Approved?");
        Assert.Contains("Human gate", Text(prepared, "topology-node-subtitle"));
        Assert.Contains("Owner Platform", Text(prepared, "topology-node-detail"));
        Assert.Contains("?", Text(prepared, "topology-node-badge"));
        Assert.Single(prepared.Scene.Nodes, node => node.Role == "topology-status");
        var svg = XDocument.Parse(prepared.ToSvg());
        foreach (var role in new[] { "topology-node-detail", "topology-detail-status" }) {
            var mark = Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == role);
            Assert.Contains(mark.DescendantsAndSelf().Attributes("fill"), attribute => attribute.Value.Contains("var(--detail,"));
        }
        AssertEnclosed(prepared);
        Assert.DoesNotContain(prepared.Diagnostics, item => item.Code.StartsWith("topology.label-", StringComparison.Ordinal)
            || item.Code == "topology.shape-content-overflow");
        Assert.Equal(130, chart.Nodes[0].Width); Assert.Equal(68, chart.Nodes[0].Height);
        ScatterBubbleColorRenderingTests.Capture(prepared, "rich-shape-content-" + shape);
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void DefaultMarkupApprovalRetainsItsQuestionSubtitleBadgeAndStatus(VisualThemeMode mode) {
        var result = new MarkupFlowParser().Parse("decision approve \"Approved?\" subtitle:\"Human gate\" badge:? status:warning");
        Assert.False(result.HasErrors);
        var flow = result.Document!;
        var source = Assert.Single(flow.Steps);
        Assert.True(source.FitLabelToContent);
        var prepared = flow.Prepare(Context(mode));
        Assert.Contains("Approved?", Text(prepared, "topology-node-label"));
        Assert.Contains("Human gate", Text(prepared, "topology-node-subtitle"));
        Assert.Contains("?", Text(prepared, "topology-node-badge"));
        AssertEnclosed(prepared);
        Assert.DoesNotContain(prepared.Diagnostics, item => item.Code == "topology.shape-content-overflow");
        var snapshot = Assert.Single(prepared.SemanticInterchange!.Nodes);
        Assert.True(snapshot.Width > source.Width || snapshot.Height > source.Height);
        Assert.True(snapshot.Flow!.FitLabelToContent);
        Assert.Equal(130, source.Width); Assert.Equal(68, source.Height);
        ScatterBubbleColorRenderingTests.Capture(prepared, "markup-approval-contained-" + mode);
    }

    [Fact]
    public void FixedFlowSizingSurvivesConfigurationMarkupAndInterchangeWithoutPaintingOutside() {
        var result = new MarkupFlowParser().Parse("steps:\n| id | label | kind | subtitle | badge | width | height | fitLabelToContent |\n| fixed | Approved? | decision | Human gate | ? | 130 | 68 | false |");
        Assert.False(result.HasErrors);
        var flow = result.Document!;
        flow.ConfigureStep("fixed", step => step.Color = "#355AAA");
        Assert.False(Assert.Single(flow.Steps).FitLabelToContent);
        var prepared = flow.Prepare(Context());
        var semantic = Assert.Single(prepared.SemanticInterchange!.Nodes);
        var projected = Assert.Single(prepared.Regions, region => region.Role == "topology-node").Bounds;
        Assert.Equal(projected.X, semantic.X); Assert.Equal(projected.Y, semantic.Y);
        Assert.Equal(projected.Width, semantic.Width); Assert.Equal(projected.Height, semantic.Height);
        Assert.False(semantic.Flow!.FitLabelToContent);
        Assert.Contains(prepared.Diagnostics, item => item.Code == "topology.shape-content-overflow");
        AssertEnclosed(prepared);
        var roundTrip = VisualArtifactInterchangeEnvelope.FromJson(prepared.ToArtifact("fixed", VisualArtifactKind.Flow).ToInterchangeJson());
        Assert.False(Assert.Single(roundTrip.Nodes).Flow!.FitLabelToContent);
        Assert.Equal(projected.X, roundTrip.Nodes[0].X); Assert.Equal(projected.Y, roundTrip.Nodes[0].Y);
        Assert.Equal(projected.Width, roundTrip.Nodes[0].Width); Assert.Equal(projected.Height, roundTrip.Nodes[0].Height);
        Assert.Equal(130, flow.Steps[0].Width); Assert.Equal(68, flow.Steps[0].Height);
        ScatterBubbleColorRenderingTests.Capture(prepared, "markup-approval-fixed-contained");
    }

    [Fact]
    public void AuthoredGlyphStripStaysBesideCompleteRichShapeText() {
        var chart = TopologyChart.Create().WithViewport(800, 500, 20);
        chart.Nodes.Add(new TopologyNode { Id = "service", Label = "Service", Subtitle = "Owner", Symbol = "API", Badge = "OK",
            X = 40, Y = 40, Width = 130, Height = 68, Shape = TopologyNodeShape.Diamond,
            FitLabelToContent = true, PreserveDisplayModeSize = true, ShowStatusBadge = false });
        var prepared = chart.Prepare(Context(), new TopologyRenderOptions { IncludeLegend = false });
        var glyph = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), item => item.Role == "topology-node-icon");
        var label = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), item => item.Role == "topology-node-label");
        Assert.Equal("API", glyph.Text.Lines.Single().Text);
        Assert.True(glyph.X < label.X);
        AssertEnclosed(prepared);
    }

    [Fact]
    public void SingleLinePolicyFitsJoinedTitleSubtitleAndDetailsBeforeRouting() {
        var chart = TopologyChart.Create().WithViewport(800, 500, 20);
        chart.Nodes.Add(new TopologyNode { Id = "review", Label = "Review\napproval", Subtitle = "Human\nreview",
            X = 20, Y = 20, Width = 60, Height = 40, Shape = TopologyNodeShape.Diamond,
            FitLabelToContent = true, PreserveDisplayModeSize = true, ShowStatusBadge = true });
        chart.Nodes[0].Details.Add(new TopologyNodeDetail { Text = "Owner\nPlatform" });
        var prepared = chart.Prepare(Context(), new TopologyRenderOptions { IncludeLegend = false,
            AllowMultilineNodeLabels = false, WrapNodeLabels = false, MaxNodeLabelLines = 1, MaxNodeSubtitleLines = 1 });
        Assert.Contains("Review approval", Text(prepared, "topology-node-label"));
        Assert.Contains("Human review", Text(prepared, "topology-node-subtitle"));
        Assert.Contains("Owner Platform", Text(prepared, "topology-node-detail"));
        Assert.DoesNotContain(prepared.Diagnostics, item => item.Code.StartsWith("topology.label-", StringComparison.Ordinal)
            || item.Code == "topology.shape-content-overflow");
        AssertEnclosed(prepared);
        ScatterBubbleColorRenderingTests.Capture(prepared, "rich-shape-single-line-policy");
    }

    [Fact]
    public void FixedEnclosedBadgeDoesNotReserveItsLegacyExternalFootprint() {
        PreparedVisual Prepare(string badge) {
            var chart = TopologyChart.Create().WithViewport(200, 180, 20);
            chart.Nodes.Add(new TopologyNode { Id = "fixed", Label = "A", Badge = badge, X = 20, Y = 20,
                Width = 32, Height = 40, Shape = TopologyNodeShape.Diamond, PreserveDisplayModeSize = true, ShowStatusBadge = false });
            return chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(200, 180), 20),
                frame: new VisualFrame(showLegend: false), font: Context().Font),
                new TopologyRenderOptions { FitContentToViewport = true, IncludeLegend = false });
        }
        var shortBadge = Prepare("?"); var longBadge = Prepare("LongBadge");
        Assert.Equal(shortBadge.Regions.Single(region => region.Role == "topology-node").Bounds,
            longBadge.Regions.Single(region => region.Role == "topology-node").Bounds);
        AssertEnclosed(longBadge);
    }

    private static IEnumerable<string> Text(PreparedVisual visual, string role) => visual.Scene.Nodes.OfType<VisualSceneText>()
        .Where(item => item.Role == role).SelectMany(item => item.Text.Lines).Select(line => line.Text);

    private static void AssertEnclosed(PreparedVisual visual) {
        var surface = Assert.Single(visual.Scene.Nodes.OfType<VisualScenePath>(), item => item.Role == "topology-node-surface" && item.Fill.HasValue);
        var outline = new LabelMarkShape(VisualSceneGeometry.Flatten(surface, 16), true, 0);
        foreach (var text in visual.Scene.Nodes.OfType<VisualSceneText>()) foreach (var line in text.Text.Lines) {
            var bounds = new ChartRect(text.LineLeft(line), text.Baseline - text.Text.Ascent, line.Width, text.Text.Metrics.Height);
            Assert.True(outline.Contains(bounds), text.Role + " must stay within the actual node surface.");
        }
        foreach (var badge in visual.Scene.Nodes.OfType<VisualSceneRectangle>().Where(item => item.Role == "topology-node-badge-surface"))
            Assert.True(outline.Contains(badge.Bounds), "The badge surface must clear the native node outline.");
        foreach (var status in visual.Scene.Nodes.OfType<VisualSceneEllipse>().Where(item => item.Role == "topology-status"))
            Assert.True(outline.Contains(new ChartRect(status.Cx - status.Rx, status.Cy - status.Ry, status.Rx * 2, status.Ry * 2)));
    }

    private static VisualRenderContext Context(VisualThemeMode mode = VisualThemeMode.Light) => new(
        new VisualLayoutOptions(new VisualSize(800, 500), 20), themeMode: mode, frame: new VisualFrame(showLegend: false),
        font: new FontSpec { Family = "Carlito", FilePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf") });
}
