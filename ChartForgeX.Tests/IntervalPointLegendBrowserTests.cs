using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class IntervalPointLegendBrowserTests {
    [BrowserTheory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task PointLegendAndMarkReadoutsRetainLocalizedTupleAndActualSeriesName(bool boxPlot, bool horizontal) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(360, 440).WithTitle("Delivery observations")
            .WithTheme(horizontal ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithXLabels("Express", "Standard").WithPointLegend().WithLegend();
        var seriesName = boxPlot ? "Distribution" : "Delivery window";
        if (boxPlot) chart.AddBoxPlot(seriesName, new[] {
            new ChartBoxPlot(1, 0, 10, 15, 25, 35), new ChartBoxPlot(2, 2, 6, 9, 11, 15)
        });
        else chart.AddRangeBar(seriesName, new[] { new ChartInterval(1, 30, 0), new ChartInterval(2, 10, 35) });
        chart.Series[0].WithInteractionKey("delivery").WithPointLabel(1, "Promised observation");
        if (horizontal) {
            if (boxPlot) chart.ConfigureBoxPlot(options => options.Orientation = ChartOrientation.Horizontal);
            else chart.ConfigureRangeBar(options => options.Orientation = ChartOrientation.Horizontal);
        }
        chart.ConfigureLabels(labels => {
            labels.Category = "Kategoria"; labels.Start = "Początek"; labels.End = "Koniec";
            labels.Minimum = "Minimum"; labels.Q1 = "Dolny kwartyl"; labels.Median = "Mediana";
            labels.Q3 = "Górny kwartyl"; labels.Maximum = "Maksimum";
        });
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), 384, 640);
        var page = session.Page; var pageErrors = new List<string>();
        page.PageError += (_, error) => pageErrors.Add(error);
        var expected = boxPlot
            ? new[] { "Kategoria", "Express", "Minimum", "0", "Dolny kwartyl", "10", "Mediana", "15", "Górny kwartyl", "25", "Maksimum", "35" }
            : new[] { "Kategoria", "Express", "Początek", "30", "Koniec", "0" };
        var legend = "[data-cfx-role='legend-item'][data-cfx-point='0']";
        await MoveToAsync(page, legend);
        Assert.Equal("Express", await page.Locator(".cfx-tooltip__title").InnerTextAsync());
        Assert.Equal(expected, await RowsAsync(page));
        await CaptureAsync("legend");
        await page.Locator(legend).FocusAsync();
        Assert.Equal(expected, await RowsAsync(page));

        await MoveToAsync(page, Point(0, 0) + (boxPlot ? " [data-cfx-role='boxplot-body']" : " [data-cfx-role='range-bar']"));
        var heading = seriesName + (boxPlot ? ": 15" : ": 0–30");
        Assert.Equal(heading, await page.Locator(".cfx-tooltip__title").InnerTextAsync());
        Assert.Equal(new[] { "Series", seriesName }.Concat(expected), await RowsAsync(page));
        await CaptureAsync("mark");
        await page.Locator(Point(0, 0)).FocusAsync();
        Assert.Equal(heading, await page.Locator(".cfx-tooltip__title").InnerTextAsync());
        Assert.Equal(new[] { "Series", seriesName }.Concat(expected), await RowsAsync(page));
        await page.Locator(Point(0, 1)).FocusAsync();
        Assert.Equal(seriesName + ": Promised observation", await page.Locator(".cfx-tooltip__title").InnerTextAsync());
        Assert.Equal(seriesName, (await RowsAsync(page))[1]);
        await CaptureAsync("keyboard-custom");
        Assert.Empty(pageErrors); AssertNoConsoleErrors(session);

        async Task CaptureAsync(string state) {
            var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
            if (string.IsNullOrWhiteSpace(directory)) return;
            Directory.CreateDirectory(directory);
            var name = "interval-point-legend-" + (boxPlot ? "box" : "range") + "-" + (horizontal ? "horizontal" : "vertical") + "-" + state;
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png") });
            await File.WriteAllTextAsync(Path.Combine(directory, name + ".json"), JsonSerializer.Serialize(new {
                heading = await page.Locator(".cfx-tooltip__title").InnerTextAsync(), rows = await RowsAsync(page), pageErrors,
                consoleErrors = session.ConsoleLog.Where(entry => entry.Type == HtmlTinkerX.HtmlConsoleMessageType.Error).ToArray()
            }));
        }
    }

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ComparisonPointLegendsRetainBothEndpointsOrUncertaintyBounds(bool errorBar) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(400, 360).WithXLabels("Express", "Standard").WithPointLegend().WithLegend();
        if (errorBar) chart.AddErrorBar("Uncertainty", new[] { new ChartErrorBar(1, 15, 0, 35), new ChartErrorBar(2, 20, 10, 25) });
        else chart.AddDumbbell("Change", new[] { new ChartDumbbell(1, 30, 0), new ChartDumbbell(2, 10, 35) });
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), 450, 460);
        var page = session.Page;
        var expected = errorBar ? new[] { "Category", "Express", "Value", "15", "Lower", "0", "Upper", "35" }
            : new[] { "Category", "Express", "Start", "30", "End", "0", "Delta", "-30" };
        var legend = "[data-cfx-role='legend-item'][data-cfx-point='0']";
        await MoveToAsync(page, legend);
        Assert.Equal("Express", await page.Locator(".cfx-tooltip__title").InnerTextAsync());
        Assert.Equal(expected, await RowsAsync(page));
        await page.Locator(legend).FocusAsync();
        Assert.Equal(expected, await RowsAsync(page));
        await page.Locator("[data-cfx-role='legend-item'][data-cfx-point='1']").FocusAsync();
        Assert.Equal("Standard", await page.Locator(".cfx-tooltip__title").InnerTextAsync());
        Assert.Equal(errorBar ? new[] { "Category", "Standard", "Value", "20", "Lower", "10", "Upper", "25" }
            : new[] { "Category", "Standard", "Start", "10", "End", "35", "Delta", "25" }, await RowsAsync(page));
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) await page.ScreenshotAsync(new PageScreenshotOptions {
            Path = Path.Combine(directory, "interval-point-legend-" + (errorBar ? "error" : "dumbbell") + ".png")
        });
        AssertNoConsoleErrors(session);
    }

    private static Task<string[]> RowsAsync(IPage page) => page.Locator(".cfx-tooltip__meta")
        .EvaluateAsync<string[]>("node => Array.from(node.children).map(child => child.textContent)");
}
