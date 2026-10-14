using System;
using ChartForgeX.Themes;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Applies shared product-neutral design tokens to the chart theme.</summary>
    public Chart WithDesignTokens(VisualDesignTokens tokens) {
        if (tokens == null) throw new ArgumentNullException(nameof(tokens));
        Options.Theme = Options.Theme.Configure(builder => {
            var palette = builder.Colors(Options.ThemeMode);
            palette.CopyFrom(tokens);
        });
        return this;
    }
}
