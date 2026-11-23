using System;
using ChartForgeX.Core;
using ChartForgeX.Themes;

namespace ChartForgeX.Markup;

public sealed partial class MarkupChartParser {
    private sealed partial class ChartState {
        internal VisualTheme? Theme;
        internal ThemeMode? ThemeMode;
    }

    private static bool TryParseFamilyOption(ChartState state, string key, string value, int lineNumber) {
        switch (NormalizeKey(key)) {
            case "theme": ParseTheme(state, value); return true;
            case "thememode": state.ThemeMode = VisualMarkupFenceOptions.ParseEnum<ThemeMode>(value, key); return true;
            case "heatmapvaluename":
                state.HeatmapValueName = new ChartColorDimensionOptions { Name = value }.Name;
                return true;
            case "gaugeminimum":
                state.GaugeMinimum = VisualMarkupFenceOptions.ParseDouble(value, key);
                state.GaugeOptionLine = lineNumber;
                return true;
            case "gaugemaximum":
                state.GaugeMaximum = VisualMarkupFenceOptions.ParseDouble(value, key);
                state.GaugeOptionLine = lineNumber;
                return true;
            default: return false;
        }
    }

    private static void ParseTheme(ChartState state, string value) {
        if (NormalizeKey(value) is "auto" or "light" or "dark") {
            state.ThemeMode = VisualMarkupFenceOptions.ParseEnum<ThemeMode>(value, "theme");
            return;
        }
        state.Theme = NormalizeKey(value) switch {
            "graphite" => VisualTheme.Graphite(),
            "accessible" => VisualTheme.Accessible(),
            "transparentoverlay" => VisualTheme.TransparentOverlay(),
            _ => throw new ArgumentException("Unknown chart theme '" + value + "'.")
        };
    }

    private static void ApplyFamilyOptions(Chart chart, ChartState state) {
        if (state.Theme != null) chart.Theme(state.Theme);
        if (state.ThemeMode.HasValue) chart.ThemeMode(state.ThemeMode.Value);
        if (state.HeatmapValueName != null) chart.Options.Heatmap.ColorDimension.Name = state.HeatmapValueName;
    }
}
