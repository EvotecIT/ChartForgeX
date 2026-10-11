using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualBlocks;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidUpperTypographyRegressionTests {
    [Theory]
    [InlineData(false, 320, false)]
    [InlineData(false, 960, false)]
    [InlineData(true, 320, false)]
    [InlineData(true, 960, false)]
    [InlineData(false, 320, true)]
    [InlineData(false, 960, true)]
    [InlineData(true, 320, true)]
    [InlineData(true, 960, true)]
    public async Task MaximumFontRetainsLegendAndVennCaptionsWithinTheViewport(bool graphite, int width, bool venn) {
        var source = "%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':256}}}%%\n" +
            (venn ? "venn-beta\nset API [\"API\"] : 60\nset UI [\"UI\"] : 55\nunion API,UI [\"Shared\"] : 18\ntext API note [\"Data\"]"
                : "pie\n\"API\": 2\n\"UI\": 1");
        var parsed = new MermaidParser().Parse(source);
        Assert.Empty(parsed.Diagnostics);
        string svg; byte[] png;
        if (venn) {
            var block = Assert.IsType<MermaidVennDocument>(parsed.Document).ToVennDiagramBlock();
            block.WithSize(width, 460); block.Options.Theme.UseGraphiteLayout = graphite;
            svg = block.ToSvg(); png = block.ToPng();
        } else {
            var chart = Assert.IsType<MermaidPieDocument>(parsed.Document).ToChart(new MermaidPieRenderOptions { Width = width, Height = 460 });
            chart.Options.Theme.UseGraphiteLayout = graphite;
            svg = chart.ToSvg(); png = chart.ToPng();
        }
        var labels = XDocument.Parse(svg).Descendants().Where(element => element.Name.LocalName == "text").Select(element => element.Value).ToArray();
        foreach (var caption in venn ? new[] { "API", "UI", "Shared", "Data" } : new[] { "API", "UI" }) Assert.Contains(caption, labels);
        Assert.NotEmpty(png);
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>", width, 460);
        Assert.True(await session.Page.EvaluateAsync<bool>("() => [...document.querySelectorAll('svg text')].every(t=>{ const b=t.getBoundingClientRect(); return b.width>0 && b.left>=-.01 && b.right<=innerWidth+.01 && b.top>=-.01 && b.bottom<=460.01; })"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            var name = (venn ? "venn" : "pie-legend") + "-font-ceiling-" + graphite + "-" + width;
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png"), FullPage = true });
            File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), png);
        }
    }
}
