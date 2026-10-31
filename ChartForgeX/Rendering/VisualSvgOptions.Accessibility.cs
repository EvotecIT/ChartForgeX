namespace ChartForgeX.Rendering;

public sealed partial class VisualSvgOptions {
    internal bool Explorable { get; private set; }

    /// <summary>Lets keyboard-enabled HTML hosts expose named scene groups instead of one image.</summary>
    internal VisualSvgOptions WithExplorableSemantics(bool enabled) =>
        new VisualSvgOptions(IdPrefix, _colorVariables, LinkTarget, Responsive, EffectProfile, EffectRasterOptions) {
            _patternTiles = _patternTiles, _textPresentation = _textPresentation, Explorable = enabled
        };
}
