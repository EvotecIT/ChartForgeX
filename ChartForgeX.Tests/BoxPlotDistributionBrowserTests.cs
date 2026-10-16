using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class BoxPlotDistributionBrowserTests {
    private const string Summary = "g[data-cfx-role='point'][data-cfx-derived='five-number-summary']";
    private const string Outlier = "g[data-cfx-role='point'][data-cfx-kind='BoxPlotOutlier']";
    private const string FirstLegend = "[data-cfx-role='legend-item'][data-cfx-group-index='0']";

    [BrowserTheory]
    [InlineData(false, 390)]
    [InlineData(true, 390)]
    [InlineData(false, 900)]
    [InlineData(true, 900)]
    public async Task RawGroupLegendsOwnAllTheirMarksWhileOutliersKeepSampleSelectionAndLocalizedReadouts(bool dark, int width) {
        if (!Enabled) return;
        var chart = Model(dark, width);
        chart.ConfigureLabels(labels => {
            labels.Category = "Kategoria"; labels.LowerWhisker = "Dolny wąs"; labels.UpperWhisker = "Górny wąs";
            labels.SampleCount = "Próbki"; labels.SourceMinimum = "Minimum próbek"; labels.OutlierCount = "Odstające";
            labels.Outlier = "Pomiar odstający";
        });
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), width + 24, 600);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await page.EvaluateAsync("() => { window.distributionSelections=[]; document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', e => distributionSelections.push(e.detail.target)); }");
        var summary = page.Locator(Summary + "[data-cfx-group-index='0']");
        var outlier = page.Locator(Outlier);
        await FocusWithKeyboardAsync(page, summary);
        var rows = await RowsAsync(page);
        Assert.Contains("Dolny wąs", rows); Assert.Contains("Górny wąs", rows); Assert.Contains("Próbki", rows);
        Assert.Contains("Minimum próbek", rows); Assert.Contains("Odstające", rows); Assert.Contains("100", rows);
        Assert.Null(await summary.GetAttributeAsync("data-cfx-source-point"));
        Assert.Null(await summary.GetAttributeAsync("data-cfx-observation-id"));
        Assert.Equal("[\"latency\",\"five-number-summary\",\"A\"]", await summary.GetAttributeAsync("data-cfx-target-id"));
        var accessible = await summary.GetAttributeAsync("aria-label");
        Assert.Contains("Dolny wąs=", accessible); Assert.Contains("Górny wąs=", accessible);
        Assert.Contains("Minimum próbek=", accessible);
        await MoveToAsync(page, Outlier);
        Assert.Equal(new[] { "Series", "Latency", "Kategoria", "Service A", "Pomiar odstający", "100" }, await RowsAsync(page));
        await FocusWithKeyboardAsync(page, outlier); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await outlier.GetAttributeAsync("aria-selected"));
        Assert.NotEqual("true", await summary.GetAttributeAsync("aria-selected"));
        using var selected = JsonDocument.Parse(await page.EvaluateAsync<string>("() => JSON.stringify(distributionSelections.at(-1))"));
        Assert.Equal("[\"latency\",\"A\"]", selected.RootElement.GetProperty("targetId").GetString());
        Assert.Equal("9", selected.RootElement.GetProperty("sourcePoint").GetString());
        Assert.Equal("A", selected.RootElement.GetProperty("groupId").GetString());
        Assert.False(selected.RootElement.TryGetProperty("derived", out _));
        await MoveToAsync(page, FirstLegend);
        Assert.Contains("cfx-hover-series", await outlier.GetAttributeAsync("class"));
        Assert.DoesNotContain("cfx-hover-series", await page.Locator(Summary + "[data-cfx-group-index='1']").GetAttributeAsync("class"));
        var legend = page.Locator(FirstLegend);
        await legend.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Contains("cfx-series-muted", await summary.GetAttributeAsync("class"));
        Assert.Contains("cfx-series-muted", await outlier.GetAttributeAsync("class"));
        Assert.DoesNotContain("cfx-series-muted", await page.Locator(Summary + "[data-cfx-group-index='1']").GetAttributeAsync("class"));
        await page.Keyboard.PressAsync("Space"); await page.Keyboard.PressAsync("i");
        Assert.Contains("cfx-series-isolated-in", await summary.GetAttributeAsync("class"));
        Assert.Contains("cfx-series-isolated-in", await outlier.GetAttributeAsync("class"));
        Assert.Contains("cfx-series-isolated-out", await page.Locator(Summary + "[data-cfx-group-index='1']").GetAttributeAsync("class"));
        await page.Keyboard.PressAsync("i");
        await FocusWithKeyboardAsync(page, summary);
        await CaptureAsync(page, $"boxplot-raw-group-{width}-{(dark ? "dark" : "light")}");
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task SparseAndNoInlierDistributionsKeepMeasuredExtremaSeparateFromInterpolatedWhiskers() {
        if (!Enabled) return;
        var series = ChartSeries.CreateBoxPlot(new ChartSeriesId("sparse"), "Sparse measurements", new[] {
            new ChartBoxPlotSampleGroup(1, new[] { 0d, 100, 101, 102 }), new ChartBoxPlotSampleGroup(2, new[] { 0d, 100 })
        }).ConfigureBoxPlotStatistics(options => { options.WhiskerPolicy = ChartBoxPlotWhiskerPolicy.Iqr; options.IqrMultiplier = 0.0001; });
        var chart = Chart.Create().WithSize(700, 400).WithXLabels("Sparse", "No inliers").WithDataLabels(false).AddSeries(series);
        chart.ConfigureLabels(labels => { labels.SourceMinimum = "Minimum źródła"; labels.InlierMinimum = "Minimum wewnętrzne"; labels.NoInliers = "Brak"; });
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), 740, 600);
        var page = session.Page;
        await FocusWithKeyboardAsync(page, page.Locator(Summary + "[data-cfx-group-index='0']"));
        var rows = await RowsAsync(page);
        Assert.True(Array.IndexOf(rows, "Minimum źródła") >= 0); Assert.True(Array.IndexOf(rows, "Minimum wewnętrzne") >= 0);
        Assert.Equal("0", rows[Array.IndexOf(rows, "Minimum źródła") + 1]);
        Assert.Equal("100", rows[Array.IndexOf(rows, "Minimum wewnętrzne") + 1]);
        Assert.Equal("75", rows[Array.IndexOf(rows, "Lower whisker") + 1]);
        await FocusWithKeyboardAsync(page, page.Locator(Summary + "[data-cfx-group-index='1']"));
        rows = await RowsAsync(page);
        Assert.Equal("Brak", rows[Array.IndexOf(rows, "Minimum wewnętrzne") + 1]);
        Assert.Equal("25", rows[Array.IndexOf(rows, "Lower whisker") + 1]);
        Assert.Equal("75", rows[Array.IndexOf(rows, "Upper whisker") + 1]);
        var accessible = await page.Locator(Summary + "[data-cfx-group-index='1']").GetAttributeAsync("aria-label");
        Assert.Contains("Lower whisker=25", accessible); Assert.Contains("Upper whisker=75", accessible);
        Assert.Contains("Minimum źródła=0", accessible); Assert.Contains("Minimum wewnętrzne=Brak", accessible);
        await CaptureAsync(page, "boxplot-no-inlier-readout"); AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task PreparedReplacementRetainsGroupAndSampleKeysIndependentlyAndClearsInliersRemovedGroupsAndOrdinals() {
        if (!Enabled) return;
        await using var session = await OpenAsync(Model(false, 700).ToInteractiveHtmlPage(), 740, 600);
        var page = session.Page;
        await page.EvaluateAsync("() => { window.distributionController=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')); }");
        await FocusWithKeyboardAsync(page, page.Locator(Summary + "[data-cfx-group-id='A']")); await page.Keyboard.PressAsync("Space");
        await FocusWithKeyboardAsync(page, page.Locator(Outlier)); await page.Keyboard.PressAsync("Space");
        Assert.Equal(2, await CountAsync());
        await UpdateAsync(Model(false, 700, reverse: true));
        Assert.Equal(2, await CountAsync());
        Assert.Equal("true", await page.Locator(Summary + "[data-cfx-group-index='1']").GetAttributeAsync("aria-selected"));
        Assert.Equal("true", await page.Locator(Outlier).GetAttributeAsync("aria-selected"));
        Assert.Equal("14", await page.Locator(Outlier).GetAttributeAsync("data-cfx-source-point"));
        var noOutliers = Model(false, 700, reverse: true);
        noOutliers.Series[0].ConfigureBoxPlotStatistics(options => options.WhiskerPolicy = ChartBoxPlotWhiskerPolicy.MinMax);
        await UpdateAsync(noOutliers);
        Assert.Equal(0, await page.Locator(Outlier).CountAsync()); Assert.Equal(1, await CountAsync());
        Assert.Equal("[\"latency\",\"five-number-summary\",\"A\"]", await page.EvaluateAsync<string>("() => distributionController.getSelection()[0].targetId"));
        await UpdateAsync(Model(false, 700, onlySecond: true)); Assert.Equal(0, await CountAsync());
        await UpdateAsync(Model(false, 700, keyed: false));
        await FocusWithKeyboardAsync(page, page.Locator(Summary + "[data-cfx-group-index='0']")); await page.Keyboard.PressAsync("Space");
        Assert.Equal(1, await CountAsync());
        await UpdateAsync(Model(false, 700, keyed: false)); Assert.Equal(0, await CountAsync());
        await CaptureAsync(page, "boxplot-raw-group-replacement"); AssertNoConsoleErrors(session);

        Task<int> CountAsync() => page.EvaluateAsync<int>("() => distributionController.getSelection().length");
        Task UpdateAsync(Chart chart) => page.EvaluateAsync("html => distributionController.update(html)", chart.ToInteractiveHtmlFragmentWithoutAssets());
    }

    private static Chart Model(bool dark, int width, bool reverse = false, bool onlySecond = false, bool keyed = true) {
        var first = new ChartBoxPlotSampleGroup(1, new[] { 0d, 1, 2, 3, 4, 5, 6, 7, 8, 100 });
        var second = new ChartBoxPlotSampleGroup(2, new[] { 2d, 3, 4, 5, 6 });
        var groups = onlySecond ? new[] { second } : reverse ? new[] { second, first } : new[] { first, second };
        var series = ChartSeries.CreateBoxPlot(new ChartSeriesId("latency"), "Latency", groups)
            .ConfigureBoxPlotStatistics(options => options.WhiskerPolicy = ChartBoxPlotWhiskerPolicy.Iqr);
        if (keyed) {
            series.WithBoxPlotGroupIds(groups.Select(group => new ChartObservationId(group.X == 1 ? "A" : "B")));
            series.WithObservationIds(groups.SelectMany(group => group.Samples.Select((_, index) =>
                new ChartObservationId(group.X == 1 && index == 9 ? "A" : (group.X == 1 ? "a-" : "b-") + index))));
        }
        return Chart.Create().WithSize(width, 400).WithTitle("Service latency").WithXLabels("Service A", "Service B")
            .WithPointLegend().WithDataLabels(false).WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light).AddSeries(series);
    }

    private static Task<string[]> RowsAsync(IPage page) => page.Locator(".cfx-tooltip__meta").EvaluateAsync<string[]>("node => Array.from(node.children).map(child => child.textContent)");
    private static async Task CaptureAsync(IPage page, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png") });
        var modules = new[] { typeof(Chart).Assembly, typeof(HtmlChartInteractionOptions).Assembly, typeof(HtmlTinkerX.HtmlBrowser).Assembly, typeof(BoxPlotDistributionBrowserTests).Assembly }
            .Select(assembly => new { path = assembly.Location, sha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(assembly.Location))) }).ToArray();
        var browsers = new List<object>();
        foreach (var process in System.Diagnostics.Process.GetProcessesByName("chrome-headless-shell")) {
            using (process) {
                var path = process.MainModule!.FileName;
                browsers.Add(new { pid = process.Id, startedUtc = process.StartTime.ToUniversalTime(), path,
                    sha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))) });
            }
        }
        File.WriteAllText(Path.Combine(directory, name + ".runtime.json"), JsonSerializer.Serialize(new {
            pid = Environment.ProcessId, modules, browsers, browserVersion = page.Context.Browser!.Version
        }));
    }
}
