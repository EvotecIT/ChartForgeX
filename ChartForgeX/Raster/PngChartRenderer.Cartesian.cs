using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawSeries(RgbaCanvas c, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var s = chart.Series[index]; var color = SeriesColor(chart, index);
        if (s.Kind == ChartSeriesKind.HorizontalBar) {
            var layout = HorizontalBarLayout(chart, plot, index);
            var zeroX = Math.Min(plot.Right, Math.Max(plot.Left, map.X(0)));
            var reservedLabels = new List<ChartLabelBounds>();
            for (var pointIndex = 0; pointIndex < s.Points.Count; pointIndex++) {
                var p = s.Points[pointIndex];
                var baseValue = chart.Options.BarMode == ChartBarMode.Stacked ? StackHorizontalBaseValue(chart, index, p) : 0;
                var baseX = chart.Options.BarMode == ChartBarMode.Stacked ? map.X(baseValue) : zeroX;
                var valueX = map.X(baseValue + p.Y);
                var left = Math.Min(baseX, valueX);
                var width = Math.Abs(valueX - baseX);
                var y = map.Y(p.X) + layout.Offset - layout.BarHeight / 2;
                var radius = chart.Options.BarMode == ChartBarMode.Stacked ? Math.Min(3, layout.BarHeight / 2) : Math.Min(7, layout.BarHeight / 2);
                if (chart.Options.BarVisualStyle.Kind == ChartBarStyle.SegmentedCapsule) {
                    DrawSegmentedHorizontalBar(c, chart.Options.BarVisualStyle, left, y, width, layout.BarHeight, p.Y, PointColor(chart, s, index, pointIndex), FillPattern(s, pointIndex));
                } else {
                    DrawGradientBar(c, left, y, width, layout.BarHeight, radius, PointColor(chart, s, index, pointIndex), FillPattern(s, pointIndex));
                }
                if (ShouldDrawDataLabels(chart, s)) {
                    var label = FormatDataLabel(chart, s, pointIndex, p.Y);
                    var labelFontSize = PngDataLabelFontSize(chart, s, pointIndex);
                    var placement = DataLabelPlacement(chart, s);
                    var inside = placement == ChartDataLabelPlacement.Inside || placement == ChartDataLabelPlacement.Center || (chart.Options.BarMode == ChartBarMode.Stacked && placement == ChartDataLabelPlacement.Auto);
                    if (inside) {
                        var labelWidth = EstimatePngEmphasizedTextWidth(label, labelFontSize);
                        if (width < labelWidth + 8) continue;
                        var labelX = left + width / 2.0 - labelWidth / 2.0;
                        var labelY = y + layout.BarHeight / 2 - labelFontSize / 2.0;
                        if (!ReservePngLabel(label, labelX, labelY, chart, plot, labelFontSize, reservedLabels)) continue;
                        DrawReadablePngLabel(c, plot, labelX, labelY, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), labelFontSize, DataLabelStyle(chart, s, pointIndex));
                    } else if (placement == ChartDataLabelPlacement.Above || placement == ChartDataLabelPlacement.Below) {
                        var labelWidth = EstimatePngEmphasizedTextWidth(label, labelFontSize);
                        var labelX = left + width / 2.0 - labelWidth / 2.0;
                        var labelY = placement == ChartDataLabelPlacement.Above ? y - labelFontSize - 4 : y + layout.BarHeight + 4;
                        if (!ReservePngLabel(label, labelX, labelY, chart, plot, labelFontSize, reservedLabels)) continue;
                        DrawReadablePngLabel(c, plot, labelX, labelY, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), labelFontSize, DataLabelStyle(chart, s, pointIndex));
                    } else {
                        var labelWidth = EstimatePngEmphasizedTextWidth(label, labelFontSize);
                        var labelX = placement == ChartDataLabelPlacement.Right || (placement == ChartDataLabelPlacement.Auto && p.Y >= 0)
                            ? Math.Min(plot.Right - labelWidth - 2, left + width + 8)
                            : Math.Max(plot.Left + 2, left - labelWidth - 8);
                        var labelY = y + layout.BarHeight / 2 - labelFontSize / 2.0;
                        if (!ReservePngLabel(label, labelX, labelY, chart, plot, labelFontSize, reservedLabels)) continue;
                        DrawReadablePngLabel(c, plot, labelX, labelY, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), labelFontSize, DataLabelStyle(chart, s, pointIndex));
                    }
                }
            }

            return;
        }

        if (s.Kind == ChartSeriesKind.Bar) {
            var layout = BarLayout(chart, plot, index);
            var zeroY = Math.Min(plot.Bottom, Math.Max(plot.Top, map.Y(0)));
            var reservedLabels = new List<ChartLabelBounds>();
            for (var pointIndex = 0; pointIndex < s.Points.Count; pointIndex++) {
                var p = s.Points[pointIndex];
                var baseValue = chart.Options.BarMode == ChartBarMode.Stacked ? StackBaseValue(chart, index, p) : 0;
                var y = map.Y(baseValue + p.Y);
                var baseY = chart.Options.BarMode == ChartBarMode.Stacked ? map.Y(baseValue) : zeroY;
                var barX = map.X(p.X) + layout.Offset - layout.BarWidth / 2;
                var barY = Math.Min(y, baseY);
                var barHeight = Math.Abs(baseY - y);
                var radius = chart.Options.BarMode == ChartBarMode.Stacked ? Math.Min(3, layout.BarWidth / 2) : Math.Min(7, layout.BarWidth / 2);
                if (chart.Options.BarVisualStyle.Kind == ChartBarStyle.SegmentedCapsule) {
                    DrawSegmentedBar(c, chart.Options.BarVisualStyle, barX, barY, layout.BarWidth, barHeight, p.Y, PointColor(chart, s, index, pointIndex), FillPattern(s, pointIndex));
                } else {
                    DrawGradientBar(c, barX, barY, layout.BarWidth, barHeight, radius, PointColor(chart, s, index, pointIndex), FillPattern(s, pointIndex));
                }
                if (ShouldDrawDataLabels(chart, s)) {
                    var label = FormatDataLabel(chart, s, pointIndex, p.Y);
                    var segmentHeight = barHeight;
                    var fontSize = PngDataLabelFontSize(chart, s, pointIndex);
                    var placement = DataLabelPlacement(chart, s);
                    if (placement == ChartDataLabelPlacement.Left || placement == ChartDataLabelPlacement.Right) {
                        var labelWidth = EstimatePngEmphasizedTextWidth(label, fontSize);
                        var labelX = placement == ChartDataLabelPlacement.Right ? barX + layout.BarWidth + 8 : barX - labelWidth - 8;
                        var labelY = barY + segmentHeight / 2 - fontSize / 2.0;
                        if (!ReservePngLabel(label, labelX, labelY, chart, plot, fontSize, reservedLabels)) continue;
                        DrawReadablePngLabel(c, plot, labelX, labelY, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize, DataLabelStyle(chart, s, pointIndex));
                    } else {
                        var inside = placement == ChartDataLabelPlacement.Inside || placement == ChartDataLabelPlacement.Center || (chart.Options.BarMode == ChartBarMode.Stacked && placement == ChartDataLabelPlacement.Auto);
                        if (inside && segmentHeight < fontSize + 8) continue;
                        var labelY = placement == ChartDataLabelPlacement.Above
                            ? barY - 10 - fontSize
                            : placement == ChartDataLabelPlacement.Below
                                ? barY + segmentHeight + 10 - fontSize
                                : inside
                                    ? barY + segmentHeight / 2 - fontSize / 2.0
                                    : p.Y >= 0 ? barY - 10 - fontSize : barY + segmentHeight + 10 - fontSize;
                        var labelX = map.X(p.X) + layout.Offset - EstimatePngEmphasizedTextWidth(label, fontSize) / 2.0;
                        if (!ReservePngLabel(label, labelX, labelY, chart, plot, fontSize, reservedLabels)) continue;
                        DrawReadablePngLabel(c, plot, labelX, labelY, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize, DataLabelStyle(chart, s, pointIndex));
                    }
                }
            }
            return;
        }
        if (s.Kind == ChartSeriesKind.Lollipop) {
            var zeroY = Math.Min(plot.Bottom, Math.Max(plot.Top, map.Y(0)));
            var markerRadius = Math.Max(4, chart.Options.Theme.MarkerRadius + 2.25);
            for (var pointIndex = 0; pointIndex < s.Points.Count; pointIndex++) {
                var p = s.Points[pointIndex];
                var x = map.X(p.X);
                var y = map.Y(p.Y);
                var pointColor = PointColor(chart, s, index, pointIndex);
                c.DrawLine(x, zeroY, x, y, ApplyOpacity(pointColor, ChartVisualPrimitives.LollipopStemOpacity), Math.Max(ChartVisualPrimitives.LollipopStemMinStrokeWidth, s.StrokeWidth * 0.62));
                DrawMarker(c, chart, x, y, markerRadius, pointColor);
                if (ShouldDrawDataLabels(chart, s)) {
                    var label = FormatDataLabel(chart, s, pointIndex, p.Y);
                    var fontSize = PngDataLabelFontSize(chart, s, pointIndex);
                    var placement = DataLabelPlacement(chart, s);
                    var labelWidth = EstimatePngEmphasizedTextWidth(label, fontSize);
                    var labelX = placement == ChartDataLabelPlacement.Right
                        ? x + markerRadius + 6
                        : placement == ChartDataLabelPlacement.Left
                            ? x - markerRadius - 6 - labelWidth
                            : x - labelWidth / 2.0;
                    var aboveY = y - fontSize - markerRadius - 4;
                    var belowY = y + markerRadius + 4;
                    var labelY = placement == ChartDataLabelPlacement.Below
                        ? belowY
                        : placement == ChartDataLabelPlacement.Center || placement == ChartDataLabelPlacement.Inside
                            ? y - fontSize / 2.0
                            : placement == ChartDataLabelPlacement.Auto && aboveY < plot.Top + 2 ? belowY : aboveY;
                    DrawReadablePngLabel(c, plot, labelX, labelY, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize, DataLabelStyle(chart, s, pointIndex));
                }
            }

            return;
        }
        if (s.Kind == ChartSeriesKind.RangeBar) {
            var intervalCount = Math.Max(1, s.Points.Count / 2);
            var barWidth = Math.Max(8, Math.Min(28, plot.Width / Math.Max(1, intervalCount * 4.0)));
            var reservedLabels = new List<ChartLabelBounds>();
            for (var pointIndex = 0; pointIndex + 1 < s.Points.Count; pointIndex += 2) {
                var start = s.Points[pointIndex];
                var end = s.Points[pointIndex + 1];
                var x = map.X(start.X);
                var y1 = map.Y(start.Y);
                var y2 = map.Y(end.Y);
                var top = Math.Min(y1, y2);
                var height = Math.Max(2, Math.Abs(y2 - y1));
                var intervalIndex = pointIndex / 2;
                var pointColor = PointColor(chart, s, index, intervalIndex);
                if (chart.Options.BarVisualStyle.Kind == ChartBarStyle.SegmentedCapsule) {
                    DrawSegmentedRangeBar(c, chart.Options.BarVisualStyle, x, top, barWidth, height, y1, y2, pointColor, FillPattern(s, intervalIndex));
                } else {
                    DrawGradientBar(c, x - barWidth / 2.0, top, barWidth, height, Math.Min(7, barWidth / 2), pointColor, FillPattern(s, intervalIndex));
                    c.DrawLine(x - barWidth * 0.75, y1, x + barWidth * 0.75, y1, pointColor, ChartVisualPrimitives.RangeBarCapStrokeWidth);
                    c.DrawLine(x - barWidth * 0.75, y2, x + barWidth * 0.75, y2, pointColor, ChartVisualPrimitives.RangeBarCapStrokeWidth);
                }
                if (ShouldDrawDataLabels(chart, s)) {
                    var label = FormatRangeBarLabel(chart, s, intervalIndex, start.Y, end.Y);
                    var fontSize = PngDataLabelFontSize(chart, s, intervalIndex);
                    var labelWidth = EstimatePngEmphasizedTextWidth(label, fontSize);
                    var placement = DataLabelPlacement(chart, s);
                    var bottom = Math.Max(y1, y2);
                    var labelX = placement == ChartDataLabelPlacement.Left
                        ? x - barWidth * 0.9 - labelWidth - 6
                        : placement == ChartDataLabelPlacement.Right
                            ? x + barWidth * 0.9 + 6
                            : x - labelWidth / 2.0;
                    var labelY = placement == ChartDataLabelPlacement.Below
                        ? bottom + 4
                        : placement == ChartDataLabelPlacement.Center || placement == ChartDataLabelPlacement.Inside || placement == ChartDataLabelPlacement.Left || placement == ChartDataLabelPlacement.Right
                            ? top + height / 2.0 - fontSize / 2.0
                            : top - fontSize - 4;
                    if (!ReservePngLabel(label, labelX, labelY, chart, plot, fontSize, reservedLabels)) continue;
                    DrawReadablePngLabel(c, plot, labelX, labelY, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize, DataLabelStyle(chart, s, intervalIndex));
                }
            }

            return;
        }
        if (s.Kind == ChartSeriesKind.BoxPlot) {
            DrawBoxPlots(c, chart, index, plot, map);
            return;
        }
        if (s.Kind == ChartSeriesKind.Bubble) {
            DrawBubbles(c, chart, index, plot, map);
            return;
        }
        if (s.Kind == ChartSeriesKind.ErrorBar) {
            DrawErrorBars(c, chart, index, plot, map);
            return;
        }
        if (s.Kind == ChartSeriesKind.Candlestick) {
            DrawCandlesticks(c, chart, index, plot, map);
            return;
        }
        if (s.Kind == ChartSeriesKind.Ohlc) {
            DrawOhlc(c, chart, index, plot, map);
            return;
        }
        if (s.Kind == ChartSeriesKind.RangeBand) {
            DrawRangeBand(c, chart, index, plot, map);
            return;
        }
        if (s.Kind == ChartSeriesKind.RangeArea) {
            DrawRangeArea(c, chart, index, plot, map);
            return;
        }
        if (s.Kind == ChartSeriesKind.Dumbbell) {
            DrawDumbbells(c, chart, index, plot, map);
            return;
        }
        if (s.Kind == ChartSeriesKind.Slope) {
            DrawSlope(c, chart, index, plot, map);
            return;
        }
        if (s.Kind == ChartSeriesKind.TrendLine) {
            DrawTrendLine(c, chart, index, map);
            return;
        }
        if (s.Kind == ChartSeriesKind.StackedArea) {
            DrawStackedArea(c, chart, index, plot, map);
            return;
        }
        if ((s.Kind == ChartSeriesKind.Area || s.Kind == ChartSeriesKind.StepArea) && s.Points.Count > 0) {
            var zeroY = Math.Min(plot.Bottom, Math.Max(plot.Top, map.Y(0)));
            var pathPoints = MapSeriesPathPoints(s, map);
            var polygon = new List<ChartPoint>(pathPoints.Count + 2) {
                new(pathPoints[0].X, zeroY)
            };
            foreach (var point in pathPoints) polygon.Add(point);
            polygon.Add(new ChartPoint(pathPoints[pathPoints.Count - 1].X, zeroY));
            c.FillPolygonVerticalGradient(polygon, ChartColor.FromRgba(color.R, color.G, color.B, 72), ChartColor.FromRgba(color.R, color.G, color.B, 8));
        }
        var linePoints = MapSeriesPathPoints(s, map);
        if (s.Kind != ChartSeriesKind.Scatter) {
            DrawPremiumPngLinePath(c, linePoints, color, s.StrokeWidth, chart.Options.LineVisualStyle);
        }
        if (s.Kind == ChartSeriesKind.Scatter || (!chart.Options.IsSparkline && (s.Kind == ChartSeriesKind.Line || s.Kind == ChartSeriesKind.StepLine))) {
            var markerRadius = s.Kind == ChartSeriesKind.Scatter ? Math.Max(ChartVisualPrimitives.ScatterMarkerMinRadius, chart.Options.Theme.MarkerRadius + ChartVisualPrimitives.ScatterMarkerRadiusExtra) : chart.Options.Theme.MarkerRadius;
            for (var pointIndex = 0; pointIndex < s.Points.Count; pointIndex++) {
                var p = s.Points[pointIndex];
                DrawMarker(c, chart, map.X(p.X), map.Y(p.Y), markerRadius, PointColor(chart, s, index, pointIndex));
            }
        }
        if (ShouldDrawDataLabels(chart, s)) {
            var reserved = new List<ChartLabelBounds>();
            var placement = DataLabelPlacement(chart, s);
            for (var pointIndex = 0; pointIndex < s.Points.Count; pointIndex++) {
                var p = s.Points[pointIndex];
                var label = FormatDataLabel(chart, s, pointIndex, p.Y);
                var fontSize = PngDataLabelFontSize(chart, s, pointIndex);
                if (IsPointCalloutSeries(s)) {
                    DrawPngPointCalloutLabel(c, chart, plot, map.X(p.X), map.Y(p.Y), label, DataLabelPlacement(chart, s), fontSize);
                    continue;
                }

                var labelWidth = EstimatePngEmphasizedTextWidth(label, fontSize);
                var labelX = placement == ChartDataLabelPlacement.Right
                    ? map.X(p.X) + 8
                    : placement == ChartDataLabelPlacement.Left
                        ? map.X(p.X) - labelWidth - 8
                        : map.X(p.X) - labelWidth / 2.0;
                var aboveY = map.Y(p.Y) - fontSize - 4;
                var belowY = map.Y(p.Y) + 8;
                var labelY = placement == ChartDataLabelPlacement.Below
                    ? belowY
                    : placement == ChartDataLabelPlacement.Center || placement == ChartDataLabelPlacement.Inside
                        ? map.Y(p.Y) - fontSize / 2.0
                        : placement == ChartDataLabelPlacement.Auto && aboveY < plot.Top + 2 ? belowY : aboveY;
                if (!ReservePngLabel(label, labelX, labelY, chart, plot, fontSize, reserved)) continue;
                DrawReadablePngLabel(c, plot, labelX, labelY, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize, DataLabelStyle(chart, s, pointIndex));
            }
        }
    }

    private static void DrawTrendLine(RgbaCanvas c, Chart chart, int index, ChartMapper map) {
        var series = chart.Series[index];
        if (series.Points.Count < 2) return;
        var color = series.Color ?? chart.Options.Theme.Palette[index % chart.Options.Theme.Palette.Length];
        var start = series.Points[0];
        var end = series.Points[series.Points.Count - 1];
        var style = chart.Options.LineVisualStyle;
        DrawPremiumPngLineSegment(c, map.X(start.X), map.Y(start.Y), map.X(end.X), map.Y(end.Y), color, series.StrokeWidth, style, dashed: true, foregroundMinStrokeWidth: ChartVisualPrimitives.TrendLineMinStrokeWidth);
    }

    private static void DrawSlope(RgbaCanvas c, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        if (series.Points.Count < 2) return;
        var color = series.Color ?? chart.Options.Theme.Palette[index % chart.Options.Theme.Palette.Length];
        var startColor = PointColor(chart, series, index, 0);
        var endColor = PointColor(chart, series, index, 1);
        var start = series.Points[0];
        var end = series.Points[1];
        var xStart = map.X(start.X);
        var yStart = map.Y(start.Y);
        var xEnd = map.X(end.X);
        var yEnd = map.Y(end.Y);
        var radius = Math.Max(ChartVisualPrimitives.SlopeMarkerMinRadius, chart.Options.Theme.MarkerRadius + ChartVisualPrimitives.SlopeMarkerRadiusExtra);

        var style = chart.Options.LineVisualStyle;
        DrawPremiumPngLineSegment(c, xStart, yStart, xEnd, yEnd, color, series.StrokeWidth, style);
        DrawMarker(c, chart, xStart, yStart, radius, startColor);
        DrawMarker(c, chart, xEnd, yEnd, radius, endColor);
        if (!ShouldDrawDataLabels(chart, series)) return;

        var halo = ReadableLabelHalo(chart);
        var startLabel = FormatValue(chart, start.Y);
        var startFontSize = PngDataLabelFontSize(chart, series, 0);
        var startX = Math.Max(plot.Left + 2, xStart - EstimatePngEmphasizedTextWidth(startLabel, startFontSize) - radius - 8);
        DrawReadablePngLabel(c, plot, startX, yStart - startFontSize / 2.0, startLabel, chart.Options.Theme.Text, halo, startFontSize, DataLabelStyle(chart, series, 0));
        var endLabel = FormatValue(chart, end.Y);
        var endFontSize = PngDataLabelFontSize(chart, series, 1);
        var endX = Math.Min(plot.Right - EstimatePngEmphasizedTextWidth(endLabel, endFontSize) - 2, xEnd + radius + 8);
        DrawReadablePngLabel(c, plot, endX, yEnd - endFontSize / 2.0, endLabel, chart.Options.Theme.Text, halo, endFontSize, DataLabelStyle(chart, series, 1));
    }

    private static void DrawStackedArea(RgbaCanvas c, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        if (series.Points.Count == 0) return;
        var color = series.Color ?? chart.Options.Theme.Palette[index % chart.Options.Theme.Palette.Length];
        var upper = new List<ChartPoint>(series.Points.Count);
        var lower = new List<ChartPoint>(series.Points.Count);
        foreach (var point in series.Points) {
            var baseValue = StackAreaBaseValue(chart, index, point);
            upper.Add(new ChartPoint(map.X(point.X), map.Y(baseValue + point.Y)));
            lower.Add(new ChartPoint(map.X(point.X), map.Y(baseValue)));
        }

        var upperPath = ChartPathBuilder.FromPoints(upper, ChartSeriesKind.Line, series.Smooth).Flatten(12);
        var lowerPath = ChartPathBuilder.FromPoints(lower, ChartSeriesKind.Line, series.Smooth).Flatten(12);
        var polygon = new List<ChartPoint>(upperPath.Count + lowerPath.Count);
        foreach (var point in upperPath) polygon.Add(point);
        for (var i = lowerPath.Count - 1; i >= 0; i--) polygon.Add(lowerPath[i]);
        c.FillPolygonVerticalGradient(polygon, ChartColor.FromRgba(color.R, color.G, color.B, 120), ChartColor.FromRgba(color.R, color.G, color.B, 34));
        DrawPremiumPngLinePath(c, upperPath, color, series.StrokeWidth, chart.Options.LineVisualStyle);

        if (!ShouldDrawDataLabels(chart, series)) return;
        var reserved = new List<ChartLabelBounds>();
        for (var pointIndex = 0; pointIndex < series.Points.Count; pointIndex++) {
            var point = series.Points[pointIndex];
            var baseValue = StackAreaBaseValue(chart, index, point);
            var label = FormatValue(chart, point.Y);
            var fontSize = PngDataLabelFontSize(chart, series, pointIndex);
            var labelX = map.X(point.X) - EstimatePngEmphasizedTextWidth(label, fontSize) / 2.0;
            var labelY = map.Y(baseValue + point.Y) - fontSize - 5;
            if (!ReservePngLabel(label, labelX, labelY, chart, plot, fontSize, reserved)) continue;
            DrawReadablePngLabel(c, plot, labelX, labelY, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize, DataLabelStyle(chart, series, pointIndex));
        }
    }

    private static bool ReservePngLabel(string label, double x, double y, Chart chart, ChartRect plot, double fontSize, List<ChartLabelBounds> reserved) {
        var fittedFontSize = TextFontSizeForEmphasizedWidth(label, Math.Max(8, plot.Width - 4), fontSize);
        var fittedLabel = TrimReadablePngLabelToWidth(label, fittedFontSize, Math.Max(8, plot.Width - 4));
        if (fittedLabel.Length == 0) return false;
        var width = EstimatePngEmphasizedTextWidth(fittedLabel, fittedFontSize) + 8;
        var height = fittedFontSize + 6;
        var left = Clamp(x, plot.Left + 2, plot.Right - width - 2);
        var top = Clamp(y, plot.Top + 2, plot.Bottom - height - 2);
        var bounds = new ChartLabelBounds(left, top, width, height);
        foreach (var item in reserved) if (bounds.Intersects(item)) return false;
        reserved.Add(bounds);
        return true;
    }

    private static void DrawGradientBar(RgbaCanvas c, double x, double y, double width, double height, double radius, ChartColor color, ChartFillPattern pattern = ChartFillPattern.None) {
        if (width <= 0.5 || height <= 0.5) return;
        var top = ChartMarkSurface.BarGradientTop(color);
        var bottom = ChartMarkSurface.BarGradientBottom(color);
        c.FillRoundedRectVerticalGradient(x, y, width, height, radius, top, bottom);
        DrawHatchOverlay(c, x, y, width, height, radius, pattern);
        var highlightAlpha = (byte)Math.Round(255 * ChartVisualPrimitives.BarHighlightOpacity);
        c.DrawLine(x + ChartVisualPrimitives.BarHighlightInset, y + ChartVisualPrimitives.BarHighlightInset, x + width - ChartVisualPrimitives.BarHighlightInset, y + ChartVisualPrimitives.BarHighlightInset, ChartColor.FromRgba(255, 255, 255, highlightAlpha), ChartVisualPrimitives.BarHighlightStrokeWidth);
    }

    private static void DrawSegmentedBar(RgbaCanvas c, ChartBarVisualStyle style, double x, double y, double width, double height, double value, ChartColor color, ChartFillPattern pattern) {
        if (width <= 0 || height <= 0) return;
        width = Math.Max(1.0, width);
        height = Math.Max(1.0, height);
        var geometry = ChartSegmentedBarGeometry.Vertical(style, x, y, width, height, value);
        c.FillRoundedRect(x, y, width, height, geometry.Radius, ApplyOpacity(color, style.BodyOpacity));
        DrawHatchOverlay(c, x, y, width, height, geometry.Radius, pattern);
        DrawSegmentedCap(c, style, geometry, color);
    }

    private static void DrawSegmentedHorizontalBar(RgbaCanvas c, ChartBarVisualStyle style, double x, double y, double width, double height, double value, ChartColor color, ChartFillPattern pattern) {
        if (width <= 0 || height <= 0) return;
        width = Math.Max(1.0, width);
        height = Math.Max(1.0, height);
        var geometry = ChartSegmentedBarGeometry.Horizontal(style, x, y, width, height, value);
        c.FillRoundedRect(x, y, width, height, geometry.Radius, ApplyOpacity(color, style.BodyOpacity));
        DrawHatchOverlay(c, x, y, width, height, geometry.Radius, pattern);
        DrawSegmentedCap(c, style, geometry, color);
    }

    private static void DrawSegmentedRangeBar(RgbaCanvas c, ChartBarVisualStyle style, double x, double y, double width, double height, double y1, double y2, ChartColor color, ChartFillPattern pattern) {
        if (width <= 0.5 || height <= 0.5) return;
        var geometry = ChartSegmentedBarGeometry.RangeCap(style, x, y1, width);
        c.FillRoundedRect(x - width / 2.0, y, width, height, geometry.Radius, ApplyOpacity(color, style.BodyOpacity));
        DrawHatchOverlay(c, x - width / 2.0, y, width, height, geometry.Radius, pattern);
        DrawSegmentedCap(c, style, geometry, color);
        DrawSegmentedCap(c, style, ChartSegmentedBarGeometry.RangeCap(style, x, y2, width), color);
    }

    private static void DrawSegmentedCap(RgbaCanvas c, ChartBarVisualStyle style, ChartSegmentedBarGeometry geometry, ChartColor color) {
        c.DrawLine(geometry.SoftShadow.X1, geometry.SoftShadow.Y1, geometry.SoftShadow.X2, geometry.SoftShadow.Y2, ApplyOpacity(color, style.CapShadowOpacity * 0.36), geometry.CapThickness + style.CapShadowSpread * 2.0);
        c.DrawLine(geometry.Shadow.X1, geometry.Shadow.Y1, geometry.Shadow.X2, geometry.Shadow.Y2, ApplyOpacity(color, style.CapShadowOpacity), geometry.CapThickness);
        c.DrawLine(geometry.Cap.X1, geometry.Cap.Y1, geometry.Cap.X2, geometry.Cap.Y2, ApplyOpacity(color, style.CapOpacity), geometry.CapThickness);
        c.DrawLine(geometry.Highlight.X1, geometry.Highlight.Y1, geometry.Highlight.X2, geometry.Highlight.Y2, ApplyOpacity(ChartColor.White, style.CapHighlightOpacity), Math.Max(1.0, geometry.CapThickness * ChartVisualPrimitives.SegmentedCapHighlightStrokeRatio));
    }

    private static void DrawHatchOverlay(RgbaCanvas c, double x, double y, double width, double height, double radius, ChartFillPattern pattern) {
        if (pattern == ChartFillPattern.None || width <= 1 || height <= 1) return;
        var color = ApplyOpacity(ChartColor.White, pattern == ChartFillPattern.Crosshatch ? 0.22 : 0.30);
        if (pattern == ChartFillPattern.DiagonalForward || pattern == ChartFillPattern.Crosshatch) DrawHatchDirection(c, x, y, width, height, radius, true, color);
        if (pattern == ChartFillPattern.DiagonalBackward || pattern == ChartFillPattern.Crosshatch) DrawHatchDirection(c, x, y, width, height, radius, false, color);
    }

    private static void DrawHatchDirection(RgbaCanvas c, double x, double y, double width, double height, double radius, bool forward, ChartColor color) {
        var spacing = 8.0;
        for (var offset = -height; offset < width + height; offset += spacing) {
            double x0;
            double y0;
            double x1;
            double y1;
            if (forward) {
                x0 = x + offset;
                y0 = y + height;
                x1 = x + offset + height;
                y1 = y;
            } else {
                x0 = x + offset;
                y0 = y;
                x1 = x + offset + height;
                y1 = y + height;
            }

            if (!ClipLineToRect(ref x0, ref y0, ref x1, ref y1, x, y, x + width, y + height)) continue;
            DrawRoundedClippedLine(c, x0, y0, x1, y1, x, y, width, height, radius, color);
        }
    }

    private static string FormatRangeBarLabel(Chart chart, ChartSeries series, int intervalIndex, double startValue, double endValue) {
        if (intervalIndex >= 0 && intervalIndex < series.PointLabels.Count && series.PointLabels[intervalIndex] != null) return series.PointLabels[intervalIndex]!;
        return FormatValue(chart, Math.Min(startValue, endValue)) + "-" + FormatValue(chart, Math.Max(startValue, endValue));
    }

    private static void DrawRoundedClippedLine(RgbaCanvas c, double x0, double y0, double x1, double y1, double rectX, double rectY, double width, double height, double radius, ChartColor color) {
        radius = Math.Max(0, Math.Min(radius, Math.Min(width, height) / 2.0));
        if (radius <= 0.000001) {
            c.DrawLine(x0, y0, x1, y1, color, 1.15);
            return;
        }

        var dx = x1 - x0;
        var dy = y1 - y0;
        var steps = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(dx * dx + dy * dy) / 1.5));
        var active = false;
        var segmentStartX = x0;
        var segmentStartY = y0;
        var previousX = x0;
        var previousY = y0;
        for (var i = 0; i <= steps; i++) {
            var t = i / (double)steps;
            var currentX = x0 + dx * t;
            var currentY = y0 + dy * t;
            var inside = IsInsideRoundedRect(currentX, currentY, rectX, rectY, width, height, radius);
            if (inside && !active) {
                segmentStartX = currentX;
                segmentStartY = currentY;
                active = true;
            } else if (!inside && active) {
                c.DrawLine(segmentStartX, segmentStartY, previousX, previousY, color, 1.15);
                active = false;
            }

            previousX = currentX;
            previousY = currentY;
        }

        if (active) c.DrawLine(segmentStartX, segmentStartY, x1, y1, color, 1.15);
    }

    private static bool IsInsideRoundedRect(double px, double py, double x, double y, double width, double height, double radius) {
        if (px < x || px > x + width || py < y || py > y + height) return false;
        var closestX = Clamp(px, x + radius, x + width - radius);
        var closestY = Clamp(py, y + radius, y + height - radius);
        var dx = px - closestX;
        var dy = py - closestY;
        return dx * dx + dy * dy <= radius * radius + 0.000001;
    }

    private static bool ClipLineToRect(ref double x0, ref double y0, ref double x1, ref double y1, double minX, double minY, double maxX, double maxY) {
        var dx = x1 - x0;
        var dy = y1 - y0;
        var t0 = 0.0;
        var t1 = 1.0;
        if (!ClipTest(-dx, x0 - minX, ref t0, ref t1)) return false;
        if (!ClipTest(dx, maxX - x0, ref t0, ref t1)) return false;
        if (!ClipTest(-dy, y0 - minY, ref t0, ref t1)) return false;
        if (!ClipTest(dy, maxY - y0, ref t0, ref t1)) return false;
        if (t1 < 1) {
            x1 = x0 + t1 * dx;
            y1 = y0 + t1 * dy;
        }
        if (t0 > 0) {
            x0 += t0 * dx;
            y0 += t0 * dy;
        }
        return true;
    }

    private static bool ClipTest(double p, double q, ref double t0, ref double t1) {
        if (Math.Abs(p) < 0.000001) return q >= 0;
        var r = q / p;
        if (p < 0) {
            if (r > t1) return false;
            if (r > t0) t0 = r;
        } else {
            if (r < t0) return false;
            if (r < t1) t1 = r;
        }
        return true;
    }

    private static void DrawMarker(RgbaCanvas c, Chart chart, double x, double y, double radius, ChartColor color) {
        c.DrawCircle(x, y, radius + ChartVisualPrimitives.PngMarkerOutlineRadiusExtra, chart.Options.Theme.CardBackground);
        c.DrawCircle(x, y, radius, color);
    }

    private static void DrawPngLinePath(RgbaCanvas c, IReadOnlyList<ChartPoint> points, ChartColor color, double strokeWidth) {
        var thickness = Math.Max(1, strokeWidth);
        c.DrawPolyline(points, color, thickness);
    }

    private static void DrawPremiumPngLinePath(RgbaCanvas c, IReadOnlyList<ChartPoint> points, ChartColor color, double strokeWidth, ChartLineVisualStyle style) {
        foreach (var layer in ChartLineVisualLayers.Build(color, strokeWidth, style)) {
            if (layer.IsVisible) DrawPngLinePath(c, points, layer.ColorWithOpacity(), layer.StrokeWidth);
        }
    }

    private static void DrawPremiumPngLineSegment(RgbaCanvas c, double x1, double y1, double x2, double y2, ChartColor color, double strokeWidth, ChartLineVisualStyle style, bool dashed = false, double foregroundMinStrokeWidth = 0) {
        foreach (var layer in ChartLineVisualLayers.Build(color, strokeWidth, style)) {
            if (!layer.IsVisible) continue;
            var width = layer.IsForeground && foregroundMinStrokeWidth > 0 ? Math.Max(foregroundMinStrokeWidth, layer.StrokeWidth) : layer.StrokeWidth;
            if (dashed) c.DrawDashedLine(x1, y1, x2, y2, layer.ColorWithOpacity(), width, 8, 6);
            else c.DrawLine(x1, y1, x2, y2, layer.ColorWithOpacity(), width);
        }
    }

    private static ChartColor PngStrokeHalo(ChartColor color) {
        return PngStrokeHalo(color, ChartVisualPrimitives.StrokeHaloOpacity);
    }

    private static ChartColor PngStrokeHalo(ChartColor color, double opacity) {
        if (color.A == 0) return color;
        var alpha = Math.Min(color.A, Math.Max(0, (int)Math.Round(color.A * opacity)));
        return ChartColor.FromRgba(color.R, color.G, color.B, (byte)alpha);
    }

    private static BarLayoutInfo BarLayout(Chart chart, ChartRect plot, int seriesIndex) {
        var barSeries = new List<int>();
        for (var i = 0; i < chart.Series.Count; i++) {
            if (chart.Series[i].Kind == ChartSeriesKind.Bar) barSeries.Add(i);
        }

        var groupCount = chart.Options.BarMode == ChartBarMode.Stacked ? 1 : Math.Max(1, barSeries.Count);
        var groupPosition = chart.Options.BarMode == ChartBarMode.Stacked ? 0 : Math.Max(0, barSeries.IndexOf(seriesIndex));
        var xValues = new HashSet<double>();
        foreach (var index in barSeries) {
            foreach (var point in chart.Series[index].Points) xValues.Add(point.X);
        }

        var categoryCount = Math.Max(1, xValues.Count);
        var slotWidth = plot.Width / categoryCount;
        var groupWidth = slotWidth * (groupCount == 1 ? 0.58 : 0.74);
        var gap = groupCount == 1 ? 0 : Math.Min(4, groupWidth * 0.08);
        var barWidth = Math.Max(3, (groupWidth - gap * (groupCount - 1)) / groupCount);
        var offset = (groupPosition - (groupCount - 1) / 2.0) * (barWidth + gap);
        return new BarLayoutInfo(barWidth, offset);
    }

    private static HorizontalBarLayoutInfo HorizontalBarLayout(Chart chart, ChartRect plot, int seriesIndex) {
        var horizontalSeries = new List<int>();
        for (var i = 0; i < chart.Series.Count; i++) {
            if (chart.Series[i].Kind == ChartSeriesKind.HorizontalBar) horizontalSeries.Add(i);
        }

        var groupCount = chart.Options.BarMode == ChartBarMode.Stacked ? 1 : Math.Max(1, horizontalSeries.Count);
        var groupPosition = chart.Options.BarMode == ChartBarMode.Stacked ? 0 : Math.Max(0, horizontalSeries.IndexOf(seriesIndex));
        var yValues = new HashSet<double>();
        foreach (var index in horizontalSeries) {
            foreach (var point in chart.Series[index].Points) yValues.Add(point.X);
        }

        var categoryCount = Math.Max(1, yValues.Count);
        var slotHeight = plot.Height / categoryCount;
        var groupHeight = slotHeight * (groupCount == 1 ? 0.56 : 0.76);
        var gap = groupCount == 1 ? 0 : Math.Min(4, groupHeight * 0.08);
        var barHeight = Math.Max(3, Math.Min(30, (groupHeight - gap * (groupCount - 1)) / groupCount));
        var offset = (groupPosition - (groupCount - 1) / 2.0) * (barHeight + gap);
        return new HorizontalBarLayoutInfo(barHeight, offset);
    }

    private static double StackBaseValue(Chart chart, int seriesIndex, ChartPoint point) {
        var sum = 0.0;
        for (var i = 0; i < seriesIndex; i++) {
            var series = chart.Series[i];
            if (series.Kind != ChartSeriesKind.Bar) continue;
            foreach (var candidate in series.Points) {
                if (Math.Abs(candidate.X - point.X) >= 0.000001) continue;
                if ((point.Y >= 0 && candidate.Y >= 0) || (point.Y < 0 && candidate.Y < 0)) sum += candidate.Y;
                break;
            }
        }

        return sum;
    }

    private static double StackHorizontalBaseValue(Chart chart, int seriesIndex, ChartPoint point) {
        var sum = 0.0;
        for (var i = 0; i < seriesIndex; i++) {
            var series = chart.Series[i];
            if (series.Kind != ChartSeriesKind.HorizontalBar) continue;
            foreach (var candidate in series.Points) {
                if (Math.Abs(candidate.X - point.X) >= 0.000001) continue;
                if ((point.Y >= 0 && candidate.Y >= 0) || (point.Y < 0 && candidate.Y < 0)) sum += candidate.Y;
                break;
            }
        }

        return sum;
    }

    private static double StackAreaBaseValue(Chart chart, int seriesIndex, ChartPoint point) {
        var sum = 0.0;
        for (var i = 0; i < seriesIndex; i++) {
            var series = chart.Series[i];
            if (series.Kind != ChartSeriesKind.StackedArea) continue;
            foreach (var candidate in series.Points) {
                if (Math.Abs(candidate.X - point.X) >= 0.000001) continue;
                if ((point.Y >= 0 && candidate.Y >= 0) || (point.Y < 0 && candidate.Y < 0)) sum += candidate.Y;
                break;
            }
        }

        return sum;
    }

    private static void DrawStackTotals(RgbaCanvas c, Chart chart, ChartRect plot, ChartMapper map) {
        var positiveTotals = new Dictionary<double, double>();
        var negativeTotals = new Dictionary<double, double>();
        foreach (var series in chart.Series) {
            if (series.Kind != ChartSeriesKind.Bar) continue;
            foreach (var point in series.Points) AddStackTotal(point.Y >= 0 ? positiveTotals : negativeTotals, point.X, point.Y);
        }

        var reservedLabels = new List<ChartLabelBounds>();
        DrawStackTotalSet(c, chart, plot, map, positiveTotals, -12, reservedLabels);
        DrawStackTotalSet(c, chart, plot, map, negativeTotals, 8, reservedLabels);
    }

    private static void DrawHorizontalStackTotals(RgbaCanvas c, Chart chart, ChartRect plot, ChartMapper map) {
        var positiveTotals = new Dictionary<double, double>();
        var negativeTotals = new Dictionary<double, double>();
        foreach (var series in chart.Series) {
            if (series.Kind != ChartSeriesKind.HorizontalBar) continue;
            foreach (var point in series.Points) AddStackTotal(point.Y >= 0 ? positiveTotals : negativeTotals, point.X, point.Y);
        }

        var reservedLabels = new List<ChartLabelBounds>();
        DrawHorizontalStackTotalSet(c, chart, plot, map, positiveTotals, 8, true, reservedLabels);
        DrawHorizontalStackTotalSet(c, chart, plot, map, negativeTotals, -8, false, reservedLabels);
    }

    private static void DrawHorizontalStackTotalSet(RgbaCanvas c, Chart chart, ChartRect plot, ChartMapper map, Dictionary<double, double> totals, double offset, bool positive, List<ChartLabelBounds> reservedLabels) {
        foreach (var item in totals) {
            if (Math.Abs(item.Value) < 0.000001) continue;
            var label = FormatValue(chart, item.Value);
            var fontSize = chart.Options.Theme.DataLabelFontSize;
            var width = EstimatePngEmphasizedTextWidth(label, fontSize);
            var x = positive ? map.X(item.Value) + offset : map.X(item.Value) + offset - width;
            x = Clamp(x, plot.Left + 2, plot.Right - width - 2);
            var y = Clamp(map.Y(item.Key) - fontSize / 2.0, plot.Top + 2, plot.Bottom - fontSize - 2);
            if (!ReservePngLabel(label, x, y, chart, plot, fontSize, reservedLabels)) continue;
            DrawReadablePngLabel(c, x, y, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize);
        }
    }

    private static void DrawStackTotalSet(RgbaCanvas c, Chart chart, ChartRect plot, ChartMapper map, Dictionary<double, double> totals, double offset, List<ChartLabelBounds> reservedLabels) {
        foreach (var item in totals) {
            if (Math.Abs(item.Value) < 0.000001) continue;
            var label = FormatValue(chart, item.Value);
            var fontSize = chart.Options.Theme.DataLabelFontSize;
            var width = EstimatePngEmphasizedTextWidth(label, fontSize);
            var x = Clamp(map.X(item.Key) - width / 2.0, plot.Left + 2, plot.Right - width - 2);
            var y = Clamp(map.Y(item.Value) + offset - fontSize / 2.0, plot.Top + 2, plot.Bottom - fontSize - 2);
            if (!ReservePngLabel(label, x, y, chart, plot, fontSize, reservedLabels)) continue;
            DrawReadablePngLabel(c, x, y, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize);
        }
    }

    private static void AddStackTotal(Dictionary<double, double> totals, double x, double y) {
        double current;
        totals.TryGetValue(x, out current);
        totals[x] = current + y;
    }

    private readonly struct BarLayoutInfo {
        public BarLayoutInfo(double barWidth, double offset) {
            BarWidth = barWidth;
            Offset = offset;
        }

        public double BarWidth { get; }

        public double Offset { get; }
    }

    private readonly struct HorizontalBarLayoutInfo {
        public HorizontalBarLayoutInfo(double barHeight, double offset) {
            BarHeight = barHeight;
            Offset = offset;
        }

        public double BarHeight { get; }

        public double Offset { get; }
    }
}
