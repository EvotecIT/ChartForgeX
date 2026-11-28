using System.Collections.Generic;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlChartLocalization {
    // Drill-down text travels in the drill-down payload, so charts without targets carry none of it.
    private static Dictionary<HtmlChartText, string> DrilldownDefaults() => new() {
        [HtmlChartText.DrilldownNavigation] = "Drill-down path",
        [HtmlChartText.DrilldownAvailable] = "Opens {label}",
        [HtmlChartText.DrilldownOpened] = "Showing {label}, level {level} of {count}",
        [HtmlChartText.DrilldownReturned] = "Back to {label}",
        [HtmlChartText.DrilldownLoading] = "Loading {label}…",
        [HtmlChartText.DrilldownFailed] = "{label} could not be loaded."
    };
}
