using System.Xml.Linq;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyHealthRingTests {
    [Theory]
    [InlineData(VisualThemeMode.Light, TopologyHealthStatus.Healthy)]
    [InlineData(VisualThemeMode.Dark, TopologyHealthStatus.Warning)]
    [InlineData(VisualThemeMode.Light, TopologyHealthStatus.Critical)]
    public void RingUsesSemanticStatusIndependentlyOfAccentAndSurvivesPreparation(VisualThemeMode mode, TopologyHealthStatus status) {
        var chart = Diagram(mode, status);
        var options = new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false, IncludeStatusBadges = false };
        var prepared = chart.Prepare(options);
        var svg = XDocument.Parse(prepared.ToSvg());
        var ring = svg.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "topology-node-health-ring");
        var mark = ring.DescendantsAndSelf().Single(element => element.Name.LocalName == "ellipse");
        var expected = new TopologyPaint(chart.Theme, mode).StatusColor(status);
        Assert.Equal(expected, (string?)mark.Attribute("stroke"));
        Assert.NotEqual("#673AB7", (string?)mark.Attribute("stroke"));
        Assert.Contains("data-node-health-ring=\"true\"", prepared.ToSvg());
        var withRing = prepared.ToPng();
        chart.Nodes[0].ShowHealthRing = false;
        chart.Nodes[0].AccessibleName = "Changed source";
        Assert.Equal(withRing, prepared.ToPng());
        Assert.DoesNotContain("Changed source", prepared.ToSvg());
        Assert.NotEqual(withRing, chart.ToPng(options));
        Assert.DoesNotContain("topology-node-health-ring", chart.ToSvg(options));
    }

    [Fact]
    public void NodeAccessibleNameRetainsFullLabelAndCallerCanLocalizeIt() {
        var chart = Diagram(VisualThemeMode.Light, TopologyHealthStatus.Warning);
        chart.Nodes[0].Subtitle = "Last update 42 seconds ago";
        chart.Nodes[0].Badge = "LEAD";
        chart.Nodes[0].MaximumLabelCharacters = 3;
        XElement Node() => XDocument.Parse(chart.ToSvg()).Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "topology-node");
        Assert.Equal("Gateway service, Last update 42 seconds ago, LEAD, Warning", (string?)Node().Attribute("aria-label"));
        chart.Nodes[0].AccessibleName = "Gateway — synchronization delayed";
        Assert.Equal(chart.Nodes[0].AccessibleName, (string?)Node().Attribute("aria-label"));
    }

    private static TopologyChart Diagram(VisualThemeMode mode, TopologyHealthStatus status) {
        var chart = TopologyChart.Create().WithId("health-ring").WithViewport(240, 200)
            .WithTheme(VisualTheme.Graphite(), mode).WithLegend(null)
            .AddNode("gateway", "Gateway service", 60, 50, width: 80, height: 80, color: "#673AB7", status: status)
            .WithNodeHealthRing("gateway");
        chart.Nodes[0].Shape = TopologyNodeShape.Circle;
        chart.Nodes[0].DisplayMode = TopologyNodeDisplayMode.Icon;
        chart.Nodes[0].PreserveDisplayModeSize = true;
        return chart;
    }
}
