using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualBlocks;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidAllocatedTypographyClosureTests {
    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(TopologyNodeDisplayMode.Card, false, TopologyEdgeRouting.Orthogonal)]
    [InlineData(TopologyNodeDisplayMode.Card, true, TopologyEdgeRouting.Orthogonal)]
    [InlineData(TopologyNodeDisplayMode.CompactCard, false, TopologyEdgeRouting.Orthogonal)]
    [InlineData(TopologyNodeDisplayMode.CompactCard, true, TopologyEdgeRouting.Orthogonal)]
    [InlineData(TopologyNodeDisplayMode.Tile, false, TopologyEdgeRouting.Orthogonal)]
    [InlineData(TopologyNodeDisplayMode.Tile, true, TopologyEdgeRouting.Orthogonal)]
    [InlineData(TopologyNodeDisplayMode.Icon, false, TopologyEdgeRouting.Orthogonal)]
    [InlineData(TopologyNodeDisplayMode.Icon, true, TopologyEdgeRouting.Orthogonal)]
    [InlineData(TopologyNodeDisplayMode.Card, false, TopologyEdgeRouting.Straight)]
    [InlineData(TopologyNodeDisplayMode.Card, true, TopologyEdgeRouting.Straight)]
    [InlineData(TopologyNodeDisplayMode.Tile, false, TopologyEdgeRouting.Straight)]
    [InlineData(TopologyNodeDisplayMode.Tile, true, TopologyEdgeRouting.Straight)]
    public async Task AutomaticActorRoutesAvoidThePaintedCaption(TopologyNodeDisplayMode mode, bool incoming, TopologyEdgeRouting routing) {
        var chart = TopologyChart.Create().WithViewport(320, 280, 16)
            .AddNode("actor", "API", 80, 40, width: 64, height: 46)
            .AddNode("target", "Store", 80, 135, width: 64, height: 46)
            .WithNodeDisplay("actor", mode)
            .AddEdge("link", incoming ? "target" : "actor", incoming ? "actor" : "target");
        chart.Nodes[0].Shape = TopologyNodeShape.Actor;
        foreach (var node in chart.Nodes) node.ShowStatusBadge = false;
        chart.Edges[0].Routing = routing;
        var options = new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false, IncludeIconLabels = true };
        var svg = chart.ToSvg(options);
        var xml = XDocument.Parse(svg);
        var label = xml.Descendants().Single(element => element.Name.LocalName == "text" && element.Value == "API");
        var size = (double)label.Attribute("font-size")!;
        var weight = (int)label.Attribute("font-weight")!;
        var builder = new VisualSceneBuilder(new VisualSize(320, 280), FontSpec.FromFamily((string)label.Attribute("font-family")!));
        var metrics = builder.MeasureText(label.Value, size, weight);
        var bounds = new ChartRect((double)label.Attribute("x")!, (double)label.Attribute("y")! - builder.TextAscent(size, weight), metrics.Width, metrics.Height);
        var path = xml.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "topology-edge-line");
        var points = Assert.Single(ChartMapPathParser.ParseSubpaths((string)path.Attribute("d")!, 1)).Points;
        var stroke = new LabelMarkShape(new[] { points.ToList() }, false, (double)path.Attribute("stroke-width")!);
        Assert.False(stroke.Intersects(bounds), "The automatic edge crosses the actor's painted caption.");
        await Capture(svg, chart.ToPng(options), "actor-auto-" + mode + "-" + incoming + "-" + routing, 320, 280);
    }

    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(360, 32, 0, false)]
    [InlineData(360, 32, 90, false)]
    [InlineData(760, 256, 45, true)]
    public async Task AxisCaptionsRetainACompleteMonth(int width, int size, double angle, bool dark) {
        var chart = Chart.Create().WithSize(width, 420).WithLegend(false).WithHeader(false)
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithXLabels("September", "October", "November")
            .ConfigureTickLabelStyle(style => style.WithFontFamily("Arial").WithFontSize(size))
            .AddLine("Readings", new[] { new ChartPoint(1, 10), new ChartPoint(2, 20), new ChartPoint(3, 15) });
        chart.Options.XAxis.LabelAngle = angle;
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var labels = XDocument.Parse(prepared.ToSvg()).Descendants().Where(element => element.Name.LocalName == "text").Select(element => element.Value).ToArray();
        Assert.Contains("September", labels);
        await Capture(prepared.ToSvg(), prepared.ToPng(), "axis-" + size + "-" + angle, width, 420);
    }

    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(TopologyNodeDisplayMode.Tile)]
    [InlineData(TopologyNodeDisplayMode.Icon)]
    [InlineData(TopologyNodeDisplayMode.CompactCard)]
    public async Task CompactActorDisplayModesPaintAndRouteTheSameFigure(TopologyNodeDisplayMode mode) {
        var chart = TopologyChart.Create().WithViewport(360, 240, 16)
            .AddNode("actor", "API", 60, 70, width: 64, height: 46)
            .AddNode("target", "Store", 240, 70, width: 64, height: 46)
            .WithNodeDisplay("actor", mode).WithNodeDisplay("target", TopologyNodeDisplayMode.Tile)
            .AddEdge("link", "actor", "target");
        chart.Nodes[0].Shape = TopologyNodeShape.Actor;
        foreach (var node in chart.Nodes) node.ShowStatusBadge = false;
        chart.Edges[0].SourcePort = TopologyEdgePort.Right;
        var options = new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false, IncludeIconLabels = true };
        var svg = chart.ToSvg(options);
        var xml = XDocument.Parse(svg);
        var surface = xml.Descendants().Single(element => (string?)element.Attribute("data-node-id") == "actor" &&
            (string?)element.Attribute("data-cfx-role") == "topology-node");
        Assert.DoesNotContain(surface.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "topology-icon-surface");
        var armSurface = surface.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "topology-node-surface").ElementAt(2);
        var arm = Assert.Single(ChartMapPathParser.ParseSubpaths((string)armSurface.Attribute("d")!, 1)).Points;
        var route = xml.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "topology-edge-line");
        var points = Assert.Single(ChartMapPathParser.ParseSubpaths((string)route.Attribute("d")!, 1)).Points;
        Assert.Equal(arm[1].Y, points[0].Y, 3);
        await Capture(svg, chart.ToPng(options), "compact-actor-" + mode, 360, 240);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(45)]
    [InlineData(90)]
    public void OversizedAxisFittingUsesTheCompleteFootprint(double angle) {
        var builder = new VisualSceneBuilder(new VisualSize(200, 100), FontSpec.FromFamily("Arial"));
        var style = new TextStyle { Font = FontSpec.FromFamily("Arial"), FontSize = 32 };
        var bounds = new ChartRect(0, 0, 100, 50);
        var fitted = VisualAxisText.FitOversized(builder, style, bounds, new[] { "September" }, angle);
        var metrics = VisualAxisText.RotatedMetrics(builder.MeasureText("September", fitted), angle);
        Assert.True(metrics.Width <= bounds.Width && metrics.Height <= bounds.Height);
        Assert.Equal(32, style.FontSize);
    }

    [Fact]
    public void OrdinaryUnrotatedAxisTextRetainsItsShorteningPolicy() {
        var builder = new VisualSceneBuilder(new VisualSize(200, 100), FontSpec.FromFamily("Arial"));
        var style = new TextStyle { Font = FontSpec.FromFamily("Arial"), FontSize = 18 };
        var fitted = VisualAxisText.FitOversized(builder, style, new ChartRect(0, 0, 30, 50), new[] { "September" });
        Assert.Same(style, fitted);
        Assert.Equal(18, fitted.FontSize);
    }

    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(18)]
    [InlineData(18.0001)]
    [InlineData(32)]
    [InlineData(256)]
    public async Task TopologyGroupHeadersRetainTheirLabelAndSubtitle(double size) {
        var result = MermaidRenderer.Render("%%{init: {'fontFamily':'Carlito','themeVariables':{'fontSize':" + size.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}}}%%\nflowchart LR\nsubgraph Service[Service group]\n A --> B\nend");
        Assert.Empty(result.Diagnostics);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        chart.Groups[0].Subtitle = "Group description";
        var fontPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        var font = FontSpec.FromFile(fontPath);
        font.Family = "Carlito";
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(960, 720)),
            VisualTheme.Graphite().WithTypography(new VisualTypography("Carlito", dataLabelSize: size)),
            frame: new VisualFrame(showLegend: false), font: font);
        var prepared = chart.Prepare(context, new TopologyRenderOptions { FitContentToViewport = true });
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        var xml = XDocument.Parse(svg);
        Assert.Contains(xml.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "topology-group-label" && node.Value == "Service group");
        Assert.Contains(xml.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "topology-group-subtitle" && node.Value == "Group description");
        await Capture(svg, png, "group-header-" + size, 960, 720, fontPath);
    }

    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(32)]
    [InlineData(256)]
    public async Task NarrowPacketSlicesRetainTheirCompleteShortFieldCaption(int size) {
        var result = MermaidRenderer.Render("%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':" + size + "}}}%%\npacket-beta\n0-1: \"ACK\"\n2-31: \"Payload\"");
        Assert.Empty(result.Diagnostics);
        var block = Assert.IsType<PacketLayoutBlock>(result.Artifact!.Model).WithSize(480, 280);
        var svg = block.ToSvg(); var png = block.ToPng();
        var labels = XDocument.Parse(svg).Descendants().Where(node => node.Name.LocalName == "text").Select(node => node.Value).ToArray();
        Assert.Contains("ACK", labels); Assert.Contains("Payload", labels);
        await Capture(svg, png, "packet-narrow-" + size, 480, 280);
    }

    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OrdinaryGroupTitlesKeepTheirWidthPolicyWhenSubtitleNeedsFitting(bool subtitle) {
        const string label = "An ordinary group title that needs shortening";
        var chart = TopologyChart.Create().WithViewport(600, 400, 0).WithLegend(null)
            .AddGroup("group", label, 40, 40, 160, 120, subtitle: subtitle ? "Description" : null);
        var fontPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        var font = FontSpec.FromFile(fontPath); font.Family = "Carlito";
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(600, 400), 0),
            VisualTheme.Graphite().WithTypography(new VisualTypography("Carlito", dataLabelSize: 18)),
            frame: new VisualFrame(showLegend: false), font: font);
        var prepared = chart.Prepare(context);
        var title = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "topology-group-label");
        Assert.Equal(18, title.Text.Size, 6);
        Assert.EndsWith("…", title.Text.Lines[0].Text);
        if (subtitle) Assert.Contains(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "topology-group-subtitle");
        await Capture(prepared.ToSvg(), prepared.ToPng(), "group-ordinary-width-" + subtitle, 600, 400, fontPath);
    }

    [Theory]
    [InlineData(64, 46)]
    [InlineData(44, 44)]
    [InlineData(22, 22)]
    public void CompactActorGeometryAndRoutingStayInsideTheNode(double width, double height) {
        var node = new TopologyNode { Id = "actor", Label = "Actor", X = 20, Y = 30, Width = width, Height = height, Shape = TopologyNodeShape.Actor };
        foreach (var point in TopologyNodeShapeGeometry.SceneParts(node).SelectMany(part => part.Path.Commands)) {
            Assert.InRange(point.X, node.X, node.X + width);
            Assert.InRange(point.Y, node.Y, node.Y + height);
        }
        var arm = TopologyNodeShapeGeometry.SceneParts(node)[2].Path.Commands[1];
        var attachment = TopologyNodeShapeGeometry.BoundaryPoint(node, 400, 100, TopologyEdgePort.Right, 0);
        Assert.Equal(arm.X, attachment.X, 3); Assert.Equal(arm.Y, attachment.Y, 3);
        var center = TopologyNodeShapeGeometry.RoutingCenter(node);
        Assert.Equal(arm.Y, center.Y, 3);
    }

    private static async Task Capture(string svg, byte[] png, string name, int width, int height, string? fontPath = null) {
        Assert.NotEmpty(png);
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>", width, height);
        if (fontPath != null) {
            await session.Page.AddStyleTagAsync(new PageAddStyleTagOptions { Content = "@font-face{font-family:'Carlito';src:url(data:font/ttf;base64," + Convert.ToBase64String(File.ReadAllBytes(fontPath)) + ")}" });
            await session.Page.EvaluateAsync("() => document.fonts.load('18px Carlito')");
            Assert.True(await session.Page.EvaluateAsync<bool>("() => document.fonts.check('18px Carlito')"));
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        File.WriteAllText(Path.Combine(captures, name + ".svg"), svg);
        File.WriteAllBytes(Path.Combine(captures, name + ".native.png"), png);
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + ".browser.png"), FullPage = true });
    }
}
