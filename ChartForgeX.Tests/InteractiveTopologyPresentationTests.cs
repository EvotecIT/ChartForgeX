using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class InteractiveTopologyPresentationTests {
    [Fact]
    public void MotionPageUsesOnePreparedTopologyAndKeepsScenariosReversible() {
        var chart = CreateTopology();
        var options = Options();
        var motion = TopologyMotionOptions.RoutePulseForScenario("delivery");
        string? svg = null;
        PreparedTopology? basis = null;
        var calls = 0;
        var html = new HtmlInteractiveTopologyRenderer().RenderPresentationPage(chart, prepared => {
            calls++;
            basis = prepared;
            return svg = prepared.WithMotion(motion).ToSvg();
        }, options);

        Assert.Equal(1, calls);
        Assert.NotNull(basis);
        Assert.Contains(svg!, html);
        var document = XDocument.Parse(svg!);
        Assert.Equal(basis.Width, (double)document.Root!.Attribute("width")!, 3);
        Assert.Equal(basis.Height, (double)document.Root.Attribute("height")!, 3);
        Assert.Contains("max-width:" + basis.Width.ToString("0.###", CultureInfo.InvariantCulture) + "px", html);
        Assert.Contains(document.Descendants(), element => element.Name.LocalName == "animateMotion");
        Assert.All(document.Descendants().Where(element => (string?)element.Attribute("data-cfx-motion-kind") == "route-pulse"),
            element => Assert.Equal("none", (string?)element.Attribute("pointer-events")));
        Assert.DoesNotContain(document.Descendants(), element => element.Name.LocalName == "script");
        Assert.Contains(document.Descendants(), element => (string?)element.Attribute("data-node-id") == "observer");
        Assert.Contains(document.Descendants(), element => (string?)element.Attribute("data-scenario-ids") == "delivery");
        Assert.DoesNotContain(document.Descendants(), element =>
            ((string?)element.Attribute("class"))?.Contains("cfx-topology--dimmed", StringComparison.Ordinal) == true);
        Assert.Contains("data-cfx-active-scenario=\"delivery\"", html);
        Assert.Contains("data-cfx-topology-scenario=\"delivery\"", html);
        Assert.Contains("data-cfx-topology-zoom=\"in\"", html);
        Assert.Contains("cfx-topology-select", html);
        Assert.Contains("data-cfx-topology-selection-panel=\"true\"", html);
        var ids = document.Descendants().Attributes("id").Select(attribute => attribute.Value).ToArray();
        Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
        foreach (var reference in document.Descendants().Where(element => element.Name.LocalName == "mpath")) {
            Assert.Contains(((string)reference.Attribute("href")!)[1..], ids);
        }
        Assert.Contains(ids, id => id.Contains("motion-embed", StringComparison.Ordinal));
        Assert.False(options.EnableHtmlInteractions);
        Assert.Equal("delivery", options.ActiveScenarioId);
        Assert.Equal(480, chart.Viewport.Width);
        Assert.Equal(260, chart.Viewport.Height);
        Assert.Equal("Service route", chart.Title);
        Assert.Empty(chart.Edges[0].Waypoints);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MotionFragmentsHonorHostAssetOwnershipAndPreparedIdentity(bool withoutAssets) {
        var renderer = new HtmlInteractiveTopologyRenderer();
        var chart = CreateTopology();
        var options = Options();
        options.CssClassPrefix = "report-route";
        var motion = TopologyMotionOptions.RoutePulseForScenario("delivery");
        var html = withoutAssets
            ? renderer.RenderPresentationFragmentWithoutAssets(chart, prepared => prepared.WithMotion(motion).ToSvg(), options)
            : renderer.RenderPresentationFragment(chart, prepared => prepared.WithMotion(motion).ToSvg(), options);
        Assert.Contains("class=\"report-route-wrapper\"", html);
        Assert.Contains("animateMotion", html);
        Assert.Contains("data-cfx-asset-source=\"" + (withoutAssets ? "host" : "inline") + "\"", html);
        Assert.Equal(!withoutAssets, html.Contains("data-cfx-topology-assets=\"true\"", StringComparison.Ordinal));
        Assert.Equal(!withoutAssets, html.Contains("<script>", StringComparison.Ordinal));
        Assert.Equal("delivery", options.ActiveScenarioId);
    }

    [Theory]
    [InlineData(1024, false, false)]
    [InlineData(1024, false, true)]
    [InlineData(390, true, true)]
    public async Task TopologyPresentationKeepsSelectionAndViewportControlsWorking(int width, bool dark, bool animate) {
        if (!InteractiveChartBrowser.Enabled) return;
        var chart = CreateTopology();
        chart.Theme = dark ? TopologyTheme.Dark() : TopologyTheme.Light();
        var renderer = new HtmlInteractiveTopologyRenderer();
        var html = animate
            ? renderer.RenderPresentationPage(chart,
                prepared => prepared.WithMotion(TopologyMotionOptions.RoutePulseForScenario("delivery")).ToSvg(), Options())
            : renderer.RenderPage(chart, Options(), null);
        await using var session = await InteractiveChartBrowser.OpenAsync(html, width, 640);
        var page = session.Page;
        var wrapper = page.Locator(".cfx-topology-wrapper");
        var node = page.Locator("[data-cfx-role='topology-node'][data-node-id='source']");
        Assert.Equal("delivery", await wrapper.GetAttributeAsync("data-cfx-active-scenario"));
        await page.Locator("[data-cfx-topology-scenario='']").ClickAsync();
        Assert.Null(await wrapper.GetAttributeAsync("data-cfx-active-scenario"));
        await wrapper.EvaluateAsync("wrapper => { window.cfxSelection = null; wrapper.addEventListener('cfx-topology-select', event => window.cfxSelection = event.detail); }");
        await node.ClickAsync();
        Assert.Equal("true", await node.GetAttributeAsync("aria-selected"));
        Assert.Equal("source", await page.EvaluateAsync<string>("() => window.cfxSelection.id"));
        Assert.False(await page.Locator("[data-cfx-topology-selection-panel]").EvaluateAsync<bool>("panel => panel.hidden"));
        await page.Locator("[data-cfx-topology-zoom='in']").ClickAsync();
        Assert.Equal("1.200", await wrapper.GetAttributeAsync("data-cfx-topology-zoom"));
        await page.WaitForFunctionAsync("() => new DOMMatrixReadOnly(getComputedStyle(document.querySelector('.cfx-topology-viewport > svg')).transform).a > 1.15");
        await page.Locator("[data-cfx-topology-reset]").ClickAsync();
        await page.WaitForFunctionAsync("() => Math.abs(new DOMMatrixReadOnly(getComputedStyle(document.querySelector('.cfx-topology-viewport > svg')).transform).a - 1) < 0.01");
        // On a compact page the detail panel covers the node; Escape returns to the topology before panning.
        await page.Keyboard.PressAsync("Escape");
        Assert.True(await page.Locator("[data-cfx-topology-selection-panel]").EvaluateAsync<bool>("panel => panel.hidden"));
        await node.ScrollIntoViewIfNeededAsync();
        var bounds = await node.BoundingBoxAsync();
        Assert.NotNull(bounds);
        await page.Mouse.MoveAsync((float)(bounds.X + bounds.Width / 2), (float)(bounds.Y + bounds.Height / 2));
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync((float)(bounds.X + bounds.Width / 2 + 24), (float)(bounds.Y + bounds.Height / 2 + 18),
            new Microsoft.Playwright.MouseMoveOptions { Steps = 4 });
        await page.Mouse.UpAsync();
        Assert.Equal("24.0", await wrapper.GetAttributeAsync("data-cfx-topology-pan-x"));
        Assert.Null(await wrapper.GetAttributeAsync("data-cfx-topology-dragging"));
        await page.Locator("[data-cfx-topology-reset]").ClickAsync();
        await page.Locator("[data-cfx-topology-scenario='delivery']").ClickAsync();
        Assert.Equal("delivery", await wrapper.GetAttributeAsync("data-cfx-active-scenario"));
        if (animate) {
            await page.EvaluateAsync("() => { const marker = document.querySelector('[data-cfx-role=\"topology-motion-marker\"]'); const box = marker.getBoundingClientRect(); window.cfxMotionX = box.x; window.cfxMotionY = box.y; }");
            await page.WaitForFunctionAsync("() => { const box = document.querySelector('[data-cfx-role=\"topology-motion-marker\"]').getBoundingClientRect(); return Math.abs(box.x - window.cfxMotionX) + Math.abs(box.y - window.cfxMotionY) > 2; }");
        }
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            var name = "topology-" + (animate ? "motion" : "static") + "-interactive-" + width;
            File.WriteAllText(Path.Combine(capture, name + ".html"), html);
            await page.ScreenshotAsync(new Microsoft.Playwright.PageScreenshotOptions {
                Path = Path.Combine(capture, name + ".png"), FullPage = true
            });
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task PointerReleaseOutsideViewportDoesNotResumePanningOnReentry() {
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync(
            new HtmlInteractiveTopologyRenderer().RenderPage(CreateTopology(), Options()), 1024, 720);
        var page = session.Page;
        var wrapper = page.Locator(".cfx-topology-wrapper");
        var viewport = page.Locator(".cfx-topology-viewport");
        var bounds = await viewport.BoundingBoxAsync();
        Assert.NotNull(bounds);
        var y = (float)(bounds.Y + Math.Min(bounds.Height / 2, 120));
        await page.Mouse.MoveAsync((float)bounds.X + 1, y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync((float)bounds.X - 15, y);
        await page.Mouse.UpAsync();
        Assert.Null(await wrapper.GetAttributeAsync("data-cfx-topology-dragging"));
        await page.Mouse.MoveAsync((float)bounds.X + 30, y);
        Assert.Equal("0.0", await wrapper.GetAttributeAsync("data-cfx-topology-pan-x"));
        Assert.Null(await wrapper.GetAttributeAsync("data-cfx-force-moving-edges"));
        var node = page.Locator("[data-cfx-role='topology-node'][data-node-id='source']");
        await node.ClickAsync();
        Assert.Equal("true", await node.GetAttributeAsync("aria-selected"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData("pointercancel")]
    [InlineData("lostpointercapture")]
    [InlineData("blur")]
    public async Task InterruptedViewportDragClearsMovementStateAndPreservesTheNextClick(string interruption) {
        if (!InteractiveChartBrowser.Enabled) return;
        var chart = CreateTopology();
        chart.LayoutMode = TopologyLayoutMode.ForceDirected;
        var options = Options();
        options.EnableHtmlForceGraphControls = true;
        await using var session = await InteractiveChartBrowser.OpenAsync(
            new HtmlInteractiveTopologyRenderer().RenderPage(chart, options), 1024, 720);
        var page = session.Page;
        var wrapper = page.Locator(".cfx-topology-wrapper");
        var viewport = page.Locator(".cfx-topology-viewport");
        var node = page.Locator("[data-cfx-role='topology-node'][data-node-id='source']");
        await viewport.EvaluateAsync("viewport => viewport.addEventListener('pointerdown', event => window.cfxDragPointerId = event.pointerId)");
        var bounds = await node.BoundingBoxAsync();
        Assert.NotNull(bounds);
        await page.Mouse.MoveAsync((float)(bounds.X + bounds.Width / 2), (float)(bounds.Y + bounds.Height / 2));
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync((float)(bounds.X + bounds.Width / 2 + 24), (float)(bounds.Y + bounds.Height / 2 + 18));
        Assert.Equal("true", await wrapper.GetAttributeAsync("data-cfx-topology-dragging"));
        Assert.Equal("true", await wrapper.GetAttributeAsync("data-cfx-force-moving-edges"));
        await viewport.EvaluateAsync("(viewport, interruption) => { if (interruption === 'lostpointercapture') viewport.releasePointerCapture(window.cfxDragPointerId); else if (interruption === 'blur') window.dispatchEvent(new Event('blur')); else viewport.dispatchEvent(new PointerEvent('pointercancel', { pointerId: window.cfxDragPointerId, bubbles: true })); }", interruption);
        await page.Mouse.MoveAsync((float)(bounds.X + bounds.Width / 2 + 28), (float)(bounds.Y + bounds.Height / 2 + 18));
        await page.Mouse.UpAsync();
        Assert.Null(await wrapper.GetAttributeAsync("data-cfx-topology-dragging"));
        Assert.Null(await wrapper.GetAttributeAsync("data-cfx-force-moving-edges"));
        Assert.Equal(0, await page.Locator(".cfx-topology-html-force-moving").CountAsync());
        var endedPan = await wrapper.GetAttributeAsync("data-cfx-topology-pan-x");
        await page.Mouse.MoveAsync((float)(bounds.X + bounds.Width / 2 + 40), (float)(bounds.Y + bounds.Height / 2 + 18));
        Assert.Equal(endedPan, await wrapper.GetAttributeAsync("data-cfx-topology-pan-x"));
        await node.FocusAsync();
        await page.Keyboard.PressAsync("Escape");
        Assert.Null(await node.GetAttributeAsync("aria-selected"));
        await node.ClickAsync();
        Assert.Equal("true", await node.GetAttributeAsync("aria-selected"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    private static TopologyChart CreateTopology() => TopologyChart.Create().WithId("motion-page")
        .WithViewport(480, 260, 20).WithTitle("Service route")
        .AddNode("source", "Source", 30, 70, width: 100, height: 60)
        .AddNode("target", "Target", 280, 180, width: 100, height: 60)
        .AddNode("observer", "Observer", 30, 210, width: 100, height: 60)
        .AddEdge("route", "source", "target", routing: TopologyEdgeRouting.Straight)
        .AddScenario("delivery", "Delivery", scenario => scenario.AddEdgeStep("route").WithSpotlight());

    private static TopologyRenderOptions Options() => new() {
        IncludeLegend = false, EnableHtmlViewportControls = true, EnableHtmlSelectionPanel = true,
        EnableHtmlScenarioControls = true, ActiveScenarioId = "delivery", IdScope = "motion-embed"
    };
}
