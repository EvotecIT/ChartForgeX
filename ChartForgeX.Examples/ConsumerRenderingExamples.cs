using ChartForgeX.Topology;
using ChartForgeX.Primitives;
using ChartForgeX.VisualArtifacts;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;

/// <summary>Small consumer repros for reviewing SVG and native PNG together.</summary>
public static class ConsumerRenderingExamples {
    /// <summary>Writes the reported consumer cases without changing their source data.</summary>
    public static void Write(string output) {
        Directory.CreateDirectory(output);
        var generatedNames = new List<string>();
        void SaveExample(string name, string svg, byte[] png) {
            Save(output, name, svg, png);
            generatedNames.Add(name);
        }
        var flow = "flowchart TD\n alert([Alert raised]) --> triage{Known issue?}\n triage -->|yes| runbook[Run the runbook]\n triage -->|no| owner[Page the service owner]\n runbook --> verify[Verify recovery]\n owner --> fix[Ship a fix]\n fix --> verify";
        var sources = new Dictionary<string, string> {
            ["sequence"] = "sequenceDiagram\n participant App\n participant IdP as Identity provider\n App->>IdP: Authorization request\n alt valid token\n IdP-->>App: Success\n else expired token\n IdP-->>App: Sign in again\n end",
            ["flow-td"] = flow,
            ["flow-lr"] = flow.Replace("flowchart TD", "flowchart LR") + "\n verify --> close((Close))",
            ["flow-branching-skip"] = "flowchart TD\n start[Alert] --> decision{Known issue?}\n decision -->|yes| short[Use runbook]\n decision -->|no| investigate[Investigate service]\n short --> verify[Verify recovery]\n investigate --> verify\n start -. escalation bypass .-> verify\n classDef oncall color:#ef4444,fill:#111827,stroke:#64748b\n class investigate oncall",
            ["state"] = "stateDiagram-v2\n [*] --> Draft\n Draft --> Review: submit\n Review --> Draft: request changes\n Review --> Approved: approve\n Approved --> Published: release\n Published --> [*]"
        };
        foreach (var pair in sources) {
            var result = MermaidRenderer.Render(pair.Value);
            if (result.HasErrors) throw new InvalidOperationException(string.Join("; ", result.Diagnostics.Select(d => d.Message)));
            SaveExample( pair.Key, result.Artifact!.ToSvg(), result.Artifact!.ToPng());
            var dark = MermaidRenderer.Render("%%{init: {'theme':'dark'}}%%\n" + pair.Value);
            if (dark.HasErrors) throw new InvalidOperationException("Cannot render dark " + pair.Key);
            SaveExample( pair.Key + "-dark", dark.Artifact!.ToSvg(), dark.Artifact!.ToPng());
            if (result.Artifact!.Model is TopologyChart fit) {
                fit.WithViewport(520, 260, 12);
                var options = new TopologyRenderOptions { FitContentToViewport = true, IncludeLegend = false };
                SaveExample( pair.Key + "-fit", fit.ToSvg(options), fit.ToPng(options));
            }
        }
        var wideFlow = MermaidRenderer.Render("%%{init: {'theme':'dark'}}%%\n" + flow.Replace("flowchart TD", "flowchart LR"), new MermaidRenderOptions {
            Flowchart = new MermaidFlowchartRenderOptions { Width = 3440, Height = 1440 }
        });
        if (wideFlow.HasErrors) throw new InvalidOperationException("Cannot render the wide Mermaid flowchart example.");
        SaveExample("flow-wide-panel-dark", wideFlow.Artifact!.ToSvg(), wideFlow.Artifact!.ToPng());
        var actorColor = MermaidRenderer.Render("flowchart LR\n A[Escalation] --> B[Response]\n classDef oncall color:#ef4444,fill:#111827,stroke:#64748b\n class A oncall");
        if (actorColor.HasErrors) throw new InvalidOperationException("Cannot render the Mermaid actor label-color example.");
        if (actorColor.Artifact!.Model is not TopologyChart actorChart)
            throw new InvalidOperationException("The Mermaid actor label-color example did not produce a topology chart.");
        actorChart.Nodes.Single(node => node.Id == "A").Shape = TopologyNodeShape.Actor;
        SaveExample("flow-class-color-actor", actorChart.ToSvg(), actorChart.ToPng());
        actorChart.Nodes.Single(node => node.Id == "A").Shape = null;
        actorChart.Nodes.Single(node => node.Id == "A").DisplayMode = TopologyNodeDisplayMode.Tile;
        var highlightOptions = new TopologyRenderOptions { IncludeLegend = false, DimmedOpacity = .5 };
        highlightOptions.HighlightNodeIds.Add("B");
        SaveExample("flow-class-color-highlight-dimming", actorChart.ToSvg(highlightOptions), actorChart.ToPng(highlightOptions));
        var topology = TopologyChart.Create().WithViewport(680, 330).WithLegend(null)
            .WithLayout(TopologyLayoutMode.Layered).WithLayoutDirection(TopologyLayoutDirection.LeftToRight)
            .AddAutoGroup("edge", "Edge").AddAutoGroup("core", "Core")
            .AddAutoNode("gw", "PAW-ADM-03", TopologyNodeKind.Network, groupId: "edge")
            .AddAutoNode("api", "Application API", TopologyNodeKind.Service, groupId: "core")
            .AddAutoNode("db", "SQL database", TopologyNodeKind.Database, groupId: "core")
            .AddEdge("gw-api", "gw", "api", "HTTPS").AddEdge("api-db", "api", "db", "SQL");
        SaveExample( "layered-groups", topology.ToSvg(), topology.ToPng());
        var fittedTopologyOptions = new TopologyRenderOptions { FitContentToViewport = true, IncludeLegend = false };
        SaveExample("topology-fit-layered-groups", topology.ToSvg(fittedTopologyOptions), topology.ToPng(fittedTopologyOptions));
        var longRoute = TopologyChart.Create().WithViewport(1200, 800).WithLegend(null).WithLayout(TopologyLayoutMode.Layered)
            .WithLayoutDirection(TopologyLayoutDirection.LeftToRight).AddAutoGroup("a", "A").AddAutoGroup("b", "B")
            .AddAutoNode("one", "One", groupId: "a").AddAutoNode("middle", "Middle", groupId: "a")
            .AddAutoNode("last", "Last", groupId: "a").AddAutoNode("other", "Other", groupId: "b")
            .AddEdge("first", "one", "middle").AddEdge("second", "middle", "last").AddEdge("skip", "one", "last", "Bypass");
        SaveExample( "layered-group-long-edge", longRoute.ToSvg(), longRoute.ToPng());
        SaveExample("topology-fit-layered-group-long-edge", longRoute.ToSvg(fittedTopologyOptions), longRoute.ToPng(fittedTopologyOptions));
        var reversedGroups = TopologyChart.Create().WithViewport(300, 330).WithLegend(null).WithLayout(TopologyLayoutMode.Layered)
            .WithLayoutDirection(TopologyLayoutDirection.LeftToRight).AddAutoGroup("b", "B").AddAutoGroup("a", "A")
            .AddAutoNode("source", "Source", groupId: "a").AddAutoNode("target", "Target", groupId: "b").AddEdge("link", "source", "target");
        SaveExample( "layered-group-declaration-order", reversedGroups.ToSvg(), reversedGroups.ToPng());
        var wideGrid = TopologyChart.Create().WithViewport(1200, 260).WithLegend(null).WithLayout(TopologyLayoutMode.GroupGrid)
            .AddAutoGroup("site-a", "Primary authentication and directory services").AddAutoGroup("site-b", "Secondary recovery services")
            .AddAutoNode("dc1", "DC01", groupId: "site-a").AddAutoNode("dns1", "DNS and catalog", groupId: "site-a")
            .AddAutoNode("dc2", "DC02", groupId: "site-b").AddAutoNode("backup", "Recovery coordinator", groupId: "site-b")
            .AddEdge("skip-grid", "dc1", "backup", "Site failover");
        var wideGridOptions = new TopologyRenderOptions { FitContentToViewport = true, IncludeLegend = false };
        SaveExample( "topology-group-grid-wide-short", wideGrid.ToSvg(wideGridOptions), wideGrid.ToPng(wideGridOptions));
        var growing = TopologyChart.Create().WithViewport(1300, 560).WithLegend(null).WithLayout(TopologyLayoutMode.Layered)
            .WithLayoutDirection(TopologyLayoutDirection.LeftToRight).AddAutoNode("gateway", "Gateway")
            .AddAutoNode("identity", "Identity service").AddEdge("request", "gateway", "identity", "HTTPS");
        var growingOptions = new TopologyRenderOptions { FitContentToViewport = true, IncludeTitle = false, IncludeLegend = false, IncludeIconLabels = true };
        SaveExample( "topology-fit-growth", growing.ToSvg(growingOptions), growing.ToPng(growingOptions));
        var sharedTrunk = TopologyChart.Create().WithViewport(900, 460).WithLayout(TopologyLayoutMode.Manual).WithLegend(null)
            .AddNode("target", "Service", 700, 180, width: 100, height: 44)
            .AddNode("north", "North input", 100, 60, width: 100, height: 44)
            .AddNode("middle", "Middle input", 100, 180, width: 100, height: 44)
            .AddNode("south", "South input", 100, 300, width: 100, height: 44)
            .AddEdge("north-link", "north", "target", "HTTPS").AddEdge("middle-link", "middle", "target", "HTTPS")
            .AddEdge("south-link", "south", "target", "HTTPS");
        foreach (var edge in sharedTrunk.Edges) edge.Routing = TopologyEdgeRouting.ObstacleAvoidingOrthogonal;
        var sharedTrunkOptions = new TopologyRenderOptions { FitContentToViewport = true, IncludeLegend = false, ShareIncomingTrunks = true };
        SaveExample("topology-fit-shared-trunk", sharedTrunk.ToSvg(sharedTrunkOptions), sharedTrunk.ToPng(sharedTrunkOptions));
        var selectedRoute = TopologyChart.Create().WithViewport(560, 320).WithLayout(TopologyLayoutMode.Manual).WithLegend(null)
            .AddNode("source", "Source", 70, 130, width: 100, height: 64).AddNode("target", "Target", 390, 130, width: 100, height: 64)
            .AddEdge("link", "source", "target", "Selected route").WithEdgeWaypoints("link", new ChartPoint(260, 18));
        var selectedRouteOptions = new TopologyRenderOptions { FitContentToViewport = true, IncludeLegend = false };
        selectedRouteOptions.SelectedEdgeIds.Add("link");
        SaveExample("topology-fit-selected-route-stroke", selectedRoute.ToSvg(selectedRouteOptions), selectedRoute.ToPng(selectedRouteOptions));
        var namedPorts = TopologyChart.Create().WithViewport(900, 340).WithLayout(TopologyLayoutMode.Manual).WithLegend(null)
            .AddNode("source", "Source", 50, 70, width: 130, height: 72).AddNode("target", "Target", 390, 70, width: 130, height: 72)
            .AddNodePort("source", "upper", TopologyEdgePort.Right, 0.2).AddNodePort("target", "lower", TopologyEdgePort.Left, 0.8)
            .AddEdge("link", "source", "target", "HTTPS").WithEdgeNamedPorts("link", "upper", "lower");
        SaveExample("topology-fit-named-ports", namedPorts.ToSvg(fittedTopologyOptions), namedPorts.ToPng(fittedTopologyOptions));
        var portFanOut = TopologyChart.Create().WithViewport(900, 400).WithLayout(TopologyLayoutMode.Manual).WithLegend(null)
            .AddNode("source", "Source", 50, 150, width: 120, height: 72)
            .AddNode("upper", "Upper target", 420, 40, width: 120, height: 72)
            .AddNode("lower", "Lower target", 420, 270, width: 120, height: 72)
            .AddEdge("upper-link", "source", "upper", "Primary").AddEdge("lower-link", "source", "lower", "Backup");
        SaveExample("topology-fit-port-fanout", portFanOut.ToSvg(fittedTopologyOptions), portFanOut.ToPng(fittedTopologyOptions));
        foreach (var mode in new[] { TopologyNodeDisplayMode.Tile, TopologyNodeDisplayMode.Icon }) {
            var badge = TopologyChart.Create().WithViewport(720, 300).WithLayout(TopologyLayoutMode.Layered)
                .WithLayoutDirection(TopologyLayoutDirection.TopToBottom).WithLegend(null)
                .AddAutoNode("one", "Database cluster").AddAutoNode("two", "Directory service")
                .AddAutoNode("three", "Recovery controller").AddEdge("first", "one", "two").AddEdge("second", "two", "three");
            foreach (var node in badge.Nodes) { node.DisplayMode = mode; node.Badge = "CRITICAL STATUS"; }
            SaveExample("topology-fit-external-badge-" + mode.ToString().ToLowerInvariant(), badge.ToSvg(growingOptions), badge.ToPng(growingOptions));
        }
        var endpointLabels = TopologyChart.Create().WithViewport(900, 360).WithLayout(TopologyLayoutMode.Manual).WithLegend(null)
            .AddNode("source", "Source", 120, 100, width: 80, height: 60).AddNode("target", "Target", 330, 100, width: 80, height: 60)
            .AddEdge("link", "source", "target").WithEdgeEndpointLabels("link", "Long source relationship label", "Long target relationship label");
        SaveExample("topology-fit-endpoint-labels", endpointLabels.ToSvg(fittedTopologyOptions), endpointLabels.ToPng(fittedTopologyOptions));
        var selectedStroke = TopologyChart.Create().WithViewport(520, 260).WithLayout(TopologyLayoutMode.Manual).WithLegend(null)
            .AddGroup("selected-group", "Selected group", 24, 24, 240, 150)
            .AddNode("selected", "Selected service", 30, 30, width: 160, height: 84, groupId: "selected-group");
        var selectedStrokeOptions = new TopologyRenderOptions { FitContentToViewport = true, IncludeLegend = false };
        selectedStrokeOptions.SelectedNodeIds.Add("selected");
        selectedStrokeOptions.SelectedGroupIds.Add("selected-group");
        SaveExample("topology-fit-selected-stroke", selectedStroke.ToSvg(selectedStrokeOptions), selectedStroke.ToPng(selectedStrokeOptions));
        var fittedCloud = TopologyChart.Create().WithViewport(560, 300).WithLayout(TopologyLayoutMode.Manual).WithLegend(null)
            .AddNode("cloud", "Cloud service", 10, 10, width: 180, height: 120);
        fittedCloud.Nodes[0].Shape = TopologyNodeShape.Cloud;
        var fittedCloudOptions = new TopologyRenderOptions { FitContentToViewport = true, IncludeLegend = false, IncludeNodeLabels = false };
        SaveExample("topology-fit-cloud-shape", fittedCloud.ToSvg(fittedCloudOptions), fittedCloud.ToPng(fittedCloudOptions));
        var unwrappedLabel = TopologyChart.Create().WithViewport(520, 260).WithLayout(TopologyLayoutMode.Manual).WithLegend(null)
            .AddNode("service", "A long service label that stays on one line", 40, 80, width: 260, height: 72);
        var unwrappedOptions = new TopologyRenderOptions { FitContentToViewport = true, IncludeLegend = false, WrapNodeLabels = false };
        SaveExample("topology-fit-unwrapped-label", unwrappedLabel.ToSvg(unwrappedOptions), unwrappedLabel.ToPng(unwrappedOptions));
        var packedGroup = TopologyChart.Create().WithViewport(1000, 520).WithLayout(TopologyLayoutMode.Layered)
            .WithLayoutDirection(TopologyLayoutDirection.LeftToRight).AddGroup("fixed", "Fixed shell", 0, 0, 520, 360)
            .AddAutoGroup("automatic", "Automatic shell").AddAutoNode("fixed-node", "Fixed member", groupId: "fixed")
            .AddAutoNode("automatic-node", "Automatic member", groupId: "automatic").AddEdge("link", "fixed-node", "automatic-node");
        SaveExample("topology-layered-single-automatic-group", packedGroup.ToSvg(), packedGroup.ToPng());
        var reverseFlowGroup = TopologyChart.Create().WithViewport(1000, 520).WithLayout(TopologyLayoutMode.Layered)
            .WithLayoutDirection(TopologyLayoutDirection.RightToLeft)
            .AddGroup("fixed", "Fixed shell", 0, 0, 520, 360).AddAutoGroup("automatic", "Automatic shell")
            .AddAutoNode("fixed-node", "Fixed member", groupId: "fixed").AddAutoNode("automatic-node", "Automatic member", groupId: "automatic");
        SaveExample("topology-layered-rtl-automatic-group", reverseFlowGroup.ToSvg(), reverseFlowGroup.ToPng());
        var interleavedGroup = TopologyChart.Create().WithViewport(1200, 620).WithLegend(null).WithLayout(TopologyLayoutMode.Layered)
            .WithLayoutDirection(TopologyLayoutDirection.LeftToRight)
            .AddGroup("before", "Explicit before", 300, 0, 220, 300).AddAutoGroup("middle", "Automatic middle")
            .AddGroup("after", "Explicit after", 0, 0, 220, 300)
            .AddNode("before-node", "Before", 350, 90, width: 120, height: 64, groupId: "before").AddAutoNode("middle-node", "Middle", groupId: "middle")
            .AddNode("after-node", "After", 50, 90, width: 120, height: 64, groupId: "after")
            .AddEdge("first", "before-node", "middle-node").AddEdge("second", "middle-node", "after-node");
        SaveExample("topology-layered-interleaved-explicit-groups", interleavedGroup.ToSvg(), interleavedGroup.ToPng());
        var labelColorModes = TopologyChart.Create().WithViewport(1120, 260).WithLayout(TopologyLayoutMode.Manual).WithLegend(null)
            .AddNode("card", "Card label", 20, 60, width: 150, height: 70).AddNode("shape", "Shape label", 230, 60, width: 150, height: 70)
            .AddNode("icon", "Icon label", 440, 60, width: 150, height: 70).AddNode("tile", "Tile label", 650, 60, width: 150, height: 70)
            .AddNode("diagram", "Diagram label", 860, 60, width: 150, height: 70);
        labelColorModes.Nodes[1].Shape = TopologyNodeShape.Diamond;
        labelColorModes.Nodes[2].DisplayMode = TopologyNodeDisplayMode.Icon;
        labelColorModes.Nodes[3].DisplayMode = TopologyNodeDisplayMode.Tile;
        labelColorModes.Nodes[4].Shape = TopologyNodeShape.Actor;
        foreach (var node in labelColorModes.Nodes) node.LabelColor = "var(--host-label, #ef4444)";
        var labelColorOptions = new TopologyRenderOptions {
            IncludeLegend = false, IncludeIconLabels = true,
            SvgColorVariables = new SvgColorVariables().Add("--host-label", ChartColor.FromHex("#ef4444"), ChartForgeX.Themes.SvgColorRole.Text)
        };
        labelColorOptions.HighlightNodeIds.Add("card");
        SaveExample("topology-label-colors-all-modes", labelColorModes.ToSvg(labelColorOptions), labelColorModes.ToPng(labelColorOptions));
        foreach (var mode in new[] { TopologyNodeDisplayMode.Tile, TopologyNodeDisplayMode.Icon }) {
            var captions = TopologyChart.Create().WithViewport(520, 150).WithLayout(TopologyLayoutMode.Layered)
                .WithLayoutDirection(TopologyLayoutDirection.TopToBottom)
                .AddAutoNode("one", "Primary directory controller")
                .AddAutoNode("two", "Secondary directory controller")
                .AddAutoNode("three", "Recovery services controller")
                .AddEdge("first", "one", "two").AddEdge("second", "two", "three");
            foreach (var node in captions.Nodes) node.DisplayMode = mode;
            SaveExample("topology-fit-captions-" + mode.ToString().ToLowerInvariant(), captions.ToSvg(growingOptions), captions.ToPng(growingOptions));
        }
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            var suffix = mode.ToString().ToLowerInvariant();
            var radar = Chart.Create().WithTheme(VisualTheme.Graphite(), mode).WithSize(520, 320)
                .WithXLabels("Mail auth", "DNSSEC", "TLS", "CT", "Policy", "Monitoring")
                .AddRadar("Current", new[] { 78d, 62, 74, 80, 70, 66 }.Select((v, i) => new ChartPoint(i + 1, v)).ToArray())
                .AddRadar("Target", Enumerable.Range(1, 6).Select(i => new ChartPoint(i, 80)).ToArray());
            SaveExample( "radar-" + suffix, radar.ToSvg(), radar.ToPng());
            var card = MetricCard.Create().WithTheme(VisualTheme.Graphite(), mode).WithSize(330, 150).WithPadding(0)
                .WithMetric("CPU", "38").WithStatus(VisualStatus.Info).WithSymbol("CPU")
                .WithMiniSparkline(new[] { 12d, 24, 18, 38, 27, 38 });
            SaveExample( "metric-" + suffix, card.ToSvg(), card.ToPng());
            var markerTheme = VisualTheme.Graphite().WithPalette(ChartColor.FromRgb(210, 30, 45), ChartColor.FromRgb(20, 170, 90), ChartColor.FromRgb(40, 90, 220));
            var list = ChartList.Create().WithTheme(markerTheme, mode).WithSize(330, 140).WithPadding(0).WithCard(false)
                .AddCheckItem("Backups verified", true, "02:10").AddCheckItem("DR test", false, "due 30 Oct");
            SaveExample( "list-" + suffix, list.ToSvg(), list.ToPng());
            var bars = Chart.Create().WithTheme(VisualTheme.Graphite(), mode).WithSize(520, 220)
                .WithXLabels("Alpha", "Beta", "Gamma").WithDataLabels(true)
                .AddBar("Count", new[] { new ChartPoint(1, 26), new ChartPoint(2, 24), new ChartPoint(3, 19) });
            SaveExample( "bars-" + suffix, bars.ToSvg(), bars.ToPng());
            var horizontal = Chart.Create().WithTheme(VisualTheme.Graphite(), mode).WithSize(520, 220)
                .WithXLabels("Alpha", "Beta", "Gamma").WithDataLabels(true)
                .AddHorizontalBar("Count", new[] { new ChartPoint(1, 26), new ChartPoint(2, 24), new ChartPoint(3, 19) });
            horizontal.Series[0].WithPointColor(0, ChartColor.FromRgb(255, 0, 0)).WithPointColor(1, ChartColor.FromRgb(0, 180, 0));
            SaveExample( "horizontal-" + suffix, horizontal.ToSvg(), horizontal.ToPng());
            var tree = Chart.Create().WithTheme(VisualTheme.Graphite(), mode).WithSize(520, 300)
                .AddTreemap("Work", new[] { new ChartHierarchyItem("a", "Alpha", value: 3), new ChartHierarchyItem("b", "Beta", value: 2) });
            SaveExample( "treemap-" + suffix, tree.ToSvg(), tree.ToPng());
            var start = new DateTime(2026, 1, 1);
            var calendar = Chart.Create().WithTheme(VisualTheme.Graphite(), mode).WithSize(1400, 220)
                .AddCalendarHeatmap("Sign-ins", Enumerable.Range(0, 126).Select(i => new ChartCalendarHeatmapItem(start.AddDays(i), i % 5 + 1)));
            SaveExample( "calendar-" + suffix, calendar.ToSvg(), calendar.ToPng());
            var largeText = VisualTheme.Graphite().WithTypography(new VisualTypography(dataLabelSize: 24));
            var table = ChartTable.Create().WithTheme(largeText, mode).WithSize(460, 260)
                .WithColumns("Check", "State").AddRow("Backups", "Verified").AddRow("Recovery test", "Scheduled");
            table.ConfigureRow(0, row => row.Cells[1].WithBadge("Verified", VisualStatus.Positive));
            SaveExample( "table-typography-" + suffix, table.ToSvg(), table.ToPng());
            var bullet = Chart.Create().WithTheme(VisualTheme.Graphite(), mode).WithSize(520, 200).AddBullet("Coverage", 72, 85);
            SaveExample( "bullet-" + suffix, bullet.ToSvg(), bullet.ToPng());
        }
        var timelineStart = new DateTime(2026, 9, 24, 0, 0, 0, DateTimeKind.Utc);
        var timeline = Chart.Create().WithTheme(VisualTheme.Graphite()).WithSize(760, 360).WithLegend(false).WithAxes(true).WithYAxisVisible(true)
            .WithStateCategories(new VisualStatusTokens().SeverityCategories())
            .AddGanttLane("Escalation lane", new[] { new ChartGanttLaneItem(timelineStart, timelineStart.AddMinutes(2), "high", "Investigate") })
            .AddGanttLane("Directory replication", new[] { new ChartGanttLaneItem(timelineStart, timelineStart.AddDays(2), "low", "Replication window") });
        for (var index = 0; index < 3; index++)
            timeline.AddGanttLane("Service " + index.ToString(System.Globalization.CultureInfo.InvariantCulture),
                new[] { new ChartGanttLaneItem(timelineStart, timelineStart.AddHours(4), "low") });
        SaveExample("timeline-dense-labels", timeline.ToSvg(), timeline.ToPng());
        File.WriteAllText(Path.Combine(output, "index.html"), "<!doctype html><meta charset=utf-8><link rel=icon href=\"data:,\"><title>Consumer rendering cases</title><style>body{font:16px sans-serif;background:#ccc}article{margin:30px}img{max-width:48%;vertical-align:top}</style>" +
            string.Join("", generatedNames.OrderBy(name => name, StringComparer.Ordinal).Select(name => "<article><h2>" + name + "</h2><img src='" + name + ".svg'><img src='" + name + ".png'></article>")));
    }
    private static void Save(string output, string name, string svg, byte[] png) {
        File.WriteAllText(Path.Combine(output, name + ".svg"), svg);
        File.WriteAllBytes(Path.Combine(output, name + ".png"), png);
        File.WriteAllText(Path.Combine(output, name + ".html"), "<!doctype html><meta charset=utf-8><link rel=icon href=\"data:,\"><title>" + name + "</title>" + svg);
    }
}
