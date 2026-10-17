using System.Text.Json;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveChartLifecycleBrowserTests {
    [BrowserTheory]
    [InlineData(false, 1440)]
    [InlineData(true, 1440)]
    [InlineData(false, 390)]
    [InlineData(true, 390)]
    public async Task LinkedExampleFiltersPreparedTotalsAndHighlightsThroughPublicControllers(bool dark, int width) {
        if (!Enabled) return;
        await using var session = await OpenAsync(LinkedChartViewsExamples.CreatePage(dark), width, 1000);
        var page = session.Page;
        await page.EvaluateAsync("""
            () => {
                window.linkedEvents=[];
                window.linkedBrushEvents=[];
                document.body.addEventListener('cfxselectionchange',event=>linkedEvents.push(event.detail));
                document.body.addEventListener('cfxbrush',event=>linkedBrushEvents.push(event.detail));
            }
            """);
        const string timeSelector = "[data-cfx-chart-id=linked-time]";
        await page.Locator(timeSelector + " [data-cfx-mode-button=brush]").ClickAsync();
        var positions = await page.Locator(timeSelector).EvaluateAsync<JsonElement>("""
            root => {
                const first=root.querySelector('g[data-cfx-role=point][data-cfx-observation-id=api-1]').getBoundingClientRect();
                const last=root.querySelector('g[data-cfx-role=point][data-cfx-observation-id=api-2]').getBoundingClientRect();
                const from={x:Math.fround(first.x+first.width/2-1),y:Math.fround(first.y+first.height/2)},
                    to={x:Math.fround(last.x+last.width/2+1),y:Math.fround(last.y+last.height/2)};
                const projection=JSON.parse(root.dataset.cfxPreparedChart).dataProjection,axis=projection.x,p=projection.plotBounds;
                const inverse=root.querySelector('.cfx-stage svg').getScreenCTM().inverse();
                const raw=[from,to].map(point=>axis.minimum+(new DOMPoint(point.x,point.y).matrixTransform(inverse).x-p.x)/p.width*(axis.maximum-axis.minimum)).sort((a,b)=>a-b);
                const ticks=[...axis.displayTicks,...axis.displayBounds].filter(tick=>tick.value>=axis.minimum&&tick.value<=axis.maximum).sort((a,b)=>a.value-b.value);
                const enclosing=[ticks.filter(tick=>tick.value<=raw[0]).at(-1),ticks.find(tick=>tick.value>=raw[1])];
                return {from,to,expectedRange:enclosing.map(tick=>tick.instant)};
            }
            """);
        await page.Mouse.MoveAsync((float)positions.GetProperty("from").GetProperty("x").GetDouble(),
            (float)positions.GetProperty("from").GetProperty("y").GetDouble());
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync((float)positions.GetProperty("to").GetProperty("x").GetDouble(),
            (float)positions.GetProperty("to").GetProperty("y").GetDouble(), new MouseMoveOptions { Steps = 5 });
        await page.Mouse.UpAsync();
        Assert.False(await page.Locator(timeSelector + " .cfx-brush-box").EvaluateAsync<bool>("node=>node.hidden"));
        var brush = await page.EvaluateAsync<JsonElement>("() => linkedBrushEvents[0]");
        var expectedRange = positions.GetProperty("expectedRange").EnumerateArray().Select(endpoint => endpoint.GetString()!).ToArray();
        Assert.Equal(expectedRange, brush.GetProperty("range").GetProperty("x").EnumerateArray().Select(endpoint => endpoint.GetString()));
        Assert.Equal(brush.GetProperty("range").GetProperty("x").GetRawText(), brush.GetProperty("displayRange").GetProperty("x").GetRawText());
        var firstDay = new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc);
        var apiValues = new[] { 24, 31, 27, 36 };
        var from = DateTimeOffset.Parse(expectedRange[0]).UtcDateTime;
        var to = DateTimeOffset.Parse(expectedRange[1]).UtcDateTime;
        var expectedDays = Enumerable.Range(0, apiValues.Length).Where(day => firstDay.AddDays(day) >= from && firstDay.AddDays(day) <= to).ToArray();
        Assert.NotEmpty(expectedDays);
        Assert.Contains("Showing " + expectedDays.Length + " days", await page.Locator("#linked-status").InnerTextAsync());
        var selectedIds = await page.Locator(timeSelector).EvaluateAsync<string[]>("root => ChartForgeXCharts.get(root).getSelection().map(target=>target.observationId).sort()");
        Assert.Equal(expectedDays.SelectMany(day => new[] { "api-" + day, "worker-" + day }).OrderBy(id => id, StringComparer.Ordinal), selectedIds);
        const string api = "[data-cfx-chart-id=linked-totals] g[data-cfx-role=point][data-cfx-observation-id=api-total]";
        await page.Locator(api).ClickAsync();
        var selection = await page.EvaluateAsync<JsonElement>("""
            () => ChartForgeXCharts.get(document.querySelector('[data-cfx-chart-id="linked-totals"]')).getSelection()
            """);
        var target = Assert.Single(selection.EnumerateArray());
        Assert.Equal("API", target.GetProperty("category").GetString());
        Assert.Equal(expectedDays.Sum(day => apiValues[day]), target.GetProperty("numericValue").GetDouble());
        Assert.Equal(2, await page.EvaluateAsync<int>("()=>linkedEvents.length"));
        Assert.Equal("lasso", await page.EvaluateAsync<string>("()=>linkedEvents[0].reason"));
        Assert.True(await page.EvaluateAsync<bool>("""
            () => {
                const time=document.querySelector('[data-cfx-chart-id="linked-time"]');
                const api=[...time.querySelectorAll('g[data-cfx-role=point][data-cfx-series-key=api]')];
                const worker=[...time.querySelectorAll('g[data-cfx-role=point][data-cfx-series-key=worker]')];
                return api.length===4 && worker.length===4
                    && api.every(mark=>mark.dataset.cfxHighlighted==='true')
                    && worker.every(mark=>mark.getAttribute('aria-selected')==='true'
                        ? Number(getComputedStyle(mark).opacity)>.9 : Number(getComputedStyle(mark).opacity)<=.35);
            }
            """));
        await CaptureAsync(page, "linked-example-brushed-" + width + "-" + (dark ? "dark" : "light"), selection);
        await page.Locator("#linked-reset").ClickAsync();
        Assert.Contains("Showing every day", await page.Locator("#linked-status").InnerTextAsync());
        Assert.True(await page.Locator(timeSelector + " .cfx-brush-box").EvaluateAsync<bool>("node=>node.hidden"));
        Assert.False(await page.Locator(timeSelector + " [data-cfx-mode-button=brush]").EvaluateAsync<bool>(
            "node=>node.getAttribute('aria-pressed')==='true'"));
        Assert.True(await page.EvaluateAsync<bool>("""
            () => [...document.querySelectorAll('.cfx-interactive-chart')].every(root=>
                ChartForgeXCharts.get(root).getSelection().length===0
                && !root.querySelector('[data-cfx-highlighted=true]'))
            """));
        Assert.Equal(2, await page.EvaluateAsync<int>("()=>linkedEvents.length"));
        if (width == 390) {
            Assert.True(await page.EvaluateAsync<bool>("""
                () => [...document.querySelectorAll('.cfx-stage')].every(stage=>stage.scrollWidth<=stage.clientWidth+1)
                """));
        }
        await CaptureAsync(page, "linked-example-cleared-" + width + "-" + (dark ? "dark" : "light"));
        AssertNoConsoleErrors(session);
    }
}
