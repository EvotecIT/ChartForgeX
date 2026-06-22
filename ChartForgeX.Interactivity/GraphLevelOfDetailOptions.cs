namespace ChartForgeX.Interactivity;

/// <summary>
/// Describes clustering and detail-reduction thresholds for graph explorer adapters.
/// </summary>
public sealed class GraphLevelOfDetailOptions {
    /// <summary>Gets or sets the node count at which adapters should prefer collapsed or summarized views.</summary>
    public int ClusterNodeThreshold { get; set; } = 150;

    /// <summary>Gets or sets the edge count at which adapters should hide edge labels until focus or selection.</summary>
    public int HideEdgeLabelsThreshold { get; set; } = 250;

    /// <summary>Gets or sets the node count at which adapters should prefer compact dot rendering over full labels.</summary>
    public int CompactNodeThreshold { get; set; } = 500;

    /// <summary>Gets or sets the node count at which Canvas or WebGL adapters should be preferred over SVG.</summary>
    public int CanvasPreferredNodeThreshold { get; set; } = 1200;

    /// <summary>Gets or sets whether clusters should be collapsed when the scene first renders.</summary>
    public bool CollapseClustersOnLoad { get; set; }
}
