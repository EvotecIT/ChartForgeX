using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GanttTodayStyleTests {
    private static readonly DateTime Start = new(2026, 1, 1);

    [Fact]
    public void SharedSourceStyleMapsToNativeStrokeWithoutReadingTheClock() {
        var root = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures");
        var source = File.ReadAllText(Path.Combine(root, "gantt-today-style.mmd"));
        using var expected = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "gantt-today-style.expected.json")));
        var parsed = new MermaidParser().ParseGantt(source);
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(expected.RootElement.GetProperty("todayMarker").GetString(), parsed.Document!.TodayMarker);
        Assert.Null(parsed.Document.ToChart().Options.GanttToday);
        var chart = parsed.Document.ToChart(new MermaidGanttRenderOptions { Today = Start.AddDays(2) });
        var line = Today(chart);
        Assert.Equal(5, line.StrokeWidth);
        Assert.Equal(ChartColor.FromRgba(0, 255, 0, 128), line.Stroke);
        Assert.Equal(new[] { 6d, 8d }, line.Dash);
        Assert.Equal(parsed.Document.ToSvg(), parsed.Document.ToSvg());
        Assert.Equal(parsed.Document.TodayMarker, parsed.Document.ToVisualArtifact().Metadata["mermaid.todayMarker"]);
    }

    [Fact]
    public void CallerStrokeOverrideWinsWhileSourceOffStillHidesTheGuide() {
        var options = new MermaidGanttRenderOptions { Today = Start.AddDays(2), TodayStyle = new ChartStrokeStyle(ChartColor.Black, 3, dash: Array.Empty<double>()) };
        var document = new MermaidParser().ParseGantt("gantt\ntodayMarker stroke:red,stroke-width:7px\nTask :2026-01-01,6d").Document!;
        var line = Today(document.ToChart(options));
        Assert.Equal(ChartColor.Black, line.Stroke);
        Assert.Equal(3, line.StrokeWidth);
        Assert.Empty(line.Dash!);
        document.TodayMarker = "OFF";
        var hidden = document.ToChart(options);
        Assert.DoesNotContain(hidden.Prepare(VisualExportRequest.ForChart(hidden).Context).Scene.Nodes, node => node.Role == "gantt-now-line");
    }

    [Theory]
    [InlineData("stroke:url(https://example.invalid/paint)")]
    [InlineData("stroke-width:NaN")]
    [InlineData("stroke-width:-1px")]
    [InlineData("stroke-width:1e300px")]
    [InlineData("opacity:2")]
    [InlineData("stroke-dasharray:0 3")]
    [InlineData("stroke-dasharray:0.001 0.001")]
    [InlineData("stroke-dasharray:1e-300 2")]
    [InlineData("filter:blur(2px)")]
    public void UnsupportedDeclarationsAreRetainedAndLocatedWhileSafeDeclarationsStillApply(string unsupported) {
        var declaration = "todayMarker stroke-width:4px," + unsupported;
        var parsed = new MermaidParser().ParseGantt("gantt\n" + declaration + "\nTask :2026-01-01,6d");
        var chart = parsed.Document!.ToChart(new MermaidGanttRenderOptions { Today = Start.AddDays(2) });
        Assert.NotEmpty(chart.ToPng());
        var warning = Assert.Single(parsed.Diagnostics);
        Assert.Equal(MermaidDiagnosticCodes.UnsupportedStatement, warning.Code);
        Assert.Equal(MermaidDiagnosticSeverity.Warning, warning.Severity);
        Assert.Equal(2, warning.Span.Line);
        Assert.Equal(declaration, Assert.Single(parsed.Document!.RawStatements).Text);
        Assert.Equal(4, Today(chart).StrokeWidth);
        Assert.DoesNotContain("https://example.invalid/paint", chart.ToSvg(), StringComparison.Ordinal);
    }

    [Fact]
    public void DeclarationCaseLastValuesOpacityProductAndNoneMapToNativeMeaning() {
        var parsed = new MermaidParser().ParseGantt("gantt\ntodayMarker STROKE:blue,stroke:red,stroke-width:0px,opacity:50%,stroke-opacity:0.5,stroke-dasharray:none\nTask :2026-01-01,6d");
        Assert.Empty(parsed.Diagnostics);
        var line = Today(parsed.Document!.ToChart(new MermaidGanttRenderOptions { Today = Start.AddDays(2) }));
        Assert.Equal(ChartColor.FromRgba(255, 0, 0, 64), line.Stroke);
        Assert.Equal(0, line.StrokeWidth);
        Assert.Empty(line.Dash!);
        var invisible = new MermaidParser().ParseGantt("gantt\ntodayMarker stroke:none\nTask :2026-01-01,6d");
        Assert.Empty(invisible.Diagnostics);
        Assert.Equal(0, Today(invisible.Document!.ToChart(new MermaidGanttRenderOptions { Today = Start.AddDays(2) })).Stroke!.Value.A);
    }

    [Fact]
    public void QuarterPixelDashBoundaryRendersAndInvalidReplacementRetainsTheLastSafePattern() {
        var parsed = new MermaidParser().ParseGantt("gantt\ntodayMarker stroke-dasharray:0.25 0.25,stroke-dasharray:0.249 2\nTask :2026-01-01,6d");
        Assert.Equal(MermaidDiagnosticCodes.UnsupportedStatement, Assert.Single(parsed.Diagnostics).Code);
        var chart = parsed.Document!.ToChart(new MermaidGanttRenderOptions { Today = Start.AddDays(2) });
        Assert.Equal(new[] { .25, .25 }, Today(chart).Dash);
        Assert.Equal(new[] { .25, .25 }, new ChartStrokeStyle(dash: new[] { .25, .25 }).Dash);
        Assert.NotEmpty(chart.ToPng());
        Assert.Contains("stroke-dasharray=\"0.25 0.25\"", chart.ToSvg(), StringComparison.Ordinal);
    }

    [Fact]
    public void NativeReferenceStylesAreImmutableAndSharedWithAnnotationsAndLaneGuides() {
        var authoredDash = new[] { 5d, 2d };
        var style = new ChartStrokeStyle(ChartColor.FromHex("#336699"), 3, .5, authoredDash);
        authoredDash[0] = 99;
        var chart = Chart.Create().WithLegend(false).AddLine("Work", new[] { new ChartPoint(0, 1), new ChartPoint(10, 2) }).AddVerticalLine(4, "Reference");
        chart.Annotations[0].StrokeStyle = style;
        var reference = Assert.Single(chart.Prepare(VisualExportRequest.ForChart(chart).Context).Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "annotation-line");
        Assert.Equal(3, reference.StrokeWidth);
        Assert.Equal(ChartColor.FromRgba(51, 102, 153, 128), reference.Stroke);
        Assert.Equal(new[] { 5d, 2d }, reference.Dash);
        var lane = Chart.Create().WithGanttLaneNow(DateTime.SpecifyKind(Start.AddDays(2), DateTimeKind.Utc))
            .WithStateCategories(new ChartStateCategory("work", "Work", ChartColor.Black))
            .AddGanttLane("Service", new[] { new ChartGanttLaneItem(DateTime.SpecifyKind(Start, DateTimeKind.Utc), null, "work") });
        lane.Options.GanttTodayStyle = style;
        Assert.Equal(reference.Stroke, Today(lane).Stroke);
        Assert.Equal(reference.StrokeWidth, Today(lane).StrokeWidth);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartStrokeStyle(width: double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartStrokeStyle(opacity: 1.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartStrokeStyle(dash: new[] { 0d }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartStrokeStyle(dash: new[] { .001, .001 }));
    }

    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(false, 960)]
    [InlineData(true, 320)]
    public async Task StyledGuideKeepsItsStrokeAcrossNativeAndBrowserTaskFills(bool dark, int width) {
        var parsed = new MermaidParser().ParseGantt("gantt\ntodayMarker stroke:#00ff00,stroke-width:5px,opacity:0.5,stroke-dasharray:6 8\nTask :2026-01-01,6d");
        Assert.Empty(parsed.Diagnostics);
        var chart = parsed.Document!.ToChart(new MermaidGanttRenderOptions { Today = Start.AddDays(2), Width = width, Height = 360 })
            .WithLegend(false).WithDataLabels(false).WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight());
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var line = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == "gantt-now-line");
        var actual = prepared.ToRgba();
        var control = VisualSceneRasterRenderer.Render(new VisualScene(prepared.Scene.Size, prepared.Scene.Nodes.Where(node => node.Role != "gantt-now-line").ToArray(), prepared.Scene.Diagnostics, prepared.Scene.Regions));
        var task = Assert.Single(prepared.Regions, region => region.Role == "gantt-task");
        var changed = 0;
        for (var y = (int)Math.Ceiling(task.Bounds.Top + 3); y < task.Bounds.Bottom - 3; y++) {
            var offset = (y * actual.Width + (int)Math.Round(line.Start.X)) * 4;
            if (Enumerable.Range(0, 3).Any(channel => Math.Abs(actual.Pixels[offset + channel] - control.Pixels[offset + channel]) > 30)) changed++;
        }
        Assert.True(changed >= 4, "Styled today ink must remain visible through the task; changed pixels: " + changed);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        var name = "gantt-today-style-" + (dark ? "dark-" : "light-") + width;
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), prepared.ToPng());
        }
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + chart.ToHtmlFragment() + "</body></html>", width, 400);
        var guide = session.Page.Locator("[data-cfx-role='gantt-now-line']");
        Assert.Equal(5, await guide.EvaluateAsync<double>("node => parseFloat(getComputedStyle(node).strokeWidth)"));
        Assert.Equal("6px, 8px", await guide.EvaluateAsync<string>("node => getComputedStyle(node).strokeDasharray"));
        Assert.Contains("0, 255, 0", await guide.EvaluateAsync<string>("node => getComputedStyle(node).stroke"));
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        if (!string.IsNullOrWhiteSpace(captures)) await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + ".png"), FullPage = true });
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    private static VisualSceneLine Today(Chart chart) => Assert.Single(chart.Prepare(VisualExportRequest.ForChart(chart).Context).Scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == "gantt-now-line");
}
