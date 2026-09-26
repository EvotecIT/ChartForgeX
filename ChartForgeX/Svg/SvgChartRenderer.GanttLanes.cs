using System;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static bool IsGanttLaneChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.GanttLane);

    private static void DrawGanttLanes(StringBuilder sb, Chart chart, ChartRect surface, string id) {
        var model = ChartGanttLaneModel.Build(chart);
        var bounds = ChartStateTimelineModel.ContentBounds(surface);
        var t = chart.Options.Theme;
        var tickStyle = chart.Options.TickLabelStyle;
        var tickFontSize = StyleFontSize(tickStyle, t.TickLabelFontSize);
        var legendStyle = chart.Options.LegendStyle;
        var legendFontSize = StyleFontSize(legendStyle, t.LegendFontSize);
        var labelWidth = 0.0;
        var summaryWidth = model.SummaryHeader == null ? 0 : EstimateSvgStyledTextWidth(chart, model.SummaryHeader, tickFontSize, tickStyle, emphasized: true);
        foreach (var row in model.Rows) {
            if (row.IsGroup) continue;
            labelWidth = Math.Max(labelWidth, EstimateSvgStyledTextWidth(chart, row.Name, tickFontSize, tickStyle, emphasized: true));
            if (row.Summary != null) summaryWidth = Math.Max(summaryWidth, EstimateSvgStyledTextWidth(chart, row.Summary, tickFontSize, tickStyle, emphasized: true));
        }

        var legend = chart.Options.ShowLegend
            ? model.Legend.Layout(text => EstimateSvgStyledTextWidth(chart, text, legendFontSize, legendStyle), bounds.Left, bounds.Width)
            : Array.Empty<ChartStateCategoryLegendItem>();
        var textHeight = EstimateSvgStyledTextHeight(tickFontSize, tickStyle);
        var nowReserve = model.NowVisible && chart.Options.ShowAxes ? textHeight + 8 : 0;
        var plot = ChartStateTimelineModel.LanePlotArea(chart, bounds, model.HasSummary, model.SummaryHeader, labelWidth, summaryWidth, textHeight, ChartStateCategoryLegend.Height(legend), nowReserve);
        var hatchId = id + "-ganttLaneHatch";
        var rowTops = model.RowTops(plot);
        var band = model.Band(plot);
        var writer = new SvgMarkupWriter(8192);
        writer.StartElement("g").Attribute("data-cfx-role", "gantt-lanes").EndStartElement().Line();
        WriteStateCategoryHatchPattern(writer, hatchId);

        foreach (var tick in model.Ticks) {
            var x = model.X(tick, plot);
            if (chart.Options.ShowGrid) {
                writer.StartElement("line").Attribute("data-cfx-role", "gantt-lanes-grid").Attribute("x1", x).Attribute("y1", plot.Top).Attribute("x2", x).Attribute("y2", plot.Bottom)
                    .Attribute("stroke", t.Grid.ToCss()).Attribute("stroke-width", ChartVisualPrimitives.GridStrokeWidth).Attribute("opacity", ChartVisualPrimitives.TimelineGridOpacity).EndEmptyElement().Line();
            }

            if (!chart.Options.ShowAxes) continue;
            var label = model.FormatTick(tick);
            WriteStateCategoryText(writer, chart, "gantt-lanes-tick-label", label, EdgeAwareStyledTextX(chart, label, x, plot, tickFontSize, tickStyle), plot.Bottom + 20, EdgeAwareStyledAnchor(chart, label, x, plot, tickFontSize, tickStyle), tickFontSize, tickStyle, "400", false);
        }

        for (var rowIndex = 0; rowIndex < model.Rows.Count; rowIndex++) {
            var row = model.Rows[rowIndex];
            var top = rowTops[rowIndex];
            var height = rowTops[rowIndex + 1] - top;
            if (row.IsGroup) {
                if (rowIndex > 0) {
                    writer.StartElement("line").Attribute("data-cfx-role", "gantt-lane-group-rule").Attribute("x1", bounds.Left).Attribute("y1", top).Attribute("x2", bounds.Right).Attribute("y2", top)
                        .Attribute("stroke", t.Grid.ToCss()).Attribute("stroke-width", ChartVisualPrimitives.GridStrokeWidth).EndEmptyElement().Line();
                }

                if (chart.Options.ShowAxes && row.Name.Length > 0) WriteStateCategoryText(writer, chart, "gantt-lane-group", TrimSvgLabelToWidth(chart, row.Name, tickFontSize, bounds.Width, tickStyle, emphasized: true), bounds.Left, top + height / 2, "start", tickFontSize, tickStyle, "700", true, t.Text);
                continue;
            }

            var series = chart.Series[row.SeriesIndex];
            if (chart.Options.ShowAxes) {
                var maxWidth = Math.Max(8, plot.Left - bounds.Left - ChartStateTimelineModel.ColumnGap);
                var fontSize = TextFontSizeForSvgWidth(chart, row.Name, maxWidth, tickFontSize, tickStyle, emphasized: true);
                WriteStateCategoryText(writer, chart, "gantt-lane-label", TrimSvgLabelToWidth(chart, row.Name, fontSize, maxWidth, tickStyle, emphasized: true), plot.Left - ChartStateTimelineModel.ColumnGap, model.BarTop(plot, top, 0) + band / 2, "end", fontSize, tickStyle, "600", true);
            }

            foreach (var placed in row.Items) {
                if (!model.TrySpan(placed, plot, out var left, out var width)) continue;
                var y = model.BarTop(plot, top, placed.SubRow);
                var summary = model.ItemSummary(row, placed);
                var radius = Math.Min(ChartGanttLaneModel.BarRadius, Math.Min(width, band) / 2);
                writer.StartElement("rect")
                    .Attribute("data-cfx-role", "gantt-lane-item")
                    .Attribute("data-cfx-series", row.SeriesIndex)
                    .Attribute("data-cfx-series-key", SeriesInteractionKey(series))
                    .Attribute("data-cfx-point", placed.PointIndex)
                    .Attribute("data-cfx-label", row.Name + " · " + (placed.Item.Label ?? placed.Category.Label))
                    .Attribute("data-cfx-status", placed.Category.Key)
                    .Attribute("data-cfx-start", ChartTimeScale.FormatInstant(chart.Options.XAxis, placed.Item.Start))
                    .Attribute("data-cfx-end", placed.Item.IsOpen ? null : ChartTimeScale.FormatInstant(chart.Options.XAxis, placed.End))
                    .Attribute("data-cfx-meta-state", placed.Category.Label)
                    .Attribute("data-cfx-meta-duration", ChartStateTimelineModel.FormatDuration(placed.End - placed.Item.Start))
                    .Attribute("data-cfx-meta-ongoing", placed.Item.IsOpen ? "true" : null)
                    .Attribute("data-cfx-meta-detail", placed.Item.Detail)
                    .Attribute("data-cfx-sub-row", placed.SubRow)
                    .Attribute("role", "img")
                    .Attribute("aria-label", summary)
                    .Attribute("x", left).Attribute("y", y).Attribute("width", width).Attribute("height", band)
                    .Attribute("rx", radius)
                    .Attribute("fill", placed.Category.Color.ToCss())
                    .EndStartElement()
                    .StartElement("title").Text(summary).EndElement()
                    .EndElement().Line();
                if (placed.Category.Hatched) WriteStateCategoryHatch(writer, hatchId, left, y, width, band, radius, "gantt-lane-item-hatch");
                var text = placed.Item.Label;
                if (text != null && width >= EstimateSvgStyledTextWidth(chart, text, tickFontSize, tickStyle, emphasized: true) + ChartGanttLaneModel.LabelPadding * 2 && band >= textHeight + 2) {
                    WriteStateCategoryText(writer, chart, "gantt-lane-item-label", text, left + ChartGanttLaneModel.LabelPadding, y + band / 2, "start", tickFontSize, tickStyle, "600", true, ChartColorMath.TextOnBackground(placed.Category.Color));
                }
            }

            if (model.HasSummary && !string.IsNullOrWhiteSpace(row.Summary)) {
                var text = TrimSvgLabelToWidth(chart, row.Summary!, tickFontSize, ChartStateTimelineModel.SummaryColumnWidth(bounds, summaryWidth), tickStyle, emphasized: true);
                WriteStateCategoryText(writer, chart, "gantt-lane-summary", text, bounds.Right - 2, model.BarTop(plot, top, 0) + band / 2, "end", tickFontSize, tickStyle, "600", true, t.Text);
            }
        }

        if (model.HasSummary && !string.IsNullOrWhiteSpace(model.SummaryHeader)) {
            WriteStateCategoryText(writer, chart, "gantt-lanes-summary-header", model.SummaryHeader!, bounds.Right - 2, plot.Top - 8, "end", tickFontSize, tickStyle, "600", false);
        }

        if (model.NowVisible) {
            var x = model.X(model.Now!.Value, plot);
            writer.StartElement("line").Attribute("data-cfx-role", "gantt-lanes-now").Attribute("x1", x).Attribute("y1", plot.Top).Attribute("x2", x).Attribute("y2", plot.Bottom)
                .Attribute("stroke", t.Text.ToCss()).Attribute("stroke-opacity", 0.7).Attribute("stroke-width", ChartVisualPrimitives.GanttTodayStrokeWidth).Attribute("stroke-dasharray", "6 5").EndEmptyElement().Line();
            if (chart.Options.ShowAxes) WriteStateCategoryText(writer, chart, "gantt-lanes-now-label", "Now", x > plot.Right - 24 ? plot.Right - 2 : x < plot.Left + 24 ? plot.Left + 2 : x, plot.Top - 6, x > plot.Right - 24 ? "end" : x < plot.Left + 24 ? "start" : "middle", tickFontSize, tickStyle, "700", false, t.Text);
        }

        if (chart.Options.ShowAxes) {
            writer.StartElement("line").Attribute("data-cfx-role", "gantt-lanes-axis").Attribute("x1", plot.Left).Attribute("y1", plot.Bottom).Attribute("x2", plot.Right).Attribute("y2", plot.Bottom)
                .Attribute("stroke", t.Axis.ToCss()).Attribute("stroke-width", ChartVisualPrimitives.AxisStrokeWidth).EndEmptyElement().Line();
            var title = XAxisTitleText(chart);
            if (!string.IsNullOrWhiteSpace(title)) {
                DrawSvgTextCenteredX(writer, chart, "gantt-lanes-x-axis-title", title, plot.Left + plot.Width / 2, plot.Bottom + ChartStateTimelineModel.AxisReserve + 14, t.MutedText, StyleFontSize(chart.Options.AxisTitleStyle, t.AxisTitleFontSize), plot.Width - 4, "600", middleBaseline: false, style: chart.Options.AxisTitleStyle);
            }
        }

        WriteStateCategoryLegend(writer, chart, legend, bounds.Bottom - ChartStateCategoryLegend.Height(legend) + 4, hatchId);
        writer.EndElement().Line();
        sb.Append(writer.Build());
    }
}
