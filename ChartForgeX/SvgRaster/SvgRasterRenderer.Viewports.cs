using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.SvgRaster;

internal static partial class SvgRasterRenderer {
    private static SvgRasterMatrix ApplyNestedSvgViewport(SvgRasterElement element, SvgRasterMatrix matrix, SvgRasterNestedViewport viewport) {
        var viewBox = element.Get("viewBox");
        var translated = matrix.Multiply(SvgRasterMatrix.Translate(viewport.X, viewport.Y));
        if (string.IsNullOrWhiteSpace(viewBox)) return translated;
        var parsed = SvgRasterViewBox.Parse(viewBox);
        return translated.Multiply(SvgRasterMatrix.FromFit(parsed, viewport.Width, viewport.Height, element.Get("preserveAspectRatio")));
    }

    private static SvgRasterNestedViewport ResolveNestedSvgViewport(SvgRasterElement element, SvgRasterViewport parent) {
        var viewBox = element.Get("viewBox");
        SvgRasterViewBox? parsed = string.IsNullOrWhiteSpace(viewBox) ? null : SvgRasterViewBox.Parse(viewBox!);
        var x = ResolveViewportLength(element.Get("x"), 0, parent.Width);
        var y = ResolveViewportLength(element.Get("y"), 0, parent.Height);
        var width = ResolveViewportLength(element.Get("width"), parent.Width, parent.Width);
        var height = ResolveViewportLength(element.Get("height"), parent.Height, parent.Height);
        return new SvgRasterNestedViewport(x, y, width, height, new SvgRasterViewport(parsed?.Width ?? width, parsed?.Height ?? height));
    }

    private static double ResolveViewportLength(string? value, double fallback, double percentageExtent) {
        if (string.IsNullOrWhiteSpace(value) || string.Equals(value!.Trim(), "auto", StringComparison.OrdinalIgnoreCase)) return fallback;
        return ResolveMaskLength(value, fallback, percentageExtent);
    }

    private static List<ChartPoint> MaskRegion(SvgRasterMask definition, SvgRasterMatrix matrix, PixelBounds bounds, SvgRasterViewport viewport) {
        if (!definition.UserSpaceOnUse) {
            var x = bounds.Left + ResolveMaskLength(definition.X, -0.1, 1) * bounds.Width;
            var y = bounds.Top + ResolveMaskLength(definition.Y, -0.1, 1) * bounds.Height;
            var regionWidth = ResolveMaskLength(definition.Width, 1.2, 1) * bounds.Width;
            var regionHeight = ResolveMaskLength(definition.Height, 1.2, 1) * bounds.Height;
            return regionWidth <= 0 || regionHeight <= 0 ? new List<ChartPoint>() : TransformRing(RectRing(x, y, regionWidth, regionHeight), matrix);
        }

        var userX = ResolveMaskLength(definition.X, -0.1 * viewport.Width, viewport.Width);
        var userY = ResolveMaskLength(definition.Y, -0.1 * viewport.Height, viewport.Height);
        var userWidth = ResolveMaskLength(definition.Width, 1.2 * viewport.Width, viewport.Width);
        var userHeight = ResolveMaskLength(definition.Height, 1.2 * viewport.Height, viewport.Height);
        return userWidth <= 0 || userHeight <= 0
            ? new List<ChartPoint>()
            : TransformRing(RectRing(userX, userY, userWidth, userHeight), matrix);
    }

    private static double ResolveMaskLength(string? value, double fallback, double percentageExtent) {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        var trimmed = value!.Trim();
        var percent = trimmed.EndsWith("%", StringComparison.Ordinal);
        if (percent) trimmed = trimmed.Substring(0, trimmed.Length - 1);
        if (percent) {
            if (!double.TryParse(trimmed, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var percentage)) return fallback;
            return percentage / 100.0 * percentageExtent;
        }
        return SvgRasterViewBox.TryParseAbsoluteLength(trimmed, out var parsed) ? parsed : fallback;
    }

    private static bool TryVisibleBounds(byte[] pixels, int width, int height, SvgRasterMatrix matrix, out PixelBounds bounds) {
        if (!matrix.TryInvert(out var inverse)) {
            bounds = default;
            return false;
        }
        var left = double.PositiveInfinity;
        var top = double.PositiveInfinity;
        var right = double.NegativeInfinity;
        var bottom = double.NegativeInfinity;
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) {
            if (pixels[(y * width + x) * 4 + 3] == 0) continue;
            IncludePoint(inverse.Transform(new ChartPoint(x, y)), ref left, ref top, ref right, ref bottom);
            IncludePoint(inverse.Transform(new ChartPoint(x + 1, y)), ref left, ref top, ref right, ref bottom);
            IncludePoint(inverse.Transform(new ChartPoint(x + 1, y + 1)), ref left, ref top, ref right, ref bottom);
            IncludePoint(inverse.Transform(new ChartPoint(x, y + 1)), ref left, ref top, ref right, ref bottom);
        }
        bounds = new PixelBounds(left, top, right >= left ? right - left : 0, bottom >= top ? bottom - top : 0);
        return !double.IsInfinity(left) && right >= left && bottom >= top;
    }

    private static void IncludePoint(ChartPoint point, ref double left, ref double top, ref double right, ref double bottom) {
        left = Math.Min(left, point.X);
        top = Math.Min(top, point.Y);
        right = Math.Max(right, point.X);
        bottom = Math.Max(bottom, point.Y);
    }

    private readonly struct PixelBounds {
        public PixelBounds(double left, double top, double width, double height) {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }

        public double Left { get; }
        public double Top { get; }
        public double Width { get; }
        public double Height { get; }
    }

    private readonly struct SvgRasterViewport {
        public SvgRasterViewport(double width, double height) {
            Width = width;
            Height = height;
        }

        public double Width { get; }
        public double Height { get; }
    }

    private readonly struct SvgRasterNestedViewport {
        public SvgRasterNestedViewport(double x, double y, double width, double height, SvgRasterViewport userViewport) {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            UserViewport = userViewport;
        }

        public double X { get; }
        public double Y { get; }
        public double Width { get; }
        public double Height { get; }
        public SvgRasterViewport UserViewport { get; }

        public List<ChartPoint> Contour(SvgRasterMatrix matrix) => TransformRing(RectRing(X, Y, Width, Height), matrix);
    }
}
