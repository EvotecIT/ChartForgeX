using System.Collections.Generic;

namespace ChartForgeX.Topology;

public sealed partial class TopologyNode {
    /// <summary>Gets or sets the color of this node's own contour strokes. Null retains the incumbent Color accent policy.</summary>
    /// <remarks>Glyphs, accent fills, captions, badges, selection overlays and descendants keep their independent paint owners.</remarks>
    public string? BorderColor { get; set; }
    private double? _strokeWidth,_opacity,_surfaceOpacity;
    /// <summary>Gets or sets the contour stroke width in model units; null inherits native theme and selection width.</summary>
    public double? StrokeWidth { get=>_strokeWidth;set{if(value.HasValue)TopologyModelGuards.PositiveFinite(value.Value,nameof(value));_strokeWidth=value;} }
    /// <summary>Gets or sets opacity of this node's completed own ink, including captions, glyphs, artwork and details.</summary>
    /// <remarks>Composites once after overlapping own marks are painted. It does not change logical attachment or hit geometry.</remarks>
    public double? Opacity { get=>_opacity;set{TopologySurfacePaintRules.Opacity(value,nameof(value));_opacity=value;} }
    /// <summary>Gets or sets opacity of each contour surface element, compositing its fill and stroke once; captions, glyphs and details are excluded.</summary>
    public double? SurfaceOpacity { get=>_surfaceOpacity;set{TopologySurfacePaintRules.Opacity(value,nameof(value));InvalidateSurfaceElementAppearance();_surfaceOpacity=value;} }
    /// <summary>Gets alternating contour dash and gap lengths in model units; an empty list retains the ordinary solid outline.</summary>
    public List<double> DashPattern { get; }=new();
}
