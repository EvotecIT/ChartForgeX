using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractiveTreemapColorTooltipBrowserTests {
    [Theory]
    [InlineData(360, false, false, false)]
    [InlineData(800, true, true, false)]
    [InlineData(360, true, true, true)]
    [InlineData(800, false, false, true)]
    public async Task TooltipsKeepAreaAndIndependentColorFactsWithLocalizedMissingStatus(int width, bool dark, bool titleOverride, bool leafKeys) {
        if (!Enabled) return;
        var colorLabel = titleOverride ? "Zmiana (%) <&>" : "Kolor";
        var chart = Chart.Create().WithSize(width, 380).WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithTitle("Przydział pracy").WithDataLabels(false).WithLabels(labels => { labels.Color = "Kolor"; labels.NoData = "Brak danych"; })
            .AddTreemap("Zespoły", new[] {
                new ChartHierarchyItem("positive", "Obsługa", value: 9, colorValue: 7.125),
                new ChartHierarchyItem("zero", "Zerowa zmiana", value: 4, colorValue: 0),
                new ChartHierarchyItem("missing", "Nieznana zmiana", value: 2)
            }).ConfigureTreemap(options => {
                options.ColorScale = ChartColorScale.Sequential(ChartForgeX.Primitives.ChartColor.FromHex("#DAE8F8"), ChartForgeX.Primitives.ChartColor.FromHex("#1C5CAB"));
                options.ShowColorScaleLegend = !leafKeys;
                if (titleOverride) options.ColorLegendTitle = colorLabel;
            });
        if (leafKeys) chart.WithPointLegend();
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var svg = prepared.ToSvg();
        var html = chart.ToInteractiveHtmlPage(options => options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit);
        chart.WithLabels(labels => { labels.Color = "Changed"; labels.NoData = "Changed"; });
        chart.Options.Treemap.ColorLegendTitle = "Changed";
        Assert.Equal(svg, prepared.ToSvg());
        await using var session = await OpenAsync(html, width + 24, 500);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        var observed = new Dictionary<string, Dictionary<string, string>>();
        var cases = new[] { "positive", "zero", "missing" }.Select(id => (Id: id, Key: false));
        if (leafKeys) cases = cases.Concat(new[] { "positive", "zero", "missing" }.Select(id => (Id: id, Key: true)));
        foreach (var (id, key) in cases) {
            var selector = "[data-cfx-node=" + id + "]";
            await MoveAwayAsync(page);
            if (key) await page.Locator("[data-cfx-legend-target-id=" + id + "]").FocusAsync();
            else if (id == "zero") await page.Locator(selector).FocusAsync();
            else await MoveToAsync(page, selector + " > [data-cfx-role=treemap-tile-mark]");
            await page.WaitForFunctionAsync("label => { const tip = document.querySelector('.cfx-tooltip'); return tip && !tip.hidden && tip.querySelector('.cfx-tooltip__title').textContent.includes(label); }",
                id == "positive" ? "Obsługa" : id == "zero" ? "Zerowa zmiana" : "Nieznana zmiana");
            using var rows = JsonDocument.Parse(await page.EvaluateAsync<string>("""
                () => JSON.stringify(Object.fromEntries(Array.from(document.querySelectorAll('.cfx-tooltip dt')).map(term => [term.textContent, term.nextElementSibling.textContent])))
                """));
            var name = id + (key ? "-key" : "");
            observed[name] = rows.RootElement.EnumerateObject().ToDictionary(row => row.Name, row => row.Value.GetString()!);
            await Capture(page, "treemap-color-tooltip-" + width + "-" + (dark ? "dark" : "light") + "-" + name, observed[name]);
        }
        Assert.Equal("9", observed["positive"]["Value"]);
        Assert.Equal("4", observed["zero"]["Value"]);
        Assert.Equal("2", observed["missing"]["Value"]);
        Assert.True(observed["positive"].ContainsKey(colorLabel), "The tooltip must include an independent color measurement row.");
        Assert.Equal("7.125", observed["positive"][colorLabel]);
        Assert.Equal("0", observed["zero"][colorLabel]);
        Assert.Equal("Brak danych", observed["missing"][colorLabel]);
        if (leafKeys) foreach (var id in new[] { "positive", "zero", "missing" }) {
            Assert.Equal(observed[id]["Value"], observed[id + "-key"]["Value"]);
            Assert.Equal(observed[id][colorLabel], observed[id + "-key"][colorLabel]);
        }
        Assert.Equal("7.125", await page.Locator("[data-cfx-node=positive]").GetAttributeAsync("data-cfx-color-value"));
        Assert.Equal("0", await page.Locator("[data-cfx-node=zero]").GetAttributeAsync("data-cfx-color-value"));
        Assert.Null(await page.Locator("[data-cfx-node=missing]").GetAttributeAsync("data-cfx-color-value"));
        Assert.Equal("true", await page.Locator("[data-cfx-node=missing]").GetAttributeAsync("data-cfx-color-missing"));
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }

    private static async Task Capture(IPage page, string name, Dictionary<string, string> rows) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png") });
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".json"), JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));
    }
}
