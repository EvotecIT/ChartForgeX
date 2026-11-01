namespace ChartForgeX.Rendering;

public sealed partial class VisualSvgOptions {
    internal bool Explorable { get; private set; }

    /// <summary>Lets keyboard-enabled HTML hosts expose named scene groups instead of one image.</summary>
    internal VisualSvgOptions WithExplorableSemantics(bool enabled) {
        var copy = Copy(); copy.Explorable = enabled; return copy;
    }
}
