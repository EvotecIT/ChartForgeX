using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Embedded charts preserve touch page scrolling until an explicit chart gesture owns the contact.</summary>
public sealed class InteractiveChartTouchScrollingBrowserTests {
    [BrowserTheory]
    [InlineData(HtmlChartResponsiveLayout.Fit)]
    [InlineData(HtmlChartResponsiveLayout.Readable)]
    [InlineData(HtmlChartResponsiveLayout.Adaptive)]
    public async Task NativeTouchScrollsTheReportAndExplicitBrushRetainsItsGesture(HtmlChartResponsiveLayout layout) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(680, 400).WithTitle("Touch report observations").WithLegend(false)
            .AddLine("Readings", new[] { new ChartPoint(0, 2), new ChartPoint(1, 8), new ChartPoint(2, 5) });
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            options.ResponsiveLayout = layout;
            options.Interaction.Enable(ChartInteractionFeatures.Brush | ChartInteractionFeatures.Pan);
        }), 390, 720, touch: true);
        var page = session.Page;
        await page.EvaluateAsync("""
            ()=>{
                document.body.insertAdjacentHTML('beforeend','<p style="height:1600px">Report continues</p>');
                window.touchTrace=[];
                for(const type of ['pointerdown','pointercancel'])document.querySelector('.cfx-stage').addEventListener(type,event=>
                    touchTrace.push({type,trusted:event.isTrusted,pointerType:event.pointerType}));
            }
            """);
        var cdp = await page.Context.NewCDPSessionAsync(page);
        try {
            Assert.Equal("auto", await page.Locator(".cfx-stage").EvaluateAsync<string>("node=>getComputedStyle(node).touchAction"));
            await DragTouchUpAsync(page, cdp);
            await page.WaitForFunctionAsync("()=>window.scrollY>50");
            Assert.True(await page.EvaluateAsync<bool>("()=>touchTrace.some(event=>event.trusted&&event.pointerType==='touch')"));
            await page.EvaluateAsync("()=>window.scrollTo(0,0)");
            await page.Locator("[data-cfx-mode-button=brush]").ClickAsync();
            Assert.Equal("none", await page.Locator(".cfx-stage").EvaluateAsync<string>("node=>getComputedStyle(node).touchAction"));
            await DragTouchUpAsync(page, cdp);
            Assert.Equal(0, await page.EvaluateAsync<double>("()=>window.scrollY"));
            Assert.False(await page.Locator(".cfx-brush-box").EvaluateAsync<bool>("node=>node.hidden"));
            var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
            if (!string.IsNullOrWhiteSpace(captures)) {
                Directory.CreateDirectory(captures);
                var name = "touch-report-scroll-" + layout.ToString().ToLowerInvariant();
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + ".png") });
                await File.WriteAllTextAsync(Path.Combine(captures, name + ".json"),
                    (await page.EvaluateAsync<JsonElement>("()=>({events:touchTrace,scrollY,brush:document.querySelector('.cfx-interactive-chart').dataset.cfxBrush})")).GetRawText());
            }
        } finally { await cdp.DetachAsync(); }
        AssertNoConsoleErrors(session);
    }

    private static async Task DragTouchUpAsync(IPage page, ICDPSession cdp) {
        var stage = await BoxAsync(page, ".cfx-stage");
        var x = stage.X + stage.Width * .65;
        var y = stage.Y + stage.Height * .8;
        var started = false;
        Task TouchAsync(string type, params object[] contacts) => cdp.SendAsync("Input.dispatchTouchEvent",
            new Dictionary<string, object> { ["type"] = type, ["touchPoints"] = contacts });
        try {
            await TouchAsync("touchStart", new { id = 1, x, y });
            started = true;
            for (var step = 1; step <= 8; step++) {
                await TouchAsync("touchMove", new { id = 1, x, y = y - step * 15 });
                await Task.Delay(20);
            }
            // Release at rest so page inertia cannot overlap the next independent brush gesture.
            await Task.Delay(120);
            await TouchAsync("touchEnd");
            started = false;
        } finally { if (started) await TouchAsync("touchCancel"); }
    }
}
