using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawPictorial(RgbaCanvas c, Chart chart, ChartRect plot) {
        ChartSeries? series = null;
        foreach (var candidate in chart.Series) if (candidate.Kind == ChartSeriesKind.Pictorial) { series = candidate; break; }
        if (series == null || series.Points.Count == 0) return;
        var max = chart.Options.PictorialMaximum ?? 0.000001;
        if (!chart.Options.PictorialMaximum.HasValue) {
            foreach (var point in series.Points) max = Math.Max(max, point.Y);
        }

        max = Math.Max(0.000001, max);
        var t = chart.Options.Theme;
        var columns = chart.Options.PictorialColumns;
        var symbolRows = BuildPngPictorialSymbolRows(series, columns, chart.Options.PictorialValuePerSymbol);
        var totalSymbolRows = 0;
        foreach (var rowCount in symbolRows) totalSymbolRows += rowCount;
        totalSymbolRows = Math.Max(1, totalSymbolRows);
        var itemGap = Math.Min(12, Math.Max(4, plot.Height * 0.018));
        var rowHeight = Math.Max(16, Math.Min(54, (plot.Height - itemGap * (series.Points.Count - 1)) / totalSymbolRows));
        var labelWidth = Math.Min(160, Math.Max(84, plot.Width * 0.26));
        var showValues = chart.Options.ShowPictorialValues;
        var valueWidth = showValues ? Math.Min(88, Math.Max(50, plot.Width * 0.14)) : 0;
        var symbolArea = Math.Max(1, plot.Width - labelWidth - valueWidth - (showValues ? 22 : 10));
        var gap = Math.Max(1.2, Math.Min(7, symbolArea / columns * 0.16));
        var maxSymbolSize = Math.Max(1, (symbolArea - gap * (columns - 1)) / columns);
        var baseSymbolSize = Math.Min(rowHeight * 0.82, maxSymbolSize);
        var symbolSize = Math.Max(3, Math.Min(maxSymbolSize, baseSymbolSize * chart.Options.PictorialSymbolScale));
        var startX = plot.Left + labelWidth + 10;
        var totalHeight = rowHeight * totalSymbolRows + itemGap * (series.Points.Count - 1);
        var startY = plot.Top + Math.Max(0, (plot.Height - totalHeight) / 2);
        var symbolRow = 0;
        for (var i = 0; i < series.Points.Count; i++) {
            var point = series.Points[i];
            var rowsForItem = symbolRows[i];
            var itemTop = startY + symbolRow * rowHeight + i * itemGap;
            var labelY = itemTop + rowsForItem * rowHeight / 2;
            var labelMaxWidth = Math.Max(8, labelWidth - 8);
            var rawLabel = FormatX(chart, point.X);
            var labelFontSize = TextFontSizeForEmphasizedWidth(rawLabel, labelMaxWidth, t.TickLabelFontSize);
            var label = TrimReadablePngLabelToWidth(rawLabel, labelFontSize, labelMaxWidth);
            if (label.Length > 0) {
                c.DrawTextEmphasized(plot.Left + labelWidth - 8 - EstimatePngEmphasizedTextWidth(label, labelFontSize), labelY - labelFontSize / 2.0, label, t.MutedText, labelFontSize);
            }
            var color = PngPictorialItemColor(series, t, i);
            var filled = chart.Options.PictorialValuePerSymbol.HasValue ? point.Y / chart.Options.PictorialValuePerSymbol.Value : point.Y / max * columns;
            for (var row = 0; row < rowsForItem; row++) {
                var y = itemTop + row * rowHeight + rowHeight / 2;
                for (var column = 0; column < columns; column++) {
                    var amount = Clamp(filled - row * columns - column, 0, 1);
                    var x = startX + column * (symbolSize + gap);
                    var emptyColor = ApplyOpacity(t.Grid, chart.Options.PictorialEmptyOpacity);
                    var shape = chart.Options.PictorialSvgPathData == null ? chart.Options.PictorialShape : chart.Options.PictorialPngFallbackShape;
                    DrawPngPictorialSymbol(c, shape, x + symbolSize / 2, y, symbolSize / 2, emptyColor);
                    if (amount >= 1) {
                        DrawPngPictorialSymbol(c, shape, x + symbolSize / 2, y, symbolSize / 2, color);
                    } else if (amount > 0) {
                        DrawPngPictorialSymbolPartial(c, shape, x + symbolSize / 2, y, symbolSize / 2, color, amount);
                    }
                }
            }

            if (showValues) {
                var valueMaxWidth = Math.Max(8, valueWidth - 4);
                var rawValue = FormatValue(chart, point.Y);
                var valueFontSize = TextFontSizeForEmphasizedWidth(rawValue, valueMaxWidth, t.DataLabelFontSize);
                var value = TrimReadablePngLabelToWidth(rawValue, valueFontSize, valueMaxWidth);
                if (value.Length > 0) {
                    c.DrawTextEmphasized(startX + symbolArea + 12, labelY - valueFontSize / 2.0, value, t.Text, valueFontSize);
                }
            }

            symbolRow += rowsForItem;
        }
    }

    private static bool IsPictorialChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.Pictorial);

    private static ChartColor PngPictorialItemColor(ChartSeries series, ChartTheme theme, int index) {
        if (index < series.PointColors.Count && series.PointColors[index].HasValue) return series.PointColors[index]!.Value;
        return series.Color ?? theme.Palette[index % theme.Palette.Length];
    }

    private static int[] BuildPngPictorialSymbolRows(ChartSeries series, int columns, double? valuePerSymbol) {
        var rows = new int[series.Points.Count];
        for (var i = 0; i < series.Points.Count; i++) {
            rows[i] = valuePerSymbol.HasValue ? Math.Max(1, (int)Math.Ceiling(series.Points[i].Y / valuePerSymbol.Value / columns)) : 1;
        }

        return rows;
    }

    private static void DrawPngPictorialSymbol(RgbaCanvas c, ChartPictorialShape shape, double cx, double cy, double radius, ChartColor color) {
        if (shape == ChartPictorialShape.Square) {
            c.FillRoundedRect(cx - radius, cy - radius, radius * 2, radius * 2, radius * 0.14, color);
        } else if (shape == ChartPictorialShape.Diamond) {
            c.FillPolygon(new[] { new ChartPoint(cx, cy - radius), new ChartPoint(cx + radius, cy), new ChartPoint(cx, cy + radius), new ChartPoint(cx - radius, cy) }, color);
        } else if (shape == ChartPictorialShape.Triangle) {
            c.FillPolygon(new[] { new ChartPoint(cx, cy - radius), new ChartPoint(cx + radius, cy + radius), new ChartPoint(cx - radius, cy + radius) }, color);
        } else if (shape == ChartPictorialShape.Star) {
            c.FillPolygon(PngStarPoints(cx, cy, radius, radius * 0.44), color);
        } else if (shape == ChartPictorialShape.Heart) {
            c.DrawCircle(cx - radius * 0.34, cy - radius * 0.34, radius * 0.44, color);
            c.DrawCircle(cx + radius * 0.34, cy - radius * 0.34, radius * 0.44, color);
            c.FillPolygon(new[] {
                new ChartPoint(cx - radius * 0.86, cy - radius * 0.18),
                new ChartPoint(cx + radius * 0.86, cy - radius * 0.18),
                new ChartPoint(cx, cy + radius * 0.88)
            }, color);
        } else if (shape == ChartPictorialShape.Shield) {
            c.FillPolygon(new[] {
                new ChartPoint(cx, cy - radius),
                new ChartPoint(cx + radius * 0.82, cy - radius * 0.55),
                new ChartPoint(cx + radius * 0.72, cy + radius * 0.32),
                new ChartPoint(cx, cy + radius),
                new ChartPoint(cx - radius * 0.72, cy + radius * 0.32),
                new ChartPoint(cx - radius * 0.82, cy - radius * 0.55)
            }, color);
        } else if (shape == ChartPictorialShape.Check) {
            c.DrawLine(cx - radius * 0.72, cy - radius * 0.02, cx - radius * 0.22, cy + radius * 0.52, color, Math.Max(2, radius * 0.34));
            c.DrawLine(cx - radius * 0.22, cy + radius * 0.52, cx + radius * 0.76, cy - radius * 0.56, color, Math.Max(2, radius * 0.34));
        } else if (shape == ChartPictorialShape.Person) {
            c.DrawCircle(cx, cy - radius * 0.48, radius * 0.34, color);
            c.FillRoundedRect(cx - radius * 0.54, cy - radius * 0.05, radius * 1.08, radius * 0.92, radius * 0.36, color);
        } else if (shape == ChartPictorialShape.PersonDress) {
            c.DrawCircle(cx, cy - radius * 0.55, radius * 0.30, color);
            c.FillRoundedRect(cx - radius * 0.30, cy - radius * 0.18, radius * 0.60, radius * 0.36, radius * 0.14, color);
            c.FillPolygon(new[] {
                new ChartPoint(cx, cy - radius * 0.12),
                new ChartPoint(cx + radius * 0.62, cy + radius * 0.72),
                new ChartPoint(cx - radius * 0.62, cy + radius * 0.72)
            }, color);
        } else {
            c.DrawCircle(cx, cy, radius, color);
        }
    }

    private static void DrawPngPictorialSymbolPartial(RgbaCanvas c, ChartPictorialShape shape, double cx, double cy, double radius, ChartColor color, double amount) {
        var padding = Math.Max(2, radius * 0.18);
        var size = Math.Max(4, (int)Math.Ceiling(radius * 2 + padding * 2));
        var localCenter = size / 2.0;
        var buffer = new RgbaCanvas(size, size, 2);
        DrawPngPictorialSymbol(buffer, shape, localCenter, localCenter, radius, color);
        var pixels = buffer.ToOutputPixels();
        var width = buffer.OutputWidth;
        var height = buffer.OutputHeight;
        var clipWidth = Math.Max(0, Math.Min(width, (int)Math.Round(width * Clamp(amount, 0, 1))));
        for (var y = 0; y < height; y++) {
            for (var x = clipWidth; x < width; x++) pixels[(y * width + x) * 4 + 3] = 0;
        }

        c.DrawImageScaled(
            (int)Math.Round(cx - localCenter),
            (int)Math.Round(cy - localCenter),
            size,
            size,
            width,
            height,
            pixels);
    }

    private static ChartPoint[] PngStarPoints(double cx, double cy, double outer, double inner) {
        var points = new ChartPoint[10];
        for (var i = 0; i < points.Length; i++) {
            var angle = -Math.PI / 2 + i * Math.PI / 5;
            var radius = i % 2 == 0 ? outer : inner;
            points[i] = new ChartPoint(cx + Math.Cos(angle) * radius, cy + Math.Sin(angle) * radius);
        }

        return points;
    }
}
