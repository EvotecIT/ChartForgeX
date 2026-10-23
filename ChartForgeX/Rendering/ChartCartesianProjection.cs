namespace ChartForgeX.Rendering;

/// <summary>Captures the physical native axes emitted together by one Cartesian layout.</summary>
internal sealed class ChartCartesianProjection {
    internal ChartCartesianProjection(ChartAxisProjection x, ChartAxisProjection y,
        ChartAxisProjection? secondaryX = null, ChartAxisProjection? secondaryY = null, bool horizontal = false) {
        X = x; Y = y; SecondaryX = secondaryX; SecondaryY = secondaryY; Horizontal = horizontal;
    }
    internal bool Horizontal { get; }
    internal ChartAxisProjection X { get; }
    internal ChartAxisProjection Y { get; }
    internal ChartAxisProjection? SecondaryX { get; }
    internal ChartAxisProjection? SecondaryY { get; }
}
