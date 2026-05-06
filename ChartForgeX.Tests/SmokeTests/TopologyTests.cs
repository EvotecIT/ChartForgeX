using System;
using System.Linq;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TopologyRendersDemoSvg() {
        var svg = CreateSampleTopologyChart().ToSvg();
        Assert(!string.IsNullOrWhiteSpace(svg), "Topology SVG output should not be empty.");
        Assert(svg.Contains("<svg", StringComparison.Ordinal), "Topology renderer should emit complete SVG.");
        Assert(svg.Contains("data-cfx-role=\"topology\"", StringComparison.Ordinal), "Topology SVG should expose a topology role.");
        Assert(svg.Contains("AMER Hub", StringComparison.Ordinal), "Topology SVG should contain expected node labels.");
        Assert(svg.Contains("24 ms", StringComparison.Ordinal), "Topology SVG should contain expected edge labels.");
        Assert(svg.Contains("AMER", StringComparison.Ordinal), "Topology SVG should contain expected group labels.");
        Assert(svg.Contains("href=\"/topology/sites/amer-hub\"", StringComparison.Ordinal), "Topology SVG should emit safe href links.");
        Assert(svg.Contains("<title>AMER Hub (Healthy)</title>", StringComparison.Ordinal), "Topology SVG should emit native SVG tooltips.");
        Assert(svg.Contains("data-node-kind=\"Hub\"", StringComparison.Ordinal), "Topology SVG should expose generic node kinds.");
        Assert(svg.Contains("data-cfx-status=\"Critical\"", StringComparison.Ordinal), "Topology SVG should expose health status metadata.");
        Assert(svg.Contains("data-cfx-role=\"topology-legend\"", StringComparison.Ordinal), "Topology SVG should render the legend.");
        var png = CreateSampleTopologyChart().ToPng();
        Assert(png.Length > 64 && png[0] == 137 && png[1] == 80 && png[2] == 78 && png[3] == 71, "Topology renderer should emit a valid PNG image.");
    }

    private static void TopologyDefaultLegendIsProductNeutral() {
        var legend = TopologyLegend.Default();
        Assert(legend.Items.Any(item => item.Status == TopologyHealthStatus.Healthy), "Default topology legend should include generic health statuses.");
        Assert(legend.Items.All(item => item.NodeKind == null), "Default topology legend should not hardcode node-kind entries.");
        Assert(legend.Items.All(item => item.EdgeKind == null), "Default topology legend should not hardcode edge-kind entries.");

        legend.AddNodeKind("API", TopologyNodeKind.Service, symbol: "API").AddEdgeKind("Dependency", TopologyEdgeKind.Dependency);
        Assert(legend.Items.Any(item => item.NodeKind == TopologyNodeKind.Service && item.Symbol == "API"), "Topology callers should be able to add caller-specific node legend symbols explicitly.");
        Assert(legend.Items.Any(item => item.EdgeKind == TopologyEdgeKind.Dependency), "Topology callers should be able to add caller-specific edge kinds explicitly.");
    }

    private static void TopologyLegendGrowsForDomainSpecificItems() {
        var chart = CreateSampleTopologyChart();
        chart.Legend = TopologyLegend.Default()
            .AddNodeKind("Hub site", TopologyNodeKind.Hub, symbol: "H")
            .AddNodeKind("Branch site", TopologyNodeKind.Branch, symbol: "B")
            .AddNodeKind("Bridgehead", TopologyNodeKind.Server, symbol: "BH")
            .AddEdgeKind("Site link", TopologyEdgeKind.Link);

        var svg = chart.ToSvg();
        Assert(svg.Contains("data-legend-kind=\"node\"", StringComparison.Ordinal), "Topology legend should render explicit node-kind items.");
        Assert(svg.Contains("data-legend-kind=\"edge\"", StringComparison.Ordinal), "Topology legend should render explicit edge-kind items.");
        Assert(svg.Contains("height=\"110\"", StringComparison.Ordinal), "Topology legend should grow when caller-added legend items need another row.");
        Assert(chart.ToPng().Length > 64, "Topology PNG legend should render caller-added legend items.");
    }

    private static void TopologyNormalizesOverlappingManualContent() {
        var chart = TopologyChart.Create()
            .WithId("overlap-demo")
            .WithTitle("Overlap Demo")
            .WithViewport(360, 260, 20)
            .WithLegend(TopologyLegend.Default().AddNodeKind("Server", TopologyNodeKind.Server, symbol: "S"))
            .AddGroup("g", "Group", 40, 70, 205, 130, TopologyHealthStatus.Healthy)
            .AddNode("a", "NODE-A", 70, 125, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "g", width: 128)
            .AddNode("b", "NODE-B", 150, 125, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "g", width: 128)
            .AddEdge("a-b", "a", "b", "12 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy);

        var svg = chart.ToSvg();
        Assert(!svg.Contains("viewBox=\"0 0 360 260\"", StringComparison.Ordinal), "Topology renderer should expand the viewport when the legend would overlap content.");
        Assert(svg.Contains("x=\"194\"", StringComparison.Ordinal), "Topology renderer should deterministically separate overlapping sibling nodes.");
        Assert(svg.Contains("width=\"108\"", StringComparison.Ordinal), "Topology renderer should compact sibling nodes when a cluster cannot fit their requested width.");
    }

    private static void TopologyFitsManualContentIntoViewport() {
        var chart = TopologyChart.Create()
            .WithId("fit-demo")
            .WithTitle("Fit Demo")
            .WithViewport(260, 180, 20)
            .WithLegend(null)
            .AddNode("left", "Left", -40, 20, TopologyNodeKind.Service, TopologyHealthStatus.Healthy)
            .AddNode("right", "Right", 300, 20, TopologyNodeKind.Database, TopologyHealthStatus.Warning)
            .AddEdge("left-right", "left", "right", "42 ms", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Straight);

        var svg = chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false });
        Assert(svg.Contains("viewBox=\"0 0 500 180\"", StringComparison.Ordinal), "Topology renderer should expand the viewport horizontally when manual content exceeds the original width.");
        Assert(svg.Contains("x=\"20\" y=\"92\" width=\"120\" height=\"64\"", StringComparison.Ordinal), "Topology renderer should shift negative or title-overlapping manual content into the safe report area.");
        Assert(svg.Contains("x=\"360\" y=\"92\" width=\"120\" height=\"64\"", StringComparison.Ordinal), "Topology renderer should preserve deterministic spacing while fitting wide manual content.");
        Assert(chart.ToPng(new TopologyRenderOptions { IncludeLegend = false }).Length > 64, "Topology PNG should use the same fitted topology layout.");
    }

    private static void TopologyReservesGroupHeaderSpace() {
        var chart = TopologyChart.Create()
            .WithId("header-safe-demo")
            .WithViewport(360, 260, 20)
            .WithLegend(null)
            .AddGroup("g", "Data Tier", 40, 70, 240, 130, TopologyHealthStatus.Warning, subtitle: "Storage")
            .AddNode("inside", "Orders Queue", 60, 94, TopologyNodeKind.Queue, TopologyHealthStatus.Warning, "g", symbol: "Q");

        var svg = chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false });
        Assert(svg.Contains("data-node-id=\"inside\"", StringComparison.Ordinal), "Topology renderer should keep header-colliding nodes visible.");
        Assert(svg.Contains("y=\"156\"", StringComparison.Ordinal), "Topology normalizer should move nodes out of the group label header band.");
        Assert(svg.Contains("height=\"174\"", StringComparison.Ordinal), "Topology normalizer should expand groups when header avoidance moves nodes downward.");
    }

    private static void TopologyRendersDeterministicLayouts() {
        foreach (var chart in new[] {
            CreateSampleTopologyChart(TopologyLayoutMode.Manual),
            CreateSampleTopologyChart(TopologyLayoutMode.GroupGrid),
            CreateSampleTopologyChart(TopologyLayoutMode.HubAndSpoke),
            CreateSampleTopologyChart(TopologyLayoutMode.Layered),
            CreateSampleTopologyChart(TopologyLayoutMode.Matrix)
        }) {
            var svg = chart.ToSvg();
            Assert(svg.Contains("data-cfx-role=\"topology-node\"", StringComparison.Ordinal), "Topology layout should render nodes: " + chart.LayoutMode + ".");
            Assert(svg.Contains("data-cfx-role=\"topology-edge\"", StringComparison.Ordinal), "Topology layout should render edges: " + chart.LayoutMode + ".");
            Assert(svg.Contains("data-cfx-role=\"topology-edge-label\"", StringComparison.Ordinal), "Topology layout should render edge labels: " + chart.LayoutMode + ".");
        }
    }

    private static void TopologyLayeredLayoutsCanFlowLeftToRight() {
        var chart = TopologyChart.Create()
            .WithId("layer-flow")
            .WithViewport(600, 320, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.Layered, TopologyLayoutDirection.LeftToRight)
            .AddNode("namespace", "Tenant", 0, 0, TopologyNodeKind.Namespace, TopologyHealthStatus.Healthy, symbol: "T")
            .AddNode("service", "API", 0, 0, TopologyNodeKind.Service, TopologyHealthStatus.Warning, symbol: "API")
            .AddNode("database", "SQL", 0, 0, TopologyNodeKind.Database, TopologyHealthStatus.Healthy, symbol: "SQL")
            .AddEdge("tenant-api", "namespace", "service", "auth", TopologyEdgeKind.AuthenticationPath, TopologyHealthStatus.Healthy, TopologyDirection.Forward)
            .AddEdge("api-sql", "service", "database", "queries", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, TopologyDirection.Forward);

        var svg = chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false });
        Assert(svg.Contains("data-layout-direction=\"LeftToRight\"", StringComparison.Ordinal), "Topology SVG should expose layout direction for host adapters.");
        Assert(svg.Contains("id=\"layer-flow-node-namespace\"", StringComparison.Ordinal), "Layered left-to-right topology should render the first layer.");
        Assert(svg.Contains("id=\"layer-flow-node-service\"", StringComparison.Ordinal), "Layered left-to-right topology should render the middle layer.");
        Assert(svg.Contains("id=\"layer-flow-node-database\"", StringComparison.Ordinal), "Layered left-to-right topology should render the last layer.");
        Assert(svg.Contains("x=\"24\"", StringComparison.Ordinal), "Layered left-to-right topology should start inside the left viewport padding.");
        Assert(svg.Contains("x=\"456\"", StringComparison.Ordinal), "Layered left-to-right topology should place the final layer near the right viewport padding.");
        Assert(chart.ToPng(new TopologyRenderOptions { IncludeLegend = false }).Length > 64, "Layered left-to-right topology should render as PNG.");
    }

    private static void TopologyEscapesTextAndSkipsUnsafeHref() {
        var chart = new TopologyChart {
            Id = "escape-demo",
            Title = "Unsafe <Topology>",
            Viewport = new TopologyViewport { Width = 420, Height = 240, Padding = 20 }
        };
        chart.Nodes.Add(new TopologyNode { Id = "a", Label = "A < B & C", X = 40, Y = 80, Href = "javascript:alert(1)", Tooltip = "Tooltip <unsafe>" });
        chart.Nodes.Add(new TopologyNode { Id = "b", Label = "B", X = 240, Y = 80, Href = "/safe?x=1&y=2" });
        chart.Edges.Add(new TopologyEdge { Id = "a-b", SourceNodeId = "a", TargetNodeId = "b", Label = "5 < 8 & ok", Href = "data:text/html,fail" });

        var svg = chart.ToSvg();
        Assert(svg.Contains("A &lt; B &amp; C", StringComparison.Ordinal), "Topology labels should be XML-escaped.");
        Assert(svg.Contains("5 &lt; 8 &amp; ok", StringComparison.Ordinal), "Topology edge labels should be XML-escaped.");
        Assert(svg.Contains("href=\"/safe?x=1&amp;y=2\"", StringComparison.Ordinal), "Safe topology hrefs should be escaped and emitted.");
        Assert(!svg.Contains("javascript:", StringComparison.OrdinalIgnoreCase), "Unsafe javascript hrefs should not be emitted.");
        Assert(!svg.Contains("data:text/html", StringComparison.OrdinalIgnoreCase), "Unsafe data hrefs should not be emitted.");
    }

    private static void TopologyEmitsSafeCssAndDataHooks() {
        var chart = TopologyChart.Create()
            .WithId("hooks")
            .WithViewport(520, 300, 20)
            .WithLegend(null)
            .AddGroup("tier", "Tier", 30, 70, 420, 160, TopologyHealthStatus.Healthy, cssClass: "tier-primary tier.primary")
            .AddNode("api", "API", 70, 130, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, "tier", cssClass: "is-primary bad\"class")
            .AddNode("sql", "SQL", 270, 130, TopologyNodeKind.Database, TopologyHealthStatus.Warning, "tier")
            .AddEdge("api-sql", "api", "sql", "42 ms", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, cssClass: "critical:path odd$class", tertiaryLabel: "p95")
            .WithGroupSymbol("tier", "region")
            .WithGroupColor("tier", "#7C3AED")
            .WithNodeColor("sql", "#2563EB")
            .WithEdgeLineStyle("api-sql", TopologyEdgeLineStyle.Dotted)
            .WithEdgeMuted("api-sql");

        chart.Groups[0].Metadata["region/name"] = "EU & US";
        chart.Nodes[0].Metadata["role type"] = "public <api>";
        chart.Nodes[0].Metrics["latency.p95"] = "42 ms";
        chart.Edges[0].Metadata["transport"] = "https";
        chart.Edges[0].Metrics["queue depth"] = "7";

        var svg = chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false });
        Assert(svg.Contains(" tier-primary\"", StringComparison.Ordinal), "Topology SVG should emit sanitized caller CSS class hooks.");
        Assert(svg.Contains(" is-primary bad-class", StringComparison.Ordinal), "Topology SVG should sanitize unsafe node CSS class tokens.");
        Assert(svg.Contains(" critical:path odd-class", StringComparison.Ordinal), "Topology SVG should preserve safe CSS namespace separators and sanitize unsafe characters.");
        Assert(svg.Contains("data-group-symbol=\"region\"", StringComparison.Ordinal), "Topology SVG should expose group symbol hooks.");
        Assert(svg.Contains("data-group-color=\"#7C3AED\"", StringComparison.Ordinal), "Topology SVG should expose explicit group accent color hooks.");
        Assert(svg.Contains("data-node-color=\"#2563EB\"", StringComparison.Ordinal), "Topology SVG should expose explicit node accent color hooks.");
        Assert(svg.Contains("data-edge-line-style=\"Dotted\"", StringComparison.Ordinal), "Topology SVG should expose explicit edge line style hooks.");
        Assert(svg.Contains(">p95</text>", StringComparison.Ordinal), "Topology SVG should render tertiary edge labels.");
        Assert(svg.Contains("data-edge-muted=\"true\"", StringComparison.Ordinal), "Topology SVG should expose muted structural edge hooks.");
        Assert(svg.Contains("cfx-topology__edge-wrap--muted", StringComparison.Ordinal), "Topology SVG should emit muted edge CSS hooks.");
        Assert(svg.Contains("data-cfx-meta-region-name=\"EU &amp; US\"", StringComparison.Ordinal), "Topology SVG should emit escaped group metadata attributes.");
        Assert(svg.Contains("data-cfx-meta-role-type=\"public &lt;api&gt;\"", StringComparison.Ordinal), "Topology SVG should emit escaped node metadata attributes.");
        Assert(svg.Contains("data-cfx-metric-latency-p95=\"42 ms\"", StringComparison.Ordinal), "Topology SVG should emit node metric attributes.");
        Assert(svg.Contains("data-cfx-metric-queue-depth=\"7\"", StringComparison.Ordinal), "Topology SVG should emit edge metric attributes.");

        var withoutData = chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false, IncludeDataAttributes = false });
        Assert(!withoutData.Contains("data-cfx-meta-role-type", StringComparison.Ordinal), "Topology render options should allow metadata attributes to be omitted.");
        Assert(chart.ToPng(new TopologyRenderOptions { IncludeLegend = false }).Length > 64, "Topology PNG should ignore SVG-only hooks while still rendering.");
    }

    private static void TopologyHtmlPagesExposeSelectionInteractions() {
        var html = CreateSampleTopologyChart().ToHtmlPage(new TopologyRenderOptions { IncludeLegend = false });
        Assert(html.Contains("data-cfx-interactive=\"true\"", StringComparison.Ordinal), "Topology HTML pages should mark interactive wrappers.");
        Assert(html.Contains("cfx-topology-select", StringComparison.Ordinal), "Topology HTML pages should dispatch host-friendly selection events.");
        Assert(html.Contains("data-cfx-selection-kind", StringComparison.Ordinal), "Topology HTML pages should track selected topology element kind.");

        var staticHtml = CreateSampleTopologyChart().ToHtmlPage(new TopologyRenderOptions { EnableHtmlInteractions = false });
        Assert(staticHtml.Contains("data-cfx-interactive=\"false\"", StringComparison.Ordinal), "Topology HTML interactions should be optional.");
        Assert(!staticHtml.Contains("cfx-topology-select", StringComparison.Ordinal), "Static topology HTML pages should omit the interaction script.");
    }

    private static void TopologyValidatorReportsActionableErrors() {
        var chart = new TopologyChart {
            Id = "bad",
            Viewport = new TopologyViewport { Width = 400, Height = 240, Padding = 12 }
        };
        chart.Groups.Add(new TopologyGroup { Id = "g", Label = "Group", X = 10, Y = 10, Width = 100, Height = 80 });
        chart.Groups.Add(new TopologyGroup { Id = "g", Label = "Duplicate", X = 130, Y = 10, Width = 100, Height = 80 });
        chart.Nodes.Add(new TopologyNode { Id = "n", Label = "Node", GroupId = "missing", X = 40, Y = 80, Width = 120, Height = 64 });
        chart.Nodes.Add(new TopologyNode { Id = "n", Label = "Duplicate", X = 220, Y = 80, Width = 120, Height = 64 });
        chart.Edges.Add(new TopologyEdge { Id = "e", SourceNodeId = "n", TargetNodeId = "missing", Label = "bad" });

        var result = new TopologyChartValidator().Validate(chart);
        Assert(!result.IsValid, "Invalid topology charts should report validation errors.");
        Assert(result.Errors.Any(error => error.Code == "duplicate-node-id"), "Topology validator should detect duplicate node ids.");
        Assert(result.Errors.Any(error => error.Code == "duplicate-group-id"), "Topology validator should detect duplicate group ids.");
        Assert(result.Errors.Any(error => error.Code == "missing-node-group"), "Topology validator should detect nodes referencing missing groups.");
        Assert(result.Errors.Any(error => error.Code == "missing-edge-target"), "Topology validator should detect edges referencing missing target nodes.");
        AssertThrows<TopologyValidationException>(() => chart.ToSvg(), "Topology renderer should throw a clear validation exception for invalid data.");
    }

    private static void TopologyValidatorRejectsInvalidDimensions() {
        var chart = CreateSampleTopologyChart();
        chart.Nodes[0].Width = 0;
        chart.Groups[0].Height = -1;
        var result = new TopologyChartValidator().Validate(chart);
        Assert(result.Errors.Any(error => error.Code == "node-width"), "Topology validator should reject invalid node dimensions.");
        Assert(result.Errors.Any(error => error.Code == "group-height"), "Topology validator should reject invalid group dimensions.");
    }

    private static void TopologyViewsRenderFocusedSubsets() {
        var chart = CreateSampleTopologyChart();
        var svg = chart.ToSvg(new TopologyRenderOptions {
            View = new TopologyView { Id = "emea", Title = "EMEA only", GroupIds = { "EMEA" } }
        });

        Assert(svg.Contains("data-chart-id=\"site-topology-emea\"", StringComparison.Ordinal), "Topology views should scope chart ids predictably.");
        Assert(svg.Contains("EMEA only", StringComparison.Ordinal), "Topology views should support title overrides.");
        Assert(svg.Contains("EMEA Hub", StringComparison.Ordinal), "Topology views should keep nodes in selected groups.");
        Assert(!svg.Contains("AMER Hub", StringComparison.Ordinal), "Topology views should omit nodes outside selected groups.");
        Assert(!svg.Contains("APAC Hub", StringComparison.Ordinal), "Topology views should omit unrelated selected-group nodes.");
        Assert(chart.ToPng(new TopologyRenderOptions { View = new TopologyView { GroupIds = { "EMEA" } } }).Length > 64, "Focused topology views should render as PNG.");
    }

    private static void TopologyViewsFilterByKindStatusAndNeighbors() {
        var chart = TopologyChart.Create()
            .WithId("service-view")
            .WithTitle("Service View")
            .WithViewport(640, 360, 20)
            .AddNode("gateway", "Gateway", 60, 140, TopologyNodeKind.Gateway, TopologyHealthStatus.Healthy, symbol: "GW")
            .AddNode("api", "API", 230, 140, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, symbol: "API")
            .AddNode("queue", "Queue", 400, 70, TopologyNodeKind.Queue, TopologyHealthStatus.Warning, symbol: "Q")
            .AddNode("sql", "SQL", 400, 210, TopologyNodeKind.Database, TopologyHealthStatus.Healthy, symbol: "SQL")
            .AddEdge("gateway-api", "gateway", "api", "18 ms", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Healthy, TopologyDirection.Forward)
            .AddEdge("api-queue", "api", "queue", "lag 9m", TopologyEdgeKind.DataFlow, TopologyHealthStatus.Warning, TopologyDirection.Forward)
            .AddEdge("api-sql", "api", "sql", "412 ms", TopologyEdgeKind.Dependency, TopologyHealthStatus.Critical, TopologyDirection.Forward);

        var neighbor = chart.ToSvg(new TopologyRenderOptions {
            View = TopologyView.AroundNode("api", 1),
            IncludeLegend = false
        });
        Assert(neighbor.Contains("data-node-id=\"gateway\"", StringComparison.Ordinal), "Topology neighbor views should include incoming neighbors by default.");
        Assert(neighbor.Contains("data-node-id=\"queue\"", StringComparison.Ordinal), "Topology neighbor views should include outgoing neighbors by default.");
        Assert(neighbor.Contains("data-edge-id=\"api-sql\"", StringComparison.Ordinal), "Topology neighbor views should include connected edges within the requested depth.");

        var outbound = chart.ToSvg(new TopologyRenderOptions {
            View = new TopologyView { FocusNodeIds = { "api" }, NeighborDepth = 1, IncludeIncomingEdges = false },
            IncludeLegend = false
        });
        Assert(!outbound.Contains("data-node-id=\"gateway\"", StringComparison.Ordinal), "Topology neighbor views should honor incoming-edge traversal toggles.");
        Assert(outbound.Contains("data-node-id=\"queue\"", StringComparison.Ordinal), "Topology outbound neighbor views should keep downstream nodes.");

        var criticalDependencies = chart.ToSvg(new TopologyRenderOptions {
            View = new TopologyView { EdgeKinds = { TopologyEdgeKind.Dependency }, HealthStatuses = { TopologyHealthStatus.Critical } },
            IncludeLegend = false
        });
        Assert(criticalDependencies.Contains("data-node-id=\"api\"", StringComparison.Ordinal), "Topology status-filtered edge views should include matching edge endpoints.");
        Assert(criticalDependencies.Contains("data-node-id=\"sql\"", StringComparison.Ordinal), "Topology status-filtered edge views should include healthy endpoints of matching critical edges.");
        Assert(criticalDependencies.Contains("data-edge-id=\"api-sql\"", StringComparison.Ordinal), "Topology edge-kind views should include matching edges.");
        Assert(!criticalDependencies.Contains("data-edge-id=\"api-queue\"", StringComparison.Ordinal), "Topology edge-kind views should exclude non-matching edge kinds.");

        Assert(chart.ToPng(new TopologyRenderOptions { View = TopologyView.AroundNode("api", 1), IncludeLegend = false }).Length > 64, "Topology neighbor views should render as PNG.");
    }

    private static void TopologyEdgeLabelsAvoidNodes() {
        var chart = TopologyChart.Create()
            .WithId("edge-label-avoidance")
            .WithViewport(520, 300, 20)
            .WithLegend(null)
            .AddNode("left", "Left", 40, 120, TopologyNodeKind.Service, TopologyHealthStatus.Healthy)
            .AddNode("middle", "Middle", 180, 120, TopologyNodeKind.Queue, TopologyHealthStatus.Warning)
            .AddNode("right", "Right", 320, 120, TopologyNodeKind.Database, TopologyHealthStatus.Healthy)
            .AddEdge("left-right", "left", "right", "blocked", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Straight);

        var svg = chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false });
        Assert(svg.Contains("data-edge-id=\"left-right\" data-label-x=\"240\" data-label-y=\"92\"", StringComparison.Ordinal), "Topology edge labels should move away from node boxes when their midpoint overlaps a node.");
        Assert(chart.ToPng(new TopologyRenderOptions { IncludeLegend = false }).Length > 64, "Topology PNG should use the same edge-label placement planner.");
    }

    private static void TopologyEdgeLabelsAvoidUnrelatedEdges() {
        var chart = TopologyChart.Create()
            .WithId("edge-label-edge-avoidance")
            .WithViewport(560, 340, 20)
            .WithLegend(null)
            .AddNode("top", "Top", 200, 40, TopologyNodeKind.Service, TopologyHealthStatus.Healthy)
            .AddNode("bottom", "Bottom", 200, 240, TopologyNodeKind.Service, TopologyHealthStatus.Healthy)
            .AddNode("left", "Left", 40, 140, TopologyNodeKind.Endpoint, TopologyHealthStatus.Healthy)
            .AddNode("right", "Right", 420, 140, TopologyNodeKind.Endpoint, TopologyHealthStatus.Healthy)
            .AddEdge("vertical", "top", "bottom", "latency", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Straight)
            .AddEdge("horizontal", "left", "right", null, TopologyEdgeKind.Connectivity, TopologyHealthStatus.Healthy, TopologyDirection.None, TopologyEdgeRouting.Straight);

        var svg = chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false });
        Assert(svg.Contains("data-edge-id=\"vertical\" data-label-x=\"260\" data-label-y=\"142\"", StringComparison.Ordinal), "Topology edge labels should avoid unrelated edge segments when selecting deterministic label positions.");
        Assert(chart.ToPng(new TopologyRenderOptions { IncludeLegend = false }).Length > 64, "Topology PNG should share edge-aware topology label placement.");
    }

    private static void TopologyParallelEdgesRouteWithDeterministicOffsets() {
        var chart = TopologyChart.Create()
            .WithId("parallel-routes")
            .WithViewport(560, 300, 20)
            .WithLegend(null)
            .AddNode("left", "Left", 80, 130, TopologyNodeKind.Service, TopologyHealthStatus.Healthy)
            .AddNode("right", "Right", 340, 130, TopologyNodeKind.Database, TopologyHealthStatus.Warning)
            .AddEdge("left-right", "left", "right", "send", TopologyEdgeKind.DataFlow, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Straight)
            .AddEdge("right-left", "right", "left", "ack", TopologyEdgeKind.DataFlow, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Straight);

        var svg = chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false });
        Assert(svg.Contains("data-edge-id=\"left-right\"", StringComparison.Ordinal), "Topology parallel edge test should render the forward path.");
        Assert(svg.Contains("data-edge-id=\"right-left\"", StringComparison.Ordinal), "Topology parallel edge test should render the reverse path.");
        Assert(svg.Contains("data-route-offset=\"-13\"", StringComparison.Ordinal), "Topology parallel edges should expose deterministic negative route offsets.");
        Assert(svg.Contains("data-route-offset=\"13\"", StringComparison.Ordinal), "Topology parallel edges should expose deterministic positive route offsets.");
        Assert(svg.Contains("data-edge-id=\"left-right\" data-label-x=\"270\" data-label-y=\"149\"", StringComparison.Ordinal), "Topology parallel edge labels should follow the offset route instead of stacking on the center line.");
        Assert(svg.Contains("data-edge-id=\"right-left\" data-label-x=\"270\" data-label-y=\"175\"", StringComparison.Ordinal), "Topology reverse parallel edge labels should be separated from the forward label.");
        Assert(chart.ToPng(new TopologyRenderOptions { IncludeLegend = false }).Length > 64, "Topology PNG should use the same deterministic parallel edge offsets.");
    }

    private static void TopologyEdgesSupportManualWaypoints() {
        var chart = TopologyChart.Create()
            .WithId("waypoint-routes")
            .WithViewport(560, 300, 20)
            .WithLegend(null)
            .AddNode("left", "Left", 40, 40, TopologyNodeKind.Service, TopologyHealthStatus.Healthy)
            .AddNode("right", "Right", 360, 40, TopologyNodeKind.Database, TopologyHealthStatus.Warning)
            .AddEdge("left-right", "left", "right", "via route", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .WithEdgeWaypoints("left-right", new ChartForgeX.Primitives.ChartPoint(200, 180), new ChartForgeX.Primitives.ChartPoint(300, 180));

        var svg = chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false });
        Assert(svg.Contains("L 200 180 L 300 180", StringComparison.Ordinal), "Topology SVG should honor explicit edge waypoints as deterministic route bends.");
        Assert(svg.Contains("data-waypoint-count=\"2\"", StringComparison.Ordinal), "Topology SVG should expose waypoint counts as host interactivity hooks.");
        Assert(svg.Contains("data-edge-id=\"left-right\" data-label-x=\"", StringComparison.Ordinal) && svg.Contains("data-label-y=\"180\"", StringComparison.Ordinal), "Topology edge labels should use the waypoint route midpoint when it is clear of other topology elements.");
        Assert(chart.ToPng(new TopologyRenderOptions { IncludeLegend = false }).Length > 64, "Topology PNG should render explicit edge waypoints.");
    }

    private static void TopologyEdgesSupportPortsAndRouteLanes() {
        var straight = TopologyChart.Create()
            .WithId("ported-straight")
            .WithViewport(540, 260, 20)
            .WithLegend(null)
            .AddNode("left", "Left", 80, 120, TopologyNodeKind.Service, TopologyHealthStatus.Healthy)
            .AddNode("right", "Right", 340, 120, TopologyNodeKind.Database, TopologyHealthStatus.Warning)
            .AddEdge("left-right", "left", "right", "port", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Straight)
            .WithEdgePorts("left-right", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgeLineStyle("left-right", TopologyEdgeLineStyle.Dashed);

        var straightSvg = straight.ToSvg(new TopologyRenderOptions { IncludeLegend = false });
        Assert(straightSvg.Contains("data-source-port=\"Right\" data-target-port=\"Left\" data-route-lane=\"0\"", StringComparison.Ordinal), "Topology SVG should expose explicit edge ports and lanes for host adapters.");
        Assert(straightSvg.Contains("data-edge-line-style=\"Dashed\"", StringComparison.Ordinal), "Topology SVG should expose explicit edge line style metadata.");
        Assert(straightSvg.Contains("stroke-dasharray=\"8 5\"", StringComparison.Ordinal), "Explicit edge line style should override health-derived dash behavior.");
        Assert(straightSvg.Contains("d=\"M 207 152 L 333 152\"", StringComparison.Ordinal), "Straight topology edges should attach to the requested source and target ports.");

        var lane = TopologyChart.Create()
            .WithId("ported-lane")
            .WithViewport(620, 360, 20)
            .WithLegend(null)
            .AddNode("source", "Source", 80, 80, TopologyNodeKind.Service, TopologyHealthStatus.Healthy)
            .AddNode("target", "Target", 320, 240, TopologyNodeKind.Database, TopologyHealthStatus.Warning)
            .AddEdge("source-target", "source", "target", "lane", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .WithEdgePorts("source-target", TopologyEdgePort.Bottom, TopologyEdgePort.Top)
            .WithEdgeRouteLane("source-target", 24);

        var laneSvg = lane.ToSvg(new TopologyRenderOptions { IncludeLegend = false });
        Assert(laneSvg.Contains("data-source-port=\"Bottom\" data-target-port=\"Top\" data-route-lane=\"24\"", StringComparison.Ordinal), "Topology SVG should expose non-zero route lanes.");
        Assert(laneSvg.Contains("d=\"M 140 151 L 140 216 L 380 216 L 380 233\"", StringComparison.Ordinal), "Orthogonal topology edges should use requested ports and deterministic route lanes.");
        Assert(lane.ToPng(new TopologyRenderOptions { IncludeLegend = false }).Length > 64, "Topology PNG should render ported orthogonal route lanes.");
    }

    private static void TopologyRenderOptionsSupportDashboardPerspectives() {
        var chart = CreateSampleTopologyChart();
        chart.Edges[0].Metrics["queue"] = "Q:12";
        chart.Edges[0].Metrics["transport"] = "MPLS";
        chart.Edges[1].Metrics["queue"] = "Q:44";

        var options = new TopologyRenderOptions {
            IncludeGroups = false,
            IncludeNodeLabels = false,
            IncludeDirectionMarkers = false,
            HighlightStatuses = { TopologyHealthStatus.Critical }
        }
        .WithEdgeMetricLabels("queue", tertiaryMetricKey: "transport")
        .WithSelectedNode("tr-branch")
        .WithSelectedEdge("emea-tr");

        var svg = chart.ToSvg(options);

        Assert(!svg.Contains("data-cfx-role=\"topology-groups\"", StringComparison.Ordinal), "Topology render options should support ungrouped views.");
        Assert(!svg.Contains("marker-end=", StringComparison.Ordinal), "Topology render options should support direction-marker toggles.");
        Assert(!svg.Contains(">AMER Hub<", StringComparison.Ordinal), "Topology render options should support node label toggles.");
        Assert(svg.Contains("Q:44", StringComparison.Ordinal), "Topology render options should support metric-driven edge labels.");
        Assert(svg.Contains(">MPLS</text>", StringComparison.Ordinal), "Topology render options should allow metric-driven tertiary edge labels.");
        Assert(!svg.Contains(">142 ms<", StringComparison.Ordinal), "Metric-driven edge labels should replace the default edge label.");
        Assert(svg.Contains("cfx-topology--highlighted", StringComparison.Ordinal), "Topology render options should mark highlighted offenders.");
        Assert(svg.Contains("cfx-topology--dimmed", StringComparison.Ordinal), "Topology render options should dim non-highlighted elements.");
        Assert(svg.Contains("data-node-id=\"tr-branch\" data-node-kind=\"Branch\" data-node-display-mode=\"Card\" data-cfx-status=\"Critical\" data-cfx-selected=\"true\"", StringComparison.Ordinal), "Topology render options should mark selected nodes without filtering the chart.");
        Assert(svg.Contains("data-edge-id=\"emea-tr\"", StringComparison.Ordinal) && svg.Contains("data-cfx-selected=\"true\"", StringComparison.Ordinal), "Topology render options should mark selected edges without filtering the chart.");
        Assert(chart.ToPng(new TopologyRenderOptions { IncludeGroups = false, IncludeNodeLabels = false, EdgeLabelMetricKey = "queue", HighlightStatuses = { TopologyHealthStatus.Critical } }).Length > 64, "Topology PNG should support dashboard perspective options.");

        var groupHighlight = chart.ToSvg(new TopologyRenderOptions { HighlightGroupIds = { "EMEA" } });
        Assert(groupHighlight.Contains("id=\"site-topology-edge-emea-tr\" class=\"cfx-topology__edge-wrap cfx-topology__edge-wrap--critical cfx-topology--highlighted\"", StringComparison.Ordinal), "Topology group highlighting should keep connected edges visible.");

        var nakedEdgeLabels = chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false, IncludeEdgeLabelBackplates = false });
        var labelStart = nakedEdgeLabels.IndexOf("data-cfx-role=\"topology-edge-label\" data-edge-id=\"amer-emea\"", StringComparison.Ordinal);
        var labelEnd = nakedEdgeLabels.IndexOf("</g>", labelStart, StringComparison.Ordinal);
        Assert(labelStart >= 0 && labelEnd > labelStart, "Topology render options should still render naked edge-label groups.");
        Assert(!nakedEdgeLabels.Substring(labelStart, labelEnd - labelStart).Contains("<rect", StringComparison.Ordinal), "Topology render options should allow screenshot-style route labels without label backplates.");
    }

    private static void TopologyPresetsAndNodeDisplayModesRenderDenseViews() {
        var chart = CreateSampleTopologyChart();
        var compact = chart.ToSvg(TopologyRenderOptions.FromPreset(TopologyViewPreset.Compact));
        Assert(compact.Contains("data-node-display-mode=\"CompactCard\"", StringComparison.Ordinal), "Topology presets should set node display modes.");
        Assert(compact.Contains("width=\"108\"", StringComparison.Ordinal), "Compact topology nodes should use deterministic compact dimensions.");

        var offenders = chart.ToSvg(TopologyRenderOptions.FromPreset(TopologyViewPreset.Offenders));
        Assert(offenders.Contains("cfx-topology--highlighted", StringComparison.Ordinal), "Offender preset should highlight warning and critical elements.");
        Assert(offenders.Contains("cfx-topology--dimmed", StringComparison.Ordinal), "Offender preset should dim unrelated elements.");

        var dots = chart.ToSvg(new TopologyRenderOptions { NodeDisplayMode = TopologyNodeDisplayMode.Dot, IncludeNodeLabels = false, IncludeLegend = false });
        Assert(dots.Contains("data-node-display-mode=\"Dot\"", StringComparison.Ordinal), "Topology dot mode should expose display-mode metadata.");
        Assert(dots.Contains("cfx-topology__node-dot", StringComparison.Ordinal), "Topology dot mode should render dot nodes.");
        Assert(!dots.Contains("<rect class=\"cfx-topology__node-card\"", StringComparison.Ordinal), "Topology dot mode should avoid full node cards.");
        var tile = chart.ToSvg(new TopologyRenderOptions { NodeDisplayMode = TopologyNodeDisplayMode.Tile, IncludeLegend = false });
        Assert(tile.Contains("data-node-display-mode=\"Tile\"", StringComparison.Ordinal), "Topology tile mode should expose display-mode metadata.");
        Assert(tile.Contains("width=\"64\" height=\"46\"", StringComparison.Ordinal), "Topology tile mode should render compact icon tiles.");
        Assert(!tile.Contains("topology-node-subtitle", StringComparison.Ordinal), "Topology tile subtitles should be opt-in for dense diagrams.");
        var tileSubtitles = chart.ToSvg(new TopologyRenderOptions { NodeDisplayMode = TopologyNodeDisplayMode.Tile, IncludeTileSubtitles = true, IncludeLegend = false });
        Assert(tileSubtitles.Contains("data-cfx-role=\"topology-node-subtitle\"", StringComparison.Ordinal), "Topology tile mode should optionally render compact subtitle chips.");
        var cardSubtitleChips = chart.ToSvg(new TopologyRenderOptions { CardSubtitleMode = TopologyCardSubtitleMode.Chip, IncludeLegend = false });
        Assert(cardSubtitleChips.Contains("data-cfx-role=\"topology-node-card-subtitle\"", StringComparison.Ordinal), "Topology card mode should optionally render compact subtitle chips.");
        Assert(chart.ToPng(new TopologyRenderOptions { NodeDisplayMode = TopologyNodeDisplayMode.Pill, IncludeLegend = false }).Length > 64, "Topology PNG should render pill node display mode.");
        Assert(chart.ToPng(new TopologyRenderOptions { CardSubtitleMode = TopologyCardSubtitleMode.Chip, IncludeLegend = false }).Length > 64, "Topology PNG should render card subtitle chips.");
    }

    private static void TopologyNodesCanOverrideDisplayModeAndBadges() {
        var chart = TopologyChart.Create()
            .WithId("mixed-node-display")
            .WithViewport(520, 300, 20)
            .WithLegend(null)
            .AddGroup("site", "Site", 40, 70, 380, 170, TopologyHealthStatus.Healthy)
            .AddNode("hub", "Hub", 95, 135, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "site", symbol: "H")
            .AddNode("cluster", "Worker Pool", 270, 132, TopologyNodeKind.Service, TopologyHealthStatus.Warning, "site", symbol: "API")
            .AddNode("queue", "Queue", 335, 134, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, "site", symbol: "Q")
            .AddEdge("hub-cluster", "hub", "cluster", "18 ms", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Warning, TopologyDirection.Forward)
            .WithNodesDisplay(TopologyNodeKind.Service, TopologyNodeDisplayMode.Dot, "+")
            .WithNodeDisplay("cluster", TopologyNodeDisplayMode.Dot, "+12");

        var svg = chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false });
        Assert(svg.Contains("data-node-id=\"hub\" data-node-kind=\"Hub\" data-node-display-mode=\"Card\"", StringComparison.Ordinal), "Topology nodes without overrides should use the render-option display mode.");
        Assert(svg.Contains("data-node-id=\"cluster\" data-node-kind=\"Service\" data-node-display-mode=\"Dot\"", StringComparison.Ordinal), "Topology nodes should be able to override display mode individually.");
        Assert(svg.Contains("data-node-id=\"queue\" data-node-kind=\"Service\" data-node-display-mode=\"Dot\"", StringComparison.Ordinal), "Topology nodes should be able to override display mode by node kind.");
        Assert(svg.Contains("data-node-badge=\"+12\"", StringComparison.Ordinal), "Topology nodes should emit safe badge metadata.");
        Assert(svg.Contains("data-node-badge=\"+\"", StringComparison.Ordinal), "Kind-based node display overrides should optionally apply a shared badge.");
        Assert(svg.Contains("data-cfx-role=\"topology-node-badge\"", StringComparison.Ordinal), "Topology SVG should render node badge overlays.");
        Assert(svg.Contains("cfx-topology__node-dot", StringComparison.Ordinal), "Per-node dot display should render a compact marker without switching the whole chart to dots.");
        Assert(chart.ToPng(new TopologyRenderOptions { IncludeLegend = false }).Length > 64, "Topology PNG should render mixed node display modes and badges.");
    }

    private static void TopologyInferredLegendsAreGenericAndDataDriven() {
        var chart = TopologyChart.Create()
            .WithId("service-map")
            .WithTitle("Service Map")
            .WithViewport(520, 320, 20)
            .AddNode("api", "API", 80, 120, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, symbol: "API")
            .AddNode("sql", "SQL", 300, 120, TopologyNodeKind.Database, TopologyHealthStatus.Warning, symbol: "SQL")
            .AddEdge("api-sql", "api", "sql", "14 ms", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, TopologyDirection.Forward);

        var svg = chart.ToSvg(new TopologyRenderOptions { LegendMode = TopologyLegendMode.Auto });
        Assert(svg.Contains("data-cfx-role=\"topology-legend\"", StringComparison.Ordinal), "Topology auto legend should render when requested.");
        Assert(svg.Contains(">API Service<", StringComparison.Ordinal), "Topology auto legend should include node symbols from chart data.");
        Assert(svg.Contains(">SQL Database<", StringComparison.Ordinal), "Topology auto legend should include database symbols from chart data.");
        Assert(svg.Contains(">Dependency<", StringComparison.Ordinal), "Topology auto legend should include used edge kinds.");
        Assert(!svg.Contains("Domain controller", StringComparison.Ordinal), "Topology auto legend should stay product neutral.");
    }

    private static TopologyChart CreateSampleTopologyChart(TopologyLayoutMode layoutMode = TopologyLayoutMode.Manual) {
        var chart = new TopologyChart {
            Id = "site-topology",
            Title = "Site Topology",
            Subtitle = "Reusable topology model built from groups, nodes, and edges.",
            LayoutMode = layoutMode,
            Viewport = new TopologyViewport { Width = 720, Height = 460, Padding = 24 },
            Legend = TopologyLegend.Default()
        };

        chart.Groups.Add(new TopologyGroup { Id = "AMER", Label = "AMER", Subtitle = "47 sites", Status = TopologyHealthStatus.Healthy, X = 40, Y = 90, Width = 250, Height = 210, Href = "/topology/regions/amer", Tooltip = "AMER topology group" });
        chart.Groups.Add(new TopologyGroup { Id = "EMEA", Label = "EMEA", Subtitle = "56 sites", Status = TopologyHealthStatus.Warning, X = 390, Y = 90, Width = 250, Height = 210, Href = "/topology/regions/emea", Tooltip = "EMEA topology group" });
        chart.Nodes.Add(new TopologyNode { Id = "amer-hub", Label = "AMER Hub", Subtitle = "Hub site", Kind = TopologyNodeKind.Hub, Status = TopologyHealthStatus.Healthy, GroupId = "AMER", X = 95, Y = 160, Href = "/topology/sites/amer-hub", Tooltip = "AMER Hub (Healthy)" });
        chart.Nodes.Add(new TopologyNode { Id = "nva-east", Label = "NVA East", Subtitle = "Branch", Kind = TopologyNodeKind.Branch, Status = TopologyHealthStatus.Healthy, GroupId = "AMER", X = 95, Y = 245, Href = "/topology/sites/nva-east" });
        chart.Nodes.Add(new TopologyNode { Id = "emea-hub", Label = "EMEA Hub", Subtitle = "Hub site", Kind = TopologyNodeKind.Hub, Status = TopologyHealthStatus.Healthy, GroupId = "EMEA", X = 445, Y = 160, Href = "/topology/sites/emea-hub" });
        chart.Nodes.Add(new TopologyNode { Id = "tr-branch", Label = "TR Branch", Subtitle = "Critical", Kind = TopologyNodeKind.Branch, Status = TopologyHealthStatus.Critical, GroupId = "EMEA", X = 445, Y = 245, Href = "/topology/sites/tr-branch" });
        chart.Edges.Add(new TopologyEdge { Id = "amer-emea", SourceNodeId = "amer-hub", TargetNodeId = "emea-hub", Kind = TopologyEdgeKind.Link, Status = TopologyHealthStatus.Healthy, Direction = TopologyDirection.Bidirectional, Routing = TopologyEdgeRouting.Straight, Label = "24 ms", Href = "/topology/links/amer-emea" });
        chart.Edges.Add(new TopologyEdge { Id = "emea-tr", SourceNodeId = "emea-hub", TargetNodeId = "tr-branch", Kind = TopologyEdgeKind.Replication, Status = TopologyHealthStatus.Critical, Direction = TopologyDirection.Forward, Routing = TopologyEdgeRouting.Orthogonal, Label = "142 ms", SecondaryLabel = "queue 44" });
        return chart;
    }
}
