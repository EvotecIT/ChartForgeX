namespace ChartForgeX.Primitives;

/// <summary>
/// Defines a rectangular region in chart pixel space.
/// </summary>
public readonly struct ChartRect {
    /// <summary>
    /// Gets the left coordinate.
    /// </summary>
    public readonly double X;

    /// <summary>
    /// Gets the top coordinate.
    /// </summary>
    public readonly double Y;

    /// <summary>
    /// Gets the width in pixels.
    /// </summary>
    public readonly double Width;

    /// <summary>
    /// Gets the height in pixels.
    /// </summary>
    public readonly double Height;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartRect"/> struct.
    /// </summary>
    /// <param name="x">The left coordinate.</param>
    /// <param name="y">The top coordinate.</param>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    public ChartRect(double x, double y, double width, double height) {
        ChartPrimitiveGuards.Finite(x, nameof(x));
        ChartPrimitiveGuards.Finite(y, nameof(y));
        ChartPrimitiveGuards.NonNegativeFinite(width, nameof(width));
        ChartPrimitiveGuards.NonNegativeFinite(height, nameof(height));
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Gets the left edge.
    /// </summary>
    public double Left => X;

    /// <summary>
    /// Gets the right edge.
    /// </summary>
    public double Right => X + Width;

    /// <summary>
    /// Gets the top edge.
    /// </summary>
    public double Top => Y;

    /// <summary>
    /// Gets the bottom edge.
    /// </summary>
    public double Bottom => Y + Height;
}
