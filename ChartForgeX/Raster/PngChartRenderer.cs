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
    public byte[] Render(Chart chart) => WritePng(RenderCanvas(chart));

    internal RgbaCanvas RenderCanvas(Chart chart) {
        ChartGuards.RenderCompatibility(chart);
        var o = chart.Options; var t = o.Theme;
        var outlineFont = TrueTypeFont.TryLoadFromPath(o.PngFontPath, o.PngFontCollectionIndex, o.PngFontFaceName);
        var previousOutlineFont = CurrentOutlineFont;
        CurrentOutlineFont = outlineFont;
        try {
            var c = new RgbaCanvas(o.Size.Width, o.Size.Height, o.PngSupersamplingScale, outlineFont, o.PngOutputScale);
            c.Clear(o.TransparentBackground ? ChartColor.Transparent : t.Background);
            if (o.ShowCard && t.UseCard) DrawCardSurface(c, o, t);
            var plot = IsSpatialMapChart(chart) ? SpatialMapPlotArea(chart) : ChartLayout.PlotArea(o);
            if (o.ShowHeader) DrawHeader(c, chart);
            void DrawSpecialChart(Action<RgbaCanvas, Chart, ChartRect> draw) {
                var legendPlot = ApplyPngLegendReserve(chart, plot);
                DrawPlotSurface(c, o, t, legendPlot);
                draw(c, chart, legendPlot);
                DrawLegend(c, chart);
            }

            if (IsPieLike(chart)) {
                DrawPlotSurface(c, o, t, plot);
                DrawPieLike(c, chart, plot);
                return c;
            }
            if (IsGaugeChart(chart)) {
                DrawSpecialChart(DrawGauge);
                return c;
            }
            if (IsCircleChart(chart)) {
                DrawSpecialChart(DrawCircleChart);
                return c;
            }
            if (IsRadialBarChart(chart)) {
                DrawPlotSurface(c, o, t, plot);
                DrawRadialBar(c, chart, plot);
                return c;
            }
            if (IsLayeredRadialChart(chart)) {
                DrawSpecialChart(DrawLayeredRadial);
                return c;
            }
            if (IsBulletChart(chart)) {
                DrawSpecialChart(DrawBullet);
                return c;
            }
            if (IsWaterfallChart(chart)) {
                DrawPlotSurface(c, o, t, plot);
                DrawWaterfall(c, chart, plot);
                return c;
            }
            if (IsRadarChart(chart)) {
                DrawSpecialChart(DrawRadar);
                return c;
            }
            if (IsPolarAreaChart(chart)) {
                DrawPlotSurface(c, o, t, plot);
                DrawPolarArea(c, chart, plot);
                return c;
            }
            if (IsFunnelChart(chart)) {
                DrawSpecialChart(DrawFunnel);
                return c;
            }
            if (IsTreemapChart(chart)) {
                DrawSpecialChart(DrawTreemap);
                return c;
            }
            if (IsPictorialChart(chart)) {
                DrawSpecialChart(DrawPictorial);
                return c;
            }
            if (IsProgressBarChart(chart)) {
                DrawSpecialChart(DrawProgressBar);
                return c;
            }
            if (IsWordCloudChart(chart)) {
                DrawSpecialChart(DrawWordCloud);
                return c;
            }
            if (IsHeatmapChart(chart)) {
                DrawSpecialChart(DrawHeatmap);
                return c;
            }
            if (IsHexbinHeatmapChart(chart)) {
                DrawSpecialChart(DrawHexbinHeatmap);
                return c;
            }
            if (IsCalendarHeatmapChart(chart)) {
                DrawSpecialChart(DrawCalendarHeatmap);
                return c;
            }
            if (IsDottedMapChart(chart)) {
                DrawSpecialChart(DrawDottedMap);
                return c;
            }
            if (IsRegionMapChart(chart)) {
                DrawSpecialChart(DrawRegionMap);
                return c;
            }
            if (IsTileMapChart(chart)) {
                DrawSpecialChart(DrawTileMap);
                return c;
            }
            if (IsTimelineChart(chart)) {
                DrawSpecialChart(DrawTimeline);
                return c;
            }
            if (IsGanttChart(chart)) {
                DrawSpecialChart(DrawGantt);
                return c;
            }
            if (IsSankeyChart(chart)) {
                DrawSpecialChart(DrawSankey);
                return c;
            }
            if (IsTreeChart(chart)) {
                DrawSpecialChart(DrawTree);
                return c;
            }
            if (IsSunburstChart(chart)) {
                DrawSpecialChart(DrawSunburst);
                return c;
            }
            var range = ChartRange.FromChart(chart);
            IReadOnlyList<double> yTicks;
            IReadOnlyList<double> xTicks;
            ChartRange? secondaryRange = null;
            IReadOnlyList<double>? secondaryTicks = null;
            if (IsHorizontalBarChart(chart)) {
                xTicks = ChartTicks.Generate(range.MinX, range.MaxX, o.TickCount);
                ApplyHorizontalValueBounds(chart, range, xTicks);
                yTicks = GetHorizontalCategoryTicks(chart, range);
                plot = ApplyHorizontalBarReserve(chart, plot, yTicks);
                if (ShowXAxis(chart)) plot = ApplyBottomReserve(chart, plot, xTicks, true);
                DrawPlotSurface(c, o, t, plot);
            } else {
                yTicks = ChartTicks.Generate(range.MinY, range.MaxY, o.TickCount);
                range.SetYBounds(yTicks[0], yTicks[yTicks.Count - 1]);
                if (ShowYAxis(chart)) plot = ApplyYAxisLabelReserve(chart, plot, yTicks);
                if (HasSecondaryYAxis(chart)) {
                    secondaryRange = ChartRange.FromSecondaryYAxis(chart, range);
                    secondaryTicks = ChartTicks.Generate(secondaryRange.MinY, secondaryRange.MaxY, o.TickCount);
                    secondaryRange.SetYBounds(secondaryTicks[0], secondaryTicks[secondaryTicks.Count - 1]);
                    plot = ApplySecondaryYAxisLabelReserve(chart, plot, secondaryTicks);
                }

                xTicks = GetXTicks(chart, range, plot);
                if (ShowXAxis(chart)) plot = ApplyBottomReserve(chart, plot, xTicks, false);
                DrawPlotSurface(c, o, t, plot);
            }

            var map = new ChartMapper(plot, range);
            var secondaryMap = secondaryRange == null ? null : new ChartMapper(plot, secondaryRange);
            if (IsHorizontalBarChart(chart)) {
                DrawHorizontalBarGrid(c, chart, plot, map, xTicks, yTicks);
                for (var i = 0; i < chart.Series.Count; i++) DrawSeries(c, chart, i, plot, map);
                if (o.BarMode == ChartBarMode.Stacked && o.ShowStackTotals) DrawHorizontalStackTotals(c, chart, plot, map);
                DrawLegend(c, chart);
                return c;
            }

            DrawAnnotationBands(c, chart, plot, map);
            foreach (var yv in yTicks) {
                var y = map.Y(yv);
                if (o.ShowGrid) c.DrawLine(plot.Left, y, plot.Right, y, t.Grid, ChartVisualPrimitives.GridStrokeWidth);
                if (ShowYAxis(chart)) {
                    var label = FormatValue(chart, yv);
                    var fontSize = PngTickFontSize(chart);
                    DrawPngTextStyled(c, Math.Max(2, plot.Left - EstimatePngTextWidth(label, fontSize) - 8), y - fontSize + 4, label, o.TickLabelStyle, t.MutedText, fontSize, emphasized: false);
                }
            }
            var xLabels = XAxisTickLabels(chart, xTicks, false);
            for (var i = 0; i < xTicks.Count; i++) {
                var x = map.X(xTicks[i]);
                var label = xLabels[i];
                if (o.ShowGrid) c.DrawLine(x, plot.Top, x, plot.Bottom, ApplyOpacity(t.Grid, ChartVisualPrimitives.GridVerticalOpacity), ChartVisualPrimitives.GridStrokeWidth);
                if (ShowXAxis(chart)) DrawXAxisTickLabel(c, chart, plot, label, x, xLabels);
            }
            if (ShowXAxis(chart) && ShowAxisLines(chart)) {
                var zeroY = map.Y(0);
                if (zeroY > plot.Top && zeroY < plot.Bottom) c.DrawLine(plot.Left, zeroY, plot.Right, zeroY, t.Axis, ChartVisualPrimitives.ZeroAxisStrokeWidth);
            }
            if (ShowXAxis(chart)) {
                if (ShowAxisLines(chart)) c.DrawLine(plot.Left, plot.Bottom, plot.Right, plot.Bottom, t.Axis, ChartVisualPrimitives.AxisStrokeWidth);
                DrawPngXAxisTitle(c, chart, plot, plot.Bottom + PngXAxisTitleOffset(chart, xLabels), PngXAxisTitleFontSize(chart));
            }
            if (ShowYAxis(chart)) {
                if (ShowAxisLines(chart)) c.DrawLine(plot.Left, plot.Top, plot.Left, plot.Bottom, t.Axis, ChartVisualPrimitives.AxisStrokeWidth);
                DrawYAxisTitle(c, chart, plot, PngAxisTitleFontSize(chart));
            }
            if (chart.Options.ShowAxes) {
                if (secondaryMap != null && secondaryTicks != null) DrawSecondaryYAxis(c, chart, plot, secondaryMap, secondaryTicks);
            }
            for (var i = 0; i < chart.Series.Count; i++) DrawSeries(c, chart, i, plot, SeriesMap(chart.Series[i], map, secondaryMap));
            if (o.BarMode == ChartBarMode.Stacked && o.ShowStackTotals) DrawStackTotals(c, chart, plot, map);
            DrawAnnotationLines(c, chart, plot, map);
            DrawLegend(c, chart);
            return c;
        } finally {
            CurrentOutlineFont = previousOutlineFont;
        }
    }

    private static void DrawCardSurface(RgbaCanvas c, ChartOptions options, ChartTheme theme) {
        DrawCardShadow(c, 14, 14, options.Size.Width - 28, options.Size.Height - 28, theme.CornerRadius, theme);
        c.FillRoundedRect(14, 14, options.Size.Width - 28, options.Size.Height - 28, theme.CornerRadius, theme.CardBackground);
        if (theme.CardBorder.A > 0) c.StrokeRoundedRect(14, 14, options.Size.Width - 28, options.Size.Height - 28, theme.CornerRadius, theme.CardBorder);
    }

    private static void DrawCardShadow(RgbaCanvas c, double x, double y, double width, double height, double radius, ChartTheme theme) {
        if (theme.ShadowOpacity <= 0) return;
        DrawShadowLayer(c, x - 1.5, y + 5, width + 3, height, radius + 1.5, theme.ShadowOpacity * 0.24);
        DrawShadowLayer(c, x - 0.75, y + 2.5, width + 1.5, height, radius + 0.75, theme.ShadowOpacity * 0.34);
    }

    private static void DrawShadowLayer(RgbaCanvas c, double x, double y, double width, double height, double radius, double opacity) {
        var alpha = (byte)Math.Max(0, Math.Min(255, Math.Round(255 * opacity)));
        if (alpha == 0) return;
        c.FillRoundedRect(x, y, width, height, radius, ChartColor.FromRgba(15, 23, 42, alpha));
    }

    private static void DrawPlotSurface(RgbaCanvas c, ChartOptions options, ChartTheme theme, ChartRect plot) {
        if (options.ShowPlotBackground) c.FillRoundedRect(plot.X, plot.Y, plot.Width, plot.Height, theme.PlotCornerRadius, theme.PlotBackground);
        if (options.ShowPlotBackground && theme.PlotBorder.A > 0) c.StrokeRoundedRect(plot.X, plot.Y, plot.Width, plot.Height, theme.PlotCornerRadius, theme.PlotBorder);
    }

    private static void DrawHeader(RgbaCanvas c, Chart chart) {
        var theme = chart.Options.Theme;
        var maxWidth = Math.Max(24, chart.Options.Size.Width - 80);
        var titleStyle = chart.Options.TitleStyle;
        var titleFontSize = TextFontSizeForEmphasizedWidth(chart.Title, maxWidth, PngStyleFontSize(titleStyle, theme.TitleFontSize));
        var title = TrimReadablePngLabelToWidth(chart.Title, titleFontSize, maxWidth);
        if (title.Length > 0) DrawPngTextStyled(c, 40, 52 - titleFontSize + 1, title, titleStyle, theme.Text, titleFontSize, emphasized: true);
        if (!string.IsNullOrWhiteSpace(chart.Subtitle)) {
            var subtitleStyle = chart.Options.SubtitleStyle;
            var subtitleMaxWidth = Math.Max(24, chart.Options.Size.Width - 84);
            var subtitleFontSize = TextFontSizeForWidth(chart.Subtitle, subtitleMaxWidth, PngStyleFontSize(subtitleStyle, theme.SubtitleFontSize));
            var subtitle = TrimPngLabelToWidth(chart.Subtitle, subtitleFontSize, subtitleMaxWidth);
            if (subtitle.Length > 0) DrawPngTextStyled(c, 42, 79 - subtitleFontSize + 1, subtitle, subtitleStyle, theme.MutedText, subtitleFontSize, emphasized: false);
        }
    }

    private static bool IsLineLikeLegend(ChartSeriesKind kind) =>
        kind == ChartSeriesKind.Line || kind == ChartSeriesKind.StepLine || kind == ChartSeriesKind.Area || kind == ChartSeriesKind.StepArea || kind == ChartSeriesKind.StackedArea || kind == ChartSeriesKind.Slope || kind == ChartSeriesKind.RangeBand || kind == ChartSeriesKind.RangeArea || kind == ChartSeriesKind.Lollipop || kind == ChartSeriesKind.Dumbbell || kind == ChartSeriesKind.ErrorBar || kind == ChartSeriesKind.Radar || kind == ChartSeriesKind.TrendLine;

    private static bool ShouldDrawDataLabels(Chart chart, ChartSeries series) => series.ShowDataLabels ?? chart.Options.ShowDataLabels;

    private static ChartColor SeriesColor(Chart chart, int index) => chart.Series[index].Color ?? chart.Options.Theme.Palette[index % chart.Options.Theme.Palette.Length];

    private static ChartColor PointColor(Chart chart, ChartSeries series, int seriesIndex, int pointIndex) =>
        pointIndex < series.PointColors.Count && series.PointColors[pointIndex].HasValue
            ? series.PointColors[pointIndex]!.Value
            : SeriesColor(chart, seriesIndex);

    private static ChartFillPattern FillPattern(ChartSeries series, int pointIndex) =>
        pointIndex >= 0 && pointIndex < series.PointFillPatterns.Count && series.PointFillPatterns[pointIndex].HasValue
            ? series.PointFillPatterns[pointIndex]!.Value
            : series.FillPattern;

    private static bool ShowXAxis(Chart chart) => !IsMapChart(chart) && chart.Options.ShowAxes && chart.Options.ShowXAxis;

    private static bool ShowYAxis(Chart chart) => !IsMapChart(chart) && chart.Options.ShowAxes && chart.Options.ShowYAxis;

    private static bool ShowAxisLines(Chart chart) => !IsMapChart(chart) && chart.Options.ShowAxes && chart.Options.ShowAxisLines;

    private static bool IsMapChart(Chart chart) => IsCalendarHeatmapChart(chart) || IsDottedMapChart(chart) || IsRegionMapChart(chart) || IsTileMapChart(chart);

    private static bool IsSpatialMapChart(Chart chart) => IsDottedMapChart(chart) || IsRegionMapChart(chart) || IsTileMapChart(chart);

    private static ChartRect SpatialMapPlotArea(Chart chart) {
        var o = chart.Options;
        var left = Math.Min(o.Padding.Left, 42);
        var right = Math.Min(o.Padding.Right, 42);
        var top = o.ShowHeader ? Math.Min(o.Padding.Top + 10, 88) : Math.Min(o.Padding.Top, 42);
        var bottom = Math.Min(o.Padding.Bottom, 42);
        return new ChartRect(left, top, Math.Max(1, o.Size.Width - left - right), Math.Max(1, o.Size.Height - top - bottom));
    }

    private static bool HasHorizontalBarDataLabels(Chart chart) {
        foreach (var series in chart.Series) if (series.Kind == ChartSeriesKind.HorizontalBar && ShouldDrawDataLabels(chart, series)) return true;
        return false;
    }

    private static bool HasSecondaryYAxis(Chart chart) {
        foreach (var series in chart.Series) {
            if (series.YAxis == ChartAxisSide.Secondary) return true;
        }

        return false;
    }

    private static ChartMapper SeriesMap(ChartSeries series, ChartMapper primaryMap, ChartMapper? secondaryMap) =>
        series.YAxis == ChartAxisSide.Secondary && secondaryMap != null ? secondaryMap : primaryMap;

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
                c.DrawDashedLine(plot.Left, y, plot.Right, y, annotation.Color, ChartVisualPrimitives.AnnotationLineStrokeWidth, ChartVisualPrimitives.AnnotationLineDash, ChartVisualPrimitives.AnnotationLineGap);
                DrawTinyAnnotationPill(c, chart, annotation, plot, plot.Right - 4, y - 7, "end");
            } else if (annotation.Kind == ChartAnnotationKind.VerticalLine) {
                var x = Clamp(map.X(annotation.Value), plot.Left, plot.Right);
                c.DrawDashedLine(x, plot.Top, x, plot.Bottom, annotation.Color, ChartVisualPrimitives.AnnotationLineStrokeWidth, ChartVisualPrimitives.AnnotationLineDash, ChartVisualPrimitives.AnnotationLineGap);
                DrawTinyAnnotationPill(c, chart, annotation, plot, x + 8, plot.Top + 16, "start");
            }
        }
    }

    private static void DrawTinyAnnotationLabel(RgbaCanvas c, Chart chart, ChartAnnotation annotation, ChartRect plot, double x, double y) {
        if (string.IsNullOrWhiteSpace(annotation.Label)) return;
        var theme = chart.Options.Theme;
        var maxTextWidth = Math.Max(24, plot.Width - 26);
        var fontSize = TextFontSizeForWidth(annotation.Label, maxTextWidth, PngTickFontSize(chart));
        var label = TrimPngLabelToWidth(annotation.Label, fontSize, maxTextWidth);
        if (label.Length == 0) return;
        var textWidth = EstimatePngTextWidth(label, fontSize);
        var width = Math.Max(34, textWidth + 16);
        var height = EstimatePngTextHeight(fontSize) + 10;
        var rectX = Clamp(x, plot.Left + 4, plot.Right - width - 4);
        var rectY = Clamp(y, plot.Top + 4, plot.Bottom - height - 4);
        var fillAlpha = theme.CardBackground.A == 0 ? (byte)220 : theme.CardBackground.A;
        var fill = ChartColor.FromRgba(theme.CardBackground.R, theme.CardBackground.G, theme.CardBackground.B, fillAlpha);
        var border = ChartColor.FromRgba(annotation.Color.R, annotation.Color.G, annotation.Color.B, 120);
        c.FillRoundedRect(rectX, rectY, width, height, Math.Min(6, height / 2), fill);
        c.StrokeRoundedRect(rectX, rectY, width, height, Math.Min(6, height / 2), border);
        c.DrawText(rectX + 8, rectY + (height - fontSize) / 2, label, annotation.Color, fontSize);
    }

    private static void DrawTinyAnnotationPill(RgbaCanvas c, Chart chart, ChartAnnotation annotation, ChartRect plot, double x, double y, string anchor) {
        if (string.IsNullOrWhiteSpace(annotation.Label)) return;
        var theme = chart.Options.Theme;
        var maxTextWidth = Math.Max(24, plot.Width - 26);
        var fontSize = TextFontSizeForWidth(annotation.Label, maxTextWidth, PngTickFontSize(chart));
        var label = TrimPngLabelToWidth(annotation.Label, fontSize, maxTextWidth);
        if (label.Length == 0) return;
        var textWidth = EstimatePngTextWidth(label, fontSize);
        var width = Math.Max(34, textWidth + 16);
        var height = EstimatePngTextHeight(fontSize) + 10;
        var rectX = anchor == "end" ? x - width : x;
        rectX = Clamp(rectX, plot.Left + 4, plot.Right - width - 4);
        var rectY = Clamp(y - height / 2, plot.Top + 4, plot.Bottom - height - 4);
        var fill = ChartColor.FromRgba(theme.CardBackground.R, theme.CardBackground.G, theme.CardBackground.B, theme.CardBackground.A == 0 ? (byte)224 : theme.CardBackground.A);
        var border = ChartColor.FromRgba(annotation.Color.R, annotation.Color.G, annotation.Color.B, 110);

        c.FillRoundedRect(rectX, rectY, width, height, Math.Min(6, height / 2), fill);
        c.StrokeRoundedRect(rectX, rectY, width, height, Math.Min(6, height / 2), border);
        c.DrawText(rectX + 8, rectY + (height - fontSize) / 2, label, annotation.Color, fontSize);
    }

    private static void DrawHorizontalBarGrid(RgbaCanvas c, Chart chart, ChartRect plot, ChartMapper map, IReadOnlyList<double> xTicks, IReadOnlyList<double> categories) {
        var o = chart.Options;
        var t = o.Theme;
        var xLabels = XAxisTickLabels(chart, xTicks, true);
        for (var i = 0; i < xTicks.Count; i++) {
            var x = map.X(xTicks[i]);
            var label = xLabels[i];
            if (o.ShowGrid) c.DrawLine(x, plot.Top, x, plot.Bottom, ApplyOpacity(t.Grid, ChartVisualPrimitives.HorizontalBarValueGridOpacity), ChartVisualPrimitives.GridStrokeWidth);
            if (ShowXAxis(chart)) DrawXAxisTickLabel(c, chart, plot, label, x, xLabels);
        }

        foreach (var category in categories) {
            var y = map.Y(category);
            if (o.ShowGrid) c.DrawLine(plot.Left, y, plot.Right, y, ApplyOpacity(t.Grid, ChartVisualPrimitives.HorizontalBarCategoryGridOpacity), ChartVisualPrimitives.GridStrokeWidth);
            if (ShowYAxis(chart)) DrawHorizontalCategoryLabel(c, chart, plot, FormatX(chart, category), y);
        }

        if (ShowXAxis(chart)) {
            var zeroX = map.X(0);
            if (ShowAxisLines(chart) && zeroX > plot.Left && zeroX < plot.Right) c.DrawLine(zeroX, plot.Top, zeroX, plot.Bottom, t.Axis, ChartVisualPrimitives.ZeroAxisStrokeWidth);
            if (ShowAxisLines(chart)) c.DrawLine(plot.Left, plot.Bottom, plot.Right, plot.Bottom, t.Axis, ChartVisualPrimitives.AxisStrokeWidth);
            DrawPngXAxisTitle(c, chart, plot, plot.Bottom + PngXAxisTitleOffset(chart, xLabels), PngXAxisTitleFontSize(chart));
        }
        if (ShowYAxis(chart)) {
            if (ShowAxisLines(chart)) c.DrawLine(plot.Left, plot.Top, plot.Left, plot.Bottom, t.Axis, ChartVisualPrimitives.AxisStrokeWidth);
            DrawYAxisTitle(c, chart, plot, PngAxisTitleFontSize(chart));
        }
    }

    private static string FormatNumber(double v) => ChartNumericFormatter.FormatCompact(v);
    private static string FormatValue(Chart chart, double value) {
        var formatter = chart.Options.ValueFormatter;
        if (formatter == null) return FormatNumber(value);
        return formatter(value) ?? string.Empty;
    }

    private static string FormatDataLabel(Chart chart, ChartSeries series, int pointIndex, double value) {
        if (pointIndex >= 0 && pointIndex < series.PointLabels.Count && series.PointLabels[pointIndex] != null) return series.PointLabels[pointIndex]!;
        return FormatValue(chart, value);
    }

    private static string FormatSecondaryValue(Chart chart, double value) {
        var formatter = chart.Options.SecondaryYAxisValueFormatter;
        if (formatter == null) return FormatValue(chart, value);
        return formatter(value) ?? string.Empty;
    }
    private static string FormatPercent(double v) => v.ToString("0.#%", CultureInfo.InvariantCulture);
    private static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(max, value));

    private static byte[] WritePng(RgbaCanvas canvas) => PngWriter.WriteRgba(canvas.OutputWidth, canvas.OutputHeight, canvas.ToOutputPixels());

    private static IReadOnlyList<double> GetXTicks(Chart chart, ChartRange range, ChartRect plot) {
        if (chart.Options.XAxisLabels.Count == 0) {
            var ticks = ChartTicks.GenerateInside(range.MinX, range.MaxX, chart.Options.TickCount);
            if (chart.Options.XAxisLabelDensity == ChartLabelDensity.All || ticks.Count < 3) return ticks;
            var generatedLabels = new List<ChartAxisLabel>(ticks.Count);
            foreach (var tick in ticks) generatedLabels.Add(new ChartAxisLabel(tick, FormatXAxisValue(chart, tick)));
            return SelectXAxisTickValues(chart, range, plot, generatedLabels);
        }

        var labels = new List<ChartAxisLabel>();
        foreach (var label in chart.Options.XAxisLabels) {
            if (label.Value >= range.MinX && label.Value <= range.MaxX) labels.Add(label);
        }

        labels.Sort((left, right) => left.Value.CompareTo(right.Value));
        return SelectXAxisTickValues(chart, range, plot, labels);
    }

    private static IReadOnlyList<double> SelectXAxisTickValues(Chart chart, ChartRange range, ChartRect plot, IReadOnlyList<ChartAxisLabel> labels) {
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

        return FormatXAxisValue(chart, value);
    }

    private static IReadOnlyList<string> XAxisTickLabels(Chart chart, IReadOnlyList<double> xTicks, bool valueAxisOnly) {
        var labels = new string[xTicks.Count];
        for (var i = 0; i < xTicks.Count; i++) labels[i] = valueAxisOnly ? FormatXAxisValue(chart, xTicks[i]) : FormatX(chart, xTicks[i]);
        return labels;
    }

    private static string FormatXAxisValue(Chart chart, double value) {
        var formatter = chart.Options.XAxisValueFormatter;
        if (formatter == null) return FormatNumber(value);
        return formatter(value) ?? string.Empty;
    }

    private static bool IsPieLike(Chart chart) => chart.Series.Count > 0 && (chart.Series[0].Kind == ChartSeriesKind.Pie || chart.Series[0].Kind == ChartSeriesKind.Donut);
    private static bool IsPolarAreaChart(Chart chart) {
        foreach (var series in chart.Series) if (series.Kind == ChartSeriesKind.PolarArea) return true;
        return false;
    }

    private static bool IsProgressBarChart(Chart chart) {
        foreach (var series in chart.Series) if (series.Kind == ChartSeriesKind.ProgressBar) return true;
        return false;
    }

    private static bool IsHexbinHeatmapChart(Chart chart) {
        foreach (var series in chart.Series) if (series.Kind == ChartSeriesKind.HexbinHeatmap) return true;
        return false;
    }

    private static bool IsHorizontalBarChart(Chart chart) {
        foreach (var series in chart.Series) if (series.Kind == ChartSeriesKind.HorizontalBar) return true;
        return false;
    }
}
