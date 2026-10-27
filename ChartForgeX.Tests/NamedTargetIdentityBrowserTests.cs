using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Native item IDs belong to their series when linked charts share interaction state.</summary>
[Trait("BrowserSuite", "Compatibility")]
public sealed class NamedTargetIdentityBrowserTests {
    [BrowserTheory]
    [InlineData("node", "north")]
    [InlineData("link", "flow")]
    public async Task NamedNativeTargetsSynchronizeOnlyWithinTheirExplicitSeries(string kind, string id) {
        if (!Enabled) return;
        var charts = new[] {
            Model("shared-A", "Source"), Model("shared-A", "Intended peer"),
            Model("unrelated-B", "Unrelated explicit peer"), Model(null, "Unrelated automatic peer")
        };
        var html = charts.ToInteractiveHtmlDashboardPage(options => {
            options.Columns = 2;
            options.Interaction.GroupName = "named-target-series-ownership";
            options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Selection
                | ChartInteractionFeatures.KeyboardNavigation | ChartInteractionFeatures.SynchronizedCharts
                | ChartInteractionFeatures.RevealLabels | ChartInteractionFeatures.FocusTrail;
            options.Tooltip.PinOnSelect = false;
        });
        await using var session = await OpenAsync(html, 1280, 1000);
        var page = session.Page;
        var roots = page.Locator(".cfx-interactive-chart");
        var selector = "[data-cfx-target-kind='" + kind + "'][data-cfx-target-id='" + id + "']";
        await page.EvaluateAsync("""
            ({kind,id})=>{
                window.namedControllers=[...document.querySelectorAll('.cfx-interactive-chart')].map(root=>ChartForgeXCharts.get(root));
                window.namedTarget=namedControllers[0].resolveTargets().find(target=>target.targetKind===kind&&target.targetId===id);
                if(!namedTarget)throw new Error('The typed source did not expose the requested native target.');
                window.namedSync=[];
                namedControllers[0].root.addEventListener('cfxsync',event=>{
                    if(['hover','reveal','trail','navigate'].includes(event.detail.action))namedSync.push(event.detail);
                });
            }
            """, new { kind, id });

        var identities = await page.EvaluateAsync<JsonElement>("""
            ({kind,id})=>namedControllers.map(controller=>controller.resolveTargets().filter(target=>target.targetKind===kind&&target.targetId===id))
            """, new { kind, id });
        Assert.Equal(4, identities.GetArrayLength());
        foreach (var targets in identities.EnumerateArray()) {
            var target = Assert.Single(targets.EnumerateArray());
            Assert.Equal(kind, target.GetProperty("targetKind").GetString());
            Assert.Equal(id, target.GetProperty("targetId").GetString());
        }
        Assert.Equal(new[] { "shared-A", "shared-A", "unrelated-B" }, identities.EnumerateArray().Take(3)
            .Select(targets => targets[0].GetProperty("seriesKey").GetString()));
        Assert.Equal("local", identities[3][0].GetProperty("seriesScope").GetString());
        Assert.Equal(new[] { 1, 1, 0, 0 }, await page.EvaluateAsync<int[]>(
            "()=>namedControllers.map(controller=>controller.resolveTargets({targets:[namedTarget]}).length)"));

        // These public commands already use the complete identity; native hover must agree with them.
        await page.EvaluateAsync("""
            ()=>{namedControllers[0].select({targets:[namedTarget]},{sync:true});
                namedControllers[0].highlight({targets:[namedTarget]},{sync:true,dim:true});}
            """);
        Assert.Equal(new[] { 1, 1, 0, 0 }, await page.EvaluateAsync<int[]>(
            "()=>namedControllers.map(controller=>controller.getSelection().length)"));
        Assert.Equal(new[] { 1, 1, 0, 0 }, await page.EvaluateAsync<int[]>(
            "()=>namedControllers.map(controller=>controller.root.querySelectorAll('.cfx-highlighted').length)"));
        await page.EvaluateAsync("()=>{namedControllers[0].clearSelection({sync:true});namedControllers[0].clearHighlight({sync:true});}");

        // Pointer hover does not move browser focus. A foreign identity must preserve this peer's own affordances.
        const string unrelatedSelector = "[data-cfx-target-kind='node'][data-cfx-target-id='south']";
        await FocusWithKeyboardAsync(page, roots.Nth(2).Locator(unrelatedSelector));
        var existingPeer = await PeerNativeState(roots.Nth(2), unrelatedSelector);
        Assert.True(existingPeer.GetProperty("focused").GetBoolean());
        Assert.Single(existingPeer.GetProperty("hovered").EnumerateArray());
        Assert.NotEmpty(existingPeer.GetProperty("trail").EnumerateArray());
        Assert.Single(existingPeer.GetProperty("reveals").EnumerateArray());

        var targetNode = roots.Nth(0).Locator(selector);
        var nativeMark = await targetNode.Locator(kind == "node" ? "[data-cfx-role=sankey-node-mark]" : "[data-cfx-role=sankey-ribbon]").BoundingBoxAsync()
            ?? throw new InvalidOperationException("The named native mark has no painted bounds.");
        await page.Mouse.MoveAsync((float)(nativeMark.X + nativeMark.Width / 2), (float)(nativeMark.Y + nativeMark.Height / 2));
        await AssertLinkedNativeState(page, selector);
        var preservedPeer = await PeerNativeState(roots.Nth(2), unrelatedSelector);
        Assert.Equal(existingPeer.GetRawText(), preservedPeer.GetRawText());
        await page.Mouse.MoveAsync(2, 2);
        var retiredPeer = await PeerNativeState(roots.Nth(2), unrelatedSelector);
        Assert.Equal(existingPeer.GetRawText(), retiredPeer.GetRawText());
        Assert.Equal(new[] { 0, 0, 0 }, await page.EvaluateAsync<int[]>("""
            ()=>[0,1,3].map(index=>namedControllers[index].root.querySelectorAll(
                '.cfx-hovered,.cfx-hover-related,.cfx-hover-series,.cfx-hover-column,.cfx-reveal-label').length)
            """));

        // Moving focus away legitimately closes that peer's readout, while its local trail remains its own.
        // Tab can visit the automatic peer before the requested focus move; retain that legitimate local trail.
        await page.Keyboard.PressAsync("Tab");
        var automaticBeforeFocus = await PeerNativeState(roots.Nth(3), selector);
        await targetNode.FocusAsync();
        Assert.True(await targetNode.EvaluateAsync<bool>("node=>node.getRootNode().activeElement===node&&node.matches(':focus-visible')"));
        await AssertLinkedNativeState(page, selector, allowOwnAutomaticTrail: true);
        var automaticAfterFocus = await PeerNativeState(roots.Nth(3), selector);
        Assert.Equal(automaticBeforeFocus.GetProperty("trail").GetRawText(), automaticAfterFocus.GetProperty("trail").GetRawText());
        Assert.Equal(0, await roots.Nth(2).Locator(".cfx-hovered,.cfx-hover-related,.cfx-hover-series,.cfx-hover-column,.cfx-reveal-label").CountAsync());
        Assert.True(await roots.Nth(2).EvaluateAsync<bool>("""
            root=>[...root.querySelectorAll('.cfx-trail')].every(node=>node.dataset.cfxSeriesKey==='unrelated-B')
            """));
        var actions = await page.EvaluateAsync<string[]>("()=>[...new Set(namedSync.map(detail=>detail.action))]");
        Assert.Contains("hover", actions);
        Assert.Contains("reveal", actions);
        Assert.Contains("trail", actions);
        Assert.True(await page.EvaluateAsync<bool>("""
            ()=>namedSync.every(detail=>detail.target?.seriesKey==='shared-A'
                && (detail.targets||detail.trail||[]).every(target=>target.seriesKey==='shared-A'))
            """));
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            if (!Path.IsPathRooted(captures)) throw new InvalidOperationException("Browser captures require an absolute output path.");
            Directory.CreateDirectory(captures);
            await page.ScreenshotAsync(new() { Path = Path.Combine(captures, "named-target-series-" + kind + ".png"), FullPage = true });
            await File.WriteAllTextAsync(Path.Combine(captures, "named-target-preserved-peer-" + kind + ".json"),
                JsonSerializer.Serialize(new { before = existingPeer, afterHover = preservedPeer, afterForeignClear = retiredPeer,
                    automaticBeforeFocus, automaticAfterFocus }));
        }
        AssertNoConsoleErrors(session);
    }

    private static async Task AssertLinkedNativeState(IPage page, string selector, bool allowOwnAutomaticTrail = false) {
        var state = await page.EvaluateAsync<JsonElement>("""
            selector=>namedControllers.map(controller=>{
                const root=controller.root,node=root.querySelector(selector);
                return {hovered:node.classList.contains('cfx-hovered'),trail:node.classList.contains('cfx-trail'),
                    reveals:root.querySelectorAll('.cfx-reveal-label').length,
                    unrelatedPaint:root.querySelectorAll('.cfx-hovered,.cfx-hover-related,.cfx-hover-series,.cfx-hover-column,.cfx-reveal-label').length,
                    ownSeriesKey:controller.resolveTargets()[0].seriesKey,
                    trails:[...root.querySelectorAll('.cfx-trail')].map(node=>({seriesKey:node.dataset.cfxSeriesKey,scope:node.dataset.cfxSeriesScope}))};
            })
            """, selector);
        foreach (var index in new[] { 0, 1 }) {
            Assert.True(state[index].GetProperty("hovered").GetBoolean());
            Assert.True(state[index].GetProperty("trail").GetBoolean());
            Assert.Equal(1, state[index].GetProperty("reveals").GetInt32());
        }
        Assert.Equal(0, state[3].GetProperty("unrelatedPaint").GetInt32());
        var automaticTrails = state[3].GetProperty("trails");
        if (!allowOwnAutomaticTrail) Assert.Empty(automaticTrails.EnumerateArray());
        foreach (var target in automaticTrails.EnumerateArray()) {
            Assert.Equal(state[3].GetProperty("ownSeriesKey").GetString(), target.GetProperty("seriesKey").GetString());
            Assert.Equal("local", target.GetProperty("scope").GetString());
        }
    }

    private static Task<JsonElement> PeerNativeState(ILocator root, string selector) => root.EvaluateAsync<JsonElement>("""
        (root,selector)=>{
            const identity=node=>[node.dataset.cfxSeriesKey,node.dataset.cfxTargetKind,node.dataset.cfxTargetId];
            return {focused:root.getRootNode().activeElement===root.querySelector(selector),
                hovered:[...root.querySelectorAll('.cfx-hovered')].map(identity),
                trail:[...root.querySelectorAll('.cfx-trail')].map(node=>[...identity(node),node.dataset.cfxTrailIndex]),
                reveals:[...root.querySelectorAll('.cfx-reveal-label')].map(node=>node.textContent)};
        }
        """, selector);

    private static Chart Model(string? seriesId, string name) {
        var nodes = new[] { new ChartNode("north", "North"), new ChartNode("south", "South") };
        var links = new[] { new ChartFlowLink("flow", "north", "south", 7) };
        var chart = Chart.Create().WithSize(560, 300).WithHeader(false).WithLegend(false);
        return seriesId is null ? chart.AddSankey(name, nodes, links)
            : chart.AddSeries(ChartSeries.CreateSankey(new ChartSeriesId(seriesId), name, nodes, links));
    }
}
