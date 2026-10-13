using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class IconCaptionFontSizeTests {
    [Theory]
    [InlineData(11)]
    [InlineData(32)]
    [InlineData(256)]
    public async Task SourceTypographyRetainsIconCaptionAndItsSharedFootprint(int size) {
        var result = MermaidRenderer.Render("%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':" + size + "}}}%%\nflowchart LR\napi[Payments API]");
        Assert.False(result.HasErrors);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        var node = Assert.Single(chart.Nodes);
        chart.WithNodeDisplay(node.Id, TopologyNodeDisplayMode.Icon);
        var options = new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false, IncludeIconLabels = true };
        var svg = chart.ToSvg(options); var xml = XDocument.Parse(svg);
        var label = Assert.Single(xml.Descendants(), element => element.Name.LocalName == "text" && element.Ancestors().Any(parent =>
            (string?)parent.Attribute("data-cfx-role") == "topology-node-label"));
        Assert.Equal("Payments API", label.Value);
        var plate = Assert.Single(xml.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "topology-node-icon-label");
        Assert.InRange((double)plate.Attribute("height")!, 15, 50);
        var png = chart.ToPng(options); Assert.NotEmpty(png);
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>", 800, 600);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            var stem = Path.Combine(directory, "icon-caption-" + size);
            File.WriteAllText(stem + ".svg", svg); File.WriteAllBytes(stem + "-native.png", png);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + "-browser.png", FullPage = true });
        }
    }
}
