using System.Text.RegularExpressions;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects a host placing the same topology more than once on one page through <see cref="TopologyRenderOptions.IdScope"/>.</summary>
public sealed class TopologyPlacementScopeTests {
    private static TopologyRenderOptions Options(string? scope) {
        var options = new TopologyRenderOptions { ActiveScenarioId = "replicate", IdScope = scope }
            .Html(html => html.ScenarioPanel()).Html(html => html.ScenarioSequence());
        options.Html.ScenarioUrlState = true;
        return options;
    }

    [Theory]
    // Two scopes, including pairs a sanitized id would merge.
    [InlineData("first", "second")]
    [InlineData("left.panel", "left-panel")]
    [InlineData("Panel", "panel")]
    [InlineData("a_b", "a-b")]
    [InlineData("left", " left ")]
    public void ScopedPlacements_OfOneChart_ShareNoIdsOrStateKeys(string firstScope, string secondScope) {
        var chart = TopologyScenarioPlaybackTests.ReplicationChart();
        var page = chart.ToInteractiveHtmlFragmentWithoutAssets(Options(firstScope)) + chart.ToInteractiveHtmlFragmentWithoutAssets(Options(secondScope));

        var ids = Regex.Matches(page, "\\sid=\"([^\"]+)\"").Select(match => match.Groups[1].Value).ToList();
        Assert.NotEmpty(ids);
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
        // The runtime keys scenario state in the address by placement, while events keep the chart's own id.
        var placements = Regex.Matches(page, "data-cfx-key=\"([^\"]+)\"").Select(match => match.Groups[1].Value).ToList();
        Assert.Equal(2, placements.Distinct(StringComparer.Ordinal).Count());
        Assert.All(placements, key => Assert.Matches("^s\\.[a-z0-9-]+\\.replication$", key));
        Assert.Equal(2, Regex.Matches(page, "class=\"cfx-topology-wrapper\"[^>]*data-chart-id=\"replication\"").Count);
    }

    [Fact]
    public void ScopedAndUnscopedPlacements_WhoseSpellingsMeet_StayApart() {
        var scoped = TopologyScenarioPlaybackTests.ReplicationChart();
        var unscoped = TopologyScenarioPlaybackTests.ReplicationChart().Id("left-replication");
        var page = scoped.ToInteractiveHtmlFragmentWithoutAssets(Options("left")) + unscoped.ToInteractiveHtmlFragmentWithoutAssets(Options(null));
        var keys = Regex.Matches(page, "data-cfx-key=\"([^\"]+)\"").Select(match => match.Groups[1].Value).ToList();
        Assert.Equal(2, keys.Distinct(StringComparer.Ordinal).Count());
        var ids = Regex.Matches(page, "\\sid=\"([^\"]+)\"").Select(match => match.Groups[1].Value).ToList();
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
    }

    [Theory]
    // Placements whose keys differ only by where the scope ends, once their ids are made SVG-safe.
    [InlineData("a-2e", "c.d", "a", "2d.c.d")]
    [InlineData("left", "routes", null, "s.left.routes")]
    public void PlacementsWithLookAlikeKeys_ShareNoSequenceIds(string? firstScope, string firstId, string? secondScope, string secondId) {
        var page = TopologyScenarioPlaybackTests.ReplicationChart().Id(firstId).ToInteractiveHtmlFragmentWithoutAssets(Options(firstScope))
            + TopologyScenarioPlaybackTests.ReplicationChart().Id(secondId).ToInteractiveHtmlFragmentWithoutAssets(Options(secondScope));
        var ids = Regex.Matches(page, "\\sid=\"([^\"]+)\"").Select(match => match.Groups[1].Value).ToList();
        Assert.NotEmpty(ids);
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
        var keys = Regex.Matches(page, "data-cfx-key=\"([^\"]+)\"").Select(match => match.Groups[1].Value).ToList();
        Assert.Equal(2, keys.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void UnscopedPlacement_KeepsTheChartIdentityAsItsStateKey() {
        var html = TopologyScenarioPlaybackTests.ReplicationChart().ToInteractiveHtmlFragmentWithoutAssets(Options(null));
        Assert.Contains("data-cfx-key=\"c.replication\"", html, StringComparison.Ordinal);
        Assert.Contains("data-chart-id=\"replication\"", html, StringComparison.Ordinal);
    }

    // A link copied before placements had their own keys names the chart's id; a scoped placement still restores it, moves
    // its state to the placement's keys, and once the reader clears it, a reload leaves it cleared, authored default or not.
    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(null)]
    [InlineData("replicate")]
    public async Task ScopedPlacement_RestoresAScenarioLinkKeyedByTheChartId(string? authored) {
        if (!InteractiveChartBrowser.Enabled) return;
        var options = new TopologyRenderOptions { IdScope = "left", ActiveScenarioId = authored }.Html(html => html.ScenarioPanel());
        options.Html.ScenarioUrlState = true; options.Html.ScenarioControls = true;
        var page = TopologyScenarioPlaybackTests.ReplicationChart().ToInteractiveHtmlPage(options);
        var session = await HtmlTinkerX.HtmlBrowser.OpenSessionForNavigationAsync(new HtmlTinkerX.HtmlBrowserLaunchOptions {
            Browser = InteractiveChartBrowser.Engine, Headless = true, Timeout = 15000, ViewportWidth = 1000, ViewportHeight = 700,
            BrowserExecutablePath = Environment.GetEnvironmentVariable("CFX_TEST_BROWSER_EXECUTABLE")
        });
        await using (session) {
            var browser = session.Page;
            await browser.RouteAsync("https://chartforgex.test/**", route => route.FulfillAsync(new() { Body = page, ContentType = "text/html; charset=utf-8" }));
            await browser.GotoAsync("https://chartforgex.test/topology.html?cfx-replication-scenario=replicate&cfx-replication-scenarioStep=1");
            await browser.WaitForFunctionAsync("() => document.querySelector('.cfx-topology-wrapper')?.getAttribute('data-cfx-active-scenario-step') === '1'");
            Assert.Contains("cfxk-s.left.replication-scenario=replicate", Uri.UnescapeDataString(browser.Url), StringComparison.Ordinal);
            await browser.Locator("[data-cfx-topology-scenario='']").First.ClickAsync();
            // The placement now keeps an empty key of its own, and the older link stays for anyone else reading it.
            await browser.WaitForFunctionAsync("() => new URL(location.href).searchParams.get('cfxk-s.left.replication-scenario') === ''");
            await browser.ReloadAsync();
            await browser.Locator(".cfx-topology-wrapper[data-cfx-runtime-bound=true]").WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Attached });
            Assert.True(string.IsNullOrEmpty(await browser.Locator(".cfx-topology-wrapper").GetAttributeAsync("data-cfx-active-scenario")));
        }
    }

    // Fragments rendered before placements had keys of their own, served with the current runtime, keep each chart's
    // scenario under its own chart id rather than sharing one key.
    [Trait("Category", "Browser")]
    [Fact]
    public async Task OlderFragmentsWithoutPlacementKeys_KeepTheirOwnScenarios() {
        if (!InteractiveChartBrowser.Enabled) return;
        var options = new TopologyRenderOptions().Html(html => html.ScenarioPanel());
        options.Html.ScenarioUrlState = true; options.Html.ScenarioControls = true;
        static string Older(string html) => Regex.Replace(html, "\\sdata-cfx-key=\"[^\"]*\"", string.Empty);
        string body = Older(TopologyScenarioPlaybackTests.ReplicationChart().Id("first").ToInteractiveHtmlPage(options));
        string second = Older(TopologyScenarioPlaybackTests.ReplicationChart().Id("second").ToInteractiveHtmlFragment(options));
        string page = body.Replace("</body>", second + "</body>", StringComparison.Ordinal);
        Assert.DoesNotContain("data-cfx-key=", page, StringComparison.Ordinal);
        var session = await HtmlTinkerX.HtmlBrowser.OpenSessionForNavigationAsync(new HtmlTinkerX.HtmlBrowserLaunchOptions {
            Browser = InteractiveChartBrowser.Engine, Headless = true, Timeout = 15000, ViewportWidth = 1000, ViewportHeight = 700,
            BrowserExecutablePath = Environment.GetEnvironmentVariable("CFX_TEST_BROWSER_EXECUTABLE")
        });
        await using (session) {
            var browser = session.Page;
            await browser.RouteAsync("https://chartforgex.test/**", route => route.FulfillAsync(new() { Body = page, ContentType = "text/html; charset=utf-8" }));
            await browser.GotoAsync("https://chartforgex.test/topology.html");
            await browser.Locator("[data-chart-id=\"second\"].cfx-topology-wrapper[data-cfx-runtime-bound=true]").WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Attached });
            await browser.Locator("[data-chart-id=\"first\"].cfx-topology-wrapper [data-cfx-topology-scenario='replicate']").First.ClickAsync();
            await browser.WaitForFunctionAsync("() => new URL(location.href).searchParams.get('cfx-first-scenario') === 'replicate'");
            await browser.ReloadAsync();
            await browser.Locator("[data-chart-id=\"second\"].cfx-topology-wrapper[data-cfx-runtime-bound=true]").WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Attached });
            Assert.Equal("replicate", await browser.Locator("[data-chart-id=\"first\"].cfx-topology-wrapper").GetAttributeAsync("data-cfx-active-scenario"));
            Assert.True(string.IsNullOrEmpty(await browser.Locator("[data-chart-id=\"second\"].cfx-topology-wrapper").GetAttributeAsync("data-cfx-active-scenario")));
        }
    }

    // A plain link from before charts had keys of their own opens every placement that has no key of its own in the address,
    // not only the first to start; a placement that clears its scenario then keeps it cleared on reload.
    [Trait("Category", "Browser")]
    [Fact]
    public async Task PlainScenarioLinks_OpenEveryPlacement() {
        if (!InteractiveChartBrowser.Enabled) return;
        TopologyRenderOptions Scoped(string scope) {
            var options = new TopologyRenderOptions { IdScope = scope }.Html(html => html.ScenarioPanel());
            options.Html.ScenarioUrlState = true; options.Html.ScenarioControls = true;
            return options;
        }
        string body = TopologyScenarioPlaybackTests.ReplicationChart().ToInteractiveHtmlPage(Scoped("left"));
        string second = TopologyScenarioPlaybackTests.ReplicationChart().ToInteractiveHtmlFragment(Scoped("right"));
        string page = body.Replace("</body>", second + "</body>", StringComparison.Ordinal);
        var session = await HtmlTinkerX.HtmlBrowser.OpenSessionForNavigationAsync(new HtmlTinkerX.HtmlBrowserLaunchOptions {
            Browser = InteractiveChartBrowser.Engine, Headless = true, Timeout = 15000, ViewportWidth = 1000, ViewportHeight = 700,
            BrowserExecutablePath = Environment.GetEnvironmentVariable("CFX_TEST_BROWSER_EXECUTABLE")
        });
        await using (session) {
            var browser = session.Page;
            await browser.RouteAsync("https://chartforgex.test/**", route => route.FulfillAsync(new() { Body = page, ContentType = "text/html; charset=utf-8" }));
            await browser.GotoAsync("https://chartforgex.test/topology.html?scenario=replicate&scenarioStep=1");
            foreach (var key in new[] { "s.left.replication", "s.right.replication" })
                await browser.WaitForFunctionAsync("key => document.querySelector(`[data-cfx-key=\"${key}\"]`)?.getAttribute('data-cfx-active-scenario-step') === '1'", key);
            await browser.Locator("[data-cfx-key=\"s.left.replication\"] [data-cfx-topology-scenario='']").First.ClickAsync();
            await browser.WaitForFunctionAsync("() => new URL(location.href).searchParams.get('cfxk-s.left.replication-scenario') === ''");
            await browser.ReloadAsync();
            await browser.Locator("[data-cfx-key=\"s.right.replication\"][data-cfx-runtime-bound=true]").WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Attached });
            Assert.True(string.IsNullOrEmpty(await browser.Locator("[data-cfx-key=\"s.left.replication\"]").GetAttributeAsync("data-cfx-active-scenario")));
            Assert.Equal("replicate", await browser.Locator("[data-cfx-key=\"s.right.replication\"]").GetAttributeAsync("data-cfx-active-scenario"));
        }
    }

    // A chart whose id spells another placement's key never reads that placement's address state as its own older link.
    [Trait("Category", "Browser")]
    [Fact]
    public async Task ChartIdsThatSpellAPlacementKey_LeaveItsAddressStateAlone() {
        if (!InteractiveChartBrowser.Enabled) return;
        TopologyRenderOptions UrlOptions(string? scope) {
            var options = new TopologyRenderOptions { IdScope = scope }.Html(html => html.ScenarioPanel());
            options.Html.ScenarioUrlState = true;
            return options;
        }
        string body = TopologyScenarioPlaybackTests.ReplicationChart().ToInteractiveHtmlPage(UrlOptions("left"));
        string second = TopologyScenarioPlaybackTests.ReplicationChart().Id("s.left.replication").ToInteractiveHtmlFragment(UrlOptions(null));
        string page = body.Replace("</body>", second + "</body>", StringComparison.Ordinal);
        var session = await HtmlTinkerX.HtmlBrowser.OpenSessionForNavigationAsync(new HtmlTinkerX.HtmlBrowserLaunchOptions {
            Browser = InteractiveChartBrowser.Engine, Headless = true, Timeout = 15000, ViewportWidth = 1000, ViewportHeight = 700,
            BrowserExecutablePath = Environment.GetEnvironmentVariable("CFX_TEST_BROWSER_EXECUTABLE")
        });
        await using (session) {
            var browser = session.Page;
            await browser.RouteAsync("https://chartforgex.test/**", route => route.FulfillAsync(new() { Body = page, ContentType = "text/html; charset=utf-8" }));
            await browser.GotoAsync("https://chartforgex.test/topology.html?cfxk-s.left.replication-scenario=replicate&cfxk-s.left.replication-scenarioStep=1");
            await browser.WaitForFunctionAsync("() => document.querySelector('[data-cfx-key=\"s.left.replication\"]')?.getAttribute('data-cfx-active-scenario-step') === '1'");
            Assert.True(string.IsNullOrEmpty(await browser.Locator("[data-chart-id=\"s.left.replication\"].cfx-topology-wrapper").GetAttributeAsync("data-cfx-active-scenario")));
            Assert.Contains("cfxk-s.left.replication-scenarioStep=1", Uri.UnescapeDataString(browser.Url), StringComparison.Ordinal);
            // A plain step belongs to no placement whose own keys name a scenario.
            await browser.GotoAsync("https://chartforgex.test/topology.html?cfxk-s.left.replication-scenario=replicate&scenarioStep=2");
            await browser.Locator("[data-cfx-key=\"s.left.replication\"][data-cfx-runtime-bound=true]").WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Attached });
            Assert.NotEqual("2", await browser.Locator("[data-cfx-key=\"s.left.replication\"]").GetAttributeAsync("data-cfx-active-scenario-step"));
        }
    }
}
