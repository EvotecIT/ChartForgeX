using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class WaterfallCheckpointBrowserTests {
    [Theory]
    [InlineData(360, false)]
    [InlineData(360, true)]
    [InlineData(900, false)]
    [InlineData(900, true)]
    public async Task PointerCaptionsKeyboardAndPointLegendsRetainCheckpointAndRawDeltaFacts(int width, bool dark) {
        if (!Enabled) return;
        var chart = Create(width, dark);
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        var name = "waterfall-checkpoints-" + width + "-" + (dark ? "dark" : "light");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
            await File.WriteAllTextAsync(Path.Combine(capture, name + ".native.svg"), prepared.ToSvg());
            await File.WriteAllBytesAsync(Path.Combine(capture, name + ".native.png"), prepared.ToPng());
        }
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), width + 24, 540);
        var page = session.Page;
        var fontDirectory = Path.GetDirectoryName(chart.Options.PngFontPath)!;
        await page.AddStyleTagAsync(new PageAddStyleTagOptions { Content =
            "@font-face{font-family:Carlito;src:url(data:font/ttf;base64," + Convert.ToBase64String(await File.ReadAllBytesAsync(Path.Combine(fontDirectory, "Carlito-Regular.ttf"))) + ") format('truetype');font-weight:400}"
            + "@font-face{font-family:Carlito;src:url(data:font/ttf;base64," + Convert.ToBase64String(await File.ReadAllBytesAsync(Path.Combine(fontDirectory, "Carlito-Bold.ttf"))) + ") format('truetype');font-weight:700}" });
        await page.EvaluateAsync("async () => { await document.fonts.load('12px Carlito'); await document.fonts.load('700 18px Carlito'); await document.fonts.ready; }");
        Assert.True(await page.EvaluateAsync<bool>("() => [...document.fonts].filter(face => face.family === 'Carlito' && face.status === 'loaded').length === 2"));
        await page.EvaluateAsync("() => { window.waterfallSelections=[]; document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect',event=>window.waterfallSelections.push(event.detail.target)); }");
        var subtotal = page.Locator(Mark(4));
        var subtotalBar = Mark(4) + " [data-cfx-role='waterfall-bar']";
        var subtotalBox = await page.Locator(subtotalBar).BoundingBoxAsync() ?? throw new InvalidOperationException("The subtotal bar was not painted.");
        var subtotalX = (float)(subtotalBox.X + subtotalBox.Width * .2); var subtotalY = (float)(subtotalBox.Y + subtotalBox.Height * .25);
        await page.Mouse.MoveAsync(subtotalX, subtotalY, new MouseMoveOptions { Steps = 4 });
        AssertRows(await TooltipRowsAsync(page), "Subtotal", "-130", "Start", "90", "End", "-40");
        Assert.Null(await subtotal.GetAttributeAsync("data-cfx-source-point"));
        Assert.Equal("2", await subtotal.GetAttributeAsync("data-cfx-source-points"));
        var subtotalId = await subtotal.GetAttributeAsync("data-cfx-target-id");
        Assert.Equal("movement:derived:subtotal:5:sources:2", subtotalId);
        await page.Mouse.ClickAsync(subtotalX, subtotalY);
        Assert.Equal("true", await subtotal.GetAttributeAsync("aria-selected"));
        Assert.Equal(1, await page.EvaluateAsync<int>("() => window.waterfallSelections.length"));
        Assert.Equal(subtotalId, await page.EvaluateAsync<string>("() => window.waterfallSelections[0].targetId"));
        Assert.True(await page.EvaluateAsync<bool>("() => window.waterfallSelections[0].sourcePoint === undefined"));
        Assert.Equal(new[] { 2 }, await page.EvaluateAsync<int[]>("() => window.waterfallSelections[0].sourcePoints"));
        await subtotal.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("false", await subtotal.GetAttributeAsync("aria-selected"));
        Assert.Equal(2, await page.EvaluateAsync<int>("() => window.waterfallSelections.length"));
        await CaptureAsync(page, capture, name + "-subtotal");

        await MoveAwayAsync(page);
        var caption = page.Locator("[data-cfx-label-for='series-0-point-6'] text");
        var captionBox = await caption.BoundingBoxAsync() ?? throw new InvalidOperationException("The total caption was not painted.");
        await page.Mouse.MoveAsync((float)(captionBox.X + captionBox.Width * .4), (float)(captionBox.Y + captionBox.Height * .5), new MouseMoveOptions { Steps = 4 });
        AssertRows(await TooltipRowsAsync(page), "Total", "30", "Start", "0", "End", "30");
        await page.Mouse.ClickAsync((float)(captionBox.X + captionBox.Width * .4), (float)(captionBox.Y + captionBox.Height * .5));
        var total = page.Locator(Mark(6));
        Assert.Equal("true", await total.GetAttributeAsync("aria-selected"));
        Assert.Equal(3, await page.EvaluateAsync<int>("() => window.waterfallSelections.length"));
        Assert.Equal(await total.GetAttributeAsync("data-cfx-target-id"), await page.EvaluateAsync<string>("() => window.waterfallSelections.at(-1).targetId"));
        Assert.Equal(new[] { 0, 1, 2, 3 }, await page.EvaluateAsync<int[]>("() => window.waterfallSelections.at(-1).sourcePoints"));
        Assert.Equal(0, await page.Locator("[data-cfx-label-for][tabindex],[data-cfx-label-for][data-cfx-target-kind]").CountAsync());
        await CaptureAsync(page, capture, name + "-total-caption");

        await total.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("false", await total.GetAttributeAsync("aria-selected"));
        Assert.Equal(4, await page.EvaluateAsync<int>("() => window.waterfallSelections.length"));
        Assert.Null(await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-tooltip-pinned"));

        await MoveAwayAsync(page);
        var delta = page.Locator(Mark(7));
        var deltaLegend = page.Locator("[data-cfx-role='legend-item'][data-cfx-point='7']");
        Assert.Equal("4", await delta.GetAttributeAsync("data-cfx-source-point"));
        Assert.Equal(await delta.GetAttributeAsync("data-cfx-target-id"), await deltaLegend.GetAttributeAsync("data-cfx-target-id"));
        await deltaLegend.FocusAsync();
        AssertRows(await TooltipRowsAsync(page), "Change", "-20", "Start", "30", "End", "10");
        var subtotalLegend = page.Locator("[data-cfx-role='legend-item'][data-cfx-point='4']");
        Assert.Equal(subtotalId, await subtotalLegend.GetAttributeAsync("data-cfx-target-id"));
        await subtotalLegend.FocusAsync();
        AssertRows(await TooltipRowsAsync(page), "Subtotal", "-130", "Start", "90", "End", "-40");
        await CaptureAsync(page, capture, name + "-legend");
        AssertNoConsoleErrors(session);
    }

    private static string Mark(int item) => "[data-cfx-point='" + item + "'][data-cfx-waterfall-kind][data-cfx-target-kind='point']:not([data-cfx-role='legend-item'])";

    private static async Task<string[]> TooltipRowsAsync(IPage page) {
        Assert.True(await page.Locator(".cfx-tooltip").IsVisibleAsync());
        return (await page.Locator(".cfx-tooltip dt,.cfx-tooltip dd").AllTextContentsAsync()).ToArray();
    }

    private static void AssertRows(string[] rows, params string[] expected) {
        Assert.Equal(expected, rows.TakeLast(expected.Length));
        Assert.DoesNotContain("Delta", rows); Assert.DoesNotContain("Value", rows); Assert.DoesNotContain("Y", rows);
    }

    private static Chart Create(int width, bool dark) {
        var chart = Chart.Create().WithSize(width, 440).WithTitle("Waterfall checkpoints")
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight()).WithFontFamily("Carlito")
            .WithPointLegend().WithDataLabels().WithDataLabelPlacement(ChartDataLabelPlacement.Inside).WithBarStyle(ChartBarStyle.Flat)
            .WithXLabels("Open", "Adjust", "Stage", "Cost", "Costs", "Recover", "Balance", "Late", "Late sum")
            .AddWaterfall("Movement", new[] {
                ChartWaterfallItem.Delta(1, 120), ChartWaterfallItem.Delta(2, -30), ChartWaterfallItem.Subtotal(3),
                ChartWaterfallItem.Delta(4, -130), ChartWaterfallItem.Subtotal(5), ChartWaterfallItem.Delta(6, 70),
                ChartWaterfallItem.Total(7), ChartWaterfallItem.Delta(8, -20), ChartWaterfallItem.Subtotal(9)
            });
        chart.Options.PngFontPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        chart.Series[0].WithInteractionKey("movement").WithPointColor(4, "#A5358A").WithPointFillPattern(4, ChartFillPattern.Crosshatch);
        return chart;
    }

    private static async Task CaptureAsync(IPage page, string? directory, string name) {
        if (string.IsNullOrWhiteSpace(directory)) return;
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png"), FullPage = true });
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".runtime.json"), JsonSerializer.Serialize(
            await page.EvaluateAsync<JsonElement>("() => ({selections:window.waterfallSelections,tooltip:document.querySelector('.cfx-tooltip').innerText})"),
            new JsonSerializerOptions { WriteIndented = true }));
    }
}
