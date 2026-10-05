using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawSeries(StringBuilder sb, Chart chart, ChartBarCoordinateMap barCoordinateMap, int index, ChartRect plot, ChartRange range, ChartMapper map, string id, bool includeInteractionTargets) {
        var series = chart.Series[index];
        var clip = chart.Options.ClipMarksToPlot && series.Kind != ChartSeriesKind.Scatter;
        if (clip) AppendSvgStart(sb, writer => writer.StartElement("g").Attribute("clip-path", $"url(#{id}-plotClip)").EndStartElement().Line());
        DrawSeriesGeometry(sb, chart, barCoordinateMap, index, plot, range, map, id, includeInteractionTargets);
        if (clip) AppendSvgEnd(sb, "g");
        if (ChartSeriesKindTraits.UsesOptionalLineMarker(series.Kind)) {
            var mapped = series.Points.Select(p => new ChartPoint(map.X(p.X), map.Y(p.Y), p.BreakBefore)).ToArray();
            DrawOptionalLineMarkers(sb, chart, series, index, mapped, series.MarkerRadius ?? chart.Options.Theme.MarkerRadius, includeInteractionTargets, plot);
        }
    }

    private static void DrawSeriesGeometry(StringBuilder sb, Chart chart, ChartBarCoordinateMap barCoordinateMap, int index, ChartRect plot, ChartRange range, ChartMapper map, string id, bool includeInteractionTargets) {
        var s = chart.Series[index]; var c = Color(chart, index); if (s.Points.Count == 0) return;
        if (s.Kind == ChartSeriesKind.HorizontalBar) { DrawHorizontalBars(sb, chart, index, plot, map, id); return; }
        if (s.Kind == ChartSeriesKind.Bar) { DrawBars(sb, chart, barCoordinateMap, index, plot, range, map, id); return; }
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
        var mapped = s.Points.Select(p => new ChartPoint(map.X(p.X), map.Y(p.Y), p.BreakBefore)).ToArray();
        if ((s.Kind == ChartSeriesKind.Area || s.Kind == ChartSeriesKind.StepArea) && (!chart.Options.Theme.FlatMarks || chart.Series.Count == 1)) {
            var zeroY = map.YBaseline();
            foreach (var segment in ChartPointSegments.Split(mapped)) {
                var first = segment[0]; var last = segment[segment.Count - 1];
                var areaTop = s.Kind == ChartSeriesKind.StepArea ? BuildStepLinePath(segment) : BuildLinePath(segment, s.Smooth);
                var area = $"{areaTop} L {F(last.X)} {F(zeroY)} L {F(first.X)} {F(zeroY)} Z";
                var areaRole = s.Kind == ChartSeriesKind.StepArea ? "step-area" : "area";
                AppendSvg(sb, writer => writer.StartElement("path").Attribute("data-cfx-role", areaRole).Attribute("data-cfx-series", index).Attribute("data-cfx-point-count", segment.Count).Attribute("d", area).Attribute("fill", chart.Options.Theme.FlatMarks ? c.ToCss() : $"url(#{id}-area{index})").OptionalAttribute("fill-opacity", chart.Options.Theme.FlatMarks ? .12 : null).EndEmptyElement().Line());
            }
        }
        if (s.Kind == ChartSeriesKind.Scatter) {
            for (var pointIndex = 0; pointIndex < mapped.Length; pointIndex++) {
                var p = mapped[pointIndex];
                if (chart.Options.ClipMarksToPlot && !ChartPlotClip.Contains(plot, p.X, p.Y)) continue;
                var raw = s.Points[pointIndex];
                var markerColor = PointColor(chart, s, index, pointIndex);
                AppendSvg(sb, writer => writer.StartElement("circle").Attribute("data-cfx-role", SeriesSemanticRole(s, "scatter-point")).Attribute("data-cfx-series", index).Attribute("data-cfx-point", pointIndex).Attribute("data-cfx-x", raw.X).Attribute("data-cfx-y", raw.Y).Attribute("cx", p.X).Attribute("cy", p.Y).Attribute("r", chart.Options.Theme.UseGraphiteLayout ? 3 : Math.Max(ChartVisualPrimitives.ScatterMarkerMinRadius, chart.Options.Theme.MarkerRadius + ChartVisualPrimitives.ScatterMarkerRadiusExtra)).Attribute("fill", markerColor.ToCss()).Attribute("opacity", chart.Options.Theme.UseGraphiteLayout ? null : "0.92").Attribute("stroke", chart.Options.Theme.CardBackground.ToCss()).Attribute("stroke-width", chart.Options.Theme.UseGraphiteLayout ? 1.5 : ChartVisualPrimitives.MarkerStrokeWidth).EndEmptyElement().Line());
            }
        } else {
            var line = s.Kind == ChartSeriesKind.StepLine ? BuildStepLinePath(mapped) : BuildLinePath(mapped, s.Smooth);
            if (s.Kind == ChartSeriesKind.StepArea) line = BuildStepLinePath(mapped);
            var lineRole = s.Kind == ChartSeriesKind.StepLine ? "step-line" : s.Kind == ChartSeriesKind.StepArea ? "step-area-line" : s.Kind == ChartSeriesKind.Area ? "area-line" : "line";
            DrawPremiumSvgLinePath(sb, lineRole, index, mapped.Length, line, c, chart.Options.Theme.UseGraphiteLayout && !s.HasExplicitStrokeWidth ? 2 : s.StrokeWidth, chart.Options.LineVisualStyle);
        }
        if (ShouldDrawDataLabels(chart, s)) DrawPointLabels(sb, chart, s, mapped, plot);
    }

}
