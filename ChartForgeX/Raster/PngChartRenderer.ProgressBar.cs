using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawProgressBar(RgbaCanvas c, Chart chart, ChartRect plot) {
        ChartSeries? series = null;
        foreach (var candidate in chart.Series) if (candidate.Kind == ChartSeriesKind.ProgressBar) { series = candidate; break; }
        if (series == null || series.Points.Count == 0) return;
        var t = chart.Options.Theme;
        var maximum = chart.Options.ProgressMaximum;
        var showValues = chart.Options.ShowProgressValues;
        var showHandles = chart.Options.ShowProgressHandles;
        var rowHeight = Math.Max(30, Math.Min(58, plot.Height / Math.Max(1, series.Points.Count)));
        var labelWidth = Math.Min(150, Math.Max(80, plot.Width * 0.24));
        var valueWidth = showValues ? Math.Min(78, Math.Max(48, plot.Width * 0.13)) : 0;
        var barArea = Math.Max(1, plot.Width - labelWidth - valueWidth - (showValues ? 28 : 14));
        var barHeight = Math.Max(6, Math.Min(30, rowHeight * chart.Options.ProgressBarThicknessRatio));
        var startX = plot.Left + labelWidth + 12;
        var startY = plot.Top + Math.Max(0, (plot.Height - rowHeight * series.Points.Count) / 2);
        for (var i = 0; i < series.Points.Count; i++) {
            var point = series.Points[i];
            var y = startY + i * rowHeight + rowHeight / 2;
            var labelMaxWidth = Math.Max(8, labelWidth - 8);
            var rawLabel = FormatX(chart, point.X);
            var tickStyle = chart.Options.TickLabelStyle;
            var labelFontSize = TextFontSizeForEmphasizedWidth(rawLabel, labelMaxWidth, PngTickFontSize(chart), tickStyle);
            var label = TrimReadablePngLabelToWidth(rawLabel, labelFontSize, labelMaxWidth, tickStyle);
            if (label.Length > 0) {
                DrawPngTextStyled(c, plot.Left + labelWidth - 8 - EstimatePngStyledTextWidth(label, labelFontSize, tickStyle, emphasized: true), y - EstimatePngStyledTextBoundsHeight(labelFontSize, tickStyle) / 2 - PngStyledTextTopExtent(labelFontSize, tickStyle), label, tickStyle, t.MutedText, labelFontSize, emphasized: true);
            }
            var color = PngProgressItemColor(series, t, i);
            var ratio = Clamp(point.Y / maximum, 0, 1);
            var filledWidth = barArea * ratio;
            c.FillRoundedRect(startX, y - barHeight / 2, barArea, barHeight, barHeight / 2, ApplyOpacity(t.Grid, chart.Options.ProgressTrackOpacity));
            c.FillRoundedRect(startX, y - barHeight / 2, filledWidth, barHeight, barHeight / 2, color);
            if (showHandles) {
                var handleRadius = Math.Max(5, barHeight * 0.62);
                c.DrawCircle(startX + filledWidth, y, handleRadius, t.CardBackground);
                c.DrawCircleOutline(startX + filledWidth, y, handleRadius, color, Math.Max(2, barHeight * 0.18));
            }
            if (showValues) {
                var valueMaxWidth = Math.Max(8, valueWidth - 4);
                var rawValue = FormatValue(chart, point.Y);
                var dataStyle = DataLabelStyle(chart, series, i);
                var valueFontSize = TextFontSizeForEmphasizedWidth(rawValue, valueMaxWidth, PngStyleFontSize(dataStyle, t.DataLabelFontSize), dataStyle);
                var value = TrimReadablePngLabelToWidth(rawValue, valueFontSize, valueMaxWidth, dataStyle);
                if (value.Length > 0) {
                    DrawPngTextStyled(c, startX + barArea + 12, y - EstimatePngStyledTextBoundsHeight(valueFontSize, dataStyle) / 2 - PngStyledTextTopExtent(valueFontSize, dataStyle), value, dataStyle, t.Text, valueFontSize, emphasized: true);
                }
            }
        }
    }

    private static ChartColor PngProgressItemColor(ChartSeries series, ChartTheme theme, int index) {
        if (index < series.PointColors.Count && series.PointColors[index].HasValue) return series.PointColors[index]!.Value;
        return series.Color ?? theme.Palette[index % theme.Palette.Length];
    }
}
