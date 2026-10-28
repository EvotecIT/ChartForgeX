using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class WordCloudTargetOwnershipBrowserTests {
    [BrowserTheory]
    [InlineData("loaded")]
    [InlineData("unsupported")]
    [InlineData("rejected")]
    public async Task LoadedAndFallbackGlyphsKeepSelectionPaintPointerKeyboardAndAvailability(string delivery) {
        if (!Enabled) return;
        var chart = Frame().WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), VisualThemeMode.Dark)
            .AddWordCloud("Topics", new[] { new ChartWordCloudItem("Visible", 100), new ChartWordCloudItem("Companion", 50) });
        var html = chart.ToInteractiveHtmlPage(options => Options(options, false));
        await using var session = await OpenAsync(html, 800, 600, configurePageAsync: async page => {
            if (delivery == "unsupported") await page.EvaluateAsync("()=>window.FontFace=undefined");
            if (delivery == "rejected") await page.EvaluateAsync("()=>{const Actual=FontFace;window.FontFace=function(...args){const face=new Actual(...args);face.load=()=>Promise.reject(new Error('Intentional owned-font rejection'));return face;};}");
        }, waitForOwnedFonts: delivery == "loaded");
        var page = session.Page;
        if (delivery == "loaded") await page.WaitForFunctionAsync("()=>[...document.fonts].some(face=>face.status==='loaded')");
        else await page.WaitForFunctionAsync("()=>[...document.querySelectorAll('[data-cfx-owned-font-native]')].some(node=>getComputedStyle(node).display!=='none')");
        await page.EvaluateAsync("()=>{window.wordRoot=document.querySelector('.cfx-interactive-chart');window.wordController=ChartForgeXCharts.get(wordRoot);window.wordTargets=wordController.resolveTargets();wordController.select({targets:[wordTargets[0]]});}");
        Assert.Equal(2, await page.EvaluateAsync<int>("()=>wordTargets.length"));
        await AssertSelectionAsync(page, 1);
        var term = page.Locator("[data-cfx-role=word-cloud-term]").First;
        await term.FocusAsync(); await page.Keyboard.PressAsync("Space"); await term.BlurAsync();
        await AssertSelectionAsync(page, 0);
        await BrowserProofCaptions.ClickAsync(page, term); await AssertSelectionAsync(page, 1);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, "word-cloud-" + delivery + "-selection.png"), FullPage = true });
        }
        // Removing all real glyph paint must still make the target unavailable.
        await term.EvaluateAsync("node=>node.querySelectorAll('[data-cfx-native-text-ink],[data-cfx-owned-font-text]').forEach(ink=>ink.style.visibility='hidden')");
        await page.WaitForFunctionAsync("()=>wordRoot.querySelectorAll('[data-cfx-state-outline]').length===0");
        await term.EvaluateAsync("node=>node.querySelectorAll('[data-cfx-native-text-ink],[data-cfx-owned-font-text]').forEach(ink=>ink.style.removeProperty('visibility'))");
        await page.EvaluateAsync("()=>wordController.clearSelection()");
        await BrowserProofCaptions.ClickAsync(page, term); await AssertSelectionAsync(page, 1);
        AssertNoConsoleErrors(session);
    }
}
