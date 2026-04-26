using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

/// <summary>
/// Renders charts to static SVG markup.
/// </summary>
public sealed partial class SvgChartRenderer {
    private const double LegendStartX = 40;
    private const double LegendRowHeight = 20;

    /// <summary>
    /// Renders the specified chart to SVG.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <returns>SVG markup.</returns>
    public string Render(Chart chart) {
        var o = chart.Options;
        var t = o.Theme;
        var w = o.Size.Width;
        var h = o.Size.Height;
        var plot = PlotArea(chart);
        var range = ChartRange.FromChart(chart);
        IReadOnlyList<double> xTicks;
        IReadOnlyList<double> yTicks;
        if (IsHorizontalBarChart(chart)) {
            xTicks = ChartTicks.Generate(range.MinX, range.MaxX, o.TickCount);
            ApplyHorizontalValueBounds(chart, range, xTicks);
            yTicks = GetHorizontalCategoryTicks(chart, range);
            plot = ApplyHorizontalBarReserve(chart, plot, yTicks);
        } else {
            yTicks = ChartTicks.Generate(range.MinY, range.MaxY, o.TickCount);
            range.SetYBounds(yTicks[0], yTicks[yTicks.Count - 1]);
            plot = ApplyYAxisLabelReserve(chart, plot, yTicks);
            xTicks = GetXTicks(chart, range, plot);
        }

        var map = new ChartMapper(plot, range);
        var id = BuildId(chart);
        var sb = new StringBuilder();
        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{w}\" height=\"{h}\" viewBox=\"0 0 {w} {h}\" role=\"img\" aria-labelledby=\"{id}-title {id}-desc\" preserveAspectRatio=\"xMidYMid meet\" style=\"max-width:100%;height:auto;display:block\" shape-rendering=\"geometricPrecision\" text-rendering=\"geometricPrecision\">");
        sb.AppendLine($"<title id=\"{id}-title\">{Escape(string.IsNullOrWhiteSpace(chart.Title) ? "ChartForgeX chart" : chart.Title)}</title>");
        sb.AppendLine($"<desc id=\"{id}-desc\">{Escape(BuildDescription(chart))}</desc>");
        sb.AppendLine("<defs>");
        sb.AppendLine($"<style>#{id} text{{font-synthesis:none}}</style>");
        sb.AppendLine($"<filter id=\"{id}-softShadow\" x=\"-20%\" y=\"-20%\" width=\"140%\" height=\"140%\"><feDropShadow dx=\"0\" dy=\"14\" stdDeviation=\"18\" flood-opacity=\"{F(Clamp(t.ShadowOpacity, 0, 1))}\"/></filter>");
        sb.AppendLine($"<clipPath id=\"{id}-plotClip\"><rect x=\"{F(plot.X)}\" y=\"{F(plot.Y)}\" width=\"{F(plot.Width)}\" height=\"{F(plot.Height)}\" rx=\"{F(t.PlotCornerRadius)}\"/></clipPath>");
        for (var i = 0; i < chart.Series.Count; i++) {
            var c = Color(chart, i);
            sb.AppendLine($"<linearGradient id=\"{id}-area{i}\" x1=\"0\" x2=\"0\" y1=\"0\" y2=\"1\"><stop offset=\"0%\" stop-color=\"{c.ToHex()}\" stop-opacity=\"0.32\"/><stop offset=\"100%\" stop-color=\"{c.ToHex()}\" stop-opacity=\"0.02\"/></linearGradient>");
            sb.AppendLine($"<linearGradient id=\"{id}-seriesFill{i}\" x1=\"0\" x2=\"0\" y1=\"0\" y2=\"1\"><stop offset=\"0%\" stop-color=\"{c.ToHex()}\" stop-opacity=\"1\"/><stop offset=\"100%\" stop-color=\"{c.ToHex()}\" stop-opacity=\"0.74\"/></linearGradient>");
        }
        for (var i = 0; i < t.Palette.Length; i++) {
            var c = t.Palette[i];
            sb.AppendLine($"<linearGradient id=\"{id}-sliceFill{i}\" x1=\"0\" x2=\"1\" y1=\"0\" y2=\"1\"><stop offset=\"0%\" stop-color=\"{c.ToHex()}\" stop-opacity=\"1\"/><stop offset=\"100%\" stop-color=\"{c.ToHex()}\" stop-opacity=\"0.78\"/></linearGradient>");
        }
        sb.AppendLine("</defs>");
        sb.AppendLine($"<g id=\"{id}\">");
        if (!o.TransparentBackground && t.Background.A > 0) sb.AppendLine($"<rect width=\"100%\" height=\"100%\" fill=\"{t.Background.ToCss()}\"/>");
        if (o.ShowCard && t.UseCard) {
            sb.AppendLine($"<rect x=\"14\" y=\"14\" width=\"{w-28}\" height=\"{h-28}\" rx=\"{F(t.CornerRadius)}\" fill=\"{t.CardBackground.ToCss()}\" filter=\"url(#{id}-softShadow)\"/>");
            sb.AppendLine($"<rect x=\"14.5\" y=\"14.5\" width=\"{w-29}\" height=\"{h-29}\" rx=\"{F(Math.Max(0, t.CornerRadius - 0.5))}\" fill=\"none\" stroke=\"{t.CardBorder.ToCss()}\"/>");
        }
        if (o.ShowPlotBackground) {
            sb.AppendLine($"<rect x=\"{F(plot.X)}\" y=\"{F(plot.Y)}\" width=\"{F(plot.Width)}\" height=\"{F(plot.Height)}\" rx=\"{F(t.PlotCornerRadius)}\" fill=\"{t.PlotBackground.ToCss()}\"/>");
            sb.AppendLine($"<rect x=\"{F(plot.X + 0.5)}\" y=\"{F(plot.Y + 0.5)}\" width=\"{F(Math.Max(0, plot.Width - 1))}\" height=\"{F(Math.Max(0, plot.Height - 1))}\" rx=\"{F(Math.Max(0, t.PlotCornerRadius - 0.5))}\" fill=\"none\" stroke=\"{t.PlotBorder.ToCss()}\"/>");
        }
        if (o.ShowHeader) DrawHeader(sb, chart);
        if (IsPieLike(chart)) {
            DrawPieLike(sb, chart, plot, id);
            sb.AppendLine("</g>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }
        if (IsGaugeChart(chart)) {
            DrawGauge(sb, chart, plot);
            sb.AppendLine("</g>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }
        if (IsBulletChart(chart)) {
            DrawBullet(sb, chart, plot, id);
            sb.AppendLine("</g>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }
        if (IsWaterfallChart(chart)) {
            DrawWaterfall(sb, chart, plot, id);
            sb.AppendLine("</g>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }
        if (IsRadarChart(chart)) {
            DrawRadar(sb, chart, plot, id);
            sb.AppendLine("</g>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }
        if (IsFunnelChart(chart)) {
            DrawFunnel(sb, chart, plot, id);
            sb.AppendLine("</g>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }
        if (IsHeatmapChart(chart)) {
            DrawHeatmap(sb, chart, plot);
            sb.AppendLine("</g>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }
        if (IsTimelineChart(chart)) {
            DrawTimeline(sb, chart, plot);
            sb.AppendLine("</g>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }
        if (IsHorizontalBarChart(chart)) {
            DrawHorizontalBarGrid(sb, chart, plot, xTicks, yTicks, map);
            sb.AppendLine($"<g clip-path=\"url(#{id}-plotClip)\">");
            for (var i = 0; i < chart.Series.Count; i++) DrawSeries(sb, chart, i, plot, range, map, id);
            sb.AppendLine("</g>");
            DrawLegend(sb, chart, w, h);
            sb.AppendLine("</g>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }

        DrawAnnotationBands(sb, chart, plot, map);
        DrawGrid(sb, chart, plot, xTicks, yTicks, map);
        DrawAnnotationLines(sb, chart, plot, map);
        sb.AppendLine($"<g clip-path=\"url(#{id}-plotClip)\">");
        for (var i = 0; i < chart.Series.Count; i++) DrawSeries(sb, chart, i, plot, range, map, id);
        if (o.BarMode == ChartBarMode.Stacked && o.ShowStackTotals) DrawStackTotals(sb, chart, plot, map);
        sb.AppendLine("</g>");
        DrawLegend(sb, chart, w, h);
        sb.AppendLine("</g>");
        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    private static void DrawHeader(StringBuilder sb, Chart chart) {
        var t = chart.Options.Theme;
        sb.AppendLine($"<text x=\"40\" y=\"52\" fill=\"{t.Text.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TitleFontSize)}\" font-weight=\"750\">{Escape(chart.Title)}</text>");
        if (!string.IsNullOrWhiteSpace(chart.Subtitle)) sb.AppendLine($"<text x=\"40\" y=\"79\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.SubtitleFontSize)}\">{Escape(chart.Subtitle)}</text>");
    }

    private static void DrawGrid(StringBuilder sb, Chart chart, ChartRect plot, IReadOnlyList<double> xTicks, IReadOnlyList<double> yTicks, ChartMapper map) {
        var o = chart.Options; var t = o.Theme;
        var xLabelAngle = Clamp(o.XAxisLabelAngle, -80, 80);
        var xLabelY = plot.Bottom + XAxisLabelOffset(chart);
        foreach (var yv in yTicks) {
            var y = map.Y(yv);
            if (o.ShowGrid) sb.AppendLine($"<line x1=\"{F(plot.Left)}\" y1=\"{F(y)}\" x2=\"{F(plot.Right)}\" y2=\"{F(y)}\" stroke=\"{t.Grid.ToCss()}\" stroke-width=\"1\"/>");
            if (o.ShowAxes) sb.AppendLine($"<text x=\"{F(plot.Left-12)}\" y=\"{F(y+4)}\" text-anchor=\"end\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\">{Escape(FormatValue(chart, yv))}</text>");
        }
        foreach (var xv in xTicks) {
            var x = map.X(xv);
            if (o.ShowGrid) sb.AppendLine($"<line x1=\"{F(x)}\" y1=\"{F(plot.Top)}\" x2=\"{F(x)}\" y2=\"{F(plot.Bottom)}\" stroke=\"{t.Grid.ToCss()}\" stroke-width=\"1\" opacity=\"0.42\"/>");
            if (o.ShowAxes) DrawXAxisLabel(sb, chart, plot, FormatX(chart, xv), x, xLabelY, xLabelAngle);
        }
        var zeroY = map.Y(0);
        if (o.ShowAxes && zeroY > plot.Top && zeroY < plot.Bottom) {
            sb.AppendLine($"<line x1=\"{F(plot.Left)}\" y1=\"{F(zeroY)}\" x2=\"{F(plot.Right)}\" y2=\"{F(zeroY)}\" stroke=\"{t.Axis.ToCss()}\" stroke-width=\"1.4\"/>");
        }
        if (!o.ShowAxes) return;
        sb.AppendLine($"<line x1=\"{F(plot.Left)}\" y1=\"{F(plot.Bottom)}\" x2=\"{F(plot.Right)}\" y2=\"{F(plot.Bottom)}\" stroke=\"{t.Axis.ToCss()}\" stroke-width=\"1.2\"/>");
        sb.AppendLine($"<line x1=\"{F(plot.Left)}\" y1=\"{F(plot.Top)}\" x2=\"{F(plot.Left)}\" y2=\"{F(plot.Bottom)}\" stroke=\"{t.Axis.ToCss()}\" stroke-width=\"1.2\"/>");
        if (!string.IsNullOrWhiteSpace(chart.XAxisTitle)) sb.AppendLine($"<text x=\"{F(plot.Left + plot.Width/2)}\" y=\"{F(plot.Bottom+XAxisTitleOffset(chart))}\" text-anchor=\"middle\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.AxisTitleFontSize)}\" font-weight=\"600\">{Escape(chart.XAxisTitle)}</text>");
        if (!string.IsNullOrWhiteSpace(chart.YAxisTitle)) sb.AppendLine($"<text transform=\"translate(26 {F(plot.Top + plot.Height/2)}) rotate(-90)\" text-anchor=\"middle\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.AxisTitleFontSize)}\" font-weight=\"600\">{Escape(chart.YAxisTitle)}</text>");
    }

    private static void DrawHorizontalBarGrid(StringBuilder sb, Chart chart, ChartRect plot, IReadOnlyList<double> xTicks, IReadOnlyList<double> categoryTicks, ChartMapper map) {
        var o = chart.Options;
        var t = o.Theme;
        foreach (var xv in xTicks) {
            var x = map.X(xv);
            if (o.ShowGrid) sb.AppendLine($"<line x1=\"{F(x)}\" y1=\"{F(plot.Top)}\" x2=\"{F(x)}\" y2=\"{F(plot.Bottom)}\" stroke=\"{t.Grid.ToCss()}\" stroke-width=\"1\" opacity=\"0.72\"/>");
            if (o.ShowAxes) sb.AppendLine($"<text x=\"{F(x)}\" y=\"{F(plot.Bottom + 21)}\" text-anchor=\"middle\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\">{Escape(FormatValue(chart, xv))}</text>");
        }

        foreach (var category in categoryTicks) {
            var y = map.Y(category);
            if (o.ShowGrid) sb.AppendLine($"<line x1=\"{F(plot.Left)}\" y1=\"{F(y)}\" x2=\"{F(plot.Right)}\" y2=\"{F(y)}\" stroke=\"{t.Grid.ToCss()}\" stroke-width=\"1\" opacity=\"0.28\"/>");
            if (o.ShowAxes) sb.AppendLine($"<text x=\"{F(plot.Left - 12)}\" y=\"{F(y + 4)}\" text-anchor=\"end\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\" font-weight=\"600\">{Escape(FormatX(chart, category))}</text>");
        }

        var zeroX = map.X(0);
        if (o.ShowAxes && zeroX > plot.Left && zeroX < plot.Right) {
            sb.AppendLine($"<line x1=\"{F(zeroX)}\" y1=\"{F(plot.Top)}\" x2=\"{F(zeroX)}\" y2=\"{F(plot.Bottom)}\" stroke=\"{t.Axis.ToCss()}\" stroke-width=\"1.4\"/>");
        }

        if (!o.ShowAxes) return;
        sb.AppendLine($"<line x1=\"{F(plot.Left)}\" y1=\"{F(plot.Bottom)}\" x2=\"{F(plot.Right)}\" y2=\"{F(plot.Bottom)}\" stroke=\"{t.Axis.ToCss()}\" stroke-width=\"1.2\"/>");
        sb.AppendLine($"<line x1=\"{F(plot.Left)}\" y1=\"{F(plot.Top)}\" x2=\"{F(plot.Left)}\" y2=\"{F(plot.Bottom)}\" stroke=\"{t.Axis.ToCss()}\" stroke-width=\"1.2\"/>");
        if (!string.IsNullOrWhiteSpace(chart.XAxisTitle)) sb.AppendLine($"<text x=\"{F(plot.Left + plot.Width / 2)}\" y=\"{F(plot.Bottom + XAxisTitleOffset(chart))}\" text-anchor=\"middle\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.AxisTitleFontSize)}\" font-weight=\"600\">{Escape(chart.XAxisTitle)}</text>");
        if (!string.IsNullOrWhiteSpace(chart.YAxisTitle)) sb.AppendLine($"<text transform=\"translate(26 {F(plot.Top + plot.Height / 2)}) rotate(-90)\" text-anchor=\"middle\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.AxisTitleFontSize)}\" font-weight=\"600\">{Escape(chart.YAxisTitle)}</text>");
    }

    private static void DrawXAxisLabel(StringBuilder sb, Chart chart, ChartRect plot, string label, double x, double y, double angle) {
        var t = chart.Options.Theme;
        if (Math.Abs(angle) < 0.001) {
            var anchor = EdgeAwareAnchor(label, x, plot, t.TickLabelFontSize);
            sb.AppendLine($"<text x=\"{F(x)}\" y=\"{F(y)}\" text-anchor=\"{anchor}\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\">{Escape(label)}</text>");
            return;
        }

        var rotatedAnchor = RotatedAnchor(label, x, plot, angle, t.TickLabelFontSize);
        sb.AppendLine($"<text x=\"{F(x)}\" y=\"{F(y)}\" text-anchor=\"{rotatedAnchor}\" dominant-baseline=\"middle\" transform=\"rotate({F(angle)} {F(x)} {F(y)})\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\">{Escape(label)}</text>");
    }

    private static void DrawAnnotationBands(StringBuilder sb, Chart chart, ChartRect plot, ChartMapper map) {
        foreach (var annotation in chart.Annotations) {
            if (!annotation.EndValue.HasValue) continue;
            if (annotation.Kind == ChartAnnotationKind.HorizontalBand) {
                var y1 = Clamp(map.Y(annotation.Value), plot.Top, plot.Bottom);
                var y2 = Clamp(map.Y(annotation.EndValue.Value), plot.Top, plot.Bottom);
                var top = Math.Min(y1, y2);
                var height = Math.Abs(y2 - y1);
                sb.AppendLine($"<rect x=\"{F(plot.Left)}\" y=\"{F(top)}\" width=\"{F(plot.Width)}\" height=\"{F(height)}\" fill=\"{annotation.Color.ToHex()}\" opacity=\"{F(annotation.Opacity)}\"/>");
                DrawBandLabel(sb, chart, annotation, plot, plot.Left + 10, top + 16);
            } else if (annotation.Kind == ChartAnnotationKind.VerticalBand) {
                var x1 = Clamp(map.X(annotation.Value), plot.Left, plot.Right);
                var x2 = Clamp(map.X(annotation.EndValue.Value), plot.Left, plot.Right);
                var left = Math.Min(x1, x2);
                var width = Math.Abs(x2 - x1);
                sb.AppendLine($"<rect x=\"{F(left)}\" y=\"{F(plot.Top)}\" width=\"{F(width)}\" height=\"{F(plot.Height)}\" fill=\"{annotation.Color.ToHex()}\" opacity=\"{F(annotation.Opacity)}\"/>");
                DrawBandLabel(sb, chart, annotation, plot, left + 8, plot.Top + 16);
            }
        }
    }

    private static void DrawAnnotationLines(StringBuilder sb, Chart chart, ChartRect plot, ChartMapper map) {
        foreach (var annotation in chart.Annotations) {
            if (annotation.Kind == ChartAnnotationKind.HorizontalLine) {
                var y = Clamp(map.Y(annotation.Value), plot.Top, plot.Bottom);
                sb.AppendLine($"<line x1=\"{F(plot.Left)}\" y1=\"{F(y)}\" x2=\"{F(plot.Right)}\" y2=\"{F(y)}\" stroke=\"{annotation.Color.ToCss()}\" stroke-width=\"1.6\" stroke-dasharray=\"6 5\"/>");
                DrawLineLabel(sb, chart, annotation, plot, plot.Right - 8, y - 7, "end");
            } else if (annotation.Kind == ChartAnnotationKind.VerticalLine) {
                var x = Clamp(map.X(annotation.Value), plot.Left, plot.Right);
                sb.AppendLine($"<line x1=\"{F(x)}\" y1=\"{F(plot.Top)}\" x2=\"{F(x)}\" y2=\"{F(plot.Bottom)}\" stroke=\"{annotation.Color.ToCss()}\" stroke-width=\"1.6\" stroke-dasharray=\"6 5\"/>");
                DrawLineLabel(sb, chart, annotation, plot, x + 8, plot.Top + 16, "start");
            }
        }
    }

    private static void DrawBandLabel(StringBuilder sb, Chart chart, ChartAnnotation annotation, ChartRect plot, double x, double y) {
        if (string.IsNullOrWhiteSpace(annotation.Label)) return;
        DrawLabelPill(sb, chart, annotation.Label, x, y, chart.Options.Theme.MutedText, "start", plot);
    }

    private static void DrawLineLabel(StringBuilder sb, Chart chart, ChartAnnotation annotation, ChartRect plot, double x, double y, string anchor) {
        if (string.IsNullOrWhiteSpace(annotation.Label)) return;
        DrawLabelPill(sb, chart, annotation.Label, x, y, annotation.Color, anchor, plot);
    }

    private static void DrawPieLike(StringBuilder sb, Chart chart, ChartRect plot, string id) {
        var series = chart.Series[0];
        var values = series.Points.Where(p => p.Y > 0).ToArray();
        if (values.Length == 0) return;

        var t = chart.Options.Theme;
        var total = values.Sum(p => p.Y);
        var radius = Math.Max(1, Math.Min(plot.Width, plot.Height) * 0.38);
        var cx = plot.Left + plot.Width * 0.42;
        var cy = plot.Top + plot.Height / 2;
        var inner = series.Kind == ChartSeriesKind.Donut ? radius * 0.58 : 0;
        var start = -Math.PI / 2;

        for (var i = 0; i < values.Length; i++) {
            var sweep = values[i].Y / total * Math.PI * 2;
            var end = start + sweep;
            sb.AppendLine($"<path d=\"{BuildSlicePath(cx, cy, radius, inner, start, end)}\" fill=\"url(#{id}-sliceFill{i % t.Palette.Length})\" fill-rule=\"evenodd\" stroke=\"{t.CardBackground.ToCss()}\" stroke-width=\"2\"/>");

            if (chart.Options.ShowDataLabels && sweep > 0.22) {
                var mid = start + sweep / 2;
                var labelRadius = inner > 0 ? (inner + radius) / 2 : radius * 0.66;
                var x = cx + Math.Cos(mid) * labelRadius;
                var y = cy + Math.Sin(mid) * labelRadius + 4;
                sb.AppendLine($"<text x=\"{F(x)}\" y=\"{F(y)}\" text-anchor=\"middle\" fill=\"{t.CardBackground.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.DataLabelFontSize)}\" font-weight=\"750\">{FormatPercent(values[i].Y / total)}</text>");
            }

            start = end;
        }

        if (series.Kind == ChartSeriesKind.Donut) {
            sb.AppendLine($"<text x=\"{F(cx)}\" y=\"{F(cy - 2)}\" text-anchor=\"middle\" fill=\"{t.Text.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"24\" font-weight=\"800\">{Escape(FormatValue(chart, total))}</text>");
            sb.AppendLine($"<text x=\"{F(cx)}\" y=\"{F(cy + 19)}\" text-anchor=\"middle\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\">{Escape(series.Name)}</text>");
        }

        if (chart.Options.ShowLegend) DrawSliceLegend(sb, chart, values, plot, total);
    }

    private static void DrawSliceLegend(StringBuilder sb, Chart chart, IReadOnlyList<ChartPoint> values, ChartRect plot, double total) {
        var t = chart.Options.Theme;
        var x = plot.Left + plot.Width * 0.72;
        var y = plot.Top + Math.Max(24, plot.Height * 0.18);
        for (var i = 0; i < values.Count; i++) {
            var color = chart.Options.Theme.Palette[i % chart.Options.Theme.Palette.Length];
            var label = SliceLabel(chart, values[i], i);
            sb.AppendLine($"<rect x=\"{F(x)}\" y=\"{F(y - 9)}\" width=\"10\" height=\"10\" rx=\"2\" fill=\"{color.ToCss()}\"/>");
            sb.AppendLine($"<text x=\"{F(x + 16)}\" y=\"{F(y)}\" fill=\"{t.Text.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.LegendFontSize)}\" font-weight=\"650\">{Escape(label)}</text>");
            sb.AppendLine($"<text x=\"{F(plot.Right - 12)}\" y=\"{F(y)}\" text-anchor=\"end\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.LegendFontSize)}\">{FormatPercent(values[i].Y / total)}</text>");
            y += 22;
        }
    }

    private static string BuildSlicePath(double cx, double cy, double radius, double innerRadius, double start, double end) {
        if (end - start >= Math.PI * 2 - 0.000001) {
            return BuildFullSlicePath(cx, cy, radius, innerRadius);
        }

        var largeArc = end - start > Math.PI ? 1 : 0;
        var x1 = cx + Math.Cos(start) * radius;
        var y1 = cy + Math.Sin(start) * radius;
        var x2 = cx + Math.Cos(end) * radius;
        var y2 = cy + Math.Sin(end) * radius;

        if (innerRadius <= 0) {
            return $"M {F(cx)} {F(cy)} L {F(x1)} {F(y1)} A {F(radius)} {F(radius)} 0 {largeArc} 1 {F(x2)} {F(y2)} Z";
        }

        var ix1 = cx + Math.Cos(start) * innerRadius;
        var iy1 = cy + Math.Sin(start) * innerRadius;
        var ix2 = cx + Math.Cos(end) * innerRadius;
        var iy2 = cy + Math.Sin(end) * innerRadius;
        return $"M {F(x1)} {F(y1)} A {F(radius)} {F(radius)} 0 {largeArc} 1 {F(x2)} {F(y2)} L {F(ix2)} {F(iy2)} A {F(innerRadius)} {F(innerRadius)} 0 {largeArc} 0 {F(ix1)} {F(iy1)} Z";
    }

    private static string BuildFullSlicePath(double cx, double cy, double radius, double innerRadius) {
        var left = cx - radius;
        var right = cx + radius;
        if (innerRadius <= 0) {
            return $"M {F(cx)} {F(cy)} m {F(-radius)} 0 A {F(radius)} {F(radius)} 0 1 1 {F(right)} {F(cy)} A {F(radius)} {F(radius)} 0 1 1 {F(left)} {F(cy)} Z";
        }

        var innerLeft = cx - innerRadius;
        var innerRight = cx + innerRadius;
        return $"M {F(left)} {F(cy)} A {F(radius)} {F(radius)} 0 1 1 {F(right)} {F(cy)} A {F(radius)} {F(radius)} 0 1 1 {F(left)} {F(cy)} M {F(innerLeft)} {F(cy)} A {F(innerRadius)} {F(innerRadius)} 0 1 0 {F(innerRight)} {F(cy)} A {F(innerRadius)} {F(innerRadius)} 0 1 0 {F(innerLeft)} {F(cy)} Z";
    }

    private static void DrawSeries(StringBuilder sb, Chart chart, int index, ChartRect plot, ChartRange range, ChartMapper map, string id) {
        var s = chart.Series[index]; var c = Color(chart, index); if (s.Points.Count == 0) return;
        if (s.Kind == ChartSeriesKind.HorizontalBar) { DrawHorizontalBars(sb, chart, index, plot, map, id); return; }
        if (s.Kind == ChartSeriesKind.Bar) { DrawBars(sb, chart, index, plot, range, map, id); return; }
        var mapped = s.Points.Select(p => new ChartPoint(map.X(p.X), map.Y(p.Y))).ToArray();
        if (s.Kind == ChartSeriesKind.Area) {
            var first = mapped[0]; var last = mapped[mapped.Length - 1];
            var zeroY = Math.Min(plot.Bottom, Math.Max(plot.Top, map.Y(0)));
            var area = $"{BuildLinePath(mapped, s.Smooth)} L {F(last.X)} {F(zeroY)} L {F(first.X)} {F(zeroY)} Z";
            sb.AppendLine($"<path d=\"{area}\" fill=\"url(#{id}-area{index})\"/>");
        }
        if (s.Kind == ChartSeriesKind.Scatter) {
            foreach (var p in mapped) sb.AppendLine($"<circle cx=\"{F(p.X)}\" cy=\"{F(p.Y)}\" r=\"{F(Math.Max(3, chart.Options.Theme.MarkerRadius + 0.75))}\" fill=\"{c.ToCss()}\" opacity=\"0.92\" stroke=\"{chart.Options.Theme.CardBackground.ToCss()}\" stroke-width=\"2\"/>");
        } else {
            var line = BuildLinePath(mapped, s.Smooth);
            sb.AppendLine($"<path d=\"{line}\" fill=\"none\" stroke=\"{c.ToCss()}\" stroke-width=\"{F(s.StrokeWidth + 5)}\" stroke-linecap=\"round\" stroke-linejoin=\"round\" opacity=\"0.14\"/>");
            sb.AppendLine($"<path d=\"{line}\" fill=\"none\" stroke=\"{c.ToCss()}\" stroke-width=\"{F(s.StrokeWidth)}\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>");
            if (!chart.Options.IsSparkline) foreach (var p in mapped) sb.AppendLine($"<circle cx=\"{F(p.X)}\" cy=\"{F(p.Y)}\" r=\"{F(chart.Options.Theme.MarkerRadius)}\" fill=\"{c.ToCss()}\" stroke=\"{chart.Options.Theme.CardBackground.ToCss()}\" stroke-width=\"2\"/>");
        }
        if (chart.Options.ShowDataLabels) DrawPointLabels(sb, chart, s, mapped, plot);
    }

    private static void DrawHorizontalBars(StringBuilder sb, Chart chart, int index, ChartRect plot, ChartMapper map, string id) {
        var s = chart.Series[index];
        var layout = HorizontalBarLayout(chart, plot, index);
        var zeroX = Math.Min(plot.Right, Math.Max(plot.Left, map.X(0)));
        for (var pointIndex = 0; pointIndex < s.Points.Count; pointIndex++) {
            var p = s.Points[pointIndex];
            var valueX = map.X(p.Y);
            var left = Math.Min(zeroX, valueX);
            var width = Math.Abs(valueX - zeroX);
            var y = map.Y(p.X) + layout.Offset - layout.BarHeight / 2;
            sb.AppendLine($"<rect data-cfx-role=\"horizontal-bar\" data-cfx-series=\"{index}\" data-cfx-point=\"{pointIndex}\" x=\"{F(left)}\" y=\"{F(y)}\" width=\"{F(width)}\" height=\"{F(layout.BarHeight)}\" rx=\"{F(Math.Min(7, layout.BarHeight / 2))}\" fill=\"url(#{id}-seriesFill{index})\" opacity=\"0.94\"/>");
            if (chart.Options.ShowDataLabels) {
                var labelX = p.Y >= 0 ? left + width + 8 : left - 8;
                var anchor = p.Y >= 0 ? "start" : "end";
                DrawHorizontalValueLabel(sb, chart, FormatValue(chart, p.Y), labelX, y + layout.BarHeight / 2, anchor, plot);
            }
        }
    }

    private static void DrawBars(StringBuilder sb, Chart chart, int index, ChartRect plot, ChartRange range, ChartMapper map, string id) {
        var s = chart.Series[index];
        var layout = BarLayout(chart, plot, index);
        var zeroY = Math.Min(plot.Bottom, Math.Max(plot.Top, map.Y(0)));
        for (var pointIndex = 0; pointIndex < s.Points.Count; pointIndex++) {
            var p = s.Points[pointIndex];
            var baseValue = chart.Options.BarMode == ChartBarMode.Stacked ? StackBaseValue(chart, index, p) : 0;
            var y = map.Y(baseValue + p.Y);
            var baseY = chart.Options.BarMode == ChartBarMode.Stacked ? map.Y(baseValue) : zeroY;
            var top = Math.Min(y, baseY);
            var height = Math.Abs(baseY - y);
            var x = map.X(p.X) + layout.Offset - layout.BarWidth / 2;
            var radius = chart.Options.BarMode == ChartBarMode.Stacked ? Math.Min(3, layout.BarWidth / 2) : Math.Min(7, layout.BarWidth / 2);
            sb.AppendLine($"<rect data-cfx-role=\"bar\" data-cfx-series=\"{index}\" data-cfx-point=\"{pointIndex}\" x=\"{F(x)}\" y=\"{F(top)}\" width=\"{F(layout.BarWidth)}\" height=\"{F(height)}\" rx=\"{F(radius)}\" fill=\"url(#{id}-seriesFill{index})\" opacity=\"0.94\"/>");
            if (chart.Options.ShowDataLabels) {
                if (chart.Options.BarMode == ChartBarMode.Stacked && height < chart.Options.Theme.DataLabelFontSize + 8) continue;
                var labelY = chart.Options.BarMode == ChartBarMode.Stacked ? top + height / 2 : p.Y >= 0 ? top - 10 : top + height + 10;
                DrawDataLabel(sb, chart, FormatValue(chart, p.Y), x + layout.BarWidth / 2, labelY, plot);
            }
        }
    }

    private static BarLayoutInfo BarLayout(Chart chart, ChartRect plot, int seriesIndex) {
        var barSeries = chart.Series
            .Select((series, index) => new { series, index })
            .Where(item => item.series.Kind == ChartSeriesKind.Bar)
            .Select(item => item.index)
            .ToArray();
        var groupCount = chart.Options.BarMode == ChartBarMode.Stacked ? 1 : Math.Max(1, barSeries.Length);
        var groupPosition = chart.Options.BarMode == ChartBarMode.Stacked ? 0 : Math.Max(0, Array.IndexOf(barSeries, seriesIndex));
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
        var horizontalSeries = chart.Series
            .Select((series, index) => new { series, index })
            .Where(item => item.series.Kind == ChartSeriesKind.HorizontalBar)
            .Select(item => item.index)
            .ToArray();
        var groupCount = Math.Max(1, horizontalSeries.Length);
        var groupPosition = Math.Max(0, Array.IndexOf(horizontalSeries, seriesIndex));
        var categoryValues = new HashSet<double>();
        foreach (var index in horizontalSeries) {
            foreach (var point in chart.Series[index].Points) categoryValues.Add(point.X);
        }

        var categoryCount = Math.Max(1, categoryValues.Count);
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

    private static void DrawStackTotals(StringBuilder sb, Chart chart, ChartRect plot, ChartMapper map) {
        var positiveTotals = new Dictionary<double, double>();
        var negativeTotals = new Dictionary<double, double>();
        foreach (var series in chart.Series) {
            if (series.Kind != ChartSeriesKind.Bar) continue;
            foreach (var point in series.Points) AddStackTotal(point.Y >= 0 ? positiveTotals : negativeTotals, point.X, point.Y);
        }

        DrawStackTotalSet(sb, chart, positiveTotals, plot, map, -14);
        DrawStackTotalSet(sb, chart, negativeTotals, plot, map, 14);
    }

    private static void DrawStackTotalSet(StringBuilder sb, Chart chart, Dictionary<double, double> totals, ChartRect plot, ChartMapper map, double offset) {
        foreach (var item in totals.OrderBy(item => item.Key)) {
            if (Math.Abs(item.Value) < 0.000001) continue;
            DrawDataLabel(sb, chart, FormatValue(chart, item.Value), map.X(item.Key), map.Y(item.Value) + offset, plot, "stack-total-label");
        }
    }

    private static void AddStackTotal(Dictionary<double, double> totals, double x, double y) {
        double current;
        totals.TryGetValue(x, out current);
        totals[x] = current + y;
    }

    private static void DrawPointLabels(StringBuilder sb, Chart chart, ChartSeries series, IReadOnlyList<ChartPoint> mapped, ChartRect plot) {
        var offset = chart.Options.Theme.MarkerRadius + 12;
        for (var i = 0; i < mapped.Count; i++) {
            var point = mapped[i];
            var labelY = point.Y - offset;
            if (labelY < plot.Top + chart.Options.Theme.DataLabelFontSize) labelY = point.Y + offset;
            DrawDataLabel(sb, chart, FormatValue(chart, series.Points[i].Y), point.X, labelY, plot);
        }
    }

    private static ChartRect PlotArea(Chart chart) {
        var plot = ChartLayout.PlotArea(chart.Options);
        if (chart.Options.IsSparkline || IsPieLike(chart)) return plot;

        var bottomReserve = 0.0;
        if (chart.Options.ShowAxes) {
            bottomReserve += XAxisTitleOffset(chart) + chart.Options.Theme.AxisTitleFontSize + 4;
            if (string.IsNullOrWhiteSpace(chart.XAxisTitle)) bottomReserve -= 18;
        }

        if (chart.Options.ShowLegend) {
            bottomReserve += 18 + BuildLegendRows(chart, chart.Options.Size.Width).Count * LegendRowHeight;
        }

        var extraBottom = Math.Max(0, bottomReserve - chart.Options.Padding.Bottom);
        if (extraBottom <= 0) return plot;
        return new ChartRect(plot.X, plot.Y, plot.Width, Math.Max(1, plot.Height - extraBottom));
    }

    private static ChartRect ApplyYAxisLabelReserve(Chart chart, ChartRect plot, IReadOnlyList<double> yTicks) {
        if (!chart.Options.ShowAxes || chart.Options.IsSparkline || IsPieLike(chart) || yTicks.Count == 0) return plot;
        var t = chart.Options.Theme;
        var widest = yTicks.Max(tick => EstimateTextWidth(FormatValue(chart, tick), t.TickLabelFontSize));
        var desiredLeft = Math.Max(plot.Left, widest + 54);
        var maxLeft = Math.Max(plot.Left, chart.Options.Size.Width - chart.Options.Padding.Right - 160);
        var adjustedLeft = Math.Min(desiredLeft, maxLeft);
        if (adjustedLeft <= plot.Left) return plot;
        var shift = adjustedLeft - plot.Left;
        return new ChartRect(plot.X + shift, plot.Y, Math.Max(1, plot.Width - shift), plot.Height);
    }

    private static ChartRect ApplyHorizontalBarReserve(Chart chart, ChartRect plot, IReadOnlyList<double> categoryTicks) {
        if (!chart.Options.ShowAxes || categoryTicks.Count == 0) return plot;
        var t = chart.Options.Theme;
        var widest = categoryTicks.Max(tick => EstimateTextWidth(FormatX(chart, tick), t.TickLabelFontSize));
        var desiredLeft = Math.Max(plot.Left, widest + 58);
        var maxLeft = Math.Max(plot.Left, chart.Options.Size.Width - chart.Options.Padding.Right - 180);
        var adjustedLeft = Math.Min(desiredLeft, maxLeft);
        var leftShift = Math.Max(0, adjustedLeft - plot.Left);
        var rightReserve = HorizontalValueLabelReserve(chart);
        if (leftShift <= 0 && rightReserve <= 0) return plot;
        return new ChartRect(plot.X + leftShift, plot.Y, Math.Max(1, plot.Width - leftShift - rightReserve), plot.Height);
    }

    private static double HorizontalValueLabelReserve(Chart chart) {
        if (!chart.Options.ShowDataLabels) return 0;
        var labels = chart.Series
            .Where(series => series.Kind == ChartSeriesKind.HorizontalBar)
            .SelectMany(series => series.Points.Select(point => FormatValue(chart, point.Y)))
            .ToArray();
        if (labels.Length == 0) return 0;
        return Math.Min(96, labels.Max(label => EstimateTextWidth(label, chart.Options.Theme.DataLabelFontSize)) + 24);
    }

    private static void ApplyHorizontalValueBounds(Chart chart, ChartRange range, IReadOnlyList<double> xTicks) {
        var min = xTicks[0];
        var max = xTicks[xTicks.Count - 1];
        if (chart.Options.ShowDataLabels) {
            var span = Math.Max(1, max - min);
            var hasPositive = chart.Series.Any(series => series.Kind == ChartSeriesKind.HorizontalBar && series.Points.Any(point => point.Y > 0));
            var hasNegative = chart.Series.Any(series => series.Kind == ChartSeriesKind.HorizontalBar && series.Points.Any(point => point.Y < 0));
            if (hasPositive) max += span * 0.08;
            if (hasNegative) min -= span * 0.08;
        }

        range.SetXBounds(min, max);
    }

    private static double XAxisLabelOffset(Chart chart) {
        var angle = Math.Abs(Clamp(chart.Options.XAxisLabelAngle, -80, 80)) * Math.PI / 180;
        if (angle < 0.001 || chart.Options.XAxisLabels.Count == 0) return 21;
        var widest = chart.Options.XAxisLabels.Max(label => EstimateTextWidth(label.Text, chart.Options.Theme.TickLabelFontSize));
        return 20 + Math.Sin(angle) * Math.Min(96, widest);
    }

    private static double XAxisTitleOffset(Chart chart) {
        return XAxisLabelOffset(chart) + (Math.Abs(chart.Options.XAxisLabelAngle) < 0.001 ? 23 : 48);
    }

}
