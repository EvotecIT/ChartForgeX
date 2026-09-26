using ChartForgeX.Typography;

namespace ChartForgeX.Topology;

public sealed partial class TopologyChart {
    internal TextMeasurementContext? TextMeasurement { get; set; }

    /// <summary>Gets or sets the render options a prepared chart was laid out with, used for caption-aware layout and routing.</summary>
    internal TopologyRenderOptions? RenderOptions { get; set; }
}
