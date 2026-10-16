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
        var topology = TopologyChart.Create().WithViewport(680, 330).WithLegend(null)
            .WithLayout(TopologyLayoutMode.Layered).WithLayoutDirection(TopologyLayoutDirection.LeftToRight)
            .AddAutoGroup("edge", "Edge").AddAutoGroup("core", "Core")
            .AddAutoNode("gw", "PAW-ADM-03", TopologyNodeKind.Network, groupId: "edge")
            .AddAutoNode("api", "Application API", TopologyNodeKind.Service, groupId: "core")
            .AddAutoNode("db", "SQL database", TopologyNodeKind.Database, groupId: "core")
            .AddEdge("gw-api", "gw", "api", "HTTPS").AddEdge("api-db", "api", "db", "SQL");
        SaveExample( "layered-groups", topology.ToSvg(), topology.ToPng());
        var longRoute = TopologyChart.Create().WithViewport(1200, 800).WithLegend(null).WithLayout(TopologyLayoutMode.Layered)
            .WithLayoutDirection(TopologyLayoutDirection.LeftToRight).AddAutoGroup("a", "A").AddAutoGroup("b", "B")
            .AddAutoNode("one", "One", groupId: "a").AddAutoNode("middle", "Middle", groupId: "a")
            .AddAutoNode("last", "Last", groupId: "a").AddAutoNode("other", "Other", groupId: "b")
            .AddEdge("first", "one", "middle").AddEdge("second", "middle", "last").AddEdge("skip", "one", "last", "Bypass");
        SaveExample( "layered-group-long-edge", longRoute.ToSvg(), longRoute.ToPng());
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
        var growingOptions = new TopologyRenderOptions { FitContentToViewport = true, IncludeTitle = false, IncludeLegend = false };
        SaveExample( "topology-fit-growth", growing.ToSvg(growingOptions), growing.ToPng(growingOptions));
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
