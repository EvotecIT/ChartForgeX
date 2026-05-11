/// <summary>
/// Writes a static SVG/PNG artifact quality dashboard for generated examples.
/// </summary>
public static partial class GalleryWriter {
    private static void WriteQualityDashboard(string output, ComparisonAsset[] pairs, int matchingPairs, BaselineSummary baseline) {
        var healthySvgs = pairs.Count(pair => pair.SvgHealth.IsHealthy);
        var healthyPngs = pairs.Count(pair => pair.PngHealth.IsHealthy);
        var healthyHtmls = pairs.Count(pair => pair.HtmlHealth.IsHealthy);
        var warningCount = pairs.Sum(pair => pair.Warnings.Length);
        var cleanPairs = pairs.Count(pair => pair.Warnings.Length == 0);
        var assigned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<!doctype html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.AppendLine("<title>ChartForgeX Quality Dashboard</title>");
        sb.AppendLine("<style>");
        sb.AppendLine(":root{color-scheme:dark;--bg:#101418;--panel:#181d23;--panel2:#12161b;--line:#303943;--text:#eef4f8;--muted:#aeb9c2;--ok:#5eead4;--warn:#fbbf24;--accent:#7dd3fc}");
        sb.AppendLine("*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--text);font-family:Inter,ui-sans-serif,system-ui,Segoe UI,Arial,sans-serif;-webkit-font-smoothing:antialiased}header{padding:34px 40px 18px;border-bottom:1px solid var(--line);background:#151a20}h1{margin:0 0 8px;font-size:29px;line-height:1.1}p{margin:0;color:var(--muted);font-size:14px}.nav,.stats{display:flex;flex-wrap:wrap;gap:8px;margin-top:16px}.nav a,.pill{border:1px solid rgba(125,211,252,.34);border-radius:6px;color:var(--accent);font-size:12px;font-weight:850;padding:6px 8px;text-decoration:none}.pill{border-radius:999px;color:#d8e4eb}.pill.ok{border-color:rgba(94,234,212,.45);color:var(--ok)}.pill.warn{border-color:rgba(251,191,36,.55);color:var(--warn)}main{padding:28px 40px 46px}");
        sb.AppendLine(".summary{display:grid;grid-template-columns:repeat(auto-fit,minmax(190px,1fr));gap:14px;margin-bottom:28px}.metric,.group{border:1px solid var(--line);border-radius:8px;background:linear-gradient(180deg,var(--panel),var(--panel2))}.metric{padding:16px}.label{color:var(--muted);font-size:12px;font-weight:800;text-transform:uppercase}.value{font-size:30px;font-weight:900;margin-top:6px}.group{margin-bottom:22px;overflow:hidden}.group-head{display:flex;justify-content:space-between;gap:18px;padding:16px;border-bottom:1px solid var(--line)}h2{margin:0 0 4px;font-size:18px}.table-wrap{overflow:auto}table{width:100%;border-collapse:collapse;min-width:760px}th,td{padding:10px 12px;border-bottom:1px solid rgba(48,57,67,.72);font-size:12px;text-align:left;vertical-align:middle}th{color:var(--muted);font-size:11px;text-transform:uppercase}td.name{font-weight:850}.mono{font-variant-numeric:tabular-nums}.links{display:flex;gap:6px;flex-wrap:wrap}.links a{color:var(--accent);text-decoration:none;font-weight:850}.status{font-weight:900}.status.ok{color:var(--ok)}.status.warn{color:var(--warn)}");
        sb.AppendLine("@media(max-width:720px){header{padding:26px 18px 16px}main{padding:22px 18px 34px}.group-head{display:block}.stats{margin-top:12px}}");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<header>");
        sb.AppendLine("<h1>ChartForgeX Quality Dashboard</h1>");
        sb.AppendLine("<p>Static artifact checks for generated SVG, dependency-free PNG, and standalone HTML outputs.</p>");
        sb.AppendLine("<div class=\"nav\"><a href=\"" + IndexFileName + "\">All examples</a><a href=\"" + CatalogFileName + "\">Grouped catalog</a><a href=\"" + ComparisonFileName + "\">SVG/PNG comparison</a><a href=\"" + ComparisonManifestFileName + "\">Manifest JSON</a></div>");
        sb.AppendLine("</header>");
        sb.AppendLine("<main>");
        sb.AppendLine("<section class=\"summary\">");
        AppendMetric(sb, "Chart pairs", pairs.Length);
        AppendMetric(sb, "Clean pairs", cleanPairs);
        AppendMetric(sb, "Dimension matches", matchingPairs);
        AppendMetric(sb, "Healthy SVGs", healthySvgs);
        AppendMetric(sb, "Healthy PNGs", healthyPngs);
        AppendMetric(sb, "Healthy HTMLs", healthyHtmls);
        AppendMetric(sb, "Warnings", warningCount);
        AppendMetric(sb, "Baseline passes", baseline.ChartMatches);
        AppendMetric(sb, "Baseline warnings", baseline.Warnings);
        sb.AppendLine("</section>");
        AppendQualityWatchlist(sb, pairs);

        foreach (var group in CatalogGroups) {
            var groupPairs = pairs.Where(pair => group.Contains(pair.Name)).ToArray();
            if (groupPairs.Length == 0) continue;
            foreach (var pair in groupPairs) assigned.Add(pair.Name);
            AppendQualityGroup(sb, group.Name, group.Description, groupPairs);
        }

        var remaining = pairs.Where(pair => !assigned.Contains(pair.Name)).ToArray();
        if (remaining.Length > 0) {
            AppendQualityGroup(sb, "Additional Examples", "Generated SVG/PNG pairs not yet assigned to a named catalog family.", remaining);
        }

        sb.AppendLine("</main>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        File.WriteAllText(Path.Combine(output, QualityDashboardFileName), sb.ToString());
    }

    private static void AppendMetric(System.Text.StringBuilder sb, string label, int value) {
        sb.AppendLine("<div class=\"metric\"><div class=\"label\">" + EscapeHtml(label) + "</div><div class=\"value\">" + value.ToString(System.Globalization.CultureInfo.InvariantCulture) + "</div></div>");
    }

    private static void AppendQualityWatchlist(System.Text.StringBuilder sb, ComparisonAsset[] pairs) {
        var watchlist = pairs
            .Where(pair => pair.Warnings.Length > 0 || pair.SvgHealth.TinyTextNodes > 0 || pair.SvgHealth.TinyStrokeNodes > 0 || pair.SvgHealth.TinyMarkerNodes > 0 || pair.SvgHealth.NearEdgeTextNodes > 0 || pair.PngHealth.EdgeInkPixels > 0)
            .OrderByDescending(pair => pair.Warnings.Length)
            .ThenByDescending(pair => pair.SvgHealth.TinyTextNodes)
            .ThenByDescending(pair => pair.SvgHealth.TinyStrokeNodes)
            .ThenByDescending(pair => pair.SvgHealth.TinyMarkerNodes)
            .ThenByDescending(pair => pair.SvgHealth.ClippedTextNodes)
            .ThenByDescending(pair => pair.PngHealth.EdgeInkPixels)
            .ThenByDescending(pair => pair.SvgHealth.NearEdgeTextNodes)
            .ThenBy(pair => pair.Name, StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToArray();
        sb.AppendLine("<section class=\"group\">");
        sb.AppendLine("<div class=\"group-head\"><div><h2>Review Watchlist</h2><p>Highest-priority SVG text bounds, marker/stroke readability, and PNG edge/foreground checks.</p></div><div class=\"stats\"><span class=\"pill " + (watchlist.Length == 0 ? "ok" : "warn") + "\">" + watchlist.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + " active</span></div></div>");
        if (watchlist.Length == 0) {
            sb.AppendLine("<div class=\"metric\"><div class=\"label\">Current status</div><div class=\"value\">Clean</div><p>No clipped SVG text or PNG edge pressure detected in current exports.</p></div>");
            sb.AppendLine("</section>");
            return;
        }

        sb.AppendLine("<div class=\"table-wrap\"><table><thead><tr><th>Chart</th><th>Warnings</th><th>SVG text</th><th>SVG stroke</th><th>SVG markers</th><th>PNG edge</th><th>Open</th></tr></thead><tbody>");
        foreach (var pair in watchlist) {
            var warningText = pair.Warnings.Length == 0 ? "watch" : string.Join(", ", pair.Warnings);
            var svgText = FormatSvgTextSize(pair.SvgHealth.MinimumTextFontSize) + " min / " + pair.SvgHealth.TinyTextNodes.ToString(System.Globalization.CultureInfo.InvariantCulture) + " tiny / " + pair.SvgHealth.ClippedTextNodes.ToString(System.Globalization.CultureInfo.InvariantCulture) + " clipped / " + pair.SvgHealth.NearEdgeTextNodes.ToString(System.Globalization.CultureInfo.InvariantCulture) + " near edge";
            var svgStroke = FormatSvgStrokeWidth(pair.SvgHealth.MinimumStrokeWidth) + " min / " + pair.SvgHealth.TinyStrokeNodes.ToString(System.Globalization.CultureInfo.InvariantCulture) + " tiny / " + pair.SvgHealth.StrokedNodes.ToString(System.Globalization.CultureInfo.InvariantCulture) + " stroked";
            var svgMarkers = FormatSvgMarkerRadius(pair.SvgHealth.MinimumMarkerRadius) + " min / " + pair.SvgHealth.TinyMarkerNodes.ToString(System.Globalization.CultureInfo.InvariantCulture) + " tiny / " + pair.SvgHealth.MarkerNodes.ToString(System.Globalization.CultureInfo.InvariantCulture) + " markers";
            var pngText = pair.PngHealth.ForegroundPixels.ToString(System.Globalization.CultureInfo.InvariantCulture) + " fg / " + pair.PngHealth.EdgeInkPixels.ToString(System.Globalization.CultureInfo.InvariantCulture) + " ink / " + FormatPngContentBounds(pair.PngHealth.ContentBounds);
            sb.AppendLine("<tr><td class=\"name\">" + EscapeHtml(pair.Name) + "</td><td class=\"status warn\">" + EscapeHtml(warningText) + "</td><td class=\"mono\">" + EscapeHtml(svgText) + "</td><td class=\"mono\">" + EscapeHtml(svgStroke) + "</td><td class=\"mono\">" + EscapeHtml(svgMarkers) + "</td><td class=\"mono\">" + EscapeHtml(pngText) + "</td><td><div class=\"links\"><a href=\"" + ComparisonFileName + "#" + EscapeHtml(pair.Name) + "\">review</a><a href=\"" + EscapeHtml(pair.Name) + ".svg\">SVG</a><a href=\"" + EscapeHtml(pair.Name) + ".png\">PNG</a></div></td></tr>");
        }

        sb.AppendLine("</tbody></table></div>");
        sb.AppendLine("</section>");
    }

    private static void AppendQualityGroup(System.Text.StringBuilder sb, string title, string description, ComparisonAsset[] pairs) {
        var clean = pairs.Count(pair => pair.Warnings.Length == 0);
        var warnings = pairs.Sum(pair => pair.Warnings.Length);
        sb.AppendLine("<section class=\"group\">");
        sb.AppendLine("<div class=\"group-head\"><div><h2>" + EscapeHtml(title) + "</h2><p>" + EscapeHtml(description) + "</p></div><div class=\"stats\"><span class=\"pill\">" + pairs.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + " pairs</span><span class=\"pill " + (clean == pairs.Length ? "ok" : "warn") + "\">" + clean.ToString(System.Globalization.CultureInfo.InvariantCulture) + " clean</span><span class=\"pill " + (warnings == 0 ? "ok" : "warn") + "\">" + warnings.ToString(System.Globalization.CultureInfo.InvariantCulture) + " warnings</span></div></div>");
        sb.AppendLine("<div class=\"table-wrap\"><table><thead><tr><th>Chart</th><th>Dimensions</th><th>SVG</th><th>PNG</th><th>Bytes</th><th>Status</th><th>Open</th></tr></thead><tbody>");
        foreach (var pair in pairs.OrderBy(pair => pair.Name, StringComparer.OrdinalIgnoreCase)) {
            var statusClass = pair.Warnings.Length == 0 ? "ok" : "warn";
            var statusText = pair.Warnings.Length == 0 ? "clean" : string.Join(", ", pair.Warnings);
            var svgQuality = pair.SvgHealth.VisualNodes.ToString(System.Globalization.CultureInfo.InvariantCulture) + " nodes / min text " + FormatSvgTextSize(pair.SvgHealth.MinimumTextFontSize) + " / min stroke " + FormatSvgStrokeWidth(pair.SvgHealth.MinimumStrokeWidth) + " / min marker " + FormatSvgMarkerRadius(pair.SvgHealth.MinimumMarkerRadius) + " / " + pair.SvgHealth.TinyTextNodes.ToString(System.Globalization.CultureInfo.InvariantCulture) + " tiny text / " + pair.SvgHealth.TinyStrokeNodes.ToString(System.Globalization.CultureInfo.InvariantCulture) + " tiny stroke / " + pair.SvgHealth.TinyMarkerNodes.ToString(System.Globalization.CultureInfo.InvariantCulture) + " tiny markers";
            var pngQuality = pair.PngHealth.VisiblePixels.ToString(System.Globalization.CultureInfo.InvariantCulture) + " px / " + pair.PngHealth.ForegroundPixels.ToString(System.Globalization.CultureInfo.InvariantCulture) + " fg / " + pair.PngHealth.DistinctColors.ToString(System.Globalization.CultureInfo.InvariantCulture) + " colors / " + pair.PngHealth.EdgeInkPixels.ToString(System.Globalization.CultureInfo.InvariantCulture) + " edge ink / bounds " + FormatPngContentBounds(pair.PngHealth.ContentBounds);
            sb.AppendLine("<tr><td class=\"name\">" + EscapeHtml(pair.Name) + "</td><td class=\"mono\">" + EscapeHtml(FormatDimensions(pair.SvgDimensions)) + "</td><td class=\"mono\">" + EscapeHtml(svgQuality) + "</td><td class=\"mono\">" + EscapeHtml(pngQuality) + "</td><td class=\"mono\">" + EscapeHtml(FormatBytes(pair.SvgBytes)) + " / " + EscapeHtml(FormatBytes(pair.PngBytes)) + "</td><td class=\"status " + statusClass + "\">" + EscapeHtml(statusText) + "</td><td><div class=\"links\"><a href=\"" + EscapeHtml(pair.Name) + ".svg\">SVG</a><a href=\"" + EscapeHtml(pair.Name) + ".png\">PNG</a><a href=\"" + ComparisonFileName + "#" + EscapeHtml(pair.Name) + "\">review</a></div></td></tr>");
        }

        sb.AppendLine("</tbody></table></div>");
        sb.AppendLine("</section>");
    }
}
