using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static bool IsGanttLaneChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.GanttLane);

    private static void DrawGanttLanes(RgbaCanvas c, Chart chart, ChartRect surface) {
        var model = ChartGanttLaneModel.Build(chart);
        var bounds = ChartStateTimelineModel.ContentBounds(surface);
        var t = chart.Options.Theme;
        var tickStyle = chart.Options.TickLabelStyle;
        var tickFontSize = PngTickFontSize(chart);
        var legendStyle = chart.Options.LegendStyle;
        var legendFontSize = PngLegendFontSize(chart);
        var labelWidth = 0.0;
        var summaryWidth = model.SummaryHeader == null ? 0 : EstimatePngStyledTextWidth(model.SummaryHeader, tickFontSize, tickStyle, emphasized: true);
        foreach (var row in model.Rows) {
            if (row.IsGroup) continue;
            labelWidth = Math.Max(labelWidth, EstimatePngStyledTextWidth(row.Name, tickFontSize, tickStyle, emphasized: true));
            if (row.Summary != null) summaryWidth = Math.Max(summaryWidth, EstimatePngStyledTextWidth(row.Summary, tickFontSize, tickStyle, emphasized: true));
        }

        var legend = chart.Options.ShowLegend
            ? model.Legend.Layout(text => EstimatePngStyledTextWidth(text, legendFontSize, legendStyle, emphasized: false), bounds.Left, bounds.Width)
            : Array.Empty<ChartStateCategoryLegendItem>();
        var textHeight = EstimatePngStyledTextBoundsHeight(tickFontSize, tickStyle);
        var nowReserve = model.NowVisible && chart.Options.ShowAxes ? textHeight + 8 : 0;
        var plot = ChartStateTimelineModel.LanePlotArea(chart, bounds, model.HasSummary, model.SummaryHeader, labelWidth, summaryWidth, textHeight, ChartStateCategoryLegend.Height(legend), nowReserve);
        var rowTops = model.RowTops(plot);
        var band = model.Band(plot);

        foreach (var tick in model.Ticks) {
            var x = model.X(tick, plot);
            if (chart.Options.ShowGrid) c.DrawLine(x, plot.Top, x, plot.Bottom, ApplyOpacity(t.Grid, ChartVisualPrimitives.TimelineGridOpacity), ChartVisualPrimitives.GridStrokeWidth);
            if (!chart.Options.ShowAxes) continue;
            var label = model.FormatTick(tick);
            var width = EstimatePngStyledTextWidth(label, tickFontSize, tickStyle, emphasized: false);
            DrawPngTextStyled(c, Clamp(x - width / 2.0, plot.Left + 2, plot.Right - width - 2), plot.Bottom + 20 - PngStyledTextBottomExtent(tickFontSize, tickStyle), label, tickStyle, t.MutedText, tickFontSize, emphasized: false);
        }

        for (var rowIndex = 0; rowIndex < model.Rows.Count; rowIndex++) {
            var row = model.Rows[rowIndex];
            var top = rowTops[rowIndex];
            var height = rowTops[rowIndex + 1] - top;
            if (row.IsGroup) {
                if (rowIndex > 0) c.DrawLine(bounds.Left, top, bounds.Right, top, t.Grid, ChartVisualPrimitives.GridStrokeWidth);
                if (chart.Options.ShowAxes && row.Name.Length > 0) {
                    var group = TrimReadablePngLabelToWidth(row.Name, tickFontSize, bounds.Width, tickStyle);
                    if (group.Length > 0) DrawStateCategoryText(c, group, bounds.Left, top + height / 2, tickStyle, t.Text, tickFontSize, true);
                }

                continue;
            }

            if (chart.Options.ShowAxes) {
                var maxWidth = Math.Max(8, plot.Left - bounds.Left - ChartStateTimelineModel.ColumnGap);
                var fontSize = TextFontSizeForEmphasizedWidth(row.Name, maxWidth, tickFontSize, tickStyle);
                var label = TrimReadablePngLabelToWidth(row.Name, fontSize, maxWidth, tickStyle);
                if (label.Length > 0) DrawStateCategoryText(c, label, plot.Left - ChartStateTimelineModel.ColumnGap - EstimatePngStyledTextWidth(label, fontSize, tickStyle, emphasized: true), model.BarTop(plot, top, 0) + band / 2, tickStyle, t.MutedText, fontSize, true);
            }

            foreach (var placed in row.Items) {
                if (!model.TrySpan(placed, plot, out var left, out var width)) continue;
                var y = model.BarTop(plot, top, placed.SubRow);
                var radius = Math.Min(ChartGanttLaneModel.BarRadius, Math.Min(width, band) / 2);
                c.FillRoundedRect(left, y, width, band, radius, placed.Category.Color);
                if (placed.Category.Hatched) DrawStateCategoryHatch(c, left, y, width, band, radius);
                var text = placed.Item.Label;
                if (text != null && width >= EstimatePngStyledTextWidth(text, tickFontSize, tickStyle, emphasized: true) + ChartGanttLaneModel.LabelPadding * 2 && band >= textHeight + 2) {
                    DrawStateCategoryText(c, text, left + ChartGanttLaneModel.LabelPadding, y + band / 2, tickStyle, ChartColorMath.TextOnBackground(placed.Category.Color), tickFontSize, true);
                }
            }

            if (model.HasSummary && !string.IsNullOrWhiteSpace(row.Summary)) {
                var text = TrimReadablePngLabelToWidth(row.Summary!, tickFontSize, ChartStateTimelineModel.SummaryColumnWidth(bounds, summaryWidth), tickStyle);
                if (text.Length > 0) DrawStateCategoryText(c, text, bounds.Right - 2 - EstimatePngStyledTextWidth(text, tickFontSize, tickStyle, emphasized: true), model.BarTop(plot, top, 0) + band / 2, tickStyle, t.Text, tickFontSize, true);
            }
        }

        if (model.HasSummary && !string.IsNullOrWhiteSpace(model.SummaryHeader)) {
            var header = model.SummaryHeader!;
            DrawPngTextStyled(c, bounds.Right - 2 - EstimatePngStyledTextWidth(header, tickFontSize, tickStyle, emphasized: true), plot.Top - 8 - PngStyledTextBottomExtent(tickFontSize, tickStyle), header, tickStyle, t.MutedText, tickFontSize, emphasized: true);
        }

        if (model.NowVisible) {
            var x = model.X(model.Now!.Value, plot);
            c.DrawDashedLine(x, plot.Top, x, plot.Bottom, ApplyOpacity(t.Text, 0.7), ChartVisualPrimitives.GanttTodayStrokeWidth, 6, 5);
            if (chart.Options.ShowAxes) {
                var width = EstimatePngStyledTextWidth(chart.Options.Labels.Now, tickFontSize, tickStyle, emphasized: true);
                var labelX = Math.Max(plot.Left + 2, Math.Min(plot.Right - 2 - width, x - width / 2));
                DrawPngTextStyled(c, labelX, plot.Top - 6 - PngStyledTextBottomExtent(tickFontSize, tickStyle), chart.Options.Labels.Now, tickStyle, t.Text, tickFontSize, emphasized: true);
            }
        }

        if (chart.Options.ShowAxes) {
            c.DrawLine(plot.Left, plot.Bottom, plot.Right, plot.Bottom, t.Axis, ChartVisualPrimitives.AxisStrokeWidth);
            if (!string.IsNullOrWhiteSpace(XAxisTitleText(chart))) DrawPngXAxisTitle(c, chart, plot, plot.Bottom + ChartStateTimelineModel.AxisReserve + 14, PngAxisTitleFontSize(chart));
        }

        DrawStateCategoryLegend(c, chart, legend, bounds.Bottom - ChartStateCategoryLegend.Height(legend) + 4);
    }
}
