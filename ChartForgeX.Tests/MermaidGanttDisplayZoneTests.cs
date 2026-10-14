using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidGanttDisplayZoneTests {
    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(300, "2026-01-02", "2026-01-03")]
    [InlineData(-300, "2026-01-01", "2026-01-02")]
    [InlineData(345, "2026-01-02", "2026-01-03")]
    public async Task DefaultLabelsUseTheZoneThatAlignedTheTicks(int minutes, params string[] expected) {
        var chart = new MermaidParser().ParseGantt("gantt\ntickInterval 1day\nTask :2026-01-01, 3d").Document!
            .ToChart(new MermaidGanttRenderOptions { Width = 900, Height = 360 });
        chart.WithXAxisTimeScale(Zone(minutes)).ConfigureXAxis(axis => {
            axis.Minimum = new DateTime(2026, 1, 1).ToOADate();
            axis.Maximum = new DateTime(2026, 1, 3).ToOADate();
        });
        Assert.Equal(expected, Labels(chart));
        var png = chart.ToPng();
        Assert.NotEmpty(png);
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + chart.ToHtmlFragment() + "</body></html>", 900, 360);
        Assert.Equal(expected, await session.Page.Locator("[data-cfx-role='schedule-tick-label']").AllTextContentsAsync());
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var name = "gantt-display-zone-" + minutes;
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png") });
        File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), png);
    }

    [Theory]
    [InlineData(300, "2026-01-02 00:00 +0500|1/2/2026, 12:00:00 AM")]
    [InlineData(-300, "2026-01-01 14:00 -0500|1/1/2026, 2:00:00 PM")]
    [InlineData(345, "2026-01-02 00:45 +0545|1/2/2026, 12:45:00 AM")]
    public void AuthoredFieldsAndCompositeFormatsFollowHostZoneChanges(int minutes, string expected) {
        var chart = new MermaidParser().ParseGantt("gantt\naxisFormat %Y-%m-%d %H:%M %Z|%c\nTask :2026-01-01, 3d").Document!.ToChart();
        var instant = new DateTime(2026, 1, 1, 19, 0, 0).ToOADate();
        var original = chart.Options.XAxisValueFormatter!(instant);
        chart.WithXAxisTimeScale(Zone(minutes));
        Assert.Equal(expected, chart.Options.XAxisValueFormatter!(instant));
        chart.WithXAxisTimeScale(TimeZoneInfo.Utc);
        Assert.Equal(original, chart.Options.XAxisValueFormatter!(instant));
        chart.WithXAxisValueFormatter(_ => "Host caption");
        Assert.All(Labels(chart), value => Assert.Equal("Host caption", value));
    }

    [Fact]
    public void EpochDirectivesKeepTheInstantWhileDaylightSavingChangesWallClockAndOffset() {
        var daylight = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(2020, 1, 1), new DateTime(2030, 12, 31),
            TimeSpan.FromHours(1),
            TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 2, DayOfWeek.Sunday),
            TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 11, 1, DayOfWeek.Sunday));
        var zone = TimeZoneInfo.CreateCustomTimeZone("Gantt daylight", TimeSpan.FromHours(-5), "Gantt daylight", "Standard", "Daylight", new[] { daylight });
        var chart = new MermaidParser().ParseGantt("gantt\naxisFormat %Y-%m-%d %H:%M %Z|%s|%Q\nTask :2026-03-08, 2d").Document!.ToChart();
        chart.WithXAxisTimeScale(zone);
        foreach (var hour in new[] { 6, 7 }) {
            var instant = new DateTimeOffset(2026, 3, 8, hour, 0, 0, TimeSpan.Zero);
            var local = hour == 6 ? "2026-03-08 01:00 -0500" : "2026-03-08 03:00 -0400";
            Assert.Equal(local + "|" + instant.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture) + "|" +
                instant.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture), chart.Options.XAxisValueFormatter!(instant.UtcDateTime.ToOADate()));
        }
    }

    private static TimeZoneInfo Zone(int minutes) => TimeZoneInfo.CreateCustomTimeZone("Gantt " + minutes,
        TimeSpan.FromMinutes(minutes), "Gantt display", "Gantt display");

    private static string[] Labels(Chart chart) => XDocument.Parse(chart.ToSvg()).Descendants()
        .Where(element => (string?)element.Attribute("data-cfx-role") == "schedule-tick-label").Select(element => element.Value).ToArray();
}
