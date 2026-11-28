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
        if (how == "click") await node.ClickAsync();
        else { await node.FocusAsync(); await browser.Keyboard.PressAsync(how); }
        await browser.WaitForFunctionAsync("() => document.querySelector('[data-node-id=\"dc02\"][aria-selected=\"true\"]') !== null || document.querySelector('.cfx-topology-wrapper')?.getAttribute('data-cfx-selection-id') === 'dc02'");
        if (follows) await browser.WaitForFunctionAsync("() => location.hash === '#record=dcs:dc02'");
        else { await browser.WaitForTimeoutAsync(200); Assert.Equal(string.Empty, await browser.EvaluateAsync<string>("() => location.hash")); }
    }

    [Trait("Category", "Browser")]
    [Fact]
    public async Task EveryLinkedNode_IsTheTopmostTargetAtItsCentre() {
        if (!InteractiveChartBrowser.Enabled) return;
        var membership = Membership();
        await using var session = await Open(membership.ToInteractiveHtmlPage(membership.DefaultRenderOptions));
        // A node's link must be what a pointer reaches anywhere on the node: its label, its body, its centre.
        var blocked = await session.Page.EvaluateAsync<string[]>(@"() => [...document.querySelectorAll('a[data-cfx-role=""topology-node-host""]')].flatMap(host => {
            const node = host.querySelector('[data-cfx-role=""topology-node""]') || host;
            node.scrollIntoView({ block: 'center' });
            const box = node.getBoundingClientRect();
            return [[0.5, 0.5], [0.15, 0.5], [0.85, 0.5], [0.5, 0.2], [0.5, 0.8]].flatMap(([x, y]) => {
                const top = document.elementFromPoint(box.x + box.width * x, box.y + box.height * y);
                return top && host.contains(top) ? [] : [host.getAttribute('href') + ' at ' + x + ',' + y + ' reaches ' + (top?.closest('[data-cfx-role]')?.getAttribute('data-cfx-role') || top?.tagName)];
            });
        })");
        Assert.Empty(blocked);
    }
}
