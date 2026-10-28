using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed partial class TopologyHierarchyNativeBrowserTests {
    [BrowserTheory]
    [InlineData(false, 1100)]
    [InlineData(true, 390)]
    public async Task NativeOrganizationCollapseOrderDirectionBookmarksAndExportsUseRealSourceIdentity(bool dark, int width) {
        if (!Enabled) return;
        var host = new Host(dark); await using var session = await OpenAsync(host.Page(), width, 800); var page = session.Page;
        await page.ExposeFunctionAsync("prepareOrganization", (JsonElement request) => host.Reply(request));
        await page.EvaluateAsync("""
            ()=>{window.org=ChartForgeXCharts.mount(document.querySelector('.cfx-interactive-chart'));window.orgChanges=[];window.orgErrors=[];window.orgRequests=[];
                org.root.addEventListener('cfxhierarchychange',event=>orgChanges.push(event.detail));org.root.addEventListener('cfxhierarchyerror',event=>orgErrors.push(event.detail));
                org.connectNative(async(request,signal)=>{orgRequests.push(request);const reply=await prepareOrganization(request);if(signal.aborted)throw new DOMException('Canceled','AbortError');return reply;},{basis:'initial',hierarchy:true});}
            """);
        await page.WaitForFunctionAsync("drawing=>org.root.dataset.cfxAdaptiveDrawing===drawing", width < 600 ? "narrow" : "wide");
        await CaptureHierarchyBoundary(page, width, dark, "initial-drawing");
        Assert.Equal(5, await page.Locator(".cfx-stage [data-cfx-role=topology-node][data-node-id]").CountAsync());
        await FocusWithKeyboardAsync(page, page.Locator("[data-cfx-node=platform]")); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await page.Locator("[data-cfx-node=platform]").GetAttributeAsync("aria-selected"));
        await FocusWithKeyboardAsync(page, page.Locator("[data-cfx-hierarchy-control=toggle][data-cfx-hierarchy-node=engineering]"));
        await CaptureHierarchyBoundary(page, width, dark, "before-collapse");
        await page.Keyboard.PressAsync("Space"); await Changes(page, 1);
        await CaptureHierarchyBoundary(page, width, dark, "after-collapse-before-focus-assertion");
        Assert.Equal(3, await page.Locator(".cfx-stage [data-cfx-role=topology-node][data-node-id]").CountAsync());
        Assert.Equal(0, await page.Locator("[data-cfx-node=platform],[data-cfx-node=research]").CountAsync());
        Assert.Equal(new[] { "engineering" }, await page.EvaluateAsync<string[]>("()=>org.getHierarchyView().collapsedNodeIds"));
        Assert.Equal(0, await page.Locator("[data-cfx-node][aria-selected=true]").CountAsync());
        Assert.Equal("engineering", await page.EvaluateAsync<string>("()=>document.activeElement.dataset.cfxHierarchyNode"));
        var engineering = page.Locator("[data-cfx-role=topology-node][data-node-id=engineering]");
        Assert.Equal("Owner", await engineering.GetAttributeAsync("data-node-badge"));
        Assert.Single(await engineering.Locator("[data-cfx-role=topology-node-badge] [data-cfx-native-text-ink]").AllAsync());
        Assert.True(await engineering.Locator("[data-cfx-role=topology-node-badge] use").CountAsync() > 0);
        Assert.Equal(5, host.SourceCount);
        await page.EvaluateAsync("""
            ()=>{org.root.dispatchEvent(new CustomEvent('cfx-capture-state',{detail:{sync:false}}));window.orgBookmark=JSON.parse(org.root.dataset.cfxInteractionSnapshot);
                org.setHierarchyView({nodeOrder:['operations','engineering'],layoutDirection:'LeftToRight'});}
            """);
        await Changes(page, 2); Assert.Equal(5, await page.Locator(".cfx-stage [data-cfx-role=topology-node]").CountAsync());
        Assert.Equal("LeftToRight", await page.EvaluateAsync<string>("()=>org.getHierarchyView().layoutDirection"));
        var drawing = await ActiveDrawing(page, host); Assert.Equal(TopologyLayoutDirection.LeftToRight, drawing.TopologyHierarchyView!.LayoutDirection);
        Assert.True(await page.EvaluateAsync<bool>("()=>document.querySelector('[data-cfx-node=operations]').getBBox().y<document.querySelector('[data-cfx-node=engineering]').getBBox().y"));
        await page.EvaluateAsync("()=>org.root.dispatchEvent(new CustomEvent('cfx-apply-state',{detail:{snapshot:orgBookmark,sync:false}}))");
        await Changes(page, 3); Assert.Equal(new[] { "engineering" }, await page.EvaluateAsync<string[]>("()=>org.getHierarchyView().collapsedNodeIds"));
        Assert.Null(await page.EvaluateAsync<string?>("()=>org.getHierarchyView().layoutDirection"));
        await page.Locator("[data-cfx-hierarchy-control=home]").ClickAsync(); await Changes(page, 4);
        Assert.Equal(5, await page.Locator(".cfx-stage [data-cfx-role=topology-node]").CountAsync());
        await page.SetViewportSizeAsync(width < 600 ? 1100 : 390, 800);
        await page.WaitForFunctionAsync("drawing=>org.root.dataset.cfxAdaptiveDrawing===drawing", width < 600 ? "wide" : "narrow");
        Assert.Equal(4, await page.EvaluateAsync<int>("()=>orgRequests.length")); Assert.Empty(await page.EvaluateAsync<string[]>("()=>org.getHierarchyView().collapsedNodeIds"));
        Assert.True(await page.EvaluateAsync<bool>("()=>{const m=JSON.parse(org.root.dataset.cfxPreparedChart);return m.series.length===0&&!m.xProjection&&!m.yProjection&&m.hierarchy.kind==='topology'&&[...org.root.querySelectorAll('[data-cfx-node]')].every(n=>!n.hasAttribute('data-cfx-series'));}"));
        var svgDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var svg = XDocument.Load(await svgDownload.PathAsync() ?? throw new InvalidOperationException("No organization SVG export."));
        Assert.Equal(5, svg.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "topology-node"));
        Assert.Contains(svg.Descendants(), node => node.Name.LocalName == "image"); Assert.DoesNotContain(svg.Descendants(), node => node.Name.LocalName == "script");
        var pngDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=png]").ClickAsync());
        var png = await File.ReadAllBytesAsync(await pngDownload.PathAsync() ?? throw new InvalidOperationException("No organization PNG export."));
        drawing = await ActiveDrawing(page, host); var scale = await page.EvaluateAsync<int>("()=>Math.max(1,Math.ceil(devicePixelRatio||1))");
        Assert.Equal((int)drawing.Size.Width * scale, System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4)));
        Assert.Equal((int)drawing.Size.Height * scale, System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4)));
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory); var prefix = Path.Combine(directory, "native-organization-" + width + (dark ? "-dark" : "-light"));
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = prefix + ".browser.png", FullPage = true });
            await svgDownload.SaveAsAsync(prefix + ".export.svg"); await pngDownload.SaveAsAsync(prefix + ".export.png");
            await File.WriteAllTextAsync(prefix + ".native.svg", drawing.ToSvg()); await File.WriteAllBytesAsync(prefix + ".native.png", drawing.ToPng());
            await File.WriteAllTextAsync(prefix + ".json", await page.EvaluateAsync<string>("()=>JSON.stringify({view:org.getHierarchyView(),requests:orgRequests,changes:orgChanges,errors:orgErrors})"));
        }
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>orgErrors.length")); AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task WrongAlternateStaleAndDisconnectedRepliesCannotChangeOrganizationSourceViewOrBasis() {
        if (!Enabled) return;
        var host = new Host(false); await using var session = await OpenAsync(host.Page(), 1100, 800); var page = session.Page;
        await page.EvaluateAsync("""
            ()=>{window.org=ChartForgeXCharts.mount(document.querySelector('.cfx-interactive-chart'));window.orgPending=[];window.orgChanges=[];window.orgErrors=[];
                org.root.addEventListener('cfxhierarchychange',e=>orgChanges.push(e.detail));org.root.addEventListener('cfxhierarchyerror',e=>orgErrors.push(e.detail));
                org.connectNative((request,signal)=>new Promise(resolve=>orgPending.push({request,signal,resolve})),{basis:'initial',hierarchy:true});
                for(const value of [{collapsedNodeIds:['missing']},{nodeOrder:['engineering','engineering']},{layoutDirection:'invented'}]){
                    let rejected=false;try{org.setHierarchyView(value);}catch(error){rejected=error instanceof TypeError;}if(!rejected)throw new Error('Invalid source request accepted.');}
                let axesRejected=false;try{org.connectNative(()=>{}, {basis:'wrong',hierarchy:true,axes:['x']});}catch(error){axesRejected=error instanceof TypeError;}
                if(!axesRejected)throw new Error('Topology manufactured numeric axes.');
                org.setHierarchyView({collapsedNodeIds:['engineering']});}
            """);
        await page.WaitForFunctionAsync("()=>orgPending.length===1");
        await page.EvaluateAsync("()=>org.setHierarchyView({collapsedNodeIds:['lead']})"); await page.WaitForFunctionAsync("()=>orgPending.length===2");
        Assert.True(await page.EvaluateAsync<bool>("()=>orgPending[0].signal.aborted"));
        await page.EvaluateAsync("html=>{orgPending[0].resolve({html,basis:'stale'});orgPending[1].resolve({html,basis:'wrong-view'});}", host.Section(new TopologyHierarchyView(new[] { "engineering" })).Html);
        await page.WaitForFunctionAsync("()=>orgErrors.length===1"); Assert.Empty(await page.EvaluateAsync<string[]>("()=>org.getHierarchyView().collapsedNodeIds"));
        await page.EvaluateAsync("()=>org.setHierarchyView({collapsedNodeIds:['engineering']})"); await page.WaitForFunctionAsync("()=>orgPending.length===3");
        Assert.Equal("initial", await page.EvaluateAsync<string>("()=>orgPending[2].request.basis"));
        var correct = host.Section(new TopologyHierarchyView(new[] { "engineering" })).Html;
        await page.EvaluateAsync("""
            html=>{const t=document.createElement('template');t.innerHTML=html;
                const narrow=t.content.querySelector('template[data-cfx-adaptive-narrow]').content.querySelector('.cfx-interactive-chart');
                const m=JSON.parse(narrow.dataset.cfxPreparedChart);m.hierarchy.collapsedNodeIds=['operations'];narrow.dataset.cfxPreparedChart=JSON.stringify(m);
                orgPending[2].resolve({html:t.innerHTML,basis:'wrong-alternate'});}
            """, correct);
        await page.WaitForFunctionAsync("()=>orgErrors.length===2"); Assert.Empty(await page.EvaluateAsync<string[]>("()=>org.getHierarchyView().collapsedNodeIds"));
        await page.EvaluateAsync("()=>org.setHierarchyView({collapsedNodeIds:['engineering']})"); await page.WaitForFunctionAsync("()=>orgPending.length===4");
        Assert.Equal("initial", await page.EvaluateAsync<string>("()=>orgPending[3].request.basis"));
        await page.EvaluateAsync("html=>orgPending[3].resolve({html,basis:'accepted'})", correct); await Changes(page, 1);
        await page.EvaluateAsync("()=>org.setHierarchyView(null)"); await page.WaitForFunctionAsync("()=>orgPending.length===5");
        Assert.Equal("accepted", await page.EvaluateAsync<string>("()=>orgPending[4].request.basis"));
        await page.EvaluateAsync("()=>org.connectNative(null,{axes:null})"); Assert.True(await page.EvaluateAsync<bool>("()=>orgPending[4].signal.aborted"));
        await page.EvaluateAsync("html=>orgPending[4].resolve({html,basis:'late-reset'})", host.Section(null).Html);
        Assert.Equal(new[] { "engineering" }, await page.EvaluateAsync<string[]>("()=>org.getHierarchyView().collapsedNodeIds"));
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>orgChanges.length")); AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task RejectedOrganizationReplyRestoresOwnedControlButAcceptedReplyDoesNotStealNewOutsideFocus() {
        if (!Enabled) return;
        var host = new Host(false); await using var session = await OpenAsync(host.Page(), 1100, 800); var page = session.Page;
        var pageErrors = new List<string>(); page.PageError += (_, error) => pageErrors.Add(error);
        await page.EvaluateAsync("""
            ()=>{window.org=ChartForgeXCharts.mount(document.querySelector('.cfx-interactive-chart'));window.orgPending=[];window.orgChanges=[];window.orgErrors=[];window.orgRequests=[];
                document.body.insertAdjacentHTML('beforeend','<button id="organization-focus-elsewhere">Continue outside chart</button>');
                org.root.addEventListener('cfxhierarchychange',e=>orgChanges.push(e.detail));org.root.addEventListener('cfxhierarchyerror',e=>orgErrors.push(e.detail));
                org.connectNative((request,signal)=>new Promise(resolve=>{orgRequests.push(request);orgPending.push({request,signal,resolve});}),{basis:'initial',hierarchy:true});}
            """);
        await page.WaitForFunctionAsync("()=>org.root.dataset.cfxAdaptiveDrawing==='wide'");
        const string engineering = "[data-cfx-hierarchy-control=toggle][data-cfx-hierarchy-node=engineering]";
        await FocusWithKeyboardAsync(page, page.Locator(engineering));
        await page.Keyboard.PressAsync("Space"); await page.WaitForFunctionAsync("()=>orgPending.length===1");
        Assert.True(await page.Locator(engineering).IsDisabledAsync());
        // A real, otherwise valid native source section has the wrong requested view.
        await page.EvaluateAsync("html=>orgPending[0].resolve({html,basis:'rejected'})", host.Section(null).Html);
        await page.WaitForFunctionAsync("()=>orgErrors.length===1");
        await CaptureHierarchyBoundary(page, 1100, false, "rejected-before-focus-assertion");
        Assert.Equal("engineering", await page.EvaluateAsync<string>("()=>document.activeElement.dataset.cfxHierarchyNode"));
        Assert.False(await page.Locator(engineering).IsDisabledAsync());
        Assert.Empty(await page.EvaluateAsync<string[]>("()=>org.getHierarchyView().collapsedNodeIds"));
        await FocusWithKeyboardAsync(page, page.Locator(engineering));
        await page.Keyboard.PressAsync("Space"); await page.WaitForFunctionAsync("()=>orgPending.length===2");
        Assert.Equal("initial", await page.EvaluateAsync<string>("()=>orgPending[1].request.basis"));
        await FocusWithKeyboardAsync(page, page.Locator("#organization-focus-elsewhere"));
        await CaptureHierarchyBoundary(page, 1100, false, "outside-focus-before-reply");
        await page.EvaluateAsync("html=>orgPending[1].resolve({html,basis:'accepted'})", host.Section(new TopologyHierarchyView(new[] { "engineering" })).Html);
        await Changes(page, 1); await CaptureHierarchyBoundary(page, 1100, false, "outside-focus-before-assertion");
        Assert.Equal("organization-focus-elsewhere", await page.EvaluateAsync<string>("()=>document.activeElement.id"));
        Assert.Equal(new[] { "engineering" }, await page.EvaluateAsync<string[]>("()=>org.getHierarchyView().collapsedNodeIds"));
        Assert.False(await page.EvaluateAsync<bool>("()=>orgPending[1].signal.aborted"));
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>orgErrors.length"));
        await page.EvaluateAsync("()=>org.dispose()"); Assert.Empty(pageErrors); AssertNoConsoleErrors(session);
    }

    private static Task Changes(IPage page, int count) => page.WaitForFunctionAsync("count=>orgChanges.length===count", count);
    private static async Task<PreparedVisual> ActiveDrawing(IPage page, Host host) =>
        await page.EvaluateAsync<string>("()=>org.root.dataset.cfxAdaptiveDrawing") == "narrow" ? host.Last.NarrowPreparedVisual! : host.Last.PreparedVisual;
    private static async Task CaptureHierarchyBoundary(IPage page, int width, bool dark, string boundary) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Hierarchy proof requires a task-owned absolute capture directory.");
        Directory.CreateDirectory(directory);
        Assert.True(await page.EvaluateAsync<bool>("() => Array.isArray(window.orgFocusEvents)"), "The passive focus diagnostic script must initialize.");
        var prefix = Path.Combine(directory, "native-organization-" + width + (dark ? "-dark-" : "-light-") + boundary);
        await File.WriteAllTextAsync(prefix + ".json", await page.EvaluateAsync<string>("""
            ()=>JSON.stringify({drawing:org.root.dataset.cfxAdaptiveDrawing,view:org.getHierarchyView(),requests:orgRequests,changes:orgChanges,errors:orgErrors,
                focusEvents:window.orgFocusEvents||[],
                active:document.activeElement?{tag:document.activeElement.tagName,id:document.activeElement.id,dataset:{...document.activeElement.dataset},
                    disabled:document.activeElement.disabled,connected:document.activeElement.isConnected,inside:org.root.contains(document.activeElement)}:null,
                navigation:[...org.root.querySelectorAll('[data-cfx-hierarchy-control]')].map(node=>({text:node.textContent,dataset:{...node.dataset},disabled:node.disabled,
                    expanded:node.getAttribute('aria-expanded'),rect:node.getBoundingClientRect().toJSON()})),
                nodes:[...org.root.querySelectorAll('.cfx-stage [data-cfx-node]')].map(node=>({id:node.dataset.cfxNode,selected:node.getAttribute('aria-selected'),text:node.textContent}))})
            """));
        await File.WriteAllTextAsync(prefix + ".html", await page.Locator(".cfx-interactive-chart").EvaluateAsync<string>("root=>root.outerHTML"));
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = prefix + ".png", FullPage = true });
    }

    private const string FocusProbe = """
        (() => {
          const describe = node => node instanceof Element ? {tag:node.tagName,id:node.id,node:node.dataset.cfxHierarchyNode,
            control:node.dataset.cfxHierarchyControl,disabled:node.disabled,connected:node.isConnected} : null;
          window.orgFocusEvents=[];
          for(const type of ['pointerdown','click','focusin','focusout']) document.addEventListener(type,event=>orgFocusEvents.push({
            type,time:performance.now(),trusted:event.isTrusted,target:describe(event.composedPath()[0]),related:describe(event.relatedTarget),
            active:describe(document.activeElement),documentFocus:document.hasFocus()}),true);
          window.addEventListener('blur',()=>orgFocusEvents.push({type:'window-blur',time:performance.now(),active:describe(document.activeElement),documentFocus:document.hasFocus()}));
        })();
        """;

    private sealed class Host {
        private readonly TopologyChart _source; private int _sequence;
        private readonly HashSet<string> _bases = new() { "initial" };
        internal Host(bool dark) { _source = TopologyHierarchySectionTests.Source(dark ? VisualThemeMode.Dark : VisualThemeMode.Light); Last = Section(null); }
        internal int SourceCount => _source.Nodes.Count;
        internal HtmlChartSection Last { get; private set; }
        internal HtmlChartSection Section(TopologyHierarchyView? view) => OrganizationHierarchyNavigationRecipe.PrepareSection(_source, view);
        internal object Reply(JsonElement request) {
            Assert.Contains(request.GetProperty("basis").GetString()!, _bases); Assert.Equal("hierarchy", request.GetProperty("channel").GetString());
            Last = OrganizationHierarchyNavigationRecipe.PrepareReply(_source, Last, request);
            var basis = "organization-" + (++_sequence); _bases.Add(basis); return new { html = Last.Html, basis };
        }
        internal string Page() => "<!doctype html><html><head><style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle()
            + "</style></head><body style='margin:12px'>" + Last.Html + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script><script>" + FocusProbe + "</script></body></html>";
    }
}
