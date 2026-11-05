using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractivePolarCrosshairBrowserTests {
    [BrowserTheory]
    [InlineData("full", true, false)]
    [InlineData("standalone", true, true)]
    [InlineData("core", false, false)]
    [InlineData("full", false, true)]
    public async Task PolarReaderShowsAngleAndRadiusForTheHoveredAndFocusedObservation(string runtime, bool radar, bool dark) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(600, 420).WithLegend(false).WithDataLabels(false)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        if (radar) {
            chart.WithXLabels("Speed", "Reliability", "Comfort", "Safety", "Efficiency")
                .AddRadar("Model A", new[] { new ChartPoint(1, 80), new ChartPoint(2, 65), new ChartPoint(3, 90), new ChartPoint(4, 70), new ChartPoint(5, 55) })
                .AddRadar("Model B", new[] { new ChartPoint(1, 50), new ChartPoint(2, 85), new ChartPoint(3, 60), new ChartPoint(4, 95), new ChartPoint(5, 75) });
        } else {
            chart.WithYAxisBounds(0, 100).WithXLabels("North", "East", "South", "West")
                .AddPolar("Signal", new[] { new ChartPoint(1, 40), new ChartPoint(2, 85), new ChartPoint(3, 55), new ChartPoint(4, 70) });
        }
        var html = chart.ToInteractiveHtmlPage(options => {
            options.Runtime = runtime == "full" ? HtmlChartRuntime.Full : HtmlChartRuntime.Standalone;
            if (runtime != "core") return;
            // The saved reader has no native text export, so it keeps browser text.
            options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.KeyboardNavigation | ChartInteractionFeatures.Crosshair;
            options.TextPresentation = HtmlChartTextPresentation.Browser;
        });
        // The standalone case reads by touch; the others by mouse.
        var touch = runtime == "standalone";
        await using var session = await OpenAsync(html, 700, 520, touch);
        var page = session.Page;
        Assert.Equal(runtime == "full" ? null : runtime, await page.EvaluateAsync<string?>(
            "() => document.querySelector('.cfx-interactive-chart')[Symbol.for('ChartForgeX.chartRuntime')]?.kind ?? null"));
        var role = radar ? "radar-point-source" : "polar-point-source";
        string Observation(int series, int point) => "[data-cfx-role='" + role + "'][data-cfx-series='" + series + "'][data-cfx-point='" + point + "']";
        var crosshair = page.Locator(".cfx-crosshair");

        var hovered = Observation(radar ? 1 : 0, 1);
        var box = (await page.Locator(hovered).BoundingBoxAsync())!;
        var cdp = touch ? await page.Context.NewCDPSessionAsync(page) : null;
        // A held contact reads like a hovering pointer; lifting it ends the reading.
        Task TouchAsync(string type, params object[] contacts) => cdp!.SendAsync("Input.dispatchTouchEvent",
            new Dictionary<string, object> { ["type"] = type, ["touchPoints"] = contacts });
        if (touch) {
            var x = box.X + box.Width / 2; var y = box.Y + box.Height / 2;
            await TouchAsync("touchStart", new { id = 1, x, y });
            await TouchAsync("touchMove", new { id = 1, x = x + 1, y });
            await page.WaitForFunctionAsync("() => !document.querySelector('.cfx-crosshair').hidden", null, new() { Timeout = 3000 });
        } else await MoveToAsync(page, hovered);
        await AssertReadoutAsync(page, hovered, radar ? "Reliability" : "East", "85");
        await CaptureAsync(page, "polar-crosshair-" + runtime + (radar ? "-radar" : "-polar") + (dark ? "-dark" : "-light") + ".png");
        if (touch) await TouchAsync("touchEnd");
        else await page.Mouse.MoveAsync(2, 2);
        Assert.True(await crosshair.IsHiddenAsync(), "Ending the pointer reading hides the polar guide.");

        var focused = page.Locator(Observation(0, 2));
        await FocusWithKeyboardAsync(page, focused);
        await AssertReadoutAsync(page, Observation(0, 2), radar ? "Comfort" : "South", radar ? "90" : "55");
        await focused.EvaluateAsync("node => node.blur()");
        Assert.True(await crosshair.IsHiddenAsync(), "Blurring the observation hides the polar guide.");
        AssertNoConsoleErrors(session);
    }

    private static async Task AssertReadoutAsync(IPage page, string selector, string angle, string radius) {
        var crosshair = page.Locator(".cfx-crosshair");
        Assert.True(await crosshair.IsVisibleAsync(), "The polar guide is shown for " + selector + ".");
        Assert.Equal(angle, await page.Locator(".cfx-crosshair__axis-value[data-cfx-axis-value=angle]").InnerTextAsync());
        Assert.Equal(radius, await page.Locator(".cfx-crosshair__axis-value[data-cfx-axis-value=radius]").InnerTextAsync());
        Assert.Equal(0, await page.Locator(".cfx-crosshair :is(.cfx-crosshair__line, [data-cfx-axis-value=x], [data-cfx-axis-value=y]):visible").CountAsync());
        var geometry = await page.EvaluateAsync<JsonElement>("""
            selector => {
              const root = document.querySelector('.cfx-interactive-chart'), mark = root.querySelector(selector).getBoundingClientRect();
              const card = root.querySelector('[data-cfx-role="frame-card"]').getBoundingClientRect();
              const spoke = root.querySelector('[data-cfx-role$="-spoke"]'), matrix = spoke.getScreenCTM();
              const centre = new DOMPoint(+spoke.getAttribute('x1'), +spoke.getAttribute('y1')).matrixTransform(matrix);
              const ring = root.querySelector('.cfx-crosshair__ring').getBoundingClientRect();
              const x = mark.left + mark.width / 2, y = mark.top + mark.height / 2;
              const labels = [...root.querySelectorAll('.cfx-crosshair__axis-value[data-cfx-axis-value=angle], .cfx-crosshair__axis-value[data-cfx-axis-value=radius]')]
                .map(label => label.getBoundingClientRect());
              return {
                ringCentre: Math.hypot(ring.left + ring.width / 2 - centre.x, ring.top + ring.height / 2 - centre.y),
                ringRadius: Math.abs(ring.width / 2 - Math.hypot(x - centre.x, y - centre.y)),
                inside: labels.every(box => box.left >= card.left && box.right <= card.right && box.top >= card.top && box.bottom <= card.bottom),
                clear: labels.every(box => box.right <= mark.left || box.left >= mark.right || box.bottom <= mark.top || box.top >= mark.bottom),
                readable: labels.every(label => label.height >= 14)
              };
            }
            """, selector);
        Assert.True(geometry.GetProperty("ringCentre").GetDouble() < 1.5, "The guide ring is centred on the polar origin: " + geometry);
        Assert.True(geometry.GetProperty("ringRadius").GetDouble() < 1.5, "The guide ring passes through the observation: " + geometry);
        Assert.True(geometry.GetProperty("inside").GetBoolean(), "Polar values stay inside the card: " + geometry);
        Assert.True(geometry.GetProperty("clear").GetBoolean(), "Polar values do not cover the observation: " + geometry);
        Assert.True(geometry.GetProperty("readable").GetBoolean(), "Polar values keep a readable pill: " + geometry);
    }

    private static async Task CaptureAsync(IPage page, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name) });
    }
}
