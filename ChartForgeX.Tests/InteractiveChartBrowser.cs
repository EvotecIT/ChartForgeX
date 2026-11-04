using ChartForgeX.Core;
using ChartForgeX.Themes;
using HtmlTinkerX;
using Microsoft.Playwright;

namespace ChartForgeX.Tests;

/// <summary>
/// Opens rendered interactive charts through an HtmlTinkerX-owned browser session so tests can
/// assert the runtime look (computed opacity, visibility, tooltip content) that string checks cannot observe.
/// </summary>
internal static partial class InteractiveChartBrowser {
    /// <summary>Environment switch shared with the other EvotecIT browser suites; optional browser tests are skipped unless it is "1".</summary>
    internal const string EnableVariable = "HFX_TEST_ENABLE_PLAYWRIGHT";
    internal const string EngineVariable = "CFX_TEST_BROWSER";

    internal const string PointTarget = "g[data-cfx-role=\"point\"]";

    internal static bool Enabled => BrowserTestPrerequisites.IsEnabled();

    /// <summary>Selects the explicit engine for compatibility proof; the required browser suite defaults to Chromium.</summary>
    internal static HtmlBrowserEngine Engine => Environment.GetEnvironmentVariable(EngineVariable)?.Trim().ToLowerInvariant() switch {
        null or "" or "chromium" => HtmlBrowserEngine.Chromium,
        "firefox" => HtmlBrowserEngine.Firefox,
        "webkit" => HtmlBrowserEngine.WebKit,
        _ => throw new InvalidOperationException(EngineVariable + " must be chromium, firefox or webkit.")
    };

    internal static async Task<HtmlBrowserSession> OpenAsync(string html, int width = 700, int height = 460, bool touch = false, Action<IPage>? configurePage = null, Func<IPage, Task>? configurePageAsync = null, bool waitForOwnedFonts = true) {
        var origin = OriginPage();
        var session = await HtmlBrowser.OpenSessionForNavigationAsync(new HtmlBrowserLaunchOptions {
            Browser = Engine,
            BrowserExecutablePath = Environment.GetEnvironmentVariable("CFX_TEST_BROWSER_EXECUTABLE"),
            Headless = true,
            Timeout = 15000,
            ViewportWidth = width,
            ViewportHeight = height,
            HasTouch = touch,
            IsMobile = touch
        });
        try {
            if (origin != "about:blank") {
                // Keep a secure, same-origin document without allocating a TCP connection
                // for every test. Gallery assets still use the real shared HTTP server.
                await session.Page.RouteAsync(origin, route => route.FulfillAsync(new() {
                    ContentType = "text/html; charset=utf-8",
                    Body = "<!doctype html><html><head><meta charset=\"utf-8\"></head><body></body></html>"
                }));
                await session.Page.GotoAsync(origin, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 15000 });
                await session.Page.UnrouteAsync(origin);
            }
            configurePage?.Invoke(session.Page);
            if (configurePageAsync != null) await configurePageAsync(session.Page);
            await session.Page.SetContentAsync(html);
            if (waitForOwnedFonts && await session.Page.EvaluateAsync<bool>("()=>isSecureContext&&typeof FontFace==='function'&&!!document.querySelector('.cfx-stage [data-cfx-owned-font-text]')"))
                await session.Page.WaitForFunctionAsync("()=>[...document.querySelectorAll('.cfx-stage [data-cfx-owned-font-text]')].every(node=>!node.hasAttribute('display'))");
            await BrowserProofCaptions.InstallNativeAsync(session.Page);
            if (configurePage is null && configurePageAsync is null)
                await SettlePresentationAsync(session.Page);
            return session;
        } catch {
            await session.DisposeAsync();
            throw;
        }
    }

    // The maintained gallery supplies a real loopback origin for crypto-backed owned fonts.
    // Fulfill a small same-origin document before installing the exact test HTML.
    // The gallery manifest can be large and is unrelated to these font-origin fixtures.
    private static string OriginPage() {
        var gallery = Environment.GetEnvironmentVariable("CFX_V2_GALLERY_URL");
        if (string.IsNullOrWhiteSpace(gallery)) return "about:blank";
        if (!Uri.TryCreate(gallery, UriKind.Absolute, out var address) || !address.IsLoopback
            || address.Scheme != Uri.UriSchemeHttp && address.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("CFX_V2_GALLERY_URL must identify the owned loopback gallery.");
        return new Uri(address, "__cfx_browser_origin.html").AbsoluteUri;
    }

    /// <summary>The approved "Checks over a week" interaction example: quiet, warning, and danger lines.</summary>
    internal static Chart StatusLines(bool dark) {
        var chart = Frame(dark, "Check results over time", "Healthy checks recede; failures remain visible")
            .WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun");
        var passed = new ChartSeries(new ChartSeriesId("passed"), "Passed", ChartSeriesKind.Line, Points(820, 940, 980, 1041, 1120, 1180, 1230)).WithState(ChartSeriesState.Quiet);
        var warnings = new ChartSeries(new ChartSeriesId("warnings"), "Warnings", ChartSeriesKind.Line, Points(120, 138, 131, 112, 98, 86, 72)).WithState(ChartSeriesState.Warning);
        var failed = new ChartSeries(new ChartSeriesId("failed"), "Failed", ChartSeriesKind.Line, Points(22, 30, 27, 31, 18, 14, 11)).WithState(ChartSeriesState.Danger);
        chart.AddSeries(passed).AddSeries(warnings).AddSeries(failed);
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

    /// <summary>Waits for finite chart presentation before reading final paint, preserving paused and unbounded host animations.</summary>
    internal static Task SettlePresentationAsync(IPage page) => page.EvaluateAsync("async()=>{const animations=[...document.querySelectorAll('.cfx-interactive-chart')].flatMap(root=>root.getAnimations({subtree:true}));await Promise.all(animations.filter(animation=>animation.playState==='running'&&animation.effect?.getComputedTiming().endTime<=1000).map(animation=>animation.finished.catch(()=>{})));}");

    internal static async Task<string[]> OpacitiesAsync(IPage page, string selector) {
        await SettlePresentationAsync(page);
        return await page.EvaluateAsync<string[]>("selector => Array.from(document.querySelectorAll(selector)).map(node => getComputedStyle(node).opacity)", selector);
    }

    internal static Task<string> TooltipTextAsync(IPage page) =>
        page.EvaluateAsync<string>("() => { const tip = document.querySelector('.cfx-tooltip'); return tip && !tip.hidden ? tip.innerText : ''; }");

    internal static async Task<LocatorBoundingBoxResult> BoxAsync(IPage page, string selector) =>
        await page.Locator(selector).First.BoundingBoxAsync() ?? throw new InvalidOperationException("No layout box for " + selector);

    /// <summary>Moves the real pointer over the element centre, offset by the given pixels, in small steps so pointer events fire.</summary>
    internal static async Task MoveToAsync(IPage page, string selector, double offsetX = 0, double offsetY = 0) {
        var box = await BoxAsync(page, selector);
        await page.Mouse.MoveAsync((float)(box.X + box.Width / 2 + offsetX), (float)(box.Y + box.Height / 2 + offsetY), new MouseMoveOptions { Steps = 4 });
    }

    /// <summary>Locates two actual series-ink positions, excluding observation hit targets and stroke gaps.</summary>
    internal static Task<double[]> NativeSeriesPositionsAsync(IPage page, string role, double[]? fractions = null) =>
        page.Locator("[data-cfx-role='" + role + "']").First.EvaluateAsync<double[]>("""
            (path, fractions) => {
                const length = path.getTotalLength(), owner = path.closest('[data-cfx-role=series]');
                const valid = point => {
                    const hit = document.elementFromPoint(point.x, point.y);
                    return !!hit && owner.contains(hit) && hit.closest('[data-cfx-target-kind]') === owner;
                };
                for (const fraction of fractions) {
                    const first = path.getPointAtLength(length * fraction).matrixTransform(path.getScreenCTM());
                    const second = path.getPointAtLength(length * fraction + 2).matrixTransform(path.getScreenCTM());
                    if (valid(first) && valid(second)) return [first.x, first.y, second.x, second.y];
                }
                throw new Error('No native series path position outside observation hit areas');
            }
            """, fractions ?? new[] { .13, .23, .37, .61, .77 });

    internal static async Task MoveAwayAsync(IPage page) {
        var size = page.ViewportSize ?? throw new InvalidOperationException("Viewport size is unavailable.");
        await page.Mouse.MoveAsync(size.Width - 4, size.Height - 4, new MouseMoveOptions { Steps = 2 });
        await SettlePresentationAsync(page);
    }

    internal static void AssertNoConsoleErrors(HtmlBrowserSession session) {
        var captures = Environment.GetEnvironmentVariable("CFX_V2_CAPTURE_ROOT");
        if (!string.IsNullOrWhiteSpace(captures)) {
            if (!Path.IsPathRooted(captures)) throw new InvalidOperationException("CFX_V2_CAPTURE_ROOT must be an explicit absolute output path.");
            var output = Path.Combine(captures, "console");
            Directory.CreateDirectory(output);
            var evidence = new {
                messages = session.ConsoleLog.Count(),
                errors = session.ConsoleLog.Where(entry => entry.Type == HtmlConsoleMessageType.Error).Select(entry => entry.Text).ToArray()
            };
            File.WriteAllText(Path.Combine(output, Guid.NewGuid().ToString("N") + ".json"),
                System.Text.Json.JsonSerializer.Serialize(evidence) + "\n");
        }
        Xunit.Assert.DoesNotContain(session.ConsoleLog, entry => entry.Type == HtmlConsoleMessageType.Error);
    }

    private static Chart Frame(bool dark, string title, string subtitle) => Chart.Create()
        .WithSize(596, 338)
        .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
        .WithTitle(title)
        .WithSubtitle(subtitle);

    private static ChartPoint[] Points(params double[] values) => values.Select((value, index) => new ChartPoint(index + 1, value)).ToArray();
}
