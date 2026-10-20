using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a horizontal line annotation at a y-axis value.
    /// </summary>
    /// <param name="value">The y-axis value.</param>
    /// <param name="label">The annotation label.</param>
    /// <param name="color">An optional annotation color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddHorizontalLine(double value, string label = "", ChartColor? color = null) {
        ChartGuards.Finite(value, nameof(value));
        AddAnnotation(new ChartAnnotation(ChartAnnotationKind.HorizontalLine, value, null, label, color ?? Options.ResolvedTheme.Text2, 1) { UsesThemeColor = !color.HasValue });
        return this;
    }

    /// <summary>
    /// Adds a vertical line annotation at an x-axis value.
    /// </summary>
    /// <param name="value">The x-axis value.</param>
    /// <param name="label">The annotation label.</param>
    /// <param name="color">An optional annotation color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddVerticalLine(double value, string label = "", ChartColor? color = null) {
        ChartGuards.Finite(value, nameof(value));
        AddAnnotation(new ChartAnnotation(ChartAnnotationKind.VerticalLine, value, null, label, color ?? Options.ResolvedTheme.Text2, 1) { UsesThemeColor = !color.HasValue });
        return this;
    }

    /// <summary>
    /// Adds a horizontal band annotation between two y-axis values.
    /// </summary>
    /// <param name="start">The first y-axis value.</param>
    /// <param name="end">The second y-axis value.</param>
    /// <param name="label">The annotation label.</param>
    /// <param name="color">An optional annotation color.</param>
    /// <param name="opacity">The band opacity.</param>
    /// <returns>The current chart.</returns>
    public Chart AddHorizontalBand(double start, double end, string label = "", ChartColor? color = null, double opacity = 0.14) {
        ChartGuards.Finite(start, nameof(start));
        ChartGuards.Finite(end, nameof(end));
        ChartGuards.UnitInterval(opacity, nameof(opacity));
        AddAnnotation(new ChartAnnotation(ChartAnnotationKind.HorizontalBand, start, end, label, color ?? Options.ResolvedTheme.Axis, opacity));
        return this;
    }

    /// <summary>
    /// Adds a vertical band annotation between two x-axis values.
    /// </summary>
    /// <param name="start">The first x-axis value.</param>
    /// <param name="end">The second x-axis value.</param>
    /// <param name="label">The annotation label.</param>
    /// <param name="color">An optional annotation color.</param>
    /// <param name="opacity">The band opacity.</param>
    /// <returns>The current chart.</returns>
    public Chart AddVerticalBand(double start, double end, string label = "", ChartColor? color = null, double opacity = 0.14) {
        ChartGuards.Finite(start, nameof(start));
        ChartGuards.Finite(end, nameof(end));
        ChartGuards.UnitInterval(opacity, nameof(opacity));
        AddAnnotation(new ChartAnnotation(ChartAnnotationKind.VerticalBand, start, end, label, color ?? Options.ResolvedTheme.Axis, opacity));
        return this;
    }
}
