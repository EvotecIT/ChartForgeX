using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

namespace ChartForgeX.Raster;

/// <summary>
/// Renders charts to dependency-free PNG images.
/// </summary>
public sealed partial class PngChartRenderer {
    [ThreadStatic]
    private static TrueTypeFont? CurrentOutlineFont;

    /// <summary>
    /// Resolves the font that would be used for PNG text rendering.
    /// </summary>
    /// <param name="chart">The chart to inspect.</param>
    /// <returns>The resolved PNG font information.</returns>
    public static PngFontInfo GetFontInfo(Chart chart) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        var options = chart.Options;
        return TrueTypeFont.ResolveInfo(options.PngFontPath, options.PngFontCollectionIndex, options.PngFontFaceName);
    }

    /// <summary>
    /// Renders the specified chart to PNG bytes.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <returns>A PNG image.</returns>
    public byte[] Render(Chart chart) {
        ChartGuards.RenderCompatibility(chart);
        var o = chart.Options; var t = o.Theme;
        var outlineFont = TrueTypeFont.TryLoadFromPath(o.PngFontPath, o.PngFontCollectionIndex, o.PngFontFaceName);
        var previousOutlineFont = CurrentOutlineFont;
        CurrentOutlineFont = outlineFont;
        try {
        var c = new RgbaCanvas(o.Size.Width, o.Size.Height, 2, outlineFont);
        c.Clear(o.TransparentBackground ? ChartColor.Transparent : t.Background);
        if (o.ShowCard && t.UseCard) DrawCardSurface(c, o, t);
        var plot = ChartLayout.PlotArea(o);
        if (o.ShowHeader) DrawHeader(c, chart);
        if (IsPieLike(chart)) {
            DrawPlotSurface(c, o, t, plot);
            DrawPieLike(c, chart, plot);
            return WritePng(c);
        }
        if (IsGaugeChart(chart)) {
            DrawPlotSurface(c, o, t, plot);
            DrawGauge(c, chart, plot);
            return WritePng(c);
        }
        if (IsBulletChart(chart)) {
            DrawPlotSurface(c, o, t, plot);
            DrawBullet(c, chart, plot);
            return WritePng(c);
        }
        if (IsWaterfallChart(chart)) {
            DrawPlotSurface(c, o, t, plot);
            DrawWaterfall(c, chart, plot);
            return WritePng(c);
        }
        if (IsRadarChart(chart)) {
            DrawPlotSurface(c, o, t, plot);
            DrawRadar(c, chart, plot);
            return WritePng(c);
        }
        if (IsFunnelChart(chart)) {
            DrawPlotSurface(c, o, t, plot);
            DrawFunnel(c, chart, plot);
            return WritePng(c);
        }
        if (IsHeatmapChart(chart)) {
            DrawPlotSurface(c, o, t, plot);
            DrawHeatmap(c, chart, plot);
            return WritePng(c);
        }
        if (IsTimelineChart(chart)) {
            DrawPlotSurface(c, o, t, plot);
            DrawTimeline(c, chart, plot);
            return WritePng(c);
        }
        var range = ChartRange.FromChart(chart);
        IReadOnlyList<double> yTicks;
        IReadOnlyList<double> xTicks;
        if (IsHorizontalBarChart(chart)) {
            xTicks = ChartTicks.Generate(range.MinX, range.MaxX, o.TickCount);
            ApplyHorizontalValueBounds(chart, range, xTicks);
            yTicks = GetHorizontalCategoryTicks(chart, range);
            plot = ApplyHorizontalBarReserve(chart, plot, yTicks);
            plot = ApplyBottomReserve(chart, plot);
            DrawPlotSurface(c, o, t, plot);
        } else {
            yTicks = ChartTicks.Generate(range.MinY, range.MaxY, o.TickCount);
            range.SetYBounds(yTicks[0], yTicks[yTicks.Count - 1]);
            plot = ApplyYAxisLabelReserve(chart, plot, yTicks);
            plot = ApplyBottomReserve(chart, plot);
            xTicks = GetXTicks(chart, range, plot);
            DrawPlotSurface(c, o, t, plot);
        }

        var map = new ChartMapper(plot, range);
        if (IsHorizontalBarChart(chart)) {
            DrawHorizontalBarGrid(c, chart, plot, map, xTicks, yTicks);
            for (var i = 0; i < chart.Series.Count; i++) DrawSeries(c, chart, i, plot, map);
            DrawLegend(c, chart);
            return WritePng(c);
        }

        DrawAnnotationBands(c, chart, plot, map);
        foreach (var yv in yTicks) {
            var y = map.Y(yv);
            if (o.ShowGrid) c.DrawLine(plot.Left, y, plot.Right, y, t.Grid, 1);
            if (o.ShowAxes) {
                var label = FormatValue(chart, yv);
                var fontSize = PngTickFontSize(chart);
                c.DrawText(Math.Max(2, plot.Left - EstimatePngTextWidth(label, fontSize) - 8), y - fontSize + 4, label, t.MutedText, fontSize);
            }
        }
        foreach (var xv in xTicks) {
            var x = map.X(xv);
            var label = FormatX(chart, xv);
            if (o.ShowGrid) c.DrawLine(x, plot.Top, x, plot.Bottom, ChartColor.FromRgba(t.Grid.R, t.Grid.G, t.Grid.B, (byte)(t.Grid.A / 2)), 1);
            if (o.ShowAxes) DrawXAxisTickLabel(c, chart, plot, label, x);
        }
        if (o.ShowAxes) {
            var zeroY = map.Y(0);
            if (zeroY > plot.Top && zeroY < plot.Bottom) c.DrawLine(plot.Left, zeroY, plot.Right, zeroY, t.Axis, 1);
            c.DrawLine(plot.Left, plot.Bottom, plot.Right, plot.Bottom, t.Axis, 1);
            c.DrawLine(plot.Left, plot.Top, plot.Left, plot.Bottom, t.Axis, 1);
            DrawAxisTitles(c, chart, plot);
        }
        for (var i = 0; i < chart.Series.Count; i++) DrawSeries(c, chart, i, plot, map);
        if (o.BarMode == ChartBarMode.Stacked && o.ShowStackTotals) DrawStackTotals(c, chart, plot, map);
        DrawAnnotationLines(c, chart, plot, map);
        DrawLegend(c, chart);
        return WritePng(c);
        } finally {
            CurrentOutlineFont = previousOutlineFont;
        }
    }

    private static void DrawCardSurface(RgbaCanvas c, ChartOptions options, ChartTheme theme) {
        c.FillRoundedRect(14, 14, options.Size.Width - 28, options.Size.Height - 28, theme.CornerRadius, theme.CardBackground);
        if (theme.CardBorder.A > 0) c.StrokeRoundedRect(14, 14, options.Size.Width - 28, options.Size.Height - 28, theme.CornerRadius, theme.CardBorder);
    }

    private static void DrawPlotSurface(RgbaCanvas c, ChartOptions options, ChartTheme theme, ChartRect plot) {
        if (options.ShowPlotBackground) c.FillRoundedRect(plot.X, plot.Y, plot.Width, plot.Height, theme.PlotCornerRadius, theme.PlotBackground);
        if (options.ShowPlotBackground && theme.PlotBorder.A > 0) c.StrokeRoundedRect(plot.X, plot.Y, plot.Width, plot.Height, theme.PlotCornerRadius, theme.PlotBorder);
    }

    private static void DrawHeader(RgbaCanvas c, Chart chart) {
        var theme = chart.Options.Theme;
        var maxWidth = Math.Max(24, chart.Options.Size.Width - 80);
        var titleFontSize = TextFontSizeForEmphasizedWidth(chart.Title, maxWidth, theme.TitleFontSize);
        var title = TrimReadablePngLabelToWidth(chart.Title, titleFontSize, maxWidth);
        if (title.Length > 0) c.DrawTextEmphasized(40, 52 - titleFontSize + 1, title, theme.Text, titleFontSize);
        if (!string.IsNullOrWhiteSpace(chart.Subtitle)) {
            var subtitleMaxWidth = Math.Max(24, chart.Options.Size.Width - 84);
            var subtitleFontSize = TextFontSizeForWidth(chart.Subtitle, subtitleMaxWidth, theme.SubtitleFontSize);
            var subtitle = TrimPngLabelToWidth(chart.Subtitle, subtitleFontSize, subtitleMaxWidth);
            if (subtitle.Length > 0) c.DrawText(42, 79 - subtitleFontSize + 1, subtitle, theme.MutedText, subtitleFontSize);
        }
    }

    private static bool IsLineLikeLegend(ChartSeriesKind kind) =>
        kind == ChartSeriesKind.Line || kind == ChartSeriesKind.Area || kind == ChartSeriesKind.Radar;

    private static void DrawAnnotationBands(RgbaCanvas c, Chart chart, ChartRect plot, ChartMapper map) {
        foreach (var annotation in chart.Annotations) {
            if (!annotation.EndValue.HasValue) continue;
            var color = ApplyOpacity(annotation.Color, annotation.Opacity);
            if (annotation.Kind == ChartAnnotationKind.HorizontalBand) {
                var y1 = Clamp(map.Y(annotation.Value), plot.Top, plot.Bottom);
                var y2 = Clamp(map.Y(annotation.EndValue.Value), plot.Top, plot.Bottom);
                c.FillRect(plot.Left, Math.Min(y1, y2), plot.Width, Math.Abs(y2 - y1), color);
                DrawTinyAnnotationLabel(c, chart, annotation, plot, plot.Left + 8, Math.Min(y1, y2) + 8);
            } else if (annotation.Kind == ChartAnnotationKind.VerticalBand) {
                var x1 = Clamp(map.X(annotation.Value), plot.Left, plot.Right);
                var x2 = Clamp(map.X(annotation.EndValue.Value), plot.Left, plot.Right);
                c.FillRect(Math.Min(x1, x2), plot.Top, Math.Abs(x2 - x1), plot.Height, color);
                DrawTinyAnnotationLabel(c, chart, annotation, plot, Math.Min(x1, x2) + 8, plot.Top + 8);
            }
        }
    }

    private static void DrawAnnotationLines(RgbaCanvas c, Chart chart, ChartRect plot, ChartMapper map) {
        foreach (var annotation in chart.Annotations) {
            if (annotation.Kind == ChartAnnotationKind.HorizontalLine) {
                var y = Clamp(map.Y(annotation.Value), plot.Top, plot.Bottom);
                c.DrawDashedLine(plot.Left, y, plot.Right, y, annotation.Color, 1);
                DrawTinyAnnotationPill(c, chart, annotation, plot.Right - 4, y - 7, "end");
            } else if (annotation.Kind == ChartAnnotationKind.VerticalLine) {
                var x = Clamp(map.X(annotation.Value), plot.Left, plot.Right);
                c.DrawDashedLine(x, plot.Top, x, plot.Bottom, annotation.Color, 1);
                DrawTinyAnnotationPill(c, chart, annotation, x + 8, plot.Top + 16, "start");
            }
        }
    }

    private static void DrawTinyAnnotationLabel(RgbaCanvas c, Chart chart, ChartAnnotation annotation, ChartRect plot, double x, double y) {
        if (string.IsNullOrWhiteSpace(annotation.Label)) return;
        var theme = chart.Options.Theme;
        var fontSize = PngTickFontSize(chart);
        var textWidth = EstimatePngTextWidth(annotation.Label, fontSize);
        var width = Math.Max(34, textWidth + 16);
        var height = EstimatePngTextHeight(fontSize) + 10;
        var rectX = Clamp(x, plot.Left + 4, plot.Right - width - 4);
        var rectY = Clamp(y, plot.Top + 4, plot.Bottom - height - 4);
        var fillAlpha = theme.CardBackground.A == 0 ? (byte)220 : theme.CardBackground.A;
        var fill = ChartColor.FromRgba(theme.CardBackground.R, theme.CardBackground.G, theme.CardBackground.B, fillAlpha);
        var border = ChartColor.FromRgba(annotation.Color.R, annotation.Color.G, annotation.Color.B, 120);
        c.FillRoundedRect(rectX, rectY, width, height, Math.Min(6, height / 2), fill);
        c.StrokeRoundedRect(rectX, rectY, width, height, Math.Min(6, height / 2), border);
        c.DrawText(rectX + 8, rectY + (height - fontSize) / 2, annotation.Label, annotation.Color, fontSize);
    }

    private static void DrawTinyAnnotationPill(RgbaCanvas c, Chart chart, ChartAnnotation annotation, double x, double y, string anchor) {
        if (string.IsNullOrWhiteSpace(annotation.Label)) return;
        var theme = chart.Options.Theme;
        var fontSize = PngTickFontSize(chart);
        var textWidth = EstimatePngTextWidth(annotation.Label, fontSize);
        var width = Math.Max(34, textWidth + 16);
        var height = EstimatePngTextHeight(fontSize) + 10;
        var rectX = anchor == "end" ? x - width : x;
        rectX = Clamp(rectX, chart.Options.Padding.Left, chart.Options.Size.Width - chart.Options.Padding.Right - width);
        var rectY = Clamp(y - height / 2, 18, chart.Options.Size.Height - chart.Options.Padding.Bottom - height);
        var fill = ChartColor.FromRgba(theme.CardBackground.R, theme.CardBackground.G, theme.CardBackground.B, theme.CardBackground.A == 0 ? (byte)224 : theme.CardBackground.A);
        var border = ChartColor.FromRgba(annotation.Color.R, annotation.Color.G, annotation.Color.B, 110);

        c.FillRoundedRect(rectX, rectY, width, height, Math.Min(6, height / 2), fill);
        c.StrokeRoundedRect(rectX, rectY, width, height, Math.Min(6, height / 2), border);
        c.DrawText(rectX + 8, rectY + (height - fontSize) / 2, annotation.Label, annotation.Color, fontSize);
    }

    private static void DrawHorizontalBarGrid(RgbaCanvas c, Chart chart, ChartRect plot, ChartMapper map, IReadOnlyList<double> xTicks, IReadOnlyList<double> categories) {
        var o = chart.Options;
        var t = o.Theme;
        foreach (var xv in xTicks) {
            var x = map.X(xv);
            var label = FormatValue(chart, xv);
            if (o.ShowGrid) c.DrawLine(x, plot.Top, x, plot.Bottom, ChartColor.FromRgba(t.Grid.R, t.Grid.G, t.Grid.B, (byte)(t.Grid.A / 2)), 1);
            if (o.ShowAxes) DrawXAxisTickLabel(c, chart, plot, label, x);
        }

        foreach (var category in categories) {
            var y = map.Y(category);
            if (o.ShowGrid) c.DrawLine(plot.Left, y, plot.Right, y, ChartColor.FromRgba(t.Grid.R, t.Grid.G, t.Grid.B, (byte)(t.Grid.A / 3)), 1);
            if (o.ShowAxes) DrawHorizontalCategoryLabel(c, chart, plot, FormatX(chart, category), y);
        }

        if (o.ShowAxes) {
            var zeroX = map.X(0);
            if (zeroX > plot.Left && zeroX < plot.Right) c.DrawLine(zeroX, plot.Top, zeroX, plot.Bottom, t.Axis, 1);
            c.DrawLine(plot.Left, plot.Bottom, plot.Right, plot.Bottom, t.Axis, 1);
            c.DrawLine(plot.Left, plot.Top, plot.Left, plot.Bottom, t.Axis, 1);
            DrawAxisTitles(c, chart, plot);
        }
    }

    private static void DrawSeries(RgbaCanvas c, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var s = chart.Series[index]; var color = s.Color ?? chart.Options.Theme.Palette[index % chart.Options.Theme.Palette.Length];
        if (s.Kind == ChartSeriesKind.HorizontalBar) {
            var layout = HorizontalBarLayout(chart, plot, index);
            var zeroX = Math.Min(plot.Right, Math.Max(plot.Left, map.X(0)));
            foreach (var p in s.Points) {
                var valueX = map.X(p.Y);
                var left = Math.Min(zeroX, valueX);
                var width = Math.Abs(valueX - zeroX);
                var y = map.Y(p.X) + layout.Offset - layout.BarHeight / 2;
                DrawGradientBar(c, left, y, width, layout.BarHeight, Math.Min(7, layout.BarHeight / 2), color);
                if (chart.Options.ShowDataLabels) {
                    var label = FormatValue(chart, p.Y);
                    var labelFontSize = HorizontalValueLabelFontSize(chart);
                    var labelWidth = EstimatePngEmphasizedTextWidth(label, labelFontSize);
                    var labelX = p.Y >= 0 ? Math.Min(plot.Right - labelWidth - 2, left + width + 8) : Math.Max(plot.Left + 2, left - labelWidth - 8);
                    DrawReadablePngLabel(c, plot, labelX, y + layout.BarHeight / 2 - labelFontSize / 2.0, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), labelFontSize);
                }
            }

            return;
        }

        if (s.Kind == ChartSeriesKind.Bar) {
            var layout = BarLayout(chart, plot, index);
            var zeroY = Math.Min(plot.Bottom, Math.Max(plot.Top, map.Y(0)));
            foreach (var p in s.Points) {
                var baseValue = chart.Options.BarMode == ChartBarMode.Stacked ? StackBaseValue(chart, index, p) : 0;
                var y = map.Y(baseValue + p.Y);
                var baseY = chart.Options.BarMode == ChartBarMode.Stacked ? map.Y(baseValue) : zeroY;
                var barX = map.X(p.X) + layout.Offset - layout.BarWidth / 2;
                var barY = Math.Min(y, baseY);
                var barHeight = Math.Abs(baseY - y);
                var radius = chart.Options.BarMode == ChartBarMode.Stacked ? Math.Min(3, layout.BarWidth / 2) : Math.Min(7, layout.BarWidth / 2);
                DrawGradientBar(c, barX, barY, layout.BarWidth, barHeight, radius, color);
                if (chart.Options.ShowDataLabels) {
                    var label = FormatValue(chart, p.Y);
                    var segmentHeight = barHeight;
                    var fontSize = chart.Options.Theme.DataLabelFontSize;
                    if (chart.Options.BarMode == ChartBarMode.Stacked && segmentHeight < fontSize + 8) continue;
                    var labelY = chart.Options.BarMode == ChartBarMode.Stacked ? barY + segmentHeight / 2 - fontSize / 2.0 : p.Y >= 0 ? barY - 10 - fontSize : barY + segmentHeight + 10 - fontSize;
                    DrawReadablePngLabel(c, plot, map.X(p.X) + layout.Offset - EstimatePngEmphasizedTextWidth(label, fontSize) / 2.0, labelY, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize);
                }
            }
            return;
        }
        if (s.Kind == ChartSeriesKind.Area && s.Points.Count > 0) {
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
        for (var i = 1; i < linePoints.Count; i++) {
            var a = linePoints[i - 1]; var b = linePoints[i];
            c.DrawLine(a.X, a.Y, b.X, b.Y, color, Math.Max(1, (int)Math.Round(s.StrokeWidth)));
        }
        if (s.Kind == ChartSeriesKind.Scatter || s.Kind == ChartSeriesKind.Line) foreach (var p in s.Points) DrawMarker(c, chart, map.X(p.X), map.Y(p.Y), 4, color);
        if (chart.Options.ShowDataLabels) {
            foreach (var p in s.Points) {
                var label = FormatValue(chart, p.Y);
                var fontSize = chart.Options.Theme.DataLabelFontSize;
                var labelX = map.X(p.X) - EstimatePngEmphasizedTextWidth(label, fontSize) / 2.0;
                var aboveY = map.Y(p.Y) - fontSize - 4;
                var belowY = map.Y(p.Y) + 8;
                var labelY = aboveY < plot.Top + 2 ? belowY : aboveY;
                DrawReadablePngLabel(c, plot, labelX, labelY, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize);
            }
        }
    }

    private static void DrawGradientBar(RgbaCanvas c, double x, double y, double width, double height, double radius, ChartColor color) {
        if (width <= 0.5 || height <= 0.5) return;
        var top = Blend(ChartColor.White, color, 0.88);
        var bottom = Blend(ChartColor.Black, color, 0.94);
        c.FillRoundedRectVerticalGradient(x, y, width, height, radius, top, bottom);
        c.DrawLine(x + 1, y + 1, x + width - 1, y + 1, ChartColor.FromRgba(255, 255, 255, 38), 1);
    }

    private static void DrawMarker(RgbaCanvas c, Chart chart, double x, double y, double radius, ChartColor color) {
        c.DrawCircle(x, y, radius + 1.8, chart.Options.Theme.CardBackground);
        c.DrawCircle(x, y, radius, color);
    }

    private static void DrawPieLike(RgbaCanvas c, Chart chart, ChartRect plot) {
        var series = chart.Series[0];
        var values = new List<ChartPoint>();
        foreach (var point in series.Points) if (point.Y > 0) values.Add(point);
        if (values.Count == 0) return;

        var total = 0d;
        foreach (var value in values) total += value.Y;
        var radius = Math.Max(1, Math.Min(plot.Width, plot.Height) * 0.38);
        var cx = plot.Left + plot.Width * 0.42;
        var cy = plot.Top + plot.Height / 2;
        var inner = series.Kind == ChartSeriesKind.Donut ? radius * 0.58 : 0;
        var start = -Math.PI / 2;
        var separator = chart.Options.Theme.CardBackground;

        for (var i = 0; i < values.Count; i++) {
            var sweep = values[i].Y / total * Math.PI * 2;
            var end = start + sweep;
            var color = chart.Options.Theme.Palette[i % chart.Options.Theme.Palette.Length];
            c.FillRingSlice(cx, cy, radius, inner, start, end, color);
            DrawSliceSeparator(c, cx, cy, radius, inner, start, separator);
            if (chart.Options.ShowDataLabels && sweep > 0.22) {
                var mid = start + sweep / 2;
                var labelRadius = inner > 0 ? (inner + radius) / 2 : radius * 0.66;
                var label = FormatPercent(values[i].Y / total);
                var fontSize = chart.Options.Theme.DataLabelFontSize;
                DrawReadablePngLabel(c, cx + Math.Cos(mid) * labelRadius - EstimatePngEmphasizedTextWidth(label, fontSize) / 2.0, cy + Math.Sin(mid) * labelRadius - fontSize / 2.0, label, chart.Options.Theme.CardBackground, chart.Options.Theme.Text, fontSize);
            }

            start = end;
        }

        c.DrawCircleOutline(cx, cy, radius, separator, 2);
        if (inner > 0) c.DrawCircleOutline(cx, cy, inner, separator, 2);

        if (series.Kind == ChartSeriesKind.Donut) {
            var totalLabel = FormatValue(chart, total);
            const double totalFontSize = 24;
            var nameFontSize = chart.Options.Theme.TickLabelFontSize;
            var centerLabelWidth = Math.Max(24, inner * 1.55);
            DrawPngTextEmphasizedCenteredX(c, cx, cy - totalFontSize / 2.0 - 2, totalLabel, chart.Options.Theme.Text, totalFontSize, centerLabelWidth);
            DrawPngTextEmphasizedCenteredX(c, cx, cy + 19 - nameFontSize + 1, series.Name, chart.Options.Theme.MutedText, nameFontSize, centerLabelWidth);
        }

        if (chart.Options.ShowLegend) DrawSliceLegend(c, chart, values, plot, total);
    }

    private static void DrawSliceSeparator(RgbaCanvas c, double cx, double cy, double outerRadius, double innerRadius, double angle, ChartColor color) {
        var startRadius = Math.Max(0, innerRadius - 0.5);
        c.DrawLine(cx + Math.Cos(angle) * startRadius, cy + Math.Sin(angle) * startRadius, cx + Math.Cos(angle) * outerRadius, cy + Math.Sin(angle) * outerRadius, color, 2);
    }

    private static void DrawSliceLegend(RgbaCanvas c, Chart chart, IReadOnlyList<ChartPoint> values, ChartRect plot, double total) {
        var fontSize = PngLegendFontSize(chart);
        const double swatchSize = 10;
        var x = plot.Left + plot.Width * 0.72;
        var y = plot.Top + Math.Max(24, plot.Height * 0.18);
        for (var i = 0; i < values.Count; i++) {
            var color = chart.Options.Theme.Palette[i % chart.Options.Theme.Palette.Length];
            var percent = FormatPercent(values[i].Y / total);
            var label = SliceLabel(chart, values[i], i);
            var labelMaxWidth = Math.Max(12, plot.Right - 36 - (x + swatchSize + 8) - EstimatePngTextWidth(percent, fontSize));
            var labelFontSize = TextFontSizeForEmphasizedWidth(label, labelMaxWidth, fontSize);
            label = TrimReadablePngLabelToWidth(label, labelFontSize, labelMaxWidth);
            c.FillRect(x, y - swatchSize + 1, swatchSize, swatchSize, color);
            if (label.Length > 0) c.DrawTextEmphasized(x + swatchSize + 8, y - labelFontSize + 3, label, chart.Options.Theme.Text, labelFontSize);
            c.DrawText(plot.Right - EstimatePngTextWidth(percent, fontSize) - 12, y - fontSize + 3, percent, chart.Options.Theme.MutedText, fontSize);
            y += fontSize + 10;
        }
    }

    private static string FormatNumber(double v) => Math.Abs(v) >= 1000 ? (v / 1000).ToString("0.#", CultureInfo.InvariantCulture) + "K" : v.ToString("0.#", CultureInfo.InvariantCulture);
    private static string FormatValue(Chart chart, double value) {
        var formatter = chart.Options.ValueFormatter;
        if (formatter == null) return FormatNumber(value);
        return formatter(value) ?? string.Empty;
    }
    private static string FormatPercent(double v) => v.ToString("0.#%", CultureInfo.InvariantCulture);
    private static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(max, value));

    private static byte[] WritePng(RgbaCanvas canvas) => PngWriter.WriteRgba(canvas.Width, canvas.Height, canvas.ToOutputPixels());

    private static IReadOnlyList<double> GetXTicks(Chart chart, ChartRange range, ChartRect plot) {
        if (chart.Options.XAxisLabels.Count == 0) return ChartTicks.GenerateInside(range.MinX, range.MaxX, chart.Options.TickCount);
        var labels = new List<ChartAxisLabel>();
        foreach (var label in chart.Options.XAxisLabels) {
            if (label.Value >= range.MinX && label.Value <= range.MaxX) labels.Add(label);
        }

        labels.Sort((left, right) => left.Value.CompareTo(right.Value));
        if (chart.Options.XAxisLabelDensity == ChartLabelDensity.All || labels.Count < 3) return LabelValues(labels);

        var fontSize = PngTickFontSize(chart);
        var widest = 0.0;
        foreach (var label in labels) widest = Math.Max(widest, EstimatePngTextWidth(label.Text, fontSize));
        var densityFactor = chart.Options.XAxisLabelDensity == ChartLabelDensity.Dense ? 0.72 : chart.Options.XAxisLabelDensity == ChartLabelDensity.Relaxed ? 1.35 : 1.0;
        var minSpacing = Math.Max(28, (widest + 18) * densityFactor);
        var maxCount = Math.Max(2, (int)Math.Floor(plot.Width / minSpacing) + 1);
        if (labels.Count <= maxCount && LabelsHaveMinimumLabelGap(labels, range, plot, fontSize, 6)) return LabelValues(labels);

        var lastLabel = labels[labels.Count - 1];
        var step = Math.Max(1, (int)Math.Ceiling((labels.Count - 1) / (double)(maxCount - 1)));
        var selected = new List<ChartAxisLabel>();
        selected.Add(labels[0]);
        for (var i = step; i < labels.Count - 1; i += step) {
            if (LabelGap(selected[selected.Count - 1], labels[i], range, plot, fontSize) >= 6 && LabelGap(labels[i], lastLabel, range, plot, fontSize) >= 6) selected.Add(labels[i]);
        }

        if (selected.Count > 1 && LabelGap(selected[selected.Count - 1], lastLabel, range, plot, fontSize) < 6) selected.RemoveAt(selected.Count - 1);
        selected.Add(lastLabel);
        return LabelValues(selected);
    }

    private static bool LabelsHaveMinimumLabelGap(IReadOnlyList<ChartAxisLabel> labels, ChartRange range, ChartRect plot, double fontSize, double minGap) {
        for (var i = 1; i < labels.Count; i++) {
            if (LabelGap(labels[i - 1], labels[i], range, plot, fontSize) < minGap) return false;
        }

        return true;
    }

    private static double LabelGap(ChartAxisLabel left, ChartAxisLabel right, ChartRange range, ChartRect plot, double fontSize) {
        var leftWidth = EstimatePngTextWidth(left.Text, fontSize);
        var rightWidth = EstimatePngTextWidth(right.Text, fontSize);
        var leftX = Clamp(ProjectX(left.Value, range, plot) - leftWidth / 2.0, plot.Left + 2, plot.Right - leftWidth - 2);
        var rightX = Clamp(ProjectX(right.Value, range, plot) - rightWidth / 2.0, plot.Left + 2, plot.Right - rightWidth - 2);
        return rightX - (leftX + leftWidth);
    }

    private static IReadOnlyList<double> LabelValues(IReadOnlyList<ChartAxisLabel> labels) {
        var values = new List<double>(labels.Count);
        for (var i = 0; i < labels.Count; i++) values.Add(labels[i].Value);
        return values;
    }

    private static IReadOnlyList<double> GetHorizontalCategoryTicks(Chart chart, ChartRange range) {
        var categories = new SortedSet<double>();
        foreach (var series in chart.Series) {
            if (series.Kind != ChartSeriesKind.HorizontalBar) continue;
            foreach (var point in series.Points) {
                if (point.X >= range.MinY && point.X <= range.MaxY) categories.Add(point.X);
            }
        }

        if (categories.Count > 0) {
            var values = new List<double>();
            foreach (var category in categories) values.Add(category);
            return values;
        }

        return ChartTicks.GenerateInside(range.MinY, range.MaxY, chart.Options.TickCount);
    }

    private static string FormatX(Chart chart, double value) {
        foreach (var label in chart.Options.XAxisLabels) {
            if (Math.Abs(label.Value - value) < 0.000001) return label.Text;
        }

        return FormatNumber(value);
    }

    private static bool IsPieLike(Chart chart) => chart.Series.Count > 0 && (chart.Series[0].Kind == ChartSeriesKind.Pie || chart.Series[0].Kind == ChartSeriesKind.Donut);
    private static bool IsHorizontalBarChart(Chart chart) {
        foreach (var series in chart.Series) if (series.Kind == ChartSeriesKind.HorizontalBar) return true;
        return false;
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

        var groupCount = Math.Max(1, horizontalSeries.Count);
        var groupPosition = Math.Max(0, horizontalSeries.IndexOf(seriesIndex));
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

    private static void DrawStackTotals(RgbaCanvas c, Chart chart, ChartRect plot, ChartMapper map) {
        var positiveTotals = new Dictionary<double, double>();
        var negativeTotals = new Dictionary<double, double>();
        foreach (var series in chart.Series) {
            if (series.Kind != ChartSeriesKind.Bar) continue;
            foreach (var point in series.Points) AddStackTotal(point.Y >= 0 ? positiveTotals : negativeTotals, point.X, point.Y);
        }

        DrawStackTotalSet(c, chart, plot, map, positiveTotals, -12);
        DrawStackTotalSet(c, chart, plot, map, negativeTotals, 8);
    }

    private static void DrawStackTotalSet(RgbaCanvas c, Chart chart, ChartRect plot, ChartMapper map, Dictionary<double, double> totals, double offset) {
        foreach (var item in totals) {
            if (Math.Abs(item.Value) < 0.000001) continue;
            var label = FormatValue(chart, item.Value);
            var fontSize = chart.Options.Theme.DataLabelFontSize;
            var width = EstimatePngEmphasizedTextWidth(label, fontSize);
            var x = Clamp(map.X(item.Key) - width / 2.0, plot.Left + 2, plot.Right - width - 2);
            var y = Clamp(map.Y(item.Value) + offset - fontSize / 2.0, plot.Top + 2, plot.Bottom - fontSize - 2);
            DrawReadablePngLabel(c, x, y, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize);
        }
    }

    private static void AddStackTotal(Dictionary<double, double> totals, double x, double y) {
        double current;
        totals.TryGetValue(x, out current);
        totals[x] = current + y;
    }

    private static string SliceLabel(Chart chart, ChartPoint point, int index) {
        foreach (var label in chart.Options.XAxisLabels) {
            if (Math.Abs(label.Value - point.X) < 0.000001) return label.Text;
        }

        return "Slice " + (index + 1);
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
