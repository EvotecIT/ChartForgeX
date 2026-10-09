using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.Raster;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidArtifactContractTests {
    [Fact]
    public void EveryConformanceFixtureExportsSvgPngAndPortableSemantics() {
        var root = new DirectoryInfo(TestRepository.Root);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "tests", "mermaid-conformance"))) root = root.Parent;
        Assert.NotNull(root);
        foreach (var path in Directory.GetFiles(Path.Combine(root!.FullName, "tests", "mermaid-conformance", "fixtures"), "*.mmd")) {
            var source = File.ReadAllText(path);
            var result = MermaidRenderer.Render(source);
            Assert.False(result.HasErrors, Path.GetFileName(path) + ": " + string.Join("; ", result.Diagnostics.Select(item => item.Message)));
            Assert.NotNull(result.Artifact);
            Assert.Equal("svg", XDocument.Parse(result.Artifact!.ToSvg()).Root!.Name.LocalName);
            var png = result.Artifact.ToPng();
            Assert.True(png.Length > 64 && png[0] == 137 && png[1] == 80, path);
            var json = result.Artifact.ToInterchangeJson();
            Assert.Equal(json, VisualArtifactInterchangeEnvelope.FromJson(json).ToJson());
            var markup = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
            Assert.Single(markup.Artifacts);
            Assert.False(markup.HasErrors, path);
        }
    }

    [Fact]
    public void ClassMultiplicityAndDiagramRowsSurviveInterchange() {
        var result = MermaidRenderer.Render("classDiagram\nclass Customer {\n+string name\n}\nCustomer \"1\" o-- \"0..*\" Order : places");
        Assert.Empty(result.Diagnostics);
        var graph = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        Assert.Equal("1", graph.Edges[0].SourceLabel);
        Assert.Equal("0..*", graph.Edges[0].TargetLabel);
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(result.Artifact.ToInterchangeJson());
        Assert.Equal(TopologyNodeShape.Rectangle, envelope.Nodes[0].Topology!.Shape);
        Assert.Equal("+string name", envelope.Nodes[0].Details[0].Text);
    }

    [Fact]
    public void ResponsibilityGroupsPreserveExternalActorsAndAlignedProcessRanks() {
        var source = "swimlane-beta LR\nsubgraph Client\nA --> B\nend\nsubgraph Service\nC --> D\nend\nB --> C";
        var envelope = MermaidRenderer.Render(source).Artifact!.ToInterchangeEnvelope();
        var client = envelope.Groups.Single(group => group.Label == "Client");
        var service = envelope.Groups.Single(group => group.Label == "Service");
        Assert.True(client.Y + client.Height < service.Y);
        Assert.Equal(envelope.Nodes.Single(node => node.Label == "A").Y, envelope.Nodes.Single(node => node.Label == "B").Y);
        var useCase = MermaidRenderer.Render("usecase-beta\ndirection LR\nactor User\nsystemBoundary App\nAction(Do work)\nend\nUser --> Action").Artifact!.ToInterchangeEnvelope();
        var boundary = useCase.Groups.Single();
        var actor = useCase.Nodes.Single(node => node.Label == "User");
        Assert.True(actor.Y + actor.Height < boundary.Y || actor.X + actor.Width < boundary.X || actor.Y > boundary.Y + boundary.Height || actor.X > boundary.X + boundary.Width);
    }

    [Fact]
    public void PngDiagramSurfaceIncludesItsClosingBorder() {
        var chart = TopologyChart.Create().WithViewport(360, 280, 20).AddNode("box", "Box", 60, 80, width: 180, height: 100);
        chart.Nodes[0].Shape = TopologyNodeShape.Rectangle;
        chart.Nodes[0].ShowStatusBadge = false;
        var image = RasterImageDecoder.Decode(chart.ToPng());
        var offset = (130 * image.Width + 60) * 4;
        Assert.True(image.Pixels[offset] < 245 && image.Pixels[offset + 1] < 245 && image.Pixels[offset + 2] < 245, "The left edge of a closed rectangle must have visible ink.");
    }

    [Fact]
    public void MillisecondDurationsDoNotInheritDayUnitLimits() {
        var result = new MermaidParser().ParseGantt("gantt\nTask :2026-01-02, 3600000ms");
        Assert.Empty(result.Diagnostics);
        Assert.Equal(TimeSpan.FromHours(1), result.Document!.Tasks[0].End - result.Document.Tasks[0].Start);
    }

    [Theory]
    [InlineData("usecase-beta\nactor A\nB --|> A")]
    [InlineData("usecase-beta\nactor A\nA ..> : include B")]
    [InlineData("usecase-beta\nsystemBoundary A\nsystemBoundary B\nC\nend\nend")]
    [InlineData("gantt\nTask :2026-01-01, 999999999999999999999999999999d")]
    public void InvalidSemanticsDoNotProduceSuccessfulArtifacts(string source) {
        var result = MermaidRenderer.Render(source);
        Assert.True(result.HasErrors);
        Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Span.Line > 0);
    }

    [Theory]
    [InlineData("---\nconfig:\n  theme: forest\n  layout: elk\n---\nflowchart LR\nA --> B")]
    [InlineData("%%{init: {'theme': 'forest', 'layout': 'elk'}}%%\nflowchart LR\nA --> B")]
    public void UnsupportedSourceConfigurationIsVisibleToCallers(string source) {
        var result = MermaidRenderer.Render(source);
        Assert.False(result.HasErrors);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Message.Contains("layout"));
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Message.Contains("forest"));
    }
}
