using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Protects native browser hits and readable raw-size facts when an authored domain caps bubble geometry.</summary>
public sealed class BubbleSizeScaleBrowserTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task SharedAndCappedRadiiRetainSourceReadoutsInBothThemesAndSizes(bool dark, bool compact) {
        if (!Enabled) return;
        var chart = Chart.Create().WithTitle("Workload and request volume")
            .WithSubtitle("One radius scale for both services")
            .WithSize(compact ? 316 : 700, compact ? 400 : 430)
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithXAxis("Workload batches").WithYAxis("Time (minutes)")
            .WithXAxisBounds(0, 5).WithYAxisBounds(0, 50).WithDataLabels(false)
            .AddBubble("Standard", new[] { new ChartBubble(.8, 18, 9), new ChartBubble(1.8, 30, 36), new ChartBubble(2.8, 24, 81) })
            .AddBubble("Priority", new[] { new ChartBubble(2.5, 38, 16), new ChartBubble(3.25, 20, 36), new ChartBubble(4.25, 35, 200) })
            .ConfigureBubble(bubble => { bubble.WithSizeDomain(0, 100); bubble.MinimumRadius = 3; bubble.MaximumRadius = 24; });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var html = chart.ToInteractiveHtmlPage(options => options.Tooltip.Mode = HtmlChartTooltipMode.Single);
        await using var session = await OpenAsync(html, compact ? 340 : 740, 640);
        var page = session.Page;
        var equalRadii = await page.Locator("g[data-cfx-size='36'] [data-cfx-role='bubble']")
            .EvaluateAllAsync<double[]>("nodes => nodes.map(node => Number(node.getAttribute('rx')))");
        Assert.Equal(new[] { 15.6, 15.6 }, equalRadii);
        var target = page.Locator(Point(1, 2));
        Assert.Equal("200", await target.GetAttributeAsync("data-cfx-size"));
        Assert.Equal("24", await target.Locator("[data-cfx-role='bubble']").GetAttributeAsync("rx"));
        await MoveToAsync(page, Point(1, 2));
        Assert.Contains("200", await TooltipTextAsync(page), StringComparison.Ordinal);
        Assert.Equal("35", await page.Locator(".cfx-tooltip dt").Filter(new LocatorFilterOptions { HasText = "Y" }).Locator("+ dd").InnerTextAsync());
        await page.EvaluateAsync("() => document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.bubbleSelection = event.detail.target)");
        await target.FocusAsync();
        await page.Keyboard.PressAsync("Space");
        var selected = await page.EvaluateAsync<JsonElement>("() => window.bubbleSelection");
        Assert.Equal("2", selected.GetProperty("point").GetString());
        Assert.Equal("35", selected.GetProperty("value").GetString());
        Assert.Contains("200", selected.GetProperty("label").GetString(), StringComparison.Ordinal);
        Assert.Equal("200", await target.GetAttributeAsync("data-cfx-size"));
        Assert.True(await page.Locator(".cfx-tooltip").EvaluateAsync<bool>("node => { const box = node.getBoundingClientRect(); return box.left >= 0 && box.right <= innerWidth && box.top >= 0 && box.bottom <= innerHeight; }"));
        Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth + 1"));
        AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BUBBLE_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            var name = "shared-capped-" + (dark ? "dark" : "light") + "-" + (compact ? "compact" : "wide");
            await File.WriteAllTextAsync(Path.Combine(captures, name + ".svg"), prepared.ToSvg());
            await File.WriteAllBytesAsync(Path.Combine(captures, name + ".png"), prepared.ToPng());
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + ".browser.png") });
            await File.WriteAllTextAsync(Path.Combine(captures, name + ".runtime.json"), JsonSerializer.Serialize(new {
                EqualRadii = equalRadii, CappedRadius = 24, RawSize = await target.GetAttributeAsync("data-cfx-size"), Selected = selected,
                Readout = await TooltipTextAsync(page), ConsoleErrors = session.ConsoleLog.Count(entry => entry.Type == HtmlTinkerX.HtmlConsoleMessageType.Error)
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
