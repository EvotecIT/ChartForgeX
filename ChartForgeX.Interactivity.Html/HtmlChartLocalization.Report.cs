using System.Collections.Generic;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlChartLocalization {
    private static Dictionary<HtmlChartText, string> ReportDefaults() => new Dictionary<HtmlChartText, string> {
        [HtmlChartText.ReportEyebrow] = "TOPOLOGY REPORT",
        [HtmlChartText.ReportSummary] = "{objects} objects · {pages} detail pages · {relationships} relationships",
        [HtmlChartText.ReportPages] = "Report pages",
        [HtmlChartText.ReportPrevious] = "Previous",
        [HtmlChartText.ReportPage] = "Page",
        [HtmlChartText.ReportPageNumber] = "Page {page}",
        [HtmlChartText.ReportFitPage] = "Fit page",
        [HtmlChartText.ReportFindObject] = "Find an object",
        [HtmlChartText.ReportFindPlaceholder] = "Name or stable ID",
        [HtmlChartText.ReportNoScript] = "Enable JavaScript to navigate this report, or use the separately exported SVG pages.",
        [HtmlChartText.ReportDiagram] = "Diagram",
        [HtmlChartText.ReportDetailPages] = "Detail pages",
        [HtmlChartText.ReportOtherRelationships] = "Relationships to other pages",
        [HtmlChartText.ReportDetailPageLink] = "Page {page} · {objects} objects",
        [HtmlChartText.ReportPageSuffix] = " · page {page}",
        [HtmlChartText.ReportPreviousDetailPages] = "Previous detail pages",
        [HtmlChartText.ReportPreviousRelationships] = "Previous relationships",
        [HtmlChartText.ReportNextDetailPages] = "Next detail pages",
        [HtmlChartText.ReportNextRelationships] = "Next relationships",
        [HtmlChartText.ReportRange] = "{first}–{last} of {count}",
        [HtmlChartText.ReportOverviewStatus] = "Overview · choose a detail page to read individual objects",
        [HtmlChartText.ReportPageStatus] = "Page {page} of {count} · scroll at natural size, or choose Fit page",
        [HtmlChartText.ReportOneMatch] = "{count} match",
        [HtmlChartText.ReportMatches] = "{count} matches",
        [HtmlChartText.ReportRefineSearch] = " · showing first 20; refine your search",
    };
}
