namespace ChartForgeX.Interactivity.Html;

/// <summary>
/// Controls how a static <see cref="ChartForgeX.Topology.TopologyChart"/> is projected into a reusable graph exploration scene.
/// </summary>
public sealed class TopologyGraphSceneOptions {
    /// <summary>Gets or sets whether topology groups should become graph clusters.</summary>
    public bool IncludeGroupsAsClusters { get; set; } = true;

    /// <summary>Gets or sets whether topology groups should also create graph cluster membership through node <c>GroupId</c>.</summary>
    public bool UseGroupsAsClusterIds { get; set; } = true;

    /// <summary>Gets or sets whether manual topology coordinates should seed graph node positions.</summary>
    public bool PreserveManualCoordinates { get; set; } = true;

    /// <summary>Gets or sets whether non-zero topology coordinates from prepared or caller-supplied nodes should seed graph node positions.</summary>
    public bool PreserveNonZeroCoordinates { get; set; }

    /// <summary>Gets or sets whether large-topology graph exploration defaults should be applied to the resulting graph scene.</summary>
    public bool UseSuperTopologyDefaults { get; set; } = true;

    /// <summary>Gets or sets whether opt-in manipulation capabilities should be advertised by the resulting graph scene.</summary>
    public bool EnableManipulation { get; set; }
}
