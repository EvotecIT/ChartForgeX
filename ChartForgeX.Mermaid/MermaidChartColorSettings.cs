using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Resolves bounded chart color variables through the canonical native color parser. Radar reads the
/// <c>cScale0</c>..<c>cScale11</c> series colours and <c>titleColor</c>; Pie reads the <c>pie1</c>..<c>pie12</c>
/// slice colours and <c>pieTitleTextColor</c>.
/// </summary>
internal sealed class MermaidChartColorSettings {
    private const int ScaleCount = 12;
    private readonly HashSet<string> _handled = new HashSet<string>(StringComparer.Ordinal);
    private readonly ChartColor?[] _colors = new ChartColor?[ScaleCount];
    private ChartColor? _title;
    private bool _slices;

    internal static bool IsScalarPath(string path) {
        if (path == "themeVariables.titleColor" || path == "themeVariables.pieTitleTextColor") return true;
        for (var index = 0; index < ScaleCount; index++)
            if (path == "themeVariables.cScale" + index || path == "themeVariables.pie" + (index + 1)) return true;
        return false;
    }

    internal bool Handles(string path) {
        foreach (var handled in _handled)
            if (path == handled || path.StartsWith(handled + ".", StringComparison.Ordinal)) return true;
        return false;
    }

    internal static MermaidChartColorSettings Read(MermaidSourceConfiguration configuration,
        Action<MermaidConfigurationSetting, string> error, bool pie = false) {
        var settings = new MermaidChartColorSettings { _slices = pie };
        settings._title = Color(pie ? "themeVariables.pieTitleTextColor" : "themeVariables.titleColor");
        for (var index = 0; index < ScaleCount; index++)
            settings._colors[index] = Color(pie ? "themeVariables.pie" + (index + 1) : "themeVariables.cScale" + index);
        return settings;

        ChartColor? Color(string path) {
            var setting = configuration.Last(path);
            if (setting == null) return null;
            settings._handled.Add(path);
            if (setting.IsString && ChartColor.TryParse(setting.Value, out var color)) return color;
            error(setting, "Mermaid configuration '" + path + "' must be a supported native color string.");
            return null;
        }
    }

    internal void Apply(Chart chart) {
        if (_title.HasValue) chart.ConfigureTitleStyle(style => style.Color = _title.Value);
        if (_slices) {
            // Pie slices are the points of one series, so the source colours apply per slice, cycling like Mermaid.
            foreach (var series in chart.Series) {
                if (Array.TrueForAll(_colors, color => !color.HasValue)) break;
                while (series.PointColors.Count < series.Points.Count) series.PointColors.Add(null);
                for (var index = 0; index < series.Points.Count; index++)
                    if (_colors[index % ScaleCount] is ChartColor color) series.PointColors[index] = color;
            }
            return;
        }
        for (var index = 0; index < chart.Series.Count; index++) {
            var color = _colors[index % ScaleCount];
            if (color.HasValue) chart.Series[index].Color = color.Value;
        }
    }
}
