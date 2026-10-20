using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Represents a static line or band annotation on a chart.
/// </summary>
public sealed partial class ChartAnnotation {
    /// <summary>Gets the authored source identity, or null for an anonymous annotation local to the current chart source.</summary>
    public ChartAnnotationId? Id { get; }

    // Statistical helpers describe source Y values; preparation resolves their physical axis.
    internal bool FollowsSeriesValueAxis { get; set; }
    // Default reference lines follow the prepared theme; explicitly authored colors remain authoritative.
    internal bool UsesThemeColor { get; set; }

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
    /// Gets whether the label is painted beside the annotation. A hidden label remains
    /// available in accessible descriptions and exported semantic metadata.
    /// </summary>
    public bool ShowLabel { get; }

    /// <summary>
    /// Gets the annotation color.
    /// </summary>
    public ChartColor Color { get; }

    /// <summary>
    /// Gets the annotation opacity used for band fills.
    /// </summary>
    public double Opacity { get; }

    /// <summary>Gets or sets optional reference-line stroke overrides. Band fills retain their own color and opacity.</summary>
    public ChartStrokeStyle? StrokeStyle { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartAnnotation"/> class.
    /// </summary>
    /// <param name="kind">The annotation kind.</param>
    /// <param name="value">The first annotation value.</param>
    /// <param name="endValue">The second annotation value for bands.</param>
    /// <param name="label">The annotation label.</param>
    /// <param name="color">The annotation color.</param>
    /// <param name="opacity">The annotation opacity used for band fills.</param>
    /// <param name="showLabel">Whether to paint the label. False retains its accessible and semantic content.</param>
    public ChartAnnotation(ChartAnnotationKind kind, double value, double? endValue, string label, ChartColor color, double opacity, bool showLabel = true) {
        if (!Enum.IsDefined(typeof(ChartAnnotationKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown annotation kind.");
        if (kind is ChartAnnotationKind.Note or ChartAnnotationKind.LineSegment or ChartAnnotationKind.ParallelChannel)
            throw new ArgumentException("Source-coordinate annotations require a source geometry factory.", nameof(kind));
        ChartGuards.Finite(value, nameof(value));
        if (endValue.HasValue) ChartGuards.Finite(endValue.Value, nameof(endValue));
        if (kind == ChartAnnotationKind.HorizontalBand || kind == ChartAnnotationKind.VerticalBand) {
            if (!endValue.HasValue) throw new ArgumentException("Band annotations require an end value.", nameof(endValue));
            if (Math.Abs(endValue.Value - value) < double.Epsilon) throw new ArgumentOutOfRangeException(nameof(endValue), endValue.Value, "Band annotation end value must differ from the start value.");
        } else if (endValue.HasValue) {
            throw new ArgumentException("Line annotations must not specify an end value.", nameof(endValue));
        }

        ChartGuards.UnitInterval(opacity, nameof(opacity));
        Kind = kind;
        Value = value;
        EndValue = endValue;
        Label = label ?? throw new ArgumentNullException(nameof(label));
        ShowLabel = showLabel;
        Color = color;
        Opacity = opacity;
    }

    /// <summary>Creates an annotation whose identity survives source replacement and collection reordering.</summary>
    /// <param name="id">The authored identity, unique among annotations within one chart.</param>
    /// <param name="kind">The annotation kind.</param>
    /// <param name="value">The first annotation value.</param>
    /// <param name="endValue">The second annotation value for bands.</param>
    /// <param name="label">The annotation caption, independent of its identity.</param>
    /// <param name="color">The annotation color.</param>
    /// <param name="opacity">The band fill opacity.</param>
    /// <param name="showLabel">Whether to paint the caption while retaining its semantic content.</param>
    public ChartAnnotation(ChartAnnotationId id, ChartAnnotationKind kind, double value, double? endValue, string label,
        ChartColor color, double opacity, bool showLabel = true) : this(kind, value, endValue, label, color, opacity, showLabel) {
        id.Validate();
        Id = id;
    }
}
