using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects native radial and schedule captions at the accepted source font ceiling.</summary>
public sealed class MermaidPolarScheduleTypographyTests {
    [Theory]
    [InlineData("radar", 320, false, 0)]
    [InlineData("radar", 960, false, 0)]
    [InlineData("radar", 320, true, 0)]
    [InlineData("radar", 960, true, 0)]
    [InlineData("gantt", 320, false, 0)]
    [InlineData("gantt", 960, false, 0)]
    [InlineData("gantt", 320, true, 45)]
    [InlineData("gantt", 960, true, 45)]
    public async Task MaximumSourceFontRetainsMeasuredAxesRowsAndMarkCaptions(string family, int width, bool graphite, double angle) {
        var body = family == "radar" ? "radar-beta\naxis API,UI,Store\ncurve signal{17,31,73}\nmax 100\nticks 4"
            : "gantt\ndateFormat YYYY-MM-DD\naxisFormat %m-%d\nsection Delivery\nBuild :a,2026-03-02,3d\nReview :b,2026-03-05,3d";
        var rendered = MermaidRenderer.Render("%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':256}}}%%\n" + body);
        Assert.False(rendered.HasErrors);
        var chart = Assert.IsType<Chart>(rendered.Artifact!.Model);
        chart.WithSize(width, 460); chart.Options.Theme.UseGraphiteLayout = graphite;
        if (family == "gantt") chart.WithDataLabels();
        var authoredTickSize = chart.Options.Theme.TickLabelFontSize;
        chart.Options.XAxis.LabelAngle = angle;
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var svg = prepared.ToSvg(); var labels = Texts(svg);
        if (family == "radar") {
            Assert.Equal(3, labels.Count(element => Role(element) == "radar-axis-label"));
            foreach (var expected in new[] { "API", "UI", "Store" }) Assert.Contains(labels, element => element.Value == expected);
            Assert.Contains(labels, element => Role(element) == "radar-ring-label");
        } else {
            Assert.Equal(2, labels.Count(element => Role(element) == "schedule-row-label"));
            Assert.True(labels.Count(element => Role(element) == "schedule-tick-label") >= 2);
            foreach (var expected in new[] { "Delivery / Build", "Delivery / Review" }) Assert.Contains(labels, element => element.Value == expected);
            Assert.Equal(2, labels.Count(element => Role(element) == "data-label"));
        }
        Assert.Equal(authoredTickSize, chart.Options.Theme.TickLabelFontSize);
        Assert.Equal(256, chart.Options.Theme.DataLabelFontSize);
        Assert.NotEmpty(prepared.ToPng());
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><body style='margin:0'>" + svg, width, 460);
        Assert.True(await session.Page.EvaluateAsync<bool>("() => [...document.querySelectorAll('svg text')].every(t=>{const b=t.getBoundingClientRect();return b.width>0 && b.height>0 && b.left>=-.1 && b.right<=innerWidth+.1 && b.top>=-.1 && b.bottom<=innerHeight+.1;})"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures); var name = family + "-font-ceiling-" + width + "-" + graphite;
        File.WriteAllBytes(Path.Combine(captures, name + ".native.png"), prepared.ToPng());
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + ".browser.png") });
    }

    [Theory]
    [InlineData(320)]
    [InlineData(960)]
    public void NativePolarUsesTheSameMeasuredAxisAndDataCaptionBounds(int width) {
        var chart = Chart.Create().WithSize(width, 360).WithPngFont("Arial")
            .AddPolar("Signal", new[] { new ChartForgeX.Primitives.ChartPoint(0, 40), new ChartForgeX.Primitives.ChartPoint(Math.PI / 2, 60), new ChartForgeX.Primitives.ChartPoint(Math.PI, 80) })
            .WithDataLabels();
        chart.Options.TickLabelStyle.FontSize = 256; chart.Options.DataLabelStyle.FontSize = 256;
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context); var labels = Texts(prepared.ToSvg());
        Assert.True(labels.Count(element => Role(element) == "polar-angle-label") >= 4);
        Assert.Equal(prepared.Regions.Count(region => region.Role == "polar-angle-label"), labels.Count(element => Role(element) == "polar-angle-label"));
        Assert.Contains(labels, element => Role(element) == "polar-radius-label");
        Assert.Contains(labels, element => Role(element) == "polar-data-label");
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(ChartSeriesKind.Gantt)]
    [InlineData(ChartSeriesKind.Timeline)]
    [InlineData(ChartSeriesKind.GanttLane)]
    [InlineData(ChartSeriesKind.StateTimeline)]
    public void NativeScheduleSiblingsFitTheirFixedTextStrips(ChartSeriesKind kind) {
        var chart = Chart.Create().WithSize(360, 360).WithPngFont("Arial").WithLegend(false).WithDataLabels();
        switch (kind) {
            case ChartSeriesKind.Gantt: chart.AddGanttTask("API", 1, 3, .5).WithGanttToday(2d); break;
            case ChartSeriesKind.Timeline: chart.AddTimelineItem("API", DateTime.FromOADate(1), DateTime.FromOADate(3)); break;
            case ChartSeriesKind.GanttLane: chart.AddGanttLane("API", new[] { new ChartGanttLaneItem(1, 3, "up", "Save") }, "Services", "99%").WithGanttToday(2d); break;
            case ChartSeriesKind.StateTimeline: chart.AddStateTimelineLane("API", new[] { new ChartStateTimelineSegment(1, 3, "up") }, "99%", "Services"); break;
        }
        if (kind is ChartSeriesKind.GanttLane or ChartSeriesKind.StateTimeline) chart.Options.LaneSummaryHeader = "Status";
        chart.Options.TickLabelStyle.FontSize = 256; chart.Options.DataLabelStyle.FontSize = 256;
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context); var labels = Texts(prepared.ToSvg());
        Assert.Contains(labels, element => Role(element) == "schedule-row-label" && element.Value == "API");
        Assert.True(labels.Count(element => Role(element) == "schedule-tick-label") >= 2);
        if (kind is ChartSeriesKind.GanttLane or ChartSeriesKind.StateTimeline)
            foreach (var expected in new[] { "Status", "99%", "Services" }) Assert.Contains(labels, element => element.Value == expected);
        if (kind is ChartSeriesKind.Gantt or ChartSeriesKind.GanttLane) Assert.Contains(labels, element => Role(element) == "gantt-now-label");
        Assert.NotEmpty(prepared.ToPng());
    }

    private static XElement[] Texts(string svg) => XDocument.Parse(svg).Descendants().Where(element => element.Name.LocalName == "text").ToArray();
    private static string? Role(XElement element) => element.AncestorsAndSelf().Select(item => (string?)item.Attribute("data-cfx-role")).FirstOrDefault(role => role != null);
}
