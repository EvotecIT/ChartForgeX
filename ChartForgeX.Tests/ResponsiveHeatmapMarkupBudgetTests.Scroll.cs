using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class ResponsiveHeatmapMarkupBudgetTests {
    [BrowserTheory]
    [InlineData(ChartHeatmapColumnWindow.Latest, false, false)]
    [InlineData(ChartHeatmapColumnWindow.Latest, false, true)]
    [InlineData(ChartHeatmapColumnWindow.Latest, true, false)]
    [InlineData(ChartHeatmapColumnWindow.Squeeze, false, false)]
    public async Task PhoneViewportHonorsColumnWindowAndKeepsItsEdgeThroughResize(ChartHeatmapColumnWindow window, bool reversed, bool dark) {
        var chart = Fixture(12).WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithHeatmapColumnWindow(window);
        chart.Options.XAxis.Reversed = reversed;
        var fragment = new HtmlResponsiveChartRenderer().RenderFragment(chart, 240, 1600);
        Assert.Equal(window, chart.Options.HeatmapColumnWindow);
        await using var session = await InteractiveChartBrowser.OpenAsync("<html><body style='margin:0'><div id='panel' style='width:390px'>" + fragment + "</div></body></html>", 390, 1000);
        var page = session.Page;
        await page.WaitForFunctionAsync("()=>Number(document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutWidth)===390");
        if (window == ChartHeatmapColumnWindow.Latest && !reversed && Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } capture) {
            Directory.CreateDirectory(capture);
            await page.Locator("[data-cfx-responsive-chart]").ScreenshotAsync(new() { Path = Path.Combine(capture, "heatmap-latest-390-" + (dark ? "dark" : "light") + ".png") });
        }
        foreach (var width in new[] { 390, 1024, 390 }) {
            await ResizeHeatmapPanel(page, width);
            var column = window == ChartHeatmapColumnWindow.Latest ? 29 : 0;
            var bounds = await page.EvaluateAsync<double[]>("""
                column=>{const h=document.querySelector('[data-cfx-responsive-chart]'),host=h.getBoundingClientRect(),
                  gutter=h.querySelector('[data-cfx-role=matrix-sticky-labels]>rect').getBoundingClientRect(),
                  cell=h.querySelector(`[data-cfx-role=heatmap-cell][data-cfx-column='${column}']`).getBoundingClientRect();
                  return [h.scrollLeft,h.scrollWidth-h.clientWidth,cell.left,cell.right,gutter.right,host.right,gutter.left,host.left,document.documentElement.scrollWidth];}
                """, column);
            Assert.InRange(bounds[2], bounds[4] - 1, bounds[5]);
            Assert.InRange(bounds[3], bounds[4], bounds[5] + 1);
            Assert.Equal(bounds[7], bounds[6], .1);
            Assert.InRange(bounds[8], 0, width);
            Assert.Equal(window == ChartHeatmapColumnWindow.Latest && !reversed ? bounds[1] : 0, bounds[0], 1);
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(ChartHeatmapColumnWindow.Latest)]
    [InlineData(ChartHeatmapColumnWindow.Squeeze)]
    public async Task FirstOverflowHonorsWindowAndReaderKeepsTheirColumnAcrossResizeAndPrint(ChartHeatmapColumnWindow window) {
        var fragment = new HtmlResponsiveChartRenderer().RenderFragment(Fixture(12).WithHeatmapColumnWindow(window), 240, 1600);
        await using var session = await InteractiveChartBrowser.OpenAsync("<html><body style='margin:0'><div id='panel' style='width:1024px'>" + fragment + "</div></body></html>", 1024, 1000);
        var page = session.Page;
        await page.WaitForFunctionAsync("()=>Number(document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutWidth)===1024");
        await ResizeHeatmapPanel(page, 390);
        var initial = await page.EvaluateAsync<double[]>("()=>{const h=document.querySelector('[data-cfx-responsive-chart]');return [h.scrollLeft,h.scrollWidth-h.clientWidth]}");
        Assert.True(initial[1] > 0);
        Assert.Equal(window == ChartHeatmapColumnWindow.Latest ? initial[1] : 0, initial[0], 1);
        await page.EvaluateAsync("async()=>{const h=document.querySelector('[data-cfx-responsive-chart]');h.scrollLeft=80;await new Promise(requestAnimationFrame);await new Promise(requestAnimationFrame);}");
        const string firstColumn = """
            ()=>{const h=document.querySelector('[data-cfx-responsive-chart]'),spec=JSON.parse(h.querySelector('[data-cfx-spec]').textContent),
              cell=h.querySelector('[data-cfx-role=heatmap-cell-shape]');return h.scrollLeft/(Number(cell.getAttribute('width'))+spec.matrix[1]);}
            """;
        var first = await page.EvaluateAsync<double>(firstColumn);
        Assert.True(first > 0);
        foreach (var width in new[] { 480, 1024, 390 }) await ResizeHeatmapPanel(page, width);
        Assert.Equal(first, await page.EvaluateAsync<double>(firstColumn), .05);
        await page.EmulateMediaAsync(new() { Media = Media.Print });
        await page.WaitForFunctionAsync("()=>document.querySelector('svg').getAttribute('width')==='390'");
        await page.EmulateMediaAsync(new() { Media = Media.Screen });
        await page.WaitForFunctionAsync("()=>Number(document.querySelector('svg').getAttribute('width'))>390");
        Assert.Equal(first, await page.EvaluateAsync<double>(firstColumn), .05);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    private static async Task ResizeHeatmapPanel(IPage page, int width) {
        await page.SetViewportSizeAsync(width, 1000);
        await page.EvaluateAsync("w=>document.querySelector('#panel').style.width=w+'px'", width);
        await page.WaitForFunctionAsync("w=>Number(document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutWidth)===w", width);
    }
}
