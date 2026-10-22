using ChartForgeX.Typography;

namespace ChartForgeX.Topology;

public sealed partial class TopologyNodeDetail {
    /// <summary>Gets or sets optional native text styling resolved over the node's detail-row typography.</summary>
    /// <remarks>The same measured text and decorations are used by SVG and PNG exports.</remarks>
    public TextStyleOverride? TextStyle { get; set; }
}
