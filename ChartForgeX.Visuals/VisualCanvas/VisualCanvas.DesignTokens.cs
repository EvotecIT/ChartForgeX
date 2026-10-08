using System;
using ChartForgeX.Themes;

namespace ChartForgeX.Composition;

public sealed partial class VisualCanvas {
    /// <summary>Applies shared product-neutral design tokens to the canvas and its renderer theme.</summary>
    public VisualCanvas WithDesignTokens(VisualDesignTokens tokens) {
        if (tokens == null) throw new ArgumentNullException(nameof(tokens));
        BackgroundTop = tokens.Background;
        BackgroundBottom = tokens.Surface;
        tokens.ApplyTo(Theme);
        return this;
    }
}
