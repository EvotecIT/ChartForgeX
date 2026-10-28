using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyNestedSwimlaneTests {
    private static TopologyChart Chart(TopologyLayoutDirection direction) {
        var chart = TopologyChart.Create().WithTitle("").WithViewport(800, 600, 24).WithLegend(null).WithLayout(TopologyLayoutMode.Swimlane, direction)
            .AddAutoGroup("parent", "Product\nDelivery", subtitle: "Authored\nresponsibility", iconId: "common:server")
            .AddAutoGroup("first", "API", iconId: "common:server").AddAutoGroup("deep", "Storage")
            .AddAutoGroup("second", "Operations").AddAutoGroup("peer", "Partner");
        chart.Groups[1].ParentGroupId = "parent"; chart.Groups[2].ParentGroupId = "first"; chart.Groups[3].ParentGroupId = "parent";
        chart.AddAutoNode("p", "Plan", groupId: "parent", width: 144, height: 64)
            .AddAutoNode("a", "Accept", groupId: "first", width: 144, height: 64)
            .AddAutoNode("b", "Build", groupId: "deep", width: 144, height: 64)
            .AddAutoNode("q", "Observe", groupId: "peer", width: 144, height: 64)
            .AddAutoNode("c", "Check", groupId: "second", width: 144, height: 64)
            .AddAutoNode("free", "External", width: 144, height: 64);
        for (var index = 0; index < chart.Nodes.Count; index++) {
            chart.Nodes[index].Metadata["layer"] = new[] { "0", "1", "2", "2", "3", "0" }[index];
            chart.Nodes[index].DisplayMode = TopologyNodeDisplayMode.Card; chart.Nodes[index].PreserveDisplayModeSize = true;
        }
        return chart;
    }

    [Theory]
    [InlineData(TopologyLayoutDirection.LeftToRight)] [InlineData(TopologyLayoutDirection.RightToLeft)]
    [InlineData(TopologyLayoutDirection.TopToBottom)] [InlineData(TopologyLayoutDirection.BottomToTop)]
    public void ActualNestedBandsPreserveSharedRanksParentDirectCardsSiblingOrderAndHeaderClearance(TopologyLayoutDirection direction) {
        var chart = Chart(direction); var prepared = chart.Prepare(new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false, AllowMultilineNodeLabels = true });
        MermaidNestedSwimlaneTests.Capture(prepared, "swimlane-nested-directions-" + direction); var saved = prepared.ToInterchangeEnvelope(); MermaidNestedSwimlaneTests.AssertContainment(saved);
        var horizontal = direction is TopologyLayoutDirection.LeftToRight or TopologyLayoutDirection.RightToLeft;
        var p = saved.Nodes.Single(node => node.Id == "p"); var b = saved.Nodes.Single(node => node.Id == "b"); var q = saved.Nodes.Single(node => node.Id == "q"); var c = saved.Nodes.Single(node => node.Id == "c");
        Assert.Equal(horizontal ? b.X : b.Y, horizontal ? q.X : q.Y);
        var first = saved.Groups.Single(group => group.Id == "first"); var second = saved.Groups.Single(group => group.Id == "second");
        Assert.True(horizontal ? p.Y!.Value + p.Height!.Value < first.Y!.Value : p.X!.Value + p.Width!.Value < first.X!.Value);
        Assert.True(horizontal ? first.Y!.Value + first.Height!.Value < second.Y!.Value : first.X!.Value + first.Width!.Value < second.X!.Value);
        var sign = direction is TopologyLayoutDirection.RightToLeft or TopologyLayoutDirection.BottomToTop ? -1 : 1;
        Assert.True(((horizontal ? c.X!.Value : c.Y!.Value) - (horizontal ? b.X!.Value : b.Y!.Value)) * sign > 144);
        foreach (var group in prepared.Analyze().Groups.Where(group => group.HeaderBounds.HasValue)) {
            var header = group.HeaderBounds!.Value;
            foreach (var node in saved.Nodes.Where(node => node.GroupId == group.Id)) Assert.True(node.Y!.Value >= header.Bottom + 12);
            foreach (var child in saved.Groups.Where(child => child.Topology!.ParentGroupId == group.Id)) Assert.True(child.Y!.Value >= header.Bottom + 12);
        }
        Assert.Equal(new[] { "parent", "first", "deep", "second", "peer" }, chart.Groups.Select(group => group.Id));
        Assert.Equal(new[] { "p", "a", "b", "q", "c", "free" }, saved.Nodes.Select(node => node.Id)); Assert.Contains("data-group-icon-shape=\"Server\"", prepared.ToSvg());
    }

    [Theory]
    [InlineData(TopologyLayoutDirection.LeftToRight)] [InlineData(TopologyLayoutDirection.TopToBottom)]
    public void ParentWithOnlyChildBandsGetsNoFabricatedEmptyLaneAndEmptyDescendantRetainsItsRealShell(TopologyLayoutDirection direction) {
        var chart = TopologyChart.Create().WithTitle("").WithViewport(800, 600, 24).WithLayout(TopologyLayoutMode.Swimlane, direction)
            .AddAutoGroup("parent", "Parent").AddAutoGroup("child", "Child").AddAutoGroup("empty", "Empty");
        chart.Groups[1].ParentGroupId = "parent"; chart.Groups[2].ParentGroupId = "parent";
        chart.AddAutoNode("node", "Node", groupId: "child", width: 144, height: 64); chart.Nodes[0].DisplayMode = TopologyNodeDisplayMode.Card; chart.Nodes[0].PreserveDisplayModeSize = true;
        var prepared = chart.Prepare(new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false }); MermaidNestedSwimlaneTests.Capture(prepared, "swimlane-no-fabricated-parent-band-" + direction);
        var saved = prepared.ToInterchangeEnvelope(); MermaidNestedSwimlaneTests.AssertContainment(saved); Assert.Single(saved.Nodes); Assert.Equal(3, saved.Groups.Count);
        var parent = saved.Groups.Single(group => group.Id == "parent"); var child = saved.Groups.Single(group => group.Id == "child");
        Assert.True(direction == TopologyLayoutDirection.LeftToRight ? child.Y!.Value - parent.Y!.Value < 100 : child.X!.Value - parent.X!.Value < 100);
        var empty = saved.Groups.Single(group => group.Id == "empty"); Assert.True(empty.Width!.Value > 0 && empty.Height!.Value > 0);
        Assert.DoesNotContain(saved.Nodes, node => node.GroupId == "parent" || node.GroupId == "empty");
    }

    [Theory]
    [InlineData(TopologyLayoutDirection.LeftToRight, TopologyEdgePort.Right, TopologyEdgePort.Top)]
    [InlineData(TopologyLayoutDirection.RightToLeft, TopologyEdgePort.Left, TopologyEdgePort.Top)]
    [InlineData(TopologyLayoutDirection.TopToBottom, TopologyEdgePort.Right, TopologyEdgePort.Top)]
    [InlineData(TopologyLayoutDirection.BottomToTop, TopologyEdgePort.Right, TopologyEdgePort.Bottom)]
    public void ExistingMirrorOwnerRetainsAuthoredPortsAndWaypointOffsetsAcrossNestedBands(TopologyLayoutDirection direction, TopologyEdgePort source, TopologyEdgePort target) {
        var chart = Chart(direction).AddEdge("link", "b", "c", routing: TopologyEdgeRouting.Orthogonal);
        chart.Edges[0].SourcePort = TopologyEdgePort.Right; chart.Edges[0].TargetPort = TopologyEdgePort.Top;
        chart.Edges[0].Waypoints.Add(new ChartPoint(500, 500)); chart.Edges[0].Waypoints.Add(new ChartPoint(600, 550));
        var prepared = chart.Prepare(new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false });
        MermaidNestedSwimlaneTests.Capture(prepared, "swimlane-authored-routes-" + direction); var saved = prepared.ToInterchangeEnvelope();
        var parent = saved.Groups.Single(group => group.Id == "parent"); var edge = Assert.Single(saved.Edges).Topology!;
        Assert.Equal(source, edge.SourcePort); Assert.Equal(target, edge.TargetPort); Assert.Equal(2, edge.Waypoints.Count);
        var first = edge.Waypoints[0]; var horizontal = direction is TopologyLayoutDirection.LeftToRight or TopologyLayoutDirection.RightToLeft;
        var reverse = direction is TopologyLayoutDirection.RightToLeft or TopologyLayoutDirection.BottomToTop;
        var origin = horizontal ? parent.X!.Value : parent.Y!.Value; var size = horizontal ? parent.Width!.Value : parent.Height!.Value;
        var position = horizontal ? first.X : first.Y;
        Assert.Equal(476, reverse ? origin + size - position : position - origin, 6);
        Assert.Equal(TopologyEdgePort.Right, chart.Edges[0].SourcePort); Assert.Equal(500, chart.Edges[0].Waypoints[0].X);
    }

    [Theory]
    [InlineData(TopologyLayoutDirection.LeftToRight)] [InlineData(TopologyLayoutDirection.BottomToTop)]
    public void HiddenAncestorCaptionsReleaseMeasuredSpaceWithoutDroppingNestedGlyphsOrMembership(TopologyLayoutDirection direction) {
        var visible = Chart(direction).Prepare(new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false });
        var chart = Chart(direction); chart.Groups[0].ShowLabel = false;
        var hidden = chart.Prepare(new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false });
        MermaidNestedSwimlaneTests.Capture(hidden, "swimlane-hidden-ancestor-caption-" + direction); MermaidNestedSwimlaneTests.AssertContainment(hidden.ToInterchangeEnvelope());
        var before = visible.Analyze().Groups.Single(group => group.Id == "parent").HeaderBounds!.Value;
        var after = hidden.Analyze().Groups.Single(group => group.Id == "parent").HeaderBounds!.Value; Assert.True(after.Height < before.Height);
        var text = hidden.Visual.Scene.Nodes.OfType<VisualSceneText>().SelectMany(node => node.Text.Lines.Select(line => line.Text));
        Assert.DoesNotContain("Product", text); Assert.DoesNotContain("Delivery", text); Assert.Contains("API", text); Assert.Contains("data-group-icon-shape=\"Server\"", hidden.ToSvg());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void MissingParentsAndCyclesUseTheExistingHierarchyDiagnosticBeforeRecursivePlacement(bool cycle) {
        var chart = TopologyChart.Create().WithLayout(TopologyLayoutMode.Swimlane).AddAutoGroup("a", "A").AddAutoGroup("b", "B");
        chart.Groups[0].ParentGroupId = "b"; chart.Groups[1].ParentGroupId = cycle ? "a" : "missing";
        var error = Assert.Throws<TopologyValidationException>(() => chart.Prepare());
        Assert.Contains(error.Result.Errors, issue => issue.Code == (cycle ? "group-parent-cycle" : "missing-parent-group"));
    }
}
