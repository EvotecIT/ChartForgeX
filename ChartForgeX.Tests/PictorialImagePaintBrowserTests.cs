using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class PictorialImagePaintBrowserTests {
    [BrowserTheory]
    [InlineData(false, 390, false)] [InlineData(true, 390, true)] [InlineData(false, 900, true)] [InlineData(true, 900, false)]
    public async Task CapturedImagePaintOwnsRealKeyboardSelectionRectangularCuesAndCurrentReplacement(bool fromSvg, int width, bool dark) {
        if (!Enabled) return;
        var chart = Model(width, dark, "opaque", fromSvg); var first = chart.ToInteractiveHtmlFragment();
        await using var session = await BrowserProofFonts.OpenAsync(first, width + 24, 620); var page = session.Page;
        await page.EvaluateAsync("() => window.imagePaint = ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'))");
        var target = page.Locator(Target); await FocusWithKeyboardAsync(page, target); await page.Keyboard.PressAsync("Space");
        var actual = await Observe(page); await Record(page, "image-paint-selected-" + width + "-" + dark + "-" + fromSvg, actual);
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected")); Assert.Equal("team", actual.GetProperty("selection")[0].GetProperty("observationId").GetString());
        Assert.NotEqual(0, actual.GetProperty("cues").GetArrayLength()); Assert.Equal(0, actual.GetProperty("imageCues").GetInt32());
        foreach (var cue in actual.GetProperty("cues").EnumerateArray()) { Assert.True(cue.GetProperty("withinImage").GetBoolean()); Assert.True(cue.GetProperty("width").GetDouble() > 0); Assert.True(cue.GetProperty("height").GetDouble() > 0); }
        Assert.Equal("true", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-keyboard-focus"));
        Assert.Equal("2px", await page.Locator("[data-cfx-focus-ink]").EvaluateAsync<string>("node => getComputedStyle(node).strokeWidth"));
        chart.WithPictorialFillDirection(ChartPictorialFillDirection.BottomToTop); chart.Series[0].Points[0] = new ChartPoint(1, .75);
        // This real source change reassigns the same caller-owned observation identity explicitly.
        chart.Series[0].WithObservationId(0, new ChartObservationId("team"));
        await page.EvaluateAsync("html => imagePaint.update(html)", chart.ToInteractiveHtmlFragmentWithoutAssets());
        Assert.Equal("true", await page.Locator(Target).GetAttributeAsync("aria-selected")); Assert.Equal("0.75", await page.Locator(Target).GetAttributeAsync("data-cfx-value"));
        Assert.True(await page.Locator(Target).EvaluateAsync<bool>("node => node.getRootNode().activeElement===node"));
        await Record(page, "image-paint-updated-" + width + "-" + dark + "-" + fromSvg, await Observe(page)); AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData("transparent", 390, false)] [InlineData("transparent", 900, true)]
    [InlineData("clipped-away", 390, false)] [InlineData("clipped-away", 900, true)]
    [InlineData("opacity-zero", 390, false)] [InlineData("opacity-zero", 900, true)]
    [InlineData("hidden", 390, false)] [InlineData("hidden", 900, true)]
    public async Task TransparentClippedOrHostHiddenImageDoesNotBorrowVisibleCaptionAsDataPaint(string condition, int width, bool dark) {
        if (!Enabled) return;
        var chart = Model(width, dark, condition, false);
        await using var session = await BrowserProofFonts.OpenAsync(chart.ToInteractiveHtmlFragment(), width + 24, 620); var page = session.Page;
        if (condition is "opacity-zero" or "hidden") {
            await page.Locator(Target + " image").EvaluateAllAsync<object?>("(nodes,property) => nodes.forEach(node=>node.style.setProperty(property,property==='opacity'?'0':'hidden'))", condition == "hidden" ? "visibility" : "opacity");
            await page.WaitForFunctionAsync("""() => !document.querySelector("[data-cfx-role=pictorial-item][tabindex='0']")""");
        }
        var actual = await Observe(page); await Record(page, "image-paint-unavailable-" + condition + "-" + width + "-" + dark, actual);
        Assert.Equal(0, actual.GetProperty("selection").GetArrayLength()); Assert.Equal(0, actual.GetProperty("dataTabStops").GetInt32()); Assert.Equal(0, actual.GetProperty("cues").GetArrayLength());
        Assert.True(await page.Locator(Target + " text").CountAsync() > 0); // Descriptive captions alone never establish data paint.
        if (condition is "opacity-zero" or "hidden") {
            await page.Locator(Target + " image").EvaluateAllAsync<object?>("nodes => nodes.forEach(node=>{node.style.opacity='1';node.style.visibility='visible'})");
            await page.WaitForFunctionAsync("""() => !!document.querySelector("[data-cfx-role=pictorial-item][tabindex='0']")""");
            await FocusWithKeyboardAsync(page, page.Locator(Target)); await page.Keyboard.PressAsync("Space"); Assert.Equal("true", await page.Locator(Target).GetAttributeAsync("aria-selected"));
        }
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(390, false)] [InlineData(900, true)]
    public async Task OrdinaryContourPictorialPaintRemainsAKeyboardSelectionControl(int width, bool dark) {
        if (!Enabled) return;
        var chart = Model(width, dark, "opaque", false).WithPictorialArtwork(null);
        await using var session = await BrowserProofFonts.OpenAsync(chart.ToInteractiveHtmlFragment(), width + 24, 620);
        await FocusWithKeyboardAsync(session.Page, session.Page.Locator(Target)); await session.Page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await session.Page.Locator(Target).GetAttributeAsync("aria-selected")); AssertNoConsoleErrors(session);
    }
    private const string Target = "[data-cfx-role=pictorial-item][data-cfx-point='0']";
    private static Chart Model(int width, bool dark, string condition, bool fromSvg) {
        var pixels = new byte[8 * 4 * 4];
        if (condition != "transparent") for (var y = 0; y < 4; y++) for (var x = condition == "clipped-away" ? 6 : 0; x < 8; x++) {
            var at = (y * 8 + x) * 4; pixels[at] = 51; pixels[at + 1] = 102; pixels[at + 2] = 153; pixels[at + 3] = 255;
        }
        var artwork = fromSvg ? ChartPictorialArtwork.FromSvg(PictorialArtworkTests.ArtworkSvg, 8, 4) : ChartPictorialArtwork.FromImage(new RgbaImage(8, 4, pixels));
        var chart = PictorialArtworkTests.Model(.25).WithSize(width, 320).WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light).WithPictorialArtwork(artwork);
        chart.Series[0].WithObservationId(0, new ChartObservationId("team")); return chart;
    }
    private static Task<JsonElement> Observe(IPage page) => page.EvaluateAsync<JsonElement>("""
        () => {
          const root=document.querySelector('.cfx-interactive-chart'),images=[...root.querySelectorAll('[data-cfx-role=pictorial-item] image')],target=images[0]?.closest('[data-cfx-role=pictorial-item]');
          return {selection:ChartForgeXCharts.get(root).getSelection(),dataTabStops:root.querySelectorAll('[data-cfx-keyboard-component=data][tabindex="0"]').length,
            imageCues:root.querySelectorAll('[data-cfx-state-outline] image').length,
            cues:[...root.querySelectorAll('[data-cfx-state-outline] rect[data-cfx-state-ink]')].map(rect=>{const b=rect.getBBox();return{x:b.x,y:b.y,width:b.width,height:b.height,
              withinImage:images.some(image=>{const a=image.getBBox();return b.x>=a.x-.001&&b.y>=a.y-.001&&b.x+b.width<=a.x+a.width+.001&&b.y+b.height<=a.y+a.height+.001})}}),
            images:images.map(image=>({alpha:image.dataset.cfxImageAlphaBounds,opacity:getComputedStyle(image).opacity,visibility:getComputedStyle(image).visibility})),target:target?{...target.dataset}:null};
        }
        """);
    private static async Task Record(IPage page, string name, JsonElement actual) {
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return;
        if (!Path.IsPathRooted(output)) throw new InvalidOperationException("Captures require an absolute task-owned directory.");
        Directory.CreateDirectory(output); await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".browser.png"), FullPage = true });
        await File.WriteAllTextAsync(Path.Combine(output, name + ".json"), actual.GetRawText());
    }
}
