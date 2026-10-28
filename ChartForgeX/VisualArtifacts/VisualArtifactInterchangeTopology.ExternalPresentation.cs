using ChartForgeX.Topology;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualArtifacts;

public sealed partial class VisualArtifactInterchangeTopologyNode {
    /// <summary>Gets or sets the enabled external caption role. Non-null requires version 2.</summary>
    public TextStyleOverride? CaptionTextStyle { get; set; }
    /// <summary>Gets or sets external caption placement. Non-null requires version 2.</summary>
    public TopologyCaptionPlacement? CaptionPlacement { get; set; }
    /// <summary>Gets or sets the enabled caption surface. Non-null requires version 2.</summary>
    public TopologyCaptionSurface? CaptionSurface { get; set; }
    /// <summary>Gets or sets the logical external caption gap. Non-null requires version 2.</summary>
    public double? CaptionGap { get; set; }
    /// <summary>Gets or sets uniform native glyph scale. Non-null requires version 2.</summary>
    public double? GlyphScale { get; set; }
    /// <summary>Gets or sets the authored logical contour radius. Non-null requires version 2.</summary>
    public double? CornerRadius { get; set; }
}
