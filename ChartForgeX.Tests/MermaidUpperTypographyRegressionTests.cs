using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualBlocks;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidUpperTypographyRegressionTests {
    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(false, 2)]
    [InlineData(false, 3)]
    [InlineData(true, 2)]
    [InlineData(true, 3)]
    public async Task NativeVennLongSetAndRegionCaptionsShareMeasuredSpace(bool dark, int count) {
        var theme = VisualTheme.Report().WithFontFamily("Arial"); theme = theme.Configure(builder => { builder.Typography = new VisualTypography(family: builder.Typography.Family, titleSize: builder.Typography.TitleSize, subtitleSize: builder.Typography.SubtitleSize, axisSize: builder.Typography.AxisSize, legendSize: builder.Typography.LegendSize, dataLabelSize: 256, axisTitleSize: builder.Typography.AxisTitleSize, scalarValueSize: builder.Typography.ScalarValueSize, centerValueSize: builder.Typography.CenterValueSize, monospaceFamily: builder.Typography.MonospaceFamily); });
        var block = VennDiagramBlock.Create().WithSize(320, 460).WithTheme(theme, dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .AddSet("A", "A collection", 60).AddSet("B", "B collection", 55)
            .AddIntersection(new[] { "A", "B" }, "Both", 18).AddText(new[] { "A" }, "note", "Data");
        var expected = new List<string> { "A collection", "B collection", "Both", "Data" };
        if (count == 3) {
            block.AddSet("C", "C collection", 40).AddIntersection(new[] { "A", "B", "C" }, "All", 10);
            expected.AddRange(new[] { "C collection", "All" });
        }
        var svg = block.ToSvg(); var png = block.ToPng();
        var labels = XDocument.Parse(svg).Descendants().Where(element => element.Name.LocalName == "text").Select(element => element.Value).ToArray();
        foreach (var caption in expected) Assert.Contains(caption, labels);
        Assert.NotEmpty(png);
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>", 320, 460);
        Assert.True(await session.Page.EvaluateAsync<bool>("() => { const boxes=[...document.querySelectorAll('svg text')].map(t=>t.getBoundingClientRect()); return boxes.every((a,i)=>a.width>0 && a.left>=-.01 && a.right<=320.01 && a.top>=-.01 && a.bottom<=460.01 && boxes.slice(i+1).every(b=>Math.min(a.right,b.right)-Math.max(a.left,b.left)<=.01 || Math.min(a.bottom,b.bottom)-Math.max(a.top,b.top)<=.01)); }"));
        Assert.True(await session.Page.EvaluateAsync<bool>("() => { const ids={'A collection':['A'],'B collection':['B'],'C collection':['C'],'Both':['A','B'],'All':['A','B','C'],'Data':['A']}; return [...document.querySelectorAll('svg text')].every(t=>{ const b=t.getBoundingClientRect(); return ids[t.textContent].every(id=>{ const c=document.querySelector('[data-set-id='+id+']').getBoundingClientRect(), x=c.x+c.width/2,y=c.y+c.height/2,r=c.width/2; return [[b.left,b.top],[b.right,b.top],[b.left,b.bottom],[b.right,b.bottom]].every(([px,py])=>(px-x)**2+(py-y)**2<=r*r+.1); }); }); }"), "Relocation must retain the caption inside its referenced sets.");
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var name = "venn-long-captions-" + dark + "-" + count;
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png"), FullPage = true });
        File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), png);
    }

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
    public async Task MaximumFontRetainsLegendAndVennCaptionsWithinTheViewport(bool dark, int width, bool venn) {
        var source = "%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':256}}}%%\n" +
            (venn ? "venn-beta\nset API [\"API\"] : 60\nset UI [\"UI\"] : 55\nunion API,UI [\"Shared\"] : 18\ntext API note [\"Data\"]"
                : "pie\n\"API\": 2\n\"UI\": 1");
        var parsed = new MermaidParser().Parse(source);
        Assert.Empty(parsed.Diagnostics);
        string svg; byte[] png;
        if (venn) {
            var block = Assert.IsType<MermaidVennDocument>(parsed.Document).ToVennDiagramBlock();
            block.WithSize(width, 460).WithThemeMode(dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
            svg = block.ToSvg(); png = block.ToPng();
        } else {
            var chart = Assert.IsType<MermaidPieDocument>(parsed.Document).ToChart(new MermaidPieRenderOptions { Width = width, Height = 460 }).WithThemeMode(dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
            svg = chart.ToSvg(); png = chart.ToPng();
        }
        var labels = XDocument.Parse(svg).Descendants().Where(element => element.Name.LocalName == "text").Select(element => element.Value).ToArray();
        foreach (var caption in venn ? new[] { "API", "UI", "Shared", "Data" } : new[] { "API", "UI" }) Assert.Contains(caption, labels);
        Assert.NotEmpty(png);
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>", width, 460);
        Assert.True(await session.Page.EvaluateAsync<bool>("() => [...document.querySelectorAll('svg text')].every(t=>{ const b=t.getBoundingClientRect(); return b.width>0 && b.left>=-.01 && b.right<=innerWidth+.01 && b.top>=-.01 && b.bottom<=460.01; })"));
        if (venn) Assert.True(await session.Page.EvaluateAsync<bool>("() => { const boxes=[...document.querySelectorAll('svg text')].filter(t=>['API','UI','Shared','Data'].includes(t.textContent)).map(t=>t.getBoundingClientRect()); return boxes.every((a,i)=>boxes.slice(i+1).every(b=>Math.min(a.right,b.right)-Math.max(a.left,b.left)<=.01 || Math.min(a.bottom,b.bottom)-Math.max(a.top,b.top)<=.01)); }"), "Venn captions must remain separate as well as inside the viewport.");
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            var name = (venn ? "venn" : "pie-legend") + "-font-ceiling-" + dark + "-" + width;
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png"), FullPage = true });
            File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), png);
        }
    }
}
