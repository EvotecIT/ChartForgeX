using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;

namespace ChartForgeX.Mermaid;

/// <summary>Validates the pinned Cynefin geometry and paint roles before native projection.</summary>
internal sealed class MermaidCynefinSettings {
    private static readonly string[] Colors = { "complexBg", "complicatedBg", "chaoticBg", "clearBg", "confusionBg",
        "boundaryColor", "cliffColor", "arrowColor", "textColor", "labelColor" };
    private static readonly string[] Numbers = { "width", "height", "padding", "boundaryAmplitude", "seed" };
    private static readonly string[] FontSizes = { "domainFontSize", "itemFontSize" };
    private static readonly string[] Widths = { "boundaryWidth", "cliffWidth", "arrowWidth" };
    private readonly Dictionary<string, ChartColor> _colors = new(StringComparer.Ordinal);
    private readonly Dictionary<string, double> _numbers = new(StringComparer.Ordinal);
    internal bool? ShowDomainDescriptions { get; private set; }
    internal bool? UseMaxWidth { get; private set; }

    internal static bool IsScalarPath(string path) {
        foreach (var name in Colors) if (path == "themeVariables.cynefin." + name) return true;
        foreach (var name in FontSizes) if (path == "themeVariables.cynefin." + name) return true;
        foreach (var name in Widths) if (path == "themeVariables.cynefin." + name) return true;
        foreach (var name in Numbers) if (path == "cynefin." + name) return true;
        return path == "cynefin.showDomainDescriptions" || path == "cynefin.useMaxWidth";
    }

    internal bool Handles(string path) {
        if (IsScalarPath(path)) return true;
        while (true) {
            var dot = path.LastIndexOf('.');
            if (dot < 0) return false;
            path = path.Substring(0, dot);
            if (IsScalarPath(path)) return true;
        }
    }

    internal static MermaidCynefinSettings Read(MermaidSourceConfiguration source, Action<MermaidConfigurationSetting, string> error) {
        var result = new MermaidCynefinSettings();
        foreach (var name in Colors) {
            var setting = source.Last("themeVariables.cynefin." + name);
            if (setting == null) continue;
            if (setting.IsString && ChartColor.TryParse(setting.Value, out var value)) result._colors[name] = value;
            else Invalid(setting, "must be a supported native color string");
        }
        foreach (var name in Numbers) Number("cynefin.", name, name == "seed" ? -9007199254740991d : name is "width" or "height" ? 1 : 0,
            name == "seed" ? 9007199254740991d : name is "width" or "height" ? 65536 : 4096);
        foreach (var name in Widths) Number("themeVariables.cynefin.", name, 0, 4096);
        foreach (var name in FontSizes) Number("themeVariables.cynefin.", name, 1, 256);
        result.ShowDomainDescriptions = Boolean("showDomainDescriptions"); result.UseMaxWidth = Boolean("useMaxWidth");
        return result;

        void Number(string prefix, string name, double minimum, double maximum) {
            var setting = source.Last(prefix + name); if (setting == null) return;
            if (setting.TryGetNumber(out var value) && value >= minimum && value <= maximum && (name != "itemFontSize" || value > 1)) result._numbers[name] = value;
            else Invalid(setting, "must be a finite number within the native geometry budget");
        }
        bool? Boolean(string name) {
            var setting = source.Last("cynefin." + name); if (setting == null) return null;
            if (!setting.IsString && bool.TryParse(setting.Value, out var value)) return value;
            Invalid(setting, "must be a boolean"); return null;
        }
        void Invalid(MermaidConfigurationSetting setting, string message) => error(setting, "Mermaid configuration '" + setting.Path + "' " + message + ".");
    }

    internal double Number(string name, double fallback) => _numbers.TryGetValue(name, out var value)
        ? value == 0 && Array.IndexOf(Widths, name) >= 0 ? fallback : value : fallback;
    internal double? AuthoredNumber(string name) => _numbers.TryGetValue(name, out var value) ? value : (double?)null;
    internal ChartColor? AuthoredColor(string name) => _colors.TryGetValue(name, out var value) ? value : (ChartColor?)null;
    internal void ApplyTitleColor(TopologyChart chart) {
        if (!_colors.TryGetValue("labelColor", out var label)) return;
        chart.Theme = chart.Theme.Configure(builder => { builder.Light.Foreground = label; builder.Dark.Foreground = label; });
    }
    internal ChartColor Color(string name, ChartColor fallback) => _colors.TryGetValue(name, out var value) ? value : fallback;
}
