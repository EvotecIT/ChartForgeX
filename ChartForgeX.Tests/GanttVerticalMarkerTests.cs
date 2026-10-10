using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GanttVerticalMarkerTests {
    private static readonly DateTime Start = new(2026, 1, 1);
    private static string Fixture(string extension) => Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", "gantt-vertical-marker." + extension);

    [Fact]
    public void SharedReferenceDatesAndMarkerOrderReachNativeRowsAndPortableOutput() {
        var source = File.ReadAllText(Fixture("mmd"));
        var parsed = new MermaidParser().ParseGantt(source);
        Assert.Empty(parsed.Diagnostics);
        var document = parsed.Document!;
        using var expected = JsonDocument.Parse(File.ReadAllText(Fixture("expected.json")));
        Assert.Equal(expected.RootElement.GetProperty("taskTimestamps").EnumerateArray().Select(task => (task[1].GetString()!, task[2].GetString()!)),
            document.Tasks.Select(task => (Stamp(task.Start), Stamp(task.End))));
        Assert.Equal(new[] { false, true, false }, document.Tasks.Select(task => task.IsVerticalMarker));
        var chart = document.ToChart(new MermaidGanttRenderOptions { Today = Start.AddDays(2) }).WithLegend(false);
        Assert.Null(chart.Options.GanttToday);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Equal(3, chart.Series.Count);
        Assert.Equal(2, prepared.Regions.Count(region => region.Role == "gantt-task"));
        var marker = Assert.Single(prepared.Regions, region => region.Role == "gantt-vertical-marker");
        Assert.Equal("series-1-point-0", marker.Id);
        Assert.True(marker.Bounds.Height > 0);
        Assert.Single(prepared.Regions, region => region.Role == "gantt-dependency");
        var tasks = prepared.Regions.Where(region => region.Role == "gantt-task").ToArray();
        var control = Chart.Create().WithTitle("Mermaid Gantt").WithSubtitle("gantt").WithSize(960, 560).WithLegend(false)
            .AddGanttTask("Work / Design", Start, Start.AddDays(3)).AddGanttTask("Work / Delivery", Start.AddDays(3), Start.AddDays(5)).AddGanttDependency(0, 1);
        var controlTasks = control.Prepare(VisualExportRequest.ForChart(control).Context).Regions.Where(region => region.Role == "gantt-task").ToArray();
        Assert.Equal(controlTasks.Select(task => task.Bounds), tasks.Select(task => task.Bounds));
        var arrow = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "gantt-dependency-arrow");
        Assert.Equal(tasks[1].Bounds.Top + tasks[1].Bounds.Height / 2, arrow.Commands[1].Y, 6);
        var json = document.ToVisualArtifact().ToInterchangeJson();
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(json);
        Assert.Equal("1", envelope.Extensions["mermaid.verticalMarkers"]);
        Assert.Equal(json, envelope.ToJson());
        Assert.Equal(document.ToSvg(), Assert.Single(new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```").Artifacts).ToSvg());
    }

    [Fact]
    public void ReferencesToMarkerRangesScheduleTasksWithoutInventingRowConnectors() {
        var parsed = new MermaidParser().ParseGantt("gantt\nDesign :design,2026-01-01,3d\nDeadline :vert,deadline,2026-01-02,4d\nAfter marker :aftermark,after deadline,2d\nDelivery :delivery,after design,2d");
        Assert.Empty(parsed.Diagnostics);
        var document = parsed.Document!;
        var chart = document.ToChart();
        Assert.Equal(Start.AddDays(5), document.Tasks[2].Start);
        Assert.Equal(Start.AddDays(7), document.Tasks[2].End);
        Assert.Equal(2, chart.GanttDependencies.Count);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Equal(3, prepared.Regions.Count(region => region.Role == "gantt-task"));
        Assert.Single(prepared.Regions, region => region.Role == "gantt-dependency");
        Assert.Contains("data-cfx-dependency=\"1\"", prepared.ToSvg());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IncomingMarkerLinksRemainInSvgMetadataWithoutInventingConnectors(bool mermaid) {
        var chart = mermaid
            ? new MermaidParser().ParseGantt("gantt\nDesign :design,2026-01-01,2d\nOther :other,2026-01-01,3d\nDeadline :vert,deadline,after design other,1d\nAfter :aftermark,after deadline,1d").Document!.ToChart()
            : Chart.Create().AddGanttTask("Design", 1, 3).AddGanttTask("Other", 1, 4).AddGanttMarker("Deadline", 4, 5)
                .AddGanttTask("After", 5, 6).AddGanttDependency(0, 2).AddGanttDependency(1, 2).AddGanttDependency(2, 3);
        chart.AddGanttDependency(0, 1);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var svg = XDocument.Parse(prepared.ToSvg());
        var marker = Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "gantt-vertical-marker");
        Assert.Equal(mermaid ? "1" : "0", (string?)marker.Attribute("data-cfx-dependency"));
        Assert.Equal(mermaid ? "1,0" : "0,1", (string?)marker.Attribute("data-cfx-dependencies"));
        var successor = Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "gantt-task" && (string?)element.Attribute("data-cfx-series") == "3");
        Assert.Equal("2", (string?)successor.Attribute("data-cfx-dependency"));
        Assert.Equal(4, chart.GanttDependencies.Count);
        Assert.Single(prepared.Regions, region => region.Role == "gantt-dependency");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MarkerPointColorOverridesSeriesInkInSvgAndNativePixels(bool pointOverride) {
        var seriesColor = ChartColor.FromHex("#d83b19");
        var pointColor = ChartColor.FromHex("#19d871");
        var chart = Chart.Create().WithSize(640, 320).WithLegend(false).WithDataLabels(false)
            .AddGanttTask("Work", 1, 9).AddGanttMarker("Deadline", 4, color: seriesColor);
        if (pointOverride) chart.Series[1].WithPointColor(0, pointColor);
        var expected = pointOverride ? pointColor : seriesColor;
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var line = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "annotation-line");
        Assert.Equal(expected, line.Stroke);
        var svgLine = Assert.Single(XDocument.Parse(prepared.ToSvg()).Descendants(), element => (string?)element.Attribute("data-cfx-role") == "annotation-line");
        Assert.Equal(expected.ToCss(), (string?)svgLine.Attribute("stroke"));
        var pixels = prepared.ToRgba();
        var control = VisualSceneRasterRenderer.Render(new VisualScene(prepared.Scene.Size, prepared.Scene.Nodes.Where(node => node.Role != "annotation-line").ToArray(), prepared.Scene.Diagnostics, prepared.Scene.Regions));
        var task = Assert.Single(prepared.Regions, region => region.Role == "gantt-task");
        var matches = 0;
        for (var y = (int)Math.Ceiling(task.Bounds.Top + 3); y < task.Bounds.Bottom - 3; y++) {
            for (var x = (int)Math.Floor(line.Commands[0].X - 1); x <= Math.Ceiling(line.Commands[0].X + 1); x++) {
                var offset = (y * pixels.Width + x) * 4;
                var dominant = pointOverride ? pixels.Pixels[offset + 1] > pixels.Pixels[offset] + 25 : pixels.Pixels[offset] > pixels.Pixels[offset + 1] + 25;
                if (dominant && Enumerable.Range(0, 3).Any(channel => Math.Abs(pixels.Pixels[offset + channel] - control.Pixels[offset + channel]) > 30)) matches++;
            }
        }
        Assert.True(matches >= 4, "The authored marker ink must survive task fills in native output; pixels: " + matches);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            File.WriteAllBytes(Path.Combine(captures, "gantt-marker-" + (pointOverride ? "point" : "series") + "-color.png"), prepared.ToPng());
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MarkerOnlySchedulesHaveNoTaskRowsAndKeepTheirAuthoredWindow(bool explicitBounds) {
        var chart = Chart.Create().WithLegend(false).AddGanttMarker("Begin", Start, Start.AddDays(9)).AddGanttMarker("End", Start.AddDays(9));
        if (explicitBounds) chart.ConfigureXAxis(axis => axis.WithBounds(Start.AddDays(2).ToOADate(), Start.AddDays(7).ToOADate()));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.DoesNotContain(prepared.Regions, region => region.Role == "gantt-task");
        Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role == "schedule-row-label");
        Assert.Equal(explicitBounds ? 0 : 2, prepared.Regions.Count(region => region.Role == "gantt-vertical-marker"));
        var svg = XDocument.Parse(prepared.ToSvg());
        var window = Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "gantt-chart");
        Assert.Equal(Start.AddDays(explicitBounds ? 2 : 0).ToOADate(), (double)window.Attribute("data-cfx-min")!);
        Assert.Equal(Start.AddDays(explicitBounds ? 7 : 9).ToOADate(), (double)window.Attribute("data-cfx-max")!);
        Assert.NotEmpty(prepared.ToPng());
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.no-data");
    }

    [Fact]
    public void NativeAnnotationsShareGuideRenderingAndMarkerInputsAreValidated() {
        var chart = Chart.Create().WithLegend(false).AddGanttTask("Work", 1, 9).AddVerticalLine(4, "Checkpoint");
        Assert.Contains("annotation-line", chart.ToSvg());
        Assert.Contains("Checkpoint", chart.ToSvg());
        Assert.NotEmpty(chart.ToPng());
        Assert.Throws<ArgumentOutOfRangeException>(() => Chart.Create().AddGanttMarker("Invalid", double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => Chart.Create().AddGanttMarker("Invalid", 3, 2));
    }

    [Fact]
    public void TodayMarkersRequireCallerTimeAndSourceOffWinsWhileStylesRemainDiagnosed() {
        var parser = new MermaidParser();
        var plain = parser.ParseGantt("gantt\nTask :2026-01-01,4d").Document!;
        Assert.Null(plain.ToChart().Options.GanttToday);
        Assert.Equal(plain.ToSvg(), plain.ToSvg());
        Assert.Contains("gantt-now-line", plain.ToSvg(new MermaidGanttRenderOptions { Today = Start.AddDays(1) }));
        var off = parser.ParseGantt("gantt\ntodayMarker off\nTask :2026-01-01,4d").Document!;
        Assert.DoesNotContain("gantt-now-line", off.ToSvg(new MermaidGanttRenderOptions { Today = Start.AddDays(1) }));
        var style = parser.ParseGantt("gantt\ntodayMarker stroke:#ff0000,stroke-width:3px\nTask :2026-01-01,4d");
        var warning = Assert.Single(style.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.UnsupportedStatement);
        Assert.Equal(MermaidDiagnosticSeverity.Warning, warning.Severity);
        Assert.Equal(2, warning.Span.Line);
        Assert.Equal("todayMarker stroke:#ff0000,stroke-width:3px", Assert.Single(style.Document!.RawStatements).Text);
    }

    [Theory]
    [InlineData("off")]
    [InlineData("OFF")]
    [InlineData("oFf")]
    public void TodayMarkerOffFollowsCaseInsensitiveDirectiveKeywords(string control) {
        var parsed = new MermaidParser().ParseGantt("gantt\ntodayMarker " + control + "\nTask :2026-01-01,4d");
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(control, parsed.Document!.TodayMarker);
        Assert.Null(parsed.Document.ToChart(new MermaidGanttRenderOptions { Today = Start.AddDays(1) }).Options.GanttToday);
    }

    [Theory]
    [InlineData(false, 960)]
    [InlineData(true, 320)]
    public async Task MarkerLineAndCaptionSelectTheAuthoredSeriesWhileOrdinaryAnnotationsRemainIndependent(bool dark, int width) {
        var chart = Chart.Create().WithSize(width, 360).WithLegend(false)
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .AddGanttTask("Work", Start, Start.AddDays(8)).AddGanttMarker("Deadline", Start.AddDays(3))
            .AddVerticalLine(Start.AddDays(6).ToOADate(), "Reference");
        chart.Series[1].WithInteractionKey("deadline-source");
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Single(prepared.Regions, region => region.Role == "gantt-vertical-marker");
        if (!InteractiveChartBrowser.Enabled) {
            Assert.Single(prepared.Regions, region => region.Role == "annotation");
            return;
        }
        await using var session = await InteractiveChartBrowser.OpenAsync(chart.ToInteractiveHtmlPage(), width, 500);
        var page = session.Page;
        await page.EvaluateAsync("() => { window.cfxSelections = []; document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.cfxSelections.push(event.detail)); }");
        var marker = page.Locator("[data-cfx-role='gantt-vertical-marker']");
        Assert.Equal("series", await marker.GetAttributeAsync("data-cfx-target-kind"));
        Assert.Equal("deadline-source", await marker.GetAttributeAsync("data-cfx-target-id"));
        await marker.Locator("[data-cfx-role='annotation-label']").ClickAsync();
        Assert.Equal(1, await page.EvaluateAsync<int>("() => window.cfxSelections.length"));
        Assert.Equal("series", await page.EvaluateAsync<string>("() => window.cfxSelections[0].target.targetKind"));
        Assert.Equal("deadline-source", await page.EvaluateAsync<string>("() => window.cfxSelections[0].target.targetId"));
        var line = await marker.Locator("[data-cfx-role='annotation-line']").BoundingBoxAsync();
        Assert.NotNull(line);
        await page.Mouse.ClickAsync(line!.X, line.Y + 1);
        Assert.Equal(2, await page.EvaluateAsync<int>("() => window.cfxSelections.length"));
        Assert.Equal("false", await marker.GetAttributeAsync("aria-selected"));
        await marker.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal(3, await page.EvaluateAsync<int>("() => window.cfxSelections.length"));
        Assert.Equal("true", await marker.GetAttributeAsync("aria-selected"));
        var annotation = page.Locator("[data-cfx-role='annotation']");
        await annotation.Locator("[data-cfx-role='annotation-label']").ClickAsync();
        Assert.Equal("annotation", await page.EvaluateAsync<string>("() => window.cfxSelections[3].target.targetKind"));
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, "gantt-marker-selection-" + (dark ? "dark-" : "light-") + width + ".png"), FullPage = true });
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        Assert.Single(prepared.Regions, region => region.Role == "annotation");
    }

    [Theory]
    [InlineData(false, 960)]
    [InlineData(true, 320)]
    public async Task MarkerInkStaysVisibleAcrossTaskFillsInNativeAndBrowserOutput(bool dark, int width) {
        var document = new MermaidParser().ParseGantt(File.ReadAllText(Fixture("mmd"))).Document!;
        var chart = document.ToChart(new MermaidGanttRenderOptions { Width = width, Height = 360 })
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight()).WithLegend(false).WithDataLabels(false);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var scene = prepared.Scene;
        var line = Assert.Single(scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "annotation-line");
        var actual = prepared.ToRgba();
        var control = VisualSceneRasterRenderer.Render(new VisualScene(scene.Size, scene.Nodes.Where(node => node.Role != "annotation-line").ToArray(), scene.Diagnostics, scene.Regions));
        var first = prepared.Regions.First(region => region.Role == "gantt-task");
        var changed = 0;
        for (var y = (int)Math.Ceiling(first.Bounds.Top + 3); y < first.Bounds.Bottom - 3; y++) {
            for (var x = (int)Math.Floor(line.Commands[0].X - 1); x <= Math.Ceiling(line.Commands[0].X + 1); x++) {
                var offset = (y * actual.Width + x) * 4;
                if (Enumerable.Range(0, 3).Any(channel => Math.Abs(actual.Pixels[offset + channel] - control.Pixels[offset + channel]) > 30)) changed++;
            }
        }
        Assert.True(changed >= 4, "The vertical marker must remain visible through the task fill; changed pixels: " + changed);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        var name = "gantt-markers-" + (dark ? "dark-" : "light-") + width;
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), prepared.ToPng());
        }
        if (InteractiveChartBrowser.Enabled) {
            await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + chart.ToHtmlFragment() + "</body></html>", width, 400);
            Assert.Equal(1, await session.Page.Locator("[data-cfx-role='gantt-vertical-marker']").CountAsync());
            Assert.Equal(2, await session.Page.Locator("[data-cfx-role='schedule-row-label']").CountAsync());
            Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
            InteractiveChartBrowser.AssertNoConsoleErrors(session);
            if (!string.IsNullOrWhiteSpace(captures)) await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + ".png"), FullPage = true });
        }
    }

    private static string Stamp(DateTime date) => date.ToString("yyyy-MM-dd'T'HH:mm:ss.fff", CultureInfo.InvariantCulture);
}
