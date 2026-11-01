using System;
using System.Linq;
using ChartForgeX.Interactivity.Html;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ResponsiveRadialLayoutBrowserTests {
    private const string Circle = """
        root=>{const boxes=[...root.querySelectorAll('[data-cfx-role=donut-slice],[data-cfx-role=pie-slice]')].map(e=>e.getBoundingClientRect());
          return [Math.min(...boxes.map(b=>b.left)),Math.min(...boxes.map(b=>b.top)),Math.max(...boxes.map(b=>b.right)),Math.max(...boxes.map(b=>b.bottom))];}
        """;

    [BrowserTheory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public async Task RingLegendsAndInteractionsReflowAtEveryWidth(bool dark, bool other) {
        var chart = ResponsiveRadialFixture.Model(dark, other);
        await using var session = await InteractiveChartBrowser.OpenAsync(ResponsiveRadialFixture.Document(ResponsiveRadialFixture.Fragment(chart, true), dark), 1440, 1100);
        var page = session.Page;
        await page.WaitForFunctionAsync("()=>document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutVersion==='spec-4'");
        string? narrow = null;
        foreach (var width in new[] { 390, 1024, 1440, 390 }) {
            await page.EvaluateAsync("w=>document.querySelector('#panel').style.width=w+'px'", width);
            await page.WaitForFunctionAsync("w=>Number(document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutWidth)===w", width);
            var circle = await page.Locator("[data-cfx-responsive-chart]").EvaluateAsync<double[]>(Circle);
            var card = await page.Locator("[data-cfx-role=frame-card]").EvaluateAsync<double[]>("e=>{const b=e.getBoundingClientRect();return [b.left,b.top,b.right,b.bottom]}");
            Assert.True(circle[0] >= card[0] - 1 && circle[1] >= card[1] - 1 && circle[2] <= card[2] + 1 && circle[3] <= card[3] + 1);
            var legends = page.Locator("[data-cfx-role=legend-item]");
            Assert.Equal(other ? 4 : 6, await legends.CountAsync());
            Assert.True(await legends.EvaluateAllAsync<bool>("""
                nodes=>nodes.every(e=>{const b=e.getBoundingClientRect(),svg=e.ownerSVGElement.getBoundingClientRect();
                  const slices=[...e.ownerSVGElement.querySelectorAll('[data-cfx-role=donut-slice],[data-cfx-role=pie-slice]')].map(p=>p.getBoundingClientRect());
                  return b.width>0&&b.height>0&&b.left>=svg.left-1&&b.right<=svg.right+1&&b.bottom+4<=Math.min(...slices.map(s=>s.top));})
                """));
            var values = await page.Locator("[data-cfx-role=legend-value]").AllTextContentsAsync();
            Assert.Equal(other ? new[] { "412", "268", "190", "215" } : new[] { "412", "268", "190", "120", "64", "31" }, values);
            Assert.Equal(other ? 4 : 6, await page.Locator("[data-cfx-role=legend-percentage]").CountAsync());
            var hitAndFocus = await page.EvaluateAsync<int[]>("""
                ()=>{const targets=[...document.querySelectorAll('[data-cfx-role=radial-point]')],paths=targets.map(t=>t.querySelector('[data-cfx-role=donut-slice],[data-cfx-role=pie-slice]'));
                  const boxes=paths.map(e=>e.getBoundingClientRect()),l=Math.min(...boxes.map(b=>b.left)),r=Math.max(...boxes.map(b=>b.right)),t=Math.min(...boxes.map(b=>b.top)),b=Math.max(...boxes.map(b=>b.bottom));
                  let angle=-Math.PI/2,hit=0,focus=0;
                  for(const target of targets){const sweep=Number(target.dataset.cfxPercent)*Math.PI*2,mid=angle+sweep/2,x=(l+r)/2+Math.cos(mid)*(r-l)*.405,y=(t+b)/2+Math.sin(mid)*(b-t)*.405;
                    if(document.elementFromPoint(x,y)?.closest('[data-cfx-role=radial-point]')===target)hit++;
                    target.focus();if(document.activeElement===target)focus++;angle+=sweep;}
                  return [hit,focus,targets.length];}
                """);
            Assert.Equal(other ? 4 : 6, hitAndFocus[2]); Assert.Equal(hitAndFocus[2], hitAndFocus[0]); Assert.Equal(hitAndFocus[2], hitAndFocus[1]);
            await legends.First.HoverAsync();
            Assert.True(await page.Locator("[data-cfx-role=radial-point]").First.EvaluateAsync<bool>("e=>e.classList.contains('cfx-hover-related')||e.classList.contains('cfx-hovered')"));
            await page.Mouse.MoveAsync(0, 0);
            var snapshot = await page.EvaluateAsync<string>("()=>JSON.stringify([...document.querySelectorAll('[data-cfx-role=legend-item],[data-cfx-role=donut-slice],[data-cfx-role=pie-slice]')].map(e=>{const b=e.getBoundingClientRect();return [b.x,b.y,b.width,b.height]}))");
            if (width == 390) { if (narrow == null) narrow = snapshot; else Assert.Equal(narrow, snapshot); }
        }
        if (other) Assert.Equal("3,4,5", await page.Locator("[data-cfx-role=radial-point][data-cfx-point='-1']").GetAttributeAsync("data-cfx-source-points"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false)] [InlineData(true)]
    public async Task NarrowRingAndLegendMatchStaticLayout(bool dark) {
        var chart = ResponsiveRadialFixture.Model(dark);
        var fragment = ResponsiveRadialFixture.Fragment(chart);
        chart.WithSize(390, 420);
        var html = ResponsiveRadialFixture.Document(fragment + "<div id='reference' style='position:absolute;left:2000px;top:0;width:390px'>" + chart.ToSvg() + "</div>", dark);
        await using var session = await InteractiveChartBrowser.OpenAsync(html, 1440, 1000);
        var page = session.Page;
        await page.WaitForFunctionAsync("()=>document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutVersion==='spec-4'");
        var actual = await page.Locator("[data-cfx-responsive-chart]").EvaluateAsync<double[]>(Circle);
        var expected = await page.Locator("#reference").EvaluateAsync<double[]>(Circle);
        Assert.Equal(expected[2] - expected[0], actual[2] - actual[0], 2);
        Assert.Equal(expected[3] - expected[1], actual[3] - actual[1], 2);
        const string rows = "root=>new Set([...root.querySelectorAll('[data-cfx-role=legend-label]')].map(e=>Math.round(e.getBoundingClientRect().top))).size";
        Assert.Equal(await page.Locator("#reference").EvaluateAsync<int>(rows), await page.Locator("[data-cfx-responsive-chart]").EvaluateAsync<int>(rows));
        const string centerSizes = "root=>[...root.querySelectorAll('[data-cfx-role=donut-center] text')].map(e=>parseFloat(getComputedStyle(e).fontSize)*Math.hypot(e.getScreenCTM().a,e.getScreenCTM().b))";
        var actualSizes = await page.Locator("[data-cfx-responsive-chart]").EvaluateAsync<double[]>(centerSizes);
        var expectedSizes = await page.Locator("#reference").EvaluateAsync<double[]>(centerSizes);
        Assert.Equal(2, actualSizes.Length);
        for (var i = 0; i < actualSizes.Length; i++) Assert.Equal(expectedSizes[i], actualSizes[i], .1);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task LongLegendNameKeepsTooltipAndCompleteNumericColumns() {
        var chart = ResponsiveRadialFixture.Model(false, longName: true);
        await using var session = await InteractiveChartBrowser.OpenAsync(ResponsiveRadialFixture.Document(ResponsiveRadialFixture.Fragment(chart), false), 390, 1000);
        var page = session.Page;
        await page.WaitForFunctionAsync("()=>document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutVersion==='spec-4'");
        var entry = page.Locator("[data-cfx-role=legend-entry]").Nth(2);
        Assert.EndsWith("…", await entry.Locator("[data-cfx-role=legend-label]").TextContentAsync());
        Assert.Contains("extended archive and compliance", await entry.Locator(":scope > title").TextContentAsync());
        Assert.Equal("190", await entry.Locator("[data-cfx-role=legend-value]").TextContentAsync());
        Assert.Equal("17.5%", await entry.Locator("[data-cfx-role=legend-percentage]").TextContentAsync());
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task CompleteLegendGrowsTheCardInsteadOfDroppingRowsOrSqueezingTheRing() {
        var chart = ResponsiveRadialFixture.Model(false);
        chart.Options.MaximumPieSlices = 20;
        chart.Series[0].Points.Clear();
        for (var index = 0; index < 18; index++) chart.Series[0].Points.Add(new ChartForgeX.Primitives.ChartPoint(index + 1, 20 - index));
        chart.WithXLabels(Enumerable.Range(1, 18).Select(i => "Service " + i + " with an unusually long entitlement and archive description that fills a whole legend row").ToArray());
        await using var session = await InteractiveChartBrowser.OpenAsync(ResponsiveRadialFixture.Document(ResponsiveRadialFixture.Fragment(chart), false), 390, 1500);
        var page = session.Page;
        await page.WaitForFunctionAsync("()=>document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutVersion==='spec-4'");
        Assert.Equal(18, await page.Locator("[data-cfx-role=legend-entry]").CountAsync());
        Assert.Equal(0, await page.Locator("[data-cfx-role=legend-entry-omitted]").CountAsync());
        Assert.True(await page.Locator("svg").EvaluateAsync<double>("e=>e.getBoundingClientRect().height") > 420);
        var circle = await page.Locator("[data-cfx-responsive-chart]").EvaluateAsync<double[]>(Circle);
        Assert.True(circle[2] - circle[0] >= 60);
        var legendBottom = await page.Locator("[data-cfx-role=legend-entry]").EvaluateAllAsync<double>("nodes=>Math.max(...nodes.map(e=>e.getBoundingClientRect().bottom))");
        Assert.True(legendBottom + 4 <= circle[1]);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task HiddenFragmentUsesTheSameLegendPositionsAfterRevealAndPrint() {
        var fragment = ResponsiveRadialFixture.Fragment(ResponsiveRadialFixture.Model(false));
        var html = ResponsiveRadialFixture.Document("<div id='hidden' style='display:none'>" + fragment + "</div>", false);
        await using var session = await InteractiveChartBrowser.OpenAsync(html, 390, 1000);
        var page = session.Page;
        await page.WaitForFunctionAsync("()=>!!document.querySelector('[data-cfx-responsive-chart]').cfxResponsive");
        await page.EvaluateAsync("()=>document.querySelector('#hidden').style.display='block'");
        await page.WaitForFunctionAsync("()=>document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutVersion==='spec-4'");
        var first = await page.Locator("[data-cfx-role=legend-entry]").First.EvaluateAsync<double>("e=>e.getBoundingClientRect().left");
        Assert.InRange(first, 23, 25);
        await page.EmulateMediaAsync(new() { Media = Microsoft.Playwright.Media.Print });
        await page.EmulateMediaAsync(new() { Media = Microsoft.Playwright.Media.Screen });
        Assert.Equal(first, await page.Locator("[data-cfx-role=legend-entry]").First.EvaluateAsync<double>("e=>e.getBoundingClientRect().left"), .1);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false)] [InlineData(true)]
    public async Task AssetlessRadialFragmentMountsWithSharedAssets(bool compressed) {
        var chart = ResponsiveRadialFixture.Model(false);
        var fragment = new HtmlResponsiveChartRenderer().RenderFragmentWithoutAssets(chart, 240, 1600, compressLayouts: compressed);
        Assert.DoesNotContain("<script", fragment, StringComparison.Ordinal);
        Assert.DoesNotContain("<style", fragment, StringComparison.Ordinal);
        var html = ResponsiveRadialFixture.Document(fragment + "<style>" + HtmlResponsiveChartRenderer.Stylesheet + "</style><script>" + HtmlResponsiveChartRenderer.InitializationScript + "</script>", false);
        await using var session = await InteractiveChartBrowser.OpenAsync(html, 390, 1000);
        await session.Page.WaitForFunctionAsync("()=>document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutVersion==='spec-4'");
        Assert.Equal(6, await session.Page.Locator("[data-cfx-role=legend-entry]").CountAsync());
        Assert.Equal(1100, chart.Options.Size.Width); Assert.Equal(420, chart.Options.Size.Height);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(ChartForgeX.Core.ChartDataLabelPlacement.Center)]
    [InlineData(ChartForgeX.Core.ChartDataLabelPlacement.Outside)]
    public async Task SliceCaptionsStayWithinTheFrameAndMatchNativeVisibility(ChartForgeX.Core.ChartDataLabelPlacement placement) {
        var chart = ResponsiveRadialFixture.Model(false);
        chart.Options.ShowDataLabels = true; chart.Options.DataLabelPlacement = placement;
        var fragment = ResponsiveRadialFixture.Fragment(chart);
        chart.WithSize(390, 420);
        var html = ResponsiveRadialFixture.Document(fragment + "<div id='reference' style='position:absolute;left:2000px;top:0;width:390px'>" + chart.ToSvg() + "</div>", false);
        await using var session = await InteractiveChartBrowser.OpenAsync(html, 1440, 1000);
        var page = session.Page;
        await page.WaitForFunctionAsync("()=>document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutVersion==='spec-4'");
        Assert.True(await page.Locator("[data-cfx-responsive-chart] [data-cfx-role=data-label]:visible").EvaluateAllAsync<bool>("nodes=>nodes.every(e=>{const b=e.getBoundingClientRect(),s=e.ownerSVGElement.getBoundingClientRect();return b.left>=s.left-1&&b.right<=s.right+1&&b.top>=s.top-1&&b.bottom<=s.bottom+1})"));
        Assert.Equal(await page.Locator("#reference [data-cfx-role=data-label]:visible").CountAsync(), await page.Locator("[data-cfx-responsive-chart] [data-cfx-role=data-label]:visible").CountAsync());
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task EmptyRadialDrawingKeepsItsNoDataCaptionInsideTheCard() {
        var chart = ResponsiveRadialFixture.Model(false); chart.Series[0].Points.Clear();
        await using var session = await InteractiveChartBrowser.OpenAsync(ResponsiveRadialFixture.Document(ResponsiveRadialFixture.Fragment(chart), false), 390, 1000);
        await session.Page.WaitForFunctionAsync("()=>document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutVersion==='spec-4'");
        Assert.True(await session.Page.Locator("[data-cfx-role=no-data]").EvaluateAsync<bool>("e=>{const b=e.getBoundingClientRect(),s=e.ownerSVGElement.getBoundingClientRect();return b.width>0&&b.left>=s.left&&b.right<=s.right&&b.top>=s.top&&b.bottom<=s.bottom}"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
