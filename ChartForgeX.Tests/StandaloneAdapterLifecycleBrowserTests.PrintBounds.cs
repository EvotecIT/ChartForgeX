using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class StandaloneAdapterLifecycleBrowserTests {
    [BrowserTheory]
    [InlineData(HtmlGraphRenderBackend.Svg, true, false, 980)]
    [InlineData(HtmlGraphRenderBackend.Svg, false, true, 390)]
    [InlineData(HtmlGraphRenderBackend.Canvas, true, true, 390)]
    [InlineData(HtmlGraphRenderBackend.Canvas, false, false, 980)]
    [InlineData(HtmlGraphRenderBackend.WebGl, true, false, 980)]
    [InlineData(HtmlGraphRenderBackend.WebGl, false, true, 390)]
    public async Task GraphPrintFitsActualVisibleLabelsSecondaryBadgesAndEdgeCaptions(
        HtmlGraphRenderBackend backend, bool singleton, bool dark, int width) {
        if (!Enabled) return;
        var label = "Boundary label " + string.Concat(Enumerable.Repeat("WideObservation_", 10));
        var secondary = "Secondary evidence " + string.Concat(Enumerable.Repeat("UnabridgedCaption_", 12));
        var badge = "Badge " + string.Concat(Enumerable.Repeat("Contribution_", 8));
        var caption = "Relationship " + new string('W', 110);
        var graph = GraphScene.Create("print-detail-bounds", "Complete printed details");
        graph.AddNode("boundary", singleton ? label : "Boundary", node => {
            node.Fixed = true; node.X = 100; node.Y = 100; node.Size = 16;
            if (singleton) { node.SecondaryLabel = secondary; node.BadgeText = badge; }
        });
        if (!singleton) {
            graph.AddNode("peer", "Peer", node => { node.Fixed = true; node.X = 300; node.Y = 100; node.Size = 16; });
            graph.AddEdge("caption", "boundary", "peer", caption, edge => edge.Directed = true);
        }
        graph.Options.Features = GraphSceneFeatures.Selection | GraphSceneFeatures.Viewport | GraphSceneFeatures.Export;
        if (backend != HtmlGraphRenderBackend.Svg) {
            graph.Options.Features |= GraphSceneFeatures.LevelOfDetail;
            graph.Options.LevelOfDetail.CanvasPreferredNodeThreshold = 1;
        }
        await using var session = await OpenAsync(Instrument(graph.ToGraphExplorerHtmlPage(options => {
            options.RenderBackend = backend; options.Theme = dark ? HtmlGraphExplorerTheme.Dark : HtmlGraphExplorerTheme.Light;
            options.PersistThemePreference = false; options.FillAvailableHeight = true;
        })), width, 720);
        var page = session.Page;
        await page.EvaluateAsync("()=>{window.retainedGraph=document.querySelector('.cfx-graph-explorer');ChartForgeXGraphExplorer.applyState(retainedGraph,{version:1,viewport:{x:23,y:17,scale:1.8}});lifetimeProbe.recordGlobals=false;lifetimeProbe.recordObservers=false;}");
        await page.EvaluateAsync("()=>document.fonts.ready.then(()=>true)");
        var before = await GraphCommittedStateAsync(page);
        await page.EmulateMediaAsync(new() { Media = Media.Print });
        await WaitForGraphPrintSurfaceAsync(page);
        var facts = await page.EvaluateAsync<JsonElement>("""
            () => {
                const svg=document.querySelector('[data-cfx-role=graph-print-surface] svg'),box=svg.getBoundingClientRect();
                const details=[...svg.querySelectorAll('[data-cfx-role=graph-node], [data-cfx-role=graph-node-details], [data-cfx-role=graph-edge-label], [data-cfx-role=graph-edge-arrows]')]
                    .filter(element=>element.getClientRects().length&&getComputedStyle(element).display!=='none');
                return {viewBox:svg.getAttribute('viewBox'),renderer:retainedGraph.dataset.cfxGraphRendererActive,
                    maxOverflow:Math.max(0,...details.map(element=>{const rect=element.getBoundingClientRect();return Math.max(box.left-rect.left,rect.right-box.right,box.top-rect.top,rect.bottom-box.bottom);})),
                    label:svg.querySelector('[data-node-details-for=boundary] .cfx-graph-node-label')?.textContent,
                    secondary:svg.querySelector('[data-node-details-for=boundary] .cfx-graph-node-secondary')?.textContent,
                    badge:svg.querySelector('[data-node-details-for=boundary] .cfx-graph-node-badge text')?.textContent,
                    caption:svg.querySelector('[data-edge-label-for=caption]')?.textContent,
                    measuredRoles:[...new Set(details.map(element=>element.dataset.cfxRole))]};
            }
            """);
        var name = "graph-print-bounds-" + backend + (singleton ? "-Singleton" : "-Edge");
        await WriteReceiptAsync(name, facts);
        await CaptureAsync(page, name);
        Assert.InRange(facts.GetProperty("maxOverflow").GetDouble(), 0, 1);
        Assert.Contains("graph-node-details", facts.GetProperty("measuredRoles").EnumerateArray().Select(item => item.GetString()));
        if (singleton) {
            Assert.Equal(label, facts.GetProperty("label").GetString());
            Assert.Equal(secondary, facts.GetProperty("secondary").GetString());
            Assert.Equal(badge, facts.GetProperty("badge").GetString());
        } else Assert.Equal(caption, facts.GetProperty("caption").GetString());
        Assert.Equal(before, await GraphCommittedStateAsync(page));
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (Engine == HtmlTinkerX.HtmlBrowserEngine.Chromium && !string.IsNullOrWhiteSpace(output))
            await page.PdfAsync(new() { Path = Path.Combine(output, name + ".pdf"), Format = "A4", Landscape = true, PrintBackground = true,
                Margin = new() { Top = "12mm", Bottom = "12mm", Left = "12mm", Right = "12mm" } });
        await page.EmulateMediaAsync(new() { Media = Media.Screen });
        await page.WaitForFunctionAsync("()=>!document.querySelector('[data-cfx-role=graph-print-surface]')");
        Assert.Equal(before, await GraphCommittedStateAsync(page));
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.dispose(retainedGraph)");
        await AssertReleasedAsync(page, name + "-released");
        AssertNoConsoleErrors(session);
    }
}
