using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Measures endpoint captions against real text and badge bounds in prepared and updated graph output.</summary>
[Trait("BrowserSuite", "Compatibility")]
public sealed class GraphEndpointCaptionBrowserTests {
    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PreparedSvgKeepsRelationshipCaptionsClearOfBothEndpointDetails(bool reverse) {
        var graph = Scene(reverse);
        var svg = graph.ToGraphSvg(configure: options => { options.Width = 960; options.Height = 560; });
        var png = graph.ToGraphPng(configure: options => { options.Width = 960; options.Height = 560; });
        await using var session = await OpenAsync("<!doctype html><html><body style=\"margin:0\"><section id=\"caption-proof\">" + svg +
            "</section><img alt=\"Paired native PNG\" width=\"960\" height=\"560\" src=\"data:image/png;base64," + Convert.ToBase64String(png) + "\"></body></html>", 1000, 1200);
        await session.Page.EvaluateAsync("async()=>{await document.fonts.ready;await document.querySelector('img').decode();}");
        var facts = await MeasureAsync(session.Page);
        await CaptureAsync(session.Page, "graph-endpoint-prepared-" + reverse, facts);
        AssertFacts(facts);
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(HtmlGraphRenderBackend.Svg, false, 980)]
    [InlineData(HtmlGraphRenderBackend.Canvas, true, 390)]
    [InlineData(HtmlGraphRenderBackend.WebGl, false, 980)]
    public async Task UpdatedEndpointsKeepCaptionClearanceInRuntimeExportsAndPrint(HtmlGraphRenderBackend backend, bool dark, int width) {
        var graph = Scene(false);
        if (backend != HtmlGraphRenderBackend.Svg) {
            graph.Options.Features |= GraphSceneFeatures.LevelOfDetail;
            graph.Options.LevelOfDetail.CanvasPreferredNodeThreshold = 1;
        }
        await using var session = await OpenAsync(graph.ToGraphExplorerHtmlPage(options => {
            options.RenderBackend = backend; options.Theme = dark ? HtmlGraphExplorerTheme.Dark : HtmlGraphExplorerTheme.Light;
            options.PersistThemePreference = false; options.FillAvailableHeight = true;
        }), width, 800);
        var page = session.Page;
        var actual = await page.Locator(".cfx-graph-explorer").GetAttributeAsync("data-cfx-graph-renderer-active");
        if (backend == HtmlGraphRenderBackend.WebGl) Assert.Contains(actual, new[] { "webgl", "canvas" });
        else Assert.Equal(backend.ToString().ToLowerInvariant(), actual);
        await page.EvaluateAsync("""
            ()=>{
                window.captionRoot=document.querySelector('.cfx-graph-explorer');
                ChartForgeXGraphExplorer.update(captionRoot,{upsertEdges:[
                    {id:'secondary-edge',sourceNodeId:'apac',targetNodeId:'europe',label:'replication'},
                    {id:'badge-edge',sourceNodeId:'worker',targetNodeId:'counter',label:'badge route'}]});
                window.captionExports={};
                captionRoot.addEventListener('cfxgraphexport',event=>{captionExports[event.detail.format]=event.detail.content;event.preventDefault();});
            }
            """);
        await page.EvaluateAsync("async()=>{await ChartForgeXGraphExplorer.export(captionRoot,'svg');await ChartForgeXGraphExplorer.export(captionRoot,'png');}");
        await page.EvaluateAsync("""
            async()=>{
                const host=document.createElement('section');host.id='caption-proof';
                const svg=document.importNode(new DOMParser().parseFromString(captionExports.svg,'image/svg+xml').documentElement,true);
                svg.style.cssText='display:block;width:960px;height:560px;max-width:none';host.append(svg);document.body.append(host);
                await document.fonts.ready;
            }
            """);
        var exported = await MeasureAsync(page);
        await CaptureAsync(page, "graph-endpoint-runtime-" + backend, exported);
        AssertFacts(exported);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            var png = await page.EvaluateAsync<string>("()=>captionExports.png");
            await File.WriteAllBytesAsync(Path.Combine(output, "graph-endpoint-runtime-" + backend + ".png"), Convert.FromBase64String(png[(png.IndexOf(',') + 1)..]));
        }
        await page.EvaluateAsync("()=>document.getElementById('caption-proof').remove()");
        await page.EmulateMediaAsync(new() { Media = Media.Print });
        await page.WaitForFunctionAsync("()=>document.querySelector('[data-cfx-role=graph-print-surface] svg')");
        var printed = await MeasureAsync(page, "[data-cfx-role=graph-print-surface]");
        await CaptureAsync(page, "graph-endpoint-print-" + backend, printed);
        AssertFacts(printed);
        await page.EmulateMediaAsync(new() { Media = Media.Screen });
        await page.WaitForFunctionAsync("()=>!document.querySelector('[data-cfx-role=graph-print-surface]')");
        AssertNoConsoleErrors(session);
    }

    private static GraphScene Scene(bool reverse) {
        var graph = GraphScene.Create("endpoint-captions", "Endpoint details")
            .AddNode("europe", "Europe", node => { node.Fixed = true; node.X = 260; node.Y = 180; node.Size = 8; node.SecondaryLabel = "4 sites · 18 workloads"; })
            .AddNode("apac", "Asia Pacific", node => { node.Fixed = true; node.X = 320; node.Y = 260; node.Size = 8; })
            .AddNode("counter", "Counter", node => { node.Fixed = true; node.X = 600; node.Y = 180; node.Size = 8; node.BadgeText = "+12345678901234567890"; })
            .AddNode("worker", "Worker", node => { node.Fixed = true; node.X = 720; node.Y = 180; node.Size = 8; })
            .AddEdge("secondary-edge", reverse ? "apac" : "europe", reverse ? "europe" : "apac", "replication")
            .AddEdge("badge-edge", reverse ? "worker" : "counter", reverse ? "counter" : "worker", "badge route");
        graph.Options.Features = GraphSceneFeatures.Viewport | GraphSceneFeatures.Export | GraphSceneFeatures.IncrementalUpdates;
        return graph;
    }

    private static Task<JsonElement> MeasureAsync(IPage page, string selector = "#caption-proof") => page.EvaluateAsync<JsonElement>("""
        selector=>{
            const svg=document.querySelector(selector+' svg'),collisions=[];
            const facts=[...svg.querySelectorAll('[data-cfx-role=graph-edge-label]')].map(caption=>{
                const id=caption.dataset.edgeLabelFor,endpoints=id==='secondary-edge'?['europe','apac']:['counter','worker'];
                const box=caption.getBoundingClientRect();
                for(const endpoint of endpoints){
                    const details=svg.querySelector('[data-node-details-for="'+endpoint+'"]');
                    for(const obstacle of details.querySelectorAll('.cfx-graph-node-label,.cfx-graph-node-secondary,.cfx-graph-node-badge')){
                        const bounds=obstacle.getBoundingClientRect();
                        const width=Math.min(box.right,bounds.right)-Math.max(box.left,bounds.left),height=Math.min(box.bottom,bounds.bottom)-Math.max(box.top,bounds.top);
                        if(width>0&&height>0)collisions.push({edge:id,endpoint,detail:obstacle.textContent,area:width*height});
                    }
                }
                return {id,text:caption.textContent,x:caption.getAttribute('x'),y:caption.getAttribute('y')};
            });
            return {facts,collisions,renderer:document.querySelector('.cfx-graph-explorer')?.dataset.cfxGraphRendererActive||'prepared-svg',secondary:svg.querySelector('[data-node-details-for=europe] .cfx-graph-node-secondary')?.textContent,
                badge:svg.querySelector('[data-node-details-for=counter] .cfx-graph-node-badge text')?.textContent};
        }
        """, selector);

    private static void AssertFacts(JsonElement facts) {
        Assert.Equal(2, facts.GetProperty("facts").GetArrayLength());
        Assert.Equal(new[] { "replication", "badge route" }, facts.GetProperty("facts").EnumerateArray().Select(item => item.GetProperty("text").GetString()));
        Assert.Equal("4 sites · 18 workloads", facts.GetProperty("secondary").GetString());
        Assert.Equal("+12345678901234567890", facts.GetProperty("badge").GetString());
        Assert.True(facts.GetProperty("collisions").GetArrayLength() == 0, facts.GetRawText());
    }

    private static async Task CaptureAsync(IPage page, string name, JsonElement facts) {
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        await File.WriteAllTextAsync(Path.Combine(output, name + ".json"), facts.GetRawText());
        await page.ScreenshotAsync(new() { Path = Path.Combine(output, name + "-screen.png"), FullPage = true });
    }
}
