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
    public string Render(Chart chart) => Render(chart, string.Empty);

    /// <summary>
    /// Renders the specified chart to SVG with an additional deterministic ID scope.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <param name="idScope">A caller-provided scope used to keep SVG element IDs unique when embedding multiple SVGs in one document.</param>
    /// <returns>SVG markup.</returns>
    public string Render(Chart chart, string idScope) {
        ChartGuards.RenderCompatibility(chart);
        var o = chart.Options;
        var t = o.Theme;
        var w = o.Size.Width;
        var h = o.Size.Height;
        var plot = PlotArea(chart);
        var range = ChartRange.FromChart(chart);
        IReadOnlyList<double> xTicks;
        IReadOnlyList<double> yTicks;
        ChartRange? secondaryRange = null;
        IReadOnlyList<double>? secondaryTicks = null;
        if (IsHorizontalBarChart(chart)) {
            xTicks = ChartTicks.Generate(range.MinX, range.MaxX, o.TickCount);
            ApplyHorizontalValueBounds(chart, range, xTicks);
            yTicks = GetHorizontalCategoryTicks(chart, range);
            plot = ApplyHorizontalBarReserve(chart, plot, yTicks);
            if (ShowXAxis(chart)) plot = ApplyXAxisBottomReserve(chart, plot, xTicks, true);
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
            if (ShowXAxis(chart)) plot = ApplyXAxisBottomReserve(chart, plot, xTicks, false);
        }

        var map = new ChartMapper(plot, range);
        var secondaryMap = secondaryRange == null ? null : new ChartMapper(plot, secondaryRange);
        var id = BuildId(chart, idScope);
        var sb = new StringBuilder();
        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{w}\" height=\"{h}\" viewBox=\"0 0 {w} {h}\" role=\"img\" aria-labelledby=\"{id}-title {id}-desc\" preserveAspectRatio=\"xMidYMid meet\" style=\"max-width:100%;height:auto;display:block\" shape-rendering=\"geometricPrecision\" text-rendering=\"geometricPrecision\">");
        sb.AppendLine($"<title id=\"{id}-title\">{Escape(string.IsNullOrWhiteSpace(chart.Title) ? "ChartForgeX chart" : chart.Title)}</title>");
        sb.AppendLine($"<desc id=\"{id}-desc\">{Escape(BuildDescription(chart))}</desc>");
        sb.AppendLine("<defs>");
        sb.AppendLine($"<style>#{id} text{{font-synthesis:none}} #{id} .cfx-crisp-stroke{{vector-effect:non-scaling-stroke;shape-rendering:geometricPrecision}} #{id} .cfx-interactive-region{{transition:opacity .12s ease,stroke-width .12s ease}} #{id} .cfx-interactive-region[data-cfx-role=\"dotted-map-connector\"]{{pointer-events:stroke}} #{id} .cfx-interactive-region:hover,#{id} .cfx-interactive-region:focus{{opacity:1;outline:none;stroke-width:var(--cfx-interactive-focus-stroke-width,2.2)}}</style>");
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
            sb.AppendLine($"<rect class=\"cfx-crisp-stroke\" x=\"14.5\" y=\"14.5\" width=\"{w-29}\" height=\"{h-29}\" rx=\"{F(Math.Max(0, t.CornerRadius - 0.5))}\" fill=\"none\" stroke=\"{t.CardBorder.ToCss()}\"/>");
        }
        if (o.ShowPlotBackground) {
            sb.AppendLine($"<rect x=\"{F(plot.X)}\" y=\"{F(plot.Y)}\" width=\"{F(plot.Width)}\" height=\"{F(plot.Height)}\" rx=\"{F(t.PlotCornerRadius)}\" fill=\"{t.PlotBackground.ToCss()}\"/>");
            sb.AppendLine($"<rect class=\"cfx-crisp-stroke\" x=\"{F(plot.X + 0.5)}\" y=\"{F(plot.Y + 0.5)}\" width=\"{F(Math.Max(0, plot.Width - 1))}\" height=\"{F(Math.Max(0, plot.Height - 1))}\" rx=\"{F(Math.Max(0, t.PlotCornerRadius - 0.5))}\" fill=\"none\" stroke=\"{t.PlotBorder.ToCss()}\"/>");
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
            DrawLegend(sb, chart, w, h);
            sb.AppendLine("</g>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }
        if (IsCircleChart(chart)) {
            DrawCircleChart(sb, chart, plot);
            DrawLegend(sb, chart, w, h);
            sb.AppendLine("</g>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }
        if (IsRadialBarChart(chart)) {
            DrawRadialBar(sb, chart, plot);
            sb.AppendLine("</g>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }
        if (IsLayeredRadialChart(chart)) { DrawLayeredRadial(sb, chart, plot); DrawLegend(sb, chart, w, h); sb.AppendLine("</g>"); sb.AppendLine("</svg>"); return sb.ToString(); }
        if (IsBulletChart(chart)) {
            DrawBullet(sb, chart, plot, id);
            DrawLegend(sb, chart, w, h);
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
        if (IsPolarAreaChart(chart)) {
            DrawPolarArea(sb, chart, plot, id);
            sb.AppendLine("</g>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }
        if (IsFunnelChart(chart)) {
            DrawFunnel(sb, chart, plot, id);
            DrawLegend(sb, chart, w, h);
            sb.AppendLine("</g>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }
        if (IsTreemapChart(chart)) {
            DrawTreemap(sb, chart, plot, id);
            DrawLegend(sb, chart, w, h);
            sb.AppendLine("</g>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }
        if (IsPictorialChart(chart)) {
            DrawPictorial(sb, chart, plot, id);
            DrawLegend(sb, chart, w, h);
            sb.AppendLine("</g>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }
        if (IsProgressBarChart(chart)) {
            DrawProgressBar(sb, chart, plot);
            DrawLegend(sb, chart, w, h);
            sb.AppendLine("</g>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }
        if (IsWordCloudChart(chart)) {
            DrawWordCloud(sb, chart, plot);
            DrawLegend(sb, chart, w, h);
            sb.AppendLine("</g>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }
        if (IsHeatmapChart(chart)) {
            DrawHeatmap(sb, chart, plot);
            DrawLegend(sb, chart, w, h);
            sb.AppendLine("</g>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }
        if (IsCalendarHeatmapChart(chart)) { DrawCalendarHeatmap(sb, chart, plot); sb.AppendLine("</g>"); sb.AppendLine("</svg>"); return sb.ToString(); }
        if (IsDottedMapChart(chart)) { DrawDottedMap(sb, chart, plot, id); sb.AppendLine("</g>"); sb.AppendLine("</svg>"); return sb.ToString(); }
        if (IsUsStateGeoMapChart(chart)) { DrawUsStateGeoMap(sb, chart, plot); sb.AppendLine("</g>"); sb.AppendLine("</svg>"); return sb.ToString(); }
        if (IsUsStateTileMapChart(chart)) { DrawUsStateTileMap(sb, chart, plot); sb.AppendLine("</g>"); sb.AppendLine("</svg>"); return sb.ToString(); }
        if (IsTimelineChart(chart)) {
            DrawTimeline(sb, chart, plot, id);
            DrawLegend(sb, chart, w, h);
            sb.AppendLine("</g>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }
        if (IsGanttChart(chart)) {
            DrawGantt(sb, chart, plot, id);
            DrawLegend(sb, chart, w, h);
            sb.AppendLine("</g>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }
        if (IsSankeyChart(chart)) {
            DrawSankey(sb, chart, plot, id);
            DrawLegend(sb, chart, w, h);
            sb.AppendLine("</g>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }
        if (IsTreeChart(chart)) {
            DrawTree(sb, chart, plot, id);
            DrawLegend(sb, chart, w, h);
            sb.AppendLine("</g>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }
        if (IsSunburstChart(chart)) {
            DrawSunburst(sb, chart, plot);
            DrawLegend(sb, chart, w, h);
            sb.AppendLine("</g>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }
        if (IsHorizontalBarChart(chart)) {
            DrawHorizontalBarGrid(sb, chart, plot, xTicks, yTicks, map);
            sb.AppendLine($"<g clip-path=\"url(#{id}-plotClip)\">");
            for (var i = 0; i < chart.Series.Count; i++) DrawSeries(sb, chart, i, plot, range, map, id);
            sb.AppendLine("</g>");
            if (o.BarMode == ChartBarMode.Stacked && o.ShowStackTotals) DrawHorizontalStackTotals(sb, chart, plot, map);
            DrawLegend(sb, chart, w, h);
            sb.AppendLine("</g>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }

        DrawAnnotationBands(sb, chart, plot, map);
        DrawGrid(sb, chart, plot, xTicks, yTicks, map);
        if (secondaryMap != null && secondaryTicks != null) DrawSecondaryYAxis(sb, chart, plot, secondaryTicks, secondaryMap);
        sb.AppendLine($"<g clip-path=\"url(#{id}-plotClip)\">");
        for (var i = 0; i < chart.Series.Count; i++) DrawSeries(sb, chart, i, plot, range, SeriesMap(chart.Series[i], map, secondaryMap), id);
        if (o.BarMode == ChartBarMode.Stacked && o.ShowStackTotals) DrawStackTotals(sb, chart, plot, map);
        sb.AppendLine("</g>");
        DrawAnnotationLines(sb, chart, plot, map);
        DrawLegend(sb, chart, w, h);
        sb.AppendLine("</g>");
        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    private static void DrawGrid(StringBuilder sb, Chart chart, ChartRect plot, IReadOnlyList<double> xTicks, IReadOnlyList<double> yTicks, ChartMapper map) {
        var o = chart.Options; var t = o.Theme; var tickStyle = o.TickLabelStyle;
        var tickFontSize = StyleFontSize(tickStyle, t.TickLabelFontSize);
        var xLabelAngle = Clamp(o.XAxisLabelAngle, -80, 80);
        var xLabels = XAxisTickLabels(chart, xTicks, false);
        var xLabelY = plot.Bottom + XAxisLabelOffset(chart, xLabels);
        var xLabelMaxWidth = AxisTickLabelMaxWidth(plot, xTicks.Count, xLabelAngle);
        foreach (var yv in yTicks) {
            var y = map.Y(yv);
            if (o.ShowGrid) sb.AppendLine($"<line x1=\"{F(plot.Left)}\" y1=\"{F(y)}\" x2=\"{F(plot.Right)}\" y2=\"{F(y)}\" stroke=\"{t.Grid.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.GridStrokeWidth)}\"/>");
            if (ShowYAxis(chart)) sb.AppendLine($"<text x=\"{F(plot.Left-12)}\" y=\"{F(y+4)}\" text-anchor=\"end\" fill=\"{StyleColor(tickStyle, t.MutedText).ToCss()}\" font-family=\"{SvgFontFamily(StyleFontFamily(chart, tickStyle))}\" font-size=\"{F(tickFontSize)}\"{SvgTextStyleAttributes(tickStyle)}>{Escape(FormatValue(chart, yv))}</text>");
        }
        for (var i = 0; i < xTicks.Count; i++) {
            var xv = xTicks[i];
            var x = map.X(xv);
            if (o.ShowGrid) sb.AppendLine($"<line x1=\"{F(x)}\" y1=\"{F(plot.Top)}\" x2=\"{F(x)}\" y2=\"{F(plot.Bottom)}\" stroke=\"{t.Grid.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.GridStrokeWidth)}\" opacity=\"{F(ChartVisualPrimitives.GridVerticalOpacity)}\"/>");
            if (ShowXAxis(chart)) DrawXAxisLabel(sb, chart, plot, xLabels[i], x, xLabelY, xLabelAngle, maxWidth: xLabelMaxWidth);
        }
        var zeroY = map.Y(0);
        if (ShowXAxis(chart) && ShowAxisLines(chart) && zeroY > plot.Top && zeroY < plot.Bottom) {
            sb.AppendLine($"<line x1=\"{F(plot.Left)}\" y1=\"{F(zeroY)}\" x2=\"{F(plot.Right)}\" y2=\"{F(zeroY)}\" stroke=\"{t.Axis.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.ZeroAxisStrokeWidth)}\"/>");
        }
        if (ShowXAxis(chart)) {
            if (ShowAxisLines(chart)) sb.AppendLine($"<line x1=\"{F(plot.Left)}\" y1=\"{F(plot.Bottom)}\" x2=\"{F(plot.Right)}\" y2=\"{F(plot.Bottom)}\" stroke=\"{t.Axis.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.AxisStrokeWidth)}\"/>");
            DrawSvgXAxisTitle(sb, chart, plot, plot.Bottom + XAxisTitleOffset(chart, xLabels));
        }
        if (ShowYAxis(chart)) {
            if (ShowAxisLines(chart)) sb.AppendLine($"<line x1=\"{F(plot.Left)}\" y1=\"{F(plot.Top)}\" x2=\"{F(plot.Left)}\" y2=\"{F(plot.Bottom)}\" stroke=\"{t.Axis.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.AxisStrokeWidth)}\"/>");
            DrawSvgYAxisTitle(sb, chart, plot, 26);
        }
    }

    private static void DrawXAxisLabel(StringBuilder sb, Chart chart, ChartRect plot, string label, double x, double y, double angle, string? role = null, double maxWidth = 0) {
        var t = chart.Options.Theme;
        var style = chart.Options.TickLabelStyle;
        var preferredFontSize = StyleFontSize(style, t.TickLabelFontSize);
        var widthLimit = maxWidth > 0 ? maxWidth : PlotLabelMaxWidth(plot);
        var fontSize = TextFontSizeForSvgWidth(label, widthLimit, preferredFontSize);
        label = TrimSvgLabelToWidth(label, fontSize, widthLimit);
        if (label.Length == 0) return;
        var roleAttribute = string.IsNullOrWhiteSpace(role) ? string.Empty : $" data-cfx-role=\"{role}\"";
        if (Math.Abs(angle) < 0.001) {
            var anchor = EdgeAwareAnchor(label, x, plot, fontSize);
            var safeX = EdgeAwareTextX(label, x, plot, fontSize);
            sb.AppendLine($"<text{roleAttribute} x=\"{F(safeX)}\" y=\"{F(y)}\" text-anchor=\"{anchor}\" fill=\"{StyleColor(style, t.MutedText).ToCss()}\" font-family=\"{SvgFontFamily(StyleFontFamily(chart, style))}\" font-size=\"{F(fontSize)}\"{SvgTextStyleAttributes(style)}>{Escape(label)}</text>");
            return;
        }

        var rotatedAnchor = RotatedAnchor(label, x, plot, angle, fontSize);
        var rotatedX = Clamp(x, plot.Left + ChartVisualPrimitives.DataLabelPlotInset, plot.Right - ChartVisualPrimitives.DataLabelPlotInset);
        sb.AppendLine($"<text{roleAttribute} x=\"{F(rotatedX)}\" y=\"{F(y)}\" text-anchor=\"{rotatedAnchor}\" dominant-baseline=\"middle\" transform=\"rotate({F(angle)} {F(rotatedX)} {F(y)})\" fill=\"{StyleColor(style, t.MutedText).ToCss()}\" font-family=\"{SvgFontFamily(StyleFontFamily(chart, style))}\" font-size=\"{F(fontSize)}\"{SvgTextStyleAttributes(style)}>{Escape(label)}</text>");
    }

    private static double AxisTickLabelMaxWidth(ChartRect plot, int tickCount, double angle) {
        var slotWidth = tickCount <= 1 ? plot.Width : plot.Width / Math.Max(1, tickCount - 1);
        var angleFactor = Math.Abs(angle) < 0.001 ? 0.92 : 1.35;
        return Math.Max(64, Math.Min(plot.Width, slotWidth * angleFactor));
    }

    private static void DrawAnnotationBands(StringBuilder sb, Chart chart, ChartRect plot, ChartMapper map) {
        foreach (var annotation in chart.Annotations) {
            if (!annotation.EndValue.HasValue) continue;
            if (annotation.Kind == ChartAnnotationKind.HorizontalBand) {
                var y1 = Clamp(map.Y(annotation.Value), plot.Top, plot.Bottom);
                var y2 = Clamp(map.Y(annotation.EndValue.Value), plot.Top, plot.Bottom);
                var top = Math.Min(y1, y2);
                var height = Math.Abs(y2 - y1);
                sb.AppendLine($"<rect data-cfx-role=\"annotation-band\" data-cfx-kind=\"{AnnotationKindName(annotation.Kind)}\" data-cfx-value=\"{F(annotation.Value)}\" data-cfx-end=\"{F(annotation.EndValue.Value)}\" data-cfx-label=\"{Escape(annotation.Label)}\" x=\"{F(plot.Left)}\" y=\"{F(top)}\" width=\"{F(plot.Width)}\" height=\"{F(height)}\" fill=\"{annotation.Color.ToHex()}\" opacity=\"{F(annotation.Opacity)}\"/>");
                DrawBandLabel(sb, chart, annotation, plot, plot.Left + 10, top + 16);
            } else if (annotation.Kind == ChartAnnotationKind.VerticalBand) {
                var x1 = Clamp(map.X(annotation.Value), plot.Left, plot.Right);
                var x2 = Clamp(map.X(annotation.EndValue.Value), plot.Left, plot.Right);
                var left = Math.Min(x1, x2);
                var width = Math.Abs(x2 - x1);
                sb.AppendLine($"<rect data-cfx-role=\"annotation-band\" data-cfx-kind=\"{AnnotationKindName(annotation.Kind)}\" data-cfx-value=\"{F(annotation.Value)}\" data-cfx-end=\"{F(annotation.EndValue.Value)}\" data-cfx-label=\"{Escape(annotation.Label)}\" x=\"{F(left)}\" y=\"{F(plot.Top)}\" width=\"{F(width)}\" height=\"{F(plot.Height)}\" fill=\"{annotation.Color.ToHex()}\" opacity=\"{F(annotation.Opacity)}\"/>");
                DrawBandLabel(sb, chart, annotation, plot, left + 8, plot.Top + 16);
            }
        }
    }

    private static void DrawAnnotationLines(StringBuilder sb, Chart chart, ChartRect plot, ChartMapper map) {
        foreach (var annotation in chart.Annotations) {
            if (annotation.Kind == ChartAnnotationKind.HorizontalLine) {
                var y = Clamp(map.Y(annotation.Value), plot.Top, plot.Bottom);
                sb.AppendLine($"<line data-cfx-role=\"annotation-line\" data-cfx-kind=\"{AnnotationKindName(annotation.Kind)}\" data-cfx-value=\"{F(annotation.Value)}\" data-cfx-label=\"{Escape(annotation.Label)}\" x1=\"{F(plot.Left)}\" y1=\"{F(y)}\" x2=\"{F(plot.Right)}\" y2=\"{F(y)}\" stroke=\"{annotation.Color.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.AnnotationLineStrokeWidth)}\" stroke-dasharray=\"{F(ChartVisualPrimitives.AnnotationLineDash)} {F(ChartVisualPrimitives.AnnotationLineGap)}\"/>");
                DrawLineLabel(sb, chart, annotation, plot, plot.Right - 8, y - 7, "end");
            } else if (annotation.Kind == ChartAnnotationKind.VerticalLine) {
                var x = Clamp(map.X(annotation.Value), plot.Left, plot.Right);
                sb.AppendLine($"<line data-cfx-role=\"annotation-line\" data-cfx-kind=\"{AnnotationKindName(annotation.Kind)}\" data-cfx-value=\"{F(annotation.Value)}\" data-cfx-label=\"{Escape(annotation.Label)}\" x1=\"{F(x)}\" y1=\"{F(plot.Top)}\" x2=\"{F(x)}\" y2=\"{F(plot.Bottom)}\" stroke=\"{annotation.Color.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.AnnotationLineStrokeWidth)}\" stroke-dasharray=\"{F(ChartVisualPrimitives.AnnotationLineDash)} {F(ChartVisualPrimitives.AnnotationLineGap)}\"/>");
                DrawLineLabel(sb, chart, annotation, plot, x + 8, plot.Top + 16, "start");
            }
        }
    }

    private static string AnnotationKindName(ChartAnnotationKind kind) {
        if (kind == ChartAnnotationKind.HorizontalLine) return "horizontal-line";
        if (kind == ChartAnnotationKind.VerticalLine) return "vertical-line";
        if (kind == ChartAnnotationKind.HorizontalBand) return "horizontal-band";
        return "vertical-band";
    }

    private static void DrawBandLabel(StringBuilder sb, Chart chart, ChartAnnotation annotation, ChartRect plot, double x, double y) {
        if (string.IsNullOrWhiteSpace(annotation.Label)) return;
        DrawLabelPill(sb, chart, annotation.Label, x, y, chart.Options.Theme.MutedText, "start", plot);
    }

    private static void DrawLineLabel(StringBuilder sb, Chart chart, ChartAnnotation annotation, ChartRect plot, double x, double y, string anchor) {
        if (string.IsNullOrWhiteSpace(annotation.Label)) return;
        DrawLabelPill(sb, chart, annotation.Label, x, y, annotation.Color, anchor, plot);
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
        if (s.Kind == ChartSeriesKind.Lollipop) { DrawLollipops(sb, chart, index, plot, map); return; }
        if (s.Kind == ChartSeriesKind.RangeBar) { DrawRangeBars(sb, chart, index, plot, map); return; }
        if (s.Kind == ChartSeriesKind.BoxPlot) { DrawBoxPlots(sb, chart, index, plot, map); return; }
        if (s.Kind == ChartSeriesKind.Bubble) { DrawBubbles(sb, chart, index, plot, map); return; }
        if (s.Kind == ChartSeriesKind.ErrorBar) { DrawErrorBars(sb, chart, index, plot, map); return; }
        if (s.Kind == ChartSeriesKind.Candlestick) { DrawCandlesticks(sb, chart, index, plot, map); return; }
        if (s.Kind == ChartSeriesKind.Ohlc) { DrawOhlc(sb, chart, index, plot, map); return; }
        if (s.Kind == ChartSeriesKind.RangeBand) { DrawRangeBand(sb, chart, index, plot, map); return; }
        if (s.Kind == ChartSeriesKind.RangeArea) { DrawRangeArea(sb, chart, index, plot, map, id); return; }
        if (s.Kind == ChartSeriesKind.Dumbbell) { DrawDumbbells(sb, chart, index, plot, map); return; }
        if (s.Kind == ChartSeriesKind.Slope) { DrawSlope(sb, chart, index, plot, map); return; }
        if (s.Kind == ChartSeriesKind.TrendLine) { DrawTrendLine(sb, chart, index, map); return; }
        if (s.Kind == ChartSeriesKind.StackedArea) { DrawStackedArea(sb, chart, index, plot, map); return; }
        var mapped = s.Points.Select(p => new ChartPoint(map.X(p.X), map.Y(p.Y))).ToArray();
        if (s.Kind == ChartSeriesKind.Area || s.Kind == ChartSeriesKind.StepArea) {
            var first = mapped[0]; var last = mapped[mapped.Length - 1];
            var zeroY = Math.Min(plot.Bottom, Math.Max(plot.Top, map.Y(0)));
            var areaTop = s.Kind == ChartSeriesKind.StepArea ? BuildStepLinePath(mapped) : BuildLinePath(mapped, s.Smooth);
            var area = $"{areaTop} L {F(last.X)} {F(zeroY)} L {F(first.X)} {F(zeroY)} Z";
            var areaRole = s.Kind == ChartSeriesKind.StepArea ? "step-area" : "area";
            sb.AppendLine($"<path data-cfx-role=\"{areaRole}\" data-cfx-series=\"{index}\" data-cfx-point-count=\"{mapped.Length}\" d=\"{area}\" fill=\"url(#{id}-area{index})\"/>");
        }
        if (s.Kind == ChartSeriesKind.Scatter) {
            for (var pointIndex = 0; pointIndex < mapped.Length; pointIndex++) {
                var p = mapped[pointIndex];
                var raw = s.Points[pointIndex];
                var markerColor = PointColor(chart, s, index, pointIndex);
                sb.AppendLine($"<circle data-cfx-role=\"scatter-point\" data-cfx-series=\"{index}\" data-cfx-point=\"{pointIndex}\" data-cfx-x=\"{F(raw.X)}\" data-cfx-y=\"{F(raw.Y)}\" cx=\"{F(p.X)}\" cy=\"{F(p.Y)}\" r=\"{F(Math.Max(ChartVisualPrimitives.ScatterMarkerMinRadius, chart.Options.Theme.MarkerRadius + ChartVisualPrimitives.ScatterMarkerRadiusExtra))}\" fill=\"{markerColor.ToCss()}\" opacity=\"0.92\" stroke=\"{chart.Options.Theme.CardBackground.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.MarkerStrokeWidth)}\"/>");
            }
        } else {
            var line = s.Kind == ChartSeriesKind.StepLine ? BuildStepLinePath(mapped) : BuildLinePath(mapped, s.Smooth);
            if (s.Kind == ChartSeriesKind.StepArea) line = BuildStepLinePath(mapped);
            var lineRole = s.Kind == ChartSeriesKind.StepLine ? "step-line" : s.Kind == ChartSeriesKind.StepArea ? "step-area-line" : s.Kind == ChartSeriesKind.Area ? "area-line" : "line";
            sb.AppendLine($"<path data-cfx-role=\"{lineRole}-halo\" data-cfx-series=\"{index}\" data-cfx-point-count=\"{mapped.Length}\" d=\"{line}\" fill=\"none\" stroke=\"{c.ToCss()}\" stroke-width=\"{F(s.StrokeWidth + ChartVisualPrimitives.LineHaloStrokeExtra)}\" stroke-linecap=\"round\" stroke-linejoin=\"round\" opacity=\"{F(ChartVisualPrimitives.StrokeHaloOpacity)}\"/>");
            sb.AppendLine($"<path data-cfx-role=\"{lineRole}\" data-cfx-series=\"{index}\" data-cfx-point-count=\"{mapped.Length}\" d=\"{line}\" fill=\"none\" stroke=\"{c.ToCss()}\" stroke-width=\"{F(s.StrokeWidth)}\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>");
            if (!chart.Options.IsSparkline) {
                for (var pointIndex = 0; pointIndex < mapped.Length; pointIndex++) {
                    var p = mapped[pointIndex];
                    var raw = s.Points[pointIndex];
                    var markerColor = PointColor(chart, s, index, pointIndex);
                    sb.AppendLine($"<circle data-cfx-role=\"line-marker\" data-cfx-series=\"{index}\" data-cfx-point=\"{pointIndex}\" data-cfx-x=\"{F(raw.X)}\" data-cfx-y=\"{F(raw.Y)}\" cx=\"{F(p.X)}\" cy=\"{F(p.Y)}\" r=\"{F(chart.Options.Theme.MarkerRadius)}\" fill=\"{markerColor.ToCss()}\" stroke=\"{chart.Options.Theme.CardBackground.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.MarkerStrokeWidth)}\"/>");
                }
            }
        }
        if (ShouldDrawDataLabels(chart, s)) DrawPointLabels(sb, chart, s, mapped, plot);
    }

    private static void DrawTrendLine(StringBuilder sb, Chart chart, int index, ChartMapper map) {
        var series = chart.Series[index];
        if (series.Points.Count < 2) return;
        var color = Color(chart, index);
        var start = series.Points[0];
        var end = series.Points[series.Points.Count - 1];
        var x1 = map.X(start.X);
        var y1 = map.Y(start.Y);
        var x2 = map.X(end.X);
        var y2 = map.Y(end.Y);
        var slope = (end.Y - start.Y) / (end.X - start.X);
        var intercept = start.Y - slope * start.X;
        sb.AppendLine($"<line data-cfx-role=\"trend-line\" data-cfx-series=\"{index}\" data-cfx-slope=\"{F(slope)}\" data-cfx-intercept=\"{F(intercept)}\" x1=\"{F(x1)}\" y1=\"{F(y1)}\" x2=\"{F(x2)}\" y2=\"{F(y2)}\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(Math.Max(ChartVisualPrimitives.TrendLineMinStrokeWidth, series.StrokeWidth))}\" stroke-linecap=\"round\" stroke-dasharray=\"8 6\" opacity=\"0.92\"/>");
    }

    private static void DrawSlope(StringBuilder sb, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        if (series.Points.Count < 2) return;
        var color = Color(chart, index);
        var startColor = PointColor(chart, series, index, 0);
        var endColor = PointColor(chart, series, index, 1);
        var start = series.Points[0];
        var end = series.Points[1];
        var xStart = map.X(start.X);
        var yStart = map.Y(start.Y);
        var xEnd = map.X(end.X);
        var yEnd = map.Y(end.Y);
        var radius = Math.Max(ChartVisualPrimitives.SlopeMarkerMinRadius, chart.Options.Theme.MarkerRadius + ChartVisualPrimitives.SlopeMarkerRadiusExtra);
        var summary = series.Name + ": " + FormatValue(chart, start.Y) + " to " + FormatValue(chart, end.Y);

        sb.AppendLine($"<g data-cfx-role=\"slope\" data-cfx-series=\"{index}\" data-cfx-start=\"{F(start.Y)}\" data-cfx-end=\"{F(end.Y)}\" data-cfx-delta=\"{F(end.Y - start.Y)}\" role=\"img\" aria-label=\"{Escape(summary)}\">");
        sb.AppendLine($"<line data-cfx-role=\"slope-line\" data-cfx-series=\"{index}\" x1=\"{F(xStart)}\" y1=\"{F(yStart)}\" x2=\"{F(xEnd)}\" y2=\"{F(yEnd)}\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(series.StrokeWidth + ChartVisualPrimitives.LineHaloStrokeExtra)}\" stroke-linecap=\"round\" opacity=\"{F(ChartVisualPrimitives.StrokeHaloOpacity)}\"/>");
        sb.AppendLine($"<line data-cfx-role=\"slope-line\" data-cfx-series=\"{index}\" x1=\"{F(xStart)}\" y1=\"{F(yStart)}\" x2=\"{F(xEnd)}\" y2=\"{F(yEnd)}\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(series.StrokeWidth)}\" stroke-linecap=\"round\"/>");
        sb.AppendLine($"<circle data-cfx-role=\"slope-start\" data-cfx-series=\"{index}\" data-cfx-value=\"{F(start.Y)}\" cx=\"{F(xStart)}\" cy=\"{F(yStart)}\" r=\"{F(radius)}\" fill=\"{startColor.ToCss()}\" stroke=\"{chart.Options.Theme.CardBackground.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.MarkerStrokeWidth)}\"/>");
        sb.AppendLine($"<circle data-cfx-role=\"slope-end\" data-cfx-series=\"{index}\" data-cfx-value=\"{F(end.Y)}\" cx=\"{F(xEnd)}\" cy=\"{F(yEnd)}\" r=\"{F(radius)}\" fill=\"{endColor.ToCss()}\" stroke=\"{chart.Options.Theme.CardBackground.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.MarkerStrokeWidth)}\"/>");
        sb.AppendLine("</g>");
        if (!ShouldDrawDataLabels(chart, series)) return;
        DrawHorizontalValueLabel(sb, chart, FormatValue(chart, start.Y), xStart - radius - 8, yStart, "end", plot, series, 0);
        DrawHorizontalValueLabel(sb, chart, FormatValue(chart, end.Y), xEnd + radius + 8, yEnd, "start", plot, series, 1);
    }

    private static void DrawStackedArea(StringBuilder sb, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        var color = Color(chart, index);
        var upper = new List<ChartPoint>(series.Points.Count);
        var lower = new List<ChartPoint>(series.Points.Count);
        foreach (var point in series.Points) {
            var baseValue = StackAreaBaseValue(chart, index, point);
            upper.Add(new ChartPoint(map.X(point.X), map.Y(baseValue + point.Y)));
            lower.Add(new ChartPoint(map.X(point.X), map.Y(baseValue)));
        }

        var upperPath = ChartPathBuilder.FromPoints(upper, ChartSeriesKind.Line, series.Smooth).Flatten(12);
        var lowerPath = ChartPathBuilder.FromPoints(lower, ChartSeriesKind.Line, series.Smooth).Flatten(12);
        sb.AppendLine($"<path data-cfx-role=\"stacked-area\" data-cfx-series=\"{index}\" data-cfx-point-count=\"{series.Points.Count}\" d=\"{BuildClosedPolygonPath(upperPath, lowerPath)}\" fill=\"{color.ToCss()}\" opacity=\"0.42\"/>");
        var line = BuildLinePath(upperPath, false);
        sb.AppendLine($"<path data-cfx-role=\"stacked-area-line\" data-cfx-series=\"{index}\" data-cfx-point-count=\"{series.Points.Count}\" d=\"{line}\" fill=\"none\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(series.StrokeWidth + 4)}\" stroke-linecap=\"round\" stroke-linejoin=\"round\" opacity=\"0.12\"/>");
        sb.AppendLine($"<path data-cfx-role=\"stacked-area-line\" data-cfx-series=\"{index}\" data-cfx-point-count=\"{series.Points.Count}\" d=\"{line}\" fill=\"none\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(series.StrokeWidth)}\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>");
        if (!ShouldDrawDataLabels(chart, series)) return;
        var labelPoints = series.Points.Select(point => new ChartPoint(map.X(point.X), map.Y(StackAreaBaseValue(chart, index, point) + point.Y))).ToArray();
        DrawPointLabels(sb, chart, series, labelPoints, plot);
    }

    private static string BuildClosedPolygonPath(IReadOnlyList<ChartPoint> upper, IReadOnlyList<ChartPoint> lower) {
        if (upper.Count == 0) return string.Empty;
        var sb = new StringBuilder();
        sb.Append("M ").Append(F(upper[0].X)).Append(' ').Append(F(upper[0].Y));
        for (var i = 1; i < upper.Count; i++) sb.Append(" L ").Append(F(upper[i].X)).Append(' ').Append(F(upper[i].Y));
        for (var i = lower.Count - 1; i >= 0; i--) sb.Append(" L ").Append(F(lower[i].X)).Append(' ').Append(F(lower[i].Y));
        sb.Append(" Z");
        return sb.ToString();
    }

    private static void DrawLollipops(StringBuilder sb, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var s = chart.Series[index];
        var zeroY = Math.Min(plot.Bottom, Math.Max(plot.Top, map.Y(0)));
        var radius = Math.Max(4, chart.Options.Theme.MarkerRadius + 2.25);
        for (var pointIndex = 0; pointIndex < s.Points.Count; pointIndex++) {
            var p = s.Points[pointIndex];
            var x = map.X(p.X);
            var y = map.Y(p.Y);
            var c = PointColor(chart, s, index, pointIndex);
            sb.AppendLine($"<line data-cfx-role=\"lollipop-stem\" data-cfx-series=\"{index}\" data-cfx-point=\"{pointIndex}\" data-cfx-x=\"{F(p.X)}\" data-cfx-y=\"{F(p.Y)}\" x1=\"{F(x)}\" y1=\"{F(zeroY)}\" x2=\"{F(x)}\" y2=\"{F(y)}\" stroke=\"{c.ToCss()}\" stroke-width=\"{F(Math.Max(ChartVisualPrimitives.LollipopStemMinStrokeWidth, s.StrokeWidth * 0.62))}\" stroke-linecap=\"round\" opacity=\"{F(ChartVisualPrimitives.LollipopStemOpacity)}\"/>");
            sb.AppendLine($"<circle data-cfx-role=\"lollipop-marker\" data-cfx-series=\"{index}\" data-cfx-point=\"{pointIndex}\" data-cfx-x=\"{F(p.X)}\" data-cfx-y=\"{F(p.Y)}\" cx=\"{F(x)}\" cy=\"{F(y)}\" r=\"{F(radius)}\" fill=\"{c.ToCss()}\" stroke=\"{chart.Options.Theme.CardBackground.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.LollipopMarkerStrokeWidth)}\"/>");
        }

        if (ShouldDrawDataLabels(chart, s)) DrawPointLabels(sb, chart, s, s.Points.Select(p => new ChartPoint(map.X(p.X), map.Y(p.Y))).ToArray(), plot);
    }

    private static void DrawBars(StringBuilder sb, Chart chart, int index, ChartRect plot, ChartRange range, ChartMapper map, string id) {
        var s = chart.Series[index];
        var layout = BarLayout(chart, plot, index);
        var zeroY = Math.Min(plot.Bottom, Math.Max(plot.Top, map.Y(0)));
        var reservedLabels = new List<ChartLabelBounds>();
        for (var pointIndex = 0; pointIndex < s.Points.Count; pointIndex++) {
            var p = s.Points[pointIndex];
            var baseValue = chart.Options.BarMode == ChartBarMode.Stacked ? StackBaseValue(chart, index, p) : 0;
            var y = map.Y(baseValue + p.Y);
            var baseY = chart.Options.BarMode == ChartBarMode.Stacked ? map.Y(baseValue) : zeroY;
            var top = Math.Min(y, baseY);
            var height = Math.Abs(baseY - y);
            var x = map.X(p.X) + layout.Offset - layout.BarWidth / 2;
            var radius = chart.Options.BarMode == ChartBarMode.Stacked ? Math.Min(3, layout.BarWidth / 2) : Math.Min(7, layout.BarWidth / 2);
            sb.AppendLine($"<rect data-cfx-role=\"bar\" data-cfx-series=\"{index}\" data-cfx-point=\"{pointIndex}\" data-cfx-x=\"{F(p.X)}\" data-cfx-y=\"{F(p.Y)}\" data-cfx-base=\"{F(baseValue)}\" x=\"{F(x)}\" y=\"{F(top)}\" width=\"{F(layout.BarWidth)}\" height=\"{F(height)}\" rx=\"{F(radius)}\" fill=\"{BarFill(chart, s, index, pointIndex, id)}\" opacity=\"{F(ChartVisualPrimitives.BarFillOpacity)}\"/>");
            DrawSvgBarHighlight(sb, x, top, layout.BarWidth, height);
            if (ShouldDrawDataLabels(chart, s)) {
                var label = FormatValue(chart, p.Y);
                var placement = DataLabelPlacement(chart, s);
                if (placement == ChartDataLabelPlacement.Left || placement == ChartDataLabelPlacement.Right) {
                    var labelX = placement == ChartDataLabelPlacement.Right ? x + layout.BarWidth + 8 : x - 8;
                    var anchor = placement == ChartDataLabelPlacement.Right ? "start" : "end";
                    if (!ReserveSvgHorizontalLabel(label, labelX, top + height / 2, anchor, chart, plot, reservedLabels)) continue;
                    DrawHorizontalValueLabel(sb, chart, label, labelX, top + height / 2, anchor, plot, s, pointIndex);
                    continue;
                }

                var inside = placement == ChartDataLabelPlacement.Inside || placement == ChartDataLabelPlacement.Center || (chart.Options.BarMode == ChartBarMode.Stacked && placement == ChartDataLabelPlacement.Auto);
                if (inside && height < chart.Options.Theme.DataLabelFontSize + 8) continue;
                var labelY = placement == ChartDataLabelPlacement.Above
                    ? top - 10
                    : placement == ChartDataLabelPlacement.Below
                        ? top + height + 10
                        : inside
                            ? top + height / 2
                            : p.Y >= 0 ? top - 10 : top + height + 10;
                if (!ReserveSvgLabel(label, x + layout.BarWidth / 2, labelY, chart, plot, reservedLabels)) continue;
                DrawDataLabel(sb, chart, label, x + layout.BarWidth / 2, labelY, plot, series: s, pointIndex: pointIndex);
            }
        }
    }

    private static void DrawSvgBarHighlight(StringBuilder sb, double x, double y, double width, double height) {
        if (width <= ChartVisualPrimitives.BarHighlightInset * 2 + 3 || height <= ChartVisualPrimitives.BarHighlightInset * 2 + 1) return;
        var inset = ChartVisualPrimitives.BarHighlightInset;
        sb.AppendLine($"<line data-cfx-role=\"bar-highlight\" x1=\"{F(x + inset)}\" y1=\"{F(y + inset)}\" x2=\"{F(x + width - inset)}\" y2=\"{F(y + inset)}\" stroke=\"#fff\" stroke-opacity=\"{F(ChartVisualPrimitives.BarHighlightOpacity)}\" stroke-width=\"{F(ChartVisualPrimitives.BarHighlightStrokeWidth)}\" stroke-linecap=\"round\"/>");
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

    private static void DrawStackTotals(StringBuilder sb, Chart chart, ChartRect plot, ChartMapper map) {
        var positiveTotals = new Dictionary<double, double>();
        var negativeTotals = new Dictionary<double, double>();
        foreach (var series in chart.Series) {
            if (series.Kind != ChartSeriesKind.Bar) continue;
            foreach (var point in series.Points) AddStackTotal(point.Y >= 0 ? positiveTotals : negativeTotals, point.X, point.Y);
        }

        var reservedLabels = new List<ChartLabelBounds>();
        DrawStackTotalSet(sb, chart, positiveTotals, plot, map, -14, reservedLabels);
        DrawStackTotalSet(sb, chart, negativeTotals, plot, map, 14, reservedLabels);
    }

    private static void DrawStackTotalSet(StringBuilder sb, Chart chart, Dictionary<double, double> totals, ChartRect plot, ChartMapper map, double offset, List<ChartLabelBounds> reservedLabels) {
        foreach (var item in totals.OrderBy(item => item.Key)) {
            if (Math.Abs(item.Value) < 0.000001) continue;
            var label = FormatValue(chart, item.Value);
            var x = map.X(item.Key);
            var y = map.Y(item.Value) + offset;
            if (!ReserveSvgLabel(label, x, y, chart, plot, reservedLabels)) continue;
            DrawDataLabel(sb, chart, label, x, y, plot, "stack-total-label");
        }
    }

    private static void AddStackTotal(Dictionary<double, double> totals, double x, double y) {
        double current;
        totals.TryGetValue(x, out current);
        totals[x] = current + y;
    }

    private static bool ReserveSvgLabel(string label, double x, double y, Chart chart, ChartRect plot, List<ChartLabelBounds> reserved) {
        var fontSize = chart.Options.Theme.DataLabelFontSize;
        label = TrimSvgLabelToWidth(label, fontSize, PlotLabelMaxWidth(plot));
        if (label.Length == 0) return false;
        var width = EstimateTextWidth(label, fontSize) + 8;
        var height = fontSize + 6;
        var safeY = Clamp(y, plot.Top + ChartVisualPrimitives.DataLabelPlotInset + height / 2.0, plot.Bottom - ChartVisualPrimitives.DataLabelPlotInset - height / 2.0);
        var safeX = EdgeAwareTextX(label, x, plot, fontSize);
        var anchor = EdgeAwareAnchor(label, x, plot, fontSize);
        var left = anchor == "end" ? safeX - width : anchor == "start" ? safeX : safeX - width / 2;
        var bounds = new ChartLabelBounds(left, safeY - height / 2, width, height);
        foreach (var item in reserved) if (bounds.Intersects(item)) return false;
        reserved.Add(bounds);
        return true;
    }

    private static ChartRect PlotArea(Chart chart) {
        var plot = IsSpatialMapChart(chart) ? SpatialMapPlotArea(chart) : ChartLayout.PlotArea(chart.Options);
        if (chart.Options.IsSparkline || IsPieLike(chart) || IsRadialBarChart(chart) || IsLayeredRadialChart(chart)) return plot;

        if (ShouldDrawLegend(chart) && IsTopLegend(chart.Options.LegendPosition)) {
            var reserve = LegendBottomReserve(chart);
            plot = new ChartRect(plot.X, plot.Y + reserve, plot.Width, Math.Max(1, plot.Height - reserve));
        } else if (ShouldDrawLegend(chart) && IsLeftLegend(chart.Options.LegendPosition)) {
            var reserve = LegendSideReserve(chart) + ChartVisualPrimitives.SideLegendPlotGap;
            plot = new ChartRect(plot.X + reserve, plot.Y, Math.Max(1, plot.Width - reserve), plot.Height);
        } else if (ShouldDrawLegend(chart) && IsRightLegend(chart.Options.LegendPosition)) {
            var reserve = LegendSideReserve(chart) + ChartVisualPrimitives.SideLegendPlotGap;
            plot = new ChartRect(plot.X, plot.Y, Math.Max(1, plot.Width - reserve), plot.Height);
        }

        var bottomReserve = 0.0;
        if (ShowXAxis(chart)) {
            bottomReserve += XAxisTitleOffset(chart) + chart.Options.Theme.AxisTitleFontSize + 4;
            if (string.IsNullOrWhiteSpace(chart.XAxisTitle)) bottomReserve -= 18;
        }

        if (ShouldDrawLegend(chart) && IsBottomLegend(chart.Options.LegendPosition)) bottomReserve += LegendBottomReserve(chart);

        var extraBottom = Math.Max(0, bottomReserve - chart.Options.Padding.Bottom);
        if (extraBottom <= 0) return plot;
        return new ChartRect(plot.X, plot.Y, plot.Width, Math.Max(1, plot.Height - extraBottom));
    }

    private static ChartRect ApplyYAxisLabelReserve(Chart chart, ChartRect plot, IReadOnlyList<double> yTicks) {
        if (!ShowYAxis(chart) || chart.Options.IsSparkline || IsPieLike(chart) || yTicks.Count == 0) return plot;
        var t = chart.Options.Theme;
        var widest = yTicks.Max(tick => EstimateTextWidth(FormatValue(chart, tick), StyleFontSize(chart.Options.TickLabelStyle, t.TickLabelFontSize)));
        var desiredLeft = Math.Max(plot.Left, widest + 54);
        var maxLeft = Math.Max(plot.Left, chart.Options.Size.Width - chart.Options.Padding.Right - 160);
        var adjustedLeft = Math.Min(desiredLeft, maxLeft);
        if (adjustedLeft <= plot.Left) return plot;
        var shift = adjustedLeft - plot.Left;
        return new ChartRect(plot.X + shift, plot.Y, Math.Max(1, plot.Width - shift), plot.Height);
    }

    private static ChartRect ApplyXAxisBottomReserve(Chart chart, ChartRect plot, IReadOnlyList<double> xTicks, bool valueAxisOnly) {
        if (!ShowXAxis(chart) || chart.Options.IsSparkline || IsPieLike(chart) || xTicks.Count == 0) return plot;
        var labels = XAxisTickLabels(chart, xTicks, valueAxisOnly);
        var bottomReserve = XAxisTitleOffset(chart, labels) + chart.Options.Theme.AxisTitleFontSize + 4;
        if (string.IsNullOrWhiteSpace(chart.XAxisTitle)) bottomReserve -= 18;
        if (ShouldDrawLegend(chart) && IsBottomLegend(chart.Options.LegendPosition)) bottomReserve += LegendBottomReserve(chart);

        var maxBottom = Math.Max(plot.Top + 1, chart.Options.Size.Height - bottomReserve);
        if (plot.Bottom <= maxBottom) return plot;
        return new ChartRect(plot.X, plot.Y, plot.Width, Math.Max(1, maxBottom - plot.Y));
    }

    private static double HorizontalValueLabelReserve(Chart chart) {
        if (!HasHorizontalBarDataLabels(chart) && !(chart.Options.BarMode == ChartBarMode.Stacked && chart.Options.ShowStackTotals)) return 0;
        string[] labels;
        if (chart.Options.BarMode == ChartBarMode.Stacked && chart.Options.ShowStackTotals) {
            var positiveTotals = new Dictionary<double, double>();
            var negativeTotals = new Dictionary<double, double>();
            foreach (var series in chart.Series) {
                if (series.Kind != ChartSeriesKind.HorizontalBar) continue;
                foreach (var point in series.Points) AddStackTotal(point.Y >= 0 ? positiveTotals : negativeTotals, point.X, point.Y);
            }

            labels = positiveTotals.Values.Concat(negativeTotals.Values).Select(value => FormatValue(chart, value)).ToArray();
        } else {
            labels = chart.Series
                .Where(series => series.Kind == ChartSeriesKind.HorizontalBar)
                .SelectMany(series => series.Points.Select(point => FormatValue(chart, point.Y)))
                .ToArray();
        }

        if (labels.Length == 0) return 0;
        return Math.Min(96, labels.Max(label => EstimateTextWidth(label, chart.Options.Theme.DataLabelFontSize)) + 24);
    }

    private static void ApplyHorizontalValueBounds(Chart chart, ChartRange range, IReadOnlyList<double> xTicks) {
        var min = xTicks[0];
        var max = xTicks[xTicks.Count - 1];
        if (HasHorizontalBarDataLabels(chart) || (chart.Options.BarMode == ChartBarMode.Stacked && chart.Options.ShowStackTotals)) {
            var span = Math.Max(1, max - min);
            var hasPositive = chart.Series.Any(series => series.Kind == ChartSeriesKind.HorizontalBar && series.Points.Any(point => point.Y > 0));
            var hasNegative = chart.Series.Any(series => series.Kind == ChartSeriesKind.HorizontalBar && series.Points.Any(point => point.Y < 0));
            if (hasPositive) max += span * 0.08;
            if (hasNegative) min -= span * 0.08;
        }

        range.SetXBounds(min, max);
    }

    private static IReadOnlyList<string> XAxisTickLabels(Chart chart, IReadOnlyList<double> xTicks, bool valueAxisOnly) {
        var labels = new string[xTicks.Count];
        for (var i = 0; i < xTicks.Count; i++) labels[i] = valueAxisOnly ? FormatXAxisValue(chart, xTicks[i]) : FormatX(chart, xTicks[i]);
        return labels;
    }

    private static double XAxisLabelOffset(Chart chart, IReadOnlyList<string>? labels = null) {
        var angle = Math.Abs(Clamp(chart.Options.XAxisLabelAngle, -80, 80)) * Math.PI / 180;
        if (angle < 0.001) return 21;
        if ((labels == null || labels.Count == 0) && chart.Options.XAxisLabels.Count == 0) return 21;
        var widest = labels != null && labels.Count > 0
            ? labels.Max(label => EstimateTextWidth(label, StyleFontSize(chart.Options.TickLabelStyle, chart.Options.Theme.TickLabelFontSize)))
            : chart.Options.XAxisLabels.Max(label => EstimateTextWidth(label.Text, StyleFontSize(chart.Options.TickLabelStyle, chart.Options.Theme.TickLabelFontSize)));
        return 20 + Math.Sin(angle) * Math.Min(96, widest);
    }

    private static double XAxisTitleOffset(Chart chart, IReadOnlyList<string>? labels = null) {
        return XAxisLabelOffset(chart, labels) + (Math.Abs(chart.Options.XAxisLabelAngle) < 0.001 ? 23 : 48);
    }

}
