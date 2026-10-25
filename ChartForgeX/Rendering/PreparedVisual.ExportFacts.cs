using System;

namespace ChartForgeX.Rendering;

/// <summary>The exact native export boundary retained by an adapter; never reconstructed from a DOM or authored source IDs.</summary>
internal sealed class VisualSvgEmission {
    internal VisualSvgEmission(string svg, string prefix, bool capturePngText) : this(svg, prefix, capturePngText, new VisualSvgOptions(prefix)) { }
    internal VisualSvgEmission(string svg, string prefix, bool capturePngText, VisualSvgOptions options) {
        Svg = svg; Prefix = prefix; CapturePngText = capturePngText;
        Options = new VisualSvgOptions(prefix, options.ColorVariables, options.LinkTarget, options.Responsive, options.EffectProfile, options.EffectRasterOptions).WithPatternTiles(options.PatternTiles);
    }
    internal VisualSvgOptions Options { get; }
    internal string Svg { get; }
    internal string Prefix { get; }
    internal bool CapturePngText { get; }
}

public sealed partial class PreparedVisual {
    /// <summary>Emits once and retains the actual resolved native namespace and requested interactive export mode.</summary>
    internal VisualSvgEmission ExportSvgEmission(VisualSvgOptions options) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        var svg = VisualSceneSvgRenderer.Render(_scene, _accessibility.Name, _accessibility.Description,
            _accessibility.Language, _accessibility.IsDecorative, options.IdPrefix, options, _capturePngText, out var prefix);
        return new VisualSvgEmission(svg, prefix, _capturePngText, options);
    }
}
