using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractiveFinancialMarkOptionsBrowserTests {
    [Theory]
    [InlineData(ChartSeriesKind.Candlestick, 360, false)]
    [InlineData(ChartSeriesKind.Candlestick, 360, true)]
    [InlineData(ChartSeriesKind.Candlestick, 800, false)]
    [InlineData(ChartSeriesKind.Candlestick, 800, true)]
    [InlineData(ChartSeriesKind.Ohlc, 360, false)]
    [InlineData(ChartSeriesKind.Ohlc, 360, true)]
    [InlineData(ChartSeriesKind.Ohlc, 800, false)]
    [InlineData(ChartSeriesKind.Ohlc, 800, true)]
    public async Task FinancialMarksRetainPointerKeyboardSelectionLegendAndSourceFacts(ChartSeriesKind kind, int width, bool dark) {
        if (!Enabled) return;
        var mode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
        var chart = V2GalleryModels.Create(kind, width == 360 ? "compact-options" : "options", mode)
            .WithSize(width, width == 360 ? 360 : 440).WithTitle(V2GalleryModels.Title(kind))
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight()).WithLegend(true);
        var name = "financial-" + kind.ToString().ToLowerInvariant() + "-" + width + "-" + (dark ? "dark" : "light");
        var directory = CaptureDirectory();
        if (directory != null) {
            await File.WriteAllTextAsync(Path.Combine(directory, name + ".svg"), chart.ToSvg());
            await File.WriteAllBytesAsync(Path.Combine(directory, name + ".native.png"), chart.ToPng());
        }
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Selection
                | ChartInteractionFeatures.KeyboardNavigation | ChartInteractionFeatures.LegendToggles;
        }), width + (width == 360 ? 0 : 48), 600);
        var page = session.Page; var target = page.Locator(Point(0, 0));
        await CaptureAsync(page, directory, name + ".browser.png");
        Assert.Equal(new[] { "44", "58", "38", "53" }, await target.EvaluateAsync<string[]>(
            "node => ['open','high','low','close'].map(part => node.getAttribute('data-cfx-' + part))"));
        var label = await target.GetAttributeAsync("aria-label");
        foreach (var fact in new[] { "open=44", "high=58", "low=38", "close=53" }) Assert.Contains(fact, label);
        if (kind == ChartSeriesKind.Candlestick)
            Assert.Equal("none", await target.Locator("[data-cfx-role='candlestick-body']").EvaluateAsync<string>("node => getComputedStyle(node).fill"));
        await MoveToAsync(page, Point(0, 0));
        Assert.Contains("cfx-hovered", await target.GetAttributeAsync("class"));
        Assert.NotEmpty(await TooltipTextAsync(page));
        var priceRows = await page.Locator(".cfx-tooltip dt").AllTextContentsAsync();
        Assert.DoesNotContain("Y", priceRows);
        foreach (var (part, price) in new[] { ("Open", "44"), ("High", "58"), ("Low", "38"), ("Close", "53") }) {
            Assert.Contains(part, priceRows);
            Assert.Equal(price, await page.Locator(".cfx-tooltip dt").Filter(new LocatorFilterOptions { HasText = part }).Locator("+ dd").InnerTextAsync());
        }
        await target.ClickAsync();
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        Assert.Contains("cfx-tooltip--pinned", await page.Locator(".cfx-tooltip").GetAttributeAsync("class"));
        await target.FocusAsync(); await page.Keyboard.PressAsync("ArrowRight");
        Assert.True(await page.Locator(Point(0, 1)).EvaluateAsync<bool>("node => node === document.activeElement"));
        await page.Locator(Point(0, 3)).FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await page.Locator(Point(0, 3)).GetAttributeAsync("aria-selected"));
        var flat = await BoxAsync(page, Point(0, 3));
        Assert.True(flat.Height > 0);
        var legend = page.Locator("[data-cfx-role='legend-item'][data-cfx-point='0']");
        await legend.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Contains("cfx-series-muted", await target.GetAttributeAsync("class"));
        await page.Keyboard.PressAsync("Space");
        Assert.DoesNotContain("cfx-series-muted", await target.GetAttributeAsync("class"));
        await page.Locator(Point(0, 3)).FocusAsync();
        await CaptureAsync(page, directory, name + ".selected.png");
        if (directory != null) {
            var runtime = await page.EvaluateAsync<JsonElement>("() => ({viewport: innerWidth, points: Array.from(document.querySelectorAll('g[data-cfx-role=point]')).map(node => {const box=node.getBBox(); return {label: node.getAttribute('aria-label'), box: {x:box.x,y:box.y,width:box.width,height:box.height}, selected: node.getAttribute('aria-selected')};}), tooltip: document.querySelector('.cfx-tooltip').innerText})");
            await File.WriteAllTextAsync(Path.Combine(directory, name + ".runtime.json"), JsonSerializer.Serialize(runtime, new JsonSerializerOptions { WriteIndented = true }));
        }
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HollowCandleFragmentRetainsTheHostBackdropAndInteriorTarget(bool dark) {
        if (!Enabled) return;
        var chart = FinancialMarkOptionsTests.Prices(ChartSeriesKind.Candlestick).WithAxes(false).WithGrid(false)
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight()).WithOverlay();
        chart.Series[0].ConfigureFinancial(financial => financial.Rising.FillOpacity = 0);
        var fragment = chart.ToInteractiveHtmlFragment(options => options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit);
        var html = "<!doctype html><html><head><style>body{margin:0;padding:12px;background:repeating-linear-gradient(90deg,#a7b7bd 0 12px,#d5dfdc 12px 24px)}.report{width:320px}.report .cfx-interactive-chart,.report .cfx-stage,.report svg{background:transparent;border:0;padding:0}</style></head><body><main class='report'>"
            + fragment + "</main></body></html>";
        await using var session = await OpenAsync(html, 350, 380);
        var target = session.Page.Locator(Point(0, 0));
        Assert.Equal("rgba(0, 0, 0, 0)", await session.Page.Locator(".cfx-stage").EvaluateAsync<string>("node => getComputedStyle(node).backgroundColor"));
        Assert.Equal("rgba(0, 0, 0, 0)", await session.Page.Locator(".cfx-interactive-chart").EvaluateAsync<string>("node => getComputedStyle(node).backgroundColor"));
        Assert.Equal("none", await target.Locator("[data-cfx-role='candlestick-body']").GetAttributeAsync("fill"));
        await MoveToAsync(session.Page, Point(0, 0));
        Assert.Contains("cfx-hovered", await target.GetAttributeAsync("class"));
        await CaptureAsync(session.Page, CaptureDirectory(), "financial-transparent-host-" + (dark ? "dark" : "light") + ".png");
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(ChartSeriesKind.Candlestick, HtmlChartTooltipMode.Single)]
    [InlineData(ChartSeriesKind.Candlestick, HtmlChartTooltipMode.SharedX)]
    [InlineData(ChartSeriesKind.Ohlc, HtmlChartTooltipMode.Single)]
    [InlineData(ChartSeriesKind.Ohlc, HtmlChartTooltipMode.SharedX)]
    public async Task FinancialReadoutAndPinnedDetailsPreserveAllZeroPricesWithoutGenericY(ChartSeriesKind kind, HtmlChartTooltipMode mode) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(400, 280).WithTitle("Zero prices").WithLegend(false);
        var source = new[] { new ChartCandlestick(1, 0, 0, 0, 0) };
        if (kind == ChartSeriesKind.Candlestick) chart.AddCandlestick("Price", source); else chart.AddOhlc("Price", source);
        chart.Series[0].ConfigureFinancial(financial => {
            financial.Rising.StrokeWidth = 3;
            if (kind == ChartSeriesKind.Candlestick) financial.Rising.FillOpacity = 0;
        });
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => options.Tooltip.Mode = mode), 460, 430);
        var page = session.Page; var target = page.Locator(Point(0, 0));
        await target.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Contains("cfx-tooltip--pinned", await page.Locator(".cfx-tooltip").GetAttributeAsync("class"));
        var name = "financial-zero-readout-" + kind.ToString().ToLowerInvariant() + "-" + mode;
        var directory = CaptureDirectory();
        await CaptureAsync(page, directory, name + ".png");
        var rows = await page.Locator(".cfx-tooltip dt").AllTextContentsAsync();
        if (directory != null) await File.WriteAllTextAsync(Path.Combine(directory, name + ".runtime.json"), JsonSerializer.Serialize(new {
            rows, tooltip = await TooltipTextAsync(page), label = await target.GetAttributeAsync("aria-label")
        }, new JsonSerializerOptions { WriteIndented = true }));
        Assert.DoesNotContain("Y", rows);
        foreach (var part in new[] { "Open", "High", "Low", "Close" }) {
            Assert.Contains(part, rows);
            Assert.Equal("0", await page.Locator(".cfx-tooltip dt").Filter(new LocatorFilterOptions { HasText = part }).Locator("+ dd").InnerTextAsync());
        }
        await page.Locator(".cfx-interactive-chart").EvaluateAsync("node => node.setAttribute('data-cfx-label-open', 'Opening price')");
        await page.Keyboard.PressAsync("Space"); await page.Keyboard.PressAsync("Space");
        Assert.Contains("Opening price", await page.Locator(".cfx-tooltip dt").AllTextContentsAsync());
        Assert.Contains("open=0 high=0 low=0 close=0", await target.GetAttributeAsync("aria-label"));
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(ChartSeriesKind.Candlestick, false)]
    [InlineData(ChartSeriesKind.Ohlc, false)]
    [InlineData(ChartSeriesKind.Candlestick, true)]
    [InlineData(ChartSeriesKind.Ohlc, true)]
    public async Task FinancialPointLegendUsesTheReferencedObservationPrices(ChartSeriesKind kind, bool zero) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(360, 320).WithPointLegend().WithDataLabels().WithTitle("Financial legend facts")
            .WithTheme(zero ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight());
        var source = new[] { new ChartCandlestick(1, 44, 58, 38, 53),
            zero ? new ChartCandlestick(2, 0, 0, 0, 0) : new ChartCandlestick(2, 100, 120, 80, 110) };
        if (kind == ChartSeriesKind.Candlestick) chart.AddCandlestick("Prices", source);
        else chart.AddOhlc("Prices", source);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), 390, 500);
        var page = session.Page;
        const string selector = "[data-cfx-role='legend-item'][data-cfx-series='0'][data-cfx-point='1']";
        var legend = page.Locator(selector);
        await MoveToAsync(page, selector);
        var directory = CaptureDirectory();
        var name = "financial-point-legend-" + kind + "-" + zero;
        await CaptureAsync(page, directory, name + ".png");
        var rows = await page.Locator(".cfx-tooltip dt").AllTextContentsAsync();
        if (directory != null) await File.WriteAllTextAsync(Path.Combine(directory, name + ".runtime.json"), JsonSerializer.Serialize(new {
            rows, tooltip = await TooltipTextAsync(page), legend = await legend.EvaluateAsync<JsonElement>("node => ({...node.dataset})"),
            captions = await page.Locator("[data-cfx-role='data-label']").EvaluateAllAsync<string[]>("nodes => nodes.map(node => node.outerHTML)")
        }, new JsonSerializerOptions { WriteIndented = true }));
        Assert.DoesNotContain("Value", rows); Assert.DoesNotContain("Y", rows);
        var prices = zero ? new[] { "0", "0", "0", "0" } : new[] { "100", "120", "80", "110" };
        var parts = new[] { "Open", "High", "Low", "Close" };
        for (var index = 0; index < parts.Length; index++) {
            Assert.Contains(parts[index], rows);
            Assert.Equal(prices[index], await page.Locator(".cfx-tooltip dt").Filter(new LocatorFilterOptions { HasText = parts[index] }).Locator("+ dd").InnerTextAsync());
        }
        await MoveAwayAsync(page); await legend.FocusAsync();
        Assert.Equal(rows, await page.Locator(".cfx-tooltip dt").AllTextContentsAsync());
        await page.Locator(".cfx-interactive-chart").EvaluateAsync("node=>node.dataset.cfxLabelOpen='Opening price'");
        await legend.BlurAsync(); await legend.FocusAsync();
        Assert.Contains("Opening price", await page.Locator(".cfx-tooltip dt").AllTextContentsAsync());
        await MoveAwayAsync(page); await legend.BlurAsync();
        const string caption = "[data-cfx-source-id='series-0-point-1-label']";
        Assert.Equal(1, await page.Locator(caption).CountAsync());
        await MoveToAsync(page, caption);
        Assert.False(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        Assert.Contains("cfx-hovered", await page.Locator(Point(0, 1)).GetAttributeAsync("class"));
        Assert.Equal(prices, await page.Locator(".cfx-tooltip dt").EvaluateAllAsync<string[]>(
            "nodes => nodes.filter(node => ['Opening price','High','Low','Close'].includes(node.textContent)).map(node => node.nextElementSibling.textContent)"));
        await CaptureAsync(page, directory, name + ".caption-hover.png");
        await page.Locator(Point(0, 1)).FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await page.Locator(Point(0, 1)).GetAttributeAsync("aria-selected"));
        Assert.Contains("cfx-tooltip--pinned", await page.Locator(".cfx-tooltip").GetAttributeAsync("class"));
        for (var index = 0; index < parts.Length; index++) {
            var part = index == 0 ? "Opening price" : parts[index];
            Assert.Equal(prices[index], await page.Locator(".cfx-tooltip dt").Filter(new LocatorFilterOptions { HasText = part }).Locator("+ dd").InnerTextAsync());
        }
        await CaptureAsync(page, directory, name + ".point-pinned.png");
        AssertNoConsoleErrors(session);
    }

    private static string? CaptureDirectory() {
        var path = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(path)) return null;
        Directory.CreateDirectory(path); return path;
    }
    private static async Task CaptureAsync(IPage page, string? directory, string name) {
        if (directory != null) await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name), FullPage = true });
    }
}
