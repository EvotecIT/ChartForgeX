using System.Globalization;
using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidGanttAxisFormatTests {
    private static string Fixture(string extension) => Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", "gantt-axis-format." + extension);

    [Theory]
    [InlineData("en-US")]
    [InlineData("pl-PL")]
    public void AuthoredFormatsMatchSharedReferenceLabelsRegardlessOfHostCulture(string culture) {
        var original = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var rendered = MermaidRenderer.Render(File.ReadAllText(Fixture("mmd")));
            Assert.Empty(rendered.Diagnostics);
            var document = Assert.IsType<MermaidGanttDocument>(rendered.Document);
            using var expected = JsonDocument.Parse(File.ReadAllText(Fixture("expected.json")));
            foreach (var contract in expected.RootElement.GetProperty("axisFormats").EnumerateArray()) {
                var task = document.Tasks.Single(value => value.Id == contract.GetProperty("id").GetString());
                foreach (var value in contract.GetProperty("values").EnumerateArray()) {
                    document.AxisFormat = value[0].GetString();
                    var chart = document.ToChart();
                    Assert.Equal(value[1].GetString(), chart.Options.XAxisValueFormatter!(task.Start.ToOADate()));
                }
            }
        } finally { CultureInfo.CurrentCulture = original; }
    }

    [Fact]
    public void DefaultAndAuthoredFormatAreAppliedThroughNativeAndMarkdownRendering() {
        const string source = "gantt\naxisFormat %a %d %b\nFirst :first, 2026-01-04, 4d";
        var rendered = MermaidRenderer.Render(source);
        Assert.Empty(rendered.Diagnostics);
        var svg = rendered.Artifact!.ToSvg();
        Assert.Contains("Sun 04 Jan", svg);
        Assert.NotEmpty(rendered.Artifact!.ToPng());
        var markup = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(markup.Diagnostics);
        Assert.Equal(svg, Assert.Single(markup.Artifacts).ToSvg());
        var defaultChart = new MermaidParser().ParseGantt(source.Replace("axisFormat %a %d %b\n", "")).Document!.ToChart();
        Assert.Equal("2026-01-04", defaultChart.Options.XAxisValueFormatter!(new DateTime(2026, 1, 4).ToOADate()));
    }

    [Theory]
    [InlineData("1970-01-01 00:00:00.000", "0|0|+0000")]
    [InlineData("1969-12-31 23:59:59.999", "-1|-1|+0000")]
    [InlineData("2021-01-01 00:00:00.000", "1609459200000|1609459200|+0000")]
    public void EpochAndZoneDirectivesUseDeterministicUtcWallClockInterpretation(string start, string expected) {
        var chart = new MermaidParser().ParseGantt("gantt\ndateFormat YYYY-MM-DD HH:mm:ss.SSS\naxisFormat %Q|%s|%Z\nTask :task, " + start + ", 1d").Document!.ToChart();
        Assert.Equal(expected, chart.Options.XAxisValueFormatter!(chart.Series[0].Points[0].X));
    }

    [Theory]
    [InlineData(false, 960)]
    [InlineData(true, 320)]
    public async Task FormattedWeekdayLabelsAreVisibleInNativeAndStaticBrowserOutput(bool dark, int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        var document = new MermaidParser().ParseGantt("gantt\naxisFormat %a %d %b\nFirst :first, 2026-01-04, 4d").Document!;
        var chart = document.ToChart(new MermaidGanttRenderOptions { Width = width, Height = 360 })
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight());
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + chart.ToHtmlFragment() + "</body></html>", width, 400);
        var labels = await session.Page.Locator("[data-cfx-role='schedule-tick-label']").AllTextContentsAsync();
        Assert.Contains("Sun 04 Jan", labels);
        Assert.Contains("Thu 08 Jan", labels);
        Assert.Equal(0, await session.Page.Locator("script").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            var name = "gantt-axis-" + (dark ? "dark-" : "light-") + width;
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + ".png"), FullPage = true });
            File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), chart.ToPng());
        }
    }
}
