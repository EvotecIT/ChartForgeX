using System;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static void CaptureReadouts(Chart chart, VisualSceneBuilder builder, ChartRange range,
        ChartRange? secondaryRange, AxisLabelCache cache, bool horizontal, bool secondaryOnly) {
        var x = horizontal ? HorizontalAxes(chart, cache).Value : chart.Options.XAxis;
        var y = horizontal ? HorizontalAxes(chart, cache).Category : chart.Options.YAxis;
        var xTicks = cache.Ticks(x, range.MinX, range.MaxX);
        var yTicks = horizontal ? HorizontalCategoryTicks(chart, range, y) : cache.Ticks(y, range.MinY, range.MaxY);
        var secondaryTicks = secondaryRange == null ? Array.Empty<double>()
            : cache.Ticks(chart.Options.SecondaryYAxis, secondaryRange.MinY, secondaryRange.MaxY);
        string X(double number, string? numericCaption = null) => cache.FormatReadout(x, number, xTicks, numericCaption);
        string Y(double number, string? numericCaption = null) => cache.FormatReadout(y, number, yTicks, numericCaption);
        string Secondary(double number, string? numericCaption = null) => cache.FormatReadout(chart.Options.SecondaryYAxis, number, secondaryTicks, numericCaption);
        if (builder.DataProjection != null) {
            // Use the same observed-axis ownership decision that created the projection.
            // Equal numeric ranges do not establish ownership of their display format.
            var useSecondary = !horizontal && secondaryOnly;
            builder.DataProjection = builder.DataProjection.WithDisplayTicks(xTicks.Select(number => new ChartAxisLabel(number, cache.FormatDisplay(x, number, xTicks))).ToArray(),
                (useSecondary ? secondaryTicks : yTicks).Select(number => new ChartAxisLabel(number, useSecondary
                    ? cache.FormatDisplay(chart.Options.SecondaryYAxis, number, secondaryTicks) : cache.FormatDisplay(y, number, yTicks))).ToArray());
        }
        builder.CaptureChartReadouts(chart, (_, number) => horizontal ? Y(number) : X(number),
            (series, number, numericCaption) => horizontal ? X(number, numericCaption)
                : series.YAxis == ChartAxisSide.Secondary ? Secondary(number, numericCaption) : Y(number, numericCaption),
            (_, number) => horizontal ? cache.FormatDisplay(y, number, yTicks) : cache.FormatDisplay(x, number, xTicks));
    }
}
