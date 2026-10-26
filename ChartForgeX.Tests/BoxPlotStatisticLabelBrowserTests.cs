using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class BoxPlotStatisticLabelBrowserTests {
    [BrowserTheory]
    [Trait("BrowserSuite", "Compatibility")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EveryStatisticCaptionSelectsAndReadsTheSameCompleteSummaryWithoutInventingSampleIdentity(bool horizontal) {
        if (!Enabled) return;
        var chart = BoxPlotStatisticLabelTests.Fixture(horizontal).WithLegend(true);
        chart.Series[0].WithPointLabel(0, "Delivery interval summary");
        chart.ConfigureBoxPlot(options => {
            options.LabelContent = ChartBoxPlotLabelContent.Statistics;
            options.StatisticLabelFormatter = context => context.Statistic + ": " + context.FormattedValue;
            foreach (var label in new[] { options.MinimumLabel, options.Q1Label, options.MedianLabel, options.Q3Label, options.MaximumLabel })
                label.Placement = horizontal ? ChartDataLabelPlacement.Above : ChartDataLabelPlacement.Right;
        });
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), 920, 650);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        var font = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        await page.AddStyleTagAsync(new PageAddStyleTagOptions { Content = "@font-face{font-family:Carlito;src:url(data:font/ttf;base64,"
            + Convert.ToBase64String(await File.ReadAllBytesAsync(font)) + ") format('truetype');font-weight:400}" });
        await page.EvaluateAsync("async () => { await document.fonts.load('12px Carlito'); await document.fonts.ready; }");
        await page.EvaluateAsync("() => { window.statisticSelections=[]; document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => statisticSelections.push(event.detail.target)); }");
        var selector = "[data-cfx-role='data-label-source'][data-cfx-label-for='series-0-point-0']";
        Assert.Equal(5, await page.Locator(selector).CountAsync());
        var expectedRows = new[] { "Series", "Delivery", "Category", "Service A", "Minimum", "-30", "Q1", "-10", "Median", "10", "Q3", "30", "Maximum", "50" };
        var names = new[] { "minimum", "q1", "median", "q3", "maximum" };
        for (var member = 0; member < names.Length; member++) {
            var caption = page.Locator(selector).Nth(member);
            await BrowserProofCaptions.MoveAsync(page, caption);
            Assert.Equal(expectedRows, await RowsAsync(page));
            Assert.Equal("Delivery: Delivery interval summary", await page.Locator(".cfx-tooltip__title").InnerTextAsync());
        }
        await BrowserProofCaptions.ClickAsync(page, page.Locator(selector).First); Assert.Equal("true", await page.Locator(Point(0, 0)).GetAttributeAsync("aria-selected"));
        using var selected = JsonDocument.Parse(await page.EvaluateAsync<string>("() => JSON.stringify(statisticSelections.at(-1))"));
        Assert.Equal("summary:0", selected.RootElement.GetProperty("targetId").GetString());
        Assert.Equal("0", selected.RootElement.GetProperty("sourcePoint").GetString());
        await page.Locator(Point(0, 0)).FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("false", await page.Locator(Point(0, 0)).GetAttributeAsync("aria-selected"));
        await page.Locator(Legend(0)).ClickAsync(); Assert.All(await OpacityAsync(page, selector), opacity => Assert.True(opacity < 1));
        await page.Locator(Legend(0)).ClickAsync(); Assert.All(await OpacityAsync(page, selector), opacity => Assert.Equal(1, opacity));
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, "boxplot-statistics-" + horizontal + ".png") });
            await File.WriteAllTextAsync(Path.Combine(directory, "boxplot-statistics-" + horizontal + ".json"), JsonSerializer.Serialize(new {
                expectedRows, captions = await page.Locator(selector + " text").AllTextContentsAsync(), pageErrors = errors,
                consoleErrors = session.ConsoleLog.Where(entry => entry.Type == HtmlTinkerX.HtmlConsoleMessageType.Error).ToArray()
            }));
        }
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }

    private static Task<string[]> RowsAsync(IPage page) => page.Locator(".cfx-tooltip__meta").EvaluateAsync<string[]>("node => Array.from(node.children).map(child => child.textContent)");
    private static Task<double[]> OpacityAsync(IPage page, string selector) => page.Locator(selector).EvaluateAllAsync<double[]>(
        "nodes => nodes.map(node => { let opacity=1; for(let n=node;n;n=n.parentElement) opacity*=Number(getComputedStyle(n).opacity); return opacity; })");
}
