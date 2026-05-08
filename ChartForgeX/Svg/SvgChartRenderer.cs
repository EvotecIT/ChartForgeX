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

    private static void AppendSvg(StringBuilder sb, Action<SvgMarkupWriter> write) {
        var writer = new SvgMarkupWriter(512);
        write(writer);
        sb.Append(writer.Build());
    }

    private static void AppendSvgStart(StringBuilder sb, Action<SvgMarkupWriter> write) {
        var writer = new SvgMarkupWriter(512);
        write(writer);
        sb.Append(writer.ToString());
    }

    private static void AppendSvgEnd(StringBuilder sb, string name) {
        SvgMarkupWriter.ValidateName(name, nameof(name));
        sb.Append('<').Append('/').Append(name).Append('>').AppendLine();
    }

    private static void WriteSvgTextStyleAttributes(SvgMarkupWriter writer, ChartTextStyle? style) {
        if (style == null) return;
        if (style.Italic) writer.Attribute("font-style", "italic");
        if (style.Underline) writer.Attribute("text-decoration", "underline");
    }

    private static void AppendLinearGradient(StringBuilder sb, string id, string x1, string x2, string y1, string y2, string startColor, double startOpacity, string endColor, double endOpacity) {
        AppendSvg(sb, writer => writer
            .StartElement("linearGradient")
            .Attribute("id", id)
            .Attribute("x1", x1)
            .Attribute("x2", x2)
            .Attribute("y1", y1)
            .Attribute("y2", y2)
            .EndStartElement()
            .StartElement("stop")
            .Attribute("offset", "0%")
            .Attribute("stop-color", startColor)
            .Attribute("stop-opacity", startOpacity)
            .EndEmptyElement()
            .StartElement("stop")
            .Attribute("offset", "100%")
            .Attribute("stop-color", endColor)
            .Attribute("stop-opacity", endOpacity)
            .EndEmptyElement()
            .EndElement()
            .Line());
    }
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
        AppendSvgStart(sb, writer => writer
            .StartElement("svg")
            .Attribute("xmlns", "http://www.w3.org/2000/svg")
            .Attribute("width", w)
            .Attribute("height", h)
            .Attribute("viewBox", $"0 0 {F(w)} {F(h)}")
            .Attribute("role", "img")
            .Attribute("aria-labelledby", $"{id}-title {id}-desc")
            .Attribute("preserveAspectRatio", "xMidYMid meet")
            .Attribute("style", "max-width:100%;height:auto;display:block")
            .Attribute("shape-rendering", "geometricPrecision")
            .Attribute("text-rendering", "geometricPrecision")
            .EndStartElement()
            .Line());
        AppendSvg(sb, writer => writer
            .StartElement("title")
            .Attribute("id", $"{id}-title")
            .Text(string.IsNullOrWhiteSpace(chart.Title) ? "ChartForgeX chart" : chart.Title)
            .EndElement()
            .Line());
        AppendSvg(sb, writer => writer
            .StartElement("desc")
            .Attribute("id", $"{id}-desc")
            .Text(BuildDescription(chart))
            .EndElement()
            .Line());
        AppendSvgStart(sb, writer => writer.StartElement("defs").EndStartElement().Line());
        AppendSvg(sb, writer => writer
            .StartElement("style")
            .Text($"#{id} text{{font-synthesis:none}} #{id} .cfx-crisp-stroke{{vector-effect:non-scaling-stroke;shape-rendering:geometricPrecision}} #{id} .cfx-interactive-region{{transition:opacity .12s ease,stroke-width .12s ease}} #{id} .cfx-interactive-region[data-cfx-role=\"dotted-map-connector\"]{{pointer-events:stroke}} #{id} .cfx-interactive-region:hover,#{id} .cfx-interactive-region:focus{{opacity:1;outline:none;stroke-width:var(--cfx-interactive-focus-stroke-width,2.2)}}")
            .EndElement()
            .Line());
        AppendSvg(sb, writer => writer
            .StartElement("filter")
            .Attribute("id", $"{id}-softShadow")
            .Attribute("x", "-20%")
            .Attribute("y", "-20%")
            .Attribute("width", "140%")
            .Attribute("height", "140%")
            .EndStartElement()
            .StartElement("feDropShadow")
            .Attribute("dx", 0)
            .Attribute("dy", 14)
            .Attribute("stdDeviation", 18)
            .Attribute("flood-opacity", Clamp(t.ShadowOpacity, 0, 1))
            .EndEmptyElement()
            .EndElement()
            .Line());
        AppendSvg(sb, writer => writer
            .StartElement("clipPath")
            .Attribute("id", $"{id}-plotClip")
            .EndStartElement()
            .StartElement("rect")
            .Attribute("x", plot.X)
            .Attribute("y", plot.Y)
            .Attribute("width", plot.Width)
            .Attribute("height", plot.Height)
            .Attribute("rx", t.PlotCornerRadius)
            .EndEmptyElement()
            .EndElement()
            .Line());
        for (var i = 0; i < chart.Series.Count; i++) {
            var c = Color(chart, i);
            AppendLinearGradient(sb, $"{id}-area{i}", "0", "0", "0", "1", c.ToHex(), 0.32, c.ToHex(), 0.02);
            AppendLinearGradient(sb, $"{id}-seriesFill{i}", "0", "0", "0", "1", c.ToHex(), 1, c.ToHex(), 0.74);
        }
        AppendFillPatternDefinitions(sb, chart, id);
        for (var i = 0; i < t.Palette.Length; i++) {
            var c = t.Palette[i];
            AppendLinearGradient(sb, $"{id}-sliceFill{i}", "0", "1", "0", "1", c.ToHex(), 1, c.ToHex(), 0.78);
        }
        AppendSvgEnd(sb, "defs");
        AppendSvgStart(sb, writer => writer.StartElement("g").Attribute("id", id).EndStartElement().Line());
        if (!o.TransparentBackground && t.Background.A > 0) {
            AppendSvg(sb, writer => writer.StartElement("rect").Attribute("width", "100%").Attribute("height", "100%").Attribute("fill", t.Background.ToCss()).EndEmptyElement().Line());
        }
        if (o.ShowCard && t.UseCard) {
            AppendSvg(sb, writer => writer.StartElement("rect").Attribute("x", 14).Attribute("y", 14).Attribute("width", w - 28).Attribute("height", h - 28).Attribute("rx", t.CornerRadius).Attribute("fill", t.CardBackground.ToCss()).Attribute("filter", $"url(#{id}-softShadow)").EndEmptyElement().Line());
            AppendSvg(sb, writer => writer.StartElement("rect").Attribute("class", "cfx-crisp-stroke").Attribute("x", 14.5).Attribute("y", 14.5).Attribute("width", w - 29).Attribute("height", h - 29).Attribute("rx", Math.Max(0, t.CornerRadius - 0.5)).Attribute("fill", "none").Attribute("stroke", t.CardBorder.ToCss()).EndEmptyElement().Line());
        }
        if (o.ShowPlotBackground) {
            AppendSvg(sb, writer => writer.StartElement("rect").Attribute("x", plot.X).Attribute("y", plot.Y).Attribute("width", plot.Width).Attribute("height", plot.Height).Attribute("rx", t.PlotCornerRadius).Attribute("fill", t.PlotBackground.ToCss()).EndEmptyElement().Line());
            AppendSvg(sb, writer => writer.StartElement("rect").Attribute("class", "cfx-crisp-stroke").Attribute("x", plot.X + 0.5).Attribute("y", plot.Y + 0.5).Attribute("width", Math.Max(0, plot.Width - 1)).Attribute("height", Math.Max(0, plot.Height - 1)).Attribute("rx", Math.Max(0, t.PlotCornerRadius - 0.5)).Attribute("fill", "none").Attribute("stroke", t.PlotBorder.ToCss()).EndEmptyElement().Line());
        }        if (o.ShowHeader) DrawHeader(sb, chart);
        if (IsPieLike(chart)) {
            DrawPieLike(sb, chart, plot, id);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }
        if (IsGaugeChart(chart)) {
            DrawGauge(sb, chart, plot);
            DrawLegend(sb, chart, w, h);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }
        if (IsCircleChart(chart)) {
            DrawCircleChart(sb, chart, plot);
            DrawLegend(sb, chart, w, h);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }
        if (IsRadialBarChart(chart)) {
            DrawRadialBar(sb, chart, plot);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }
        if (IsLayeredRadialChart(chart)) { DrawLayeredRadial(sb, chart, plot); DrawLegend(sb, chart, w, h); AppendSvgEnd(sb, "g"); AppendSvgEnd(sb, "svg"); return sb.ToString(); }
        if (IsBulletChart(chart)) {
            DrawBullet(sb, chart, plot, id);
            DrawLegend(sb, chart, w, h);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }
        if (IsWaterfallChart(chart)) {
            DrawWaterfall(sb, chart, plot, id);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }
        if (IsRadarChart(chart)) {
            DrawRadar(sb, chart, plot, id);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }
        if (IsPolarAreaChart(chart)) {
            DrawPolarArea(sb, chart, plot, id);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }
        if (IsFunnelChart(chart)) {
            DrawFunnel(sb, chart, plot, id);
            DrawLegend(sb, chart, w, h);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }
        if (IsTreemapChart(chart)) {
            DrawTreemap(sb, chart, plot, id);
            DrawLegend(sb, chart, w, h);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }
        if (IsPictorialChart(chart)) {
            DrawPictorial(sb, chart, plot, id);
            DrawLegend(sb, chart, w, h);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }
        if (IsProgressBarChart(chart)) {
            DrawProgressBar(sb, chart, plot);
            DrawLegend(sb, chart, w, h);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }
        if (IsWordCloudChart(chart)) {
            DrawWordCloud(sb, chart, plot);
            DrawLegend(sb, chart, w, h);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }
        if (IsHeatmapChart(chart)) {
            DrawHeatmap(sb, chart, plot);
            DrawLegend(sb, chart, w, h);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }
        if (IsHexbinHeatmapChart(chart)) {
            DrawHexbinHeatmap(sb, chart, plot);
            DrawLegend(sb, chart, w, h);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }
        if (IsCalendarHeatmapChart(chart)) { DrawCalendarHeatmap(sb, chart, plot); AppendSvgEnd(sb, "g"); AppendSvgEnd(sb, "svg"); return sb.ToString(); }
        if (IsDottedMapChart(chart)) { DrawDottedMap(sb, chart, plot, id); AppendSvgEnd(sb, "g"); AppendSvgEnd(sb, "svg"); return sb.ToString(); }
        if (IsRegionMapChart(chart)) { DrawRegionMap(sb, chart, plot); AppendSvgEnd(sb, "g"); AppendSvgEnd(sb, "svg"); return sb.ToString(); }
        if (IsTileMapChart(chart)) { DrawTileMap(sb, chart, plot); AppendSvgEnd(sb, "g"); AppendSvgEnd(sb, "svg"); return sb.ToString(); }
        if (IsTimelineChart(chart)) {
            DrawTimeline(sb, chart, plot, id);
            DrawLegend(sb, chart, w, h);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }
        if (IsGanttChart(chart)) {
            DrawGantt(sb, chart, plot, id);
            DrawLegend(sb, chart, w, h);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }
        if (IsSankeyChart(chart)) {
            DrawSankey(sb, chart, plot, id);
            DrawLegend(sb, chart, w, h);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }
        if (IsTreeChart(chart)) {
            DrawTree(sb, chart, plot, id);
            DrawLegend(sb, chart, w, h);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }
        if (IsSunburstChart(chart)) {
            DrawSunburst(sb, chart, plot);
            DrawLegend(sb, chart, w, h);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }
        if (IsHorizontalBarChart(chart)) {
            DrawHorizontalBarGrid(sb, chart, plot, xTicks, yTicks, map);
            AppendSvgStart(sb, writer => writer.StartElement("g").Attribute("clip-path", $"url(#{id}-plotClip)").EndStartElement().Line());
            for (var i = 0; i < chart.Series.Count; i++) DrawSeries(sb, chart, i, plot, range, map, id);
            AppendSvgEnd(sb, "g");
            if (o.BarMode == ChartBarMode.Stacked && o.ShowStackTotals) DrawHorizontalStackTotals(sb, chart, plot, map);
            DrawLegend(sb, chart, w, h);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }

        DrawAnnotationBands(sb, chart, plot, map);
        DrawGrid(sb, chart, plot, xTicks, yTicks, map);
        if (secondaryMap != null && secondaryTicks != null) DrawSecondaryYAxis(sb, chart, plot, secondaryTicks, secondaryMap);
        AppendSvgStart(sb, writer => writer.StartElement("g").Attribute("clip-path", $"url(#{id}-plotClip)").EndStartElement().Line());
        for (var i = 0; i < chart.Series.Count; i++) DrawSeries(sb, chart, i, plot, range, SeriesMap(chart.Series[i], map, secondaryMap), id);
        if (o.BarMode == ChartBarMode.Stacked && o.ShowStackTotals) DrawStackTotals(sb, chart, plot, map);
        AppendSvgEnd(sb, "g");
        DrawAnnotationLines(sb, chart, plot, map);
        DrawLegend(sb, chart, w, h);
        AppendSvgEnd(sb, "g");
        AppendSvgEnd(sb, "svg");
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
            if (o.ShowGrid) AppendSvg(sb, writer => writer.StartElement("line").Attribute("x1", plot.Left).Attribute("y1", y).Attribute("x2", plot.Right).Attribute("y2", y).Attribute("stroke", t.Grid.ToCss()).Attribute("stroke-width", ChartVisualPrimitives.GridStrokeWidth).EndEmptyElement().Line());
            if (ShowYAxis(chart)) {
                AppendSvg(sb, writer => {
                    writer.StartElement("text").Attribute("x", plot.Left - 12).Attribute("y", y + 4).Attribute("text-anchor", "end").Attribute("fill", StyleColor(tickStyle, t.MutedText).ToCss()).Attribute("font-family", SvgFontFamily(StyleFontFamily(chart, tickStyle))).Attribute("font-size", tickFontSize);
                    WriteSvgTextStyleAttributes(writer, tickStyle);
                    writer.Text(FormatValue(chart, yv)).EndElement().Line();
                });
            }
        }
        for (var i = 0; i < xTicks.Count; i++) {
            var xv = xTicks[i];
            var x = map.X(xv);
            if (o.ShowGrid) AppendSvg(sb, writer => writer.StartElement("line").Attribute("x1", x).Attribute("y1", plot.Top).Attribute("x2", x).Attribute("y2", plot.Bottom).Attribute("stroke", t.Grid.ToCss()).Attribute("stroke-width", ChartVisualPrimitives.GridStrokeWidth).Attribute("opacity", ChartVisualPrimitives.GridVerticalOpacity).EndEmptyElement().Line());
            if (ShowXAxis(chart)) DrawXAxisLabel(sb, chart, plot, xLabels[i], x, xLabelY, xLabelAngle, maxWidth: xLabelMaxWidth);
        }
        var zeroY = map.Y(0);
        if (ShowXAxis(chart) && ShowAxisLines(chart) && zeroY > plot.Top && zeroY < plot.Bottom) {
            AppendSvg(sb, writer => writer.StartElement("line").Attribute("x1", plot.Left).Attribute("y1", zeroY).Attribute("x2", plot.Right).Attribute("y2", zeroY).Attribute("stroke", t.Axis.ToCss()).Attribute("stroke-width", ChartVisualPrimitives.ZeroAxisStrokeWidth).EndEmptyElement().Line());
        }
        if (ShowXAxis(chart)) {
            if (ShowAxisLines(chart)) AppendSvg(sb, writer => writer.StartElement("line").Attribute("x1", plot.Left).Attribute("y1", plot.Bottom).Attribute("x2", plot.Right).Attribute("y2", plot.Bottom).Attribute("stroke", t.Axis.ToCss()).Attribute("stroke-width", ChartVisualPrimitives.AxisStrokeWidth).EndEmptyElement().Line());
            DrawSvgXAxisTitle(sb, chart, plot, plot.Bottom + XAxisTitleOffset(chart, xLabels));
        }
        if (ShowYAxis(chart)) {
            if (ShowAxisLines(chart)) AppendSvg(sb, writer => writer.StartElement("line").Attribute("x1", plot.Left).Attribute("y1", plot.Top).Attribute("x2", plot.Left).Attribute("y2", plot.Bottom).Attribute("stroke", t.Axis.ToCss()).Attribute("stroke-width", ChartVisualPrimitives.AxisStrokeWidth).EndEmptyElement().Line());
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
        if (Math.Abs(angle) < 0.001) {
            var anchor = EdgeAwareAnchor(label, x, plot, fontSize);
            var safeX = EdgeAwareTextX(label, x, plot, fontSize);
            AppendSvg(sb, writer => {
                writer.StartElement("text");
                if (!string.IsNullOrWhiteSpace(role)) writer.Attribute("data-cfx-role", role);
                writer.Attribute("x", safeX).Attribute("y", y).Attribute("text-anchor", anchor).Attribute("fill", StyleColor(style, t.MutedText).ToCss()).Attribute("font-family", SvgFontFamily(StyleFontFamily(chart, style))).Attribute("font-size", fontSize);
                WriteSvgTextStyleAttributes(writer, style);
                writer.Text(label).EndElement().Line();
            });
            return;
        }

        var rotatedAnchor = RotatedAnchor(label, x, plot, angle, fontSize);
        var rotatedX = Clamp(x, plot.Left + ChartVisualPrimitives.DataLabelPlotInset, plot.Right - ChartVisualPrimitives.DataLabelPlotInset);
        AppendSvg(sb, writer => {
            writer.StartElement("text");
            if (!string.IsNullOrWhiteSpace(role)) writer.Attribute("data-cfx-role", role);
            writer.Attribute("x", rotatedX).Attribute("y", y).Attribute("text-anchor", rotatedAnchor).Attribute("dominant-baseline", "middle").Attribute("transform", $"rotate({F(angle)} {F(rotatedX)} {F(y)})").Attribute("fill", StyleColor(style, t.MutedText).ToCss()).Attribute("font-family", SvgFontFamily(StyleFontFamily(chart, style))).Attribute("font-size", fontSize);
            WriteSvgTextStyleAttributes(writer, style);
            writer.Text(label).EndElement().Line();
        });
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
                AppendSvg(sb, writer => writer.StartElement("rect").Attribute("data-cfx-role", "annotation-band").Attribute("data-cfx-kind", AnnotationKindName(annotation.Kind)).Attribute("data-cfx-value", annotation.Value).Attribute("data-cfx-end", annotation.EndValue.Value).Attribute("data-cfx-label", annotation.Label).Attribute("x", plot.Left).Attribute("y", top).Attribute("width", plot.Width).Attribute("height", height).Attribute("fill", annotation.Color.ToHex()).Attribute("opacity", annotation.Opacity).EndEmptyElement().Line());
                DrawBandLabel(sb, chart, annotation, plot, plot.Left + 10, top + 16);
            } else if (annotation.Kind == ChartAnnotationKind.VerticalBand) {
                var x1 = Clamp(map.X(annotation.Value), plot.Left, plot.Right);
                var x2 = Clamp(map.X(annotation.EndValue.Value), plot.Left, plot.Right);
                var left = Math.Min(x1, x2);
                var width = Math.Abs(x2 - x1);
                AppendSvg(sb, writer => writer.StartElement("rect").Attribute("data-cfx-role", "annotation-band").Attribute("data-cfx-kind", AnnotationKindName(annotation.Kind)).Attribute("data-cfx-value", annotation.Value).Attribute("data-cfx-end", annotation.EndValue.Value).Attribute("data-cfx-label", annotation.Label).Attribute("x", left).Attribute("y", plot.Top).Attribute("width", width).Attribute("height", plot.Height).Attribute("fill", annotation.Color.ToHex()).Attribute("opacity", annotation.Opacity).EndEmptyElement().Line());
                DrawBandLabel(sb, chart, annotation, plot, left + 8, plot.Top + 16);
            }
        }
    }

    private static void DrawAnnotationLines(StringBuilder sb, Chart chart, ChartRect plot, ChartMapper map) {
        foreach (var annotation in chart.Annotations) {
            if (annotation.Kind == ChartAnnotationKind.HorizontalLine) {
                var y = Clamp(map.Y(annotation.Value), plot.Top, plot.Bottom);
                AppendSvg(sb, writer => writer.StartElement("line").Attribute("data-cfx-role", "annotation-line").Attribute("data-cfx-kind", AnnotationKindName(annotation.Kind)).Attribute("data-cfx-value", annotation.Value).Attribute("data-cfx-label", annotation.Label).Attribute("x1", plot.Left).Attribute("y1", y).Attribute("x2", plot.Right).Attribute("y2", y).Attribute("stroke", annotation.Color.ToCss()).Attribute("stroke-width", ChartVisualPrimitives.AnnotationLineStrokeWidth).Attribute("stroke-dasharray", $"{F(ChartVisualPrimitives.AnnotationLineDash)} {F(ChartVisualPrimitives.AnnotationLineGap)}").EndEmptyElement().Line());
                DrawLineLabel(sb, chart, annotation, plot, plot.Right - 8, y - 7, "end");
            } else if (annotation.Kind == ChartAnnotationKind.VerticalLine) {
                var x = Clamp(map.X(annotation.Value), plot.Left, plot.Right);
                AppendSvg(sb, writer => writer.StartElement("line").Attribute("data-cfx-role", "annotation-line").Attribute("data-cfx-kind", AnnotationKindName(annotation.Kind)).Attribute("data-cfx-value", annotation.Value).Attribute("data-cfx-label", annotation.Label).Attribute("x1", x).Attribute("y1", plot.Top).Attribute("x2", x).Attribute("y2", plot.Bottom).Attribute("stroke", annotation.Color.ToCss()).Attribute("stroke-width", ChartVisualPrimitives.AnnotationLineStrokeWidth).Attribute("stroke-dasharray", $"{F(ChartVisualPrimitives.AnnotationLineDash)} {F(ChartVisualPrimitives.AnnotationLineGap)}").EndEmptyElement().Line());
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
        if (s.Kind == ChartSeriesKind.RangeBar) { DrawRangeBars(sb, chart, index, plot, map, id); return; }
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
            AppendSvg(sb, writer => writer.StartElement("path").Attribute("data-cfx-role", areaRole).Attribute("data-cfx-series", index).Attribute("data-cfx-point-count", mapped.Length).Attribute("d", area).Attribute("fill", $"url(#{id}-area{index})").EndEmptyElement().Line());
        }
        if (s.Kind == ChartSeriesKind.Scatter) {
            for (var pointIndex = 0; pointIndex < mapped.Length; pointIndex++) {
                var p = mapped[pointIndex];
                var raw = s.Points[pointIndex];
                var markerColor = PointColor(chart, s, index, pointIndex);
                AppendSvg(sb, writer => writer.StartElement("circle").Attribute("data-cfx-role", SeriesSemanticRole(s, "scatter-point")).Attribute("data-cfx-series", index).Attribute("data-cfx-point", pointIndex).Attribute("data-cfx-x", raw.X).Attribute("data-cfx-y", raw.Y).Attribute("cx", p.X).Attribute("cy", p.Y).Attribute("r", Math.Max(ChartVisualPrimitives.ScatterMarkerMinRadius, chart.Options.Theme.MarkerRadius + ChartVisualPrimitives.ScatterMarkerRadiusExtra)).Attribute("fill", markerColor.ToCss()).Attribute("opacity", "0.92").Attribute("stroke", chart.Options.Theme.CardBackground.ToCss()).Attribute("stroke-width", ChartVisualPrimitives.MarkerStrokeWidth).EndEmptyElement().Line());
            }
        } else {
            var line = s.Kind == ChartSeriesKind.StepLine ? BuildStepLinePath(mapped) : BuildLinePath(mapped, s.Smooth);
            if (s.Kind == ChartSeriesKind.StepArea) line = BuildStepLinePath(mapped);
            var lineRole = s.Kind == ChartSeriesKind.StepLine ? "step-line" : s.Kind == ChartSeriesKind.StepArea ? "step-area-line" : s.Kind == ChartSeriesKind.Area ? "area-line" : "line";
            AppendSvg(sb, writer => writer.StartElement("path").Attribute("data-cfx-role", $"{lineRole}-halo").Attribute("data-cfx-series", index).Attribute("data-cfx-point-count", mapped.Length).Attribute("d", line).Attribute("fill", "none").Attribute("stroke", c.ToCss()).Attribute("stroke-width", s.StrokeWidth + ChartVisualPrimitives.LineHaloStrokeExtra).Attribute("stroke-linecap", "round").Attribute("stroke-linejoin", "round").Attribute("opacity", ChartVisualPrimitives.StrokeHaloOpacity).EndEmptyElement().Line());
            AppendSvg(sb, writer => writer.StartElement("path").Attribute("data-cfx-role", lineRole).Attribute("data-cfx-series", index).Attribute("data-cfx-point-count", mapped.Length).Attribute("d", line).Attribute("fill", "none").Attribute("stroke", c.ToCss()).Attribute("stroke-width", s.StrokeWidth).Attribute("stroke-linecap", "round").Attribute("stroke-linejoin", "round").EndEmptyElement().Line());
            if (!chart.Options.IsSparkline) {
                for (var pointIndex = 0; pointIndex < mapped.Length; pointIndex++) {
                    var p = mapped[pointIndex];
                    var raw = s.Points[pointIndex];
                    var markerColor = PointColor(chart, s, index, pointIndex);
                    AppendSvg(sb, writer => writer.StartElement("circle").Attribute("data-cfx-role", "line-marker").Attribute("data-cfx-series", index).Attribute("data-cfx-point", pointIndex).Attribute("data-cfx-x", raw.X).Attribute("data-cfx-y", raw.Y).Attribute("cx", p.X).Attribute("cy", p.Y).Attribute("r", chart.Options.Theme.MarkerRadius).Attribute("fill", markerColor.ToCss()).Attribute("stroke", chart.Options.Theme.CardBackground.ToCss()).Attribute("stroke-width", ChartVisualPrimitives.MarkerStrokeWidth).EndEmptyElement().Line());
                }
            }
        }
        if (ShouldDrawDataLabels(chart, s)) DrawPointLabels(sb, chart, s, mapped, plot);
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
