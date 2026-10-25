namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    private double _polarRotationDegrees;

    /// <summary>Gets or sets the clockwise rotation of dedicated Radar, Polar and PolarArea marks, guides and captions in degrees. Zero preserves their existing starting rays.</summary>
    /// <remarks>Any finite rotation is accepted and reduced modulo one turn only for projection; source radians and categories stay unchanged.
    /// Mixed numeric radial charts keep their separate RadialGeometry start/end-angle policy and reject a nonzero value here.</remarks>
    public double PolarRotationDegrees {
        get => _polarRotationDegrees;
        set { ChartGuards.Finite(value, nameof(value)); _polarRotationDegrees = value; }
    }
}
