using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractiveNearestAcquisitionBrowserTests {
    [BrowserFact]
    public async Task InferredRankingKeepsDomTiesAndLivePaintGeometryAfterMutationAndResize() {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(800, 500).WithHeader(false).WithLegend(false)
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddScatter("Readings", new[] { new ChartPoint(3, 5), new ChartPoint(3, 5), new ChartPoint(8, 5) });
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            options.Interaction.Features = ChartInteractionFeatures.Tooltips;
            options.Tooltip.Range = HtmlChartTooltipRange.Nearest;
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
        }), 1000, 720);
        var page = session.Page;
        await page.EvaluateAsync("""
            () => {
              const root = document.querySelector('.cfx-interactive-chart');
              root.addEventListener('cfxhover', event => { window.nearestHover = event.detail.target; });
              window.nearestProbe = () => {
                const point = root.querySelector('g[data-cfx-role="point"][data-cfx-point="0"]');
                const box = point.getBoundingClientRect();
                return { x: box.left + box.width / 2, y: box.top + box.height / 2 + 60 };
              };
            }
            """);
        var probe = await page.EvaluateAsync<JsonElement>("() => window.nearestProbe()");
        await Probe(0);
        await page.Locator(Point(0, 0)).EvaluateAsync("node => node.style.opacity = '0'");
        await Probe(1);
        await page.Locator(Point(0, 1)).EvaluateAsync("node => node.setAttribute('transform', 'translate(500 0)')");
        await Probe(2);
        await page.Locator(Point(0, 0)).EvaluateAsync("node => node.style.removeProperty('opacity')");
        await page.Locator(Point(0, 1)).EvaluateAsync("node => node.removeAttribute('transform')");
        await page.SetViewportSizeAsync(650, 720);
        probe = await page.EvaluateAsync<JsonElement>("() => window.nearestProbe()");
        await Probe(0);
        AssertNoConsoleErrors(session);

        async Task Probe(int expected) {
            await MoveAwayAsync(page);
            await page.EvaluateAsync("() => { window.nearestHover = null; }");
            await page.Mouse.MoveAsync((float)probe.GetProperty("x").GetDouble(), (float)probe.GetProperty("y").GetDouble());
            await page.WaitForFunctionAsync("expected => Number(window.nearestHover?.sourcePoint) === expected && !document.querySelector('.cfx-tooltip').hidden", expected);
            Assert.Equal(expected, await page.EvaluateAsync<int>("() => Number(window.nearestHover.sourcePoint)"));
        }
    }
}
