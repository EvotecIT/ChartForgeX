using System;
using ChartForgeX.Core;

namespace ChartForgeX.Mermaid;

/// <summary>Maps validated treemap presentation settings into the native hierarchy owner.</summary>
internal sealed class MermaidTreemapSettings {
    internal bool? ShowValues { get; private set; }

    internal static bool IsScalarPath(string path) => path == "treemap.showValues";
    internal bool Handles(string path) => IsScalarPath(path) || path.StartsWith("treemap.showValues.", StringComparison.Ordinal);

    internal static MermaidTreemapSettings Read(MermaidSourceConfiguration configuration, Action<MermaidConfigurationSetting, string> error) {
        var settings = new MermaidTreemapSettings();
        var setting = configuration.Last("treemap.showValues");
        if (setting == null) return settings;
        if (!setting.IsString && bool.TryParse(setting.Value, out var show)) settings.ShowValues = show;
        else error(setting, "Mermaid configuration 'treemap.showValues' must be a boolean.");
        return settings;
    }

    internal void Apply(Chart chart) {
        if (ShowValues.HasValue) chart.Options.Treemap.ShowValues = ShowValues.Value;
    }
}
