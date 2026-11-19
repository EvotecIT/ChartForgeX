using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Protects rich card nodes and the initial layout progress bar in every explorer backend.</summary>
[Trait("BrowserSuite", "Compatibility")]
public sealed class GraphCardNodeBrowserTests {
    // The runtime script also names the role in a selector, so match the element's own attribute run.
    private const string Markup = "data-cfx-role=\"graph-layout-progress\" role=\"progressbar\"";
    // Records layout progress events from before the explorer mounts, so the first report is captured too.
    private const string Recorder = "<script>window.__layoutProgress=[];document.addEventListener('cfxgraphlayoutprogress',e=>window.__layoutProgress.push({progress:e.detail.progress,done:e.detail.done,"
        + "visible:e.target.querySelector?.('[data-cfx-role=graph-layout-progress]')?.hidden===false}),true);</script>";
    private const string Avatar = "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='40' height='20'%3E%3Crect width='40' height='20' fill='%232563eb'/%3E%3C/svg%3E";

    [BrowserTheory]
    [InlineData(HtmlGraphRenderBackend.Svg)]
    [InlineData(HtmlGraphRenderBackend.Canvas)]
    [InlineData(HtmlGraphRenderBackend.WebGl)]
    public async Task CardNodesShowImageTitleSubtitleAndBadge(HtmlGraphRenderBackend backend) {
        var scene = CardScene();
        await using var session = await OpenAsync(scene.ToGraphExplorerHtmlPage(o => { o.RenderBackend = backend; o.PersistThemePreference = false; }), 960, 640);
        var page = session.Page;
        await page.WaitForFunctionAsync("()=>+document.querySelector('.cfx-graph-explorer').dataset.cfxGraphFitScale>0");

        var node = await page.EvaluateAsync<JsonElement>("""
            ()=>{const node=document.querySelector('[data-cfx-role=graph-node][data-node-id=dc1]');
                return {card:node.dataset.nodeCard,shape:node.dataset.nodeShape,size:+node.dataset.nodeSize,image:node.dataset.nodeImageUrl||''};}
            """);
        Assert.Equal("true", node.GetProperty("card").GetString());
        Assert.Equal("box", node.GetProperty("shape").GetString());
        Assert.True(node.GetProperty("size").GetDouble() >= 62, node.GetRawText());
        Assert.StartsWith("data:image/svg+xml", node.GetProperty("image").GetString(), StringComparison.Ordinal);

        var exported = await page.EvaluateAsync<JsonElement>("""
            async()=>{const root=document.querySelector('.cfx-graph-explorer');let svg;
                root.addEventListener('cfxgraphexport',e=>{svg=e.detail.content;e.preventDefault();},{once:true});
                await ChartForgeXGraphExplorer.export(root,'svg');
                const doc=new DOMParser().parseFromString(svg,'image/svg+xml');
                const texts=[...doc.querySelectorAll('text')].map(t=>t.textContent);
                const images=[...doc.querySelectorAll('image.cfx-graph-node-card-image')];
                const icons=[...doc.querySelectorAll('.cfx-graph-node-card-icon')].map(t=>t.textContent);
                return {images:images.length,ratio:images[0]?.getAttribute('preserveAspectRatio')||'',alt:images[0]?.getAttribute('aria-label')||'',
                    width:+(images[0]?.getAttribute('width')||0),title:texts.includes('DC01'),subtitle:texts.includes('Warsaw · 4 roles'),badge:texts.includes('3'),icons};}
            """);
        Assert.Equal(1, exported.GetProperty("images").GetInt32());
        Assert.Equal("xMidYMid meet", exported.GetProperty("ratio").GetString());
        Assert.Equal("Domain controller", exported.GetProperty("alt").GetString());
        Assert.Equal(32, exported.GetProperty("width").GetDouble());
        Assert.True(exported.GetProperty("title").GetBoolean(), exported.GetRawText());
        Assert.True(exported.GetProperty("subtitle").GetBoolean(), exported.GetRawText());
        Assert.True(exported.GetProperty("badge").GetBoolean(), exported.GetRawText());
        // The icon-only card keeps its icon; the image card does not draw its icon over the image.
        Assert.Equal(new[] { "S" }, exported.GetProperty("icons").EnumerateArray().Select(item => item.GetString()));

        // The PNG export paints the card image (it is preloaded like other node images), and SVG never clips it to a circle.
        var png = await page.EvaluateAsync<JsonElement>("""
            async()=>{const root=document.querySelector('.cfx-graph-explorer');let svg,png;
                root.addEventListener('cfxgraphexport',e=>{if(e.detail.format==='svg')svg=e.detail.content;else png=e.detail.content;e.preventDefault();});
                await ChartForgeXGraphExplorer.export(root,'png');await ChartForgeXGraphExplorer.export(root,'svg');
                const host=document.createElement('div');host.style.cssText='position:absolute;left:0;top:0';
                host.append(document.importNode(new DOMParser().parseFromString(svg,'image/svg+xml').documentElement,true));document.body.append(host);
                const frame=host.querySelector('svg').getBoundingClientRect(),slot=host.querySelector('image.cfx-graph-node-card-image').getBoundingClientRect();
                const fx=(slot.left+slot.width/2-frame.left)/frame.width,fy=(slot.top+slot.height/2-frame.top)/frame.height;host.remove();
                const image=new Image();image.src=png;await image.decode();
                const canvas=document.createElement('canvas');canvas.width=image.naturalWidth;canvas.height=image.naturalHeight;
                const context=canvas.getContext('2d');context.drawImage(image,0,0);
                const pixel=context.getImageData(Math.round(fx*canvas.width),Math.round(fy*canvas.height),1,1).data;
                const live=document.querySelector('.cfx-graph-node-card-image');
                return {r:pixel[0],g:pixel[1],b:pixel[2],clip:live?getComputedStyle(live).clipPath:'none'};}
            """);
        Assert.True(png.GetProperty("b").GetInt32() > 200 && png.GetProperty("r").GetInt32() < 80, png.GetRawText());
        Assert.Equal("none", png.GetProperty("clip").GetString());

        // Cards added by a patch keep the same minimum size and image slot as server-rendered cards.
        var patched = await page.EvaluateAsync<JsonElement>($$"""
            ()=>{const root=document.querySelector('.cfx-graph-explorer');
                ChartForgeXGraphExplorer.update(root,{upsertNodes:[{id:'dc3',label:'DC03',shape:'box',card:true,imageUrl:"{{Avatar}}",size:8,x:300,y:440}]});
                const node=root.querySelector('[data-cfx-role=graph-node][data-node-id=dc3]');
                ChartForgeXGraphExplorer.update(root,{upsertNodes:[{id:'topo',label:'Topology',shape:'box',metadata:{'topology.card':'true'},size:40,x:620,y:440}]});
                ChartForgeXGraphExplorer.update(root,{upsertNodes:[{id:'topo',label:'Topology',shape:'box',size:30,x:620,y:440}]});
                ChartForgeXGraphExplorer.update(root,{upsertNodes:[{id:'topo',label:'Topology',shape:'box',size:36,x:620,y:440}]});
                const topology=root.querySelector('[data-cfx-role=graph-node][data-node-id=topo]');
                return {size:+node.dataset.nodeSize,card:node.dataset.nodeCard,images:node.querySelectorAll('image.cfx-graph-node-card-image').length,
                    topologySize:+topology.dataset.nodeSize,topologyCard:topology.dataset.nodeCard};}
            """);
        Assert.Equal(62, patched.GetProperty("size").GetDouble());
        Assert.Equal("true", patched.GetProperty("card").GetString());
        // Topology cards keep their own sizes, even when a later patch omits their metadata.
        Assert.Equal("true", patched.GetProperty("topologyCard").GetString());
        Assert.Equal(36, patched.GetProperty("topologySize").GetDouble());
        if (backend == HtmlGraphRenderBackend.Svg) Assert.Equal(1, patched.GetProperty("images").GetInt32());
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task DraggingDuringTheFirstLayoutEndsItsProgressReport() {
        var scene = RingScene(180);
        // A budget the layout cannot reach and a negligible velocity threshold keep it running until the drag.
        scene.Options.Physics.Stabilization.Iterations = 1_000_000;
        scene.Options.Physics.MinVelocity = 1e-12;
        // A pinned node stays under the pointer while the rest of the layout moves; pinned nodes can still be dragged.
        scene.Nodes.Single(node => node.Id == "n0").Fixed = true;
        var html = scene.ToGraphExplorerHtmlPage(o => { o.RenderBackend = HtmlGraphRenderBackend.Svg; o.PersistThemePreference = false; });
        var head = html.IndexOf("<head>", StringComparison.OrdinalIgnoreCase);
        await using var session = await OpenAsync(html.Insert(head + "<head>".Length, Recorder), 960, 640);
        var page = session.Page;
        await page.WaitForFunctionAsync("()=>window.__layoutProgress.some(item=>item.progress===0)&&document.querySelector('.cfx-graph-explorer').dataset.cfxGraphLayoutProgress==='running'");
        var point = await page.Locator("[data-cfx-role=graph-node][data-node-id=n0]").EvaluateAsync<double[]>("node=>{const b=node.getBoundingClientRect();return [b.x+b.width/2,b.y+b.height/2];}");
        await page.Mouse.MoveAsync((float)point[0], (float)point[1]);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync((float)point[0] + 40, (float)point[1] + 30, new() { Steps = 4 });
        await page.Mouse.UpAsync();
        await page.WaitForFunctionAsync("()=>document.querySelector('.cfx-graph-explorer').dataset.cfxGraphLayoutProgress==='done'");
        var events = (await page.EvaluateAsync<JsonElement>("()=>window.__layoutProgress")).EnumerateArray().ToArray();
        var progress = events.Select(item => item.GetProperty("progress").GetDouble()).ToArray();
        Assert.Equal(progress.OrderBy(value => value), progress);
        Assert.Single(events, item => item.GetProperty("done").GetBoolean());
        Assert.True(events[^1].GetProperty("done").GetBoolean());
        Assert.True(await page.Locator("[data-cfx-role=graph-layout-progress]").EvaluateAsync<bool>("bar=>bar.hidden"));
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(HtmlGraphRenderBackend.Svg)]
    [InlineData(HtmlGraphRenderBackend.WebGl)]
    public async Task InitialLayoutReportsProgressAndHidesWhenSettled(HtmlGraphRenderBackend backend) {
        var scene = RingScene(180);
        var html = scene.ToGraphExplorerHtmlPage(o => { o.RenderBackend = backend; o.PersistThemePreference = false; });
        var head = html.IndexOf("<head>", StringComparison.OrdinalIgnoreCase);
        Assert.True(head >= 0);
        await using var session = await OpenAsync(html.Insert(head + "<head>".Length, Recorder), 960, 640);
        var page = session.Page;
        await page.WaitForFunctionAsync("()=>document.querySelector('.cfx-graph-explorer').dataset.cfxGraphLayoutProgress==='done'");

        var facts = await page.EvaluateAsync<JsonElement>("""
            ()=>{const bar=document.querySelector('[data-cfx-role=graph-layout-progress]');
                return {events:window.__layoutProgress,hidden:bar.hidden,now:bar.getAttribute('aria-valuenow'),role:bar.getAttribute('role'),
                    label:bar.getAttribute('aria-label'),physics:document.querySelector('.cfx-graph-explorer').dataset.cfxGraphPhysicsState};}
            """);
        var events = facts.GetProperty("events").EnumerateArray().ToArray();
        Assert.True(events.Length >= 2, facts.GetRawText());
        Assert.False(events[0].GetProperty("done").GetBoolean());
        Assert.Equal(0, events[0].GetProperty("progress").GetDouble());
        Assert.True(events[0].GetProperty("visible").GetBoolean(), facts.GetRawText());
        var progress = events.Select(item => item.GetProperty("progress").GetDouble()).ToArray();
        Assert.Equal(progress.OrderBy(value => value), progress);
        Assert.All(events[..^1], item => Assert.True(item.GetProperty("progress").GetDouble() < 1));
        Assert.True(events[^1].GetProperty("done").GetBoolean());
        Assert.Equal(1, events[^1].GetProperty("progress").GetDouble());
        Assert.Single(events, item => item.GetProperty("done").GetBoolean());
        Assert.True(facts.GetProperty("hidden").GetBoolean(), facts.GetRawText());
        Assert.Equal("100", facts.GetProperty("now").GetString());
        Assert.Equal("progressbar", facts.GetProperty("role").GetString());
        Assert.Equal("Arranging graph", facts.GetProperty("label").GetString());
        Assert.Equal("stabilized", facts.GetProperty("physics").GetString());

        // A later reheat must not bring the overlay back.
        await page.EvaluateAsync("()=>{const root=document.querySelector('.cfx-graph-explorer');window.__layoutProgress=[];ChartForgeXGraphExplorer.update(root,{upsertNodes:[{id:'extra',label:'Extra'}]});}");
        Assert.Equal("graph-patch", await page.Locator(".cfx-graph-explorer").EvaluateAsync<string>("root=>root.dataset.cfxGraphPhysicsReason"));
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>window.__layoutProgress.length"));
        Assert.True(await page.Locator("[data-cfx-role=graph-layout-progress]").EvaluateAsync<bool>("bar=>bar.hidden"));
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task DisposingDuringTheFirstLayoutEndsItsProgressReport() {
        var scene = RingScene(180);
        scene.Options.Physics.Stabilization.Iterations = 1_000_000;
        scene.Options.Physics.MinVelocity = 1e-12;
        var html = scene.ToGraphExplorerHtmlPage(o => { o.RenderBackend = HtmlGraphRenderBackend.Svg; o.PersistThemePreference = false; });
        var head = html.IndexOf("<head>", StringComparison.OrdinalIgnoreCase);
        await using var session = await OpenAsync(html.Insert(head + "<head>".Length, Recorder), 960, 640);
        var page = session.Page;
        await page.WaitForFunctionAsync("()=>document.querySelector('.cfx-graph-explorer').dataset.cfxGraphLayoutProgress==='running'");

        var facts = await page.EvaluateAsync<JsonElement>("""
            ()=>{const root=document.querySelector('.cfx-graph-explorer'),parent=root.parentNode,next=root.nextSibling;
                const doneEvents=[];document.addEventListener('cfxgraphlayoutprogress',e=>{if(e.detail.done)doneEvents.push(e.detail.graphId);});
                // Remove the root first: its completion must still reach document listeners.
                root.remove();ChartForgeXGraphExplorer.dispose(root);parent.insertBefore(root,next);
                return {state:root.dataset.cfxGraphLayoutProgress||'',hidden:root.querySelector('[data-cfx-role=graph-layout-progress]').hidden,doneEvents:doneEvents.length};}
            """);
        Assert.Equal("", facts.GetProperty("state").GetString());
        Assert.Equal(1, facts.GetProperty("doneEvents").GetInt32());
        Assert.True(facts.GetProperty("hidden").GetBoolean(), facts.GetRawText());
        // A remount of the same element reports its own first layout again.
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.mount(document.querySelector('.cfx-graph-explorer'))");
        await page.WaitForFunctionAsync("()=>document.querySelector('.cfx-graph-explorer').dataset.cfxGraphLayoutProgress==='running'");
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public void ProgressMarkupFollowsOptionsAndGraphSize() {
        Assert.DoesNotContain(Markup, RingScene(40).ToGraphExplorerHtmlPage(), StringComparison.Ordinal);
        Assert.Contains(Markup, RingScene(40).ToGraphExplorerHtmlPage(o => o.LayoutProgressMinimumNodes = 10), StringComparison.Ordinal);
        Assert.DoesNotContain(Markup, RingScene(180).ToGraphExplorerHtmlPage(o => o.IncludeLayoutProgress = false), StringComparison.Ordinal);
        var withoutPhysics = RingScene(180);
        withoutPhysics.Options.Disable(GraphSceneFeatures.RuntimePhysics);
        Assert.DoesNotContain(Markup, withoutPhysics.ToGraphExplorerHtmlPage(), StringComparison.Ordinal);
        var html = RingScene(180).ToGraphExplorerHtmlPage(o => o.Localization[HtmlChartText.GraphLayoutProgress] = "Układanie grafu");
        Assert.Contains("aria-label=\"Układanie grafu\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void CardShapeRendersAsAnAccessibleBoxCardWithItsImage() {
        var html = CardScene().ToGraphExplorerHtmlPage();
        Assert.Contains("data-node-card=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"cfx-graph-node-card-image\"", html, StringComparison.Ordinal);
        Assert.Contains("cfx-graph-node-card-label", html, StringComparison.Ordinal);
        Assert.Contains("cfx-graph-node-card-secondary", html, StringComparison.Ordinal);
        Assert.Contains("data-node-size=\"62\"", html, StringComparison.Ordinal);
    }

    private static GraphScene CardScene() {
        var scene = GraphScene.Create("cards", "Directory estate")
            .AddNode("dc1", "DC01", node => {
                node.Shape = GraphNodeShape.Card; node.ImageUrl = Avatar; node.ImageAlt = "Domain controller";
                node.SecondaryLabel = "Warsaw · 4 roles"; node.BadgeText = "3"; node.Status = "warning"; node.IconText = "D";
                node.X = 300; node.Y = 280;
            })
            .AddNode("site", "Warsaw", node => { node.Shape = GraphNodeShape.Card; node.IconText = "S"; node.SecondaryLabel = "Site"; node.X = 620; node.Y = 280; })
            .AddEdge("link", "dc1", "site");
        scene.Options.Features = GraphSceneFeatures.Viewport | GraphSceneFeatures.Selection | GraphSceneFeatures.Export | GraphSceneFeatures.IncrementalUpdates;
        return scene;
    }

    private static GraphScene RingScene(int count) {
        var scene = GraphScene.Create("layout-progress", "Layout progress");
        for (var i = 0; i < count; i++) scene.AddNode("n" + i, "Node " + i);
        for (var i = 0; i < count; i++) scene.AddEdge("e" + i, "n" + i, "n" + ((i + 1) % count));
        scene.Options.Enable(GraphSceneFeatures.RuntimePhysics | GraphSceneFeatures.Stabilization | GraphSceneFeatures.IncrementalUpdates);
        scene.Options.Physics.Solver = GraphPhysicsSolver.BarnesHut;
        scene.Options.Physics.Stabilization.Iterations = 120;
        return scene;
    }
}
