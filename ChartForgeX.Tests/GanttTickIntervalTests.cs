using System.Globalization;
using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GanttTickIntervalTests {
    private static string Fixture(string extension) => Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", "gantt-tick-interval." + extension);

    [Fact]
    public void NativeCalendarBoundariesMatchSharedD3ReferenceCases() {
        using var expected = JsonDocument.Parse(File.ReadAllText(Fixture("expected.json")));
        foreach (var contract in expected.RootElement.GetProperty("tickCases").EnumerateArray()) {
            var interval = new ChartTimeTickInterval(Enum.Parse<ChartTimeTickUnit>(contract.GetProperty("unit").GetString()!),
                contract.GetProperty("count").GetInt32(), Enum.Parse<DayOfWeek>(contract.GetProperty("weekday").GetString()!));
            var start = DateTime.Parse(contract.GetProperty("start").GetString()!, CultureInfo.InvariantCulture);
            var end = DateTime.Parse(contract.GetProperty("end").GetString()!, CultureInfo.InvariantCulture);
            var ticks = ChartTimeScale.GenerateFixed(new ChartAxis(), start.ToOADate(), end.ToOADate(), interval);
            Assert.NotNull(ticks);
            Assert.Equal(contract.GetProperty("values").EnumerateArray().Select(value => value.GetString()),
                ticks.Select(value => DateTime.FromOADate(value).ToString("yyyy-MM-dd'T'HH:mm:ss.fff", CultureInfo.InvariantCulture)));
        }
    }

    [Fact]
    public void MermaidWeekdayAndIntervalReachNativeSceneAndPortableOutput() {
        var parsed = new MermaidParser().ParseGantt(File.ReadAllText(Fixture("mmd")));
        Assert.Empty(parsed.Diagnostics);
        var document = parsed.Document!;
        Assert.Equal(DayOfWeek.Tuesday, document.Weekday);
        var chart = document.ToChart();
        Assert.Equal(ChartTimeTickUnit.Week, chart.Options.GanttTickInterval!.Unit);
        Assert.Equal(2, chart.Options.GanttTickInterval.Count);
        Assert.Equal(DayOfWeek.Tuesday, chart.Options.GanttTickInterval.WeekStart);
        var svg = chart.ToSvg();
        foreach (var label in new[] { "Tue 13 Jan", "Tue 27 Jan", "Tue 10 Feb", "Tue 24 Feb" }) Assert.Contains(label, svg);
        Assert.NotEmpty(chart.ToPng());
        var json = document.ToVisualArtifact().ToInterchangeJson();
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(json);
        Assert.Equal("tuesday", envelope.Extensions["mermaid.weekday"]);
        Assert.Equal(json, envelope.ToJson());
        var markup = new MermaidVisualMarkupParser().Parse("```mermaid\n" + File.ReadAllText(Fixture("mmd")) + "\n```");
        Assert.Empty(markup.Diagnostics);
        Assert.Equal(svg, Assert.Single(markup.Artifacts).ToSvg());
    }

    [Fact]
    public void EmptyFixedWindowDoesNotInventAutomaticTicksAndExplicitLabelsTakePrecedence() {
        var chart = Chart.Create().WithLegend(false).AddGanttTask("Window", new DateTime(2026, 1, 7), new DateTime(2026, 1, 8))
            .WithGanttTickInterval(new ChartTimeTickInterval(ChartTimeTickUnit.Week, 2, DayOfWeek.Tuesday));
        Assert.DoesNotContain("schedule-tick-label", chart.ToSvg());
        Assert.NotEmpty(chart.ToPng());
        chart.ConfigureXAxis(axis => axis.Labels.Add(new ChartAxisLabel(new DateTime(2026, 1, 7), "Authored")));
        Assert.Contains("Authored", chart.ToSvg());
        chart.WithGanttTickInterval(null);
        Assert.Null(chart.Options.GanttTickInterval);
        var numeric = Chart.Create().AddGanttTask("Numeric", 1, 9).WithGanttTickInterval(new ChartTimeTickInterval(ChartTimeTickUnit.Month));
        Assert.Contains("schedule-tick-label", numeric.ToSvg());
    }

    [Theory]
    [InlineData("0day")]
    [InlineData("2days")]
    [InlineData("2 day")]
    [InlineData("1year")]
    public void ReferenceInvalidIntervalSpellingsRetainTextAndUseAutomaticTicks(string text) {
        var result = new MermaidParser().ParseGantt("gantt\ntickInterval " + text + "\nTask :2026-01-01, 5d");
        Assert.Empty(result.Diagnostics);
        Assert.Equal(text, result.Document!.TickInterval);
        Assert.Null(result.Document.ToChart().Options.GanttTickInterval);
        Assert.Contains("schedule-tick-label", result.Document.ToSvg());
    }

    [Fact]
    public void DenseIntervalsAndUnrepresentableWindowsUseBoundedAutomaticFallback() {
        var interval = new ChartTimeTickInterval(ChartTimeTickUnit.Millisecond);
        var start = new DateTime(2026, 1, 1).ToOADate();
        Assert.Null(ChartTimeScale.GenerateFixed(new ChartAxis(), start, start + 1, interval));
        Assert.Null(ChartTimeScale.GenerateFixed(new ChartAxis(), -1, 1, interval));
        Assert.Empty(ChartTimeScale.GenerateFixed(new ChartAxis(), start, start + 1, new ChartTimeTickInterval(ChartTimeTickUnit.Week, int.MaxValue))!);
        Assert.Contains("schedule-tick-label", Chart.Create().AddGanttTask("Window", new DateTime(2026, 1, 1), new DateTime(2026, 1, 2))
            .WithGanttTickInterval(interval).ToSvg());
    }

    [Fact]
    public void IntervalConstructionAndAuthoredWeekdayRejectInvalidValues() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartTimeTickInterval((ChartTimeTickUnit)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartTimeTickInterval(ChartTimeTickUnit.Day, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartTimeTickInterval(ChartTimeTickUnit.Week, 1, (DayOfWeek)7));
        foreach (var weekday in new[] { "8", "noday" }) Assert.Contains(new MermaidParser().ParseGantt("gantt\nweekday " + weekday + "\nTask :2026-01-01, 1d").Diagnostics, diagnostic => diagnostic.Severity == MermaidDiagnosticSeverity.Error);
    }

    [Fact]
    public void EpochMillisecondsIgnoreDisplayZone() {
        var offsetZone = TimeZoneInfo.CreateCustomTimeZone("TickOffset", TimeSpan.FromMinutes(345), "Tick offset", "Tick offset");
        var start = new DateTime(1970, 1, 1).ToOADate();
        var interval = new ChartTimeTickInterval(ChartTimeTickUnit.Millisecond, 7);
        Assert.Equal(ChartTimeScale.GenerateFixed(new ChartAxis(), start, start + .000001, interval),
            ChartTimeScale.GenerateFixed(new ChartAxis { TimeZone = offsetZone }, start, start + .000001, interval));
    }

    [Fact]
    public void NativeMillisecondLabelsPreservePrecisionAndExplicitFormattingWins() {
        var start = new DateTime(2026, 1, 1);
        var chart = Chart.Create().WithLegend(false).AddGanttTask("Window", start, start.AddMilliseconds(300))
            .WithGanttTickInterval(new ChartTimeTickInterval(ChartTimeTickUnit.Millisecond, 100));
        var svg = chart.ToSvg();
        foreach (var label in new[] { "00:00:00.000", "00:00:00.100", "00:00:00.200", "00:00:00.300" }) Assert.Contains(">" + label + "</text>", svg);
        Assert.NotEmpty(chart.ToPng());
        chart.WithXAxisValueFormatter(value => "Custom " + DateTime.FromOADate(value).Millisecond.ToString(CultureInfo.InvariantCulture));
        Assert.Contains(">Custom 100</text>", chart.ToSvg());
        chart.WithXAxisValueFormatter(null).ConfigureXAxis(axis => axis.Labels.Add(new ChartAxisLabel(start.AddMilliseconds(100), "Authored tick")));
        Assert.Contains(">Authored tick</text>", chart.ToSvg());
    }

    [Theory]
    [InlineData(11, 1, 4)]
    [InlineData(3, 8, 5)]
    public void ClockTicksRetainRealInstantsAcrossRepeatedAndMissingLocalHours(int month, int day, int hour) {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "Eastern Standard Time" : "America/New_York");
        var ticks = ChartTimeScale.GenerateFixed(new ChartAxis { TimeZone = zone }, new DateTime(2026, month, day, hour, 0, 0).ToOADate(),
            new DateTime(2026, month, day, hour + 4, 0, 0).ToOADate(), new ChartTimeTickInterval(ChartTimeTickUnit.Hour));
        Assert.NotNull(ticks);
        Assert.Equal(Enumerable.Range(hour, 5), ticks.Select(value => DateTime.FromOADate(value).Hour));
    }

    [Theory]
    [InlineData(false, 960)]
    [InlineData(true, 320)]
    public async Task WeeklyTicksStayReadableInNativeAndStaticBrowserOutput(bool dark, int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        var document = new MermaidParser().ParseGantt(File.ReadAllText(Fixture("mmd"))).Document!;
        document.AxisFormat = "%m/%d";
        var chart = document.ToChart(new MermaidGanttRenderOptions { Width = width, Height = 360 })
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight()).WithLegend(false);
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + chart.ToHtmlFragment() + "</body></html>", width, 400);
        Assert.Equal(new[] { "01/13", "01/27", "02/10", "02/24" }, await session.Page.Locator("[data-cfx-role='schedule-tick-label']").AllTextContentsAsync());
        Assert.Equal(0, await session.Page.Locator("script").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            var name = "gantt-ticks-" + (dark ? "dark-" : "light-") + width;
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + ".png"), FullPage = true });
            File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), chart.ToPng());
        }
    }
}
