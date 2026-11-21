using ChartForgeX.Interactivity.Html;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class DataRendererFamilyBrowserTests {
    private static string Page => "<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\">"
        + "<style>body{font-family:system-ui}#chart{width:640px}</style></head><body><div id=\"chart\"></div><script>"
        + HtmlDataChartAssets.Script.Replace("</", "<\\/") + "</script></body></html>";

    [BrowserFact]
    public async Task DonutSlicesShareTheCircleInProportionToTheirValues() {
        await using var session = await OpenAsync(Page);
        var page = session.Page;
        await page.EvaluateAsync("()=>{window.chart=ChartForgeXRenderer.create(document.getElementById('chart'),{type:'donut',label:'Findings',"
            + "categories:[{id:'a',label:'Critical'},{id:'b',label:'High'},{id:'c',label:'Low'}],series:[{id:'count',name:'Findings',values:[1,3,null]}]},{onSelect:()=>{}});}");
        Assert.Equal(2, await page.Locator("[data-cfx-category] [data-cfx-ink]").CountAsync());
        // Each sector's arc spans its share: a quarter and three quarters of the circle.
        var flags = await page.EvaluateAsync<string[]>("()=>[...document.querySelectorAll('[data-cfx-ink]')].map(node=>node.getAttribute('d').match(/A[^A]*? 0 ([01]) 1/)[1])");
        Assert.Equal(new[] { "0", "1" }, flags);
        Assert.Equal("Findings, High: 3", await page.Locator("[data-cfx-category='b']").GetAttributeAsync("aria-label"));
        Assert.Contains("4", await page.Locator("svg").TextContentAsync(), StringComparison.Ordinal);
        Assert.Equal("Low", await page.EvaluateAsync<string>("()=>[...document.querySelectorAll('svg text')].map(node=>node.firstChild.nodeValue).find(text=>text==='Low')"));
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task GaugeFillsItsArcToTheValueAndMarksTheTarget() {
        await using var session = await OpenAsync(Page);
        var page = session.Page;
        await page.EvaluateAsync("()=>{window.chart=ChartForgeXRenderer.create(document.getElementById('chart'),{type:'gauge',label:'Replication',minimum:0,maximum:200,"
            + "categories:[{id:'now',label:'Latency ms'}],series:[{id:'value',name:'Latency',values:[50]},{id:'target',name:'Target',values:[100]}]});}");
        var ends = await page.EvaluateAsync<double[]>("""
            () => {
              const svg = document.querySelector('svg'), arc = document.querySelector('[data-cfx-series=value] [data-cfx-ink]');
              const numbers = arc.getAttribute('d').match(/-?\d+(\.\d+)?/g).map(Number);
              return [numbers[0], numbers[1], numbers[7], numbers[8]];
            }
            """);
        // 240 degrees from 150: a quarter of the range ends 60 degrees later, at 210 (upper left of centre).
        var target = await page.Locator("[data-cfx-series='target'] line").EvaluateAsync<double[]>("node=>[+node.getAttribute('x1'),+node.getAttribute('y1'),+node.getAttribute('x2'),+node.getAttribute('y2')]");
        Assert.True(Math.Abs(ends[2] - ends[0]) < 0.001 && ends[3] < ends[1], "150 and 210 degrees mirror each other across the horizontal.");
        Assert.True(Math.Abs(target[0] - target[2]) < 0.001 && target[3] < target[1], "The halfway target points straight up.");
        await Assert.ThrowsAnyAsync<Exception>(() => page.EvaluateAsync("()=>chart.update({type:'gauge',label:'x',categories:[{id:'a',label:'a'},{id:'b',label:'b'}],series:[{id:'v',name:'v',values:[1,2]}]})"));
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task StateTimelineMergesEqualPeriodsAndAppendsLivePeriodsInAWindow() {
        await using var session = await OpenAsync(Page);
        var page = session.Page;
        await page.EvaluateAsync("""
            () => {
              const hour = index => ({ id: 'h' + index, label: index + ':00', instant: new Date(Date.UTC(2026, 10, 21, index)).toISOString() });
              window.chart = ChartForgeXRenderer.create(document.getElementById('chart'), {
                type: 'state-timeline', label: 'Site health',
                states: [{ id: 'up', label: 'Up', color: '#2f9e44' }, { id: 'down', label: 'Down', color: '#d6336c' }],
                categories: [0, 1, 2, 3].map(hour),
                series: [{ id: 'dc1', name: 'DC1', values: [0, 0, 1, null] }, { id: 'dc2', name: 'DC2', values: [0, 0, 0, 0] }]
              }, { onSelect: () => {} });
              window.hour = hour;
            }
            """);
        Assert.Equal(3, await page.Locator("[data-cfx-ink]").CountAsync());
        Assert.Equal("DC1, 0:00 – 1:00: Up", await page.Locator("[data-cfx-series='dc1'][data-cfx-category='h0']").GetAttributeAsync("aria-label"));
        Assert.Equal("#d6336c", await page.Locator("[data-cfx-series='dc1'][data-cfx-category='h2'] rect").GetAttributeAsync("fill"));
        // Two live periods arrive; the window keeps the latest four, and a series without news gets no ink there.
        await page.EvaluateAsync("()=>chart.append({categories:[hour(4),hour(5)],values:{dc1:[1,1]}},{maximum:4})");
        Assert.Equal(new[] { "h2", "h4" }, await page.EvaluateAsync<string[]>("()=>[...new Set([...document.querySelectorAll('[data-cfx-category]')].map(node=>node.dataset.cfxCategory))].sort()"));
        Assert.Equal("DC1, 4:00 – 5:00: Down", await page.Locator("[data-cfx-series='dc1'][data-cfx-category='h4']").GetAttributeAsync("aria-label"));
        Assert.Equal("DC2, 2:00 – 3:00: Up", await page.Locator("[data-cfx-series='dc2'][data-cfx-category='h2']").GetAttributeAsync("aria-label"));
        await page.EvaluateAsync("()=>chart.replaceSeries({id:'dc2',name:'DC2',values:[1,1,1,1]})");
        Assert.Equal("DC2, 2:00 – 5:00: Down", await page.Locator("[data-cfx-series='dc2']").GetAttributeAsync("aria-label"));
        await Assert.ThrowsAnyAsync<Exception>(() => page.EvaluateAsync("()=>chart.append({categories:[hour(6)],values:{dc9:[0]}})"));
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task ZeroArcsStaySelectableAndOverflowingSpansAreRejected() {
        await using var session = await OpenAsync(Page);
        var page = session.Page;
        await page.EvaluateAsync("()=>{window.picked=[];window.chart=ChartForgeXRenderer.create(document.getElementById('chart'),{type:'donut',label:'Findings',"
            + "categories:[{id:'zero',label:'None'},{id:'all',label:'All'}],series:[{id:'count',name:'Findings',values:[0,4]}]},{onSelect:mark=>picked.push(mark.categoryId)});}");
        await page.Locator("[data-cfx-category='zero'] [data-cfx-target]").ClickAsync(new() { Force = true });
        Assert.Equal("zero", await page.EvaluateAsync<string>("()=>picked[0]"));
        await page.EvaluateAsync("()=>chart.update({type:'gauge',label:'Load',minimum:0,maximum:10,categories:[{id:'now',label:'Now'}],series:[{id:'v',name:'Load',values:[0]}]})");
        Assert.Equal(1, await page.Locator("[data-cfx-series='v'] [data-cfx-target]").CountAsync());
        await Assert.ThrowsAnyAsync<Exception>(() => page.EvaluateAsync("()=>chart.update({type:'gauge',label:'x',minimum:-1e308,maximum:1e308,categories:[{id:'a',label:'a'}],series:[{id:'v',name:'v',values:[0]}]})"));
        await Assert.ThrowsAnyAsync<Exception>(() => page.EvaluateAsync("()=>chart.update({type:'donut',label:'x',categories:[{id:'a',label:'a'},{id:'b',label:'b'}],series:[{id:'v',name:'v',values:[1e308,1e308]}]})"));
        // A series id that names an inherited property still gets null for the new period.
        await page.EvaluateAsync("()=>chart.update({type:'line',label:'x',categories:[{id:'a',label:'a'}],series:[{id:'constructor',name:'C',values:[1]},{id:'v',name:'V',values:[2]}]})");
        await page.EvaluateAsync("()=>chart.append({categories:[{id:'b',label:'b'}],values:{v:[3]}})");
        Assert.Equal(1, await page.Locator("[data-cfx-series='constructor'] [data-cfx-ink]").CountAsync());
        AssertNoConsoleErrors(session);
    }
}
