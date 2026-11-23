using ChartForgeX.Primitives;
namespace ChartForgeX.Topology;

public sealed partial class TopologyEdge {
    /// <summary>Gets or sets an explicit relationship and endpoint label paint. Null keeps the theme's text paints.</summary>
    /// <remarks>This paint is independent of the relationship line, status and palette identity.</remarks>
    public ChartColor? LabelColor { get; set; }

    /// <summary>Gets or sets the relationship and endpoint label backplate fill; null retains the active native theme.</summary>
    /// <remarks>The existing IncludeEdgeLabelBackplates option still controls visibility. This paint does not recolor text or the relationship line.</remarks>
    public ChartColor? LabelBackgroundColor { get; set; }
}
