using System.Text.Json;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using HtmlTinkerX;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Actual hosts preserve readable axis text and source readouts after measured title/rotation layout.</summary>
public sealed class NumericRadialAxisTextBrowserTests {
    [Theory]
    [InlineData(false, 360, false)]
    [InlineData(false, 360, true)]
    [InlineData(false, 800, false)]
    [InlineData(false, 800, true)]
    [InlineData(true, 360, false)]
    [InlineData(true, 360, true)]
    [InlineData(true, 800, false)]
    [InlineData(true, 800, true)]
    public async Task TitlesAndFixedLabelsRetainNativeCentreTooltipAndSelection(bool bars, int width, bool dark) {
        if (!Enabled) return;
        var chart = NumericRadialAxisTextTests.Configured(bars, width, dark);
        chart.Series[0].WithInteractionKey("requests"); chart.Series[1].WithInteractionKey("resolved");
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var mark = NumericRadialSeriesTests.Marks(prepared.Scene)[0];
        var angle = mark.Start + mark.Sweep / 2; var radius = (mark.Inner + mark.Outer) / 2;
        var native = new { x = mark.Cx + Math.Cos(angle) * radius, y = mark.Cy + Math.Sin(angle) * radius };
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), width + 40, width == 360 ? 440 : 520);
        var page = session.Page;
        Assert.Equal(3, await page.Locator("[data-cfx-role='radial-value-axis-title'] text,[data-cfx-role='radial-category-axis-title'] text").CountAsync());
        Assert.True(await page.EvaluateAsync<bool>("""
            () => {
                const svg = document.querySelector('.cfx-stage svg').getBoundingClientRect();
                const texts = Array.from(document.querySelectorAll('[data-cfx-role="radial-value-axis-title"] text,[data-cfx-role="radial-category-axis-title"] text,[data-cfx-role="radial-value-label"] text,[data-cfx-role="radial-category-label"] text'));
                const boxes = texts.map(text => text.getBoundingClientRect());
                return boxes.every((a,i) => a.width > 0 && a.height > 0 && a.left >= svg.left-.1 && a.right <= svg.right+.1 && a.top >= svg.top-.1 && a.bottom <= svg.bottom+.1 &&
                    boxes.slice(i+1).every(b => a.right<=b.left+.1 || b.right<=a.left+.1 || a.bottom<=b.top+.1 || b.bottom<=a.top+.1));
            }
            """));
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        var stem = string.IsNullOrWhiteSpace(capture) ? null : Path.Combine(capture, $"axis-text-{(bars ? "bar" : "column")}-{(width == 360 ? "compact" : "wide")}-{(dark ? "dark" : "light")}");
        if (stem != null) {
            Directory.CreateDirectory(capture!);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + "-browser.png", FullPage = true });
        }
        await page.EvaluateAsync("() => { window.cfxSelections=[];document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect',event=>window.cfxSelections.push(event.detail)); }");
        var screen = await page.EvaluateAsync<double[]>("point => { const svg=document.querySelector('.cfx-stage svg'); const p=svg.createSVGPoint(); p.x=point.x; p.y=point.y; const q=p.matrixTransform(svg.getScreenCTM());return [q.x,q.y]; }", native);
        await page.Mouse.MoveAsync((float)screen[0], (float)screen[1], new MouseMoveOptions { Steps = 4 });
        var tooltip = await TooltipTextAsync(page);
        Assert.Matches("1,?200", tooltip);
        await page.Mouse.ClickAsync((float)screen[0], (float)screen[1]);
        Assert.Equal("true", await page.Locator(Point(0, 0)).GetAttributeAsync("aria-selected"));
        Assert.Equal("0", await page.EvaluateAsync<string>("() => window.cfxSelections[0].target.sourcePoint"));
        Assert.Equal("requests:0", await page.EvaluateAsync<string>("() => window.cfxSelections[0].target.targetId"));
        if (stem != null) {
            File.WriteAllText(stem + ".svg", prepared.ToSvg()); File.WriteAllBytes(stem + "-native.png", prepared.ToPng());
            File.WriteAllText(stem + ".html", await page.ContentAsync());
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + "-readout.png", FullPage = true });
            File.WriteAllText(stem + "-receipt.json", JsonSerializer.Serialize(new { nativeCentre = native, screenCentre = screen, tooltip,
                sourcePoint = await page.EvaluateAsync<string>("() => window.cfxSelections[0].target.sourcePoint"),
                textBounds = NumericRadialAxisTextTests.Footprints(prepared.Scene).Select(item => new { item.Text.Role, item.Text.Id, item.Bounds, item.Angle }),
                console = session.ConsoleLog.Select(entry => new { type = entry.Type.ToString(), entry.Text }) }, new JsonSerializerOptions { WriteIndented = true }));
        }
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task InsufficientSpaceRetainsKeyboardFactsWithoutInventingAPointerSurface() {
        if (!Enabled) return;
        var chart = NumericRadialAxisTextTests.LongTitles(true);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), 400, 440);
        var page = session.Page; var target = page.Locator(Point(0, 0));
        Assert.Equal(6, await page.Locator("[data-cfx-role='point'][data-cfx-geometry-status='layout-collapse']").CountAsync());
        Assert.Equal(0, await page.Locator("[data-cfx-role='radial-bar'],[data-cfx-role='point'] path,[data-cfx-role='point'] rect").CountAsync());
        Assert.Equal("0", await target.GetAttributeAsync("tabindex"));
        await target.FocusAsync();
        Assert.Matches("1,?200", await TooltipTextAsync(page));
        await page.Keyboard.PressAsync("ArrowRight");
        Assert.Equal("1", await page.EvaluateAsync<string>("() => document.activeElement.dataset.cfxPoint"));
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            var stem = Path.Combine(capture, "axis-text-layout-collapse");
            Directory.CreateDirectory(capture);
            File.WriteAllText(stem + ".svg", chart.ToSvg()); File.WriteAllBytes(stem + "-native.png", chart.ToPng());
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + "-browser.png" });
            File.WriteAllText(stem + "-receipt.json", JsonSerializer.Serialize(new { pointCount = 6, pointerSurfaces = 0, keyboardReadout = await TooltipTextAsync(page), consoleErrors = session.ConsoleLog.Count(entry => entry.Type == HtmlConsoleMessageType.Error) }));
        }
        AssertNoConsoleErrors(session);
    }
}
