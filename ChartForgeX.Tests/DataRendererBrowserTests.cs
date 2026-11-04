using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Primitives;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class DataRendererBrowserTests {
    private const string Data = """
        {type:'column',label:'Orders by region',categories:[
          {id:'north',label:'North'},{id:'south',label:'South'},{id:'west',label:'West'}],
        series:[{id:'open',name:'Open',values:[4,1,-2]},{id:'closed',name:'Closed',values:[2,null,3]}]}
        """;

    private static string Page => "<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\">"
        + "<style>body{font-family:system-ui}#chart{width:640px}</style></head><body>"
        + "<div id=\"chart\"><span id=\"retained\">Host content</span></div><script>"
        + HtmlDataChartAssets.Script.Replace("</", "<\\/") + "</script></body></html>";

    [BrowserTheory]
    [InlineData("line")]
    [InlineData("area")]
    [InlineData("bar")]
    [InlineData("column")]
    [InlineData("stacked")]
    [InlineData("win-loss")]
    [InlineData("bullet")]
    public async Task NumericFamiliesRenderFiniteSignedValuesAndStableMarkIdentities(string kind) {
        await using var session = await OpenAsync(Page);
        await session.Page.EvaluateAsync("kind => { const data=" + Data
            + ";data.type=kind;window.chart=ChartForgeXRenderer.create(document.getElementById('chart'),data,{onSelect:()=>{}});}", kind);
        var page = session.Page;
        Assert.Equal(5, await page.Locator("svg [data-cfx-category]").CountAsync());
        Assert.Equal("Orders by region", await page.Locator("svg").GetAttributeAsync("aria-label"));
        Assert.False(await page.EvaluateAsync<bool>("()=>/NaN|Infinity/.test(document.querySelector('svg').outerHTML)"));
        Assert.Equal(1, await page.Locator("svg [tabindex='0']").CountAsync());
        Assert.Equal("Open, West: -2", await page.Locator("[data-cfx-series='open'][data-cfx-category='west']").GetAttributeAsync("aria-label"));
        if (kind is "column" or "stacked")
            Assert.Equal(ChartCategorySlotLayout.Create(new[] { 0d, 550d / 3, 1100d / 3 }, 550, 0,
                kind == "stacked" ? 1 : 2, 0, false, VisualTheme.Graphite().MaximumBarWidth).Thickness,
                await page.Locator("[data-cfx-series='open'][data-cfx-category='north'] [data-cfx-ink]")
                    .EvaluateAsync<double>("node=>Number(node.getAttribute('width'))"), 8);
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task KeyboardSelectionAndFocusSurviveDataUpdatesAndResponsiveLayout() {
        await using var session = await OpenAsync(Page);
        var page = session.Page;
        await page.EvaluateAsync("()=>{document.documentElement.lang='en_US';window.data=" + Data + ";window.selected=[];window.chart=ChartForgeXRenderer.create("
            + "document.getElementById('chart'),data,{onSelect:value=>selected.push(value)});}");
        var target = page.Locator("[data-cfx-series='open'][data-cfx-category='south']");
        await target.FocusAsync();
        await target.PressAsync("Enter");
        Assert.Equal("south", await page.EvaluateAsync<string>("()=>selected[0].categoryId"));
        await page.EvaluateAsync("()=>{data.series[0].values[1]=7;chart.update(data);document.getElementById('chart').style.width='320px';}");
        await page.WaitForFunctionAsync("()=>document.querySelector('svg').viewBox.baseVal.width===320");
        Assert.True(await page.EvaluateAsync<bool>("()=>document.activeElement.dataset.cfxCategory==='south'"));
        await target.PressAsync("Enter");
        Assert.Equal(7, await page.EvaluateAsync<int>("()=>selected[1].value"));
        Assert.Equal("true", await target.GetAttributeAsync("aria-pressed"));
        Assert.True(await target.EvaluateAsync<bool>("node=>{const event=new KeyboardEvent('keydown',{key:'End',ctrlKey:true,bubbles:true,cancelable:true});"
            + "node.dispatchEvent(event);return !event.defaultPrevented&&document.activeElement===node;}"));
        await target.PressAsync("Control+Enter");
        Assert.Equal(7, await page.EvaluateAsync<int>("()=>selected[2].value"));
        await target.PressAsync("End");
        Assert.Equal("west", await page.EvaluateAsync<string>("()=>document.activeElement.dataset.cfxCategory"));
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task InvalidAndSparseUpdatesLeaveTheAcceptedDrawingIntact() {
        await using var session = await OpenAsync(Page);
        var page = session.Page;
        await page.EvaluateAsync("()=>{window.data=" + Data + ";window.chart=ChartForgeXRenderer.create(document.getElementById('chart'),data);}");
        var result = await page.EvaluateAsync<bool>("""
            () => {
              const original=document.querySelector('svg'), invalid=[];
              const sparse=structuredClone(data); sparse.series[0].values=Array(3); invalid.push(sparse);
              const duplicate=structuredClone(data); duplicate.categories[1].id='north'; invalid.push(duplicate);
              const infinite=structuredClone(data); infinite.series[0].values[0]=Infinity; invalid.push(infinite);
              const overflow=structuredClone(data); overflow.type='stacked';
              overflow.series.forEach(series=>series.values[0]=Number.MAX_VALUE); invalid.push(overflow);
              return invalid.every(value=>{
                try { chart.update(value); return false; }
                catch { return document.querySelector('svg')===original; }
              });
            }
            """);
        Assert.True(result);
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task ReplacingAndAbortingControllersReleasesOnlyTheirOwnDrawing() {
        await using var session = await OpenAsync(Page);
        var page = session.Page;
        Assert.True(await page.EvaluateAsync<bool>("()=>{const data=" + Data + ";const host=document.getElementById('chart');"
            + "const oldAbort=new AbortController(),nextAbort=new AbortController();"
            + "const old=ChartForgeXRenderer.create(host,data,{signal:oldAbort.signal});"
            + "const next=ChartForgeXRenderer.create(host,data,{signal:nextAbort.signal});oldAbort.abort();"
            + "const kept=host.querySelectorAll('svg').length===1;next.update(data);nextAbort.abort();next.destroy();"
            + "let rejected=false;try{next.update(data);}catch{rejected=true;}"
            + "return kept&&rejected&&!host.querySelector('svg')&&document.getElementById('retained').textContent==='Host content';}"));
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task TemporalSpacingUsesInstantsAndMissingValuesKeepSeparateLineSegments() {
        await using var session = await OpenAsync(Page);
        var page = session.Page;
        var ratio = await page.EvaluateAsync<double>("()=>{const data=" + Data + ";data.type='line';data.series=data.series.slice(0,1);"
            + "data.categories.forEach((category,index)=>category.instant=['2026-01-01','2026-01-02','2026-01-11'][index]);"
            + "window.data=data;window.chart=ChartForgeXRenderer.create(document.getElementById('chart'),data);"
            + "const xs=[...document.querySelectorAll('circle')].map(node=>Number(node.getAttribute('cx')));return(xs[1]-xs[0])/(xs[2]-xs[0]);}");
        Assert.Equal(.1, ratio, 8);
        await page.EvaluateAsync("()=>{data.series[0].values[1]=null;chart.update(data);}");
        Assert.Equal(2, await page.Locator("svg > path[stroke]").CountAsync());
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task ZeroMarksRetainZeroInkAndPointerSelection() {
        await using var session = await OpenAsync(Page);
        var page = session.Page;
        await page.EvaluateAsync("()=>{window.data=" + Data + ";window.selected=[];"
            + "data.series[0].values[0]=0;window.chart=ChartForgeXRenderer.create(document.getElementById('chart'),data,"
            + "{onSelect:item=>selected.push(item)});}");
        var zero = page.Locator("[data-cfx-series='open'][data-cfx-category='north']");
        Assert.Equal("0", await zero.Locator("[data-cfx-ink]").GetAttributeAsync("height"));
        await zero.ClickAsync();
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>selected[0].value"));
        Assert.Equal("currentColor", await zero.Locator("[data-cfx-ink]").GetAttributeAsync("stroke"));
        Assert.Equal("currentColor", await zero.Locator("[data-cfx-target]").GetAttributeAsync("stroke"));
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task RemovedMarksAndAnEmptyDrawingKeepKeyboardFocus() {
        await using var session = await OpenAsync(Page);
        var page = session.Page;
        await page.EvaluateAsync("()=>{window.data=" + Data + ";window.chart=ChartForgeXRenderer.create(document.getElementById('chart'),data,{onSelect:()=>{}});}");
        await page.Locator("[data-cfx-series='open'][data-cfx-category='south']").FocusAsync();
        await page.EvaluateAsync("()=>{data.series[0].values[1]=null;chart.update(data);}");
        Assert.True(await page.EvaluateAsync<bool>("()=>document.activeElement.dataset.cfxCategory==='north'"));
        await page.EvaluateAsync("()=>{data.categories=[];data.series.forEach(series=>series.values=[]);chart.update(data);}");
        Assert.True(await page.EvaluateAsync<bool>("()=>document.activeElement===document.querySelector('svg')"));
        await page.EvaluateAsync("()=>{data=" + Data + ";chart.update(data);}");
        Assert.True(await page.EvaluateAsync<bool>("()=>document.activeElement.dataset.cfxCategory==='north'"));
        Assert.Null(await page.Locator("#chart").GetAttributeAsync("tabindex"));
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task NativeSlotAndStackPolicyMatchAcrossResponsiveWidths() {
        await using var session = await OpenAsync(Page);
        var page = session.Page;
        await page.EvaluateAsync("()=>{window.data=" + Data + ";window.chart=ChartForgeXRenderer.create(document.getElementById('chart'),data);}");
        foreach (var width in new[] { 240, 640, 1600 }) {
            await page.EvaluateAsync("width=>{document.getElementById('chart').style.width=width+'px';chart.update(data);}", width);
            var extent = width - 90d;
            var centers = new[] { extent / 6, extent / 2, extent * 5 / 6 };
            for (var series = 0; series < 2; series++) {
                var expected = ChartCategorySlotLayout.Create(centers, extent, 0, 2, series, false,
                    VisualTheme.Graphite().MaximumBarWidth);
                var bounds = await page.Locator($"[data-cfx-series='{(series == 0 ? "open" : "closed")}'][data-cfx-category='north'] [data-cfx-ink]")
                    .EvaluateAsync<double[]>("node=>[Number(node.getAttribute('x')),Number(node.getAttribute('width'))]");
                Assert.Equal(expected.Thickness, bounds[1], 8);
                Assert.Equal(66 + centers[0] + expected.Offset - expected.Thickness / 2, bounds[0], 8);
            }
        }
        await page.EvaluateAsync("()=>{data.type='stacked';chart.update(data);}");
        const double plotHeight = 234;
        var expectedSegment = ChartMarkSurface.StackSegmentBounds(new ChartRect(0, 68 + plotHeight * 2 / 8, 64, plotHeight * 4 / 8),
            new ChartRect(0, 68, 64, plotHeight * 6 / 8), false);
        var segment = await page.Locator("[data-cfx-series='open'][data-cfx-category='north'] [data-cfx-ink]")
            .EvaluateAsync<double[]>("node=>[Number(node.getAttribute('y')),Number(node.getAttribute('height'))]");
        Assert.Equal(expectedSegment.Top, segment[0], 8);
        Assert.Equal(expectedSegment.Height, segment[1], 8);
        foreach (var item in new[] { (Id: "open", Value: -2d), (Id: "closed", Value: 3d) }) {
            var baseline = 68 + plotHeight * 6 / 8;
            var valueY = 68 + plotHeight * (6 - item.Value) / 8;
            var expectedMixed = ChartMarkSurface.StackSegmentBounds(new ChartRect(0, Math.Min(baseline, valueY), 64, Math.Abs(valueY - baseline)),
                new ChartRect(0, 68 + plotHeight * 3 / 8, 64, plotHeight * 5 / 8), false);
            var mixed = await page.Locator($"[data-cfx-series='{item.Id}'][data-cfx-category='west'] [data-cfx-ink]")
                .EvaluateAsync<double[]>("node=>[Number(node.getAttribute('y')),Number(node.getAttribute('height'))]");
            Assert.Equal(expectedMixed.Top, mixed[0], 8);
            Assert.Equal(expectedMixed.Height, mixed[1], 8);
        }
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task ExtremeFiniteDomainsKeepFiniteTickAndMarkCoordinates() {
        await using var session = await OpenAsync(Page);
        var page = session.Page;
        await page.EvaluateAsync("()=>{const data=" + Data + ";data.series=data.series.slice(0,1);"
            + "data.series[0].values=[1e308,0,null];ChartForgeXRenderer.create(document.getElementById('chart'),data);}");
        Assert.False(await page.EvaluateAsync<bool>("()=>/NaN|Infinity|∞/.test(document.querySelector('svg').outerHTML)"));
        AssertNoConsoleErrors(session);
    }
}
