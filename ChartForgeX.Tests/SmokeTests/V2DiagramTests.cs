using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Native diagram producers share the fixed scene contract while rejecting unmigrated presentation.</summary>
public sealed class V2DiagramTests {
    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void TopologyAndSequenceShareCommonFrameThemeAndLogicalSize(VisualThemeMode mode) {
        var context = Context(mode);
        IVisualRenderable topology = Topology(); IVisualRenderable sequence = Sequence();
        var first = topology.Prepare(context); var second = sequence.Prepare(context);
        Assert.Equal(640, first.Size.Width); Assert.Equal(400, first.Size.Height);
        Assert.Equal(first.Size.Width, second.Size.Width); Assert.Equal(first.Size.Height, second.Size.Height);
        var firstSvg = XDocument.Parse(first.ToSvg()); var secondSvg = XDocument.Parse(second.ToSvg());
        Assert.Equal(Find(firstSvg, "frame-heading").ToString(), Find(secondSvg, "frame-heading").ToString());
        Assert.Equal(Find(firstSvg, "background").Attribute("fill")?.Value, Find(secondSvg, "background").Attribute("fill")?.Value);
        Assert.Equal(context.Theme.Resolve(mode).Status.Pass.Fill.ToCss(), Find(firstSvg, "topology-status").Attribute("fill")?.Value);
        Assert.Equal(640, first.ToRgba().Width); Assert.Equal(400, second.ToRgba().Height);
        Assert.Contains(first.Regions, region => region.Id == "source" && region.Role == "topology-node");
        Assert.Contains(first.Regions, region => region.Id == "route" && region.Role == "topology-edge");
        Assert.Contains(second.Regions, region => region.Id == "client" && region.Role == "sequence-participant");
        Assert.Contains(second.Regions, region => region.Id == "message-1" && region.Label == "Request\npayload");
        Assert.Contains("data-source=\"client\"", second.ToSvg());
        Assert.Contains("data-direction=\"Forward\"", first.ToSvg());
    }

    [Fact]
    public void NativeDiagramsDetachFromSourceAndRetainMultilineTextAcrossExports() {
        var topology = Topology(); var sequence = Sequence();
        var first = topology.Prepare(Context()); var second = sequence.Prepare(Context());
        Assert.Equal(24, topology.Nodes[0].X); Assert.Equal(70, topology.Nodes[0].Y);
        Assert.Equal(640, topology.Viewport.Width); Assert.Equal(400, topology.Viewport.Height);
        Assert.Equal(640, sequence.Width); Assert.Equal(400, sequence.Height);
        var firstSvg = first.ToSvg(); var secondSvg = second.ToSvg();
        var firstPng = first.ToPng(); var secondPng = second.ToPng();
        Assert.Contains("Source", firstSvg); Assert.Contains("service", firstSvg);
        Assert.Contains("Request", secondSvg); Assert.Contains("payload", secondSvg);
        topology.Nodes[0].Label = "changed"; topology.Nodes.Clear(); topology.Edges.Clear();
        sequence.Participants[0].Label = "changed"; sequence.Messages[0].Text = "changed";
        Assert.Equal(firstSvg, first.ToSvg()); Assert.Equal(firstPng, first.ToPng());
        Assert.Equal(secondSvg, second.ToSvg()); Assert.Equal(secondPng, second.ToPng());
    }

    [Fact]
    public void LayeredTopologyReusesLayoutWithoutMutatingTheSourceOrGrowingTheViewport() {
        var topology = Topology(); topology.LayoutMode = TopologyLayoutMode.Layered;
        foreach (var node in topology.Nodes) { node.X = 0; node.Y = 0; }
        var prepared = topology.Prepare(Context());
        var nodes = prepared.Regions.Where(region => region.Role == "topology-node").ToArray();
        Assert.Equal(2, nodes.Length); Assert.NotEqual(nodes[0].Bounds.X, nodes[1].Bounds.X);
        Assert.All(topology.Nodes, node => { Assert.Equal(0, node.X); Assert.Equal(0, node.Y); });
        Assert.Equal(640, prepared.Size.Width); Assert.Equal(400, prepared.Size.Height);
    }

    [Fact]
    public void CommonTransparentBackgroundSurvivesDiagramFrameResolution() {
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(640, 400)), frame: new VisualFrame(showLegend: false, transparentBackground: true));
        foreach (var visual in new IVisualRenderable[] { Topology(), Sequence() }) {
            var prepared = visual.Prepare(context); var image = prepared.ToRgba();
            Assert.Equal(0, image.Pixels[3]);
            Assert.DoesNotContain("data-cfx-role=\"background\"", prepared.ToSvg());
        }
    }

    [Fact]
    public void BoundedDiagramsRejectUnmigratedFeaturesAndContentThatWouldBeClipped() {
        var oversized = Topology(); oversized.Nodes[0].Width = 800;
        Assert.Contains("fixed viewport", Assert.Throws<NotSupportedException>(() => oversized.Prepare(Context())).Message);
        var actor = Sequence(); actor.Participants[0].Kind = SequenceArtifactParticipantKind.Actor;
        Assert.Contains("data-kind=\"Actor\"", actor.Prepare(Context()).ToSvg());
        var self = Sequence(); self.Messages[0].TargetId = self.Messages[0].SourceId;
        Assert.Equal(4, self.Prepare(Context()).ToArtifact("self", VisualArtifactKind.Sequence).ToInterchangeEnvelope().Edges[0].ResolvedRoute.Count);
        var tall = Sequence(); for (var i = 0; i < 12; i++) tall.AddMessage("client", "server", "More");
        Assert.Contains("enlarge", Assert.Throws<NotSupportedException>(() => tall.Prepare(Context())).Message);
    }

    [Fact]
    public void DiagramLabelsUseTheActualPreparedFontAndDiagnoseTruncationWithoutLosingSourceText() {
        var topology = Topology(); topology.Nodes[0].Label = new string('W', 100);
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(640, 400)), font: new ChartForgeX.Typography.FontSpec { Family = "CFX intentionally missing diagram font" });
        var prepared = topology.Prepare(context);
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code.StartsWith("topology.label", StringComparison.Ordinal));
        Assert.Contains(new string('W', 100), prepared.ToArtifact("labels", VisualArtifactKind.Topology).ToInterchangeJson());
    }

    private static VisualRenderContext Context(VisualThemeMode mode = VisualThemeMode.Light) => new(
        new VisualLayoutOptions(new VisualSize(640, 400)), themeMode: mode,
        frame: new VisualFrame("Shared diagrams", "Prepared static output", showLegend: false));

    private static TopologyChart Topology() {
        var chart = TopologyChart.Create(); chart.Id = "topology";
        chart.Viewport = new TopologyViewport { Width = 640, Height = 400 };
        chart.Nodes.Add(new TopologyNode { Id = "source", Label = "Source\nservice", X = 24, Y = 70, Width = 140, Height = 64, Status = TopologyHealthStatus.Healthy });
        chart.Nodes.Add(new TopologyNode { Id = "target", Label = "Target", X = 300, Y = 70, Width = 140, Height = 64 });
        chart.Edges.Add(new TopologyEdge { Id = "route", SourceNodeId = "source", TargetNodeId = "target", Direction = VisualLinkDirection.Forward, Routing = TopologyEdgeRouting.Straight, LineStyle = TopologyEdgeLineStyle.Solid, Label = "Request" });
        return chart;
    }

    private static SequenceArtifact Sequence() => SequenceArtifact.Create("session").WithSize(640, 400)
        .AddParticipant("client", "Client").AddParticipant("server", "Server")
        .AddMessage("client", "server", "Request\npayload")
        .AddMessage("server", "client", "Response", SequenceArtifactMessageLineStyle.Dashed, SequenceArtifactMessageKind.Return);

    private static XElement Find(XDocument document, string role) => document.Descendants().First(element => (string?)element.Attribute("data-cfx-role") == role);
}
