using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;

namespace ChartForgeX.Themes;

public sealed partial class VisualTheme {
    private static VisualTypography ReadChartTypography(string family, string mono, GeoJsonValue value, double reference) {
        var chart = value.AsObject("typography.chart");
        Dictionary<string, GeoJsonValue> Role(string name) => Required(chart, name, "typography.chart").AsObject(name);
        double Number(Dictionary<string, GeoJsonValue> owner, string key) {
            var text = Required(owner, key, "typography.chart").AsString(key);
            var rem = text.EndsWith("rem", StringComparison.Ordinal);
            var suffix = rem ? 3 : text.EndsWith("px", StringComparison.Ordinal) ? 2 : 0;
            if (!double.TryParse(text.Substring(0, text.Length - suffix), NumberStyles.Float, CultureInfo.InvariantCulture, out var n)
                || double.IsNaN(n) || double.IsInfinity(n) || n <= 0) throw new ArgumentException("Chart type values must be positive finite numbers.");
            return n * (rem ? reference : 1);
        }
        int Weight(string name, string key = "weight") {
            var n = Number(Role(name), key);
            if (n < 1 || n > 1000 || n != Math.Floor(n)) throw new ArgumentException("Chart font weights must be integers from 1 to 1000.");
            return (int)n;
        }
        var center = Role("centerValue");
        var minimum = Number(chart, "minimumSize");
        if (minimum < 10) throw new ArgumentException("Chart text must retain the 10 px minimum.");
        VisualTypography Scale(bool compact) {
            double Size(string name) => Number(Role(name), compact ? "compactSize" : "size");
            return new VisualTypography(family,
                titleSize: Size("title"), subtitleSize: Size("subtitle"), axisSize: Size("tick"),
                axisTitleSize: Size("axisTitle"), legendSize: Size("legend"), dataLabelSize: Size("dataLabel"),
                centerValueSize: Number(center, "minimumSize"), tileLabelSize: Size("panelTitle"),
                monospaceFamily: mono) {
                MinimumTextSize = minimum, CompactBreakpoint = Number(chart, "compactBreakpoint"),
                TitleWeight = Weight("title"), PanelTitleWeight = Weight("panelTitle"), SubtitleWeight = Weight("subtitle"), AxisTitleWeight = Weight("axisTitle"),
                TickWeight = Weight("tick"), LegendWeight = Weight("legend"), LegendValueWeight = Weight("legend", "valueWeight"),
                DataLabelWeight = Weight("dataLabel"), CenterValueWeight = Weight("centerValue"),
                CenterMinimumSize = Number(center, "minimumSize"), CenterMaximumSize = Number(center, "maximumSize"),
                CenterRadiusFactor = Number(center, "radiusFactor"), CenterCaptionSize = Number(center, "captionSize"),
                CenterCaptionGap = Number(center, "captionGap")
            };
        }
        var wide = Scale(false);
        if (wide.CenterMaximumSize < wide.CenterMinimumSize) throw new ArgumentException("The chart centre maximum must be at least its minimum.");
        wide.CompactScale = Scale(true);
        wide.CompactScale.WideScale = wide;
        if (chart.TryGetValue("resolvedSize", out var resolved) && resolved.AsString("chart.resolvedSize") == "compact") return wide.CompactScale;
        return wide;
    }
}
