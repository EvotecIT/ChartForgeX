using ChartForgeX.Core;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class StandaloneViewportBrowserTests {
    [BrowserFact]
    public async Task PinchChangesBothSourceWindowsAndCancellationEndsTheGesture() {
        if (!Enabled) return;
        await using var session = await OpenAsync(Page(ChartSeriesKind.Line), 980, 800,
            touch: Engine == HtmlTinkerX.HtmlBrowserEngine.Chromium);
        var page = session.Page;
        await MountAsync(page);
        await page.EvaluateAsync("""
            () => {
                controller.setViewport({x:{minimum:2,maximum:10},y:{minimum:20,maximum:80}});
                const stage=controller.root.querySelector('.cfx-stage'), plot=JSON.parse(controller.root.dataset.cfxPreparedChart).dataProjection.plotBounds;
                const screen=(x,y)=>new DOMPoint(plot.x+plot.width*x,plot.y+plot.height*y).matrixTransform(stage.querySelector('svg').getScreenCTM());
                const capture=stage.setPointerCapture,hasCapture=stage.hasPointerCapture;
                // Cross-engine event proof substitutes only OS pointer capture. The
                // trusted Chromium contacts below also exercise real capture.
                stage.setPointerCapture=()=>{};stage.hasPointerCapture=()=>false;
                const pointer=(type,id,x)=>{const p=screen(x,.5);stage.dispatchEvent(new PointerEvent(type,{pointerId:id,pointerType:'touch',button:0,bubbles:true,clientX:p.x,clientY:p.y}));};
                try {
                    pointer('pointerdown',71,.35);pointer('pointerdown',72,.65);
                    pointer('pointermove',72,.8);pointer('pointercancel',72,.8);pointer('pointerup',71,.35);
                } finally {stage.setPointerCapture=capture;stage.hasPointerCapture=hasCapture;}
            }
            """);
        var window = await ViewportAsync(page);
        Assert.InRange(window.GetProperty("x").GetProperty("maximum").GetDouble() - window.GetProperty("x").GetProperty("minimum").GetDouble(), 5.32, 5.35);
        Assert.InRange(window.GetProperty("y").GetProperty("maximum").GetDouble() - window.GetProperty("y").GetProperty("minimum").GetDouble(), 39.9, 40.1);
        await AssertTicksAsync(page, "y", window.GetProperty("y").GetProperty("minimum").GetDouble(), window.GetProperty("y").GetProperty("maximum").GetDouble());
        if (Engine == HtmlTinkerX.HtmlBrowserEngine.Chromium) {
            await page.EvaluateAsync("()=>controller.setViewport({x:{minimum:2,maximum:10},y:{minimum:20,maximum:80}})");
            var first = await PlotPointerAsync(page, .35, .5);
            var last = await PlotPointerAsync(page, .65, .5);
            var moved = await PlotPointerAsync(page, .8, .5);
            var cdp = await page.Context.NewCDPSessionAsync(page);
            Task TouchAsync(string type, params object[] contacts) => cdp.SendAsync("Input.dispatchTouchEvent",
                new Dictionary<string, object> { ["type"] = type, ["touchPoints"] = contacts });
            var touching = false;
            try {
                await TouchAsync("touchStart", new { id = 1, x = first.X, y = first.Y }, new { id = 2, x = last.X, y = last.Y });
                touching = true;
                await TouchAsync("touchMove", new { id = 1, x = first.X, y = first.Y }, new { id = 2, x = moved.X, y = moved.Y });
                await TouchAsync("touchEnd");
                touching = false;
                var trusted = await ViewportAsync(page);
                Assert.True(trusted.GetProperty("x").GetProperty("maximum").GetDouble() - trusted.GetProperty("x").GetProperty("minimum").GetDouble() < 6);
                Assert.True(trusted.GetProperty("y").GetProperty("maximum").GetDouble() - trusted.GetProperty("y").GetProperty("minimum").GetDouble() < 45);
            } finally { if (touching) await TouchAsync("touchCancel"); await cdp.DetachAsync(); }
        }
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(ChartSeriesKind.Line, false, 980)]
    [InlineData(ChartSeriesKind.Bar, true, 390)]
    [InlineData(ChartSeriesKind.Area, false, 980)]
    [InlineData(ChartSeriesKind.Scatter, true, 390)]
    [InlineData(ChartSeriesKind.Candlestick, false, 980)]
    public async Task DetachedControlsWheelPanBrushAndResetOperateOnSourceWindows(ChartSeriesKind kind, bool dark, int width) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Page(kind, dark: dark), width, 1000);
        var page = session.Page;
        await MountAsync(page);
        await page.Locator("[data-cfx-zoom=in]").ClickAsync();
        AssertWindow((await ViewportAsync(page)).GetProperty("x"), 1.2, 10.8);
        await AssertTicksAsync(page, "x", 1.2, 10.8);
        Assert.Equal(1, (await SceneAsync(page)).GetProperty("matrixScaleX").GetDouble(), 6);
        await page.Locator("[data-cfx-zoom=out]").ClickAsync();
        AssertWindow((await ViewportAsync(page)).GetProperty("x"), 0, 12);

        await page.EvaluateAsync("()=>controller.setViewport({x:{minimum:2,maximum:10},y:{minimum:20,maximum:80}})");
        var pointer = await PlotPointerAsync(page, .5, .5);
        await page.Mouse.MoveAsync(pointer.X, pointer.Y);
        await page.Mouse.WheelAsync(0, -100);
        await page.WaitForFunctionAsync("()=>controller.getXWindow().maximum-controller.getXWindow().minimum<8");
        var wheeled = await ViewportAsync(page);
        AssertWindow(wheeled.GetProperty("y"), 20, 80);
        await AssertTicksAsync(page, "x", wheeled.GetProperty("x").GetProperty("minimum").GetDouble(), wheeled.GetProperty("x").GetProperty("maximum").GetDouble());

        await page.EvaluateAsync("()=>controller.setViewport({x:{minimum:2,maximum:10},y:{minimum:20,maximum:80}})");
        await page.Locator("[data-cfx-mode-button=pan]").ClickAsync();
        var start = await PlotPointerAsync(page, .5, .5);
        var end = await PlotPointerAsync(page, .6, .5);
        await DragAsync(page, start, end);
        var panned = await ViewportAsync(page);
        var minimum = panned.GetProperty("x").GetProperty("minimum").GetDouble();
        var maximum = panned.GetProperty("x").GetProperty("maximum").GetDouble();
        Assert.InRange(minimum, 1.1, 1.3);
        Assert.Equal(8, maximum - minimum, 5);
        AssertWindow(panned.GetProperty("y"), 20, 80);
        await AssertTicksAsync(page, "x", minimum, maximum);
        Assert.Equal(1, (await SceneAsync(page)).GetProperty("matrixScaleX").GetDouble(), 6);

        await page.Locator("[data-cfx-reset]").ClickAsync();
        await page.Locator("[data-cfx-mode-button=brush]").ClickAsync();
        await DragAsync(page, await PlotPointerAsync(page, .25, .2), await PlotPointerAsync(page, .75, .8));
        var brushed = await ViewportAsync(page);
        Assert.InRange(brushed.GetProperty("x").GetProperty("minimum").GetDouble(), 2.9, 3.1);
        Assert.InRange(brushed.GetProperty("x").GetProperty("maximum").GetDouble(), 8.9, 9.1);
        Assert.InRange(brushed.GetProperty("y").GetProperty("minimum").GetDouble(), 19, 21);
        Assert.InRange(brushed.GetProperty("y").GetProperty("maximum").GetDouble(), 79, 81);
        await AssertTicksAsync(page, "x", 2.9, 9.1);
        Assert.True(await page.Locator(".cfx-brush-box").IsHiddenAsync());
        await CaptureAsync(page, "brush-" + kind + "-" + width + "-" + (dark ? "dark" : "light"));
        await page.Locator("[data-cfx-reset]").ClickAsync();
        AssertWindow((await ViewportAsync(page)).GetProperty("x"), 0, 12);
        AssertWindow((await ViewportAsync(page)).GetProperty("y"), 0, 100);
        Assert.True(await page.Locator("[data-cfx-reset]").IsHiddenAsync());
        AssertNoConsoleErrors(session);
    }

    private static async Task<(float X, float Y)> PlotPointerAsync(IPage page, double x, double y) {
        var point = await page.EvaluateAsync<double[]>("""
            ({x,y})=>{
                const svg=controller.root.querySelector('.cfx-stage svg'),plot=JSON.parse(controller.root.dataset.cfxPreparedChart).dataProjection.plotBounds;
                const point=new DOMPoint(plot.x+plot.width*x,plot.y+plot.height*y).matrixTransform(svg.getScreenCTM());
                return [point.x,point.y];
            }
            """, new { x, y });
        return ((float)point[0], (float)point[1]);
    }

    private static async Task DragAsync(IPage page, (float X, float Y) start, (float X, float Y) end) {
        await page.Mouse.MoveAsync(start.X, start.Y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(end.X, end.Y, new MouseMoveOptions { Steps = 6 });
        await page.Mouse.UpAsync();
    }
}
