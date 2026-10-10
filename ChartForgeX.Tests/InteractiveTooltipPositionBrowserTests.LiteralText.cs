using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipPositionBrowserTests {
    [Theory]
    [InlineData(340, false, false)]
    [InlineData(340, true, true)]
    [InlineData(950, true, false)]
    [InlineData(950, false, true)]
    public async Task NativeScheduleDetailWrapsInsidePointerAndKeyboardReadoutsWithoutTruncation(int width, bool dark, bool keyboard) {
        if (!Enabled) return;
        var detail = "JobCorrelationId=" + new string('a', 96) + " & \"retained\" <script>window.cfxDetailInjection=1</script> 'verbatim'";
        var start = new DateTime(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc);
        var chart = Chart.Create().WithSize(800, 440).WithTitle("Job execution")
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight()).WithXAxisTimeScale()
            .WithStateCategories(new VisualStatusTokens().SeverityCategories())
            .AddGanttLane("Worker", new[] { new ChartGanttLaneItem(start, start.AddHours(2), "info", "Export", detail) });
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Tooltip.Mode = HtmlChartTooltipMode.Single;
            options.Tooltip.Position.Anchor = HtmlChartTooltipAnchor.Node;
        }), width, 760);
        var page = session.Page;
        const string selector = "g[data-cfx-role=gantt-lane-item]";
        if (keyboard) {
            await page.Locator(selector).FocusAsync();
            await page.Keyboard.PressAsync("Space");
        } else await PointerAsync(page, selector);
        await WaitForTipAsync(page);
        var geometry = await AssertPositionAsync(page, HtmlChartTooltipAnchor.Node, new[] { HtmlChartTooltipPlacement.BottomRight }, selector: selector);
        var content = await page.EvaluateAsync<JsonElement>("""
            () => {
                const tip=document.querySelector('.cfx-tooltip'), bounds=node=>{const b=node.getBoundingClientRect();return {left:b.left,right:b.right,top:b.top,bottom:b.bottom};};
                const fields=Array.from(tip.querySelectorAll('.cfx-tooltip__title,dt,dd')).map(node=>{
                    const range=document.createRange();range.selectNodeContents(node);
                    return {tag:node.tagName,text:node.textContent,box:bounds(node),textBounds:Array.from(range.getClientRects()).map(b=>({left:b.left,right:b.right,top:b.top,bottom:b.bottom}))};
                });
                const detail=Array.from(tip.querySelectorAll('dt')).find(node=>node.textContent==='Detail')?.nextElementSibling;
                return {tip:bounds(tip),fields,detail:detail?.textContent,title:tip.querySelector('.cfx-tooltip__title').textContent,
                    injected:!!window.cfxDetailInjection,unexpectedElements:tip.querySelectorAll('script').length};
            }
            """);
        await CaptureAsync(page, $"position-literal-detail-{width}-{(dark ? "dark" : "light")}-{(keyboard ? "keyboard" : "pointer")}", new { detail, geometry, content }, session);
        Assert.Equal(detail, content.GetProperty("detail").GetString());
        Assert.Contains(detail, content.GetProperty("title").GetString()!, StringComparison.Ordinal);
        Assert.False(content.GetProperty("injected").GetBoolean());
        Assert.Equal(0, content.GetProperty("unexpectedElements").GetInt32());
        var tip = content.GetProperty("tip");
        foreach (var field in content.GetProperty("fields").EnumerateArray()) {
            foreach (var text in field.GetProperty("textBounds").EnumerateArray()) {
                Assert.InRange(text.GetProperty("left").GetDouble(), tip.GetProperty("left").GetDouble() - .15, tip.GetProperty("right").GetDouble() + .15);
                Assert.InRange(text.GetProperty("right").GetDouble(), tip.GetProperty("left").GetDouble() - .15, tip.GetProperty("right").GetDouble() + .15);
            }
        }
        AssertNoConsoleErrors(session);
    }
}
