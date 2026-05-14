using System.Text;

internal static partial class TopologyVisualExamples {
    private static void WriteManifest(string target, IReadOnlyList<VisualArtifact> artifacts) {
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("  \"name\": \"ChartForgeX topology visual coverage\",");
        sb.AppendLine("  \"host\": \"HtmlForgeX-ready SVG/HTML/PNG artifacts\",");
        sb.AppendLine("  \"interactiveContract\": \"Inline SVG elements expose ids, CSS classes, hrefs, title tooltips, and data-cfx/data-node/data-edge hooks. Complete topology HTML pages are static by default and opt into cfx-topology-* interaction events when requested.\",");
        sb.AppendLine("  \"baselinePolicy\": \"Topology, geographic topology, and topology-adjacent map artifacts are release-gated by this manifest, required SVG metadata hooks, SVG/HTML/PNG file generation, and PNG size checks. They intentionally stay outside visual-baseline.json until dense routing and geographic layout polish settle enough for numeric visual baselines.\",");
        sb.AppendLine("  \"baselineScope\": \"visual-capability-manifest\",");
        sb.AppendLine("  \"baselineCandidates\": [");
        sb.AppendLine("    \"visual-topology-explorer\",");
        sb.AppendLine("    \"visual-entity-relationship-overview\",");
        sb.AppendLine("    \"visual-secure-access-arbitrary-icons\",");
        sb.AppendLine("    \"visual-mini-correlation-map\",");
        sb.AppendLine("    \"visual-evidence-timeline-relationship\",");
        sb.AppendLine("    \"visual-replication-mesh-explorer\",");
        sb.AppendLine("    \"visual-subnets-site-links-map\",");
        sb.AppendLine("    \"visual-nested-user-hierarchy\",");
        sb.AppendLine("    \"visual-nested-user-hierarchy-bottom-top\",");
        sb.AppendLine("    \"visual-nested-user-hierarchy-left-right\",");
        sb.AppendLine("    \"visual-nested-user-hierarchy-right-left\",");
        sb.AppendLine("    \"visual-geographic-topology-map\"");
        sb.AppendLine("  ],");
        sb.AppendLine("  \"artifacts\": [");
        for (var i = 0; i < artifacts.Count; i++) {
            var artifact = artifacts[i];
            sb.AppendLine("    {");
            sb.AppendLine("      \"name\": \"" + EscapeJson(artifact.Name) + "\",");
            sb.AppendLine("      \"title\": \"" + EscapeJson(artifact.Title) + "\",");
            sb.AppendLine("      \"kind\": \"" + EscapeJson(artifact.Kind) + "\",");
            sb.AppendLine("      \"svg\": \"" + EscapeJson(artifact.Name + ".svg") + "\",");
            sb.AppendLine("      \"html\": \"" + EscapeJson(artifact.Name + ".html") + "\",");
            sb.AppendLine("      \"png\": \"" + EscapeJson(artifact.Name + ".png") + "\",");
            sb.AppendLine("      \"notes\": \"" + EscapeJson(artifact.Notes) + "\"");
            sb.Append("    }");
            sb.AppendLine(i == artifacts.Count - 1 ? string.Empty : ",");
        }

        sb.AppendLine("  ]");
        sb.AppendLine("}");
        File.WriteAllText(Path.Combine(target, "visual-capability-manifest.json"), sb.ToString(), Encoding.UTF8);
    }

    private static void WriteCoverageIndex(string target, IReadOnlyList<VisualArtifact> artifacts) {
        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html>");
        sb.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.AppendLine("<title>ChartForgeX Topology Visual Coverage</title>");
        sb.AppendLine("<style>body{margin:0;background:#f8fafc;color:#0f172a;font-family:Inter,Segoe UI,system-ui,sans-serif;padding:24px}main{max-width:1280px;margin:0 auto}h1{font-size:24px;margin:0 0 8px}.lead{color:#475569;margin:0 0 20px}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(300px,1fr));gap:16px}.card{background:white;border:1px solid #dbe3ef;border-radius:10px;padding:14px;box-shadow:0 12px 28px rgba(15,23,42,.06)}.card h2{font-size:15px;margin:0 0 8px}.kind{font-size:11px;font-weight:700;text-transform:uppercase;color:#2563eb}.notes{font-size:12px;color:#475569;min-height:48px}.links{display:flex;gap:10px;flex-wrap:wrap}.links a{font-size:12px;color:#2563eb;text-decoration:none;font-weight:700}.preview{display:block;width:100%;height:auto;border:1px solid #e2e8f0;border-radius:8px;margin:10px 0;background:white}</style>");
        sb.AppendLine("</head><body><main>");
        sb.AppendLine("<h1>ChartForgeX Topology Visual Coverage</h1>");
        sb.AppendLine("<p class=\"lead\">Generated SVG, HTML, and PNG artifacts for topology and map-based visuals. Topology HTML pages include lightweight selection events; SVG and map outputs stay host-ready through stable metadata hooks for HtmlForgeX.</p>");
        sb.AppendLine("<div class=\"grid\">");
        foreach (var artifact in artifacts) {
            sb.AppendLine("<article class=\"card\">");
            sb.AppendLine("<div class=\"kind\">" + EscapeHtml(artifact.Kind) + "</div>");
            sb.AppendLine("<h2>" + EscapeHtml(artifact.Title) + "</h2>");
            sb.AppendLine("<p class=\"notes\">" + EscapeHtml(artifact.Notes) + "</p>");
            sb.AppendLine("<img class=\"preview\" loading=\"lazy\" src=\"" + EscapeHtml(artifact.Name) + ".svg\" alt=\"" + EscapeHtml(artifact.Title) + "\">");
            sb.AppendLine("<div class=\"links\"><a href=\"" + EscapeHtml(artifact.Name) + ".svg\">SVG</a><a href=\"" + EscapeHtml(artifact.Name) + ".html\">HTML</a><a href=\"" + EscapeHtml(artifact.Name) + ".png\">PNG</a></div>");
            sb.AppendLine("</article>");
        }

        sb.AppendLine("</div>");
        sb.AppendLine("</main></body></html>");
        File.WriteAllText(Path.Combine(target, "visual-coverage.html"), sb.ToString(), Encoding.UTF8);
    }

    private static string EscapeHtml(string value) => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    private static string EscapeJson(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");

    private readonly struct VisualArtifact {
        public VisualArtifact(string name, string title, string kind, string notes) {
            Name = name;
            Title = title;
            Kind = kind;
            Notes = notes;
        }

        public readonly string Name;
        public readonly string Title;
        public readonly string Kind;
        public readonly string Notes;
    }
}
