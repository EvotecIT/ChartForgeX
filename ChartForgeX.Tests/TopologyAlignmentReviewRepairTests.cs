using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed partial class TopologyAlignmentReviewRepairTests {
    [Theory]
    [InlineData(false, 390)] [InlineData(true, 1100)]
    public void CompleteSourceAndFenceKeepEveryResolvedEdgeClearOfActualForeignBodies(bool dark, int width) {
        var source = File.ReadAllText(Path.Combine(TestRepository.Root, "ChartForgeX.Tests", "Fixtures", "Mermaid", "architecture-ordered-alignments.mmd"));
        var options = MermaidArchitectureNativeDepthTests.Options(dark, width); var result = MermaidRenderer.Render(source, options); Assert.Empty(result.Diagnostics);
        var artifact = result.Artifact!; TopologyAlignmentReviewCausalTests.Capture(artifact, "alignment-review-source-clearance-" + width + "-" + dark);
        var saved = artifact.ToInterchangeEnvelope(); AssertClear(saved); MermaidArchitectureAlignmentTests.AssertGeometry(saved);
        var fence = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```"); Assert.False(fence.HasErrors); var fenced = Assert.Single(fence.Artifacts);
        TopologyAlignmentReviewCausalTests.Capture(fenced, "alignment-review-fence-clearance-" + width + "-" + dark); AssertClear(fenced.ToInterchangeEnvelope());
        var restored = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson()).ToTopologyChart();
        var after = restored.ToVisualArtifact().ToInterchangeEnvelope(); AssertClear(after); MermaidArchitectureAlignmentTests.AssertGeometry(after);
        Assert.Equal(saved.Nodes.Select(node => (node.Id, node.X, node.Y, node.Width, node.Height)), after.Nodes.Select(node => (node.Id, node.X, node.Y, node.Width, node.Height)));
        Assert.Equal(saved.Edges.Select(edge => (edge.Id, edge.Topology!.Routing, edge.Topology.SourcePort, edge.Topology.TargetPort)), after.Edges.Select(edge => (edge.Id, edge.Topology!.Routing, edge.Topology.SourcePort, edge.Topology.TargetPort)));
    }

    [Theory]
    [InlineData(false, false, true, false)] [InlineData(false, true, true, false)]
    [InlineData(true, false, true, false)] [InlineData(true, true, true, false)]
    [InlineData(false, false, false, false)] [InlineData(true, true, false, false)]
    [InlineData(false, false, true, true)] [InlineData(true, false, true, true)]
    public void NativeManualAndImportedNarrowGapsKeepPortsAndRoutingWhileClearingRealBodies(bool horizontal, bool reverse, bool aligned, bool named) {
        var chart = NarrowGapChart(horizontal, reverse, aligned, named); var sourcePorts = chart.Edges.Select(edge => (edge.SourcePort, edge.TargetPort, edge.Routing, edge.SourcePortId, edge.TargetPortId)).ToArray();
        var resolvedPorts = chart.Edges.Select(edge => (
            SourcePort: edge.SourcePortId == null ? edge.SourcePort : chart.Nodes.Single(node => node.Id == edge.SourceNodeId).Ports.Single(port => port.Id == edge.SourcePortId).Side,
            TargetPort: edge.TargetPortId == null ? edge.TargetPort : chart.Nodes.Single(node => node.Id == edge.TargetNodeId).Ports.Single(port => port.Id == edge.TargetPortId).Side,
            edge.Routing, edge.SourcePortId, edge.TargetPortId)).ToArray();
        var artifact = chart.ToVisualArtifact(); TopologyAlignmentReviewCausalTests.Capture(artifact, "alignment-review-native-" + horizontal + "-" + reverse + "-" + aligned + "-" + named);
        var saved = artifact.ToInterchangeEnvelope(); AssertClear(saved);
        Assert.Equal(resolvedPorts, saved.Edges.Select(edge => (edge.Topology!.SourcePort, edge.Topology.TargetPort, edge.Topology.Routing, edge.SourcePortId, edge.TargetPortId)));
        var report = chart.Prepare(new TopologyRenderOptions { IncludeLegend = false, IncludeTitle = false }).Analyze(); Assert.All(report.Edges, edge => Assert.True(edge.SourceAttached && edge.TargetAttached));
        var restored = VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart(); var after = restored.ToVisualArtifact().ToInterchangeEnvelope(); AssertClear(after);
        Assert.Equal(saved.Nodes.Select(node => (node.Id, node.X, node.Y, node.Width, node.Height)), after.Nodes.Select(node => (node.Id, node.X, node.Y, node.Width, node.Height)));
        Assert.Equal(sourcePorts, chart.Edges.Select(edge => (edge.SourcePort, edge.TargetPort, edge.Routing, edge.SourcePortId, edge.TargetPortId)));
        if (named) {
            var edge = saved.Edges[0]; var start = edge.ResolvedRoute[0]; var end = edge.ResolvedRoute[edge.ResolvedRoute.Count - 1];
            var source = saved.Nodes.Single(node => node.Id == edge.SourceId); var target = saved.Nodes.Single(node => node.Id == edge.TargetId);
            Assert.Equal(horizontal ? source.Y!.Value + source.Height!.Value * .35 : source.X!.Value + source.Width!.Value * .35, horizontal ? start.Y : start.X, 6);
            Assert.Equal(horizontal ? target.Y!.Value + target.Height!.Value * .65 : target.X!.Value + target.Width!.Value * .65, horizontal ? end.Y : end.X, 6);
            Assert.Equal(.35, restored.Nodes.Single(node => node.Id == edge.SourceId).Ports[0].Offset); Assert.Equal(.65, restored.Nodes.Single(node => node.Id == edge.TargetId).Ports[0].Offset);
        }
    }

    [Fact]
    public void AuthoredWaypointsKeepTheirExactRouteFactsAndAreNeverReplacedByAutomaticClearance() {
        var chart = NarrowGapChart(false, false, true, false); var edge = chart.Edges[0]; edge.Waypoints.Add(new ChartPoint(235, 200));
        var before = edge.Waypoints.ToArray(); var artifact = chart.ToVisualArtifact(); TopologyAlignmentReviewCausalTests.Capture(artifact, "alignment-review-authored-waypoint-control");
        var saved = artifact.ToInterchangeEnvelope(); var restored = saved.ToTopologyChart(); Assert.Equal(before, edge.Waypoints);
        Assert.Equal(saved.Edges[0].Topology!.Waypoints.Select(point => (point.X, point.Y)), restored.Edges[0].Waypoints.Select(point => (point.X, point.Y)));
        Assert.True(TopologyAlignmentReviewCausalTests.CrossesBody(saved.Edges[0], saved.Nodes.Single(node => node.Id == "obstacle")));
        Assert.Equal(TopologyEdgePort.Bottom, restored.Edges[0].SourcePort); Assert.Equal(TopologyEdgePort.Top, restored.Edges[0].TargetPort);
    }

    [Theory]
    [InlineData(TopologyLayoutDirection.TopToBottom, TopologyLayoutDirection.LeftToRight, TopologyAlignmentKind.Column, false)]
    [InlineData(TopologyLayoutDirection.BottomToTop, TopologyLayoutDirection.RightToLeft, TopologyAlignmentKind.Column, false)]
    [InlineData(TopologyLayoutDirection.LeftToRight, TopologyLayoutDirection.TopToBottom, TopologyAlignmentKind.Row, false)]
    [InlineData(TopologyLayoutDirection.RightToLeft, TopologyLayoutDirection.BottomToTop, TopologyAlignmentKind.Row, false)]
    [InlineData(TopologyLayoutDirection.TopToBottom, TopologyLayoutDirection.RightToLeft, TopologyAlignmentKind.Column, true)]
    [InlineData(TopologyLayoutDirection.RightToLeft, TopologyLayoutDirection.BottomToTop, TopologyAlignmentKind.Row, true)]
    public void NearestDirectionChainsKeepCompartmentOrderCrossDimensionsSeparatorsAndPortableFacts(TopologyLayoutDirection global, TopologyLayoutDirection local, TopologyAlignmentKind kind, bool middleOverride) {
        var chart = DirectionChart(global, local, kind, middleOverride); var sourceFacts = chart.Groups.Select(group => (group.Id, group.ParentGroupId, group.LayoutDirection)).ToArray();
        var artifact = chart.ToVisualArtifact(); TopologyAlignmentReviewCausalTests.Capture(artifact, "alignment-review-direction-" + global + "-" + local + "-" + middleOverride);
        var saved = artifact.ToInterchangeEnvelope(); AssertDirection(saved, kind);
        Assert.Equal(sourceFacts, saved.Groups.Select(group => (group.Id, group.Topology!.ParentGroupId, group.Topology.LayoutDirection)));
        var xml = XDocument.Parse(artifact.ToSvg()); var separator = Assert.Single(xml.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "topology-compartment-separator");
        double Number(string name) => double.Parse(separator.Attribute(name)!.Value, CultureInfo.InvariantCulture);
        if (kind == TopologyAlignmentKind.Column) { Assert.Equal(Number("y1"), Number("y2")); Assert.True(Number("x2") > Number("x1")); }
        else { Assert.Equal(Number("x1"), Number("x2")); Assert.True(Number("y2") > Number("y1")); }
        var restored = VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart(); var after = restored.ToVisualArtifact().ToInterchangeEnvelope(); AssertDirection(after, kind);
        Assert.Equal(saved.Groups.Select(group => (group.Id, group.X, group.Y, group.Width, group.Height)), after.Groups.Select(group => (group.Id, group.X, group.Y, group.Width, group.Height)));
        Assert.Equal(sourceFacts, chart.Groups.Select(group => (group.Id, group.ParentGroupId, group.LayoutDirection)));
    }

    internal static TopologyChart NarrowGapChart(bool horizontal, bool reverse, bool aligned, bool named) {
        var chart = TopologyChart.Create().WithTitle("").WithViewport(900, 700).WithLayout(TopologyLayoutMode.Manual)
            .AddNode("source", "Source", horizontal ? 80 : 160, horizontal ? 160 : 80, width: 150, height: 72)
            .AddNode("obstacle", "Audit", horizontal ? 246 : 160, horizontal ? 160 : 168, width: 150, height: 72)
            .AddNode("target", "Bus", horizontal ? 412 : 206, horizontal ? 174 : 256, width: 58, height: 44);
        foreach (var node in chart.Nodes) { node.DisplayMode = TopologyNodeDisplayMode.Card; node.PreserveDisplayModeSize = true; }
        var sourceId = reverse ? "target" : "source"; var targetId = reverse ? "source" : "target";
        chart.AddEdge("handoff", sourceId, targetId, routing: aligned ? TopologyEdgeRouting.Orthogonal : TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
        var edge = chart.Edges[0]; edge.SourcePort = horizontal ? reverse ? TopologyEdgePort.Left : TopologyEdgePort.Right : reverse ? TopologyEdgePort.Top : TopologyEdgePort.Bottom;
        edge.TargetPort = horizontal ? reverse ? TopologyEdgePort.Right : TopologyEdgePort.Left : reverse ? TopologyEdgePort.Bottom : TopologyEdgePort.Top;
        if (aligned) chart.Alignments.Add(new TopologyAlignment("order", horizontal ? TopologyAlignmentKind.Row : TopologyAlignmentKind.Column, new[] { "source", "obstacle", "target" }));
        if (named) {
            chart.Nodes.Single(node => node.Id == sourceId).Ports.Add(new TopologyNodePort { Id = "out", Side = edge.SourcePort, Offset = .35 });
            chart.Nodes.Single(node => node.Id == targetId).Ports.Add(new TopologyNodePort { Id = "in", Side = edge.TargetPort, Offset = .65 });
            edge.SourcePortId = "out"; edge.TargetPortId = "in";
            edge.SourcePort = TopologyEdgePort.Auto; edge.TargetPort = TopologyEdgePort.Auto;
        }
        return chart;
    }

    internal static TopologyChart DirectionChart(TopologyLayoutDirection global, TopologyLayoutDirection local, TopologyAlignmentKind kind, bool middleOverride) {
        var chart = TopologyAlignmentReviewCausalTests.CompartmentChart(global, local, kind);
        if (middleOverride) {
            chart.Groups.Single(group => group.Id == "outer").LayoutDirection = global;
            chart.AddAutoGroup("middle", "Nearest authored direction"); var middle = chart.Groups.Last(); middle.ParentGroupId = "outer"; middle.LayoutDirection = local;
            chart.Groups.Single(group => group.Id == "inner").ParentGroupId = "middle";
        }
        return chart;
    }

    internal static void AssertDirection(VisualArtifactInterchangeEnvelope saved, TopologyAlignmentKind kind) {
        var first = saved.Groups.Single(group => group.Id == "first"); var second = saved.Groups.Single(group => group.Id == "second");
        if (kind == TopologyAlignmentKind.Column) { Assert.Equal(first.X, second.X); Assert.Equal(first.Width, second.Width); Assert.True(first.Y!.Value + first.Height!.Value < second.Y!.Value); }
        else { Assert.Equal(first.Y, second.Y); Assert.Equal(first.Height, second.Height); Assert.True(first.X!.Value + first.Width!.Value < second.X!.Value); }
        foreach (var group in saved.Groups) {
            foreach (var node in saved.Nodes.Where(node => node.GroupId == group.Id)) Contains(group, node.X!.Value, node.Y!.Value, node.Width!.Value, node.Height!.Value);
            foreach (var child in saved.Groups.Where(child => child.Topology!.ParentGroupId == group.Id)) Contains(group, child.X!.Value, child.Y!.Value, child.Width!.Value, child.Height!.Value);
        }
        static void Contains(VisualArtifactInterchangeGroup parent, double x, double y, double width, double height) => Assert.True(x >= parent.X!.Value - .000001 && y >= parent.Y!.Value - .000001 && x + width <= parent.X!.Value + parent.Width!.Value + .000001 && y + height <= parent.Y!.Value + parent.Height!.Value + .000001);
    }

    internal static void AssertClear(VisualArtifactInterchangeEnvelope saved) {
        foreach (var edge in saved.Edges) foreach (var node in saved.Nodes) {
            if (node.Id == edge.SourceId || node.Id == edge.TargetId || node.Topology!.DisplayMode == TopologyNodeDisplayMode.Hidden) continue;
            Assert.False(TopologyAlignmentReviewCausalTests.CrossesBody(edge, node), edge.Id + " crosses actual " + node.Id + " body.");
        }
    }
}
