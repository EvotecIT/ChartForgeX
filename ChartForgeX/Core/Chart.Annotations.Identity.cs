using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Adds an authored horizontal line whose identity is independent of its y-axis value and caption.</summary>
    /// <param name="id">The identity, unique within this chart's annotations.</param>
    /// <param name="value">The y-axis value.</param>
    /// <param name="label">The optional caption.</param>
    /// <param name="color">An optional explicit color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddHorizontalLine(ChartAnnotationId id, double value, string label = "", ChartColor? color = null) =>
        AddAnnotation(new ChartAnnotation(id, ChartAnnotationKind.HorizontalLine, value, null, label,
            color ?? Options.ResolvedTheme.Text2, 1) { UsesThemeColor = !color.HasValue });

    /// <summary>Adds an authored vertical line whose identity is independent of its x-axis value and caption.</summary>
    /// <param name="id">The identity, unique within this chart's annotations.</param>
    /// <param name="value">The x-axis value.</param>
    /// <param name="label">The optional caption.</param>
    /// <param name="color">An optional explicit color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddVerticalLine(ChartAnnotationId id, double value, string label = "", ChartColor? color = null) =>
        AddAnnotation(new ChartAnnotation(id, ChartAnnotationKind.VerticalLine, value, null, label,
            color ?? Options.ResolvedTheme.Text2, 1) { UsesThemeColor = !color.HasValue });

    /// <summary>Adds an authored horizontal band whose identity is independent of its y-axis interval and caption.</summary>
    /// <param name="id">The identity, unique within this chart's annotations.</param>
    /// <param name="start">The first y-axis value.</param>
    /// <param name="end">The second y-axis value.</param>
    /// <param name="label">The optional caption.</param>
    /// <param name="color">An optional explicit color.</param>
    /// <param name="opacity">The band opacity.</param>
    /// <returns>The current chart.</returns>
    public Chart AddHorizontalBand(ChartAnnotationId id, double start, double end, string label = "", ChartColor? color = null, double opacity = .14) =>
        AddAnnotation(new ChartAnnotation(id, ChartAnnotationKind.HorizontalBand, start, end, label,
            color ?? Options.ResolvedTheme.Axis, opacity));

    /// <summary>Adds an authored vertical band whose identity is independent of its x-axis interval and caption.</summary>
    /// <param name="id">The identity, unique within this chart's annotations.</param>
    /// <param name="start">The first x-axis value.</param>
    /// <param name="end">The second x-axis value.</param>
    /// <param name="label">The optional caption.</param>
    /// <param name="color">An optional explicit color.</param>
    /// <param name="opacity">The band opacity.</param>
    /// <returns>The current chart.</returns>
    public Chart AddVerticalBand(ChartAnnotationId id, double start, double end, string label = "", ChartColor? color = null, double opacity = .14) =>
        AddAnnotation(new ChartAnnotation(id, ChartAnnotationKind.VerticalBand, start, end, label,
            color ?? Options.ResolvedTheme.Axis, opacity));

}
