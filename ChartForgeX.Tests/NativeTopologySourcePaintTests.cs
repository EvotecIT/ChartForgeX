using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeTopologySourcePaintTests {
    [Fact]
    public void NodeGroupCatalogAndDetailVariablesRetainTheirFallbackAlphaAndHighlightPolicy() {
        var variable = Create(true);
        var literal = Create(false);
        var prepared = variable.Chart.Prepare(variable.Options);
        var svg = XDocument.Parse(prepared.ToSvg());
        XElement Entity(string role, string id) => svg.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == role
            && (string?)element.Attribute(role == "topology-group" ? "data-group-id" : "data-node-id") == id);
        XElement Mark(XElement owner, string role) => owner.Descendants().First(element => (string?)element.Attribute("data-cfx-role") == role);
        var card = Entity("topology-node", "card");
        var cardSurface = Mark(card, "topology-node-surface");
        Assert.Contains("var(--node,", (string?)cardSurface.Attribute("stroke"));
        Assert.Contains("50%", (string?)cardSurface.Attribute("stroke"));
        Assert.Contains("var(--background,", (string?)cardSurface.Attribute("fill"));
        var detail = card.Descendants().Single(element => element.Name.LocalName == "text" && element.Value == "Status Ready");
        Assert.Contains("var(--detail,", (string?)detail.Attribute("fill"));
        Assert.Contains("50%", (string?)detail.Attribute("fill"));
        var group = Mark(Entity("topology-group", "region"), "topology-group-surface");
        Assert.Contains("var(--group,", (string?)group.Attribute("stroke"));
        Assert.Contains("25%", (string?)group.Attribute("stroke"));
        var catalog = Entity("topology-node", "catalog");
        Assert.Contains(catalog.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "topology-icon-surface"
            && ((string?)element.Attribute("fill"))?.Contains("var(--icon,") == true);
        var tinted = Entity("topology-node", "tinted");
        Assert.Contains("color-mix(in srgb,", (string?)Mark(tinted, "topology-node-surface").Attribute("fill"));
        Assert.Contains("var(--tint,", (string?)Mark(tinted, "topology-node-surface").Attribute("fill"));
        Assert.Equal(literal.Chart.ToPng(literal.Options), prepared.ToPng());
        Assert.Equal("var(--node, #CC224480)", prepared.ToInterchangeEnvelope().Nodes.Single(node => node.Id == "card").Color);
    }

    private static (TopologyChart Chart, TopologyRenderOptions Options) Create(bool variables) {
        string Paint(string name, string fallback) => variables ? "var(--" + name + ", " + fallback + ")" : fallback;
        var icon = new TopologyIconDefinition("source-paint", "service", "Service", TopologyNodeKind.Service, TopologyIconShape.Application) {
            Color = Paint("icon", "#22BB4480")
        };
        var catalog = TopologyIconCatalog.Default().AddPack(new TopologyIconPack("source-paint", "Source paint").AddIcon(icon));
        var chart = TopologyChart.Create().WithViewport(920, 440, 20).WithLegend(null)
            .AddGroup("region", "Region", 30, 30, 850, 350, color: Paint("group", "#2244CC80"))
            .AddNode("card", "Card", 60, 120, groupId: "region", width: 190, height: 110,
                color: Paint("node", "#CC224480"), backgroundColor: Paint("background", "#11223380"))
            .AddNodeDetail("card", "Status", "Ready", color: Paint("detail", "#4422CC80"))
            .AddNode("catalog", "Catalog", 290, 120, groupId: "region", width: 150, height: 80, iconId: "source-paint:service")
            .AddNode("tinted", "Tinted", 480, 120, groupId: "region", width: 150, height: 80, color: Paint("tint", "#BB442280"))
            .AddNode("focus", "Focus", 700, 120, groupId: "region", width: 100, height: 60);
        var options = new TopologyRenderOptions { IncludeLegend = false, IconCatalog = catalog,
            NodeSurfaceStyle = TopologyNodeSurfaceStyle.Tinted, GroupSurfaceStyle = TopologyGroupSurfaceStyle.Tinted, DimmedOpacity = .5 };
        options.HighlightNodeIds.Add("focus");
        return (chart, options);
    }
}
