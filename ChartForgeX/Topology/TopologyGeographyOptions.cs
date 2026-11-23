using PowerForge.Fluent;

namespace ChartForgeX.Topology;

/// <summary>Geographic callouts and region hulls. Configure it through <see cref="TopologyRenderOptions.Geography"/>.</summary>
[Fluent(Copy = nameof(Clone))]
public sealed class TopologyGeographyOptions {
    internal TopologyGeographyOptions Clone() => (TopologyGeographyOptions)MemberwiseClone();
    /// <summary>Gets or sets whether geographic topology layouts should render map callout summaries for coordinated groups.</summary>
    public bool IncludeCallouts { get; set; }


    /// <summary>Gets or sets whether geographic topology layouts should render soft status-colored region hulls around coordinated groups.</summary>
    public bool IncludeRegionHulls { get; set; }


    /// <summary>Gets or sets whether geographic callouts should prefer map-edge placements before near-anchor placement.</summary>
    public bool PreferCalloutMapEdges { get; set; }


    /// <summary>Gets or sets the maximum number of geographic callout summaries to render.</summary>
    public int CalloutMaxItems { get; set; } = 4;


    /// <summary>Gets or sets the padding added around nodes when calculating geographic region hull radius.</summary>
    public double RegionHullPadding { get; set; } = 22;


    /// <summary>Gets or sets the minimum geographic region hull radius.</summary>
    public double RegionHullMinRadius { get; set; } = 52;


    /// <summary>Gets or sets the maximum geographic region hull radius.</summary>
    public double RegionHullMaxRadius { get; set; } = 96;
}
