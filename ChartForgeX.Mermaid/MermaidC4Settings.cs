using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Mermaid;

/// <summary>Admits finite source scalars declared and consumed by the pinned C4 source owners.</summary>
internal sealed partial class MermaidC4Settings {
    private static readonly string[] ElementTypes = { "person", "system", "system_db", "system_queue", "container", "container_db", "container_queue", "component", "component_db", "component_queue" };
    private static readonly string[] GeometryNames = { "diagramMarginX", "diagramMarginY", "c4ShapeMargin", "c4ShapePadding", "width", "nextLinePaddingX" };
    private readonly Dictionary<string, ElementPresentation> _elements = new(StringComparer.Ordinal);
    private readonly Dictionary<string, double> _geometry = new(StringComparer.Ordinal);
    private TextStyleOverride? _boundary, _message;
    private bool? _wrap;
    internal bool HasSettings => _elements.Count > 0 || _geometry.Count > 0 || _boundary != null || _message != null || _wrap.HasValue;
    internal static bool IsScalarPath(string path) {
        if (!path.StartsWith("c4.", StringComparison.Ordinal)) return false;
        var name = path.Substring(3);
        if (name == "wrap" || Array.IndexOf(GeometryNames, name) >= 0) return true;
        foreach (var type in Types()) if (name == type + "FontFamily" || name == type + "FontSize" || name == type + "FontWeight"
            || name == type + "_bg_color" || name == type + "_border_color") return true;
        return name == "boundaryFontFamily" || name == "boundaryFontSize" || name == "boundaryFontWeight"
            || name == "messageFontFamily" || name == "messageFontSize" || name == "messageFontWeight";
    }
    internal bool Handles(string path) {
        if (!path.StartsWith("c4.", StringComparison.Ordinal)) return false;
        if (IsScalarPath(path)) return true;
        var dot = path.IndexOf('.', 3);
        return dot >= 0 && IsScalarPath(path.Substring(0, dot));
    }
    private static IEnumerable<string> Types() {
        foreach (var type in ElementTypes) { yield return type; yield return "external_" + type; }
    }
    internal static MermaidC4Settings Read(MermaidSourceConfiguration configuration, Action<MermaidConfigurationSetting, string> error) {
        var result = new MermaidC4Settings();
        foreach (var name in GeometryNames) {
            var setting = configuration.Last("c4." + name); if (setting == null) continue;
            var positive = name == "width";
            if (setting.TryGetNumber(out var value) && !double.IsNaN(value) && !double.IsInfinity(value)
                && value >= (positive ? 1 : 0) && value <= 10000) result._geometry[name] = value;
            else Invalid(setting, positive ? "must be a finite number from 1 through 10000" : "must be a finite number from 0 through 10000");
        }
        var wrap = configuration.Last("c4.wrap");
        if (wrap != null) {
            if (!wrap.IsString && bool.TryParse(wrap.Value, out var enabled)) result._wrap = enabled;
            else Invalid(wrap, "must be a boolean");
        }
        foreach (var type in Types()) {
            var font = Font(type); var fill = Color(type + "_bg_color"); var border = Color(type + "_border_color");
            if (font != null || fill != null || border != null) result._elements[type] = new ElementPresentation(font, fill, border);
        }
        result._boundary = Font("boundary"); result._message = Font("message");
        return result;
        string? Color(string key) {
            var setting = configuration.Last("c4." + key); if (setting == null) return null;
            if (setting.IsString && ChartColor.TryParse(setting.Value, out _)) return setting.Value;
            Invalid(setting, "must be a supported native color string"); return null;
        }
        TextStyleOverride? Font(string type) => MermaidConfigurationTypography.Read(configuration,"c4."+type,error);
        void Invalid(MermaidConfigurationSetting setting, string reason) => error(setting, "Mermaid configuration '" + setting.Path + "' " + reason + ".");
    }
    private sealed class ElementPresentation {
        internal ElementPresentation(TextStyleOverride? font, string? fill, string? border) { Font = font; Fill = fill; Border = border; }
        internal TextStyleOverride? Font { get; }
        internal string? Fill { get; }
        internal string? Border { get; }
    }
}
