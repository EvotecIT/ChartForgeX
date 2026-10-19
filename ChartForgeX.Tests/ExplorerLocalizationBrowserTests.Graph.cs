using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class ExplorerLocalizationBrowserTests {
    [BrowserTheory]
    [InlineData(HtmlGraphRenderBackend.Svg, 390, false)]
    [InlineData(HtmlGraphRenderBackend.Canvas, 960, true)]
    [InlineData(HtmlGraphRenderBackend.WebGl, 960, false)]
    public async Task GraphMessagesFollowNativeInputPatchExportAndRemountWithoutTranslatingFacts(HtmlGraphRenderBackend backend, int width, bool external) {
        if (!Enabled) return;
        var graph = GraphScene.Create("translated-graph", "Authored title")
            .AddNode("a", "Source {target}", node => { node.Fixed = true; node.X = 200; node.Y = 170; node.Status = "Healthy"; node.Size = 24; })
            .AddNode("b", "Authored destination", node => { node.Fixed = true; node.X = 500; node.Y = 170; node.Size = 24; })
            .AddEdge("flow", "a", "b", "Authored relationship");
        graph.Options.Enable(GraphSceneFeatures.Manipulation | GraphSceneFeatures.IncrementalUpdates);
        graph.Options.Manipulation.EnableEditing();
        var html = graph.ToGraphExplorerHtmlPage(options => {
            options.RenderBackend = backend; options.Theme = HtmlGraphExplorerTheme.Light;
            options.PersistThemePreference = false; Arabic(options.Localization);
            if (external) options.ExternalAssets = new HtmlAssetReferences("https://assets.example.test/");
        });
        await using var session = await OpenAsync("<!doctype html><html><body></body></html>", width, 850);
        var page = session.Page;
        if (external) foreach (var asset in HtmlInteractiveAssetFiles.GraphExplorer())
            await page.RouteAsync("https://assets.example.test/" + asset.FileName, route => route.FulfillAsync(new() { ContentType = asset.ContentType, Body = asset.Content }));
        var baseline = graph.ToGraphExplorerHtmlPage(options => { options.RenderBackend = backend; options.Theme = HtmlGraphExplorerTheme.Light; options.PersistThemePreference = false; });
        await page.SetContentAsync(baseline);
        await AssertGraphChromeContrastAsync(page);
        var native = await NativeSnapshotAsync(page, ".cfx-graph-explorer");
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.theme(document.querySelector('.cfx-graph-explorer'),'dark')");
        await AssertGraphChromeContrastAsync(page);
        await page.SetContentAsync(html);
        await AssertGraphChromeContrastAsync(page);
        Assert.Equal(native, await NativeSnapshotAsync(page, ".cfx-graph-explorer"));
        await ObserveInputAsync(page);
        var root = page.Locator(".cfx-graph-explorer");
        await page.WaitForFunctionAsync("()=>!!document.querySelector('[data-cfx-graph-bound=true]')");
        Assert.Equal("rtl", await root.EvaluateAsync<string>("root=>getComputedStyle(root).direction"));
        Assert.Equal("البحث عن عنصر", await page.Locator("[data-cfx-graph-search]").GetAttributeAsync("placeholder"));
        await page.EvaluateAsync("()=>{window.graphRoot=document.querySelector('.cfx-graph-explorer');window.exports=[];graphRoot.addEventListener('cfxgraphexport',event=>{event.preventDefault();exports.push(event.detail)});}");
        await ClickGraphNodeAsync(page, "a");
        Assert.Equal("a", await page.EvaluateAsync<string>("()=>ChartForgeXGraphExplorer.get(graphRoot).selection.primary"));
        await page.Locator("[data-cfx-graph-search]").FillAsync("Source");
        Assert.Equal("نتيجة 1", await page.Locator("[data-cfx-role=graph-search-status]").TextContentAsync());
        await page.Locator("[data-cfx-graph-search]").FillAsync("Authored");
        Assert.Equal("2 matches", await page.Locator("[data-cfx-role=graph-search-status]").TextContentAsync());
        await AssertLeadingEnglishNumberOrderAsync(page.Locator("[data-cfx-role=graph-search-status]"));
        await page.Locator("[data-cfx-graph-search]").FillAsync("");
        var actual = await root.GetAttributeAsync("data-cfx-graph-renderer-active");
        var focus = actual == "svg" ? root.Locator("[data-cfx-role=graph-node][data-node-id=a]") : root.Locator("[data-cfx-role=graph-" + actual + "]");
        await FocusWithKeyboardAsync(page, focus);
        await page.Keyboard.PressAsync("Home");
        Assert.True(await root.EvaluateAsync<bool>("root=>root.contains(document.activeElement)&&document.activeElement.matches(':focus-visible')"));
        if (actual == "svg") Assert.Equal("flow", await page.Locator(":focus").GetAttributeAsync("data-edge-id"));
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.highlight(graphRoot,[{role:'graph-node',id:'b'}])");
        Assert.Equal("عناصر مميزة 1", await root.Locator("[data-cfx-role=graph-announcer]").TextContentAsync());
        await CaptureAsync(page, "graph-" + backend + "-selected");

        await root.Locator("[data-cfx-graph-action=edit]").ClickAsync();
        await root.Locator("[data-cfx-graph-editor-action=add-edge]").ClickAsync();
        var form = root.Locator("[data-cfx-graph-editor-form=edge]");
        await form.Locator("[data-cfx-graph-editor-field=id]").FillAsync("invalid");
        await form.Locator("[data-cfx-graph-editor-field=source]").FillAsync("a");
        await form.Locator("[data-cfx-graph-editor-field=target]").FillAsync("absent");
        await form.Locator("button[type=submit]").ClickAsync();
        Assert.Equal("العلاقة invalid: نهاية مفقودة", await root.Locator("[data-cfx-graph-editor-status]").TextContentAsync());
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>ChartForgeXGraphExplorer.get(graphRoot).edges.length"));
        var error = await page.EvaluateAsync<string>("()=>{try{ChartForgeXGraphExplorer.update(graphRoot,{upsertEdges:[{id:'invalid',source:'a',target:'absent'}]})}catch(error){return error.message}return ''}");
        Assert.Equal("Graph patch edge 'invalid' references a missing endpoint.", error);
        await root.Locator("[data-cfx-graph-editor-close]").ClickAsync();
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.update(graphRoot,{upsertEdges:[{id:'plain',source:'a',target:'b'}]})");
        await AssertCurrentEndpointNamesAsync(page, "Authored destination");
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.update(graphRoot,{upsertNodes:[{id:'b',label:'Node-only update',x:500,y:170}]})");
        await AssertCurrentEndpointNamesAsync(page, "Node-only update");
        await page.EvaluateAsync("()=>{ChartForgeXGraphExplorer.update(graphRoot,{upsertNodes:[{id:'b',label:'Authored update',x:500,y:170}],upsertEdges:[{id:'plain',source:'a',target:'b'},{id:'flow',source:'a',target:'b',label:'Authored relationship'}]});ChartForgeXGraphExplorer.theme(graphRoot,'dark');}");
        await AssertCurrentEndpointNamesAsync(page, "Authored update");
        await AssertGraphChromeContrastAsync(page);
        Assert.Contains("داكن", await root.Locator("[data-cfx-graph-action=theme]").GetAttributeAsync("aria-label"));
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.export(graphRoot,'svg')");
        var svg = await page.EvaluateAsync<string>("()=>exports.at(-1).content");
        Assert.Contains("Authored update", svg);
        Assert.DoesNotContain("نهاية مفقودة", svg);
        await page.EvaluateAsync("()=>{ChartForgeXGraphExplorer.dispose(graphRoot);ChartForgeXGraphExplorer.mount(graphRoot);}");
        await AssertCurrentEndpointNamesAsync(page, "Authored update");
        await page.Locator("[data-cfx-graph-search]").FillAsync("Authored update");
        Assert.Equal("نتيجة 1", await page.Locator("[data-cfx-role=graph-search-status]").TextContentAsync());
        var state = await page.EvaluateAsync<JsonElement>("()=>ChartForgeXGraphExplorer.get(graphRoot)");
        Assert.Equal("Healthy", state.GetProperty("nodes")[0].GetProperty("status").GetString());
        await CaptureAsync(page, "graph-" + backend + "-updated");
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.dispose(graphRoot)");
        AssertNoConsoleErrors(session);
    }

    private static async Task AssertGraphChromeContrastAsync(IPage page) {
        // Theme colors transition for 160ms. Observe their real completion rather than capturing
        // a light surface paired with the next theme's foreground halfway through the animation.
        await page.Locator(".cfx-graph-header").EvaluateAsync("""
            async header=>{void header.offsetWidth;await Promise.all(header.getAnimations({subtree:true}).map(animation=>animation.finished.catch(()=>{})));}
            """);
        var controls = page.Locator(".cfx-graph-search,.cfx-graph-filter,.cfx-graph-toolbar-group--appearance button");
        foreach (var target in await controls.AllAsync()) {
            Assert.True(await target.IsEnabledAsync());
            var paints = await target.EvaluateAsync<string[]>("""
                node=>{const backgrounds=[];for(let owner=node;owner;owner=owner.parentElement)backgrounds.unshift(getComputedStyle(owner).backgroundColor);
                    return [getComputedStyle(node).color,...backgrounds];}
                """);
            var rgba = await ResolveCssColorsAsync(page, paints);
            ChartColor Color(int[] value) => ChartColor.FromRgba((byte)value[0], (byte)value[1], (byte)value[2], (byte)value[3]);
            var background = ChartColor.White;
            for (var index = 1; index < rgba.Length; index++) background = ChartColorMath.CompositeOverOpaque(Color(rgba[index]), background);
            var ink = ChartColorMath.CompositeOverOpaque(Color(rgba[0]), background);
            Assert.True(ChartColorMath.ContrastRatio(ink, background) >= 4.5, "Enabled graph chrome must retain readable settled foreground and surface colors.");
        }
    }

    private static async Task AssertCurrentEndpointNamesAsync(IPage page, string targetLabel) {
        var facts = await page.EvaluateAsync<JsonElement>("""
            async()=>{const model=ChartForgeXGraphExplorer.get(graphRoot);await ChartForgeXGraphExplorer.export(graphRoot,'svg');
                const svg=new DOMParser().parseFromString(exports.at(-1).content,'image/svg+xml');
                return {target:model.nodes.find(node=>node.id==='b').label,selection:model.selection.primary,
                    source:model.edges.find(edge=>edge.id==='plain').source,targetId:model.edges.find(edge=>edge.id==='plain').target,
                    edges:[...svg.querySelectorAll('[data-cfx-role=graph-edge]')].map(edge=>({id:edge.dataset.edgeId,name:edge.getAttribute('aria-label')}))};}
            """);
        Assert.Equal(targetLabel, facts.GetProperty("target").GetString());
        Assert.Equal("a", facts.GetProperty("selection").GetString());
        Assert.Equal("a", facts.GetProperty("source").GetString());
        Assert.Equal("b", facts.GetProperty("targetId").GetString());
        var edges = facts.GetProperty("edges").EnumerateArray().ToArray();
        Assert.Equal("صلة Source {target} / " + targetLabel, edges.Single(edge => edge.GetProperty("id").GetString() == "plain").GetProperty("name").GetString());
        Assert.Equal("Authored relationship", edges.Single(edge => edge.GetProperty("id").GetString() == "flow").GetProperty("name").GetString());
        // Physical SVG and accelerated semantic items share the same current endpoint names.
        Assert.Equal("صلة Source {target} / " + targetLabel, await page.Locator(".cfx-graph-explorer").EvaluateAsync<string>("root=>{const item=root.querySelector('[data-cfx-role=graph-edge][data-edge-id=plain]');return item?.getAttribute('aria-label')||''}"));
    }

    private static async Task ClickGraphNodeAsync(IPage page, string id) {
        var point = await page.EvaluateAsync<JsonElement>("""
            id=>{const root=graphRoot,actual=root.dataset.cfxGraphRendererActive,
                surface=root.querySelector('[data-cfx-role="graph-'+(actual==='svg'?'scene':actual)+'"]'),box=surface.getBoundingClientRect(),
                viewBox=root.querySelector('[data-cfx-role=graph-scene]').viewBox.baseVal,
                position=ChartForgeXGraphExplorer.positions(root).find(node=>node.id===id),camera=ChartForgeXGraphExplorer.captureState(root).viewport,
                scale=Math.min(box.width/viewBox.width,box.height/viewBox.height),
                x=box.left+(box.width-viewBox.width*scale)/2+(position.x*camera.scale+camera.x)*scale,
                y=box.top+(box.height-viewBox.height*scale)/2+(position.y*camera.scale+camera.y)*scale,
                hit=document.elementFromPoint(x,y);
                return{x,y,hit:actual==='svg'?hit?.closest('[data-node-id]')?.dataset.nodeId===id:hit===surface};}
            """, id);
        Assert.True(point.GetProperty("hit").GetBoolean(), point.GetRawText());
        await page.Mouse.ClickAsync((float)point.GetProperty("x").GetDouble(), (float)point.GetProperty("y").GetDouble());
    }
}
