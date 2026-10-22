using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Mermaid;

/// <summary>Maps source color variables to the existing immutable native presentation roles.</summary>
internal sealed class MermaidThemeColorSettings {
    private static readonly string[] Names = {
        "background", "textColor", "primaryColor", "primaryTextColor", "primaryBorderColor",
        "secondaryColor", "tertiaryColor", "lineColor"
    };
    private readonly Dictionary<string, ChartColor> _colors = new(StringComparer.Ordinal);

    internal static bool IsScalarPath(string path) {
        foreach (var name in Names) if (path == "themeVariables." + name) return true;
        return false;
    }

    internal bool Handles(string path) {
        foreach (var name in Names) {
            var scalar = "themeVariables." + name;
            if (path == scalar || path.StartsWith(scalar + ".", StringComparison.Ordinal)) return true;
        }
        return false;
    }

    internal static MermaidThemeColorSettings Read(MermaidSourceConfiguration configuration,
        Action<MermaidConfigurationSetting, string> error) {
        var result = new MermaidThemeColorSettings();
        foreach (var name in Names) {
            var setting = configuration.Last("themeVariables." + name);
            if (setting == null) continue;
            if (setting.IsString && ChartColor.TryParse(setting.Value, out var color)) result._colors[name] = color;
            else error(setting, "Mermaid configuration '" + setting.Path + "' must be a supported native color string.");
        }
        return result;
    }

    internal VisualTheme Apply(VisualTheme source) {
        if (_colors.Count == 0) return source;
        return source.Configure(builder => { Apply(builder.Light); Apply(builder.Dark); });
    }

    private void Apply(VisualDesignTokens tokens) {
        if (_colors.TryGetValue("background", out var background)) tokens.Background = background;
        if (_colors.TryGetValue("primaryColor", out var primary)) { tokens.Surface = primary; tokens.Neutral = primary; }
        if (_colors.TryGetValue("secondaryColor", out var secondary)) { tokens.ElevatedSurface = secondary; tokens.SecondaryAccent = secondary; }
        if (_colors.TryGetValue("tertiaryColor", out var tertiary)) tokens.Neutral2 = tertiary;
        // Specific primary roles take precedence over their shared text/border defaults.
        if (_colors.TryGetValue("textColor", out var text)) { tokens.Foreground = text; tokens.MutedForeground = text; }
        if (_colors.TryGetValue("primaryTextColor", out var primaryText)) tokens.Foreground = primaryText;
        if (_colors.TryGetValue("primaryBorderColor", out var border)) tokens.Border = border;
        if (_colors.TryGetValue("lineColor", out var line)) {
            tokens.Axis = line; tokens.Grid = line;
            // Mermaid topology relationships have the native Unknown state unless authored otherwise.
            // Configure its existing theme pair rather than writing explicit colors into source edges.
            tokens.Status.Neutral = new VisualTokenColor(line, line);
        }
    }
}
