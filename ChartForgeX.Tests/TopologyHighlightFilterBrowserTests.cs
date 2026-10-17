using System.Text.Json;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Topology highlight intent survives filters, while only a visible match can dim other items.</summary>
public sealed class TopologyHighlightFilterBrowserTests {
    [BrowserTheory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task FilterVisibilityRefreshesRetainedNodeAndEdgeHighlights(bool force, bool dark) {
        if (!Enabled) return;
        var chart = TopologyChart.Create().WithViewport(900, 400, 20).WithLegend(null)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .AddNode("a", "API", 80, 140).AddNode("b", "Worker", 370, 140).AddNode("c", "Queue", 670, 140)
            .AddEdge("ab", "a", "b", "Dispatch");
        if (force) chart.LayoutMode = TopologyLayoutMode.ForceDirected;
        await using var session = await OpenAsync(new HtmlInteractiveTopologyRenderer().RenderPage(chart,
            new TopologyRenderOptions { EnableHtmlSelectionPanel = false, EnableHtmlForceGraphControls = force }), dark ? 390 : 1440, 720);
        var page = session.Page;
        var prefix = (force ? "force" : "manual") + (dark ? "-dark" : "-light");
        await Command(page, "cfx-topology-highlight", new { nodeIds = new[] { "missing" }, dim = true });
        Assert.Equal(0, await page.Locator("[data-cfx-highlight-muted]").CountAsync());
        Assert.InRange(await ApiOpacity(page), .999, 1.001);

        await Command(page, "cfx-topology-highlight", new { nodeIds = new[] { "b" }, dim = true });
        Assert.InRange(await ApiOpacity(page), 0, .5);
        await Filter(page, force, "API");
        await AssertOnlyApiForeground(page);
        Assert.Equal("true", await page.Locator(Node("b")).GetAttributeAsync("data-cfx-highlighted"));
        await Capture(page, prefix + "-highlight-then-filter");
        await ClearFilter(page, force);
        Assert.InRange(await ApiOpacity(page), 0, .5);
        Assert.NotEqual("none", await page.Locator(Node("b")).EvaluateAsync<string>("node=>getComputedStyle(node).display"));

        await Command(page, "cfx-topology-clear-highlight");
        await Filter(page, force, "API");
        await Command(page, "cfx-topology-highlight", new { nodeIds = new[] { "b" }, dim = true });
        await AssertOnlyApiForeground(page);
        await ClearFilter(page, force);
        Assert.InRange(await ApiOpacity(page), 0, .5);

        await Command(page, "cfx-topology-highlight", new { edgeIds = new[] { "ab" }, dim = true });
        await Filter(page, force, "API");
        await AssertOnlyApiForeground(page);
        Assert.Equal("true", await page.Locator("[data-cfx-role=topology-edge][data-edge-id=ab]").GetAttributeAsync("data-cfx-highlighted"));
        await ClearFilter(page, force);
        Assert.InRange(await ApiOpacity(page), 0, .5);

        // Hiding edges alone removes the foreground too, even while both endpoint nodes remain visible.
        await Command(page, "cfx-topology-set-filter", new { edges = false });
        Assert.InRange(await ApiOpacity(page), .999, 1.001);
        Assert.Equal(0, await page.Locator("[data-cfx-highlight-muted]").CountAsync());
        await Command(page, "cfx-topology-clear-filter");
        Assert.InRange(await ApiOpacity(page), 0, .5);

        await Filter(page, force, "API");
        await Command(page, "cfx-topology-clear-highlight");
        await ClearFilter(page, force);
        Assert.Equal(0, await page.Locator("[data-cfx-highlight-muted],[data-cfx-highlighted=true]").CountAsync());
        Assert.InRange(await ApiOpacity(page), .999, 1.001);
        await Capture(page, prefix + "-cleared-intent");
        AssertNoConsoleErrors(session);
    }

    private static string Node(string id) => "[data-cfx-role=topology-node][data-node-id=" + id + "]";
    private static Task<double> ApiOpacity(IPage page) => page.Locator(Node("a")).EvaluateAsync<double>(
        "node=>{let opacity=1;for(let p=node;p;p=p.parentElement)opacity*=Number(getComputedStyle(p).opacity);return opacity}");
    private static async Task AssertOnlyApiForeground(IPage page) {
        Assert.Equal("none", await page.Locator(Node("b")).EvaluateAsync<string>("node=>getComputedStyle(node).display"));
        Assert.Equal("none", await page.Locator(Node("c")).EvaluateAsync<string>("node=>getComputedStyle(node).display"));
        Assert.Equal(0, await page.Locator("[data-cfx-highlight-muted]").CountAsync());
        Assert.InRange(await ApiOpacity(page), .999, 1.001);
    }
    private static Task Command(IPage page, string name, object? detail = null) => page.Locator(".cfx-topology-wrapper")
        .EvaluateAsync("(root,request)=>root.dispatchEvent(new CustomEvent(request.name,{detail:request.detail||{}}))", new { name, detail });
    private static Task Filter(IPage page, bool force, string query) => Command(page,
        force ? "cfx-topology-force-filter-set" : "cfx-topology-set-filter", new { query });
    private static Task ClearFilter(IPage page, bool force) => force ? Filter(page, true, "") : Command(page, "cfx-topology-clear-filter");
    private static async Task Capture(IPage page, string name) {
        var root = Environment.GetEnvironmentVariable("CFX_V2_CAPTURE_ROOT");
        if (string.IsNullOrWhiteSpace(root)) return;
        var folder = Path.Combine(root, "topology-filter-highlight"); Directory.CreateDirectory(folder);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(folder, name + ".png"), Animations = ScreenshotAnimations.Disabled });
        await File.WriteAllTextAsync(Path.Combine(folder, name + ".json"), JsonSerializer.Serialize(new {
            highlighted = await page.Locator("[data-cfx-highlighted=true]").CountAsync(),
            muted = await page.Locator("[data-cfx-highlight-muted]").CountAsync(), opacity = await ApiOpacity(page)
        }));
    }
}
