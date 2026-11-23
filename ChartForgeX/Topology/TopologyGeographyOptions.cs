using PowerForge.Fluent;

namespace ChartForgeX.Topology;

/// <summary>Geographic callouts and region hulls. Configure it through <see cref="TopologyRenderOptions.Geography"/>.</summary>
[Fluent(Copy = nameof(Clone))]
public sealed class TopologyGeographyOptions {
    internal TopologyGeographyOptions Clone() => (TopologyGeographyOptions)MemberwiseClone();
}
