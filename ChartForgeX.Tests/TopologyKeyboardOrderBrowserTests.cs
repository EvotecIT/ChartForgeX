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

    [Fact]
    // A downloaded or static SVG has no runtime: every link keeps its own tab stop.
    public void StaticSvg_KeepsATabStopOnEveryLink() {
        string svg = Chart().ToSvg();
        var links = System.Text.RegularExpressions.Regex.Matches(svg, "<a\\s[^>]*>");
        Assert.Equal(3, links.Count);
        Assert.All(links, link => Assert.Contains("tabindex=\"0\"", link.Value, StringComparison.Ordinal));
    }
}
