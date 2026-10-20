using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Protects readable small-scene fit and caption association with the actual painted route.</summary>
[Trait("BrowserSuite", "Compatibility")]
public sealed class GraphReadabilityBrowserTests {
    [BrowserTheory]
    [InlineData(HtmlGraphRenderBackend.Svg, false)]
    [InlineData(HtmlGraphRenderBackend.Canvas, true)]
    [InlineData(HtmlGraphRenderBackend.WebGl, false)]
    public async Task CompactFitUsesScreenSpaceAndResizePreservesTouchedCamera(HtmlGraphRenderBackend backend, bool dark) {
        var scene = GraphScene.Create("readable-fit", "Regional services")
            .AddNode("global", "Global", n => { Place(n, 480, 190); n.BadgeText = "42"; })
            .AddNode("europe", "Europe", n => { Place(n, 380, 280); n.SecondaryLabel = "4 sites · 18 workloads"; })
            .AddNode("americas", "Americas", n => Place(n, 580, 280))
            .AddNode("apac", "Asia Pacific", n => Place(n, 480, 370))
            .AddEdge("eu", "global", "europe")
            .AddEdge("us", "global", "americas")
            .AddEdge("ap", "global", "apac");
        scene.Options.Features = GraphSceneFeatures.Viewport | GraphSceneFeatures.Selection | GraphSceneFeatures.Export;
        await using var session = await OpenAsync(scene.ToGraphExplorerHtmlPage(o => {
            o.RenderBackend = backend; o.Theme = dark ? HtmlGraphExplorerTheme.Dark : HtmlGraphExplorerTheme.Light;
            o.PersistThemePreference = false;
        }), 390, 844);
        var page = session.Page;
        await page.WaitForFunctionAsync("()=>+document.querySelector('.cfx-graph-explorer').dataset.cfxGraphFitScale>0");
        var facts = await FitFacts(page);
        Assert.True(facts.GetProperty("minimumFont").GetDouble() >= 10.5, facts.GetRawText());
        Assert.True(facts.GetProperty("inside").GetBoolean(), facts.GetRawText());
        Assert.Equal(4, facts.GetProperty("nodes").GetArrayLength());
        await Capture(page, "compact-fit-" + backend, facts);
        var point = await page.Locator("[data-cfx-role=graph-node][data-node-id=europe]").EvaluateAsync<double[]>("node=>{const b=node.getBoundingClientRect();return [b.x+b.width/2,b.y+b.height/2];}");
        await page.Mouse.ClickAsync((float)point[0], (float)point[1]);
        Assert.Equal("true", await page.Locator("[data-cfx-role=graph-node][data-node-id=europe]").GetAttributeAsync("aria-pressed"));
        var exported = await page.EvaluateAsync<JsonElement>("""
            async()=>{const root=document.querySelector('.cfx-graph-explorer');let svg,png;
                root.addEventListener('cfxgraphexport',e=>{if(e.detail.format==='svg')svg=e.detail.content;else png=e.detail.content;e.preventDefault();});
                await ChartForgeXGraphExplorer.export(root,'svg');await ChartForgeXGraphExplorer.export(root,'png');
                const image=new Image();image.src=png;await image.decode();
                const visibleSvg=svg;root.style.display='none';
                try {await ChartForgeXGraphExplorer.export(root,'svg');await ChartForgeXGraphExplorer.export(root,'png');} finally {root.style.display='';}
                const hiddenImage=new Image();hiddenImage.src=png;await hiddenImage.decode();
                const hiddenSvg=new DOMParser().parseFromString(svg,'image/svg+xml').documentElement;
                const host=document.createElement('section');host.id='fit-snapshot';host.append(document.importNode(new DOMParser().parseFromString(visibleSvg,'image/svg+xml').documentElement,true));
                document.body.append(host);const frame=host.querySelector('svg'),view=frame.viewBox.baseVal;
                return {svgWidth:+frame.getAttribute('width'),svgHeight:+frame.getAttribute('height'),pngWidth:image.naturalWidth,pngHeight:image.naturalHeight,hiddenWidth:hiddenImage.naturalWidth,hiddenHeight:hiddenImage.naturalHeight,
                    hiddenSvgWidth:+hiddenSvg.getAttribute('width'),hiddenSvgHeight:+hiddenSvg.getAttribute('height'),aspect:view.width/view.height};}
            """);
        Assert.True(exported.GetProperty("svgHeight").GetDouble() > exported.GetProperty("svgWidth").GetDouble(), exported.GetRawText());
        Assert.Equal(exported.GetProperty("svgWidth").GetDouble() / exported.GetProperty("svgHeight").GetDouble(), exported.GetProperty("aspect").GetDouble(), 3);
        Assert.Equal(exported.GetProperty("svgWidth").GetDouble(), exported.GetProperty("hiddenSvgWidth").GetDouble());
        Assert.Equal(exported.GetProperty("svgHeight").GetDouble(), exported.GetProperty("hiddenSvgHeight").GetDouble());
        Assert.Equal(exported.GetProperty("pngWidth").GetInt32(), exported.GetProperty("hiddenWidth").GetInt32());
        Assert.Equal(exported.GetProperty("pngHeight").GetInt32(), exported.GetProperty("hiddenHeight").GetInt32());
        var snapshot = await FitFacts(page, "#fit-snapshot");
        Assert.True(snapshot.GetProperty("inside").GetBoolean(), snapshot.GetRawText());
        Assert.True(snapshot.GetProperty("minimumFont").GetDouble() >= 10.5, snapshot.GetRawText());
        await Capture(page, "compact-fit-export-" + backend, exported);
        await page.EvaluateAsync("()=>document.getElementById('fit-snapshot').remove()");
        await page.GetByRole(AriaRole.Button, new() { Name = "Zoom in", Exact = true }).ClickAsync();
        var touched = await FitFacts(page);
        await page.SetViewportSizeAsync(960, 720);
        await page.WaitForFunctionAsync("()=>document.querySelector('.cfx-graph-stage').getBoundingClientRect().width>700");
        var resized = await FitFacts(page);
        Assert.Equal(touched.GetProperty("camera").GetRawText(), resized.GetProperty("camera").GetRawText());
        Assert.Equal(touched.GetProperty("nodes").GetRawText(), resized.GetProperty("nodes").GetRawText());
        await page.GetByRole(AriaRole.Button, new() { Name = "Fit graph to view", Exact = true }).ClickAsync();
        var fitted = await FitFacts(page);
        Assert.True(fitted.GetProperty("inside").GetBoolean(), fitted.GetRawText());
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(HtmlGraphRenderBackend.Svg, false)]
    [InlineData(HtmlGraphRenderBackend.Svg, true)]
    [InlineData(HtmlGraphRenderBackend.Canvas, false)]
    [InlineData(HtmlGraphRenderBackend.Canvas, true)]
    [InlineData(HtmlGraphRenderBackend.WebGl, false)]
    [InlineData(HtmlGraphRenderBackend.WebGl, true)]
    public async Task FitMeasuresUntruncatedPrimaryAndSecondaryCaptionsInPhysicalAndVirtualScenes(HtmlGraphRenderBackend backend, bool secondary) {
        const string caption = "north-europe-production-authentication-service.example.internal — retained complete hostname and service description";
        var scene = GraphScene.Create("long-fit", "Complete service captions").AddNode("service", secondary ? "Worker" : caption, n => {
            Place(n, 480, 280); if (secondary) n.SecondaryLabel = caption;
        });
        scene.Options.Features = GraphSceneFeatures.Viewport | GraphSceneFeatures.Export;
        if (backend != HtmlGraphRenderBackend.Svg) {
            scene.Options.Features |= GraphSceneFeatures.LevelOfDetail;
            scene.Options.LevelOfDetail.CanvasPreferredNodeThreshold = 1;
            scene.Options.LevelOfDetail.OverviewScaleThreshold = .01;
            scene.Options.LevelOfDetail.DetailScaleThreshold = .02;
        }
        var errors = new List<string>();
        await using var session = await OpenAsync(scene.ToGraphExplorerHtmlPage(o => o.RenderBackend = backend), 390, 844,
            configurePage: page => page.PageError += (_, error) => errors.Add(error));
        var page = session.Page;
        var snapshot = await page.EvaluateAsync<string>("""
            async()=>{const root=document.querySelector('.cfx-graph-explorer');let content;
                root.addEventListener('cfxgraphexport',e=>{content=e.detail.content;e.preventDefault();});
                await ChartForgeXGraphExplorer.export(root,'svg');return content;}
            """);
        Assert.Contains(caption, snapshot, StringComparison.Ordinal);
        await page.EvaluateAsync("""
            source=>{const host=document.createElement('section');host.id='long-snapshot';host.append(document.importNode(new DOMParser().parseFromString(source,'image/svg+xml').documentElement,true));document.body.append(host);}
            """, snapshot);
        var facts = await FitFacts(page, "#long-snapshot");
        await Capture(page, "long-fit-" + backend + "-" + secondary, facts);
        Assert.True(facts.GetProperty("inside").GetBoolean(), facts.GetRawText());
        Assert.Empty(errors);
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NativeStaticFitIncludesTheSameCompletePrimaryAndSecondaryCaptions(bool secondary) {
        const string caption = "north-europe-production-authentication-service.example.internal — retained complete hostname and service description";
        var scene = GraphScene.Create("native-long-fit", "Complete service captions").AddNode("service", secondary ? "Worker" : caption, n => {
            Place(n, 480, 280); if (secondary) n.SecondaryLabel = caption;
        });
        void Options(GraphSceneStaticRenderOptions options) { options.Width = 390; options.Height = 280; }
        var svg = scene.ToGraphSvg(configure: Options);
        var png = scene.ToGraphPng(configure: Options);
        await using var session = await OpenAsync("<html><body style='margin:0'><section id='native-long-fit'>" + svg +
            "</section><img alt='Native PNG' width='390' height='280' src='data:image/png;base64," + Convert.ToBase64String(png) + "'></body></html>", 430, 700);
        var facts = await FitFacts(session.Page, "#native-long-fit");
        Assert.True(facts.GetProperty("inside").GetBoolean(), facts.GetRawText());
        await Capture(session.Page, "native-long-fit-" + secondary, facts);
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task TallCompactFitKeepsPaintClearOfFloatingControls() {
        var scene = GraphScene.Create("tall-fit", "Service chain");
        for (var i = 0; i < 5; i++) scene.AddNode("node-" + i, "Service " + i, n => Place(n, 480, 100 + i * 100));
        for (var i = 0; i < 4; i++) scene.AddEdge("edge-" + i, "node-" + i, "node-" + (i + 1));
        scene.Options.Features = GraphSceneFeatures.Viewport;
        await using var session = await OpenAsync(scene.ToGraphExplorerHtmlPage(), 390, 844);
        var overlaps = await session.Page.EvaluateAsync<string[]>("""
            ()=>{const root=document.querySelector('.cfx-graph-explorer'),controls=root.querySelector('.cfx-graph-command-rail').getBoundingClientRect();
                return [...root.querySelectorAll('[data-cfx-role=graph-node],.cfx-graph-node-label')].filter(n=>{const b=n.getBoundingClientRect();return Math.min(b.right,controls.right)>Math.max(b.left,controls.left)&&Math.min(b.bottom,controls.bottom)>Math.max(b.top,controls.top);}).map(n=>n.textContent||n.dataset.nodeId);}
            """);
        Assert.Empty(overlaps);
        await Capture(session.Page, "compact-tall-fit", await FitFacts(session.Page));
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PreparedAndLiveCaptionsUseClearSpaceAlongTheirRoute(bool curved) {
        var scene = GraphScene.Create("near-route", "Route with a middle obstruction")
            .AddNode("source", "Source", n => Place(n, 100, 280))
            .AddNode("target", "Target", n => Place(n, 860, 280))
            .AddNode("middle", "Unrelated service", n => { Place(n, 480, curved ? 410 : 280); n.Shape = GraphNodeShape.Box; n.Size = 65; })
            .AddEdge("transfer", "source", "target", "replication", e => { e.Curvature = curved ? 260 : 0; });
        scene.Options.Features = GraphSceneFeatures.Viewport | GraphSceneFeatures.Export;
        var svg = scene.ToGraphSvg();
        var png = scene.ToGraphPng();
        await using (var prepared = await OpenAsync("<html><body><section id='proof'>" + svg + "</section><img width='960' alt='Native PNG' src='data:image/png;base64," + Convert.ToBase64String(png) + "'></body></html>", 1000, 1200)) {
            var facts = await CaptionFacts(prepared.Page, "#proof");
            AssertCaption(facts);
            if (curved) Assert.True(facts.GetProperty("bend").GetDouble() > 100, facts.GetRawText());
            await Capture(prepared.Page, "near-route-prepared-" + curved, facts);
            AssertNoConsoleErrors(prepared);
        }
        await using var session = await OpenAsync(scene.ToGraphExplorerHtmlPage(o => o.PersistThemePreference = false), 980, 760);
        var live = await CaptionFacts(session.Page, ".cfx-graph-explorer");
        AssertCaption(live);
        await Capture(session.Page, "near-route-live-" + curved, live);
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(HtmlGraphRenderBackend.Svg, false)]
    [InlineData(HtmlGraphRenderBackend.Canvas, false)]
    [InlineData(HtmlGraphRenderBackend.WebGl, false)]
    [InlineData(HtmlGraphRenderBackend.Svg, true)]
    [InlineData(HtmlGraphRenderBackend.Canvas, true)]
    [InlineData(HtmlGraphRenderBackend.WebGl, true)]
    public async Task ShortRelationshipUsesEmptySpaceBetweenPaintedNodeDetails(HtmlGraphRenderBackend backend, bool moved) {
        var scene = GraphScene.Create("short-caption", "Regional services")
            .AddNode("global", "Global", n => { Place(n, 479.47, 190.73); n.BadgeText = "42"; })
            .AddNode("europe", "Europe", n => { Place(n, 606.12, 267.10); n.SecondaryLabel = "4 sites · 18 workloads"; n.Status = "warning"; })
            .AddNode("americas", "Americas", n => { Place(n, 341.25, 266.80); n.SecondaryLabel = "3 sites · 14 workloads"; })
            .AddNode("apac", "Asia Pacific", n => { Place(n, 493.57, 277.66); n.SecondaryLabel = "2 sites · 10 workloads"; })
            .AddEdge("eu", "global", "europe", configure: e => e.Directed = true)
            .AddEdge("us", "global", "americas", configure: e => e.Directed = true)
            .AddEdge("ap", "global", "apac", configure: e => e.Directed = true)
            .AddEdge("transfer", "europe", "apac", "replication");
        if (moved) {
            var positions = new[] { (472.929, 222.855), (566.219, 249.577), (373.954, 248.593), (508.383, 286.183) };
            for (var i = 0; i < positions.Length; i++) { scene.Nodes[i].X = positions[i].Item1; scene.Nodes[i].Y = positions[i].Item2; }
        }
        // The moved short edge needs one caption row of normal clearance; the
        // original clear-gap scene keeps the stricter near-route bound.
        var maximumDistance = moved ? 55 : 35;
        scene.Options.Features = GraphSceneFeatures.Viewport | GraphSceneFeatures.Export;
        if (backend == HtmlGraphRenderBackend.Svg) {
            await using var prepared = await OpenAsync("<html><body><section id='proof'>" + scene.ToGraphSvg() + "</section><img alt='Native PNG' src='data:image/png;base64," + Convert.ToBase64String(scene.ToGraphPng()) + "'></body></html>", 1000, 1200);
            var native = await CaptionFacts(prepared.Page, "#proof");
            await Capture(prepared.Page, "short-caption-native-" + moved, native);
            AssertCaption(native, maximumDistance);
        }
        await using var session = await OpenAsync(scene.ToGraphExplorerHtmlPage(o => o.RenderBackend = backend), 390, 844);
        var live = await CaptionFacts(session.Page, ".cfx-graph-explorer");
        await Capture(session.Page, "short-caption-live-" + backend + "-" + moved, live);
        AssertCaption(live, maximumDistance);
        await session.Page.EmulateMediaAsync(new() { Media = Media.Print });
        await session.Page.WaitForFunctionAsync("()=>document.querySelector('[data-cfx-role=graph-print-surface] svg')");
        var printed = await CaptionFacts(session.Page, "[data-cfx-role=graph-print-surface]");
        await Capture(session.Page, "short-caption-print-" + backend + "-" + moved, printed);
        AssertCaption(printed, maximumDistance);
        await session.Page.EmulateMediaAsync(new() { Media = Media.Screen });
        AssertNoConsoleErrors(session);
    }

    private static void Place(GraphSceneNode node, double x, double y) { node.Fixed = true; node.X = x; node.Y = y; node.Size = 8; }

    private static Task<JsonElement> FitFacts(IPage page, string selector = ".cfx-graph-explorer") => page.EvaluateAsync<JsonElement>("""
        selector=>{const root=document.querySelector(selector),svg=root.querySelector('[data-cfx-role=graph-scene]')||root.querySelector('svg'),frame=svg.getBoundingClientRect(),
            labels=[...svg.querySelectorAll('.cfx-graph-node-label')].filter(n=>n.getClientRects().length&&getComputedStyle(n).display!=='none');
            return {frame:frame.toJSON(),captions:[...svg.querySelectorAll('text')].map(n=>({text:n.textContent,font:getComputedStyle(n).font,box:n.getBBox(),screen:n.getBoundingClientRect().toJSON()})),minimumFont:Math.min(...labels.map(n=>parseFloat(getComputedStyle(n).fontSize)*Math.hypot(n.getScreenCTM().a,n.getScreenCTM().b))),
                inside:[...svg.querySelectorAll('text')].filter(n=>n.getClientRects().length&&getComputedStyle(n).display!=='none').every(n=>{const b=n.getBoundingClientRect();return b.left>=frame.left&&b.right<=frame.right&&b.top>=frame.top&&b.bottom<=frame.bottom;}),
                camera:[root.dataset.cfxViewportX,root.dataset.cfxViewportY,root.dataset.cfxViewportScale],
                nodes:[...svg.querySelectorAll('[data-cfx-role=graph-node]')].map(n=>[n.dataset.nodeId,n.dataset.nodeX,n.dataset.nodeY])};}
        """, selector);

    private static Task<JsonElement> CaptionFacts(IPage page, string selector) => page.EvaluateAsync<JsonElement>("""
        selector=>{const root=document.querySelector(selector),label=root.querySelector('[data-edge-label-for=transfer]'),path=root.querySelector('[data-edge-id=transfer]'),
            point={x:+label.getAttribute('x'),y:+label.getAttribute('y')},length=path.getTotalLength();let distance=Infinity;
            for(let i=0;i<=1000;i++){const p=path.getPointAtLength(length*i/1000);distance=Math.min(distance,Math.hypot(p.x-point.x,p.y-(point.y-4)));}
            const box=label.getBoundingClientRect(),collisions=[...root.querySelectorAll('[data-cfx-role=graph-node],.cfx-graph-node-label,.cfx-graph-node-secondary,.cfx-graph-node-badge')].filter(n=>{const b=n.getBoundingClientRect();return Math.min(b.right,box.right)>Math.max(b.left,box.left)&&Math.min(b.bottom,box.bottom)>Math.max(b.top,box.top);}).map(n=>n.textContent||n.dataset.nodeId);
            return {distance,collisions,text:label.textContent,point,bend:Math.abs(path.getPointAtLength(length/2).y-280)};}
        """, selector);

    private static void AssertCaption(JsonElement facts, double maximumDistance = 35) {
        Assert.Equal("replication", facts.GetProperty("text").GetString());
        Assert.True(facts.GetProperty("distance").GetDouble() <= maximumDistance, facts.GetRawText());
        Assert.Empty(facts.GetProperty("collisions").EnumerateArray());
    }

    private static async Task Capture(IPage page, string name, JsonElement facts) {
        var folder = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(folder)) return;
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, name + ".json"), facts.GetRawText());
        await page.ScreenshotAsync(new() { Path = Path.Combine(folder, name + ".png"), FullPage = true });
    }
}
