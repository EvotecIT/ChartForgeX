using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;

namespace ChartForgeX.Mermaid;

/// <summary>Maps source color variables to the existing immutable native presentation roles.</summary>
internal sealed class MermaidRequirementThemeSettings {
    private static readonly string[] Names = { "requirementBackground", "requirementBorderColor", "requirementTextColor", "relationColor", "relationLabelColor", "relationLabelBackground" };
    private readonly Dictionary<string, string> _colors = new(StringComparer.Ordinal);


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

    internal static MermaidRequirementThemeSettings Read(MermaidSourceConfiguration configuration,
        Action<MermaidConfigurationSetting, string> error) {
        var result = new MermaidRequirementThemeSettings();
        foreach (var name in Names) {
            var setting = configuration.Last("themeVariables." + name);
            if (setting == null) continue;
            if (setting.IsString && ChartColor.TryParse(setting.Value, out _)) result._colors[name] = setting.Value!;
            else error(setting, "Mermaid configuration '" + setting.Path + "' must be a supported native color string.");
        }
        return result;
    }

    internal void Apply(TopologyNode node) {
        if (_colors.TryGetValue("requirementBackground", out var fill)) node.BackgroundColor = fill;
        if (_colors.TryGetValue("requirementBorderColor", out var border)) node.Color = border;
        if (_colors.TryGetValue("requirementTextColor", out var text)) node.LabelColor = text;
    }
    internal void Apply(TopologyEdge edge) {
        if (_colors.TryGetValue("relationColor", out var line)) edge.Color = line;
        if (_colors.TryGetValue("relationLabelColor", out var text)) edge.LabelColor = text;
        if (_colors.TryGetValue("relationLabelBackground", out var fill)) edge.LabelBackgroundColor = fill;
    }
}
