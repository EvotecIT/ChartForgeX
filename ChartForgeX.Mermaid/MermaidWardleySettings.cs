using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Mermaid;

/// <summary>Validates supported Wardley colors and scalar host geometry at the shared configuration boundary.</summary>
internal sealed class MermaidWardleySettings {
    private static readonly string[] Colors = { "backgroundColor", "axisColor", "axisTextColor", "gridColor", "componentFill", "componentStroke",
        "componentLabelColor", "linkStroke", "evolutionStroke", "annotationStroke", "annotationTextColor", "annotationFill" };
    private readonly Dictionary<string, ChartColor> _colors = new(StringComparer.Ordinal);
    internal int? Width { get; private set; }
    internal int? Height { get; private set; }
    internal double? Padding { get; private set; }
    internal bool? UseMaxWidth { get; private set; }
    internal bool? ShowGrid { get; private set; }
    internal static bool IsScalarPath(string path) {
        if (path == "wardley-beta.width" || path == "wardley-beta.height" || path == "wardley-beta.padding" || path == "wardley-beta.useMaxWidth" || path == "wardley-beta.showGrid") return true;
        foreach (var name in Colors) if (path == "themeVariables.wardley." + name) return true;
        return false;
    }
    internal bool Handles(string path) {
        if (IsScalarPath(path)) return true;
        foreach (var name in Colors) if (path.StartsWith("themeVariables.wardley." + name + ".", StringComparison.Ordinal)) return true;
        foreach (var name in new[] { "width", "height", "padding", "useMaxWidth", "showGrid" }) if (path.StartsWith("wardley-beta." + name + ".", StringComparison.Ordinal)) return true;
        return false;
    }
    internal static MermaidWardleySettings Read(MermaidSourceConfiguration configuration, Action<MermaidConfigurationSetting, string> error) {
        var result = new MermaidWardleySettings();
        foreach (var name in Colors) {
            var setting = configuration.Last("themeVariables.wardley." + name); if (setting == null) continue;
            if (setting.IsString && ChartColor.TryParse(setting.Value, out var color)) result._colors[name] = color;
            else Invalid(setting, "must be a supported native color string");
        }
        result.Width = Integer("width"); result.Height = Integer("height");
        var padding = configuration.Last("wardley-beta.padding");
        if (padding != null) {
            if (padding.TryGetNumber(out var value) && value >= 0 && value <= 4096) result.Padding = value;
            else Invalid(padding, "must be a finite number from zero through 4096");
        }
        result.UseMaxWidth = Boolean("useMaxWidth"); result.ShowGrid = Boolean("showGrid");
        return result;
        bool? Boolean(string key) {
            var setting = configuration.Last("wardley-beta." + key); if (setting == null) return null;
            if (!setting.IsString && bool.TryParse(setting.Value, out var value)) return value;
            Invalid(setting, "must be a boolean"); return null;
        }
        int? Integer(string key) {
            var setting = configuration.Last("wardley-beta." + key); if (setting == null) return null;
            if (setting.TryGetNumber(out var value) && value >= 1 && value <= int.MaxValue && value == Math.Floor(value)) return (int)value;
            Invalid(setting, "must be a positive 32-bit integer"); return null;
        }
        void Invalid(MermaidConfigurationSetting setting, string reason) => error(setting, "Mermaid configuration '" + setting.Path + "' " + reason + ".");
    }
    internal WardleyMapStyle? Style() {
        if (_colors.Count == 0) return null;
        ChartColor? Color(string name) => _colors.TryGetValue(name, out var value) ? value : null;
        return new WardleyMapStyle(Color("backgroundColor"), Color("axisColor"), Color("axisTextColor"), Color("gridColor"),
            Color("componentFill"), Color("componentStroke"), Color("componentLabelColor"), Color("linkStroke"), Color("evolutionStroke"),
            Color("annotationStroke"), Color("annotationTextColor"), Color("annotationFill"));
    }
}
