using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Highlights a specific x-axis tick label with the supplied color.
    /// </summary>
    /// <param name="value">The x-axis value whose displayed label should be highlighted.</param>
    /// <param name="color">The highlight color. When omitted, the first theme palette color is used.</param>
    /// <returns>The current chart.</returns>
    public Chart WithHighlightedXAxisLabel(double value, ChartColor? color = null) {
        ChartGuards.Finite(value, nameof(value));
        Options.XAxisLabelHighlights[value] = color ?? Options.Theme.Palette[0];
        return this;
    }

    /// <summary>
    /// Highlights a specific x-axis tick label with a color from the current theme palette.
    /// </summary>
    /// <param name="value">The x-axis value whose displayed label should be highlighted.</param>
    /// <param name="paletteIndex">The zero-based theme palette index. Values greater than the palette length wrap.</param>
    /// <returns>The current chart.</returns>
    public Chart WithHighlightedXAxisLabel(double value, int paletteIndex) => WithHighlightedXAxisLabel(value, PaletteColor(paletteIndex));

    /// <summary>
    /// Highlights an x-axis category label and draws guide lines around that category.
    /// </summary>
    /// <param name="value">The x-axis category value to focus.</param>
    /// <param name="halfWidth">Half of the focused category width in axis units.</param>
    /// <param name="color">The focus color. When omitted, the first theme palette color is used.</param>
    /// <returns>The current chart.</returns>
    public Chart WithFocusedXAxisCategory(double value, double halfWidth = 0.5, ChartColor? color = null) {
        ChartGuards.Finite(value, nameof(value));
        ChartGuards.Finite(halfWidth, nameof(halfWidth));
        if (halfWidth <= 0) throw new ArgumentOutOfRangeException(nameof(halfWidth), halfWidth, "Category focus width must be positive.");
        var focusColor = color ?? Options.Theme.Palette[0];
        WithHighlightedXAxisLabel(value, focusColor);
        var left = value - halfWidth;
        var right = value + halfWidth;
        AddXAxisFocusGuideLine(left, focusColor);
        AddXAxisFocusGuideLine(right, focusColor);
        return this;
    }

    /// <summary>
    /// Highlights an x-axis category label and draws guide lines around that category using a color from the current theme palette.
    /// </summary>
    /// <param name="value">The x-axis category value to focus.</param>
    /// <param name="paletteIndex">The zero-based theme palette index. Values greater than the palette length wrap.</param>
    /// <param name="halfWidth">Half of the focused category width in axis units.</param>
    /// <returns>The current chart.</returns>
    public Chart WithFocusedXAxisCategory(double value, int paletteIndex, double halfWidth = 0.5) => WithFocusedXAxisCategory(value, halfWidth, PaletteColor(paletteIndex));

    /// <summary>
    /// Highlights a contiguous x-axis range with a translucent band and matching labels when explicit labels exist.
    /// </summary>
    /// <param name="start">The first x-axis value in the highlighted range.</param>
    /// <param name="end">The second x-axis value in the highlighted range.</param>
    /// <param name="color">The highlight color. When omitted, the first theme palette color is used.</param>
    /// <param name="opacity">The highlight band opacity.</param>
    /// <param name="label">The range metadata label.</param>
    /// <returns>The current chart.</returns>
    public Chart WithHighlightedXAxisRange(double start, double end, ChartColor? color = null, double opacity = 0.10, string label = "selected-window") {
        ChartGuards.Finite(start, nameof(start));
        ChartGuards.Finite(end, nameof(end));
        ChartGuards.UnitInterval(opacity, nameof(opacity));
        var rangeColor = color ?? Options.Theme.Palette[0];
        var annotation = new ChartAnnotation(ChartAnnotationKind.VerticalBand, start, end, label, rangeColor, opacity);
        Annotations.Add(annotation);
        Options.XAxisFocusGuideAnnotations.Add(annotation);

        var minimum = Math.Min(start, end);
        var maximum = Math.Max(start, end);
        foreach (var axisLabel in Options.XAxisLabels) {
            if (axisLabel.Value >= minimum && axisLabel.Value <= maximum) Options.XAxisLabelHighlights[axisLabel.Value] = rangeColor;
        }

        return this;
    }

    /// <summary>
    /// Highlights a contiguous x-axis range using a color from the current theme palette.
    /// </summary>
    /// <param name="start">The first x-axis value in the highlighted range.</param>
    /// <param name="end">The second x-axis value in the highlighted range.</param>
    /// <param name="paletteIndex">The zero-based theme palette index. Values greater than the palette length wrap.</param>
    /// <param name="opacity">The highlight band opacity.</param>
    /// <param name="label">The range metadata label.</param>
    /// <returns>The current chart.</returns>
    public Chart WithHighlightedXAxisRange(double start, double end, int paletteIndex, double opacity = 0.10, string label = "selected-window") => WithHighlightedXAxisRange(start, end, PaletteColor(paletteIndex), opacity, label);

    /// <summary>
    /// Clears all x-axis tick label highlights.
    /// </summary>
    /// <returns>The current chart.</returns>
    public Chart ClearHighlightedXAxisLabels() {
        Options.XAxisLabelHighlights.Clear();
        if (Options.XAxisFocusGuideAnnotations.Count > 0) {
            Annotations.RemoveAll(annotation => Options.XAxisFocusGuideAnnotations.Contains(annotation));
            Options.XAxisFocusGuideAnnotations.Clear();
        }

        return this;
    }

    private void AddXAxisFocusGuideLine(double value, ChartColor color) {
        var annotation = new ChartAnnotation(ChartAnnotationKind.VerticalLine, value, null, "", color, 1);
        Annotations.Add(annotation);
        Options.XAxisFocusGuideAnnotations.Add(annotation);
    }

    private ChartColor PaletteColor(int paletteIndex) {
        if (paletteIndex < 0) throw new ArgumentOutOfRangeException(nameof(paletteIndex), paletteIndex, "Palette index must be non-negative.");
        return Options.Theme.Palette[paletteIndex % Options.Theme.Palette.Length];
    }
}
