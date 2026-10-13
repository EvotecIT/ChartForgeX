using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class RangeBarBrowserTests {
    [BrowserFact]
    public async Task NeighboringHorizontalIntervalsKeepLocalizedRawReadoutsAndIndependentSelectionAndMuting() {
        if (!Enabled) return;
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        var chart = V2GalleryModels.Create(ChartSeriesKind.RangeBar, "compact-horizontal")
            .WithSize(360, 440).WithTheme(ChartTheme.GraphiteDark()).WithDataLabels(false).WithFontFamily("Carlito")
            .WithPngFont(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf"));
        chart.Options.XAxis.WithBounds(0, 60);
        chart.ConfigureLabels(labels => { labels.Category = "Kategoria"; labels.Start = "Początek"; labels.End = "Koniec"; });
        chart.Series[0].WithInteractionKey("expected").WithPointLabel(1, "Promised delivery"); chart.Series[1].WithInteractionKey("observed");
        chart.Options.YAxis.Labels.AddRange(chart.Options.XAxis.Labels.Select(label => new ChartAxisLabel(label.Value,
            label.Value == 2 ? "Express delivery" : label.Text)));
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            options.Tooltip.Mode = HtmlChartTooltipMode.SharedX;
            options.Interaction.Disable(ChartInteractionFeatures.Crosshair);
        }), 384, 600);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        var fontDirectory = Path.GetDirectoryName(chart.Options.PngFontPath)!;
        await page.AddStyleTagAsync(new PageAddStyleTagOptions { Content =
            "@font-face{font-family:Carlito;src:url(data:font/ttf;base64," + Convert.ToBase64String(await File.ReadAllBytesAsync(Path.Combine(fontDirectory, "Carlito-Regular.ttf"))) + ") format('truetype');font-weight:400}"
            + "@font-face{font-family:Carlito;src:url(data:font/ttf;base64," + Convert.ToBase64String(await File.ReadAllBytesAsync(Path.Combine(fontDirectory, "Carlito-Bold.ttf"))) + ") format('truetype');font-weight:700}" });
        await page.EvaluateAsync("async () => { await document.fonts.load('12px Carlito'); await document.fonts.load('700 18px Carlito'); await document.fonts.ready; }");
        Assert.True(await page.EvaluateAsync<bool>("() => [...document.fonts].filter(face => face.family === 'Carlito' && face.status === 'loaded').length === 2"));
        await page.EvaluateAsync("() => { window.rangeSelections=[]; document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.rangeSelections.push(event.detail.target)); }");
        var first = Point(0, 1); var neighbor = Point(1, 1);
        await MoveToAsync(page, first + " [data-cfx-role='range-bar']");
        Assert.Equal(new[] { "Series", "Expected", "Kategoria", "Express delivery", "Początek", "10", "Koniec", "30" }, await RowsAsync(page));
        var customHeading = await page.Locator(".cfx-tooltip__title").InnerTextAsync();
        Assert.Equal("Expected: Promised delivery", customHeading);
        await MoveToAsync(page, neighbor + " [data-cfx-role='range-bar']");
        var rawRows = await RowsAsync(page);
        Assert.Equal(new[] { "Series", "Observed", "Kategoria", "Express delivery", "Początek", "32", "Koniec", "12" }, rawRows);
        var defaultHeading = await page.Locator(".cfx-tooltip__title").InnerTextAsync();
        Assert.Equal("Observed: 12–32", defaultHeading);
        var rawAria = await page.Locator(neighbor).GetAttributeAsync("aria-label");
        Assert.Contains("x=2 start=32 end=12", rawAria);
        if (!string.IsNullOrWhiteSpace(directory))
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, "range-bar-descending-hover.png") });
        await page.Locator(neighbor).FocusAsync();
        Assert.Equal(rawRows, await RowsAsync(page));
        Assert.Equal(defaultHeading, await page.Locator(".cfx-tooltip__title").InnerTextAsync());
        Assert.Equal("32", await page.Locator(neighbor).GetAttributeAsync("data-cfx-source-0-y"));
        Assert.Equal("12", await page.Locator(neighbor).GetAttributeAsync("data-cfx-source-1-y"));
        Assert.Equal("2", await page.Locator(neighbor).GetAttributeAsync("data-cfx-source-0-index"));
        Assert.Equal("3", await page.Locator(neighbor).GetAttributeAsync("data-cfx-source-1-index"));
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await page.Locator(neighbor).GetAttributeAsync("aria-selected"));
        Assert.Equal("observed:1", await page.EvaluateAsync<string>("() => window.rangeSelections.at(-1).targetId"));
        Assert.Equal("point", await page.EvaluateAsync<string>("() => window.rangeSelections.at(-1).targetKind"));
        await page.Locator(first).FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await page.Locator(first).GetAttributeAsync("aria-selected"));
        Assert.Equal("true", await page.Locator(neighbor).GetAttributeAsync("aria-selected"));
        Assert.Equal(new[] { "Standard", "Express delivery", "Premium", "Bulk" }, await page.Locator("[data-cfx-role='axis-y-label']").AllTextContentsAsync());
        await page.Locator(Legend(1)).ClickAsync();
        Assert.Equal(1, await page.Locator("g[data-cfx-role='series'][data-cfx-series='1'].cfx-series-muted").CountAsync());
        await page.Locator(first).FocusAsync();
        Assert.Equal("Expected", (await RowsAsync(page))[1]);
        if (!string.IsNullOrWhiteSpace(directory)) {
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, "range-bar-neighbors.png") });
            await File.WriteAllTextAsync(Path.Combine(directory, "range-bar-neighbors.json"), JsonSerializer.Serialize(new {
                rawRows, defaultHeading, customHeading, rawAria, firstSelected = await page.Locator(first).GetAttributeAsync("aria-selected"),
                neighborSelected = await page.Locator(neighbor).GetAttributeAsync("aria-selected"),
                consoleErrors = session.ConsoleLog.Where(entry => entry.Type == HtmlTinkerX.HtmlConsoleMessageType.Error).ToArray(), pageErrors = errors
            }));
        }
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }

    private static Task<string[]> RowsAsync(IPage page) => page.Locator(".cfx-tooltip__meta").EvaluateAsync<string[]>("node => Array.from(node.children).map(child => child.textContent)");
}
