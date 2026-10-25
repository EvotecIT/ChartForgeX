using ChartForgeX.Typography;

namespace ChartForgeX.Topology;

/// <summary>Positions an enabled external native caption around its body slot.</summary>
public enum TopologyCaptionPlacement {
    /// <summary>Places the caption below the body.</summary>
    Bottom,
    /// <summary>Places the caption above the body.</summary>
    Top
}
/// <summary>Selects the surface behind an enabled external caption.</summary>
public enum TopologyCaptionSurface {
    /// <summary>Paints caption text without a separate plate.</summary>
    Plain,
    /// <summary>Paints the measured native caption plate.</summary>
    Plate
}

public sealed partial class TopologyNode {
    /// <summary>Gets or sets the enabled external caption role. Its explicit font size is literal logical units; null retains the legacy role.</summary>
    /// <remarks>Supported for external Icon/Tile and explicitly enabled Artwork captions, excluding Actor shapes and embedded tables. Styling never reveals a hidden or absent caption.</remarks>
    public TextStyleOverride? CaptionTextStyle { get; set; }
    private TopologyCaptionPlacement? _captionPlacement;
    private TopologyCaptionSurface? _captionSurface;
    private double? _glyphScale, _cornerRadius, _captionGap;
    /// <summary>Gets or sets an external caption position. Null preserves each display mode's incumbent behavior.</summary>
    /// <remarks>Explicit Bottom/Top admits an Artwork caption; a surface choice alone does not. Top currently requires Manual or Layered layout.</remarks>
    public TopologyCaptionPlacement? CaptionPlacement {get=>_captionPlacement;set{if(value.HasValue)TopologyModelGuards.EnumDefined(value.Value,nameof(value));_captionPlacement=value;}}
    /// <summary>Gets or sets the surface behind an enabled external caption. Null retains the display mode's ordinary surface.</summary>
    public TopologyCaptionSurface? CaptionSurface {get=>_captionSurface;set{if(value.HasValue)TopologyModelGuards.EnumDefined(value.Value,nameof(value));_captionSurface=value;}}
    /// <summary>Gets or sets the logical gap between the body and enabled external caption. Null retains incumbent mode gaps.</summary>
    /// <remarks>Explicit gaps currently require Manual or Layered layout.</remarks>
    public double? CaptionGap { get => _captionGap; set { if (value.HasValue) TopologyModelGuards.NonNegativeFinite(value.Value, nameof(value)); _captionGap = value; } }
    /// <summary>Gets or sets a uniform scale over the canonical 26-unit glyph coordinate system in Icon/Tile mode.</summary>
    /// <remarks>It scales glyph, text-symbol fallback and captured artwork once before viewport scale. It does not create a glyph or stretch an aspect ratio.</remarks>
    public double? GlyphScale {get=>_glyphScale;set{if(value.HasValue)TopologyModelGuards.PositiveFinite(value.Value,nameof(value));_glyphScale=value;}}
    /// <summary>Gets or sets a model-unit corner radius for Rectangle/Rounded or an unshaped card surface, clamped to half the body.</summary>
    /// <remarks>Null retains incumbent contour, attachment, fitting and paint behavior.</remarks>
    public double? CornerRadius {get=>_cornerRadius;set{if(value.HasValue)TopologyModelGuards.NonNegativeFinite(value.Value,nameof(value));_cornerRadius=value;}}
}
