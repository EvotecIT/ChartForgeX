using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

public static partial class V2GalleryModels {
    private static Chart? Specialty(ChartSeriesKind kind, string variant, VisualThemeMode mode) {
        var chart = Categories(kind); var start = new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc);
        switch (kind) {
            case ChartSeriesKind.Funnel: return chart.AddFunnel("Requests", new[] { new ChartPoint(1, 120), new ChartPoint(2, 95), new ChartPoint(3, 74), new ChartPoint(4, 41) })
                .WithXLabels("Received", "Qualified", "Reviewed", "Completed").WithDataLabels();
            case ChartSeriesKind.Pyramid: return chart.AddPyramid("Allocation", new[] { new ChartPoint(1, 50), new ChartPoint(2, 30), new ChartPoint(3, 20) })
                .WithXLabels("Services", "Platform", "Support").WithDataLabels();
            case ChartSeriesKind.Timeline: return Chart.Create().AddTimelineItem("Discovery", start, start.AddDays(3)).AddTimelineItem("Implementation", start.AddDays(2), start.AddDays(7))
                .AddTimelineItem("Validation", start.AddDays(6), start.AddDays(9))
                .ConfigureXAxis(axis => axis.ValueFormat = ChartValueFormat.Custom(value => DateTime.FromOADate(value).ToString("MMM d", System.Globalization.CultureInfo.InvariantCulture)));
            case ChartSeriesKind.Gantt: return Chart.Create().AddGanttTask("Discovery", start, start.AddDays(3), .9).AddGanttTask("Implementation", start.AddDays(3), start.AddDays(7), .55, 0)
                .AddGanttTask("Validation", start.AddDays(7), start.AddDays(9), .1, 1).AddGanttMilestone("Delivery", start.AddDays(9), 2)
                .ConfigureXAxis(axis => axis.ValueFormat = ChartValueFormat.Custom(value => DateTime.FromOADate(value).ToString("MMM d", System.Globalization.CultureInfo.InvariantCulture)));
            case ChartSeriesKind.Sankey: return FlowRelationships(variant);
            case ChartSeriesKind.Chord: return ChordRelationships(variant);
            case ChartSeriesKind.Tree: return TeamRelationships(variant);
            case ChartSeriesKind.Sunburst: return Sunburst(variant, mode);
            case ChartSeriesKind.Treemap: return Treemap(variant, mode);
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
