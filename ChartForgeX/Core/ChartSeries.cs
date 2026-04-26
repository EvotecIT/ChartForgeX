using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Represents one named series of points in a chart.
/// </summary>
public sealed class ChartSeries {
    /// <summary>
    /// Gets the display name shown in legends.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the series rendering kind.
    /// </summary>
    public ChartSeriesKind Kind { get; }

    /// <summary>
    /// Gets the ordered data points in the series.
    /// </summary>
    public List<ChartPoint> Points { get; } = new();

    /// <summary>
    /// Gets or sets the series color. When null, the chart theme palette is used.
    /// </summary>
    public ChartColor? Color { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether capable renderers should smooth connected line segments.
    /// </summary>
    public bool Smooth { get; set; }

    /// <summary>
    /// Gets or sets the stroke width for line and area series.
    /// </summary>
    public double StrokeWidth { get; set; } = 3;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartSeries"/> class.
    /// </summary>
    /// <param name="name">The display name shown in legends.</param>
    /// <param name="kind">The series rendering kind.</param>
    /// <param name="points">The ordered data points.</param>
    public ChartSeries(string name, ChartSeriesKind kind, IEnumerable<ChartPoint> points) {
        Name = name;
        Kind = kind;
        Points.AddRange(points);
    }
}
