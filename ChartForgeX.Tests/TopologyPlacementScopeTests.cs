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

    [Fact]
    public void UnscopedPlacement_KeepsTheChartIdentityAsItsStateKey() {
        var html = TopologyScenarioPlaybackTests.ReplicationChart().ToInteractiveHtmlFragmentWithoutAssets(Options(null));
        Assert.Contains("data-cfx-key=\"c.replication\"", html, StringComparison.Ordinal);
        Assert.Contains("data-chart-id=\"replication\"", html, StringComparison.Ordinal);
    }

    // A link copied before placements had their own keys names the chart's id; a scoped placement still restores it, moves
    // its state to the placement's keys, and once the reader clears it, a reload leaves it cleared.
    [Trait("Category", "Browser")]
    [Fact]
    public async Task ScopedPlacement_RestoresAScenarioLinkKeyedByTheChartId() {
        if (!InteractiveChartBrowser.Enabled) return;
        var options = new TopologyRenderOptions { IdScope = "left" }.Html(html => html.ScenarioPanel());
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
            await browser.Locator("[data-cfx-topology-scenario='']").First.ClickAsync();
            await browser.WaitForFunctionAsync("() => !location.search.includes('scenario')");
            await browser.ReloadAsync();
            await browser.Locator(".cfx-topology-wrapper[data-cfx-runtime-bound=true]").WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Attached });
            Assert.True(string.IsNullOrEmpty(await browser.Locator(".cfx-topology-wrapper").GetAttributeAsync("data-cfx-active-scenario")));
        }
    }
}
