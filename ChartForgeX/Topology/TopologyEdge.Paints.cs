namespace ChartForgeX.Topology;

public sealed partial class TopologyEdge {
    /// <summary>Gets or sets an explicit relationship and endpoint label paint. Null keeps the theme's text paints.</summary>
    /// <remarks>This paint is independent of the relationship line, status and palette identity.</remarks>
    public string? LabelColor { get; set; }
}
