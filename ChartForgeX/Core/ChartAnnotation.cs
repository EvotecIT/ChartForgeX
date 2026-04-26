using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Represents a static line or band annotation on a chart.
/// </summary>
public sealed class ChartAnnotation {
    /// <summary>
    /// Gets the annotation kind.
    /// </summary>
    public ChartAnnotationKind Kind { get; }

    /// <summary>
    /// Gets the first annotation value.
    /// </summary>
    public double Value { get; }

    /// <summary>
    /// Gets the second annotation value for band annotations.
    /// </summary>
    public double? EndValue { get; }

    /// <summary>
    /// Gets the annotation label.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Gets the annotation color.
    /// </summary>
    public ChartColor Color { get; }

    /// <summary>
    /// Gets the annotation opacity used for band fills.
    /// </summary>
    public double Opacity { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartAnnotation"/> class.
    /// </summary>
    /// <param name="kind">The annotation kind.</param>
    /// <param name="value">The first annotation value.</param>
    /// <param name="endValue">The second annotation value for bands.</param>
    /// <param name="label">The annotation label.</param>
    /// <param name="color">The annotation color.</param>
    /// <param name="opacity">The annotation opacity used for band fills.</param>
    public ChartAnnotation(ChartAnnotationKind kind, double value, double? endValue, string label, ChartColor color, double opacity) {
        Kind = kind;
        Value = value;
        EndValue = endValue;
        Label = label;
        Color = color;
        Opacity = opacity;
    }
}
