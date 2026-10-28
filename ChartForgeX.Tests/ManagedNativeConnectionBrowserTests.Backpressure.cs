using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class ManagedNativeConnectionBrowserTests {
    [BrowserTheory]
    [Trait("BrowserSuite", "Compatibility")]
    [InlineData(390, false, false)] [InlineData(390, true, false)]
    [InlineData(900, false, false)] [InlineData(900, true, false)]
    [InlineData(390, false, true)] [InlineData(390, true, true)]
    [InlineData(900, false, true)] [InlineData(900, true, true)]
    public async Task NativeProducerAdmissionKeepsLatestTypedFactsBusyInputsResetSelectionAndFinalDownloads(int width, bool dark, bool typed) {
        if (!Enabled) return;
        var host = new NativeHost(width, dark ? VisualThemeMode.Dark : VisualThemeMode.Light, typed, navigation: true);
        await using var session = await BrowserProofFonts.OpenAsync(host.Page, width + 24, 800);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await ConnectLatestWindowAsync(page, host);
        await page.EvaluateAsync("() => {window.admissionEditor=managed.editAnnotations();}");
        await page.EvaluateAsync("() => managed.select({targets:[managed.resolveTargets().find(target=>target.annotationId==='note')]})");
        await page.Locator("[data-cfx-x-range=start]").FocusAsync();
        await page.EvaluateAsync("() => managed.setViewport({x:{minimum:1,maximum:9}})"); await PendingAsync(page, 1);
        await page.EvaluateAsync("() => {for(let index=0;index<1000;index++)managed.setViewport({x:{minimum:2+index/10000,maximum:8-index/10000}})}");
        await DrainAsync(page);
        await CaptureAdmissionAsync(page, host, "initial", "queued-" + width + "-" + dark + "-" + typed, errors, session.ConsoleLog);
        Assert.Equal(1, await page.EvaluateAsync<int>("() => pending.length"));
        Assert.True(await page.EvaluateAsync<bool>("() => pending[0].signal.aborted&&[...managed.root.querySelectorAll('[data-cfx-export],[data-cfx-zoom]')].every(button=>button.disabled)"));
        Assert.True(await page.EvaluateAsync<bool>("() => [...managed.root.querySelectorAll('[data-cfx-x-navigation] input,[data-cfx-x-navigation] button,.cfx-annotation-editor input,.cfx-annotation-editor button,.cfx-annotation-editor select')].every(input=>input.disabled)"));
        Assert.Equal("TypeError", await page.EvaluateAsync<string>("() => {try{managed.applyAnnotationChange({operation:'remove',id:'note'});return 'accepted';}catch(error){return error.name;}}"));
        Assert.Equal(1, await page.EvaluateAsync<int>("() => pending.length"));
        host.ReplaceProducerOffset(7);
        await ResolveAsync(page, 0); await PendingAsync(page, 2);
        Assert.Equal("initial", await RequestFieldAsync(page, 1, "basis"));
        var basis = await ResolveAsync(page, 1); await ChangesAsync(page, 1);
        await CaptureAdmissionAsync(page, host, basis, "accepted-" + width + "-" + dark + "-" + typed, errors, session.ConsoleLog);
        AssertWindow(await page.EvaluateAsync<JsonElement>("() => managed.getXWindow()"), 2.0999, 7.9001);
        Assert.Equal("note", await page.EvaluateAsync<string>("() => managed.getSelection()[0].annotationId"));
        Assert.True(await page.Locator("[data-cfx-x-range=start]").EvaluateAsync<bool>("node=>node===document.activeElement"));
        var reading = page.Locator("g[data-cfx-series-key=readings][data-cfx-source-point='5'][data-cfx-point]");
        Assert.Equal("52", await reading.GetAttributeAsync(typed ? "data-cfx-start" : "data-cfx-y"));
        if (typed) { Assert.Equal("60", await reading.GetAttributeAsync("data-cfx-end")); Assert.Equal("reading-5", await reading.GetAttributeAsync("data-cfx-observation-id")); }
        Assert.True(await page.EvaluateAsync<bool>("() => [...managed.root.querySelectorAll('[data-cfx-export]')].every(button=>!button.disabled)"));
        var svg = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var svgPath = await svg.PathAsync() ?? throw new InvalidOperationException("The accepted native SVG did not download.");
        var exported = XDocument.Load(svgPath);
        Assert.Single(exported.Descendants(), node => (string?)node.Attribute("data-cfx-annotation-id") == "note");
        Assert.Contains(exported.Descendants(), node => (string?)node.Attribute("data-cfx-source-point") == "5"
            && (string?)node.Attribute(typed ? "data-cfx-start" : "data-cfx-y") == "52");
        var png = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=png]").ClickAsync());
        var pngPath = await png.PathAsync() ?? throw new InvalidOperationException("The accepted native PNG did not download.");
        var decoded = PngReader.Decode(await File.ReadAllBytesAsync(pngPath)); Assert.True(decoded.Width > 0 && decoded.Height > 0);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            File.Copy(svgPath, Path.Combine(output, "native-admission-final-" + width + "-" + dark + "-" + typed + ".export.svg"), true);
            File.Copy(pngPath, Path.Combine(output, "native-admission-final-" + width + "-" + dark + "-" + typed + ".export.png"), true);
        }
        Assert.Empty(await page.EvaluateAsync<string[]>("() => managedErrors.map(error=>error.message)"));
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }

    private static async Task ConnectLatestWindowAsync(IPage page, NativeHost host) {
        await ConnectAsync(page, host, "x");
        await page.EvaluateAsync(AdmissionFocusProbe);
        await page.EvaluateAsync("""
            () => {window.admissionRequests=[];managed.root.addEventListener('cfxviewportrequest',event=>admissionRequests.push(event.detail));
                managed.connectNative(controlledNative,{basis:'initial',axes:['x'],preparationBackpressure:'latest-window'});}
            """);
    }

    // Passive diagnostic for browser-specific disabled-control focus ordering; never changes focus or eligibility.
    private const string AdmissionFocusProbe = """
        () => {
            const root=document.querySelector('.cfx-interactive-chart'),ids=new WeakMap();let next=0;
            const describe=node=>node instanceof Element?{identity:ids.get(node)||(()=>{const id=++next;ids.set(node,id);return id;})(),
                tag:node.tagName,id:node.id,dataset:{...node.dataset},disabled:node.disabled,connected:node.isConnected,
                inside:root.contains(node),focusVisible:node.matches(':focus-visible')}:null;
            const rows=window.admissionFocusEvents=[];
            const snapshot=(type,extra={})=>rows.push({type,time:performance.now(),documentFocus:document.hasFocus(),
                active:describe(document.activeElement),pending:window.pending?.length,revision:window.managed?.sceneRevision,...extra});
            for(const type of ['pointerdown','click','focusin','focusout','keydown'])document.addEventListener(type,event=>{
                snapshot(type,{trusted:event.isTrusted,key:event.key,target:describe(event.composedPath()[0]),related:describe(event.relatedTarget),
                    prevented:event.defaultPrevented});
                queueMicrotask(()=>snapshot(type+'-microtask',{target:describe(event.composedPath()[0]||event.target)}));
            },true);
            new MutationObserver(records=>snapshot('native-control-mutations',{recordCount:records.length,
                changes:[...new Set(records.map(record=>record.target))].filter(node=>node instanceof Element)
                    .map(node=>({node:describe(node),attributes:[...new Set(records.filter(record=>record.target===node).map(record=>record.attributeName))]}))}))
                .observe(root,{attributes:true,subtree:true,attributeFilter:['disabled','aria-busy','hidden']});
            for(const type of ['cfxviewportrequest','cfxviewportchange','cfxviewporterror','cfxupdated'])root.addEventListener(type,event=>snapshot(type,{detail:event.detail}));
            window.addEventListener('blur',()=>snapshot('window-blur'));
            snapshot('probe-installed');
        }
        """;

    private static async Task CaptureAdmissionAsync(IPage page, NativeHost host, string basis, string name, IEnumerable<string> errors, object consoleLog) {
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output); var stem = Path.Combine(output, "native-admission-" + name); var section = host.Section(basis);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
        var observed = await page.EvaluateAsync<JsonElement>("""
            () => ({requests:pending.map(item=>({request:item.request,aborted:item.signal.aborted})),events:window.admissionRequests||[],
                changes:managedChanges,errors:managedErrors,revision:managed.sceneRevision,viewport:managed.getViewport(),selection:managed.getSelection(),
                focusEvents:window.admissionFocusEvents||[],
                active:document.activeElement?{tag:document.activeElement.tagName,control:document.activeElement.dataset?.cfxXControl,disabled:document.activeElement.disabled,connected:document.activeElement.isConnected}:null,
                controls:[...managed.root.querySelectorAll('[data-cfx-export],[data-cfx-zoom],[data-cfx-reset]')].map(node=>({action:node.dataset.cfxExport||node.dataset.cfxZoom||'reset',disabled:node.disabled,hidden:node.hidden})),
                marks:[...managed.root.querySelectorAll('g[data-cfx-series-key=readings][data-cfx-source-point][data-cfx-point]')].map(node=>({source:node.dataset.cfxSourcePoint,observation:node.dataset.cfxObservationId,label:node.getAttribute('aria-label'),box:node.getBoundingClientRect().toJSON()}))})
            """);
        File.WriteAllText(stem + ".observed.json", JsonSerializer.Serialize(new { observed, pageErrors = errors, consoleLog }));
        File.WriteAllText(stem + ".native.svg", section.PreparedVisual.ToSvg()); File.WriteAllBytes(stem + ".native.png", section.PreparedVisual.ToPng());
        File.WriteAllText(stem + ".section.html", section.Html);
    }
}
