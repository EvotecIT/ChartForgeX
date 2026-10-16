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
        var flow = "flowchart TD\n alert([Alert raised]) --> triage{Known issue?}\n triage -->|yes| runbook[Run the runbook]\n triage -->|no| owner[Page the service owner]\n runbook --> verify[Verify recovery]\n owner --> fix[Ship a fix]\n fix --> verify";
        var sources = new Dictionary<string, string> {
            ["sequence"] = "sequenceDiagram\n participant App\n participant IdP as Identity provider\n App->>IdP: Authorization request\n alt valid token\n IdP-->>App: Success\n else expired token\n IdP-->>App: Sign in again\n end",
            ["flow-td"] = flow,
            ["flow-lr"] = flow.Replace("flowchart TD", "flowchart LR") + "\n verify --> close((Close))",
            ["state"] = "stateDiagram-v2\n [*] --> Draft\n Draft --> Review: submit\n Review --> Draft: request changes\n Review --> Approved: approve\n Approved --> Published: release\n Published --> [*]"
        };
        foreach (var pair in sources) {
            var result = MermaidRenderer.Render(pair.Value);
            if (result.HasErrors) throw new InvalidOperationException(string.Join("; ", result.Diagnostics.Select(d => d.Message)));
            Save(output, pair.Key, result.Artifact!.ToSvg(), result.Artifact!.ToPng());
            var dark = MermaidRenderer.Render("%%{init: {'theme':'dark'}}%%\n" + pair.Value);
            if (dark.HasErrors) throw new InvalidOperationException("Cannot render dark " + pair.Key);
            Save(output, pair.Key + "-dark", dark.Artifact!.ToSvg(), dark.Artifact!.ToPng());
            if (result.Artifact!.Model is TopologyChart fit) {
                fit.WithViewport(520, 260, 12);
                var options = new TopologyRenderOptions { FitContentToViewport = true, IncludeLegend = false };
                Save(output, pair.Key + "-fit", fit.ToSvg(options), fit.ToPng(options));
            }
        }
        var topology = TopologyChart.Create().WithViewport(680, 330).WithLegend(null)
            .WithLayout(TopologyLayoutMode.Layered).WithLayoutDirection(TopologyLayoutDirection.LeftToRight)
            .AddAutoGroup("edge", "Edge").AddAutoGroup("core", "Core")
            .AddAutoNode("gw", "PAW-ADM-03", TopologyNodeKind.Network, groupId: "edge")
            .AddAutoNode("api", "Application API", TopologyNodeKind.Service, groupId: "core")
            .AddAutoNode("db", "SQL database", TopologyNodeKind.Database, groupId: "core")
            .AddEdge("gw-api", "gw", "api", "HTTPS").AddEdge("api-db", "api", "db", "SQL");
        Save(output, "layered-groups", topology.ToSvg(), topology.ToPng());
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            var suffix = mode.ToString().ToLowerInvariant();
            var radar = Chart.Create().WithTheme(VisualTheme.Graphite(), mode).WithSize(520, 320)
                .WithXLabels("Mail auth", "DNSSEC", "TLS", "CT", "Policy", "Monitoring")
                .AddRadar("Current", new[] { 78d, 62, 74, 80, 70, 66 }.Select((v, i) => new ChartPoint(i + 1, v)).ToArray())
                .AddRadar("Target", Enumerable.Range(1, 6).Select(i => new ChartPoint(i, 80)).ToArray());
            Save(output, "radar-" + suffix, radar.ToSvg(), radar.ToPng());
            var card = MetricCard.Create().WithTheme(VisualTheme.Graphite(), mode).WithSize(330, 150).WithPadding(0)
                .WithMetric("CPU", "38").WithStatus(VisualStatus.Warning).WithSymbol("CPU")
                .WithMiniSparkline(new[] { 12d, 24, 18, 38, 27, 38 });
            Save(output, "metric-" + suffix, card.ToSvg(), card.ToPng());
            var list = ChartList.Create().WithTheme(VisualTheme.Graphite(), mode).WithSize(330, 140).WithPadding(0).WithCard(false)
                .AddCheckItem("Backups verified", true, "02:10").AddCheckItem("DR test", false, "due 30 Oct");
            Save(output, "list-" + suffix, list.ToSvg(), list.ToPng());
            var bars = Chart.Create().WithTheme(VisualTheme.Graphite(), mode).WithSize(520, 220)
                .WithXLabels("Alpha", "Beta", "Gamma").WithDataLabels(true)
                .AddBar("Count", new[] { new ChartPoint(1, 26), new ChartPoint(2, 24), new ChartPoint(3, 19) });
            Save(output, "bars-" + suffix, bars.ToSvg(), bars.ToPng());
            var horizontal = Chart.Create().WithTheme(VisualTheme.Graphite(), mode).WithSize(520, 220)
                .WithXLabels("Alpha", "Beta", "Gamma").WithDataLabels(true)
                .AddHorizontalBar("Count", new[] { new ChartPoint(1, 26), new ChartPoint(2, 24), new ChartPoint(3, 19) });
            horizontal.Series[0].WithPointColor(0, ChartColor.FromRgb(255, 0, 0)).WithPointColor(1, ChartColor.FromRgb(0, 180, 0));
            Save(output, "horizontal-" + suffix, horizontal.ToSvg(), horizontal.ToPng());
            var tree = Chart.Create().WithTheme(VisualTheme.Graphite(), mode).WithSize(520, 300)
                .AddTreemap("Work", new[] { new ChartHierarchyItem("a", "Alpha", value: 3), new ChartHierarchyItem("b", "Beta", value: 2) });
            Save(output, "treemap-" + suffix, tree.ToSvg(), tree.ToPng());
            var start = new DateTime(2026, 1, 1);
            var calendar = Chart.Create().WithTheme(VisualTheme.Graphite(), mode).WithSize(1400, 220)
                .AddCalendarHeatmap("Sign-ins", Enumerable.Range(0, 126).Select(i => new ChartCalendarHeatmapItem(start.AddDays(i), i % 5 + 1)));
            Save(output, "calendar-" + suffix, calendar.ToSvg(), calendar.ToPng());
            var largeText = VisualTheme.Graphite().WithTypography(new VisualTypography(dataLabelSize: 24));
            var table = ChartTable.Create().WithTheme(largeText, mode).WithSize(460, 260)
                .WithColumns("Check", "State").AddRow("Backups", "Verified").AddRow("Recovery test", "Scheduled");
            Save(output, "table-typography-" + suffix, table.ToSvg(), table.ToPng());
            var bullet = Chart.Create().WithTheme(VisualTheme.Graphite(), mode).WithSize(520, 200).AddBullet("Coverage", 72, 85);
            Save(output, "bullet-" + suffix, bullet.ToSvg(), bullet.ToPng());
        }
        File.WriteAllText(Path.Combine(output, "index.html"), "<!doctype html><meta charset=utf-8><link rel=icon href=\"data:,\"><title>Consumer rendering cases</title><style>body{font:16px sans-serif;background:#ccc}article{margin:30px}img{max-width:48%;vertical-align:top}</style>" +
            string.Join("", Directory.GetFiles(output, "*.svg").OrderBy(x => x).Select(f => "<article><h2>" + Path.GetFileNameWithoutExtension(f) + "</h2><img src='" + Path.GetFileName(f) + "'><img src='" + Path.GetFileNameWithoutExtension(f) + ".png'></article>")));
    }
    private static void Save(string output, string name, string svg, byte[] png) {
        File.WriteAllText(Path.Combine(output, name + ".svg"), svg);
        File.WriteAllBytes(Path.Combine(output, name + ".png"), png);
        File.WriteAllText(Path.Combine(output, name + ".html"), "<!doctype html><meta charset=utf-8><link rel=icon href=\"data:,\"><title>" + name + "</title>" + svg);
    }
}
