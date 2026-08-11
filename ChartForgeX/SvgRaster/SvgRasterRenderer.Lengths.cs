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

    private static void ResolveRoundedRectRadii(SvgRasterElement element, SvgRasterViewport viewport, double width, double height, out double rx, out double ry) {
        var rxValue = element.Get("rx");
        var ryValue = element.Get("ry");
        rx = IsAutomaticLength(rxValue) ? VerticalLength(element, "ry", viewport) : HorizontalLength(element, "rx", viewport);
        ry = IsAutomaticLength(ryValue) ? HorizontalLength(element, "rx", viewport) : VerticalLength(element, "ry", viewport);
        rx = Math.Max(0, Math.Min(rx, width / 2D));
        ry = Math.Max(0, Math.Min(ry, height / 2D));
    }

    private static bool IsAutomaticLength(string? value) => string.IsNullOrWhiteSpace(value) || string.Equals(value!.Trim(), "auto", StringComparison.OrdinalIgnoreCase);
}
