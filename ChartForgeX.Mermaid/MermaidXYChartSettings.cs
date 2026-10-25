using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Mermaid;

/// <summary>Maps supported XY source scalars into existing chart, axis, label and theme roles.</summary>
internal sealed partial class MermaidXYChartSettings {
    private readonly HashSet<string> _handled = new(StringComparer.Ordinal);
    internal int? Width, Height;
    internal bool? ShowTitle, ShowLegend, ShowDataLabel, ShowDataLabelOutsideBar, UseMaxWidth;
    internal ChartOrientation? Orientation;
    private double? _titleFontSize, _legendFontSize;
    private bool? _xTitle, _yTitle, _xLine, _yLine;
    private double? _xRotation, _yRotation;
    private static readonly string[] Keys = { "width", "height", "showTitle", "showLegend", "showDataLabel",
        "showDataLabelOutsideBar", "useMaxWidth", "titleFontSize", "legendFontSize", "chartOrientation",
        "xAxis.showTitle", "yAxis.showTitle", "xAxis.showAxisLine", "yAxis.showAxisLine", "xAxis.labelRotation", "yAxis.labelRotation" };

    internal static bool IsScalarPath(string path) => path.StartsWith("xyChart.", StringComparison.Ordinal)
        && Array.IndexOf(Keys, path.Substring(8)) >= 0 || IsPaintScalarPath(path);
    internal bool Handles(string path) {
        foreach (var item in _handled) if (path == item || path.StartsWith(item + ".", StringComparison.Ordinal)) return true;
        return false;
    }
    internal static MermaidXYChartSettings Read(MermaidSourceConfiguration configuration,
        Action<MermaidConfigurationSetting, string> error) {
        var settings = new MermaidXYChartSettings();
        settings.Width = Dimension("width"); settings.Height = Dimension("height");
        settings.ShowTitle = Boolean("showTitle"); settings.ShowLegend = Boolean("showLegend");
        settings.ShowDataLabel = Boolean("showDataLabel"); settings.ShowDataLabelOutsideBar = Boolean("showDataLabelOutsideBar");
        settings.UseMaxWidth = Boolean("useMaxWidth");
        settings._titleFontSize = Font("titleFontSize"); settings._legendFontSize = Font("legendFontSize");
        settings._xTitle = Boolean("xAxis.showTitle"); settings._yTitle = Boolean("yAxis.showTitle");
        settings._xLine = Boolean("xAxis.showAxisLine"); settings._yLine = Boolean("yAxis.showAxisLine");
        settings._xRotation = Number("xAxis.labelRotation"); settings._yRotation = Number("yAxis.labelRotation");
        var orientation = Select("chartOrientation");
        if (orientation != null) {
            if (orientation.IsString && orientation.Value == "horizontal") settings.Orientation = ChartOrientation.Horizontal;
            else if (orientation.IsString && orientation.Value == "vertical") settings.Orientation = ChartOrientation.Vertical;
            else Invalid(orientation, "must be horizontal or vertical");
        }
        settings.ReadPaint(configuration, error);
        return settings;

        MermaidConfigurationSetting? Select(string key) {
            var setting = configuration.Last("xyChart." + key);
            if (setting != null) settings._handled.Add(setting.Path);
            return setting;
        }
        void Invalid(MermaidConfigurationSetting setting, string rule) => error(setting, "Mermaid configuration '" + setting.Path + "' " + rule + ".");
        bool? Boolean(string key) {
            var setting = Select(key); if (setting == null) return null;
            if (!setting.IsString && bool.TryParse(setting.Value, out var value)) return value;
            Invalid(setting, "must be a boolean"); return null;
        }
        double? Number(string key) {
            var setting = Select(key); if (setting == null) return null;
            if (setting.TryGetNumber(out var value)) return value;
            Invalid(setting, "must be a finite number"); return null;
        }
        int? Dimension(string key) {
            var value = Number(key); if (!value.HasValue) return null;
            if (value.Value > 0 && value.Value <= int.MaxValue && Math.Truncate(value.Value) == value.Value) return (int)value.Value;
            Invalid(configuration.Last("xyChart." + key)!, "must be a positive whole pixel dimension within the native integer range"); return null;
        }
        double? Font(string key) {
            var value = Number(key); if (!value.HasValue) return null;
            if (value.Value > 0 && value.Value <= 512) return value;
            Invalid(configuration.Last("xyChart." + key)!, "must be positive and at most 512 pixels"); return null;
        }
    }
    internal void Apply(Chart chart, bool horizontal, MermaidPresentationOptions? host) {
        var x = horizontal ? chart.Options.YAxis : chart.Options.XAxis;
        var y = horizontal ? chart.Options.XAxis : chart.Options.YAxis;
        if (_xTitle == false) { if (horizontal) chart.WithYAxis(string.Empty); else chart.WithXAxis(string.Empty); }
        if (_yTitle == false) { if (horizontal) chart.WithXAxis(string.Empty); else chart.WithYAxis(string.Empty); }
        if (_xLine.HasValue) x.ShowLine = _xLine.Value;
        if (_yLine.HasValue) y.ShowLine = _yLine.Value;
        if (_xRotation.HasValue) x.LabelAngle = _xRotation.Value;
        if (_yRotation.HasValue) y.LabelAngle = _yRotation.Value;
        if (host?.Theme == null && host?.FontSize == null) {
            if (_titleFontSize.HasValue) chart.ConfigureTitleStyle(style => style.FontSize = _titleFontSize.Value);
            if (_legendFontSize.HasValue) chart.ConfigureLegendStyle(style => style.FontSize = _legendFontSize.Value);
        }
        ApplyPaint(chart, host);
    }
}
