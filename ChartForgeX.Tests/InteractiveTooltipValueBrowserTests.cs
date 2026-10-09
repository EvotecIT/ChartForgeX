using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractiveTooltipValueBrowserTests {
    [Theory]
    [InlineData("node", false)]
    [InlineData("node", true)]
    [InlineData("flow", false)]
    [InlineData("flow", true)]
    [InlineData("zero", false)]
    [InlineData("zero", true)]
    [InlineData("cell", false)]
    [InlineData("cell", true)]
    [InlineData("cartesian", false)]
    [InlineData("cartesian", true)]
    public async Task TooltipFieldsDistinguishActualYFromValueOnlyFacts(string family, bool dark) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(600, 340).WithTheme(dark ? ChartTheme.Dark() : ChartTheme.Light());
        var nodes = new[] { new ChartNode("teams", "All teams"), new ChartNode("support", "Support") };
        var selector = ""; var expected = family is "zero" or "cell" ? "0" : family == "cartesian" ? "7" : "8";
        switch (family) {
            case "node": chart.AddTree("Teams", nodes, new[] { new ChartTreeLink("teams", "support", 8) }); selector = "[data-cfx-node=support]"; break;
            case "flow": chart.AddSankey("Requests", nodes, new[] { new ChartFlowLink("work", "teams", "support", 8) }); selector = "[data-cfx-target-kind=link][data-cfx-target-id=work]"; break;
            case "zero": chart.AddGauge("Capacity", 0); selector = "[data-cfx-role=gauge]"; break;
            case "cell": chart.WithXLabels("Monday").AddHeatmapRow("Totals", new[] { new ChartPoint(0, 0) }); selector = "[data-cfx-role=heatmap-cell]"; break;
            case "cartesian": chart.AddDumbbell("Latency", new[] { new ChartDumbbell(3, 7, 12) }); selector = Point(0, 0); break;
        }
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), 640, 450);
        var page = session.Page; var target = page.Locator(selector);
        Assert.Equal(expected, await target.GetAttributeAsync(family == "cartesian" ? "data-cfx-y" : "data-cfx-value"));
        if (family is "cartesian" or "cell") Assert.Equal(expected, await target.GetAttributeAsync("data-cfx-y"));
        else Assert.Null(await target.GetAttributeAsync("data-cfx-y"));
        var id = await target.GetAttributeAsync("data-cfx-target-id");
        await page.EvaluateAsync("() => document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.selection = event.detail.target)");
        await target.FocusAsync(); await page.Keyboard.PressAsync("Space");
        var rows = await page.Locator(".cfx-tooltip dt").AllTextContentsAsync();
        Assert.Contains(family == "cartesian" ? "Y" : "Value", rows);
        Assert.DoesNotContain(family == "cartesian" ? "Value" : "Y", rows);
        Assert.Equal(expected, await page.EvaluateAsync<string>("() => window.selection.value"));
        Assert.Equal(id, await page.EvaluateAsync<string>("() => window.selection.targetId"));
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture); var name = "tooltip-fact-" + family + "-" + (dark ? "dark" : "light");
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, name + ".png") });
            var facts = await page.EvaluateAsync<JsonElement>("() => ({ target: window.selection, tooltip: document.querySelector('.cfx-tooltip').innerText })");
            await File.WriteAllTextAsync(Path.Combine(capture, name + ".runtime.json"), JsonSerializer.Serialize(facts, new JsonSerializerOptions { WriteIndented = true }));
        }
        AssertNoConsoleErrors(session);
    }
}
