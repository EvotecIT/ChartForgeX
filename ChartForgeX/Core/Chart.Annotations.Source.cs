using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Adds a Cartesian note at an exact source coordinate. It follows native resize, viewport and export preparation.</summary>
    public Chart AddNote(ChartPoint anchor, string label, ChartColor? color = null, ChartAnnotationId? id = null,
        ChartAxisSide yAxis = ChartAxisSide.Primary) => AddSourceAnnotation(ChartAnnotation.Note(anchor, label,
            color ?? Options.ResolvedTheme.Text2, id, yAxis), !color.HasValue);

    /// <summary>Adds a finite source-coordinate line without expanding the data domain.</summary>
    public Chart AddLineSegment(ChartPoint start, ChartPoint end, string label = "", ChartColor? color = null,
        ChartAnnotationId? id = null, ChartAxisSide yAxis = ChartAxisSide.Primary) => AddSourceAnnotation(
            ChartAnnotation.LineSegment(start, end, label, color ?? Options.ResolvedTheme.Text2, id, yAxis), !color.HasValue);

    /// <summary>Adds a channel with a signed offset in source Y units. It is clipped to the current plot.</summary>
    public Chart AddParallelChannel(ChartPoint start, ChartPoint end, double yOffset, string label = "", ChartColor? color = null,
        ChartAnnotationId? id = null, ChartAxisSide yAxis = ChartAxisSide.Primary, double opacity = .14) => AddSourceAnnotation(
            ChartAnnotation.ParallelChannel(start, end, yOffset, label, color ?? Options.ResolvedTheme.Text2, id, yAxis, opacity), !color.HasValue);

    private Chart AddSourceAnnotation(ChartAnnotation annotation, bool themeColor) {
        annotation.UsesThemeColor = themeColor;
        if (annotation.Id.HasValue) return AddAuthoredAnnotation(annotation);
        Annotations.Add(annotation); return this;
    }
}
