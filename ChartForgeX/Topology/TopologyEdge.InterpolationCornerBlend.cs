namespace ChartForgeX.Topology;

public sealed partial class TopologyEdge {
    private double? _interpolationCornerBlendLength;
    /// <summary>Gets or sets the logical leg distance used to soften orthogonal interpolation control vertices.</summary>
    /// <remarks>Null retains the standard interpolation input; zero is identity. A value requires non-null Interpolation
    /// and cannot combine with authored PathGeometry. The native route remains authoritative and editable.</remarks>
    public double? InterpolationCornerBlendLength {
        get => _interpolationCornerBlendLength;
        set { if (value.HasValue) TopologyModelGuards.NonNegativeFinite(value.Value, nameof(value)); _interpolationCornerBlendLength = value; }
    }
}
