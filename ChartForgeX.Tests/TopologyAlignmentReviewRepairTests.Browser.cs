using System.Text.Json;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class TopologyAlignmentReviewRepairTests {
    [BrowserTheory]
    [InlineData(false, 390)] [InlineData(true, 1100)]
    public async Task ActualDirectAndFencedSvgPathsClearForeignBodiesWithOrderedAlignments(bool dark, int width) {
        var source = MermaidArchitectureAlignmentTests.Fixture(); var options = MermaidArchitectureNativeDepthTests.Options(dark, width);
        var direct = MermaidRenderer.Render(source, options); Assert.Empty(direct.Diagnostics);
        await ObserveClearanceAsync(direct.Artifact!, width, "alignment-review-source-direct-" + width, alignedSource: true);
        var fence = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```"); Assert.False(fence.HasErrors);
        await ObserveClearanceAsync(Assert.Single(fence.Artifacts), width, "alignment-review-source-fence-" + width, alignedSource: true);
    }

    [BrowserTheory]
    [InlineData(false, 390)] [InlineData(true, 1100)]
    public async Task ActualNamedPortManualAndTypedImportedPathsClearTheNarrowGap(bool horizontal, int width) {
        var chart = NarrowGapChart(horizontal, reverse: false, aligned: true, named: true);
        chart.Theme = chart.Theme.WithFontFamily(BrowserProofFonts.Family);
        var artifact = chart.ToVisualArtifact();
        await ObserveClearanceAsync(artifact, width, "alignment-review-manual-named-" + width, alignedSource: false);
        var restored = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson()).ToTopologyChart();
        await ObserveClearanceAsync(restored.ToVisualArtifact(), width, "alignment-review-imported-named-" + width, alignedSource: false);
    }

    [BrowserTheory]
    [InlineData(TopologyLayoutDirection.TopToBottom, TopologyLayoutDirection.RightToLeft, TopologyAlignmentKind.Column, 390)]
    [InlineData(TopologyLayoutDirection.RightToLeft, TopologyLayoutDirection.BottomToTop, TopologyAlignmentKind.Row, 1100)]
    public async Task ActualNestedNodeAlignmentAndPaintedSeparatorsUseNearestAuthoredDirection(TopologyLayoutDirection global, TopologyLayoutDirection local, TopologyAlignmentKind kind, int width) {
        var chart = DirectionChart(global, local, kind, middleOverride: true);
        chart.Theme = chart.Theme.WithFontFamily(BrowserProofFonts.Family);
        var artifact = chart.ToVisualArtifact(); var name = "alignment-review-inherited-browser-" + width;
        TopologyAlignmentReviewCausalTests.Capture(artifact, name);
        await using var session = await BrowserProofFonts.OpenAsync(artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize }), width, 800);
        await CaptureBrowserAsync(session.Page, name);
        var observed = await session.Page.EvaluateAsync<JsonElement>("""
            () => {
                const bounds=e=>{const r=e.getBoundingClientRect();return {x:r.x,y:r.y,right:r.right,bottom:r.bottom,width:r.width,height:r.height}};
                const groups=[...document.querySelectorAll('[data-cfx-role="topology-group"]')].filter(e=>e.querySelector('[data-cfx-role="topology-group-surface"]')).map(e=>({id:e.dataset.groupId,parent:e.dataset.parentGroupId??null,...bounds(e.querySelector('[data-cfx-role="topology-group-surface"]'))}));
                const nodes=[...document.querySelectorAll('[data-cfx-role="topology-node"]')].map(e=>({id:e.dataset.nodeId,parent:e.dataset.groupId??null,...bounds(e.querySelector('[data-cfx-role="topology-node-surface"]'))}));
                const separators=[...document.querySelectorAll('[data-cfx-role="topology-compartment-separator"]')].map(e=>bounds(e));
                return {groups,nodes,separators,overflow:document.documentElement.scrollWidth>innerWidth};
            }
            """);
        CaptureObserved(name, observed); AssertDirection(artifact.ToInterchangeEnvelope(), kind);
        var groups = observed.GetProperty("groups").EnumerateArray().ToArray();
        var nodes = observed.GetProperty("nodes").EnumerateArray().ToArray();
        var first = nodes.Single(node => node.GetProperty("id").GetString() == "a"); var second = nodes.Single(node => node.GetProperty("id").GetString() == "b");
        if (kind == TopologyAlignmentKind.Column) { Equal(first, second, "x"); Equal(first, second, "width"); Assert.True(first.GetProperty("bottom").GetDouble() < second.GetProperty("y").GetDouble()); }
        else { Equal(first, second, "y"); Equal(first, second, "height"); Assert.True(first.GetProperty("right").GetDouble() < second.GetProperty("x").GetDouble()); }
        // Compartments have semantic bounds, no painted surface. Observe their real
        // nodes against the three actual containing shells; native assertions cover
        // the compartment bounds without manufacturing a browser rectangle.
        Assert.Equal(3, groups.Length); Assert.Equal(2, nodes.Length);
        foreach (var parent in groups) foreach (var child in nodes) {
            Assert.True(child.GetProperty("x").GetDouble() >= parent.GetProperty("x").GetDouble() - .1 && child.GetProperty("y").GetDouble() >= parent.GetProperty("y").GetDouble() - .1
                && child.GetProperty("right").GetDouble() <= parent.GetProperty("right").GetDouble() + .1 && child.GetProperty("bottom").GetDouble() <= parent.GetProperty("bottom").GetDouble() + .1);
        }
        var separator = Assert.Single(observed.GetProperty("separators").EnumerateArray());
        Assert.True(separator.GetProperty(kind == TopologyAlignmentKind.Column ? "width" : "height").GetDouble() > 1);
        Assert.InRange(separator.GetProperty(kind == TopologyAlignmentKind.Column ? "height" : "width").GetDouble(), 0, .1);
        Assert.False(observed.GetProperty("overflow").GetBoolean()); InteractiveChartBrowser.AssertNoConsoleErrors(session);
        static void Equal(JsonElement a, JsonElement b, string key) => Assert.InRange(Math.Abs(a.GetProperty(key).GetDouble() - b.GetProperty(key).GetDouble()), 0, .1);
    }

    private static async Task ObserveClearanceAsync(VisualArtifact artifact, int width, string name, bool alignedSource) {
        TopologyAlignmentReviewCausalTests.Capture(artifact, name);
        await using var session = await BrowserProofFonts.OpenAsync(artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize }), width, 800);
        await CaptureBrowserAsync(session.Page, name);
        var observed = await session.Page.EvaluateAsync<JsonElement>("""
            () => {
                const svg=document.querySelector('svg'),hits=[],missing=[];
                const nodes=[...svg.querySelectorAll('[data-cfx-role="topology-node"]')].map(e=>({id:e.dataset.nodeId,body:e.querySelector('[data-cfx-role="topology-node-surface"]')?.getBoundingClientRect()})).filter(n=>n.body&&n.body.width&&n.body.height);
                const edges=[...svg.querySelectorAll('[data-cfx-role="topology-edge"]')];
                for(const edge of edges){
                    const path=edge.querySelector('[data-cfx-role="topology-edge-line"]');
                    if(!path||!path.getTotalLength){missing.push(edge.dataset.edgeId);continue}
                    const matrix=path.getScreenCTM(),length=path.getTotalLength(),steps=Math.max(1,Math.ceil(length*2));
                    for(const node of nodes){
                        if(node.id===edge.dataset.sourceNodeId||node.id===edge.dataset.targetNodeId)continue;
                        for(let i=0;i<=steps;i++){
                            const p=path.getPointAtLength(length*i/steps).matrixTransform(matrix),b=node.body;
                            if(p.x>b.left+.1&&p.x<b.right-.1&&p.y>b.top+.1&&p.y<b.bottom-.1){hits.push({edge:edge.dataset.edgeId,node:node.id,x:p.x,y:p.y});break}
                        }
                    }
                }
                return {hits,missing,edges:edges.length,nodes:nodes.length,overflow:document.documentElement.scrollWidth>innerWidth,scripts:svg.querySelectorAll('script,image,foreignObject').length};
            }
            """);
        CaptureObserved(name, observed); Assert.Empty(observed.GetProperty("hits").EnumerateArray()); Assert.Empty(observed.GetProperty("missing").EnumerateArray());
        Assert.Equal(alignedSource ? 4 : 1, observed.GetProperty("edges").GetInt32()); Assert.Equal(alignedSource ? 7 : 3, observed.GetProperty("nodes").GetInt32());
        AssertClear(artifact.ToInterchangeEnvelope()); if (alignedSource) MermaidArchitectureAlignmentTests.AssertGeometry(artifact.ToInterchangeEnvelope());
        Assert.False(observed.GetProperty("overflow").GetBoolean()); Assert.Equal(0, observed.GetProperty("scripts").GetInt32()); InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    private static async Task CaptureBrowserAsync(IPage page, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".browser.png"), FullPage = true });
        var host = page.Locator("[data-cfx-html-sizing='preserve-size']"); await host.EvaluateAsync("element=>element.scrollLeft=element.scrollWidth");
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".scrolled.browser.png"), FullPage = true }); await host.EvaluateAsync("element=>element.scrollLeft=0");
    }
    private static void CaptureObserved(string name, JsonElement observed) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        File.WriteAllText(Path.Combine(output, name + ".observed.json"), observed.GetRawText());
    }
}
