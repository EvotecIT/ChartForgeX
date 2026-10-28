using ChartForgeX.Topology;
using ChartForgeX.Mermaid;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.VisualArtifacts;
using System.Text.Json;
using System.Xml.Linq;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class CompoundLongEdgePortTests {
    [Fact]
    public void VersionedCompoundGroupsPreserveMembershipAndRelationshipsThroughMarkdownFences() {
        var source = File.ReadAllText(MermaidFlowchartLabelDepthTests.Fixture("flowchart-compound-long-edge.mmd"));
        using var expected = JsonDocument.Parse(File.ReadAllText(MermaidFlowchartLabelDepthTests.Fixture("flowchart-compound-long-edge.expected.json")));
        var direct = MermaidRenderer.Render(source);
        Assert.Empty(direct.Diagnostics);
        var document = Assert.IsType<MermaidFlowchartDocument>(direct.Document);
        foreach (var group in expected.RootElement.GetProperty("groups").EnumerateArray()) {
            var actual = document.Subgraphs.Single(item => item.Id == group.GetProperty("id").GetString());
            Assert.Equal(group.GetProperty("title").GetString(), actual.Title);
            Assert.Equal(group.GetProperty("members").EnumerateArray().Select(item => item.GetString()), actual.NodeIds.OrderBy(id => id));
        }
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fenced.Diagnostics);
        var artifact = Assert.Single(fenced.Artifacts);
        var nativeArtifact = Assert.IsType<VisualArtifact>(direct.Artifact);
        Assert.Equal(nativeArtifact.ToSvg(), artifact.ToSvg());
        Assert.Equal(nativeArtifact.ToPng(), artifact.ToPng());
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        Assert.Equal(expected.RootElement.GetProperty("nodes").EnumerateArray().Select(item => item.GetString()), envelope.Nodes.Select(node => node.Id));
        Assert.Equal(expected.RootElement.GetProperty("edges").EnumerateArray().Select(edge => (edge[0].GetString(), edge[1].GetString())),
            envelope.Edges.Select(edge => ((string?)edge.SourceId, (string?)edge.TargetId)));
    }

    [Theory]
    [InlineData("LR")]
    [InlineData("RL")]
    [InlineData("TB")]
    [InlineData("BT")]
    public void FittedMermaidCompoundExportsKeepOrthogonalEndpointStubs(string direction) {
        var source = "%%{init: {\"flowchart\":{\"curve\":\"linear\"}}}%%\n"
            + File.ReadAllText(MermaidFlowchartLabelDepthTests.Fixture("flowchart-compound-long-edge.mmd"))
                .Replace("flowchart LR", "flowchart " + direction, StringComparison.Ordinal);
        var options = new MermaidRenderOptions { Flowchart = new MermaidFlowchartRenderOptions { Width = 608, Height = 440 } };
        var artifact = MermaidRenderer.Render(source, options).Artifact!;
        var svgText = artifact.ToSvg();
        if (Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output);
            var stem = Path.Combine(output, "compound-endpoint-stubs-" + direction);
            File.WriteAllText(stem + ".svg", svgText); File.WriteAllBytes(stem + ".native.png", artifact.ToPng());
        }
        var svg = XDocument.Parse(svgText);
        var paths = svg.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "topology-edge-line").ToArray();
        Assert.NotEmpty(paths);
        foreach (var path in paths) {
            var data = (string)path.Attribute("d")!;
            var route = Assert.Single(ChartForgeX.Core.ChartMapPathParser.ParseSubpaths(data, pixelsPerUnit: 8)).Points;
            Assert.True(route.Count >= 2, data);
            // Native corner blending may chamfer interior turns; both endpoint legs must keep the layout axis.
            foreach (var (start, end) in new[] { (route[0], route[1]), (route[^2], route[^1]) }) {
                var dx = Math.Abs(end.X - start.X); var dy = Math.Abs(end.Y - start.Y);
                Assert.True(dx + dy > .002 && (direction is "LR" or "RL" ? dy < .002 : dx < .002), data);
            }
        }
    }

    [Theory]
    [InlineData(TopologyLayoutDirection.LeftToRight)]
    [InlineData(TopologyLayoutDirection.RightToLeft)]
    [InlineData(TopologyLayoutDirection.TopToBottom)]
    [InlineData(TopologyLayoutDirection.BottomToTop)]
    [InlineData(TopologyLayoutDirection.LeftToRight, true)]
    [InlineData(TopologyLayoutDirection.RightToLeft, true)]
    [InlineData(TopologyLayoutDirection.TopToBottom, true)]
    [InlineData(TopologyLayoutDirection.BottomToTop, true)]
    public void PackedLongEdgeKeepsOrthogonalEndLegsAndASeparateTargetPort(TopologyLayoutDirection direction, bool shaped = false) {
        var chart = TopologyChart.Create().WithViewport(1200, 800).WithLegend(null)
            .WithLayout(TopologyLayoutMode.Layered).WithLayoutDirection(direction)
            .AddAutoGroup("a", "A").AddAutoGroup("b", "B")
            .AddAutoNode("one", "One", groupId: "a").AddAutoNode("middle", "Middle", groupId: "a")
            .AddAutoNode("last", "Last", groupId: "a").AddAutoNode("other", "Other", groupId: "b")
            .AddEdge("first", "one", "middle").AddEdge("second", "middle", "last").AddEdge("skip", "one", "last", "Bypass");
        if (shaped) foreach (var node in chart.Nodes) { node.Shape = TopologyNodeShape.Rectangle; node.ShowStatusBadge = false; }
        var prepared = TopologyLayoutEngine.Prepare(chart);
        var nodes = prepared.Nodes.ToDictionary(node => node.Id);
        var skip = prepared.Edges.Single(edge => edge.Id == "skip");
        var direct = prepared.Edges.Single(edge => edge.Id == "second");
        var route = TopologyRenderPrimitives.EdgePoints(prepared, skip, nodes);
        var neighbor = TopologyRenderPrimitives.EdgePoints(prepared, direct, nodes);
        for (var index = 1; index < route.Count; index++) Assert.True(
            Math.Abs(route[index].X - route[index - 1].X) < .001 || Math.Abs(route[index].Y - route[index - 1].Y) < .001,
            "Every long-edge segment must remain orthogonal: " + string.Join(";", route.Select(point => $"{point.X},{point.Y}")));
        Assert.True(Math.Abs(route[^1].X - neighbor[^1].X) + Math.Abs(route[^1].Y - neighbor[^1].Y) >= 8,
            "Long and adjacent direct arrows must not share the same target port.");
        var middle = nodes["middle"];
        for (var index = 1; index < route.Count; index++) {
            var a = route[index - 1]; var b = route[index];
            Assert.False(Math.Abs(a.X - b.X) < .001
                ? a.X > middle.X && a.X < middle.X + middle.Width && Math.Max(a.Y, b.Y) > middle.Y && Math.Min(a.Y, b.Y) < middle.Y + middle.Height
                : a.Y > middle.Y && a.Y < middle.Y + middle.Height && Math.Max(a.X, b.X) > middle.X && Math.Min(a.X, b.X) < middle.X + middle.Width);
        }
        Assert.NotEmpty(chart.ToPng(new TopologyRenderOptions { FitContentToViewport = true, IncludeLegend = false }));
    }
}
