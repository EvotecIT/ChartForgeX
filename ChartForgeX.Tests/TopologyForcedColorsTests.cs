using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>
/// A topology can keep its status marks in their colours in forced-colours mode, as charts can, without pinning the node
/// and group cards whose labels sit on the canvas.
/// </summary>
public sealed class TopologyForcedColorsTests {
    [Fact]
    public void PinStateColorsInForcedColors_IsOptIn() {
        Assert.DoesNotContain("forced-color-adjust", Diagram().ToSvg(), StringComparison.Ordinal);
        Assert.False(new TopologyRenderOptions().PinStateColorsInForcedColors);

        var pinned = Diagram().ToSvg("report", new TopologyRenderOptions().WithStateColorsPinnedInForcedColors());
        var rule = Regex.Match(pinned, @"(?<selectors>[^{}]*)\{forced-color-adjust:none\}");
        Assert.True(rule.Success);
        var selectors = rule.Groups["selectors"].Value.Split(',').Select(selector => selector.Trim()).ToArray();
        // Status leaves, edges, status badges, callout chips, and this render's markers; never a whole node or group.
        Assert.Contains("[id=\"report-sites\"] [data-cfx-status]:not(g):not([class$=\"__geo-region-hull\"])", selectors);
        Assert.Contains("[id=\"report-sites\"] [data-cfx-role=topology-edge]", selectors);
        Assert.Contains("[id=\"report-sites\"] [data-cfx-role=topology-node-status]", selectors);
        Assert.Contains("marker[id^=\"report-sites-arrow-\"]", selectors);
        Assert.DoesNotContain("[id=\"report-sites\"] [data-cfx-status]", selectors);

        var document = XDocument.Parse(pinned);
        var markers = document.Descendants().Where(element => element.Name.LocalName == "marker").Select(element => (string)element.Attribute("id")!).ToArray();
        Assert.NotEmpty(markers);
        Assert.All(markers, id => Assert.Matches("^report-sites-(arrow|circle|diamond)-", id));
        var statusLeaves = document.Descendants().Where(element => element.Attribute("data-cfx-status") != null && element.Name.LocalName != "g").ToArray();
        Assert.Contains(statusLeaves, element => element.Parent != null && (string?)element.Parent.Attribute("data-cfx-role") == "topology-legend-item");

        Assert.Equal(Diagram().ToPng(), Diagram().ToPng(new TopologyRenderOptions().WithStateColorsPinnedInForcedColors()));
    }

    [Fact]
    public void PinStateColorsInForcedColors_WritesTheRuleWithoutTheRestOfTheCss() {
        var svg = Diagram().ToSvg(new TopologyRenderOptions { IncludeCss = false, PinStateColorsInForcedColors = true });
        Assert.Contains("{forced-color-adjust:none}", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("font-synthesis", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("<style", Diagram().ToSvg(new TopologyRenderOptions { IncludeCss = false }), StringComparison.Ordinal);
    }

    private static TopologyChart Diagram() => TopologyChart.Create().WithId("sites").WithViewport(720, 420)
        .AddAutoGroup("waw", "Warsaw", TopologyHealthStatus.Warning)
        .AddAutoNode("dc1", "DC01", TopologyNodeKind.Server, TopologyHealthStatus.Healthy, groupId: "waw")
        .AddAutoNode("dc2", "DC02", TopologyNodeKind.Server, TopologyHealthStatus.Critical, groupId: "waw")
        .AddAutoNode("dc3", "DC03", TopologyNodeKind.Server, TopologyHealthStatus.Warning)
        .AddEdge("a", "dc1", "dc2", status: TopologyHealthStatus.Critical, direction: VisualLinkDirection.Forward)
        .AddEdge("b", "dc2", "dc3", status: TopologyHealthStatus.Warning, direction: VisualLinkDirection.Bidirectional)
        .WithLegend(TopologyLegend.Create("Health").AddStatus("Healthy", TopologyHealthStatus.Healthy).AddStatus("Critical", TopologyHealthStatus.Critical));
}
