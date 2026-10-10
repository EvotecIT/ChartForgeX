using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class NumericRadialPrecisionBrowserTests {
    [Theory]
    [InlineData(true, 0, false)]
    [InlineData(true, 1, false)]
    [InlineData(true, 2, false)]
    [InlineData(true, 0, true)]
    [InlineData(true, 1, true)]
    [InlineData(true, 2, true)]
    [InlineData(false, 0, false)]
    [InlineData(false, 1, false)]
    [InlineData(false, 2, false)]
    [InlineData(false, 0, true)]
    [InlineData(false, 1, true)]
    [InlineData(false, 2, true)]
    public async Task EncodedPrecisionFactsKeepRawKeyboardReadoutsWithoutPointerAcquisition(bool bars, int range, bool caption) {
        if (!Enabled) return;
        var chart = NumericRadialSeriesTests.Add(Chart.Create().WithSize(600, 440).WithHeader(false)
            .WithLegend(false).WithAxes(false).WithGrid(false).WithDataLabels(caption)
            .WithDataLabelPlacement(ChartDataLabelPlacement.Outside), bars, "Observed", new ChartPoint(1, 1), new ChartPoint(2, 100000000));
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            options.Tooltip.Mode = HtmlChartTooltipMode.Single;
            options.Tooltip.Range = range == 0 ? HtmlChartTooltipRange.Exact : range == 1 ? HtmlChartTooltipRange.WithinDistance(20) : HtmlChartTooltipRange.Nearest;
        }), 700, 520);
        var page = session.Page;
        var stem = $"encoded-precision-{(bars ? "bar" : "column")}-{range}{(caption ? "-caption" : "")}";
        var target = page.Locator(Point(0, 0));
        var id = await target.GetAttributeAsync("data-cfx-target-id");
        await page.EvaluateAsync("() => { window.cfxPrecisionEvents=[]; const root=document.querySelector('.cfx-interactive-chart'); for(const type of ['cfxhover','cfxselect','cfxtooltip'])root.addEventListener(type,event=>window.cfxPrecisionEvents.push({type,detail:event.detail})); }");
        var location = await target.EvaluateAsync<float[]>("""
            node=>{
                const root=node.closest('.cfx-interactive-chart'),prepared=JSON.parse(root.dataset.cfxPreparedChart);
                const region=prepared.regions.find(region=>region.id===node.dataset.cfxSourceId);
                const point=new DOMPoint(region.x+region.width/2,region.y+region.height/2).matrixTransform(node.ownerSVGElement.getScreenCTM());
                return [point.x,point.y];
            }
            """);
        if (caption) {
            var label = await page.Locator("[data-cfx-label-for='series-0-point-0'] text").First.BoundingBoxAsync()
                ?? throw new InvalidOperationException("Missing painted source caption.");
            location = new[] { (float)(label.X + label.Width * .35), (float)(label.Y + label.Height * .45) };
        }
        await page.Mouse.MoveAsync(location[0], location[1]);
        await page.Mouse.ClickAsync(location[0], location[1]);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(Path.Combine(directory, stem + ".svg"), chart.ToSvg());
            await File.WriteAllBytesAsync(Path.Combine(directory, stem + ".png"), chart.ToPng());
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, stem + "-browser.png") });
            var state = await target.EvaluateAsync<JsonElement>("node=>({status:node.dataset.cfxGeometryStatus,value:node.dataset.cfxY,selected:node.getAttribute('aria-selected'),paths:Array.from(node.querySelectorAll('path')).map(path=>({d:path.getAttribute('d'),box:(()=>{const b=path.getBBox();return{x:b.x,y:b.y,width:b.width,height:b.height}})()})),hitAreas:node.querySelectorAll('[data-cfx-browser-hit-area]').length,events:window.cfxPrecisionEvents})");
            await File.WriteAllTextAsync(Path.Combine(directory, stem + "-state.json"), JsonSerializer.Serialize(new { bars, range, caption, location, state }, new JsonSerializerOptions { WriteIndented = true }));
        }
        Assert.Equal(0, await page.EvaluateAsync<int>("id=>window.cfxPrecisionEvents.filter(event=>event.detail.target?.targetId===id).length", id));
        Assert.NotEqual("true", await target.GetAttributeAsync("aria-selected"));
        Assert.Equal("precision-collapse", await target.GetAttributeAsync("data-cfx-geometry-status"));
        Assert.Equal(0, await target.Locator("[data-cfx-browser-hit-area]").CountAsync());
        await MoveAwayAsync(page);
        if (await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-tooltip-pinned") == "true")
            await page.Locator("[data-cfx-reset]").ClickAsync();
        await target.FocusAsync(); Assert.Contains("Observed", await TooltipTextAsync(page));
        Assert.Equal("1", await target.GetAttributeAsync("data-cfx-y"));
        await page.Keyboard.PressAsync("Space"); Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        Assert.Equal("true", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-tooltip-pinned"));
        await page.Locator("[data-cfx-reset]").ClickAsync(); await MoveAwayAsync(page);
        var large = page.Locator(Point(0, 1));
        var mark = NumericRadialSeriesTests.Marks(chart.Prepare(VisualExportRequest.ForChart(chart).Context).Scene).Last();
        var angle = mark.Start + mark.Sweep / 2; var radius = (mark.Inner + mark.Outer) / 2;
        var painted = await large.EvaluateAsync<float[]>("(node,p)=>{const point=new DOMPoint(p.x,p.y).matrixTransform(node.ownerSVGElement.getScreenCTM());return [point.x,point.y];}",
            new { x = mark.Cx + Math.Cos(angle) * radius, y = mark.Cy + Math.Sin(angle) * radius });
        await page.Mouse.MoveAsync(painted[0], painted[1], new MouseMoveOptions { Steps = 4 });
        Assert.NotEqual(string.Empty, await TooltipTextAsync(page));
        Assert.Contains((await large.GetAttributeAsync("data-cfx-target-id"))!,
            (await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-hover-key"))!);
        Assert.Equal("100000000", await large.GetAttributeAsync("data-cfx-y"));
        await page.Mouse.ClickAsync(painted[0], painted[1]);
        Assert.Equal("true", await large.GetAttributeAsync("aria-selected"));
        AssertNoConsoleErrors(session);
        if (!string.IsNullOrWhiteSpace(directory)) {
            await File.WriteAllTextAsync(Path.Combine(directory, stem + "-console.json"), JsonSerializer.Serialize(session.ConsoleLog, new JsonSerializerOptions { WriteIndented = true }));
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, stem + "-painted-positive.png") });
        }
    }
}
