using System.Globalization;
using System.Text.Json;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>A dense interactive topology opens readable, navigates by pointer and keyboard, and its find bar focuses matches.</summary>
public sealed class TopologyLargeNavigationBrowserTests {
    // Node captions inside the viewport and their smallest on-screen size.
    private const string CaptionFacts = @"() => {
        const viewport = document.querySelector('.cfx-topology-viewport').getBoundingClientRect();
        const sizes = [...document.querySelectorAll('[data-cfx-role=""topology-node-label""]')]
            .filter(label => { const box = label.getBoundingClientRect(); return box.width > 0 && box.left >= viewport.left && box.right <= viewport.right && box.top >= viewport.top && box.bottom <= viewport.bottom; })
            .map(label => { const matrix = label.getScreenCTM(); return parseFloat(getComputedStyle(label.querySelector('text')).fontSize) * Math.hypot(matrix.a, matrix.b); });
        return { count: sizes.length, smallest: sizes.length ? Math.min(...sizes) : 0, height: viewport.height };
    }";

    [BrowserTheory]
    [InlineData(1024, false)]
    [InlineData(375, true)]
    public async Task DenseTopologyOpensReadableAndFindFocusesMatches(int width, bool dark) {
        if (!InteractiveChartBrowser.Enabled) return;
        var chart = DenseEstate().WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        var options = new TopologyRenderOptions { ReadableDenseLayout = true, NodeDisplayMode = TopologyNodeDisplayMode.Tile, EnableHtmlSelectionPanel = true }.WithHtmlFindControls();
        var html = new HtmlInteractiveTopologyRenderer().RenderPage(chart, options);
        await using var session = await InteractiveChartBrowser.OpenAsync(html, width, 820);
        var page = session.Page;
        var wrapper = page.Locator(".cfx-topology-wrapper");
        var viewport = page.Locator(".cfx-topology-viewport");
        await page.WaitForFunctionAsync("() => document.querySelector('.cfx-topology-wrapper').hasAttribute('data-cfx-readable-zoom')");
        await Settle(page);

        // Opens magnified in a bounded frame rather than shrinking every caption to fit the column.
        var opening = await page.EvaluateAsync<JsonElement>(CaptionFacts);
        Assert.True(opening.GetProperty("count").GetInt32() > 0, opening.GetRawText());
        Assert.True(opening.GetProperty("smallest").GetDouble() >= 9.95, opening.GetRawText());
        Assert.True(opening.GetProperty("height").GetDouble() <= 820, opening.GetRawText());
        // Only the plot zooms: the heading is lifted into HTML at its normal size and fits the page.
        var heading = await page.EvaluateAsync<JsonElement>(@"() => {
            const heading = document.querySelector('[data-cfx-topology-heading]'), box = heading.getBoundingClientRect();
            const drawn = document.querySelector('.cfx-topology-viewport > svg [data-cfx-role=""frame-heading""]');
            return { title: heading.querySelector('[class$=""heading__title""]').textContent, inside: box.left >= 0 && box.right <= innerWidth,
                titleSize: parseFloat(getComputedStyle(heading.querySelector('[class$=""heading__title""]')).fontSize), drawnHidden: getComputedStyle(drawn).visibility === 'hidden' };
        }");
        Assert.Equal("Service estate", heading.GetProperty("title").GetString());
        Assert.True(heading.GetProperty("inside").GetBoolean() && heading.GetProperty("drawnHidden").GetBoolean(), heading.GetRawText());
        Assert.InRange(heading.GetProperty("titleSize").GetDouble(), 12, 24);
        var readableZoom = double.Parse((await wrapper.GetAttributeAsync("data-cfx-topology-zoom"))!, CultureInfo.InvariantCulture);
        Assert.True(readableZoom > 1, readableZoom.ToString(CultureInfo.InvariantCulture));

        // Fit is the overview; reset returns to the readable opening view.
        await page.Locator("[data-cfx-topology-fit]").ClickAsync();
        Assert.True(double.Parse((await wrapper.GetAttributeAsync("data-cfx-topology-zoom"))!, CultureInfo.InvariantCulture) < readableZoom);
        await page.Locator("[data-cfx-topology-reset]").ClickAsync();
        Assert.Equal(readableZoom.ToString("0.000", CultureInfo.InvariantCulture), await wrapper.GetAttributeAsync("data-cfx-topology-zoom"));

        // Keyboard: the viewport is a focus target that zooms with + and pans with arrows.
        await viewport.FocusAsync();
        await page.Keyboard.PressAsync("+");
        Assert.True(double.Parse((await wrapper.GetAttributeAsync("data-cfx-topology-zoom"))!, CultureInfo.InvariantCulture) > readableZoom);
        var panX = await wrapper.GetAttributeAsync("data-cfx-topology-pan-x");
        await page.Keyboard.PressAsync("ArrowRight");
        Assert.NotEqual(panX, await wrapper.GetAttributeAsync("data-cfx-topology-pan-x"));
        await page.Keyboard.PressAsync("0");
        Assert.True(double.Parse((await wrapper.GetAttributeAsync("data-cfx-topology-zoom"))!, CultureInfo.InvariantCulture) < readableZoom);

        // Find highlights the match in context, brings it into a readable view, and Enter selects and focuses it.
        var target = page.Locator("[data-cfx-role='topology-node'][data-node-id='site-11-04']");
        await page.Locator("[data-cfx-topology-find]").FillAsync("Database 11");
        await Settle(page);
        Assert.Equal("1", await wrapper.GetAttributeAsync("data-cfx-find-count"));
        Assert.Equal("true", await target.GetAttributeAsync("data-cfx-highlighted"));
        Assert.Contains("1", await page.Locator("[data-cfx-topology-find-summary]").TextContentAsync());
        Assert.True(await page.Locator("[data-cfx-role='topology-node'][data-node-id='site-01-04']").EvaluateAsync<bool>("node => node.classList.contains('cfx-topology-html-muted')"));
        var revealed = await target.EvaluateAsync<JsonElement>(@"node => {
            const box = node.getBoundingClientRect(), view = document.querySelector('.cfx-topology-viewport').getBoundingClientRect();
            const label = node.querySelector('[data-cfx-role=""topology-node-label""]'), matrix = label.getScreenCTM();
            return { inside: box.left >= view.left && box.right <= view.right && box.top >= view.top && box.bottom <= view.bottom,
                caption: parseFloat(getComputedStyle(label.querySelector('text')).fontSize) * Math.hypot(matrix.a, matrix.b) };
        }");
        Assert.True(revealed.GetProperty("inside").GetBoolean(), revealed.GetRawText());
        Assert.True(revealed.GetProperty("caption").GetDouble() >= 9.95, revealed.GetRawText());
        await page.Locator("[data-cfx-topology-find]").PressAsync("Enter");
        await Settle(page);
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        Assert.Equal("site-11-04", await page.EvaluateAsync<string>("() => document.activeElement.getAttribute('data-node-id')"));
        var focusedCaption = await page.EvaluateAsync<JsonElement>(CaptionFacts);
        Assert.True(focusedCaption.GetProperty("smallest").GetDouble() >= 9.95, focusedCaption.GetRawText());

        await page.Locator("[data-cfx-topology-find]").FillAsync(string.Empty);
        Assert.Null(await wrapper.GetAttributeAsync("data-cfx-find-count"));
        Assert.Null(await target.GetAttributeAsync("data-cfx-highlighted"));
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            await page.ScreenshotAsync(new Microsoft.Playwright.PageScreenshotOptions { Path = Path.Combine(capture, "topology-large-navigation-" + width + (dark ? "-dark" : "-light") + ".png"), FullPage = true });
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task FitOpeningViewKeepsTheWholeDenseDrawingInView() {
        if (!InteractiveChartBrowser.Enabled) return;
        var options = new TopologyRenderOptions { ReadableDenseLayout = true, NodeDisplayMode = TopologyNodeDisplayMode.Tile, EnableHtmlViewportControls = true }
            .WithHtmlOpeningView(TopologyHtmlOpeningView.Fit);
        var html = new HtmlInteractiveTopologyRenderer().RenderPage(DenseEstate(), options);
        await using var session = await InteractiveChartBrowser.OpenAsync(html, 375, 820);
        var page = session.Page;
        await page.WaitForFunctionAsync("() => document.querySelector('.cfx-topology-wrapper').hasAttribute('data-cfx-runtime-bound')");
        await Settle(page);
        var wrapper = page.Locator(".cfx-topology-wrapper");
        // The host asked for the overview, so the drawing stays fitted where the default would open magnified.
        Assert.Null(await wrapper.GetAttributeAsync("data-cfx-readable-zoom"));
        Assert.Equal(0, await page.Locator("[data-cfx-topology-heading]").CountAsync());
        var fits = await page.EvaluateAsync<bool>(@"() => {
            const view = document.querySelector('.cfx-topology-viewport').getBoundingClientRect(), svg = document.querySelector('.cfx-topology-viewport > svg').getBoundingClientRect();
            return svg.width <= view.width + 1 && svg.left >= view.left - 1;
        }");
        Assert.True(fits);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task ThemeAdaptiveCardLeavesTheSurfaceToItsHost() {
        if (!InteractiveChartBrowser.Enabled) return;
        // A host that supplies the colour roles owns the surface; the card must not paint the drawing's literal canvas.
        var options = new TopologyRenderOptions { ReadableDenseLayout = true, NodeDisplayMode = TopologyNodeDisplayMode.Tile,
            EnableHtmlViewportControls = true, SvgColorVariables = SvgColorVariables.SharedRoles() };
        var html = new HtmlInteractiveTopologyRenderer().RenderPage(DenseEstate(), options);
        await using var session = await InteractiveChartBrowser.OpenAsync(html, 375, 820);
        var page = session.Page;
        await page.WaitForFunctionAsync("() => document.querySelector('.cfx-topology-wrapper').hasAttribute('data-cfx-readable-zoom')");
        Assert.Equal("rgba(0, 0, 0, 0)", await page.Locator("[data-cfx-topology-card]").EvaluateAsync<string>("card => getComputedStyle(card).backgroundColor"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    // The viewport eases its transform over 120 ms; geometry is read once it rests.
    private static Task Settle(Microsoft.Playwright.IPage page) => page.EvaluateAsync("() => Promise.all(document.querySelector('.cfx-topology-viewport > svg').getAnimations().map(animation => animation.finished.catch(() => {})))");

    private static TopologyChart DenseEstate() {
        var roles = new[] { ("Gateway", TopologyNodeKind.Gateway), ("API", TopologyNodeKind.Service), ("Worker", TopologyNodeKind.Process),
            ("Queue", TopologyNodeKind.Queue), ("Database", TopologyNodeKind.Database), ("Cache", TopologyNodeKind.Storage) };
        var chart = TopologyChart.Create().WithId("dense-estate").WithTitle("Service estate")
            .WithViewport(1600, 900, 24).WithLayout(TopologyLayoutMode.DenseGrouped, TopologyLayoutDirection.LeftToRight);
        for (var site = 1; site <= 12; site++) {
            var group = "site-" + site.ToString("00", CultureInfo.InvariantCulture);
            chart.AddAutoGroup(group, "Site " + site.ToString(CultureInfo.InvariantCulture), TopologyHealthStatus.Healthy);
            for (var node = 0; node < 6; node++) {
                var (name, kind) = roles[node];
                chart.AddAutoNode(group + "-" + node.ToString("00", CultureInfo.InvariantCulture), name + " " + site.ToString("00", CultureInfo.InvariantCulture), kind, TopologyHealthStatus.Healthy, group, width: 96, height: 40);
                if (node > 0) chart.AddEdge(group + "-call-" + node.ToString(CultureInfo.InvariantCulture), group + "-00", group + "-" + node.ToString("00", CultureInfo.InvariantCulture));
            }
            if (site > 1) chart.AddEdge(group + "-uplink", group + "-00", "site-01-00");
        }
        return chart;
    }
}
