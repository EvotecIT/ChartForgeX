using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidEntityShellLifecycleTests {
    [Fact]
    public void ForwardShellReferencesMoveAuthoredClassesAndPaintsBeforeLaterGroupOverrides() {
        var result = MermaidRenderer.Render(MermaidClassErCaptionTests.Source("er"), MermaidClassErCaptionTests.Options(false, 1100));
        Assert.False(result.HasErrors);
        var document = Assert.IsType<MermaidEntityRelationshipDocument>(result.Document);
        Assert.Equal(new[] { "Outside", "C", "O" }, document.Entities.Select(node => node.Id));
        var shell = document.Groups.Single(group => group.Id == "Domain: core");
        Assert.Equal(new[] { "shell" }, shell.Classes);
        Assert.Equal(("#dcfce7", "#92400e", "#14532d"), (shell.Styles["fill"], shell.Styles["stroke"], shell.Styles["color"]));
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        var native = chart.Groups.Single(group => group.Id == shell.Id);
        Assert.Equal(("Data domain", "#dcfce7", "#92400e", "#14532d"), (native.Label, native.BackgroundColor, native.BorderColor, native.LabelColor));
        Assert.Equal("**Data** domain", native.Metadata["mermaid.source.label"]);
        Assert.DoesNotContain(chart.Nodes, node => document.Groups.Any(group => group.Id == node.Id));
        Assert.Equal("Order group", chart.Nodes.Single(node => node.Id == "C").GroupId);
        Assert.Equal("Customer\naccount", chart.Nodes.Single(node => node.Id == "C").Label);
        Assert.Equal("string id  PK  External key & value", chart.Nodes.Single(node => node.Id == "C").Details[0].Text);
        var edges = chart.Edges;
        Assert.Equal(("Outside", "Domain: core", TopologyMarkerKind.ExactlyOne, TopologyMarkerKind.ZeroOrMany),
            (edges[0].SourceNodeId, edges[0].TargetNodeId, edges[0].SourceMarker, edges[0].TargetMarker));
        Assert.Equal(("Domain: core", "Order group", TopologyEdgeLineStyle.Dashed, TopologyMarkerKind.OneOrMany),
            (edges[2].SourceNodeId, edges[2].TargetNodeId, edges[2].LineStyle, edges[2].TargetMarker));
        var svg = XDocument.Parse(result.Artifact.ToSvg());
        Assert.Contains(svg.Descendants(), element => string.Equals((string?)element.Attribute("fill"), "#dcfce7", StringComparison.OrdinalIgnoreCase));
        var image = RasterImageDecoder.Decode(result.Artifact.ToPng()); var expected = ChartColor.Parse("#dcfce7");
        Assert.True(Enumerable.Range(0, image.Pixels.Length / 4).Count(index => image.Pixels[index * 4] == expected.R && image.Pixels[index * 4 + 1] == expected.G && image.Pixels[index * 4 + 2] == expected.B) > 20);
    }

    [Fact]
    public void NestedShellRoutesAttachToRealGroupBoundsAndPortableEndpointsStayAuthoredIdentities() {
        var result = MermaidRenderer.Render(MermaidClassErCaptionTests.Source("er"), MermaidClassErCaptionTests.Options(false, 390));
        Assert.False(result.HasErrors); var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        var prepared = chart.Prepare(); var report = prepared.Analyze();
        Assert.Empty(report.Collisions);
        var outer = report.Groups.Single(group => group.Id == "Domain: core"); var inner = report.Groups.Single(group => group.Id == "Order group");
        Contains(outer.Bounds, inner.Bounds);
        foreach (var node in report.Nodes.Where(node => node.Id == "C" || node.Id == "O")) {
            Contains(inner.Bounds, node.Bounds);
            Assert.False(inner.HeaderBounds.HasValue && Overlaps(inner.HeaderBounds.Value, node.Bounds));
        }
        Assert.All(report.Edges, edge => { Assert.True(edge.SourceAttached, edge.Id); Assert.True(edge.TargetAttached, edge.Id); });
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(prepared.ToInterchangeEnvelope().ToJson());
        Assert.Equal(chart.Edges.Select(edge => (edge.SourceNodeId, edge.TargetNodeId)), envelope.Edges.Select(edge => (edge.SourceId, edge.TargetId)));
        Assert.Equal(TopologyLayoutDirection.TopToBottom, envelope.Groups.Single(group => group.Id == "Domain: core").Topology!.LayoutDirection);
        Assert.Equal(TopologyLayoutDirection.BottomToTop, envelope.Groups.Single(group => group.Id == "Order group").Topology!.LayoutDirection);
        Assert.Equal((3, 2, 3), (envelope.Nodes.Count, envelope.Groups.Count, envelope.Edges.Count));
        var readability = prepared.AssessReadability(prepared.Width, prepared.Height);
        Assert.Equal(0, readability.NodeCollisionCount); Assert.Equal(0, readability.OutOfBoundsNodeCount);
    }

    [Theory]
    [InlineData("A {\nint id PK\n}")]
    [InlineData("A[\"Entity alias\"]")]
    public void ARealEntityAfterTheGroupStillRejectsIdentityAmbiguity(string declaration) {
        var source = "erDiagram\nOutside ||--o{ A : enters\nsubgraph A\nB\nend\n" + declaration;
        var result = MermaidRenderer.Render(source); Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidStatement);
        var fence = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fence.Artifacts); Assert.True(fence.HasErrors);
    }

    private static void Contains(ChartRect outer, ChartRect inner) {
        Assert.True(inner.Left >= outer.Left && inner.Right <= outer.Right && inner.Top >= outer.Top && inner.Bottom <= outer.Bottom);
    }
    private static bool Overlaps(ChartRect first, ChartRect second) => first.Left < second.Right && first.Right > second.Left && first.Top < second.Bottom && first.Bottom > second.Top;
}
