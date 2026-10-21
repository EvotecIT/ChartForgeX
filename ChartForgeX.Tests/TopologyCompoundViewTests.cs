using System.Xml.Linq;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Group shells remain endpoints, rather than synthetic nodes, through native views.</summary>
public sealed class TopologyCompoundViewTests {
    [Theory]
    [InlineData("edge", false)]
    [InlineData("edge", true)]
    [InlineData("kind", false)]
    [InlineData("kind", true)]
    [InlineData("status", false)]
    [InlineData("status", true)]
    [InlineData("node", false)]
    [InlineData("node", true)]
    [InlineData("node-kind", false)]
    [InlineData("node-kind", true)]
    public void SelectorsRetainEligibleShellRelationships(string selector, bool reverse) {
        var view = new TopologyView();
        switch (selector) {
            case "edge": view.EdgeIds.Add("e"); break;
            case "kind": view.EdgeKinds.Add(TopologyEdgeKind.Dependency); break;
            case "status": view.HealthStatuses.Add(TopologyHealthStatus.Warning); break;
            case "node": view.NodeIds.Add("b"); break;
            case "node-kind": view.NodeKinds.Add(TopologyNodeKind.Network); break;
        }
        var prepared = Fixture(reverse).Prepare(Options(view));
        var facts = prepared.ToInterchangeEnvelope();
        var edge = Assert.Single(facts.Edges);
        Assert.Equal("e", edge.Id);
        Assert.Equal(reverse ? "b" : "g", edge.SourceId);
        Assert.Equal(reverse ? "g" : "b", edge.TargetId);
        Assert.Contains(facts.Groups, group => group.Id == "g");
        Assert.Contains(facts.Nodes, node => node.Id == "b");
        Assert.DoesNotContain(facts.Nodes, node => node.Id == "g");
        if (selector is "node" or "node-kind" or "status") Assert.Single(facts.Nodes);
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "topology-edge");
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(false, true, false, 1, true)]
    [InlineData(false, false, true, 1, false)]
    [InlineData(true, false, true, 1, true)]
    [InlineData(true, true, false, 1, false)]
    [InlineData(false, true, true, 0, false)]
    public void NodeFocusTraversesShellEndpointsInTheSelectedDirection(bool reverse, bool incoming, bool outgoing, int depth, bool retained) {
        var view = TopologyView.AroundNode("b", depth);
        view.IncludeIncomingEdges = incoming;
        view.IncludeOutgoingEdges = outgoing;
        var facts = Fixture(reverse).Prepare(Options(view)).ToInterchangeEnvelope();
        Assert.Equal(retained ? new[] { "e" } : Array.Empty<string>(), facts.Edges.Select(edge => edge.Id));
        Assert.Equal(new[] { "b" }, facts.Nodes.Select(node => node.Id));
        Assert.DoesNotContain(facts.Nodes, node => node.Id == "g");
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    public void FocusDepthCountsShellHopsWithoutIncludingUnrelatedMembers(int depth, int edgeCount) {
        var facts = Fixture().Prepare(Options(TopologyView.AroundNode("b", depth))).ToInterchangeEnvelope();
        Assert.Equal(edgeCount, facts.Edges.Count);
        Assert.Equal(edgeCount, facts.Nodes.Count);
        Assert.DoesNotContain(facts.Nodes, node => node.Id == "a");
        Assert.Contains(facts.Groups, group => group.Id == "g");
    }

    [Fact]
    public void StatusSelectedShellToShellEdgeRestoresOnlyItsEndpointGroups() {
        var chart = TopologyChart.Create().WithLayout(TopologyLayoutMode.Layered)
            .AddAutoGroup("root", "Parent", TopologyHealthStatus.Healthy)
            .AddAutoGroup("g", "Child", TopologyHealthStatus.Healthy)
            .AddAutoGroup("h", "Other", TopologyHealthStatus.Healthy)
            .AddAutoNode("a", "A", status: TopologyHealthStatus.Healthy, groupId: "g")
            .AddAutoNode("b", "B", status: TopologyHealthStatus.Healthy, groupId: "h")
            .AddAutoNode("z", "Visible", status: TopologyHealthStatus.Warning)
            .AddEdge("e", "g", "h", status: TopologyHealthStatus.Warning);
        chart.Groups[1].ParentGroupId = "root";
        var view = new TopologyView { IncludeNodeGroups = false };
        view.HealthStatuses.Add(TopologyHealthStatus.Warning);
        var prepared = chart.Prepare(Options(view));
        var facts = prepared.ToInterchangeEnvelope();
        Assert.Equal(new[] { "z" }, facts.Nodes.Select(node => node.Id));
        Assert.Equal(new[] { "g", "h" }, facts.Groups.Select(group => group.Id));
        Assert.Null(facts.Groups[0].Topology!.ParentGroupId);
        var edge = Assert.Single(facts.Edges);
        Assert.Equal(("g", "h"), (edge.SourceId, edge.TargetId));
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void FilteringAnAuthoredNodeNeverRetargetsItsEdgeToASameIdShell() {
        var chart = SameIdFixture();
        var view = new TopologyView { NodeIds = { "b" } };
        var facts = chart.Prepare(Options(view)).ToInterchangeEnvelope();
        Assert.Empty(facts.Edges);
        Assert.Equal(new[] { "b" }, facts.Nodes.Select(node => node.Id));
    }

    [Fact]
    public void NodeEdgesDoNotRestoreUnselectedSameIdShells() {
        var chart = SameIdFixture();
        var view = new TopologyView { IncludeNodeGroups = false, HealthStatuses = { TopologyHealthStatus.Warning } };
        var facts = chart.Prepare(Options(view)).ToInterchangeEnvelope();
        Assert.Equal(new[] { "g", "b" }, facts.Nodes.Select(node => node.Id));
        Assert.Single(facts.Edges);
        Assert.Empty(facts.Groups);

        chart.AddAutoGroup("selected", "Selected");
        chart.Nodes[0].GroupId = "selected";
        chart.Nodes[1].GroupId = "selected";
        view = new TopologyView { GroupIds = { "selected" } };
        facts = chart.Prepare(Options(view)).ToInterchangeEnvelope();
        Assert.Single(facts.Edges);
        Assert.Equal("selected", Assert.Single(facts.Groups).Id);
    }

    [Fact]
    public void GroupScopeAndConnectedEdgePolicyStillExcludeOutsideRelationships() {
        var chart = Fixture();
        var facts = chart.Prepare(Options(new TopologyView { GroupIds = { "g" } })).ToInterchangeEnvelope();
        Assert.Empty(facts.Edges);
        Assert.Equal(new[] { "a" }, facts.Nodes.Select(node => node.Id));
        facts = chart.Prepare(Options(new TopologyView { NodeIds = { "b" }, IncludeConnectedEdges = false })).ToInterchangeEnvelope();
        Assert.Empty(facts.Edges);
    }

    private static TopologyRenderOptions Options(TopologyView view) => new() { View = view, IncludeLegend = false };

    private static TopologyChart Fixture(bool reverse = false) => TopologyChart.Create().WithLayout(TopologyLayoutMode.Layered)
        .AddAutoGroup("g", "Group", TopologyHealthStatus.Healthy)
        .AddAutoNode("a", "Member", TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "g")
        .AddAutoNode("b", "Outside", TopologyNodeKind.Network, TopologyHealthStatus.Warning)
        .AddAutoNode("c", "Other", TopologyNodeKind.Server, TopologyHealthStatus.Healthy)
        .AddEdge("e", reverse ? "b" : "g", reverse ? "g" : "b", "relation", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning)
        .AddEdge("other", "g", "c", kind: TopologyEdgeKind.Connectivity, status: TopologyHealthStatus.Healthy);

    private static TopologyChart SameIdFixture() => TopologyChart.Create().WithLayout(TopologyLayoutMode.Layered)
        .AddAutoGroup("g", "Shell", TopologyHealthStatus.Healthy)
        .AddAutoNode("g", "Authored node", status: TopologyHealthStatus.Warning)
        .AddAutoNode("b", "Outside", status: TopologyHealthStatus.Warning)
        .AddAutoNode("a", "Member", status: TopologyHealthStatus.Healthy, groupId: "g")
        .AddEdge("e", "g", "b", status: TopologyHealthStatus.Warning);
}
