namespace ChartForgeX.Topology;

public sealed partial class TopologyNode {
    internal TopologySurfaceElementAppearance? SurfaceElementAppearance { get; set; }
    private void InvalidateSurfaceElementAppearance() => SurfaceElementAppearance = null;
}
