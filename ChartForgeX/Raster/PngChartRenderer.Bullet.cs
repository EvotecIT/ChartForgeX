using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawBullet(RgbaCanvas c, Chart chart, ChartRect basePlot) {
        var rows = new List<BulletRow>();
        for (var i = 0; i < chart.Series.Count; i++) {
            var series = chart.Series[i];
            if (series.Kind == ChartSeriesKind.Bullet && series.Points.Count >= 2) rows.Add(new BulletRow(series, i));
        }

        if (rows.Count == 0) return;
        var tickFontSize = PngTickFontSize(chart);
        var labelReserve = BulletLabelReserve(chart, rows);
        var valueReserve = BulletValueReserve(chart, rows);
        var content = BulletContentBounds(basePlot);
        FitBulletReserves(content.Width, ref labelReserve, ref valueReserve);
        var plot = new ChartRect(content.X + labelReserve, content.Y + 18, Math.Max(1, content.Width - labelReserve - valueReserve), Math.Max(1, content.Height - 54));
        var rowHeight = Math.Min(64, plot.Height / Math.Max(1, rows.Count));
        var barHeight = Math.Max(16, Math.Min(26, rowHeight * 0.38));

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++) {
            var row = rows[rowIndex];
            var y = plot.Top + rowHeight * rowIndex + rowHeight / 2;
            var min = BulletMin(row.Series);
            var max = BulletMax(row.Series);
            if (Math.Abs(max - min) < 0.000001) max = min + 1;
            var accent = row.Series.Color ?? chart.Options.Theme.Palette[row.Index % chart.Options.Theme.Palette.Length];

            var actualValue = BulletValue(row.Series);
            var targetValue = BulletTarget(row.Series);
            var value = Clamp(actualValue, min, max);
            var target = Clamp(targetValue, min, max);
            var valueX = BulletX(plot, min, max, value);
            var targetX = BulletX(plot, min, max, target);
            var status = BulletStatus(actualValue, targetValue);
            var statusColor = BulletStatusColor(chart, status);
            var dataStyle = DataLabelStyle(chart, row.Series, 0);
            var rowLabelMaxWidth = Math.Max(8, labelReserve - 16);
            var rowLabelFontSize = TextFontSizeForEmphasizedWidth(row.Series.Name, rowLabelMaxWidth, PngStyleFontSize(dataStyle, chart.Options.Theme.LegendFontSize), dataStyle);
            var rowLabel = TrimReadablePngLabelToWidth(row.Series.Name, rowLabelFontSize, rowLabelMaxWidth, dataStyle);
            var showLabels = row.Series.ShowDataLabels != false;
            if (showLabels && rowLabel.Length > 0) DrawPngTextStyled(c, content.Left, y - EstimatePngStyledTextHeight(rowLabelFontSize, dataStyle) / 2.0, rowLabel, dataStyle, chart.Options.Theme.Text, rowLabelFontSize, emphasized: true);
            DrawBulletRanges(c, row.Series, plot, y, barHeight, min, max, accent);
            DrawGradientBar(c, plot.Left, y - barHeight * 0.24, Math.Max(2, valueX - plot.Left), barHeight * 0.48, barHeight * 0.24, accent);
            c.DrawLine(targetX, y - barHeight * 0.65, targetX, y + barHeight * 0.65, chart.Options.Theme.Text, ChartVisualPrimitives.BulletTargetStrokeWidth);
            if (showLabels) {
                DrawBulletTargetLabel(c, chart, row.Series, FormatValue(chart, targetValue), targetX, y - barHeight * 0.92, plot, tickFontSize);
                c.DrawCircle(plot.Right + 8, y, ChartVisualPrimitives.PngStatusMarkerOutlineRadius, chart.Options.Theme.CardBackground);
                c.DrawCircle(plot.Right + 8, y, ChartVisualPrimitives.StatusMarkerRadius, statusColor);
                var rawValueLabel = FormatValue(chart, actualValue);
                var valueLabelMaxWidth = Math.Max(8, valueReserve - 24);
                var valueLabelFontSize = TextFontSizeForEmphasizedWidth(rawValueLabel, valueLabelMaxWidth, PngDataLabelFontSize(chart, row.Series, 0), dataStyle);
                var valueLabel = TrimReadablePngLabelToWidth(rawValueLabel, valueLabelFontSize, valueLabelMaxWidth, dataStyle);
                if (valueLabel.Length > 0) DrawPngTextStyled(c, plot.Right + 18, y - EstimatePngStyledTextHeight(valueLabelFontSize, dataStyle) / 2.0, valueLabel, dataStyle, chart.Options.Theme.Text, valueLabelFontSize, emphasized: true);
            }
        }

        DrawBulletAxis(c, chart, plot, rows[0].Series, content.Bottom - 12);
    }

    private static ChartRect BulletContentBounds(ChartRect basePlot) =>
        new(
            basePlot.X + ChartVisualPrimitives.BulletContentInset,
            basePlot.Y + ChartVisualPrimitives.BulletContentInset,
            Math.Max(1, basePlot.Width - ChartVisualPrimitives.BulletContentInset * 2),
            Math.Max(1, basePlot.Height - ChartVisualPrimitives.BulletContentInset * 2));

    private static void FitBulletReserves(double contentWidth, ref double labelReserve, ref double valueReserve) {
        var minimumPlotWidth = Math.Min(80, Math.Max(1, contentWidth * 0.25));
        var reserveBudget = Math.Max(0, contentWidth - minimumPlotWidth);
        var totalReserve = labelReserve + valueReserve;
        if (totalReserve <= reserveBudget || totalReserve <= 0) return;
        var ratio = reserveBudget / totalReserve;
        labelReserve *= ratio;
        valueReserve *= ratio;
    }

    private static void DrawBulletRanges(RgbaCanvas c, ChartSeries series, ChartRect plot, double y, double barHeight, double min, double max, ChartColor accent) {
        var previous = min;
        var ends = BulletRangeEnds(series, min, max);
        for (var i = 0; i < ends.Count; i++) {
            var end = Clamp(ends[i], min, max);
            if (end <= previous) continue;
            var x = BulletX(plot, min, max, previous);
            var width = BulletX(plot, min, max, end) - x;
            var alpha = (byte)Math.Max(24, 72 - i * 14);
            c.FillRoundedRect(x, y - barHeight / 2, width, barHeight, barHeight / 2, ChartColor.FromRgba(accent.R, accent.G, accent.B, alpha));
            previous = end;
        }
    }

    private static void DrawBulletAxis(RgbaCanvas c, Chart chart, ChartRect plot, ChartSeries reference, double y) {
        if (!chart.Options.ShowAxes) return;
        var min = BulletMin(reference);
        var max = BulletMax(reference);
        if (Math.Abs(max - min) < 0.000001) max = min + 1;
        var ticks = new[] { min, min + (max - min) / 2, max };
        c.DrawLine(plot.Left, y, plot.Right, y, chart.Options.Theme.Axis, ChartVisualPrimitives.BulletAxisStrokeWidth);
        foreach (var tick in ticks) {
            var x = BulletX(plot, min, max, tick);
            c.DrawLine(x, y - 4, x, y + 4, chart.Options.Theme.Axis, ChartVisualPrimitives.BulletAxisStrokeWidth);
            var label = FormatValue(chart, tick);
            var style = chart.Options.TickLabelStyle;
            var fontSize = PngTickFontSize(chart);
            DrawPngTextStyled(c, EdgeAwarePngLabelX(label, x, plot, fontSize, style), y + 20 - fontSize + 1, label, style, chart.Options.Theme.MutedText, fontSize, emphasized: false);
        }
    }

    private static void DrawBulletTargetLabel(RgbaCanvas c, Chart chart, ChartSeries series, string label, double x, double y, ChartRect plot, double fontSize) {
        var style = DataLabelStyle(chart, series, 1);
        var text = "target " + label;
        var maxWidth = Math.Max(8, plot.Width - 8);
        fontSize = TextFontSizeForEmphasizedWidth(text, maxWidth, PngStyleFontSize(style, fontSize), style);
        text = TrimReadablePngLabelToWidth(text, fontSize, maxWidth, style);
        if (text.Length == 0) return;

        var width = EstimatePngStyledTextWidth(text, fontSize, style, emphasized: true);
        var height = EstimatePngStyledTextHeight(fontSize, style);
        var safeX = Clamp(x - width / 2.0, plot.Left + 4, plot.Right - width - 4);
        var safeY = Clamp(y - fontSize + 1, plot.Top + 3, plot.Bottom - height - 3);
        var halo = ReadableLabelHalo(chart);
        DrawReadablePngLabel(c, safeX, safeY, text, chart.Options.Theme.MutedText, halo, fontSize, style);
    }

    private static bool IsBulletChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.Bullet);

    private static double BulletMin(ChartSeries series) => series.Points[0].X;

    private static double BulletMax(ChartSeries series) => series.Points[1].X;

    private static double BulletValue(ChartSeries series) => series.Points[0].Y;

    private static double BulletTarget(ChartSeries series) => series.Points[1].Y;

    private static double BulletX(ChartRect plot, double min, double max, double value) => plot.Left + (value - min) / (max - min) * plot.Width;

    private static List<double> BulletRangeEnds(ChartSeries series, double min, double max) {
        var ends = new List<double>();
        for (var i = 2; i < series.Points.Count; i++) {
            var value = series.Points[i].X;
            if (value > min && value < max) ends.Add(value);
        }

        if (ends.Count == 0) {
            var span = max - min;
            ends.Add(min + span * 0.6);
            ends.Add(min + span * 0.8);
        }

        ends.Sort();
        ends.Add(max);
        return ends;
    }

    private static string BulletStatus(double value, double target) {
        if (value < target - 0.000001) return "below-target";
        if (value > target + 0.000001) return "above-target";
        return "meets-target";
    }

    private static ChartColor BulletStatusColor(Chart chart, string status) {
        return status == "below-target" ? chart.Options.Theme.Negative : chart.Options.Theme.Positive;
    }

    private static double BulletLabelReserve(Chart chart, IReadOnlyList<BulletRow> rows) {
        var widest = 0.0;
        foreach (var row in rows) {
            if (row.Series.ShowDataLabels == false) continue;
            var style = DataLabelStyle(chart, row.Series, 0);
            var fontSize = PngStyleFontSize(style, chart.Options.Theme.LegendFontSize);
            widest = Math.Max(widest, EstimatePngStyledTextWidth(row.Series.Name, fontSize, style, emphasized: true));
        }
        return widest <= 0 ? 10 : Math.Min(240, Math.Max(128, widest + 34));
    }

    private static double BulletValueReserve(Chart chart, IReadOnlyList<BulletRow> rows) {
        var widest = 0.0;
        foreach (var row in rows) {
            if (row.Series.ShowDataLabels == false) continue;
            var style = DataLabelStyle(chart, row.Series, 0);
            var fontSize = PngDataLabelFontSize(chart, row.Series, 0);
            widest = Math.Max(widest, EstimatePngStyledTextWidth(FormatValue(chart, BulletValue(row.Series)), fontSize, style, emphasized: true));
        }
        return widest <= 0 ? 12 : Math.Min(142, Math.Max(84, widest + 38));
    }

    private readonly struct BulletRow {
        public BulletRow(ChartSeries series, int index) {
            Series = series;
            Index = index;
        }

        public ChartSeries Series { get; }

        public int Index { get; }
    }
}
