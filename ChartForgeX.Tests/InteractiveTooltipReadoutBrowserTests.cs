using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Protects readable data tooltips without stripping host identity or authored metadata.</summary>
public sealed class InteractiveTooltipReadoutBrowserTests {
    [Theory]
    [InlineData("funnel", false)]
    [InlineData("funnel", true)]
    [InlineData("pie", false)]
    [InlineData("pie", true)]
    [InlineData("scatter", false)]
    [InlineData("scatter", true)]
    [InlineData("gauge", false)]
    [InlineData("gauge", true)]
    public async Task ReadoutKeepsDataAndAuthoredMetadataWhileEventsRetainRenderingIdentity(string family, bool dark) {
        if (!Enabled) return;
        var width = dark ? 340 : 700;
        var chart = Chart.Create().WithSize(596, 338).WithXLabels("North", "South")
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight());
        switch (family) {
            case "funnel": chart.AddFunnel("Requests", ChartPoints.FromValues(7, 3)); break;
            case "pie": chart.AddPie("Requests", ChartPoints.FromValues(7, 3)); break;
            case "scatter": chart.AddScatter("Observed", new[] { new ChartPoint(1, 0), new ChartPoint(2, 3) }); break;
            case "gauge": chart.AddGauge("Capacity", 0); break;
        }
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => options.TooltipMode = HtmlChartTooltipMode.Single), width, 560);
        var page = session.Page;
        var target = page.Locator("[data-cfx-keyboard-component='data']").First;
        var raw = await target.EvaluateAsync<JsonElement>("node => ({ role: node.dataset.cfxRole, kind: node.dataset.cfxKind, point: node.dataset.cfxPoint, id: node.dataset.cfxTargetId })");
        await target.FocusAsync();
        var rows = await page.Locator(".cfx-tooltip dt").AllTextContentsAsync();
        Assert.DoesNotContain("Role", rows);
        Assert.DoesNotContain("Point", rows);
        Assert.DoesNotContain("Kind", rows);
        var valueLabel = family == "scatter" ? "Y" : "Value";
        var valueRow = page.Locator(".cfx-tooltip dt").Filter(new LocatorFilterOptions { HasText = valueLabel }).Locator("+ dd");
        Assert.Equal(family is "scatter" or "gauge" ? "0" : "7", await valueRow.InnerTextAsync());
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        var name = "tooltip-readout-" + family + "-" + (dark ? "dark-compact" : "light-wide");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, name + "-default.png") });
        }

        // Hosts attach business metadata to the existing native target, without replacing its geometry or identity.
        const string authoredKind = "Receipt <img src=x onerror=alert(1)>";
        await target.EvaluateAsync("(node, kind) => { node.setAttribute('data-cfx-meta-kind', kind); node.setAttribute('data-cfx-meta-source-record', 'record-17'); }", authoredKind);
        await page.EvaluateAsync("() => document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.readoutSelection = event.detail.target)");
        await target.BlurAsync();
        await target.FocusAsync();
        await page.Keyboard.PressAsync("Space");
        Assert.Equal(authoredKind, await page.Locator(".cfx-tooltip dt").Filter(new LocatorFilterOptions { HasText = "Kind" }).Locator("+ dd").InnerTextAsync());
        Assert.Equal("record-17", await page.Locator(".cfx-tooltip dt").Filter(new LocatorFilterOptions { HasText = "Source Record" }).Locator("+ dd").InnerTextAsync());
        Assert.Equal(0, await page.Locator(".cfx-tooltip img").CountAsync());
        var selected = await page.EvaluateAsync<JsonElement>("() => window.readoutSelection");
        Assert.Equal(raw.GetProperty("role").GetString(), selected.GetProperty("role").GetString());
        Assert.Equal(raw.GetProperty("kind").GetString(), selected.GetProperty("kind").GetString());
        Assert.Equal(raw.GetProperty("id").GetString(), selected.GetProperty("targetId").GetString());
        Assert.Equal(raw.TryGetProperty("point", out var point) ? point.GetString() : null,
            selected.TryGetProperty("point", out var selectedPoint) ? selectedPoint.GetString() : null);
        Assert.True(await page.Locator(".cfx-tooltip").EvaluateAsync<bool>("node => { const box = node.getBoundingClientRect(); return box.left >= 0 && box.right <= innerWidth && box.top >= 0 && box.bottom <= innerHeight; }"));
        if (!string.IsNullOrWhiteSpace(capture)) {
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, name + ".png") });
            await File.WriteAllTextAsync(Path.Combine(capture, name + ".runtime.json"), JsonSerializer.Serialize(new { Raw = raw, Selected = selected, Rows = await page.Locator(".cfx-tooltip dt").AllTextContentsAsync() }, new JsonSerializerOptions { WriteIndented = true }));
        }
        AssertNoConsoleErrors(session);
    }
}
