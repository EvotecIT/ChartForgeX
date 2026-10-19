using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class SvgHostProjectionBrowserTests {
    [BrowserTheory]
    [Trait("BrowserSuite", "Compatibility")]
    [InlineData(1, "xMidYMid meet", false)]
    [InlineData(1, "xMidYMid meet", true)]
    [InlineData(1.5, "xMidYMid meet", false)]
    [InlineData(1.5, "xMidYMid meet", true)]
    [InlineData(1.5, "xMidYMid slice", false)]
    [InlineData(1.5, "none", false)]
    public async Task HostZoomKeepsPartialPointerReadoutsFocusAndGuidesOnActualSvgMarks(double zoom, string aspect, bool decorated) {
        if (!Enabled) return;
        await using var session = await OpenAsync(ProjectionChart().ToInteractiveHtmlPage(ProjectionOptions), 980, 900);
        var page = session.Page;
        await ConfigureHostAsync(page, zoom, aspect, decorated);
        var target = page.Locator("g[data-cfx-observation-id=middle]");
        await target.EvaluateAsync("node=>node.setAttribute('transform','translate(7 3)')");
        await page.EvaluateAsync("""
            async()=>{
                const node=document.querySelector('g[data-cfx-observation-id=middle]');
                const host=document.createElement('button');host.textContent='Continue report';document.body.appendChild(host);
                // Following report content gives the host enough normal-flow scroll range to clip the chosen mark.
                const following=document.createElement('section');following.style.minHeight='100vh';
                following.textContent='Report details continue below the chart.';document.body.appendChild(following);host.focus();
                for(let i=0;i<2;i++){
                    const box=node.getBoundingClientRect();window.scrollBy(0,(box.top+box.bottom)/2-6);
                    await new Promise(requestAnimationFrame);
                }
            }
            """);
        var pointer = await ActualCenterAsync(target);
        Assert.True(float.IsFinite(pointer[0]) && float.IsFinite(pointer[1]), "The actual SVG mark has finite screen coordinates.");
        Assert.InRange(pointer[1], 5, 7);
        Assert.Equal("middle", await page.EvaluateAsync<string>(
            "point=>document.elementFromPoint(point.x,point.y)?.closest('[data-cfx-observation-id]')?.dataset.cfxObservationId",
            new { x = (double)pointer[0], y = (double)pointer[1] }));
        await page.Mouse.MoveAsync(pointer[0], pointer[1]);
        await page.WaitForFunctionAsync("()=>!document.querySelector('.cfx-tooltip').hidden");
        Assert.Contains("Middle", await TooltipTextAsync(page));
        await CaptureAsync(page, "partial", zoom, aspect, decorated);

        await target.EvaluateAsync("node=>{const box=node.getBoundingClientRect();window.scrollBy(0,(box.top+box.bottom)/2-250)}");
        await FocusWithKeyboardAsync(page, target);
        Assert.True(await target.EvaluateAsync<bool>("""
            node=>{
                const mark=node.getBoundingClientRect(),ring=node.closest('.cfx-interactive-chart').querySelector('[data-cfx-focus-ink]').getBoundingClientRect();
                return (mark.left+mark.right)/2>=ring.left&&(mark.left+mark.right)/2<=ring.right
                    &&(mark.top+mark.bottom)/2>=ring.top&&(mark.top+mark.bottom)/2<=ring.bottom;
            }
            """), "The rendered keyboard indicator surrounds the actual transformed mark.");
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        await CaptureAsync(page, "focus", zoom, aspect, decorated);
        await page.Keyboard.PressAsync("Escape");
        await page.EvaluateAsync("()=>document.querySelector('body>button').focus({preventScroll:true})");
        pointer = await ActualCenterAsync(target);
        await page.Mouse.MoveAsync(pointer[0] + 2, pointer[1]);
        await page.Mouse.MoveAsync(pointer[0], pointer[1]);
        await page.WaitForFunctionAsync("()=>!document.querySelector('.cfx-crosshair').hidden");
        var guide = await page.EvaluateAsync<JsonElement>("""
            ()=>{
                const root=document.querySelector('.cfx-interactive-chart'),stage=root.querySelector('.cfx-stage'),
                    plot=root.querySelector('[data-projection-oracle]').getBoundingClientRect(),guide=root.querySelector('.cfx-crosshair').getBoundingClientRect(),
                    viewport=stage.getBoundingClientRect(),scaleX=viewport.width/stage.offsetWidth,scaleY=viewport.height/stage.offsetHeight;
                const left=Math.max(plot.left,viewport.left+stage.clientLeft*scaleX),top=Math.max(plot.top,viewport.top+stage.clientTop*scaleY),
                    right=Math.min(plot.right,viewport.left+(stage.clientLeft+stage.clientWidth)*scaleX),
                    bottom=Math.min(plot.bottom,viewport.top+(stage.clientTop+stage.clientHeight)*scaleY);
                return {guide:guide.toJSON(),expected:{left,top,right,bottom}};
            }
            """);
        foreach (var edge in new[] { "left", "top", "right", "bottom" })
            Assert.InRange(guide.GetProperty("guide").GetProperty(edge).GetDouble() - guide.GetProperty("expected").GetProperty(edge).GetDouble(), -1, 1);
        await CaptureAsync(page, "guide", zoom, aspect, decorated);
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [Trait("BrowserSuite", "Compatibility")]
    [InlineData(1, "xMidYMid meet", false)]
    [InlineData(1, "xMidYMid meet", true)]
    [InlineData(1.5, "xMidYMid meet", false)]
    [InlineData(1.5, "xMidYMid meet", true)]
    [InlineData(1.5, "xMidYMid slice", false)]
    [InlineData(1.5, "none", false)]
    public async Task HostZoomKeepsBrushDataAndNativeViewportAnchorsInTheRenderedPlot(double zoom, string aspect, bool decorated) {
        if (!Enabled) return;
        await using var session = await OpenAsync(ProjectionChart().ToInteractiveHtmlPage(ProjectionOptions), 980, 900);
        var page = session.Page;
        await ConfigureHostAsync(page, zoom, aspect, decorated);
        var positions = await page.EvaluateAsync<float[]>("""
            ()=>{
                window.projectionBrush=[];document.body.addEventListener('cfxbrush',event=>projectionBrush.push(event.detail));
                const plot=document.querySelector('[data-projection-oracle]').getBoundingClientRect();
                return [plot.left+plot.width*.3,plot.top+plot.height*.4,plot.left+plot.width*.7,plot.top+plot.height*.6];
            }
            """);
        await page.Locator("[data-cfx-mode-button=brush]").ClickAsync();
        await page.Mouse.MoveAsync(positions[0], positions[1]);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(positions[2], positions[3], new() { Steps = 4 });
        await page.Mouse.UpAsync();
        var brush = await page.EvaluateAsync<JsonElement>("()=>projectionBrush[0]");
        var range = brush.GetProperty("range").GetProperty("x").EnumerateArray().Select(value => value.GetDouble()).ToArray();
        Assert.InRange(range[0], .59, .61);
        Assert.InRange(range[1], 1.39, 1.41);
        Assert.Equal("Middle", brush.GetProperty("rangeLabel").GetString());
        Assert.Equal(new[] { "middle" }, await page.EvaluateAsync<string[]>(
            "()=>ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')).getSelection().map(target=>target.observationId)"));
        await CaptureAsync(page, "brush", zoom, aspect, decorated);

        await page.EvaluateAsync("""
            ()=>{
                const root=document.querySelector('.cfx-interactive-chart'),controller=ChartForgeXCharts.get(root);
                window.projectionViewport=[];
                controller.connectXViewport(request=>{projectionViewport.push(request);return new Promise(()=>{})});
            }
            """);
        await page.Locator("[data-cfx-mode-button=pan]").ClickAsync();
        await page.Mouse.MoveAsync(positions[0], positions[1]);
        await page.Mouse.WheelAsync(0, -100);
        await page.WaitForFunctionAsync("()=>projectionViewport.length===1");
        var viewport = await page.EvaluateAsync<JsonElement>("()=>projectionViewport[0]");
        var expected = await page.EvaluateAsync<double[]>("""
            ()=>{
                const root=document.querySelector('.cfx-interactive-chart'),p=JSON.parse(root.dataset.cfxPreparedChart).xProjection.plotBounds,
                    factor=1.08,anchor=p.x+p.width*.3;
                return [anchor-(anchor-p.x)/factor,anchor+(p.x+p.width-anchor)/factor];
            }
            """);
        Assert.InRange(viewport.GetProperty("left").GetDouble() - expected[0], -.1, .1);
        Assert.InRange(viewport.GetProperty("right").GetDouble() - expected[1], -.1, .1);
        AssertNoConsoleErrors(session);
    }

    private static Chart ProjectionChart() => Chart.Create().WithSize(680, 380).WithLegend(false)
        .WithXAxisBounds(0, 2).WithYAxisBounds(0, 10)
        .WithXLabels(new[] { new ChartAxisLabel(0, "First"), new ChartAxisLabel(1, "Middle"), new ChartAxisLabel(2, "Last") })
        .AddSeries(new ChartSeries(new ChartSeriesId("readings"), "Readings", ChartSeriesKind.Scatter,
            new[] { new ChartPoint(0, 2), new ChartPoint(1, 5), new ChartPoint(2, 8) })
            .WithObservationIds(new[] { new ChartObservationId("first"), new ChartObservationId("middle"), new ChartObservationId("last") }));

    private static void ProjectionOptions(HtmlChartInteractionOptions options) {
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
        options.Interaction.Enable(ChartInteractionFeatures.Brush | ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan);
        options.Interaction.BrushAxis = ChartBrushAxis.X;
    }

    private static Task ConfigureHostAsync(IPage page, double zoom, string aspect, bool decorated) => page.EvaluateAsync("""
        ({zoom,aspect,decorated})=>{
            document.documentElement.style.zoom=String(zoom);
            const root=document.querySelector('.cfx-interactive-chart'),svg=root.querySelector('.cfx-stage svg');
            svg.style.width='608px';svg.style.height='380px';
            if(decorated){svg.style.border='4px solid';svg.style.padding='6px';svg.style.boxSizing='border-box'};
            const stage=root.querySelector('.cfx-stage');stage.style.transform='scale(1.05,.95)';stage.style.transformOrigin='0 0';
            svg.setAttribute('preserveAspectRatio',aspect);
            const plot=JSON.parse(root.dataset.cfxPreparedChart).dataProjection.plotBounds,oracle=document.createElementNS('http://www.w3.org/2000/svg','rect');
            for(const key of ['x','y','width','height'])oracle.setAttribute(key,plot[key]);
            oracle.setAttribute('fill','none');oracle.setAttribute('pointer-events','none');oracle.setAttribute('data-projection-oracle','');svg.appendChild(oracle);
        }
        """, new { zoom, aspect, decorated });

    private static Task<float[]> ActualCenterAsync(ILocator target) => target.EvaluateAsync<float[]>(
        "node=>{const box=node.getBoundingClientRect();return[(box.left+box.right)/2,(box.top+box.bottom)/2]}");

    private static async Task CaptureAsync(IPage page, string state, double zoom, string aspect, bool decorated) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new() { Path = Path.Combine(directory, "svg-projection-" + state + "-" + zoom + "-" + aspect.Replace(' ', '-') + "-" + decorated + ".png") });
    }
}
