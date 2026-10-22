using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ResponsiveChartLayoutContractsTests {
    [BrowserFact]
    public async Task IdenticalUnscopedChartsHaveIndependentDefinitionsAtDifferentWidths() {
        if (!InteractiveChartBrowser.Enabled) return;
        Chart Build() => Chart.Create().WithSize(760, 280).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .WithStackedBars().AddBar("Count", new[] { new ChartPoint(1, 1), new ChartPoint(2, 3) });
        var renderer = new HtmlResponsiveChartRenderer();
        var html = "<html><body style='margin:0'><div style='width:320px'>" + renderer.RenderFragment(Build(), 240, 1600) +
            "</div><div style='width:1120px'>" + renderer.RenderFragment(Build(), 240, 1600) + "</div></body></html>";
        await using var session = await InteractiveChartBrowser.OpenAsync(html, 1440, 700);
        await session.Page.EvaluateAsync("async()=>await Promise.all([...document.querySelectorAll('[data-cfx-responsive-chart]')].map(h=>h.cfxResponsiveReady))");
        Assert.True(await session.Page.EvaluateAsync<bool>("()=>{const ids=[...document.querySelectorAll('svg [id],svg[id]')].map(n=>n.id);return ids.length===new Set(ids).size;}"));
        Assert.True(await session.Page.EvaluateAsync<bool>("()=>[...document.querySelectorAll('svg [clip-path]')].every(n=>{const id=n.getAttribute('clip-path').slice(5,-1);return n.ownerSVGElement.contains(document.getElementById(id));})"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SparseGroupedBarsRetainTheirSeriesSlotAndCategoryAnchorAtEveryWidth(bool equivalentCoordinates) {
        if (!InteractiveChartBrowser.Enabled) return;
        var chart = Chart.Create().WithSize(760, 280).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .WithBarStyle(ChartBarStyle.Flat).WithMaximumBarWidth(220).WithXLabels("First", "Second")
            .AddBar("A", new[] { new ChartPoint(1, 1), new ChartPoint(2, 3) })
            .AddBar("B", new[] { new ChartPoint(equivalentCoordinates ? Math.BitIncrement(Math.BitIncrement(Math.BitIncrement(1))) : 1, 2) });
        var html = "<html><body style='margin:0'><div id='panel' style='width:320px'>" +
            new HtmlResponsiveChartRenderer().RenderFragment(chart, 240, 1600) + "</div></body></html>";
        await using var session = await InteractiveChartBrowser.OpenAsync(html, 1440, 700);
        foreach (var width in new[] { 320, 779, 1120, 320 }) {
            await session.Page.EvaluateAsync("w=>document.querySelector('#panel').style.width=w+'px'", width);
            await session.Page.WaitForFunctionAsync("w=>Number(document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutWidth)===w", width);
            var geometry = await session.Page.EvaluateAsync<double[]>("()=>{const box=(s,p)=>document.querySelector(`[data-cfx-role=point][data-cfx-series='${s}'][data-cfx-point='${p}'] [data-cfx-role=bar]`).getBoundingClientRect(),a=box(0,0),b=box(1,0),c=box(0,1),label=[...document.querySelectorAll('[data-cfx-role=axis-x-label] text')].find(n=>n.textContent==='Second').getBoundingClientRect();return [a.width,b.width,c.width,(b.x+b.width/2-a.x-a.width/2)/2,label.x+label.width/2-c.x-c.width/2];}");
            Assert.InRange(geometry[0], 20, 220); Assert.Equal(geometry[0], geometry[1], .1); Assert.Equal(geometry[0], geometry[2], .1);
            Assert.Equal(geometry[3], geometry[4], .5);
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task OversizedLegendCaptionsFitTheirLaneAndOmittedRowsRetainFullNames() {
        if (!InteractiveChartBrowser.Enabled) return;
        var chart = Chart.Create().WithSize(760, 280).WithHeader(false).WithDataLabels(false);
        for (var i = 0; i < 7; i++) chart.AddBar("Observations awaiting verification from the regional operations team " + i, new[] { new ChartPoint(1, i + 1) });
        var html = "<html><body style='margin:0'><div id='panel' style='width:320px'>" +
            new HtmlResponsiveChartRenderer().RenderFragment(chart, 240, 1600) + "</div></body></html>";
        await using var session = await InteractiveChartBrowser.OpenAsync(html, 1440, 700);
        foreach (var width in new[] { 320, 1120, 320 }) {
            await session.Page.EvaluateAsync("w=>document.querySelector('#panel').style.width=w+'px'", width);
            await session.Page.WaitForFunctionAsync("w=>Number(document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutWidth)===w", width);
            Assert.True(await session.Page.EvaluateAsync<bool>("w=>[...document.querySelectorAll('[data-cfx-role=legend-entry],[data-cfx-role=legend-overflow]')].filter(n=>getComputedStyle(n).display!=='none').every(n=>{const b=n.getBoundingClientRect();return b.x>=-.5&&b.right<=w+.5&&b.bottom<=280;})", width));
            if (width == 320) {
                var label = session.Page.Locator("[data-cfx-role=legend-entry]").First;
                Assert.EndsWith("\u2026", await label.Locator("[data-cfx-role=legend-label]").TextContentAsync());
                Assert.Contains("regional operations team 0", await label.Locator(":scope > title").TextContentAsync());
                Assert.Contains("more", await session.Page.Locator("[data-cfx-role=legend-overflow]").TextContentAsync());
            }
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
    [BrowserFact]
    public async Task PrintLayoutUsesPanelWidthBelowTheScreenScrollingMinimum() {
        if (!InteractiveChartBrowser.Enabled) return;
        var chart = Chart.Create().WithSize(760, 280).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .AddLine("Count", new[] { new ChartPoint(1, 1), new ChartPoint(2, 3) });
        var html = "<html><head><style>@media print{#panel{width:320px!important}}</style></head><body style='margin:0'><div id='panel' style='width:600px'>" +
            new HtmlResponsiveChartRenderer().RenderFragment(chart, 480, 1600, printWidth: 320) + "</div></body></html>";
        await using var session = await InteractiveChartBrowser.OpenAsync(html, 1440, 700);
        await session.Page.WaitForFunctionAsync("()=>Number(document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutWidth)===600");
        await session.Page.EmulateMediaAsync(new() { Media = Microsoft.Playwright.Media.Print });
        await session.Page.WaitForFunctionAsync("()=>Number(document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutWidth)===320");
        await session.Page.WaitForTimeoutAsync(150);
        var size = await session.Page.Locator("svg").EvaluateAsync<double[]>("s=>{const b=s.getBoundingClientRect();return[b.width,b.height];}");
        Assert.Equal(320, size[0], .1); Assert.Equal(280, size[1], .1);
        await session.Page.EmulateMediaAsync(new() { Media = Microsoft.Playwright.Media.Screen });
        await session.Page.WaitForFunctionAsync("()=>Number(document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutWidth)===600");
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OversizedRadialLegendKeepsValuesAndPercentagesInsideItsLaneAfterResize(bool donut) {
        if (!InteractiveChartBrowser.Enabled) return;
        var chart = Chart.Create().WithSize(760, 360).WithHeader(false).WithDataLabels(false)
            .WithLegendPosition(ChartLegendPosition.Bottom).WithXLabels("Observations awaiting verification from the regional operations team", "Verified observations");
        var points = new[] { new ChartPoint(1, 1234), new ChartPoint(2, 5678) };
        if (donut) chart.AddDonut("Counts", points); else chart.AddPie("Counts", points);
        var html = "<html><body style='margin:0'><div id='panel' style='width:320px'>" +
            new HtmlResponsiveChartRenderer().RenderFragment(chart, 240, 1600) + "</div></body></html>";
        await using var session = await InteractiveChartBrowser.OpenAsync(html, 1440, 700);
        foreach (var width in new[] { 320, 1120, 320 }) {
            await session.Page.EvaluateAsync("w=>document.querySelector('#panel').style.width=w+'px'", width);
            await session.Page.WaitForFunctionAsync("w=>Number(document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutWidth)===w", width);
            var bounds = await session.Page.EvaluateAsync<double[][]>("()=>[...document.querySelectorAll('[data-cfx-role=legend-entry]')].filter(n=>getComputedStyle(n).display!=='none').map(n=>{const b=n.getBoundingClientRect();return[b.x,b.right,n.querySelectorAll('[data-cfx-role=legend-value],[data-cfx-role=legend-percentage]').length];})");
            Assert.Equal(2, bounds.Length);
            Assert.All(bounds, box => { Assert.InRange(box[0], -.5, width); Assert.InRange(box[1], 0, width + .5); Assert.Equal(2, box[2]); });
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

}
