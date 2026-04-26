namespace ChartForgeX.Primitives;

/// <summary>
/// Defines the pixel dimensions of a rendered chart.
/// </summary>
public readonly struct ChartSize {
    /// <summary>
    /// Gets the chart width in pixels.
    /// </summary>
    public readonly int Width;

    /// <summary>
    /// Gets the chart height in pixels.
    /// </summary>
    public readonly int Height;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartSize"/> struct.
    /// </summary>
    /// <param name="width">The chart width in pixels.</param>
    /// <param name="height">The chart height in pixels.</param>
    public ChartSize(int width, int height) { Width = width; Height = height; }
}
