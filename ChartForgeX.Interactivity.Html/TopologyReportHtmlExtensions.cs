using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using ChartForgeX.Topology;
using ChartForgeX.Primitives;

namespace ChartForgeX.Interactivity.Html;

/// <summary>Interactive navigation for bounded topology reports.</summary>
public static partial class TopologyReportHtmlExtensions {
    /// <summary>Renders page navigation, bounded object search, and cross-page relationship links. Only the active SVG is mounted.</summary>
    public static string ToInteractiveHtmlPage(this TopologyReport report) => RenderReport(report, new HtmlChartLocalization());

    /// <summary>Renders localized report controls while preserving authored object labels, identities and source-language search.</summary>
    /// <param name="report">The detached topology report.</param>
    /// <param name="configureLocalization">Configuration for control messages and chrome language/direction.</param>
    public static string ToInteractiveHtmlPage(this TopologyReport report, Action<HtmlChartLocalization> configureLocalization) {
        if (report == null) throw new ArgumentNullException(nameof(report));
        if (configureLocalization == null) throw new ArgumentNullException(nameof(configureLocalization));
        var localization = new HtmlChartLocalization();
        configureLocalization(localization);
        return RenderReport(report, localization);
    }

    private static string RenderReport(TopologyReport report, HtmlChartLocalization text) {
        if (report == null) throw new ArgumentNullException(nameof(report));
        var source = report.Source;
        var labels = report.NodeLabels;
        string title = report.Overview.Title!;
        string language = string.IsNullOrWhiteSpace(source.Language) ? "en" : source.Language!;
        string uiLanguage = text.ResolveLanguage(null);
        var html = new StringBuilder("<!doctype html><html lang=\"").Append(Text(text.Language ?? language)).Append('"');
        if (text.DirectionAttribute != null) html.Append(" dir=\"").Append(text.DirectionAttribute).Append('"');
        html.Append("><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>");
        html.Append(Text(title)).Append("</title><style>").Append(ReportStyles).Append("</style></head><body><main lang=\"").Append(Text(uiLanguage)).Append("\" data-cfx-ui=\"").Append(Text(text.ToReportJson())).Append("\">");
        html.Append("<header><p class=\"eyebrow\">").Append(Text(text[HtmlChartText.ReportEyebrow])).Append("</p><h1 lang=\"").Append(Text(language)).Append("\">").Append(Text(title)).Append("</h1><p data-cfx-report-summary>")
            .Append(Text(text.Format(HtmlChartText.ReportSummary, ("objects", Number(source.NodeCount)), ("pages", Number(report.Pages.Count)), ("relationships", Number(source.EdgeCount))))).Append("</p></header>");
        html.Append("<nav aria-label=\"").Append(Text(text[HtmlChartText.ReportPages])).Append("\"><button id=\"previous\" type=\"button\">").Append(Text(text[HtmlChartText.ReportPrevious])).Append("</button><label>").Append(Text(text[HtmlChartText.ReportPage])).Append(" <select id=\"page\"><option value=\"0\">").Append(Text(text[HtmlChartText.ExplorerOverview])).Append("</option>");
        for (int i = 0; i < report.Pages.Count; i++) html.Append("<option value=\"").Append(i + 1).Append("\">").Append(Text(text.Format(HtmlChartText.ReportPageNumber, ("page", Number(i + 1))))).Append("</option>");
        html.Append("</select></label><button id=\"next\" type=\"button\">").Append(Text(text[HtmlChartText.Next])).Append("</button><label class=\"fit\"><input id=\"fit\" type=\"checkbox\"> ").Append(Text(text[HtmlChartText.ReportFitPage])).Append("</label></nav>");
        html.Append("<div class=\"search\"><label for=\"search\">").Append(Text(text[HtmlChartText.ReportFindObject])).Append("</label><input id=\"search\" type=\"search\" placeholder=\"").Append(Text(text[HtmlChartText.ReportFindPlaceholder])).Append("\" autocomplete=\"off\"><p id=\"search-status\" role=\"status\"></p><div id=\"results\"></div></div>");
        html.Append("<p id=\"page-status\" role=\"status\"></p><div id=\"content\"></div><noscript>").Append(Text(text[HtmlChartText.ReportNoScript])).Append("</noscript><script type=\"application/json\" id=\"objects\">[");
        var objects = report.OrderedNodeIds.Select(nodeId => new[] {
            Number(report.NodePages[nodeId]), labels[nodeId] + " — " + nodeId, "", language, labels[nodeId], nodeId
        });
        WriteRecords(html, objects);
        html.Append("]</script>");
        var linksByPage = report.CrossPageLinks.SelectMany(link => new[] { (Page: link.SourcePage, Link: link), (Page: link.TargetPage, Link: link) }).ToLookup(item => item.Page, item => item.Link);
        WritePage(html, report, 0, report.Overview, labels, linksByPage[0], text, uiLanguage);
        for (int i = 0; i < report.Pages.Count; i++) WritePage(html, report, i + 1, report.Pages[i], labels, linksByPage[i + 1], text, uiLanguage);
        return html.Append("</main><script>").Append(HtmlInteractiveAssets.LocalizationScript).Append(HtmlInteractiveAssets.AdapterLifetimeScript).Append(HtmlInteractiveAssets.NativeTextScript).Append(ReportScript).Append("</script></body></html>").ToString();
    }

    private static void WritePage(StringBuilder html, TopologyReport report, int number, PreparedTopology page,
        System.Collections.Generic.IReadOnlyDictionary<string, string> labels, System.Collections.Generic.IEnumerable<TopologyReportLink> links,
        HtmlChartLocalization text, string uiLanguage) {
        string body = "<section aria-label=\"" + Text(text[HtmlChartText.ReportDiagram]) + "\" class=\"diagram\" tabindex=\"0\">" + HtmlInteractiveTopologyRenderer.AutomaticText(page) + "</section>"
            + "<section class=\"connections\"><h2>" + Text(text[number == 0 ? HtmlChartText.ReportDetailPages : HtmlChartText.ReportOtherRelationships])
            + "</h2><div class=\"links\"></div><div class=\"relationship-pages\"></div></section>";
        html.Append("<script type=\"application/json\" id=\"page-").Append(number).Append("\">").Append(HtmlJsonString.Encode(body)).Append("</script>");
        html.Append("<script type=\"application/json\" id=\"links-").Append(number).Append("\">[");
        var records = number == 0
            ? report.Pages.Select((detail, index) => new[] { Number(index + 1), text.Format(HtmlChartText.ReportDetailPageLink, ("page", Number(index + 1)), ("objects", Number(detail.NodeCount))), "", uiLanguage })
            : links.Select(link => {
                int target = link.SourcePage == number ? link.TargetPage : link.SourcePage;
                string arrow = link.Direction switch { VisualLinkDirection.Forward => " → ", VisualLinkDirection.Backward => " ← ", VisualLinkDirection.Bidirectional => " ↔ ", _ => " — " };
                return new[] { Number(target), labels[link.SourceNodeId] + arrow + labels[link.TargetNodeId] + " · " + link.EdgeId, text.Format(HtmlChartText.ReportPageSuffix, ("page", Number(target))), string.IsNullOrWhiteSpace(report.Source.Language) ? "en" : report.Source.Language! };
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

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    private static string Text(string value) => WebUtility.HtmlEncode(value);
}
