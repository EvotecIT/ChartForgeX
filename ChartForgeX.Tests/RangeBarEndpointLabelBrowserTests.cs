using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class RangeBarEndpointLabelBrowserTests {
    [Fact]
    public async Task BothEndpointCaptionsResolveTheSameRawObservationHeadingSelectionAndMuting() {
        if (!Enabled) return;
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        var chart = RangeBarEndpointLabelTests.Fixture(true).WithSize(360, 360).WithLegend(true).WithTheme(ChartTheme.GraphiteDark());
        chart.Series[0].WithInteractionKey("expected").WithPointLabel(0, "Promised <&> delivery");
        chart.Series[1].WithInteractionKey("observed");
        chart.ConfigureLabels(labels => { labels.Category = "Kategoria"; labels.Start = "Początek"; labels.End = "Koniec"; });
        chart.Options.RangeBar.EndpointLabelFormatter = context => context.FormattedValue + (context.Endpoint == ChartRangeBarEndpoint.Start ? " S" : " E");
        if (!string.IsNullOrWhiteSpace(directory)) {
            await File.WriteAllTextAsync(Path.Combine(directory, "range-bar-endpoint-captions.svg"), chart.ToSvg());
            await File.WriteAllBytesAsync(Path.Combine(directory, "range-bar-endpoint-captions.native.png"), chart.ToPng());
        }
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            options.Tooltip.Mode = HtmlChartTooltipMode.SharedX;
            options.Interaction.Disable(ChartInteractionFeatures.Crosshair);
        }), 390, 620);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        var fontPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        await page.AddStyleTagAsync(new PageAddStyleTagOptions { Content = "@font-face{font-family:Carlito;src:url(data:font/ttf;base64,"
            + Convert.ToBase64String(await File.ReadAllBytesAsync(fontPath)) + ") format('truetype');font-weight:400}" });
        await page.EvaluateAsync("async () => { await document.fonts.load('12px Carlito'); await document.fonts.ready; }");
        await page.EvaluateAsync("() => { window.endpointSelections=[]; document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.endpointSelections.push(event.detail.target)); }");
        var point = Point(0, 0);
        var captions = "[data-cfx-role='data-label-source'][data-cfx-label-for='series-0-point-0']";
        Assert.Equal(2, await page.Locator(captions).CountAsync());
        Assert.Equal(new[] { "30 S", "-10 E" }, await page.Locator(captions + " text").AllTextContentsAsync());
        var ids = await page.Locator("[id]").EvaluateAllAsync<string[]>("nodes => nodes.map(node => node.id)");
        Assert.Equal(ids.Length, ids.Distinct().Count());
        var rows = new[] { "Series", "Expected", "Kategoria", "Express", "Początek", "30", "Koniec", "-10" };
        for (var caption = 0; caption < 2; caption++) {
            var box = await page.Locator(captions).Nth(caption).BoundingBoxAsync() ?? throw new InvalidOperationException("Endpoint caption has no layout box");
            await page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2, new MouseMoveOptions { Steps = 4 });
            Assert.Equal(rows, await RowsAsync(page));
            Assert.Equal("Expected: Promised <&> delivery", await page.Locator(".cfx-tooltip__title").InnerTextAsync());
        }
        if (!string.IsNullOrWhiteSpace(directory))
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, "range-bar-endpoint-captions-hover.png") });
        await page.Locator(captions).First.ClickAsync();
        Assert.Equal("true", await page.Locator(point).GetAttributeAsync("aria-selected"));
        Assert.Equal("expected:0", await page.EvaluateAsync<string>("() => window.endpointSelections.at(-1).targetId"));
        await page.Locator(Point(1, 0)).FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await page.Locator(Point(1, 0)).GetAttributeAsync("aria-selected"));
        await page.Locator(point).FocusAsync();
        Assert.Equal("Observed", (await RowsAsync(page))[1]);
        Assert.Equal("true", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-tooltip-pinned"));
        // Focus preserves an explicitly pinned readout. Activate its owner again before inspecting another observation.
        await page.Locator(Point(1, 0)).FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("false", await page.Locator(Point(1, 0)).GetAttributeAsync("aria-selected"));
        await page.Locator(point).FocusAsync();
        Assert.Equal(rows, await RowsAsync(page));
        Assert.Equal("30", await page.Locator(point).GetAttributeAsync("data-cfx-source-0-y"));
        Assert.Equal("-10", await page.Locator(point).GetAttributeAsync("data-cfx-source-1-y"));
        Assert.Equal("0", await page.Locator(point).GetAttributeAsync("data-cfx-source-0-index"));
        Assert.Equal("1", await page.Locator(point).GetAttributeAsync("data-cfx-source-1-index"));
        await page.Locator(Legend(0)).ClickAsync();
        Assert.Equal(1, await page.Locator("g[data-cfx-role='series'][data-cfx-series='0'].cfx-series-muted").CountAsync());
        var opacity = await page.Locator(captions).EvaluateAllAsync<string[]>("nodes => nodes.map(node => getComputedStyle(node).opacity)");
        Assert.All(opacity, value => Assert.True(double.Parse(value, System.Globalization.CultureInfo.InvariantCulture) < 1));
        await page.Locator(Legend(0)).ClickAsync();
        await page.Locator(point).FocusAsync(); Assert.Equal(rows, await RowsAsync(page));
        if (!string.IsNullOrWhiteSpace(directory)) {
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, "range-bar-endpoint-captions-selected.png") });
            await File.WriteAllTextAsync(Path.Combine(directory, "range-bar-endpoint-captions.json"), JsonSerializer.Serialize(new {
                rows, heading = await page.Locator(".cfx-tooltip__title").InnerTextAsync(), mutedCaptionOpacity = opacity,
                selected = await page.Locator(point).GetAttributeAsync("aria-selected"),
                consoleErrors = session.ConsoleLog.Where(entry => entry.Type == HtmlTinkerX.HtmlConsoleMessageType.Error).ToArray(), pageErrors = errors
            }));
        }
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }

    private static Task<string[]> RowsAsync(IPage page) => page.Locator(".cfx-tooltip__meta").EvaluateAsync<string[]>("node => Array.from(node.children).map(child => child.textContent)");
}
