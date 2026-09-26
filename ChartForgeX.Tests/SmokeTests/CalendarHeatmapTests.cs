using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void CalendarHeatmapRendersContributionGrid() {
        var chart = Chart.Create()
            .WithSize(760, 360)
            .WithTitle("Consistency Journey")
            .AddCalendarHeatmap("Commits", new[] {
                new ChartCalendarHeatmapItem(new DateTime(2026, 1, 5), 1),
                new ChartCalendarHeatmapItem(new DateTime(2026, 1, 6), 4),
                new ChartCalendarHeatmapItem(new DateTime(2026, 1, 7), 0),
                new ChartCalendarHeatmapItem(new DateTime(2026, 2, 12), 7),
                new ChartCalendarHeatmapItem(new DateTime(2026, 2, 12), 2, ChartColor.FromHex("#22C55E")),
                new ChartCalendarHeatmapItem(new DateTime(2026, 3, 21), 12)
            });

        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-role=\"calendar-heatmap\"", StringComparison.Ordinal), "Calendar heatmaps should expose a role marker.");
        Assert(svg.Contains("data-cfx-role=\"calendar-heatmap\" data-cfx-label=\"Commits\" data-cfx-start-date=\"2026-01-04\" data-cfx-end-date=\"2026-03-21\" data-cfx-day-count=\"77\" data-cfx-filled-day-count=\"5\" data-cfx-empty-day-count=\"72\"", StringComparison.Ordinal), "Calendar heatmap containers should expose label, date range, and coverage metadata.");
        Assert(svg.Contains("data-cfx-min-value=\"0\" data-cfx-max-value=\"12\"", StringComparison.Ordinal), "Calendar heatmap containers should expose the source value range.");
        Assert(svg.Contains("Consistency Journey calendar heatmap for Commits from 2026-01-05 to 2026-03-21 with 5 dated values.", StringComparison.Ordinal), "Calendar heatmap SVG descriptions should summarize the specialized chart shape.");
        Assert(svg.Contains("role=\"group\" aria-label=\"Commits calendar heatmap from 2026-01-04 to 2026-03-21 with 5 filled days and 72 empty days\"", StringComparison.Ordinal), "Calendar heatmap containers should expose a useful group label.");
        Assert(!svg.Contains("data-cfx-role=\"legend\"", StringComparison.Ordinal), "Calendar heatmaps should not emit generic series legends.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"calendar-heatmap-cell\"") >= 70, "Calendar heatmaps should render a continuous day grid across the date range.");
        Assert(svg.Contains("data-cfx-date=\"2026-01-05\"", StringComparison.Ordinal), "Calendar heatmap cells should expose ISO dates.");
        Assert(svg.Contains("data-cfx-date=\"2026-01-05\" data-cfx-week-index=\"0\" data-cfx-weekday-index=\"1\"", StringComparison.Ordinal), "Calendar heatmap cells should expose computed week and weekday indexes.");
        Assert(svg.Contains("data-cfx-date=\"2026-02-12\" data-cfx-week-index=\"5\" data-cfx-weekday-index=\"4\" data-cfx-value=\"9\"", StringComparison.Ordinal), "Duplicate calendar heatmap dates should aggregate into one day cell while preserving grid metadata.");
        Assert(svg.Contains("<title>Commits, 2026-02-12: 9</title>", StringComparison.Ordinal), "Calendar heatmap cells should expose native SVG hover titles.");
        Assert(svg.Contains("class=\"cfx-interactive-region\" tabindex=\"0\" focusable=\"true\" data-cfx-role=\"calendar-heatmap-cell\"", StringComparison.Ordinal), "Calendar heatmap cells should be keyboard-focusable interactive SVG regions.");
        Assert(svg.Contains("data-cfx-empty=\"true\"", StringComparison.Ordinal), "Calendar heatmaps should mark empty days separately from explicit zero values.");
        Assert(svg.Contains("data-cfx-date=\"2026-01-04\" data-cfx-week-index=\"0\" data-cfx-weekday-index=\"0\" data-cfx-value=\"0\" data-cfx-level=\"0\" data-cfx-empty=\"true\" data-cfx-status=\"empty\"", StringComparison.Ordinal), "Calendar heatmap cells without data should expose empty status metadata and grid coordinates.");
        Assert(svg.Contains("<title>Commits, 2026-01-04: No data</title>", StringComparison.Ordinal), "Calendar heatmap empty cells should describe missing data in native SVG hover titles.");
        Assert(svg.Contains("data-cfx-date=\"2026-01-07\" data-cfx-week-index=\"0\" data-cfx-weekday-index=\"3\" data-cfx-value=\"0\" data-cfx-level=\"0\" data-cfx-empty=\"false\" role=", StringComparison.Ordinal), "Calendar heatmap explicit zero values should remain real low-value cells with grid coordinates and no status.");
        Assert(svg.Contains("data-cfx-role=\"calendar-heatmap-weekday-label\"", StringComparison.Ordinal), "Calendar heatmaps should render weekday labels.");
        Assert(svg.Contains("data-cfx-role=\"calendar-heatmap-month-label\"", StringComparison.Ordinal), "Calendar heatmaps should render month labels.");
        Assert(svg.Contains("data-cfx-role=\"calendar-heatmap-scale-step\"", StringComparison.Ordinal), "Calendar heatmaps should render a contribution scale.");
        Assert(svg.Contains("data-cfx-role=\"calendar-heatmap-scale-no-data\" data-cfx-status=\"empty\"", StringComparison.Ordinal), "Calendar heatmap scales should explain missing days separately from the value scale.");
        Assert(svg.Contains("data-cfx-role=\"calendar-heatmap-scale-step\" data-cfx-level=\"0\" data-cfx-value=\"0\" x=", StringComparison.Ordinal), "Calendar heatmap scale steps should expose the low value as data, not as no-data.");
        Assert(svg.Contains("data-cfx-date=\"2026-01-04\"", StringComparison.Ordinal) && svg.Contains("fill=\"#CCD2DA\"", StringComparison.Ordinal), "Calendar heatmap empty cells should use a visible neutral fill on light themes.");
        var lessLabelX = GetAttribute(svg, "data-cfx-role=\"calendar-heatmap-scale-label\"", "x");
        var noDataX = GetAttribute(svg, "data-cfx-role=\"calendar-heatmap-scale-no-data\"", "x");
        var noDataWidth = GetAttribute(svg, "data-cfx-role=\"calendar-heatmap-scale-no-data\"", "width");
        var scaleX = GetAttribute(svg, "data-cfx-role=\"calendar-heatmap-scale-step\"", "x");
        Assert(lessLabelX < noDataX, "Calendar heatmap no-data scale swatches should not overlap the Less label.");
        Assert(noDataX + noDataWidth < scaleX, "Calendar heatmap no-data scale swatches should not overlap the value scale start.");
        Assert(chart.ToPng().Length > 64, "Calendar heatmaps should render PNG output.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddCalendarHeatmap("Empty", Array.Empty<ChartCalendarHeatmapItem>()), "Calendar heatmaps should reject empty inputs.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartCalendarHeatmapItem(new DateTime(2026, 1, 1), -1), "Calendar heatmap values should reject negatives.");
    }

    private static void CalendarHeatmapDoesNotLabelPaddingMonths() {
        var svg = Chart.Create()
            .WithSize(760, 360)
            .AddCalendarHeatmap("Commits", new[] {
                new ChartCalendarHeatmapItem(new DateTime(2026, 1, 1), 1),
                new ChartCalendarHeatmapItem(new DateTime(2026, 12, 31), 3)
            })
            .ToSvg();

        Assert(CountOccurrences(svg, ">Jan</text>") == 1, "Calendar heatmaps should not add month labels for padded trailing weeks.");
        Assert(svg.Contains("data-cfx-date=\"2027-01-01\"", StringComparison.Ordinal), "Calendar heatmaps should still render padded trailing week cells.");
        Assert(Chart.Create().AddCalendarHeatmap("Commits", new[] { new ChartCalendarHeatmapItem(new DateTime(2026, 12, 31), 3) }).ToPng().Length > 64, "Calendar heatmaps with padded trailing weeks should render PNG output.");
    }

    private static void CalendarHeatmapCompleteWeeksDoNotShowNoDataScale() {
        var start = new DateTime(2026, 1, 4);
        var items = new ChartCalendarHeatmapItem[7];
        for (var i = 0; i < items.Length; i++) items[i] = new ChartCalendarHeatmapItem(start.AddDays(i), i);

        var svg = Chart.Create()
            .WithSize(360, 220)
            .AddCalendarHeatmap("Commits", items)
            .ToSvg();

        Assert(!svg.Contains("data-cfx-empty=\"true\"", StringComparison.Ordinal), "Calendar heatmaps with complete week data should not mark any cell empty.");
        Assert(svg.Contains("data-cfx-filled-day-count=\"7\" data-cfx-empty-day-count=\"0\"", StringComparison.Ordinal), "Complete calendar heatmaps should expose zero empty days at container level.");
        Assert(svg.Contains("data-cfx-min-value=\"0\" data-cfx-max-value=\"6\"", StringComparison.Ordinal), "Complete calendar heatmaps should expose the true source value range.");
        Assert(!svg.Contains("data-cfx-role=\"calendar-heatmap-scale-no-data\"", StringComparison.Ordinal), "Complete calendar heatmaps should not reserve a missing-data scale swatch.");
        Assert(svg.Contains("data-cfx-role=\"calendar-heatmap-scale-step\" data-cfx-level=\"0\" data-cfx-value=\"0\" x=", StringComparison.Ordinal), "Complete calendar heatmap value scales should start with the real low value.");
    }

    private static void CalendarHeatmapUsesLocalContributionRange() {
        var chart = Chart.Create()
            .WithSize(360, 220)
            .AddCalendarHeatmap("Commits", new[] {
                new ChartCalendarHeatmapItem(new DateTime(2026, 1, 1), 1),
                new ChartCalendarHeatmapItem(new DateTime(2026, 1, 2), 16)
            }, ChartColor.FromHex("#22C55E"));

        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-date=\"2026-01-02\"", StringComparison.Ordinal) && svg.Contains("fill=\"#22C55E\"", StringComparison.Ordinal), "Calendar heatmaps should scale contribution colors to their local data range instead of treating small counts as percentages.");
        Assert(svg.Contains("data-cfx-role=\"calendar-heatmap-scale-step\" data-cfx-level=\"4\" data-cfx-value=\"16\" x=", StringComparison.Ordinal), "Calendar heatmap scales should mark the local maximum as the high-intensity step.");
        Assert(chart.ToPng().Length > 64, "Calendar heatmap local contribution scaling should render PNG output.");
    }
}
