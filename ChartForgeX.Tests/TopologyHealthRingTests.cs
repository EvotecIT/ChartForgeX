using System.Xml.Linq;
using System.Text.Json;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
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

    [Fact]
    public void RingAndLocalizedAccessibleNameSurviveDirectAndPreparedInterchangeSnapshots() {
        const string name = "Brama — ostrzeżenie <42>";
        var chart = Diagram(VisualThemeMode.Dark, TopologyHealthStatus.Warning);
        chart.Nodes[0].AccessibleName = name;
        var prepared = chart.Prepare();
        var direct = chart.ToVisualArtifact().ToInterchangeEnvelope();
        chart.Nodes[0].ShowHealthRing = false;
        chart.Nodes[0].AccessibleName = "Changed source";
        foreach (var envelope in new[] { direct, prepared.ToInterchangeEnvelope() }) {
            var json = envelope.ToJson();
            var roundTrip = VisualArtifactInterchangeEnvelope.FromUtf8Json(envelope.ToUtf8Json());
            var node = Assert.Single(roundTrip.Nodes).Topology!;
            Assert.True(node.ShowHealthRing);
            Assert.Equal(name, node.AccessibleName);
            Assert.Equal(json, roundTrip.ToJson());
        }
        var returned = prepared.ToInterchangeEnvelope();
        returned.Nodes[0].Topology!.ShowHealthRing = false;
        returned.Nodes[0].Topology!.AccessibleName = "Changed snapshot";
        Assert.True(prepared.ToInterchangeEnvelope().Nodes[0].Topology!.ShowHealthRing);
        Assert.Equal(name, prepared.ToInterchangeEnvelope().Nodes[0].Topology!.AccessibleName);
    }

    [Fact]
    public void OlderInterchangeWithoutOptionalRingOrNameKeepsTheDefaults() {
        var chart = Diagram(VisualThemeMode.Light, TopologyHealthStatus.Unknown);
        chart.Nodes[0].ShowHealthRing = false;
        var json = chart.ToVisualArtifact().ToInterchangeJson();
        using var document = JsonDocument.Parse(json);
        var topology = document.RootElement.GetProperty("nodes")[0].GetProperty("topology");
        Assert.False(topology.TryGetProperty("showHealthRing", out _));
        Assert.False(topology.TryGetProperty("accessibleName", out _));
        var node = Assert.Single(VisualArtifactInterchangeEnvelope.FromJson(json).Nodes).Topology!;
        Assert.False(node.ShowHealthRing);
        Assert.Null(node.AccessibleName);
    }

    [Fact]
    public void InterchangeAccessibleNameUsesTheExistingTextBudgetInBothDirections() {
        var envelope = Diagram(VisualThemeMode.Light, TopologyHealthStatus.Healthy).ToVisualArtifact().ToInterchangeEnvelope();
        var name = new string('x', VisualArtifactInterchangeValidation.MaximumTextCharacters);
        envelope.Nodes[0].Topology!.AccessibleName = name;
        var json = envelope.ToJson();
        Assert.Equal(name, VisualArtifactInterchangeEnvelope.FromJson(json).Nodes[0].Topology!.AccessibleName);
        envelope.Nodes[0].Topology!.AccessibleName = name + "x";
        Assert.Throws<ArgumentException>(() => envelope.ToJson());
        Assert.Throws<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(json.Replace(name, name + "x", StringComparison.Ordinal)));
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
