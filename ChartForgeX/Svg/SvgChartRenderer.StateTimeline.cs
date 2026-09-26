using System;
using System.Collections.Generic;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static bool IsStateTimelineChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.StateTimeline);

    private static void DrawStateTimeline(StringBuilder sb, Chart chart, ChartRect surface, string id) {
        var model = ChartStateTimelineModel.Build(chart);
        var bounds = ChartStateTimelineModel.ContentBounds(surface);
        var t = chart.Options.Theme;
        var tickStyle = chart.Options.TickLabelStyle;
        var tickFontSize = StyleFontSize(tickStyle, t.TickLabelFontSize);
        var legendStyle = chart.Options.LegendStyle;
        var legendFontSize = StyleFontSize(legendStyle, t.LegendFontSize);
        var laneLabelWidth = 0.0;
        var summaryWidth = model.SummaryHeader == null ? 0 : EstimateSvgStyledTextWidth(chart, model.SummaryHeader, tickFontSize, tickStyle, emphasized: true);
        foreach (var lane in model.Lanes) {
            laneLabelWidth = Math.Max(laneLabelWidth, EstimateSvgStyledTextWidth(chart, lane.Name, tickFontSize, tickStyle, emphasized: true));
            if (lane.Summary != null) summaryWidth = Math.Max(summaryWidth, EstimateSvgStyledTextWidth(chart, lane.Summary, tickFontSize, tickStyle, emphasized: true));
        }

        var legend = chart.Options.ShowLegend
            ? model.Legend.Layout(text => EstimateSvgStyledTextWidth(chart, text, legendFontSize, legendStyle), bounds.Left, bounds.Width)
            : Array.Empty<ChartStateCategoryLegendItem>();
        var plot = model.PlotArea(bounds, laneLabelWidth, summaryWidth, EstimateSvgStyledTextHeight(tickFontSize, tickStyle), ChartStateCategoryLegend.Height(legend));
        var hatchId = id + "-stateHatch";
        var writer = new SvgMarkupWriter(8192);
        writer.StartElement("g").Attribute("data-cfx-role", "state-timeline").EndStartElement().Line();
        WriteStateCategoryHatchPattern(writer, hatchId);

        foreach (var tick in model.Ticks) {
            var x = model.X(tick, plot);
            if (chart.Options.ShowGrid) {
                writer.StartElement("line").Attribute("data-cfx-role", "state-timeline-grid").Attribute("x1", x).Attribute("y1", plot.Top).Attribute("x2", x).Attribute("y2", plot.Bottom)
                    .Attribute("stroke", t.Grid.ToCss()).Attribute("stroke-width", ChartVisualPrimitives.GridStrokeWidth).Attribute("opacity", ChartVisualPrimitives.TimelineGridOpacity).EndEmptyElement().Line();
            }

            if (!chart.Options.ShowAxes) continue;
            var label = model.FormatTick(tick);
            WriteStateCategoryText(writer, chart, "state-timeline-tick-label", label, EdgeAwareStyledTextX(chart, label, x, plot, tickFontSize, tickStyle), plot.Bottom + 20, EdgeAwareStyledAnchor(chart, label, x, plot, tickFontSize, tickStyle), tickFontSize, tickStyle, "400", false);
        }

        var band = model.LaneBand(plot);
        for (var laneIndex = 0; laneIndex < model.Lanes.Count; laneIndex++) {
            var lane = model.Lanes[laneIndex];
            var series = chart.Series[lane.SeriesIndex];
            var y = model.LaneTop(plot, laneIndex);
            writer.StartElement("rect").Attribute("data-cfx-role", "state-lane-track").Attribute("x", plot.Left).Attribute("y", y).Attribute("width", plot.Width).Attribute("height", band)
                .Attribute("rx", ChartStateTimelineModel.SegmentRadius).Attribute("fill", t.Grid.ToCss()).Attribute("opacity", 0.35).EndEmptyElement().Line();
            if (chart.Options.ShowAxes) {
                var maxWidth = Math.Max(8, plot.Left - bounds.Left - ChartStateTimelineModel.ColumnGap);
                var fontSize = TextFontSizeForSvgWidth(chart, lane.Name, maxWidth, tickFontSize, tickStyle, emphasized: true);
                WriteStateCategoryText(writer, chart, "state-lane-label", TrimSvgLabelToWidth(chart, lane.Name, fontSize, maxWidth, tickStyle, emphasized: true), plot.Left - ChartStateTimelineModel.ColumnGap, y + band / 2, "end", fontSize, tickStyle, "600", true);
            }

            foreach (var segment in lane.Segments) {
                if (!model.TrySegmentSpan(segment, plot, out var left, out var width)) continue;
                var summary = model.SegmentSummary(lane, segment);
                writer.StartElement("rect")
                    .Attribute("data-cfx-role", "state-segment")
                    .Attribute("data-cfx-series", lane.SeriesIndex)
                    .Attribute("data-cfx-series-key", SeriesInteractionKey(series))
                    .Attribute("data-cfx-point", segment.PointIndex)
                    .Attribute("data-cfx-label", lane.Name + " · " + segment.State.Label)
                    .Attribute("data-cfx-status", segment.State.Key)
                    .Attribute("data-cfx-start", model.FormatInstant(segment.Start))
                    .Attribute("data-cfx-end", model.FormatInstant(segment.End))
                    .Attribute("data-cfx-meta-duration", ChartStateTimelineModel.FormatDuration(segment.End - segment.Start))
                    .Attribute("data-cfx-meta-detail", segment.Detail)
                    .Attribute("role", "img")
                    .Attribute("aria-label", summary)
                    .Attribute("x", left).Attribute("y", y).Attribute("width", width).Attribute("height", band)
                    .Attribute("rx", Math.Min(ChartStateTimelineModel.SegmentRadius, width / 2))
                    .Attribute("fill", segment.State.Color.ToCss())
                    .EndStartElement()
                    .StartElement("title").Text(summary).EndElement()
                    .EndElement().Line();
                if (segment.State.Hatched) WriteStateCategoryHatch(writer, hatchId, left, y, width, band);
            }

            if (model.HasSummary && !string.IsNullOrWhiteSpace(lane.Summary)) {
                var summaryText = TrimSvgLabelToWidth(chart, lane.Summary!, tickFontSize, ChartStateTimelineModel.SummaryColumnWidth(bounds, summaryWidth), tickStyle, emphasized: true);
                WriteStateCategoryText(writer, chart, "state-lane-summary", summaryText, bounds.Right - 2, y + band / 2, "end", tickFontSize, tickStyle, "600", true, t.Text);
            }
        }

        if (model.HasSummary && !string.IsNullOrWhiteSpace(model.SummaryHeader)) {
            WriteStateCategoryText(writer, chart, "state-summary-header", model.SummaryHeader!, bounds.Right - 2, plot.Top - 8, "end", tickFontSize, tickStyle, "600", false);
        }

        if (chart.Options.ShowAxes) {
            writer.StartElement("line").Attribute("data-cfx-role", "state-timeline-axis").Attribute("x1", plot.Left).Attribute("y1", plot.Bottom).Attribute("x2", plot.Right).Attribute("y2", plot.Bottom)
                .Attribute("stroke", t.Axis.ToCss()).Attribute("stroke-width", ChartVisualPrimitives.AxisStrokeWidth).EndEmptyElement().Line();
            var title = XAxisTitleText(chart);
            if (!string.IsNullOrWhiteSpace(title)) {
                DrawSvgTextCenteredX(writer, chart, "state-timeline-x-axis-title", title, plot.Left + plot.Width / 2, plot.Bottom + ChartStateTimelineModel.AxisReserve + 14, t.MutedText, StyleFontSize(chart.Options.AxisTitleStyle, t.AxisTitleFontSize), plot.Width - 4, "600", middleBaseline: false, style: chart.Options.AxisTitleStyle);
            }
        }

        WriteStateCategoryLegend(writer, chart, legend, bounds.Bottom - ChartStateCategoryLegend.Height(legend) + 4, hatchId);

        writer.EndElement().Line();
        sb.Append(writer.Build());
    }
}
