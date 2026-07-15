using System;
using ChartForgeX.Themes;

namespace ChartForgeX.Topology;

public sealed partial class TopologyChart {
    /// <summary>Applies shared product-neutral design tokens to the topology theme.</summary>
    public TopologyChart WithDesignTokens(VisualDesignTokens tokens) {
        if (tokens == null) throw new ArgumentNullException(nameof(tokens));
        Theme = tokens.ApplyTo(Theme ?? TopologyTheme.Light());
        return this;
    }
}
