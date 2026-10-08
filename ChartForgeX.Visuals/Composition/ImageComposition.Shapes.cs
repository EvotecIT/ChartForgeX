using System;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.Composition;

/// <summary>Defines how the two ends of an open stroke are finished.</summary>
public enum ImageLineCap {
    /// <summary>The stroke stops exactly at its end points.</summary>
    Butt,
    /// <summary>Each end is finished with a half disc as wide as the stroke.</summary>
    Round,
    /// <summary>Each end is extended by half the stroke thickness.</summary>
    Square
}

public sealed partial class ImageComposition {
    /// <summary>Draws a filled, antialiased circle.</summary>
    public ImageComposition FillCircle(double centerX, double centerY, double radius, ChartColor color) =>
        FillEllipse(centerX, centerY, radius, radius, color);

    /// <summary>Draws an antialiased circle outline centered on the radius.</summary>
    public ImageComposition StrokeCircle(double centerX, double centerY, double radius, ChartColor color, double thickness = 1) =>
        StrokeEllipse(centerX, centerY, radius, radius, color, thickness);

    /// <summary>Draws a filled, antialiased ellipse.</summary>
    public ImageComposition FillEllipse(double centerX, double centerY, double radiusX, double radiusY, ChartColor color) {
        ValidateEllipse(centerX, centerY, radiusX, radiusY);
        _canvas.FillEllipse(centerX, centerY, radiusX, radiusY, color);
        return this;
    }

    /// <summary>Draws an antialiased ellipse outline centered on the radii.</summary>
    public ImageComposition StrokeEllipse(double centerX, double centerY, double radiusX, double radiusY, ChartColor color, double thickness = 1) {
        ValidateEllipse(centerX, centerY, radiusX, radiusY);
        ValidatePositive(thickness, nameof(thickness));
        _canvas.StrokeEllipse(centerX, centerY, radiusX, radiusY, color, thickness);
        return this;
    }

    /// <summary>
    /// Draws an antialiased circular arc. Angles are in degrees, measured clockwise on screen from
    /// the positive x axis (three o'clock), so -90 starts at twelve o'clock. A positive sweep runs
    /// clockwise; a sweep of 360 degrees or more draws the whole ring.
    /// </summary>
    public ImageComposition DrawArc(double centerX, double centerY, double radius, double startAngle, double sweepAngle, ChartColor color, double thickness = 1, ImageLineCap lineCap = ImageLineCap.Round) {
        ValidateFinite(centerX, nameof(centerX));
        ValidateFinite(centerY, nameof(centerY));
        ValidatePositive(radius, nameof(radius));
        ValidateFinite(startAngle, nameof(startAngle));
        ValidateFinite(sweepAngle, nameof(sweepAngle));
        ValidatePositive(thickness, nameof(thickness));
        if (!Enum.IsDefined(typeof(ImageLineCap), lineCap)) throw new ArgumentOutOfRangeException(nameof(lineCap), lineCap, "Unknown line cap.");
        var cap = lineCap == ImageLineCap.Round ? RasterLineCap.Round : lineCap == ImageLineCap.Square ? RasterLineCap.Square : RasterLineCap.Butt;
        _canvas.StrokeArc(centerX, centerY, radius, startAngle * Math.PI / 180, sweepAngle * Math.PI / 180, color, thickness, cap);
        return this;
    }

    /// <summary>
    /// Draws a progress ring: a full track with a round-capped arc over it that covers
    /// <paramref name="fraction"/> of the ring, clockwise from <paramref name="startAngle"/> degrees
    /// (-90 is twelve o'clock). A fraction of zero draws only the track; one or more fills the ring.
    /// </summary>
    public ImageComposition DrawProgressRing(double centerX, double centerY, double radius, double thickness, double fraction, ChartColor trackColor, ChartColor color, double startAngle = -90) {
        ValidateFinite(fraction, nameof(fraction));
        StrokeCircle(centerX, centerY, radius, trackColor, thickness);
        fraction = Math.Max(0, Math.Min(1, fraction));
        return fraction <= 0 ? this : DrawArc(centerX, centerY, radius, startAngle, fraction * 360, color, thickness, ImageLineCap.Round);
    }

    /// <summary>
    /// Fills a rectangle with a two-color linear gradient. The angle is in degrees: 0 runs left to
    /// right and 90 (the default) runs top to bottom. A positive <paramref name="radius"/> rounds the corners.
    /// </summary>
    public ImageComposition FillRectangleLinearGradient(double x, double y, double width, double height, ChartColor startColor, ChartColor endColor, double angle = 90, double radius = 0) {
        ValidateRect(x, y, width, height);
        ValidateFinite(angle, nameof(angle));
        ValidateNonNegative(radius, nameof(radius));
        var radians = angle * Math.PI / 180;
        var directionX = Math.Cos(radians);
        var directionY = Math.Sin(radians);
        // Like a CSS gradient line: long enough that the corners take the two end colors.
        var half = (Math.Abs(width * directionX) + Math.Abs(height * directionY)) / 2;
        var midX = x + width / 2;
        var midY = y + height / 2;
        _canvas.FillRectLinearGradient(x, y, width, height, radius,
            new ChartPoint(midX - directionX * half, midY - directionY * half),
            new ChartPoint(midX + directionX * half, midY + directionY * half),
            TwoStops(startColor, endColor));
        return this;
    }

    /// <summary>
    /// Fills a rectangle with a two-color radial gradient: <paramref name="innerColor"/> at the gradient
    /// center fading to <paramref name="outerColor"/> at <paramref name="gradientRadius"/> and beyond.
    /// The center is in composition coordinates and may lie outside the rectangle. A positive
    /// <paramref name="radius"/> rounds the corners.
    /// </summary>
    public ImageComposition FillRectangleRadialGradient(double x, double y, double width, double height, double gradientCenterX, double gradientCenterY, double gradientRadius, ChartColor innerColor, ChartColor outerColor, double radius = 0) {
        ValidateRect(x, y, width, height);
        ValidateFinite(gradientCenterX, nameof(gradientCenterX));
        ValidateFinite(gradientCenterY, nameof(gradientCenterY));
        ValidatePositive(gradientRadius, nameof(gradientRadius));
        ValidateNonNegative(radius, nameof(radius));
        _canvas.FillRectRadialGradient(x, y, width, height, radius, new ChartPoint(gradientCenterX, gradientCenterY), gradientRadius, TwoStops(innerColor, outerColor));
        return this;
    }

    private static RasterGradientStop[] TwoStops(ChartColor first, ChartColor second) =>
        new[] { new RasterGradientStop(0, first), new RasterGradientStop(1, second) };

    private static void ValidateEllipse(double centerX, double centerY, double radiusX, double radiusY) {
        ValidateFinite(centerX, nameof(centerX));
        ValidateFinite(centerY, nameof(centerY));
        ValidatePositive(radiusX, nameof(radiusX));
        ValidatePositive(radiusY, nameof(radiusY));
    }
}
