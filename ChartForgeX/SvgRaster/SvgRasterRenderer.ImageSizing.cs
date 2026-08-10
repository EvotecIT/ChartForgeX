using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.SvgRaster;

internal static partial class SvgRasterRenderer {
    private const long MinimumTransformedImagePixels = 1024L * 1024L;
    private const long MaximumTransformedImagePixels = 8L * 1024L * 1024L;

    private static (int Width, int Height) ResolveTransformedImageDimensions(ChartPoint origin, ChartPoint xAxis, ChartPoint yAxis, int canvasWidth, int canvasHeight) {
        var maximum = Math.Max(1, checked(Math.Max(canvasWidth, canvasHeight) * 2));
        var requestedWidth = Math.Max(1D, Math.Min(maximum, Math.Ceiling(ImageAxisLength(origin, xAxis))));
        var requestedHeight = Math.Max(1D, Math.Min(maximum, Math.Ceiling(ImageAxisLength(origin, yAxis))));
        var visiblePixels = checked((long)canvasWidth * canvasHeight);
        var pixelBudget = Math.Min(MaximumTransformedImagePixels, Math.Max(MinimumTransformedImagePixels, checked(visiblePixels * 4L)));
        var requestedPixels = requestedWidth * requestedHeight;
        var scale = requestedPixels > pixelBudget ? Math.Sqrt(pixelBudget / requestedPixels) : 1D;
        return (
            Math.Max(1, (int)Math.Floor(requestedWidth * scale)),
            Math.Max(1, (int)Math.Floor(requestedHeight * scale)));
    }

    private static double ImageAxisLength(ChartPoint first, ChartPoint second) {
        var deltaX = second.X - first.X;
        var deltaY = second.Y - first.Y;
        return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }
}
