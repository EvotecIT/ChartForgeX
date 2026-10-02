using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static bool IsStateTimelineChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.StateTimeline);

    private static void DrawStateTimeline(RgbaCanvas c, Chart chart, ChartRect surface) {
        var model = ChartStateTimelineModel.Build(chart);
        var bounds = ChartStateTimelineModel.ContentBounds(surface);
        var t = chart.Options.Theme;
        var tickStyle = chart.Options.TickLabelStyle;
        var tickFontSize = PngTickFontSize(chart);
        var legendStyle = chart.Options.LegendStyle.WithDefaultFontWeight(600);
        var legendFontSize = PngLegendFontSize(chart);
        var laneLabelWidth = 0.0;
        var summaryWidth = model.SummaryHeader == null ? 0 : EstimatePngStyledTextWidth(model.SummaryHeader, tickFontSize, tickStyle, emphasized: true);
        foreach (var lane in model.Lanes) {
            laneLabelWidth = Math.Max(laneLabelWidth, EstimatePngStyledTextWidth(lane.Name, tickFontSize, tickStyle, emphasized: true));
            if (lane.Summary != null) summaryWidth = Math.Max(summaryWidth, EstimatePngStyledTextWidth(lane.Summary, tickFontSize, tickStyle, emphasized: true));
        }

        var tickLabels = model.Ticks.Select(model.FormatTick).ToArray();
        var axisLabelReserve = Math.Max(ChartStateTimelineModel.AxisReserve, PngXAxisTitleOffset(chart, tickLabels)
            + (string.IsNullOrWhiteSpace(XAxisTitleText(chart)) ? 0 : EstimatePngStyledTextHeight(PngXAxisTitleFontSize(chart), chart.Options.AxisTitleStyle.WithDefaultFontWeight(600)) + 4));
        var legend = chart.Options.ShowLegend
            ? model.LayoutLegend(text => EstimatePngStyledTextWidth(text, legendFontSize, legendStyle, emphasized: false), bounds.Left, bounds.Width, ChartStateTimelineModel.LegendBudgetHeight(model.PlotArea(bounds, laneLabelWidth, summaryWidth, EstimatePngStyledTextBoundsHeight(tickFontSize, tickStyle), 0, axisLabelReserve, 0).Height))
            : Array.Empty<ChartStateCategoryLegendItem>();
        var plot = model.PlotArea(bounds, laneLabelWidth, summaryWidth, EstimatePngStyledTextBoundsHeight(tickFontSize, tickStyle), ChartStateTimelineModel.LegendHeight(chart, legend), axisLabelReserve, 0);

        var range = new ChartRange();
        range.SetXBounds(model.Min, model.Max);
        var labels = model.Ticks.Select(tick => new ChartAxisLabel(tick, model.FormatTick(tick))).ToArray();
        var labelTicks = SelectXAxisTickValues(chart, range, plot, labels);

        foreach (var tick in model.Ticks) {
            var x = model.X(tick, plot);
            var gridStyle = chart.Options.GridLineStyle;
            if (chart.Options.ShowGrid && gridStyle.ShowVerticalLines) DrawPngGridLine(c, x, plot.Top, x, plot.Bottom, ApplyOpacity(t.Grid, gridStyle.VerticalOpacity), gridStyle);
            if (!ShowXAxis(chart) || !labelTicks.Contains(tick)) continue;
            var label = model.FormatTick(tick);
            DrawXAxisTickLabel(c, chart, plot, label, x, tick, labelTicks.Select(model.FormatTick).ToArray());
        }

        foreach (var group in model.Groups) {
            var top = model.GroupTop(plot, group);
            if (group.Offset > 0) c.DrawLine(bounds.Left, top, bounds.Right, top, t.Grid, ChartVisualPrimitives.GridStrokeWidth);
            if (!ShowYAxis(chart) || group.Name.Length == 0) continue;
            var groupLabel = TrimReadablePngLabelToWidth(group.Name, tickFontSize, bounds.Width, tickStyle);
            if (groupLabel.Length > 0) DrawStateCategoryText(c, groupLabel, bounds.Left, top + model.GroupHeight(plot, group) / 2, tickStyle, t.Text, tickFontSize, true);
        }

        var band = model.LaneBand(plot);
        for (var laneIndex = 0; laneIndex < model.Lanes.Count; laneIndex++) {
            var lane = model.Lanes[laneIndex];
            var y = model.LaneTop(plot, laneIndex);
            c.FillRoundedRect(plot.Left, y, plot.Width, band, ChartStateTimelineModel.SegmentRadius, ApplyOpacity(t.Grid, 0.35));
            if (ShowYAxis(chart)) {
                var maxWidth = Math.Max(8, plot.Left - bounds.Left - ChartStateTimelineModel.ColumnGap);
                var fontSize = TextFontSizeForEmphasizedWidth(lane.Name, maxWidth, tickFontSize, tickStyle);
                var label = TrimReadablePngLabelToWidth(lane.Name, fontSize, maxWidth, tickStyle);
                if (label.Length > 0) DrawStateCategoryText(c, label, plot.Left - ChartStateTimelineModel.ColumnGap - EstimatePngStyledTextWidth(label, fontSize, tickStyle, emphasized: true), y + band / 2, tickStyle, t.MutedText, fontSize, true);
            }

            foreach (var segment in lane.Segments) {
                if (!model.TrySegmentSpan(segment, plot, out var left, out var width)) continue;
                DrawStateMark(c, ChartStateMark.For(chart, segment.State), left, y, width, band, Math.Min(ChartStateTimelineModel.SegmentRadius, width / 2));
            }

            if (model.HasSummary && !string.IsNullOrWhiteSpace(lane.Summary)) {
                var summaryText = TrimReadablePngLabelToWidth(lane.Summary!, tickFontSize, ChartStateTimelineModel.SummaryColumnWidth(bounds, summaryWidth), tickStyle);
                if (summaryText.Length > 0) DrawStateCategoryText(c, summaryText, bounds.Right - 2 - EstimatePngStyledTextWidth(summaryText, tickFontSize, tickStyle, emphasized: true), y + band / 2, tickStyle, t.Text, tickFontSize, true);
            }
        }

        if (model.HasSummary && !string.IsNullOrWhiteSpace(model.SummaryHeader)) {
            var header = TrimReadablePngLabelToWidth(model.SummaryHeader!, tickFontSize, ChartStateTimelineModel.SummaryColumnWidth(bounds, summaryWidth), tickStyle);
            DrawPngTextStyled(c, bounds.Right - 2 - EstimatePngStyledTextWidth(header, tickFontSize, tickStyle, emphasized: true), plot.Top - ChartStateTimelineModel.SummaryHeaderOffset - PngStyledTextBottomExtent(tickFontSize, tickStyle), header, tickStyle, t.MutedText, tickFontSize, emphasized: true);
        }

        if (ShowXAxis(chart)) {
            if (ShowXAxisLine(chart)) c.DrawLine(plot.Left, plot.Bottom, plot.Right, plot.Bottom, t.Axis, ChartVisualPrimitives.AxisStrokeWidth, RasterLineCap.Butt);
            if (!string.IsNullOrWhiteSpace(XAxisTitleText(chart))) DrawPngXAxisTitle(c, chart, plot, plot.Bottom + PngXAxisTitleOffset(chart, tickLabels), PngAxisTitleFontSize(chart));
        }

        DrawStateCategoryLegend(c, chart, legend, bounds.Bottom - ChartStateTimelineModel.LegendHeight(chart, legend) + 4, bounds);
    }

}
