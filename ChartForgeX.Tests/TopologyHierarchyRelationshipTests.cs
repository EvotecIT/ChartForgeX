using System.Xml.Linq;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyHierarchyRelationshipTests {
    [Fact]
    public void ParentEdgeLabelsAndHealthReachStaticAndBrowserContracts() {
        var chart = TopologyChart.Create().WithViewport(640, 480).AddHierarchy(new[] {
            new TopologyHierarchyItem("reference", "Reference") { Status = TopologyHealthStatus.Healthy, ParentEdgeLabel = "No incoming edge" },
            new TopologyHierarchyItem("relay", "Relay", "reference") { ParentEdgeLabel = "+0.2 ms < 1 ms", Badge = "REF", ShowHealthRing = true },
            new TopologyHierarchyItem("leaf", "Leaf", "relay") { ParentEdgeLabel = "+84 ms", ParentEdgeStatus = TopologyHealthStatus.Warning, Status = TopologyHealthStatus.Warning }
        }, new TopologyHierarchyOptions { EdgeStatus = TopologyHealthStatus.Healthy });
        var relayEdge = chart.Edges.Single(edge => edge.TargetNodeId == "relay");
        var leafEdge = chart.Edges.Single(edge => edge.TargetNodeId == "leaf");
        Assert.Equal("+0.2 ms < 1 ms", relayEdge.Label);
        Assert.Equal(TopologyHealthStatus.Healthy, relayEdge.Status);
        Assert.Equal(TopologyHealthStatus.Warning, leafEdge.Status);
        Assert.Equal(VisualLinkDirection.Forward, leafEdge.Direction);
        Assert.Equal("REF", chart.Nodes.Single(node => node.Id == "relay").Badge);
        Assert.True(chart.Nodes.Single(node => node.Id == "relay").ShowHealthRing);
        var svg = chart.ToSvg();
        Assert.DoesNotContain("<script", svg, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("No incoming edge", svg);
        Assert.Contains("+0.2 ms &lt; 1 ms", svg);
        Assert.Contains("+84 ms", svg);
        Assert.NotEmpty(XDocument.Parse(svg).Descendants());
        var html = new HtmlInteractiveTopologyRenderer().RenderPage(chart);
        Assert.Contains("data-edge-label=\"+84 ms\"", html);
        Assert.Contains("cfx-topology-select", html);
        Assert.True(chart.ToPng().Length > 100);
    }

    [Fact]
    public void FilteringTreeKeepsTheOriginalIncomingRelationshipOnVisibleNodes() {
        var chart = TopologyChart.Create().AddHierarchy(new[] {
            new TopologyHierarchyItem("root", "Reference"),
            new TopologyHierarchyItem("relay", "Relay", "root") { ParentEdgeLabel = "+2 ms" },
            new TopologyHierarchyItem("leaf", "Leaf", "relay") { ParentEdgeLabel = "+9 ms" }
        }, new TopologyHierarchyOptions { MaxLevel = 1 });
        Assert.Equal("+2 ms", Assert.Single(chart.Edges).Label);
        Assert.DoesNotContain(chart.Nodes, node => node.Id == "leaf");
    }
}
