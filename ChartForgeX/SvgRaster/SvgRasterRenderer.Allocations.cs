using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

internal static partial class SvgRasterRenderer {
    /// <summary>Uses the current document context before each ordinary compositor canvas allocation.</summary>
    private static RgbaCanvas AllocateCanvas(int width, int height, SvgRasterDefinitions definitions) {
        definitions.Filters?.ReserveCanvas(width, height);
        return new RgbaCanvas(width, height, 1);
    }
}
