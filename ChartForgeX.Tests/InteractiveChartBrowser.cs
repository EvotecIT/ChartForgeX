using ChartForgeX.Core;
using ChartForgeX.Themes;
using HtmlTinkerX;
using Microsoft.Playwright;

namespace ChartForgeX.Tests;

/// <summary>
/// Opens rendered interactive charts in headless Chromium through an HtmlTinkerX-owned session so tests can
/// assert the runtime look (computed opacity, visibility, tooltip content) that string checks cannot observe.
/// </summary>
internal static class InteractiveChartBrowser {
    /// <summary>Environment switch shared with the other EvotecIT browser suites; browser tests return early unless it is "1".</summary>
    internal const string EnableVariable = "HFX_TEST_ENABLE_PLAYWRIGHT";

    internal const string PointTarget = "g[data-cfx-role=\"point\"]";

    internal static bool Enabled => string.Equals(Environment.GetEnvironmentVariable(EnableVariable), "1", StringComparison.Ordinal);

    internal static async Task<HtmlBrowserSession> OpenAsync(string html, int width = 700, int height = 460) {
        var session = await HtmlBrowser.OpenSessionAsync("about:blank", new HtmlBrowserLaunchOptions {
            Browser = HtmlBrowserEngine.Chromium,
            Headless = true,
            Timeout = 15000,
            ViewportWidth = width,
            ViewportHeight = height
        });
        await session.Page.SetContentAsync(html);
        return session;
    }

    /// <summary>The approved "Checks over a week" interaction example: quiet, warning, and danger lines.</summary>
    internal static Chart StatusLines(bool dark) {
        var chart = Frame(dark, "Check results over time", "Healthy checks recede; failures remain visible")
            .WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun")
            .AddLine("Passed", Points(820, 940, 980, 1041, 1120, 1180, 1230))
            .AddLine("Warnings", Points(120, 138, 131, 112, 98, 86, 72))
            .AddLine("Failed", Points(22, 30, 27, 31, 18, 14, 11))
            .WithSeriesState("Passed", ChartSeriesState.Quiet)
            .WithSeriesState("Warnings", ChartSeriesState.Warning)
            .WithSeriesState("Failed", ChartSeriesState.Danger);
        chart.Options.YAxis.Maximum = 1400;
        chart.Options.YAxis.TickCount = 8;
        return chart;
    }

    internal static Chart SeverityBars(bool dark) => Frame(dark, "Findings by severity", "Current and previous run")
        .WithXLabels("Critical", "High", "Medium", "Low", "Info")
        .AddBar("Current", Points(6, 30, 82, 124, 208))
        .AddBar("Previous", Points(9, 39, 95, 116, 186));

    internal static string Legend(int series) => "[data-cfx-role=\"legend-item\"][data-cfx-series=\"" + series + "\"]";

    internal static string Point(int series, int point) => PointTarget + "[data-cfx-series=\"" + series + "\"][data-cfx-point=\"" + point + "\"]";

    internal static Task<string[]> OpacitiesAsync(IPage page, string selector) =>
        page.EvaluateAsync<string[]>("selector => Array.from(document.querySelectorAll(selector)).map(node => getComputedStyle(node).opacity)", selector);

    internal static Task<string> TooltipTextAsync(IPage page) =>
        page.EvaluateAsync<string>("() => { const tip = document.querySelector('.cfx-tooltip'); return tip && !tip.hidden ? tip.innerText : ''; }");

    internal static async Task<LocatorBoundingBoxResult> BoxAsync(IPage page, string selector) =>
        await page.Locator(selector).First.BoundingBoxAsync() ?? throw new InvalidOperationException("No layout box for " + selector);

    /// <summary>Moves the real pointer over the element centre, offset by the given pixels, in small steps so pointer events fire.</summary>
    internal static async Task MoveToAsync(IPage page, string selector, double offsetX = 0, double offsetY = 0) {
        var box = await BoxAsync(page, selector);
        await page.Mouse.MoveAsync((float)(box.X + box.Width / 2 + offsetX), (float)(box.Y + box.Height / 2 + offsetY), new MouseMoveOptions { Steps = 4 });
    }

    internal static async Task MoveAwayAsync(IPage page) {
        var size = page.ViewportSize ?? throw new InvalidOperationException("Viewport size is unavailable.");
        await page.Mouse.MoveAsync(size.Width - 4, size.Height - 4, new MouseMoveOptions { Steps = 2 });
    }

    internal static void AssertNoConsoleErrors(HtmlBrowserSession session) =>
        Xunit.Assert.DoesNotContain(session.ConsoleLog, entry => entry.Type == HtmlConsoleMessageType.Error);

    private static Chart Frame(bool dark, string title, string subtitle) => Chart.Create()
        .WithSize(596, 338)
        .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
        .WithTitle(title)
        .WithSubtitle(subtitle);

    private static ChartPoint[] Points(params double[] values) => values.Select((value, index) => new ChartPoint(index + 1, value)).ToArray();
}
