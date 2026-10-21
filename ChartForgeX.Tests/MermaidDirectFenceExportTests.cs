using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects the same public rendering contract through source and Markdown entry points.</summary>
public sealed class MermaidDirectFenceExportTests {
    public static IEnumerable<object[]> Sources() {
        foreach (var fixture in new[] { "class-native-notes-generics", "class-two-way", "state-controls-notes",
            "state-nested-directions", "er-alias-attributes", "sequence-lifetimes-boxes",
            "flowchart-nested-directions", "requirement-basic" }) {
            var source = File.ReadAllText(Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", fixture + ".mmd"));
            foreach (var dark in new[] { false, true }) yield return new object[] { fixture, source, dark };
        }
        foreach (var dark in new[] { false, true }) yield return new object[] { "kanban-relative-indent",
            "kanban\n  todo[To do]\n    first[First task]\n  done[Done]\n    second[Second task]", dark };
    }

    [Theory]
    [MemberData(nameof(Sources))]
    public void DirectAndFencedNotationExportsHaveIdenticalSvgAndNativePng(string fixture, string source, bool dark) {
        var options = new MermaidRenderOptions {
            Presentation = new MermaidPresentationOptions { ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light,
                FontFamily = BrowserProofFonts.Family, FontSize = 12 },
            Flowchart = new MermaidFlowchartRenderOptions { Width = 360, Height = 760 },
            Class = new MermaidTopologyRenderOptions { Width = 360, Height = 760 },
            State = new MermaidTopologyRenderOptions { Width = 360, Height = 760 },
            EntityRelationship = new MermaidTopologyRenderOptions { Width = 360, Height = 760 },
            Requirement = new MermaidTopologyRenderOptions { Width = 360, Height = 760 },
            Kanban = new MermaidTopologyRenderOptions { Width = 360, Height = 760 },
            Sequence = new MermaidSequenceRenderOptions { Width = 360, Height = 760 }
        };
        var direct = MermaidRenderer.Render(source, options);
        Assert.Empty(direct.Diagnostics);
        var fenced = new MermaidVisualMarkupParser(options).Parse("Before\n\n```mermaid\n" + source + "\n```\nAfter");
        Assert.Empty(fenced.Diagnostics);
        var sourceArtifact = Assert.IsType<VisualArtifact>(direct.Artifact);
        var fenceArtifact = Assert.Single(fenced.Artifacts);
        var svg = sourceArtifact.ToSvg();
        var png = sourceArtifact.ToPng();
        Assert.Equal(svg, fenceArtifact.ToSvg());
        Assert.Equal(png, fenceArtifact.ToPng());
        var xml = XDocument.Parse(svg);
        Assert.DoesNotContain(xml.Descendants(), element => element.Name.LocalName is "script" or "foreignObject");
        var measured = sourceArtifact.MeasureRasterSize();
        var pixels = RasterImageDecoder.Decode(png);
        Assert.Equal((int)measured.Width, pixels.Width);
        Assert.Equal((int)measured.Height, pixels.Height);
        Assert.Contains(pixels.Pixels, value => value != 0);
        var sourceFacts = sourceArtifact.ToInterchangeEnvelope();
        var fenceFacts = fenceArtifact.ToInterchangeEnvelope();
        Assert.Equal(sourceFacts.Nodes.Select(node => (node.Id, node.Label)), fenceFacts.Nodes.Select(node => (node.Id, node.Label)));
        Assert.Equal(sourceFacts.Edges.Select(edge => (edge.SourceId, edge.TargetId, edge.Label)),
            fenceFacts.Edges.Select(edge => (edge.SourceId, edge.TargetId, edge.Label)));
        Assert.Equal(sourceFacts.Groups.Select(group => group.Id), fenceFacts.Groups.Select(group => group.Id));
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        var name = Path.Combine(output, "direct-fence-" + fixture + (dark ? "-dark" : "-light"));
        File.WriteAllText(name + ".svg", svg);
        File.WriteAllBytes(name + ".native.png", png);
        File.WriteAllText(name + ".json", JsonSerializer.Serialize(new {
            fixture, dark, pixels.Width, pixels.Height, directSvgEqualsFence = true, directPngEqualsFence = true,
            nodes = sourceFacts.Nodes.Count, edges = sourceFacts.Edges.Count, groups = sourceFacts.Groups.Count
        }));
    }
}
