using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Checks standalone page framing and the native chart's print presentation in Chromium.</summary>
public sealed class InteractiveHtmlPagePolishBrowserTests {
    [BrowserTheory]
    [InlineData(false, false, 380, HtmlChartResponsiveLayout.Fit)]
    [InlineData(false, false, 980, HtmlChartResponsiveLayout.Readable)]
    [InlineData(false, true, 380, HtmlChartResponsiveLayout.Readable)]
    [InlineData(false, true, 980, HtmlChartResponsiveLayout.Fit)]
    [InlineData(true, false, 380, HtmlChartResponsiveLayout.Fit)]
    [InlineData(true, false, 980, HtmlChartResponsiveLayout.Readable)]
    [InlineData(true, true, 380, HtmlChartResponsiveLayout.Readable)]
    [InlineData(true, true, 980, HtmlChartResponsiveLayout.Fit)]
    public async Task StandalonePagesPrintTheCompleteNativeChartAndRestoreTheScreenView(
        bool externalAssets, bool dark, int width, HtmlChartResponsiveLayout layout) {
        if (!Enabled) return;
        var chart = StatusLines(dark);
        var html = chart.ToInteractiveHtmlPage(options => {
            options.ResponsiveLayout = layout;
            options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan);
            if (externalAssets) options.ExternalAssets = new HtmlAssetReferences("https://assets.example.test/");
        });
        await using var session = await OpenAsync("<!doctype html><html><body></body></html>", width, 620);
        var page = session.Page;
        var errors = new List<string>();
        page.PageError += (_, error) => errors.Add(error);
        if (externalAssets) await page.RouteAsync("https://assets.example.test/**", async route => {
            var asset = HtmlInteractiveAssetFiles.Charts().Single(file => route.Request.Url.EndsWith(file.FileName, StringComparison.Ordinal));
            await route.FulfillAsync(new RouteFulfillOptions { ContentType = asset.ContentType, Body = asset.Content });
        });
        await page.SetContentAsync(html);
        var name = (externalAssets ? "external" : "inline") + "-" + (dark ? "dark" : "light") + "-" + width + "-" + layout;
        var original = await ReadStateAsync(page);
        var originalX = original.GetProperty("viewport").GetProperty("x");
        var originalSpan = originalX.GetProperty("maximum").GetDouble() - originalX.GetProperty("minimum").GetDouble();
        await page.Locator(".cfx-interactive-chart").HoverAsync();
        await page.Locator("[data-cfx-zoom=in]").ClickAsync();
        await page.WaitForFunctionAsync("span => {const r=document.querySelector('.cfx-interactive-chart'),x=window.ChartForgeXCharts.get(r).getViewport().x;return x.maximum-x.minimum<span;}", originalSpan);
        // Keep the focused observation inside the zoomed plot; edge observations may be clipped.
        await page.Locator(Point(0, 3)).FocusAsync();
        await page.WaitForFunctionAsync("() => document.querySelector('.cfx-interactive-chart').getAnimations({subtree:true}).every(animation=>animation.playState!=='running')");
        var pointerFocus = await ReadStateAsync(page);
        await CaptureAsync(page, name + "-pointer-focus-screen", pointerFocus);
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { Media = Media.Print });
        await WaitForPrintProjectionAsync(page, true);
        var pointerPrinted = await ReadStateAsync(page);
        await CaptureAsync(page, name + "-pointer-focus-print", pointerPrinted);
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { Media = Media.Screen });
        await WaitForPrintProjectionAsync(page, false);
        await page.Locator(Legend(1)).PressAsync("Space");
        await page.Locator(Point(0, 3)).FocusAsync();
        await page.WaitForFunctionAsync("() => document.querySelector('.cfx-interactive-chart').getAnimations({subtree:true}).every(animation=>animation.playState!=='running')");
        var screen = await ReadStateAsync(page);
        await CaptureAsync(page, name + "-screen", screen);
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { Media = Media.Print });
        await WaitForPrintProjectionAsync(page, true);
        var printed = await ReadStateAsync(page);
        await CaptureAsync(page, name + "-print", printed);
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { Media = Media.Screen });
        await WaitForPrintProjectionAsync(page, false);
        await page.WaitForFunctionAsync("() => document.querySelector('.cfx-interactive-chart').getAnimations({subtree:true}).every(animation=>animation.playState!=='running')");
        var restored = await ReadStateAsync(page);
        Assert.Empty(errors);
        AssertNoConsoleErrors(session);

        Assert.Equal("antialiased", screen.GetProperty("smoothing").GetString());
        Assert.Equal("geometricprecision", screen.GetProperty("textRendering").GetString()!.ToLowerInvariant());
        Assert.True(screen.GetProperty("toolbarVisible").GetBoolean());
        Assert.True(screen.GetProperty("tooltipVisible").GetBoolean());
        var screenX = screen.GetProperty("viewport").GetProperty("x");
        Assert.True(screenX.GetProperty("maximum").GetDouble() - screenX.GetProperty("minimum").GetDouble() < originalSpan);
        Assert.NotEqual(original.GetProperty("shapeGeometry").GetRawText(), pointerFocus.GetProperty("shapeGeometry").GetRawText());
        Assert.NotEqual(original.GetProperty("shapeGeometry").GetRawText(), screen.GetProperty("shapeGeometry").GetRawText());
        Assert.Equal(original.GetProperty("scale").GetDouble(), screen.GetProperty("scale").GetDouble(), 3);
        Assert.Equal(original.GetProperty("frameWidth").GetDouble(), pointerFocus.GetProperty("frameWidth").GetDouble(), 3);
        Assert.Equal(original.GetProperty("frameHeight").GetDouble(), pointerFocus.GetProperty("frameHeight").GetDouble(), 3);
        Assert.InRange(screen.GetProperty("documentOverflow").GetDouble(), 0, 1);
        Assert.Equal("rgba(0, 0, 0, 0)", printed.GetProperty("bodyBackground").GetString());
        Assert.Equal("rgba(0, 0, 0, 0)", printed.GetProperty("shellBackground").GetString());
        Assert.False(printed.GetProperty("toolbarVisible").GetBoolean());
        Assert.False(printed.GetProperty("tooltipVisible").GetBoolean());
        Assert.False(printed.GetProperty("resetVisible").GetBoolean());
        Assert.Equal(0, pointerPrinted.GetProperty("browserAxisCount").GetInt32());
        Assert.Equal(0, printed.GetProperty("browserAxisCount").GetInt32());
        Assert.Equal(original.GetProperty("sourceAxisGeometry").GetRawText(), pointerPrinted.GetProperty("sourceAxisGeometry").GetRawText());
        Assert.Equal(original.GetProperty("sourceAxisGeometry").GetRawText(), printed.GetProperty("sourceAxisGeometry").GetRawText());
        foreach (var snapshot in new[] { pointerPrinted, printed }) {
            Assert.True(snapshot.GetProperty("sourceAxisDisplay").GetArrayLength() > 0);
            Assert.All(snapshot.GetProperty("sourceAxisDisplay").EnumerateArray(), axis => Assert.NotEqual("none", axis[1].GetString()));
            Assert.False(snapshot.GetProperty("responsiveHeadingVisible").GetBoolean());
            Assert.True(snapshot.GetProperty("sourceHeadingVisibility").GetArrayLength() > 0);
            Assert.All(snapshot.GetProperty("sourceHeadingVisibility").EnumerateArray(), heading => Assert.Equal("visible", heading.GetString()));
        }
        Assert.True(pointerFocus.GetProperty("nativePointFocused").GetBoolean());
        Assert.True(pointerPrinted.GetProperty("nativePointFocused").GetBoolean());
        Assert.False(pointerFocus.GetProperty("focusVisible").GetBoolean());
        Assert.Equal("none", pointerFocus.GetProperty("focusOutlineStyle").GetString());
        Assert.Equal("none", pointerPrinted.GetProperty("focusOutlineStyle").GetString());
        Assert.False(pointerFocus.GetProperty("nativeFocusVisible").GetBoolean());
        Assert.False(pointerPrinted.GetProperty("nativeFocusVisible").GetBoolean());
        Assert.Equal(pointerFocus.GetProperty("paint").GetRawText(), pointerPrinted.GetProperty("paint").GetRawText());
        Assert.Equal(original.GetProperty("shapeGeometry").GetRawText(), pointerPrinted.GetProperty("shapeGeometry").GetRawText());
        Assert.True(screen.GetProperty("nativePointFocused").GetBoolean());
        Assert.True(printed.GetProperty("nativePointFocused").GetBoolean());
        Assert.True(screen.GetProperty("focusVisible").GetBoolean());
        Assert.True(screen.GetProperty("nativeFocusVisible").GetBoolean());
        Assert.False(printed.GetProperty("nativeFocusVisible").GetBoolean());
        Assert.True(restored.GetProperty("nativeFocusVisible").GetBoolean());
        Assert.Equal("none", printed.GetProperty("focusOutlineStyle").GetString());
        Assert.Equal(screen.GetProperty("focusOutlineStyle").GetString(), restored.GetProperty("focusOutlineStyle").GetString());
        Assert.Equal(screen.GetProperty("focusOutlineWidth").GetString(), restored.GetProperty("focusOutlineWidth").GetString());
        Assert.Equal(screen.GetProperty("paint").GetRawText(), printed.GetProperty("paint").GetRawText());
        Assert.Equal(screen.GetProperty("paint").GetRawText(), restored.GetProperty("paint").GetRawText());
        Assert.Equal(original.GetProperty("shapeGeometry").GetRawText(), printed.GetProperty("shapeGeometry").GetRawText());
        Assert.Equal(screen.GetProperty("shapeGeometry").GetRawText(), restored.GetProperty("shapeGeometry").GetRawText());
        Assert.Equal(screen.GetProperty("viewport").GetRawText(), printed.GetProperty("viewport").GetRawText());
        Assert.Equal(screen.GetProperty("viewport").GetRawText(), restored.GetProperty("viewport").GetRawText());
        Assert.Equal("true", screen.GetProperty("mutedLegend").GetString());
        Assert.Equal("true", printed.GetProperty("mutedLegend").GetString());
        Assert.Equal("true", restored.GetProperty("mutedLegend").GetString());
        Assert.Equal("none", printed.GetProperty("transform").GetString());
        Assert.InRange(printed.GetProperty("stageOverflow").GetDouble(), 0, 1);
        Assert.Equal(printed.GetProperty("stageWidth").GetDouble(), printed.GetProperty("renderedWidth").GetDouble(), 1);
        Assert.InRange(printed.GetProperty("documentOverflow").GetDouble(), 0, 1);
        Assert.Equal(screen.GetProperty("viewBox").GetString(), printed.GetProperty("viewBox").GetString());
        Assert.Equal(printed.GetProperty("nativeAspect").GetDouble(), printed.GetProperty("renderedAspect").GetDouble(), 3);
        Assert.Equal(screen.GetProperty("scale").GetDouble(), restored.GetProperty("scale").GetDouble(), 3);
        Assert.Equal(screen.GetProperty("frameWidth").GetDouble(), restored.GetProperty("frameWidth").GetDouble(), 3);
        Assert.Equal(screen.GetProperty("frameHeight").GetDouble(), restored.GetProperty("frameHeight").GetDouble(), 3);
        Assert.True(restored.GetProperty("toolbarVisible").GetBoolean());
    }

    private static async Task WaitForPrintProjectionAsync(IPage page, bool printing) {
        await page.WaitForFunctionAsync("printing=>document.querySelector('.cfx-interactive-chart')._cfxStandaloneViewport?.printing===printing", printing);
        // Print layout can deliver ResizeObserver after the media event itself.
        await page.EvaluateAsync("()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))");
    }

    private static Task<JsonElement> ReadStateAsync(IPage page) => page.EvaluateAsync<JsonElement>("""
        () => {
            const root=document.querySelector('.cfx-interactive-chart'), stage=root.querySelector('.cfx-stage'), svg=stage.querySelector('svg');
            const visible=selector=>{const node=document.querySelector(selector);return !!node && !node.hidden && getComputedStyle(node).display!=='none';};
            const bounds=svg.getBoundingClientRect(), box=svg.viewBox.baseVal;
            const focused=document.activeElement, focusStyle=getComputedStyle(focused);
            const series=[...svg.querySelectorAll('[data-cfx-role="series"]')];
            const marks=series.flatMap(owner=>[...owner.querySelectorAll('path,line,rect,circle,ellipse,polygon,polyline')])
                .filter(node=>!node.closest('defs,[data-cfx-native-text-ink]')&&!node.hasAttribute('data-cfx-browser-hit-area'));
            const shapeGeometry=marks.map(node=>[node.closest('[data-cfx-role="series"]').dataset.cfxSeries,node.localName,
                ...['d','points','x','x1','x2','cx','y','y1','y2','cy','width','height','r','rx','ry'].map(name=>node.getAttribute(name))]);
            // Browser axis layers and projection visibility are geometry, covered separately.
            // Retain authored inline paint, computed stroke and opacity on every source series.
            const paint=series.flatMap(owner=>[owner,...owner.querySelectorAll('[stroke],[opacity]')])
                .filter(node=>node.hasAttribute('stroke')||node.hasAttribute('opacity'))
                .filter(node=>!node.closest('defs,[data-cfx-native-text-ink]'))
                .map(node=>[node.localName,node.getAttribute('data-cfx-role'),node.getAttribute('stroke'),node.getAttribute('opacity'),
                    [...node.style].filter(name=>name!=='visibility').map(name=>[name,node.style.getPropertyValue(name),node.style.getPropertyPriority(name)]),
                    getComputedStyle(node).stroke,getComputedStyle(node).opacity]);
            return {
                smoothing:getComputedStyle(document.body).webkitFontSmoothing,textRendering:getComputedStyle(document.body).textRendering,
                bodyBackground:getComputedStyle(document.body).backgroundColor,shellBackground:getComputedStyle(document.querySelector('.cfx-shell')).backgroundColor,
                toolbarVisible:visible('.cfx-toolbar'),tooltipVisible:visible('.cfx-tooltip'),resetVisible:visible('.cfx-reset'),
                viewport:window.ChartForgeXCharts.get(root).getViewport(),shapeGeometry,paint,
                browserAxisCount:svg.querySelectorAll('[data-cfx-browser-axis]').length,
                sourceAxisDisplay:[...svg.querySelectorAll('[data-cfx-role^="axis-"][data-cfx-role$="-label"]')].map(node=>[node.dataset.cfxRole,getComputedStyle(node).display]),
                sourceAxisGeometry:[...svg.querySelectorAll('[data-cfx-role^="axis-"][data-cfx-role$="-label"]')].map(node=>[node.dataset.cfxRole,node.getAttribute('transform'),node.getAttribute('x'),node.getAttribute('y')]),
                responsiveHeadingVisible:visible('.cfx-responsive-heading'),
                sourceHeadingVisibility:[...svg.querySelectorAll('[data-cfx-role="frame-heading-source"],[data-cfx-role="frame-subtitle-source"]')].map(node=>getComputedStyle(node).visibility),
                transform:getComputedStyle(svg).transform,scale:new DOMMatrix(getComputedStyle(svg).transform).a,viewBox:svg.getAttribute('viewBox'),
                stageOverflow:Math.max(0,stage.scrollWidth-stage.clientWidth),documentOverflow:Math.max(0,document.documentElement.scrollWidth-innerWidth),
                nativeAspect:box.width/box.height,renderedAspect:bounds.width/bounds.height,renderedWidth:bounds.width,stageWidth:stage.getBoundingClientRect().width,
                frameWidth:bounds.width,frameHeight:bounds.height,
                nativePointFocused:focused.matches('g[data-cfx-role="point"][data-cfx-series="0"][data-cfx-point="3"]'),
                focusVisible:focused.matches(':focus-visible'),
                nativeFocusVisible:visible('[data-cfx-keyboard-focus-outline]'),
                focusOutlineStyle:focusStyle.outlineStyle,focusOutlineWidth:focusStyle.outlineWidth,focusOutlineColor:focusStyle.outlineColor,
                mutedLegend:root.querySelector('[data-cfx-role="legend-item"][data-cfx-series="1"]').dataset.cfxMuted
            };
        }
        """);

    private static async Task CaptureAsync(IPage page, string name, JsonElement evidence) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Capture output must be an absolute path.");
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, "page-polish-" + name + ".png"), FullPage = true });
        await File.WriteAllTextAsync(Path.Combine(directory, "page-polish-" + name + ".json"), evidence.GetRawText());
    }
}
