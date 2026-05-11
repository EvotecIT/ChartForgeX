using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Topology;

internal static class TopologyExamples {
    public static void Write(string output) {
        var target = Path.Combine(output, "topology-demo");
        Directory.CreateDirectory(target);
        WriteAll(target);

        var artifacts = Path.Combine(FindRepositoryRoot(), "artifacts", "topology-demo");
        Directory.CreateDirectory(artifacts);
        WriteAll(artifacts);
    }

    private static void WriteAll(string target) {
        var iconCatalog = BuildDemoIconCatalog();
        var demos = new[] {
            ("site-topology", BuildSiteTopologyChart()),
            ("replication-mesh", BuildReplicationMeshChart()),
            ("subnets-site-links", BuildSubnetsSiteLinksChart()),
            ("geographic-topology", BuildGeographicTopologyChart()),
            ("dc-connectivity", BuildDcConnectivityChart()),
            ("service-dependency", BuildServiceDependencyChart()),
            ("icon-palette", iconCatalog.ToPaletteChart(new TopologyIconPaletteOptions {
                Title = "Topology Icon Palette",
                Subtitle = "Built-in stencil models plus sample artwork-backed vendor icons.",
                PacksPerRow = 3,
                ColumnsPerPack = 4
            }))
        };

        foreach (var demo in demos) {
            var options = DemoRenderOptions(demo.Item1, iconCatalog);
            demo.Item2.SaveSvg(Path.Combine(target, demo.Item1 + ".svg"), options);
            demo.Item2.SaveHtml(Path.Combine(target, demo.Item1 + ".html"), options);
            demo.Item2.SavePng(Path.Combine(target, demo.Item1 + ".png"), options);
        }

        WriteView(demos[0].Item2, Path.Combine(target, "site-topology-emea-view"), new TopologyView { Id = "emea-view", Title = "Site Topology - EMEA View", Subtitle = "Focused view rendered from the same reusable topology model.", GroupIds = { "EMEA" } });
        WriteView(demos[1].Item2, Path.Combine(target, "replication-mesh-critical-view"), new TopologyView { Id = "critical-view", Title = "Replication Mesh - Critical Paths", Subtitle = "Focused view using explicit node ids and connected critical paths.", NodeIds = { "fra-dc1", "fra-dc2", "sin-dc1", "sin-dc2", "sfo-dc2" } });
        WriteRenderedView(demos[1].Item2, Path.Combine(target, "replication-mesh-dc-view"), new TopologyRenderOptions { IncludeGroups = false, IncludeGroupLabels = false, EdgeLabelMetricKey = "lag", EdgeSecondaryLabelMetricKey = "queue" });
        WriteRenderedView(demos[1].Item2, Path.Combine(target, "replication-mesh-offenders-view"), new TopologyRenderOptions { EdgeLabelMetricKey = "lag", EdgeSecondaryLabelMetricKey = "lastSuccess", HighlightStatuses = { TopologyHealthStatus.Warning, TopologyHealthStatus.Critical } });
        var serviceCompact = TopologyRenderOptions.FromPreset(TopologyViewPreset.Compact);
        serviceCompact.LegendMode = TopologyLegendMode.Auto;
        WriteRenderedView(demos[5].Item2, Path.Combine(target, "service-dependency-compact-view"), serviceCompact);
        WriteRenderedView(demos[5].Item2, Path.Combine(target, "service-dependency-api-neighbors-view"), new TopologyRenderOptions {
            View = TopologyView.AroundNode("api", 1),
            LegendMode = TopologyLegendMode.Auto
        });
        WriteRenderedView(demos[5].Item2, Path.Combine(target, "service-dependency-critical-dependencies-view"), new TopologyRenderOptions {
            View = new TopologyView {
                Id = "critical-dependencies",
                Title = "Service Dependency - Critical Dependencies",
                Subtitle = "Filtered view using generic edge kind and health selectors.",
                EdgeKinds = { TopologyEdgeKind.Dependency },
                HealthStatuses = { TopologyHealthStatus.Critical }
            },
            LegendMode = TopologyLegendMode.Auto
        });

        TopologyVisualExamples.Write(target);
        File.WriteAllText(Path.Combine(target, "icon-stencil-browser.html"), iconCatalog.ToStencilBrowserHtmlPage(new TopologyIconStencilBrowserOptions {
            Title = "Topology Stencil Browser",
            Subtitle = "Search and choose built-in, directory, cloud, people, and sample vendor icons from one reusable catalog.",
            PacksPerRow = 3,
            ColumnsPerPack = 4
        }), Encoding.UTF8);
        File.WriteAllText(Path.Combine(target, "index.html"), BuildIndex(demos, iconCatalog), Encoding.UTF8);
    }

    private static TopologyRenderOptions? DemoRenderOptions(string name, TopologyIconCatalog iconCatalog) {
        if (name == "service-dependency") return new TopologyRenderOptions { LegendMode = TopologyLegendMode.Auto };
        if (name == "icon-palette") {
            return new TopologyRenderOptions {
                IconCatalog = iconCatalog,
                IncludeLegend = false,
                EnableHtmlInteractions = true
            };
        }

        return null;
    }

    private static TopologyIconCatalog BuildDemoIconCatalog() {
        return TopologyIconCatalog.Default()
            .AddPack(new TopologyIconPack("azure-example", "Azure Example Icons", vendor: "Microsoft", version: "sample")
                .WithMetadata("licensing", "Sample artwork; hosts can replace these with approved official vendor assets.")
                .WithTags("azure", "cloud", "vendor", "sample")
                .AddIcon(new TopologyIconDefinition("azure-example", "data-factory", "Data Factory", TopologyNodeKind.Process, TopologyIconShape.Application) {
                    Symbol = "ADF",
                    Color = "#0078D4",
                    Category = "Azure",
                    DisplayMode = TopologyNodeDisplayMode.Tile,
                    Artwork = TopologyIconArtwork.InlineSvg("<rect x=\"7\" y=\"10\" width=\"13\" height=\"28\" rx=\"2\" fill=\"#0078D4\"/><rect x=\"28\" y=\"6\" width=\"13\" height=\"32\" rx=\"2\" fill=\"#50E6FF\"/><path d=\"M20 17 H28 M20 31 H28\" stroke=\"#243A5E\" stroke-width=\"3\" stroke-linecap=\"round\"/><circle cx=\"13.5\" cy=\"17\" r=\"2.6\" fill=\"#FFFFFF\"/><circle cx=\"34.5\" cy=\"17\" r=\"2.6\" fill=\"#FFFFFF\"/><circle cx=\"34.5\" cy=\"29\" r=\"2.6\" fill=\"#FFFFFF\"/>", "0 0 48 48")
                }.WithTags("data-factory", "pipeline", "etl"))
                .AddIcon(new TopologyIconDefinition("azure-example", "key-vault", "Key Vault", TopologyNodeKind.Certificate, TopologyIconShape.Certificate) {
                    Symbol = "KV",
                    Color = "#0078D4",
                    Category = "Security",
                    DisplayMode = TopologyNodeDisplayMode.Tile,
                    Artwork = TopologyIconArtwork.InlineSvg("<circle cx=\"24\" cy=\"16\" r=\"9\" fill=\"#50E6FF\"/><rect x=\"13\" y=\"21\" width=\"22\" height=\"18\" rx=\"3\" fill=\"#0078D4\"/><path d=\"M20 21 v-5 a4 4 0 0 1 8 0 v5\" fill=\"none\" stroke=\"#243A5E\" stroke-width=\"3\" stroke-linecap=\"round\"/><circle cx=\"24\" cy=\"30\" r=\"3\" fill=\"#FFFFFF\"/><path d=\"M24 33 v4\" stroke=\"#FFFFFF\" stroke-width=\"2.5\" stroke-linecap=\"round\"/>", "0 0 48 48")
                }.WithTags("key-vault", "secret", "certificate"))
                .AddIcon(new TopologyIconDefinition("azure-example", "resource-group", "Resource Group", TopologyNodeKind.Group, TopologyIconShape.Network) {
                    Symbol = "RG",
                    Color = "#0078D4",
                    Category = "Azure",
                    DisplayMode = TopologyNodeDisplayMode.Tile,
                    Artwork = TopologyIconArtwork.InlineSvg("<rect x=\"7\" y=\"8\" width=\"13\" height=\"13\" rx=\"2\" fill=\"#0078D4\"/><rect x=\"28\" y=\"8\" width=\"13\" height=\"13\" rx=\"2\" fill=\"#50E6FF\"/><rect x=\"17\" y=\"27\" width=\"14\" height=\"13\" rx=\"2\" fill=\"#7FBA00\"/><path d=\"M20 14.5 H28 M24 21 V27\" stroke=\"#243A5E\" stroke-width=\"2.6\" stroke-linecap=\"round\"/>", "0 0 48 48")
                }.WithTags("resource-group", "group", "azure")));
    }

    private static void WriteView(TopologyChart chart, string pathWithoutExtension, TopologyView view) {
        var options = new TopologyRenderOptions { View = view };
        WriteRenderedView(chart, pathWithoutExtension, options);
    }

    private static void WriteRenderedView(TopologyChart chart, string pathWithoutExtension, TopologyRenderOptions options) {
        chart.SaveSvg(pathWithoutExtension + ".svg", options);
        chart.SaveHtml(pathWithoutExtension + ".html", options);
        chart.SavePng(pathWithoutExtension + ".png", options);
    }

    private static string BuildIndex((string Name, TopologyChart Chart)[] demos, TopologyIconCatalog iconCatalog) {
        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.AppendLine("<title>ChartForgeX Topology Demos</title>");
        sb.AppendLine("<style>body{margin:0;background:#f8fafc;color:#0f172a;font-family:Inter,Segoe UI,system-ui,sans-serif;padding:24px}.demo{margin:0 auto 24px;max-width:1240px;background:white;border:1px solid #dbe3ef;border-radius:12px;padding:16px;box-shadow:0 12px 28px rgba(15,23,42,.06)}h1{max-width:1240px;margin:0 auto 20px;font-size:24px}.demo h2{font-size:16px;margin:0 0 12px}.demo svg{width:100%;height:auto;display:block}</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<h1>ChartForgeX Topology Demos</h1>");
        sb.AppendLine("<nav class=\"demo\" aria-label=\"Topology extras\"><h2>Topology extras</h2><p><a href=\"icon-stencil-browser.html\">Open stencil browser</a> · <a href=\"visual-coverage.html\">Open visual coverage gallery</a></p></nav>");
        foreach (var demo in demos) {
            sb.AppendLine("<section class=\"demo\">");
            sb.AppendLine("<h2>" + Escape(demo.Chart.Title ?? demo.Name) + "</h2>");
            var options = DemoRenderOptions(demo.Name, iconCatalog);
            sb.AppendLine(demo.Chart.ToSvg(options));
            sb.AppendLine("</section>");
        }

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    private static TopologyChart BuildSiteTopologyChart() {
        return TopologyChart.Create()
            .WithId("site-topology")
            .WithTitle("Site Topology")
            .WithSubtitle("Sample regional site topology with hubs, branches, bridgeheads, and site-link health.")
            .WithViewport(1200, 680, 28)
            .WithTheme(TopologyTheme.Light())
            .WithLegend(TopologyLegend.Default()
                .AddNodeKind("Hub site", TopologyNodeKind.Hub, symbol: "H")
                .AddNodeKind("Branch site", TopologyNodeKind.Branch, symbol: "B")
                .AddNodeKind("Bridgehead", TopologyNodeKind.Server, symbol: "BH")
                .AddEdgeKind("Site link", TopologyEdgeKind.Link))
            .AddGroup("AMER", "AMER", 60, 110, 310, 330, TopologyHealthStatus.Healthy, "47 sites", "/topology/regions/amer", "AMER topology group")
            .AddGroup("EMEA", "EMEA", 435, 110, 330, 330, TopologyHealthStatus.Warning, "56 sites", "/topology/regions/emea", "EMEA topology group")
            .AddGroup("APAC", "APAC", 835, 110, 310, 330, TopologyHealthStatus.Critical, "39 sites", "/topology/regions/apac", "APAC topology group")
            .AddNode("amer-hub", "AMER Hub", 160, 170, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "AMER", "Hub site", "/topology/sites/amer-hub", "AMER Hub (Healthy)", 128, 62)
            .AddNode("nva-east", "NVA East", 95, 275, TopologyNodeKind.Branch, TopologyHealthStatus.Healthy, "AMER", "Branch", "/topology/sites/nva-east", null, 128, 62)
            .AddNode("chi-dc", "CHI DC", 230, 275, TopologyNodeKind.Server, TopologyHealthStatus.Warning, "AMER", "Bridgehead", "/topology/sites/chi-dc", null, 128, 62, "BH")
            .AddNode("lon-hub", "EMEA Hub", 540, 170, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "EMEA", "Hub site", "/topology/sites/lon-hub", null, 128, 62)
            .AddNode("eu-west", "EU West", 465, 275, TopologyNodeKind.Branch, TopologyHealthStatus.Healthy, "EMEA", "Branch", "/topology/sites/eu-west", null, 128, 62)
            .AddNode("tr-branch", "TR Branch", 615, 275, TopologyNodeKind.Branch, TopologyHealthStatus.Warning, "EMEA", "High latency", "/topology/sites/tr-branch", null, 128, 62)
            .AddNode("apac-hub", "APAC Hub", 930, 170, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "APAC", "Hub site", "/topology/sites/apac-hub", null, 128, 62)
            .AddNode("anz", "ANZ", 875, 275, TopologyNodeKind.Branch, TopologyHealthStatus.Critical, "APAC", "Critical", "/topology/sites/anz", null, 128, 62)
            .AddNode("in-india", "IN India", 1010, 275, TopologyNodeKind.Branch, TopologyHealthStatus.Unknown, "APAC", "Unknown", "/topology/sites/in-india", null, 128, 62)
            .AddNode("bh-1", "Bridgehead DC 1", 415, 510, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, null, "Healthy", "/topology/bridgeheads/bh-1", null, 128, 62, "BH")
            .AddNode("bh-2", "Bridgehead DC 2", 650, 510, TopologyNodeKind.Server, TopologyHealthStatus.Critical, null, "Degraded", "/topology/bridgeheads/bh-2", null, 128, 62, "BH")
            .AddEdge("amer-emea", "amer-hub", "lon-hub", "24 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Healthy, TopologyDirection.Bidirectional, TopologyEdgeRouting.Straight, href: "/topology/links/amer-emea")
            .AddEdge("emea-apac", "lon-hub", "apac-hub", "82 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Warning, TopologyDirection.Bidirectional, TopologyEdgeRouting.Straight, href: "/topology/links/emea-apac")
            .AddEdge("apac-anz", "apac-hub", "anz", "142 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Critical, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal, href: "/topology/links/apac-anz")
            .AddEdge("amer-bh", "nva-east", "bh-1", "32 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal, "bridgehead", "/topology/links/amer-bh")
            .AddEdge("bh-path", "bh-1", "bh-2", "68 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Straight, "queue 44", "/topology/links/bh-path");
    }

    private static TopologyChart BuildReplicationMeshChart() {
        var chart = TopologyChart.Create()
            .WithId("replication-mesh")
            .WithTitle("Replication Mesh")
            .WithSubtitle("Sample deterministic replication paths with lag, queue, and last-success labels.")
            .WithViewport(1200, 680, 28)
            .WithLegend(TopologyLegend.Default().AddNodeKind("Domain controller", TopologyNodeKind.Server, symbol: "DC").AddEdgeKind("Replication", TopologyEdgeKind.Replication))
            .AddGroup("HQ-NYC", "HQ-NYC", 55, 120, 205, 135, TopologyHealthStatus.Healthy, "2 DCs", "/replication/sites/hq-nyc")
            .AddGroup("LON-LON", "LON-LON", 350, 120, 205, 135, TopologyHealthStatus.Healthy, "2 DCs", "/replication/sites/lon-lon")
            .AddGroup("FRA-FRA", "FRA-FRA", 645, 120, 205, 135, TopologyHealthStatus.Warning, "2 DCs", "/replication/sites/fra-fra")
            .AddGroup("SFO-SFO", "SFO-SFO", 180, 410, 225, 135, TopologyHealthStatus.Healthy, "2 DCs", "/replication/sites/sfo-sfo")
            .AddGroup("SIN-SIN", "SIN-SIN", 650, 410, 225, 135, TopologyHealthStatus.Critical, "2 DCs", "/replication/sites/sin-sin")
            .AddNode("nyc-dc1", "NYC-DC1", 83, 178, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "HQ-NYC", "DC", "/replication/nodes/nyc-dc1", null, 128, 62, "DC")
            .AddNode("nyc-dc2", "NYC-DC2", 171, 178, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "HQ-NYC", "DC", "/replication/nodes/nyc-dc2", null, 128, 62, "DC")
            .AddNode("lon-dc1", "LON-DC1", 378, 178, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "LON-LON", "DC", "/replication/nodes/lon-dc1", null, 128, 62, "DC")
            .AddNode("lon-dc2", "LON-DC2", 466, 178, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "LON-LON", "DC", "/replication/nodes/lon-dc2", null, 128, 62, "DC")
            .AddNode("fra-dc1", "FRA-DC1", 673, 178, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "FRA-FRA", "DC", "/replication/nodes/fra-dc1", null, 128, 62, "DC")
            .AddNode("fra-dc2", "FRA-DC2", 761, 178, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "FRA-FRA", "DC", "/replication/nodes/fra-dc2", null, 128, 62, "DC")
            .AddNode("sfo-dc1", "SFO-DC1", 215, 468, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "SFO-SFO", "DC", "/replication/nodes/sfo-dc1", null, 128, 62, "DC")
            .AddNode("sfo-dc2", "SFO-DC2", 303, 468, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "SFO-SFO", "DC", "/replication/nodes/sfo-dc2", null, 128, 62, "DC")
            .AddNode("sin-dc1", "SIN-DC1", 685, 468, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "SIN-SIN", "DC", "/replication/nodes/sin-dc1", null, 128, 62, "DC")
            .AddNode("sin-dc2", "SIN-DC2", 773, 468, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "SIN-SIN", "DC", "/replication/nodes/sin-dc2", null, 128, 62, "DC")
            .AddEdge("nyc-lon", "nyc-dc1", "lon-dc1", "105 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Straight, "Q:312; 12m ago", "/replication/paths/nyc-lon")
            .AddEdge("lon-fra", "lon-dc2", "fra-dc1", "156 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Straight, "Q:842; 3m ago", "/replication/paths/lon-fra")
            .AddEdge("fra-sin", "fra-dc2", "sin-dc1", "238 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Critical, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal, "Q:1124; 2m ago", "/replication/paths/fra-sin")
            .AddEdge("sfo-sin", "sfo-dc2", "sin-dc1", "214 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Critical, TopologyDirection.Forward, TopologyEdgeRouting.Straight, "Q:1552; 1m ago", "/replication/paths/sfo-sin")
            .AddEdge("lon-sfo", "lon-dc1", "sfo-dc1", "118 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.Bidirectional, TopologyEdgeRouting.Orthogonal, "Q:301; 10m ago", "/replication/paths/lon-sfo");
        AddReplicationMetrics(chart);
        chart
            .WithEdgePorts("nyc-lon", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("lon-fra", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("fra-sin", TopologyEdgePort.Bottom, TopologyEdgePort.Top)
            .WithEdgeRouteLane("fra-sin", 24)
            .WithEdgePorts("sfo-sin", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("lon-sfo", TopologyEdgePort.Bottom, TopologyEdgePort.Top)
            .WithEdgeRouteLane("lon-sfo", -24);
        foreach (var node in chart.Nodes.Where(node => node.Kind == TopologyNodeKind.Server)) {
            node.DisplayMode = TopologyNodeDisplayMode.Icon;
            node.Badge = node.Id.EndsWith("dc1", StringComparison.Ordinal) ? "1" : "2";
        }

        return chart;
    }

    private static TopologyChart BuildSubnetsSiteLinksChart() {
        return TopologyChart.Create()
            .WithId("subnets-site-links")
            .WithTitle("Subnets and Site Links")
            .WithSubtitle("Sample mapping of sites, subnet groups, bridgeheads, costs, transport, and mapping issues.")
            .WithViewport(1200, 680, 28)
            .WithLegend(TopologyLegend.Default().AddNodeKind("Subnet", TopologyNodeKind.NetworkSegment, symbol: "NET").AddNodeKind("Subnet group", TopologyNodeKind.Network, symbol: "NW").AddEdgeKind("Subnet mapping", TopologyEdgeKind.Mapping))
            .AddGroup("AMER", "AMER", 55, 120, 315, 335, TopologyHealthStatus.Healthy, "47 sites", "/subnets/regions/amer")
            .AddGroup("EMEA", "EMEA", 435, 120, 315, 335, TopologyHealthStatus.Warning, "56 sites", "/subnets/regions/emea")
            .AddGroup("APAC", "APAC", 815, 120, 315, 335, TopologyHealthStatus.Critical, "39 sites", "/subnets/regions/apac")
            .AddNode("amer-hub", "AMER Hub", 145, 175, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "AMER", "10.0.0.0/16", "/subnets/sites/amer-hub", null, 128, 62)
            .AddNode("na-west", "NA-West", 90, 290, TopologyNodeKind.Network, TopologyHealthStatus.Healthy, "AMER", "10.1.0.0/24", "/subnets/10.1.0.0-24", null, 128, 62)
            .AddNode("emea-hub", "EMEA Hub", 525, 175, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "EMEA", "10.10.0.0/16", "/subnets/sites/emea-hub", null, 128, 62)
            .AddNode("eu-east", "EU East", 605, 290, TopologyNodeKind.Network, TopologyHealthStatus.Warning, "EMEA", "10.10.3.0/24", "/subnets/10.10.3.0-24", null, 128, 62)
            .AddNode("apac-hub", "APAC Hub", 905, 175, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "APAC", "10.20.0.0/16", "/subnets/sites/apac-hub", null, 128, 62)
            .AddNode("anz-subnet", "10.20.2.0/24", 855, 290, TopologyNodeKind.NetworkSegment, TopologyHealthStatus.Critical, "APAC", "Overlapping", "/subnets/10.20.2.0-24", null, 128, 62)
            .AddNode("orphan", "172.31.50.0/24", 515, 520, TopologyNodeKind.NetworkSegment, TopologyHealthStatus.Warning, null, "Orphaned", "/subnets/172.31.50.0-24", null, 128, 62)
            .AddNode("bridgehead", "Bridgehead DC", 745, 520, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, null, "172.16.0.0/16", "/subnets/bridgeheads/1", null, 128, 62, "BH")
            .AddEdge("amer-emea", "amer-hub", "emea-hub", "MPLS $1.20", TopologyEdgeKind.Link, TopologyHealthStatus.Healthy, TopologyDirection.Bidirectional, TopologyEdgeRouting.Straight, "24 ms", "/subnets/links/amer-emea")
            .AddEdge("emea-apac", "emea-hub", "apac-hub", "MPLS $1.35", TopologyEdgeKind.Link, TopologyHealthStatus.Warning, TopologyDirection.Bidirectional, TopologyEdgeRouting.Straight, "82 ms", "/subnets/links/emea-apac")
            .AddEdge("apac-anz", "apac-hub", "anz-subnet", "MPLS $1.10", TopologyEdgeKind.Mapping, TopologyHealthStatus.Critical, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal, "62 ms", "/subnets/links/apac-anz")
            .AddEdge("orphan-bh", "orphan", "bridgehead", "unmapped", TopologyEdgeKind.Mapping, TopologyHealthStatus.Unknown, TopologyDirection.None, TopologyEdgeRouting.Straight, "needs owner", "/subnets/issues/orphan-bh")
            .WithEdgePorts("amer-emea", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("emea-apac", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("apac-anz", TopologyEdgePort.Bottom, TopologyEdgePort.Top)
            .WithEdgeRouteLane("apac-anz", 18);
    }

    private static TopologyChart BuildGeographicTopologyChart() {
        var chart = TopologyChart.Create()
            .WithId("geographic-topology")
            .WithTitle("Geographic Topology")
            .WithSubtitle("Abstract region placement with clustered child sites and WAN latency labels.")
            .WithLayout(TopologyLayoutMode.Geographic)
            .WithMapViewport(ChartMapViewport.World())
            .WithViewport(1200, 680, 28)
            .WithLegend(TopologyLegend.Default().AddNodeKind("Region", TopologyNodeKind.Location, symbol: "R").AddEdgeKind("Connectivity", TopologyEdgeKind.Connectivity))
            .AddNode("amer", "AMER", 180, 270, TopologyNodeKind.Location, TopologyHealthStatus.Healthy, null, "47 sites", "/geo/regions/amer", null, 128, 62)
            .AddNode("emea", "EMEA", 520, 210, TopologyNodeKind.Location, TopologyHealthStatus.Healthy, null, "56 sites", "/geo/regions/emea", null, 128, 62)
            .AddNode("apac", "APAC", 860, 330, TopologyNodeKind.Location, TopologyHealthStatus.Critical, null, "39 sites", "/geo/regions/apac", null, 128, 62)
            .AddNode("nva", "NVA East", 120, 410, TopologyNodeKind.Branch, TopologyHealthStatus.Healthy, null, "Healthy", "/geo/sites/nva", null, 128, 62)
            .AddNode("chi", "CHI DC", 250, 415, TopologyNodeKind.Server, TopologyHealthStatus.Warning, null, "Warning", "/geo/sites/chi", null, 128, 62, "DC")
            .AddNode("uk", "UK DC", 490, 360, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, null, "Healthy", "/geo/sites/uk", null, 128, 62, "DC")
            .AddNode("de", "DE Branch", 625, 355, TopologyNodeKind.Branch, TopologyHealthStatus.Warning, null, "Warning", "/geo/sites/de", null, 128, 62)
            .AddNode("sg", "SG DC", 815, 485, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, null, "Healthy", "/geo/sites/sg", null, 128, 62, "DC")
            .AddNode("anz", "ANZ", 960, 485, TopologyNodeKind.Branch, TopologyHealthStatus.Critical, null, "Down", "/geo/sites/anz", null, 128, 62)
            .AddEdge("amer-emea", "amer", "emea", "68 ms", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Healthy, TopologyDirection.Bidirectional, TopologyEdgeRouting.Curved, "WAN", "/geo/links/amer-emea")
            .AddEdge("emea-apac", "emea", "apac", "92 ms", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Critical, TopologyDirection.Bidirectional, TopologyEdgeRouting.Curved, "WAN", "/geo/links/emea-apac")
            .AddEdge("amer-apac", "amer", "apac", "142 ms", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Warning, TopologyDirection.Bidirectional, TopologyEdgeRouting.Curved, "backup", "/geo/links/amer-apac")
            .AddEdge("amer-nva", "amer", "nva", "local", TopologyEdgeKind.Dependency, TopologyHealthStatus.Healthy, routing: TopologyEdgeRouting.Curved, href: "/geo/links/amer-nva")
            .AddEdge("apac-anz", "apac", "anz", "down", TopologyEdgeKind.Dependency, TopologyHealthStatus.Critical, routing: TopologyEdgeRouting.Curved, href: "/geo/links/apac-anz")
            .WithNodeCoordinates("amer", -98.5795, 39.8283)
            .WithNodeCoordinates("emea", 12.4964, 41.9028)
            .WithNodeCoordinates("apac", 105, 18)
            .WithNodeCoordinates("nva", -74.006, 40.7128)
            .WithNodeCoordinates("chi", -87.6298, 41.8781)
            .WithNodeCoordinates("uk", -0.1276, 51.5072)
            .WithNodeCoordinates("de", 8.6821, 50.1109)
            .WithNodeCoordinates("sg", 103.8198, 1.3521)
            .WithNodeCoordinates("anz", 151.2093, -33.8688);
        foreach (var node in chart.Nodes.Where(node => node.Kind is TopologyNodeKind.Branch or TopologyNodeKind.Server)) {
            node.DisplayMode = TopologyNodeDisplayMode.Dot;
            node.Badge = node.Symbol ?? node.Label.Split(' ')[0];
        }

        return chart;
    }

    private static TopologyChart BuildDcConnectivityChart() {
        return TopologyChart.Create()
            .WithId("dc-connectivity")
            .WithTitle("Domain Controller Connectivity")
            .WithSubtitle("Sample selected-domain-controller connectivity, connection objects, and service health.")
            .WithLayout(TopologyLayoutMode.HubAndSpoke)
            .WithViewport(1200, 620, 28)
            .WithLegend(TopologyLegend.Default()
                .AddNodeKind("Domain controller", TopologyNodeKind.Server, symbol: "DC")
                .AddNodeKind("Global catalog", TopologyNodeKind.Server, symbol: "GC")
                .AddNodeKind("RODC", TopologyNodeKind.Server, symbol: "RO")
                .AddEdgeKind("Connection object", TopologyEdgeKind.Connectivity)
                .AddEdgeKind("Authentication path", TopologyEdgeKind.AuthenticationPath))
            .AddGroup("selected", "Selected DC", 455, 160, 290, 250, TopologyHealthStatus.Healthy, "NA-DC01", "/directory/dcs/na-dc01")
            .AddGroup("partners", "Connection Partners", 70, 120, 1060, 330, TopologyHealthStatus.Warning, "Inbound and outbound")
            .AddNode("na-dc01", "NA-DC01", 540, 250, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "selected", "PDC, RID", "/directory/dcs/na-dc01", width: 136, symbol: "GC")
            .AddNode("na-dc02", "NA-DC02", 150, 150, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "partners", "Inbound", "/directory/dcs/na-dc02", symbol: "DC")
            .AddNode("na-rodc01", "NA-RODC01", 150, 270, TopologyNodeKind.Server, TopologyHealthStatus.Unknown, "partners", "Branch", "/directory/dcs/na-rodc01", symbol: "RO")
            .AddNode("eu-dc01", "EU-DC01", 905, 150, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "partners", "Schema Master", "/directory/dcs/eu-dc01", symbol: "GC")
            .AddNode("ap-dc01", "AP-DC01", 905, 270, TopologyNodeKind.Server, TopologyHealthStatus.Warning, "partners", "High lag", "/directory/dcs/ap-dc01", symbol: "DC")
            .AddNode("na-dc04", "NA-DC04", 560, 395, TopologyNodeKind.Server, TopologyHealthStatus.Critical, "partners", "LDAP down", "/directory/dcs/na-dc04", symbol: "DC")
            .AddEdge("na-dc02-na-dc01", "na-dc02", "na-dc01", "LDAP OK", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Straight, "Kerberos OK", "/directory/connections/na-dc02-na-dc01")
            .AddEdge("na-rodc01-na-dc01", "na-rodc01", "na-dc01", "RODC", TopologyEdgeKind.AuthenticationPath, TopologyHealthStatus.Unknown, TopologyDirection.Forward, TopologyEdgeRouting.Straight, "No recent bind", "/directory/connections/na-rodc01-na-dc01")
            .AddEdge("na-dc01-eu-dc01", "na-dc01", "eu-dc01", "72 ms", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Healthy, TopologyDirection.Bidirectional, TopologyEdgeRouting.Straight, "RPC", "/directory/connections/na-dc01-eu-dc01")
            .AddEdge("na-dc01-ap-dc01", "na-dc01", "ap-dc01", "168 ms", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Warning, TopologyDirection.Bidirectional, TopologyEdgeRouting.Straight, "Queue 18", "/directory/connections/na-dc01-ap-dc01")
            .AddEdge("na-dc01-na-dc04", "na-dc01", "na-dc04", "LDAP fail", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Critical, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal, "DNS OK", "/directory/connections/na-dc01-na-dc04");
    }

    private static void AddReplicationMetrics(TopologyChart chart) {
        AddMetrics(chart, "nyc-lon", "105 ms", "Q:312", "12m ago", "15m + 2m", "7 / 7");
        AddMetrics(chart, "lon-fra", "156 ms", "Q:842", "3m ago", "15m + 5m", "7 / 7");
        AddMetrics(chart, "fra-sin", "238 ms", "Q:1124", "2m ago", "15m + 10m", "6 / 7");
        AddMetrics(chart, "sfo-sin", "214 ms", "Q:1552", "1m ago", "15m + 10m", "6 / 7");
        AddMetrics(chart, "lon-sfo", "118 ms", "Q:301", "10m ago", "15m + 5m", "7 / 7");
    }

    private static void AddMetrics(TopologyChart chart, string edgeId, string lag, string queue, string lastSuccess, string schedule, string namingContexts) {
        var edge = chart.Edges.First(edge => edge.Id == edgeId);
        edge.Metrics["lag"] = lag;
        edge.Metrics["queue"] = queue;
        edge.Metrics["lastSuccess"] = lastSuccess;
        edge.Metrics["schedule"] = schedule;
        edge.Metrics["namingContexts"] = namingContexts;
    }

    private static TopologyChart BuildServiceDependencyChart() {
        return TopologyChart.Create()
            .WithId("service-dependency")
            .WithTitle("Service Dependency")
            .WithSubtitle("Generic service, database, queue, and team ownership topology using product-neutral node kinds.")
            .WithLayout(TopologyLayoutMode.Layered)
            .WithViewport(1200, 640, 28)
            .WithLegend(null)
            .AddGroup("edge", "Edge Tier", 55, 120, 250, 300, TopologyHealthStatus.Healthy, "Gateways")
            .AddGroup("app", "Application Tier", 365, 120, 310, 300, TopologyHealthStatus.Warning, "Services")
            .AddGroup("data", "Data Tier", 735, 120, 330, 300, TopologyHealthStatus.Critical, "Stores")
            .AddNode("gateway", "Public Gateway", 110, 190, TopologyNodeKind.Gateway, TopologyHealthStatus.Healthy, "edge", "TLS 1.3", "/services/gateway", symbol: "GW")
            .AddNode("api", "Orders API", 430, 160, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, "app", "API", "/services/orders-api", symbol: "API")
            .AddNode("worker", "Billing Worker", 430, 285, TopologyNodeKind.Process, TopologyHealthStatus.Warning, "app", "Queue lag", "/services/billing-worker", symbol: "JOB")
            .AddNode("queue", "Orders Queue", 775, 145, TopologyNodeKind.Queue, TopologyHealthStatus.Warning, "data", "1,284 pending", "/services/orders-queue", symbol: "Q")
            .AddNode("sql", "Orders SQL", 820, 285, TopologyNodeKind.Database, TopologyHealthStatus.Critical, "data", "P95 412 ms", "/services/orders-sql", symbol: "SQL")
            .AddNode("team", "Payments Team", 500, 500, TopologyNodeKind.Team, TopologyHealthStatus.Healthy, null, "Owner", "/teams/payments", symbol: "TM")
            .AddEdge("gateway-api", "gateway", "api", "18 ms", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Straight, "HTTPS", "/service-map/gateway-api")
            .AddEdge("api-queue", "api", "queue", "34 ms", TopologyEdgeKind.DataFlow, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal, "publish", "/service-map/api-queue")
            .AddEdge("worker-queue", "worker", "queue", "lag 9m", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, TopologyDirection.Bidirectional, TopologyEdgeRouting.Orthogonal, "consume", "/service-map/worker-queue")
            .AddEdge("worker-sql", "worker", "sql", "412 ms", TopologyEdgeKind.Dependency, TopologyHealthStatus.Critical, TopologyDirection.Forward, TopologyEdgeRouting.Straight, "P95", "/service-map/worker-sql")
            .AddEdge("team-api", "team", "api", "owns", TopologyEdgeKind.Ownership, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Curved, href: "/service-map/team-api")
            .AddEdge("team-worker", "team", "worker", "owns", TopologyEdgeKind.Ownership, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Curved, href: "/service-map/team-worker");
    }

    private static string FindRepositoryRoot() {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(current)) {
            if (File.Exists(Path.Combine(current, "ChartForgeX.sln"))) return current;
            var parent = Directory.GetParent(current);
            if (parent == null) break;
            current = parent.FullName;
        }

        return AppContext.BaseDirectory;
    }

    private static string Escape(string value) => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
