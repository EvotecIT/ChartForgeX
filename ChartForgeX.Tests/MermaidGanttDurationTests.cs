using System.Globalization;
using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidGanttDurationTests {
    [Theory]
    [InlineData(false, 960)]
    [InlineData(true, 960)]
    [InlineData(false, 320)]
    [InlineData(true, 320)]
    public async Task CalendarAndMinuteBarsRetainTheirDifferentDurationsInStaticHosts(bool dark, int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        var result = new MermaidParser().ParseGantt("gantt\ndateFormat YYYY-MM-DD\nMonth :month, 2026-01-31, 1M\nMinute :minute, 2026-01-31, 1m");
        Assert.False(result.HasErrors);
        var chart = result.Document!.ToChart().WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight());
        var html = "<!doctype html><html><body style=\"margin:0\">" + chart.ToHtmlFragment() + "</body></html>";
        await using var session = await InteractiveChartBrowser.OpenAsync(html, width, 600);
        var bars = session.Page.Locator("[data-cfx-role=\"gantt-task-shape\"]");
        Assert.Equal(2, await bars.CountAsync());
        var month = (await bars.Nth(0).BoundingBoxAsync())!;
        var minute = (await bars.Nth(1).BoundingBoxAsync())!;
        Assert.True(month.Width > minute.Width * 100, "A calendar month must render longer than a minute.");
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        Assert.Equal(0, await session.Page.Locator("script").CountAsync());
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions {
                Path = Path.Combine(captures, "gantt-month-minute-" + (dark ? "dark-" : "light-") + width + ".png"), FullPage = true
            });
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [Fact]
    public void DurationTimestampsMatchTheUpstreamConformanceFixture() {
        var fixtures = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures");
        var result = new MermaidParser().ParseGantt(File.ReadAllText(Path.Combine(fixtures, "gantt-duration-units.mmd")));
        Assert.False(result.HasErrors, string.Join("; ", result.Diagnostics.Select(item => item.Message)));
        var document = Assert.IsType<MermaidGanttDocument>(result.Document);
        using var expected = JsonDocument.Parse(File.ReadAllText(Path.Combine(fixtures, "gantt-duration-units.expected.json")));
        var timestamps = expected.RootElement.GetProperty("taskTimestamps").EnumerateArray().ToArray();
        Assert.Equal(timestamps.Length, document.Tasks.Count);
        for (var index = 0; index < timestamps.Length; index++) {
            Assert.Equal(timestamps[index][0].GetString(), document.Tasks[index].Id);
            Assert.Equal(timestamps[index][1].GetString(), document.Tasks[index].Start.ToString("yyyy-MM-dd'T'HH:mm:ss.fff", CultureInfo.InvariantCulture));
            Assert.Equal(timestamps[index][2].GetString(), document.Tasks[index].End.ToString("yyyy-MM-dd'T'HH:mm:ss.fff", CultureInfo.InvariantCulture));
        }
    }

    [Fact]
    public void CalendarDurationStillAppliesExclusionsAndFeedsAfterDependencies() {
        var result = new MermaidParser().ParseGantt("gantt\nexcludes weekends\nMonth :month, 2026-01-31, 1M\nMinute :minute, after month, 1m");
        Assert.False(result.HasErrors);
        var tasks = result.Document!.Tasks;
        Assert.Equal(new DateTime(2026, 3, 11), tasks[0].End);
        Assert.Equal(tasks[0].End, tasks[1].Start);
        Assert.Equal(tasks[0].End.AddMinutes(1), tasks[1].End);
        Assert.Equal(new[] { "month" }, tasks[1].DependencyIds);
    }

    [Theory]
    [InlineData("1month")]
    [InlineData("1months")]
    [InlineData("1year")]
    [InlineData("1years")]
    [InlineData("1Y")]
    [InlineData("1 M")]
    [InlineData("1 y")]
    [InlineData("1\tM")]
    [InlineData("1\ty")]
    [InlineData(".5M")]
    [InlineData("1.M")]
    [InlineData(".5y")]
    [InlineData("1.y")]
    public void UnsupportedCalendarUnitNamesProduceSourceDiagnostics(string duration) {
        var result = new MermaidParser().ParseGantt("gantt\nTask :task, 2026-01-31, " + duration);
        Assert.True(result.HasErrors);
        Assert.Empty(result.Document!.Tasks);
        Assert.Contains(result.Diagnostics, item => item.Span.Line == 2 && item.Severity == MermaidDiagnosticSeverity.Error);
    }

    [Theory]
    [InlineData("9999-10-31", "2M", "9999-12-31T00:00:00.000")]
    [InlineData("9999-12-30", "1d", "9999-12-31T00:00:00.000")]
    [InlineData("9999-12-31", "1h", "9999-12-31T01:00:00.000")]
    [InlineData("9999-12-31", "1ms", "9999-12-31T00:00:00.001")]
    public void ValidUpperRangeDatesDoNotOverflowTheExclusionCursor(string start, string duration, string expectedEnd) {
        var result = new MermaidParser().ParseGantt("gantt\nexcludes 0001-01-01\nTask :task, " + start + ", " + duration);
        Assert.False(result.HasErrors);
        Assert.Equal(expectedEnd, Assert.Single(result.Document!.Tasks).End.ToString("yyyy-MM-dd'T'HH:mm:ss.fff", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ExcludingTheLastSupportedDateStillReportsAnUnreachableEnd() {
        var result = new MermaidParser().ParseGantt("gantt\nexcludes 9999-12-31\nTask :task, 9999-12-30, 1d");
        Assert.True(result.HasErrors);
        Assert.Contains(result.Diagnostics, item => item.Span.Line == 3 && item.Severity == MermaidDiagnosticSeverity.Error);
    }

    [Theory]
    [InlineData("9999-12-31", "1M")]
    [InlineData("9999-12-31", "1y")]
    [InlineData("2026-01-01", "99999999999999999999999999999999999999M")]
    [InlineData("2026-01-01", "99999999999999999999999999999999999999d")]
    public void OutOfRangeDurationsProduceSourceDiagnostics(string start, string duration) {
        var result = new MermaidParser().ParseGantt("gantt\nTask :task, " + start + ", " + duration);
        Assert.True(result.HasErrors);
        Assert.Contains(result.Diagnostics, item => item.Span.Line == 2 && item.Severity == MermaidDiagnosticSeverity.Error);
    }
}
