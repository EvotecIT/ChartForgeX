using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidArchitectureCausalTests {
    [Fact]
    public void ArchitectureNestedParentsReachTheExistingNativeHierarchy() {
        var parsed = new MermaidParser().ParseArchitecture("architecture-beta\ngroup outer[Outer]\ngroup inner[Inner] in outer\nservice api[API] in inner");
        Assert.Empty(parsed.Diagnostics);
        var chart = parsed.Document!.ToTopologyChart();
        Assert.Equal("outer", chart.Groups.Single(group => group.Id == "inner").ParentGroupId);
    }

    [Fact]
    public void ArchitectureAuthoredEndpointSidesReachExistingNativePorts() {
        var parsed = new MermaidParser().ParseArchitecture("architecture-beta\nservice a[A]\nservice b[B]\na:B --> T:b");
        Assert.Empty(parsed.Diagnostics);
        var edge = Assert.Single(parsed.Document!.ToTopologyChart().Edges);
        Assert.Equal(TopologyEdgePort.Bottom, edge.SourcePort); Assert.Equal(TopologyEdgePort.Top, edge.TargetPort);
    }

    [Fact]
    public void ArchitectureBoundaryModifierRoutesThroughTheActualGroupWithoutFabricatedNodes() {
        var parsed = new MermaidParser().ParseArchitecture("architecture-beta\ngroup scope[Scope]\nservice a[A] in scope\nservice b[B]\na{group}:R --> L:b");
        Assert.Empty(parsed.Diagnostics);
        var chart = parsed.Document!.ToTopologyChart();
        Assert.Equal("scope", Assert.Single(chart.Edges).SourceNodeId);
        Assert.Equal(2, chart.Nodes.Count);
    }
}
