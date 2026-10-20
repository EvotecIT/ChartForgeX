namespace ChartForgeX.Rendering;

/// <summary>Captures the native axes emitted together by a Cartesian layout.</summary>
internal sealed class ChartCartesianProjection {
    internal ChartCartesianProjection(ChartXProjection x, ChartYProjection? y) { X = x; Y = y; }
    internal ChartXProjection X { get; }
    internal ChartYProjection? Y { get; }
}
