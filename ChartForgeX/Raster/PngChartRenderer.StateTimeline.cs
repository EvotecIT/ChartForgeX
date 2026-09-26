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
            ? model.LayoutLegend(text => EstimatePngStyledTextWidth(text, legendFontSize, legendStyle, emphasized: false), bounds.Left, bounds.Width)
            : Array.Empty<ChartStateTimelineLegendItem>();
        var plot = model.PlotArea(bounds, laneLabelWidth, summaryWidth, EstimatePngStyledTextBoundsHeight(tickFontSize, tickStyle), ChartStateTimelineModel.LegendHeight(legend));

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
                if (label.Length > 0) DrawStateTimelineText(c, label, plot.Left - ChartStateTimelineModel.ColumnGap - EstimatePngStyledTextWidth(label, fontSize, tickStyle, emphasized: true), y + band / 2, tickStyle, t.MutedText, fontSize, true);
            }

            foreach (var segment in lane.Segments) {
                if (!model.TrySegmentSpan(segment, plot, out var left, out var width)) continue;
                c.FillRoundedRect(left, y, width, band, Math.Min(ChartStateTimelineModel.SegmentRadius, width / 2), segment.State.Color);
                if (segment.State.Hatched) DrawStateTimelineHatch(c, left, y, width, band);
            }

            if (model.HasSummary && !string.IsNullOrWhiteSpace(lane.Summary)) {
                var summaryText = TrimReadablePngLabelToWidth(lane.Summary!, tickFontSize, ChartStateTimelineModel.SummaryColumnWidth(bounds, summaryWidth), tickStyle);
                if (summaryText.Length > 0) DrawStateTimelineText(c, summaryText, bounds.Right - 2 - EstimatePngStyledTextWidth(summaryText, tickFontSize, tickStyle, emphasized: true), y + band / 2, tickStyle, t.Text, tickFontSize, true);
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

        var legendTop = bounds.Bottom - ChartStateTimelineModel.LegendHeight(legend) + 4;
        foreach (var item in legend) {
            var rowY = legendTop + item.Row * ChartStateTimelineModel.LegendRowHeight;
            c.FillRoundedRect(item.X, rowY, ChartStateTimelineModel.LegendSwatch, ChartStateTimelineModel.LegendSwatch, ChartStateTimelineModel.SegmentRadius, item.State.Color);
            if (item.State.Hatched) DrawStateTimelineHatch(c, item.X, rowY, ChartStateTimelineModel.LegendSwatch, ChartStateTimelineModel.LegendSwatch);
            DrawStateTimelineText(c, item.State.Label, item.X + ChartStateTimelineModel.LegendSwatch + 6, rowY + ChartStateTimelineModel.LegendSwatch / 2, legendStyle, t.MutedText, legendFontSize, false);
        }
    }

    private static void DrawStateTimelineHatch(RgbaCanvas c, double x, double y, double width, double height) {
        var color = ApplyOpacity(ChartColor.White, ChartStateTimelineModel.HatchOpacity);
        foreach (var line in ChartPatternLineGeometry.Build(ChartFillPattern.DiagonalForward, x, y, width, height, Math.Min(ChartStateTimelineModel.SegmentRadius, width / 2), ChartStateTimelineModel.HatchSpacing * 1.4142)) {
            c.DrawLine(line.X1, line.Y1, line.X2, line.Y2, color, 1.5);
        }
    }

    private static void DrawStateTimelineText(RgbaCanvas c, string text, double x, double centerY, TextStyleOverride style, ChartColor color, double fontSize, bool emphasized) {
        DrawPngTextStyled(c, x, centerY - EstimatePngStyledTextBoundsHeight(fontSize, style) / 2 - PngStyledTextTopExtent(fontSize, style), text, style, color, fontSize, emphasized);
    }
}
