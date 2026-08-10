using System;
using System.Text;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void PublicSvgRasterizerPreservesViewportAndDpiMetadata() {
        const string svg = "<svg xmlns='http://www.w3.org/2000/svg' width='120' height='60' viewBox='0 0 120 60'><rect width='120' height='60' fill='#2563eb'/></svg>";
        byte[] png = SvgRasterizer.ToPng(svg, options: new ChartForgeX.Core.RasterImageOptions { Dpi = 144D });
        Assert(png.Length > 100, "Public SVG rasterization should produce a non-empty PNG.");
        Assert(png[0] == 137 && png[1] == 80 && png[2] == 78 && png[3] == 71, "Public SVG rasterization should emit a PNG signature.");
        Assert(System.Text.Encoding.ASCII.GetString(png).Contains("pHYs", StringComparison.Ordinal), "Public SVG rasterization should preserve PNG DPI metadata.");
        AssertThrows<ArgumentOutOfRangeException>(() => SvgRasterizer.ToPng(svg, 0, 60), "Public SVG rasterization should reject invalid output dimensions.");
        var widthOnly = RasterImageDecoder.Decode(SvgRasterizer.ToPng(svg, 240, null));
        var heightOnly = RasterImageDecoder.Decode(SvgRasterizer.ToPng(svg, null, 120));
        Assert(widthOnly.Width == 240 && widthOnly.Height == 120, "A width-only raster override should derive height from the SVG aspect ratio.");
        Assert(heightOnly.Width == 240 && heightOnly.Height == 120, "A height-only raster override should derive width from the SVG aspect ratio.");
        const string partialIntrinsic = "<svg xmlns='http://www.w3.org/2000/svg' width='100' viewBox='0\t0\r\n200 100'><rect width='200' height='100' fill='#2563eb'/></svg>";
        var partialIntrinsicImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(partialIntrinsic));
        var partialIntrinsicOverride = RasterImageDecoder.Decode(SvgRasterizer.ToPng(partialIntrinsic, null, 100));
        Assert(partialIntrinsicImage.Width == 100 && partialIntrinsicImage.Height == 50, "A partial intrinsic viewport should derive its missing axis from the viewBox ratio.");
        Assert(partialIntrinsicOverride.Width == 200 && partialIntrinsicOverride.Height == 100, "A one-axis override should preserve the derived partial intrinsic aspect ratio.");
        const string intrinsicOnly = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40'><rect x='80' y='10' width='10' height='20' fill='#ef4444'/></svg>";
        var intrinsicImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(intrinsicOnly));
        Assert(IsPixelNear(intrinsicImage.Pixels, intrinsicImage.Width, 85, 20, 239, 68, 68), "Public SVG rasterization should use intrinsic dimensions as the coordinate viewport when viewBox is absent.");
        const string rootPresentation = "<svg xmlns='http://www.w3.org/2000/svg' width='30' height='10' fill='#ef4444' style='opacity:1'><style>.accent{fill:#2563eb}</style><rect width='10' height='10'/><g class='accent'><rect x='10' width='10' height='10'/></g><rect x='20' width='10' height='10'/></svg>";
        var rootPresentationImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(rootPresentation));
        Assert(IsPixelNear(rootPresentationImage.Pixels, 30, 5, 5, 239, 68, 68), "Public SVG rasterization should preserve inherited root presentation attributes.");
        Assert(IsPixelNear(rootPresentationImage.Pixels, 30, 15, 5, 37, 99, 235), "Public SVG rasterization should preserve root-contained CSS class rules.");
        const string rootSelectorSemantics = "<svg xmlns='http://www.w3.org/2000/svg' width='20' height='10' fill='#ef4444'><style>g rect{fill:#2563eb} svg rect.accent{fill:#16a34a}</style><rect width='10' height='10'/><rect class='accent' x='10' width='10' height='10'/></svg>";
        var rootSelectorImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(rootSelectorSemantics));
        Assert(IsPixelNear(rootSelectorImage.Pixels, 20, 5, 5, 239, 68, 68), "Public SVG rasterization should not introduce a selector-visible group around root children.");
        Assert(IsPixelNear(rootSelectorImage.Pixels, 20, 15, 5, 22, 163, 74), "Public SVG rasterization should retain the source svg root in descendant selector matching.");
        var transparentImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng("<svg xmlns='http://www.w3.org/2000/svg' width='20' height='10'></svg>"));
        Assert(transparentImage.Width == 20 && transparentImage.Height == 10 && transparentImage.Pixels.All(value => value == 0), "Public SVG rasterization should accept a valid fully transparent document.");
        const string offsetViewBox = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='50' viewBox='10 20 200 100' preserveAspectRatio='none' fill='#16a34a'><rect x='10' y='20' width='200' height='100'/></svg>";
        var offsetViewBoxImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(offsetViewBox));
        Assert(IsPixelNear(offsetViewBoxImage.Pixels, 100, 50, 25, 22, 163, 74), "Public SVG rasterization should apply a non-zero source viewBox exactly once while preserving root presentation.");
        const string svgWithDtd = "<!DOCTYPE svg [<!ENTITY external SYSTEM 'file:///not-allowed'>]><svg xmlns='http://www.w3.org/2000/svg' width='10' height='10'><text>&external;</text></svg>";
        AssertThrows<FormatException>(() => SvgRasterizer.ToPng(svgWithDtd), "Public SVG rasterization should reject DTD and external-entity input.");
    }

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

        var overlappingGroup = "<g opacity='.5'><rect width='70' height='40' fill='#ff0000'/><rect x='30' width='70' height='40' fill='#ff0000'/></g>";
        Assert(SvgRasterRenderer.TryRenderFragment(overlappingGroup, "0 0 100 40", "none", 100, 40, out var grouped), "SVG rasterization should composite an opaque group before applying its opacity.");
        Assert(PixelAlpha(grouped, 100, 15, 20) is >= 126 and <= 129 && PixelAlpha(grouped, 100, 50, 20) is >= 126 and <= 129, "Group opacity should be applied once to the combined layer, including overlapping children.");

        const string rootOpacity = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40' opacity='.5'><rect width='70' height='40' fill='#ff0000'/><rect x='30' width='70' height='40' fill='#ff0000'/></svg>";
        var rootOpacityImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(rootOpacity));
        Assert(PixelAlpha(rootOpacityImage.Pixels, 100, 15, 20) is >= 126 and <= 129 && PixelAlpha(rootOpacityImage.Pixels, 100, 50, 20) is >= 126 and <= 129, "Root opacity should be applied once after compositing all root children.");

        const string rootClip = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40' clip-path='url(#left-half)'><defs><clipPath id='left-half'><rect width='50' height='40'/></clipPath></defs><rect width='100' height='40' fill='#ff0000'/></svg>";
        var rootClipImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(rootClip));
        Assert(IsPixelNear(rootClipImage.Pixels, 100, 25, 20, 255, 0, 0) && PixelAlpha(rootClipImage.Pixels, 100, 75, 20) == 0, "Root clip paths should composite all document children before clipping the raster output.");

        const string rootMask = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40' mask='url(#left-half)'><defs><mask id='left-half'><rect width='50' height='40' fill='#ffffff'/></mask></defs><rect width='100' height='40' fill='#2563eb'/></svg>";
        var rootMaskImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(rootMask));
        Assert(IsPixelNear(rootMaskImage.Pixels, 100, 25, 20, 37, 99, 235) && PixelAlpha(rootMaskImage.Pixels, 100, 75, 20) == 0, "Root masks should composite all document children before masking the raster output.");

        const string physicalViewport = "<svg xmlns='http://www.w3.org/2000/svg' width='2in' height='1in'><rect width='192' height='96' fill='#2563eb'/></svg>";
        var physicalViewportImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(physicalViewport));
        Assert(physicalViewportImage.Width == 192 && physicalViewportImage.Height == 96, "Public SVG rasterization should resolve absolute physical viewport units at 96 CSS pixels per inch.");

        var nestedText = "<text x='4' y='28' font-size='28' fill='#ff0000' opacity='.5'><tspan>OO</tspan></text>";
        Assert(SvgRasterRenderer.TryRenderFragment(nestedText, "0 0 100 40", "none", 100, 40, out var nestedTextPixels), "SVG rasterization should render tspan text inside an opacity layer.");
        Assert(MaximumAlpha(nestedTextPixels) is >= 126 and <= 129, "Container text opacity should apply to tspan glyphs exactly once.");
    }

    private static void SvgRasterStrokeJoinsHonorRoundBevelAndMiter() {
        Assert(TryRenderStrokeJoin("bevel", out var bevel), "SVG rasterization should render bevel stroke joins.");
        Assert(TryRenderStrokeJoin("round", out var round), "SVG rasterization should render round stroke joins.");
        Assert(TryRenderStrokeJoin("miter", out var miter), "SVG rasterization should render miter stroke joins.");

        Assert(PixelAlpha(bevel, 100, 50, 13) == 0, "Bevel joins should cut off the outer corner at the two offset endpoints.");
        Assert(PixelAlpha(round, 100, 50, 13) > 0, "Round joins should cover the curved outer corner beyond a bevel join.");
        Assert(PixelAlpha(miter, 100, 50, 7) > 0, "Miter joins should extend to the intersection of the outer stroke edges.");
        Assert(PixelAlpha(round, 100, 50, 7) == 0 && PixelAlpha(bevel, 100, 50, 7) == 0, "Round and bevel joins should not inherit the miter tip.");

        Assert(TryRenderClosedStrokeJoin("bevel", out var closedBevel), "SVG rasterization should render closed bevel stroke joins.");
        Assert(TryRenderClosedStrokeJoin("miter", out var closedMiter), "SVG rasterization should render closed miter stroke joins.");
        Assert(PixelAlpha(closedBevel, 100, 24, 24) == 0, "Closed bevel strokes should cut off the wraparound corner.");
        Assert(PixelAlpha(closedMiter, 100, 24, 24) > 0, "Closed miter strokes should join the duplicated first and last vertex.");

        Assert(TryRenderStrokeJoin("miter", out var limitedMiter, "1"), "SVG rasterization should parse explicit stroke miter limits.");
        Assert(PixelAlpha(limitedMiter, 100, 50, 7) == 0, "A low SVG miter limit should fall back to a bevel join.");
    }

    private static bool TryRenderStrokeJoin(string lineJoin, out byte[] pixels, string? miterLimit = null) {
        var miterLimitAttribute = miterLimit == null ? string.Empty : " stroke-miterlimit='" + miterLimit + "'";
        var markup = "<path d='M20 80 L50 20 L80 80' fill='none' stroke='#ff0000' stroke-width='16' stroke-linecap='butt' stroke-linejoin='" + lineJoin + "'" + miterLimitAttribute + "/>";
        return SvgRasterRenderer.TryRenderFragment(markup, "0 0 100 100", "none", 100, 100, out pixels);
    }

    private static bool TryRenderClosedStrokeJoin(string lineJoin, out byte[] pixels) {
        var markup = "<path d='M30 30 L70 30 L70 70 L30 70 Z' fill='none' stroke='#ff0000' stroke-width='16' stroke-linecap='butt' stroke-linejoin='" + lineJoin + "'/>";
        return SvgRasterRenderer.TryRenderFragment(markup, "0 0 100 100", "none", 100, 100, out pixels);
    }

    private static string SvgData(string markup) => "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(markup));

    private static byte PixelAlpha(byte[] rgba, int width, int x, int y) => rgba[(y * width + x) * 4 + 3];

    private static byte MaximumAlpha(byte[] rgba) {
        byte maximum = 0;
        for (var index = 3; index < rgba.Length; index += 4) maximum = Math.Max(maximum, rgba[index]);
        return maximum;
    }

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
