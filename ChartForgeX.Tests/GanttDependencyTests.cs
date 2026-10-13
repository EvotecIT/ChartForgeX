using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GanttDependencyTests {
    private static string Fixture(string extension) => Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", "gantt-forward-after." + extension);

    [Fact]
    public void ForwardAndMultipleAfterReferencesKeepSharedDatesSourceOrderAndAllLinks() {
        var source = File.ReadAllText(Fixture("mmd"));
        var result = MermaidRenderer.Render(source);
        Assert.Empty(result.Diagnostics);
        var document = Assert.IsType<MermaidGanttDocument>(result.Document);
        using var expected = JsonDocument.Parse(File.ReadAllText(Fixture("expected.json")));
        var tasks = expected.RootElement.GetProperty("tasks").EnumerateArray().ToArray();
        Assert.Equal(tasks.Length, document.Tasks.Count);
        for (var i = 0; i < tasks.Length; i++) {
            Assert.Equal(tasks[i][0].GetString(), document.Tasks[i].Id);
            Assert.Equal(tasks[i][1].GetString(), document.Tasks[i].Start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            Assert.Equal(tasks[i][2].GetString(), document.Tasks[i].End.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }
        Assert.Equal(2, document.Tasks[0].DependencyIndex);
        Assert.Equal(new[] { "first", "other" }, document.Tasks[0].DependencyIds);
        var chart = Assert.IsType<Chart>(result.Artifact!.Model);
        Assert.Equal(new[] { (chart.Series[2].Id, chart.Series[0].Id), (chart.Series[1].Id, chart.Series[0].Id), (chart.Series[0].Id, chart.Series[3].Id) },
            chart.GanttDependencies.Select(link => (link.PredecessorId, link.SuccessorId)));
        var svg = result.Artifact.ToSvg();
        Assert.Equal(3, XDocument.Parse(svg).Descendants().Count(element => (string?)element.Attribute("data-cfx-role") == "gantt-dependency"));
        Assert.NotEmpty(result.Artifact.ToPng());
        var json = result.Artifact.ToInterchangeJson();
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(json);
        Assert.Equal("3", envelope.Extensions["mermaid.dependencies"]);
        Assert.Equal(json, envelope.ToJson());
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fenced.Diagnostics);
        Assert.Equal(chart.GanttDependencies, Assert.IsType<Chart>(Assert.Single(fenced.Artifacts).Model).GanttDependencies);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DirectConversionRejectsAmbiguousOrMissingDependencyIds(bool duplicate) {
        var document = new MermaidParser().ParseGantt("gantt\nA :a, 2026-01-01, 1d\nB :b, after a, 1d").Document!;
        if (duplicate) document.Tasks.Add(document.Tasks[0]);
        else document.Tasks[1].DependencyIds.Add("missing");
        Assert.Throws<ArgumentException>(() => document.ToChart());
    }

    [Theory]
    [InlineData("A :a, after b, 1d\nB :b, after a, 1d", "circular")]
    [InlineData("A :a, after a, 1d", "circular")]
    [InlineData("A :a, after missing, 1d", "declared")]
    public void InvalidForwardSchedulesKeepLocatedErrorsAndDoNotRender(string tasks, string message) {
        var rendered = MermaidRenderer.Render("gantt\n" + tasks);
        Assert.True(rendered.HasErrors);
        Assert.Null(rendered.Artifact);
        Assert.Contains(rendered.Diagnostics, diagnostic => diagnostic.Span.Line == 2 && diagnostic.Message.Contains(message));
    }

    [Fact]
    public void NativeLinksCombineWithInlineDependenciesWithoutDuplicationOrRescheduling() {
        var chart = Chart.Create().AddGanttTask("First", 1, 3);
        var first = chart.Series[0];
        chart.AddGanttTask("Last", 4, 6, dependsOn: first).AddGanttTask("Other", 1, 4);
        var last = chart.Series[1]; var other = chart.Series[2];
        var before = chart.Series.Select(series => series.GanttItems[0]).ToArray();
        chart.AddGanttDependency(first, last).AddGanttDependency(other, last).AddGanttDependency(other, last);
        var links = chart.GanttDependencies;
        Assert.Equal(new[] { (first.Id, last.Id), (other.Id, last.Id) }, links.Select(link => (link.PredecessorId, link.SuccessorId)));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var svg = prepared.ToSvg();
        Assert.Equal(2, prepared.Regions.Count(region => region.Role == "gantt-dependency"));
        Assert.Equal(2, prepared.Scene.Nodes.OfType<VisualScenePath>().Count(path => path.Role == "gantt-dependency-line"));
        Assert.NotEmpty(prepared.ToPng());
        chart.AddGanttDependency(first, other);
        Assert.Equal(2, links.Count);
        Assert.Equal(svg, prepared.ToSvg());
        Assert.Equal(before, chart.Series.Select(series => series.GanttItems[0]));
    }

    [Fact]
    public void NativeLinksRequireDistinctOwnedGanttHandlesAndFailedSugarDoesNotAppend() {
        var chart = Chart.Create().AddGanttTask("A", 1, 2).AddGanttTask("B", 2, 3);
        var first = chart.Series[0]; var second = chart.Series[1];
        var foreign = ChartSeries.CreateGanttTask(new ChartSeriesId("foreign"), "A", 1, 2);
        Assert.Throws<ArgumentNullException>(() => chart.AddGanttDependency(null!, second));
        Assert.Throws<ArgumentNullException>(() => chart.AddGanttDependency(first, null!));
        Assert.Throws<ArgumentException>(() => chart.AddGanttDependency(first, first));
        Assert.Throws<ArgumentException>(() => chart.AddGanttDependency(foreign, second));
        Chart.Create().AddSeries(foreign);
        Assert.Throws<ArgumentException>(() => chart.AddGanttDependency(first, foreign));
        Assert.Throws<ArgumentException>(() => chart.AddGanttTask("Rejected", 3, 4, dependsOn: foreign));
        Assert.Equal(2, chart.Series.Count);
        Assert.Empty(chart.GanttDependencies);
    }

    [Fact]
    public void NativeRemovalPrunesIncidentLinksWithoutResurrectingThemOnReattachment() {
        var chart = Chart.Create().AddGanttTask("A", 1, 2).AddGanttTask("B", 2, 3);
        var removed = chart.Series[1];
        chart.AddGanttDependency(removed, chart.Series[0]).RemoveSeries(removed);
        Assert.Empty(chart.GanttDependencies);
        Assert.NotEmpty(chart.ToSvg()); Assert.NotEmpty(chart.ToPng());
        chart.AddSeries(removed);
        Assert.Empty(chart.GanttDependencies);
    }

    [Fact]
    public void NativeLinksRejectOtherChartFamiliesAndRevalidateAfterReplacement() {
        var chart = Chart.Create().AddGanttTask("A", 1, 2).AddGanttTask("B", 2, 3);
        chart.AddGanttDependency(chart.Series[1], chart.Series[0]).RemoveSeries(chart.Series[1]);
        chart.AddBar("Counts", new[] { new ChartForgeX.Primitives.ChartPoint(1, 2) });
        Assert.Throws<ArgumentException>(() => chart.AddGanttDependency(chart.Series[0], chart.Series[1]));
        Assert.Throws<InvalidOperationException>(() => chart.ToSvg());
        chart.ClearSeries().AddLine("Reused", new[] { new ChartForgeX.Primitives.ChartPoint(1, 2) });
        Assert.Empty(chart.GanttDependencies); Assert.NotEmpty(chart.ToSvg()); Assert.NotEmpty(chart.ToPng());
    }

    [BrowserTheory]
    [InlineData(false, 960)]
    [InlineData(true, 320)]
    public async Task ForwardAndMultipleLinksAreObservedInStaticBrowserOutput(bool dark, int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        var document = new MermaidParser().ParseGantt(File.ReadAllText(Fixture("mmd"))).Document!;
        var chart = document.ToChart(new MermaidGanttRenderOptions { Width = width, Height = 420 })
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + chart.ToHtmlFragment() + "</body></html>", width, 460);
        Assert.Equal(3, await session.Page.Locator("[data-cfx-role='gantt-dependency-line']").CountAsync());
        Assert.Equal(4, await session.Page.Locator("[data-cfx-role='gantt-task'],[data-cfx-role='gantt-milestone']").CountAsync());
        Assert.Equal(0, await session.Page.Locator("script").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            var name = "gantt-forward-" + (dark ? "dark-" : "light-") + width;
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + ".png"), FullPage = true });
            File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), chart.ToPng());
        }
    }
}
