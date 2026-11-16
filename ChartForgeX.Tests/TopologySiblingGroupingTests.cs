using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;
using Xunit;
using static ChartForgeX.Tests.TopologyEdgePresentationTests;

namespace ChartForgeX.Tests;

public sealed class TopologySiblingGroupingTests {
    [Fact]
    public void SameKindLeavesUnderOneParentBecomeOneCardWithMemberRows() {
        var chart = Site(5).WithSiblingGrouping(titleFormatter: (kind, members) => "Domain controllers");
        chart.Nodes.Single(node => node.Id == "dc2").Badge = "PDC";
        chart.Nodes.Single(node => node.Id == "dc3").Status = TopologyHealthStatus.Warning;
        var prepared = chart.Prepare(Options());
        Assert.Equal(3, prepared.NodeCount);
        Assert.Equal(2, prepared.EdgeCount);
        var document = XDocument.Parse(prepared.ToSvg());
        var card = Assert.Single(document.Descendants(), element => Role(element) == "topology-node" && element.Attribute("data-sibling-count") != null);
        Assert.Equal("5", (string?)card.Attribute("data-sibling-count"));
        Assert.Equal("dc1 dc2 dc3 dc4 dc5", (string?)card.Attribute("data-sibling-member-ids"));
        Assert.Equal("Domain controllers", (string?)card.Attribute("data-node-label"));
        Assert.Equal("Warning", (string?)card.Attribute("data-cfx-status"));
        var rows = card.Descendants().Where(element => Role(element) == "topology-sibling-row").ToArray();
        Assert.Equal(new[] { "dc1", "dc2", "dc3", "dc4", "dc5" }, rows.Select(row => (string?)row.Attribute("data-node-id")));
        Assert.Equal("10.0.0.1", (string?)rows[0].Attribute("data-node-subtitle"));
        Assert.Single(rows[1].Descendants(), element => Role(element) == "topology-sibling-chip-surface");
        var surface = Rect(card.Elements().First(element => Role(element) == "topology-node-surface"));
        Assert.Equal(TopologySiblingCard.HeaderHeight + 5 * TopologySiblingCard.RowHeight + TopologySiblingCard.BottomPadding, surface.Height, 3);
        // Every row sits inside the card below its header.
        Assert.All(rows.SelectMany(row => row.Descendants()).Where(element => element.Name.LocalName == "rect").Select(Rect),
            rect => Assert.True(rect.Y >= surface.Y + TopologySiblingCard.HeaderHeight - 1 && rect.Bottom <= surface.Bottom && rect.Right <= surface.Right));
        Assert.DoesNotContain(document.Descendants(), element => Role(element) == "topology-sibling-toggle");
        var edge = Assert.Single(document.Descendants(), element => Role(element) == "topology-edge" && (string?)element.Attribute("data-target-node-id") == (string?)card.Attribute("data-node-id"));
        Assert.Equal("hub", (string?)edge.Attribute("data-source-node-id"));
        Assert.Equal(5, chart.Nodes.Count(node => node.Kind == TopologyNodeKind.Server));
    }

    [Fact]
    public void ThresholdKindAndNamedNodesDecideWhatIsGrouped() {
        Assert.Equal(7, Site(5).WithSiblingGrouping(minimumCount: 6).Prepare(Options()).NodeCount);
        Assert.Equal(7, Site(5).Prepare(Options()).NodeCount);

        var mixed = Site(4).WithSiblingGrouping();
        mixed.Nodes.Single(node => node.Id == "dc4").Kind = TopologyNodeKind.Database;
        Assert.Equal(4, mixed.Prepare(Options()).NodeCount);

        var named = Site(3).WithSiblingGrouping();
        named.Scenarios.Add(new TopologyScenario { Id = "path", Label = "Path" });
        named.Scenarios[0].Steps.Add(new TopologyScenarioStep { Id = "dc1", Kind = TopologyScenarioStepKind.Node });
        Assert.Equal(5, named.Prepare(Options()).NodeCount);

        var routed = Site(3).WithSiblingGrouping();
        routed.Scenarios.Add(new TopologyScenario { Id = "replication", Label = "Replication" });
        routed.Scenarios[0].Steps.Add(new TopologyScenarioStep { Id = "e1", Kind = TopologyScenarioStepKind.Edge });
        var kept = routed.Prepare(Options());
        Assert.Equal(5, kept.NodeCount);
        Assert.Contains(XDocument.Parse(kept.ToSvg()).Descendants(), element => Role(element) == "topology-edge" && (string?)element.Attribute("data-edge-id") == "e1");

        var linked = Site(3).WithSiblingGrouping().AddEdge("peer", "dc1", "dc2");
        Assert.Equal(5, linked.Prepare(Options()).NodeCount);
    }

    [Fact]
    public void LongMemberListsSummariseTheRowsBeyondTheLimit() {
        var prepared = Site(12).WithSiblingGrouping(maximumRows: 8).Prepare(Options());
        var card = XDocument.Parse(prepared.ToSvg()).Descendants().Single(element => element.Attribute("data-sibling-count") != null);
        Assert.Equal(7, card.Descendants().Count(element => Role(element) == "topology-sibling-row"));
        Assert.Single(card.Descendants(), element => Role(element) == "topology-sibling-more");
        Assert.Equal("12", (string?)card.Attribute("data-sibling-count"));
    }

    [Fact]
    public void LargeSetsStartCollapsedToTheirHeaderAndCount() {
        var prepared = Site(40).WithSiblingGrouping().Prepare(Options());
        var card = XDocument.Parse(prepared.ToSvg()).Descendants().Single(element => element.Attribute("data-sibling-count") != null);
        Assert.Equal("false", (string?)card.Attribute("data-sibling-expanded"));
        var surface = Rect(card.Elements().First(element => Role(element) == "topology-node-surface"));
        Assert.Equal(TopologySiblingCard.HeaderHeight, surface.Height, 3);
        var rows = card.Descendants().Single(element => Role(element) == "topology-sibling-rows");
        Assert.Equal("0", (string?)rows.Attribute("opacity"));
        Assert.Equal(7, rows.Descendants().Count(element => Role(element) == "topology-sibling-row"));
        Assert.Equal(TopologySiblingCard.HeaderHeight + 8 * TopologySiblingCard.RowHeight + TopologySiblingCard.BottomPadding,
            double.Parse((string)card.Attribute("data-sibling-expanded-height")!, System.Globalization.CultureInfo.InvariantCulture), 3);
        Assert.Equal("true", (string?)XDocument.Parse(Site(40).WithSiblingGrouping(collapseCount: 0).Prepare(Options()).ToSvg()).Descendants()
            .Single(element => element.Attribute("data-sibling-count") != null).Attribute("data-sibling-expanded"));
    }

    [Theory]
    [InlineData(TopologyLayoutDirection.TopToBottom)]
    [InlineData(TopologyLayoutDirection.LeftToRight)]
    public void ManyChildrenStaggerIntoTwoOffsetRowsWithClearLinks(TopologyLayoutDirection direction) {
        var chart = Site(12, direction).WithSiblingGrouping(minimumCount: 50);
        var document = XDocument.Parse(chart.Prepare(Options()).ToSvg());
        var vertical = direction == TopologyLayoutDirection.TopToBottom;
        var children = document.Descendants().Where(element => Role(element) == "topology-node" && ((string?)element.Attribute("data-node-id"))!.StartsWith("dc", StringComparison.Ordinal))
            .Select(element => (Id: (string)element.Attribute("data-node-id")!, Row: (string?)element.Attribute("data-cfx-meta-siblinggroup-staggerrow"),
                Box: Rect(element.Elements().First(child => Role(child) == "topology-node-surface")))).ToArray();
        Assert.Equal(12, children.Length);
        Assert.Equal(6, children.Count(child => child.Row == "0"));
        Assert.Equal(6, children.Count(child => child.Row == "1"));
        double Rank(ChartRect box) => vertical ? box.Y : box.X;
        double Along(ChartRect box) => vertical ? box.X + box.Width / 2 : box.Y + box.Height / 2;
        Assert.Single(children.Where(child => child.Row == "0").Select(child => Math.Round(Rank(child.Box))).Distinct());
        Assert.Single(children.Where(child => child.Row == "1").Select(child => Math.Round(Rank(child.Box))).Distinct());
        // Consecutive children alternate rows and sit half a slot apart, so the far row looks through the gaps.
        var ordered = children.OrderBy(child => Along(child.Box)).ToArray();
        Assert.All(ordered.Skip(1).Select((child, index) => (child, previous: ordered[index])), pair => Assert.NotEqual(pair.previous.Row, pair.child.Row));
        foreach (var a in children) Assert.DoesNotContain(children, b => b.Id != a.Id && Overlaps(a.Box, b.Box));
        foreach (var edge in document.Descendants().Where(element => Role(element) == "topology-edge")) {
            var target = (string?)edge.Attribute("data-target-node-id");
            var route = Points((string?)edge.Descendants().First(element => Role(element) == "topology-edge-line").Attribute("d"));
            foreach (var other in children.Where(child => child.Id != target))
                Assert.False(Crosses(route, other.Box), target + " crosses " + other.Id);
        }
    }

    [Fact]
    public void InteractiveCardsCarryTheToggleAndCollapsedGeometry() {
        var html = Site(4).WithSiblingGrouping().ToInteractiveHtmlFragment(Options());
        Assert.Contains("data-cfx-role=\"topology-sibling-toggle\"", html);
        Assert.Contains("data-sibling-collapsed-height=\"36\"", html);
        Assert.Contains("data-sibling-expanded=\"true\"", html);
        Assert.Contains("cfx-topology-sibling-toggle", html);
    }

    private static TopologyChart Site(int controllers, TopologyLayoutDirection direction = TopologyLayoutDirection.TopToBottom) {
        var chart = TopologyChart.Create().WithId("site").WithViewport(1200, 700, 30).WithLayout(TopologyLayoutMode.Layered, direction).WithLegend(null)
            .AddAutoNode("hub", "London", TopologyNodeKind.Location, TopologyHealthStatus.Healthy)
            .AddAutoNode("par", "Paris", TopologyNodeKind.Location, TopologyHealthStatus.Healthy)
            .AddEdge("link", "hub", "par", kind: TopologyEdgeKind.Replication);
        for (var index = 1; index <= controllers; index++)
            chart.AddAutoNode("dc" + index, "LON-DC" + index.ToString("00", System.Globalization.CultureInfo.InvariantCulture), TopologyNodeKind.Server,
                TopologyHealthStatus.Healthy, subtitle: "10.0.0." + index, symbol: "DC")
                .AddEdge("e" + index, "hub", "dc" + index, kind: TopologyEdgeKind.Replication);
        return chart;
    }

    private static bool Crosses(ChartPoint[] route, ChartRect box) {
        var inner = new ChartRect(box.X + 1, box.Y + 1, box.Width - 2, box.Height - 2);
        for (var index = 1; index < route.Length; index++) {
            var a = route[index - 1]; var b = route[index];
            for (var step = 0; step <= 40; step++) {
                var x = a.X + (b.X - a.X) * step / 40; var y = a.Y + (b.Y - a.Y) * step / 40;
                if (x > inner.X && x < inner.Right && y > inner.Y && y < inner.Bottom) return true;
            }
        }
        return false;
    }
}
