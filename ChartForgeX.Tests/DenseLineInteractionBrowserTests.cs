using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>
/// Hover and zoom on a dense line stay local to the observations they read. Work proportional to every observation
/// made each line crossing and each zoom of a 10,000-point line take hundreds of milliseconds.
/// </summary>
public sealed class DenseLineInteractionBrowserTests {
    private const int Points = 2_000;

    // Records class changes on the series and its observations, and every geometry write on observation adapter marks.
    private const string Watch = """
        () => {
          const series = document.querySelector('[data-cfx-role="series"]');
          window.denseLine = { series: false, observations: new Set(), adapterWrites: 0 };
          new MutationObserver(records => records.forEach(record => {
            const node = record.target;
            if (node === series && record.attributeName === 'class')
              window.denseLine.series ||= /\bcfx-hovered\b/.test((record.oldValue || '') + ' ' + node.getAttribute('class'));
            else if (record.attributeName === 'class' && node.matches('[data-cfx-point]')) window.denseLine.observations.add(node);
            else if (node.matches('[data-cfx-browser-hit-area],.cfx-prepared-point-marker') && record.attributeName !== 'class') window.denseLine.adapterWrites++;
          })).observe(series, { subtree: true, attributes: true, attributeOldValue: true });
        }
        """;

    [BrowserFact]
    public async Task CrossingTheLinePaintsOnlyTheObservationsItReads() {
        if (!Enabled) return;
        await using var session = await OpenAsync(Page(), 1400, 900);
        var page = session.Page;
        var point = await BoxAsync(page, Point(0, 700));
        var x = (float)(point.X + point.Width / 2);
        await page.Mouse.MoveAsync(x, (float)(point.Y + point.Height / 2 + 160), new MouseMoveOptions { Steps = 4 });
        await FramesAsync(page);
        await page.EvaluateAsync(Watch);

        await page.Mouse.MoveAsync(x, (float)(point.Y + point.Height / 2), new MouseMoveOptions { Steps = 4 });
        await FramesAsync(page);
        Assert.Equal("series", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-hover-mode"));
        await page.Mouse.MoveAsync(x, (float)(point.Y + point.Height / 2 + 160), new MouseMoveOptions { Steps = 4 });
        await FramesAsync(page);
        Assert.Equal("shared", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-hover-mode"));

        Assert.False(await page.EvaluateAsync<bool>("() => window.denseLine.series"), "Crossing the line hovered the whole series.");
        var touched = await page.EvaluateAsync<int>("() => window.denseLine.observations.size");
        // The two moves take eight pointer steps and each step reads at most one observation; the old runtime restyled all of them.
        Assert.True(touched <= 8, $"Crossing the line changed the classes of {touched} of {Points} observations.");
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task ZoomMovesEachMarklessObservationAsOneAndKeepsItsMarkerOnTheLine() {
        if (!Enabled) return;
        await using var session = await OpenAsync(Page(), 1400, 900,
            configurePageAsync: page => page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce }));
        var page = session.Page;
        await page.EvaluateAsync(Watch);
        await page.Locator("[data-cfx-zoom=in]").EvaluateAsync("button => button.click()");
        await FramesAsync(page);
        Assert.Equal(0, await page.EvaluateAsync<int>("() => window.denseLine.adapterWrites"));

        await MoveToAsync(page, Point(0, 1000));
        await page.WaitForFunctionAsync("() => !!document.querySelector('[data-cfx-point].cfx-hovered')");
        var offset = await page.EvaluateAsync<double>("""
            () => {
              const point = document.querySelector('[data-cfx-point].cfx-hovered'), line = document.querySelector('[data-cfx-role="series"] [data-cfx-role="line"]');
              const values = line.getAttribute('d').match(/-?\d+(\.\d+)?(e-?\d+)?/gi).map(Number), index = Number(point.dataset.cfxPoint) * 2;
              const vertex = new DOMPoint(values[index], values[index + 1]).matrixTransform(line.getScreenCTM());
              const marker = point.querySelector('.cfx-prepared-point-marker').getBoundingClientRect();
              return Math.hypot(marker.left + marker.width / 2 - vertex.x, marker.top + marker.height / 2 - vertex.y);
            }
            """);
        Assert.True(offset < .5, $"The zoomed hover marker sits {offset:F2} px from its line vertex.");
        AssertNoConsoleErrors(session);
    }

    private static string Page() => Chart.Create().WithSize(1200, 600).WithLegend(false)
        .AddLine("Signal", Enumerable.Range(0, Points).Select(index => new ChartPoint(index, Math.Sin(index * .013) * 40 + index % 17)))
        .ToInteractiveHtmlPage(options => options.Interaction.Features =
            ChartInteractionFeatures.ReportReview | ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan);

    private static Task FramesAsync(IPage page) =>
        page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");
}
