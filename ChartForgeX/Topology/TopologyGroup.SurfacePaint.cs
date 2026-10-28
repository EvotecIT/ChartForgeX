using System.Collections.Generic;

namespace ChartForgeX.Topology;

public sealed partial class TopologyGroup {
    private double? _strokeWidth,_opacity,_surfaceOpacity;
    /// <summary>Gets or sets the container boundary width in model units; null inherits native theme and selection width.</summary>
    public double? StrokeWidth { get=>_strokeWidth;set{if(value.HasValue)TopologyModelGuards.PositiveFinite(value.Value,nameof(value));_strokeWidth=value;} }
    /// <summary>Gets or sets opacity of this container's completed own ink, excluding descendant nodes, groups and relationships.</summary>
    /// <remarks>Nondefault surface paint overrides require a standard Container outside Geographic layout.</remarks>
    public double? Opacity { get=>_opacity;set{TopologySurfacePaintRules.Opacity(value,nameof(value));_opacity=value;} }
    /// <summary>Gets or sets per-boundary-element fill and stroke opacity, excluding caption, glyph and descendant ink.</summary>
    public double? SurfaceOpacity { get=>_surfaceOpacity;set{TopologySurfacePaintRules.Opacity(value,nameof(value));_surfaceOpacity=value;} }
    /// <summary>Gets alternating boundary dash and gap lengths in model units; an empty list retains the ordinary solid outline.</summary>
    public List<double> DashPattern { get; }=new();
}
