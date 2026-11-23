using System.Globalization;
using ChartForgeX;
using ChartForgeX.Core;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TimelineAndGanttHonorExplicitXAxisBounds() {
        var timeline = Chart.Create()
            .Size(640, 360)
            .XAxis(x => x.Bounds(0, 10))
            .TickCount(6)
            .XAxis(x => x.LabelBy(value => "T" + value.ToString("0", CultureInfo.InvariantCulture)))
            .AddTimelineRange("Rollout", 2, 4);
        var timelineSvg = timeline.ToSvg();
        Assert(timelineSvg.Contains(">T0</text>", System.StringComparison.Ordinal), "Timeline x-axis bounds should drive the first visible tick.");
        Assert(timelineSvg.Contains(">T10</text>", System.StringComparison.Ordinal), "Timeline x-axis bounds should drive the last visible tick.");
        Assert(timeline.ToPng().Length > 64, "Timeline explicit bounds should render valid PNG output.");

        var gantt = Chart.Create()
            .Size(640, 360)
            .XAxis(x => x.Bounds(0, 10))
            .TickCount(6)
            .XAxis(x => x.LabelBy(value => "W" + value.ToString("0", CultureInfo.InvariantCulture)))
            .Gantt(g => g.Today(12))
            .AddGanttTask("Task", 2, 4, 0.5);
        var ganttSvg = gantt.ToSvg();
        Assert(ganttSvg.Contains(">W0</text>", System.StringComparison.Ordinal), "Gantt x-axis bounds should drive the first visible tick.");
        Assert(ganttSvg.Contains(">W10</text>", System.StringComparison.Ordinal), "Gantt x-axis bounds should drive the last visible tick.");
        Assert(!ganttSvg.Contains("data-cfx-role=\"gantt-today\"", System.StringComparison.Ordinal), "Gantt current markers outside explicit x-axis bounds should not render.");
        Assert(gantt.ToPng().Length > 64, "Gantt explicit bounds should render valid PNG output.");
    }
}
