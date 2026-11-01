using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;

namespace ChartForgeX.Html;

public sealed partial class HtmlChartRenderer {
    private static string RenderDataTables(Chart chart, ChartDataTableMode mode) {
        var title = string.IsNullOrWhiteSpace(chart.Title) ? chart.Options.Labels.UntitledChart : chart.Title;
        var writer = new HtmlMarkupWriter();
        writer.StartElement("style").EndStartElement().RawTrusted(
            ".chartforgex-data-tables{max-width:100%;overflow:auto}.chartforgex-data-tables table{border-collapse:collapse;margin:1em 0;font:inherit;color:inherit}" +
            ".chartforgex-data-tables caption{text-align:left;font-weight:bold}.chartforgex-data-tables th,.chartforgex-data-tables td{padding:.35em .65em;text-align:left;border-bottom:1px solid currentColor}" +
            "@media screen{.chartforgex-data-table-hidden{position:absolute;width:1px;height:1px;padding:0;margin:-1px;overflow:hidden;clip:rect(0,0,0,0);white-space:nowrap;border:0}}" +
            "@media print{.chartforgex-data-tables{overflow:visible}.chartforgex-data-tables::details-content{display:block;content-visibility:visible}.chartforgex-data-tables table{break-inside:avoid}}")
            .EndElement();
        writer.StartElement(mode == ChartDataTableMode.Details ? "details" : "div")
            .Attribute("class", "chartforgex-data-tables" + (mode == ChartDataTableMode.VisuallyHidden ? " chartforgex-data-table-hidden" : string.Empty))
            .EndStartElement();
        if (mode == ChartDataTableMode.Details)
            writer.StartElement("summary").EndStartElement().Text("View chart data: " + title).EndElement();
        foreach (var series in chart.Series) WriteDataTable(writer, chart, series, title);
        if (chart.Series.Count == 0) writer.StartElement("p").EndStartElement().Text(chart.Options.Labels.NoData).EndElement();
        return writer.EndElement().Build();
    }

    private static void WriteDataTable(HtmlMarkupWriter writer, Chart chart, ChartSeries series, string title) {
        var headers = DataTableHeaders(chart, series);
        var caption = title + " — " + series.Name;
        if (!string.IsNullOrWhiteSpace(series.LaneGroup)) caption += " — " + series.LaneGroup;
        if (!string.IsNullOrWhiteSpace(series.LaneSummary)) caption += " — " + series.LaneSummary;
        writer.StartElement("table").EndStartElement()
            .StartElement("caption").EndStartElement().Text(caption).EndElement()
            .StartElement("thead").EndStartElement().StartElement("tr").EndStartElement();
        foreach (var header in headers) writer.StartElement("th").Attribute("scope", "col").EndStartElement().Text(header).EndElement();
        writer.EndElement().EndElement().StartElement("tbody").EndStartElement();
        var hasRows = false;
        foreach (var row in DataTableRows(chart, series)) {
            hasRows = true;
            writer.StartElement("tr").EndStartElement();
            for (var index = 0; index < row.Length; index++) {
                writer.StartElement(index == 0 ? "th" : "td");
                if (index == 0) writer.Attribute("scope", "row");
                writer.EndStartElement().Text(row[index]).EndElement();
            }
            writer.EndElement();
        }
        if (!hasRows) writer.StartElement("tr").EndStartElement().StartElement("td").Attribute("colspan", headers.Length)
            .EndStartElement().Text(chart.Options.Labels.NoData).EndElement().EndElement();
        writer.EndElement().EndElement();
    }

    private static string[] DataTableHeaders(Chart chart, ChartSeries series) {
        var words = chart.Options.Labels;
        switch (series.Kind) {
            case ChartSeriesKind.Gantt: return new[] { words.Start, words.End, "Progress", "Kind", "Label" };
            case ChartSeriesKind.Timeline: return new[] { words.Start, words.End, "Label" };
            case ChartSeriesKind.GanttLane: return new[] { words.Start, words.End, words.Category, "Label", "Detail" };
            case ChartSeriesKind.StateTimeline: return new[] { words.Start, words.End, "State", "Detail", "Label" };
            case ChartSeriesKind.CalendarHeatmap: return new[] { "Date", words.Value, "Label" };
            case ChartSeriesKind.RangeBand:
            case ChartSeriesKind.RangeArea: return new[] { CoordinateHeader(chart, series), "Lower", "Upper", "Label" };
            case ChartSeriesKind.RangeBar:
            case ChartSeriesKind.Dumbbell: return new[] { CoordinateHeader(chart, series), words.Start, words.End, "Label" };
        }
        if (series.IsCategoricalHeatmapRow) return new[] { words.Category, "State", "Label", "Detail" };
        if (series.UsesTypedObservations || ChartSeries.IsRelationshipKind(series.Kind))
            throw new NotSupportedException("HTML data tables do not yet support " + series.Kind + " observations.");
        return new[] { CoordinateHeader(chart, series), ValueHeader(chart, series), "Label" };
    }

    private static IEnumerable<string?[]> DataTableRows(Chart chart, ChartSeries series) {
        string Coordinate(double value) => ChartAxisValueFormatter.FindExplicitLabel(ChartCartesianOrientation.CategoryLabels(chart), value)
            ?? ChartAxisValueFormatter.Format(ChartCartesianOrientation.CategoryAxis(chart), value);
        string Value(double value) => chart.Options.WithUnit(series.ValueAxis == ChartAxisSide.Secondary
            && ChartCartesianOrientation.ValueAxis(chart, series.ValueAxis).LabelFormatter is { } formatter ? formatter(value)
            : ChartNumericFormatter.FormatValue(chart.Options, value));
        string? Label(int index) => index < series.PointLabels.Count ? series.PointLabels[index] : null;
        string State(string key) => chart.Options.StateCategories.FirstOrDefault(category => category.Key == key)?.Label ?? key;

        switch (series.Kind) {
            case ChartSeriesKind.Gantt:
                for (var index = 0; index < series.GanttItems.Count; index++) {
                    var item = series.GanttItems[index];
                    yield return new[] { Coordinate(item.Start), item.IsOpenEnded ? chart.Options.Labels.Ongoing : Coordinate(item.End),
                        item.Progress.ToString("P0", CultureInfo.InvariantCulture), item.IsVerticalMarker ? "Marker" : item.IsMilestone ? "Milestone" : "Task", Label(index) };
                }
                yield break;
            case ChartSeriesKind.Timeline:
                for (var index = 0; index < series.Points.Count; index++) {
                    var item = series.Points[index];
                    yield return new[] { Coordinate(item.X), Coordinate(item.Y), Label(index) };
                }
                yield break;
            case ChartSeriesKind.GanttLane:
                for (var index = 0; index < series.GanttLaneItems.Count; index++) {
                    var item = series.GanttLaneItems[index];
                    yield return new[] { Coordinate(item.Start), item.End.HasValue ? Coordinate(item.End.Value) : chart.Options.Labels.Ongoing,
                        State(item.Category), item.Label ?? Label(index), item.Detail };
                }
                yield break;
            case ChartSeriesKind.StateTimeline:
                for (var index = 0; index < series.StateTimelineSegments.Count; index++) {
                    var item = series.StateTimelineSegments[index];
                    yield return new[] { Coordinate(item.Start), Coordinate(item.End), State(item.State), item.Detail, Label(index) };
                }
                yield break;
            case ChartSeriesKind.CalendarHeatmap:
                for (var index = 0; index < series.CalendarItems.Count; index++) {
                    var item = series.CalendarItems[index];
                    yield return new[] { chart.Options.Labels.FormatDate(item.Date), Value(item.Value), Label(index) };
                }
                yield break;
            case ChartSeriesKind.RangeBand:
            case ChartSeriesKind.RangeArea:
                for (var index = 0; index < series.Ranges.Count; index++) {
                    var item = series.Ranges[index];
                    yield return new[] { Coordinate(item.X), Value(item.Lower), Value(item.Upper), Label(index) };
                }
                yield break;
            case ChartSeriesKind.RangeBar:
                for (var index = 0; index < series.Intervals.Count; index++) {
                    var item = series.Intervals[index];
                    yield return new[] { Coordinate(item.X), Value(item.Start), Value(item.End), Label(index) };
                }
                yield break;
            case ChartSeriesKind.Dumbbell:
                for (var index = 0; index < series.Dumbbells.Count; index++) {
                    var item = series.Dumbbells[index];
                    yield return new[] { Coordinate(item.X), Value(item.Start), Value(item.End), Label(index) };
                }
                yield break;
        }
        if (series.IsCategoricalHeatmapRow) {
            for (var index = 0; index < series.HeatmapCells.Count; index++) {
                var cell = series.HeatmapCells[index];
                yield return new[] { Coordinate(index + 1), cell.HasValue ? State(cell.Value.State) : chart.Options.Labels.NoData,
                    cell?.Text ?? Label(index), cell?.Tooltip };
            }
            yield break;
        }
        if (series.HeatmapColumnCount.HasValue) {
            // Masked numeric matrices retain their authored column span even though null cells have no point.
            var cells = series.Points.Select((point, index) => new { Point = point, Index = index }).ToDictionary(cell => cell.Point.X);
            for (var column = 1; column <= series.HeatmapColumnCount.Value; column++)
                yield return cells.TryGetValue(column, out var cell)
                    ? new[] { Coordinate(column), Value(cell.Point.Y), Label(cell.Index) }
                    : new[] { Coordinate(column), chart.Options.Labels.NoData, null };
            yield break;
        }
        for (var index = 0; index < series.Points.Count; index++) {
            var point = series.Points[index];
            yield return new[] { Coordinate(point.X), Value(point.Y), Label(index) };
        }
    }

    private static string CoordinateHeader(Chart chart, ChartSeries series) {
        var title = ChartCartesianOrientation.IsHorizontal(chart) ? chart.YAxisTitle : chart.XAxisTitle;
        return !string.IsNullOrWhiteSpace(title) ? title : series.Kind == ChartSeriesKind.Scatter ? "X" : chart.Options.Labels.Category;
    }

    private static string ValueHeader(Chart chart, ChartSeries series) {
        var title = series.ValueAxis == ChartAxisSide.Secondary ? chart.SecondaryYAxisTitle
            : ChartCartesianOrientation.IsHorizontal(chart) ? chart.XAxisTitle : chart.YAxisTitle;
        return !string.IsNullOrWhiteSpace(title) ? title : chart.Options.Labels.Value;
    }
}
