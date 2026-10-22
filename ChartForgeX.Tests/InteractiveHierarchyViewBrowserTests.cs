using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class InteractiveHierarchyViewBrowserTests {
    [BrowserTheory]
    [InlineData(ChartSeriesKind.Treemap, true, 390)]
    [InlineData(ChartSeriesKind.Treemap, false, 1200)]
    [InlineData(ChartSeriesKind.Sunburst, true, 390)]
    [InlineData(ChartSeriesKind.Sunburst, false, 1200)]
    public async Task NativeHierarchyActivationUpHomeBookmarksAndExportsUseOneAcceptedView(ChartSeriesKind kind, bool dark, int width) {
        if (!Enabled) return;
        var host = new Host(kind, dark);
        await using var session = await OpenAsync(host.Page(), width, 760);
        var page = session.Page;
        await page.ExposeFunctionAsync("prepareHierarchyHost", (JsonElement request) => host.Reply(request));
        await page.EvaluateAsync("""
            ()=>{
                window.hierarchyController=ChartForgeXCharts.mount(document.querySelector('.cfx-interactive-chart'));
                window.hierarchyChanges=[];window.hierarchyErrors=[];window.hierarchyRequests=[];
                hierarchyController.root.addEventListener('cfxhierarchychange',e=>hierarchyChanges.push(e.detail));
                hierarchyController.root.addEventListener('cfxhierarchyerror',e=>hierarchyErrors.push(e.detail));
                hierarchyController.connectNative(async(request,signal)=>{
                    hierarchyRequests.push(request);const reply=await prepareHierarchyHost(request);
                    if(signal.aborted)throw new DOMException('Canceled','AbortError');return reply;
                },{basis:'initial',axes:null,hierarchy:true});
            }
            """);
        await page.Locator("[data-cfx-node=services]").FocusAsync(); await page.Keyboard.PressAsync("Enter");
        await Changes(page, 1); Assert.Equal("services", await Root(page));
        var worker = page.Locator("[data-cfx-node=worker]");
        await worker.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await worker.GetAttributeAsync("aria-selected"));
        await page.EvaluateAsync("""
            ()=>{const root=hierarchyController.root;root.dispatchEvent(new CustomEvent('cfx-capture-state',{detail:{sync:false}}));
                window.hierarchyBookmark=JSON.parse(root.dataset.cfxInteractionSnapshot);hierarchyController.setHierarchyView({rootId:'api'});}
            """);
        await Changes(page, 2); Assert.Equal("api", await Root(page));
        await page.Locator("[data-cfx-hierarchy-control=up]").ClickAsync(); await Changes(page, 3);
        Assert.Equal("services", await Root(page));
        await page.Locator("[data-cfx-hierarchy-control=home]").ClickAsync(); await Changes(page, 4);
        Assert.Null(await Root(page));
        await page.EvaluateAsync("()=>hierarchyController.root.dispatchEvent(new CustomEvent('cfx-apply-state',{detail:{snapshot:hierarchyBookmark,sync:false}}))");
        await Changes(page, 5); Assert.Equal("services", await Root(page));
        Assert.Equal("true", await page.Locator("[data-cfx-node=worker]").GetAttributeAsync("aria-selected"));
        Assert.NotEqual("0", await page.Locator("[data-cfx-node=other]").GetAttributeAsync("tabindex"));
        Assert.Equal(0, await page.Locator("[data-cfx-node=other] [data-cfx-browser-hit-area]").CountAsync());
        Assert.True(await page.Locator(".cfx-tooltip").EvaluateAsync<bool>("tip=>tip.hidden"));
        if (kind == ChartSeriesKind.Sunburst) {
            await page.Locator("[data-cfx-node=api]").FocusAsync();
            Assert.Equal("item", await page.Locator("[data-cfx-node=api]").GetAttributeAsync("data-cfx-highlight-state"));
            Assert.Equal("other-branch", await page.Locator("[data-cfx-node=worker]").GetAttributeAsync("data-cfx-highlight-state"));
            Assert.Null(await page.Locator("[data-cfx-node=other]").GetAttributeAsync("data-cfx-highlight-managed"));
            await page.Locator("[data-cfx-hierarchy-control=home]").FocusAsync();
        }
        await page.SetViewportSizeAsync(width < 600 ? 1200 : 390, 760);
        await page.WaitForFunctionAsync("drawing=>hierarchyController.root.dataset.cfxAdaptiveDrawing===drawing", width < 600 ? "wide" : "narrow");
        Assert.Equal("services", await Root(page));
        Assert.Equal(5, await page.EvaluateAsync<int>("()=>hierarchyRequests.length"));
        var svgDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var svgPath = await svgDownload.PathAsync() ?? throw new InvalidOperationException("No hierarchy SVG download.");
        var svg = XDocument.Load(svgPath);
        Assert.Equal("services", (string?)Assert.Single(svg.Descendants(), node => (string?)node.Attribute("data-cfx-node") == "services").Attribute("data-cfx-view-root"));
        Assert.Equal("outside-view", (string?)Assert.Single(svg.Descendants(), node => (string?)node.Attribute("data-cfx-node") == "other").Attribute("data-cfx-geometry-status"));
        var pngDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=png]").ClickAsync());
        var pngPath = await pngDownload.PathAsync() ?? throw new InvalidOperationException("No hierarchy PNG download.");
        var png = await File.ReadAllBytesAsync(pngPath);
        var drawing = width < 600 ? host.Last.PreparedVisual : host.Last.NarrowPreparedVisual!;
        var exportScale = await page.EvaluateAsync<int>("()=>Math.max(1,Math.ceil(devicePixelRatio||1))");
        Assert.Equal((int)drawing.Size.Width * exportScale, System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4)));
        Assert.Equal((int)drawing.Size.Height * exportScale, System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4)));
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory); var name = "native-hierarchy-" + kind + "-" + width;
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png"), FullPage = true });
            await svgDownload.SaveAsAsync(Path.Combine(directory, name + ".export.svg"));
            await pngDownload.SaveAsAsync(Path.Combine(directory, name + ".export.png"));
            await File.WriteAllTextAsync(Path.Combine(directory, name + ".native.svg"), drawing.ToSvg());
            await File.WriteAllBytesAsync(Path.Combine(directory, name + ".native.png"), drawing.ToPng());
            await File.WriteAllTextAsync(Path.Combine(directory, name + ".json"), await page.EvaluateAsync<string>("()=>JSON.stringify({view:hierarchyController.getHierarchyView(),requests:hierarchyRequests,errors:hierarchyErrors})"));
        }
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>hierarchyErrors.length")); AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task SupersededInvalidAndDisconnectedRepliesCannotAdvanceHierarchyOrItsBasis() {
        if (!Enabled) return;
        var host = new Host(ChartSeriesKind.Treemap, false);
        await using var session = await OpenAsync(host.Page(), 980, 760); var page = session.Page;
        await page.EvaluateAsync("""
            ()=>{
                window.hierarchyController=ChartForgeXCharts.mount(document.querySelector('.cfx-interactive-chart'));
                window.pendingHierarchy=[];window.hierarchyChanges=[];window.hierarchyErrors=[];
                hierarchyController.root.addEventListener('cfxhierarchychange',e=>hierarchyChanges.push(e.detail));
                hierarchyController.root.addEventListener('cfxhierarchyerror',e=>hierarchyErrors.push(e.detail));
                hierarchyController.connectNative((request,signal)=>new Promise((resolve,reject)=>pendingHierarchy.push({request,signal,resolve,reject})),
                    {basis:'initial',axes:null,hierarchy:true});
                let rejected=false;try{hierarchyController.setHierarchyView({rootId:'missing'});}catch(e){rejected=e instanceof TypeError;}
                if(!rejected)throw new Error('Missing hierarchy identity was accepted.');
                hierarchyController.setHierarchyView({rootId:'services'});
            }
            """);
        await page.WaitForFunctionAsync("()=>pendingHierarchy.length===1");
        await page.EvaluateAsync("()=>hierarchyController.setHierarchyView({rootId:'api'})");
        await page.WaitForFunctionAsync("()=>pendingHierarchy.length===2");
        Assert.True(await page.EvaluateAsync<bool>("()=>pendingHierarchy[0].signal.aborted"));
        await page.EvaluateAsync("html=>{pendingHierarchy[0].resolve({html,basis:'stale'});pendingHierarchy[1].resolve({html,basis:'wrong-root'});}", host.Section("services").Html);
        await page.WaitForFunctionAsync("()=>hierarchyErrors.length===1"); Assert.Null(await Root(page));
        await page.EvaluateAsync("()=>hierarchyController.setHierarchyView({rootId:'api'})");
        await page.WaitForFunctionAsync("()=>pendingHierarchy.length===3");
        Assert.Equal("initial", await page.EvaluateAsync<string>("()=>pendingHierarchy[2].request.basis"));
        await page.EvaluateAsync("html=>pendingHierarchy[2].resolve({html,basis:'accepted-api'})", host.Section("api").Html);
        await Changes(page, 1); Assert.Equal("api", await Root(page));
        await page.EvaluateAsync("()=>hierarchyController.setHierarchyView({rootId:'services'})");
        await page.WaitForFunctionAsync("()=>pendingHierarchy.length===4");
        Assert.Equal("accepted-api", await page.EvaluateAsync<string>("()=>pendingHierarchy[3].request.basis"));
        await page.EvaluateAsync("html=>hierarchyController.update(html,{basis:'host-update'})", host.Section("services").Html);
        Assert.True(await page.EvaluateAsync<bool>("()=>pendingHierarchy[3].signal.aborted"));
        await page.EvaluateAsync("html=>pendingHierarchy[3].resolve({html,basis:'late-api'})", host.Section("api").Html);
        Assert.Equal("services", await Root(page));
        await page.EvaluateAsync("()=>{hierarchyController.setHierarchyView({rootId:'api'});}");
        await page.WaitForFunctionAsync("()=>pendingHierarchy.length===5");
        Assert.Equal("host-update", await page.EvaluateAsync<string>("()=>pendingHierarchy[4].request.basis"));
        await page.EvaluateAsync("()=>hierarchyController.connectNative(null,{axes:null})");
        Assert.True(await page.EvaluateAsync<bool>("()=>pendingHierarchy[4].signal.aborted"));
        await page.EvaluateAsync("html=>pendingHierarchy[4].resolve({html,basis:'disconnected'})", host.Section("api").Html);
        Assert.Equal("services", await Root(page)); Assert.Equal(1, await page.EvaluateAsync<int>("()=>hierarchyChanges.length"));
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task SunburstSectorGapsHaveNoPointerTargetAndExportsKeepCommittedInsets() {
        if (!Enabled) return;
        var host = new Host(ChartSeriesKind.Sunburst, true, spaced: true);
        await using var session = await OpenAsync(host.Page(), 390, 760); var page = session.Page;
        await page.EvaluateAsync("""
            ()=>{
                window.hierarchyController=ChartForgeXCharts.mount(document.querySelector('.cfx-interactive-chart'));
                window.hierarchySelections=[];window.hierarchyChanges=[];
                hierarchyController.root.addEventListener('cfxselect',e=>hierarchySelections.push(e.detail));
                hierarchyController.root.addEventListener('cfxhierarchychange',e=>hierarchyChanges.push(e.detail));
            }
            """);
        var positions = await page.EvaluateAsync<double[]>("""
            ()=>{
                const svg=hierarchyController.root.querySelector('.cfx-stage svg');
                const root=svg.querySelector('[data-cfx-node=root] [data-cfx-role=sunburst-segment-mark]').getBBox();
                const node=svg.querySelector('[data-cfx-node=services]'),d=node.dataset;
                const radius=(Number(d.cfxPaintInnerRadius)+Number(d.cfxPaintOuterRadius))/2;
                const center={x:root.x+root.width/2,y:root.y+root.height/2},matrix=svg.getScreenCTM();
                const project=angle=>new DOMPoint(center.x+Math.cos(angle)*radius,center.y+Math.sin(angle)*radius).matrixTransform(matrix);
                const ink=project(Number(d.cfxPaintStartAngle)+Number(d.cfxPaintSweep)/2),gap=project(Number(d.cfxStartAngle));
                return [ink.x,ink.y,gap.x,gap.y];
            }
            """);
        Assert.Equal("services", await page.EvaluateAsync<string>("p=>document.elementFromPoint(p[0],p[1]).closest('[data-cfx-node]')?.dataset.cfxNode", positions));
        await page.Mouse.ClickAsync((float)positions[0], (float)positions[1]);
        await page.WaitForFunctionAsync("()=>hierarchySelections.length===1");
        Assert.Equal("true", await page.Locator("[data-cfx-node=services]").GetAttributeAsync("aria-selected"));
        Assert.Null(await page.EvaluateAsync<string?>("p=>document.elementFromPoint(p[2],p[3]).closest('[data-cfx-node]')?.dataset.cfxNode??null", positions));
        await page.Mouse.MoveAsync((float)positions[2], (float)positions[3]);
        await page.Mouse.ClickAsync((float)positions[2], (float)positions[3]);
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>hierarchySelections.length"));
        Assert.Equal(0, await page.Locator("[data-cfx-passive=true] [data-cfx-browser-hit-area]").CountAsync());
        await page.ExposeFunctionAsync("prepareHierarchyHost", (JsonElement request) => host.Reply(request));
        await page.EvaluateAsync("""
            ()=>{hierarchyController.connectNative(request=>prepareHierarchyHost(request),{basis:'initial',axes:null,hierarchy:true});
                hierarchyController.setHierarchyView({rootId:'services'});}
            """);
        await Changes(page, 1); Assert.Equal("services", await Root(page));
        var svgDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var svg = XDocument.Load(await svgDownload.PathAsync() ?? throw new InvalidOperationException("No spaced Sunburst SVG download."));
        var root = Assert.Single(svg.Descendants(), node => (string?)node.Attribute("data-cfx-node") == "services");
        Assert.Equal("10", (string?)root.Attribute("data-cfx-sector-gap"));
        Assert.Equal("services", (string?)root.Attribute("data-cfx-view-root"));
        Assert.Equal("0", (string?)root.Attribute("data-cfx-paint-inner-radius"));
        Assert.Equal(Math.PI * 2, double.Parse(root.Attribute("data-cfx-paint-sweep")!.Value, System.Globalization.CultureInfo.InvariantCulture), 12);
        var pngDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=png]").ClickAsync());
        var png = await File.ReadAllBytesAsync(await pngDownload.PathAsync() ?? throw new InvalidOperationException("No spaced Sunburst PNG download."));
        var drawing = host.Last.NarrowPreparedVisual!;
        var scale = await page.EvaluateAsync<int>("()=>Math.max(1,Math.ceil(devicePixelRatio||1))");
        Assert.Equal((int)drawing.Size.Width * scale, System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4)));
        Assert.Equal((int)drawing.Size.Height * scale, System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4)));
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, "sunburst-sector-gap-browser.png"), FullPage = true });
            await svgDownload.SaveAsAsync(Path.Combine(directory, "sunburst-sector-gap.export.svg"));
            await pngDownload.SaveAsAsync(Path.Combine(directory, "sunburst-sector-gap.export.png"));
            await File.WriteAllTextAsync(Path.Combine(directory, "sunburst-sector-gap.native.svg"), drawing.ToSvg());
            await File.WriteAllBytesAsync(Path.Combine(directory, "sunburst-sector-gap.native.png"), drawing.ToPng());
        }
        AssertNoConsoleErrors(session);
    }

    private static Task Changes(IPage page, int count) => page.WaitForFunctionAsync("count=>hierarchyChanges.length===count", count);
    private static Task<string?> Root(IPage page) => page.EvaluateAsync<string?>("()=>hierarchyController.getHierarchyView().rootId");
    private sealed class Host {
        private readonly Chart _chart;
        private readonly HtmlInteractiveChartRenderer _renderer = new();
        private int _sequence;
        private readonly HashSet<string> _bases = new() { "initial" };
        internal Host(ChartSeriesKind kind, bool dark, bool spaced = false) {
            _chart = HierarchyViewPreparationTests.Source(kind, dark).WithDataLabels();
            if (spaced) _chart.ConfigureSunburst(options => { options.SectorGap = 10; options.MinimumLabelFontSize = 9; options.CornerRadius = 4; });
            Last = Section(null);
        }
        internal HtmlChartSection Last { get; private set; }
        internal HtmlChartSection Section(string? root) => _renderer.PrepareHierarchySection(_chart, root == null ? null : new ChartHierarchyView(root), Configure);
        private static void Configure(HtmlChartInteractionOptions options) {
            options.Interaction.ChartId = "native-hierarchy";
            options.Interaction.Enable(ChartInteractionFeatures.HierarchyNavigation | ChartInteractionFeatures.StateBookmarks | ChartInteractionFeatures.Export);
            options.Highlight.Mode = HtmlChartHighlightMode.RootBranch;
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive; options.Adaptive.NarrowWidth = 340; options.Adaptive.NarrowHeight = 420;
        }
        internal object Reply(JsonElement request) {
            Assert.True(_bases.Contains(request.GetProperty("basis").GetString()!));
            Assert.Equal("hierarchy", request.GetProperty("channel").GetString());
            var root = request.GetProperty("rootId"); Last = Section(root.ValueKind == JsonValueKind.Null ? null : root.GetString());
            var basis = "hierarchy-" + (++_sequence); _bases.Add(basis);
            return new { html = Last.Html, basis };
        }
        internal string Page() => "<!doctype html><html><head><style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle()
            + "</style></head><body style='margin:12px'>" + Last.Html + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script></body></html>";
    }
}
