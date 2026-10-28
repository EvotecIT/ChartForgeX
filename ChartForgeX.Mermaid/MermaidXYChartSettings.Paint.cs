using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Mermaid;

internal sealed partial class MermaidXYChartSettings {
    private const string PaintPrefix = "themeVariables.xyChart.";
    private static readonly string[] PaintKeys = { "backgroundColor", "titleColor", "legendTextColor", "dataLabelColor", "plotColorPalette" };
    private ChartColor? _background, _title, _legend, _dataLabel;
    private ChartColor[]? _palette;
    private static bool IsPaintScalarPath(string path) => path.StartsWith(PaintPrefix, StringComparison.Ordinal)
        && Array.IndexOf(PaintKeys, path.Substring(PaintPrefix.Length)) >= 0;
    private void ReadPaint(MermaidSourceConfiguration configuration, Action<MermaidConfigurationSetting, string> error) {
        _background = Color("backgroundColor"); _title = Color("titleColor");
        _legend = Color("legendTextColor"); _dataLabel = Color("dataLabelColor");
        var palette = Select("plotColorPalette");
        if (palette != null) {
            var parts = palette.IsString ? palette.Value?.Split(',') : null;
            if (parts == null || parts.Length == 0) Invalid(palette, "must be a non-empty comma-separated string of native literal colors");
            else {
                var colors = new ChartColor[parts.Length]; var valid = true;
                for (var index = 0; index < parts.Length; index++) if (!ChartColor.TryParse(parts[index].Trim(), out colors[index])) { valid = false; break; }
                if (valid) _palette = colors;
                else Invalid(palette, "must contain only native literal colors");
            }
        }
        MermaidConfigurationSetting? Select(string key) {
            var setting = configuration.Last(PaintPrefix + key);
            if (setting != null) _handled.Add(setting.Path);
            return setting;
        }
        void Invalid(MermaidConfigurationSetting setting, string rule) => error(setting, "Mermaid configuration '" + setting.Path + "' " + rule + ".");
        ChartColor? Color(string key) {
            var setting = Select(key); if (setting == null) return null;
            if (setting.IsString && ChartColor.TryParse(setting.Value, out var color)) return color;
            Invalid(setting, "must be a native literal color string"); return null;
        }
    }
    private void ApplyPaint(Chart chart, MermaidPresentationOptions? host) {
        // An explicit host theme owns colors. Source background still requests a painted canvas, using that host color.
        if (_background.HasValue) chart.WithTransparentBackground(false);
        if (host?.Theme != null) return;
        var theme = chart.Options.Theme;
        if (_background.HasValue) theme = theme.Configure(builder => { builder.Light.Background = _background.Value; builder.Dark.Background = _background.Value; });
        if (_palette != null) theme = theme.WithPalette(_palette);
        chart.WithTheme(theme);
        if (_title.HasValue) chart.ConfigureTitleStyle(style => style.Color = _title.Value);
        if (_legend.HasValue) chart.ConfigureLegendStyle(style => style.Color = _legend.Value);
        if (_dataLabel.HasValue) chart.ConfigureDataLabelStyle(style => style.Color = _dataLabel.Value);
    }
}
