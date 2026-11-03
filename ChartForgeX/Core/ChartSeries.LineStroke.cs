namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Gets or sets immutable stroke overrides for connected lines, area edges, comparison stems, error bars and box outlines.</summary>
    /// <remarks>The style width takes precedence over an explicitly authored <see cref="StrokeWidth"/>, then the family/theme default.
    /// Null restores the legacy width and family paint. Radar and polar OutlineStroke settings take precedence over this series style.
    /// Fills and point markers retain their independent styling.</remarks>
    public ChartStrokeStyle? StrokeStyle { get; set; }

    /// <summary>Sets the series line stroke, or clears it to restore legacy width and family paint.</summary>
    /// <param name="style">The immutable stroke overrides, or null to clear them.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithStrokeStyle(ChartStrokeStyle? style) {
        StrokeStyle = style;
        return this;
    }
}
