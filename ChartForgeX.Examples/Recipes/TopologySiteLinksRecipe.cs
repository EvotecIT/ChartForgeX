using ChartForgeX.Core;
using ChartForgeX.Themes;
using ChartForgeX.Topology;

/// <summary>Authors an Active Directory style site map with grouped domain controllers and annotated site links.</summary>
/// <remarks>Shows sibling grouping, striped transports, value chips, uplink markers and assumed links in one diagram.</remarks>
public static class TopologySiteLinksRecipe {
    /// <summary>Creates the site map in either theme mode.</summary>
    public static TopologyChart Create(string variant = "sites", ThemeMode mode = ThemeMode.Light) {
        // "wide" is the shared recipe registry default; this example has a single wide site map.
        if (variant is not ("sites" or "wide")) throw new ArgumentOutOfRangeException(nameof(variant));
        var chart = TopologyChart.Create().Id("ad-site-links").Title("Contoso replication sites")
            .Subtitle("Domain controllers grouped per site; links show transports, cost and evidence")
            .Theme(VisualTheme.Graphite(), mode).Viewport(new TopologyViewport(1360, 720, 24))
            .LayoutMode(TopologyLayoutMode.Layered).LayoutDirection(TopologyLayoutDirection.TopToBottom)
            .SiblingGrouping(new TopologySiblingGrouping { TitleFormatter = (kind, members) => "Domain controllers" });
        chart.DefaultRenderOptions(new TopologyRenderOptions { FitContentToViewport = true, NodeDisplayMode = TopologyNodeDisplayMode.Card });
        chart.Legend(TopologyLegend.Create("Legend")
            .AddStatus("Healthy", TopologyHealthStatus.Healthy).AddStatus("Warning", TopologyHealthStatus.Warning)
            .AddEdgeKind("Site link", TopologyEdgeKind.Replication));

        chart.AddAutoNode("hub", "London", TopologyNodeKind.Location, TopologyHealthStatus.Healthy, subtitle: "Hub site");
        chart.Nodes[0].Metadata["layer"] = "0";
        AddControllers(chart, "hub", "LON", "10.10.0.", 6, "1");
        chart.Nodes.Single(node => node.Id == "hub-dc1").Badge = "PDC";
        chart.Nodes.Single(node => node.Id == "hub-dc2").Badge = "GC";
        chart.Nodes.Single(node => node.Id == "hub-dc5").Status = TopologyHealthStatus.Warning;

        Site(chart, "par", "Paris", 3, "10.20.0.");
        Site(chart, "ber", "Berlin", 1, "10.30.0.");
        Site(chart, "nyc", "New York", 2, "10.40.0.");
        Site(chart, "tyo", "Tokyo", 1, "10.50.0.");

        chart.AddEdge("hub-par", "hub", "par", kind: TopologyEdgeKind.Replication, status: TopologyHealthStatus.Healthy)
            .AddEdgeStripe("hub-par", "#2563EB", "IP").AddEdgeStripe("hub-par", "#F97316", "SMTP")
            .AddEdgeChip("hub-par", "cost 100").UpdateEdge("hub-par", e => e.Evidence(TopologyEdgeEvidence.Confirmed).EvidenceText("Site link object DEFAULTIPSITELINK"));
        chart.AddEdge("hub-ber", "hub", "ber", kind: TopologyEdgeKind.Replication, status: TopologyHealthStatus.Healthy)
            .AddEdgeChip("hub-ber", "cost 200").UpdateEdge("hub-ber", e => e.Evidence(TopologyEdgeEvidence.Confirmed).EvidenceText(null));
        chart.AddEdge("hub-nyc", "hub", "nyc", kind: TopologyEdgeKind.Replication, status: TopologyHealthStatus.Warning)
            .AddEdgeStripe("hub-nyc", "#2563EB", "IP").AddEdgeStripe("hub-nyc", "#F97316", "SMTP")
            .AddEdgeChip("hub-nyc", "cost 300").AddEdgeChip("hub-nyc", "lag 47m", Tone.Warning)
            .UpdateEdge("hub-nyc", e => e.Evidence(TopologyEdgeEvidence.Confirmed).EvidenceText(null));
        chart.AddEdge("hub-tyo", "hub", "tyo", kind: TopologyEdgeKind.Replication, status: TopologyHealthStatus.Healthy)
            .AddEdgeChip("hub-tyo", "cost 500")
            .UpdateEdge("hub-tyo", e => e.Evidence(TopologyEdgeEvidence.Assumed).EvidenceText("Inferred from KCC-generated connection objects"));
        // The branch end names its bridgehead; the uplink marker points back to the hub.
        foreach (var (site, bridgehead) in new[] { ("par", "PAR-DC01"), ("ber", "BER-DC01"), ("nyc", "NEW-DC01"), ("tyo", "TOK-DC01") })
            chart.UpdateEdge("hub-" + site, e => e.SourceLabel(null).TargetLabel(bridgehead)).UpdateEdge("hub-" + site, e => e.UplinkEnd(TopologyEdgeEnd.Target));
        return chart;
    }

    private static void Site(TopologyChart chart, string id, string name, int controllers, string subnet) {
        chart.AddAutoNode(id, name, TopologyNodeKind.Location, TopologyHealthStatus.Healthy, subtitle: "Branch site");
        chart.Nodes[chart.Nodes.Count - 1].Metadata["layer"] = "1";
        AddControllers(chart, id, name.Substring(0, 3).ToUpperInvariant(), subnet, controllers, "2");
    }

    private static void AddControllers(TopologyChart chart, string site, string prefix, string subnet, int count, string layer) {
        for (var index = 1; index <= count; index++) {
            var id = site + "-dc" + index;
            chart.AddAutoNode(id, prefix + "-DC0" + index, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, subtitle: subnet + (10 + index), symbol: "DC");
            chart.Nodes[chart.Nodes.Count - 1].Metadata["layer"] = layer;
            chart.AddEdge(site + "-" + id, site, id, kind: TopologyEdgeKind.Replication, status: TopologyHealthStatus.Healthy);
        }
    }
}
