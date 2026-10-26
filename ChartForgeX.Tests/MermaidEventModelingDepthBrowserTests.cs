using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class MermaidEventModelingDepthBrowserTests {
    [BrowserTheory]
    [InlineData(false, 390)] [InlineData(true, 390)]
    [InlineData(false, 1200)] [InlineData(true, 1200)]
    public async Task CompleteTimelineKeepsEveryTypedRowAndLaneCaptionInsideNativeSurfacesAndScrollableHtml(bool dark, int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        var result = MermaidRenderer.Render(MermaidEventModelingDepthTests.Source, MermaidEventModelingDepthTests.Options(dark, width));
        Assert.False(result.HasErrors); Assert.Empty(result.Diagnostics); var artifact = result.Artifact!;
        var svg = artifact.ToSvg(); var png = artifact.ToPng();
        await using var session = await BrowserProofFonts.OpenAsync(artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize }), width, 760);
        Assert.Equal(0, await session.Page.Locator("script, image, foreignObject").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("""
            () => {
                const nodes=[...document.querySelectorAll('[data-cfx-role="topology-node"]')];
                const within=(a,b)=>b.x>=a.x-.2&&b.y>=a.y-.2&&b.x+b.width<=a.x+a.width+.2&&b.y+b.height<=a.y+a.height+.2;
                if(nodes.length!==9||!nodes.every(node=>{
                    const surface=node.querySelector('[data-cfx-role="topology-node-surface"]')?.getBBox();
                    return surface&&[...node.querySelectorAll('text')].every(text=>within(surface,text.getBBox()));
                }))return false;
                const groups=[...document.querySelectorAll('[data-cfx-role="topology-group"]')];
                if(groups.length!==6||!groups.every(group=>{
                    const surface=group.querySelector('[data-cfx-role="topology-group-surface"]')?.getBBox();
                    const captions=[...group.querySelectorAll('[data-cfx-role="topology-group-label"]')];
                    return surface&&captions.length===1&&captions.every(text=>within(surface,text.getBBox()));
                }))return false;
                const order=['30','10','20','05','06','04','08','01','02'].map(id=>nodes.find(node=>node.dataset.nodeId===id).getBoundingClientRect());
                return order.slice(1).every((bounds,index)=>bounds.x>=order[index].right-1);
            }
            """));
        var text = string.Join("\n", await session.Page.Locator("svg text").AllTextContentsAsync());
        foreach (var expected in new[] { "String: ABC", "Alex Morgan", "/escaped~1key/~0value", "Boolean: true", "Null: null", "reset" }) Assert.Contains(expected, text);
        var host = session.Page.Locator("[data-cfx-html-sizing=preserve-size]");
        Assert.True(await host.EvaluateAsync<bool>("element=>element.scrollWidth>element.clientWidth"));
        await BeginStaticHostKeyTraceAsync(session.Page, host);
        await session.Page.Keyboard.PressAsync("Tab");
        await CaptureStaticHostKeyTraceAsync(session.Page, dark, width, "after-native-tab");
        Assert.True(await host.EvaluateAsync<bool>("node=>node.getRootNode().activeElement===node"), "The fresh static artifact host receives native Tab focus.");
        Assert.True(await host.EvaluateAsync<bool>("node=>node.matches(':focus-visible')"), "The static artifact host has keyboard-visible focus.");
        try {
            await session.Page.Keyboard.DownAsync("ArrowRight");
            await CaptureStaticHostKeyTraceAsync(session.Page, dark, width, "before-scroll-assertion");
            await session.Page.WaitForFunctionAsync("() => document.querySelector('[data-cfx-html-sizing=preserve-size]').scrollLeft > 0");
        } finally {
            await session.Page.Keyboard.UpAsync("ArrowRight");
            await CaptureStaticHostKeyTraceAsync(session.Page, dark, width, "after-sustained-arrow-release");
        }
        Assert.True(await host.EvaluateAsync<double>("element=>element.scrollLeft") > 0);
        Assert.True(await session.Page.EvaluateAsync<bool>("()=>document.documentElement.scrollWidth<=innerWidth"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory); var path = Path.Combine(directory, "event-native-browser-" + width + (dark ? "-dark" : "-light"));
            File.WriteAllText(path + ".svg", svg); File.WriteAllBytes(path + ".native.png", png);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = path + ".browser.png", FullPage = true });
        }
    }
    [BrowserTheory]
    [InlineData(false)] [InlineData(true)]
    public async Task LongNamespaceCaptionIsCompleteInsideTheNativeLaneInBrowserGeometry(bool dark) {
        if (!InteractiveChartBrowser.Enabled) return;
        var name = string.Concat(Enumerable.Repeat("Operations", 20));
        var source = "eventmodeling\ntf 30 ui " + name + ".X\ntf 10 cmd Save\ntf 20 ui " + name + ".Y";
        var result = MermaidRenderer.Render(source, MermaidEventModelingDepthTests.Options(dark, 390)); Assert.False(result.HasErrors);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model); var label = chart.Groups.Single(group => group.Label.Contains(name)).Label;
        await using var session = await BrowserProofFonts.OpenAsync(result.Artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize }), 390, 760);
        var caption = session.Page.Locator("[data-cfx-role=topology-group-label]").Filter(new LocatorFilterOptions { HasText = name });
        Assert.Equal(label, await caption.TextContentAsync());
        Assert.True(await caption.EvaluateAsync<bool>("""
            text => {
                const a=text.closest('[data-cfx-role="topology-group"]').querySelector('[data-cfx-role="topology-group-surface"]').getBBox();
                const b=text.getBBox();
                return b.x>=a.x-.2&&b.y>=a.y-.2&&b.x+b.width<=a.x+a.width+.2&&b.y+b.height<=a.y+a.height+.2;
            }
            """));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory); var path = Path.Combine(directory, "event-long-lane-browser-" + (dark ? "dark" : "light"));
            File.WriteAllText(path + ".svg", result.Artifact.ToSvg()); File.WriteAllBytes(path + ".native.png", result.Artifact.ToPng());
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = path + ".browser.png", FullPage = true });
        }
    }

    // Diagnostic only: actual native input, no focus/scroll assignment or event cancellation.
    private static async Task BeginStaticHostKeyTraceAsync(IPage page, ILocator host) {
        Assert.True(await host.EvaluateAsync<bool>("""
            host => {
                const candidates=[...document.querySelectorAll('a[href],area[href],button,input,select,textarea,iframe,[tabindex],[contenteditable]')]
                    .filter(node=>node.tabIndex>=0&&!node.disabled&&node.getClientRects().length
                        &&getComputedStyle(node).visibility!=='hidden'&&getComputedStyle(node).display!=='none');
                const identify=node=>node instanceof Element?{tag:node.tagName,id:node.id,role:node.getAttribute('role'),sizing:node.getAttribute('data-cfx-html-sizing')}:null;
                const proof=window.staticHostKeyProof={events:[],dropped:0,initialActive:identify(document.activeElement),
                    initialScrollLeft:host.scrollLeft,tabStops:candidates.map(identify),documentFocus:document.hasFocus(),
                    visibility:document.visibilityState,hidden:document.hidden};
                for(const kind of ['focus','blur'])window.addEventListener(kind,event=>{
                    if(proof.events.length>=96){proof.dropped++;return;}
                    proof.events.push({kind:'window-'+kind,time:performance.now(),trusted:event.isTrusted,
                        active:identify(document.activeElement),documentFocus:document.hasFocus(),visibility:document.visibilityState});
                },{passive:true});
                for(const kind of ['keydown','keyup','focusin','focusout','scroll'])for(const capture of [true,false])
                    document.addEventListener(kind,event=>{
                        if(proof.events.length>=96){proof.dropped++;return;}
                        const row={kind,capture,time:performance.now(),key:event.key??null,code:event.code??null,
                            trusted:event.isTrusted,defaultPrevented:event.defaultPrevented,target:identify(event.target),
                            related:identify(event.relatedTarget),active:identify(document.activeElement),scrollLeft:host.scrollLeft,
                            documentFocus:document.hasFocus(),visibility:document.visibilityState};
                        proof.events.push(row);
                        queueMicrotask(()=>{row.defaultPreventedAfter=event.defaultPrevented;row.scrollLeftAfterMicrotask=host.scrollLeft;});
                    },{capture,passive:true});
                return candidates.length===1&&candidates[0]===host&&document.activeElement!==host&&host.scrollLeft===0;
            }
            """), "The fresh static page has this single native tab stop and starts without host focus or horizontal scrolling.");
    }

    private static async Task CaptureStaticHostKeyTraceAsync(IPage page, bool dark, int width, string phase) {
        var directory=Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if(string.IsNullOrWhiteSpace(directory))return;
        if(!Path.IsPathRooted(directory))throw new InvalidOperationException("Capture output must be an absolute path.");
        Directory.CreateDirectory(directory);
        var raw=await page.EvaluateAsync<string>("""
            () => {
                const host=document.querySelector('[data-cfx-html-sizing=preserve-size]'),active=document.activeElement;
                return JSON.stringify({proof:window.staticHostKeyProof,state:{isActive:active===host,
                    documentFocus:document.hasFocus(),visibility:document.visibilityState,hidden:document.hidden,
                    userActivation:{active:navigator.userActivation?.isActive,hasBeenActive:navigator.userActivation?.hasBeenActive},
                    active:active?{tag:active.tagName,id:active.id,role:active.getAttribute('role')}:null,
                    focusVisible:host.matches(':focus-visible'),scrollLeft:host.scrollLeft,scrollWidth:host.scrollWidth,
                    clientWidth:host.clientWidth,scrollTop:host.scrollTop,hostRect:host.getBoundingClientRect().toJSON(),
                    overflowX:getComputedStyle(host).overflowX,scrollBehavior:getComputedStyle(host).scrollBehavior,
                    scriptCount:document.querySelectorAll('script').length}},null,2);
            }
            """);
        await File.WriteAllTextAsync(Path.Combine(directory,"event-native-tab-"+phase+"-"+width+(dark?"-dark":"-light")+".json"),raw);
    }

}
