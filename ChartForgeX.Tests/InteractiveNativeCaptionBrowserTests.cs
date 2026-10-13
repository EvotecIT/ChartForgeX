using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Protects native caption acquisition without promoting decorations or bypassing mark availability.</summary>
public sealed class InteractiveNativeCaptionBrowserTests {
    [BrowserTheory]
    [InlineData("region", "exact", 0)]
    [InlineData("region", "distance", 0)]
    [InlineData("region", "nearest", 0)]
    [InlineData("region", "exact", 120)]
    [InlineData("tile", "exact", 0)]
    [InlineData("tile", "distance", 0)]
    [InlineData("tile", "nearest", 0)]
    [InlineData("tile", "exact", 120)]
    [InlineData("gauge", "exact", 0)]
    [InlineData("gauge", "distance", 0)]
    [InlineData("gauge", "nearest", 0)]
    [InlineData("gauge", "exact", 120)]
    public async Task NestedNativeCaptionUsesItsOwnerThroughPointerPinAndAvailability(string family, string range, int delay) {
        if (!Enabled) return;
        var tile = family == "tile"; var gauge = family == "gauge";
        var chart = Chart.Create().WithSize(800, 440).WithHeader(false).WithLegend(false)
            .WithMapLabels().WithTheme(VisualTheme.Graphite(), VisualThemeMode.Dark);
        if (!gauge) chart.ConfigureMapColorDimension(options => options.ShowLegend = false);
        if (gauge) chart.AddGauge("Coverage", 55);
        else if (tile) chart.AddTileMap("Coverage", ChartTileMapCatalog.Get("us-states"), new[] { new ChartRegionMapItem("CO", 55) });
        else chart.AddRegionMap("Coverage", new ChartMapDefinition("regions", "Regions", 100, 100, new[] {
            new ChartMapRegion("CO", "Colorado", "M10 10H90V90H10Z")
        }), new[] { new ChartRegionMapItem("CO", 55) });
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            options.Tooltip.Mode = HtmlChartTooltipMode.Single;
            options.Tooltip.Range = range == "exact" ? HtmlChartTooltipRange.Exact
                : range == "nearest" ? HtmlChartTooltipRange.Nearest : HtmlChartTooltipRange.WithinDistance(120);
            options.Tooltip.DelayMilliseconds = delay;
        }), 824, 600);
        var page = session.Page;
        var regionSelector = gauge ? "[data-cfx-role='gauge']" : "[data-cfx-role='" + (tile ? "tile" : "region") + "-map-region-source'][data-cfx-region='CO']";
        var region = page.Locator(regionSelector);
        var caption = regionSelector + (gauge ? " [data-cfx-role='gauge-label-source'] text" : " text");
        await page.EvaluateAsync("() => document.querySelector('.cfx-interactive-chart').addEventListener('cfxhover', event => window.nativeCaptionHover = event.detail.target)");
        await MoveToAsync(page, caption);
        Assert.True(await page.Locator(caption).EvaluateAsync<bool>("node => { const b = node.getBoundingClientRect(); return node.contains(document.elementFromPoint(b.x + b.width / 2, b.y + b.height / 2)); }"));
        await WaitForValueAsync();
        var hover = await page.EvaluateAsync<JsonElement>("() => window.nativeCaptionHover");
        Assert.Equal(gauge ? "point" : "region", hover.GetProperty("targetKind").GetString());
        Assert.Equal(gauge ? "Coverage:0" : "CO", hover.GetProperty("targetId").GetString());
        await page.Locator(caption).ClickAsync();
        Assert.Equal("true", await region.GetAttributeAsync("aria-selected"));
        Assert.Equal("true", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-tooltip-pinned"));
        await CaptureAsync("caption-pinned");
        await region.FocusAsync(); await page.Keyboard.PressAsync("Space"); await region.BlurAsync();
        Assert.Equal("false", await region.GetAttributeAsync("aria-selected"));

        // A hidden native owner cannot acquire a readout merely because its caption has a browser hit box.
        await region.EvaluateAsync("node => node.style.opacity = '0'");
        await MoveAwayAsync(page); await MoveToAsync(page, caption);
        Assert.True(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        await region.EvaluateAsync("node => node.style.removeProperty('opacity')");
        await MoveAwayAsync(page); await MoveToAsync(page, caption); await WaitForValueAsync();

        // A producer's independently identified nested target remains a boundary, even without its own paint.
        await page.Locator(caption).EvaluateAsync("node => { const wrapper = node.parentElement; wrapper.dataset.cfxTargetKind = 'annotation'; wrapper.dataset.cfxTargetId = 'independent-caption'; }");
        await MoveAwayAsync(page); await MoveToAsync(page, caption);
        Assert.True(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        AssertNoConsoleErrors(session);

        Task WaitForValueAsync() => page.WaitForFunctionAsync("() => { const tip = document.querySelector('.cfx-tooltip'); return tip && !tip.hidden && Array.from(tip.querySelectorAll('dt')).some(term => term.textContent === 'Value' && term.nextElementSibling.textContent === '55'); }");

        async Task CaptureAsync(string stage) {
            var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
            if (string.IsNullOrWhiteSpace(directory)) return;
            Directory.CreateDirectory(directory);
            var name = "native-caption-" + family + "-" + range + "-" + delay + "-" + stage;
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png") });
            await File.WriteAllTextAsync(Path.Combine(directory, name + ".json"), JsonSerializer.Serialize(new {
                Hover = await page.EvaluateAsync<JsonElement>("() => window.nativeCaptionHover"),
                Readout = await TooltipTextAsync(page), Region = await region.GetAttributeAsync("aria-label"),
                Selected = await region.GetAttributeAsync("aria-selected"), ConsoleErrors = session.ConsoleLog.Count(entry => entry.Type == HtmlTinkerX.HtmlConsoleMessageType.Error)
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
