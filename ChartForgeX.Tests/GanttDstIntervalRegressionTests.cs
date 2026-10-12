using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GanttDstIntervalRegressionTests {
    [Theory]
    [InlineData(false, 1, "13:00|14:00|14:30|15:30|16:30|17:30|18:30")]
    [InlineData(false, 2, "13:00|15:30|17:30")]
    [InlineData(false, 3, "13:00|16:30")]
    [InlineData(true, 1, "13:30|14:30|15:00|16:00|17:00|18:00|19:00")]
    [InlineData(true, 2, "13:30")]
    [InlineData(true, 3, "13:30|16:00|19:00")]
    public void FractionalDaylightHourRangesMatchPinnedD3Instants(bool spring, int count, string expected) {
        var date = spring ? new DateTime(2026, 10, 3) : new DateTime(2026, 4, 4);
        var start = date.AddHours(13).AddMinutes(spring ? 30 : 0);
        var end = date.AddHours(19);
        var ticks = ChartTimeScale.GenerateFixed(new ChartAxis { TimeZone = FractionalDaylightZone() }, start.ToOADate(), end.ToOADate(),
            new ChartTimeTickInterval(ChartTimeTickUnit.Hour, count));
        Assert.NotNull(ticks);
        Assert.Equal(expected.Split('|'), ticks!.Select(value => DateTime.FromOADate(value).ToString("HH:mm", CultureInfo.InvariantCulture)));
    }

    [Theory]
    [InlineData(ChartTimeTickUnit.Day)]
    [InlineData(ChartTimeTickUnit.Week)]
    [InlineData(ChartTimeTickUnit.Month)]
    public void CalendarBoundariesInAMidnightGapKeepTheFirstValidInstant(ChartTimeTickUnit unit) {
        var axis = new ChartAxis { TimeZone = MidnightGapZone() };
        var start = new DateTime(2026, 9, 1, 4, 0, 0).ToOADate();
        var ticks = ChartTimeScale.GenerateFixed(axis, start, start + .5, new ChartTimeTickInterval(unit, 1, DayOfWeek.Tuesday));
        Assert.Equal(new[] { start }, ticks);
        var spanning = ChartTimeScale.GenerateFixed(axis, start - 1, start + 1.5, new ChartTimeTickInterval(unit, 1, DayOfWeek.Tuesday));
        Assert.Contains(start, spanning!);
        if (unit == ChartTimeTickUnit.Day) Assert.Contains(new DateTime(2026, 9, 2, 3, 0, 0).ToOADate(), spanning!);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ClearingSourceOrImplicitFormattingRestoresNativeLabels(bool authored) {
        const string source = "gantt\ndateFormat YYYY-MM-DD HH:mm\ntickInterval 1hour\n";
        var document = new MermaidParser().ParseGantt(source + (authored ? "axisFormat Source%H\n" : "") + "Window :2026-01-01 00:00, 6h").Document!;
        var native = Chart.Create().WithSize(900, 360).AddGanttTask("Window", new DateTime(2026, 1, 1), new DateTime(2026, 1, 1, 6, 0, 0))
            .WithGanttTickInterval(new ChartTimeTickInterval(ChartTimeTickUnit.Hour));
        Chart? last = null;
        for (var route = 0; route < 4; route++) {
            var chart = document.ToChart(new MermaidGanttRenderOptions { Width = 900, Height = 360 });
            Assert.False(Labels(native).SequenceEqual(Labels(chart)));
            switch (route) {
                case 0: chart.WithXAxisValueFormatter(null); break;
                case 1: chart.Options.XAxisValueFormatter = null; break;
                case 2: chart.Options.XAxis.LabelFormatter = null; break;
                default: chart.Options.XAxis.ValueFormat = null; break;
            }
            Assert.Equal(Labels(native), Labels(chart));
            last = chart;
        }
        var final = last!;
        Assert.NotEmpty(final.ToPng());
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + final.ToSvg() + "</body></html>", 900, 360);
        Assert.Equal(Labels(native), await session.Page.Locator("[data-cfx-role='schedule-tick-label']").AllTextContentsAsync());
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, "gantt-reset-" + authored + "-browser.png") });
        File.WriteAllBytes(Path.Combine(captures, "gantt-reset-" + authored + "-native.png"), final.ToPng());
    }

    private static string[] Labels(Chart chart) => XDocument.Parse(chart.ToSvg()).Descendants()
        .Where(element => (string?)element.Attribute("data-cfx-role") == "schedule-tick-label").Select(element => element.Value).ToArray();

    private static TimeZoneInfo FractionalDaylightZone() {
        var rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), TimeSpan.FromMinutes(30),
            TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 10, 4),
            TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 4, 5));
        return TimeZoneInfo.CreateCustomTimeZone("Portable fractional DST", TimeSpan.FromMinutes(630), "Portable fractional DST", "Standard", "Daylight", new[] { rule });
    }

    private static TimeZoneInfo MidnightGapZone() {
        var rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), TimeSpan.FromHours(1),
            TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 0, 0, 0), 9, 1),
            TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 0, 0, 0), 4, 5));
        return TimeZoneInfo.CreateCustomTimeZone("Portable midnight DST", TimeSpan.FromHours(-4), "Portable midnight DST", "Standard", "Daylight", new[] { rule });
    }
}
