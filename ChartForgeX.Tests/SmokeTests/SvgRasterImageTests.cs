using System;
using System.Text;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SvgRasterTextPreservesTypographyStyles() {
        const string regularSvg = "<svg xmlns='http://www.w3.org/2000/svg' width='240' height='60'><text x='8' y='42' font-size='32' fill='#ef4444'>MMMMiiii</text></svg>";
        const string italicSvg = "<svg xmlns='http://www.w3.org/2000/svg' width='240' height='60'><text x='8' y='42' font-size='32' font-style='italic' fill='#ef4444'>MMMMiiii</text></svg>";
        const string underlinedSvg = "<svg xmlns='http://www.w3.org/2000/svg' width='240' height='60'><text x='8' y='42' font-size='32' text-decoration='underline' fill='#ef4444'>MMMMiiii</text></svg>";
        const string numericBoldSvg = "<svg xmlns='http://www.w3.org/2000/svg' width='240' height='60'><text x='8' y='42' font-size='32' font-weight='750' fill='#ef4444'>MMMMiiii</text></svg>";
        var regular = RasterImageDecoder.Decode(SvgRasterizer.ToPng(regularSvg));
        var italic = RasterImageDecoder.Decode(SvgRasterizer.ToPng(italicSvg));
        var underlined = RasterImageDecoder.Decode(SvgRasterizer.ToPng(underlinedSvg));
        var numericBold = RasterImageDecoder.Decode(SvgRasterizer.ToPng(numericBoldSvg));
        var regularBounds = SvgColorBounds(regular.Pixels, regular.Width, regular.Height, 239, 68, 68);
        var italicBounds = SvgColorBounds(italic.Pixels, italic.Width, italic.Height, 239, 68, 68);
        var underlinedBounds = SvgColorBounds(underlined.Pixels, underlined.Width, underlined.Height, 239, 68, 68);

        Assert(italicBounds.Width > regularBounds.Width, "SVG rasterization should preserve italic or oblique glyph overhang and measurement.");
        Assert(underlinedBounds.Bottom > regularBounds.Bottom, "SVG rasterization should preserve underlined text decoration in the raster artifact.");
        Assert(CountPixelsNear(numericBold.Pixels, numericBold.Width, 0, 0, numericBold.Width - 1, numericBold.Height - 1, 239, 68, 68) > CountPixelsNear(regular.Pixels, regular.Width, 0, 0, regular.Width - 1, regular.Height - 1, 239, 68, 68), "SVG rasterization should treat numeric font weights of 600 or greater as emphasized text.");

        var serifFont = TrueTypeFont.TryLoadForFamily("serif", out _);
        var monospaceFont = TrueTypeFont.TryLoadForFamily("monospace", out _);
        if (serifFont != null && monospaceFont != null && !string.Equals(serifFont.DisplayName, monospaceFont.DisplayName, StringComparison.OrdinalIgnoreCase)) {
            const string styledFamilySvg = "<svg xmlns='http://www.w3.org/2000/svg' width='240' height='60'><style>.styled{font-family:monospace;font-style:oblique;text-decoration:underline}</style><g class='styled'><text x='8' y='42' font-size='32' fill='#2563eb'>MMMMiiii</text></g></svg>";
            var styledFamily = RasterImageDecoder.Decode(SvgRasterizer.ToPng(styledFamilySvg));
            var styledBounds = SvgColorBounds(styledFamily.Pixels, styledFamily.Width, styledFamily.Height, 37, 99, 235);
            Assert(styledBounds.HasPixels && styledBounds.Bottom > regularBounds.Bottom && styledBounds.Width != italicBounds.Width, "SVG rasterization should resolve CSS font family, oblique style, and underline together.");
        }
    }

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
        var dimensionlessImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng("<svg xmlns='http://www.w3.org/2000/svg'><rect width='300' height='150' fill='#2563eb'/></svg>"));
        var widthOnlyIntrinsic = RasterImageDecoder.Decode(SvgRasterizer.ToPng("<svg xmlns='http://www.w3.org/2000/svg' width='100'><rect width='100' height='150' fill='#2563eb'/></svg>"));
        var heightOnlyIntrinsic = RasterImageDecoder.Decode(SvgRasterizer.ToPng("<svg xmlns='http://www.w3.org/2000/svg' height='40'><rect width='300' height='40' fill='#2563eb'/></svg>"));
        Assert(dimensionlessImage.Width == 300 && dimensionlessImage.Height == 150, "A dimensionless SVG should use the standard 300 by 150 intrinsic viewport.");
        Assert(widthOnlyIntrinsic.Width == 100 && widthOnlyIntrinsic.Height == 150 && heightOnlyIntrinsic.Width == 300 && heightOnlyIntrinsic.Height == 40, "A missing intrinsic axis without a viewBox should use that axis's standard SVG default.");
        var squareViewBoxOnly = RasterImageDecoder.Decode(SvgRasterizer.ToPng("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100'><rect width='100' height='100' fill='#2563eb'/></svg>"));
        var wideViewBoxOnly = RasterImageDecoder.Decode(SvgRasterizer.ToPng("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 200 100'><rect width='200' height='100' fill='#2563eb'/></svg>"));
        var portraitViewBoxOnly = RasterImageDecoder.Decode(SvgRasterizer.ToPng("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 200'><rect width='100' height='200' fill='#2563eb'/></svg>"));
        Assert(squareViewBoxOnly.Width == 150 && squareViewBoxOnly.Height == 150 && wideViewBoxOnly.Width == 300 && wideViewBoxOnly.Height == 150 && portraitViewBoxOnly.Width == 75 && portraitViewBoxOnly.Height == 150, "A viewBox-only SVG should fit its intrinsic ratio inside the standard 300 by 150 default object size.");
        var subpixelIntrinsic = RasterImageDecoder.Decode(SvgRasterizer.ToPng("<svg xmlns='http://www.w3.org/2000/svg' width='0.4' height='0.4'><rect width='0.4' height='0.4' fill='#2563eb'/></svg>"));
        var thinViewBoxOnly = RasterImageDecoder.Decode(SvgRasterizer.ToPng("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 1000 1'><rect width='1000' height='1' fill='#2563eb'/></svg>"));
        Assert(subpixelIntrinsic.Width == 1 && subpixelIntrinsic.Height == 1 && thinViewBoxOnly.Width == 300 && thinViewBoxOnly.Height == 1, "Positive subpixel intrinsic and fitted viewBox dimensions should clamp to one output pixel.");
        AssertThrows<ArgumentOutOfRangeException>(() => SvgRasterizer.ToPng("<svg xmlns='http://www.w3.org/2000/svg' width='0' height='40'><rect width='100' height='40'/></svg>"), "Explicit zero root SVG viewport dimensions should not fall back to the standard intrinsic size.");
        AssertThrows<ArgumentOutOfRangeException>(() => SvgRasterizer.ToPng("<svg xmlns='http://www.w3.org/2000/svg' width='100' height='-1px'><rect width='100' height='40'/></svg>"), "Explicit negative root SVG viewport dimensions should be rejected before raster allocation.");
        AssertThrows<ArgumentOutOfRangeException>(() => SvgRasterizer.ToPng("<svg xmlns='http://www.w3.org/2000/svg' width='1e100' height='1'><rect width='1' height='1'/></svg>"), "Intrinsic SVG dimensions should be range-checked before integer conversion or raster allocation.");
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
        const string inheritedVisibility = "<svg xmlns='http://www.w3.org/2000/svg' width='40' height='10' visibility='hidden'><rect width='10' height='10' fill='#ef4444'/><g visibility='visible'><rect x='10' width='10' height='10' fill='#2563eb'/></g><g display='none'><rect x='20' width='10' height='10' visibility='visible' fill='#ef4444'/></g><rect x='30' width='10' height='10' style='display:none;display:inline;visibility:visible' fill='#16a34a'/></svg>";
        var inheritedVisibilityImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(inheritedVisibility));
        Assert(PixelAlpha(inheritedVisibilityImage.Pixels, 40, 5, 5) == 0, "Inherited hidden visibility should suppress elements that do not override it.");
        Assert(IsPixelNear(inheritedVisibilityImage.Pixels, 40, 15, 5, 37, 99, 235), "Descendants should be able to restore visible SVG visibility.");
        Assert(PixelAlpha(inheritedVisibilityImage.Pixels, 40, 25, 5) == 0, "Visibility overrides should not escape an ancestor with display none.");
        Assert(IsPixelNear(inheritedVisibilityImage.Pixels, 40, 35, 5, 22, 163, 74), "Later display and visibility declarations should win on the same visible element.");
        var transparentImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng("<svg xmlns='http://www.w3.org/2000/svg' width='20' height='10'></svg>"));
        Assert(transparentImage.Width == 20 && transparentImage.Height == 10 && transparentImage.Pixels.All(value => value == 0), "Public SVG rasterization should accept a valid fully transparent document.");
        const string offsetViewBox = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='50' viewBox='10 20 200 100' preserveAspectRatio='none' fill='#16a34a'><rect x='10' y='20' width='200' height='100'/></svg>";
        var offsetViewBoxImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(offsetViewBox));
        Assert(IsPixelNear(offsetViewBoxImage.Pixels, 100, 50, 25, 22, 163, 74), "Public SVG rasterization should apply a non-zero source viewBox exactly once while preserving root presentation.");
        const string svgWithDtd = "<!DOCTYPE svg [<!ENTITY external SYSTEM 'file:///not-allowed'>]><svg xmlns='http://www.w3.org/2000/svg' width='10' height='10'><text>&external;</text></svg>";
        AssertThrows<FormatException>(() => SvgRasterizer.ToPng(svgWithDtd), "Public SVG rasterization should reject DTD and external-entity input.");
        var deeplyNestedSvg = new StringBuilder("<svg xmlns='http://www.w3.org/2000/svg' width='10' height='10'>");
        for (var depth = 0; depth < SvgRasterParser.MaximumElementDepth; depth++) deeplyNestedSvg.Append("<g>");
        deeplyNestedSvg.Append("<rect width='10' height='10'/>");
        for (var depth = 0; depth < SvgRasterParser.MaximumElementDepth; depth++) deeplyNestedSvg.Append("</g>");
        deeplyNestedSvg.Append("</svg>");
        AssertThrows<FormatException>(() => SvgRasterizer.ToPng(deeplyNestedSvg.ToString()), "Public SVG rasterization should reject excessive element depth before recursive parsing or rendering.");
        const string excessiveTextStroke = "<svg xmlns='http://www.w3.org/2000/svg' width='20' height='20'><text x='2' y='16' font-size='12' fill='none' stroke='#ef4444' stroke-width='1e300'>A</text></svg>";
        AssertThrows<NotSupportedException>(() => SvgRasterizer.ToPng(excessiveTextStroke), "Public SVG rasterization should reject text paint that cannot fit the bounded intermediate allocation.");
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

        var asymmetricPixels = new byte[] { 255, 0, 0, 255, 0, 0, 255, 255 };
        var asymmetricSource = "data:image/png;base64," + Convert.ToBase64String(PngWriter.WriteRgba(new RgbaImage(2, 1, asymmetricPixels)));
        var rotatedMarkup = "<image x='20' y='20' width='40' height='20' preserveAspectRatio='none' transform='rotate(90 40 30)' href='" + asymmetricSource + "'/>";
        Assert(SvgRasterRenderer.TryRenderFragment(rotatedMarkup, "0 0 60 60", "none", 60, 60, out var rotated), "SVG rasterization should apply image rotation while sampling source pixels.");
        Assert(IsPixelNear(rotated, 60, 40, 15, 255, 0, 0) && IsPixelNear(rotated, 60, 40, 45, 0, 0, 255), "Rotated image pixels should follow the transformed image axes instead of stretching into their axis-aligned bounds.");

        var rotatedCircleSvg = "<svg xmlns='http://www.w3.org/2000/svg' width='60' height='60'><image x='20' y='20' width='40' height='20' preserveAspectRatio='none' transform='rotate(90 40 30)' style='clip-path:circle(50%)' href='" + asymmetricSource + "'/></svg>";
        var rotatedCircle = RasterImageDecoder.Decode(SvgRasterizer.ToPng(rotatedCircleSvg));
        var rotatedCircleRed = (25 * 60 + 40) * 4;
        var rotatedCircleBlue = (35 * 60 + 40) * 4;
        Assert(PixelAlpha(rotatedCircle.Pixels, 60, 40, 15) == 0 && rotatedCircle.Pixels[rotatedCircleRed] > rotatedCircle.Pixels[rotatedCircleRed + 2] && rotatedCircle.Pixels[rotatedCircleBlue + 2] > rotatedCircle.Pixels[rotatedCircleBlue], "Circular image clipping should stay in local image space while the clipped pixels follow the public document transform path.");

        var skewedMarkup = "<image x='10' y='10' width='20' height='20' preserveAspectRatio='none' transform='skewX(45)' href='" + solidSource + "'/>";
        Assert(SvgRasterRenderer.TryRenderFragment(skewedMarkup, "0 0 70 40", "none", 70, 40, out var skewed), "SVG rasterization should apply image skew while sampling source pixels.");
        Assert(PixelAlpha(skewed, 70, 22, 29) == 0 && IsPixelNear(skewed, 70, 50, 29, 255, 0, 0), "Skewed images should retain their parallelogram footprint instead of filling the transformed axis-aligned bounds.");

        var thinTransformedMarkup = "<svg xmlns='http://www.w3.org/2000/svg' width='10000' height='1' viewBox='0 0 10000 1' preserveAspectRatio='none'><image width='1' height='1' transform='scale(10000)' style='clip-path:circle(50%)' href='" + solidSource + "'/></svg>";
        var thinTransformed = RasterImageDecoder.Decode(SvgRasterizer.ToPng(thinTransformedMarkup));
        Assert(thinTransformed.Width == 10000 && thinTransformed.Height == 1, "Transformed image intermediates should remain bounded by the visible output budget, including local circle clipping.");

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

        const string paintedPrimitive = "<rect x='10' y='5' width='60' height='30' fill='#ff0000' stroke='#0000ff' stroke-width='10' opacity='.5'/>";
        Assert(SvgRasterRenderer.TryRenderFragment(paintedPrimitive, "0 0 80 40", "none", 80, 40, out var paintedPrimitivePixels), "SVG rasterization should render a partially opaque primitive with fill and stroke paint.");
        Assert(PixelAlpha(paintedPrimitivePixels, 80, 12, 20) is >= 126 and <= 129 && PixelAlpha(paintedPrimitivePixels, 80, 40, 20) is >= 126 and <= 129, "Primitive opacity should composite overlapping fill and stroke paint before applying alpha once.");
        const string gradientOpacity = "<defs><linearGradient id='solid-gradient'><stop stop-color='#ef4444'/><stop offset='1' stop-color='#ef4444'/></linearGradient></defs><rect width='40' height='20' fill='url(#solid-gradient)' opacity='.5'/>";
        Assert(SvgRasterRenderer.TryRenderFragment(gradientOpacity, "0 0 40 20", "none", 40, 20, out var gradientOpacityPixels), "SVG rasterization should render referenced fill paint with element opacity.");
        Assert(MaximumAlpha(gradientOpacityPixels) is >= 126 and <= 129, "Gradient-only primitive opacity should affect the referenced fill exactly once.");

        const string ellipticalCorners = "<rect width='100' height='40' rx='20' ry='5' fill='#ef4444'/>";
        Assert(SvgRasterRenderer.TryRenderFragment(ellipticalCorners, "0 0 100 40", "none", 100, 40, out var ellipticalCornerPixels), "SVG rasterization should render rounded rectangles with independent radii.");
        Assert(IsPixelNear(ellipticalCornerPixels, 100, 5, 2, 239, 68, 68), "Rounded rectangles should preserve shallow elliptical ry corners instead of collapsing both axes to rx.");
        const string ellipticalClip = "<defs><clipPath id='elliptical'><rect width='100' height='40' rx='20' ry='5'/></clipPath></defs><rect width='100' height='40' fill='#2563eb' clip-path='url(#elliptical)'/>";
        Assert(SvgRasterRenderer.TryRenderFragment(ellipticalClip, "0 0 100 40", "none", 100, 40, out var ellipticalClipPixels) && IsPixelNear(ellipticalClipPixels, 100, 5, 2, 37, 99, 235), "Rounded rectangle clip paths should preserve independent rx and ry geometry.");
        const string automaticHorizontalRadius = "<rect width='100' height='40' rx='auto' ry='5' fill='#16a34a'/>";
        Assert(SvgRasterRenderer.TryRenderFragment(automaticHorizontalRadius, "0 0 100 40", "none", 100, 40, out var automaticHorizontalRadiusPixels) && IsPixelNear(automaticHorizontalRadiusPixels, 100, 5, 2, 22, 163, 74), "An automatic rounded-rectangle rx should copy the resolved ry value.");
        const string automaticVerticalClipRadius = "<defs><clipPath id='automatic-radius'><rect width='100' height='40' rx='20' ry='auto'/></clipPath></defs><rect width='100' height='40' fill='#8b5cf6' clip-path='url(#automatic-radius)'/>";
        Assert(SvgRasterRenderer.TryRenderFragment(automaticVerticalClipRadius, "0 0 100 40", "none", 100, 40, out var automaticVerticalRadiusPixels) && PixelAlpha(automaticVerticalRadiusPixels, 100, 5, 5) == 0, "An automatic rounded-rectangle ry should copy rx before clip-path geometry is constructed.");

        const string openFilledPath = "<path d='M5 5 L35 5 L35 35' fill='#16a34a' stroke='#2563eb' stroke-width='2'/>";
        Assert(SvgRasterRenderer.TryRenderFragment(openFilledPath, "0 0 40 40", "none", 40, 40, out var openFilledPathPixels), "SVG rasterization should fill an open path while keeping its stroke open.");
        Assert(IsPixelNear(openFilledPathPixels, 40, 30, 10, 22, 163, 74), "Open SVG subpaths should close implicitly for fill geometry.");
        const string mixedClosurePath = "<path d='M5 5 L25 5 L25 25 Z M50 5 L85 5 L85 35' fill='none' stroke='#2563eb' stroke-width='4'/>";
        Assert(SvgRasterRenderer.TryRenderFragment(mixedClosurePath, "0 0 90 40", "none", 90, 40, out var mixedClosurePathPixels), "SVG rasterization should preserve closure independently for every path subpath.");
        Assert(PixelAlpha(mixedClosurePathPixels, 90, 67, 20) == 0, "A closed subpath should not force a sibling open subpath's stroke to close.");

        const string markedPrimitive = "<defs><marker id='arrow-opacity' viewBox='0 0 10 10' refX='9' refY='5' markerWidth='8' markerHeight='8' orient='auto'><path d='M0 0 L10 5 L0 10 z' fill='#ff0000'/></marker></defs><path d='M10 20 L70 20' fill='none' stroke='#111111' stroke-width='4' marker-end='url(#arrow-opacity)' opacity='.5'/>";
        Assert(SvgRasterRenderer.TryRenderFragment(markedPrimitive, "0 0 80 40", "none", 80, 40, out var markedPrimitivePixels), "SVG rasterization should render a partially opaque stroked path and marker as one painted primitive.");
        Assert(MaximumAlpha(markedPrimitivePixels) is >= 126 and <= 129, "Path opacity should composite marker and stroke paint before applying alpha once.");
        const string affineMarker = "<defs><marker id='affine-arrow' viewBox='0 0 10 10' refX='9' refY='5' markerWidth='8' markerHeight='8' markerUnits='userSpaceOnUse' orient='auto'><path d='M0 0 L10 5 L0 10 z' fill='#ef4444'/></marker></defs><g transform='scale(2 1)'><path d='M10 30 L45 30' fill='none' stroke='#111111' marker-end='url(#affine-arrow)'/></g>";
        Assert(SvgRasterRenderer.TryRenderFragment(affineMarker, "0 0 120 60", "none", 120, 60, out var affineMarkerPixels), "SVG rasterization should render markers under non-uniform host transforms.");
        var affineMarkerBounds = SvgColorBounds(affineMarkerPixels, 120, 60, 239, 68, 68);
        Assert(affineMarkerBounds.HasPixels && affineMarkerBounds.Width > affineMarkerBounds.Height * 1.4, $"SVG markers should preserve the host affine transform instead of collapsing it to a uniform scale (bounds {affineMarkerBounds.Width}x{affineMarkerBounds.Height}).");
        const string transformedLineMarker = "<defs><marker id='line-arrow' viewBox='0 0 10 10' refX='9' refY='5' markerWidth='8' markerHeight='8' markerUnits='userSpaceOnUse' orient='auto'><path d='M0 0 L10 5 L0 10 z' fill='#ef4444'/></marker></defs><line x1='10' y1='30' x2='45' y2='30' stroke='#111111' marker-end='url(#line-arrow)' transform='scale(2 1)'/>";
        Assert(SvgRasterRenderer.TryRenderFragment(transformedLineMarker, "0 0 120 60", "none", 120, 60, out var transformedLineMarkerPixels), "SVG rasterization should render line markers from source coordinates under host transforms.");
        var transformedLineMarkerBounds = SvgColorBounds(transformedLineMarkerPixels, 120, 60, 239, 68, 68);
        Assert(transformedLineMarkerBounds.HasPixels && transformedLineMarkerBounds.Right < 95 && transformedLineMarkerBounds.Width > transformedLineMarkerBounds.Height * 1.4, "Line markers should apply the host affine transform once.");

        const string rootOpacity = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40' opacity='.5'><rect width='70' height='40' fill='#ff0000'/><rect x='30' width='70' height='40' fill='#ff0000'/></svg>";
        var rootOpacityImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(rootOpacity));
        Assert(PixelAlpha(rootOpacityImage.Pixels, 100, 15, 20) is >= 126 and <= 129 && PixelAlpha(rootOpacityImage.Pixels, 100, 50, 20) is >= 126 and <= 129, "Root opacity should be applied once after compositing all root children.");

        const string rootClip = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40' clip-path='url(#left-half)'><defs><clipPath id='left-half'><rect width='50' height='40'/></clipPath></defs><rect width='100' height='40' fill='#ff0000'/></svg>";
        var rootClipImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(rootClip));
        Assert(IsPixelNear(rootClipImage.Pixels, 100, 25, 20, 255, 0, 0) && PixelAlpha(rootClipImage.Pixels, 100, 75, 20) == 0, "Root clip paths should composite all document children before clipping the raster output.");

        const string rootMask = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40' mask='url(#left-half)'><defs><mask id='left-half'><rect width='50' height='40' fill='#ffffff'/></mask></defs><rect width='100' height='40' fill='#2563eb'/></svg>";
        var rootMaskImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(rootMask));
        Assert(IsPixelNear(rootMaskImage.Pixels, 100, 25, 20, 37, 99, 235) && PixelAlpha(rootMaskImage.Pixels, 100, 75, 20) == 0, "Root masks should composite all document children before masking the raster output.");
        const string boundedMask = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40'><defs><mask id='bounded' maskUnits='userSpaceOnUse' x='0' y='0' width='10' height='40'><rect width='100' height='40' fill='#ffffff'/></mask></defs><rect width='100' height='40' fill='#2563eb' mask='url(#bounded)'/></svg>";
        var boundedMaskImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(boundedMask));
        Assert(IsPixelNear(boundedMaskImage.Pixels, 100, 5, 20, 37, 99, 235) && PixelAlpha(boundedMaskImage.Pixels, 100, 15, 20) == 0, "User-space mask regions should clip mask paint to their declared x, y, width, and height.");
        const string scaledBoundedMask = "<svg xmlns='http://www.w3.org/2000/svg' width='200' height='100' viewBox='0 0 100 50'><defs><mask id='scaled-bounded' maskUnits='userSpaceOnUse' x='0' y='0' width='50%' height='100%'><rect width='100' height='50' fill='#ffffff'/></mask></defs><rect width='100' height='50' fill='#2563eb' mask='url(#scaled-bounded)'/></svg>";
        var scaledBoundedMaskImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(scaledBoundedMask));
        Assert(IsPixelNear(scaledBoundedMaskImage.Pixels, 200, 90, 50, 37, 99, 235) && PixelAlpha(scaledBoundedMaskImage.Pixels, 200, 110, 50) == 0, "User-space mask percentages should resolve in the logical SVG viewport before output scaling.");
        const string pixelUnitMask = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40'><defs><mask id='pixel-unit' maskUnits='userSpaceOnUse' x='0px' y='0px' width='10px' height='40px'><rect width='100' height='40' fill='#ffffff'/></mask></defs><rect width='100' height='40' fill='#2563eb' mask='url(#pixel-unit)'/></svg>";
        var pixelUnitMaskImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(pixelUnitMask));
        Assert(IsPixelNear(pixelUnitMaskImage.Pixels, 100, 5, 20, 37, 99, 235) && PixelAlpha(pixelUnitMaskImage.Pixels, 100, 15, 20) == 0, "Mask regions should preserve explicit absolute SVG length units.");
        const string objectBoundedMask = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40'><defs><mask id='object-bounded' x='0' y='0' width='50%' height='100%'><rect width='100' height='40' fill='#ffffff'/></mask></defs><rect x='20' y='10' width='40' height='20' fill='#16a34a' mask='url(#object-bounded)'/></svg>";
        var objectBoundedMaskImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(objectBoundedMask));
        Assert(IsPixelNear(objectBoundedMaskImage.Pixels, 100, 25, 20, 22, 163, 74) && PixelAlpha(objectBoundedMaskImage.Pixels, 100, 50, 20) == 0, "Object-bounding-box mask regions should resolve percentages against the masked element footprint.");
        const string objectBoundedContent = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40'><defs><mask id='object-content' maskContentUnits='objectBoundingBox'><rect width='1' height='1' fill='#ffffff'/></mask></defs><rect x='20' y='10' width='40' height='20' fill='#0ea5e9' mask='url(#object-content)'/></svg>";
        var objectBoundedContentImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(objectBoundedContent));
        Assert(IsPixelNear(objectBoundedContentImage.Pixels, 100, 40, 20, 14, 165, 233), "Object-bounding-box mask content should scale normalized child coordinates across the masked element footprint.");
        const string percentageObjectContent = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40'><defs><mask id='percentage-content' maskContentUnits='objectBoundingBox'><rect width='50%' height='100%' fill='#ffffff'/></mask></defs><rect x='20' y='10' width='40' height='20' fill='#f97316' mask='url(#percentage-content)'/></svg>";
        var percentageObjectContentImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(percentageObjectContent));
        Assert(IsPixelNear(percentageObjectContentImage.Pixels, 100, 30, 20, 249, 115, 22) && PixelAlpha(percentageObjectContentImage.Pixels, 100, 50, 20) == 0, "Percentage mask geometry should resolve within the normalized object-bounding-box coordinate system.");
        const string clippedObjectContent = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40'><defs><clipPath id='visible-half'><rect x='20' y='10' width='20' height='20'/></clipPath><mask id='geometry-content' maskContentUnits='objectBoundingBox'><rect width='.5' height='1' fill='#ffffff'/></mask></defs><rect x='20' y='10' width='40' height='20' fill='#a855f7' clip-path='url(#visible-half)' mask='url(#geometry-content)'/></svg>";
        var clippedObjectContentImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(clippedObjectContent));
        Assert(IsPixelNear(clippedObjectContentImage.Pixels, 100, 35, 20, 168, 85, 247), "Object-bounding-box mask content should use geometric bounds even when clipping narrows the painted footprint.");
        const string hiddenGeometryObjectContent = "<svg xmlns='http://www.w3.org/2000/svg' width='120' height='40'><defs><mask id='hidden-geometry' maskContentUnits='objectBoundingBox'><rect width='1' height='1' fill='#ffffff'/></mask></defs><g mask='url(#hidden-geometry)'><rect x='20' y='10' width='40' height='20' fill='#14b8a6'/><rect x='100' y='10' width='20' height='20' display='none'/></g></svg>";
        var hiddenGeometryObjectContentImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(hiddenGeometryObjectContent));
        Assert(IsPixelNear(hiddenGeometryObjectContentImage.Pixels, 120, 40, 20, 20, 184, 166), "Hidden descendants should not expand the geometric object bounding box used by normalized mask content.");
        const string referencedPercentageObjectContent = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40'><defs><g id='half-mask'><rect width='50%' height='100%' fill='#ffffff'/></g><mask id='referenced-content' maskContentUnits='objectBoundingBox'><use href='#half-mask'/></mask></defs><rect x='20' y='10' width='40' height='20' fill='#e11d48' mask='url(#referenced-content)'/></svg>";
        var referencedPercentageObjectContentImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(referencedPercentageObjectContent));
        Assert(IsPixelNear(referencedPercentageObjectContentImage.Pixels, 100, 30, 20, 225, 29, 72) && PixelAlpha(referencedPercentageObjectContentImage.Pixels, 100, 50, 20) == 0, "Referenced mask descendants should resolve percentage geometry in the object-bounding-box coordinate system.");
        const string rotatedObjectMask = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='60'><defs><mask id='rotated-object' x='0' y='0' width='50%' height='100%'><rect width='100' height='60' fill='#ffffff'/></mask></defs><rect x='30' y='20' width='40' height='20' transform='rotate(90 50 30)' fill='#16a34a' mask='url(#rotated-object)'/></svg>";
        var rotatedObjectMaskImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(rotatedObjectMask));
        Assert(IsPixelNear(rotatedObjectMaskImage.Pixels, 100, 50, 15, 22, 163, 74) && PixelAlpha(rotatedObjectMaskImage.Pixels, 100, 50, 45) == 0, "Object-bounding-box mask regions should retain the masked element's local orientation under transforms.");
        const string rootAlphaMask = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40' mask='url(#alpha-half)'><defs><mask id='alpha-half' mask-type='alpha'><rect width='50' height='40' fill='#000000'/></mask></defs><rect width='100' height='40' fill='#2563eb'/></svg>";
        var rootAlphaMaskImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(rootAlphaMask));
        Assert(IsPixelNear(rootAlphaMaskImage.Pixels, 100, 25, 20, 37, 99, 235) && PixelAlpha(rootAlphaMaskImage.Pixels, 100, 75, 20) == 0, "Alpha-mode root masks should use mask opacity rather than RGB luminance.");
        const string styledElementAlphaMask = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40'><style>.alpha-mask{mask-type:alpha}</style><defs><mask id='alpha-element' class='alpha-mask'><rect width='50' height='40' fill='#000000'/></mask></defs><rect width='100' height='40' fill='#ef4444' mask='url(#alpha-element)'/></svg>";
        var styledElementAlphaMaskImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(styledElementAlphaMask));
        Assert(IsPixelNear(styledElementAlphaMaskImage.Pixels, 100, 25, 20, 239, 68, 68) && PixelAlpha(styledElementAlphaMaskImage.Pixels, 100, 75, 20) == 0, "CSS mask-type alpha should apply to element-level SVG masks.");
        const string descendantStyledAlphaMask = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40'><style>svg defs .alpha-mask{mask-type:alpha}</style><defs><mask id='descendant-alpha' class='alpha-mask'><rect width='50' height='40' fill='#000000'/></mask></defs><rect width='100' height='40' fill='#f59e0b' mask='url(#descendant-alpha)'/></svg>";
        var descendantStyledAlphaMaskImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(descendantStyledAlphaMask));
        Assert(IsPixelNear(descendantStyledAlphaMaskImage.Pixels, 100, 25, 20, 245, 158, 11) && PixelAlpha(descendantStyledAlphaMaskImage.Pixels, 100, 75, 20) == 0, "Descendant selectors should resolve mask-type against a mask's definition ancestry.");
        const string inheritedMaskTypeVariable = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40' style='--mask-mode:alpha'><defs><mask id='variable-alpha' style='mask-type:var(--mask-mode)'><rect width='50' height='40' fill='#000000'/></mask></defs><rect width='100' height='40' fill='#8b5cf6' mask='url(#variable-alpha)'/></svg>";
        var inheritedMaskTypeVariableImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(inheritedMaskTypeVariable));
        Assert(IsPixelNear(inheritedMaskTypeVariableImage.Pixels, 100, 25, 20, 139, 92, 246) && PixelAlpha(inheritedMaskTypeVariableImage.Pixels, 100, 75, 20) == 0, "Inherited CSS custom properties should resolve mask-type on referenced masks.");
        const string inheritedMaskPaint = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40'><defs><mask id='inherited-paint' fill='#ffffff'><rect width='50' height='40'/></mask></defs><rect width='100' height='40' fill='#22c55e' mask='url(#inherited-paint)'/></svg>";
        var inheritedMaskPaintImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(inheritedMaskPaint));
        Assert(IsPixelNear(inheritedMaskPaintImage.Pixels, 100, 25, 20, 34, 197, 94) && PixelAlpha(inheritedMaskPaintImage.Pixels, 100, 75, 20) == 0, "Mask children should inherit paint from the mask root before luminance is applied.");
        const string nonInheritedMaskType = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40' mask-type='alpha'><defs><mask id='default-luminance'><rect width='50' height='40' fill='#000000'/></mask></defs><rect width='100' height='40' fill='#06b6d4' mask='url(#default-luminance)'/></svg>";
        var nonInheritedMaskTypeImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(nonInheritedMaskType));
        Assert(MaximumAlpha(nonInheritedMaskTypeImage.Pixels) == 0, "mask-type should remain non-inherited so each mask defaults to luminance unless it declares alpha.");

        const string nestedViewport = "<svg xmlns='http://www.w3.org/2000/svg' width='60' height='40'><svg x='10' y='10' width='20' height='20' viewBox='0 0 20 20'><rect x='-10' y='-10' width='40' height='40' fill='#ff0000'/></svg></svg>";
        var nestedViewportImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(nestedViewport));
        Assert(IsPixelNear(nestedViewportImage.Pixels, 60, 15, 15, 255, 0, 0) && PixelAlpha(nestedViewportImage.Pixels, 60, 5, 15) == 0 && PixelAlpha(nestedViewportImage.Pixels, 60, 35, 15) == 0, "Nested SVG viewports should clip overflowing descendants by default.");
        const string visibleNestedViewport = "<svg xmlns='http://www.w3.org/2000/svg' width='60' height='40'><svg x='10' y='10' width='20' height='20' viewBox='0 0 20 20' overflow='visible'><rect x='-10' y='-10' width='40' height='40' fill='#ff0000'/></svg></svg>";
        var visibleNestedViewportImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(visibleNestedViewport));
        Assert(IsPixelNear(visibleNestedViewportImage.Pixels, 60, 5, 15, 255, 0, 0) && IsPixelNear(visibleNestedViewportImage.Pixels, 60, 35, 15, 255, 0, 0), "Nested SVG overflow visible should deliberately expose descendants outside the viewport.");
        const string percentageNestedViewport = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40'><svg x='10%' y='25%' width='50%' height='50%' viewBox='0 0 10 10' preserveAspectRatio='none'><rect width='10' height='10' fill='#ff0000'/></svg></svg>";
        var percentageNestedViewportImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(percentageNestedViewport));
        Assert(IsPixelNear(percentageNestedViewportImage.Pixels, 100, 20, 20, 255, 0, 0) && PixelAlpha(percentageNestedViewportImage.Pixels, 100, 5, 20) == 0 && PixelAlpha(percentageNestedViewportImage.Pixels, 100, 65, 20) == 0, "Nested SVG percentage dimensions and positions should resolve against the parent viewport.");
        const string pixelUnitNestedViewport = "<svg xmlns='http://www.w3.org/2000/svg' width='60' height='40'><svg x='10px' y='10px' width='20px' height='20px' viewBox='0 0 20 20'><rect width='20' height='20' fill='#ff0000'/></svg></svg>";
        var pixelUnitNestedViewportImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(pixelUnitNestedViewport));
        Assert(IsPixelNear(pixelUnitNestedViewportImage.Pixels, 60, 15, 15, 255, 0, 0) && PixelAlpha(pixelUnitNestedViewportImage.Pixels, 60, 5, 15) == 0 && PixelAlpha(pixelUnitNestedViewportImage.Pixels, 60, 35, 15) == 0, "Nested SVG viewports should preserve explicit absolute SVG length units.");
        const string zeroNestedViewport = "<svg xmlns='http://www.w3.org/2000/svg' width='60' height='40'><svg width='0' height='20' overflow='visible'><rect width='60' height='40' fill='#ff0000'/></svg></svg>";
        var zeroNestedViewportImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(zeroNestedViewport));
        Assert(MaximumAlpha(zeroNestedViewportImage.Pixels) == 0, "A nested SVG with a zero viewport dimension should not render descendants even when overflow is visible.");

        const string physicalViewport = "<svg xmlns='http://www.w3.org/2000/svg' width='2in' height='1in'><rect width='192' height='96' fill='#2563eb'/></svg>";
        var physicalViewportImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(physicalViewport));
        Assert(physicalViewportImage.Width == 192 && physicalViewportImage.Height == 96, "Public SVG rasterization should resolve absolute physical viewport units at 96 CSS pixels per inch.");

        const string percentageGeometry = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100'><rect width='25%' height='25%' fill='#ef4444'/><circle cx='50%' cy='20%' r='8%' fill='#16a34a'/><ellipse cx='80%' cy='20%' rx='8%' ry='12%' fill='#2563eb'/><line x1='0%' y1='55%' x2='100%' y2='55%' stroke='#111827' stroke-width='3'/></svg>";
        var percentageGeometryImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(percentageGeometry));
        Assert(IsPixelNear(percentageGeometryImage.Pixels, 100, 20, 20, 239, 68, 68), "Percentage rectangle dimensions should resolve against the active SVG viewport.");
        Assert(IsPixelNear(percentageGeometryImage.Pixels, 100, 50, 20, 22, 163, 74) && IsPixelNear(percentageGeometryImage.Pixels, 100, 80, 20, 37, 99, 235), "Percentage circle and ellipse coordinates should resolve against the appropriate viewport axes.");
        Assert(PixelAlpha(percentageGeometryImage.Pixels, 100, 5, 55) > 0 && PixelAlpha(percentageGeometryImage.Pixels, 100, 95, 55) > 0, "Percentage line endpoints should span the active viewport.");
        var percentageImageMarkup = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40'><image x='50%' y='25%' width='25%' height='50%' preserveAspectRatio='none' href='" + imageSource + "'/></svg>";
        var percentageImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(percentageImageMarkup));
        Assert(IsPixelNear(percentageImage.Pixels, 100, 60, 20, 255, 0, 0) && PixelAlpha(percentageImage.Pixels, 100, 40, 20) == 0, "Percentage image placement should resolve against the active viewport.");

        const string objectBoundingBoxClip = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40'><defs><clipPath id='half' clipPathUnits='objectBoundingBox'><rect width='50%' height='100%'/></clipPath></defs><rect x='20' y='10' width='40' height='20' fill='#f97316' clip-path='url(#half)'/></svg>";
        var objectBoundingBoxClipImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(objectBoundingBoxClip));
        Assert(IsPixelNear(objectBoundingBoxClipImage.Pixels, 100, 30, 20, 249, 115, 22) && PixelAlpha(objectBoundingBoxClipImage.Pixels, 100, 50, 20) == 0, "Object-bounding-box clip paths should map normalized percentage geometry into the target element's geometric bounds.");
        const string userSpaceUseClip = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40'><defs><g id='clip-shape'><rect width='20' height='20'/></g><clipPath id='reused'><use href='#clip-shape' x='30' y='10'/></clipPath></defs><rect width='100' height='40' fill='#f97316' clip-path='url(#reused)'/></svg>";
        var userSpaceUseClipImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(userSpaceUseClip));
        Assert(IsPixelNear(userSpaceUseClipImage.Pixels, 100, 40, 20, 249, 115, 22) && PixelAlpha(userSpaceUseClipImage.Pixels, 100, 20, 20) == 0, "User-space clip paths should expand use references, including referenced container content and use translation.");
        const string userSpaceSymbolClip = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40'><style>defs .hole{clip-rule:evenodd}</style><defs><symbol id='clip-symbol' class='hole'><path d='M10 5 H90 V35 H10 Z M35 12 H65 V28 H35 Z'/></symbol><symbol id='paint-symbol' fill='#2563eb'><rect width='20' height='20'/></symbol><clipPath id='symbol-hole'><use href='#clip-symbol' width='100' height='40'/></clipPath></defs><rect width='100' height='40' fill='#f97316' clip-path='url(#symbol-hole)'/><use href='#paint-symbol' x='0' y='10' width='20' height='20'/></svg>";
        var userSpaceSymbolClipImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(userSpaceSymbolClip));
        Assert(IsPixelNear(userSpaceSymbolClipImage.Pixels, 100, 20, 20, 249, 115, 22) && PixelAlpha(userSpaceSymbolClipImage.Pixels, 100, 50, 20) == 0, "User-space symbol clips should preserve source CSS and inherited clip-rule semantics.");
        Assert(IsPixelNear(userSpaceSymbolClipImage.Pixels, 100, 5, 20, 37, 99, 235), "Ordinary symbol use should inherit presentation paint declared on the referenced symbol.");
        const string nestedObjectBoundingBoxClip = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40'><defs><clipPath id='nested' clipPathUnits='objectBoundingBox'><svg width='1' height='1' viewBox='0 0 10 10' preserveAspectRatio='none'><rect width='100%' height='100%'/></svg></clipPath></defs><rect x='20' y='10' width='40' height='20' fill='#14b8a6' clip-path='url(#nested)'/></svg>";
        var nestedObjectBoundingBoxClipImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(nestedObjectBoundingBoxClip));
        Assert(IsPixelNear(nestedObjectBoundingBoxClipImage.Pixels, 100, 55, 20, 20, 184, 166), "Nested viewports inside object-bounding-box content should resolve descendant percentages in their own viewBox.");

        const string percentageSymbol = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40'><defs><symbol id='half-symbol' viewBox='0 0 10 10' preserveAspectRatio='none'><rect width='50%' height='100%' fill='#8b5cf6'/></symbol></defs><use href='#half-symbol' x='10' y='10' width='40' height='20'/></svg>";
        var percentageSymbolImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(percentageSymbol));
        Assert(IsPixelNear(percentageSymbolImage.Pixels, 100, 25, 20, 139, 92, 246) && PixelAlpha(percentageSymbolImage.Pixels, 100, 35, 20) == 0, "Percentage geometry in referenced symbols should resolve against the symbol viewBox viewport.");
        const string percentageViewBoxlessSymbol = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40'><defs><symbol id='plain-symbol'><rect width='50%' height='100%' fill='#0ea5e9'/></symbol></defs><use href='#plain-symbol' x='10' y='10' width='40' height='20'/></svg>";
        var percentageViewBoxlessSymbolImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(percentageViewBoxlessSymbol));
        Assert(IsPixelNear(percentageViewBoxlessSymbolImage.Pixels, 100, 25, 20, 14, 165, 233) && PixelAlpha(percentageViewBoxlessSymbolImage.Pixels, 100, 35, 20) == 0, "ViewBox-less symbols should use the use element's viewport for percentage geometry.");
        const string objectBoundingBoxSymbolClip = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40'><defs><symbol id='clip-symbol'><rect width='50%' height='100%'/></symbol><clipPath id='symbol-clip' clipPathUnits='objectBoundingBox'><use href='#clip-symbol' width='1' height='1'/></clipPath></defs><rect x='20' y='10' width='40' height='20' fill='#e11d48' clip-path='url(#symbol-clip)'/></svg>";
        var objectBoundingBoxSymbolClipImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(objectBoundingBoxSymbolClip));
        Assert(IsPixelNear(objectBoundingBoxSymbolClipImage.Pixels, 100, 30, 20, 225, 29, 72) && PixelAlpha(objectBoundingBoxSymbolClipImage.Pixels, 100, 50, 20) == 0, "Object-bounding-box preprocessing should expand referenced viewBox-less symbols into a renderable viewport.");

        const string percentagePattern = "<svg xmlns='http://www.w3.org/2000/svg' width='40' height='20'><defs><pattern id='half-tile' patternUnits='userSpaceOnUse' x='0' y='0' width='20' height='20' viewBox='0 0 10 10' preserveAspectRatio='none'><rect width='50%' height='100%' fill='#ef4444'/></pattern></defs><rect width='40' height='20' fill='url(#half-tile)'/></svg>";
        var percentagePatternImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(percentagePattern));
        Assert(IsPixelNear(percentagePatternImage.Pixels, 40, 5, 10, 239, 68, 68) && PixelAlpha(percentagePatternImage.Pixels, 40, 15, 10) == 0 && IsPixelNear(percentagePatternImage.Pixels, 40, 25, 10, 239, 68, 68), "Pattern percentages should resolve against the pattern viewBox independently of tile pixel size.");

        const string mixedText = "<svg xmlns='http://www.w3.org/2000/svg' width='140' height='40'><text x='4' y='30' font-size='24' fill='#ef4444'>A <tspan fill='#2563eb'>B</tspan> C</text></svg>";
        var mixedTextImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(mixedText));
        var redText = SvgColorBounds(mixedTextImage.Pixels, 140, 40, 239, 68, 68);
        var blueText = SvgColorBounds(mixedTextImage.Pixels, 140, 40, 37, 99, 235);
        Assert(redText.HasPixels && blueText.HasPixels && redText.Left < blueText.Left && redText.Right > blueText.Right, "Mixed text content should preserve direct text nodes before and after styled tspans in document order.");

        const string transformedText = "<svg xmlns='http://www.w3.org/2000/svg' width='80' height='80'><text x='12' y='18' font-size='16' fill='#ef4444' transform='rotate(90 12 18)'>TEST</text></svg>";
        var transformedTextImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(transformedText));
        var transformedTextBounds = SvgColorBounds(transformedTextImage.Pixels, 80, 80, 239, 68, 68);
        Assert(transformedTextBounds.HasPixels && transformedTextBounds.Height > transformedTextBounds.Width, "Affine text transforms should rotate glyph pixels rather than only moving the text anchor.");
        const string strokedText = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40'><text x='4' y='30' font-size='24' fill='none' stroke='#ef4444' stroke-width='2'>OUTLINE</text></svg>";
        var strokedTextImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(strokedText));
        Assert(SvgColorBounds(strokedTextImage.Pixels, 100, 40, 239, 68, 68).HasPixels, "Stroke-only SVG text should remain visible in PNG output.");
        const string referencedTextFill = "<svg xmlns='http://www.w3.org/2000/svg' width='140' height='50'><defs><linearGradient id='text-gradient'><stop stop-color='#ef4444'/><stop offset='1' stop-color='#2563eb'/></linearGradient><pattern id='text-pattern' width='8' height='8' patternUnits='userSpaceOnUse'><rect width='4' height='8' fill='#16a34a'/></pattern></defs><text x='4' y='38' font-size='32' fill='url(#text-gradient)'>GRAD</text><text x='100' y='38' font-size='32' fill='url(#text-pattern)'>P</text></svg>";
        var referencedTextFillImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(referencedTextFill));
        Assert(HasRedAndBlueDominantPixels(referencedTextFillImage.Pixels) && SvgAlphaBounds(referencedTextFillImage.Pixels, 140, 50).Right > 100, "Referenced gradient and pattern fills should paint SVG text glyphs in PNG output.");
        const string segmentedGradientText = "<svg xmlns='http://www.w3.org/2000/svg' width='160' height='55'><defs><linearGradient id='shared-text'><stop offset='0' stop-color='#ef4444'/><stop offset='.49' stop-color='#ef4444'/><stop offset='.51' stop-color='#2563eb'/><stop offset='1' stop-color='#2563eb'/></linearGradient></defs><text x='4' y='44' font-size='40' fill='url(#shared-text)'>OO<tspan>OO</tspan></text></svg>";
        var segmentedGradientTextImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(segmentedGradientText));
        var segmentedRedBounds = SvgColorBounds(segmentedGradientTextImage.Pixels, 160, 55, 239, 68, 68);
        var segmentedBlueBounds = SvgColorBounds(segmentedGradientTextImage.Pixels, 160, 55, 37, 99, 235);
        Assert(segmentedRedBounds.HasPixels && segmentedBlueBounds.HasPixels && segmentedRedBounds.Right < segmentedBlueBounds.Left, "Object-bounding-box text paint should share one element geometry across adjacent text and tspan runs.");
        const string affineReferencedText = "<svg xmlns='http://www.w3.org/2000/svg' width='220' height='100'><defs><linearGradient id='affine-gradient'><stop offset='0' stop-color='#ef4444'/><stop offset='.49' stop-color='#ef4444'/><stop offset='.51' stop-color='#2563eb'/><stop offset='1' stop-color='#2563eb'/></linearGradient><pattern id='affine-pattern' width='1' height='1' patternContentUnits='objectBoundingBox'><rect width='.49' height='1' fill='#ef4444'/><rect x='.51' width='.49' height='1' fill='#2563eb'/></pattern></defs><text x='20' y='46' font-size='40' fill='url(#affine-gradient)'><tspan transform='skewX(25)'>OOOO</tspan></text><text x='20' y='94' font-size='40' fill='url(#affine-pattern)'><tspan transform='skewX(25)'>OOOO</tspan></text></svg>";
        var affineReferencedTextImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(affineReferencedText));
        var affineGradientRed = SvgColorBoundsInRows(affineReferencedTextImage.Pixels, 220, 0, 49, 239, 68, 68);
        var affineGradientBlue = SvgColorBoundsInRows(affineReferencedTextImage.Pixels, 220, 0, 49, 37, 99, 235);
        var affinePatternRed = SvgColorBoundsInRows(affineReferencedTextImage.Pixels, 220, 50, 99, 239, 68, 68);
        var affinePatternBlue = SvgColorBoundsInRows(affineReferencedTextImage.Pixels, 220, 50, 99, 37, 99, 235);
        Assert(affineGradientRed.HasPixels && affineGradientBlue.HasPixels && affineGradientRed.Right - affineGradientBlue.Left <= 12, $"Object-bounding-box gradients should remain fixed to root text geometry across transformed tspans (red {affineGradientRed.Left}-{affineGradientRed.Right}, blue {affineGradientBlue.Left}-{affineGradientBlue.Right}).");
        Assert(affinePatternRed.HasPixels && affinePatternBlue.HasPixels && affinePatternRed.Right - affinePatternBlue.Left <= 12, $"Object-bounding-box patterns should remain fixed to root text geometry across transformed tspans (red {affinePatternRed.Left}-{affinePatternRed.Right}, blue {affinePatternBlue.Left}-{affinePatternBlue.Right}).");
        const string paintedSpanText = "<svg xmlns='http://www.w3.org/2000/svg' width='180' height='90'><text x='8' y='68' font-size='60'><tspan fill='#ef4444' stroke='#2563eb' stroke-width='2'>OO</tspan></text></svg>";
        var paintedSpanTextImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(paintedSpanText));
        Assert(SvgColorBounds(paintedSpanTextImage.Pixels, 180, 90, 239, 68, 68).HasPixels && SvgColorBounds(paintedSpanTextImage.Pixels, 180, 90, 37, 99, 235).HasPixels, "Combined text paint should retain both fill and the default overlaid stroke.");
        const string translucentPaintedSpanText = "<svg xmlns='http://www.w3.org/2000/svg' width='180' height='90'><text x='8' y='68' font-size='60'><tspan fill='#ef4444' stroke='#2563eb' stroke-width='2' opacity='.5'>OO</tspan></text></svg>";
        var translucentPaintedSpanTextImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(translucentPaintedSpanText));
        Assert(MaximumAlpha(translucentPaintedSpanTextImage.Pixels) is >= 126 and <= 129, "Leaf tspan opacity should composite fill and stroke exactly once.");

        const string centeredMixedText = "<svg xmlns='http://www.w3.org/2000/svg' width='140' height='40'><text x='70' y='30' text-anchor='middle' font-size='24' fill='#ef4444'>A<tspan fill='#2563eb'>B</tspan>C</text></svg>";
        var centeredMixedTextImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(centeredMixedText));
        var centeredBounds = SvgAlphaBounds(centeredMixedTextImage.Pixels, 140, 40);
        Assert(centeredBounds.HasPixels && Math.Abs((centeredBounds.Left + centeredBounds.Right) / 2.0 - 70) < 5, "Text anchoring should center a mixed-style text chunk once rather than centering every run independently.");
        const string positionedCenteredText = "<svg xmlns='http://www.w3.org/2000/svg' width='140' height='40'><text y='30' text-anchor='middle' font-size='24' fill='#ef4444'><tspan x='70' fill='#2563eb'>B</tspan>C</text></svg>";
        var positionedCenteredTextImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(positionedCenteredText));
        var positionedCenteredBounds = SvgAlphaBounds(positionedCenteredTextImage.Pixels, 140, 40);
        Assert(positionedCenteredBounds.HasPixels && Math.Abs((positionedCenteredBounds.Left + positionedCenteredBounds.Right) / 2.0 - 70) < 5, "An absolute tspan position should start an anchored chunk that includes following sibling text.");

        const string transformedSpanText = "<svg xmlns='http://www.w3.org/2000/svg' width='80' height='80'><text x='12' y='18' font-size='16' fill='#16a34a'><tspan transform='rotate(90 12 18)'>TEST</tspan></text></svg>";
        var transformedSpanTextImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(transformedSpanText));
        var transformedSpanBounds = SvgColorBounds(transformedSpanTextImage.Pixels, 80, 80, 22, 163, 74);
        Assert(transformedSpanBounds.HasPixels && transformedSpanBounds.Height > transformedSpanBounds.Width, "Nested tspan transforms should affect glyph pixels.");

        const string preservedText = "<svg xmlns='http://www.w3.org/2000/svg' width='140' height='40'><text x='4' y='30' font-size='20' fill='#ef4444' xml:space='preserve'>A   B</text></svg>";
        const string collapsedText = "<svg xmlns='http://www.w3.org/2000/svg' width='140' height='40'><text x='4' y='30' font-size='20' fill='#ef4444'>A   B</text></svg>";
        var preservedTextBounds = SvgColorBounds(RasterImageDecoder.Decode(SvgRasterizer.ToPng(preservedText)).Pixels, 140, 40, 239, 68, 68);
        var collapsedTextBounds = SvgColorBounds(RasterImageDecoder.Decode(SvgRasterizer.ToPng(collapsedText)).Pixels, 140, 40, 239, 68, 68);
        Assert(preservedTextBounds.Width > collapsedTextBounds.Width, "Explicitly preserved SVG whitespace should retain repeated spaces instead of using normal collapsing.");
        const string preLineText = "<svg xmlns='http://www.w3.org/2000/svg' width='80' height='60'><text x='4' y='18' font-size='16' fill='#2563eb' style='white-space:pre-line'>A   A\nB</text></svg>";
        var preLineTextBounds = SvgColorBounds(RasterImageDecoder.Decode(SvgRasterizer.ToPng(preLineText)).Pixels, 80, 60, 37, 99, 235);
        Assert(preLineTextBounds.HasPixels && preLineTextBounds.Height > 20, "white-space pre-line should collapse repeated spaces while preserving explicit line breaks.");

        var nestedText = "<text x='4' y='28' font-size='28' fill='#ff0000' opacity='.5'><tspan>OO</tspan></text>";
        Assert(SvgRasterRenderer.TryRenderFragment(nestedText, "0 0 100 40", "none", 100, 40, out var nestedTextPixels), "SVG rasterization should render tspan text inside an opacity layer.");
        Assert(MaximumAlpha(nestedTextPixels) is >= 126 and <= 129, "Container text opacity should apply to tspan glyphs exactly once.");
        var nestedSpanOpacity = "<text x='4' y='28' font-size='28' fill='#ff0000'><tspan opacity='.5'>O<tspan>O</tspan></tspan></text>";
        Assert(SvgRasterRenderer.TryRenderFragment(nestedSpanOpacity, "0 0 100 40", "none", 100, 40, out var nestedSpanOpacityPixels), "SVG rasterization should composite nested tspan opacity as a subtree layer.");
        Assert(MaximumAlpha(nestedSpanOpacityPixels) is >= 126 and <= 129, "Nested tspan opacity should affect direct and descendant glyphs exactly once.");
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

    private static bool HasRedAndBlueDominantPixels(byte[] rgba) {
        var red = false;
        var blue = false;
        for (var index = 0; index < rgba.Length; index += 4) {
            if (rgba[index + 3] < 100) continue;
            red |= rgba[index] > rgba[index + 2] + 32;
            blue |= rgba[index + 2] > rgba[index] + 32;
            if (red && blue) return true;
        }
        return false;
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

    private static SvgPixelColorBounds SvgColorBounds(byte[] rgba, int width, int height, byte red, byte green, byte blue) {
        return SvgColorBoundsInRows(rgba, width, 0, height - 1, red, green, blue);
    }

    private static SvgPixelColorBounds SvgColorBoundsInRows(byte[] rgba, int width, int topRow, int bottomRow, byte red, byte green, byte blue) {
        var height = rgba.Length / 4 / width;
        var left = width;
        var top = height;
        var right = -1;
        var bottom = -1;
        for (var y = Math.Max(0, topRow); y <= Math.Min(height - 1, bottomRow); y++) for (var x = 0; x < width; x++) {
            var index = (y * width + x) * 4;
            if (Math.Abs(rgba[index] - red) > 8 || Math.Abs(rgba[index + 1] - green) > 8 || Math.Abs(rgba[index + 2] - blue) > 8 || rgba[index + 3] < 200) continue;
            left = Math.Min(left, x);
            top = Math.Min(top, y);
            right = Math.Max(right, x);
            bottom = Math.Max(bottom, y);
        }
        return new SvgPixelColorBounds(left, top, right, bottom);
    }

    private static SvgPixelColorBounds SvgAlphaBounds(byte[] rgba, int width, int height) {
        var left = width;
        var top = height;
        var right = -1;
        var bottom = -1;
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) {
            if (rgba[(y * width + x) * 4 + 3] < 32) continue;
            left = Math.Min(left, x);
            top = Math.Min(top, y);
            right = Math.Max(right, x);
            bottom = Math.Max(bottom, y);
        }
        return new SvgPixelColorBounds(left, top, right, bottom);
    }

    private readonly struct SvgPixelColorBounds {
        public SvgPixelColorBounds(int left, int top, int right, int bottom) {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public int Left { get; }
        public int Top { get; }
        public int Right { get; }
        public int Bottom { get; }
        public bool HasPixels => Right >= Left && Bottom >= Top;
        public int Width => HasPixels ? Right - Left + 1 : 0;
        public int Height => HasPixels ? Bottom - Top + 1 : 0;
    }
}
