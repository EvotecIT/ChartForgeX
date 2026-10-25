using System;

namespace ChartForgeX.Mermaid;

/// <summary>Validates the bounded source icon switch; icon packs and CSS remain inert.</summary>
internal sealed class MermaidTreeViewSettings {
    internal bool ShowIcons { get; private set; }
    internal static bool IsScalarPath(string path) => path == "treeView.showIcons";
    internal bool Handles(string path) => IsScalarPath(path);
    internal static MermaidTreeViewSettings Read(MermaidSourceConfiguration configuration, Action<MermaidConfigurationSetting, string> error) {
        var settings = new MermaidTreeViewSettings(); var setting = configuration.Last("treeView.showIcons");
        if (setting == null) return settings;
        if (!setting.IsString && bool.TryParse(setting.Value, out var value)) settings.ShowIcons = value;
        else error(setting, "Mermaid configuration 'treeView.showIcons' must be a boolean.");
        return settings;
    }
}
