using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>
/// Every interactive surface, split into asset files, runs under <c>script-src 'self'; style-src 'self'</c> without a
/// violation and without inline scripts or styles, before and after interaction. Add a surface to <see cref="Fixtures"/>.
/// </summary>
public sealed class ExternalCspBrowserTests {
    private static readonly Dictionary<string, Func<string>> Pages = new() {
        ["chart"] = () => Line().ToInteractiveHtmlPage(options =>
            options.Interaction.Enable(ChartInteractionFeatures.Export | ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan)),
        ["chart adaptive"] = () => Line().ToInteractiveHtmlPage(options => {
            options.Interaction.Enable(ChartInteractionFeatures.Export);
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive; options.Adaptive.Breakpoint = 1200;
        }),
        ["dashboard"] = () => new HtmlInteractiveDashboardRenderer().RenderPage(new[] { Line(), Bars() },
            options => options.Interaction.Enable(ChartInteractionFeatures.Export | ChartInteractionFeatures.SynchronizedCharts)),
        ["topology"] = () => new HtmlInteractiveTopologyRenderer().RenderPage(Topology(),
            new TopologyRenderOptions { Html = { ExportControls = true, ViewportControls = true } }),
        ["flow diagram"] = () => FlowRecipe.Create().ToInteractiveHtmlPage(),
        ["sequence diagram"] = () => SequenceRecipe.Create().ToInteractiveHtmlPage(),
        ["graph explorer"] = () => Graph().ToGraphExplorerHtmlPage(options => options.PersistThemePreference = false),
        ["graph explorer with asset references"] = () => Graph().ToGraphExplorerHtmlPage(options => {
            options.PersistThemePreference = false; options.ExternalAssets = new HtmlAssetReferences(ExternalCspBrowserHarness.AssetPath);
        })
    };

    private static readonly Dictionary<string, string> Roots = new() {
        ["chart"] = ".cfx-interactive-chart", ["chart adaptive"] = ".cfx-interactive-chart", ["dashboard"] = ".cfx-interactive-chart",
        ["topology"] = "[data-chart-id]", ["flow diagram"] = "svg", ["sequence diagram"] = "svg",
        ["graph explorer"] = ".cfx-graph-explorer", ["graph explorer with asset references"] = ".cfx-graph-explorer"
    };

    public static IEnumerable<object[]> Fixtures => Pages.Keys.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void SplitPagesLoadEveryScriptAndStyleFromFiles(string fixture) =>
        ExternalCspBrowserHarness.AssertSourceLoadsFilesOnly(ExternalCspBrowserHarness.Split(Pages[fixture]()));

    [BrowserTheory]
    [MemberData(nameof(Fixtures))]
    public async Task SplitPagesRunUnderAFilesOnlyPolicy(string fixture) {
        if (!InteractiveChartBrowser.Enabled) return;
        var page = ExternalCspBrowserHarness.Split(Pages[fixture]());
        var extra = fixture.EndsWith("with asset references", StringComparison.Ordinal) ? HtmlInteractiveAssetFiles.GraphExplorer() : null;
        await using var session = await ExternalCspBrowserHarness.OpenAsync(page, extra);
        var browser = session.Page;
        if (fixture.StartsWith("graph explorer", StringComparison.Ordinal)) {
            // Physics for a scene this size runs in the same-origin worker file and settles there.
            await browser.WaitForFunctionAsync("() => document.querySelector('.cfx-graph-explorer').dataset.cfxGraphPhysicsThread === 'worker'"
                + " || document.querySelector('.cfx-graph-explorer').dataset.cfxGraphPhysicsState === 'stabilized'", null, new() { Timeout = 15000 });
            await browser.WaitForFunctionAsync("() => document.querySelector('.cfx-graph-explorer').dataset.cfxGraphPhysicsState === 'stabilized'", null, new() { Timeout = 30000 });
            Assert.Equal("worker", await browser.EvaluateAsync<string>("() => document.querySelector('.cfx-graph-explorer').dataset.cfxGraphPhysicsThread"));
        }
        await ExternalCspBrowserHarness.InteractAsync(browser, Roots[fixture]);
        await ExternalCspBrowserHarness.AssertCleanAsync(browser);
    }

    // The harness itself: a runtime style attribute write and an unsplit page both fail it.
    [BrowserFact]
    public async Task HarnessReportsInlineScriptsStylesAndStyleAttributeWrites() {
        if (!InteractiveChartBrowser.Enabled) return;
        var inline = Pages["chart"]();
        var page = ExternalCspBrowserHarness.Split("<p>probe</p>");
        await using var session = await ExternalCspBrowserHarness.OpenAsync(page);
        await session.Page.EvaluateAsync("() => document.querySelector('p').setAttribute('style', 'color:red')");
        var error = await Assert.ThrowsAsync<Xunit.Sdk.TrueException>(() => ExternalCspBrowserHarness.AssertCleanAsync(session.Page));
        Assert.Contains("style-src-attr", error.Message, StringComparison.Ordinal);
        Assert.Throws<Xunit.Sdk.TrueException>(() => ExternalCspBrowserHarness.AssertSourceLoadsFilesOnly(new HtmlExternalPage(inline, Array.Empty<HtmlAssetFile>())));
    }

    private static Chart Line() => Chart.Create().Title("Checks").Size(640, 360).XLabels("Mon", "Tue", "Wed", "Thu")
        .AddLine("Passed", new[] { new ChartPoint(1, 4), new ChartPoint(2, 6), new ChartPoint(3, 5), new ChartPoint(4, 7) })
        .AddLine("Failed", new[] { new ChartPoint(1, 1), new ChartPoint(2, 2), new ChartPoint(3, 1), new ChartPoint(4, 3) });

    private static Chart Bars() => Chart.Create().Title("Domains").Size(640, 360).XLabels("North", "South", "East")
        .AddBar("Controllers", new[] { new ChartPoint(1, 12), new ChartPoint(2, 9), new ChartPoint(3, 15) });

    private static TopologyChart Topology() => TopologyChart.Create().Id("notes").Title("Save a note")
        .LayoutMode(TopologyLayoutMode.Layered).LayoutDirection(TopologyLayoutDirection.LeftToRight)
        .AddNode("app", "Notes app", 0, 0, TopologyNodeKind.Person)
        .AddNode("api", "Notes API", 0, 0, TopologyNodeKind.Server)
        .AddNode("db", "Notes store", 0, 0, TopologyNodeKind.Database)
        .AddEdge("http", "app", "api", "HTTPS").AddEdge("sql", "api", "db", "SQL")
        .AddScenario("save", "Save a note", scenario => scenario.AddEdgeStep("http").AddEdgeStep("sql"));

    private static GraphScene Graph() {
        var scene = GraphScene.Create("services", "Services");
        for (var index = 0; index < 170; index++) scene.AddNode("n" + index, "Service " + index);
        for (var index = 1; index < 170; index++) scene.AddEdge("e" + index, "n" + (index - 1), "n" + index);
        scene.Options.Enable(GraphSceneFeatures.RuntimePhysics | GraphSceneFeatures.Stabilization);
        scene.Options.Physics.Solver = GraphPhysicsSolver.BarnesHut;
        return scene;
    }
}
