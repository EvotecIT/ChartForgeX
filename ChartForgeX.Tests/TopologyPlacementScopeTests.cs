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

    [Fact]
    public void ScopedPlacements_OfOneChart_ShareNoIdsOrStateKeys() {
        var chart = TopologyScenarioPlaybackTests.ReplicationChart();
        var page = chart.ToInteractiveHtmlFragmentWithoutAssets(Options("first")) + chart.ToInteractiveHtmlFragmentWithoutAssets(Options("second"));

        var ids = Regex.Matches(page, "\\sid=\"([^\"]+)\"").Select(match => match.Groups[1].Value).ToList();
        Assert.NotEmpty(ids);
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
        // The runtime keys scenario state in the address by placement, while events keep the chart's own id.
        var placements = Regex.Matches(page, "data-cfx-placement=\"([^\"]+)\"").Select(match => match.Groups[1].Value).ToList();
        Assert.Equal(new[] { "first-replication", "second-replication" }, placements);
        Assert.Equal(2, Regex.Matches(page, "class=\"cfx-topology-wrapper\"[^>]*data-chart-id=\"replication\"").Count);
    }

    [Fact]
    public void UnscopedPlacement_KeepsTheChartIdentityAsItsStateKey() {
        var html = TopologyScenarioPlaybackTests.ReplicationChart().ToInteractiveHtmlFragmentWithoutAssets(Options(null));
        Assert.DoesNotContain("data-cfx-placement", html, StringComparison.Ordinal);
        Assert.Contains("data-chart-id=\"replication\"", html, StringComparison.Ordinal);
    }
}
