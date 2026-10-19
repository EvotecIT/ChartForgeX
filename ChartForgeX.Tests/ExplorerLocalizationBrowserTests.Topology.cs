using System.Xml.Linq;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Topology;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class ExplorerLocalizationBrowserTests {
    [BrowserTheory]
    [InlineData(390, false)]
    [InlineData(960, true)]
    public async Task TopologyMessagesStayScopedAcrossNativeSelectionFilteringAndCanonicalAssetDelivery(int width, bool external) {
        if (!Enabled) return;
        var chart = TopologyFixture();
        var options = new TopologyRenderOptions { EnableHtmlSelectionPanel = true, EnableHtmlViewportControls = true,
            EnableHtmlExportControls = true, EnableHtmlForceGraphControls = true };
        var renderer = new HtmlInteractiveTopologyRenderer();
        Arabic(renderer.Localization);
        var asset = HtmlInteractiveAssetFiles.TopologyScript(options);
        var englishPeer = new HtmlInteractiveTopologyRenderer().RenderFragmentWithoutAssets(TopologyFixture().WithId("english-topology"), options);
        // The host route deliberately registers only the public canonical asset, not a test-only prelude.
        var html = external ? renderer.RenderPage(chart, options, new HtmlAssetReferences("https://assets.example.test/"))
            : "<!doctype html><html lang=ar dir=rtl><head><meta charset=utf-8></head><body>" + renderer.RenderFragmentWithoutAssets(chart, options)
                + englishPeer + "<script>" + asset.Content + "</script></body></html>";
        await using var session = await OpenAsync("<!doctype html><html><body></body></html>", width, 850);
        var page = session.Page;
        if (external) await page.RouteAsync("https://assets.example.test/" + asset.FileName, route => route.FulfillAsync(new() { ContentType = asset.ContentType, Body = asset.Content }));
        // Without-assets callers also register the public native stylesheet, as a normal embedding host does.
        if (!external) html = html.Replace("</head>", "<style>" + TopologyHtmlRenderer.BuildPageStyle(options, chart.ResolvedTheme) + "</style></head>", StringComparison.Ordinal);
        await page.SetContentAsync(new HtmlInteractiveTopologyRenderer().RenderPage(chart, options));
        var native = await NativeSnapshotAsync(page, ".cfx-topology-wrapper");
        await page.SetContentAsync(html);
        Assert.Equal(native, await NativeSnapshotAsync(page, ".cfx-topology-wrapper[data-chart-id=translated-topology]"));
        await ObserveInputAsync(page);
        var root = page.Locator(".cfx-topology-wrapper[data-chart-id=translated-topology]");
        await page.WaitForFunctionAsync("()=>!!document.querySelector('[data-cfx-runtime-bound=true]')");
        if (!external) {
            Assert.Equal("Find node, edge, group", await page.Locator("[data-chart-id=english-topology] [data-cfx-force-search]").GetAttributeAsync("placeholder"));
            await AssertLeadingEnglishNumberOrderAsync(page.Locator("[data-chart-id=english-topology] [data-cfx-force-summary]"));
        }
        Assert.Equal("rtl", await root.EvaluateAsync<string>("root=>getComputedStyle(root).direction"));
        Assert.Equal("البحث في المسار", await root.Locator("[data-cfx-force-search]").GetAttributeAsync("placeholder"));
        Assert.Equal("Healthy", await root.Locator("[data-cfx-force-status] option").Filter(new() { HasText = "سليم" }).GetAttributeAsync("value"));
        var source = root.Locator("[data-cfx-role=topology-node][data-node-id=source-a]");
        await FocusWithKeyboardAsync(page, source);
        await page.Keyboard.PressAsync("Enter");
        Assert.Equal("source-a", await root.GetAttributeAsync("data-cfx-selection-id"));
        Assert.Equal("Authored source {count}", await root.Locator("[data-cfx-selection-title]").TextContentAsync());
        Assert.Contains("المعرّف", await root.Locator("[data-cfx-selection-facts]").TextContentAsync());
        Assert.Contains("Healthy", await root.Locator("[data-cfx-selection-facts]").TextContentAsync());
        await CaptureAsync(page, "topology-" + width + "-selected");
        await root.Locator("[data-cfx-force-search]").FillAsync("Authored source");
        Assert.Equal("العقد 1؛ العلاقات 0", await root.Locator("[data-cfx-force-summary]").TextContentAsync());
        await root.Locator("[data-cfx-force-search]").FillAsync("");
        await root.Locator("[data-cfx-topology-scenario=route]").ClickAsync();
        Assert.Contains("خطوات 2", await root.Locator("[data-cfx-scenario-panel-meta]").TextContentAsync());
        Assert.Equal("Authored route", await root.Locator("[data-cfx-scenario-panel-title]").TextContentAsync());
        var download = await page.RunAndWaitForDownloadAsync(() => root.Locator("[data-cfx-topology-export=svg]").ClickAsync());
        var path = await download.PathAsync();
        Assert.NotNull(path);
        var exported = await File.ReadAllTextAsync(path!);
        Assert.Contains("Authored source {count}", exported);
        Assert.DoesNotContain("أدوات المسار", exported);
        await page.EvaluateAsync("()=>{window.topologyRoot=document.querySelector('[data-chart-id=translated-topology]');ChartForgeXTopology.dispose(topologyRoot);ChartForgeXTopology.mount(topologyRoot);}");
        await source.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        Assert.Contains("المعرّف", await root.Locator("[data-cfx-selection-facts]").TextContentAsync());
        await CaptureAsync(page, "topology-" + width + "-remounted");
        await page.EvaluateAsync("()=>ChartForgeXTopology.dispose(document)");
        foreach (var authored in new[] { "direction=\"rtl\"", "style=\"direction:rtl\"" }) {
            var presentation = renderer.RenderPresentationPage(chart, prepared => {
                var svg = XDocument.Parse(prepared.ToSvg());
                if (authored.StartsWith("style", StringComparison.Ordinal))
                    svg.Root!.SetAttributeValue("style", (string?)svg.Root.Attribute("style") + ";direction:rtl");
                else svg.Root!.SetAttributeValue("direction", "rtl");
                return svg.ToString(SaveOptions.DisableFormatting);
            }, options);
            await page.SetContentAsync(presentation);
            Assert.Equal("rtl", await page.Locator(".cfx-topology-viewport>svg").EvaluateAsync<string>("svg=>getComputedStyle(svg).direction"));
            Assert.Equal("rtl", await page.Locator(".cfx-topology-wrapper").EvaluateAsync<string>("root=>getComputedStyle(root).direction"));
            await CaptureAsync(page, "topology-" + width + (authored.StartsWith("style", StringComparison.Ordinal) ? "-authored-style" : "-authored-direction"));
            await page.EvaluateAsync("()=>ChartForgeXTopology.dispose(document)");
        }
        AssertNoConsoleErrors(session);
    }

    private static TopologyChart TopologyFixture() => TopologyChart.Create().WithId("translated-topology").WithViewport(720, 360)
        .WithTitle("Authored topology").WithLayout(TopologyLayoutMode.RelationshipRadial).ConfigureAccessibility(options => options.Language = "pl-PL")
        .AddNode("source-a", "Authored source {count}", 70, 130, status: TopologyHealthStatus.Healthy)
        .AddNode("source-b", "Authored destination", 450, 130)
        .AddEdge("link", "source-a", "source-b", "Authored relationship")
        .AddScenario("route", "Authored route", route => route.AddNodeStep("source-a").AddNodeStep("source-b"));
}
