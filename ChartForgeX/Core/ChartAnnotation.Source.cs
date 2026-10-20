using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class ChartAnnotation {
    /// <summary>Gets the source anchor of a note, line segment or channel; axis-spanning guides return null.</summary>
    public ChartPoint? SourceStart { get; }

    /// <summary>Gets the second source endpoint of a line segment or channel; notes and axis-spanning guides return null.</summary>
    public ChartPoint? SourceEnd { get; }

    /// <summary>Gets the channel's signed offset in source Y units; other annotations return zero.</summary>
    /// <remarks>On nonlinear Y axes the two boundaries are mapped independently; they need not be parallel on screen.</remarks>
    public double ChannelOffset { get; }

    /// <summary>Gets the value axis owning source-coordinate Y values. Axis-spanning guides retain their existing physical-axis semantics.</summary>
    public ChartAxisSide SourceYAxis { get; }

    internal bool HasSourceGeometry => SourceStart.HasValue;
    internal bool IsBand => EndValue.HasValue || Kind == ChartAnnotationKind.ParallelChannel;

    private ChartAnnotation(ChartAnnotationKind kind, ChartPoint start, ChartPoint? end, double offset, string label,
        ChartColor color, double opacity, bool showLabel, ChartAnnotationId? id, ChartAxisSide yAxis) {
        ValidateSourcePoint(start, nameof(start));
        if (end.HasValue) ValidateSourcePoint(end.Value, nameof(end));
        if (kind != ChartAnnotationKind.Note && (!end.HasValue || start.X == end.Value.X && start.Y == end.Value.Y))
            throw new ArgumentException("Source line endpoints must differ.", nameof(end));
        ChartGuards.Finite(offset, nameof(offset));
        if (kind == ChartAnnotationKind.ParallelChannel && offset == 0)
            throw new ArgumentOutOfRangeException(nameof(offset), offset, "A channel offset must be nonzero.");
        ChartGuards.Finite(start.Y + offset, nameof(offset));
        if (end.HasValue) ChartGuards.Finite(end.Value.Y + offset, nameof(offset));
        if (!Enum.IsDefined(typeof(ChartAxisSide), yAxis)) throw new ArgumentOutOfRangeException(nameof(yAxis));
        ChartGuards.UnitInterval(opacity, nameof(opacity));
        if (id.HasValue) id.Value.Validate();
        Kind = kind; SourceStart = start; SourceEnd = end; ChannelOffset = offset; SourceYAxis = yAxis;
        Value = start.X; Label = label ?? throw new ArgumentNullException(nameof(label)); Color = color;
        Opacity = opacity; ShowLabel = showLabel; Id = id;
    }

    /// <summary>Creates a Cartesian note anchored to exact source coordinates without expanding the data domain.</summary>
    public static ChartAnnotation Note(ChartPoint anchor, string label, ChartColor color, ChartAnnotationId? id = null,
        ChartAxisSide yAxis = ChartAxisSide.Primary, bool showLabel = true) =>
        new(ChartAnnotationKind.Note, anchor, null, 0, label, color, 1, showLabel, id, yAxis);

    /// <summary>Creates a finite Cartesian source line, clipped to the plot without changing source endpoints or axis bounds.</summary>
    public static ChartAnnotation LineSegment(ChartPoint start, ChartPoint end, string label, ChartColor color,
        ChartAnnotationId? id = null, ChartAxisSide yAxis = ChartAxisSide.Primary, bool showLabel = true) =>
        new(ChartAnnotationKind.LineSegment, start, end, 0, label, color, 1, showLabel, id, yAxis);

    /// <summary>Creates two source-coordinate boundaries separated by a signed Y offset, with translucent fill between them.</summary>
    /// <remarks>The offset uses source units, including on logarithmic axes. It is not a perpendicular screen-distance measurement.</remarks>
    public static ChartAnnotation ParallelChannel(ChartPoint start, ChartPoint end, double yOffset, string label, ChartColor color,
        ChartAnnotationId? id = null, ChartAxisSide yAxis = ChartAxisSide.Primary, double opacity = .14, bool showLabel = true) =>
        new(ChartAnnotationKind.ParallelChannel, start, end, yOffset, label, color, opacity, showLabel, id, yAxis);

    private static void ValidateSourcePoint(ChartPoint point, string name) {
        ChartGuards.Finite(point.X, name + ".X"); ChartGuards.Finite(point.Y, name + ".Y");
    }
}
