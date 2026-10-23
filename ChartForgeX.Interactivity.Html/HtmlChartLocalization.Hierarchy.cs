using System.Collections.Generic;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlChartLocalization {
    private static Dictionary<HtmlChartText, string> HierarchyDefaults() => new() {
        [HtmlChartText.HierarchyNavigation] = "Hierarchy navigation",
        [HtmlChartText.HierarchyUp] = "Up one level",
        [HtmlChartText.HierarchyHome] = "Full hierarchy",
        [HtmlChartText.HierarchyUpdating] = "Updating hierarchy…",
        [HtmlChartText.HierarchyFailed] = "The hierarchy view could not be updated.",
        [HtmlChartText.HierarchyDisconnected] = "Connect native preparation to navigate the hierarchy."
    };
}
