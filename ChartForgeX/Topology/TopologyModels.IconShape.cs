namespace ChartForgeX.Topology;

public sealed partial class TopologyNode {
    /// <summary>Gets or sets an explicit native glyph shape before catalog/kind fallback. Null preserves catalog/kind inference.</summary>
    /// <remarks>Auto keeps the normal fallback. A retained non-null policy is captured in interchange version2, even without an IconId.</remarks>
    public TopologyIconShape? IconShape { get; set; }
}
