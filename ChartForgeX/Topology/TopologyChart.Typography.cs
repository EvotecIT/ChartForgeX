using ChartForgeX.Typography;

namespace ChartForgeX.Topology;

public sealed partial class TopologyChart {
    internal TextMeasurementContext? TextMeasurement { get; set; }

    /// <summary>Gets or sets the render options a prepared chart was laid out with, used for caption-aware layout and routing.</summary>
    internal TopologyRenderOptions? RenderOptions { get; set; }

    /// <summary>
    /// Gets or sets the render options used when a render, prepare, diagnostics, or export call passes none. A builder
    /// that returns a chart can attach the options the chart was designed for, so every host draws the same layout
    /// without passing them along separately. Options passed to a call replace these as a whole; the two are not merged.
    /// </summary>
    public TopologyRenderOptions? DefaultRenderOptions { get; set; }

    /// <summary>Returns the options a call should use: those passed in, else the chart's own, else the defaults.</summary>
    internal TopologyRenderOptions ResolveRenderOptions(TopologyRenderOptions? options) => options ?? DefaultRenderOptions ?? new TopologyRenderOptions();
}
