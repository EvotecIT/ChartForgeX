using System.Globalization;
using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Observed browser text and native raster use the same oriented and density-aware prepared scene.</summary>
public sealed class NumericRadialAxisPolicyBrowserTests {
    [BrowserTheory]
    [InlineData(true, true, true, false, false)]
    [InlineData(true, false, false, true, false)]
    [InlineData(false, true, false, true, false)]
    [InlineData(false, false, true, false, false)]
    [InlineData(true, true, false, false, true)]
    [InlineData(false, false, true, false, true)]
    public async Task AxisPoliciesRetainFittedInkAndNativePointSelection(bool bars, bool compact, bool dark, bool fullTurn, bool all) {
        if (!Enabled) return;
        var width = compact ? 360 : 800; var height = compact ? 360 : 440;
        var chart = all ? NumericRadialLabelDensityTests.CrowdedChart(bars) : NumericRadialAxisPolicyTests.RelativeChart(bars, compact);
        chart.WithSize(width, height).WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight());
        if (fullTurn) chart.WithRadialGeometry(new(30, 390, .2, .25, .1));
        if (all) chart.WithDataLabels().WithDataLabelPlacement(ChartDataLabelPlacement.Left);
        else {
            chart.Options.XAxis.LabelDensity = ChartLabelDensity.Relaxed;
            chart.Options.YAxis.LabelDensity = chart.Options.SecondaryYAxis.LabelDensity = ChartLabelDensity.Dense;
            chart.WithDataLabels().WithDataLabelPlacement(ChartDataLabelPlacement.Inside);
        }
        NumericRadialAxisTextBrowserTests.UseProofFonts(chart);
        var prepared = NumericRadialAxisPolicyTests.Prepare(chart);
        await using var session = await NumericRadialAxisTextBrowserTests.OpenTitleChartAsync(chart, width + 40, height + 80);
        var page = session.Page;
        var observed = await page.EvaluateAsync<JsonElement>("""
            () => {
                const svg = document.querySelector('.cfx-stage svg').getBoundingClientRect();
                const selector = '[data-cfx-role="radial-value-label"] text,[data-cfx-role="radial-category-label"] text';
                const captions = Array.from(document.querySelectorAll(selector)).map(text => {
                    const box=text.getBoundingClientRect(), matrix=text.getScreenCTM();
                    return { text:text.textContent, left:box.left, right:box.right, top:box.top, bottom:box.bottom,
                        angle:Math.atan2(matrix.b,matrix.a)*180/Math.PI };
                });
                return { captions, fitted:captions.length>0 && captions.every(box=>box.left>=svg.left-.75 && box.right<=svg.right+.75 && box.top>=svg.top-.75 && box.bottom<=svg.bottom+.75),
                    upright:captions.every(box=>box.angle>=-90.01 && box.angle<=90.01) };
            }
            """);
        Assert.True(observed.GetProperty("fitted").GetBoolean());
        Assert.True(observed.GetProperty("upright").GetBoolean());
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        var name = $"axis-policy-{(all ? "all" : "relative")}-{(bars ? "bar" : "column")}-{(compact ? "compact" : "wide")}-{(dark ? "dark" : "light")}-{(fullTurn ? "full" : "partial")}";
        var stem = string.IsNullOrWhiteSpace(capture) ? null : Path.Combine(capture, name);
        if (stem != null) {
            Directory.CreateDirectory(capture!);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + "-browser.png", FullPage = true });
            File.WriteAllText(stem + ".svg", prepared.ToSvg()); File.WriteAllBytes(stem + "-native.png", prepared.ToPng());
            File.WriteAllText(stem + ".html", await page.ContentAsync());
        }
        var mark = NumericRadialSeriesTests.Marks(prepared.Scene)[0];
        var angle = mark.Start + mark.Sweep / 2; var radius = (mark.Inner + mark.Outer) / 2;
        var point = new { x = mark.Cx + Math.Cos(angle) * radius, y = mark.Cy + Math.Sin(angle) * radius };
        var screen = await page.EvaluateAsync<double[]>("point=>{const svg=document.querySelector('.cfx-stage svg'),p=svg.createSVGPoint();p.x=point.x;p.y=point.y;const q=p.matrixTransform(svg.getScreenCTM());return [q.x,q.y];}", point);
        await page.Mouse.MoveAsync((float)screen[0], (float)screen[1], new MouseMoveOptions { Steps = 4 });
        Assert.Contains(chart.Series[0].Points[0].Y.ToString("0", CultureInfo.InvariantCulture), (await TooltipTextAsync(page)).Replace(",", "", StringComparison.Ordinal), StringComparison.Ordinal);
        await page.Mouse.ClickAsync((float)screen[0], (float)screen[1]);
        Assert.Equal("true", await page.Locator(Point(0, 0)).GetAttributeAsync("aria-selected"));
        AssertNoConsoleErrors(session);
        if (stem != null) File.WriteAllText(stem + "-receipt.json", JsonSerializer.Serialize(new {
            name, bars, width, height, dark, fullTurn, all, observed,
            diagnostics = prepared.Diagnostics.Select(item => item.Code),
            nativeCaptions = NumericRadialAxisTextTests.Footprints(prepared.Scene).Where(item => item.Text.Role is "radial-category-label" or "radial-value-label")
                .Select(item => new { item.Text.Id, item.Angle, item.Bounds }),
            console = session.ConsoleLog.Select(item => new { type = item.Type.ToString(), item.Text }),
            selected = await page.Locator(Point(0, 0)).GetAttributeAsync("aria-selected")
        }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
