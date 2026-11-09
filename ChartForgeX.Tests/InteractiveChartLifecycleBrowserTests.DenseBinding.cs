using System.Diagnostics;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveChartLifecycleBrowserTests {
    // Binding used to search the whole series once per point and to re-lay the drawing out between point
    // measurements, so a 10,000-point line took minutes to become interactive. The bound is deliberately generous.
    [BrowserFact]
    public async Task DenseInteractiveLineBindsWithinSecondsAndHoverMeasuresOnlyNearbyPoints() {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(1200, 600).WithLegend(false)
            .AddLine("Signal", Enumerable.Range(0, 10000).Select(index => new ChartPoint(index, Math.Sin(index / 300.0) * 40 + 100)));
        var html = chart.ToInteractiveHtmlPage(options =>
            options.Interaction.Enable(ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan));
        await using var session = await OpenAsync("<!doctype html><html><body></body></html>", 1300, 760, configurePage: _ => { });
        var page = session.Page;

        var watch = Stopwatch.StartNew();
        await page.SetContentAsync(html, new() { Timeout = 60000 });
        await page.WaitForSelectorAsync(Root + "[data-cfx-runtime-bound=\"true\"]", new() { Timeout = 60000 });
        watch.Stop();
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(15), $"A 10,000-point line took {watch.Elapsed.TotalSeconds:0.0} s to bind.");

        await page.EvaluateAsync("() => document.querySelector('[data-cfx-point=\"5000\"]').scrollIntoView({ block: 'center' })");
        await HoverPointAsync(page, 5000);
        // Hover used to measure every point and repaint every target on each move; moving to the neighbouring point
        // now measures only the observations near the pointer.
        await page.EvaluateAsync("""
            () => { const measure = Element.prototype.getBoundingClientRect; window.densePointMeasures = 0;
              Element.prototype.getBoundingClientRect = function () { if (this.matches('[data-cfx-point]')) window.densePointMeasures++; return measure.call(this); }; }
            """);
        await HoverPointAsync(page, 5001);
        await page.EvaluateAsync("() => new Promise(done => requestAnimationFrame(() => requestAnimationFrame(done)))");
        Assert.InRange(await page.EvaluateAsync<int>("() => window.densePointMeasures"), 0, 50);
    }

    private static async Task HoverPointAsync(Microsoft.Playwright.IPage page, int index) {
        var point = page.Locator(PointTarget + "[data-cfx-point=\"" + index + "\"]");
        var label = await point.GetAttributeAsync("data-cfx-label");
        var centre = await point.EvaluateAsync<double[]>("node => { const box = node.getBoundingClientRect(); return [box.left + box.width / 2, box.top + box.height / 2]; }");
        await page.Mouse.MoveAsync((float)centre[0], (float)centre[1]);
        await page.WaitForFunctionAsync("label => { const tip = document.querySelector('.cfx-tooltip'); return !!tip && !tip.hidden && tip.textContent.includes(label); }", label);
    }
}
