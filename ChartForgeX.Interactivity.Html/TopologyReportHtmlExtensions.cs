using System;
using System.Linq;
using System.Net;
using System.Text;
using ChartForgeX.Topology;

namespace ChartForgeX.Interactivity.Html;

/// <summary>Provides a self-contained navigator for prepared topology reports.</summary>
public static partial class TopologyReportHtmlExtensions {
    /// <summary>Renders page navigation, bounded object search, and cross-page relationship links. Only the active SVG is mounted.</summary>
    public static string ToInteractiveHtmlPage(this TopologyReport report) {
        if (report == null) throw new ArgumentNullException(nameof(report));
        var source = report.Source.ToInterchangeEnvelope();
        var labels = report.NodeLabels;
        string title = string.IsNullOrWhiteSpace(source.Title) ? "Topology report" : source.Title;
        var html = new StringBuilder("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>");
        html.Append(Text(title)).Append("</title><style>").Append(ReportStyles).Append("</style></head><body><main>");
        html.Append("<header><p class=\"eyebrow\">TOPOLOGY REPORT</p><h1>").Append(Text(title)).Append("</h1><p>")
            .Append(source.Nodes.Count).Append(" objects · ").Append(report.Pages.Count).Append(" detail pages · ").Append(source.Edges.Count).Append(" relationships</p></header>");
        html.Append("<nav aria-label=\"Report pages\"><button id=\"previous\" type=\"button\">Previous</button><label>Page <select id=\"page\"><option value=\"0\">Overview</option>");
        for (int i = 0; i < report.Pages.Count; i++) html.Append("<option value=\"").Append(i + 1).Append("\">Page ").Append(i + 1).Append("</option>");
        html.Append("</select></label><button id=\"next\" type=\"button\">Next</button><label class=\"fit\"><input id=\"fit\" type=\"checkbox\"> Fit page</label></nav>");
        html.Append("<div class=\"search\"><label for=\"search\">Find an object</label><input id=\"search\" type=\"search\" placeholder=\"Name or stable ID\" autocomplete=\"off\"><p id=\"search-status\" role=\"status\"></p><div id=\"results\"></div></div>");
        html.Append("<p id=\"page-status\" role=\"status\"></p><div id=\"content\"></div><noscript>Enable JavaScript to navigate this report, or use the separately exported SVG pages.</noscript>");
        html.Append("<template id=\"objects\">");
        foreach (var node in labels) html.Append("<button type=\"button\" data-page=\"").Append(report.NodePages[node.Key]).Append("\">").Append(Text(node.Value)).Append(" — ").Append(Text(node.Key)).Append("</button>");
        html.Append("</template>");
        WritePage(html, report, 0, report.Overview, labels);
        for (int i = 0; i < report.Pages.Count; i++) WritePage(html, report, i + 1, report.Pages[i], labels);
        return html.Append("</main><script>").Append(ReportScript).Append("</script></body></html>").ToString();
    }

    private static void WritePage(StringBuilder html, TopologyReport report, int number, PreparedTopology page,
        System.Collections.Generic.IReadOnlyDictionary<string, string> labels) {
        html.Append("<template id=\"page-").Append(number).Append("\"><section aria-label=\"Diagram\" class=\"diagram\" tabindex=\"0\">").Append(page.ToSvg()).Append("</section>");
        html.Append("<section class=\"connections\"><h2>").Append(number == 0 ? "Detail pages" : "Relationships to other pages").Append("</h2><div class=\"links\">");
        if (number == 0) {
            for (int i = 0; i < report.Pages.Count; i++) html.Append("<button type=\"button\" data-page=\"").Append(i + 1).Append("\">Page ").Append(i + 1).Append(" · ").Append(report.Pages[i].NodeCount).Append(" objects</button>");
        } else {
            foreach (var link in report.CrossPageLinks.Where(link => link.SourcePage == number || link.TargetPage == number)) {
                int target = link.SourcePage == number ? link.TargetPage : link.SourcePage;
                html.Append("<button type=\"button\" data-page=\"").Append(target).Append("\">").Append(Text(labels[link.SourceNodeId])).Append(" → ").Append(Text(labels[link.TargetNodeId]))
                    .Append(" · ").Append(Text(link.EdgeId)).Append(" · page ").Append(target).Append("</button>");
            }
        }
        html.Append("</div></section></template>");
    }

    private static string Text(string value) => WebUtility.HtmlEncode(value);

}
