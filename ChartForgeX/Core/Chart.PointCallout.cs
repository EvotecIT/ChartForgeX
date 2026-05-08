using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <inheritdoc />
public sealed partial class Chart {
    /// <summary>
    /// Adds a labeled point callout rendered as a one-point scatter series.
    /// </summary>
    /// <param name="label">The callout label.</param>
    /// <param name="point">The callout point.</param>
    /// <param name="color">An optional callout color.</param>
    /// <param name="placement">The label placement around the point.</param>
    /// <returns>The current chart.</returns>
    public Chart AddPointCallout(string label, ChartPoint point, ChartColor? color = null, ChartDataLabelPlacement placement = ChartDataLabelPlacement.Above) {
        if (label == null) throw new ArgumentNullException(nameof(label));
        AddScatter(label, new[] { point }, color);
        Series[Series.Count - 1]
            .WithDataLabels()
            .WithDataLabelPlacement(placement)
            .WithLegendEntry(false)
            .WithSemanticRole("point-callout")
            .WithPointLabel(0, label);
        return this;
    }

    /// <summary>
    /// Adds a labeled point callout rendered as a one-point scatter series.
    /// </summary>
    /// <param name="label">The callout label.</param>
    /// <param name="x">The callout x-coordinate.</param>
    /// <param name="y">The callout y-coordinate.</param>
    /// <param name="color">An optional callout color.</param>
    /// <param name="placement">The label placement around the point.</param>
    /// <returns>The current chart.</returns>
    public Chart AddPointCallout(string label, double x, double y, ChartColor? color = null, ChartDataLabelPlacement placement = ChartDataLabelPlacement.Above) =>
        AddPointCallout(label, new ChartPoint(x, y), color, placement);
}
