using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidGanttDateAnchorTests {
    private static readonly DateTime Anchor = new(2028, 2, 20, 17, 45, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("Do MMMM", "29th February", "2028-02-29T00:00:00")]
    [InlineData("DDD", "60", "2028-02-29T00:00:00")]
    [InlineData("[Q]Q", "Q3", "2028-07-01T00:00:00")]
    [InlineData("hh:mm A", "01:25 PM", "2028-02-20T13:25:00")]
    [InlineData("MM-dd", "02-29", "2028-02-29T00:00:00")]
    [InlineData("%m/%d", "02/29", "2028-02-29T00:00:00")]
    [InlineData("'yyyy' MM-dd", "yyyy 02-29", "2028-02-29T00:00:00")]
    public void MissingFieldsUseExplicitCalendarContextAndAuthoredYearsRemainIndependent(string format, string input, string expected) {
        var parsed = new MermaidParser().ParseGantt("gantt\ndateFormat " + format + "\nTask :" + input + ",1h",
            new MermaidParseOptions { GanttDateAnchor = Anchor });
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(expected, Assert.Single(parsed.Document!.Tasks).Start.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture));
        Assert.Equal(DateTimeKind.Unspecified, parsed.Document.DateAnchor!.Value.Kind);
        Assert.Equal(Anchor.Date, parsed.Document.DateAnchor);
    }

    [Theory]
    [MemberData(nameof(ReferenceDates))]
    public void PartialStartAndEndFieldsMatchThePinnedReferenceCalendar(string format, string start, string end, string expectedStart, string expectedEnd) {
        var parsed = new MermaidParser().ParseGantt("gantt\ndateFormat " + format + "\nTask :task," + start + "," + end,
            new MermaidParseOptions { GanttDateAnchor = Anchor });
        Assert.Empty(parsed.Diagnostics);
        var task = Assert.Single(parsed.Document!.Tasks);
        Assert.Equal(expectedStart, task.Start.ToString("yyyy-MM-dd'T'HH:mm:ss.fff", CultureInfo.InvariantCulture));
        Assert.Equal(expectedEnd, task.End.ToString("yyyy-MM-dd'T'HH:mm:ss.fff", CultureInfo.InvariantCulture));
    }

    public static IEnumerable<object[]> ReferenceDates() {
        using var expected = JsonDocument.Parse(File.ReadAllText(Fixture("gantt-date-anchor.expected.json")));
        return expected.RootElement.GetProperty("ganttInputDates").EnumerateArray()
            .Select(values => values.EnumerateArray().Select(value => (object)value.GetString()!).ToArray()).ToArray();
    }

    [Fact]
    public void LeapYearDependencyFixtureMatchesTheReferenceClockWithoutReadingTheRuntimeClock() {
        using var expected = JsonDocument.Parse(File.ReadAllText(Fixture("gantt-date-anchor.expected.json")));
        var parsed = new MermaidParser().ParseGantt(File.ReadAllText(Fixture("gantt-date-anchor.mmd")),
            new MermaidParseOptions { GanttDateAnchor = DateTime.ParseExact(expected.RootElement.GetProperty("dateAnchor").GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture) });
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(expected.RootElement.GetProperty("taskTimestamps").EnumerateArray().Select(task => (task[0].GetString(), task[1].GetString(), task[2].GetString())),
            parsed.Document!.Tasks.Select(task => ((string?)task.Id, (string?)task.Start.ToString("yyyy-MM-dd'T'HH:mm:ss.fff", CultureInfo.InvariantCulture), (string?)task.End.ToString("yyyy-MM-dd'T'HH:mm:ss.fff", CultureInfo.InvariantCulture))));
        var renderedTasks = XDocument.Parse(parsed.Document.ToSvg()).Descendants()
            .Where(element => (string?)element.Attribute("data-cfx-role") == "gantt-task").ToArray();
        var timestamps = expected.RootElement.GetProperty("taskTimestamps").EnumerateArray().ToArray();
        Assert.Equal(timestamps.Length, renderedTasks.Length);
        for (var index = 0; index < timestamps.Length; index++) {
            var start = DateTime.ParseExact(timestamps[index][1].GetString()!, "yyyy-MM-dd'T'HH:mm:ss.fff", CultureInfo.InvariantCulture);
            var end = DateTime.ParseExact(timestamps[index][2].GetString()!, "yyyy-MM-dd'T'HH:mm:ss.fff", CultureInfo.InvariantCulture);
            Assert.Equal(start.ToOADate(), (double)renderedTasks[index].Attribute("data-cfx-start")!, 6);
            Assert.Equal(end.ToOADate(), (double)renderedTasks[index].Attribute("data-cfx-end")!, 6);
        }
    }

    private static string Fixture(string name) => Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", name);

    [Theory]
    [InlineData(false, 960)]
    [InlineData(true, 320)]
    public async Task AnchoredLeapYearScheduleKeepsItsDatesInNativeAndCompactHtml(bool dark, int width) {
        var rendered = MermaidRenderer.Render(File.ReadAllText(Fixture("gantt-date-anchor.mmd")),
            new MermaidRenderOptions { ParseOptions = new MermaidParseOptions { GanttDateAnchor = Anchor } });
        Assert.Empty(rendered.Diagnostics);
        var chart = Assert.IsType<MermaidGanttDocument>(rendered.Document).ToChart(new MermaidGanttRenderOptions { Width = width, Height = 360 })
            .WithTheme(dark ? ChartForgeX.Themes.ChartTheme.GraphiteDark() : ChartForgeX.Themes.ChartTheme.GraphiteLight());
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        var name = "gantt-date-anchor-" + (dark ? "dark-" : "light-") + width;
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), chart.ToPng());
        }
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + chart.ToHtmlFragment() + "</body></html>", width, 400);
        Assert.Equal(2, await session.Page.Locator("[data-cfx-role='gantt-task']").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        if (!string.IsNullOrWhiteSpace(captures)) await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + ".png"), FullPage = true });
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData("MM-DD", "02-29")]
    [InlineData("HH:mm", "13:25")]
    [InlineData("Do MMMM", "29th February")]
    [InlineData("DDD", "60")]
    [InlineData("MM-dd", "02-29")]
    [InlineData("%m/%d", "02/29")]
    [InlineData("'yyyy' MM-dd", "yyyy 02-29")]
    public void MissingAnchorProducesLocatedErrorInsteadOfChoosingTheCurrentYear(string format, string input) {
        var rendered = MermaidRenderer.Render("gantt\ndateFormat " + format + "\nTask :" + input + ",1h");
        Assert.Null(rendered.Artifact);
        Assert.True(rendered.HasErrors);
        Assert.Contains(rendered.Diagnostics, diagnostic => diagnostic.Span.Line == 3 && diagnostic.Message.Contains("GanttDateAnchor", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("MM-DD", "excludes 01-03", "01-02", "2d", "2026-01-05")]
    [InlineData("MMM D", "excludes Jan 3", "Jan 2", "2d", "2026-01-05")]
    [InlineData("MM-dd", "excludes 01-03", "01-02", "2d", "2026-01-05")]
    [InlineData("MM-DD", "excludes weekends\nincludes 01-03", "01-02", "1d", "2026-01-03")]
    [InlineData("MM-DD", "excludes 01-01", "2026-12-30", "3d", "2027-01-02")]
    [InlineData("HH:mm", "excludes 13:00", "13:00", "2d", "2026-01-04")]
    [InlineData("YYYY-MM", "excludes 2026-01", "2026-01-02", "2d", "2026-01-04")]
    [InlineData("YYYY-[Q]Q", "excludes 2026-Q1", "2026-01-02", "2d", "2026-01-04")]
    [InlineData("YYYY HH:mm", "excludes 2026 13:00", "2026-01-02T13:00", "2d", "2026-01-04")]
    [InlineData("YYYY-MM", "excludes weekends\nincludes 2026-01", "2026-01-02", "1d", "2026-01-05")]
    [InlineData("D", "excludes Saturday, 03 January 2026", "2026-01-09", "2d", "2026-01-11")]
    [InlineData("D", "excludes weekends\nincludes Saturday, 03 January 2026", "2026-01-02", "1d", "2026-01-03")]
    public void CalendarDatesUseTheAnchorAndDoNotBecomeRecurringRules(string format, string calendar, string input, string duration, string expectedEnd) {
        var parsed = new MermaidParser().ParseGantt("gantt\n" + calendar + "\ndateFormat " + format + "\nTask :" + input + "," + duration,
            new MermaidParseOptions { GanttDateAnchor = new DateTime(2026, 1, 2) });
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(expectedEnd, Assert.Single(parsed.Document!.Tasks).End.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("MM-DD", "excludes 02-29")]
    [InlineData("MM-DD", "includes 02-29")]
    [InlineData("MMM D", "excludes Feb 29")]
    [InlineData("HH:mm", "excludes 13:00")]
    [InlineData("MM-dd", "includes 02-29")]
    [InlineData("%m/%d", "includes 02/29")]
    public void CompleteTaskFallbackDoesNotHideAMissingCalendarAnchor(string format, string calendar) {
        var rendered = MermaidRenderer.Render("gantt\n" + calendar + "\ndateFormat " + format + "\nTask :task,2028-02-28,2d");
        Assert.True(rendered.HasErrors);
        Assert.Null(rendered.Artifact);
        Assert.Contains(rendered.Diagnostics, diagnostic => diagnostic.Span.Line == 2 && diagnostic.Message.Contains("GanttDateAnchor", StringComparison.Ordinal));
    }

    [Fact]
    public void CompleteIsoAndRecurringCalendarRulesDoNotRequireAnAnchor() {
        var parsed = new MermaidParser().ParseGantt("gantt\nexcludes weekends 2028-02-29\nincludes monday\ndateFormat MM-DD\nTask :task,2028-02-28,2d");
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(new DateTime(2028, 3, 2), Assert.Single(parsed.Document!.Tasks).End);
    }

    [Fact]
    public void TypedGenericSourceAndMarkdownEntryPointsShareAnchorAndRetainItInPortableOutput() {
        const string source = "gantt\ndateFormat MM-DD\nTask :02-29,1d";
        var options = new MermaidRenderOptions { ParseOptions = new MermaidParseOptions { GanttDateAnchor = Anchor } };
        var typed = new MermaidParser().ParseGantt(source, options.ParseOptions);
        var generic = new MermaidParser().Parse(source, options.ParseOptions);
        var rendered = MermaidRenderer.Render(source, options);
        var markup = new MermaidVisualMarkupParser(options);
        options.ParseOptions.GanttDateAnchor = new DateTime(2027, 1, 1);
        Assert.Empty(typed.Diagnostics); Assert.Empty(generic.Diagnostics); Assert.Empty(rendered.Diagnostics);
        var document = Assert.IsType<MermaidGanttDocument>(typed.Document);
        var artifact = Assert.IsAssignableFrom<VisualArtifact>(rendered.Artifact);
        Assert.Equal(document.ToSvg(), generic.Document!.ToSvg());
        Assert.Equal(document.ToSvg(), artifact.ToSvg());
        Assert.Equal(document.ToSvg(), Assert.Single(markup.Parse("```mermaid\n" + source + "\n```").Artifacts).ToSvg());
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        Assert.Equal("2028-02-20", envelope.Extensions["mermaid.dateAnchor"]);
        Assert.NotEmpty(artifact.ToPng());
    }

    [Fact]
    public void ExplicitIsoDatesRemainAvailableWithoutAnAnchorUnderAYearlessFormat() {
        var parsed = new MermaidParser().ParseGantt("gantt\ndateFormat HH:mm\nTask :2026-01-02,2026-01-03");
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(new DateTime(2026, 1, 2), Assert.Single(parsed.Document!.Tasks).Start);
    }

    [Theory]
    [InlineData("January 2")]
    [InlineData("13:25")]
    [InlineData("13:25:00.2026")]
    public void LoosePartialFallbackCannotSupplyTheSystemDateToAYearfulFormat(string input) {
        var rendered = MermaidRenderer.Render("gantt\nTask :" + input + ",1h");
        Assert.True(rendered.HasErrors);
        Assert.Null(rendered.Artifact);
    }
}
