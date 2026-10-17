using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Public notifications cross host boundaries while keeping typed identity and cancellation.</summary>
public sealed class ShadowHostEventsBrowserTests {
    [BrowserTheory]
    [InlineData("open")]
    [InlineData("closed")]
    public async Task NativeCommandsBrushAndUpdateReachOuterShadowHosts(string mode) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(660, 360).WithHeader(false)
            .WithXAxisBounds(0, 2).WithYAxisBounds(0, 30)
            .AddSeries(ChartSeries.CreateLine(new ChartSeriesId("readings"), "Readings",
                new[] { new ChartPoint(0, 10), new ChartPoint(1, 20), new ChartPoint(2, 15) })
                .WithObservationIds(new[] { new ChartObservationId("north"), new ChartObservationId("central"), new ChartObservationId("south") }));
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            options.Interaction.ChartId = "shadow-native";
            options.Interaction.Features = ChartInteractionFeatures.Selection | ChartInteractionFeatures.Brush
                | ChartInteractionFeatures.SynchronizedCharts | ChartInteractionFeatures.CompareMarkers;
            options.Interaction.GroupName = "shadow-native-group";
            options.Interaction.BrushAxis = ChartBrushAxis.X;
        }), 900, 700);
        var page = session.Page;
        var pageErrors = new List<string>();
        page.PageError += (_, error) => pageErrors.Add(error);
        await InstallBoundary(page, mode, ".cfx-interactive-chart", new[] {
            "cfxselect", "cfxselectionchange", "cfxcompare", "cfxsync", "cfxhighlight", "cfxhighlightclear",
            "cfxbrush", "cfxbrushclear", "cfxupdated"
        });
        var silent = await page.EvaluateAsync<int>("""
            () => {
                const api=ChartForgeXCharts.get(shadowEventRoot);
                api.select({observationIds:['north']});api.clearSelection();
                api.highlight({seriesKeys:['readings']});api.clearHighlight();
                return shadowEventLog.length;
            }
            """);
        Assert.Equal(0, silent);
        await page.EvaluateAsync("""
            () => {
                const root=shadowEventRoot,api=ChartForgeXCharts.get(root);
                api.select({observationIds:['north']},{emit:true,sync:true});
                api.highlight({seriesKeys:['readings']},{emit:true});api.clearHighlight({emit:true});
                const replacement=root.cloneNode(true);replacement.removeAttribute('data-cfx-runtime-bound');
                api.update(replacement);
            }
            """);
        var commands = await Snapshot(page);
        AssertBoundary(commands, mode, new[] { "cfxselect", "cfxselectionchange", "cfxcompare", "cfxsync", "cfxhighlight", "cfxhighlightclear", "cfxupdated" });
        var selected = commands.GetProperty("events").EnumerateArray().Single(item =>
            item.GetProperty("scope").GetString() == "ancestor" && item.GetProperty("name").GetString() == "cfxselect");
        Assert.Equal("shadow-native", selected.GetProperty("detail").GetProperty("chartId").GetString());
        Assert.Equal("north", selected.GetProperty("detail").GetProperty("target").GetProperty("observationId").GetString());
        Assert.Equal(10, selected.GetProperty("detail").GetProperty("target").GetProperty("numericValue").GetDouble());

        var positions = await page.EvaluateAsync<double[]>("""
            () => {
                const root=shadowEventRoot,p=JSON.parse(root.dataset.cfxPreparedChart).dataProjection.plotBounds;
                root.querySelector('[data-cfx-mode-button=brush]').click();
                const m=root.querySelector('.cfx-stage svg').getScreenCTM();
                const a=new DOMPoint(p.x+p.width*.2,p.y+p.height*.3).matrixTransform(m);
                const b=new DOMPoint(p.x+p.width*.8,p.y+p.height*.7).matrixTransform(m);
                return [a.x,a.y,b.x,b.y];
            }
            """);
        await page.Mouse.MoveAsync((float)positions[0], (float)positions[1]); await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync((float)positions[2], (float)positions[3], new MouseMoveOptions { Steps = 4 }); await page.Mouse.UpAsync();
        await page.EvaluateAsync("()=>ChartForgeXCharts.get(shadowEventRoot).clearBrush({emit:true})");
        var brushed = await Snapshot(page);
        AssertBoundary(brushed, mode, new[] { "cfxbrush", "cfxbrushclear" });
        var brush = brushed.GetProperty("events").EnumerateArray().Single(item =>
            item.GetProperty("scope").GetString() == "ancestor" && item.GetProperty("name").GetString() == "cfxbrush");
        Assert.Equal("shadow-native", brush.GetProperty("detail").GetProperty("chartId").GetString());
        Assert.Equal(2, brush.GetProperty("detail").GetProperty("range").GetProperty("x").GetArrayLength());
        var brushSelection = await page.EvaluateAsync<JsonElement>("()=>ChartForgeXCharts.get(shadowEventRoot).getSelection()");
        Assert.Contains(brushSelection.EnumerateArray(), item => item.GetProperty("observationId").GetString() == "central");
        Assert.False(await page.EvaluateAsync<bool>("()=>!!shadowEventRoot.dataset.cfxBrush"));
        await Capture(page, "native-" + mode, brushed);
        Assert.Empty(pageErrors);
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData("open")]
    [InlineData("closed")]
    public async Task GraphSelectionAndCancelableChangesReachOuterShadowHosts(string mode) {
        if (!Enabled) return;
        var graph = GraphScene.Create("shadow-graph", "Service connections")
            .AddNode("api", "API", node => { node.Fixed = true; node.X = 180; node.Y = 180; })
            .AddNode("worker", "Worker", node => { node.Fixed = true; node.X = 460; node.Y = 180; })
            .AddEdge("work", "api", "worker", "Work");
        graph.Options.Enable(GraphSceneFeatures.IncrementalUpdates | GraphSceneFeatures.Manipulation | GraphSceneFeatures.History);
        graph.Options.Manipulation.EnableEditing();
        await using var session = await OpenAsync(graph.ToGraphExplorerHtmlPage(options => {
            options.PersistThemePreference = false;
            options.Theme = mode == "closed" ? HtmlGraphExplorerTheme.Dark : HtmlGraphExplorerTheme.Light;
        }), 900, 700);
        var page = session.Page;
        var pageErrors = new List<string>();
        page.PageError += (_, error) => pageErrors.Add(error);
        await InstallBoundary(page, mode, ".cfx-graph-explorer", new[] { "cfxgraphselection", "cfxgraphbeforechange", "cfxgraphchange", "cfxgraphpatch" });
        Assert.Equal(0, await page.EvaluateAsync<int>("""
            () => { ChartForgeXGraphExplorer.select(shadowEventRoot,[{role:'graph-node',id:'api'}]);
                ChartForgeXGraphExplorer.clearSelection(shadowEventRoot);return shadowEventLog.length; }
            """));
        var canceled = await page.EvaluateAsync<JsonElement>("""
            () => {
                const root=shadowEventRoot,api=ChartForgeXGraphExplorer;
                api.select(root,[{role:'graph-node',id:'worker'}],{emit:true});
                window.shadowCancel=event=>event.preventDefault();shadowEventAncestor.addEventListener('cfxgraphbeforechange',shadowCancel);
                const result=api.change(root,{upsertNodes:[{id:'api',label:'Changed'}]});
                return JSON.parse(JSON.stringify({result,document:api.get(root),undoCount:root.dataset.cfxGraphUndoCount,events:shadowEventLog}));
            }
            """);
        Assert.False(canceled.GetProperty("result").GetBoolean());
        Assert.Equal("API", canceled.GetProperty("document").GetProperty("nodes").EnumerateArray()
            .Single(item => item.GetProperty("id").GetString() == "api").GetProperty("label").GetString());
        Assert.Equal("0", canceled.GetProperty("undoCount").GetString());
        AssertBoundary(canceled, mode, new[] { "cfxgraphselection", "cfxgraphbeforechange" });
        Assert.DoesNotContain(canceled.GetProperty("events").EnumerateArray(), item => item.GetProperty("name").GetString() == "cfxgraphchange");
        var outer = canceled.GetProperty("events").EnumerateArray().Single(item =>
            item.GetProperty("scope").GetString() == "ancestor" && item.GetProperty("name").GetString() == "cfxgraphselection");
        Assert.Equal("shadow-graph", outer.GetProperty("detail").GetProperty("graphId").GetString());
        Assert.Equal("worker", outer.GetProperty("detail").GetProperty("items")[0].GetProperty("id").GetString());
        Assert.True(canceled.GetProperty("events").EnumerateArray().Single(item =>
            item.GetProperty("scope").GetString() == "ancestor" && item.GetProperty("name").GetString() == "cfxgraphbeforechange").GetProperty("cancelable").GetBoolean());
        await page.EvaluateAsync("""
            () => { shadowEventAncestor.removeEventListener('cfxgraphbeforechange',shadowCancel);
                ChartForgeXGraphExplorer.change(shadowEventRoot,{upsertNodes:[{id:'api',label:'Changed'}]}); }
            """);
        var changed = await Snapshot(page);
        AssertBoundary(changed, mode, new[] { "cfxgraphchange", "cfxgraphpatch" });
        Assert.Equal("1", await page.EvaluateAsync<string>("()=>shadowEventRoot.dataset.cfxGraphUndoCount"));
        await Capture(page, "graph-" + mode, changed);
        Assert.Empty(pageErrors);
        AssertNoConsoleErrors(session);
    }

    private static Task InstallBoundary(IPage page, string mode, string selector, string[] names) => page.EvaluateAsync("""
        ({mode,selector,names}) => {
            const root=document.querySelector(selector),ancestor=document.createElement('main'),host=document.createElement('div');
            root.before(ancestor);ancestor.append(host);host.id='shadow-event-host';
            const shadow=host.attachShadow({mode});
            for(const style of document.querySelectorAll('style'))shadow.append(style.cloneNode(true));
            shadow.append(root);window.shadowEventRoot=root;window.shadowEventAncestor=ancestor;window.shadowEventLog=[];
            for(const [scope,node] of [['root',root],['shadow',shadow],['host',host],['ancestor',ancestor],['document',document]])
                for(const name of names)node.addEventListener(name,event=>shadowEventLog.push({scope,name,composed:event.composed,
                    bubbles:event.bubbles,cancelable:event.cancelable,targetIsHost:event.target===host,
                    pathIncludesRoot:event.composedPath().includes(root),detail:JSON.parse(JSON.stringify(event.detail))}));
        }
        """, new { mode, selector, names });

    private static Task<JsonElement> Snapshot(IPage page) => page.EvaluateAsync<JsonElement>("()=>JSON.parse(JSON.stringify({events:shadowEventLog}))");

    private static void AssertBoundary(JsonElement state, string mode, string[] names) {
        var events = state.GetProperty("events").EnumerateArray().ToArray();
        foreach (var name in names) {
            var rootEvents = events.Where(item => item.GetProperty("name").GetString() == name && item.GetProperty("scope").GetString() == "root").ToArray();
            Assert.NotEmpty(rootEvents);
            foreach (var scope in new[] { "shadow", "host", "ancestor", "document" }) {
                var delivered = events.Where(item => item.GetProperty("name").GetString() == name && item.GetProperty("scope").GetString() == scope).ToArray();
                Assert.Equal(rootEvents.Length, delivered.Length);
                Assert.All(delivered, item => { Assert.True(item.GetProperty("bubbles").GetBoolean()); Assert.True(item.GetProperty("composed").GetBoolean()); });
                if (scope is "host" or "ancestor" or "document") Assert.All(delivered, item => {
                    Assert.True(item.GetProperty("targetIsHost").GetBoolean());
                    Assert.Equal(mode == "open", item.GetProperty("pathIncludesRoot").GetBoolean());
                });
            }
        }
    }

    private static async Task Capture(IPage page, string name, object evidence) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Capture output must be absolute.");
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, "shadow-host-" + name + ".png"), FullPage = true });
        await File.WriteAllTextAsync(Path.Combine(directory, "shadow-host-" + name + ".json"), JsonSerializer.Serialize(evidence));
    }
}
