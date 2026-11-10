using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class StandaloneViewportBrowserTests {
    [BrowserFact]
    public async Task ZoomedWindowsKeepTheAuthoredStepExplicitValuesAndMinorGrid() {
        if (!Enabled) return;
        await using var session = await OpenAsync(FamilyPage(AxisPolicyChart(bands: false)), 980, 800);
        var page = session.Page;
        await MountAsync(page);
        Assert.Null(await page.Locator(Root).GetAttributeAsync("data-cfx-zoom-declined"));
        await page.EvaluateAsync("()=>controller.setViewport({x:{minimum:2,maximum:10},y:{minimum:20,maximum:80}})");
        await SettleMotionAsync(page);
        var scene = await page.EvaluateAsync<JsonElement>("""
            () => {
                const svg = controller.root.querySelector('.cfx-stage svg');
                const values = axis => [...svg.querySelectorAll('[data-cfx-browser-axis=' + axis + '] text[data-cfx-axis-value]')].map(node => Number(node.dataset.cfxAxisValue));
                const minor = [...svg.querySelectorAll('[data-cfx-browser-grid=y] [data-cfx-role=grid-y-minor]')].map(node => Number(node.getAttribute('y1')));
                const hidden = [...svg.querySelectorAll('[data-cfx-role=grid-y-minor]')].filter(node => !node.closest('[data-cfx-browser-grid]')).every(node => node.style.display === 'none');
                const plot = JSON.parse(controller.root.dataset.cfxPreparedChart).dataProjection.plotBounds;
                return { x: values('x'), y: values('y'), minor, hidden, plot };
            }
            """);
        // Whole multiples of the Y step of 10 and the authored X values inside the window, never automatic nice ticks.
        Assert.Equal(new double[] { 20, 30, 40, 50, 60, 70, 80 }, scene.GetProperty("y").EnumerateArray().Select(value => value.GetDouble()));
        Assert.Equal(new double[] { 3, 6, 9 }, scene.GetProperty("x").EnumerateArray().Select(value => value.GetDouble()));
        // One minor line halves each re-projected interval: 25, 35 … 75 down the zoomed plot.
        var plot = scene.GetProperty("plot");
        var expected = new[] { 75d, 65, 55, 45, 35, 25 }.Select(value => plot.GetProperty("y").GetDouble() + (1 - (value - 20) / 60) * plot.GetProperty("height").GetDouble());
        Assert.Equal(expected.OrderBy(value => value).Select(value => Math.Round(value, 2)),
            scene.GetProperty("minor").EnumerateArray().Select(value => Math.Round(value.GetDouble(), 2)).OrderBy(value => value));
        Assert.True(scene.GetProperty("hidden").GetBoolean(), "Static minor lines must not stay at their unzoomed positions.");
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task AlternatingGridBandsDeclineSavedPageZoomVisibly() {
        if (!Enabled) return;
        await using var session = await OpenAsync(FamilyPage(AxisPolicyChart(bands: true)), 980, 800);
        var page = session.Page;
        await MountAsync(page);
        Assert.Equal("true", await page.Locator(Root).GetAttributeAsync("data-cfx-zoom-declined"));
        Assert.True(await page.Locator("[data-cfx-zoom=in]").IsDisabledAsync());
        AssertNoConsoleErrors(session);
    }

    private static Chart AxisPolicyChart(bool bands) {
        var chart = Chart.Create().WithSize(800, 440).WithTitle("Saved axis policy")
            .WithXAxisBounds(0, 12).WithYAxisBounds(0, 100)
            .AddLine("Samples", Enumerable.Range(0, 13).Select(index => new ChartPoint(index, 10 + index * 6)).ToArray())
            .ConfigureXAxis(axis => axis.WithTickValues(0, 3, 6, 9, 12))
            .ConfigureYAxis(axis => axis.WithTickStep(10).WithMinorTicks(1));
        if (bands) chart.Options.YAxis.ConfigureGrid(grid => grid.WithBands(ChartColor.FromHex("#3979C6")));
        return chart;
    }
}
