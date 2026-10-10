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
    [InlineData(360, false, null)]
    [InlineData(360, true, null)]
    [InlineData(900, false, null)]
    [InlineData(900, true, null)]
    [InlineData(360, false, 120d)]
    [InlineData(360, true, -120d)]
    [InlineData(900, false, -120d)]
    [InlineData(900, true, 120d)]
    public async Task PointerCaptionsKeyboardAndPointLegendsRetainOpeningCheckpointAndRawDeltaFacts(int width, bool dark, double? opening) {
        if (!Enabled) return;
        var sign = opening < 0 ? -1 : 1;
        var chart = Create(width, dark, opening);
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        var name = (opening.HasValue ? "waterfall-opening-" : "waterfall-checkpoints-") + width + "-" + (dark ? "dark" : "light");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
            await File.WriteAllTextAsync(Path.Combine(capture, name + ".native.svg"), prepared.ToSvg());
            await File.WriteAllBytesAsync(Path.Combine(capture, name + ".native.png"), prepared.ToPng());
        }
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), width + 24, 540);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        var fontDirectory = Path.GetDirectoryName(chart.Options.PngFontPath)!;
        await page.AddStyleTagAsync(new PageAddStyleTagOptions { Content =
            "@font-face{font-family:Carlito;src:url(data:font/ttf;base64," + Convert.ToBase64String(await File.ReadAllBytesAsync(Path.Combine(fontDirectory, "Carlito-Regular.ttf"))) + ") format('truetype');font-weight:400}"
            + "@font-face{font-family:Carlito;src:url(data:font/ttf;base64," + Convert.ToBase64String(await File.ReadAllBytesAsync(Path.Combine(fontDirectory, "Carlito-Bold.ttf"))) + ") format('truetype');font-weight:700}" });
        await page.EvaluateAsync("async () => { await document.fonts.load('12px Carlito'); await document.fonts.load('700 18px Carlito'); await document.fonts.ready; }");
        Assert.True(await page.EvaluateAsync<bool>("() => [...document.fonts].filter(face => face.family === 'Carlito' && face.status === 'loaded').length === 2"));
        await page.EvaluateAsync("() => { window.waterfallSelections=[]; document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect',event=>window.waterfallSelections.push(event.detail.target)); }");
        if (opening.HasValue) {
            var openingMark = page.Locator(Mark(0));
            var openingBox = await BoxAsync(page, Mark(0) + " [data-cfx-role='waterfall-bar']");
            var openingX = (float)(openingBox.X + openingBox.Width * .2); var openingY = (float)(openingBox.Y + openingBox.Height * .25);
            await page.Mouse.MoveAsync(openingX, openingY, new MouseMoveOptions { Steps = 4 });
            AssertRows(await TooltipRowsAsync(page), "Initial balance", Signed(120, sign), "Start", "0", "End", Signed(120, sign));
            Assert.Equal("opening-balance", await openingMark.GetAttributeAsync("data-cfx-waterfall-kind"));
            Assert.Equal("0", await openingMark.GetAttributeAsync("data-cfx-source-point"));
            Assert.Null(await openingMark.GetAttributeAsync("data-cfx-delta")); Assert.Null(await openingMark.GetAttributeAsync("data-cfx-derived"));
            Assert.Equal("movement:0", await openingMark.GetAttributeAsync("data-cfx-target-id"));
            await page.Mouse.ClickAsync(openingX, openingY);
            Assert.Equal("true", await openingMark.GetAttributeAsync("aria-selected"));
            Assert.Equal(0, await page.EvaluateAsync<int>("() => window.waterfallSelections.at(-1).sourcePoint"));
            await openingMark.FocusAsync(); await page.Keyboard.PressAsync("Space");
            Assert.Equal("false", await openingMark.GetAttributeAsync("aria-selected"));
            await MoveAwayAsync(page);
            var openingCaption = await BoxAsync(page, "[data-cfx-label-for='series-0-point-0'] text");
            var captionX = (float)(openingCaption.X + openingCaption.Width * .4); var captionY = (float)(openingCaption.Y + openingCaption.Height * .5);
            await page.Mouse.MoveAsync(captionX, captionY, new MouseMoveOptions { Steps = 4 });
            AssertRows(await TooltipRowsAsync(page), "Initial balance", Signed(120, sign), "Start", "0", "End", Signed(120, sign));
            await page.Mouse.ClickAsync(captionX, captionY);
            Assert.Equal("true", await openingMark.GetAttributeAsync("aria-selected"));
            Assert.Equal("movement:0", await page.EvaluateAsync<string>("() => window.waterfallSelections.at(-1).targetId"));
            Assert.Equal(0, await page.EvaluateAsync<int>("() => window.waterfallSelections.at(-1).sourcePoint"));
            await CaptureAsync(page, capture, name + "-opening-caption");
            await openingMark.FocusAsync(); await page.Keyboard.PressAsync("Space");
            Assert.Equal("false", await openingMark.GetAttributeAsync("aria-selected"));
            await page.EvaluateAsync("() => { window.waterfallSelections=[]; }");
        }
        var subtotal = page.Locator(Mark(4));
        var subtotalBar = Mark(4) + " [data-cfx-role='waterfall-bar']";
        var subtotalBox = await page.Locator(subtotalBar).BoundingBoxAsync() ?? throw new InvalidOperationException("The subtotal bar was not painted.");
        var subtotalX = (float)(subtotalBox.X + subtotalBox.Width * .2); var subtotalY = (float)(subtotalBox.Y + subtotalBox.Height * .25);
        await page.Mouse.MoveAsync(subtotalX, subtotalY, new MouseMoveOptions { Steps = 4 });
        AssertRows(await TooltipRowsAsync(page), "Subtotal", Signed(-130, sign), "Start", Signed(90, sign), "End", Signed(-40, sign));
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
        AssertRows(await TooltipRowsAsync(page), "Total", Signed(30, sign), "Start", "0", "End", Signed(30, sign));
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
        AssertRows(await TooltipRowsAsync(page), "Change", Signed(-20, sign), "Start", Signed(30, sign), "End", Signed(10, sign));
        var subtotalLegend = page.Locator("[data-cfx-role='legend-item'][data-cfx-point='4']");
        Assert.Equal(subtotalId, await subtotalLegend.GetAttributeAsync("data-cfx-target-id"));
        await subtotalLegend.FocusAsync();
        AssertRows(await TooltipRowsAsync(page), "Subtotal", Signed(-130, sign), "Start", Signed(90, sign), "End", Signed(-40, sign));
        await CaptureAsync(page, capture, name + "-legend");
        if (opening.HasValue) {
            var openingLegend = page.Locator("[data-cfx-role='legend-item'][data-cfx-point='0']");
            Assert.Equal("movement:0", await openingLegend.GetAttributeAsync("data-cfx-target-id"));
            await openingLegend.FocusAsync();
            AssertRows(await TooltipRowsAsync(page), "Initial balance", Signed(120, sign), "Start", "0", "End", Signed(120, sign));
            await page.Keyboard.PressAsync("Space");
            Assert.Equal("true", await openingLegend.GetAttributeAsync("data-cfx-muted"));
            Assert.Contains("cfx-series-muted", await page.Locator(Mark(0)).GetAttributeAsync("class"));
            Assert.DoesNotContain("cfx-series-muted", await page.Locator(Mark(1)).GetAttributeAsync("class"));
            await page.Keyboard.PressAsync("Space");
            Assert.Equal("false", await openingLegend.GetAttributeAsync("data-cfx-muted"));
            Assert.Equal("movement:0", await page.Locator(Mark(0)).GetAttributeAsync("data-cfx-target-id"));
            Assert.Equal("0", await page.Locator(Mark(0)).GetAttributeAsync("data-cfx-source-point"));
            Assert.Equal(4, await page.EvaluateAsync<int>("() => window.waterfallSelections.length"));
            await deltaLegend.FocusAsync(); await openingLegend.FocusAsync();
            AssertRows(await TooltipRowsAsync(page), "Initial balance", Signed(120, sign), "Start", "0", "End", Signed(120, sign));
            await CaptureAsync(page, capture, name + "-opening-legend");
        }
        if (!string.IsNullOrWhiteSpace(capture))
            await File.WriteAllTextAsync(Path.Combine(capture, name + ".console.json"), JsonSerializer.Serialize(new { console = session.ConsoleLog, pageErrors = errors }));
        Assert.Empty(errors);
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

    private static string Signed(int value, int sign) => (value * sign).ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static Chart Create(int width, bool dark, double? opening) {
        var sign = opening < 0 ? -1 : 1;
        var chart = Chart.Create().WithSize(width, 440).WithTitle(opening.HasValue ? "Waterfall opening balance" : "Waterfall checkpoints")
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight()).WithFontFamily("Carlito")
            .WithPointLegend().WithDataLabels().WithDataLabelPlacement(ChartDataLabelPlacement.Inside).WithBarStyle(ChartBarStyle.Flat)
            .WithXLabels("Open", "Adjust", "Stage", "Cost", "Costs", "Recover", "Balance", "Late", "Late sum")
            .AddWaterfall("Movement", new[] {
                opening.HasValue ? ChartWaterfallItem.OpeningBalance(1, opening.Value) : ChartWaterfallItem.Delta(1, 120),
                ChartWaterfallItem.Delta(2, -30 * sign), ChartWaterfallItem.Subtotal(3),
                ChartWaterfallItem.Delta(4, -130 * sign), ChartWaterfallItem.Subtotal(5), ChartWaterfallItem.Delta(6, 70 * sign),
                ChartWaterfallItem.Total(7), ChartWaterfallItem.Delta(8, -20 * sign), ChartWaterfallItem.Subtotal(9)
            });
        chart.Options.PngFontPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        chart.Series[0].WithInteractionKey("movement").WithPointColor(4, "#A5358A").WithPointFillPattern(4, ChartFillPattern.Crosshatch);
        if (opening.HasValue) {
            chart.ConfigureLabels(labels => labels.OpeningBalance = "Initial balance");
            chart.Series[0].WithPointLabel(0, "Opening caption");
        }
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
