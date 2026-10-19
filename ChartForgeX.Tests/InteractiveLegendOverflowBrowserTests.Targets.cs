using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveLegendOverflowBrowserTests {
    [BrowserTheory]
    [Trait("BrowserSuite", "Compatibility")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OmittedPointAndNamedNodeCommandsRetainTheirNativeTargetIdentity(bool hierarchy) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(680, 380).WithLegend().WithPointLegend().WithLegendPosition(ChartLegendPosition.Right)
            .WithLegendBudget(maximumRows: 1).WithTheme(VisualTheme.Graphite());
        if (hierarchy) chart.AddSeries(ChartSeries.CreateTreemap(new ChartSeriesId("entries"), "Entries",
            Enumerable.Range(0, 8).Select(index => new ChartHierarchyItem("node-" + index, "Repeated name", value: index + 1))));
        else {
            var series = ChartSeries.CreateBar(new ChartSeriesId("entries"), "Entries",
                Enumerable.Range(0, 8).Select(index => new ChartPoint(index, index * 2)));
            for (var index = 0; index < 8; index++) series.WithObservationId(index, new ChartObservationId("item-" + index));
            chart.AddSeries(series);
        }
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit), 390, 900);
        var page = session.Page;
        await page.EvaluateAsync("""
            ()=>{
                window.targetLegendEvents=[];
                document.querySelector('.cfx-interactive-chart').addEventListener('cfxseries',event=>targetLegendEvents.push(event.detail));
            }
            """);
        await page.Locator(Panel + " summary").ClickAsync();
        var mute = page.Locator(Rows + " [data-cfx-legend-action=mute]").First;
        await mute.ClickAsync();
        Assert.Equal(hierarchy ? "node" : "point", await page.EvaluateAsync<string>("()=>targetLegendEvents[0].targetKind"));
        if (hierarchy) Assert.Equal("node-0", await page.EvaluateAsync<string>("()=>targetLegendEvents[0].targetId"));
        else Assert.Equal("item-0", await page.EvaluateAsync<string>("()=>targetLegendEvents[0].observationId"));
        var target = hierarchy ? ".cfx-stage [data-cfx-target-kind=node][data-cfx-target-id=node-0]"
            : ".cfx-stage g[data-cfx-role=point][data-cfx-observation-id=item-0]";
        Assert.True(await page.Locator(target).EvaluateAsync<bool>("node=>node.classList.contains('cfx-series-muted')"));
        Assert.Equal(1, await page.Locator(".cfx-stage [data-cfx-target-kind].cfx-series-muted").CountAsync());
        await page.Locator(Rows + " [data-cfx-legend-action=isolate]").First.ClickAsync();
        Assert.True(await page.Locator(target).EvaluateAsync<bool>("node=>node.classList.contains('cfx-series-isolated-in')"));
        Assert.Equal(0, await page.Locator(Omitted + "[tabindex]," + Omitted + "[data-cfx-target-kind]," + Omitted + " [data-cfx-browser-hit-area]").CountAsync());
        await CaptureAsync(page, "legend-overflow-target-" + hierarchy);
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    [Trait("BrowserSuite", "Compatibility")]
    public async Task LinkedOverflowCommandsUpdateOffPagePeersWithoutEchoingOrUsingSeriesPositions() {
        if (!Enabled) return;
        string Fragment(string id, bool reverse) => DenseSeries(false, reverse: reverse).ToInteractiveHtmlFragment(options => {
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Interaction.ChartId = id; options.Interaction.GroupName = "legend-peers";
            options.Interaction.Enable(ChartInteractionFeatures.SynchronizedCharts);
        });
        var html = "<!doctype html><html><head></head><body>" + Fragment("first", false) + Fragment("second", true) + "</body></html>";
        await using var session = await OpenAsync(html, 980, 1100);
        var page = session.Page;
        var first = page.Locator(".cfx-interactive-chart[data-cfx-chart-id=first]");
        var second = page.Locator(".cfx-interactive-chart[data-cfx-chart-id=second]");
        await page.EvaluateAsync("""
            ()=>{
                window.peerLegendEvents=[];
                for(const root of document.querySelectorAll('.cfx-interactive-chart'))
                    root.addEventListener('cfxseries',event=>peerLegendEvents.push(root.dataset.cfxChartId));
            }
            """);
        await first.Locator(Panel + " summary").ClickAsync();
        await second.Locator(Panel + " summary").ClickAsync();
        var firstMute = first.Locator("[data-cfx-legend-action=mute]").First;
        await firstMute.ClickAsync();
        Assert.True(await second.Locator(".cfx-stage [data-cfx-role=series][data-cfx-series='12']").EvaluateAsync<bool>(
            "node=>node.classList.contains('cfx-series-muted')"));
        await second.Locator("[data-cfx-legend-page=next]").ClickAsync();
        await second.Locator("[data-cfx-legend-page=next]").ClickAsync();
        var peerMute = second.Locator("[data-cfx-legend-action=mute]").Last;
        Assert.Contains("Service 01", await peerMute.InnerTextAsync());
        Assert.Equal("true", await peerMute.GetAttributeAsync("aria-pressed"));
        await peerMute.ClickAsync();
        Assert.Equal("false", await firstMute.GetAttributeAsync("aria-pressed"));
        Assert.Equal(new[] { "first", "second" }, await page.EvaluateAsync<string[]>("()=>peerLegendEvents"));
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [Trait("BrowserSuite", "Compatibility")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PassiveStateKeysAndDisabledLegendCommandsRemainDescriptive(bool passiveState) {
        if (!Enabled) return;
        Chart chart;
        if (passiveState) {
            var day = new DateTime(2025, 1, 1);
            var states = Enumerable.Range(0, 8).Select(index => new ChartStateCategory("state-" + index, "State " + index,
                ChartColor.FromRgb((byte)(20 + index * 20), 80, 140))).ToArray();
            chart = Chart.Create().WithSize(680, 380).WithLegend().WithLegendPosition(ChartLegendPosition.Right)
                .WithLegendBudget(maximumRows: 1).WithStateCategories(states)
                .AddStateTimelineLane("Lane", new[] { new ChartStateTimelineSegment(day, day.AddHours(1), "state-0") });
        } else chart = DenseSeries(false, count: 8);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            if (!passiveState) options.Interaction.Disable(ChartInteractionFeatures.LegendToggles | ChartInteractionFeatures.KeyboardNavigation);
        }), 390, 900);
        var page = session.Page;
        await page.Locator(Panel + " summary").ClickAsync();
        Assert.Equal(6, await page.Locator(Rows).CountAsync());
        Assert.Equal(0, await page.Locator("[data-cfx-legend-action]").CountAsync());
        Assert.Contains(passiveState ? "State 0" : "Service 01", await page.Locator(Rows).First.InnerTextAsync());
        await page.Locator("[data-cfx-legend-page=next]").ClickAsync();
        Assert.Equal(2, await page.Locator(Rows).CountAsync());
        Assert.Equal(0, await page.Locator(".cfx-stage .cfx-series-muted,.cfx-stage .cfx-series-isolated-out").CountAsync());
        if (!passiveState) Assert.Equal(0, await page.Locator(".cfx-stage [tabindex='0']").CountAsync());
        await CaptureAsync(page, "legend-overflow-descriptive-" + passiveState);
        AssertNoConsoleErrors(session);
    }
}
