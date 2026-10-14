using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Mermaid;

/// <summary>Resolves bounded chart color variables through the canonical native color parser.</summary>
internal sealed class MermaidChartColorSettings {
    private const int ScaleCount = 12;
    private readonly HashSet<string> _handled = new HashSet<string>(StringComparer.Ordinal);
    private readonly ChartColor?[] _colors = new ChartColor?[ScaleCount];
    private ChartColor? _title;

    internal static bool IsScalarPath(string path) {
        if (path == "themeVariables.titleColor") return true;
        for (var index = 0; index < ScaleCount; index++)
            if (path == "themeVariables.cScale" + index) return true;
        return false;
    }

    internal bool Handles(string path) {
        foreach (var handled in _handled)
            if (path == handled || path.StartsWith(handled + ".", StringComparison.Ordinal)) return true;
        return false;
    }

    internal static MermaidChartColorSettings Read(MermaidSourceConfiguration configuration,
        Action<MermaidConfigurationSetting, string> error) {
        var settings = new MermaidChartColorSettings();
        settings._title = Color("themeVariables.titleColor");
        for (var index = 0; index < ScaleCount; index++) settings._colors[index] = Color("themeVariables.cScale" + index);
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
        for (var index = 0; index < chart.Series.Count; index++) {
            var color = _colors[index % ScaleCount];
            if (color.HasValue) chart.Series[index].Color = color.Value;
        }
    }
}
