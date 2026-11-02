using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

/// <summary>The source used to paint a chart mark's interior.</summary>
public enum ChartFillKind {
    /// <summary>A straight gradient in normalized mark coordinates.</summary>
    LinearGradient,
    /// <summary>An elliptical gradient in normalized mark coordinates.</summary>
    RadialGradient,
    /// <summary>An embedded image stretched to the mark bounds.</summary>
    Image,
    /// <summary>An embedded image repeated at an authored logical tile size.</summary>
    Pattern
}

/// <summary>An immutable color stop. Offsets range from zero to one.</summary>
public readonly struct ChartGradientStop {
    /// <summary>Creates a stop at the given fraction of the gradient ramp.</summary>
    public ChartGradientStop(double offset, ChartColor color) {
        ChartGuards.Finite(offset, nameof(offset));
        if (offset < 0 || offset > 1) throw new ArgumentOutOfRangeException(nameof(offset));
        Offset = offset; Color = color;
    }
    /// <summary>Gets the position along the ramp.</summary>
    public double Offset { get; }
    /// <summary>Gets the color, including its alpha channel.</summary>
    public ChartColor Color { get; }
}

/// <summary>An immutable, dependency-free fill shared by chart SVG and native raster output.</summary>
/// <remarks>Gradient coordinates are fractions of each mark's bounds: (0,0) is top-left and (1,1) is bottom-right.
/// Images are embedded pixel snapshots; no files, URLs, browser scripts or external I/O are required.</remarks>
public sealed class ChartFill {
    private ChartFill(ChartFillKind kind, ChartPoint start, ChartPoint end, double radius,
        ChartGradientStop[]? stops, VisualSceneImageData? image, double tileWidth, double tileHeight) {
        Kind = kind; Start = start; End = end; Radius = radius; ImageData = image;
        TileWidth = tileWidth; TileHeight = tileHeight;
        var copy = stops == null ? Array.Empty<ChartGradientStop>() : (ChartGradientStop[])stops.Clone();
        for (var i = 1; i < copy.Length; i++)
            if (copy[i].Offset < copy[i - 1].Offset) throw new ArgumentException("Gradient stops must be ordered.", nameof(stops));
        Stops = Array.AsReadOnly(copy);
    }
    /// <summary>Gets the paint source.</summary>
    public ChartFillKind Kind { get; }
    /// <summary>Gets the linear start or radial center in normalized mark coordinates.</summary>
    public ChartPoint Start { get; }
    /// <summary>Gets the linear endpoint in normalized mark coordinates.</summary>
    public ChartPoint End { get; }
    /// <summary>Gets the radial radius as a fraction of mark width and height.</summary>
    public double Radius { get; }
    /// <summary>Gets the detached ordered stops; image fills have no stops.</summary>
    public IReadOnlyList<ChartGradientStop> Stops { get; }
    /// <summary>Gets the repeating tile width in logical chart units; zero for other fill kinds.</summary>
    public double TileWidth { get; }
    /// <summary>Gets the repeating tile height in logical chart units; zero for other fill kinds.</summary>
    public double TileHeight { get; }
    internal VisualSceneImageData? ImageData { get; }

    /// <summary>Creates a straight gradient with at least two ordered stops.</summary>
    public static ChartFill LinearGradient(ChartPoint start, ChartPoint end, params ChartGradientStop[] stops) {
        Point(start, nameof(start)); Point(end, nameof(end)); GradientStops(stops);
        if (start.X == end.X && start.Y == end.Y) throw new ArgumentException("Gradient endpoints must differ.", nameof(end));
        return new ChartFill(ChartFillKind.LinearGradient, start, end, 0, stops, null, 0, 0);
    }
    /// <summary>Creates an elliptical gradient whose center and radius use normalized mark coordinates.</summary>
    public static ChartFill RadialGradient(ChartPoint center, double radius, params ChartGradientStop[] stops) {
        Point(center, nameof(center)); VisualSize.Positive(radius, nameof(radius)); GradientStops(stops);
        return new ChartFill(ChartFillKind.RadialGradient, center, default, radius, stops, null, 0, 0);
    }
    /// <summary>Copies RGBA pixels and stretches the embedded image to each filled mark's bounds.</summary>
    public static ChartFill Image(RgbaImage image) =>
        new(ChartFillKind.Image, default, default, 0, null, new VisualSceneImageData(image), 0, 0);

    /// <summary>Copies RGBA pixels and repeats them from the mark's top-left at the specified logical tile size.</summary>
    public static ChartFill Pattern(RgbaImage image, double tileWidth, double tileHeight) {
        VisualSize.Positive(tileWidth, nameof(tileWidth)); VisualSize.Positive(tileHeight, nameof(tileHeight));
        return new ChartFill(ChartFillKind.Pattern, default, default, 0, null, new VisualSceneImageData(image), tileWidth, tileHeight);
    }
    private static void GradientStops(ChartGradientStop[] stops) {
        if (stops == null) throw new ArgumentNullException(nameof(stops));
        if (stops.Length < 2) throw new ArgumentException("A gradient needs at least two ordered stops.", nameof(stops));
    }
    private static void Point(ChartPoint point, string parameter) {
        ChartGuards.Finite(point.X, parameter); ChartGuards.Finite(point.Y, parameter);
    }
}
