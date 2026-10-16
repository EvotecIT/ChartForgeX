using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static void DrawFinancial(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        ChartMapper map, int index, VisualThemeColors colors, List<LabelPlacementRequest> labels, List<LabelObstacle> obstacles) {
        var series = chart.Series[index]; var stride = ObservationStride(series.Kind); var count = series.RenderPoints.Count / stride;
        for (var item = 0; item < count; item++) {
            var raw = item * stride; var xValue = series.RenderPoints[raw].X; var x = map.X(xValue);
            var open = series.RenderPoints[raw].Y; var high = series.RenderPoints[raw + 1].Y; var low = series.RenderPoints[raw + 2].Y; var close = series.RenderPoints[raw + 3].Y;
            var style = FinancialStyle(series, index, item, close >= open, colors);
            var candle = series.Kind == ChartSeriesKind.Candlestick;
            var width = candle ? Math.Max(8, Math.Min(22, plot.Width / Math.Max(1, count * 5))) : Math.Max(7, Math.Min(18, plot.Width / Math.Max(1, count * 6)));
            var highY = map.Y(high); var lowY = map.Y(low); var openY = map.Y(open); var closeY = map.Y(close);
            var body = FinancialBody(x, width, openY, closeY);
            var bounds = FinancialBounds(candle, body, highY, lowY, openY, closeY, style);
            var label = ResolveObservationLabel(chart, context, series, item, colors, format => format("close", close));
            using (ObservationGroup(builder, series, index, item, raw, stride, bounds, label,
                ("x", xValue), ("open", open), ("high", high), ("low", low), ("close", close))) {
                DrawFinancialMark(builder, candle, body, highY, lowY, openY, closeY, style,
                    ObservationPattern(series, item), ChartStateMark.Backdrop(chart.Options, colors, context.Frame));
            }
            ObservationLabel(chart, context, series, index, item, new ChartPoint(x, closeY), bounds, close, label, labels, obstacles);
        }
    }
}
