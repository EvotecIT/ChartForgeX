using System;

namespace ChartForgeX.Mermaid;

/// <summary>Validates source color modes before the topology adapter assigns theme palette slots.</summary>
internal sealed class MermaidMindMapSettings {
    internal MermaidMindMapColorMode ColorBy { get; private set; }
    internal static bool IsScalarPath(string path) => path == "mindmap.colorBy";
    internal bool Handles(string path) => IsScalarPath(path) || path.StartsWith("mindmap.colorBy.", StringComparison.Ordinal);

    internal static MermaidMindMapSettings Read(MermaidSourceConfiguration configuration, Action<MermaidConfigurationSetting, string> error) {
        var settings = new MermaidMindMapSettings();
        var setting = configuration.Last("mindmap.colorBy");
        if (setting == null) return settings;
        if (setting.IsString && setting.Value == "branch") settings.ColorBy = MermaidMindMapColorMode.Branch;
        else if (setting.IsString && setting.Value == "depth") settings.ColorBy = MermaidMindMapColorMode.Depth;
        else error(setting, "Mermaid configuration 'mindmap.colorBy' must be 'branch' or 'depth'.");
        return settings;
    }
}
