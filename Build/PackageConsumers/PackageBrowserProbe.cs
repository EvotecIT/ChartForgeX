using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using HtmlTinkerX;
using Microsoft.Playwright;

/// <summary>Exercises the scripts and editor actually embedded in the restored HTML adapter package.</summary>
internal static class PackageBrowserProbe {
    internal static async Task RunDocumentAsync(string html) {
        if (Environment.GetEnvironmentVariable("HFX_TEST_ENABLE_PLAYWRIGHT") != "1") {
            Console.WriteLine("Public HTML document browser execution not requested; set HFX_TEST_ENABLE_PLAYWRIGHT=1 to qualify it.");
            return;
        }
        foreach (var width in new[] { 390, 900 }) {
            await using var session = await HtmlBrowser.OpenSessionAsync("about:blank", new HtmlBrowserLaunchOptions {
                Browser = Engine(), Headless = true, ViewportWidth = width, ViewportHeight = 760, Timeout = 15000
            });
            var errors = new System.Collections.Generic.List<string>();
            session.Page.PageError += (_, error) => errors.Add(error);
            await session.Page.SetContentAsync(html);
            PackageAssertions.Require(await session.Page.Locator("svg:visible g[data-cfx-role='pyramid-stage']").CountAsync() == 16,
                "The public HTML adapter did not display every candidate partition at " + width + " pixels.");
            PackageAssertions.Require(await session.Page.Locator("svg:visible [data-cfx-role='pyramid-label']").CountAsync() > 0,
                "The public HTML adapter displayed no readable candidate captions.");
            await Capture(session.Page, "public-html-adapter-" + width + "-" + Engine());
            PackageAssertions.Require(errors.Count == 0 && !session.ConsoleLog.Any(entry => entry.Type == HtmlConsoleMessageType.Error),
                "The public HTML adapter reported a runtime error: " + string.Join("; ", errors));
        }
        Console.WriteLine("Public HTML adapter displayed the staged SVG at compact and wide viewports in " + Engine() + ".");
    }

    internal static async Task RunAsync(string chartHtml, string graphHtml) {
        if (Environment.GetEnvironmentVariable("HFX_TEST_ENABLE_PLAYWRIGHT") != "1") {
            Console.WriteLine("Packed HTML browser execution not requested; set HFX_TEST_ENABLE_PLAYWRIGHT=1 to qualify it.");
            return;
        }
        var engine = Engine();
        foreach (var width in new[] { 390, 900 }) {
            await using var session = await HtmlBrowser.OpenSessionAsync("about:blank", new HtmlBrowserLaunchOptions {
                Browser = engine, Headless = true, ViewportWidth = width, ViewportHeight = 760, Timeout = 15000
            });
            var page = session.Page;
            var errors = new System.Collections.Generic.List<string>();
            page.PageError += (_, error) => errors.Add(error);
            await page.SetContentAsync(chartHtml);
            var point = page.Locator("g[data-cfx-role='point'][data-cfx-observation-id='package-bubble']");
            await point.FocusAsync();
            await page.Keyboard.PressAsync("Space");
            PackageAssertions.Require(await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-compare-count") == "1",
                "Packed chart keyboard selection did not execute.");
            PackageAssertions.Require(await point.GetAttributeAsync("data-cfx-size") == "7",
                "Packed chart selection lost the retained bubble size fact.");
            await Capture(page, "packed-chart-" + width + "-" + engine);

            await page.SetContentAsync(graphHtml);
            await page.Locator("[data-cfx-graph-action='edit']").ClickAsync();
            await page.Locator("[data-cfx-graph-editor-action='add-node']").ClickAsync();
            var form = page.Locator("[data-cfx-graph-editor-form='node']");
            await form.Locator("[data-cfx-graph-editor-field='id']").FillAsync("packed-worker");
            await form.Locator("[data-cfx-graph-editor-field='label']").FillAsync("Packed worker");
            await form.Locator("button[type='submit']").ClickAsync();
            PackageAssertions.Require(await page.Locator("[data-node-id='packed-worker'][data-cfx-role='graph-node']").CountAsync() == 1,
                "Packed graph editor did not add the authored node.");
            await page.Locator("[data-cfx-graph-editor-close]").ClickAsync();
            await page.Locator("[data-cfx-graph-action='undo']").ClickAsync();
            PackageAssertions.Require(await page.Locator("[data-node-id='packed-worker'][data-cfx-role='graph-node']").CountAsync() == 0,
                "Packed graph editor undo did not restore the previous document.");
            await page.Locator("[data-cfx-graph-action='redo']").ClickAsync();
            PackageAssertions.Require(await page.Locator("[data-node-id='packed-worker'][data-cfx-role='graph-node']").CountAsync() == 1,
                "Packed graph editor redo lost the added node.");
            var physics = page.Locator("[data-cfx-graph-action='physics']");
            if (await physics.GetAttributeAsync("aria-pressed") == "true") await physics.ClickAsync();
            var positions = await page.EvaluateAsync<string>("()=>JSON.stringify(ChartForgeXGraphExplorer.positions(document.querySelector('.cfx-graph-explorer')))");
            await page.Locator("[data-cfx-graph-action='fit']").ClickAsync();
            PackageAssertions.Require(await page.EvaluateAsync<bool>("""
                async()=>{
                    await new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)));
                    const root=document.querySelector('.cfx-graph-explorer'),actual=root.dataset.cfxGraphRendererActive,
                        surface=root.querySelector('[data-cfx-role="graph-'+(actual==='svg'?'scene':actual)+'"]'),box=surface.getBoundingClientRect(),
                        view=root.querySelector('[data-cfx-role=graph-scene]').viewBox.baseVal,
                        camera=ChartForgeXGraphExplorer.captureState(root).viewport,
                        scale=Math.min(box.width/view.width,box.height/view.height),nodes=ChartForgeXGraphExplorer.positions(root);
                    return nodes.length===3&&nodes.every(node=>{
                        const x=box.left+(box.width-view.width*scale)/2+(node.x*camera.scale+camera.x)*scale,
                            y=box.top+(box.height-view.height*scale)/2+(node.y*camera.scale+camera.y)*scale,
                            hit=document.elementFromPoint(x,y);
                        return x>=box.left&&x<=box.right&&y>=box.top&&y<=box.bottom&&
                            (actual==='svg'?hit?.closest('[data-node-id]')?.dataset.nodeId===node.id:hit===surface);
                    });
                }
                """), "Packed graph Fit did not expose all three edited nodes at " + width + " pixels.");
            PackageAssertions.Require(positions == await page.EvaluateAsync<string>("()=>JSON.stringify(ChartForgeXGraphExplorer.positions(document.querySelector('.cfx-graph-explorer')))"),
                "Packed graph Fit changed the edited node positions.");
            await Capture(page, "packed-editor-" + width + "-" + engine);
            PackageAssertions.Require(errors.Count == 0 && !session.ConsoleLog.Any(entry => entry.Type == HtmlConsoleMessageType.Error),
                "Packed HTML runtime reported an error: " + string.Join("; ", errors));
        }
        Console.WriteLine("Packed HTML keyboard selection and graph editor add/undo/redo/Fit passed in " + engine + ".");
    }

    internal static async Task RunNativeChartAsync(string initial, string window, string edited, string reset) {
        if (Environment.GetEnvironmentVariable("HFX_TEST_ENABLE_PLAYWRIGHT") != "1") {
            Console.WriteLine("Packed native exploration browser execution not requested.");
            return;
        }
        foreach (var width in new[] { 390, 900 }) {
            await using var session = await HtmlBrowser.OpenSessionAsync("about:blank", new HtmlBrowserLaunchOptions {
                Browser = Engine(), Headless = true, ViewportWidth = width, ViewportHeight = 1000, Timeout = 15000
            });
            var page = session.Page; var errors = new System.Collections.Generic.List<string>();
            page.PageError += (_, error) => errors.Add(error);
            await page.SetContentAsync(initial);
            await page.EvaluateAsync("""
                ({window:windowHtml,edited,reset})=>{
                    window.depth=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));
                    window.depthChanges=[];window.depthErrors=[];window.depthRequests=[];
                    depth.root.addEventListener('cfxviewportchange',event=>depthChanges.push(event.detail));
                    depth.root.addEventListener('cfxannotationchange',event=>depthChanges.push(event.detail));
                    depth.root.addEventListener('cfxviewporterror',event=>depthErrors.push(event.detail));
                    depth.root.addEventListener('cfxannotationerror',event=>depthErrors.push(event.detail));
                    depth.connectNative(async(request,signal)=>{
                        if(!Object.isFrozen(request)||!(signal instanceof AbortSignal)||signal.aborted)
                            throw new Error('Packed managed preparation lost its immutable request or live abort signal');
                        depthRequests.push(request);
                        await Promise.resolve();
                        if(request.channel==='viewport'&&request.kind==='window'){
                            if(request.basis!=='initial'||request.sourceViewport.x.minimum!==2||request.sourceViewport.x.maximum!==8
                                ||request.sourceViewport.y.minimum!==20||request.sourceViewport.y.maximum!==80)
                                throw new Error('Unexpected packed managed source window or committed basis');
                            return {html:windowHtml,basis:'window'};
                        }
                        if(request.channel==='annotation'){
                            if(request.basis!=='window'||request.kind!=='replace'||request.annotationId!=='review'
                                ||request.sourceDrawing.start.x!==6||!Object.isFrozen(request.sourceDrawing.start))
                                throw new Error('Packed managed source command facts or committed basis changed');
                            return {html:edited,basis:'edited'};
                        }
                        if(request.channel==='viewport'&&request.kind==='reset'&&request.basis==='edited')
                            return {html:reset,basis:'reset'};
                        throw new Error('Unexpected packed managed preparation channel, operation or committed basis');
                    },{basis:'initial',axes:'xy'});
                    depth.select({targets:[depth.resolveTargets().find(target=>target.annotationId==='review')]});
                    depth.setViewport({x:{minimum:2,maximum:8},y:{minimum:20,maximum:80}});
                }
                """, new { window, edited, reset });
            await page.WaitForFunctionAsync("()=>depthChanges.length===1");
            PackageAssertions.Require(await page.EvaluateAsync<bool>("()=>depth.getXWindow().minimum===2&&depth.getYWindow().maximum===80&&depthRequests.length===1&&depthRequests[0].basis==='initial'"),
                "Packed browser did not commit both native source windows.");
            await page.EvaluateAsync("()=>depth.applyAnnotationChange({operation:'replace',id:'review',drawing:{kind:'Note',start:{x:6,y:45},label:'Reviewed'}})");
            await page.WaitForFunctionAsync("()=>depthChanges.length===2");
            PackageAssertions.Require(await page.EvaluateAsync<bool>("()=>depth.getSelection()[0]?.annotationId==='review'&&depth.getXWindow().minimum===2&&depth.getYWindow().minimum===20&&depthRequests.length===2&&depthRequests[1].basis==='window'"),
                "Packed native drawing edit lost the source window or identified selection.");
            await page.Locator("[data-cfx-annotation-id=review]").FocusAsync();
            await page.Keyboard.PressAsync("Shift+F10");
            var menu = page.Locator(".cfx-chart-context-menu");
            var download = await page.RunAndWaitForDownloadAsync(() => menu.Locator("[data-cfx-context-action=svg]").ClickAsync());
            var path = await download.PathAsync() ?? throw new InvalidOperationException("Packed native menu did not export SVG.");
            var svg = XDocument.Load(path);
            var note = svg.Descendants().Single(node => (string?)node.Attribute("data-cfx-annotation-id") == "review");
            PackageAssertions.Require((string?)note.Attribute("data-cfx-source-x") == "6" && (string?)note.Attribute("data-cfx-label") == "Reviewed",
                "Packed export did not contain the committed native source drawing.");
            await Capture(page, "packed-native-commands-" + width + "-net" + Environment.Version.Major + "-" + Engine());
            await page.Locator("[data-cfx-reset]").ClickAsync();
            await page.WaitForFunctionAsync("()=>depthChanges.length===3");
            PackageAssertions.Require(await page.EvaluateAsync<bool>("""
                ()=>depth.getXWindow().minimum===0&&depth.getYWindow().maximum===100&&depth.getSelection().length===0
                    &&depthRequests.length===3&&depthRequests[2].basis==='edited'
                    &&depthRequests.map(request=>request.channel).join(',')==='viewport,annotation,viewport'
                    &&new Set(depthRequests.map(request=>request.sceneRevision)).size===3
                    &&new Set(depthRequests.map(request=>request.requestId)).size===3
                """),
                "Packed reset did not restore both source domains and clear reader selection.");
            PackageAssertions.Require(errors.Count == 0 && await page.EvaluateAsync<bool>("()=>depthErrors.length===0")
                && !session.ConsoleLog.Any(entry => entry.Type == HtmlConsoleMessageType.Error),
                "Packed native exploration reported a runtime error: " + string.Join("; ", errors));
        }
        Console.WriteLine("Packed managed native connection advanced committed basis through XY windows, source commands, menu export and reset in " + Engine() + ".");
    }

    private static HtmlBrowserEngine Engine() => Environment.GetEnvironmentVariable("CFX_TEST_BROWSER")?.Trim().ToLowerInvariant() switch {
        null or "" or "chromium" => HtmlBrowserEngine.Chromium,
        "firefox" => HtmlBrowserEngine.Firefox,
        "webkit" => HtmlBrowserEngine.WebKit,
        _ => throw new InvalidOperationException("CFX_TEST_BROWSER must be chromium, firefox or webkit.")
    };

    private static async Task Capture(IPage page, string name) {
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(output)) return;
        if (!Path.IsPathRooted(output)) throw new InvalidOperationException("CFX_BROWSER_CAPTURE_DIRECTORY must be absolute.");
        Directory.CreateDirectory(output);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".png"), FullPage = true });
    }
}
