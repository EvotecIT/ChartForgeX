using System.Collections.Generic;

namespace ChartForgeX.VisualArtifacts;

public sealed partial class VisualArtifactInterchangeTopologyNode {
    /// <summary>Gets or sets the separate own-contour stroke color. Non-null requires version2.</summary>
    public string? BorderColor { get; set; }
    /// <summary>Gets or sets the captured contour width. Non-null requires version2.</summary>
    public double? StrokeWidth { get;set; }
    /// <summary>Gets or sets completed own-object ink opacity. Non-null requires version2.</summary>
    public double? Opacity { get;set; }
    /// <summary>Gets or sets separate per-contour-element opacity. Non-null requires version2.</summary>
    public double? SurfaceOpacity { get;set; }
    /// <summary>Gets captured alternating contour dash/gap pairs. Nonempty requires version2.</summary>
    public List<double> DashPattern { get; }=new();
}

public sealed partial class VisualArtifactInterchangeTopologyGroup {
    /// <summary>Gets or sets the captured boundary width. Non-null requires version2.</summary>
    public double? StrokeWidth { get;set; }
    /// <summary>Gets or sets completed own-object ink opacity, excluding descendants. Non-null requires version2.</summary>
    public double? Opacity { get;set; }
    /// <summary>Gets or sets separate per-boundary-element opacity. Non-null requires version2.</summary>
    public double? SurfaceOpacity { get;set; }
    /// <summary>Gets captured alternating boundary dash/gap pairs. Nonempty requires version2.</summary>
    public List<double> DashPattern { get; }=new();
}
