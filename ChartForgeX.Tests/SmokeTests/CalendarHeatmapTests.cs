using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static XElement[] CalendarRole(XDocument document, string role) => document.Descendants()
        .Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();

    private static void CalendarHeatmapRendersContributionGrid() {
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(760, 360).WithTitle("Consistency Journey")
            .AddCalendarHeatmap("Commits", new[] {
                new ChartCalendarHeatmapItem(new DateTime(2026, 1, 5), 1),
                new ChartCalendarHeatmapItem(new DateTime(2026, 1, 6), 4),
                new ChartCalendarHeatmapItem(new DateTime(2026, 1, 7), 0),
                new ChartCalendarHeatmapItem(new DateTime(2026, 2, 12), 7),
                new ChartCalendarHeatmapItem(new DateTime(2026, 2, 12), 2, ChartColor.FromHex("#22C55E")),
                new ChartCalendarHeatmapItem(new DateTime(2026, 3, 21), 12)
            });
        var document = XDocument.Parse(chart.ToSvg());
        var group = CalendarRole(document, "calendar-heatmap").Single();
        Assert((string?)group.Attribute("data-cfx-start") == "2026-01-04" && (string?)group.Attribute("data-cfx-end") == "2026-03-21",
            "Calendar groups should retain the complete padded week range.");
        Assert((string?)group.Attribute("data-cfx-min") == "0" && (string?)group.Attribute("data-cfx-max") == "12",
            "Calendar groups should retain the source value range.");
        Assert((string?)group.Attribute("data-cfx-value-count") == "4" && (string?)group.Attribute("data-cfx-zero-count") == "1"
            && (string?)group.Attribute("data-cfx-empty-count") == "72", "Calendar coverage should distinguish activity, explicit zero and missing days.");
        Assert(((string?)group.Attribute("aria-label"))?.Contains("Commits", StringComparison.Ordinal) == true,
            "Calendar groups should expose their series to screen readers.");
        var cells = CalendarRole(document, "calendar-cell");
        Assert(cells.Length == 77, "Calendar heatmaps should render every day in the complete padded week range.");
        Assert(cells.All(cell => cell.Attribute("tabindex") == null), "Static calendar days should be named without creating extra tab stops.");
        var duplicate = cells.Single(cell => (string?)cell.Attribute("data-cfx-date") == "2026-02-12");
        Assert((string?)duplicate.Attribute("data-cfx-value") == "9" && (string?)duplicate.Attribute("data-cfx-week-index") == "5"
            && (string?)duplicate.Attribute("data-cfx-weekday-index") == "4", "Duplicate dates should aggregate while retaining their calendar position.");
        Assert(duplicate.Tooltip() == "Commits, 2026-02-12: 9", "Calendar days should retain native SVG hover descriptions.");
        var missing = cells.Single(cell => (string?)cell.Attribute("data-cfx-date") == "2026-01-04");
        Assert((string?)missing.Attribute("data-cfx-empty") == "true" && missing.Attribute("data-cfx-level") == null
            && missing.Tooltip() == "Commits, 2026-01-04: No data", "Missing days should remain distinct from zero activity.");
        var scale = CalendarRole(document, "calendar-scale-step");
        Assert(scale.Any(step => (string?)step.Attribute("data-cfx-zero") == "true"), "The scale should represent explicit zero separately.");
        Assert(scale.Any(step => (string?)step.Attribute("data-cfx-empty") == "false" && (string?)step.Attribute("data-cfx-zero") == "false"
            && (string?)step.Attribute("data-cfx-value") == "1" && (string?)step.Attribute("data-cfx-level") == "1"),
            "Calendar intensity scales should start at the smallest nonzero value.");
        var bottom = scale.Max(step => (double)step.RenderedAttribute("y")! + (double)step.RenderedAttribute("height")!);
        Assert(CalendarRole(document, "calendar-scale-label").All(label => (double)label.RenderedAttribute("y")! > bottom),
            "Calendar scale captions should sit below their swatches.");
        Assert(chart.ToPng().Length > 64, "Calendar heatmaps should render through the native PNG pipeline.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddCalendarHeatmap("Commits", Array.Empty<ChartCalendarHeatmapItem>()), "Calendar heatmaps should reject empty inputs.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartCalendarHeatmapItem(new DateTime(2026, 1, 1), -1), "Calendar heatmap values should reject negatives.");
    }

    private static void CalendarHeatmapDoesNotLabelPaddingMonths() {
        var chart = Chart.Create().WithSize(760, 360).AddCalendarHeatmap("Commits", new[] {
            new ChartCalendarHeatmapItem(new DateTime(2026, 1, 1), 1), new ChartCalendarHeatmapItem(new DateTime(2026, 12, 31), 3)
        });
        var document = XDocument.Parse(chart.ToSvg());
        Assert(CalendarRole(document, "calendar-month").Count(label => label.Value == "Jan") == 1,
            "Calendar heatmaps should not label padded trailing months a second time.");
        Assert(CalendarRole(document, "calendar-cell").Any(cell => (string?)cell.Attribute("data-cfx-date") == "2027-01-01"),
            "Calendar heatmaps should retain padded trailing week cells.");
        Assert(chart.ToPng().Length > 64, "Calendar heatmaps with padded trailing weeks should render PNG output.");
    }

    private static void CalendarHeatmapCompleteWeeksDoNotShowNoDataScale() {
        var start = new DateTime(2026, 1, 4);
        var items = Enumerable.Range(0, 7).Select(index => new ChartCalendarHeatmapItem(start.AddDays(index), index)).ToArray();
        var document = XDocument.Parse(Chart.Create().WithSize(360, 220).AddCalendarHeatmap("Commits", items).ToSvg());
        Assert(CalendarRole(document, "calendar-cell").All(cell => (string?)cell.Attribute("data-cfx-empty") == "false"),
            "Complete weeks should not contain missing-day marks.");
        Assert((string?)CalendarRole(document, "calendar-heatmap").Single().Attribute("data-cfx-empty-count") == "0",
            "Calendar groups should expose zero missing days for complete weeks.");
        Assert(CalendarRole(document, "calendar-scale-step").All(step => (string?)step.Attribute("data-cfx-empty") == "false"),
            "Complete weeks should not reserve a missing-data swatch.");
        Assert(CalendarRole(document, "calendar-scale-label").All(label => label.Value != "No data"),
            "Complete weeks should not show a missing-data caption.");
        Assert(CalendarRole(document, "calendar-scale-step").Any(step => (string?)step.Attribute("data-cfx-value") == "1"
            && (string?)step.Attribute("data-cfx-level") == "1"), "The value ramp should start at the smallest nonzero value.");
    }

    private static void CalendarHeatmapUsesLocalContributionRange() {
        var chart = Chart.Create().WithSize(360, 220).AddCalendarHeatmap("Commits", new[] {
            new ChartCalendarHeatmapItem(new DateTime(2026, 1, 1), 1), new ChartCalendarHeatmapItem(new DateTime(2026, 1, 2), 16)
        }, ChartColor.FromHex("#22C55E"));
        var document = XDocument.Parse(chart.ToSvg());
        var maximum = CalendarRole(document, "calendar-cell").Single(cell => (string?)cell.Attribute("data-cfx-date") == "2026-01-02");
        Assert((string?)maximum.RenderedAttribute("fill") == "#22C55E", "Contribution colors should reach the authored high color at the local maximum.");
        Assert(CalendarRole(document, "calendar-scale-step").Any(step => (string?)step.Attribute("data-cfx-value") == "16"
            && (string?)step.Attribute("data-cfx-level") == "4"), "The scale should retain the local maximum and its intensity.");
        Assert(chart.ToPng().Length > 64, "Calendar local contribution scaling should render PNG output.");
    }
}