using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

internal static partial class Program {
    private static void MixedScheduleContracts() {
        var chart = Chart.Create().WithSize(800, 520).WithHeader(false).WithLegend(false).WithXAxisBounds(0, 10)
            .WithStateCategories(new ChartStateCategory("up", "Up", ChartColor.FromHex("#218A63")));
        chart.AddSeries(new ChartSeries(new ChartSeriesId("window"), "Window", ChartSeriesKind.Timeline, new[] { new ChartPoint(1, 9) }));
        var first = ChartSeries.CreateGanttTask(new ChartSeriesId("first"), "First", 2, 4, .5); chart.AddSeries(first);
        chart.AddSeries(ChartSeries.CreateStateTimelineLane(new ChartSeriesId("state"), "State", new[] { new ChartStateTimelineSegment(2, 8, "up") }));
        chart.AddSeries(ChartSeries.CreateGanttLane(new ChartSeriesId("resource"), "Resource", new[] { new ChartGanttLaneItem(3, 7, "up") }));
        var last = ChartSeries.CreateGanttMilestone(new ChartSeriesId("last"), "Last", 8); chart.AddSeries(last).AddGanttDependency(first, last);
        chart.Options.XAxis.Reversed = chart.Options.YAxis.Reversed = true;
        var prepared = chart.Prepare(DepthContext(chart, showLegend: false)); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        var xml = XDocument.Parse(svg);
        PackageAssertions.Require(xml.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "schedule-composition") == 1,
            "Packed mixed schedule did not use one native schedule owner.");
        foreach (var role in new[] { "timeline-item", "gantt-task", "gantt-milestone", "state-timeline-segment", "gantt-lane-item", "gantt-dependency-arrow" })
            PackageAssertions.Require(xml.Descendants().Any(node => (string?)node.Attribute("data-cfx-role") == role), "Packed mixed schedule lost " + role);
        PackageAssertions.Require(first.GanttItems[0].Start == 2 && first.GanttItems[0].End == 4 && chart.Series[0].Points[0].X == 1,
            "Schedule reversal rewrote raw source endpoints.");
        first.UpdateGanttItem(0, new ChartGanttItem(2, 5, .75));
        PackageAssertions.Require(svg == prepared.ToSvg() && png.SequenceEqual(prepared.ToPng()), "A source schedule edit changed a detached export.");
        PackageAssertions.Png(png);
    }
}
