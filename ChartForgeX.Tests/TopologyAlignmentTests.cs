using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyAlignmentTests {
    private static TopologyChart Chart() {
        var chart = TopologyChart.Create().WithTitle("").WithViewport(800, 600, 24).WithLayout(TopologyLayoutMode.Layered, TopologyLayoutDirection.LeftToRight);
        foreach (var id in new[] { "third", "first", "second", "below" }) {
            chart.AddAutoNode(id, id, width: 140, height: 60); var node = chart.Nodes.Last();
            node.DisplayMode = TopologyNodeDisplayMode.Card; node.PreserveDisplayModeSize = true;
        }
        return chart;
    }

    [Theory]
    [InlineData(TopologyLayoutDirection.LeftToRight)] [InlineData(TopologyLayoutDirection.RightToLeft)]
    [InlineData(TopologyLayoutDirection.TopToBottom)] [InlineData(TopologyLayoutDirection.BottomToTop)]
    public void GlobalAxesAndCompatibleOverlapsPreserveSourceOrderInEveryPreferredFlow(TopologyLayoutDirection direction) {
        var chart = Chart(); chart.LayoutDirection = direction;
        chart.Alignments.Add(new TopologyAlignment("row", TopologyAlignmentKind.Row, new[] { "first", "second", "third" }));
        chart.Alignments.Add(new TopologyAlignment("pivot", TopologyAlignmentKind.Column, new[] { "second", "below" }));
        chart.Alignments.Add(new TopologyAlignment("redundant", TopologyAlignmentKind.Row, new[] { "first", "third" }));
        var prepared = chart.Prepare(); Capture(prepared, "native-alignments-" + direction); var envelope = prepared.ToInterchangeEnvelope();
        Assert.Equal(new[] { "third", "first", "second", "below" }, envelope.Nodes.Select(node => node.Id));
        var first = envelope.Nodes.Single(node => node.Id == "first"); var second = envelope.Nodes.Single(node => node.Id == "second");
        var third = envelope.Nodes.Single(node => node.Id == "third"); var below = envelope.Nodes.Single(node => node.Id == "below");
        Assert.Equal(first.Y, second.Y); Assert.Equal(first.Y, third.Y); Assert.Equal(second.X, below.X);
        Assert.True(first.X!.Value + first.Width!.Value < second.X); Assert.True(second.X!.Value + second.Width!.Value < third.X);
        Assert.True(second.Y!.Value + second.Height!.Value < below.Y); Assert.All(chart.Nodes, node => { Assert.Equal(0, node.X); Assert.Equal(0, node.Y); });
        Assert.Equal(new[] { "row", "pivot", "redundant" }, envelope.Topology!.Alignments.Select(alignment => alignment.Id));
    }

    [Fact]
    public void ColumnAcrossPreferredHorizontalGroupsFindsTheFeasibleVerticalSeparation() {
        var chart = TopologyChart.Create().WithTitle("").WithViewport(800, 600).WithLayout(TopologyLayoutMode.Layered, TopologyLayoutDirection.LeftToRight)
            .AddAutoGroup("left", "Left").AddAutoGroup("right", "Right")
            .AddAutoNode("a", "A", groupId: "left").AddAutoNode("b", "B", groupId: "right");
        chart.Alignments.Add(new TopologyAlignment("vertical", TopologyAlignmentKind.Column, new[] { "a", "b" }));
        var prepared = chart.Prepare(); Capture(prepared, "native-alignments-alternative"); var saved = prepared.ToInterchangeEnvelope();
        var a = saved.Nodes.Single(node => node.Id == "a"); var b = saved.Nodes.Single(node => node.Id == "b");
        Assert.Equal(a.X!.Value + a.Width!.Value / 2, b.X!.Value + b.Width!.Value / 2, 6);
        var left = saved.Groups.Single(group => group.Id == "left"); var right = saved.Groups.Single(group => group.Id == "right");
        Assert.True(left.Y!.Value + left.Height!.Value <= right.Y); Assert.True(left.X < right.X!.Value + right.Width!.Value && right.X < left.X!.Value + left.Width!.Value);
    }

    [Fact]
    public void InterleavedParentShellsAreAGenuineConflictRatherThanAResourceResult() {
        var chart = TopologyChart.Create().WithTitle("").WithLayout(TopologyLayoutMode.Layered)
            .AddAutoGroup("one", "One").AddAutoGroup("two", "Two")
            .AddAutoNode("a", "A", groupId: "one").AddAutoNode("c", "C", groupId: "one")
            .AddAutoNode("b", "B", groupId: "two").AddAutoNode("d", "D", groupId: "two");
        chart.Alignments.Add(new TopologyAlignment("forward", TopologyAlignmentKind.Row, new[] { "a", "b" }));
        chart.Alignments.Add(new TopologyAlignment("reverse", TopologyAlignmentKind.Row, new[] { "d", "c" }));
        var error = Assert.Throws<TopologyValidationException>(() => chart.Prepare()); Assert.Contains(error.Result.Errors, issue => issue.Code == "alignment-conflict");
        Assert.All(chart.Nodes, node => Assert.Equal(0, node.X)); Assert.All(chart.Groups, group => Assert.Equal(0, group.Width));
    }

    [Fact]
    public void FilteredViewsProjectOrderedMembersOnceAndKeepRawPreparedAndSourceCollectionsDetached() {
        var chart = Chart(); var declaration = new TopologyAlignment("row", TopologyAlignmentKind.Row, new[] { "first", "second", "third" });
        declaration.Metadata["source"] = "authored"; chart.Alignments.Add(declaration);
        chart.Alignments.Add(new TopologyAlignment("drop", TopologyAlignmentKind.Column, new[] { "second", "below" }));
        var view = new TopologyView { Id = "selection" }; view.NodeIds.AddRange(new[] { "third", "first" });
        var prepared = chart.Prepare(new TopologyRenderOptions { View = view, IncludeLegend = false });
        var saved = prepared.ToInterchangeEnvelope(); var projected = Assert.Single(saved.Topology!.Alignments);
        Assert.Equal(new[] { "first", "third" }, projected.NodeIds); Assert.Equal("authored", projected.Extensions["source"]);
        Assert.Equal(3, chart.Alignments[0].NodeIds.Count); Assert.Equal(2, chart.Alignments.Count);
        var json = saved.ToJson(); chart.Alignments.Clear(); projected.NodeIds.Clear(); projected.Extensions.Clear();
        Assert.Equal(json, prepared.ToInterchangeEnvelope().ToJson());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void HiddenNodesKeepTheirConstraintsWithoutPaintingOrBlockingVisibleSiblings(bool global) {
        var chart = Chart();
        if (global) foreach (var node in chart.Nodes) node.DisplayMode = null;
        else chart.Nodes.Single(node => node.Id == "below").DisplayMode = TopologyNodeDisplayMode.Hidden;
        chart.Alignments.Add(new TopologyAlignment("column", TopologyAlignmentKind.Column, new[] { "first", "below" }));
        var prepared = chart.Prepare(new TopologyRenderOptions { NodeDisplayMode = global ? TopologyNodeDisplayMode.Hidden : TopologyNodeDisplayMode.Card }); var saved = prepared.ToInterchangeEnvelope();
        var first = saved.Nodes.Single(node => node.Id == "first"); var hidden = saved.Nodes.Single(node => node.Id == "below");
        Assert.Equal(first.X!.Value + first.Width!.Value / 2, hidden.X!.Value + hidden.Width!.Value / 2, 6);
        Assert.DoesNotContain(prepared.Visual.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Id == "below" && node.Role == "topology-node");
        Assert.Equal(TopologyNodeDisplayMode.Hidden, hidden.Topology!.DisplayMode);
        var imported = saved.ToTopologyChart(); Assert.True(new TopologyChartValidator().Validate(imported).IsValid);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void UnsupportedAutomaticStatesAreExplicitAndNeverMoveAuthoredInput(int state) {
        var chart = Chart(); chart.Alignments.Add(new TopologyAlignment("row", TopologyAlignmentKind.Row, new[] { "first", "second" }));
        if (state == 0) chart.Nodes[0].X = 12;
        if (state == 1) chart.Nodes[0].Metadata["layer"] = "3";
        if (state == 2) { chart.AddEdge("span", "first", "second"); chart.Edges[0].MinimumRankSpan = 2; }
        if (state == 3) chart.LayoutMode = TopologyLayoutMode.DenseGrouped;
        var error = Assert.Throws<TopologyValidationException>(() => chart.Prepare()); Assert.Contains(error.Result.Errors, issue => issue.Code == "alignment-unsupported-state");
        Assert.Equal(state == 0 ? 12 : 0, chart.Nodes[0].X); Assert.Equal(0, chart.Nodes[1].X);
    }

    [Fact]
    public void AuthoredWaypointsPortsAnchorsAndRoutingSurviveConstrainedPlacementAndTypedImport() {
        var chart = Chart(); chart.Alignments.Add(new TopologyAlignment("row", TopologyAlignmentKind.Row, new[] { "first", "second" }));
        chart.AddEdge("path", "first", "second"); var edge = chart.Edges[0];
        edge.Routing = TopologyEdgeRouting.Straight; edge.SourcePort = TopologyEdgePort.Bottom; edge.TargetPort = TopologyEdgePort.Top;
        edge.Waypoints.Add(new ChartPoint(200, 400)); edge.Waypoints.Add(new ChartPoint(500, 400)); edge.LabelAnchorX = 350; edge.LabelAnchorY = 400; edge.HasLabelAnchorOverride = true;
        var prepared = chart.Prepare(); Capture(prepared, "native-alignments-authored-path"); var saved = prepared.ToInterchangeEnvelope();
        var facts = saved.Edges[0].Topology!; Assert.Equal(TopologyEdgeRouting.Straight, facts.Routing); Assert.Equal(TopologyEdgePort.Bottom, facts.SourcePort); Assert.Equal(TopologyEdgePort.Top, facts.TargetPort);
        var restored = saved.ToTopologyChart(); Assert.Equal(facts.Waypoints.Select(point => (point.X, point.Y)), restored.Edges[0].Waypoints.Select(point => (point.X, point.Y)));
        Assert.Equal(facts.LabelAnchor!.X, restored.Edges[0].LabelAnchorX); Assert.Equal(facts.LabelAnchor!.Y, restored.Edges[0].LabelAnchorY);
        Assert.Equal(new[] { new ChartPoint(200, 400), new ChartPoint(500, 400) }, edge.Waypoints);
    }

    [Fact]
    public void ManualGeometryRejectsBrokenCentersOrderAndActualParentContainmentWithoutInventingAGap() {
        var chart = Chart(); chart.Alignments.Add(new TopologyAlignment("row", TopologyAlignmentKind.Row, new[] { "first", "second" }));
        var saved = chart.Prepare().ToInterchangeEnvelope(); var restored = saved.ToTopologyChart();
        var first = restored.Nodes.Single(node => node.Id == "first"); var second = restored.Nodes.Single(node => node.Id == "second");
        second.X = first.X + first.Width; Assert.True(new TopologyChartValidator().Validate(restored).IsValid);
        second.Y += 2; Assert.Contains(new TopologyChartValidator().Validate(restored).Errors, issue => issue.Code == "alignment-geometry"); second.Y -= 2;
        var parent = new TopologyGroup { Id = "scope", Label = "Scope", X = first.X, Y = first.Y, Width = 10, Height = 10 }; restored.Groups.Add(parent); first.GroupId = "scope";
        Assert.Contains(new TopologyChartValidator().Validate(restored).Errors, issue => issue.Code == "alignment-containment");
    }

    [Theory]
    [InlineData(0, "alignment-reference")] [InlineData(1, "alignment-duplicate-member")]
    [InlineData(2, "alignment-members")] [InlineData(3, "alignment-duplicate-id")]
    public void EditedNativeDeclarationsFailStructuredValidationBeforeAnyPlacement(int edit, string code) {
        var chart = Chart(); var declaration = new TopologyAlignment("row", TopologyAlignmentKind.Row, new[] { "first", "second" }); chart.Alignments.Add(declaration);
        if (edit == 0) declaration.NodeIds[0] = "missing";
        if (edit == 1) declaration.NodeIds[1] = "first";
        if (edit == 2) declaration.NodeIds.Clear();
        if (edit == 3) chart.Alignments.Add(new TopologyAlignment("row", TopologyAlignmentKind.Column, new[] { "second", "below" }));
        var error = Assert.Throws<TopologyValidationException>(() => chart.Prepare()); Assert.Contains(error.Result.Errors, issue => issue.Code == code);
        Assert.All(chart.Nodes, node => { Assert.Equal(0, node.X); Assert.Equal(0, node.Y); });
    }

    [Fact]
    public void TypedPortableValidationRejectsMissingDuplicateMembersAndGlobalIdentityCollisions() {
        var chart = Chart(); chart.Alignments.Add(new TopologyAlignment("row", TopologyAlignmentKind.Row, new[] { "first", "second" }));
        var json = chart.Prepare().ToInterchangeEnvelope().ToJson();
        foreach (var bad in new[] { "missing", "first" }) {
            var saved = VisualArtifactInterchangeEnvelope.FromJson(json); saved.Topology!.Alignments[0].NodeIds[1] = bad;
            Assert.Throws<ArgumentException>(() => saved.Validate());
        }
        var collision = VisualArtifactInterchangeEnvelope.FromJson(json); collision.Topology!.Alignments[0].Id = collision.Nodes[0].Id;
        Assert.Throws<ArgumentException>(() => collision.Validate());
        var duplicate = VisualArtifactInterchangeEnvelope.FromJson(json); duplicate.Topology!.Alignments.Add(duplicate.Topology!.Alignments[0]); Assert.Throws<ArgumentException>(() => duplicate.Validate());
    }

    [Fact]
    public void OperationalCapsCountFailedWorkAndDistinguishFeasibleZeroCyclesFromPositiveConflicts() {
        var budget = new TopologyAlignmentBudget("authored"); var axis = new TopologyAlignmentAxis(3, budget);
        axis.Add(0, 1, 0); axis.Add(1, 0, 0); axis.Add(1, 2, 12);
        Assert.Equal(new double[] { 0, 0, 12 }, axis.Solve()); var retained = axis.Count;
        axis.Add(2, 0, 1); Assert.Null(axis.Solve()); axis.Rollback(retained); Assert.Equal(new double[] { 0, 0, 12 }, axis.Solve());
        var work = new TopologyAlignmentBudget("authored"); work.Work(TopologyAlignmentBudget.MaximumWork);
        var error = Assert.Throws<TopologyAlignmentLimitException>(() => work.Work()); Assert.Equal("Topology.AlignmentWork", error.LimitName); Assert.Equal(8000001, error.Actual);
        var decisions = new TopologyAlignmentBudget("authored"); for (var i = 0; i < TopologyAlignmentBudget.MaximumDecisions; i++) decisions.Decision();
        var decision = Assert.Throws<TopologyAlignmentLimitException>(() => decisions.Decision()); Assert.Equal("Topology.AlignmentDecisions", decision.LimitName); Assert.Equal(4097, decision.Actual);
    }

    private static void Capture(PreparedTopology prepared, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); var path = Path.Combine(output, name); File.WriteAllText(path + ".svg", prepared.ToSvg());
        File.WriteAllBytes(path + ".native.png", prepared.ToPng()); File.WriteAllText(path + ".native.json", prepared.ToInterchangeEnvelope().ToJson());
    }
}
