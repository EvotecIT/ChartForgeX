using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class StandaloneAdapterLifecycleBrowserTests {
    private const string GraphEnvironmentLabel = "North · Łódź العربية";

    /// <summary>Qualifies real graph controls and screen ink through preference changes, host scaling, print and reuse.</summary>
    [BrowserTheory]
    [InlineData(HtmlGraphRenderBackend.Svg, false, 980)]
    [InlineData(HtmlGraphRenderBackend.Svg, true, 390)]
    [InlineData(HtmlGraphRenderBackend.Canvas, false, 980)]
    [InlineData(HtmlGraphRenderBackend.Canvas, true, 390)]
    [InlineData(HtmlGraphRenderBackend.WebGl, false, 980)]
    [InlineData(HtmlGraphRenderBackend.WebGl, true, 390)]
    public async Task GraphEnvironmentPreferencesKeepControlsPaintPrintAndTeardownUsable(HtmlGraphRenderBackend backend, bool dark, int width) {
        if (!Enabled) return;
        if (!dark) await AssertGraphEnvironmentHostSizingAsync(backend);
        var graph = GraphEnvironmentFixture(backend);
        await using var session = await OpenAsync(Instrument(graph.ToGraphExplorerHtmlPage(options => {
            options.RenderBackend = backend;
            options.Theme = dark ? HtmlGraphExplorerTheme.Dark : HtmlGraphExplorerTheme.Light;
            options.PersistThemePreference = false;
        })), width, 900);
        var page = session.Page;
        await page.EvaluateAsync("""
            () => {
                window.retainedGraph=document.querySelector('.cfx-graph-explorer');
                window.graphEnvironmentSelections=0;window.graphEnvironmentExports=[];
                retainedGraph.addEventListener('cfxgraphselection',()=>graphEnvironmentSelections++);
                retainedGraph.addEventListener('cfxgraphexport',event=>{
                    event.preventDefault();
                    const svg=event.detail.format==='svg'?new DOMParser().parseFromString(event.detail.content,'image/svg+xml'):null;
                    graphEnvironmentExports.push({format:event.detail.format,nodes:svg?.querySelectorAll('[data-cfx-role=graph-node]').length,
                        label:svg?.querySelector('[data-node-id=a]')?.getAttribute('data-node-label')});
                });
                lifetimeProbe.recordGlobals=false;lifetimeProbe.recordObservers=false;
            }
            """);
        var root = page.Locator(".cfx-graph-explorer");
        var actual = await root.GetAttributeAsync("data-cfx-graph-renderer-active");
        if (backend == HtmlGraphRenderBackend.WebGl) {
            Assert.Contains(actual, new[] { "webgl", "canvas" });
            var available = await page.EvaluateAsync<bool>("()=>!!retainedGraph.querySelector('[data-cfx-role=graph-webgl]').getContext('webgl2')");
            if (available) Assert.Equal("webgl", actual);
        }
        else Assert.Equal(backend.ToString().ToLowerInvariant(), actual);
        var name = "graph-environment-" + backend + "-" + (dark ? "dark" : "light");
        await CaptureGraphEnvironmentAsync(page, name + "-baseline");

        await SetGraphEnvironmentPreferencesAsync(page, enabled: true);
        await page.EvaluateAsync("()=>document.documentElement.style.zoom='1.5'");
        var search = page.GetByRole(AriaRole.Searchbox, new() { Name = "Search graph", Exact = true });
        await ReachGraphEnvironmentControlAsync(page, search);
        await AssertGraphEnvironmentFocusPaintAsync(page, search, name + "-search-focus");
        await AssertGraphEnvironmentTextContrastAsync(page, search);
        await search.FillAsync("Łódź");
        await page.Keyboard.PressAsync("Enter");
        Assert.Equal("a", await GraphEnvironmentPrimaryAsync(page));
        Assert.Contains(GraphEnvironmentLabel, await page.Locator(".cfx-graph-tooltip").InnerTextAsync());
        await search.FillAsync("");
        var fit = page.GetByRole(AriaRole.Button, new() { Name = "Fit graph to view", Exact = true });
        await fit.ClickAsync();
        var clear = page.GetByRole(AriaRole.Button, new() { Name = "Clear selection", Exact = true });
        await clear.ClickAsync();

        var keyboardTarget = actual == "svg"
            ? page.Locator("[data-cfx-role=graph-scene] [data-cfx-role][tabindex='0']").First
            : page.Locator("[data-cfx-role=graph-" + actual + "]");
        await ReachGraphEnvironmentControlAsync(page, keyboardTarget);
        await page.Keyboard.PressAsync("ArrowRight");
        if (actual == "svg") {
            var focused = await page.EvaluateAsync<string[]>("""
                ()=>{const node=document.activeElement;return [node.dataset.nodeId||node.dataset.edgeId,
                    node.dataset.nodeLabel||node.dataset.edgeLabel];}
                """);
            Assert.False(string.IsNullOrWhiteSpace(focused[0]));
            await page.Keyboard.PressAsync("Space");
            Assert.Equal(focused[0], await GraphEnvironmentPrimaryAsync(page));
            Assert.Contains(focused[1], await page.Locator(".cfx-graph-tooltip").InnerTextAsync());
        } else {
            await page.Keyboard.PressAsync("Home");
            await page.Keyboard.PressAsync("Space");
            Assert.Equal("a", await GraphEnvironmentPrimaryAsync(page));
            Assert.Contains(GraphEnvironmentLabel, await page.Locator(".cfx-graph-tooltip").InnerTextAsync());
        }
        await CaptureGraphEnvironmentAsync(page, name + "-keyboard-selection");

        // The screenshot pair measures actual selection ink, including accelerated surfaces.
        await clear.ClickAsync();
        var unselected = await CaptureGraphEnvironmentFrameAsync(page, "a", name + "-unselected");
        await ClickGraphEnvironmentPointAsync(page, "a");
        Assert.Equal("a", await GraphEnvironmentPrimaryAsync(page));
        await page.Mouse.MoveAsync(2, 2);
        var selected = await CaptureGraphEnvironmentFrameAsync(page, "a", name + "-selected");
        await AssertGraphEnvironmentMarkPaintAsync(page, unselected, selected, name + "-selection-pixels");
        var scale = await page.EvaluateAsync<double>("()=>ChartForgeXGraphExplorer.captureState(retainedGraph).viewport.scale");
        await ClickGraphEnvironmentControlAsync(page, page.GetByRole(AriaRole.Button, new() { Name = "Zoom out", Exact = true }), name + "-zoom-out");
        Assert.True(await page.EvaluateAsync<double>("()=>ChartForgeXGraphExplorer.captureState(retainedGraph).viewport.scale") < scale);
        Assert.False(await page.Locator(".cfx-graph-command-menu").EvaluateAsync<bool>("menu=>menu.open"));
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.highlight(retainedGraph,[{role:'graph-node',id:'c'}],{dim:false})");
        await AssertGraphEnvironmentTextContrastAsync(page, page.Locator(".cfx-graph-tooltip"));

        var export = page.GetByRole(AriaRole.Button, new() { Name = "Export graph", Exact = true });
        await ReachGraphEnvironmentControlAsync(page, export);
        await page.Keyboard.PressAsync("Enter");
        await WriteReceiptAsync(name + "-export-key-immediate", await page.EvaluateAsync<JsonElement>("""
            ()=>{const summary=retainedGraph.querySelector('.cfx-graph-command-menu summary');
                return {open:summary.closest('details').open,expanded:summary.getAttribute('aria-expanded'),focused:document.activeElement===summary};}
            """));
        await WaitForGraphEnvironmentExportAsync(page, expanded: true);
        await AssertGraphEnvironmentMenuPaintAsync(page, name);
        await AssertGraphEnvironmentFocusPaintAsync(page, export, name + "-export-focus");
        await page.Keyboard.PressAsync("Escape");
        await WaitForGraphEnvironmentExportAsync(page, expanded: false);
        Assert.True(await export.EvaluateAsync<bool>("node=>node===document.activeElement"));
        await page.Keyboard.PressAsync("Enter");
        await WaitForGraphEnvironmentExportAsync(page, expanded: true);
        var exportSvg = page.GetByRole(AriaRole.Button, new() { Name = "Export SVG document", Exact = true });
        await ReachGraphEnvironmentControlAsync(page, exportSvg);
        await AssertGraphEnvironmentTextContrastAsync(page, exportSvg);
        await page.Keyboard.PressAsync("Enter");
        await WaitForGraphEnvironmentExportAsync(page, expanded: false);
        var exported = await page.EvaluateAsync<JsonElement>("()=>graphEnvironmentExports.at(-1)");
        Assert.Equal("svg", exported.GetProperty("format").GetString());
        Assert.Equal(4, exported.GetProperty("nodes").GetInt32());
        Assert.Equal(GraphEnvironmentLabel, exported.GetProperty("label").GetString());

        await page.GetByRole(AriaRole.Button, new() { Name = "Focus selected node neighborhood", Exact = true }).ClickAsync();
        Assert.True(await page.Locator("[data-cfx-role=graph-neighborhood-navigation]").IsVisibleAsync());
        await page.GetByRole(AriaRole.Button, new() { Name = "Clear neighborhood focus", Exact = true }).ClickAsync();
        var state = await GraphCommittedStateAsync(page);
        await CaptureGraphEnvironmentAsync(page, name + "-controls");
        await AssertGraphEnvironmentPrintAsync(page, name, state);

        await SetGraphEnvironmentPreferencesAsync(page, enabled: false);
        Assert.Equal(state, await GraphCommittedStateAsync(page));
        await CaptureGraphEnvironmentAsync(page, name + "-preferences-restored");
        await SetGraphEnvironmentPreferencesAsync(page, enabled: true);
        Assert.Equal(state, await GraphCommittedStateAsync(page));
        await page.EmulateMediaAsync(new() { Media = Media.Print, ForcedColors = ForcedColors.Active, ReducedMotion = ReducedMotion.Reduce });
        await WaitForGraphPrintSurfaceAsync(page);
        Assert.True(await page.EvaluateAsync<bool>("()=>ChartForgeXGraphExplorer.dispose(retainedGraph)"));
        Assert.Equal(0, await page.Locator("[data-cfx-role=graph-print-surface]").CountAsync());
        await AssertReleasedAsync(page, name + "-disposed");
        await page.EmulateMediaAsync(new() { Media = Media.Screen, ForcedColors = ForcedColors.Active, ReducedMotion = ReducedMotion.Reduce });
        await page.EvaluateAsync("""
            ()=>{lifetimeProbe.recordGlobals=true;lifetimeProbe.recordObservers=true;
                ChartForgeXGraphExplorer.mount(retainedGraph);
                lifetimeProbe.recordGlobals=false;lifetimeProbe.recordObservers=false;
                ChartForgeXGraphExplorer.clearHighlight(retainedGraph);ChartForgeXGraphExplorer.clearSelection(retainedGraph);
                graphEnvironmentSelections=0;}
            """);
        await ClickGraphEnvironmentPointAsync(page, "b");
        Assert.Equal("b", await GraphEnvironmentPrimaryAsync(page));
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>graphEnvironmentSelections"));
        await CaptureGraphEnvironmentAsync(page, name + "-remounted");
        Assert.True(await page.EvaluateAsync<bool>("()=>ChartForgeXGraphExplorer.dispose(retainedGraph)"));
        await AssertReleasedAsync(page, name + "-final-release");
        AssertNoConsoleErrors(session);
    }

    private static GraphScene GraphEnvironmentFixture(HtmlGraphRenderBackend backend) {
        var graph = GraphScene.Create("environment-graph", "Graph controls and source facts")
            .AddNode("a", GraphEnvironmentLabel, node => { node.Fixed = true; node.X = 220; node.Y = 180; node.Size = 30; node.SecondaryLabel = "Measured input"; })
            .AddNode("b", "Processing", node => { node.Fixed = true; node.X = 580; node.Y = 180; node.Size = 30; })
            .AddNode("c", "Independent reference", node => { node.Fixed = true; node.X = 220; node.Y = 380; node.Size = 30; })
            .AddNode("d", "Storage", node => { node.Fixed = true; node.X = 580; node.Y = 380; node.Size = 30; })
            .AddEdge("ab", "a", "b", "Receives", edge => edge.Directed = true)
            .AddEdge("bd", "b", "d", "Writes", edge => edge.Directed = true);
        graph.Options.Features = GraphSceneFeatures.Selection | GraphSceneFeatures.MultiSelection | GraphSceneFeatures.Search
            | GraphSceneFeatures.Filtering | GraphSceneFeatures.Viewport | GraphSceneFeatures.Export
            | GraphSceneFeatures.StateSnapshots | GraphSceneFeatures.NeighborhoodFocus;
        if (backend != HtmlGraphRenderBackend.Svg) {
            graph.Options.Features |= GraphSceneFeatures.LevelOfDetail;
            graph.Options.LevelOfDetail.CanvasPreferredNodeThreshold = 1;
        }
        return graph;
    }
}
