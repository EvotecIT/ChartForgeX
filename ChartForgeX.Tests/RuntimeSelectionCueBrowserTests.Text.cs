using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class RuntimeSelectionCueBrowserTests {
    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NativeTextOnlySelectionKeepsItsPaintedCueAndIdentityWithoutFilters(bool annotation) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(640, 360).WithTitle("Native text selection")
            .WithTheme(VisualTheme.Graphite(), VisualThemeMode.Dark);
        var selector = "[data-cfx-role=word-cloud-term][data-cfx-point='0']";
        if (annotation) {
            chart.AddBar("Readings", new[] { new ChartPoint(1, 4), new ChartPoint(2, 10) });
            chart.Annotations.Add(new ChartAnnotation(ChartAnnotationKind.HorizontalBand, 6, 8, "Authored caption", ChartColor.FromHex("#ef4444"), 0));
            selector = "[data-cfx-role=annotation]";
        } else chart.AddWordCloud("Topics", new[] { new ChartWordCloudItem("Visible", 100), new ChartWordCloudItem("Companion", 50) });
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => Options(options, false, true)), 390, 680);
        var page = session.Page;
        await page.AddStyleTagAsync(new() { Content = ".cfx-stage svg,.cfx-stage svg *{filter:none!important}" });
        if (annotation) await page.AddStyleTagAsync(new() { Content = "[data-cfx-role=annotation-label-backplate]{display:none}" });
        await Bind(page);
        var target = page.Locator(selector).First;
        var id = await target.GetAttributeAsync("data-cfx-target-id");
        var kind = await target.GetAttributeAsync("data-cfx-target-kind");
        var text = await target.Locator("text").First.EvaluateAsync<string>("node=>node.outerHTML");
        Task<string> TextPaint() => target.Locator("text").First.EvaluateAsync<string>("""
            node=>JSON.stringify({text:node.textContent,paint:['fill','fill-opacity','stroke','stroke-opacity','font-family','font-weight','opacity']
                .map(key=>node.getAttribute(key))})
            """);
        var paint = await TextPaint();
        await page.EvaluateAsync("target=>cueController.select({targets:[target]})", new { targetKind = kind, targetId = id });
        await page.WaitForFunctionAsync("()=>cueRoot.querySelectorAll('[data-cfx-state-outline]').length===1");
        Assert.Equal(id, Assert.Single(await page.EvaluateAsync<string[]>("()=>cueController.getSelection().map(t=>t.targetId)")));
        Assert.Equal(text, await target.Locator("text").First.EvaluateAsync<string>("node=>node.outerHTML"));
        Assert.Equal(1, await page.Locator("[data-cfx-state-footprint]").CountAsync());
        var visible = PngReader.Decode(await page.Locator(".cfx-stage svg").ScreenshotAsync());
        await page.Locator("[data-cfx-state-outline]").EvaluateAsync("node=>node.style.visibility='hidden'");
        var hidden = PngReader.Decode(await page.Locator(".cfx-stage svg").ScreenshotAsync());
        Assert.True(ChangedPixels(visible, hidden) > 8);
        await page.Locator("[data-cfx-state-outline]").EvaluateAsync("node=>node.style.removeProperty('visibility')");
        var svg = await DownloadSvg(page);
        Assert.Contains("data-cfx-state-footprint", svg, StringComparison.Ordinal);
        await Capture(session, "selection-cue-text-" + annotation);
        await target.Locator("text").First.EvaluateAsync("node=>{node.dataset.cueOldStyle=node.getAttribute('style')??'';node.style.opacity='0';}");
        await page.WaitForFunctionAsync("()=>cueRoot.querySelectorAll('[data-cfx-state-outline]').length===0");
        await target.Locator("text").First.EvaluateAsync("node=>{const style=node.dataset.cueOldStyle;delete node.dataset.cueOldStyle;if(style)node.setAttribute('style',style);else node.removeAttribute('style');}");
        await page.WaitForFunctionAsync("()=>cueRoot.querySelectorAll('[data-cfx-state-outline]').length===1");
        await page.SetViewportSizeAsync(820, 680);
        await FocusWithKeyboardAsync(page, target);
        await AssertNativeKeyboardFocusAsync(page);
        await page.EvaluateAsync("()=>cueController.clearSelection()");
        Assert.Equal(0, await page.Locator("[data-cfx-state-outline]").CountAsync());
        Assert.Equal(paint, await TextPaint());
        AssertNoConsoleErrors(session);
    }
}
