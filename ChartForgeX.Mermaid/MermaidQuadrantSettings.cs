using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Mermaid;

/// <summary>Reads only Quadrant settings with faithful existing native owners; other settings keep located diagnostics.</summary>
internal sealed class MermaidQuadrantSettings {
    private static readonly string[] Fields = { "chartWidth", "chartHeight", "pointRadius", "pointLabelFontSize", "titleFontSize", "showXAxis", "showYAxis", "showTitle" };
    private static readonly string[] Colors = { "quadrant1Fill", "quadrant2Fill", "quadrant3Fill", "quadrant4Fill", "quadrantPointFill", "quadrantPointTextFill", "quadrantXAxisTextFill", "quadrantTitleFill" };
    private readonly Dictionary<string, ChartColor> _colors = new(StringComparer.Ordinal);
    private double? _pointRadius, _pointFontSize, _titleFontSize;
    private bool? _showX, _showY, _showTitle;
    internal int? Width { get; private set; }
    internal int? Height { get; private set; }
    internal static bool IsScalarPath(string path) {
        foreach (var key in Fields) if (path == "quadrantChart." + key) return true;
        foreach (var key in Colors) if (path == "themeVariables." + key) return true;
        return false;
    }
    internal bool Handles(string path) {
        foreach (var key in Fields) if (path == "quadrantChart." + key || path.StartsWith("quadrantChart." + key + ".", StringComparison.Ordinal)) return true;
        foreach (var key in Colors) if (path == "themeVariables." + key || path.StartsWith("themeVariables." + key + ".", StringComparison.Ordinal)) return true;
        return false;
    }
    internal static MermaidQuadrantSettings Read(MermaidSourceConfiguration source, Action<MermaidConfigurationSetting, string> error) {
        var settings = new MermaidQuadrantSettings();
        settings.Width = Dimension("chartWidth"); settings.Height = Dimension("chartHeight");
        settings._pointRadius = Number("pointRadius", 0, 1024); settings._pointFontSize = Number("pointLabelFontSize", 1, 256);
        settings._titleFontSize = Number("titleFontSize", 1, 256); settings._showX = Boolean("showXAxis"); settings._showY = Boolean("showYAxis"); settings._showTitle = Boolean("showTitle");
        foreach (var key in Colors) {
            var value = source.Last("themeVariables." + key); if (value == null) continue;
            if (value.IsString && ChartColor.TryParse(value.Value, out var color)) settings._colors[key] = color;
            else error(value, "Mermaid configuration '" + value.Path + "' must be a supported native solid color string.");
        }
        return settings;
        double? Number(string key, double minimum, double maximum) {
            var value = source.Last("quadrantChart." + key); if (value == null) return null;
            if (value.TryGetNumber(out var number) && number >= minimum && number <= maximum) return number;
            error(value, "Mermaid configuration '" + value.Path + "' must be a finite number from " + minimum + " through " + maximum + "."); return null;
        }
        int? Dimension(string key) {
            var number = Number(key, 1, int.MaxValue); if (!number.HasValue) return null;
            if (Math.Truncate(number.Value) == number.Value) return (int)number.Value;
            error(source.Last("quadrantChart." + key)!, "Mermaid Quadrant dimensions must use positive whole native pixels."); return null;
        }
        bool? Boolean(string key) {
            var value = source.Last("quadrantChart." + key); if (value == null) return null;
            if (!value.IsString && bool.TryParse(value.Value, out var boolean)) return boolean;
            error(value, "Mermaid configuration '" + value.Path + "' must be a boolean."); return null;
        }
    }
    internal ChartColor? RegionFill(int quadrant) => _colors.TryGetValue("quadrant" + quadrant + "Fill", out var color) ? color : null;
    internal void Apply(Chart chart) {
        var series = chart.Series[0];
        if (_showX.HasValue) chart.Options.XAxis.Visible = _showX.Value;
        if (_showY.HasValue) chart.Options.YAxis.Visible = _showY.Value;
        if (_showTitle == false) chart.WithTitle(string.Empty);
        if (_pointRadius.HasValue) series.ConfigureMarkers(marker => marker.Radius = _pointRadius.Value);
        if (_colors.TryGetValue("quadrantPointFill", out var fill)) series.ConfigureMarkers(marker => marker.Fill = fill);
        if (_pointFontSize.HasValue || _colors.ContainsKey("quadrantPointTextFill")) series.ConfigureDataLabelStyle(style => {
            if (_pointFontSize.HasValue) style.FontSize = _pointFontSize.Value;
            if (_colors.TryGetValue("quadrantPointTextFill", out var text)) style.Color = text;
        });
        if (_titleFontSize.HasValue || _colors.ContainsKey("quadrantTitleFill")) chart.ConfigureTitleStyle(style => {
            if (_titleFontSize.HasValue) style.FontSize = _titleFontSize.Value;
            if (_colors.TryGetValue("quadrantTitleFill", out var title)) style.Color = title;
        });
        if (_colors.TryGetValue("quadrantXAxisTextFill", out var axis))
            foreach (var label in chart.Options.XAxis.Labels) chart.Options.XAxisLabelHighlights[label.Value] = axis;
    }
}
