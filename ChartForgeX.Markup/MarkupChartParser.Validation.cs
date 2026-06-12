using System;

namespace ChartForgeX.Markup;

public sealed partial class MarkupChartParser {
    private static int ParseTickCount(string value, string name) {
        var count = VisualMarkupFenceOptions.ParseInt32(value, name);
        if (count < 2) throw new ArgumentException("Chart tickCount must be at least 2.");
        return count;
    }

    private static string ValidateChartType(string type) {
        switch (NormalizeKey(type)) {
            case "line":
            case "smoothline":
            case "stepline":
            case "area":
            case "smootharea":
            case "steparea":
            case "stackedarea":
            case "smoothstackedarea":
            case "scatter":
            case "lollipop":
            case "radar":
            case "funnel":
            case "polararea":
            case "polar":
            case "donut":
            case "pie":
            case "horizontalbar":
            case "hbar":
            case "waterfall":
            case "bar":
            case "column":
                return type;
            default:
                throw new ArgumentException("Unknown chart type '" + type + "'.");
        }
    }
}
