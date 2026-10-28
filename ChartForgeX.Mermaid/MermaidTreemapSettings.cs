using System;
using ChartForgeX.Core;

namespace ChartForgeX.Mermaid;

/// <summary>Maps validated treemap presentation settings into the native hierarchy owner.</summary>
internal sealed class MermaidTreemapSettings {
    internal bool? ShowValues { get; private set; }
    internal ChartValueFormat? ValueFormat { get; private set; }

    internal static bool IsScalarPath(string path) => path is "treemap.showValues" or "treemap.valueFormat";
    internal bool Handles(string path) => IsScalarPath(path) || path.StartsWith("treemap.showValues.", StringComparison.Ordinal)
        || path.StartsWith("treemap.valueFormat.", StringComparison.Ordinal);

    internal static MermaidTreemapSettings Read(MermaidSourceConfiguration configuration,
        Action<MermaidConfigurationSetting, string> error, Action<MermaidConfigurationSetting, string> unsupported) {
        var settings = new MermaidTreemapSettings();
        var setting = configuration.Last("treemap.showValues");
        if (setting != null) {
            if (!setting.IsString && bool.TryParse(setting.Value, out var show)) settings.ShowValues = show;
            else error(setting, "Mermaid configuration 'treemap.showValues' must be a boolean.");
        }
        setting = configuration.Last("treemap.valueFormat");
        if (setting != null) {
            if (!setting.IsString || setting.Value == null || setting.Value.Length > 128)
                error(setting, "Mermaid configuration 'treemap.valueFormat' must be a string of at most 128 characters.");
            else if (MermaidTreemapValueFormat.TryRead(setting.Value, out var format)) settings.ValueFormat = format;
            else unsupported(setting, "Mermaid treemap valueFormat is retained without application; native mapping supports grouping, dollar currency and fixed decimal/percentage patterns with precision zero through fifteen.");
        }
        return settings;
    }

    internal void Apply(Chart chart) {
        if (ShowValues.HasValue) chart.Options.Treemap.ShowValues = ShowValues.Value;
        if (ValueFormat != null) chart.Options.ValueFormat = ValueFormat;
    }
}
