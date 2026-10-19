using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class RuntimeSelectionCueBrowserTests {
    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BranchSelectionAndHostHighlightKeepIndependentNativeCuesAcrossPeerHover(bool dark) {
        if (!Enabled) return;
        var chart = SunburstBranchHighlightExamples.CreateChart(360, dark);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            Options(options, false, true);
            options.Highlight.Mode = HtmlChartHighlightMode.RootBranch;
            options.Highlight.HighlightedItem.Opacity = 1;
            options.Highlight.UnhighlightedItem.Opacity = .2;
            options.Highlight.UnhighlightedBranch.Opacity = 0;
            options.Highlight.HighlightedItem.Stroke = ChartColor.FromRgb(20, 180, 230);
            options.Highlight.HighlightedItem.StrokeWidth = 2;
        }), 390, 680);
        var page = session.Page;
        await page.AddStyleTagAsync(new() { Content = ".cfx-stage svg,.cfx-stage svg *{filter:none!important}" });
        await Bind(page);
        var native = await page.EvaluateAsync<string>("()=>JSON.stringify([...cueRoot.querySelectorAll('[data-cfx-role=sunburst-segment-mark],[data-cfx-role=sunburst-pattern]')].map(n=>n.outerHTML))");
        await page.EvaluateAsync("""
            ()=>{const target=id=>({targets:[{targetKind:'node',targetId:cueRoot.querySelector('[data-cfx-node="'+id+'"]').dataset.cfxTargetId}]});
                cueController.select(target('engineering-support'));cueController.highlight(target('platform'),{dim:false});}
            """);
        await AssertCues(page, 2);
        await MoveToNative(page, "operations-support");
        await page.WaitForFunctionAsync("()=>cueRoot.querySelector('[data-cfx-node=operations-support]').dataset.cfxHighlightState==='item'");
        await AssertCues(page, 2);
        Assert.Equal(new[] { "1", "1" }, await page.EvaluateAsync<string[]>("""()=>['engineering-support','platform'].map(id=>getComputedStyle(cueRoot.querySelector('[data-cfx-node="'+id+'"] > [data-cfx-highlight-paint]')).opacity)"""));
        Assert.Equal(native, await page.EvaluateAsync<string>("()=>JSON.stringify([...cueRoot.querySelectorAll('[data-cfx-role=sunburst-segment-mark],[data-cfx-role=sunburst-pattern]')].map(n=>n.outerHTML))"));
        await Capture(session, "selection-cue-branch-" + dark);
        await page.EvaluateAsync("()=>{cueController.clearSelection();cueController.clearHighlight();const zero=cueRoot.querySelector('[data-cfx-node=reserve]');cueController.select({targets:[{targetKind:'node',targetId:zero.dataset.cfxTargetId}]});}");
        await AssertCues(page, 0);
        var zero = page.Locator("[data-cfx-node=reserve]");
        await FocusWithKeyboardAsync(page, zero);
        Assert.Contains("0", await TooltipTextAsync(page), StringComparison.Ordinal);
        await page.EvaluateAsync("()=>cueController.clearSelection()");
        await AssertCues(page, 0);
        AssertNoConsoleErrors(session);
    }

    private static async Task MoveToNative(IPage page, string id) {
        var point = await NativeInteriorPointAsync(page, "[data-cfx-node='" + id + "']", "[data-cfx-role=sunburst-segment-mark]");
        await page.Mouse.MoveAsync((float)point[0], (float)point[1], new() { Steps = 4 });
    }
}
