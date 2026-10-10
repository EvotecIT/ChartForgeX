using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using System.Text.Json;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractivePolarIdentityBrowserTests {
    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public async Task PolarFamilyPointsRetainSeriesIdentityAndIndependentLegendMuting(bool radar, bool compactDark) {
        if (!Enabled) return;
        var chart = CreateChart(radar, compactDark);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), compactDark ? 390 : 700, compactDark ? 620 : 550);
        var page = session.Page;
        var role = radar ? "radar-point-source" : "polar-point-source";
        var pointSelector = "[data-cfx-role='" + role + "']";
        var actualSelector = pointSelector + "[data-cfx-source-id^='series-0-point-']";
        var targetSelector = pointSelector + "[data-cfx-source-id^='series-1-point-']";
        var actual = page.Locator(actualSelector);
        var targets = page.Locator(targetSelector);
        Assert.Equal(4, await actual.CountAsync());
        Assert.Equal(4, await targets.CountAsync());
        var identities = await page.EvaluateAsync<string[]>("selector => Array.from(document.querySelectorAll(selector)).map(node => node.dataset.cfxTargetId)", pointSelector);
        Assert.Equal(8, identities.Distinct().Count());
        for (var index = 0; index < 4; index++) {
            await AssertPointAsync(actual.Nth(index), 0, "Actual", "actual-source", index);
            await AssertPointAsync(targets.Nth(index), 1, "Target", "target-source", index);
        }

        await page.EvaluateAsync("() => { window.polarIdentityEvents = { hover: [], selection: [] }; const root = document.querySelector('.cfx-interactive-chart'); root.addEventListener('cfxhover', event => window.polarIdentityEvents.hover.push(event.detail)); root.addEventListener('cfxselect', event => window.polarIdentityEvents.selection.push(event.detail)); }");
        var targetPointSelector = pointSelector + "[data-cfx-source-id='series-1-point-1']";
        var targetPoint = page.Locator(targetPointSelector);
        await targetPoint.ScrollIntoViewIfNeededAsync();
        await MoveToAsync(page, targetPointSelector);
        await page.WaitForFunctionAsync("() => window.polarIdentityEvents.hover.some(event => event.target.targetKind === 'point')");
        using (var hover = JsonDocument.Parse(await page.EvaluateAsync<string>("() => JSON.stringify(window.polarIdentityEvents.hover.filter(event => event.target.targetKind === 'point').at(-1))")))
            AssertTargetEvent(hover.RootElement);
        Assert.Contains("Target", await TooltipTextAsync(page));
        await targetPoint.ClickAsync();
        using (var selection = JsonDocument.Parse(await page.EvaluateAsync<string>("() => JSON.stringify(window.polarIdentityEvents.selection.at(-1))")))
            AssertTargetEvent(selection.RootElement);
        Assert.Equal("true", await targetPoint.GetAttributeAsync("aria-selected"));
        Assert.Equal(1, await page.Locator(pointSelector + "[aria-selected='true']").CountAsync());
        Assert.Equal(0, await page.Locator(actualSelector + "[aria-selected='true']").CountAsync());
        await CaptureAsync(page, radar, compactDark, "target-selected");

        await page.Locator(Legend(0)).ClickAsync();
        Assert.Equal("true", await page.Locator(Legend(0)).GetAttributeAsync("data-cfx-muted"));
        Assert.NotEqual("true", await page.Locator(Legend(1)).GetAttributeAsync("data-cfx-muted"));
        Assert.All(await page.EvaluateAsync<bool[]>("selector => Array.from(document.querySelectorAll(selector)).map(node => node.classList.contains('cfx-series-muted'))", actualSelector), value => Assert.True(value));
        Assert.All(await page.EvaluateAsync<bool[]>("selector => Array.from(document.querySelectorAll(selector)).map(node => !!node.closest('.cfx-series-muted'))", actualSelector), value => Assert.True(value));
        Assert.All(await page.EvaluateAsync<bool[]>("selector => Array.from(document.querySelectorAll(selector)).map(node => node.classList.contains('cfx-series-muted'))", targetSelector), value => Assert.False(value));
        Assert.All(await page.EvaluateAsync<bool[]>("selector => Array.from(document.querySelectorAll(selector)).map(node => !!node.closest('.cfx-series-muted'))", targetSelector), value => Assert.False(value));
        Assert.Equal("true", await targetPoint.GetAttributeAsync("aria-selected"));
        Assert.Equal(0, await page.Locator(actualSelector + "[aria-selected='true']").CountAsync());
        await targetPoint.ClickAsync();
        Assert.Equal("false", await targetPoint.GetAttributeAsync("aria-selected"));
        using (var selection = JsonDocument.Parse(await page.EvaluateAsync<string>("() => JSON.stringify(window.polarIdentityEvents.selection.at(-1))")))
            AssertTargetEvent(selection.RootElement);
        await CaptureAsync(page, radar, compactDark, "actual-muted");
        AssertNoConsoleErrors(session);
    }

    private static async Task AssertPointAsync(Microsoft.Playwright.ILocator point, int series, string name, string key, int sourcePoint) {
        Assert.Equal(series.ToString(), await point.GetAttributeAsync("data-cfx-series"));
        Assert.Equal(name, await point.GetAttributeAsync("data-cfx-series-name"));
        Assert.Equal(key, await point.GetAttributeAsync("data-cfx-series-key"));
        Assert.Equal(sourcePoint.ToString(), await point.GetAttributeAsync("data-cfx-source-point"));
        Assert.Equal(key + ":" + sourcePoint, await point.GetAttributeAsync("data-cfx-target-id"));
    }

    private static void AssertTargetEvent(JsonElement detail) {
        var target = detail.GetProperty("target");
        Assert.Contains("Target", detail.GetProperty("label").GetString());
        Assert.Equal("point", target.GetProperty("targetKind").GetString());
        Assert.Equal("1", target.GetProperty("series").GetString());
        Assert.Equal("target-source", target.GetProperty("seriesKey").GetString());
        Assert.Equal("target-source:1", target.GetProperty("targetId").GetString());
        Assert.Equal("1", target.GetProperty("sourcePoint").GetString());
        Assert.Equal("7", target.GetProperty("value").GetString());
    }

    private static Chart CreateChart(bool radar, bool dark) {
        var chart = Chart.Create().WithSize(596, 420).WithLegend()
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithTitle(radar ? "Radar series identities" : "Polar series identities")
            .WithSubtitle("Actual and Target have distinct source identities");
        var coordinates = radar ? new[] { 1d, 2, 3, 4 } : new[] { 0d, Math.PI / 2, Math.PI, 3 * Math.PI / 2 };
        var actual = coordinates.Zip(new[] { 3d, 4, 5, 4 }, (x, value) => new ChartPoint(x, value));
        var target = coordinates.Zip(new[] { 8d, 7, 6, 8 }, (x, value) => new ChartPoint(x, value));
        if (radar) chart.WithXLabels("Availability", "Latency", "Capacity", "Recovery")
            .AddRadarArea("Actual", actual, ChartColor.FromHex("#16a34a"))
            .AddRadarLine("Target", target, ChartColor.FromHex("#2563eb"));
        else chart.AddPolar("Actual", actual, ChartColor.FromHex("#16a34a"))
            .AddPolar("Target", target, ChartColor.FromHex("#2563eb"));
        chart.Series[0].WithInteractionKey("actual-source");
        chart.Series[1].WithInteractionKey("target-source");
        return chart;
    }

    private static async Task CaptureAsync(Microsoft.Playwright.IPage page, bool radar, bool compactDark, string state) {
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(capture)) return;
        Directory.CreateDirectory(capture);
        await page.ScreenshotAsync(new Microsoft.Playwright.PageScreenshotOptions {
            Path = Path.Combine(capture, (radar ? "radar" : "polar") + "-identity-" + (compactDark ? "compact-dark" : "wide-light") + "-" + state + ".png")
        });
    }
}
