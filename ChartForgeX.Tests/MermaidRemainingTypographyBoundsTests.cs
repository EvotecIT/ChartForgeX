using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidRemainingTypographyBoundsTests {
    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(false, 320, false)]
    [InlineData(false, 960, false)]
    [InlineData(true, 320, false)]
    [InlineData(true, 960, false)]
    [InlineData(false, 320, true)]
    [InlineData(false, 960, true)]
    [InlineData(true, 320, true)]
    [InlineData(true, 960, true)]
    public async Task MaximumBodyFontKeepsBlockCellAndEdgeLabelsVisible(bool dark, int width, bool circle) {
        var theme = VisualTheme.Report().WithFontFamily("Arial"); theme = theme.Configure(builder => { builder.Typography = new VisualTypography(family: builder.Typography.Family, titleSize: builder.Typography.TitleSize, subtitleSize: builder.Typography.SubtitleSize, axisSize: builder.Typography.AxisSize, legendSize: builder.Typography.LegendSize, dataLabelSize: 256, axisTitleSize: builder.Typography.AxisTitleSize, scalarValueSize: builder.Typography.ScalarValueSize, centerValueSize: builder.Typography.CenterValueSize, monospaceFamily: builder.Typography.MonospaceFamily); });
        var block = BlockLayoutBlock.Create().WithSize(width, 300).WithCard(false).WithTheme(theme, dark ? VisualThemeMode.Dark : VisualThemeMode.Light).WithColumns(2)
            .AddItem("a", "API").AddItem("b", "Store", shape: circle ? BlockLayoutShape.Circle : BlockLayoutShape.Rectangle).AddEdge("a", "b", "link");
        var svg = block.ToSvg();
        var labels = XDocument.Parse(svg).Descendants().Where(element => element.Name.LocalName == "text").Select(element => element.Value).ToArray();
        foreach (var caption in new[] { "API", "Store", "link" }) Assert.Contains(caption, labels);
        Assert.NotEmpty(block.ToPng());
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>", width, 300);
        await Capture(session, block.ToPng(), "block-font-ceiling-" + dark + "-" + width + (circle ? "-circle" : ""));
        var bounds = await session.Page.EvaluateAsync<string>("() => JSON.stringify([...document.querySelectorAll('svg text,[data-cfx-role=block-layout-node]')].map(n=>({label:n.textContent,bounds:n.getBoundingClientRect().toJSON()})))");
        Assert.True(await session.Page.EvaluateAsync<bool>("() => { const nodes=[...document.querySelectorAll('[data-cfx-role=block-layout-node]')]; return [...document.querySelectorAll('svg text')].filter(t=>['API','Store'].includes(t.textContent)).every(t=>{ const b=t.getBoundingClientRect(); return b.width>0 && nodes.some(n=>{const r=n.getBoundingClientRect(); if(n.localName==='circle') { const dx=Math.max(Math.abs(b.left-r.x-r.width/2),Math.abs(b.right-r.x-r.width/2)),dy=Math.max(Math.abs(b.top-r.y-r.height/2),Math.abs(b.bottom-r.y-r.height/2)); return dx*dx+dy*dy<=r.width*r.width/4; } return b.left>=r.left && b.right<=r.right && b.top>=r.top && b.bottom<=r.bottom;}); }); }"), bounds);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(false, 320)]
    [InlineData(false, 960)]
    [InlineData(true, 320)]
    [InlineData(true, 960)]
    public async Task MaximumSourceFontRetainsChartTitleAndContent(bool dark, int width) {
        var parsed = new MermaidParser().ParsePie("%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':256}}}%%\npie\ntitle Visible\n\"A\": 2\n\"B\": 1");
        Assert.Empty(parsed.Diagnostics);
        var chart = parsed.Document!.ToChart(new MermaidPieRenderOptions { Width = width, Height = 460 }).WithThemeMode(dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        var svg = chart.ToSvg();
        var title = Assert.Single(XDocument.Parse(svg).Descendants(), element => element.Name.LocalName == "text" && element.Value == "Visible");
        Assert.True(double.Parse(title.Attribute("font-size")!.Value, CultureInfo.InvariantCulture) > 0);
        var png = chart.ToPng(); Assert.NotEmpty(png);
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>", width, 460);
        Assert.True(await session.Page.EvaluateAsync<bool>("() => { const t=[...document.querySelectorAll('svg text')].find(t=>t.textContent==='Visible'); const b=t.getBoundingClientRect(); return b.width>0 && b.left>=0 && b.right<=innerWidth && b.top>=0 && b.bottom<230; }"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        await Capture(session, png, "pie-title-font-ceiling-" + dark + "-" + width);
    }

    private static async Task Capture(HtmlTinkerX.HtmlBrowserSession session, byte[] png, string name) {
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png"), FullPage = true });
        File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), png);
    }
}
