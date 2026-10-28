using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class LocalizedHeatmapTooltipBrowserTests {
    [BrowserTheory]
    [InlineData(390, VisualThemeMode.Dark)]
    [InlineData(900, VisualThemeMode.Light)]
    public async Task ShippedRuntimeReadsTheAuthoredLevelCaptionAndCapturedValue(int width, VisualThemeMode mode) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(width, 320).WithTheme(VisualTheme.Report(), mode)
            .WithFontFamily(BrowserProofFonts.Family).WithLegend(false)
            .ConfigureLabels(labels => labels.Level = "Poziom")
            .AddHeatmapRow("Logons", new[] { 2d, 40d, 90d, 400d });
        var fragment = chart.ToInteractiveHtmlFragmentWithoutAssets(options => {
            options.IdScope = "localized-level";
            options.Interaction.Enable(ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.KeyboardNavigation);
        });
        var html = "<!doctype html><html><head><style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle()
            + "</style></head><body>" + fragment + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script></body></html>";
        await using var session = await BrowserProofFonts.OpenAsync(html, width + 24, 540);
        var page = session.Page;
        var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        var cell = page.Locator("[data-cfx-role=heatmap-cell][data-cfx-level]").Last;
        var level = await cell.GetAttributeAsync("data-cfx-level");
        await cell.FocusAsync();
        await page.WaitForFunctionAsync("()=>!document.querySelector('.cfx-tooltip').hidden");
        var rows = await page.EvaluateAsync<JsonElement>("""
            ()=>Array.from(document.querySelectorAll('.cfx-tooltip__meta dt'),term=>({name:term.textContent,value:term.nextElementSibling.textContent}))
            """);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Level tooltip captures require an absolute task path.");
            Directory.CreateDirectory(directory);
            var stem = Path.Combine(directory, "localized-level-tooltip-" + width + "-" + mode);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
            await File.WriteAllTextAsync(stem + ".json", JsonSerializer.Serialize(new { level, rows, errors, session.ConsoleLog }));
        }
        var fields = rows.EnumerateArray().ToArray();
        var captured = Assert.Single(fields, row => row.GetProperty("name").GetString() == "Poziom");
        Assert.Equal(level, captured.GetProperty("value").GetString());
        Assert.DoesNotContain(fields, row => row.GetProperty("name").GetString() == "Level");
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }
}
