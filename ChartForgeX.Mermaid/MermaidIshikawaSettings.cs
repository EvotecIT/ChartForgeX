using System;
using ChartForgeX.Primitives;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Mermaid;

/// <summary>Projects the pinned Ishikawa family's supported configuration into existing native roles.</summary>
internal sealed class MermaidIshikawaSettings {
    internal double? Padding { get; private set; }
    internal bool? UseMaxWidth { get; private set; }
    private ChartColor? _background, _line, _text;
    internal static bool IsScalarPath(string path) => path == "ishikawa.diagramPadding" || path == "ishikawa.useMaxWidth" || path == "themeVariables.mainBkg";
    internal bool Handles(string path) => IsScalarPath(path) || path.StartsWith("ishikawa.diagramPadding.", StringComparison.Ordinal)
        || path.StartsWith("ishikawa.useMaxWidth.", StringComparison.Ordinal) || path.StartsWith("themeVariables.mainBkg.", StringComparison.Ordinal);
    internal static MermaidIshikawaSettings Read(MermaidSourceConfiguration source, Action<MermaidConfigurationSetting, string> error) {
        var result = new MermaidIshikawaSettings(); var padding = source.Last("ishikawa.diagramPadding");
        if (padding != null) {
            if (padding.TryGetNumber(out var value) && value >= 0) result.Padding = value;
            else error(padding, "Mermaid configuration 'ishikawa.diagramPadding' must be finite and non-negative.");
        }
        var maximum = source.Last("ishikawa.useMaxWidth");
        if (maximum != null) {
            if (!maximum.IsString && bool.TryParse(maximum.Value, out var value)) result.UseMaxWidth = value;
            else error(maximum, "Mermaid configuration 'ishikawa.useMaxWidth' must be a boolean.");
        }
        result._background = ReadColor("mainBkg", true); result._line = ReadColor("lineColor", false); result._text = ReadColor("textColor", false);
        return result;

        ChartColor? ReadColor(string name, bool validate) {
            var setting = source.Last("themeVariables." + name); if (setting == null) return null;
            if (setting.IsString && ChartColor.TryParse(setting.Value, out var color)) return color;
            if (validate) error(setting, "Mermaid configuration '" + setting.Path + "' must be a supported native color string.");
            return null;
        }
    }
    internal void Apply(FishboneDiagramBlock block, MermaidPresentationOptions? host) {
        if (host?.Theme != null || !_background.HasValue && !_line.HasValue && !_text.HasValue) return;
        block.Options.Theme = block.Options.Theme.Configure(builder => {
            foreach (var tokens in new[] { builder.Light, builder.Dark }) {
                if (_background.HasValue) tokens.Surface = _background.Value;
                if (_line.HasValue) { tokens.Axis = _line.Value; tokens.Palette = new[] { _line.Value }; }
                if (_text.HasValue) { tokens.Foreground = _text.Value; tokens.Muted = _text.Value; tokens.MutedForeground = _text.Value; }
            }
        });
    }
}
