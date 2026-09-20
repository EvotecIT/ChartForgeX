using System;
using System.Linq;
using System.Net;
using System.Text;
using ChartForgeX.Topology;
using ChartForgeX.Primitives;

namespace ChartForgeX.Interactivity.Html;

/// <summary>Provides a self-contained navigator for prepared topology reports.</summary>
public static partial class TopologyReportHtmlExtensions {
    /// <summary>Renders page navigation, bounded object search, and cross-page relationship links. Only the active SVG is mounted.</summary>
    public static string ToInteractiveHtmlPage(this TopologyReport report) {
        if (report == null) throw new ArgumentNullException(nameof(report));
        var source = report.Source;
        var labels = report.NodeLabels;
        string title = report.Overview.Title!;
        string language = Text(string.IsNullOrWhiteSpace(source.Language) ? "en" : source.Language!);
        var html = new StringBuilder("<!doctype html><html lang=\"").Append(language).Append("\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>");
        html.Append(Text(title)).Append("</title><style>").Append(ReportStyles).Append("</style></head><body><main lang=\"en\">");
        html.Append("<header><p class=\"eyebrow\">TOPOLOGY REPORT</p><h1 lang=\"").Append(language).Append("\">").Append(Text(title)).Append("</h1><p>")
            .Append(source.NodeCount).Append(" objects · ").Append(report.Pages.Count).Append(" detail pages · ").Append(source.EdgeCount).Append(" relationships</p></header>");
        html.Append("<nav aria-label=\"Report pages\"><button id=\"previous\" type=\"button\">Previous</button><label>Page <select id=\"page\"><option value=\"0\">Overview</option>");
        for (int i = 0; i < report.Pages.Count; i++) html.Append("<option value=\"").Append(i + 1).Append("\">Page ").Append(i + 1).Append("</option>");
        html.Append("</select></label><button id=\"next\" type=\"button\">Next</button><label class=\"fit\"><input id=\"fit\" type=\"checkbox\"> Fit page</label></nav>");
        html.Append("<div class=\"search\"><label for=\"search\">Find an object</label><input id=\"search\" type=\"search\" placeholder=\"Name or stable ID\" autocomplete=\"off\"><p id=\"search-status\" role=\"status\"></p><div id=\"results\"></div></div>");
        html.Append("<p id=\"page-status\" role=\"status\"></p><div id=\"content\"></div><noscript>Enable JavaScript to navigate this report, or use the separately exported SVG pages.</noscript>");
        html.Append("<script type=\"application/json\" id=\"objects\">[");
        var objects = labels.Select(node => new[] { report.NodePages[node.Key].ToString(System.Globalization.CultureInfo.InvariantCulture), node.Value + " — " + node.Key, "", string.IsNullOrWhiteSpace(source.Language) ? "en" : source.Language! });
        WriteRecords(html, objects);
        html.Append("]</script>");
        var linksByPage = report.CrossPageLinks.SelectMany(link => new[] { (Page: link.SourcePage, Link: link), (Page: link.TargetPage, Link: link) }).ToLookup(item => item.Page, item => item.Link);
        WritePage(html, report, 0, report.Overview, labels, linksByPage[0]);
        for (int i = 0; i < report.Pages.Count; i++) WritePage(html, report, i + 1, report.Pages[i], labels, linksByPage[i + 1]);
        return html.Append("</main><script>").Append(ReportScript).Append("</script></body></html>").ToString();
    }

    private static void WritePage(StringBuilder html, TopologyReport report, int number, PreparedTopology page,
        System.Collections.Generic.IReadOnlyDictionary<string, string> labels, System.Collections.Generic.IEnumerable<TopologyReportLink> links) {
        html.Append("<template id=\"page-").Append(number).Append("\"><section aria-label=\"Diagram\" class=\"diagram\" tabindex=\"0\">").Append(page.ToSvg()).Append("</section>");
        html.Append("<section class=\"connections\"><h2>").Append(number == 0 ? "Detail pages" : "Relationships to other pages").Append("</h2><div class=\"links\"></div><div class=\"relationship-pages\"></div></section></template>");
        html.Append("<script type=\"application/json\" id=\"links-").Append(number).Append("\">[");
        var records = number == 0
            ? report.Pages.Select((detail, index) => new[] { (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), "Page " + (index + 1) + " · " + detail.NodeCount + " objects", "", "en" })
            : links.Select(link => {
                int target = link.SourcePage == number ? link.TargetPage : link.SourcePage;
                string arrow = link.Direction switch { VisualLinkDirection.Forward => " → ", VisualLinkDirection.Backward => " ← ", VisualLinkDirection.Bidirectional => " ↔ ", _ => " — " };
                return new[] { target.ToString(System.Globalization.CultureInfo.InvariantCulture), labels[link.SourceNodeId] + arrow + labels[link.TargetNodeId] + " · " + link.EdgeId, " · page " + target, string.IsNullOrWhiteSpace(report.Source.Language) ? "en" : report.Source.Language! };
            });
        WriteRecords(html, records);
        html.Append("]</script>");
    }

    private static void WriteRecords(StringBuilder html, System.Collections.Generic.IEnumerable<string[]> records) {
        bool first = true;
        foreach (var record in records) {
            if (!first) html.Append(',');
            first = false;
            html.Append('[').Append(string.Join(",", record.Select(HtmlJsonString.Encode))).Append(']');
        }
    }

    private static string Text(string value) => WebUtility.HtmlEncode(value);

}
