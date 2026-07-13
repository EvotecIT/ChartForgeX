using System;
using System.Text;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SvgRasterDocumentsUseIntrinsicDimensionsAndCssImageClipping() {
        var document = SvgRasterParser.ParseDocument("<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40'><rect width='100' height='40'/></svg>");
        Assert(Math.Abs(document.ViewBox.Width - 100) < 0.001 && Math.Abs(document.ViewBox.Height - 40) < 0.001, "SVG documents without a viewBox should derive their raster viewport from intrinsic width and height.");

        var positionedSource = SvgData("<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100'><rect x='70' y='35' width='20' height='30' fill='#ff0000'/></svg>");
        Assert(SvgRasterRenderer.TryRenderFragment("<image x='0' y='0' width='100' height='100' href='" + positionedSource + "'/>", "0 0 100 100", "none", 100, 100, out var positioned), "SVG rasterization should decode embedded documents that use intrinsic dimensions.");
        Assert(IsPixelNear(positioned, 100, 80, 50, 255, 0, 0), "Embedded SVG artwork should retain geometry outside the legacy 24 by 24 fallback viewport.");

        var solidSource = SvgData("<svg xmlns='http://www.w3.org/2000/svg' width='20' height='20'><rect width='20' height='20' fill='#ff0000'/></svg>");
        var clippedMarkup = "<style>.round image{clip-path:circle(50%)}.round .rect{clip-path:none}</style><g class='round'><image x='0' y='0' width='100' height='100' href='" + solidSource + "'/></g>";
        Assert(SvgRasterRenderer.TryRenderFragment(clippedMarkup, "0 0 100 100", "none", 100, 100, out var clipped), "SVG rasterization should support centered CSS circle clipping on image elements.");
        Assert(PixelAlpha(clipped, 100, 2, 2) == 0 && IsPixelNear(clipped, 100, 50, 50, 255, 0, 0), "Circular image clipping should remove square corners while preserving the image center.");

        var rectangularMarkup = "<style>.round image{clip-path:circle(50%)}.round .rect{clip-path:none}</style><g class='round'><image class='rect' x='0' y='0' width='100' height='100' href='" + solidSource + "'/></g>";
        Assert(SvgRasterRenderer.TryRenderFragment(rectangularMarkup, "0 0 100 100", "none", 100, 100, out var rectangular), "SVG rasterization should allow a more specific rule to disable image clipping.");
        Assert(IsPixelNear(rectangular, 100, 2, 2, 255, 0, 0), "Rectangular graph images should retain their corners when circular clipping is disabled.");

        var wideSource = SvgData("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 200 100'><rect width='200' height='100' fill='#ff0000'/></svg>");
        Assert(SvgRasterRenderer.TryRenderFragment("<image x='0' y='0' width='200' height='100' href='" + wideSource + "'/>", "0 0 200 100", "none", 200, 100, out var wide), "SVG rasterization should render embedded artwork against its destination aspect ratio.");
        Assert(IsPixelNear(wide, 200, 100, 5, 255, 0, 0), "Rectangular embedded SVG artwork should not acquire square intermediate letterboxing before destination scaling.");

        var markerMarkup = "<defs><marker id='arrow' viewBox='0 0 10 10' refX='9' refY='5' markerWidth='8' markerHeight='8' orient='auto'><path d='M0 0 L10 5 L0 10 z' fill='#ff0000'/></marker></defs><path d='M10 50 L90 50' fill='none' stroke='#111111' stroke-width='2' marker-end='url(#arrow)'/>";
        Assert(SvgRasterRenderer.TryRenderFragment(markerMarkup, "0 0 100 100", "none", 100, 100, out var marked), "SVG rasterization should render referenced path markers.");
        Assert(CountPixelsNear(marked, 100, 74, 35, 96, 65, 255, 0, 0) > 20, "Directed SVG markers should remain visible in dependency-free PNG output.");
    }

    private static void SvgRasterImagesPreserveAspectRatioAndIgnoreMediaRules() {
        var pixels = new byte[200 * 100 * 4];
        for (var index = 0; index < pixels.Length; index += 4) { pixels[index] = 255; pixels[index + 3] = 255; }
        var imageSource = "data:image/png;base64," + Convert.ToBase64String(PngWriter.WriteRgba(new RgbaImage(200, 100, pixels)));
        Assert(SvgRasterRenderer.TryRenderFragment("<image x='0' y='0' width='100' height='100' href='" + imageSource + "'/>", "0 0 100 100", "none", 100, 100, out var contained), "SVG rasterization should decode self-contained raster images.");
        Assert(PixelAlpha(contained, 100, 50, 5) == 0 && IsPixelNear(contained, 100, 50, 50, 255, 0, 0), "Default image preserveAspectRatio should contain wide artwork without stretching it into a square.");

        var mediaMarkup = "<style>.target{fill:#ff0000;opacity:.25}@media (prefers-contrast: more){.target{opacity:1}.ignored{fill:#00ff00}}.after{fill:#0000ff}</style><rect class='target' width='40' height='40'/><rect class='after' x='60' width='40' height='40'/>";
        Assert(SvgRasterRenderer.TryRenderFragment(mediaMarkup, "0 0 100 40", "none", 100, 40, out var styled), "SVG rasterization should parse base rules around unsupported media queries.");
        Assert(PixelAlpha(styled, 100, 20, 20) is >= 62 and <= 66 && IsPixelNear(styled, 100, 80, 20, 0, 0, 255), "Unsupported media blocks should be skipped as a whole without leaking nested contrast rules or hiding following base rules.");
    }

    private static string SvgData(string markup) => "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(markup));

    private static byte PixelAlpha(byte[] rgba, int width, int x, int y) => rgba[(y * width + x) * 4 + 3];

    private static bool IsPixelNear(byte[] rgba, int width, int x, int y, byte red, byte green, byte blue) {
        var index = (y * width + x) * 4;
        return Math.Abs(rgba[index] - red) <= 4 && Math.Abs(rgba[index + 1] - green) <= 4 && Math.Abs(rgba[index + 2] - blue) <= 4 && rgba[index + 3] >= 250;
    }

    private static int CountPixelsNear(byte[] rgba, int width, int left, int top, int right, int bottom, byte red, byte green, byte blue) {
        var count = 0;
        for (var y = top; y <= bottom; y++) for (var x = left; x <= right; x++) if (IsPixelNear(rgba, width, x, y, red, green, blue)) count++;
        return count;
    }
}
