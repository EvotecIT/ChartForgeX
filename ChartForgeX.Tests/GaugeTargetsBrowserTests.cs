using System.Globalization;
using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Checks captured gauge reference readouts without creating extra observations or browser targets.</summary>
public sealed class GaugeTargetsBrowserTests {
    [BrowserTheory]
    [InlineData(ChartGaugeForm.Arc, false)]
    [InlineData(ChartGaugeForm.Needle, false)]
    [InlineData(ChartGaugeForm.Linear, false)]
    [InlineData(ChartGaugeForm.Arc, true)]
    [InlineData(ChartGaugeForm.Needle, true)]
    [InlineData(ChartGaugeForm.Linear, true)]
    public async Task MultipleTargetsRetainCapturedCaptionsAndOneObservedSelection(ChartGaugeForm form, bool customFormatter) {
        if (!Enabled) return;
        var culture = CultureInfo.GetCultureInfo("fr-FR");
        var reading = new ChartGaugeValue(81.75, targets: new[] { new ChartGaugeTarget(72.5, "Minimum <safe>"), new ChartGaugeTarget(91.25) });
        var series = ChartSeries.CreateGauge(new ChartSeriesId("readiness"), "Readiness", reading)
            .WithObservationId(0, new ChartObservationId("reading"));
        var chart = Chart.Create().WithSize(customFormatter ? 340 : 640, 360).WithLegend(false)
            .WithTheme(VisualTheme.Graphite(), customFormatter ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithTitle("Measured readiness").AddSeries(series).ConfigureGauge(options => options.Form = form);
        if (customFormatter) chart.WithValueFormatter(value => "Reading " + value.ToString("0.00", culture));
        else chart.WithValueFormat(ChartValueFormat.Number("0.00", culture));
        if (customFormatter) chart.ConfigureLabels(labels => labels.Target = "Objectif");
        var expected = customFormatter ? new[] { "Reading 72,50", "Reading 91,25" } : new[] { "72,50", "91,25" };
        var html = chart.ToInteractiveHtmlPage(options => {
            options.Tooltip.Mode = HtmlChartTooltipMode.Single;
            options.Tooltip.PinOnSelect = false;
            options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Selection | ChartInteractionFeatures.KeyboardNavigation;
        });
        await using var session = await OpenAsync(html, customFormatter ? 390 : 760, 540);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        var gauge = page.Locator("[data-cfx-role='gauge'][data-cfx-target-kind='point']");
        Assert.Equal(1, await gauge.CountAsync());
        Assert.Equal("2", await gauge.GetAttributeAsync("data-cfx-gauge-target-count"));
        Assert.Equal("72.5", await gauge.GetAttributeAsync("data-cfx-gauge-target-0-value"));
        Assert.Equal("91.25", await gauge.GetAttributeAsync("data-cfx-gauge-target-1-value"));
        Assert.Equal(expected[0], await gauge.GetAttributeAsync("data-cfx-gauge-target-0-formatted-value"));
        Assert.Equal(expected[1], await gauge.GetAttributeAsync("data-cfx-gauge-target-1-formatted-value"));
        Assert.Equal(1, await page.Locator(".cfx-interactive-chart [data-cfx-target-kind='point']").CountAsync());
        Assert.Equal(0, await page.Locator("[data-cfx-role='gauge-target'][data-cfx-target-kind],[data-cfx-role='gauge-target'][tabindex]").CountAsync());
        await page.EvaluateAsync("() => { window.gaugeSelections = []; document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.gaugeSelections.push(event.detail.target)); }");
        await FocusWithKeyboardAsync(page, gauge);
        Assert.True(await page.Locator(".cfx-tooltip").IsVisibleAsync());
        var keyboardRows = await TargetRowsAsync(page);
        Assert.Equal(new[] { "Minimum <safe>", customFormatter ? "Objectif" : "Target" }, keyboardRows.Select(row => row[0]));
        Assert.Equal(expected, keyboardRows.Select(row => row[1]));
        await page.Keyboard.PressAsync("Space");
        var selected = await page.EvaluateAsync<JsonElement>("() => window.gaugeSelections");
        var target = Assert.Single(selected.EnumerateArray());
        Assert.Equal("point", target.GetProperty("targetKind").GetString());
        Assert.Equal("readiness", target.GetProperty("seriesKey").GetString());
        Assert.Equal("reading", target.GetProperty("observationId").GetString());
        Assert.Equal("0", target.GetProperty("sourcePoint").GetString());
        Assert.Equal("81.75", target.GetProperty("value").GetString());
        Assert.Equal(await gauge.GetAttributeAsync("data-cfx-target-id"), target.GetProperty("targetId").GetString());
        Assert.Equal(1, await page.EvaluateAsync<int>("() => ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')).getSelection().length"));
        await gauge.BlurAsync(); await MoveAwayAsync(page);
        // SVG lines can have a zero-width box; selected-value overlays also share this gauge's hit ownership.
        // Move the real pointer to the reference's painted centre instead of demanding a box-shaped DOM hit target.
        await MoveToAsync(page, "[data-cfx-role='gauge-target']");
        Assert.True(await page.Locator(".cfx-tooltip").IsVisibleAsync());
        Assert.Equal(keyboardRows.SelectMany(row => row), (await TargetRowsAsync(page)).SelectMany(row => row));
        Assert.Equal(1, await page.EvaluateAsync<int>("() => window.gaugeSelections.length"));
        Assert.Equal(0, await page.Locator(".cfx-tooltip a").CountAsync());
        await CaptureAsync(page, "gauge-targets-" + form.ToString().ToLowerInvariant() + (customFormatter ? "-custom-compact-dark" : "-culture-wide-light"), selected);
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }

    private static Task<string[][]> TargetRowsAsync(IPage page) => page.EvaluateAsync<string[][]>("""
        () => Array.from(document.querySelectorAll('.cfx-tooltip dt'))
          .filter(term => term.textContent === 'Minimum <safe>' || term.textContent === 'Target' || term.textContent === 'Objectif')
          .map(term => [term.textContent, term.nextElementSibling.textContent])
        """);

    private static async Task CaptureAsync(IPage page, string name, JsonElement selection) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); var path = Path.Combine(directory, name);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = path + ".png" });
        await File.WriteAllTextAsync(path + ".html", await page.ContentAsync());
        await File.WriteAllTextAsync(path + ".json", JsonSerializer.Serialize(selection, new JsonSerializerOptions { WriteIndented = true }));
    }
}
