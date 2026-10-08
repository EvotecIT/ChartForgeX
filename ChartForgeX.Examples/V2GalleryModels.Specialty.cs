using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

public static partial class V2GalleryModels {
    private static Chart? Specialty(ChartSeriesKind kind, string variant, VisualThemeMode mode) {
        var chart = Categories(); var start = new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc);
        var links = new[] { new ChartTreeLink("All teams", "Engineering", 60), new ChartTreeLink("All teams", "Operations", 40),
            new ChartTreeLink("Engineering", "Platform", 35), new ChartTreeLink("Engineering", "Services", 25), new ChartTreeLink("Operations", "Support", 40) };
        switch (kind) {
            case ChartSeriesKind.Funnel: return chart.AddFunnel("Requests", new[] { new ChartPoint(1, 120), new ChartPoint(2, 95), new ChartPoint(3, 74), new ChartPoint(4, 41) }).WithXLabels("Received", "Qualified", "Reviewed", "Completed");
            case ChartSeriesKind.Timeline: return Chart.Create().AddTimelineItem("Discovery", start, start.AddDays(3)).AddTimelineItem("Implementation", start.AddDays(2), start.AddDays(7)).AddTimelineItem("Validation", start.AddDays(6), start.AddDays(9));
            case ChartSeriesKind.Gantt: return Chart.Create().AddGanttTask("Discovery", start, start.AddDays(3), .9).AddGanttTask("Implementation", start.AddDays(3), start.AddDays(7), .55, 0)
                .AddGanttTask("Validation", start.AddDays(7), start.AddDays(9), .1, 1).AddGanttMilestone("Delivery", start.AddDays(9), 2);
            case ChartSeriesKind.Sankey: return Chart.Create().AddSankey("Requests", new[] { new ChartSankeyLink("Received", "Automatic", 72), new ChartSankeyLink("Received", "Manual", 28),
                new ChartSankeyLink("Automatic", "Completed", 65), new ChartSankeyLink("Automatic", "Review", 7), new ChartSankeyLink("Manual", "Completed", 20), new ChartSankeyLink("Manual", "Review", 8) });
            case ChartSeriesKind.Tree: return Chart.Create().AddTree("Teams", links);
            case ChartSeriesKind.Sunburst: return Chart.Create().AddSunburst("Teams", links);
            case ChartSeriesKind.Treemap: return Chart.Create().AddTreemap("Allocation", new[] { new ChartTreemapItem("Platform", 35), new ChartTreemapItem("Services", 25), new ChartTreemapItem("Support", 25), new ChartTreemapItem("Research", 15) });
            case ChartSeriesKind.Pictorial:
                chart = Chart.Create().AddPictorial("Assessments", new[] { new ChartPictorialItem("Team A", 74), new ChartPictorialItem("Team B", 46), new ChartPictorialItem("Team C", 61) }, ChartPictorialShape.Person)
                    .WithPictorialMaximum(100).WithPictorialValuePerSymbol(10).WithPictorialColumns(10);
                if (variant == "options") chart.WithPictorialSvgPath("M2 2H22V22H2Z M8 8V16H16V8Z", new ChartRect(0, 0, 24, 24));
                return chart;
            case ChartSeriesKind.ProgressBar:
                chart = Chart.Create().AddProgressBars("Delivery", new[] { new ChartProgressItem("Reviewed", 76), new ChartProgressItem("Verified", 58), new ChartProgressItem("Complete", 42) }).WithProgressHandles();
                if (variant == "options") chart.WithProgressMaximum(120).WithProgressBarThickness(.42).WithProgressTrackOpacity(.3);
                return chart;
            case ChartSeriesKind.WordCloud: return Chart.Create().AddWordCloud("Topics", new[] { new ChartWordCloudItem("Availability", 20), new ChartWordCloudItem("Identity", 16), new ChartWordCloudItem("Network", 14),
                new ChartWordCloudItem("Storage", 12), new ChartWordCloudItem("Recovery", 10), new ChartWordCloudItem("Capacity", 8), new ChartWordCloudItem("Services", 6), new ChartWordCloudItem("Validation", 4) });
            case ChartSeriesKind.StateTimeline: return States(mode).AddStateTimelineLane("Primary", new[] { new ChartStateTimelineSegment(start, start.AddHours(3), "pass"),
                new ChartStateTimelineSegment(start.AddHours(3), start.AddHours(5), "warning", "Increased response time"), new ChartStateTimelineSegment(start.AddHours(6), start.AddHours(10), "pass") }, group: "Production")
                .AddStateTimelineLane("Secondary", new[] { new ChartStateTimelineSegment(start, start.AddHours(6), "pass"), new ChartStateTimelineSegment(start.AddHours(7), start.AddHours(10), "critical", "No response") }, group: "Production");
            case ChartSeriesKind.GanttLane: return States(mode).WithGanttLaneNow(start.AddHours(12)).AddGanttLane("Primary", new[] {
                new ChartGanttLaneItem(start, start.AddHours(5), "warning", "Investigation", "Degraded response time"), new ChartGanttLaneItem(start.AddHours(3), start.AddHours(7), "critical", "Recovery"),
                new ChartGanttLaneItem(start.AddHours(9), null, "warning", "Monitoring") }, "Production")
                .AddGanttLane("Secondary", new[] { new ChartGanttLaneItem(start.AddHours(2), start.AddHours(6), "warning", "Maintenance") }, "Production");
            default: return null;
        }
    }
}
