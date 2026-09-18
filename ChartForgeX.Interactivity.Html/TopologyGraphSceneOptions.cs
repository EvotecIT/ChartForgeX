namespace ChartForgeX.Interactivity.Html;

/// <summary>
/// Controls how a static <see cref="ChartForgeX.Topology.TopologyChart"/> is projected into a reusable graph exploration scene.
/// </summary>
public sealed class TopologyGraphSceneOptions {
    /// <summary>Gets or sets the icon catalog used to resolve topology icon symbols, colors, shapes, and artwork.</summary>
    public ChartForgeX.Topology.TopologyIconCatalog? IconCatalog { get; set; }

    /// <summary>Gets or sets whether safe node and catalog artwork should become graph image nodes.</summary>
    public bool IncludeResolvedIconArtwork { get; set; } = true;

    /// <summary>Gets or sets whether hierarchy parent and level metadata should become native graph hierarchy fields.</summary>
    public bool PreserveHierarchyMetadata { get; set; } = true;

    /// <summary>Gets or sets whether topology groups should become graph clusters.</summary>
    public bool IncludeGroupsAsClusters { get; set; } = true;

    /// <summary>Gets or sets whether topology groups should also create graph cluster membership through node <c>GroupId</c>.</summary>
    public bool UseGroupsAsClusterIds { get; set; } = true;

    /// <summary>Gets or sets the node count at which declared topology groups start collapsed for a readable overview; set to zero to keep groups expanded.</summary>
    public int CollapseGroupsOnLoadThreshold { get; set; } = 32;

    /// <summary>Gets or sets whether secondary and tertiary edge facts should be appended to the visible edge label instead of remaining available as inspector metadata.</summary>
    public bool IncludeEdgeDetailInLabels { get; set; } = true;

    /// <summary>Gets or sets the edge count at which overview labels are hidden until selection or focus; set to zero to retain the graph scene default.</summary>
    public int DenseEdgeLabelThreshold { get; set; } = 32;

    /// <summary>Gets or sets whether manual topology coordinates should seed graph node positions.</summary>
    public bool PreserveManualCoordinates { get; set; } = true;

    /// <summary>Gets or sets whether non-zero topology coordinates from prepared or caller-supplied nodes should seed graph node positions.</summary>
    public bool PreserveNonZeroCoordinates { get; set; }

    /// <summary>Gets or sets whether deterministic topology layout coordinates should seed the interactive graph before browser physics begins.</summary>
    public bool SeedPreparedLayout { get; set; } = true;

    /// <summary>Gets or sets whether nodes seeded from the prepared topology layout should remain fixed.</summary>
    public bool FixPreparedLayout { get; set; }

    /// <summary>Gets or sets whether browser physics should immediately replace a deterministic prepared layout on load; users can still start stabilization from the explorer controls when this is false.</summary>
    public bool StabilizePreparedLayoutOnLoad { get; set; }

    /// <summary>Gets or sets whether large-topology graph exploration defaults should be applied to the resulting graph scene.</summary>
    public bool UseSuperTopologyDefaults { get; set; } = true;

    /// <summary>Gets or sets whether opt-in manipulation capabilities should be advertised by the resulting graph scene.</summary>
    public bool EnableManipulation { get; set; }
}
