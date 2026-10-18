using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidFlowchartCaptionLayoutTests {
    private const string Source = "flowchart LR\nrequest[Request] --> approved{Approved?} --> done((Done))\nstore[(Audit log)] --> complete(((Complete))) --> manual[/Manual<br/>review/]\nstyle approved fill:#d6e7fa,stroke:#3d68a0\nlinkStyle 0 stroke:#3d68a0\n";

    [BrowserTheory]
    [InlineData(false, 640)]
    [InlineData(false, 1100)]
    [InlineData(true, 640)]
    [InlineData(true, 1100)]
    public async Task ShapeOnlyFlowchartsCenterMeasuredCaptionsWithoutInfrastructureGlyphs(bool dark, int width) {
        var source = "%%{init: {'fontFamily':'" + BrowserProofFonts.Family + "'"
            + (dark ? ",'theme':'dark'" : "") + "}}%%\n"
            + (dark ? Source.Replace("#d6e7fa", "#25354a", StringComparison.Ordinal) : Source);
        var options = new MermaidRenderOptions { Flowchart = new MermaidFlowchartRenderOptions { Width = width, Height = 480 } };
        var result = MermaidRenderer.Render(source, options);
        Assert.Empty(result.Diagnostics);
        var artifact = result.Artifact!;
        var fenced = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fenced.Diagnostics);
        var fencedArtifact = Assert.Single(fenced.Artifacts);
        var svg = artifact.ToSvg(); var png = artifact.ToPng();
        Assert.Equal(svg, fencedArtifact.ToSvg());
        Assert.Equal(png, fencedArtifact.ToPng());
        await using var session = await BrowserProofFonts.OpenAsync(svg, width, 480);
        await Capture(session, svg, png, "shape-captions-" + width + (dark ? "-dark" : "-light"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);

        var chart = Assert.IsType<TopologyChart>(artifact.Model);
        Assert.All(chart.Nodes, node => Assert.Equal(TopologyNodeKind.Generic, node.Kind));
        var expectedShapes = new[] { TopologyNodeShape.Rectangle, TopologyNodeShape.Diamond, TopologyNodeShape.Circle,
            TopologyNodeShape.Cylinder, TopologyNodeShape.DoubleCircle, TopologyNodeShape.Parallelogram };
        Assert.Equal(expectedShapes, chart.Nodes.Select(node => node.Shape!.Value));
        Assert.Equal(dark ? "#25354a" : "#d6e7fa", chart.Nodes.Single(node => node.Id == "approved").BackgroundColor);
        Assert.Equal("#3d68a0", chart.Edges[0].Color);
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        Assert.Equal(chart.Nodes.Select(node => node.Id), envelope.Nodes.Select(node => node.Id));
        Assert.Equal(chart.Nodes.Select(node => node.Label), envelope.Nodes.Select(node => node.Label));
        Assert.Equal(chart.Edges.Select(edge => (edge.SourceNodeId, edge.TargetNodeId)),
            envelope.Edges.Select(edge => (edge.SourceId, edge.TargetId)));
        Assert.All(chart.Nodes, node => Assert.Contains("mermaid.source.line", node.Metadata.Keys));
        Assert.DoesNotContain(XDocument.Parse(svg).Descendants(), node => ((string?)node.Attribute("data-cfx-role"))?.StartsWith("topology-icon", StringComparison.Ordinal) == true);
        var metrics = await session.Page.EvaluateAsync<JsonElement>("""
            () => [...document.querySelectorAll('[data-cfx-role="topology-node"]')].map(node => {
                const surface = [...node.querySelectorAll('[data-cfx-role="topology-node-surface"]')].map(mark => mark.getBBox());
                const texts = [...node.querySelectorAll('[data-cfx-role="topology-node-label"] text')].map(mark => mark.getBBox());
                const union = boxes => ({ x: Math.min(...boxes.map(b => b.x)), y: Math.min(...boxes.map(b => b.y)), right: Math.max(...boxes.map(b => b.x + b.width)), bottom: Math.max(...boxes.map(b => b.y + b.height)) });
                return { id: node.getAttribute('data-node-id'), surface: union(surface), text: union(texts), captions: [...node.querySelectorAll('[data-cfx-role="topology-node-label"] text')].map(mark => mark.textContent) };
            })
            """);
        foreach (var node in metrics.EnumerateArray()) {
            var surface = node.GetProperty("surface"); var text = node.GetProperty("text");
            Assert.InRange(text.GetProperty("x").GetDouble(), surface.GetProperty("x").GetDouble(), surface.GetProperty("right").GetDouble());
            Assert.True(text.GetProperty("right").GetDouble() <= surface.GetProperty("right").GetDouble());
            Assert.InRange(text.GetProperty("y").GetDouble(), surface.GetProperty("y").GetDouble(), surface.GetProperty("bottom").GetDouble());
            Assert.True(text.GetProperty("bottom").GetDouble() <= surface.GetProperty("bottom").GetDouble());
            Assert.InRange(Math.Abs((text.GetProperty("x").GetDouble() + text.GetProperty("right").GetDouble()
                - surface.GetProperty("x").GetDouble() - surface.GetProperty("right").GetDouble()) / 2), 0, 1);
        }
        Assert.Equal(new[] { "Manual", "review" }, metrics.EnumerateArray().Single(node => node.GetProperty("id").GetString() == "manual").GetProperty("captions").EnumerateArray().Select(node => node.GetString()));
        Assert.Equal("Manual<br/>review", chart.Nodes.Single(node => node.Id == "manual").Metadata["mermaid.label"]);
    }

    [Fact]
    public void FlowchartCaptionLengthUsesAvailableGeometryAndRetainsDetachedFacts() {
        const string label = "Review the quarterly access request";
        var parsed = new MermaidParser().ParseFlowchart("flowchart LR\nrequest[" + label + "] --> approved{Approved?}");
        Assert.Empty(parsed.Diagnostics);
        var chart = parsed.Document!.ToTopologyChart();
        var prepared = chart.Prepare().Visual;
        var captions = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "topology-node-label").SelectMany(node => node.Text.Lines).Select(line => line.Text);
        Assert.Contains(label, captions);
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code.StartsWith("topology.label-", StringComparison.Ordinal));
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(prepared.ToArtifact().ToInterchangeJson());
        Assert.Equal(label, envelope.Nodes.Single(node => node.Id == "request").Label);
        chart.Nodes[0].Label = "Changed later"; chart.Nodes[0].Shape = TopologyNodeShape.Circle;
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Theory]
    [InlineData("<br>")]
    [InlineData("<br/>")]
    [InlineData("<BR />")]
    public void MultilineCaptionsUseSourceLineBreaksInEveryPlainShape(string separator) {
        var caption = "Ready" + separator + "now";
        var source = "flowchart LR\nA[" + caption + "]\nB{" + caption + "}\nC((" + caption + "))\nD[(" + caption + ")]\nE(((" + caption + ")))\nF[/" + caption + "/]";
        var direct = MermaidRenderer.Render(source);
        Assert.Empty(direct.Diagnostics);
        var artifact = direct.Artifact!;
        var chart = Assert.IsType<TopologyChart>(artifact.Model);
        Assert.All(chart.Nodes, node => { Assert.Equal("Ready\nnow", node.Label); Assert.Equal(caption, node.Metadata["mermaid.label"]); });
        var document = Assert.IsType<MermaidFlowchartDocument>(direct.Document);
        Assert.All(document.Nodes, node => Assert.Equal(caption, node.Text));
        var labels = XDocument.Parse(artifact.ToSvg()).Descendants().Where(node => node.Name.LocalName == "text"
            && (string?)node.Parent?.Attribute("data-cfx-role") == "topology-node-label").Select(node => node.Value).ToArray();
        Assert.Equal(6, labels.Count(label => label == "Ready"));
        Assert.Equal(6, labels.Count(label => label == "now"));
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fenced.Diagnostics);
        Assert.Equal(artifact.ToSvg(), Assert.Single(fenced.Artifacts).ToSvg());
        Assert.Equal(artifact.ToPng(), Assert.Single(fenced.Artifacts).ToPng());
    }

    private static async Task Capture(HtmlTinkerX.HtmlBrowserSession session, string svg, byte[] png, string name) {
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, name + ".svg"), svg);
        File.WriteAllBytes(Path.Combine(output, name + ".native.png"), png);
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".browser.png"), FullPage = true });
    }
}
