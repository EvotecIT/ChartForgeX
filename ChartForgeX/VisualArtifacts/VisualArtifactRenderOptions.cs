using ChartForgeX.Topology;
using ChartForgeX.Core;

namespace ChartForgeX.VisualArtifacts;

/// <summary>Defines static core rendering options shared by artifacts and host adapters.</summary>
public sealed class VisualArtifactRenderOptions {
    /// <summary>Gets or sets topology-specific rendering options.</summary>
    public TopologyRenderOptions? Topology { get; set; }
    /// <summary>Gets or sets dependency-free raster encoding options.</summary>
    public RasterImageOptions? Raster { get; set; }
}
