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
        string X(double number) => cache.Format(x, number, horizontal ? chart.Options.ValueFormatter : null, xTicks);
        string Y(double number) => cache.Format(y, number, horizontal ? null : chart.Options.ValueFormatter, yTicks);
        string Secondary(double number) => cache.Format(chart.Options.SecondaryYAxis, number, chart.Options.ValueFormatter, secondaryTicks);
        if (builder.DataProjection != null) {
            // Use the same observed-axis ownership decision that created the projection.
            // Equal numeric ranges do not establish ownership of their display format.
            var useSecondary = !horizontal && secondaryOnly;
            builder.DataProjection = builder.DataProjection.WithDisplayTicks(xTicks.Select(number => new ChartAxisLabel(number, X(number))).ToArray(),
                (useSecondary ? secondaryTicks : yTicks).Select(number => new ChartAxisLabel(number, useSecondary ? Secondary(number) : Y(number))).ToArray());
        }
        builder.CaptureChartReadouts(chart, (_, number) => horizontal ? Y(number) : X(number),
            (series, number) => horizontal ? X(number) : series.YAxis == ChartAxisSide.Secondary ? Secondary(number) : Y(number));
    }
}
