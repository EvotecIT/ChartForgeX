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
        var document = XDocument.Parse(pinned);
        var groups = document.Descendants().Where(element => (string?)element.Attribute("data-cfx-pin-state-colors") == "true").ToArray();
        Assert.NotEmpty(groups);
        Assert.All(groups, group => Assert.Equal("forced-color-adjust:none", (string?)group.Attribute("style")));
        Assert.Contains(groups, group => group.Descendants().Any(element => (string?)element.Attribute("data-cfx-role") == "topology-marker"));
        Assert.Contains(groups, group => group.Descendants().Any(element => (string?)element.Attribute("data-cfx-role") == "topology-status"));
        Assert.Contains(groups, group => group.Ancestors().Any(element => (string?)element.Attribute("data-cfx-role") == "topology-legend-item"));
        // Pin only status ink; card labels and surfaces remain available to forced-color adaptation.
        Assert.DoesNotContain(groups.SelectMany(group => group.Descendants()), element =>
            (string?)element.Attribute("data-cfx-role") == "topology-node" || (string?)element.Attribute("data-cfx-role") == "topology-group");

        Assert.Equal(Diagram().ToPng(), Diagram().ToPng(new TopologyRenderOptions().WithStateColorsPinnedInForcedColors()));
    }

    [Fact]
    public void PinStateColorsInForcedColors_UsesBoundedGroupsWithoutStylesheets() {
        var svg = Diagram().ToSvg(new TopologyRenderOptions { IncludeCss = false, PinStateColorsInForcedColors = true });
        Assert.Contains("style=\"forced-color-adjust:none\"", svg, StringComparison.Ordinal);
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
