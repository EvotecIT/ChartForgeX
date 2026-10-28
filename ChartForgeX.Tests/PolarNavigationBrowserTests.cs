using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Physical gestures request native Polar drawings through the same basis, cancellation and adaptive acceptance owner.</summary>
[Trait("BrowserSuite", "Compatibility")]
public sealed class PolarNavigationBrowserTests {
    [BrowserTheory]
    [InlineData(390, false)] [InlineData(390, true)] [InlineData(1100, false)] [InlineData(1100, true)]
    public async Task WheelAndKeyboardToolbarCommitOnlyTheHostPreparedDrawingAndRetainNativeExports(int width, bool dark) {
        if (!Enabled) return;
        var host = new Host(dark); await using var session = await BrowserProofFonts.OpenAsync(host.Page, width, 800);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await Connect(page, host);
        var revision = await page.EvaluateAsync<string>("()=>polar.sceneRevision"); var source = await Source(page);
        var svg = await page.Locator(".cfx-stage svg").GetAttributeAsync("viewBox");
        var pointer = await Point(page, 6.1, .8); await page.Mouse.MoveAsync(pointer.X, pointer.Y); await page.Mouse.WheelAsync(0, -120);
        await Pending(page, 1); Assert.Equal("zoom", await page.EvaluateAsync<string>("()=>requests[0].request.kind"));
        Assert.Equal(1.08, await page.EvaluateAsync<double>("()=>requests[0].request.factor"));
        Assert.Equal(revision, await page.EvaluateAsync<string>("()=>polar.sceneRevision")); Assert.Equal(source, await Source(page));
        await Resolve(page, 0); await Changed(page, 1);
        Assert.NotEqual(revision, await page.EvaluateAsync<string>("()=>polar.sceneRevision"));
        Assert.Equal(1.3 / 1.08, await page.EvaluateAsync<double>("()=>{const a=polar.getPolarViewport().angle;return a.maximum-a.minimum}"), 8);
        Assert.Equal(source, await Source(page)); Assert.Equal(svg, await page.Locator(".cfx-stage svg").GetAttributeAsync("viewBox"));
        Assert.True(await page.EvaluateAsync<bool>("()=>Number(polar.root.dataset.cfxZoom)===1&&Number(polar.root.dataset.cfxPanX)===0&&Number(polar.root.dataset.cfxPanY)===0"));
        var zoom = page.Locator("button[data-cfx-zoom=in]"); await zoom.FocusAsync(); await page.Keyboard.PressAsync("Enter");
        await Pending(page, 2); var accepted = await Resolve(page, 1); await Changed(page, 2);
        Assert.Equal("zoom", await page.EvaluateAsync<string>("()=>requests[1].request.kind"));
        Assert.Equal(1.25, await page.EvaluateAsync<double>("()=>requests[1].request.factor"));
        Assert.Equal("initial", await page.EvaluateAsync<string>("()=>requests[0].request.basis"));
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var path = await download.PathAsync() ?? throw new InvalidOperationException("The native polar SVG did not download.");
        var native = host.Snapshot(accepted, width < 700); var exported = XDocument.Load(path); var nativeSvg = XDocument.Parse(native.ToSvg());
        string[] Paths(XDocument document) => document.Descendants().Where(node => node.Name.LocalName == "path" &&
            new[] { "polar-line", "polar-area" }.Contains((string?)node.Attribute("data-cfx-role"))).Select(node => (string?)node.Attribute("d") ?? string.Empty).ToArray();
        Assert.NotEmpty(Paths(nativeSvg)); Assert.Equal(Paths(nativeSvg), Paths(exported));
        Assert.Equal(10, exported.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "polar-point-source"));
        PolarViewportContractTests.Capture("polar-gesture-accepted-" + width + "-" + dark, native.ToSvg(), native.ToPng());
        await Capture(page, "polar-gesture-wheel-" + width + "-" + (dark ? "dark" : "light"), download);
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false)] [InlineData(true)]
    public async Task AngularDragUnwrapsThePhysicalSeamAndMakesOneNativeRequestOnRelease(bool reversed) {
        if (!Enabled) return;
        var host = new Host(false, reversed, seam: true); await using var session = await BrowserProofFonts.OpenAsync(host.Page, 900, 800);
        var page = session.Page; await Connect(page, host, "['angle']");
        var before = await page.EvaluateAsync<string>("()=>polar.sceneRevision"); await page.Locator("[data-cfx-mode-button=pan]").ClickAsync();
        var first = await ScreenBearing(page, 170 * Math.PI / 180, .8); var last = await ScreenBearing(page, 190 * Math.PI / 180, .8);
        await page.Mouse.MoveAsync(first.X, first.Y); await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(last.X, last.Y, new MouseMoveOptions { Steps = 8 });
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>requests.length")); Assert.Equal(before, await page.EvaluateAsync<string>("()=>polar.sceneRevision"));
        await page.Mouse.UpAsync(); await Pending(page, 1);
        Assert.Equal("pan", await page.EvaluateAsync<string>("()=>requests[0].request.kind"));
        await Capture(page, "polar-angular-before-precision-assertion-" + reversed);
        // Real browser engines can quantize requested mouse positions to physical pixels.
        // Independently derive the precise pan from the actual trusted, captured stage input.
        var delivered = await page.EvaluateAsync<JsonElement>("""
            () => {
                const captured=row=>({...row,projectionJson:JSON.stringify(row.projection)});
                return {down:polarGestureEvents.filter(row=>row.type==='pointerdown'&&row.stageOwned).map(captured),
                    up:polarGestureEvents.filter(row=>row.type==='pointerup'&&row.stageOwned&&row.captured).map(captured)};
            }
            """);
        var deliveredDown = Assert.Single(delivered.GetProperty("down").EnumerateArray());
        var deliveredUp = Assert.Single(delivered.GetProperty("up").EnumerateArray());
        Assert.True(deliveredDown.GetProperty("trusted").GetBoolean()); Assert.True(deliveredUp.GetProperty("trusted").GetBoolean());
        Assert.Equal(deliveredDown.GetProperty("pointerId").GetInt32(), deliveredUp.GetProperty("pointerId").GetInt32());
        var downMatrix = deliveredDown.GetProperty("matrix"); var upMatrix = deliveredUp.GetProperty("matrix");
        foreach (var component in new[] { "a", "b", "c", "d", "e", "f" })
            Assert.Equal(downMatrix.GetProperty(component).GetDouble(), upMatrix.GetProperty(component).GetDouble());
        var projection = deliveredDown.GetProperty("projection");
        Assert.Equal(deliveredDown.GetProperty("projectionJson").GetString(), deliveredUp.GetProperty("projectionJson").GetString());
        var centerX = projection.GetProperty("centerX").GetDouble(); var centerY = projection.GetProperty("centerY").GetDouble();
        var downLogical = deliveredDown.GetProperty("logical"); var upLogical = deliveredUp.GetProperty("logical");
        var firstBearing = Math.Atan2(downLogical.GetProperty("y").GetDouble() - centerY, downLogical.GetProperty("x").GetDouble() - centerX);
        var lastBearing = Math.Atan2(upLogical.GetProperty("y").GetDouble() - centerY, upLogical.GetProperty("x").GetDouble() - centerX);
        Assert.InRange(firstBearing, Math.PI / 2, Math.PI); Assert.InRange(lastBearing, -Math.PI, -Math.PI / 2);
        AssertIntendedPointer(deliveredDown, 170 * Math.PI / 180); AssertIntendedPointer(deliveredUp, 190 * Math.PI / 180);
        var expectedClockwiseDelta = Math.Atan2(Math.Sin(lastBearing - firstBearing), Math.Cos(lastBearing - firstBearing));
        Assert.True(expectedClockwiseDelta > 0);
        Assert.Equal(expectedClockwiseDelta, await page.EvaluateAsync<double>("()=>requests[0].request.clockwiseAngleDelta"), 7);
        void AssertIntendedPointer(JsonElement actual, double bearing) {
            var matrix = actual.GetProperty("matrix"); var radius = projection.GetProperty("outerRadius").GetDouble() * .8;
            var x = centerX + Math.Cos(bearing) * radius; var y = centerY + Math.Sin(bearing) * radius;
            var clientX = matrix.GetProperty("a").GetDouble() * x + matrix.GetProperty("c").GetDouble() * y + matrix.GetProperty("e").GetDouble();
            var clientY = matrix.GetProperty("b").GetDouble() * x + matrix.GetProperty("d").GetDouble() * y + matrix.GetProperty("f").GetDouble();
            // This bounds physical input delivery, not the precision of the requested native delta.
            Assert.InRange(Math.Abs(actual.GetProperty("clientX").GetDouble() - clientX), 0d, 1d);
            Assert.InRange(Math.Abs(actual.GetProperty("clientY").GetDouble() - clientY), 0d, 1d);
        }
        await Resolve(page, 0); await Changed(page, 1);
        var expectedMinimum = 2.6 + (reversed ? 1 : -1) * expectedClockwiseDelta;
        Assert.Equal(expectedMinimum, await page.EvaluateAsync<double>("()=>polar.getPolarViewport().angle.minimum"), 7);
        Assert.Equal(20, await page.EvaluateAsync<double>("()=>polar.getPolarViewport().radius.minimum"));
        Assert.Equal(10, await page.EvaluateAsync<double>("()=>polar.getPolarViewport().secondaryRadius.minimum"));
        Assert.True(await page.EvaluateAsync<bool>("()=>Number(polar.root.dataset.cfxZoom)===1&&Number(polar.root.dataset.cfxPanX)===0&&Number(polar.root.dataset.cfxPanY)===0"));
        await Capture(page, "polar-angular-drag-seam-" + reversed); AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(390)] [InlineData(1100)]
    public async Task RadialDragMovesBothSelectedTransformsWithoutChangingTheDirectionalWindow(int width) {
        if (!Enabled) return;
        var host = new Host(width == 390); await using var session = await BrowserProofFonts.OpenAsync(host.Page, width, 800);
        var page = session.Page; await Connect(page, host, "['radius','secondaryRadius']");
        await page.Locator("[data-cfx-mode-button=pan]").ClickAsync(); var source = await Source(page);
        var first = await Point(page, 6.1, .75); var last = await Point(page, 6.1, .85);
        await page.Mouse.MoveAsync(first.X, first.Y); await page.Mouse.DownAsync(); await page.Mouse.MoveAsync(last.X, last.Y, new MouseMoveOptions { Steps = 4 });
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>requests.length"));
        await page.Mouse.UpAsync(); await Pending(page, 1);
        Assert.True(await page.EvaluateAsync<bool>("()=>requests[0].request.outwardRadiusDelta>0"));
        Assert.Equal(new[] { "radius", "secondaryRadius" }, await page.EvaluateAsync<string[]>("()=>requests[0].request.axes"));
        await Resolve(page, 0); await Changed(page, 1);
        Assert.Equal(5.5, await page.EvaluateAsync<double>("()=>polar.getPolarViewport().angle.minimum"));
        Assert.Equal(6.8, await page.EvaluateAsync<double>("()=>polar.getPolarViewport().angle.maximum"));
        Assert.Equal(60, await page.EvaluateAsync<double>("()=>{const r=polar.getPolarViewport().radius;return r.maximum-r.minimum}"), 7);
        Assert.Equal(10, await page.EvaluateAsync<double>("()=>{const r=polar.getPolarViewport().secondaryRadius;return r.maximum/r.minimum}"), 7);
        Assert.Equal(source, await Source(page)); await Capture(page, "polar-radial-drag-" + width); AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task PointerCancelNeverManufacturesAPanRequest() {
        if (!Enabled) return;
        var host = new Host(false); await using var session = await BrowserProofFonts.OpenAsync(host.Page, 900, 800);
        var page = session.Page; await Connect(page, host); await page.Locator("[data-cfx-mode-button=pan]").ClickAsync();
        var first = await Point(page, 6.1, .8); await page.EvaluateAsync("()=>polar.root.querySelector('.cfx-stage').addEventListener('pointerdown',event=>window.gesturePointerId=event.pointerId,{once:true})"); await page.Mouse.MoveAsync(first.X, first.Y); await page.Mouse.DownAsync();
        await page.EvaluateAsync("()=>{const stage=polar.root.querySelector('.cfx-stage');stage.dispatchEvent(new PointerEvent('pointercancel',{pointerId:gesturePointerId,pointerType:'mouse',bubbles:true}));}");
        await page.Mouse.UpAsync();
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>requests.length")); AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task HtmlStringConnectionUsesTheSameOptInNativeGestureOwner() {
        if (!Enabled) return;
        var host = new Host(false); await using var session = await BrowserProofFonts.OpenAsync(host.Page, 900, 800);
        var page = session.Page; await Connect(page, host); var revision = await page.EvaluateAsync<string>("()=>polar.sceneRevision");
        await page.EvaluateAsync("()=>polar.connectPolarViewport(async request=>(await preparePolarHost({...request,basis:'initial'})).html,{axes:['angle'],gestures:true})");
        await page.Locator("button[data-cfx-zoom=in]").ClickAsync(); await Changed(page, 1);
        Assert.NotEqual(revision, await page.EvaluateAsync<string>("()=>polar.sceneRevision"));
        Assert.Equal(1.3 / 1.25, await page.EvaluateAsync<double>("()=>{const a=polar.getPolarViewport().angle;return a.maximum-a.minimum}"), 8);
        Assert.Equal(20, await page.EvaluateAsync<double>("()=>polar.getPolarViewport().radius.minimum")); AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task OptInHitAndScrollOwnershipDoNotConsumeUnconfiguredOrUnsupportedGestures() {
        if (!Enabled) return;
        var host = new Host(false); await using var session = await BrowserProofFonts.OpenAsync(host.Page, 900, 800);
        var page = session.Page; await Connect(page, host, gestures: false);
        Assert.True(await page.Locator("button[data-cfx-zoom]").EvaluateAllAsync<bool>("nodes=>nodes.every(node=>node.disabled)"));
        var hit = await Point(page, 6.1, .8);
        Assert.False(await WheelEvent(page, hit)); Assert.Equal(0, await page.EvaluateAsync<int>("()=>requests.length"));
        await Connect(page, host); Assert.False(await page.Locator("button[data-cfx-zoom=in]").IsDisabledAsync());
        Assert.True(await page.EvaluateAsync<bool>("()=>{for(const value of ['yes',1,null,{}]){let failed=false;try{polar.connectNative(()=>{}, {basis:'initial',coordinateSystem:'polar',gestures:value});}catch(error){failed=error instanceof TypeError;}if(!failed)return false;}return true;}"));
        foreach (var point in new[] { await Point(page, 6.1, 0), await Point(page, 6.1, .1), await Point(page, 3, .8), await Point(page, 6.1, 1.2) }) Assert.False(await WheelEvent(page, point));
        Assert.False(await WheelEvent(page, hit, ctrl: true)); Assert.False(await WheelEvent(page, hit, delta: 0));
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>requests.length"));
        Assert.Equal("auto", await page.Locator(".cfx-stage").EvaluateAsync<string>("node=>getComputedStyle(node).touchAction"));
        await page.Locator("[data-cfx-mode-button=pan]").ClickAsync();
        Assert.Equal("none", await page.Locator(".cfx-stage").EvaluateAsync<string>("node=>getComputedStyle(node).touchAction"));
        await page.Locator("[data-cfx-mode-button=pan]").ClickAsync();
        Assert.Equal("auto", await page.Locator(".cfx-stage").EvaluateAsync<string>("node=>getComputedStyle(node).touchAction"));
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(390)] [InlineData(1100)]
    public async Task EscapeCancelsPendingGesturesAndUncommittedDragsWithoutAdvancingBasis(int width) {
        if (!Enabled) return;
        var host = new Host(false); await using var session = await BrowserProofFonts.OpenAsync(host.Page, width, 800);
        var page = session.Page; await Connect(page, host); var revision = await page.EvaluateAsync<string>("()=>polar.sceneRevision");
        var hit = await Point(page, 6.1, .8); await page.Mouse.MoveAsync(hit.X, hit.Y); await page.Mouse.WheelAsync(0, -120); await Pending(page, 1);
        await page.EvaluateAsync("async()=>window.cancelledReply=await preparePolarHost(requests[0].request)");
        await page.Locator("button[data-cfx-zoom=in]").FocusAsync(); await page.Keyboard.PressAsync("Escape");
        Assert.True(await page.EvaluateAsync<bool>("()=>requests[0].signal.aborted"));
        await page.EvaluateAsync("()=>requests[0].resolve(cancelledReply)"); await Flush(page);
        Assert.Equal(revision, await page.EvaluateAsync<string>("()=>polar.sceneRevision")); Assert.Equal(0, await page.EvaluateAsync<int>("()=>changes.length"));
        await page.Locator("[data-cfx-mode-button=pan]").ClickAsync();
        await page.Mouse.MoveAsync(hit.X, hit.Y); await page.Mouse.DownAsync(); await page.Mouse.MoveAsync(hit.X + 10, hit.Y + 3);
        await page.Keyboard.PressAsync("Escape"); await page.Mouse.UpAsync();
        await Capture(page, "polar-escape-before-request-count-assertion-" + width);
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>requests.length"));
        await page.Locator("button[data-cfx-zoom=in]").ClickAsync(); await Pending(page, 2);
        Assert.Equal("initial", await page.EvaluateAsync<string>("()=>requests[1].request.basis"));
        await Resolve(page, 1); await Changed(page, 1);
        await page.Locator("[data-cfx-reset]").ClickAsync(); await Pending(page, 3); await Resolve(page, 2); await Changed(page, 2);
        Assert.Equal(0, await page.EvaluateAsync<double>("()=>polar.getPolarViewport().radius.innerRadius"));
        Assert.Equal(Math.PI * 2, await page.EvaluateAsync<double>("()=>polar.getPolarViewport().angle.maximum"));
        await Capture(page, "polar-gesture-cancel-reset-" + width); AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(390, "domain")] [InlineData(1100, "domain")]
    [InlineData(390, "source")] [InlineData(1100, "source")]
    [InlineData(390, "unselected")] [InlineData(1100, "unselected")]
    [InlineData(390, "annulus")] [InlineData(1100, "annulus")]
    public async Task InvalidAlternateSourceOrProjectionNeverReplacesAnAcceptedDrawing(int width, string mutation) {
        if (!Enabled) return;
        var host = new Host(false); await using var session = await BrowserProofFonts.OpenAsync(host.Page, width, 800);
        var page = session.Page; await Connect(page, host, "['angle']");
        var revision = await page.EvaluateAsync<string>("()=>polar.sceneRevision"); var source = await Source(page);
        await page.Locator("button[data-cfx-zoom=in]").ClickAsync(); await Pending(page, 1);
        await page.EvaluateAsync("""
            async mutation=>{
                const request=requests[0].request,reply=await preparePolarHost(request),template=document.createElement('template');
                template.innerHTML=reply.html;const root=template.content.firstElementChild;
                // Fresh generated replies have a wide outer drawing and a narrow template. The live runtime owns its separate wide template.
                const narrow=Array.from(root.children).find(node=>node instanceof HTMLTemplateElement&&node.hasAttribute('data-cfx-adaptive-narrow'));
                if(root.dataset.cfxAdaptiveDrawing!=='wide'||!narrow||narrow.content.children.length!==1
                    ||!['wide','narrow'].includes(request.drawing)||request.drawing!==polar.root.dataset.cfxAdaptiveDrawing)
                    throw Error('Fixture requires the generated wide/narrow reply and the current captured request drawing.');
                const alternate=request.drawing==='narrow'?root:narrow.content.firstElementChild;
                if(alternate.dataset.cfxAdaptiveDrawing!==(request.drawing==='narrow'?'wide':'narrow'))throw Error('Fixture must mutate the inactive native drawing.');
                const meta=JSON.parse(alternate.dataset.cfxPreparedChart);
                if(mutation==='domain')meta.polarProjection.secondaryRadius.fullMaximum+=1;
                else if(mutation==='annulus')meta.polarProjection.radius.innerRadius=-1;
                else if(mutation==='unselected')meta.polarProjection.secondaryRadius.minimum+=1;
                else alternate.querySelector('[data-cfx-role=polar-point-source]').setAttribute('data-cfx-angle','99');
                alternate.dataset.cfxPreparedChart=JSON.stringify(meta);requests[0].resolve({html:root.outerHTML,basis:'invalid'});
            }
            """, mutation);
        await page.WaitForFunctionAsync("()=>errors.length===1"); Assert.Equal(revision, await page.EvaluateAsync<string>("()=>polar.sceneRevision"));
        Assert.Equal(source, await Source(page)); Assert.Equal(0, await page.EvaluateAsync<int>("()=>changes.length"));
        await page.Locator("button[data-cfx-zoom=in]").ClickAsync(); await Pending(page, 2);
        Assert.Equal("initial", await page.EvaluateAsync<string>("()=>requests[1].request.basis"));
        await Resolve(page, 1); await Changed(page, 1); await Capture(page, "polar-gesture-reject-" + mutation + "-" + width);
        AssertNoConsoleErrors(session);
    }

    private static async Task Connect(IPage page, Host host, string axes = "['angle','radius','secondaryRadius']", bool gestures = true) {
        if (!await page.EvaluateAsync<bool>("()=>typeof preparePolarHost==='function'"))
            await page.ExposeFunctionAsync("preparePolarHost", (JsonElement request) => host.Prepare(request));
        await page.EvaluateAsync("()=>{window.polar=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));window.requests=[];window.changes=[];window.errors=[];polar.root.addEventListener('cfxviewportchange',event=>changes.push(event.detail));polar.root.addEventListener('cfxviewporterror',event=>errors.push(event.detail));polar.connectNative((request,signal)=>new Promise(resolve=>requests.push({request,signal,resolve})),{basis:'initial',coordinateSystem:'polar',axes:" + axes + ",gestures:" + (gestures ? "true" : "false") + "});}");
        await page.EvaluateAsync(PolarGestureProbe);
    }
    private static Task Pending(IPage page, int count) => page.WaitForFunctionAsync("count=>requests.length===count", count);
    private static Task Changed(IPage page, int count) => page.WaitForFunctionAsync("count=>changes.length===count", count);
    private static Task Flush(IPage page) => page.EvaluateAsync("()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))");
    private static Task<string> Resolve(IPage page, int index) => page.EvaluateAsync<string>("async index=>{const reply=await preparePolarHost(requests[index].request);requests[index].resolve(reply);return reply.basis;}", index);
    private static Task<string> Source(IPage page) => page.EvaluateAsync<string>("()=>JSON.stringify(Array.from(document.querySelectorAll('[data-cfx-role=polar-point-source]'),node=>[node.dataset.cfxAngle,node.dataset.cfxValue,node.dataset.cfxSourceId,node.dataset.cfxObservationId]))");
    private static async Task<Pointer> Point(IPage page, double angle, double fraction) {
        var point = await page.EvaluateAsync<float[]>("({angle,fraction})=>{const p=JSON.parse(polar.root.dataset.cfxPreparedChart).polarProjection;const a=(p.angleReversed?angle:-angle)+p.rotationRadians;const point=new DOMPoint(p.centerX+Math.cos(a)*p.outerRadius*fraction,p.centerY+Math.sin(a)*p.outerRadius*fraction).matrixTransform(polar.root.querySelector('.cfx-stage svg').getScreenCTM());return [point.x,point.y];}", new { angle, fraction });
        return new Pointer(point[0], point[1]);
    }
    private static async Task<Pointer> ScreenBearing(IPage page, double angle, double fraction) {
        var point = await page.EvaluateAsync<float[]>("({angle,fraction})=>{const p=JSON.parse(polar.root.dataset.cfxPreparedChart).polarProjection;const point=new DOMPoint(p.centerX+Math.cos(angle)*p.outerRadius*fraction,p.centerY+Math.sin(angle)*p.outerRadius*fraction).matrixTransform(polar.root.querySelector('.cfx-stage svg').getScreenCTM());return [point.x,point.y];}", new { angle, fraction });
        return new Pointer(point[0], point[1]);
    }
    private static Task<bool> WheelEvent(IPage page, Pointer point, bool ctrl = false, double delta = -120) => page.EvaluateAsync<bool>("({x,y,ctrl,delta})=>{if(![x,y,delta].every(Number.isFinite))throw Error('Fixture requires finite wheel coordinates/delta: '+JSON.stringify({x,y,delta}));const event=new WheelEvent('wheel',{clientX:x,clientY:y,deltaY:delta,ctrlKey:ctrl,bubbles:true,cancelable:true});polar.root.querySelector('.cfx-stage').dispatchEvent(event);return event.defaultPrevented;}", new { x = (double)point.X, y = (double)point.Y, ctrl, delta });
    private static async Task Capture(IPage page, string name, IDownload? download = null) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png"), FullPage = true });
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".json"), await page.EvaluateAsync<string>("()=>JSON.stringify({requests:requests.map(item=>item.request),changes,errors,view:polar.getPolarViewport(),gestureEvents:window.polarGestureEvents||[]},null,2)"));
        if (download != null) await download.SaveAsAsync(Path.Combine(directory, name + ".svg"));
    }
    // Passive physical input trace. Expected precision, cancellation and request assertions are unchanged.
    private const string PolarGestureProbe = """
        () => {
            if(window.polarGestureTraceInstalled)return;window.polarGestureTraceInstalled=true;
            const root=polar.root,stage=root.querySelector('.cfx-stage'),rows=window.polarGestureEvents=[];
            const identity=node=>node instanceof Element?{tag:node.tagName,id:node.id,mode:node.dataset.cfxModeButton,
                role:node.dataset.cfxRole,inside:root.contains(node),focusVisible:node.matches(':focus-visible')}:null;
            for(const type of ['pointerdown','pointermove','pointerup','pointercancel','gotpointercapture','lostpointercapture','keydown','keyup'])
                document.addEventListener(type,event=>{
                    if(type==='pointermove'&&!event.buttons)return;
                    const svg=root.querySelector('.cfx-stage svg'),matrix=svg?.getScreenCTM(),projection=JSON.parse(root.dataset.cfxPreparedChart).polarProjection;
                    const point=matrix&&Number.isFinite(event.clientX)&&Number.isFinite(event.clientY)
                        ?new DOMPoint(event.clientX,event.clientY).matrixTransform(matrix.inverse()):null;
                    const row={type,time:performance.now(),trusted:event.isTrusted,key:event.key,code:event.code,
                        stageOwned:stage===event.target||stage.contains(event.composedPath()[0]),
                        clientX:event.clientX,clientY:event.clientY,pointerId:event.pointerId,buttons:event.buttons,
                        target:identity(event.composedPath()[0]),active:identity(document.activeElement),documentFocus:document.hasFocus(),
                        path:event.composedPath().filter(node=>node instanceof Element).slice(0,8).map(identity),
                        captured:Number.isInteger(event.pointerId)&&stage.hasPointerCapture(event.pointerId),prevented:event.defaultPrevented,
                        matrix:matrix?{a:matrix.a,b:matrix.b,c:matrix.c,d:matrix.d,e:matrix.e,f:matrix.f}:null,
                        logical:point?{x:point.x,y:point.y,bearing:Math.atan2(point.y-projection.centerY,point.x-projection.centerX),
                            radius:Math.hypot(point.x-projection.centerX,point.y-projection.centerY)}:null,
                        projection,requestCount:requests.length};
                    rows.push(row);queueMicrotask(()=>{row.preventedAfter=event.defaultPrevented;row.requestCountAfter=requests.length;
                        row.capturedAfter=Number.isInteger(event.pointerId)&&stage.hasPointerCapture(event.pointerId);});
                },true);
        }
        """;

    private sealed record Pointer(float X, float Y);
    private sealed class Host {
        private readonly Chart _chart;
        private readonly Dictionary<string, HtmlChartSection> _snapshots = new();
        internal Host(bool dark, bool reversed = false, bool seam = false) {
            _chart = PolarViewportRecipe.Create(900, dark ? VisualThemeMode.Dark : VisualThemeMode.Light).WithFontFamily(BrowserProofFonts.Family);
            _chart.Options.XAxis.Reversed = reversed;
            var window = seam ? new ChartPolarViewport(new ChartAxisWindow(2.6, 3.7), new ChartAxisWindow(20, 80), new ChartAxisWindow(10, 100)) : PolarNavigationTests.Window();
            _snapshots.Add("initial", PolarNavigationRecipe.PrepareSection(_chart, window));
        }
        internal string Page => "<style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle() + "</style>" + _snapshots["initial"].Html + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script>";
        internal object Prepare(JsonElement request) {
            lock (_snapshots) {
                var basis = _snapshots[request.GetProperty("basis").GetString()!];
                var section = PolarNavigationRecipe.PrepareReply(_chart, basis, request); var next = "polar-" + _snapshots.Count;
                _snapshots.Add(next, section); return new { html = section.Html, basis = next };
            }
        }
        internal ChartForgeX.Rendering.PreparedVisual Snapshot(string basis, bool narrow) => narrow ? _snapshots[basis].NarrowPreparedVisual! : _snapshots[basis].PreparedVisual;
    }
}
