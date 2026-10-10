using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractiveSunburstSecondaryLabelBrowserTests {
    [Theory]
    [InlineData(360, false, 0)]
    [InlineData(360, true, 0)]
    [InlineData(800, false, 0)]
    [InlineData(800, true, 0)]
    [InlineData(360, false, 6)]
    [InlineData(360, true, 6)]
    [InlineData(800, false, 6)]
    [InlineData(800, true, 6)]
    public async Task PreparedSecondaryTextHasSafePointerAndKeyboardReadoutsWithoutInventingNumericTargets(int width, bool dark, double radius) {
        if (!Enabled) return;
        var chart = SunburstSecondaryLabelTests.Gallery(width, dark, radius).WithHeader(true)
            .WithTitle("Allocation by team").WithSubtitle("Primary captions and authored secondary text");
        chart.ConfigureLabels(labels => { labels.AuthoredValue = "Dostarczono <&>"; labels.Remainder = "Pozostało <&>"; });
        chart.Series[0].WithInteractionKey("allocation-source");
        var descriptions = new Dictionary<string, string> {
            ["all"] = "Allocation", ["engineering"] = "Delivery <b>& teams", ["operations-support"] = "Shared service", ["reserve"] = "No allocation <&>"
        };
        var inclusive = radius > 0; var calls = 0;
        chart.ConfigureSunburst(options => {
            options.ParentValuePolicy = inclusive ? ChartHierarchyValuePolicy.AuthoredTotal : ChartHierarchyValuePolicy.LeafAggregate;
            options.SecondaryLabelFormatter = context => { calls++; return descriptions.TryGetValue(context.Item.Id, out var text) ? text : null; };
        });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var html = chart.ToInteractiveHtmlPage(options => options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit);
        Assert.Equal(14, calls);
        var font = Convert.ToBase64String(await File.ReadAllBytesAsync(chart.Options.PngFontPath!));
        html = html.Replace("</head>", "<style>@font-face{font-family:'Carlito';src:url(data:font/ttf;base64," + font + ")}</style></head>", StringComparison.Ordinal);
        chart.Options.Sunburst.SecondaryLabelFormatter = _ => throw new InvalidOperationException("Prepared captions must remain detached.");
        chart.Options.Sunburst.SecondaryLabelStyle.WithFontSize(40); descriptions["engineering"] = "Changed after preparation";
        await using var session = await OpenAsync(html, width + 24, width < 500 ? 510 : 590);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await page.EvaluateAsync("() => document.fonts.ready");
        Assert.True(await page.EvaluateAsync<bool>("() => document.fonts.check('12px Carlito')"));
        Assert.Equal(7, await page.Locator("[data-cfx-target-kind=node]").CountAsync());
        Assert.Equal(0, await page.Locator("[data-cfx-target-kind=node][data-cfx-point],[data-cfx-target-kind=node][data-cfx-source-point]").CountAsync());
        Assert.True(await page.Locator("[data-cfx-role=sunburst-secondary-label]").CountAsync() >= 1);
        await page.EvaluateAsync("() => document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.selectedTarget = event.detail.target)");
        await Capture(page, prepared, html, width, dark, radius, "chart", errors, session.ConsoleLog.Select(entry => entry.Type + ": " + entry.Text));
        var engineering = page.Locator("[data-cfx-node=engineering]");
        var point = await NativeHit(engineering);
        await page.Mouse.MoveAsync((float)point[0], (float)point[1], new MouseMoveOptions { Steps = 3 });
        await AssertSecondary(page, "Delivery <b>& teams");
        Assert.Equal(0, await page.Locator(".cfx-tooltip__secondary b").CountAsync());
        Assert.Equal(inclusive ? "80" : "60", await engineering.GetAttributeAsync("data-cfx-value"));
        Assert.Equal("12", await engineering.GetAttributeAsync("data-cfx-color-value"));
        await engineering.FocusAsync(); await page.Keyboard.PressAsync("Enter");
        using (var target = JsonDocument.Parse(await page.EvaluateAsync<string>("() => JSON.stringify(window.selectedTarget)"))) {
            Assert.Equal("node", target.RootElement.GetProperty("targetKind").GetString());
            Assert.Equal("engineering", target.RootElement.GetProperty("targetId").GetString());
            Assert.Equal("Engineering", target.RootElement.GetProperty("label").GetString());
            Assert.Equal("allocation-source", target.RootElement.GetProperty("seriesKey").GetString());
            Assert.Equal(inclusive ? "80" : "60", target.RootElement.GetProperty("value").GetString());
            Assert.False(target.RootElement.TryGetProperty("point", out _));
        }
        await AssertSecondary(page, "Delivery <b>& teams");
        var rows = await page.Locator(".cfx-tooltip__meta").InnerTextAsync();
        Assert.Contains(inclusive ? "Pozostało <&>" : "Dostarczono <&>", rows);
        Assert.DoesNotContain("Delivery", rows);
        await Capture(page, prepared, html, width, dark, radius, "readout", errors, session.ConsoleLog.Select(entry => entry.Type + ": " + entry.Text));
        await engineering.FocusAsync(); await page.Keyboard.PressAsync("Enter");
        await page.EvaluateAsync("() => document.activeElement?.blur()"); await MoveAwayAsync(page);
        var secondaryCaption = page.Locator("[data-cfx-role=sunburst-secondary-label]").First;
        await secondaryCaption.HoverAsync();
        await page.WaitForFunctionAsync("() => !!document.querySelector('.cfx-hovered[data-cfx-target-kind=node]')");
        Assert.False(string.IsNullOrEmpty(await TooltipTextAsync(page)));
        var zero = page.Locator("[data-cfx-node=reserve]");
        Assert.Equal(0, await zero.Locator("path,text").CountAsync());
        await zero.FocusAsync(); await page.Keyboard.PressAsync("Space");
        await AssertSecondary(page, "No allocation <&>");
        Assert.Equal("reserve", await page.EvaluateAsync<string>("() => window.selectedTarget.targetId"));
        Assert.Equal("0", await page.EvaluateAsync<string>("() => window.selectedTarget.value"));
        Assert.Equal(7, await page.Locator("[data-cfx-target-kind=node]").CountAsync());
        Assert.Equal(14, calls); Assert.Empty(errors); AssertNoConsoleErrors(session);
        await Capture(page, prepared, html, width, dark, radius, "zero", errors, session.ConsoleLog.Select(entry => entry.Type + ": " + entry.Text));
    }

    private static async Task AssertSecondary(IPage page, string text) {
        await page.WaitForFunctionAsync("() => { const tip=document.querySelector('.cfx-tooltip'); return tip && !tip.hidden; }");
        Assert.Equal(text, await page.Locator(".cfx-tooltip__secondary").InnerTextAsync());
    }
    private static Task<double[]> NativeHit(ILocator node) => node.EvaluateAsync<double[]>("""
        node => { const box=node.getBoundingClientRect();
            for (let y=1;y<30;y++) for (let x=1;x<30;x++) {
                const px=box.left+box.width*x/30, py=box.top+box.height*y/30, hit=document.elementFromPoint(px,py);
                if (hit && hit.closest('[data-cfx-target-kind=node]') === node && !hit.hasAttribute('data-cfx-browser-hit-area')) return [px,py];
            } throw new Error('No native Sunburst sector hit'); }
        """);
    private static async Task Capture(IPage page, PreparedVisual prepared, string html, int width, bool dark, double radius,
        string state, List<string> errors, IEnumerable<string> console) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        var stem = "secondary-" + width + "-" + (dark ? "dark" : "light") + "-r" + radius + "-" + state;
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, stem + ".png") });
        await File.WriteAllTextAsync(Path.Combine(directory, stem + ".json"), JsonSerializer.Serialize(new { width, dark, radius, state, errors, console }, new JsonSerializerOptions { WriteIndented = true }));
        if (state != "chart") return;
        await File.WriteAllTextAsync(Path.Combine(directory, stem + ".html"), html);
        await File.WriteAllTextAsync(Path.Combine(directory, stem + ".svg"), prepared.ToSvg());
        await File.WriteAllBytesAsync(Path.Combine(directory, stem + ".native.png"), prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }
}
