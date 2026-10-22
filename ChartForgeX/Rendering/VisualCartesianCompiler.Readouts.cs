using System;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static void CaptureReadouts(Chart chart, VisualSceneBuilder builder, ChartRange range,
        ChartRange? secondaryRange, AxisLabelCache cache, bool horizontal, bool secondaryOnly) {
        var x = horizontal ? HorizontalAxes(chart, cache).Value : chart.Options.XAxis;
        var y = horizontal ? HorizontalAxes(chart, cache).Category : chart.Options.YAxis;
        var secondary = ChartCartesianOrientation.ValueAxis(chart, ChartAxisSide.Secondary);
        var xTicks = cache.Ticks(x, range.MinX, range.MaxX);
        var yTicks = horizontal && y.PreparedTimeDomain == null
            ? HorizontalCategoryTicks(chart, range, y) : cache.Ticks(y, range.MinY, range.MaxY);
        var secondaryTicks = secondaryRange == null ? Array.Empty<double>() : cache.Ticks(secondary,
            horizontal ? secondaryRange.MinX : secondaryRange.MinY, horizontal ? secondaryRange.MaxX : secondaryRange.MaxY);
        string X(double number, string? caption = null) => cache.FormatReadout(x, number, xTicks, caption);
        string Y(double number, string? caption = null) => cache.FormatReadout(y, number, yTicks, caption);
        string Secondary(double number, string? caption = null) => cache.FormatReadout(secondary, number, secondaryTicks, caption);
        if (builder.DataProjection != null) {
            // Captions use the same observed-axis ownership decision as the detached gesture map.
            var displayX = horizontal && secondaryOnly ? secondary : x;
            var displayY = !horizontal && secondaryOnly ? secondary : y;
            var displayXTicks = horizontal && secondaryOnly ? secondaryTicks : xTicks;
            var displayYTicks = !horizontal && secondaryOnly ? secondaryTicks : yTicks;
            var categoryAxis = horizontal ? y : x; var categoryTicks = horizontal ? yTicks : xTicks;
            var categoryLabels = cache.CategoryGroups?.Plan.Bindings ?? ChartCartesianCategoryLabels.CaptureBindings(chart, categoryAxis,
                number => cache.FormatDisplay(categoryAxis, number, categoryTicks));
            builder.DataProjection = builder.DataProjection.WithDisplayTicks(
                displayXTicks.Select(number => new ChartAxisLabel(number, cache.FormatDisplay(displayX, number, displayXTicks))).ToArray(),
                displayYTicks.Select(number => new ChartAxisLabel(number, cache.FormatDisplay(displayY, number, displayYTicks))).ToArray(),
                formatX: categoryLabels != null && !horizontal ? null : number => cache.FormatDisplay(displayX, number, displayXTicks),
                formatY: categoryLabels != null && horizontal ? null : number => cache.FormatDisplay(displayY, number, displayYTicks),
                categoryLabels: categoryLabels);
        }
        builder.CaptureChartReadouts(chart, (_, number) => horizontal ? Y(number) : X(number),
            (series, number, caption) => series.ValueAxis == ChartAxisSide.Secondary ? Secondary(number, caption)
                : horizontal ? X(number, caption) : Y(number, caption),
            (_, number) => horizontal ? cache.FormatDisplay(y, number, yTicks) : cache.FormatDisplay(x, number, xTicks));
        if (cache.CategoryGroups != null) builder.CaptureCategoryGroupReadouts(cache.CategoryGroups.Plan.Bindings);
    }
}
