using ChartForgeX.Interactivity.Html;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects the topology keyboard model: Tab moves between nodes; arrows reach a node's edges and group.</summary>
public sealed class TopologyKeyboardOrderBrowserTests {
    private static TopologyChart Chart() {
        var chart = TopologyChart.Create().Id("keys")
            .AddGroup("site", "Warsaw", 20, 20, 520, 140)
            .AddNode("dc01", "DC01", 40, 60, groupId: "site", href: "#record=dcs:dc01", width: 120, height: 40)
            .AddNode("dc02", "DC02", 240, 60, groupId: "site", width: 120, height: 40)
            .AddNode("dc03", "DC03", 420, 60, groupId: "site", width: 100, height: 40)
            .AddEdge("replication", "dc01", "dc02").AddEdge("backup", "dc02", "dc03");
        chart.Groups.Single(group => group.Id == "site").Href = "#record=sites:warsaw";
        chart.Edges.Single(edge => edge.Id == "replication").Href = "#record=links:replication";
        return chart;
    }

    private static async Task<HtmlTinkerX.HtmlBrowserSession> Open(string page) {
        var session = await HtmlTinkerX.HtmlBrowser.OpenSessionForNavigationAsync(new HtmlTinkerX.HtmlBrowserLaunchOptions {
            Browser = InteractiveChartBrowser.Engine, Headless = true, Timeout = 15000, ViewportWidth = 1000, ViewportHeight = 700,
            BrowserExecutablePath = Environment.GetEnvironmentVariable("CFX_TEST_BROWSER_EXECUTABLE")
        });
        try {
            await session.Page.RouteAsync("https://chartforgex.test/**", route => route.FulfillAsync(new() { Body = page, ContentType = "text/html; charset=utf-8" }));
            await session.Page.GotoAsync("https://chartforgex.test/topology.html");
            await session.Page.Locator("[data-cfx-interactive=true][data-cfx-runtime-bound=true]").WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Attached });
            return session;
        } catch {
            await session.DisposeAsync();
            throw;
        }
    }

    // What the focused element stands for: the node, edge or group it is or links for.
    private const string Focused = @"() => {
        const active = document.activeElement;
        const owner = active?.closest('[data-cfx-role=""topology-node""],[data-cfx-role=""topology-edge""],[data-cfx-role=""topology-group""]')
            || (active?.matches('a') ? active.querySelector('[data-cfx-role=""topology-node""],[data-cfx-role=""topology-edge""],[data-cfx-role=""topology-group""]') : null);
        return owner ? owner.getAttribute('data-cfx-role').replace('topology-', '') + ':' + (owner.getAttribute('data-node-id') || owner.getAttribute('data-edge-id') || owner.getAttribute('data-group-id')) : '';
    }";

    [Trait("Category", "Browser")]
    [Fact]
    public async Task Tab_MovesBetweenNodesOnly() {
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await Open(Chart().ToInteractiveHtmlPage());
        var browser = session.Page;
        var stops = new List<string>();
        for (int i = 0; i < 8; i++) {
            await browser.Keyboard.PressAsync("Tab");
            var focused = await browser.EvaluateAsync<string>(Focused);
            if (focused.Length > 0 && !stops.Contains(focused)) stops.Add(focused);
        }
        Assert.Equal(new[] { "node:dc01", "node:dc02", "node:dc03" }, stops);
    }

    [Trait("Category", "Browser")]
    [Theory]
    // A linked edge and a linked group are still reached from a node with the arrow keys, and Enter follows their links.
    [InlineData("edge:replication", "#record=links:replication")]
    [InlineData("group:site", "#record=sites:warsaw")]
    public async Task Arrows_ReachLinkedEdgesAndGroups_AndEnterFollowsThem(string target, string hash) {
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await Open(Chart().ToInteractiveHtmlPage());
        var browser = session.Page;
        await browser.Locator("[data-cfx-role=\"topology-node\"][data-node-id=\"dc01\"]").FocusAsync();
        string? reached = null;
        for (int i = 0; i < 6 && reached != target; i++) {
            await browser.Keyboard.PressAsync("ArrowRight");
            reached = await browser.EvaluateAsync<string>(Focused);
        }
        Assert.Equal(target, reached);
        Assert.True(await browser.EvaluateAsync<bool>("() => document.activeElement.matches('a')"), "The element's own link has focus.");
        await browser.Keyboard.PressAsync("Enter");
        await browser.WaitForFunctionAsync("hash => location.hash === hash", hash);
    }

    [Trait("Category", "Browser")]
    [Fact]
    // A parent group no node's arrows reach (its nodes sit in a child group) keeps its own tab stop.
    public async Task ItemsNoNodeReaches_KeepTheirTabStops() {
        if (!InteractiveChartBrowser.Enabled) return;
        var chart = TopologyChart.Create().Id("nested")
            .AddGroup("region", "EMEA", 10, 10, 560, 180).AddGroup("site", "Warsaw", 30, 40, 400, 120)
            .AddNode("dc01", "DC01", 60, 80, groupId: "site", width: 120, height: 40);
        chart.Groups.Single(group => group.Id == "site").ParentGroupId = "region";
        chart.Groups.Single(group => group.Id == "region").Href = "#record=regions:emea";
        await using var session = await Open(chart.ToInteractiveHtmlPage());
        var browser = session.Page;
        var stops = new List<string>();
        for (int i = 0; i < 6; i++) {
            await browser.Keyboard.PressAsync("Tab");
            var focused = await browser.EvaluateAsync<string>(Focused);
            if (focused.Length > 0 && !stops.Contains(focused)) stops.Add(focused);
        }
        Assert.Contains("group:region", stops);
        Assert.Contains("node:dc01", stops);
        Assert.DoesNotContain("group:site", stops);
    }

    [Trait("Category", "Browser")]
    [Fact]
    // Focus that arrives by Tab starts a fresh arrow cycle from the newly focused node.
    public async Task TabbingBackToANode_StartsAFreshArrowCycle() {
        if (!InteractiveChartBrowser.Enabled) return;
        var chart = TopologyChart.Create().Id("chain")
            .AddNode("a", "A", 40, 60, width: 100, height: 40).AddNode("b", "B", 220, 60, width: 100, height: 40).AddNode("c", "C", 400, 60, width: 100, height: 40)
            .AddEdge("ab", "a", "b").AddEdge("bc", "b", "c");
        chart.Edges.Single(edge => edge.Id == "ab").Href = "#record=links:ab";
        chart.Edges.Single(edge => edge.Id == "bc").Href = "#record=links:bc";
        await using var session = await Open(chart.ToInteractiveHtmlPage());
        var browser = session.Page;
        await browser.Locator("[data-cfx-role=\"topology-node\"][data-node-id=\"a\"]").FocusAsync();
        foreach (var key in new[] { "ArrowRight", "Tab", "Shift+Tab", "ArrowLeft", "Enter" }) await browser.Keyboard.PressAsync(key);
        await browser.WaitForFunctionAsync("() => location.hash === '#record=links:bc'");
    }

    [Trait("Category", "Browser")]
    [Fact]
    // When the runtime lets go, the links it took out of the tab order get their tab stops back.
    public async Task Dispose_RestoresTabStops() {
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await Open(Chart().ToInteractiveHtmlPage());
        var browser = session.Page;
        var edgeLink = browser.Locator("a[data-cfx-role=\"topology-edge-host\"]");
        Assert.Equal("-1", await edgeLink.GetAttributeAsync("tabindex"));
        await browser.EvaluateAsync("() => ChartForgeXTopology.dispose(document)");
        Assert.Equal("0", await edgeLink.GetAttributeAsync("tabindex"));
    }

    [Trait("Category", "Browser")]
    [Fact]
    // Working out the tab order on mount grows with the chart, not with its square: doubling a chain of nodes roughly
    // doubles the elements the runtime's queries return while it mounts.
    public async Task MountWork_GrowsLinearlyWithTheChart() {
        if (!InteractiveChartBrowser.Enabled) return;
        async Task<long> Visited(int nodes) {
            var chart = TopologyChart.Create().Id("scale" + nodes);
            for (int i = 0; i < nodes; i++) chart.AddNode("n" + i, "Node " + i, 40 + (i % 40) * 140, 40 + (i / 40) * 90, groupId: null, width: 100, height: 40);
            for (int i = 1; i < nodes; i++) chart.AddEdge("e" + i, "n" + (i - 1), "n" + i);
            var session = await HtmlTinkerX.HtmlBrowser.OpenSessionForNavigationAsync(new HtmlTinkerX.HtmlBrowserLaunchOptions {
                Browser = InteractiveChartBrowser.Engine, Headless = true, Timeout = 60000, ViewportWidth = 1000, ViewportHeight = 700,
                BrowserExecutablePath = Environment.GetEnvironmentVariable("CFX_TEST_BROWSER_EXECUTABLE")
            });
            await using (session) {
                // Counts what every query returns until the chart is bound.
                await session.Page.AddInitScriptAsync("(() => { window.__visited = 0; for (const type of [Element, Document]) { const query = type.prototype.querySelectorAll; type.prototype.querySelectorAll = function (selector) { const found = query.call(this, selector); if (!window.__bound) window.__visited += found.length; return found; }; } })()");
                string page = chart.ToInteractiveHtmlPage();
                await session.Page.RouteAsync("https://chartforgex.test/**", route => route.FulfillAsync(new() { Body = page, ContentType = "text/html; charset=utf-8" }));
                await session.Page.GotoAsync("https://chartforgex.test/topology.html");
                await session.Page.Locator("[data-cfx-interactive=true][data-cfx-runtime-bound=true]").WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Attached, Timeout = 60000 });
                return await session.Page.EvaluateAsync<long>("() => { window.__bound = true; return window.__visited; }");
            }
        }
        long small = await Visited(400), large = await Visited(800);
        Assert.True(large < small * 3, $"400 nodes visited {small} elements, 800 visited {large}.");
    }

    [Fact]
    // A downloaded or static SVG has no runtime: every link keeps its own tab stop.
    public void StaticSvg_KeepsATabStopOnEveryLink() {
        string svg = Chart().ToSvg();
        var links = System.Text.RegularExpressions.Regex.Matches(svg, "<a\\s[^>]*>");
        Assert.Equal(3, links.Count);
        Assert.All(links, link => Assert.Contains("tabindex=\"0\"", link.Value, StringComparison.Ordinal));
    }
}
