using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ReferenceStrokeDashTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExplicitSolidLineOverridesInheritedHostDashForLinesAndPaths(bool annotation) {
        var chart = Chart.Create().WithSize(640, 320).WithLegend(false).AddGanttTask("Work", 1, 9);
        var role = annotation ? "annotation-line" : "gantt-now-line";
        if (annotation) {
            chart.AddVerticalLine(4, "Reference");
            chart.Annotations[0].StrokeStyle = new ChartStrokeStyle(width: 4, dash: Array.Empty<double>());
        } else {
            var parsed = new MermaidParser().ParseGantt("gantt\ntodayMarker stroke-width:4px,stroke-dasharray:none\nTask :2026-01-01,6d");
            Assert.Empty(parsed.Diagnostics);
            chart = parsed.Document!.ToChart(new MermaidGanttRenderOptions { Today = new DateTime(2026, 1, 3), Width = 640, Height = 320 }).WithLegend(false);
        }
        var svg = chart.ToSvg();
        if (InteractiveChartBrowser.Enabled) {
            await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>", 640, 360);
            await session.Page.EvaluateAsync("() => document.querySelector('svg').style.strokeDasharray = '8 4'");
            var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
            if (!string.IsNullOrWhiteSpace(captures)) {
                Directory.CreateDirectory(captures);
                var name = "solid-reference-" + (annotation ? "annotation" : "today");
                File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), chart.ToPng());
                await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + ".png"), FullPage = true });
            }
            Assert.Equal("none", await session.Page.Locator("[data-cfx-role='" + role + "']").EvaluateAsync<string>("element => getComputedStyle(element).strokeDasharray"));
            InteractiveChartBrowser.AssertNoConsoleErrors(session);
        }
        var mark = Assert.Single(XDocument.Parse(svg).Descendants(), element => (string?)element.Attribute("data-cfx-role") == role);
        Assert.Equal("none", (string?)mark.Attribute("stroke-dasharray"));
    }
}
