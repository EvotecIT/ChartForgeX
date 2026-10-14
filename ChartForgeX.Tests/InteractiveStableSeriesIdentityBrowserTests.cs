using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractiveStableSeriesIdentityBrowserTests {
    [BrowserFact]
    public async Task NumericSeriesScenarioIdsResolveTheirRetainedSeriesAfterReordering() {
        if (!Enabled) return;
        var numeric = ChartSeries.CreateLine(new ChartSeriesId("0"), "Reading", new[] { new ChartPoint(1, 5), new ChartPoint(2, 8) });
        var other = ChartSeries.CreateLine(new ChartSeriesId("other"), "0", new[] { new ChartPoint(1, 2), new ChartPoint(2, 3) });
        var chart = Chart.Create().WithSize(600, 360).AddSeries(numeric).AddSeries(other).MoveSeries(numeric, 1);
        var html = chart.ToInteractiveHtmlPage(options => {
            options.Interaction.AddScenario("numeric-id", "Numeric ID", scenario => scenario
                .AddSeriesStep(numeric.Id, "Typed ID")
                .AddSeriesStep("0", "String ID"));
        });
        await using var session = await OpenAsync(html, 700, 700);
        var page = session.Page;
        await page.Locator("[data-cfx-scenario='numeric-id']").ClickAsync();
        foreach (var index in new[] { 1, 2 }) {
            await page.Locator("[data-cfx-scenario-step-index='" + index + "']").ClickAsync();
            Assert.Equal("0", await page.Locator(Point(1, 0)).GetAttributeAsync("data-cfx-series-key"));
            Assert.True(await page.Locator(Point(1, 0)).EvaluateAsync<bool>("node => node.classList.contains('cfx-scenario-step-active')"));
            Assert.True(await page.Locator(Point(1, 1)).EvaluateAsync<bool>("node => node.classList.contains('cfx-scenario-step-active')"));
            Assert.Equal(0, await page.Locator("[data-cfx-series='0'].cfx-scenario-active").CountAsync());
            Assert.Equal(0, await page.Locator("[data-cfx-series='0'].cfx-scenario-step-active").CountAsync());
        }
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExplicitSourceIdsSynchronizeAcrossNamesAndOrderingWhileAutomaticIdsStayLocal(bool explicitIds) {
        if (!Enabled) return;
        var first = Chart.Create().WithSize(500, 300).WithTitle("Source").WithPointLegend();
        var second = Chart.Create().WithSize(500, 300).WithTitle("Peer").WithPointLegend();
        var points = new[] { new ChartPoint(1, 5), new ChartPoint(2, 8) };
        if (explicitIds) {
            first.AddSeries(ChartSeries.CreateScatter(new ChartSeriesId("readings"), "Reading", points));
            second.AddSeries(ChartSeries.CreateScatter(new ChartSeriesId("readings"), "Localized reading", System.Linq.Enumerable.Reverse(points)));
        } else {
            first.AddScatter("Reading", points);
            second.AddScatter("Localized reading", System.Linq.Enumerable.Reverse(points));
        }
        first.Series[0].WithObservationId(0, new ChartObservationId("north")).WithObservationId(1, new ChartObservationId("south"));
        second.Series[0].WithObservationId(0, new ChartObservationId("south")).WithObservationId(1, new ChartObservationId("north"));
        var html = new[] { first, second }.ToInteractiveHtmlDashboardPage(options => {
            options.Columns = 1;
            options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Selection
                | ChartInteractionFeatures.KeyboardNavigation | ChartInteractionFeatures.SynchronizedCharts;
            options.Interaction.GroupName = "stable-source-identities";
        }).Replace("<main ", "<button id='outside-chart'>Outside chart</button><main ", StringComparison.Ordinal);
        await using var session = await OpenAsync(html, 700, 900);
        var page = session.Page;
        var roots = page.Locator(".cfx-interactive-chart");
        var source = roots.Nth(0).Locator(Point(0, 0));
        var peer = roots.Nth(1).Locator(Point(0, 1));
        await roots.Nth(0).EvaluateAsync("root => { window.identityHoverSync=[]; root.addEventListener('cfxsync', event => { if (['hover','crosshair','navigate','hover-clear'].includes(event.detail.action)) window.identityHoverSync.push(event.detail.action); }); }");
        await source.FocusAsync();
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await source.GetAttributeAsync("aria-selected"));
        Assert.Equal("true", await roots.Nth(0).GetAttributeAsync("data-cfx-tooltip-pinned"));
        Assert.Equal("5", await source.GetAttributeAsync("data-cfx-y"));
        Assert.Equal("north", await source.GetAttributeAsync("data-cfx-observation-id"));
        var sourceLegend = roots.Nth(0).Locator("[data-cfx-role='legend-item'][data-cfx-point='0']");
        Assert.Equal(await source.GetAttributeAsync("data-cfx-target-id"), await sourceLegend.GetAttributeAsync("data-cfx-target-id"));
        if (explicitIds) {
            Assert.Equal(await source.GetAttributeAsync("data-cfx-target-id"), await peer.GetAttributeAsync("data-cfx-target-id"));
            Assert.Equal("true", await peer.GetAttributeAsync("aria-selected"));
            Assert.Equal("5", await peer.GetAttributeAsync("data-cfx-y"));
            Assert.Null(await roots.Nth(1).Locator(Point(0, 0)).GetAttributeAsync("aria-selected"));
        } else {
            Assert.Equal(0, await roots.Nth(1).Locator("[aria-selected=true]").CountAsync());
            await page.Locator("#outside-chart").FocusAsync();
            Assert.Empty(await page.EvaluateAsync<string[]>("() => window.identityHoverSync"));
        }
        AssertNoConsoleErrors(session);
    }
}
