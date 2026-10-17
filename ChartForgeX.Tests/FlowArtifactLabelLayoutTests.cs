using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class FlowArtifactLabelLayoutTests : IDisposable {
    private const string Family = "CFX Flow Label Carlito";

    public FlowArtifactLabelLayoutTests() {
        var fonts = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito");
        foreach (var face in new[] { "Carlito-Regular.ttf", "Carlito-Bold.ttf" })
            Assert.True(File.Exists(Path.Combine(fonts, face)), "The measured caption fixture font must exist.");
        FontRegistry.Register(Family, Path.Combine(fonts, "Carlito-Regular.ttf"), 400);
        FontRegistry.Register(Family, Path.Combine(fonts, "Carlito-Bold.ttf"), 700);
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void DefaultShortConnectorLabelStaysNearItsOwnRouteAndClearsPaintedCaptions(VisualThemeMode mode) {
        var flow = FlowRecipe.Create();
        // Keep the library-default short run even if the gallery later chooses a larger decision.
        flow.ConfigureStep("decision", step => { step.Width = 130; step.Height = 68; });
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(900, 600)), VisualTheme.Graphite(), mode,
            new VisualFrame("Assessment workflow", "Typed lanes, decisions and branched routes", showLegend: false), FontSpec.FromFamily(Family));
        var prepared = flow.Prepare(context);
        ScatterBubbleColorRenderingTests.Capture(prepared, "flow-default-short-route-" + mode.ToString().ToLowerInvariant());
        var semantics = prepared.SemanticInterchange!;
        var edge = semantics.Edges.Single(item => item.Label == "Evaluate");
        var label = Assert.Single(prepared.Scene.Regions, region => region.Id == edge.Id + "-label").Bounds;
        var glyphs = edge.ResolvedLabelBounds!.Value;
        Assert.True(glyphs.Left >= label.Left && glyphs.Right <= label.Right && glyphs.Top >= label.Top && glyphs.Bottom <= label.Bottom);
        var first = edge.ResolvedRoute[0]; var last = edge.ResolvedRoute[edge.ResolvedRoute.Count - 1];
        var centerX = label.Left + label.Width / 2; var centerY = label.Top + label.Height / 2;
        Assert.All(edge.ResolvedRoute, point => Assert.Equal(first.Y, point.Y, 6));
        Assert.Equal((first.X + last.X) / 2, centerX, 6);
        var endpoints = semantics.Nodes.Where(node => node.Id is "received" or "decision").ToArray();
        Assert.Equal(2, endpoints.Length);
        Assert.InRange(Math.Abs(centerY - first.Y), 0, endpoints.Max(node => node.Height!.Value));
        var lane = semantics.Groups.Single(group => group.Id == "service");
        Assert.True(label.Left >= lane.X && label.Right <= lane.X + lane.Width && label.Top >= lane.Y && label.Bottom <= lane.Y + lane.Height,
            "An automatic caption for a short in-lane route must remain associated with that lane.");
        foreach (var node in semantics.Nodes)
            Assert.False(Overlaps(label, new ChartRect(node.X!.Value, node.Y!.Value, node.Width!.Value, node.Height!.Value)), "The badge covers a task surface.");
        var captions = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role is "topology-group-label" or "topology-group-subtitle").ToArray();
        Assert.Equal(2, captions.Length);
        foreach (var caption in captions) Assert.False(Overlaps(label, PaintedTextBounds(caption)), "The badge covers a measured lane caption.");

        var svg = XDocument.Parse(prepared.ToSvg());
        var group = Assert.Single(svg.Descendants(), node => (string?)node.Attribute("data-cfx-source-id") == edge.Id + "-label");
        Assert.Equal(centerX, Number(group, "data-label-x"), 6);
        Assert.Equal(centerY, Number(group, "data-label-y"), 6);
        Assert.Equal(first.Y, Number(group, "data-label-anchor-y"), 6);
        Assert.Equal("false", (string?)group.Attribute("data-label-anchor-override"));
        Assert.Equal("false", (string?)group.Attribute("data-label-leader"));
        Assert.Contains(group.Descendants(), node => node.Name.LocalName is "text" or "path");
        var text = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "topology-edge-label-text" && node.Text.Lines.Single().Text == "Evaluate");
        var ink = PaintedTextBounds(text);
        Assert.True(ink.Left >= label.Left && ink.Right <= label.Right && ink.Top >= label.Top && ink.Bottom <= label.Bottom);
        var image = PngReader.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        Assert.Equal(900, image.Width); Assert.Equal(600, image.Height);
        Assert.True(HasNativeInk(prepared, image, ink, text), "Native raster must paint Evaluate at the same measured caption bounds as SVG.");
    }

    [Fact]
    public void EndpointCaptionUsesUnpaintedHeaderSpaceWithoutMovingAwayFromItsEndpoint() {
        var chart = TopologyChart.Create().WithViewport(400, 300, 16).WithLegend(null)
            .AddGroup("service", "Service", 32, 60, 300, 180)
            .AddNode("source", "Source", 180, 45, width: 20, height: 20)
            .AddNode("target", "Target", 300, 45, width: 20, height: 20)
            .AddEdge("link", "source", "target", routing: TopologyEdgeRouting.Orthogonal)
            .WithEdgeEndpointLabels("link", "TCP", null);
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(400, 300), 16), VisualTheme.Graphite(),
            frame: new VisualFrame(showLegend: false), font: FontSpec.FromFamily(Family));
        var options = new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false, IncludeDirectionMarkers = false };
        var shown = chart.Prepare(context, options);
        ScatterBubbleColorRenderingTests.Capture(shown, "flow-endpoint-header-space");
        options.IncludeGroupLabels = false;
        var hidden = chart.Prepare(context, options);
        var label = Assert.Single(shown.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "topology-endpoint-label");
        var control = Assert.Single(hidden.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "topology-endpoint-label");
        var route = Assert.Single(shown.SemanticInterchange!.Edges).ResolvedRoute[0];
        var controlRoute = Assert.Single(hidden.SemanticInterchange!.Edges).ResolvedRoute[0];
        Assert.Equal(control.X - controlRoute.X, label.X - route.X, 6);
        Assert.Equal(control.Baseline - controlRoute.Y, label.Baseline - route.Y, 6);
        var svg = XDocument.Parse(shown.ToSvg());
        var surface = Assert.Single(svg.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "topology-endpoint-label-surface");
        var bounds = new ChartRect(Number(surface, "x"), Number(surface, "y"), Number(surface, "width"), Number(surface, "height"));
        foreach (var node in shown.SemanticInterchange.Nodes)
            Assert.False(Overlaps(bounds, new ChartRect(node.X!.Value, node.Y!.Value, node.Width!.Value, node.Height!.Value)), "The endpoint badge covers a task surface.");
        var header = Assert.Single(shown.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "topology-group-label");
        Assert.False(Overlaps(bounds, PaintedTextBounds(header)), "The endpoint badge covers the real lane caption.");
        Assert.Equal("TCP", Assert.Single(label.Text.Lines).Text);
        var endpoint = Assert.Single(svg.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "topology-edge-endpoint-label");
        Assert.Equal(label.X, Number(endpoint, "data-label-x"), 6);
        Assert.Equal(bounds.Top + bounds.Height / 2, Number(endpoint, "data-label-y"), 3);
        Assert.Contains(endpoint.Descendants(), node => node.Name.LocalName is "text" or "path");
        var image = PngReader.Decode(shown.ToPng(new VisualRenderOptions(supersampling: 1)));
        Assert.True(HasNativeInk(shown, image, PaintedTextBounds(label), label), "The endpoint caption must paint at its measured SVG location.");
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void GalleryDecisionDisplaysItsCompleteQuestionAtTheAuthoredExampleSize(VisualThemeMode mode) {
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(900, 600)), VisualTheme.Graphite(), mode,
            new VisualFrame("Assessment workflow", "Typed lanes, decisions and branched routes", showLegend: false), FontSpec.FromFamily(Family));
        var prepared = FlowRecipe.Create().Prepare(context);
        ScatterBubbleColorRenderingTests.Capture(prepared, "flow-complete-decision-" + mode.ToString().ToLowerInvariant());
        var question = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Id == "decision-label").ToArray();
        Assert.NotEmpty(question);
        Assert.Equal("Automatic checks pass?", string.Join(" ", question.SelectMany(node => node.Text.Lines).Select(line => line.Text)));
        var decision = prepared.SemanticInterchange!.Nodes.Single(node => node.Id == "decision");
        var bounds = new ChartRect(decision.X!.Value, decision.Y!.Value, decision.Width!.Value, decision.Height!.Value);
        var image = PngReader.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        foreach (var line in question) {
            var ink = PaintedTextBounds(line);
            Assert.True(ink.Left >= bounds.Left && ink.Right <= bounds.Right && ink.Top >= bounds.Top && ink.Bottom <= bounds.Bottom);
            foreach (var node in prepared.SemanticInterchange.Nodes.Where(node => node.Id != "decision"))
                Assert.False(Overlaps(ink, new ChartRect(node.X!.Value, node.Y!.Value, node.Width!.Value, node.Height!.Value)));
            Assert.True(HasNativeInk(prepared, image, ink, line), "The complete decision question must remain visible in native raster.");
        }
    }

    private static ChartRect PaintedTextBounds(VisualSceneText text) => new(text.LineLeft(text.Text.Lines[0]),
        text.Baseline - text.Text.Ascent, text.Text.Metrics.Width, text.Text.Metrics.Height);

    private static bool Overlaps(ChartRect a, ChartRect b) => a.Left < b.Right && a.Right > b.Left && a.Top < b.Bottom && a.Bottom > b.Top;

    private static double Number(XElement element, string name) => double.Parse(element.Attribute(name)!.Value, CultureInfo.InvariantCulture);

    private static bool HasNativeInk(PreparedVisual prepared, RgbaImage image, ChartRect bounds, VisualSceneText caption) {
        // Keep every paint and coordinate; remove only this text run to isolate its antialiased ink.
        var scene = prepared.Scene;
        var without = VisualSceneRasterRenderer.Render(new VisualScene(scene.Size,
            scene.Nodes.Where(node => !ReferenceEquals(node, caption)).ToArray(), scene.Diagnostics, scene.Regions), supersampling: 1);
        var left = Math.Max(0, (int)Math.Floor(bounds.Left)); var right = Math.Min(image.Width, (int)Math.Ceiling(bounds.Right));
        var top = Math.Max(0, (int)Math.Floor(bounds.Top)); var bottom = Math.Min(image.Height, (int)Math.Ceiling(bounds.Bottom));
        for (var y = top; y < bottom; y++) for (var x = left; x < right; x++) {
            var offset = (y * image.Width + x) * 4;
            for (var channel = 0; channel < 4; channel++)
                if (image.Pixels[offset + channel] != without.Pixels[offset + channel]) return true;
        }
        return false;
    }

    public void Dispose() => FontRegistry.Clear();
}
