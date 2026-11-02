using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Accepts the saved-reader feature bundles on the generated showcase pages for each standalone Cartesian family.</summary>
[Trait("BrowserSuite", "Compatibility")]
public sealed class StandaloneInteractionBrowserTests {
    private const string Root = ".cfx-interactive-chart";

    public static TheoryData<ChartSeriesKind> Kinds() => new(HtmlInteractionShowcaseExamples.Kinds);

    [BrowserTheory]
    [MemberData(nameof(Kinds))]
    public async Task MotionReadoutExportAndIsolationWorkTogetherOnTheSavedReader(ChartSeriesKind kind) {
        if (!Enabled) return;
        await using var session = await OpenAsync(HtmlInteractionShowcaseExamples.CreatePage(kind), 1100, 760);
        var page = session.Page;
        // Load: each painted series animates as an effect only, then settles on the native geometry.
        Assert.True(await page.EvaluateAsync<bool>("()=>document.querySelector('.cfx-interactive-chart').dataset.cfxMotion==='enter'"));
        await SettleAsync(page);
        var settled = await GeometryAsync(page);

        // Readout: crosshair values sit on both axis edges, and the page's renderer or C# template owns the text.
        var point = await CenterAsync(page, "[data-cfx-role=series][data-cfx-series=\"0\"] [data-cfx-point=\"3\"]");
        await page.Mouse.MoveAsync(point.X, point.Y);
        await page.WaitForFunctionAsync("()=>!document.querySelector('.cfx-crosshair').hidden");
        var axisValues = await page.Locator(".cfx-crosshair__axis-value:not([hidden])").AllInnerTextsAsync();
        Assert.Equal(2, axisValues.Count(text => text.Trim().Length > 0));
        var tooltip = await TooltipTextAsync(page);
        if (kind == ChartSeriesKind.Bar) Assert.Matches(@"^\w+ on \w+: \d+ requests$", tooltip.Trim());
        else Assert.True(await page.Locator(".cfx-tooltip strong").CountAsync() == 1, "The page renderer supplies the tooltip content.");

        // Zoom eases from the displayed window: the API reports the target at once, the drawing follows within the duration.
        await page.Locator("[data-cfx-zoom=in]").ClickAsync();
        var target = await page.EvaluateAsync<JsonElement>("()=>ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')).getViewport().x");
        Assert.True(await page.EvaluateAsync<bool>("()=>!!document.querySelector('.cfx-interactive-chart')._cfxStandaloneViewport.motionTarget"));
        await SettleAsync(page);
        var painted = await page.EvaluateAsync<JsonElement>("()=>document.querySelector('.cfx-interactive-chart')._cfxStandaloneViewport.windows.x");
        Assert.Equal(target.GetProperty("minimum").GetDouble(), painted.GetProperty("minimum").GetDouble(), 9);
        Assert.Equal(target.GetProperty("maximum").GetDouble(), painted.GetProperty("maximum").GetDouble(), 9);
        await page.Locator("[data-cfx-zoom-reset]").ClickAsync();
        await SettleAsync(page);
        await AssertNativeAsync(page, settled, "Reset zoom");


        // Context menu: data downloads and copies, then the host item reports the selection and rows.
        await page.Mouse.ClickAsync(point.X, point.Y + 30, new() { Button = MouseButton.Right });
        var actions = await page.Locator(".cfx-chart-context-menu [data-cfx-context-action]").EvaluateAllAsync<string[]>("nodes=>nodes.map(node=>node.dataset.cfxContextAction)");
        Assert.Equal(new[] { "csv", "copy-csv", "json", "copy-json", "flag" }, actions.SkipWhile(action => action is "reset" or "svg" or "png").ToArray());
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-context-action=csv]").ClickAsync());
        var csv = await File.ReadAllTextAsync(await download.PathAsync() ?? throw new InvalidOperationException("No download path."));
        var lines = csv.Split("\r\n");
        Assert.Contains("\"seriesKey\"", lines[0]);
        Assert.True(lines.Length > 10, "Every retained observation is exported, including those outside the window.");
        await page.EvaluateAsync("()=>document.querySelector('.cfx-interactive-chart').addEventListener('cfxcontextaction',event=>window.hostAction=event.detail)");
        await page.Mouse.ClickAsync(point.X, point.Y + 30, new() { Button = MouseButton.Right });
        await page.Locator("[data-cfx-context-action=flag]").ClickAsync();
        Assert.True(await page.EvaluateAsync<bool>("()=>window.hostAction.id==='flag'&&Array.isArray(window.hostAction.selection)&&window.hostAction.data.length>10"));

        // Double-click isolates the pointed series and rescales; repeating restores every series.
        var second = await CenterAsync(page, "[data-cfx-role=series][data-cfx-series=\"1\"] [data-cfx-point=\"5\"]");
        await page.Mouse.DblClickAsync(second.X, second.Y);
        Assert.StartsWith("1:", await page.Locator(Root).GetAttributeAsync("data-cfx-isolated-series") ?? "");
        Assert.Equal("", await page.EvaluateAsync<string>("()=>getSelection().toString()"));
        await SettleAsync(page);
        await page.Mouse.DblClickAsync(second.X, second.Y);
        await page.WaitForFunctionAsync("()=>!document.querySelector('.cfx-interactive-chart').hasAttribute('data-cfx-isolated-series')");
        await SettleAsync(page);
        await AssertNativeAsync(page, settled, "Restoring isolation");
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task ReducedMotionPaintsEveryChangeAtOnce() {
        if (!Enabled) return;
        await using var session = await OpenAsync(HtmlInteractionShowcaseExamples.CreatePage(ChartSeriesKind.Line), 1100, 760,
            configurePageAsync: page => page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce }));
        var page = session.Page;
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>document.getAnimations().length"));
        await page.Locator("[data-cfx-role=legend-item][data-cfx-series=\"2\"]").ClickAsync();
        Assert.False(await page.EvaluateAsync<bool>("()=>!!document.querySelector('.cfx-interactive-chart')._cfxStandaloneViewport.motionTarget"));
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NarrowReaderKeepsToolbarInsideTheCardAndReticksZoomedAxesReadably(bool dark) {
        if (!Enabled) return;
        await using var session = await OpenAsync(HtmlCrosshairExamples.CreatePage(HtmlChartCrosshairLineStyle.Solid, dark), 375, 812, touch: true);
        var page = session.Page;
        await SettleAsync(page);
        var layout = await page.EvaluateAsync<JsonElement>("""
            ()=>{const box=node=>node.getBoundingClientRect(),heading=document.querySelector('.cfx-responsive-heading'),toolbar=document.querySelector('.cfx-toolbar');
              const svg=document.querySelector('.cfx-stage svg'),plot=JSON.parse(document.querySelector('.cfx-interactive-chart').dataset.cfxPreparedChart).dataProjection.plotBounds;
              const scale=svg.clientWidth/svg.viewBox.baseVal.width,title=document.createRange();title.selectNodeContents(heading.firstChild);
              const text=[...title.getClientRects()],bar=box(toolbar),head=box(heading);
              return {inside:bar.top>=head.top&&bar.right<=head.right+.5,
                overlap:text.some(r=>r.right>bar.left&&r.left<bar.right&&r.bottom>bar.top&&r.top<bar.bottom),
                gap:(box(svg).top+plot.y*scale)-head.bottom};}
            """);
        Assert.True(layout.GetProperty("inside").GetBoolean(), "The toolbar sits inside the card's top band.");
        Assert.False(layout.GetProperty("overlap").GetBoolean(), "The toolbar must not cover the title.");
        Assert.InRange(layout.GetProperty("gap").GetDouble(), 0, 32);
        await page.Locator("[data-cfx-zoom=in]").ClickAsync();
        await SettleAsync(page);
        await page.EvaluateAsync("()=>ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')).setXWindow(1,9)");
        var ticks = await page.Locator("[data-cfx-browser-axis=x] text").EvaluateAllAsync<double[][]>("nodes=>nodes.map(node=>{const r=node.getBoundingClientRect();return [r.left,r.right];})");
        Assert.True(ticks.Length >= 3, "A zoomed narrow axis keeps at least three ticks: " + ticks.Length);
        for (var index = 1; index < ticks.Length; index++) Assert.True(ticks[index][0] - ticks[index - 1][1] >= 8, "Tick labels keep their own room.");
        var target = await CenterAsync(page, "[data-cfx-point]");
        Assert.True(await page.Locator("[data-cfx-point] [data-cfx-browser-hit-area]").First.EvaluateAsync<bool>("node=>node.getBoundingClientRect().width>=35.5"),
            "Touch layouts give data marks a 36 CSS pixel target.");
        Assert.True(target.X > 0);
        AssertNoConsoleErrors(session);
    }

    // Waits for entry animations and any eased window transition to finish.
    private static Task SettleAsync(IPage page) => page.EvaluateAsync("""
        async()=>{
          await Promise.all(document.getAnimations().map(animation=>animation.finished.catch(()=>{})));
          const view=document.querySelector('.cfx-interactive-chart')._cfxStandaloneViewport;
          for(let frame=0;frame<120&&view?.motionTarget;frame++) await new Promise(requestAnimationFrame);
          await new Promise(requestAnimationFrame);
        }
        """);
    private static async Task AssertNativeAsync(IPage page, string native, string action) {
        var geometry = await GeometryAsync(page);
        Assert.True(native == geometry, action + " restores the native drawing: " + await page.EvaluateAsync<string>(
            "()=>{const v=document.querySelector('.cfx-interactive-chart')._cfxStandaloneViewport;return JSON.stringify({windows:v.windows,target:v.motionTarget,muted:document.querySelectorAll('.cfx-series-muted,.cfx-series-isolated-out').length});}")
            + "\n" + native.Substring(0, Math.Min(160, native.Length)) + "\n" + geometry.Substring(0, Math.Min(160, geometry.Length)));
    }

    private static Task<string> GeometryAsync(IPage page) => page.EvaluateAsync<string>(
        "()=>[...document.querySelectorAll('.cfx-stage svg [data-cfx-role=series] :is(path,rect,circle,line)')].filter(node=>!node.closest('[data-cfx-browser-hit-area],[data-cfx-state-outline],[data-cfx-highlight-outline],[data-cfx-keyboard-focus-outline],.cfx-prepared-point-marker')).map(node=>['d','x','y','width','height','cx','cy'].map(name=>node.getAttribute(name)).join(',')).join('|')");

    private static async Task<(float X, float Y)> CenterAsync(IPage page, string selector) {
        var box = await page.Locator(selector).First.BoundingBoxAsync() ?? throw new InvalidOperationException("Missing " + selector);
        return (box.X + box.Width / 2, box.Y + box.Height / 2);
    }
}
