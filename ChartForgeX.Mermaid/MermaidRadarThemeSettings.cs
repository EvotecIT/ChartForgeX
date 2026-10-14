using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Mermaid;

/// <summary>Maps authored radar paint and typography into shared native owners after base presentation.</summary>
internal sealed class MermaidRadarThemeSettings {
    private const string Prefix = "themeVariables.radar.";
    private static readonly string[] Keys = { "axisColor", "axisStrokeWidth", "axisLabelFontSize",
        "curveOpacity", "curveStrokeWidth", "graticuleColor", "graticuleOpacity", "graticuleStrokeWidth", "legendFontSize" };
    private readonly HashSet<string> _handled = new HashSet<string>(StringComparer.Ordinal);
    private ChartColor? _axisColor, _gridColor;
    private double? _axisWidth, _gridWidth, _curveWidth, _gridOpacity, _curveOpacity, _axisFontSize, _legendFontSize;

    internal static bool IsScalarPath(string path) => Array.IndexOf(Keys, path.StartsWith(Prefix, StringComparison.Ordinal)
        ? path.Substring(Prefix.Length) : string.Empty) >= 0;

    internal bool Handles(string path) {
        foreach (var handled in _handled)
            if (path == handled || path.StartsWith(handled + ".", StringComparison.Ordinal)) return true;
        return false;
    }

    internal static MermaidRadarThemeSettings Read(MermaidSourceConfiguration configuration,
        Action<MermaidConfigurationSetting, string> error) {
        var settings = new MermaidRadarThemeSettings();
        settings._axisColor = Color("axisColor"); settings._gridColor = Color("graticuleColor");
        settings._axisWidth = Number("axisStrokeWidth", 0, 1024); settings._gridWidth = Number("graticuleStrokeWidth", 0, 1024);
        settings._curveWidth = Number("curveStrokeWidth", 0, 1024);
        settings._gridOpacity = Number("graticuleOpacity", 0, 1); settings._curveOpacity = Number("curveOpacity", 0, 1);
        settings._axisFontSize = Number("axisLabelFontSize", 1, 256); settings._legendFontSize = Number("legendFontSize", 1, 256);
        return settings;

        MermaidConfigurationSetting? Select(string key) {
            var setting = configuration.Last(Prefix + key);
            if (setting != null) settings._handled.Add(setting.Path);
            return setting;
        }
        ChartColor? Color(string key) {
            var setting = Select(key);
            if (setting == null) return null;
            if (setting.IsString && ChartColor.TryParse(setting.Value, out var color)) return color;
            error(setting, "Mermaid configuration '" + setting.Path + "' must be a supported native color string.");
            return null;
        }
        double? Number(string key, double minimum, double maximum) {
            var setting = Select(key);
            if (setting == null) return null;
            if (setting.TryGetNumber(out var value) && value >= minimum && value <= maximum) return value;
            error(setting, "Mermaid configuration '" + setting.Path + "' must be a finite number from "
                + minimum + " through " + maximum + ".");
            return null;
        }
    }

    internal void Apply(Chart chart) {
        if (_axisColor.HasValue || _axisWidth.HasValue)
            chart.Options.PolarAxisStroke = new ChartStrokeStyle(_axisColor, _axisWidth);
        if (_gridColor.HasValue || _gridWidth.HasValue)
            chart.Options.PolarGridStroke = new ChartStrokeStyle(_gridColor, _gridWidth);
        if (_gridOpacity.HasValue) {
            chart.Options.PolarGridFill = _gridColor;
            chart.Options.PolarGridFillOpacity = _gridOpacity.Value;
        }
        if (_axisColor.HasValue || _axisFontSize.HasValue) chart.ConfigureTickLabelStyle(style => {
            if (_axisColor.HasValue) style.Color = _axisColor.Value;
            if (_axisFontSize.HasValue) style.FontSize = _axisFontSize.Value;
        });
        if (_legendFontSize.HasValue) chart.ConfigureLegendStyle(style => style.FontSize = _legendFontSize.Value);
        foreach (var series in chart.Series) {
            if (series.Kind != ChartSeriesKind.Radar) continue;
            series.ConfigureRadar(options => {
                if (_curveOpacity.HasValue) options.FillOpacity = _curveOpacity.Value;
                if (_curveWidth.HasValue) options.OutlineStroke = new ChartStrokeStyle(width: _curveWidth.Value);
            });
        }
    }
}
