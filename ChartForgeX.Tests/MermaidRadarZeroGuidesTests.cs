using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidRadarZeroGuidesTests {
    [Theory]
    [InlineData("circle")]
    [InlineData("polygon")]
    public async Task ZeroTicksHideConcentricGuidesAndKeepAxesAndObservations(string shape) {
        var source = "radar-beta\naxis A,B,C\ncurve signal{17,31,73}\nmax 100\ngraticule " + shape + "\nticks 0";
        var rendered = MermaidRenderer.Render(source);
        Assert.False(rendered.HasErrors);
        Assert.Empty(rendered.Diagnostics);
        var document = Assert.IsType<MermaidRadarDocument>(rendered.Document);
        Assert.Equal(0, document.Ticks);
        Assert.Equal("0", rendered.Artifact!.Metadata["mermaid.ticks"]);
        var chart = document.ToChart(new MermaidRadarRenderOptions { Width = 520, Height = 380 });
        var normal = new MermaidParser().ParseRadar(source.Replace("ticks 0", "ticks 5")).Document!.ToChart(new MermaidRadarRenderOptions { Width = 520, Height = 380 });
        var scene = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var original = normal.Prepare(VisualExportRequest.ForChart(normal).Context);
        Assert.DoesNotContain(scene.Scene.Nodes, node => node.Role == "radar-ring");
        Assert.Equal(3, scene.Scene.Nodes.Count(node => node.Role == "radar-spoke"));
        Assert.Equal(original.Regions.Where(region => region.Role == "radar-point").Select(region => region.Bounds),
            scene.Regions.Where(region => region.Role == "radar-point").Select(region => region.Bounds));
        Assert.Equal(3, XDocument.Parse(scene.ToSvg()).Descendants().Count(e => (string?)e.Attribute("data-cfx-role") == "radar-axis-label"));
        var png = scene.ToPng(); Assert.NotEmpty(png);
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fenced.Diagnostics);
        Assert.Equal(rendered.Artifact.ToSvg(), Assert.Single(fenced.Artifacts).ToSvg());
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + scene.ToSvg() + "</body></html>", 520, 380);
        Assert.Equal(3, await session.Page.Locator("[data-cfx-role='radar-point']").CountAsync());
        Assert.Equal(3, await session.Page.Locator("[data-cfx-role='radar-axis-label']").CountAsync());
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, "radar-zero-" + shape + "-browser.png") });
        File.WriteAllBytes(Path.Combine(captures, "radar-zero-" + shape + "-native.png"), png);
    }
}
