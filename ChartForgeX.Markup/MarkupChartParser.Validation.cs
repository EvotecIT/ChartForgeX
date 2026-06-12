using System;

namespace ChartForgeX.Markup;

public sealed partial class MarkupChartParser {
    private static int ParsePositiveInt32(string value, string name) {
        var parsed = VisualMarkupFenceOptions.ParseInt32(value, name);
        if (parsed <= 0) throw new ArgumentException("Chart " + name + " must be positive.");
        return parsed;
    }

    private static double ParseNonNegativeFiniteDouble(string value, string name) {
        var parsed = ParseDouble(value);
        if (double.IsNaN(parsed) || double.IsInfinity(parsed) || parsed < 0) throw new ArgumentException("Chart " + name + " must be a non-negative finite number.");
        return parsed;
    }

    private static void ParseAxisBounds(string minimumValue, string maximumValue, string axisName, out double minimum, out double maximum) {
        minimum = ParseDouble(minimumValue);
        maximum = ParseDouble(maximumValue);
        if (double.IsNaN(minimum) || double.IsInfinity(minimum) || double.IsNaN(maximum) || double.IsInfinity(maximum)) throw new ArgumentException(axisName + " bounds must be finite numbers.");
        if (maximum <= minimum) throw new ArgumentException(axisName + " maximum must be greater than minimum.");
    }

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
