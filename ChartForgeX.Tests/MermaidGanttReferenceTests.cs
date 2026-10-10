using System.Globalization;
using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidGanttReferenceTests {
    [Theory]
    [InlineData(false, 960)]
    [InlineData(true, 320)]
    public async Task AuthoredMilestoneRangeProducesOrderedStaticMarks(bool dark, int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        var fixture = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", "gantt-milestone-dependencies.mmd");
        var result = new MermaidParser().ParseGantt(File.ReadAllText(fixture));
        Assert.False(result.HasErrors);
        var chart = result.Document!.ToChart().WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight());
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style=\"margin:0\">" + chart.ToHtmlFragment() + "</body></html>", width, 600);
        var marks = session.Page.Locator("[data-cfx-role=\"gantt-task-shape\"], [data-cfx-role=\"gantt-milestone-shape\"]");
        Assert.Equal(3, await marks.CountAsync());
        var window = (await marks.Nth(0).BoundingBoxAsync())!;
        var gate = (await marks.Nth(1).BoundingBoxAsync())!;
        var tail = (await marks.Nth(2).BoundingBoxAsync())!;
        Assert.True(window.X + window.Width < gate.X && gate.X + gate.Width < tail.X,
            "The until window ends before the milestone midpoint, and the dependent starts after its authored end.");
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        Assert.Equal(0, await session.Page.Locator("script").CountAsync());
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions {
                Path = Path.Combine(captures, "gantt-references-" + (dark ? "dark-" : "light-") + width + ".png"), FullPage = true
            });
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData("gantt-until")]
    [InlineData("gantt-repeated-calendar")]
    [InlineData("gantt-milestone-dependencies")]
    [InlineData("gantt-render-calendar")]
    public void ResolvedDatesMatchSharedUpstreamExpectations(string name) {
        var fixtures = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures");
        var result = new MermaidParser().ParseGantt(File.ReadAllText(Path.Combine(fixtures, name + ".mmd")));
        Assert.False(result.HasErrors, string.Join("; ", result.Diagnostics.Select(item => item.Message)));
        var document = result.Document!;
        using var expected = JsonDocument.Parse(File.ReadAllText(Path.Combine(fixtures, name + ".expected.json")));
        var tasks = expected.RootElement.GetProperty("tasks").EnumerateArray().ToArray();
        Assert.Equal(tasks.Length, document.Tasks.Count);
        for (var index = 0; index < tasks.Length; index++) {
            Assert.Equal(tasks[index][0].GetString(), document.Tasks[index].Id);
            Assert.Equal(tasks[index][1].GetString(), document.Tasks[index].Start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            Assert.Equal(tasks[index][2].GetString(), document.Tasks[index].End.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }
        if (expected.RootElement.TryGetProperty("taskRenderEnds", out var renderEnds)) {
            Assert.Equal(renderEnds.EnumerateArray().Select(item => item.GetString()), document.Tasks.Select(task => task.RenderEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
        }
    }

    [Fact]
    public void UntilReferencesRemainInspectableWithoutBecomingAfterConnectors() {
        var result = new MermaidParser().ParseGantt("gantt\nA :a, 2026-01-01, until b c\nB :b, 2026-01-05, 0d\nC :c, 2026-01-03, 0d");
        Assert.False(result.HasErrors);
        var document = result.Document!;
        Assert.Equal(new[] { "b", "c" }, document.Tasks[0].UntilTaskIds);
        Assert.Empty(document.Tasks[0].DependencyIds);
        Assert.Equal(-1, document.Tasks[0].DependencyIndex);
        Assert.Equal(new DateTime(2026, 1, 3), document.Tasks[0].End);
        Assert.Equal(2, document.Tasks[0].Span.Line);
        Assert.Equal("2", document.ToVisualArtifact().Metadata["mermaid.untilReferences"]);
    }

    [Fact]
    public void FinalDateFormatAppliesToTasksDeclaredBeforeIt() {
        var result = new MermaidParser().ParseGantt("gantt\nTask :task, 02/01/2026, 1d\ndateFormat DD/MM/YYYY");
        Assert.False(result.HasErrors);
        Assert.Equal(new DateTime(2026, 1, 2), Assert.Single(result.Document!.Tasks).Start);
    }

    [Theory]
    [InlineData("A :a, 2026-01-01, until missing", "until")]
    [InlineData("A :a, 2026-01-01, until", "until")]
    [InlineData("A :a, 2026-01-01, until b\nB :b, after a, 1d", "circular")]
    [InlineData("A :a, 2026-01-02, until b\nB :b, 2026-01-01, 0d", "greater than or equal")]
    public void InvalidEndReferencesProduceLocatedErrors(string tasks, string message) {
        var result = new MermaidParser().ParseGantt("gantt\n" + tasks);
        Assert.True(result.HasErrors);
        Assert.Contains(result.Diagnostics, item => item.Span.Line == 2 && item.Severity == MermaidDiagnosticSeverity.Error && item.Message.Contains(message, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void IndependentStartDatesCanReferenceEachOtherAsEnds() {
        var result = new MermaidParser().ParseGantt("gantt\nA :a, 2026-01-01, until b\nB :b, 2026-01-01, until a");
        Assert.False(result.HasErrors);
        Assert.Equal(2, result.Document!.Tasks.Count);
        Assert.All(result.Document.Tasks, task => Assert.Equal(task.Start, task.End));
    }

    [Theory]
    [InlineData("2026-01-05", "until anchor", 6, 5)]
    [InlineData("2026-01-05", "3d", 6, 5)]
    [InlineData("2026-01-03", "until anchor", 6, 6)]
    [InlineData("2026-01-03", "3d", 6, 6)]
    [InlineData("2026-01-03, 2026-01-05", "until anchor", 7, 7)]
    [InlineData("2026-01-03, 2026-01-05", "3d", 7, 7)]
    [InlineData("weekends", "until anchor", 7, 7)]
    [InlineData("weekends", "3d", 7, 7)]
    public void CalendarSchedulingAndVisibleEndsFollowUpstreamTrailingExclusionRules(string excludes, string end, int scheduleDay, int visibleDay) {
        var result = new MermaidParser().ParseGantt("gantt\nexcludes " + excludes + "\nWindow :window, 2026-01-02, " + end + "\nAnchor :anchor, 2026-01-05, 0d\nAfter :afterTask, after window, 0d\nImplicit :0d");
        Assert.False(result.HasErrors);
        var document = result.Document!;
        Assert.Equal(new DateTime(2026, 1, scheduleDay), document.Tasks[0].End);
        Assert.Equal(new DateTime(2026, 1, visibleDay), document.Tasks[0].RenderEnd);
        Assert.Equal(document.Tasks[0].End, document.Tasks[2].Start);
        Assert.Equal(document.Tasks[2].End, document.Tasks[3].Start);
        Assert.Equal(document.Tasks[0].RenderEnd.ToOADate(), document.ToChart().Series[0].Points[0].Y);
    }

    [Theory]
    [InlineData(false, 960)]
    [InlineData(true, 320)]
    public async Task ExcludedUntilEndpointKeepsTheBarBeforeItsDependent(bool dark, int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        var source = "gantt\nexcludes 2026-01-05\nWindow :window, 2026-01-02, until anchor\nAnchor :anchor, 2026-01-05, 1d\nTail :tail, after window, 1d";
        var result = new MermaidParser().ParseGantt(source);
        Assert.False(result.HasErrors);
        var chart = result.Document!.ToChart().WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight());
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style=\"margin:0\">" + chart.ToHtmlFragment() + "</body></html>", width, 600);
        var bars = session.Page.Locator("[data-cfx-role=\"gantt-task-shape\"]");
        Assert.Equal(3, await bars.CountAsync());
        var window = (await bars.Nth(0).BoundingBoxAsync())!;
        var anchor = (await bars.Nth(1).BoundingBoxAsync())!;
        var tail = (await bars.Nth(2).BoundingBoxAsync())!;
        Assert.True(Math.Abs(window.X + window.Width - anchor.X) < 1, "The until bar must end at the referenced start.");
        Assert.True(tail.X > anchor.X, "After scheduling must retain the excluded day.");
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        Assert.Equal(0, await session.Page.Locator("script").CountAsync());
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            var name = "gantt-excluded-until-" + (dark ? "dark-" : "light-") + width;
            await session.Page.ScreenshotAsync(new PageScreenshotOptions {Path = Path.Combine(captures, name + ".png"), FullPage = true});
            File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), chart.ToPng());
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [Fact]
    public void MilestoneRenderingKeepsItsMidpointWhileDependenciesUseItsAuthoredRange() {
        var result = new MermaidParser().ParseGantt("gantt\nGate :milestone, gate, 2026-01-02, 2d\nTail :tail, after gate, 1d");
        Assert.False(result.HasErrors);
        var document = result.Document!;
        Assert.Equal(new DateTime(2026, 1, 2), document.Tasks[0].Start);
        Assert.Equal(new DateTime(2026, 1, 4), document.Tasks[0].End);
        Assert.Equal(document.Tasks[0].End, document.Tasks[1].Start);
        Assert.Equal(new DateTime(2026, 1, 3).ToOADate(), document.ToChart().Series[0].Points[0].X);
    }
}
