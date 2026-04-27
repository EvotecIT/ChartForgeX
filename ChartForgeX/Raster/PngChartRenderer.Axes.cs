using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static double EdgeAwarePngLabelX(string label, double x, ChartRect plot, double fontSize) {
        var width = EstimatePngTextWidth(label, fontSize);
        return Clamp(x - width / 2.0, plot.Left + 2, plot.Right - width - 2);
    }

    private static void DrawXAxisTickLabel(RgbaCanvas c, Chart chart, ChartRect plot, string label, double x) {
        var theme = chart.Options.Theme;
        var fontSize = PngTickFontSize(chart);
        var angle = Clamp(chart.Options.XAxisLabelAngle, -80, 80);
        if (Math.Abs(angle) < 0.001) {
            c.DrawText(EdgeAwarePngLabelX(label, x, plot, fontSize), plot.Bottom + PngXAxisLabelOffset(chart) - fontSize + 1, label, theme.MutedText, fontSize);
            return;
        }

        var width = EstimatePngTextWidth(label, fontSize);
        var height = EstimatePngTextHeight(fontSize);
        var anchorX = Clamp(x, plot.Left + 4, plot.Right - 4);
        var anchorY = plot.Bottom + PngXAxisLabelOffset(chart);
        var originX = angle < 0 ? width : 0;
        if (x <= plot.Left + width * 0.4) originX = 0;
        if (x >= plot.Right - width * 0.4) originX = width;
        c.DrawTextRotated(anchorX, anchorY, label, theme.MutedText, fontSize, angle, originX, height / 2.0);
    }

    private static double ProjectX(double value, ChartRange range, ChartRect plot) {
        var span = range.MaxX - range.MinX;
        if (Math.Abs(span) < 0.0000001) return plot.Left + plot.Width / 2;
        return plot.Left + (value - range.MinX) / span * plot.Width;
    }

    private static void DrawAxisTitles(RgbaCanvas c, Chart chart, ChartRect plot) {
        var theme = chart.Options.Theme;
        if (!string.IsNullOrWhiteSpace(chart.XAxisTitle)) {
            var fontSize = PngXAxisTitleFontSize(chart);
            var width = EstimatePngEmphasizedTextWidth(chart.XAxisTitle, fontSize);
            c.DrawTextEmphasized(Clamp(plot.Left + plot.Width / 2 - width / 2.0, plot.Left + 2, plot.Right - width - 2), plot.Bottom + PngXAxisTitleOffset(chart) - fontSize + 1, chart.XAxisTitle, theme.MutedText, fontSize);
        }

        DrawYAxisTitle(c, chart, plot, PngAxisTitleFontSize(chart));
    }

    private static void DrawDetailAxisTitles(RgbaCanvas c, Chart chart, ChartRect plot, int textScale) {
        var theme = chart.Options.Theme;
        if (!string.IsNullOrWhiteSpace(chart.XAxisTitle)) {
            var fontSize = PngAxisTitleFontSize(chart);
            var width = EstimatePngEmphasizedTextWidth(chart.XAxisTitle, fontSize);
            c.DrawTextEmphasized(Clamp(plot.Left + plot.Width / 2 - width / 2.0, plot.Left + 2, plot.Right - width - 2), plot.Bottom + 48 - fontSize + 1, chart.XAxisTitle, theme.MutedText, fontSize);
        }

        DrawYAxisTitle(c, chart, plot, PngAxisTitleFontSize(chart));
    }

    private static void DrawYAxisTitle(RgbaCanvas c, Chart chart, ChartRect plot, double preferredFontSize) {
        if (string.IsNullOrWhiteSpace(chart.YAxisTitle)) return;
        var fontSize = TextFontSizeForWidth(chart.YAxisTitle, Math.Max(40, plot.Height * 0.72), preferredFontSize);
        var width = EstimatePngEmphasizedTextWidth(chart.YAxisTitle, fontSize);
        var height = EstimatePngTextHeight(fontSize);
        var axisX = Clamp(28, 18, Math.Max(18, plot.Left - height - 14));
        c.DrawTextRotatedEmphasized(axisX, plot.Top + plot.Height / 2.0, chart.YAxisTitle, chart.Options.Theme.MutedText, fontSize, -90, width / 2.0, height / 2.0);
    }

    private static ChartRect ApplyHorizontalBarReserve(Chart chart, ChartRect plot, IReadOnlyList<double> categories) {
        if (!chart.Options.ShowAxes || categories.Count == 0) return plot;
        var fontSize = HorizontalCategoryFontSize(chart);
        var widest = 0.0;
        var wrapWidth = HorizontalCategoryWrapWidth(chart);
        foreach (var category in categories) widest = Math.Max(widest, WrappedLabelWidth(FormatX(chart, category), fontSize, wrapWidth));
        var desiredLeft = Math.Max(plot.Left, widest + 70);
        var maxLeft = Math.Max(plot.Left, Math.Min(chart.Options.Size.Width * 0.42, chart.Options.Size.Width - chart.Options.Padding.Right - 160));
        var adjustedLeft = Math.Min(desiredLeft, maxLeft);
        var leftShift = Math.Max(0, adjustedLeft - plot.Left);
        var rightReserve = HorizontalValueLabelReserve(chart);
        if (leftShift <= 0 && rightReserve <= 0) return plot;
        return new ChartRect(plot.X + leftShift, plot.Y, Math.Max(1, plot.Width - leftShift - rightReserve), plot.Height);
    }

    private static ChartRect ApplyYAxisLabelReserve(Chart chart, ChartRect plot, IReadOnlyList<double> yTicks) {
        if (!chart.Options.ShowAxes || chart.Options.IsSparkline || yTicks.Count == 0) return plot;
        var fontSize = PngTickFontSize(chart);
        var widest = 0.0;
        foreach (var tick in yTicks) widest = Math.Max(widest, EstimatePngTextWidth(FormatValue(chart, tick), fontSize));
        var desiredLeft = Math.Max(plot.Left, widest + 54);
        var maxLeft = Math.Max(plot.Left, chart.Options.Size.Width - chart.Options.Padding.Right - 160);
        var adjustedLeft = Math.Min(desiredLeft, maxLeft);
        var leftShift = Math.Max(0, adjustedLeft - plot.Left);
        if (leftShift <= 0) return plot;
        return new ChartRect(plot.X + leftShift, plot.Y, Math.Max(1, plot.Width - leftShift), plot.Height);
    }

    private static ChartRect ApplyBottomReserve(Chart chart, ChartRect plot) {
        if (chart.Options.IsSparkline || IsPieLike(chart)) return plot;

        var bottomReserve = 0.0;
        if (chart.Options.ShowAxes) {
            bottomReserve += PngXAxisTitleOffset(chart) + PngAxisTitleFontSize(chart) + 4;
            if (string.IsNullOrWhiteSpace(chart.XAxisTitle)) bottomReserve -= 16;
        }

        if (chart.Options.ShowLegend && chart.Series.Count > 0) bottomReserve += 18 + PngLegendRowCount(chart) * (PngLegendFontSize(chart) + 6);
        var extraBottom = Math.Max(0, bottomReserve - chart.Options.Padding.Bottom);
        return extraBottom <= 0 ? plot : new ChartRect(plot.X, plot.Y, plot.Width, Math.Max(1, plot.Height - extraBottom));
    }

    private static int PngLegendRowCount(Chart chart) {
        var fontSize = PngLegendFontSize(chart);
        var symbolWidth = 18;
        var x = 40.0;
        var rows = 1;
        var maxX = Math.Max(80, chart.Options.Size.Width - 40);
        for (var i = 0; i < chart.Series.Count; i++) {
            var itemWidth = symbolWidth + 10 + EstimatePngEmphasizedTextWidth(chart.Series[i].Name, fontSize) + 18;
            if (i > 0 && x + itemWidth > maxX) {
                rows++;
                x = 40;
            }

            x += itemWidth;
        }

        return rows;
    }

    private static double PngXAxisLabelOffset(Chart chart) {
        var angle = Math.Abs(Clamp(chart.Options.XAxisLabelAngle, -80, 80)) * Math.PI / 180.0;
        if (angle < 0.001 || chart.Options.XAxisLabels.Count == 0) return 21;
        var fontSize = PngTickFontSize(chart);
        var widest = 0.0;
        foreach (var label in chart.Options.XAxisLabels) widest = Math.Max(widest, EstimatePngTextWidth(label.Text, fontSize));
        return 20 + Math.Sin(angle) * Math.Min(96, widest);
    }

    private static double PngXAxisTitleOffset(Chart chart) {
        return PngXAxisLabelOffset(chart) + (Math.Abs(chart.Options.XAxisLabelAngle) < 0.001 ? 23 : 48);
    }

    private static double PngXAxisTitleFontSize(Chart chart) => TextFontSizeForWidth(chart.XAxisTitle, Math.Max(48, chart.Options.Size.Width - chart.Options.Padding.Left - chart.Options.Padding.Right), PngAxisTitleFontSize(chart));

    private static double HorizontalCategoryFontSize(Chart chart) => PngTickFontSize(chart);

    private static double HorizontalCategoryWrapWidth(Chart chart) => Math.Max(90, Math.Min(230, chart.Options.Size.Width * 0.28));

    private static double WrappedLabelWidth(string label, double fontSize, double maxWidth) {
        var lines = WrapHorizontalCategoryLabel(label, fontSize, maxWidth);
        var widest = 0.0;
        foreach (var line in lines) widest = Math.Max(widest, EstimatePngTextWidth(line, fontSize));
        return widest;
    }

    private static string[] WrapHorizontalCategoryLabel(string label, double fontSize, double maxWidth) {
        if (EstimatePngTextWidth(label, fontSize) <= maxWidth) return new[] { label };
        var words = label.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 2) return new[] { label };

        var bestIndex = 1;
        var bestScore = double.PositiveInfinity;
        for (var i = 1; i < words.Length; i++) {
            var first = string.Join(" ", words, 0, i);
            var second = string.Join(" ", words, i, words.Length - i);
            var firstWidth = EstimatePngTextWidth(first, fontSize);
            var secondWidth = EstimatePngTextWidth(second, fontSize);
            var score = Math.Max(firstWidth, secondWidth) + Math.Abs(firstWidth - secondWidth) * 0.18;
            if (score >= bestScore) continue;
            bestScore = score;
            bestIndex = i;
        }

        return new[] { string.Join(" ", words, 0, bestIndex), string.Join(" ", words, bestIndex, words.Length - bestIndex) };
    }

    private static void DrawHorizontalCategoryLabel(RgbaCanvas c, Chart chart, ChartRect plot, string label, double y) {
        var fontSize = HorizontalCategoryFontSize(chart);
        var lines = WrapHorizontalCategoryLabel(label, fontSize, HorizontalCategoryWrapWidth(chart));
        var lineHeight = EstimatePngTextHeight(fontSize) + 3;
        var top = y - (lines.Length * lineHeight - (lineHeight - EstimatePngTextHeight(fontSize))) / 2.0;
        for (var i = 0; i < lines.Length; i++) {
            var line = lines[i];
            c.DrawTextEmphasized(plot.Left - EstimatePngEmphasizedTextWidth(line, fontSize) - 10, top + i * lineHeight, line, chart.Options.Theme.MutedText, fontSize);
        }
    }

    private static double HorizontalValueLabelReserve(Chart chart) {
        if (!chart.Options.ShowDataLabels) return 0;
        var widest = 0.0;
        var fontSize = HorizontalValueLabelFontSize(chart);
        foreach (var series in chart.Series) {
            if (series.Kind != ChartSeriesKind.HorizontalBar) continue;
            foreach (var point in series.Points) widest = Math.Max(widest, EstimatePngTextWidth(FormatValue(chart, point.Y), fontSize));
        }

        return widest == 0 ? 0 : Math.Min(104, widest + 20);
    }

    private static double HorizontalValueLabelFontSize(Chart chart) => TextFontSizeForWidth("100%", 72, chart.Options.Theme.DataLabelFontSize);

    private static void ApplyHorizontalValueBounds(Chart chart, ChartRange range, IReadOnlyList<double> xTicks) {
        var min = xTicks[0];
        var max = xTicks[xTicks.Count - 1];
        if (chart.Options.ShowDataLabels) {
            var span = Math.Max(1, max - min);
            var hasPositive = false;
            var hasNegative = false;
            foreach (var series in chart.Series) {
                if (series.Kind != ChartSeriesKind.HorizontalBar) continue;
                foreach (var point in series.Points) {
                    if (point.Y > 0) hasPositive = true;
                    if (point.Y < 0) hasNegative = true;
                }
            }

            if (hasPositive) max += span * 0.08;
            if (hasNegative) min -= span * 0.08;
        }

        range.SetXBounds(min, max);
    }
}
