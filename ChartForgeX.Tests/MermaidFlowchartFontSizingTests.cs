using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidFlowchartFontSizingTests {
    private const string MissingFamily = "ChartForgeX Missing Flowchart Regression Font 8CF5B4";
    private const string Source = "flowchart LR\n  triage -->|no| owner[Page the service owner]\n  triage -->|yes| runbook[Run the runbook]\n  runbook --> verify[Verify recovery]";

    [Theory]
    [InlineData(12)]
    [InlineData(18)]
    public void MissingFontFlowchartRetainsCompleteLabelsInOnePreparedSvgAndPngScene(double fontSize) {
        var parsed = new MermaidParser().ParseFlowchart(Source);
        Assert.Empty(parsed.Diagnostics);
        var chart = parsed.Document!.ToTopologyChart();
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(1200, 500)),
            theme: VisualTheme.Graphite().WithTypography(new VisualTypography(dataLabelSize: fontSize)),
            frame: new VisualFrame(showLegend: false), font: FontSpec.FromFamily(MissingFamily));
        var visual = chart.Prepare(context, new TopologyRenderOptions { IncludeLegend = false, FitContentToViewport = true, MaxNodeLabelLines = 1 });
        Capture(visual, "missing-font-flowchart-" + fontSize);
        var labels = visual.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "topology-node-label").ToArray();
        Assert.All(labels, node => Assert.Equal(MissingFamily, node.Text.Style.Font.Family));
        Assert.Equal(chart.Nodes.Select(node => node.Label), labels.Select(node => string.Join("\n", node.Text.Lines.Select(line => line.Text))));
        Assert.DoesNotContain(visual.Diagnostics, diagnostic => diagnostic.Code.StartsWith("topology.label-", StringComparison.Ordinal));
        var svg = XDocument.Parse(visual.ToSvg());
        Assert.All(chart.Nodes, node => Assert.Contains(svg.Descendants(), element => element.Name.LocalName == "text" && element.Value == node.Label));
        var png = PngReader.Decode(visual.ToPng());
        Assert.Equal((int)Math.Ceiling(visual.Size.Width), png.Width);
        Assert.Equal((int)Math.Ceiling(visual.Size.Height), png.Height);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(TopologyNodeShape.Rectangle)]
    [InlineData(TopologyNodeShape.Diamond)]
    [InlineData(TopologyNodeShape.Circle)]
    [InlineData(TopologyNodeShape.DoubleCircle)]
    [InlineData(TopologyNodeShape.Stadium)]
    [InlineData(TopologyNodeShape.Hexagon)]
    [InlineData(TopologyNodeShape.Cylinder)]
    [InlineData(TopologyNodeShape.Ellipse)]
    [InlineData(TopologyNodeShape.Cloud)]
    public void OptedInNativeNodesFitMultilineFontMetricsInsideTheirShapeAndKeepMinimums(TopologyNodeShape? shape) {
        const string label = "Page the service owner\nVerify recovery";
        var chart = TopologyChart.Create().WithViewport(1200, 500);
        var node = new TopologyNode { Id = "owner", Label = label, Shape = shape, Width = 90, Height = 24,
            FitLabelToContent = true, PreserveDisplayModeSize = true, ShowStatusBadge = false };
        chart.Nodes.Add(node);
        var theme = VisualTheme.Graphite().WithTypography(new VisualTypography(dataLabelSize: 18));
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(1200, 500)), theme,
            frame: new VisualFrame(showLegend: false), font: FontSpec.FromFamily(MissingFamily));
        var visual = chart.Prepare(context, new TopologyRenderOptions { IncludeLegend = false });
        var labels = visual.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "topology-node-label").ToArray();
        Assert.Equal(label, string.Join("\n", labels.SelectMany(text => text.Text.Lines).Select(line => line.Text)));
        Assert.All(labels, text => { Assert.Equal(18, text.Text.Size); Assert.Equal(600, text.Text.Style.Font.Weight); });
        Assert.DoesNotContain(visual.Diagnostics, diagnostic => diagnostic.Code.StartsWith("topology.label-", StringComparison.Ordinal));
        var envelope = visual.ToArtifact().ToInterchangeEnvelope();
        var fitted = Assert.Single(envelope.Nodes);
        Assert.True(fitted.Width >= 90 && fitted.Height > 24);
        foreach (var text in labels) {
            var left = text.X - (text.Alignment == TextAlignment.Center ? text.Text.Metrics.Width / 2 : 0);
            var top = text.Baseline - text.Text.Ascent;
            Assert.True(left >= fitted.X && left + text.Text.Metrics.Width <= fitted.X + fitted.Width);
            Assert.True(top >= fitted.Y && top + text.Text.Metrics.Height <= fitted.Y + fitted.Height);
        }
        if (shape is TopologyNodeShape.Circle or TopologyNodeShape.DoubleCircle) Assert.Equal(fitted.Width, fitted.Height);
        Assert.Equal(90, node.Width); Assert.Equal(24, node.Height);
        Assert.True(fitted.Topology!.FitLabelToContent);
        var roundTrip = VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson());
        Assert.True(Assert.Single(roundTrip.Nodes).Topology!.FitLabelToContent);
        Capture(visual, "missing-font-shape-" + (shape?.ToString() ?? "Card"));
    }

    [Fact]
    public void FixedNodesAndDisabledMermaidFittingKeepTheirAuthoredDimensions() {
        var parsed = new MermaidParser().ParseFlowchart("flowchart LR\nowner[Page the service owner]");
        var chart = parsed.Document!.ToTopologyChart(new MermaidFlowchartRenderOptions { FitContent = false });
        Assert.False(chart.Nodes[0].FitLabelToContent);
        chart.Nodes[0].Width = 70; chart.Nodes[0].Height = 40;
        var visual = chart.Prepare(new TopologyRenderOptions { IncludeLegend = false, MaxNodeLabelLines = 1 }).Visual;
        var node = Assert.Single(visual.ToArtifact().ToInterchangeEnvelope().Nodes);
        Assert.Equal(70, node.Width); Assert.Equal(40, node.Height);
        Assert.False(node.Topology!.FitLabelToContent);
        Assert.Contains(visual.Diagnostics, diagnostic => diagnostic.Code == "topology.label-truncated");
    }

    [Fact]
    public void UseCaseActorsKeepTheirAuthoredLabelAndExternalCaptionPolicy() {
        const string label = "Project administrator";
        var rendered = MermaidRenderer.Render("usecase-beta\nactor User[\"" + label + "\"]\nUser --> Action(Do work)");
        Assert.Empty(rendered.Diagnostics);
        var chart = Assert.IsType<TopologyChart>(rendered.Artifact!.Model);
        var source = chart.Nodes.Single(node => node.Id == "User");
        Assert.Equal(label, source.Label);
        Assert.False(source.FitLabelToContent);
        Assert.Equal(24 + label.Length * 7.1, source.Width);
        Assert.Equal(108, source.Height);
        Assert.True(chart.Nodes.Single(node => node.Id == "Action").FitLabelToContent);
        var svg = XDocument.Parse(rendered.Artifact.ToSvg());
        var actor = svg.Descendants().Single(element => (string?)element.Attribute("data-node-id") == "User"
            && (string?)element.Attribute("data-cfx-role") == "topology-node");
        Assert.Equal(label, (string?)actor.Attribute("data-node-label"));
        var prepared = chart.Prepare(new TopologyRenderOptions { IncludeLegend = false });
        var fitted = prepared.ToInterchangeEnvelope().Nodes.Single(node => node.Id == "User");
        Assert.Equal(source.Width, fitted.Width); Assert.Equal(source.Height, fitted.Height);
        Assert.Equal(label, fitted.Label);
        var caption = Assert.Single(prepared.Visual.Scene.Nodes.OfType<VisualSceneText>(), text => text.Id == "User-label-0");
        Assert.True(caption.Baseline - caption.Text.Ascent > fitted.Y + 68);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OptedInCaptionsRetainCompleteTextWhenSyntheticBoldMetricsChangeAfterScaling(bool resizePreparedLayout) {
        var fontPath = Path.Combine(Path.GetTempPath(), "cfx-topology-fit-" + Guid.NewGuid().ToString("N") + ".ttf");
        try {
            using (var input = typeof(MermaidFlowchartFontSizingTests).Assembly.GetManifestResourceStream("ChartForgeX.Tests.Fixtures.OpenType.bitmap-1-1-1.ttf")!)
            using (var output = File.Create(fontPath)) input.CopyTo(output);
            var label = new string('A', 100);
            var chart = TopologyChart.Create().WithViewport(1200, 400);
            chart.Nodes.Add(new TopologyNode { Id = "owner", Label = label, Shape = TopologyNodeShape.Rectangle,
                X = 24, Y = 24, Width = 40, Height = 60, FitLabelToContent = true, PreserveDisplayModeSize = true, ShowStatusBadge = false });
            var theme = VisualTheme.Graphite().WithTypography(new VisualTypography(dataLabelSize: 18));
            var options = new TopologyRenderOptions { IncludeLegend = false, FitContentToViewport = true, MaxNodeLabelLines = 1 };
            VisualRenderContext Context(int width) => new(new VisualLayoutOptions(new VisualSize(width, 180)), theme,
                frame: new VisualFrame(showLegend: false), font: FontSpec.FromFile(fontPath));
            PreparedVisual visual;
            if (resizePreparedLayout) {
                var compiler = new VisualTopologyCompiler(chart, Context(1200), options);
                var prepared = new PreparedTopology(compiler, compiler.Compile(), new VisualRenderOptions());
                visual = prepared.WithOutputSize(320, 180).Visual;
            } else visual = chart.Prepare(Context(320), options);
            var text = Assert.Single(visual.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "topology-node-label");
            Assert.True(text.Text.Face.SynthesizeBold);
            Assert.NotNull(text.Text.Face.Font);
            Assert.True(text.Text.Size < 9);
            Assert.Equal(label, Assert.Single(text.Text.Lines).Text);
            Assert.DoesNotContain(visual.Diagnostics, diagnostic => diagnostic.Code.StartsWith("topology.label-", StringComparison.Ordinal));
            var node = Assert.Single(visual.ToArtifact().ToInterchangeEnvelope().Nodes);
            var left = text.X - text.Text.Metrics.Width / 2;
            Assert.True(left >= node.X && left + text.Text.Metrics.Width <= node.X + node.Width);
            var png = PngReader.Decode(visual.ToPng());
            Assert.Equal(320, png.Width);
            Capture(visual, "scaled-synthetic-bold-" + (resizePreparedLayout ? "prepared" : "direct"));
        } finally { File.Delete(fontPath); }
    }

    [Theory]
    [InlineData(32)]
    [InlineData(256)]
    public void LargeSourceFontsKeepDefaultMermaidConversionAndNaturalGeometryComplete(double fontSize) {
        var result = MermaidRenderer.Render("%%{init: {'fontFamily':'Carlito','themeVariables':{'fontSize':"
            + fontSize.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + "}}}%%\nflowchart LR\nsubgraph Service[Service group]\n A --> B\nend");
        Assert.Empty(result.Diagnostics);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        var authoredWidth = chart.Viewport.Width; var authoredHeight = chart.Viewport.Height;
        var prepared = chart.Prepare(new TopologyRenderOptions { IncludeLegend = false });
        Assert.DoesNotContain(prepared.Visual.Diagnostics, diagnostic => diagnostic.Code == "topology.content-fitted");
        var envelope = prepared.ToInterchangeEnvelope();
        Assert.Equal(new[] { "A", "B" }, envelope.Nodes.Select(node => node.Label));
        Assert.Equal("Service group", Assert.Single(envelope.Groups).Label);
        foreach (var node in envelope.Nodes) {
            Assert.True(node.X >= 0 && node.X + node.Width <= prepared.Width);
            Assert.True(node.Y >= 0 && node.Y + node.Height <= prepared.Height);
        }
        var svg = XDocument.Parse(result.Artifact.ToSvg());
        var png = PngReader.Decode(result.Artifact.ToPng());
        Assert.Equal((int)Math.Ceiling((double)svg.Root!.Attribute("width")!), png.Width);
        Assert.Equal((int)Math.Ceiling((double)svg.Root.Attribute("height")!), png.Height);
        Assert.Equal(authoredWidth, chart.Viewport.Width); Assert.Equal(authoredHeight, chart.Viewport.Height);
        Capture(prepared.Visual, "natural-source-font-" + fontSize);
    }

    [BrowserTheory]
    [InlineData(640)]
    [InlineData(1200)]
    public async Task MissingFontFlowchartSvgKeepsFullCaptionsInsideTheirBrowserSurfaces(int width) {
        var chart = new MermaidParser().ParseFlowchart(Source).Document!.ToTopologyChart();
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, 500)),
            VisualTheme.Graphite().WithTypography(new VisualTypography(dataLabelSize: 18)),
            frame: new VisualFrame(showLegend: false), font: FontSpec.FromFamily(MissingFamily));
        var visual = chart.Prepare(context, new TopologyRenderOptions { IncludeLegend = false, FitContentToViewport = true, MaxNodeLabelLines = 1 });
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + visual.ToSvg() + "</body></html>", width, 500);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var contained = await session.Page.EvaluateAsync<bool>("""
            () => [...document.querySelectorAll('[data-cfx-role="topology-node"]')].every(node => {
                const surface = node.querySelector('[data-cfx-role="topology-node-surface"]').getBBox();
                const label = node.querySelector('[data-cfx-role="topology-node-label"] text');
                const text = label.getBBox();
                return label.textContent === node.getAttribute('data-node-label') && text.x >= surface.x
                    && text.x + text.width <= surface.x + surface.width && text.y >= surface.y
                    && text.y + text.height <= surface.y + surface.height;
            })
            """);
        Assert.True(contained, "Browser SVG labels must remain complete and inside their measured nodes.");
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output);
            Capture(visual, "missing-font-browser-" + width);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, "missing-font-browser-" + width + ".png") });
        }
    }

    private static void Capture(PreparedVisual visual, string name) {
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, name + ".svg"), visual.ToSvg());
        File.WriteAllBytes(Path.Combine(output, name + ".native.png"), visual.ToPng());
    }
}
