using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static double EdgeAwarePngLabelX(string label, double x, ChartRect plot, double fontSize) {
        var width = EstimatePngTextWidth(label, fontSize);
        return Clamp(x - width / 2.0, plot.Left + ChartVisualPrimitives.DataLabelPlotInset, plot.Right - width - ChartVisualPrimitives.DataLabelPlotInset);
    }

    private static void DrawXAxisTickLabel(RgbaCanvas c, Chart chart, ChartRect plot, string label, double x, double value, IReadOnlyList<string>? axisLabels = null) {
        var preferredFontSize = PngTickFontSize(chart);
        var color = chart.Options.TryGetXAxisLabelHighlight(value, out var highlight) ? highlight : PngTickColor(chart);
        var angle = Clamp(chart.Options.XAxisLabelAngle, -80, 80);
        var maxWidth = PngAxisTickLabelMaxWidth(plot, axisLabels?.Count ?? 0, angle);
        var fontSize = TextFontSizeForWidth(label, maxWidth, preferredFontSize);
        label = TrimPngLabelToWidth(label, fontSize, maxWidth);
        if (label.Length == 0) return;
        if (Math.Abs(angle) < 0.001) {
            DrawPngTextStyled(c, EdgeAwarePngLabelX(label, x, plot, fontSize), plot.Bottom + PngXAxisLabelOffset(chart, axisLabels) - fontSize + 1, label, chart.Options.TickLabelStyle, color, fontSize, emphasized: false);
            return;
        }

        var width = EstimatePngTextWidth(label, fontSize);
        var height = EstimatePngTextHeight(fontSize);
        var anchorX = Clamp(x, plot.Left + 4, plot.Right - 4);
        var anchorY = plot.Bottom + PngXAxisLabelOffset(chart, axisLabels);
        var originX = angle < 0 ? width : 0;
        if (x <= plot.Left + width * 0.4) originX = 0;
        if (x >= plot.Right - width * 0.4) originX = width;
        c.DrawTextRotated(anchorX, anchorY, label, color, fontSize, angle, originX, height / 2.0);
    }

    private static double PngAxisTickLabelMaxWidth(ChartRect plot, int tickCount, double angle) {
        if (tickCount <= 0) return Math.Max(8, plot.Width - ChartVisualPrimitives.DataLabelPlotInset * 2);
        var slotWidth = tickCount <= 1 ? plot.Width : plot.Width / Math.Max(1, tickCount - 1);
        var angleFactor = Math.Abs(angle) < 0.001 ? 0.92 : 1.35;
        return Math.Max(64, Math.Min(plot.Width, slotWidth * angleFactor));
    }

    private static double ProjectX(double value, ChartRange range, ChartRect plot) {
        var span = range.MaxX - range.MinX;
        if (Math.Abs(span) < 0.0000001) return plot.Left + plot.Width / 2;
        return plot.Left + (value - range.MinX) / span * plot.Width;
    }

    private static void DrawAxisTitles(RgbaCanvas c, Chart chart, ChartRect plot, IReadOnlyList<string>? xAxisLabels = null) {
        var theme = chart.Options.Theme;
        if (ShowXAxis(chart) && !string.IsNullOrWhiteSpace(chart.XAxisTitle)) {
            DrawPngXAxisTitle(c, chart, plot, plot.Bottom + PngXAxisTitleOffset(chart, xAxisLabels), PngXAxisTitleFontSize(chart));
        }

        if (ShowYAxis(chart)) DrawYAxisTitle(c, chart, plot, PngAxisTitleFontSize(chart));
    }

    private static void DrawDetailAxisTitles(RgbaCanvas c, Chart chart, ChartRect plot, int textScale) {
        if (!string.IsNullOrWhiteSpace(chart.XAxisTitle)) {
            DrawPngXAxisTitle(c, chart, plot, plot.Bottom + 48, PngAxisTitleFontSize(chart));
        }

        DrawYAxisTitle(c, chart, plot, PngAxisTitleFontSize(chart));
    }

    private static void DrawPngXAxisTitle(RgbaCanvas c, Chart chart, ChartRect plot, double baselineY, double preferredFontSize) {
        var fontSize = TextFontSizeForEmphasizedWidth(chart.XAxisTitle, Math.Max(48, plot.Width - 4), preferredFontSize);
        var label = TrimReadablePngLabelToWidth(chart.XAxisTitle, fontSize, Math.Max(48, plot.Width - 4));
        if (label.Length == 0) return;
        var width = EstimatePngEmphasizedTextWidth(label, fontSize);
        DrawPngTextStyled(c, Clamp(plot.Left + plot.Width / 2 - width / 2.0, plot.Left + 2, plot.Right - width - 2), baselineY - fontSize + 1, label, chart.Options.AxisTitleStyle, chart.Options.Theme.MutedText, fontSize, emphasized: true);
    }

    private static void DrawYAxisTitle(RgbaCanvas c, Chart chart, ChartRect plot, double preferredFontSize) {
        if (string.IsNullOrWhiteSpace(chart.YAxisTitle)) return;
        var fontSize = TextFontSizeForEmphasizedWidth(chart.YAxisTitle, Math.Max(40, plot.Height * 0.72), preferredFontSize);
        var label = TrimReadablePngLabelToWidth(chart.YAxisTitle, fontSize, Math.Max(40, plot.Height * 0.72));
        if (label.Length == 0) return;
        var width = EstimatePngEmphasizedTextWidth(label, fontSize);
        var height = EstimatePngTextHeight(fontSize);
        var axisX = Clamp(28, 18, Math.Max(18, plot.Left - height - 14));
        c.DrawTextRotatedEmphasized(axisX, plot.Top + plot.Height / 2.0, label, PngStyleColor(chart.Options.AxisTitleStyle, chart.Options.Theme.MutedText), fontSize, -90, width / 2.0, height / 2.0);
    }

    private static void DrawSecondaryYAxis(RgbaCanvas c, Chart chart, ChartRect plot, ChartMapper map, IReadOnlyList<double> yTicks) {
        var theme = chart.Options.Theme;
        var preferredFontSize = PngTickFontSize(chart);
        var labelMaxWidth = Math.Max(24, chart.Options.Size.Width - plot.Right - 12);
        if (ShowAxisLines(chart)) DrawPngGuideLine(c, plot.Right, plot.Top, plot.Right, plot.Bottom, theme.Axis, ChartVisualPrimitives.AxisStrokeWidth);
        foreach (var tick in yTicks) {
            var rawLabel = FormatSecondaryValue(chart, tick);
            var fontSize = TextFontSizeForWidth(rawLabel, labelMaxWidth, preferredFontSize);
            var label = TrimPngLabelToWidth(rawLabel, fontSize, labelMaxWidth);
            if (label.Length == 0) continue;
            DrawPngTextStyled(c, Math.Min(chart.Options.Size.Width - EstimatePngTextWidth(label, fontSize) - 2, plot.Right + 8), map.Y(tick) - fontSize + 4, label, chart.Options.TickLabelStyle, theme.MutedText, fontSize, emphasized: false);
        }

        if (string.IsNullOrWhiteSpace(chart.SecondaryYAxisTitle)) return;
        var titleMaxWidth = Math.Max(40, plot.Height * 0.72);
        var titleFontSize = TextFontSizeForEmphasizedWidth(chart.SecondaryYAxisTitle, titleMaxWidth, PngAxisTitleFontSize(chart));
        var title = TrimReadablePngLabelToWidth(chart.SecondaryYAxisTitle, titleFontSize, titleMaxWidth);
        if (title.Length == 0) return;
        var width = EstimatePngEmphasizedTextWidth(title, titleFontSize);
        var height = EstimatePngTextHeight(titleFontSize);
        c.DrawTextRotatedEmphasized(Math.Min(chart.Options.Size.Width - 18, plot.Right + 54), plot.Top + plot.Height / 2.0, title, PngStyleColor(chart.Options.AxisTitleStyle, theme.MutedText), titleFontSize, 90, width / 2.0, height / 2.0);
    }

    private static ChartRect ApplyHorizontalBarReserve(Chart chart, ChartRect plot, IReadOnlyList<double> categories) {
        if (categories.Count == 0) return plot;
        var fontSize = HorizontalCategoryFontSize(chart);
        var leftShift = 0.0;
        if (ShowYAxis(chart)) {
            var widest = 0.0;
            var wrapWidth = HorizontalCategoryWrapWidth(chart);
            foreach (var category in categories) widest = Math.Max(widest, WrappedLabelWidth(FormatX(chart, category), fontSize, wrapWidth));
            var desiredLeft = Math.Max(plot.Left, widest + 70);
            var maxLeft = Math.Max(plot.Left, Math.Min(chart.Options.Size.Width * 0.42, chart.Options.Size.Width - chart.Options.Padding.Right - 160));
            var adjustedLeft = Math.Min(desiredLeft, maxLeft);
            leftShift = Math.Max(0, adjustedLeft - plot.Left);
        }
        var rightReserve = HorizontalValueLabelReserve(chart);
        if (leftShift <= 0 && rightReserve <= 0) return plot;
        return new ChartRect(plot.X + leftShift, plot.Y, Math.Max(1, plot.Width - leftShift - rightReserve), plot.Height);
    }

    private static ChartRect ApplyYAxisLabelReserve(Chart chart, ChartRect plot, IReadOnlyList<double> yTicks) {
        if (!ShowYAxis(chart) || chart.Options.IsSparkline || yTicks.Count == 0) return plot;
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

    private static ChartRect ApplySecondaryYAxisLabelReserve(Chart chart, ChartRect plot, IReadOnlyList<double> yTicks) {
        if (!ShowYAxis(chart) || chart.Options.IsSparkline || yTicks.Count == 0) return plot;
        var fontSize = PngTickFontSize(chart);
        var widest = 0.0;
        foreach (var tick in yTicks) widest = Math.Max(widest, EstimatePngTextWidth(FormatSecondaryValue(chart, tick), fontSize));
        var titleReserve = string.IsNullOrWhiteSpace(chart.SecondaryYAxisTitle) ? 0 : PngAxisTitleFontSize(chart) + 18;
        var reserve = Math.Min(150, widest + 30 + titleReserve);
        if (reserve <= 0) return plot;
        return new ChartRect(plot.X, plot.Y, Math.Max(1, plot.Width - reserve), plot.Height);
    }

    private static ChartRect ApplyBottomReserve(Chart chart, ChartRect plot, IReadOnlyList<double> xTicks, bool valueAxisOnly) {
        if (chart.Options.IsSparkline || IsPieLike(chart)) return plot;

        var bottomReserve = 0.0;
        if (ShowXAxis(chart)) {
            var xLabels = XAxisTickLabels(chart, xTicks, valueAxisOnly);
            bottomReserve += PngXAxisTitleOffset(chart, xLabels) + PngAxisTitleFontSize(chart) + 4;
            if (string.IsNullOrWhiteSpace(chart.XAxisTitle)) bottomReserve -= 16;
        }

        if (ShouldDrawLegend(chart) && PngIsBottomLegend(chart.Options.LegendPosition)) bottomReserve += PngLegendBottomReserve(chart);
        var maxBottom = Math.Max(plot.Top + 1, chart.Options.Size.Height - bottomReserve);
        plot = plot.Bottom <= maxBottom ? plot : new ChartRect(plot.X, plot.Y, plot.Width, Math.Max(1, maxBottom - plot.Y));
        if (ShouldDrawLegend(chart) && PngIsTopLegend(chart.Options.LegendPosition)) {
            var reserve = PngLegendBottomReserve(chart);
            plot = new ChartRect(plot.X, plot.Y + reserve, plot.Width, Math.Max(1, plot.Height - reserve));
        } else if (ShouldDrawLegend(chart) && PngIsLeftLegend(chart.Options.LegendPosition)) {
            var reserve = PngLegendSideReserve(chart) + ChartVisualPrimitives.SideLegendPlotGap;
            plot = new ChartRect(plot.X + reserve, plot.Y, Math.Max(1, plot.Width - reserve), plot.Height);
        } else if (ShouldDrawLegend(chart) && PngIsRightLegend(chart.Options.LegendPosition)) {
            var reserve = PngLegendSideReserve(chart) + ChartVisualPrimitives.SideLegendPlotGap;
            plot = new ChartRect(plot.X, plot.Y, Math.Max(1, plot.Width - reserve), plot.Height);
        }

        return plot;
    }

    private static int PngLegendRowCount(Chart chart) {
        var fontSize = PngLegendFontSize(chart);
        var symbolWidth = 18;
        var x = 0.0;
        var rows = 1;
        var maxX = Math.Max(80, chart.Options.Size.Width - 80);
        var entries = BuildPngLegendEntries(chart);
        if (PngIsVerticalLegend(chart.Options.LegendPosition)) return entries.Count;
        for (var i = 0; i < entries.Count; i++) {
            var itemWidth = symbolWidth + 10 + EstimatePngEmphasizedTextWidth(entries[i].Label, fontSize) + 18;
            if (i > 0 && x + itemWidth > maxX) {
                rows++;
                x = 0;
            }

            x += itemWidth;
        }

        return rows;
    }

    private static double PngXAxisLabelOffset(Chart chart, IReadOnlyList<string>? labels = null) {
        var angle = Math.Abs(Clamp(chart.Options.XAxisLabelAngle, -80, 80)) * Math.PI / 180.0;
        if (angle < 0.001) return 21;
        if ((labels == null || labels.Count == 0) && chart.Options.XAxisLabels.Count == 0) return 21;
        var fontSize = PngTickFontSize(chart);
        var widest = 0.0;
        if (labels != null && labels.Count > 0) {
            foreach (var label in labels) widest = Math.Max(widest, EstimatePngTextWidth(label, fontSize));
        } else {
            foreach (var label in chart.Options.XAxisLabels) widest = Math.Max(widest, EstimatePngTextWidth(label.Text, fontSize));
        }

        return 20 + Math.Sin(angle) * Math.Min(96, widest);
    }

    private static double PngXAxisTitleOffset(Chart chart, IReadOnlyList<string>? labels = null) {
        return PngXAxisLabelOffset(chart, labels) + (Math.Abs(chart.Options.XAxisLabelAngle) < 0.001 ? 23 : 48);
    }

    private static double PngXAxisTitleFontSize(Chart chart) => TextFontSizeForEmphasizedWidth(chart.XAxisTitle, Math.Max(48, chart.Options.Size.Width - chart.Options.Padding.Left - chart.Options.Padding.Right), PngAxisTitleFontSize(chart));

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
        var maxWidth = Math.Max(8, plot.Left - 24);
        for (var i = 0; i < lines.Length; i++) {
            var line = TrimReadablePngLabelToWidth(lines[i], fontSize, maxWidth);
            if (line.Length == 0) continue;
            DrawPngTextStyled(c, plot.Left - EstimatePngEmphasizedTextWidth(line, fontSize) - 10, top + i * lineHeight, line, chart.Options.TickLabelStyle, chart.Options.Theme.MutedText, fontSize, emphasized: true);
        }
    }

    private static double HorizontalValueLabelReserve(Chart chart) {
        if (!HasHorizontalBarDataLabels(chart) && !(chart.Options.BarMode == ChartBarMode.Stacked && chart.Options.ShowStackTotals)) return 0;
        var widest = 0.0;
        var fontSize = HorizontalValueLabelFontSize(chart);
        if (chart.Options.BarMode == ChartBarMode.Stacked && chart.Options.ShowStackTotals) {
            var positiveTotals = new Dictionary<double, double>();
            var negativeTotals = new Dictionary<double, double>();
            foreach (var series in chart.Series) {
                if (series.Kind != ChartSeriesKind.HorizontalBar) continue;
                foreach (var point in series.Points) AddStackTotal(point.Y >= 0 ? positiveTotals : negativeTotals, point.X, point.Y);
            }

            foreach (var value in positiveTotals.Values) widest = Math.Max(widest, EstimatePngTextWidth(FormatValue(chart, value), fontSize));
            foreach (var value in negativeTotals.Values) widest = Math.Max(widest, EstimatePngTextWidth(FormatValue(chart, value), fontSize));
        } else {
            foreach (var series in chart.Series) {
                if (series.Kind != ChartSeriesKind.HorizontalBar) continue;
                foreach (var point in series.Points) widest = Math.Max(widest, EstimatePngTextWidth(FormatValue(chart, point.Y), fontSize));
            }
        }

        return widest == 0 ? 0 : Math.Min(104, widest + 20);
    }

    private static double HorizontalValueLabelFontSize(Chart chart) => TextFontSizeForEmphasizedWidth("100%", 72, chart.Options.Theme.DataLabelFontSize);

    private static void ApplyHorizontalValueBounds(Chart chart, ChartRange range, IReadOnlyList<double> xTicks) {
        var min = xTicks[0];
        var max = xTicks[xTicks.Count - 1];
        if (HasHorizontalBarDataLabels(chart) || (chart.Options.BarMode == ChartBarMode.Stacked && chart.Options.ShowStackTotals)) {
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
