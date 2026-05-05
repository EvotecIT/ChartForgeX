using System.Text;
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
        var demos = new[] {
            ("site-topology", TopologyDemoCharts.SiteTopologyDemo()),
            ("replication-mesh", TopologyDemoCharts.ReplicationMeshDemo()),
            ("subnets-site-links", TopologyDemoCharts.SubnetsSiteLinksDemo()),
            ("geographic-topology", TopologyDemoCharts.GeographicTopologyDemo())
        };

        foreach (var demo in demos) {
            demo.Item2.SaveSvg(Path.Combine(target, demo.Item1 + ".svg"));
            demo.Item2.SaveHtml(Path.Combine(target, demo.Item1 + ".html"));
            demo.Item2.SavePng(Path.Combine(target, demo.Item1 + ".png"));
        }

        WriteView(demos[0].Item2, Path.Combine(target, "site-topology-emea-view"), new TopologyView { Id = "emea-view", Title = "Site Topology - EMEA View", Subtitle = "Focused view rendered from the same reusable topology model.", GroupIds = { "EMEA" } });
        WriteView(demos[1].Item2, Path.Combine(target, "replication-mesh-critical-view"), new TopologyView { Id = "critical-view", Title = "Replication Mesh - Critical Paths", Subtitle = "Focused view using explicit node ids and connected critical paths.", NodeIds = { "fra-dc1", "fra-dc2", "sin-dc1", "sin-dc2", "sfo-dc2" } });

        File.WriteAllText(Path.Combine(target, "index.html"), BuildIndex(demos), Encoding.UTF8);
    }

    private static void WriteView(TopologyChart chart, string pathWithoutExtension, TopologyView view) {
        var options = new TopologyRenderOptions { View = view };
        chart.SaveSvg(pathWithoutExtension + ".svg", options);
        chart.SaveHtml(pathWithoutExtension + ".html", options);
        chart.SavePng(pathWithoutExtension + ".png", options);
    }

    private static string BuildIndex((string Name, TopologyChart Chart)[] demos) {
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
        foreach (var demo in demos) {
            sb.AppendLine("<section class=\"demo\">");
            sb.AppendLine("<h2>" + Escape(demo.Chart.Title ?? demo.Name) + "</h2>");
            sb.AppendLine(demo.Chart.ToSvg());
            sb.AppendLine("</section>");
        }

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
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
