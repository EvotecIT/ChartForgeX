namespace ChartForgeX.Primitives;

/// <summary>
/// Defines the spacing between the chart edge and plot area.
/// </summary>
public readonly struct ChartPadding {
    /// <summary>
    /// Gets the left padding in pixels.
    /// </summary>
    public readonly double Left;

    /// <summary>
    /// Gets the top padding in pixels.
    /// </summary>
    public readonly double Top;

    /// <summary>
    /// Gets the right padding in pixels.
    /// </summary>
    public readonly double Right;

    /// <summary>
    /// Gets the bottom padding in pixels.
    /// </summary>
    public readonly double Bottom;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartPadding"/> struct.
    /// </summary>
    /// <param name="left">The left padding in pixels.</param>
    /// <param name="top">The top padding in pixels.</param>
    /// <param name="right">The right padding in pixels.</param>
    /// <param name="bottom">The bottom padding in pixels.</param>
    public ChartPadding(double left, double top, double right, double bottom) { Left = left; Top = top; Right = right; Bottom = bottom; }

    /// <summary>
    /// Creates padding with the same value on every side.
    /// </summary>
    /// <param name="value">The padding in pixels.</param>
    /// <returns>A padding value with equal sides.</returns>
    public static ChartPadding All(double value) => new(value, value, value, value);
}
