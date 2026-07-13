using System;

namespace ChartForgeX.SvgRaster;

/// <summary>Resolves SVG image preserveAspectRatio semantics into destination and source rectangles.</summary>
internal readonly struct SvgRasterImagePlacement {
    private SvgRasterImagePlacement(int x, int y, int width, int height, double sourceX, double sourceY, double sourceWidth, double sourceHeight) {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        SourceX = sourceX;
        SourceY = sourceY;
        SourceWidth = sourceWidth;
        SourceHeight = sourceHeight;
    }

    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }
    public double SourceX { get; }
    public double SourceY { get; }
    public double SourceWidth { get; }
    public double SourceHeight { get; }

    /// <summary>Maps an intrinsic source image into an SVG image viewport using meet, slice, alignment, or stretch behavior.</summary>
    public static SvgRasterImagePlacement Resolve(int destinationWidth, int destinationHeight, int sourceWidth, int sourceHeight, string? preserveAspectRatio) {
        var aspectRatio = SvgRasterPreserveAspectRatio.Parse(preserveAspectRatio);
        if (aspectRatio.Stretch) {
            return new SvgRasterImagePlacement(0, 0, destinationWidth, destinationHeight, 0, 0, sourceWidth, sourceHeight);
        }

        var scaleX = destinationWidth / (double)sourceWidth;
        var scaleY = destinationHeight / (double)sourceHeight;
        var scale = aspectRatio.Slice ? Math.Max(scaleX, scaleY) : Math.Min(scaleX, scaleY);
        if (!aspectRatio.Slice) {
            var width = Math.Max(1, (int)Math.Round(sourceWidth * scale));
            var height = Math.Max(1, (int)Math.Round(sourceHeight * scale));
            return new SvgRasterImagePlacement(
                (int)Math.Round(aspectRatio.AlignX(destinationWidth - width)),
                (int)Math.Round(aspectRatio.AlignY(destinationHeight - height)),
                width,
                height,
                0,
                0,
                sourceWidth,
                sourceHeight);
        }

        var sourceRectWidth = destinationWidth / scale;
        var sourceRectHeight = destinationHeight / scale;
        return new SvgRasterImagePlacement(
            0,
            0,
            destinationWidth,
            destinationHeight,
            aspectRatio.AlignX(sourceWidth - sourceRectWidth),
            aspectRatio.AlignY(sourceHeight - sourceRectHeight),
            sourceRectWidth,
            sourceRectHeight);
    }
}
