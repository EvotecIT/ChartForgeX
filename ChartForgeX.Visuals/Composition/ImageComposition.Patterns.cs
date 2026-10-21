using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Composition;

public sealed partial class ImageComposition {
    /// <summary>Overlays evenly spaced horizontal and vertical lines across this composition.</summary>
    /// <param name="color">Line color, including its alpha. Intersections receive both lines.</param>
    /// <param name="spacing">Distance between lines in pixels; at least one.</param>
    /// <param name="thickness">Line thickness in pixels.</param>
    /// <param name="offset">First line coordinate, from zero up to the spacing. Defaults to half the spacing, rounded down.</param>
    public ImageComposition FillGridPattern(ChartColor color, int spacing = 48, double thickness = 1, int? offset = null) {
        ValidatePatternSpacing(spacing);
        ValidatePositive(thickness, nameof(thickness));
        var phase = offset ?? spacing / 2;
        ValidatePatternOffset(phase, spacing);
        if (color.A == 0) return this;
        // Double coordinates avoid integer overflow when a large spacing steps past the artboard.
        for (double x = phase; x < Width; x += spacing) FillRectangle(x, 0, thickness, Height, color);
        for (double y = phase; y < Height; y += spacing) FillRectangle(0, y, Width, thickness, color);
        return this;
    }

    /// <summary>Overlays a regular grid of antialiased dots across this composition.</summary>
    /// <param name="color">Dot color, including its alpha.</param>
    /// <param name="spacing">Distance between dot centers in pixels; at least one.</param>
    /// <param name="radius">Dot radius in pixels.</param>
    /// <param name="offset">First center on both axes, from zero up to the spacing. Defaults to half the spacing.</param>
    public ImageComposition FillDotPattern(ChartColor color, int spacing = 48, double radius = 1, double? offset = null) {
        ValidatePatternSpacing(spacing);
        ValidatePositive(radius, nameof(radius));
        var phase = offset ?? spacing / 2d;
        ValidatePatternOffset(phase, spacing);
        if (color.A == 0) return this;
        for (double y = phase; y < Height; y += spacing) {
            for (double x = phase; x < Width; x += spacing) FillCircle(x, y, radius, color);
        }
        return this;
    }

    private static void ValidatePatternSpacing(int spacing) {
        if (spacing < 1) throw new ArgumentOutOfRangeException(nameof(spacing), spacing, "Pattern spacing must be at least one pixel.");
    }

    private static void ValidatePatternOffset(double offset, int spacing) {
        if (double.IsNaN(offset) || double.IsInfinity(offset) || offset < 0 || offset >= spacing)
            throw new ArgumentOutOfRangeException(nameof(offset), offset, "Pattern offset must be finite, nonnegative and smaller than its spacing.");
    }
}
