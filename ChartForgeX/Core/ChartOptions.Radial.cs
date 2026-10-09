using System;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    private ChartRadialGeometryOptions _radialGeometry = new();

    /// <summary>Gets or sets immutable geometry for numeric radial bars and columns.</summary>
    public ChartRadialGeometryOptions RadialGeometry {
        get => _radialGeometry;
        set => _radialGeometry = value ?? throw new ArgumentNullException(nameof(value));
    }
}
