using System;

namespace ChartForgeX.SvgRaster;

internal static partial class SvgRasterRenderer {
    private static double HorizontalLength(SvgRasterElement element, string name, SvgRasterViewport viewport, double fallback = 0) =>
        ResolveViewportLength(element.Get(name), fallback, viewport.Width);

    private static double VerticalLength(SvgRasterElement element, string name, SvgRasterViewport viewport, double fallback = 0) =>
        ResolveViewportLength(element.Get(name), fallback, viewport.Height);

    private static double DiagonalLength(SvgRasterElement element, string name, SvgRasterViewport viewport, double fallback = 0) {
        var extent = Math.Sqrt(viewport.Width * viewport.Width + viewport.Height * viewport.Height) / Math.Sqrt(2);
        return ResolveViewportLength(element.Get(name), fallback, extent);
    }
}
