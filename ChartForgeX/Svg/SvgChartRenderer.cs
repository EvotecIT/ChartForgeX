using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

namespace ChartForgeX.Svg;

/// <summary>
/// Renders charts to static SVG markup.
/// </summary>
public sealed partial class SvgChartRenderer {
    private const double LegendStartX = 40;

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
    public string Render(Chart chart, string idScope) => Render(chart, idScope, includeInteractionTargets: false);

    internal string RenderForInteraction(Chart chart, string idScope) => Render(chart, idScope, includeInteractionTargets: true);

    internal ChartLabelScene RenderLabelScene(Chart chart) {
        ChartGuards.RenderCompatibility(chart);
        var font = ChartFont(chart);
        using var measurement = ChartLabelScene.OpenFontScope(font);
        var markup = RenderCore(chart, BuildProvisionalId(chart, string.Empty), false);
        var variables = chart.Options.SvgColorVariables;
        return ChartLabelScene.Create(SvgPaint.Resolve(variables?.Apply(markup) ?? markup, variables), font);
    }

    private static Typography.FontSpec ChartFont(Chart chart) => new() { Family = chart.Options.Theme.FontFamily, FilePath = chart.Options.PngFontPath, CollectionIndex = chart.Options.PngFontCollectionIndex, FaceName = chart.Options.PngFontFaceName };

    private string Render(Chart chart, string idScope, bool includeInteractionTargets) {
        var variables = chart.Options.SvgColorVariables;
        var bound = RenderBound(chart, idScope, includeInteractionTargets);
        return SvgPaint.Resolve(variables?.Apply(bound) ?? bound, variables);
    }

    /// <summary>
    /// Renders a grid panel. A chart with its own colour variables resolves its paints with them but keeps derived
    /// literals as tokens, so the grid's variables, applied to the whole grid afterwards, cannot map them by value; a
    /// chart without variables leaves every paint to the grid.
    /// </summary>
    internal string RenderGridPanel(Chart chart, string idScope) {
        var variables = chart.Options.SvgColorVariables;
        var bound = RenderBound(chart, idScope, includeInteractionTargets: false);
        return variables == null ? bound : SvgPaint.Resolve(variables.Apply(bound), variables, keepLiterals: true);
    }

    private string RenderBound(Chart chart, string idScope, bool includeInteractionTargets) {
        ChartGuards.RenderCompatibility(chart);
        var font = ChartFont(chart);
        using var measurement = ChartLabelScene.OpenFontScope(font);
        var provisionalId = BuildProvisionalId(chart, idScope);
        var svg = RenderCore(chart, provisionalId, includeInteractionTargets);
        svg = ChartLabelScene.Create(svg, font).ToSvg();
        return SvgRenderedIdentity.Bind(svg, provisionalId, "cfx", idScope, string.Empty);
    }

    private string RenderCore(Chart chart, string id, bool includeInteractionTargets) {
        var o = chart.Options;
        var t = o.Theme;
        var w = o.Size.Width;
        var h = o.Size.Height;
        var plot = CalendarFrame(chart, PlotArea(chart));
        var barCoordinateMap = ChartBarCoordinateMap.Create(chart);
        var range = ChartRange.FromChart(chart, barCoordinateMap);
        IReadOnlyList<double> xTicks = Array.Empty<double>();
        IReadOnlyList<double> yTicks = Array.Empty<double>();
        ChartRange? secondaryRange = null;
        IReadOnlyList<double>? secondaryTicks = null;
        ChartMapper? map = null;
        ChartMapper? secondaryMap = null;
        if (ChartSeriesKindTraits.UsesCartesianXAxis(chart)) {
            if (IsHorizontalBarChart(chart)) {
                xTicks = ChartTicks.Generate(o.XAxis, range.MinX, range.MaxX);
                ApplyHorizontalValueBounds(chart, range, xTicks);
                yTicks = GetHorizontalCategoryTicks(chart, range);
                plot = ApplyHorizontalBarReserve(chart, plot, yTicks);
                if (ShowXAxis(chart)) plot = ApplyXAxisBottomReserve(chart, plot, xTicks, true);
            } else {
                yTicks = ChartTicks.Generate(o.YAxis, range.MinY, range.MaxY);
                range.SetYBounds(o.YAxis.Minimum ?? yTicks[0], o.YAxis.Maximum ?? yTicks[yTicks.Count - 1]);
                if (ShowYAxis(chart)) plot = ApplyYAxisLabelReserve(chart, plot, yTicks);
                if (HasSecondaryYAxis(chart)) {
                    secondaryRange = ChartRange.FromSecondaryYAxis(chart, range);
                    secondaryTicks = ChartTicks.Generate(o.SecondaryYAxis, secondaryRange.MinY, secondaryRange.MaxY);
                    secondaryRange.SetYBounds(o.SecondaryYAxis.Minimum ?? secondaryTicks[0], o.SecondaryYAxis.Maximum ?? secondaryTicks[secondaryTicks.Count - 1]);
                    plot = ApplySecondaryYAxisLabelReserve(chart, plot, secondaryTicks);
                }

                ChartNumericDomain.RoundX(chart, range);
                xTicks = GetXTicks(chart, range, plot);
                if (ShowXAxis(chart)) plot = ApplyXAxisBottomReserve(chart, plot, xTicks, false);
            }

            map = IsHorizontalBarChart(chart)
                ? ChartMapper.ForHorizontalBars(plot, range, o.XAxis)
                : new ChartMapper(plot, range, o.XAxis, o.YAxis);
            secondaryMap = secondaryRange == null ? null : new ChartMapper(plot, secondaryRange, o.XAxis, o.SecondaryYAxis);
        }
        var accessibility = chart.Accessibility;
        var sb = new StringBuilder();
        AppendSvgStart(sb, writer => {
            writer.StartElement("svg")
                .Attribute("xmlns", "http://www.w3.org/2000/svg")
                .Attribute("width", w)
                .Attribute("height", h)
                .Attribute("viewBox", $"0 0 {F(w)} {F(h)}")
                .Attribute("role", accessibility.IsDecorative ? null : "img")
                .Attribute("aria-hidden", accessibility.IsDecorative ? "true" : null)
                .Attribute("aria-labelledby", accessibility.IsDecorative ? null : $"{id}-title {id}-desc")
                .Attribute("lang", accessibility.Language)
                .Attribute("preserveAspectRatio", "xMidYMid meet")
                .Attribute("style", "max-width:100%;height:auto;display:block")
                .Attribute("shape-rendering", "geometricPrecision")
                .Attribute("text-rendering", "geometricPrecision");
            writer.Attribute("data-cfx-look", t.UseGraphiteLayout ? "graphite" : null);
            WriteSeriesInteractionMap(writer, chart);
            writer.EndStartElement().Line();
        });
        if (!accessibility.IsDecorative) {
            AppendSvg(sb, writer => writer
                .StartElement("title")
                .Attribute("id", $"{id}-title")
                .Text(accessibility.Name ?? (string.IsNullOrWhiteSpace(chart.Title) ? chart.Options.Labels.UntitledChart : chart.Title))
                .EndElement()
                .Line());
            AppendSvg(sb, writer => writer
                .StartElement("desc")
                .Attribute("id", $"{id}-desc")
                .Text(accessibility.Description ?? BuildDescription(chart))
                .EndElement()
                .Line());
        }
        AppendSvgStart(sb, writer => writer.StartElement("defs").EndStartElement().Line());
        AppendSvg(sb, writer => writer
            .StartElement("style")
            .Text($"#{id} text{{-webkit-font-smoothing:antialiased;text-rendering:geometricPrecision;font-synthesis:none}} #{id} .cfx-crisp-stroke,#{id} .{ChartVisualPrimitives.SvgGuideStrokeClass},#{id} .{ChartVisualPrimitives.SvgPremiumStrokeClass}{{vector-effect:non-scaling-stroke;shape-rendering:geometricPrecision}} #{id} .{ChartVisualPrimitives.SvgGuideStrokeClass}{{shape-rendering:crispEdges}} #{id} .cfx-interactive-region[data-cfx-role=\"dotted-map-connector\"]{{pointer-events:stroke}} #{id} .cfx-interactive-region:hover,#{id} .cfx-interactive-region:focus{{opacity:1;outline:none;stroke-width:var(--cfx-interactive-focus-stroke-width,2.2)}}" + ForcedColorsRule(chart, id) + FontPaletteRules(chart))
            .EndElement()
            .Line());
        if (!t.FlatMarks) {
            WriteSvgCardShadowFilter(sb, id, t);
            WriteSvgSurfaceGradient(sb, id, "cardSurface", t.CardBackground);
            WriteSvgSurfaceGradient(sb, id, "plotSurface", t.PlotBackground);
        }
        AppendSvg(sb, writer => writer
            .StartElement("clipPath")
            .Attribute("id", $"{id}-plotClip")
            .EndStartElement()
            .StartElement("rect")
            .Attribute("x", plot.X)
            .Attribute("y", plot.Y)
            .Attribute("width", plot.Width)
            .Attribute("height", plot.Height)
            .EndEmptyElement()
            .EndElement()
            .Line());
        for (var i = 0; i < chart.Series.Count; i++) {
            if (t.FlatMarks && o.BarVisualStyle.Kind == ChartBarStyle.Flat) continue;
            var c = Color(chart, i);
            AppendLinearGradient(sb, $"{id}-area{i}", "0", "0", "0", "1", c.ToHex(), 0.32, c.ToHex(), 0.02);
            AppendBarSurfaceGradient(sb, $"{id}-seriesFill{i}", c);
            for (var pointIndex = 0; pointIndex < chart.Series[i].PointColors.Count; pointIndex++) {
                if (chart.Series[i].PointColors[pointIndex].HasValue) AppendBarSurfaceGradient(sb, $"{id}-seriesFill{i}-point{pointIndex}", chart.Series[i].PointColors[pointIndex]!.Value);
            }
        }
        AppendFillPatternDefinitions(sb, chart, id);
        for (var i = 0; i < t.Palette.Length; i++) {
            if (t.FlatMarks) continue;
            var c = t.Palette[i];
            var start = ChartMarkSurface.SliceGradientStart;
            var end = ChartMarkSurface.SliceGradientEnd;
            AppendLinearGradient(sb, $"{id}-sliceFill{i}", start.X.ToString(CultureInfo.InvariantCulture), end.X.ToString(CultureInfo.InvariantCulture), start.Y.ToString(CultureInfo.InvariantCulture), end.Y.ToString(CultureInfo.InvariantCulture), c.ToCss(), 1, c.ToCss(), ChartVisualPrimitives.SliceGradientBottomOpacity);
        }
        AppendSvgEnd(sb, "defs");
        AppendSvgStart(sb, writer => writer.StartElement("g").Attribute("id", id).EndStartElement().Line());
        if (!o.HostOwnsFrame && !o.TransparentBackground && t.Background.A > 0 && !(t.UseGraphiteLayout && o.ShowCard && t.UseCard)) {
            AppendSvg(sb, writer => writer.StartElement("rect").Attribute("width", "100%").Attribute("height", "100%").Attribute("fill", t.Background.ToCss()).EndEmptyElement().Line());
        }
        if (o.ShowCard && t.UseCard && !o.HostOwnsFrame) {
            DrawSvgCardSurface(sb, id, t, w, h);
        }
        if (o.ShowPlotBackground && !o.HostOwnsFrame && !t.FlatMarks) {
            AppendSvg(sb, writer => writer.StartElement("rect").Attribute("x", plot.X).Attribute("y", plot.Y).Attribute("width", plot.Width).Attribute("height", plot.Height).Attribute("rx", t.PlotCornerRadius).Attribute("fill", $"url(#{id}-plotSurface)").EndEmptyElement().Line());
            AppendSvg(sb, writer => writer.StartElement("rect").Attribute("class", "cfx-crisp-stroke").Attribute("x", plot.X + 0.5).Attribute("y", plot.Y + 0.5).Attribute("width", Math.Max(0, plot.Width - 1)).Attribute("height", Math.Max(0, plot.Height - 1)).Attribute("rx", Math.Max(0, t.PlotCornerRadius - 0.5)).Attribute("fill", "none").Attribute("stroke", t.PlotBorder.ToCss()).EndEmptyElement().Line());
            if (t.PlotBackground.A > 0) DrawSvgSurfaceHighlight(sb, plot.X, plot.Y, plot.Width, plot.Height, t.PlotCornerRadius, ChartVisualPrimitives.PlotInnerHighlightInset, ChartVisualPrimitives.PlotInnerHighlightOpacity, "plot-inner-highlight");
        }
        if (o.ShowHeader) DrawHeader(sb, chart, plot);
        if (IsPieLike(chart)) {
            DrawPieLike(sb, chart, plot, id);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }
        if (IsGaugeChart(chart)) {
            DrawGauge(sb, chart, plot);
            DrawLegend(sb, chart, w, h, plot);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }
        if (IsCircleChart(chart)) {
            DrawCircleChart(sb, chart, plot);
            DrawLegend(sb, chart, w, h, plot);
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
        if (IsLayeredRadialChart(chart)) { DrawLayeredRadial(sb, chart, plot); DrawLegend(sb, chart, w, h, plot); AppendSvgEnd(sb, "g"); AppendSvgEnd(sb, "svg"); return sb.ToString(); }
        if (IsBulletChart(chart)) {
            DrawBullet(sb, chart, plot, id);
            DrawLegend(sb, chart, w, h, plot);
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
        if (IsPolarChart(chart)) {
            DrawPolar(sb, chart, plot);
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
            DrawLegend(sb, chart, w, h, plot);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }
        if (IsTreemapChart(chart)) {
            DrawTreemap(sb, chart, plot, id);
            DrawLegend(sb, chart, w, h, plot);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }
        if (IsPictorialChart(chart)) {
            DrawPictorial(sb, chart, plot, id);
            DrawLegend(sb, chart, w, h, plot);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }
        if (IsProgressBarChart(chart)) {
            DrawProgressBar(sb, chart, plot);
            DrawLegend(sb, chart, w, h, plot);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }
        if (IsWordCloudChart(chart)) {
            DrawWordCloud(sb, chart, plot);
            DrawLegend(sb, chart, w, h, plot);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }
        if (IsHeatmapChart(chart)) {
            DrawHeatmap(sb, chart, plot, id);
            DrawLegend(sb, chart, w, h, plot);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }
        if (IsHexbinHeatmapChart(chart)) {
            DrawHexbinHeatmap(sb, chart, plot);
            DrawLegend(sb, chart, w, h, plot);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }
        if (IsCalendarHeatmapChart(chart)) { DrawCalendarHeatmap(sb, chart, plot); AppendSvgEnd(sb, "g"); AppendSvgEnd(sb, "svg"); return sb.ToString(); }
        if (IsDottedMapChart(chart)) { DrawDottedMap(sb, chart, plot, id); AppendSvgEnd(sb, "g"); AppendSvgEnd(sb, "svg"); return sb.ToString(); }
        if (IsRegionMapChart(chart)) { DrawRegionMap(sb, chart, plot); AppendSvgEnd(sb, "g"); AppendSvgEnd(sb, "svg"); return sb.ToString(); }
        if (IsTileMapChart(chart)) { DrawTileMap(sb, chart, plot); AppendSvgEnd(sb, "g"); AppendSvgEnd(sb, "svg"); return sb.ToString(); }
        if (IsStateTimelineChart(chart)) { DrawStateTimeline(sb, chart, plot, id); AppendSvgEnd(sb, "g"); AppendSvgEnd(sb, "svg"); return sb.ToString(); }
        if (IsGanttLaneChart(chart)) { DrawGanttLanes(sb, chart, plot, id); AppendSvgEnd(sb, "g"); AppendSvgEnd(sb, "svg"); return sb.ToString(); }
        if (IsTimelineChart(chart)) {
            DrawTimeline(sb, chart, plot, id);
            DrawLegend(sb, chart, w, h, plot);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }
        if (IsGanttChart(chart)) {
            DrawGantt(sb, chart, plot, id);
            DrawLegend(sb, chart, w, h, plot);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }
        if (IsSankeyChart(chart)) {
            DrawSankey(sb, chart, plot, id);
            DrawLegend(sb, chart, w, h, plot);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }
        if (IsTreeChart(chart)) {
            DrawTree(sb, chart, plot, id);
            DrawLegend(sb, chart, w, h, plot);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }
        if (IsSunburstChart(chart)) {
            DrawSunburst(sb, chart, plot);
            DrawLegend(sb, chart, w, h, plot);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }
        if (map == null) throw new InvalidOperationException("The chart does not provide a cartesian rendering path.");
        if (IsHorizontalBarChart(chart)) {
            DrawHorizontalBarGrid(sb, chart, plot, xTicks, yTicks, map);
            AppendSvgStart(sb, writer => writer.StartElement("g").EndStartElement().Line());
            foreach (var i in ChartSeriesColours.DrawingOrder(chart)) DrawSeries(sb, chart, barCoordinateMap, i, plot, range, map, id, includeInteractionTargets);
            AppendSvgEnd(sb, "g");
            if (o.BarMode == ChartBarMode.Stacked && o.ShowStackTotals) DrawHorizontalStackTotals(sb, chart, plot, map);
            DrawLegend(sb, chart, w, h, plot);
            AppendSvgEnd(sb, "g");
            AppendSvgEnd(sb, "svg");
            return sb.ToString();
        }

        DrawAnnotationBands(sb, chart, plot, map);
        DrawGrid(sb, chart, plot, xTicks, yTicks, map);
        if (secondaryMap != null && secondaryTicks != null) DrawSecondaryYAxis(sb, chart, plot, secondaryTicks, secondaryMap);
        AppendSvgStart(sb, writer => writer.StartElement("g").EndStartElement().Line());
        foreach (var i in ChartSeriesColours.DrawingOrder(chart)) DrawSeries(sb, chart, barCoordinateMap, i, plot, range, SeriesMap(chart.Series[i], map, secondaryMap), id, includeInteractionTargets);
        if (o.BarMode == ChartBarMode.Stacked && o.ShowStackTotals) DrawStackTotals(sb, chart, barCoordinateMap, plot, map);
        AppendSvgEnd(sb, "g");
        DrawAnnotationLines(sb, chart, plot, map);
        DrawLegend(sb, chart, w, h, plot);
        AppendSvgEnd(sb, "g");
        AppendSvgEnd(sb, "svg");
        return sb.ToString();
    }

    private static void WriteSvgCardShadowFilter(StringBuilder sb, string id, ChartTheme theme) {
        var expansion = ChartVisualPrimitives.SvgCardShadowFilterExpansion;
        AppendSvg(sb, writer => writer
            .StartElement("filter")
            .Attribute("id", $"{id}-softShadow")
            .Attribute("x", $"-{F(expansion)}%")
            .Attribute("y", $"-{F(expansion)}%")
            .Attribute("width", $"{F(100 + expansion * 2)}%")
            .Attribute("height", $"{F(100 + expansion * 2)}%")
            .EndStartElement()
            .StartElement("feDropShadow")
            .Attribute("dx", 0)
            .Attribute("dy", ChartVisualPrimitives.SvgCardShadowKeyYOffset)
            .Attribute("stdDeviation", ChartVisualPrimitives.SvgCardShadowKeyBlur)
            .Attribute("flood-color", theme.ShadowColor.ToCss())
            .Attribute("flood-opacity", Clamp(theme.ShadowOpacity * ChartVisualPrimitives.SvgCardShadowKeyOpacityRatio, 0, 1))
            .EndEmptyElement()
            .StartElement("feDropShadow")
            .Attribute("dx", 0)
            .Attribute("dy", ChartVisualPrimitives.SvgCardShadowYOffset)
            .Attribute("stdDeviation", ChartVisualPrimitives.SvgCardShadowBlur)
            .Attribute("flood-color", theme.ShadowColor.ToCss())
            .Attribute("flood-opacity", Clamp(theme.ShadowOpacity, 0, 1))
            .EndEmptyElement()
            .EndElement()
            .Line());
    }

    private static void DrawSvgCardSurface(StringBuilder sb, string id, ChartTheme theme, double width, double height) {
        if (theme.FlatMarks) {
            AppendSvg(sb, writer => writer.StartElement("rect").Attribute("data-cfx-role", "card-surface")
                .Attribute("x", .5).Attribute("y", .5).Attribute("width", width - 1).Attribute("height", height - 1)
                .Attribute("rx", theme.CornerRadius).Attribute("fill", theme.CardBackground.ToCss())
                .Attribute("stroke", theme.CardBorder.ToCss()).Attribute("stroke-width", 1).EndEmptyElement().Line());
            return;
        }
        var cardInset = ChartVisualPrimitives.CardSurfaceInset;
        var borderInset = ChartVisualPrimitives.CardBorderInset;
        var borderPosition = cardInset + borderInset;
        AppendSvg(sb, writer => writer.StartElement("rect")
            .Attribute("data-cfx-role", "card-surface")
            .Attribute("x", cardInset)
            .Attribute("y", cardInset)
            .Attribute("width", width - cardInset * 2)
            .Attribute("height", height - cardInset * 2)
            .Attribute("rx", theme.CornerRadius)
            .Attribute("fill", $"url(#{id}-cardSurface)")
            .Attribute("filter", $"url(#{id}-softShadow)")
            .EndEmptyElement()
            .Line());
        AppendSvg(sb, writer => writer.StartElement("rect")
            .Attribute("data-cfx-role", "card-border")
            .Attribute("class", "cfx-crisp-stroke")
            .Attribute("x", borderPosition)
            .Attribute("y", borderPosition)
            .Attribute("width", width - borderPosition * 2)
            .Attribute("height", height - borderPosition * 2)
            .Attribute("rx", Math.Max(0, theme.CornerRadius - borderInset))
            .Attribute("fill", "none")
            .Attribute("stroke", theme.CardBorder.ToCss())
            .EndEmptyElement()
            .Line());
        if (theme.CardBackground.A > 0) {
            DrawSvgSurfaceHighlight(sb, cardInset, cardInset, width - cardInset * 2, height - cardInset * 2, theme.CornerRadius, ChartVisualPrimitives.CardInnerHighlightInset, ChartVisualPrimitives.CardInnerHighlightOpacity, "card-inner-highlight");
        }
    }

    private static void DrawGrid(StringBuilder sb, Chart chart, ChartRect plot, IReadOnlyList<double> xTicks, IReadOnlyList<double> yTicks, ChartMapper map) {
        var o = chart.Options; var t = o.Theme; var tickStyle = o.TickLabelStyle;
        var gridStyle = o.GridLineStyle;
        var tickFontSize = StyleFontSize(tickStyle, t.TickLabelFontSize);
        var xLabelAngle = Clamp(o.XAxisLabelAngle, -80, 80);
        var xLabels = XAxisTickLabels(chart, xTicks, false);
        var xLabelY = plot.Bottom + XAxisLabelOffset(chart, xLabels);
        var xLabelMaxWidth = AxisTickLabelMaxWidth(plot, xTicks.Count, xLabelAngle);
        for (var yIndex = 0; yIndex < yTicks.Count; yIndex++) {
            var yv = yTicks[yIndex];
            var y = map.Y(yv);
            if (o.ShowGrid && gridStyle.ShowHorizontalLines) WriteSvgGuideLine(sb, null, plot.Left, y, plot.Right, y, SvgPaint.Of(t.UseGraphiteLayout && Math.Abs(yv) < .000001 ? t.Axis : t.Grid, t.UseGraphiteLayout && Math.Abs(yv) < .000001 ? SvgColorRole.Axis : SvgColorRole.Grid), gridStyle.StrokeWidth, gridStyle.HorizontalOpacity, gridStyle);
            if (ShowYAxis(chart) && ChartAxisDensity.ShowVerticalLabel(yIndex, yTicks.Count, plot.Height, tickFontSize, o.YAxisLabelDensity)) {
                AppendSvg(sb, writer => {
                    var label = StyleText(tickStyle, FormatYAxisValue(chart, yv, yTicks));
                    writer.StartElement("text").Attribute("data-cfx-role", "y-axis-label").Attribute("data-cfx-value", yv).Attribute("x", plot.Left - (t.UseGraphiteLayout ? 8 : 12)).Attribute("y", y + 4).Attribute("text-anchor", "end").Attribute("fill", StyleColor(tickStyle, t.MutedText).ToCss()).Attribute("font-family", SvgFontFamily(StyleFontFamily(chart, tickStyle))).Attribute("font-size", tickFontSize).Attribute("font-weight", StyleWeight(tickStyle, "400"));
                    WriteSvgTextStyleAttributes(writer, tickStyle);
                    WriteSvgStyledTextContent(writer, tickStyle, label).EndElement().Line();
                });
            }
        }
        for (var i = 0; i < xTicks.Count; i++) {
            var xv = xTicks[i];
            var x = map.X(xv);
            if (o.ShowGrid && gridStyle.ShowVerticalLines) WriteSvgGridLine(sb, x, plot.Top, x, plot.Bottom, t.Grid.ToCss(), gridStyle.StrokeWidth, gridStyle.VerticalOpacity, gridStyle);
            var labelColor = o.TryGetXAxisLabelHighlight(xv, out var highlight) ? highlight : (ChartColor?)null;
            if (ShowXAxis(chart)) DrawXAxisLabel(sb, chart, plot, xLabels[i], x, xLabelY, xLabelAngle, maxWidth: xLabelMaxWidth, color: labelColor);
        }
        if (o.YAxis.Scale != ChartScaleKind.Logarithmic) {
            var zeroY = map.Y(0);
            if (ShowXAxisLine(chart) && zeroY > plot.Top && zeroY < plot.Bottom) {
                WriteSvgGuideLine(sb, null, plot.Left, zeroY, plot.Right, zeroY, t.Axis.ToCss(), ChartVisualPrimitives.ZeroAxisStrokeWidth);
            }
        }
        if (ShowXAxis(chart)) {
            if (ShowXAxisLine(chart)) WriteSvgGuideLine(sb, null, plot.Left, plot.Bottom, plot.Right, plot.Bottom, t.Axis.ToCss(), ChartVisualPrimitives.AxisStrokeWidth);
            DrawSvgXAxisTitle(sb, chart, plot, plot.Bottom + XAxisTitleOffset(chart, xLabels));
        }
        if (ShowYAxis(chart)) {
            if (ShowYAxisLine(chart)) WriteSvgGuideLine(sb, null, plot.Left, plot.Top, plot.Left, plot.Bottom, t.Axis.ToCss(), ChartVisualPrimitives.AxisStrokeWidth);
            DrawSvgYAxisTitle(sb, chart, plot, 26);
        }
    }

    private static void DrawXAxisLabel(StringBuilder sb, Chart chart, ChartRect plot, string label, double x, double y, double angle, string? role = null, double maxWidth = 0, ChartColor? color = null) {
        var t = chart.Options.Theme;
        var style = chart.Options.TickLabelStyle;
        var labelColor = color ?? StyleColor(style, t.MutedText);
        var preferredFontSize = StyleFontSize(style, t.TickLabelFontSize);
        var widthLimit = maxWidth > 0 ? maxWidth : PlotLabelMaxWidth(plot);
        var fontSize = TextFontSizeForSvgWidth(chart, label, widthLimit, preferredFontSize, style);
        label = TrimSvgLabelToWidth(chart, label, fontSize, widthLimit, style);
        if (label.Length == 0) return;
        if (Math.Abs(angle) < 0.001) {
            var centered = t.UseGraphiteLayout || chart.Options.XAxis.Scale == ChartScaleKind.Linear && chart.Options.XAxisLabels.Count == 0 && ChartSeriesKindTraits.UsesCartesianXAxis(chart);
            var anchor = centered ? "middle" : EdgeAwareStyledAnchor(chart, label, x, plot, fontSize, style);
            var safeX = centered ? x : EdgeAwareStyledTextX(chart, label, x, plot, fontSize, style);
            AppendSvg(sb, writer => {
                writer.StartElement("text");
                writer.Attribute("data-cfx-role", string.IsNullOrWhiteSpace(role) ? "x-axis-label" : role);
                writer.Attribute("x", safeX).Attribute("y", y).Attribute("text-anchor", anchor).Attribute("fill", labelColor.ToCss()).Attribute("font-family", SvgFontFamily(StyleFontFamily(chart, style))).Attribute("font-size", fontSize).Attribute("font-weight", StyleWeight(style, "400"));
                WriteSvgTextStyleAttributes(writer, style);
                WriteSvgStyledTextContent(writer, style, label).EndElement().Line();
            });
            return;
        }

        var rotatedAnchor = RotatedStyledAnchor(chart, label, x, plot, angle, fontSize, style);
        var rotatedX = Clamp(x, plot.Left + ChartVisualPrimitives.DataLabelPlotInset, plot.Right - ChartVisualPrimitives.DataLabelPlotInset);
        AppendSvg(sb, writer => {
            writer.StartElement("text");
            writer.Attribute("data-cfx-role", string.IsNullOrWhiteSpace(role) ? "x-axis-label" : role);
            writer.Attribute("x", rotatedX).Attribute("y", y).Attribute("text-anchor", rotatedAnchor).Attribute("dominant-baseline", "middle").Attribute("transform", $"rotate({F(angle)} {F(rotatedX)} {F(y)})").Attribute("fill", labelColor.ToCss()).Attribute("font-family", SvgFontFamily(StyleFontFamily(chart, style))).Attribute("font-size", fontSize).Attribute("font-weight", StyleWeight(style, "400"));
            WriteSvgTextStyleAttributes(writer, style);
            WriteSvgStyledTextContent(writer, style, label).EndElement().Line();
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

    private static string BuildSlicePath(double cx, double cy, double radius, double innerRadius, double start, double end) =>
        ChartSlicePathGeometry.BuildPath(cx, cy, radius, innerRadius, start, end);

    private static ChartRect PlotArea(Chart chart) {
        var plot = IsSpatialMapChart(chart) ? SpatialMapPlotArea(chart) : ChartLayout.PlotArea(chart.Options);
        if (chart.Options.Theme.UseGraphiteLayout && !chart.Options.HasExplicitPadding && !chart.Options.IsSparkline) {
            var top = chart.Options.ShowHeader ? ChartLayout.HeaderBottom(chart) + 8 : chart.Options.Padding.Top;
            plot = new ChartRect(plot.X, top, plot.Width, Math.Max(1, chart.Options.Size.Height - chart.Options.Padding.Bottom - top));
        }
        if (chart.Options.IsSparkline || IsPieLike(chart) || IsRadialBarChart(chart) || IsLayeredRadialChart(chart) || IsStateTimelineChart(chart) || IsGanttLaneChart(chart)) return plot;

        if (ShouldDrawLegend(chart) && IsTopLegend(chart.Options.LegendPosition)) {
            var reserve = LegendBottomReserve(chart);
            plot = new ChartRect(plot.X, plot.Y + reserve, plot.Width, Math.Max(1, plot.Height - reserve));
        } else if (ShouldDrawLegend(chart) && IsLeftLegend(chart.Options.LegendPosition)) {
            var legendReserve = LegendSideReserve(chart);
            var reserve = legendReserve > 0 ? legendReserve + ChartVisualPrimitives.SideLegendPlotGap : 0;
            plot = new ChartRect(plot.X + reserve, plot.Y, Math.Max(1, plot.Width - reserve), plot.Height);
        } else if (ShouldDrawLegend(chart) && IsRightLegend(chart.Options.LegendPosition)) {
            var legendReserve = LegendSideReserve(chart);
            var reserve = legendReserve > 0 ? legendReserve + ChartVisualPrimitives.SideLegendPlotGap : 0;
            plot = new ChartRect(plot.X, plot.Y, Math.Max(1, plot.Width - reserve), plot.Height);
        }

        var bottomReserve = 0.0;
        // A matrix heatmap reserves the band under it for its own column labels (see ApplyHeatmapLabelReserve).
        if (ShowXAxis(chart) && !IsHeatmapChart(chart)) {
            bottomReserve += SvgXAxisBottomReserve(chart, null, chart.Options.Size.Width - chart.Options.Padding.Left - chart.Options.Padding.Right);
        }

        if (ShouldDrawLegend(chart) && IsBottomLegend(chart.Options.LegendPosition)) bottomReserve += LegendBottomReserve(chart);

        var extraBottom = Math.Max(0, bottomReserve - chart.Options.Padding.Bottom);
        if (extraBottom <= 0) return plot;
        return new ChartRect(plot.X, plot.Y, plot.Width, Math.Max(1, plot.Height - extraBottom));
    }

    private static ChartRect ApplyYAxisLabelReserve(Chart chart, ChartRect plot, IReadOnlyList<double> yTicks) {
        if (!ShowYAxis(chart) || chart.Options.IsSparkline || IsPieLike(chart) || yTicks.Count == 0) return plot;
        var t = chart.Options.Theme;
        var tickStyle = chart.Options.TickLabelStyle;
        var tickFontSize = StyleFontSize(tickStyle, t.TickLabelFontSize);
        var widest = yTicks.Max(tick => EstimateSvgStyledTextWidth(chart, FormatYAxisValue(chart, tick, yTicks), tickFontSize, tickStyle));
        var titleHeight = string.IsNullOrWhiteSpace(chart.YAxisTitle) ? 0 : SvgYAxisTitleHeight(chart, plot.Height);
        var desiredLeft = Math.Max(plot.Left, t.UseGraphiteLayout ? chart.Options.Padding.Left + widest + 8 : widest + 54 + Math.Max(0, titleHeight - t.AxisTitleFontSize));
        var maxLeft = Math.Max(plot.Left, chart.Options.Size.Width - chart.Options.Padding.Right - 160);
        var adjustedLeft = Math.Min(desiredLeft, maxLeft);
        if (adjustedLeft <= plot.Left) return plot;
        var shift = adjustedLeft - plot.Left;
        return new ChartRect(plot.X + shift, plot.Y, Math.Max(1, plot.Width - shift), plot.Height);
    }

    private static ChartRect ApplyXAxisBottomReserve(Chart chart, ChartRect plot, IReadOnlyList<double> xTicks, bool valueAxisOnly) {
        if (!ShowXAxis(chart) || chart.Options.IsSparkline || IsPieLike(chart) || xTicks.Count == 0) return plot;
        var labels = XAxisTickLabels(chart, xTicks, valueAxisOnly);
        var bottomReserve = SvgXAxisBottomReserve(chart, labels, plot.Width);
        if (ShouldDrawLegend(chart) && IsBottomLegend(chart.Options.LegendPosition)) bottomReserve += LegendBottomReserve(chart);

        var maxBottom = Math.Max(plot.Top + 1, chart.Options.Size.Height - bottomReserve);
        if (plot.Bottom <= maxBottom) return plot;
        return new ChartRect(plot.X, plot.Y, plot.Width, Math.Max(1, maxBottom - plot.Y));
    }

    private static double HorizontalValueLabelReserve(Chart chart) {
        if (!HasHorizontalBarDataLabels(chart) && !(chart.Options.BarMode == ChartBarMode.Stacked && chart.Options.ShowStackTotals)) return 0;
        var maxWidth = 0.0;
        if (chart.Options.BarMode == ChartBarMode.Stacked && chart.Options.ShowStackTotals) {
            var positiveTotals = new Dictionary<double, double>();
            var negativeTotals = new Dictionary<double, double>();
            foreach (var series in chart.Series) {
                if (series.Kind != ChartSeriesKind.HorizontalBar) continue;
                foreach (var point in series.Points) AddStackTotal(point.Y >= 0 ? positiveTotals : negativeTotals, point.X, point.Y);
            }

            var style = chart.Options.DataLabelStyle;
            var fontSize = StyleFontSize(style, chart.Options.Theme.DataLabelFontSize);
            foreach (var value in positiveTotals.Values.Concat(negativeTotals.Values)) {
                maxWidth = Math.Max(maxWidth, EstimateTextWidth(StyleText(style, FormatValue(chart, value)), fontSize));
            }
        } else {
            foreach (var series in chart.Series.Where(series => series.Kind == ChartSeriesKind.HorizontalBar)) {
                for (var pointIndex = 0; pointIndex < series.Points.Count; pointIndex++) {
                    var style = DataLabelStyle(chart, series, pointIndex);
                    var fontSize = StyleFontSize(style, chart.Options.Theme.DataLabelFontSize);
                    var label = StyleText(style, FormatValue(chart, series.Points[pointIndex].Y));
                    maxWidth = Math.Max(maxWidth, EstimateTextWidth(label, fontSize));
                }
            }
        }

        return maxWidth <= 0 ? 0 : Math.Min(96, maxWidth + 24);
    }

    private static void ApplyHorizontalValueBounds(Chart chart, ChartRange range, IReadOnlyList<double> xTicks) {
        range.SetXBounds(chart.Options.XAxis.Minimum ?? xTicks[0], chart.Options.XAxis.Maximum ?? xTicks[xTicks.Count - 1]);
    }

    private static IReadOnlyList<string> XAxisTickLabels(Chart chart, IReadOnlyList<double> xTicks, bool valueAxisOnly) {
        var labels = new string[xTicks.Count];
        for (var i = 0; i < xTicks.Count; i++) labels[i] = ChartTimeScale.FormatFallbackTick(chart, xTicks, xTicks[i], valueAxisOnly) ?? (valueAxisOnly ? FormatXAxisValue(chart, xTicks[i]) : FormatX(chart, xTicks[i]));
        return labels;
    }

    private static double XAxisLabelOffset(Chart chart, IReadOnlyList<string>? labels = null) {
        var angle = Math.Abs(Clamp(chart.Options.XAxisLabelAngle, -80, 80)) * Math.PI / 180;
        var tickStyle = chart.Options.TickLabelStyle;
        var tickFontSize = StyleFontSize(tickStyle, chart.Options.Theme.TickLabelFontSize);
        var tickHeight = EstimateSvgStyledTextHeight(tickFontSize, tickStyle);
        var baseOffset = Math.Max(21, tickHeight + 8);
        if (angle < 0.001) return baseOffset;
        if ((labels == null || labels.Count == 0) && chart.Options.XAxisLabels.Count == 0) return baseOffset;
        var widest = labels != null && labels.Count > 0
            ? labels.Max(label => EstimateSvgStyledTextWidth(chart, label, tickFontSize, tickStyle))
            : chart.Options.XAxisLabels.Max(label => EstimateSvgStyledTextWidth(chart, label.Text, tickFontSize, tickStyle));
        return Math.Max(baseOffset, tickHeight + 7 + Math.Sin(angle) * Math.Min(96, widest));
    }

    private static double XAxisTitleOffset(Chart chart, IReadOnlyList<string>? labels = null) {
        var tickHeight = EstimateSvgStyledTextHeight(StyleFontSize(chart.Options.TickLabelStyle, chart.Options.Theme.TickLabelFontSize), chart.Options.TickLabelStyle);
        var titleHeight = SvgXAxisTitleHeight(chart, chart.Options.Size.Width);
        var gap = Math.Max(Math.Abs(chart.Options.XAxisLabelAngle) < 0.001 ? tickHeight + 10 : Math.Max(48, tickHeight + 10), titleHeight + tickHeight * 0.25 + 4);
        return XAxisLabelOffset(chart, labels) + gap;
    }

    private static double SvgXAxisBottomReserve(Chart chart, IReadOnlyList<string>? labels, double maxWidth) {
        var tickHeight = EstimateSvgStyledTextHeight(StyleFontSize(chart.Options.TickLabelStyle, chart.Options.Theme.TickLabelFontSize), chart.Options.TickLabelStyle);
        if (string.IsNullOrWhiteSpace(XAxisTitleText(chart))) return XAxisLabelOffset(chart, labels) + tickHeight + 10;
        return XAxisTitleOffset(chart, labels) + SvgXAxisTitleHeight(chart, maxWidth) + 4;
    }

}
