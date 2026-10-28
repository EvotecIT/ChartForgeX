using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed partial class MermaidNestedSwimlaneTests {
    internal static string Fixture(string direction = "LR") => File.ReadAllText(Path.Combine(TestRepository.Root,
        "ChartForgeX.Tests", "Fixtures", "Mermaid", "swimlane-native-nesting.mmd")).Replace("swimlane-beta LR", "swimlane-beta " + direction, StringComparison.Ordinal);
    internal static MermaidRenderOptions Options(bool dark, int width) => new() {
        Presentation = new MermaidPresentationOptions { ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light, FontFamily = BrowserProofFonts.Family },
        Swimlane = new MermaidFlowchartRenderOptions { Width = width, Height = 750 }
    };

    [Theory]
    [InlineData("LR")] [InlineData("RL")] [InlineData("TB")] [InlineData("BT")]
    public void CompleteNestedSourceKeepsAstSpansRanksAndRealParentContainmentThroughPortableRestoration(string direction) {
        var source = Fixture(direction); var parsed = new MermaidParser().ParseFlowchart(source); Assert.Empty(parsed.Diagnostics);
        var document = parsed.Document!; Assert.Equal(source, document.SourceText); Assert.Equal(MermaidDiagramKind.Swimlane, document.Kind);
        using var expected = JsonDocument.Parse(File.ReadAllText(Path.Combine(TestRepository.Root, "ChartForgeX.Tests", "Fixtures", "Mermaid", "swimlane-native-nesting.expected.json")));
        Assert.Equal(expected.RootElement.GetProperty("sourceLines").EnumerateArray().Select(value => value.GetInt32()), document.Statements.Select(statement => statement.Span.Line));
        Assert.Equal(expected.RootElement.GetProperty("nodes").EnumerateArray().Select(value => value.GetString()), document.Nodes.Select(node => node.Id));
        var chart = document.ToTopologyChart(); Assert.Equal(TopologyLayoutMode.Swimlane, chart.LayoutMode);
        Assert.Equal(expected.RootElement.GetProperty("edges").EnumerateArray().Select(value => (value[0].GetString(), value[1].GetString())), chart.Edges.Select(edge => ((string?)edge.SourceNodeId, (string?)edge.TargetNodeId)));
        var prepared = chart.Prepare(); Capture(prepared, "swimlane-nesting-native-" + direction); var saved = prepared.ToInterchangeEnvelope();
        AssertContainment(saved);
        foreach (var row in expected.RootElement.GetProperty("nodeGroups").EnumerateArray()) {
            Assert.Equal(row[1].GetString(), document.Nodes.Single(node => node.Id == row[0].GetString()).SubgraphId);
            Assert.Equal(row[1].GetString(), saved.Nodes.Single(node => node.Id == row[0].GetString()).GroupId);
        }
        foreach (var row in expected.RootElement.GetProperty("groups").EnumerateArray())
            Assert.Equal(row[1].GetString(), saved.Groups.Single(group => group.Id == row[0].GetString()).Topology!.ParentGroupId);
        Assert.Equal(2, saved.Edges.Single(edge => edge.SourceId == "S" && edge.TargetId == "C").Topology!.MinimumRankSpan);
        var json = saved.ToJson(); var restored = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart();
        Assert.Equal(TopologyLayoutMode.Manual, restored.LayoutMode); var detached = restored.Prepare().ToInterchangeEnvelope(); AssertContainment(detached);
        foreach (var group in saved.Groups) {
            var copy = detached.Groups.Single(item => item.Id == group.Id);
            Assert.Equal((group.X, group.Y, group.Width, group.Height), (copy.X, copy.Y, copy.Width, copy.Height));
        }
        var svg = prepared.ToSvg(); chart.Groups[0].ParentGroupId = "Changed"; chart.Nodes[0].Label = "Changed";
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(json, prepared.ToInterchangeEnvelope().ToJson()); Assert.True(prepared.ToPng().Length > 64);
    }

    [Theory]
    [InlineData(false, 390)] [InlineData(true, 390)] [InlineData(false, 1100)] [InlineData(true, 1100)]
    public void CompleteSourceUsesTheSameNativeSceneInDirectAndMarkdownSvgPngConsumers(bool dark, int width) {
        var source = Fixture(); var options = Options(dark, width); var direct = MermaidRenderer.Render(source, options); Assert.Empty(direct.Diagnostics);
        var markup = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```"); Assert.False(markup.HasErrors);
        var artifact = direct.Artifact!; Capture(artifact, "swimlane-nesting-host-" + width + (dark ? "-dark" : "-light"));
        var other = Assert.Single(markup.Artifacts); Assert.Equal(artifact.ToSvg(), other.ToSvg()); Assert.Equal(artifact.ToPng(), other.ToPng()); AssertContainment(artifact.ToInterchangeEnvelope());
        var xml = XDocument.Parse(artifact.ToSvg()); Assert.DoesNotContain(xml.Descendants(), element => element.Name.LocalName is "script" or "image" or "foreignObject");
        Assert.Equal(5, artifact.ToInterchangeEnvelope().Groups.Count); Assert.Equal(8, artifact.ToInterchangeEnvelope().Nodes.Count);
    }

    internal static void AssertContainment(VisualArtifactInterchangeEnvelope saved) {
        foreach (var child in saved.Groups.Where(group => group.Topology!.ParentGroupId != null))
            Contains(saved.Groups.Single(group => group.Id == child.Topology!.ParentGroupId), child.X!.Value, child.Y!.Value, child.Width!.Value, child.Height!.Value);
        foreach (var node in saved.Nodes.Where(node => node.GroupId != null))
            Contains(saved.Groups.Single(group => group.Id == node.GroupId), node.X!.Value, node.Y!.Value, node.Width!.Value, node.Height!.Value);
    }
    private static void Contains(VisualArtifactInterchangeGroup parent, double x, double y, double width, double height) {
        Assert.True(x >= parent.X!.Value && y >= parent.Y!.Value && x + width <= parent.X!.Value + parent.Width!.Value + .001
            && y + height <= parent.Y!.Value + parent.Height!.Value + .001, "Native group shells must contain actual direct cards and descendant shells.");
    }
    internal static void Capture(PreparedTopology prepared, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, name + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(output, name + ".native.png"), prepared.ToPng()); File.WriteAllText(Path.Combine(output, name + ".native.json"), prepared.ToInterchangeEnvelope().ToJson());
    }
    internal static void Capture(VisualArtifact artifact, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, name + ".svg"), artifact.ToSvg());
        File.WriteAllBytes(Path.Combine(output, name + ".native.png"), artifact.ToPng()); File.WriteAllText(Path.Combine(output, name + ".native.json"), artifact.ToInterchangeJson());
    }
}
