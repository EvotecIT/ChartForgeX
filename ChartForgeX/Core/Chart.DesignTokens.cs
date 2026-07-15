using System;
using ChartForgeX.Themes;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Applies shared product-neutral design tokens to the chart theme.</summary>
    public Chart WithDesignTokens(VisualDesignTokens tokens) {
        if (tokens == null) throw new ArgumentNullException(nameof(tokens));
        tokens.ApplyTo(Options.Theme);
        return this;
    }
}
