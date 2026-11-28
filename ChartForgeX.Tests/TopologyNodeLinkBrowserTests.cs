using ChartForgeX.Interactivity.Html;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects nodes that link somewhere: the reader reaches the link by pointer and by keyboard, whatever groups surround the node.</summary>
public sealed class TopologyNodeLinkBrowserTests {
    // A directory's group membership: tiles in flat domain groups, long names that wrap their captions, and the groups
    // linking to their records.
    internal static TopologyChart Membership() {
        var chart = TopologyChart.Create().Id("membership")
            .Viewport(new TopologyViewport(1360, 520, 24))
            .LayoutMode(TopologyLayoutMode.DenseGrouped).LayoutDirection(TopologyLayoutDirection.LeftToRight)
            .AddAutoGroup("corp", "corp.example").AddAutoGroup("emea", "emea.corp.example");
        chart.DefaultRenderOptions = new TopologyRenderOptions { ReadableDenseLayout = true, NodeDisplayMode = TopologyNodeDisplayMode.Tile, IncludeTileSubtitles = true, IncludeEdgeLabels = false, IncludeStatusBadges = false };
        string[] names = { "Domain Admins", "App-Finance-Admins", "Finance-Leads", "Server Operators", "Tier0-Breakglass", "Tier1-Operators" };
        for (int i = 0; i < names.Length; i++)
            chart.AddAutoNode("g" + i, names[i], groupId: i % 2 == 0 ? "corp" : "emea", subtitle: "Group", href: "#record=groups:g" + i, width: 120, height: 40, symbol: "G");
        for (int i = 1; i < names.Length; i++) chart.AddEdge("e" + i, "g" + (i - 1), "g" + i);
        return chart;
    }

    private static async Task<HtmlTinkerX.HtmlBrowserSession> Open(string page) {
        var session = await HtmlTinkerX.HtmlBrowser.OpenSessionForNavigationAsync(new HtmlTinkerX.HtmlBrowserLaunchOptions {
            Browser = InteractiveChartBrowser.Engine, Headless = true, Timeout = 15000, ViewportWidth = 1200, ViewportHeight = 900,
            BrowserExecutablePath = Environment.GetEnvironmentVariable("CFX_TEST_BROWSER_EXECUTABLE")
        });
        await session.Page.RouteAsync("https://chartforgex.test/**", route => route.FulfillAsync(new() { Body = page, ContentType = "text/html; charset=utf-8" }));
        await session.Page.GotoAsync("https://chartforgex.test/topology.html");
        await session.Page.Locator(".cfx-topology-wrapper[data-cfx-runtime-bound=true]").WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Attached });
        return session;
    }

    [Fact]
    // An unlabelled edge is announced by its endpoints' labels; a node without a label falls back to its id.
    public void UnlabelledEdges_AreNamedByTheirNodesLabels() {
        var chart = TopologyChart.Create().Id("names")
            // A linked node makes the chart explorable, which is when edges carry accessible names.
            .AddAutoNode("site-0-dc-0", "DC01", href: "#record=dcs:dc01").AddAutoNode("site-1-dc-0", "DC02").AddAutoNode("unnamed", "Unnamed")
            .AddEdge("replication", "site-0-dc-0", "site-1-dc-0").AddEdge("orphan", "site-1-dc-0", "unnamed");
        chart.Edges.Single(e => e.Id == "replication").Direction = VisualLinkDirection.Bidirectional;
        var unnamed = chart.Nodes.Single(n => n.Id == "unnamed"); unnamed.ShowLabel = false; unnamed.Label = string.Empty;
        var html = chart.ToInteractiveHtmlFragmentWithoutAssets(new TopologyRenderOptions());
        Assert.Contains("aria-label=\"DC01 to DC02 and DC02 to DC01\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"DC02 and unnamed\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("site-0-dc-0 to", html, StringComparison.Ordinal);
    }

    [Trait("Category", "Browser")]
    [Theory]
    // Enter on a focused linked node selects it and follows its link, as a click does; Space only selects it.
    [InlineData("click", true)]
    [InlineData("Enter", true)]
    [InlineData(" ", false)]
    public async Task ActivatingALinkedNode_FollowsItsLinkByPointerAndEnter(string how, bool follows) {
        if (!InteractiveChartBrowser.Enabled) return;
        var chart = TopologyChart.Create().Id("links")
            .AddNode("dc01", "DC01", 40, 40, href: "#record=dcs:dc01", width: 140).AddNode("dc02", "DC02", 260, 40, href: "#record=dcs:dc02", width: 140)
            .AddEdge("dc01-dc02", "dc01", "dc02");
        await using var session = await Open(chart.ToInteractiveHtmlPage());
        var browser = session.Page;
        var node = browser.Locator("[data-cfx-role=\"topology-node\"][data-node-id=\"dc02\"]");
        await browser.EvaluateAsync("() => document.addEventListener('click', e => { window.__composed = e.composed; }, true)");
        if (how == "click") await node.ClickAsync();
        else { await node.FocusAsync(); await browser.Keyboard.PressAsync(how); }
        // A host that embeds the chart in a shadow root sees keyboard activation as it sees a click.
        if (how == "Enter") Assert.True(await browser.EvaluateAsync<bool>("() => window.__composed === true"));
        await browser.WaitForFunctionAsync("() => document.querySelector('[data-node-id=\"dc02\"][aria-selected=\"true\"]') !== null || document.querySelector('.cfx-topology-wrapper')?.getAttribute('data-cfx-selection-id') === 'dc02'");
        if (follows) await browser.WaitForFunctionAsync("() => location.hash === '#record=dcs:dc02'");
        else { await browser.WaitForTimeoutAsync(200); Assert.Equal(string.Empty, await browser.EvaluateAsync<string>("() => location.hash")); }
    }

    [Trait("Category", "Browser")]
    [Fact]
    // The hit area bridges a tile and its caption without reaching the tile below, even when that tile is drawn first.
    public async Task ALinkedTilesHitArea_LeavesItsNeighbourAlone() {
        if (!InteractiveChartBrowser.Enabled) return;
        var chart = TopologyChart.Create().Id("stack")
            // Overlapping horizontal spans and a gap smaller than the caption band; the upper tile is drawn later.
            .AddNode("lower", "Lower", 80, 98, href: "#record=groups:lower", width: 120, height: 40)
            .AddNode("upper", "Upper", 40, 40, href: "#record=groups:upper", width: 120, height: 40);
        chart.DefaultRenderOptions = new TopologyRenderOptions { NodeDisplayMode = TopologyNodeDisplayMode.Tile, MaxNodeLabelLines = 3 };
        await using var session = await Open(chart.ToInteractiveHtmlPage(chart.DefaultRenderOptions));
        var owner = await session.Page.EvaluateAsync<string>(@"() => {
            const node = document.querySelector('[data-cfx-role=""topology-node""][data-node-id=""lower""]');
            const box = node.querySelector('[data-cfx-role*=""surface""]')?.getBoundingClientRect() || node.getBoundingClientRect();
            return document.elementFromPoint(box.x + box.width / 2, box.y + 2)?.closest('a')?.getAttribute('href') || '';
        }");
        Assert.Equal("#record=groups:lower", owner);
    }

    [Trait("Category", "Browser")]
    [Theory]
    // The gap below a linked tile acts for the tile: it selects it and follows its link, in a new tab when links open
    // there, and takes no pointer once a filter hides the tile.
    [InlineData("select")]
    [InlineData("new tab")]
    [InlineData("filtered")]
    public async Task TheGapBelowALinkedTile_ActsForThatTile(string how) {
        if (!InteractiveChartBrowser.Enabled) return;
        var chart = TopologyChart.Create().Id("gap").AddNode("dc01", "Domain Controller Alpha", 40, 40, href: "#record=dcs:dc01", width: 120, height: 40);
        var options = new TopologyRenderOptions { NodeDisplayMode = TopologyNodeDisplayMode.Tile, OpenLinksInNewTab = how == "new tab" };
        await using var session = await Open(chart.ToInteractiveHtmlPage(options));
        var browser = session.Page;
        var hit = browser.Locator("a[data-cfx-role=\"topology-node-hit-host\"]");
        Assert.Equal("dc01", await hit.GetAttributeAsync("data-cfx-pointer-for"));
        Assert.Equal("-1", await hit.GetAttributeAsync("tabindex"));
        if (how == "new tab") { Assert.Equal("_blank", await hit.GetAttributeAsync("target")); return; }
        if (how == "filtered") {
            await browser.EvaluateAsync("() => document.querySelector('.cfx-topology-wrapper').dispatchEvent(new CustomEvent('cfx-topology-set-filter', { detail: { query: 'nothing matches this' } }))");
            await browser.WaitForFunctionAsync("() => document.querySelector('a[data-cfx-role=\"topology-node-hit-host\"]').getAttribute('pointer-events') === 'none'");
            return;
        }
        var point = await browser.EvaluateAsync<double[]>(@"() => {
            const tile = document.querySelector('[data-cfx-role=""topology-node""] [data-cfx-role*=""surface""]').getBoundingClientRect();
            return [tile.x + tile.width / 2, tile.bottom + 2];
        }");
        await browser.Mouse.ClickAsync((float)point[0], (float)point[1]);
        await browser.WaitForFunctionAsync("() => location.hash === '#record=dcs:dc01' && document.querySelector('.cfx-topology-wrapper').getAttribute('data-cfx-selection-id') === 'dc01'");
    }

    [Trait("Category", "Browser")]
    [Fact]
    public async Task EveryLinkedNode_IsTheTopmostTargetAtItsCentre() {
        if (!InteractiveChartBrowser.Enabled) return;
        var membership = Membership();
        await using var session = await Open(membership.ToInteractiveHtmlPage(membership.DefaultRenderOptions));
        // A node's link is what a pointer reaches on its tile, in the gap below the tile and on its caption.
        var blocked = await session.Page.EvaluateAsync<string[]>(@"() => [...document.querySelectorAll('a[data-cfx-role=""topology-node-host""]')].flatMap(host => {
            const node = host.querySelector('[data-cfx-role=""topology-node""]') || host;
            node.scrollIntoView({ block: 'center' });
            const tile = (node.querySelector('[data-cfx-role*=""surface""]') || node).getBoundingClientRect();
            const caption = [...node.querySelectorAll('text')].map(t => t.getBoundingClientRect()).find(b => b.top >= tile.bottom);
            const points = [[tile.x + tile.width / 2, tile.y + tile.height / 2, 'tile'], [tile.x + tile.width / 2, tile.bottom + 2, 'gap']];
            if (caption) points.push([caption.x + caption.width / 2, caption.y + caption.height / 2, 'caption']);
            return points.flatMap(([x, y, where]) => {
                const top = document.elementFromPoint(x, y);
                return top && top.closest('a')?.getAttribute('href') === host.getAttribute('href') ? [] : [host.getAttribute('href') + ' ' + where + ' reaches ' + (top?.closest('[data-cfx-role]')?.getAttribute('data-cfx-role') || top?.tagName)];
            });
        })");
        Assert.Empty(blocked);
    }
}
