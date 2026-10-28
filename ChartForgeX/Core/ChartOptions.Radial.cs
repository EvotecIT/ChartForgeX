using System;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    private ChartRadialGeometryOptions _radialGeometry = new();
    /// <summary>Gets or sets opt-in automatic Pie/Donut bands. Null preserves existing single-series and explicit-band behavior.</summary>
    public ChartRadialBandAllocation? RadialBandAllocation { get; set; }

    /// <summary>Gets or sets immutable geometry for numeric radial bars and columns, including column/radar compositions.</summary>
    /// <remarks>Area value encoding is limited to homogeneous nonnegative linear RadialColumn series; the default Radius encoding retains existing family behavior.</remarks>
    public ChartRadialGeometryOptions RadialGeometry {
        get => _radialGeometry;
        set => _radialGeometry = value ?? throw new ArgumentNullException(nameof(value));
    }
}
