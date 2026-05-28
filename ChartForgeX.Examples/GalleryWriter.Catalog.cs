/// <summary>
/// Writes the grouped static example catalog page.
/// </summary>
public static partial class GalleryWriter {
    private static readonly CatalogGroup[] CatalogGroups = {
        new(
            "Report Essentials",
            "Core chart types for operational reports, trend panels, and executive summaries.",
            "domain-security-dark",
            "ct-volume-light",
            "ct-regional-light",
            "monthly-posture-dark",
            "license-cost-light",
            "policy-backlog-step-area-light",
            "endpoint-latency-light",
            "endpoint-latency-range-light",
            "observed-remediation-trend-light",
            "annotation-edge-dark",
            "data-label-readability-dark"),
        new(
            "Bars and Composition",
            "Category comparison, stacked contribution, proportions, and sorted contribution views.",
            "security-findings-grouped-light",
            "domain-findings-stacked-dark",
            "domain-control-horizontal-light",
            "result-mix-donut",
            "result-mix-pie-light",
            "zero-value-donut-light",
            "zero-value-polar-area-light",
            "zero-value-funnel-stage-light",
            "findings-composition-treemap-light",
            "findings-pareto-dark"),
        new(
            "Statistical and Distribution",
            "Charts for spread, uncertainty, paired deltas, clustering, and observed ranges.",
            "endpoint-latency-histogram-light",
            "endpoint-latency-boxplot-dark",
            "exposure-clusters-bubble-light",
            "detection-confidence-errorbar-dark",
            "forecast-envelope-rangeband-dark",
            "forecast-interval-rangearea-light",
            "remediation-lift-dumbbell-light",
            "control-improvement-slope-light"),
        new(
            "Specialized Reports",
            "Gauge, circle, bullet, waterfall, radial, funnel, timeline, financial range, and compact outputs.",
            "security-posture-gauge-dark",
            "policy-readiness-circle-light",
            "control-coverage-radialbar-dark",
            "control-targets-bullet-dark",
            "remediation-impact-waterfall-dark",
            "security-posture-radar-dark",
            "control-contribution-polar-area-light",
            "domain-remediation-funnel-dark",
            "domain-remediation-timeline-light",
            "domain-remediation-gantt-light",
            "finding-flow-sankey-light",
            "control-hierarchy-tree-light",
            "control-partition-sunburst-aurora",
            "audience-pictorial-candy",
            "support-themes-word-cloud-editorial",
            "signal-windows-candlestick-light",
            "signal-windows-ohlc-dark",
            "policy-state-step-line-dark",
            "control-readiness-lollipop-dark",
            "warnings-sparkline"),
        new(
            "Wellness Dashboards",
            "Composed mobile-style health and nutrition cards plus generic layered radial primitives.",
            "wellness-layered-radial-progress",
            "wellness-weight-data-gauge",
            "wellness-calories-intake-dashboard",
            "wellness-weekly-progress-dashboard"),
        new(
            "Dashboard Patterns",
            "Reusable KPI cards, progress rows, honeycomb utilization, ticketing analytics, MRR trend cards, and composed dashboard grids.",
            "dashboard-kpi-bar-sparkline",
            "dashboard-device-progress-bars",
            "dashboard-project-activity-sparkline",
            "dashboard-attendance-hexbin-heatmap",
            "dashboard-mrr-trend-card",
            "dashboard-mrr-driver-bars",
            "dashboard-ticketing-revenue-trend",
            "dashboard-ticketing-package-mix",
            "dashboard-ticketing-promoter-attribution",
            "dashboard-ticketing-guest-funnel",
            "dashboard-ticketing-analytics-grid",
            "dashboard-hr-overview-grid",
            "dashboard-saas-mrr-grid",
            "dashboard-premium-trend-style",
            "dashboard-segmented-column-style",
            "dashboard-segmented-horizontal-style",
            "report-summary-metric-strip",
            "transparent-report-summary-metric-strip",
            "powerbginfo-endpoint-snapshot",
            "dashboard-restaurant-reports-range-strip",
            "dashboard-restaurant-order-status",
            "dashboard-restaurant-customers-hexbin",
            "dashboard-restaurant-occupation-bars",
            "dashboard-restaurant-weekly-summary",
            "dashboard-restaurant-overview-grid",
            "dashboard-chart-portfolio-grid"),
        new(
            "Dashboard Inspiration",
            "Screenshot-inspired operations, project, HR, and payment analytics compositions used to drive the next visual-block capabilities.",
            "dashboard-appointment-operations-grid",
            "dashboard-channel-share-capsule-loop",
            "dashboard-certificate-inventory-composition-strip",
            "dashboard-content-performance-rows",
            "dashboard-conversion-funnel-columns",
            "dashboard-project-progress-card",
            "dashboard-project-task-composition-card",
            "dashboard-project-track-card",
            "dashboard-project-schedule-timeline",
            "dashboard-shipment-activity-panel",
            "dashboard-hr-operations-grid",
            "dashboard-payment-analytics-grid"),
        new(
            "Maps and Geography",
            "Dotted world maps, focused regional viewports, route overlays, and value-colored region maps.",
            "travel-dotted-map-dark",
            "sea-route-suez-cape-map-light",
            "map-viewport-showcase-grid",
            "revenue-europe-country-map-light",
            "revenue-region-map-us-states-light",
            "revenue-tile-map-us-states-light",
            "map-world-route-light",
            "map-europe-route-light",
            "map-north-america-route-light",
            "map-south-america-route-light",
            "map-africa-route-light",
            "map-asia-route-light",
            "map-oceania-route-light",
            "map-poland-route-light",
            "eu-industrial-births-nuts3-light",
            "industrial-births-region-map-us-states-light",
            "industrial-births-poland-nuts3-light",
            "industrial-births-france-nuts3-light",
            "industrial-births-germany-nuts3-light"),
        new(
            "Topology Diagrams",
            "Product-neutral topology maps for sites, replication, connectivity, geography, subnets, services, and reusable icon catalogs.",
            "site-topology",
            "replication-mesh",
            "dc-connectivity",
            "geographic-topology",
            "subnets-site-links",
            "service-dependency",
            "icon-palette",
            "icon-stencil-browser"),
        new(
            "Topology Focus Views",
            "Focused topology variants for regional drilldowns, critical views, domain-controller filters, offenders, compact service maps, and dependency neighborhoods.",
            "site-topology-emea-view",
            "replication-mesh-critical-view",
            "replication-mesh-dc-view",
            "replication-mesh-offenders-view",
            "service-dependency-compact-view",
            "service-dependency-api-neighbors-view",
            "service-dependency-critical-dependencies-view"),
        new(
            "Topology Interactive Demos",
            "Topology-first HTML demos for route scenarios, graph exploration, scoped deep links, and host dashboard events.",
            "topology-demo",
            "visual-force-relationship-graph",
            "visual-force-busy-relationship-graph",
            "visual-relationship-radial-ego-graph",
            "visual-relationship-radial-500-ego-graph",
            "visual-replication-mesh-explorer"),
        new(
            "Relationship Diagrams",
            "Topology-rendered relationship maps that stay in SVG/PNG comparison baselines for release visual review.",
            "visual-entity-relationship-overview",
            "visual-evidence-timeline-relationship",
            "visual-impact-dependency-overview",
            "visual-mini-correlation-map",
            "visual-ownership-evidence-bundle",
            "visual-secure-access-arbitrary-icons"),
        new(
            "Topology Visual Showcase",
            "Reusable directory, team, region, replication, service, coverage, and WAN topology examples that demonstrate ChartForgeX as a diagram engine.",
            "visual-ad-sites-hierarchy",
            "visual-coverage",
            "visual-dc-connectivity-map",
            "visual-directory-health-replication",
            "visual-directory-level-window",
            "visual-geographic-region-map",
            "visual-geographic-topology-map",
            "visual-nested-user-hierarchy",
            "visual-nested-user-hierarchy-bottom-top",
            "visual-nested-user-hierarchy-left-right",
            "visual-nested-user-hierarchy-right-left",
            "visual-replication-health-hub",
            "visual-reusable-regional-topology",
            "visual-service-dependency-map",
            "visual-site-distribution-map",
            "visual-subnets-site-links-map",
            "visual-team-hierarchy-builder",
            "visual-topology-explorer",
            "visual-wan-latency-map"),
        new(
            "Interactive HTML Adapter",
            "Host-side HTML adapter demos for interactive chart review and synchronized dashboard exploration.",
            "domain-security-interactive",
            "executive-interactive-dashboard"),
        new(
            "Visual Systems",
            "Themes, brand kits, palettes, fonts, and pictorial symbol picker outputs.",
            "theme-font-showcase-grid",
            "brand-kit-showcase-grid",
            "palette-swatch-showcase-grid",
            "pictorial-symbol-showcase-grid",
            "pictorial-isotype-showcase-grid",
            "people-infographic-showcase-grid",
            "word-cloud-control-showcase-grid",
            "data-label-placement-showcase-grid",
            "point-color-customization-showcase-grid",
            "text-style-showcase-editorial"),
        new(
            "Matrices and Small Multiples",
            "Dense review surfaces for grids, heatmaps, and shared-axis panels.",
            "control-coverage-heatmap-dark",
            "developer-consistency-calendar-light",
            "control-scorecards-grid",
            "shared-axis-coverage-grid",
            "domain-signal-mix-stacked-area-dark")
    };

    private static void WriteCatalog(string output, string[] htmlFiles) {
        var cards = htmlFiles
            .Select(file => new CatalogCard(
                file,
                Path.GetFileName(file),
                Path.GetFileNameWithoutExtension(file),
                ReadTitle(file)))
            .OrderBy(card => card.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var assigned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<!doctype html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.AppendLine("<title>ChartForgeX Chart Catalog</title>");
        sb.AppendLine("<style>");
        sb.AppendLine(":root{color-scheme:dark;--bg:#151515;--panel:#202224;--panel2:#191a1c;--preview:#161718;--line:#34383c;--softline:#2b2f33;--text:#f4f0e8;--muted:#beb7a8;--accent:#2dd4bf;--gold:#f4b860}");
        sb.AppendLine("*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--text);font-family:Inter,ui-sans-serif,system-ui,Segoe UI,Arial,sans-serif;-webkit-font-smoothing:antialiased}");
        sb.AppendLine("header{padding:34px 42px 22px;border-bottom:1px solid var(--line);background:#1a1b1d}h1{margin:0 0 8px;font-size:29px;line-height:1.12}p{margin:0;color:var(--muted);font-size:14px;max-width:840px}.nav{display:flex;flex-wrap:wrap;gap:8px;margin-top:16px}.nav a,.links a{color:var(--accent);text-decoration:none;font-size:12px;font-weight:800;border:1px solid rgba(45,212,191,.34);border-radius:6px;padding:6px 8px}.nav a:hover,.links a:hover{background:rgba(45,212,191,.12)}");
        sb.AppendLine("main{padding:30px 42px 48px}.section{margin:0 0 38px}.section-head{display:flex;align-items:end;justify-content:space-between;gap:18px;margin:0 0 16px;border-bottom:1px solid var(--line);padding-bottom:12px}h2{margin:0 0 6px;font-size:21px;line-height:1.15}.count{flex:0 0 auto;border:1px solid rgba(244,184,96,.44);border-radius:999px;color:var(--gold);font-size:12px;font-weight:850;padding:6px 10px}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(min(100%,540px),1fr));gap:20px;align-items:start}");
        sb.AppendLine(".card{min-width:0;border:1px solid var(--line);border-radius:8px;background:linear-gradient(180deg,var(--panel),var(--panel2));overflow:hidden;box-shadow:0 14px 30px rgba(0,0,0,.16)}.preview{display:grid;place-items:center;aspect-ratio:var(--preview-aspect,16/10);background:var(--preview);border-bottom:1px solid var(--softline);padding:12px}.preview img{display:block;max-width:100%;max-height:100%;object-fit:contain}.preview iframe{display:block;width:100%;height:100%;border:0;background:var(--preview)}.body{padding:14px 15px 16px}.title{font-size:15px;font-weight:850;line-height:1.24;margin-bottom:11px}.links{display:flex;flex-wrap:wrap;gap:8px}");
        sb.AppendLine("@media(max-width:760px){header{padding:26px 18px 18px}main{padding:22px 18px 34px}.section-head{display:block}.count{display:inline-flex;margin-top:12px}.grid{grid-template-columns:1fr}.preview{aspect-ratio:4/3;padding:12px}}");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<header>");
        sb.AppendLine("<h1>ChartForgeX Chart Catalog</h1>");
        sb.AppendLine("<p>Grouped static examples for reviewing the current chart surface across HTML, SVG, and dependency-free PNG output.</p>");
        sb.AppendLine("<div class=\"nav\"><a href=\"" + IndexFileName + "\">All examples</a><a href=\"" + QualityDashboardFileName + "\">Quality dashboard</a><a href=\"" + ComparisonFileName + "\">SVG/PNG comparison</a></div>");
        sb.AppendLine("</header>");
        sb.AppendLine("<main>");

        foreach (var group in CatalogGroups) {
            var groupCards = cards.Where(card => group.Contains(card.BaseName)).ToArray();
            if (groupCards.Length == 0) continue;
            foreach (var card in groupCards) assigned.Add(card.BaseName);
            AppendCatalogSection(sb, group.Name, group.Description, groupCards);
        }

        var remaining = cards.Where(card => !assigned.Contains(card.BaseName)).ToArray();
        if (remaining.Length > 0) {
            AppendCatalogSection(
                sb,
                "Additional Examples",
                "Generated outputs not yet assigned to a named catalog family.",
                remaining);
        }

        sb.AppendLine("</main>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        File.WriteAllText(Path.Combine(output, CatalogFileName), sb.ToString());
    }

    private static void AppendCatalogSection(System.Text.StringBuilder sb, string title, string description, CatalogCard[] cards) {
        sb.AppendLine("<section class=\"section\">");
        sb.AppendLine("<div class=\"section-head\"><div><h2>" + EscapeHtml(title) + "</h2><p>" + EscapeHtml(description) + "</p></div><span class=\"count\">" + cards.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + " examples</span></div>");
        sb.AppendLine("<div class=\"grid\">");
        foreach (var card in cards) {
            AppendCatalogCard(sb, card);
        }

        sb.AppendLine("</div>");
        sb.AppendLine("</section>");
    }

    private static void AppendCatalogCard(System.Text.StringBuilder sb, CatalogCard card) {
        var directory = Path.GetDirectoryName(card.FilePath) ?? string.Empty;
        var svgExists = File.Exists(Path.Combine(directory, card.BaseName + ".svg"));
        var pngExists = File.Exists(Path.Combine(directory, card.BaseName + ".png"));
        var csharpExists = File.Exists(Path.Combine(directory, card.BaseName + ".csharp.txt"));
        sb.AppendLine("<article class=\"card\">");
        if (svgExists) {
            var previewStyle = CatalogPreviewStyle(Path.Combine(directory, card.BaseName + ".svg"));
            sb.AppendLine("<a class=\"preview\" href=\"" + EscapeHtml(card.FileName) + "\" aria-label=\"Open " + EscapeHtml(card.Title) + "\"" + previewStyle + "><img loading=\"lazy\" src=\"" + EscapeHtml(card.BaseName) + ".svg\" alt=\"\"></a>");
        } else {
            sb.AppendLine("<div class=\"preview\"><iframe loading=\"lazy\" src=\"" + EscapeHtml(card.FileName) + "\" title=\"" + EscapeHtml(card.Title) + "\"></iframe></div>");
        }
        sb.AppendLine("<div class=\"body\">");
        sb.AppendLine("<div class=\"title\">" + EscapeHtml(card.Title) + "</div>");
        sb.AppendLine("<div class=\"links\">");
        sb.AppendLine("<a href=\"" + EscapeHtml(card.FileName) + "\">HTML</a>");
        if (svgExists) sb.AppendLine("<a href=\"" + EscapeHtml(card.BaseName) + ".svg\">SVG</a>");
        if (pngExists) sb.AppendLine("<a href=\"" + EscapeHtml(card.BaseName) + ".png\">PNG</a>");
        if (csharpExists) sb.AppendLine("<a href=\"" + EscapeHtml(card.BaseName) + ".csharp.txt\">C#</a>");
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");
        sb.AppendLine("</article>");
    }

    private static string CatalogPreviewStyle(string svgPath) {
        var aspect = ReadSvgAspectRatio(svgPath);
        if (!aspect.HasValue) return string.Empty;
        var boundedAspect = Math.Min(2.15, Math.Max(1.05, aspect.Value));
        return " style=\"--preview-aspect:" + boundedAspect.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "\"";
    }

    private static double? ReadSvgAspectRatio(string svgPath) {
        try {
            var document = System.Xml.Linq.XDocument.Load(svgPath);
            var root = document.Root;
            if (root == null) return null;
            var width = ParseSvgLength(root.Attribute("width")?.Value);
            var height = ParseSvgLength(root.Attribute("height")?.Value);
            if (width <= 0 || height <= 0) return null;
            return width / height;
        } catch (Exception) {
            return null;
        }
    }

    private static double ParseSvgLength(string? value) {
        if (string.IsNullOrWhiteSpace(value)) return 0;
        var end = 0;
        while (end < value.Length && (char.IsDigit(value[end]) || value[end] == '.' || value[end] == '-' || value[end] == '+')) end++;
        if (end == 0) return 0;
        return double.TryParse(value.Substring(0, end), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
    }

    private static bool IsTopologyCatalogExample(string fileName) {
        foreach (var group in CatalogGroups) {
            if (group.Name.StartsWith("Topology ", StringComparison.OrdinalIgnoreCase) && group.Contains(fileName)) return true;
        }

        return false;
    }

    private readonly struct CatalogGroup {
        public CatalogGroup(string name, string description, params string[] fileNames) {
            Name = name;
            Description = description;
            FileNames = new HashSet<string>(fileNames, StringComparer.OrdinalIgnoreCase);
        }

        public string Name { get; }

        public string Description { get; }

        private HashSet<string> FileNames { get; }

        public bool Contains(string fileName) => FileNames.Contains(fileName);
    }

    private readonly struct CatalogCard {
        public CatalogCard(string filePath, string fileName, string baseName, string title) {
            FilePath = filePath;
            FileName = fileName;
            BaseName = baseName;
            Title = title;
        }

        public string FilePath { get; }

        public string FileName { get; }

        public string BaseName { get; }

        public string Title { get; }
    }
}
