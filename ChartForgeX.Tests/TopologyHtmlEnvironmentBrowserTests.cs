using System.Text.Json;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class TopologyHtmlEnvironmentBrowserTests {
    [BrowserTheory]
    [InlineData(false, false, 980, "cfx-topology")]
    [InlineData(true, true, 390, "report-route")]
    public async Task InteractiveTopologyKeepsNativeLanguageAndPrintsTheCompleteSceneBeforeRestoringItsCamera(
        bool dark, bool externalAssets, int width, string prefix) {
        if (!Enabled) return;
        var chart = TopologyChart.Create().WithId("environment-topology").WithViewport(840, 420, 20)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithTitle("Trasa przetwarzania").WithLegend(null).ConfigureAccessibility(options => options.Language = "pl-PL")
            .AddNode("api", "Odbiór", 60, 150).AddNode("worker", "Przetwarzanie", 330, 150).AddNode("store", "Zapis", 620, 150)
            .AddEdge("request", "api", "worker", "Żądanie").AddEdge("result", "worker", "store", "Wynik");
        var options = new TopologyRenderOptions { EnableHtmlViewportControls = true, EnableHtmlExportControls = true,
            EnableHtmlSelectionPanel = true, HtmlNarrowMinimumHeight = 288, CssClassPrefix = prefix };
        options.SelectedNodeIds.Add("worker");
        var asset = HtmlInteractiveAssetFiles.TopologyScript(options);
        var html = new HtmlInteractiveTopologyRenderer().RenderPage(chart, options,
            externalAssets ? new HtmlAssetReferences("https://assets.example.test/") : null);
        await using var session = await OpenAsync("<!doctype html><html><body></body></html>", width, 760);
        var page = session.Page;
        if (externalAssets) await page.RouteAsync("https://assets.example.test/**", route =>
            route.FulfillAsync(new() { ContentType = asset.ContentType, Body = asset.Content }));
        await page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active, ReducedMotion = ReducedMotion.Reduce });
        await page.SetContentAsync(html);
        var root = page.Locator("." + prefix + "-wrapper");
        await page.Locator("[data-cfx-topology-zoom=in]").ClickAsync();
        await page.Locator("[data-cfx-topology-zoom=in]").ClickAsync();
        Assert.Equal("0s", await root.Locator("." + prefix + "-viewport>svg").EvaluateAsync<string>("svg=>getComputedStyle(svg).transitionDuration"));
        Assert.True(await root.Locator("." + prefix + "-selection-panel").IsVisibleAsync());
        await AssertControlContrastAsync(page, root);
        var screen = await ReadEnvironmentAsync(root, prefix);
        await CaptureEnvironmentAsync(page, prefix + "-screen", screen);
        await page.EmulateMediaAsync(new() { Media = Media.Print, ForcedColors = ForcedColors.Active, ReducedMotion = ReducedMotion.Reduce });
        var printed = await ReadEnvironmentAsync(root, prefix);
        await CaptureEnvironmentAsync(page, prefix + "-print", printed);
        await page.EmulateMediaAsync(new() { Media = Media.Screen, ForcedColors = ForcedColors.Active, ReducedMotion = ReducedMotion.Reduce });
        var restored = await ReadEnvironmentAsync(root, prefix);
        Assert.Equal(screen.GetProperty("camera").GetRawText(), restored.GetProperty("camera").GetRawText());
        Assert.Equal(screen.GetProperty("transform").GetString(), restored.GetProperty("transform").GetString());
        Assert.Equal(screen.GetProperty("selected").GetRawText(), restored.GetProperty("selected").GetRawText());
        Assert.Equal(screen.GetProperty("viewBox").GetString(), printed.GetProperty("viewBox").GetString());
        Assert.Equal("none", printed.GetProperty("transform").GetString());
        Assert.Equal(0, printed.GetProperty("visibleControls").GetInt32());
        Assert.InRange(printed.GetProperty("viewportOverflow").GetDouble(), 0, 1);
        Assert.InRange(printed.GetProperty("pageOverflow").GetDouble(), 0, 1);
        Assert.Equal(printed.GetProperty("nativeAspect").GetDouble(), printed.GetProperty("renderedAspect").GetDouble(), 3);
        Assert.Equal("pl-PL", await page.Locator("html").GetAttributeAsync("lang"));
        AssertNoConsoleErrors(session);
    }

    private static async Task AssertControlContrastAsync(IPage page, ILocator root) {
        var paints = await root.Locator("[data-cfx-topology-zoom=in]").EvaluateAsync<string[]>("""
            button=>{const backgrounds=[];for(let node=button;node;node=node.parentElement)backgrounds.unshift(getComputedStyle(node).backgroundColor);
                return[getComputedStyle(button).color,...backgrounds]}
            """);
        var rgba = await ResolveCssColorsAsync(page, paints);
        ChartColor Color(int[] value) => ChartColor.FromRgba((byte)value[0], (byte)value[1], (byte)value[2], (byte)value[3]);
        var background = ChartColor.White;
        for (var index = 1; index < rgba.Length; index++) background = ChartColorMath.CompositeOverOpaque(Color(rgba[index]), background);
        var ink = ChartColorMath.CompositeOverOpaque(Color(rgba[0]), background);
        Assert.True(ChartColorMath.ContrastRatio(ink, background) >= 4.5, "Topology control text retains contrast in the emulated forced-color environment.");
    }

    private static Task<JsonElement> ReadEnvironmentAsync(ILocator root, string prefix) => root.EvaluateAsync<JsonElement>("""
        (root,prefix)=>{
            const svg=root.querySelector('.'+prefix+'-viewport>svg'),viewport=svg.parentElement,box=svg.getBoundingClientRect(),viewBox=svg.viewBox.baseVal;
            const visible=node=>!node.hidden&&getComputedStyle(node).display!=='none'&&getComputedStyle(node).visibility!=='hidden';
            return {forced:matchMedia('(forced-colors:active)').matches,reduced:matchMedia('(prefers-reduced-motion:reduce)').matches,language:document.documentElement.lang,transform:getComputedStyle(svg).transform,viewBox:svg.getAttribute('viewBox'),
                camera:{zoom:root.dataset.cfxTopologyZoom,x:root.dataset.cfxTopologyPanX,y:root.dataset.cfxTopologyPanY},
                selected:[...root.querySelectorAll('[aria-selected=true]')].map(node=>node.getAttribute('data-node-id')||node.getAttribute('data-edge-id')),
                visibleControls:[...root.querySelectorAll('.'+prefix+'-controls,.'+prefix+'-selection-panel,.'+prefix+'-force-controls,.'+prefix+'-scenario-panel,.'+prefix+'-scenarios')].filter(visible).length,
                viewportOverflow:Math.max(0,viewport.scrollWidth-viewport.clientWidth),pageOverflow:Math.max(0,document.documentElement.scrollWidth-innerWidth),
                nativeAspect:viewBox.width/viewBox.height,renderedAspect:box.width/box.height,svg:box.toJSON(),viewport:viewport.getBoundingClientRect().toJSON()};
        }
        """, prefix);

    private static async Task CaptureEnvironmentAsync(IPage page, string name, JsonElement receipt) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, "topology-environment-" + name + ".json"), receipt.GetRawText());
        await page.ScreenshotAsync(new() { Path = Path.Combine(directory, "topology-environment-" + name + ".png"), FullPage = true });
    }
}
