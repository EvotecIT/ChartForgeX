using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>A bounded engine-independent acceptance path; trusted Chromium touch/CDP proof stays in its owning suite.</summary>
[Trait("BrowserSuite", "Compatibility")]
public sealed class InteractiveBrowserCompatibilityTests {
    private const string Root = ".cfx-interactive-chart";
    private const string North = "[data-cfx-role=point][data-cfx-observation-id=north]";
    private const string South = "[data-cfx-role=point][data-cfx-observation-id=south]";

    [BrowserTheory]
    [InlineData(false, 390)]
    [InlineData(true, 1000)]
    public async Task OrdinaryChartKeepsPointerKeyboardAndUpdateStateAcrossItsLifetime(bool dark, int width) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Observations(dark).ToInteractiveHtmlPage(Configure), width, 720);
        Assert.Equal(Engine.ToString().ToLowerInvariant(), session.Browser?.BrowserType.Name);
        var page = session.Page;
        await page.Locator(Root).EvaluateAsync("""
            root=>{
                window.compatibilityInput=[];
                for(const type of ['pointerenter','pointermove','focusin'])root.addEventListener(type,event=>{
                    const target=event.target.closest?.('[data-cfx-target-kind]');
                    compatibilityInput.push({type,trusted:event.isTrusted,x:event.clientX,y:event.clientY,target:target?.dataset.cfxTargetId});
                },true);
            }
            """);
        await MoveToAsync(page, North);
        try { await WaitForReadoutAsync(page, "12"); }
        catch { await CaptureAsync(page, "ordinary-pointer-failure-" + width); throw; }
        Assert.Contains("12", await TooltipTextAsync(page));
        await FocusWithKeyboardAsync(page, page.Locator(North));
        await AssertNativeKeyboardFocusAsync(page);
        await page.Keyboard.PressAsync("ArrowRight");
        Assert.True(await page.Locator(South).EvaluateAsync<bool>("node=>node.getRootNode().activeElement===node"));
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await page.Locator(South).GetAttributeAsync("aria-selected"));
        await page.Locator(Root).EvaluateAsync("root=>window.compatibilityController=ChartForgeXCharts.mount(root)");
        await page.EvaluateAsync("html=>compatibilityController.update(html)", Observations(dark, updated: true).ToInteractiveHtmlFragmentWithoutAssets(Configure));
        Assert.Equal("17", await page.Locator(South).GetAttributeAsync("data-cfx-y"));
        Assert.Equal("true", await page.Locator(South).GetAttributeAsync("aria-selected"));
        Assert.True(await page.Locator(South).EvaluateAsync<bool>("node=>node.getRootNode().activeElement===node"));
        await page.Keyboard.PressAsync("ArrowLeft");
        await page.Keyboard.PressAsync("ArrowRight");
        await WaitForReadoutAsync(page, "17");
        Assert.Contains("17", await TooltipTextAsync(page));
        await AssertNativeKeyboardFocusAsync(page);
        await CaptureAsync(page, "ordinary-" + width);
        await FocusWithKeyboardAsync(page, page.Locator("[data-cfx-role=legend-item]"));
        await AssertNativeKeyboardFocusAsync(page);
        await CaptureAsync(page, "legend-focus-" + width);
        await page.Locator(South).ClickAsync();
        Assert.Equal(0, await page.Locator("[data-cfx-keyboard-focus-outline]").CountAsync());
        Assert.True(await page.EvaluateAsync<bool>("""
            ()=>{
                const previous=compatibilityController,root=previous.root;
                previous.dispose();
                if(ChartForgeXCharts.get(root)!==null)return false;
                if(root.querySelector('[data-cfx-keyboard-focus-outline]'))return false;
                const current=ChartForgeXCharts.mount(root);
                return current!==previous&&current.root===root&&current.getSelection().length===0;
            }
            """));
        await MoveAwayAsync(page);
        await MoveToAsync(page, South);
        await WaitForReadoutAsync(page, "17");
        Assert.Contains("17", await TooltipTextAsync(page));
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false, 390)]
    [InlineData(true, 1000)]
    public async Task ConnectedWindowExportsItsCommittedSceneAndResetsWithoutChromiumCommands(bool dark, int width) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(700, 400).WithTitle("Connected window compatibility").WithLegend(false)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .AddSeries(ChartSeries.CreateLine(new ChartSeriesId("measurements"), "Measurements",
                Enumerable.Range(0, 13).Select(index => new ChartPoint(index, 4 + index % 5))));
        var renderer = new HtmlInteractiveChartRenderer();
        void WindowOptions(HtmlChartInteractionOptions options) {
            Configure(options);
            options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Export);
        }
        var initial = renderer.PrepareSection(chart, configure: WindowOptions);
        var html = "<!doctype html><html><head><style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle()
            + "</style></head><body>" + initial.Html + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script></body></html>";
        await using var session = await OpenAsync(html, width, 720);
        Assert.Equal(Engine.ToString().ToLowerInvariant(), session.Browser?.BrowserType.Name);
        var page = session.Page;
        await page.ExposeFunctionAsync("prepareCompatibilityWindow", (JsonElement request) => {
            // This smoke sends one zoom from the immutable initial scene, then a reset.
            var viewport = request.GetProperty("kind").GetString() == "reset" ? null
                : initial.PreparedVisual.XProjection!.ConstrainViewport(request.GetProperty("left").GetDouble(), request.GetProperty("right").GetDouble());
            return renderer.PrepareSection(chart, viewport, WindowOptions).Html;
        });
        await page.Locator(Root).EvaluateAsync("""
            root=>{
                window.compatibilityChanges=[];
                root.addEventListener('cfxviewportchange',event=>compatibilityChanges.push(event.detail));
                window.compatibilityController=ChartForgeXCharts.mount(root);
                compatibilityController.connectXViewport(request=>prepareCompatibilityWindow(request));
            }
            """);
        await page.Locator("[data-cfx-zoom=in]").ClickAsync();
        await page.WaitForFunctionAsync("()=>compatibilityChanges.length===1");
        var projection = await page.Locator(Root).EvaluateAsync<JsonElement>("root=>JSON.parse(root.dataset.cfxPreparedChart).xProjection");
        Assert.True(projection.GetProperty("minimum").GetDouble() > projection.GetProperty("fullMinimum").GetDouble());
        Assert.True(projection.GetProperty("maximum").GetDouble() < projection.GetProperty("fullMaximum").GetDouble());
        var labels = await page.Locator("[data-cfx-role=axis-x-label]").AllTextContentsAsync();
        var svgDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var svgPath = await svgDownload.PathAsync() ?? throw new InvalidOperationException("The committed window produced no SVG download.");
        var svg = XDocument.Load(svgPath);
        Assert.Equal(labels, svg.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "axis-x-label").Select(node => node.Value));
        var pngDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=png]").ClickAsync());
        var pngPath = await pngDownload.PathAsync() ?? throw new InvalidOperationException("The committed window produced no PNG download.");
        var png = await File.ReadAllBytesAsync(pngPath);
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, png.Take(8));
        var captures = CaptureDirectory();
        if (captures != null) {
            await svgDownload.SaveAsAsync(Path.Combine(captures, "compatibility-" + Engine.ToString().ToLowerInvariant() + "-window-" + width + ".svg"));
            await pngDownload.SaveAsAsync(Path.Combine(captures, "compatibility-" + Engine.ToString().ToLowerInvariant() + "-window-" + width + ".png"));
        }
        await CaptureAsync(page, "window-" + width);
        await page.Locator("[data-cfx-reset]").ClickAsync();
        await page.WaitForFunctionAsync("()=>compatibilityChanges.length===2");
        var reset = await page.Locator(Root).EvaluateAsync<JsonElement>("root=>JSON.parse(root.dataset.cfxPreparedChart).xProjection");
        Assert.Equal(reset.GetProperty("fullMinimum").GetDouble(), reset.GetProperty("minimum").GetDouble());
        Assert.Equal(reset.GetProperty("fullMaximum").GetDouble(), reset.GetProperty("maximum").GetDouble());
        AssertNoConsoleErrors(session);
    }

    private static Chart Observations(bool dark, bool updated = false) => Chart.Create().WithSize(700, 380)
        .WithTitle("Browser observations").WithLegend().WithDataLabels(false)
        .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
        .AddSeries(ChartSeries.CreateLine(new ChartSeriesId("observations"), "Observations",
                new[] { new ChartPoint(0, 12), new ChartPoint(1, updated ? 17 : 8), new ChartPoint(2, 10) })
            .WithObservationId(0, new ChartObservationId("north")).WithObservationId(1, new ChartObservationId("south")));

    private static void Configure(HtmlChartInteractionOptions options) {
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
        options.Interaction.ChartId = "browser-compatibility";
        options.Tooltip.PinOnSelect = false;
    }

    private static string? CaptureDirectory() {
        var path = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (!Path.IsPathRooted(path)) throw new InvalidOperationException("Browser captures require an absolute task-owned directory.");
        Directory.CreateDirectory(path);
        return path;
    }

    private static Task WaitForReadoutAsync(IPage page, string expected) => page.WaitForFunctionAsync("""
        expected=>{
            const tip=document.querySelector('.cfx-tooltip');
            return tip&&!tip.hidden&&tip.innerText.includes(expected);
        }
        """, expected, new PageWaitForFunctionOptions { Timeout = 5000 });

    private static async Task CaptureAsync(IPage page, string name) {
        var path = CaptureDirectory();
        if (path == null) return;
        var prefix = "compatibility-" + Engine.ToString().ToLowerInvariant() + "-" + name;
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(path, prefix + ".png"), FullPage = true });
        var state = await page.Locator(Root).EvaluateAsync<JsonElement>("""
            root=>({attributes:Object.fromEntries([...root.attributes].map(value=>[value.name,value.value])),
                tooltip:{hidden:root.querySelector('.cfx-tooltip').hidden,text:root.querySelector('.cfx-tooltip').innerText},
                input:window.compatibilityInput,
                clips:[...root.querySelectorAll('clipPath rect')].map(node=>({attributes:Object.fromEntries([...node.attributes].map(value=>[value.name,value.value])),
                    box:{x:node.getBBox().x,y:node.getBBox().y,width:node.getBBox().width,height:node.getBBox().height},
                    lengths:{x:node.x.baseVal.value,y:node.y.baseVal.value,width:node.width.baseVal.value,height:node.height.baseVal.value}})),
                lines:[...root.querySelectorAll('[data-cfx-role=line]')].map(node=>({attributes:Object.fromEntries([...node.attributes].map(value=>[value.name,value.value])),
                    paint:{fill:getComputedStyle(node).fill,stroke:getComputedStyle(node).stroke,width:getComputedStyle(node).strokeWidth}})),
                targets:[...root.querySelectorAll('[data-cfx-role=point]')].map(node=>({data:{...node.dataset},
                    nativeRegion:node._cfxNativeRegion,matrix:(matrix=>matrix&&Object.fromEntries(['a','b','c','d','e','f'].map(key=>[key,matrix[key]])))(node.getScreenCTM()),
                    bounds:node.getBoundingClientRect().toJSON(),style:{display:getComputedStyle(node).display,visibility:getComputedStyle(node).visibility,opacity:getComputedStyle(node).opacity},
                    focused:node.getRootNode().activeElement===node}))})
            """);
        await File.WriteAllTextAsync(Path.Combine(path, prefix + ".json"), JsonSerializer.Serialize(new { browser = Engine.ToString(), state }));
    }
}
