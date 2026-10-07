using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static void DrawFinancial(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        ChartMapper map, int index, VisualThemeColors colors, List<LabelPlacementRequest> labels, List<LabelObstacle> obstacles) {
        var series = chart.Series[index]; var stride = ObservationStride(series.Kind); var count = series.Points.Count / stride;
        for (var sample = 0; sample < series.BoxPlotSourceSamples.Count; sample++)
            builder.AddRegion(new VisualSemanticRegion(SeriesId(index) + "-sample-" + Number(sample), "source-sample",
                new ChartRect(plot.Left, plot.Top, 0, 0), "value=" + Number(series.BoxPlotSourceSamples[sample])));
        for (var item = 0; item < count; item++) {
            var raw = item * stride; var xValue = series.Points[raw].X; var x = map.X(xValue);
            if (series.Kind == ChartSeriesKind.BoxPlot) {
                var minimum = series.Points[raw].Y; var q1 = series.Points[raw + 1].Y; var median = series.Points[raw + 2].Y;
                var q3 = series.Points[raw + 3].Y; var maximum = series.Points[raw + 4].Y;
                var width = Math.Max(14, Math.Min(46, plot.Width / Math.Max(1, count * 5))); var cap = width * .74;
                var top = map.Y(q3); var bottom = map.Y(q1); var minY = map.Y(minimum); var maxY = map.Y(maximum);
                var box = new ChartRect(x - width / 2, Math.Min(top, bottom), width, Math.Max(2, Math.Abs(bottom - top)));
                var bounds = Extents(x - width / 2, Math.Min(maxY, box.Top), x + width / 2, Math.Max(minY, box.Bottom));
                var color = PointColor(series, index, item, colors);
                var label = ResolveObservationLabel(chart, context, series, item, colors, () => Value(chart, median));
                using (ObservationGroup(builder, series, index, item, raw, stride, bounds, label,
                    ("x", xValue), ("minimum", minimum), ("q1", q1), ("median", median), ("q3", q3), ("maximum", maximum))) {
                    var stroke = series.HasExplicitStrokeWidth ? series.StrokeWidth : ChartVisualPrimitives.BoxPlotStrokeWidth;
                    builder.Line(x, minY, x, maxY, ChartColorMath.WithOpacity(color, ChartVisualPrimitives.BoxPlotWhiskerOpacity), stroke, role: "boxplot-whisker");
                    builder.Line(x - cap / 2, minY, x + cap / 2, minY, color, stroke, role: "boxplot-cap");
                    builder.Line(x - cap / 2, maxY, x + cap / 2, maxY, color, stroke, role: "boxplot-cap");
                    var fill = ChartColorMath.WithOpacity(color, ChartVisualPrimitives.BoxPlotBodyFillOpacity);
                    var radius = Math.Min(ChartVisualPrimitives.BoxPlotBodyRadius, box.Height / 2);
                    builder.Rect(box, fill, color, stroke, radius, role: "boxplot-body");
                    DrawPattern(builder, RoundedRectanglePath(box, radius), ObservationPattern(series, item), fill, ChartStateMark.Backdrop(chart.Options, colors, context.Frame), "boxplot-pattern");
                    builder.Line(box.Left, map.Y(median), box.Right, map.Y(median), color,
                        series.HasExplicitStrokeWidth ? series.StrokeWidth : ChartVisualPrimitives.BoxPlotMedianStrokeWidth, role: "boxplot-median");
                }
                ObservationLabel(chart, context, series, index, item, new ChartPoint(x, map.Y(median)), bounds, median, label, labels, obstacles);
            } else {
                var open = series.Points[raw].Y; var high = series.Points[raw + 1].Y; var low = series.Points[raw + 2].Y; var close = series.Points[raw + 3].Y;
                var rising = close >= open; var color = FinancialColor(series, index, item, rising, colors);
                var candle = series.Kind == ChartSeriesKind.Candlestick;
                var width = candle ? Math.Max(8, Math.Min(22, plot.Width / Math.Max(1, count * 5))) : Math.Max(7, Math.Min(18, plot.Width / Math.Max(1, count * 6)));
                var highY = map.Y(high); var lowY = map.Y(low); var openY = map.Y(open); var closeY = map.Y(close);
                var bounds = Extents(x - width / 2, Math.Min(highY, Math.Min(openY, closeY)), x + width / 2, Math.Max(lowY, Math.Max(openY, closeY)));
                var label = ResolveObservationLabel(chart, context, series, item, colors, () => Value(chart, close));
                using (ObservationGroup(builder, series, index, item, raw, stride, bounds, label,
                    ("x", xValue), ("open", open), ("high", high), ("low", low), ("close", close))) {
                    var stroke = series.HasExplicitStrokeWidth ? series.StrokeWidth : candle ? ChartVisualPrimitives.CandlestickStrokeWidth : ChartVisualPrimitives.OhlcStrokeWidth;
                    builder.Line(x, highY, x, lowY, color, stroke, role: candle ? "candlestick-wick" : "ohlc-stem");
                    if (candle) {
                        var height = Math.Max(2, Math.Abs(closeY - openY));
                        var body = new ChartRect(x - width / 2, (openY + closeY - height) / 2, width, height);
                        var opacity = rising ? ChartVisualPrimitives.CandlestickRisingFillOpacity : ChartVisualPrimitives.CandlestickFallingFillOpacity;
                        var fill = ChartColorMath.WithOpacity(color, opacity); var radius = Math.Min(ChartVisualPrimitives.CandlestickBodyRadius, height / 2);
                        builder.Rect(body, fill, color, stroke, radius, role: "candlestick-body");
                        DrawPattern(builder, RoundedRectanglePath(body, radius), ObservationPattern(series, item), fill, ChartStateMark.Backdrop(chart.Options, colors, context.Frame), "candlestick-pattern");
                    } else {
                        builder.Line(x - width / 2, openY, x, openY, color, stroke, role: "ohlc-open");
                        builder.Line(x, closeY, x + width / 2, closeY, color, stroke, role: "ohlc-close");
                    }
                }
                ObservationLabel(chart, context, series, index, item, new ChartPoint(x, closeY), bounds, close, label, labels, obstacles);
            }
        }
    }
}
