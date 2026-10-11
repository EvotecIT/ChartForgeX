using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidRemainingTypographyBoundsTests {
    [Theory]
    [InlineData(false, 320)]
    [InlineData(false, 960)]
    [InlineData(true, 320)]
    [InlineData(true, 960)]
    public async Task MaximumBodyFontKeepsBlockCellAndEdgeLabelsVisible(bool graphite, int width) {
        var theme = ChartTheme.Light();
        theme.UseGraphiteLayout = graphite; theme.FontFamily = "Arial"; theme.DataLabelFontSize = 256;
        var block = BlockLayoutBlock.Create().WithSize(width, 300).WithCard(false).WithTheme(theme).WithColumns(2)
            .AddItem("a", "API").AddItem("b", "Store").AddEdge("a", "b", "link");
        var svg = block.ToSvg();
        var labels = XDocument.Parse(svg).Descendants().Where(element => element.Name.LocalName == "text").Select(element => element.Value).ToArray();
        foreach (var caption in new[] { "API", "Store", "link" }) Assert.Contains(caption, labels);
        Assert.NotEmpty(block.ToPng());
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>", width, 300);
        await Capture(session, block.ToPng(), "block-font-ceiling-" + graphite + "-" + width);
        var bounds = await session.Page.EvaluateAsync<string>("() => JSON.stringify([...document.querySelectorAll('svg text,[data-cfx-role=block-layout-node]')].map(n=>({label:n.textContent,bounds:n.getBoundingClientRect().toJSON()})))");
        Assert.True(await session.Page.EvaluateAsync<bool>("() => { const nodes=[...document.querySelectorAll('[data-cfx-role=block-layout-node]')]; return [...document.querySelectorAll('svg text')].filter(t=>['API','Store'].includes(t.textContent)).every(t=>{ const b=t.getBoundingClientRect(); return b.width>0 && nodes.some(n=>{const r=n.getBoundingClientRect(); return b.left>=r.left && b.right<=r.right && b.top>=r.top && b.bottom<=r.bottom;}); }); }"), bounds);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false, 320)]
    [InlineData(false, 960)]
    [InlineData(true, 320)]
    [InlineData(true, 960)]
    public async Task MaximumSourceFontRetainsChartTitleAndContent(bool graphite, int width) {
        var parsed = new MermaidParser().ParsePie("%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':256}}}%%\npie\ntitle Visible\n\"A\": 2\n\"B\": 1");
        Assert.Empty(parsed.Diagnostics);
        var chart = parsed.Document!.ToChart(new MermaidPieRenderOptions { Width = width, Height = 460 });
        chart.Options.Theme.UseGraphiteLayout = graphite;
        var svg = chart.ToSvg();
        var title = Assert.Single(XDocument.Parse(svg).Descendants(), element => element.Name.LocalName == "text" && element.Value == "Visible");
        Assert.True(double.Parse(title.Attribute("font-size")!.Value, CultureInfo.InvariantCulture) > 0);
        var png = chart.ToPng(); Assert.NotEmpty(png);
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>", width, 460);
        Assert.True(await session.Page.EvaluateAsync<bool>("() => { const t=[...document.querySelectorAll('svg text')].find(t=>t.textContent==='Visible'); const b=t.getBoundingClientRect(); return b.width>0 && b.left>=0 && b.right<=innerWidth && b.top>=0 && b.bottom<230; }"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        await Capture(session, png, "pie-title-font-ceiling-" + graphite + "-" + width);
    }

    private static async Task Capture(HtmlTinkerX.HtmlBrowserSession session, byte[] png, string name) {
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png"), FullPage = true });
        File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), png);
    }
}
