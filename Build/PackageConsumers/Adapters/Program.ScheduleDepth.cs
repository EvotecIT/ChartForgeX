using System;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;

internal static partial class Program {
    private static void MixedScheduleAdapterContract() {
        var chart = Chart.Create().WithSize(700, 460).WithXAxisBounds(0, 10)
            .AddTimelineRange("Window", 1, 9).AddStateTimelineLane("State", new[] { new ChartStateTimelineSegment(2, 8, "up") });
        chart.Options.XAxis.Reversed = chart.Options.YAxis.Reversed = true;
        var html = chart.ToInteractiveHtmlFragment(options => options.Interaction.Enable(ChartInteractionFeatures.Export));
        PackageAssertions.Require(PackageAssertions.Contains(html, "schedule-composition") && PackageAssertions.Contains(html, "state-timeline-segment")
            && PackageAssertions.Contains(html, "timeline-item") && PackageAssertions.Contains(System.Net.WebUtility.HtmlDecode(html), "\"reversed\":true"),
            "Packed schedule adapter lost the common native projection or real typed source targets.");
    }
}
