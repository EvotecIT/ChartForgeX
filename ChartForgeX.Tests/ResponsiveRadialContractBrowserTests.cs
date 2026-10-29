using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ResponsiveRadialContractBrowserTests {
    [BrowserFact]
    public async Task CustomFrameTypographyKeepsLegendColumnsSeparateThroughResize() {
        var chart = ResponsiveRadialFixture.Model(false)
            .ConfigureTitleStyle(style => style.WithFontSize(28))
            .ConfigureLegendStyle(style => style.WithFontSize(18));
        await using var session = await InteractiveChartBrowser.OpenAsync(ResponsiveRadialFixture.Document(ResponsiveRadialFixture.Fragment(chart), false), 1440, 1200);
        var page = session.Page;
        foreach (var width in new[] { 390, 1440, 390 }) {
            await page.EvaluateAsync("w=>document.querySelector('#panel').style.width=w+'px'", width);
            await page.WaitForFunctionAsync("w=>Number(document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutWidth)===w", width);
            Assert.Equal(6, await page.Locator("[data-cfx-role=legend-entry]:visible").CountAsync());
            Assert.True(await page.Locator("[data-cfx-role=legend-entry]").EvaluateAllAsync<bool>("""
                nodes=>nodes.every(e=>{const label=e.querySelector('[data-cfx-role=legend-label]'),value=e.querySelector('[data-cfx-role=legend-value]'),percent=e.querySelector('[data-cfx-role=legend-percentage]'),b=e.getBoundingClientRect(),svg=e.ownerSVGElement.getBoundingClientRect();
                  return parseFloat(getComputedStyle(label.querySelector('text')).fontSize)===18&&label.getBoundingClientRect().right+4<=value.getBoundingClientRect().left&&value.getBoundingClientRect().right+4<=percent.getBoundingClientRect().left&&b.left>=svg.left-1&&b.right<=svg.right+1;})
                """));
            Assert.True(await page.Locator("[data-cfx-role=frame-heading-source]").First.EvaluateAsync<bool>("e=>{const b=e.getBoundingClientRect(),legend=e.ownerSVGElement.querySelector('[data-cfx-role=legend-entry]').getBoundingClientRect();return b.height>=25&&b.bottom<legend.top}"));
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(ChartDataLabelPlacement.Outside, false)]
    [InlineData(ChartDataLabelPlacement.Above, false)]
    [InlineData(ChartDataLabelPlacement.Outside, true)]
    [InlineData(ChartDataLabelPlacement.Above, true)]
    [InlineData(ChartDataLabelPlacement.Below, true)]
    public async Task SharedBandCalloutsAndHalosFollowTheirSlices(ChartDataLabelPlacement placement, bool bothRings) {
        var chart = Chart.Create().WithSize(1100, 420).WithHeader(false).WithLegend(false).WithDataLabels().WithXLabels("A", "B");
        var inner = new ChartSeries(new ChartSeriesId("prior"), "Prior", ChartSeriesKind.Donut,
            new[] { new ChartPoint(1, 70), new ChartPoint(2, 30) }).WithRadialBand(new ChartRadialBand(.2, .5));
        inner.DataLabelPlacement = placement;
        var outer = new ChartSeries(new ChartSeriesId("current"), "Current", ChartSeriesKind.Donut,
            new[] { new ChartPoint(1, 55), new ChartPoint(2, 45) }).WithRadialBand(new ChartRadialBand(.65, 1));
        outer.ShowDataLabels = bothRings; outer.DataLabelPlacement = placement;
        chart.AddSeries(inner).AddSeries(outer);
        await using var session = await InteractiveChartBrowser.OpenAsync(ResponsiveRadialFixture.Document(ResponsiveRadialFixture.Fragment(chart), false), 1440, 1000);
        var page = session.Page;
        string? narrow = null;
        foreach (var width in new[] { 390, 1440, 390 }) {
            await page.EvaluateAsync("w=>document.querySelector('#panel').style.width=w+'px'", width);
            await page.WaitForFunctionAsync("w=>Number(document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutWidth)===w", width);
            Assert.Equal(bothRings ? 4 : 2, await page.Locator("[data-cfx-radial-callout=overlay] [data-cfx-role=data-label]:visible").CountAsync());
            Assert.True(await page.Locator("[data-cfx-radial-callout=overlay]").EvaluateAllAsync<bool>("""
                nodes=>nodes.every(e=>{const text=e.querySelector('text'),b=text.getBoundingClientRect(),svg=e.ownerSVGElement.getBoundingClientRect(),line=e.querySelector('[data-cfx-role=data-label-connector]'),halo=e.querySelector('[data-cfx-role=data-label-connector-halo]');
                  if(!(b.left>=svg.left-1&&b.right<=svg.right+1&&b.top>=svg.top-1&&b.bottom<=svg.bottom+1)||!e.dataset.cfxLabelFor.startsWith('series-'+e.dataset.cfxSeries+'-')||!line||line.getAttribute('d')!==halo?.getAttribute('d'))return false;
                  const first=line.getPointAtLength(0),point=new DOMPoint(first.x,first.y).matrixTransform(line.getScreenCTM()),slice=[...e.ownerSVGElement.querySelectorAll('[data-cfx-role=donut-slice]')].find(s=>s.dataset.cfxSourceId===e.dataset.cfxLabelFor),sb=slice.getBoundingClientRect();
                  return point.x>=sb.left-1&&point.x<=sb.right+1&&point.y>=sb.top-1&&point.y<=sb.bottom+1;})
                """));
            Assert.True(await page.Locator("[data-cfx-radial-callout=overlay] [data-cfx-role=data-label]:visible").EvaluateAllAsync<bool>("nodes=>nodes.every((n,i)=>nodes.slice(i+1).every(m=>{const a=n.getBoundingClientRect(),b=m.getBoundingClientRect();return a.right+1<=b.left||b.right+1<=a.left||a.bottom+1<=b.top||b.bottom+1<=a.top}))"));
            var snapshot = await page.Locator("[data-cfx-radial-callout=overlay]").EvaluateAllAsync<string>("nodes=>JSON.stringify(nodes.map(e=>{const b=e.getBoundingClientRect();return [b.x,b.y,b.width,b.height]}))");
            if (width == 390) { if (narrow == null) narrow = snapshot; else Assert.Equal(narrow, snapshot); }
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false)] [InlineData(true)]
    public async Task OverflowTotalKeepsACompleteExponentWhileAuthoredCenterTextCanShorten(bool authored) {
        var chart = Chart.Create().WithSize(320, 240).WithHeader(false).WithLegend(false).WithDataLabels()
            .AddDonut("Total", new[] { new ChartPoint(1, 1e308), new ChartPoint(2, 1e308) });
        chart.Options.DonutInnerRadiusRatio = .35;
        if (authored) chart.Options.DonutCenterValue = "A deliberately long authored caption";
        var fragment = new HtmlResponsiveChartRenderer().RenderFragment(chart, 240, 1600);
        await using var session = await InteractiveChartBrowser.OpenAsync(ResponsiveRadialFixture.Document(fragment, false), 1440, 1000);
        var page = session.Page;
        foreach (var width in new[] { 320, 1440, 320 }) {
            await page.EvaluateAsync("w=>document.querySelector('#panel').style.width=w+'px'", width);
            await page.WaitForFunctionAsync("w=>Number(document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutWidth)===w", width);
            var text = await page.Locator("[data-cfx-role=donut-total-label] text").TextContentAsync();
            if (authored) Assert.EndsWith("…", text);
            else Assert.Matches("^> ?(?:1\\.79|1)E308$", text);
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
