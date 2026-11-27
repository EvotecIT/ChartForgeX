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
        Assert.All(placements, key => Assert.Matches("^[a-z0-9-]+-replication$", key));
        Assert.Equal(2, Regex.Matches(page, "class=\"cfx-topology-wrapper\"[^>]*data-chart-id=\"replication\"").Count);
    }

    [Fact]
    public void UnscopedPlacement_KeepsTheChartIdentityAsItsStateKey() {
        var html = TopologyScenarioPlaybackTests.ReplicationChart().ToInteractiveHtmlFragmentWithoutAssets(Options(null));
        Assert.Contains("data-cfx-key=\"replication\"", html, StringComparison.Ordinal);
        Assert.Contains("data-chart-id=\"replication\"", html, StringComparison.Ordinal);
    }

    // A link copied before placements had their own keys names the chart's id; a scoped placement still restores it.
    [Trait("Category", "Browser")]
    [Fact]
    public async Task ScopedPlacement_RestoresAScenarioLinkKeyedByTheChartId() {
        if (!InteractiveChartBrowser.Enabled) return;
        var page = TopologyScenarioPlaybackTests.ReplicationChart().ToInteractiveHtmlPage(Options("left"));
        var session = await HtmlTinkerX.HtmlBrowser.OpenSessionForNavigationAsync(new HtmlTinkerX.HtmlBrowserLaunchOptions {
            Browser = InteractiveChartBrowser.Engine, Headless = true, Timeout = 15000, ViewportWidth = 1000, ViewportHeight = 700,
            BrowserExecutablePath = Environment.GetEnvironmentVariable("CFX_TEST_BROWSER_EXECUTABLE")
        });
        await using (session) {
            await session.Page.RouteAsync("https://chartforgex.test/**", route => route.FulfillAsync(new() { Body = page, ContentType = "text/html; charset=utf-8" }));
            await session.Page.GotoAsync("https://chartforgex.test/topology.html?cfx-replication-scenario=replicate&cfx-replication-scenarioStep=1");
            await session.Page.WaitForFunctionAsync("() => document.querySelector('.cfx-topology-wrapper')?.getAttribute('data-cfx-active-scenario-step') === '1'");
        }
    }
}

