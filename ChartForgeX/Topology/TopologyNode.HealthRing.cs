namespace ChartForgeX.Topology;

public sealed partial class TopologyNode {
    /// <summary>Gets or sets whether a ring inside the node bounds shows its health independently of its accent or icon colour.</summary>
    /// <remarks>The ring uses the semantic status token in SVG and PNG. It is most useful with circular shapes and icon or dot nodes.
    /// It does not replace the node's accessible status or optional status badge.</remarks>
    public bool ShowHealthRing { get; set; }
}
