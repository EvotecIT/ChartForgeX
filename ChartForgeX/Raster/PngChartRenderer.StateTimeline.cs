using System;
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
        var legendStyle = chart.Options.LegendStyle;
        var legendFontSize = PngLegendFontSize(chart);
        var laneLabelWidth = 0.0;
        var summaryWidth = model.SummaryHeader == null ? 0 : EstimatePngStyledTextWidth(model.SummaryHeader, tickFontSize, tickStyle, emphasized: true);
        foreach (var lane in model.Lanes) {
            laneLabelWidth = Math.Max(laneLabelWidth, EstimatePngStyledTextWidth(lane.Name, tickFontSize, tickStyle, emphasized: true));
            if (lane.Summary != null) summaryWidth = Math.Max(summaryWidth, EstimatePngStyledTextWidth(lane.Summary, tickFontSize, tickStyle, emphasized: true));
        }

        var legend = chart.Options.ShowLegend
            ? model.Legend.Layout(text => EstimatePngStyledTextWidth(text, legendFontSize, legendStyle, emphasized: false), bounds.Left, bounds.Width)
            : Array.Empty<ChartStateCategoryLegendItem>();
        var plot = model.PlotArea(bounds, laneLabelWidth, summaryWidth, EstimatePngStyledTextBoundsHeight(tickFontSize, tickStyle), ChartStateCategoryLegend.Height(legend));

        foreach (var tick in model.Ticks) {
            var x = model.X(tick, plot);
            if (chart.Options.ShowGrid) c.DrawLine(x, plot.Top, x, plot.Bottom, ApplyOpacity(t.Grid, ChartVisualPrimitives.TimelineGridOpacity), ChartVisualPrimitives.GridStrokeWidth);
            if (!chart.Options.ShowAxes) continue;
            var label = model.FormatTick(tick);
            var width = EstimatePngStyledTextWidth(label, tickFontSize, tickStyle, emphasized: false);
            DrawPngTextStyled(c, Clamp(x - width / 2.0, plot.Left + 2, plot.Right - width - 2), plot.Bottom + 20 - PngStyledTextBottomExtent(tickFontSize, tickStyle), label, tickStyle, t.MutedText, tickFontSize, emphasized: false);
        }

        var band = model.LaneBand(plot);
        for (var laneIndex = 0; laneIndex < model.Lanes.Count; laneIndex++) {
            var lane = model.Lanes[laneIndex];
            var y = model.LaneTop(plot, laneIndex);
            c.FillRoundedRect(plot.Left, y, plot.Width, band, ChartStateTimelineModel.SegmentRadius, ApplyOpacity(t.Grid, 0.35));
            if (chart.Options.ShowAxes) {
                var maxWidth = Math.Max(8, plot.Left - bounds.Left - ChartStateTimelineModel.ColumnGap);
                var fontSize = TextFontSizeForEmphasizedWidth(lane.Name, maxWidth, tickFontSize, tickStyle);
                var label = TrimReadablePngLabelToWidth(lane.Name, fontSize, maxWidth, tickStyle);
                if (label.Length > 0) DrawStateCategoryText(c, label, plot.Left - ChartStateTimelineModel.ColumnGap - EstimatePngStyledTextWidth(label, fontSize, tickStyle, emphasized: true), y + band / 2, tickStyle, t.MutedText, fontSize, true);
            }

            foreach (var segment in lane.Segments) {
                if (!model.TrySegmentSpan(segment, plot, out var left, out var width)) continue;
                c.FillRoundedRect(left, y, width, band, Math.Min(ChartStateTimelineModel.SegmentRadius, width / 2), segment.State.Color);
                if (segment.State.Hatched) DrawStateCategoryHatch(c, left, y, width, band);
            }

            if (model.HasSummary && !string.IsNullOrWhiteSpace(lane.Summary)) {
                var summaryText = TrimReadablePngLabelToWidth(lane.Summary!, tickFontSize, ChartStateTimelineModel.SummaryColumnWidth(bounds, summaryWidth), tickStyle);
                if (summaryText.Length > 0) DrawStateCategoryText(c, summaryText, bounds.Right - 2 - EstimatePngStyledTextWidth(summaryText, tickFontSize, tickStyle, emphasized: true), y + band / 2, tickStyle, t.Text, tickFontSize, true);
            }
        }

        if (model.HasSummary && !string.IsNullOrWhiteSpace(model.SummaryHeader)) {
            var header = model.SummaryHeader!;
            DrawPngTextStyled(c, bounds.Right - 2 - EstimatePngStyledTextWidth(header, tickFontSize, tickStyle, emphasized: true), plot.Top - 8 - PngStyledTextBottomExtent(tickFontSize, tickStyle), header, tickStyle, t.MutedText, tickFontSize, emphasized: true);
        }

        if (chart.Options.ShowAxes) {
            c.DrawLine(plot.Left, plot.Bottom, plot.Right, plot.Bottom, t.Axis, ChartVisualPrimitives.AxisStrokeWidth);
            if (!string.IsNullOrWhiteSpace(XAxisTitleText(chart))) DrawPngXAxisTitle(c, chart, plot, plot.Bottom + ChartStateTimelineModel.AxisReserve + 14, PngAxisTitleFontSize(chart));
        }

        DrawStateCategoryLegend(c, chart, legend, bounds.Bottom - ChartStateCategoryLegend.Height(legend) + 4);
    }
}
