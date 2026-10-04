using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal sealed partial class RgbaCanvas {
    /// <summary>Paints one integral output cell without rounding logical fractional boundaries.</summary>
    internal void FillOutputPixel(double x, double y, ChartColor color) {
        // Authored bitmap cells are already on the output grid. Preserve the same cell at every
        // antialiasing level, composing its coverage exactly once into each device sample.
        if (!(x >= 0 && y >= 0 && x < OutputWidth && y < OutputHeight)) return;
        var left = (int)x * _supersamplingScale;
        var top = (int)y * _supersamplingScale;
        for (var row = top; row < top + _supersamplingScale; row++)
            for (var column = left; column < left + _supersamplingScale; column++) BlendPixel(column, row, color);
    }
}
