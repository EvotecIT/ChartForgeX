using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class BoxPlotBrowserTests {
    [BrowserFact]
    public async Task NeighboringHorizontalSummariesKeepAllFiveLocalizedFactsAndIndependentSelectionAndMuting() {
        if (!Enabled) return;
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        var chart = V2GalleryModels.Create(ChartSeriesKind.BoxPlot, "compact-horizontal")
            .WithSize(360, 440).WithTheme(VisualTheme.Graphite(), VisualThemeMode.Dark).WithDataLabels(false).WithFontFamily("Carlito")
            .WithPngFont(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf"));
        chart.Options.XAxis.WithBounds(0, 60);
        chart.ConfigureLabels(labels => { labels.Category = "Kategoria"; labels.Minimum = "Minimum lokalne"; labels.Q1 = "Dolny kwartyl"; labels.Median = "Mediana"; labels.Q3 = "Górny kwartyl"; labels.Maximum = "Maksimum"; });
        chart.Series[0].WithPointLabel(1, "Promised distribution"); chart.Options.YAxis.Labels.AddRange(chart.Options.XAxis.Labels.Select(label => new ChartAxisLabel(label.Value,
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
        await page.EvaluateAsync("() => { window.boxSelections=[]; document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.boxSelections.push(event.detail.target)); }");
        var first = Point(0, 1); var neighbor = Point(1, 1);
        await MoveToAsync(page, first + " [data-cfx-role='boxplot-body']");
        Assert.Equal(new[] { "Series", "Expected", "Kategoria", "Express delivery", "Minimum lokalne", "5", "Dolny kwartyl", "10", "Mediana", "15", "Górny kwartyl", "25", "Maksimum", "35" }, await RowsAsync(page));
        var customHeading = await page.Locator(".cfx-tooltip__title").InnerTextAsync();
        Assert.Equal("Expected: Promised distribution", customHeading);
        await MoveToAsync(page, neighbor + " [data-cfx-role='boxplot-body']");
        var rawRows = await RowsAsync(page);
        Assert.Equal(new[] { "Series", "Observed", "Kategoria", "Express delivery", "Minimum lokalne", "8", "Dolny kwartyl", "12", "Mediana", "22", "Górny kwartyl", "30", "Maksimum", "42" }, rawRows);
        var defaultHeading = await page.Locator(".cfx-tooltip__title").InnerTextAsync();
        Assert.Equal("Observed: 22", defaultHeading);
        var rawAria = await page.Locator(neighbor).GetAttributeAsync("aria-label");
        Assert.Contains("x=2 minimum=8 q1=12 median=22 q3=30 maximum=42", rawAria);
        if (!string.IsNullOrWhiteSpace(directory))
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, "box-plot-five-number-hover.png") });
        await page.Locator(neighbor).FocusAsync();
        Assert.Equal(rawRows, await RowsAsync(page));
        Assert.Equal(defaultHeading, await page.Locator(".cfx-tooltip__title").InnerTextAsync());
        var raw = new[] { "8", "12", "22", "30", "42" };
        var fields = new[] { "minimum", "q1", "median", "q3", "maximum" };
        for (var member = 0; member < raw.Length; member++) {
            Assert.Equal(raw[member], await page.Locator(neighbor).GetAttributeAsync("data-cfx-" + fields[member]));
        }
        Assert.Equal("1", await page.Locator(neighbor).GetAttributeAsync("data-cfx-source-point"));
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await page.Locator(neighbor).GetAttributeAsync("aria-selected"));
        Assert.Equal("observed:1", await page.EvaluateAsync<string>("() => window.boxSelections.at(-1).targetId"));
        Assert.Equal("point", await page.EvaluateAsync<string>("() => window.boxSelections.at(-1).targetKind"));
        await page.Locator(first).FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await page.Locator(first).GetAttributeAsync("aria-selected"));
        Assert.Equal("true", await page.Locator(neighbor).GetAttributeAsync("aria-selected"));
        Assert.Equal(new[] { "Standard", "Express delivery", "Premium", "Bulk" }, await page.Locator("[data-cfx-role='axis-y-label']").AllTextContentsAsync());
        await page.Locator(Legend(1)).ClickAsync();
        Assert.Equal(1, await page.Locator("g[data-cfx-role='series'][data-cfx-series='1'].cfx-series-muted").CountAsync());
        await page.Locator(first).FocusAsync();
        Assert.Equal("Expected", (await RowsAsync(page))[1]);
        if (!string.IsNullOrWhiteSpace(directory)) {
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, "box-plot-neighbors.png") });
            await File.WriteAllTextAsync(Path.Combine(directory, "box-plot-neighbors.json"), JsonSerializer.Serialize(new {
                rawRows, defaultHeading, customHeading, rawAria, firstSelected = await page.Locator(first).GetAttributeAsync("aria-selected"),
                neighborSelected = await page.Locator(neighbor).GetAttributeAsync("aria-selected"),
                consoleErrors = session.ConsoleLog.Where(entry => entry.Type == HtmlTinkerX.HtmlConsoleMessageType.Error).ToArray(), pageErrors = errors
            }));
        }
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task RawSampleSummaryKeepsInterpolatedFactsAndDerivedSelectionIdentity() {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(640, 400).WithXLabels("Raw input").WithPointLegend().WithLegend()
            .AddSeries(ChartSeries.CreateBoxPlot(new ChartSeriesId("raw-distribution"), "Distribution", 1, new[] { 30d, 0, 20, 10 }));
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => options.Tooltip.Mode = HtmlChartTooltipMode.SharedX));
        var page = session.Page;
        await page.EvaluateAsync("() => { window.rawSelections=[]; document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.rawSelections.push(event.detail.target)); }");
        await MoveToAsync(page, Legend(0));
        Assert.Equal(new[] { "Category", "Raw input", "Minimum", "0", "Q1", "7.5", "Median", "15", "Q3", "22.5", "Maximum", "30" }, await RowsAsync(page));
        var point = page.Locator(Point(0, 0));
        await point.FocusAsync();
        Assert.Equal(new[] { "Series", "Distribution", "Category", "Raw input", "Minimum", "0", "Q1", "7.5", "Median", "15", "Q3", "22.5", "Maximum", "30" }, await RowsAsync(page));
        Assert.Equal("Distribution: 15", await page.Locator(".cfx-tooltip__title").InnerTextAsync());
        Assert.Equal("five-number-summary", await point.GetAttributeAsync("data-cfx-derived"));
        Assert.Equal("4", await point.GetAttributeAsync("data-cfx-source-count"));
        Assert.Null(await point.GetAttributeAsync("data-cfx-source-point"));
        await page.Keyboard.PressAsync("Space");
        using var events = JsonDocument.Parse(await page.EvaluateAsync<string>("() => JSON.stringify(window.rawSelections.at(-1))"));
        var target = events.RootElement;
        Assert.Equal("raw-distribution:derived:five-number-summary:0", target.GetProperty("targetId").GetString());
        Assert.False(target.TryGetProperty("sourcePoint", out _));
        Assert.False(target.TryGetProperty("sourcePoints", out _));
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, "box-plot-raw-summary.png") });
        AssertNoConsoleErrors(session);
    }

    private static Task<string[]> RowsAsync(IPage page) => page.Locator(".cfx-tooltip__meta").EvaluateAsync<string[]>("node => Array.from(node.children).map(child => child.textContent)");
}
