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

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NarrowHeadingLegendAndPlotReadAsOneCardWithCrispLegendText(bool dark) {
        if (!Enabled) return;
        await using var session = await OpenAsync(HtmlInteractionShowcaseExamples.CreatePage(ChartSeriesKind.Line, dark), 375, 812, touch: true);
        var page = session.Page;
        await SettleAsync(page);
        await page.Locator(".cfx-responsive-legend__item").First.ClickAsync();
        await SettleAsync(page);
        var card = await page.EvaluateAsync<JsonElement>("""
            ()=>{const frame=document.querySelector('.cfx-frame'),parts=[...frame.querySelectorAll(':scope > .cfx-responsive-heading, :scope > .cfx-responsive-legend, :scope > .cfx-stage')];
              const box=node=>node.getBoundingClientRect(),style=node=>getComputedStyle(node),stage=frame.querySelector(':scope > .cfx-stage');
              const items=[...frame.querySelectorAll('.cfx-responsive-legend__item')];
              return {gaps:parts.slice(1).map((node,index)=>box(node).top-box(parts[index]).bottom),
                backgrounds:parts.slice(0,2).map(node=>style(node).backgroundColor),cardFill:style(frame).getPropertyValue('--cfx-card-fill').trim(),
                stageTopRadius:style(stage).borderTopLeftRadius,
                itemBorders:[...new Set(items.map(item=>style(item).borderTopColor))].length,
                rasterLabels:frame.querySelectorAll('.cfx-responsive-legend__label svg').length,
                hiddenSwatch:style(items[0].querySelector('[data-cfx-role=legend-swatch]')).fillOpacity,
                shownSwatch:style(items[1].querySelector('[data-cfx-role=legend-swatch]')).fillOpacity};}
            """);
        Assert.All(card.GetProperty("gaps").EnumerateArray(), gap => Assert.InRange(gap.GetDouble(), -1.01, 0.01));
        Assert.All(card.GetProperty("backgrounds").EnumerateArray(), background => Assert.False(string.IsNullOrEmpty(background.GetString())));
        Assert.Equal("0px", card.GetProperty("stageTopRadius").GetString());
        Assert.Equal(1, card.GetProperty("itemBorders").GetInt32());
        Assert.Equal(0, card.GetProperty("rasterLabels").GetInt32());
        Assert.Equal("0", card.GetProperty("hiddenSwatch").GetString());
        Assert.Equal("1", card.GetProperty("shownSwatch").GetString());
        // A hidden entry stays readable: its label holds at least 3:1 against the card and its
        // outlined marker keeps the series colour at reduced strength.
        var hidden = await page.EvaluateAsync<JsonElement>("""
            ()=>{const item=document.querySelector('.cfx-responsive-legend__item'),label=item.querySelector('.cfx-responsive-legend__label');
              const swatch=item.querySelector('[data-cfx-role=legend-swatch]'),style=getComputedStyle(swatch);
              const rgb=text=>{const c=document.createElement('canvas').getContext('2d');c.fillStyle=text;c.fillRect(0,0,1,1);return [...c.getImageData(0,0,1,1).data];};
              let alpha=rgb(getComputedStyle(label).color)[3]/255;for(let node=label;node&&node!==document;node=node.parentElement)alpha*=Number(getComputedStyle(node).opacity);
              const fg=rgb(getComputedStyle(label).color),bg=rgb(getComputedStyle(document.querySelector('.cfx-frame')).getPropertyValue('--cfx-card-fill').trim());
              const mix=fg.slice(0,3).map((v,i)=>v*alpha+bg[i]*(1-alpha)),lum=c=>c.map(v=>{v/=255;return v<=.03928?v/12.92:((v+.055)/1.055)**2.4}).reduce((s,v,i)=>s+v*[.2126,.7152,.0722][i],0);
              const [a,b]=[lum(mix),lum(bg.slice(0,3))].sort((x,y)=>y-x);
              const chain=[];for(let node=label;node&&node!==document;node=node.parentElement)chain.push(node.tagName+'.'+node.className+':'+getComputedStyle(node).opacity);
              return {contrast:(a+.05)/(b+.05),chain:chain.slice(0,4).join(' > ')+' color='+getComputedStyle(label).color,stroke:style.stroke,fill:swatch.getAttribute('fill'),strokeOpacity:Number(style.strokeOpacity)};}
            """);
        Assert.True(hidden.GetProperty("contrast").GetDouble() >= 3, "Hidden legend label contrast: " + hidden.GetProperty("contrast").GetDouble() + " " + hidden.GetProperty("chain").GetString());
        Assert.InRange(hidden.GetProperty("strokeOpacity").GetDouble(), .3, .9);
        Assert.NotEqual("none", hidden.GetProperty("stroke").GetString());
        AssertNoConsoleErrors(session);
    }

    // Whole-number axes keep 1/2/5 steps; fractional axes may use 2.5. Either way new captions use the
    // native invariant notation rather than the browser's locale.
    [BrowserTheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ZoomedTicksKeepTheNativeStepPolicyAndCaptionNotation(bool whole) {
        if (!Enabled) return;
        var points = Enumerable.Range(0, whole ? 24 : 49).Select(index => new ChartForgeX.Primitives.ChartPoint(whole ? index : index * .5, 10 + index % 7)).ToArray();
        var html = Chart.Create().WithSize(800, 400).WithTitle("Ticks").AddLine("Samples", points).ToInteractiveHtmlPage(options =>
            options.Interaction.Enable(ChartForgeX.Interactivity.ChartInteractionFeatures.Zoom));
        await using var session = await OpenAsync(html, 1100, 700, configurePageAsync: page => page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce }));
        var page = session.Page;
        var fractional = false;
        foreach (var window in whole ? new[] { (4.14, 18.86), (5.61, 17.39), (6.4, 9.6) } : new[] { (4.14, 18.86), (5.61, 17.39), (6.4, 9.6), (7.05, 8.45) }) {
            await page.EvaluateAsync("w=>ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')).setXWindow(w[0],w[1])", new[] { window.Item1, window.Item2 });
            var ticks = await page.Locator("[data-cfx-browser-axis=x] text").EvaluateAllAsync<string[][]>("nodes=>nodes.map(node=>[node.dataset.cfxAxisValue,node.textContent])");
            Assert.True(ticks.Length >= 3, string.Join(" ", ticks.Select(tick => tick[1])) + " in " + window);
            foreach (var tick in ticks) {
                var value = double.Parse(tick[0], System.Globalization.CultureInfo.InvariantCulture);
                Assert.DoesNotContain(",", tick[1]);
                fractional |= tick[1].Contains('.');
                Assert.Equal(value, double.Parse(tick[1], System.Globalization.CultureInfo.InvariantCulture), 9);
                if (whole) Assert.True(value == Math.Round(value), "Whole-number axes never tick between integers: " + tick[1]);
            }
            var steps = ticks.Zip(ticks.Skip(1), (a, b) => double.Parse(b[0], System.Globalization.CultureInfo.InvariantCulture) - double.Parse(a[0], System.Globalization.CultureInfo.InvariantCulture)).Distinct().ToArray();
            if (whole) Assert.All(steps, step => Assert.Contains(step / Math.Pow(10, Math.Floor(Math.Log10(step))), new[] { 1d, 2d, 5d }));
        }
        Assert.Equal(!whole, fractional);
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
