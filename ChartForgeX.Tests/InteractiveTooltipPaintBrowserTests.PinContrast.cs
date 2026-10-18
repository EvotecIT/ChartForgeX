using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipPaintBrowserTests {
    [BrowserTheory]
    [Trait("BrowserSuite", "Compatibility")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PinnedLabelKeepsReadableThemeContrastAndTranslatedContent(bool dark) {
        if (!Enabled) return;
        const string caption = "Przypięte";
        var chart = Frame(dark).WithTitle("Pinned observations")
            .AddLine("Reading", new[] { new ChartPoint(0, 5), new ChartPoint(1, 8) });
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Localization[HtmlChartText.Pinned] = caption;
        }), 700, 760);
        var page = session.Page;
        await FocusWithKeyboardAsync(page, page.Locator(Point(0, 1)));
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await page.Locator(Root).GetAttributeAsync("data-cfx-tooltip-pinned"));
        var tip = page.Locator(".cfx-tooltip");
        Assert.True(await tip.IsVisibleAsync());
        var paint = await tip.EvaluateAsync<JsonElement>("""
            tip=>{
                const badge=getComputedStyle(tip,'::before'),surface=getComputedStyle(tip);
                return {ink:badge.color,surface:surface.backgroundColor,content:badge.content,
                    weight:badge.fontWeight,transform:badge.textTransform};
            }
            """);
        var rgba = await ResolveCssColorsAsync(page, paint.GetProperty("ink").GetString()!, paint.GetProperty("surface").GetString()!);
        ChartColor Color(int[] channels) => ChartColor.FromRgba((byte)channels[0], (byte)channels[1], (byte)channels[2], (byte)channels[3]);
        Assert.Equal(255, rgba[1][3]);
        var contrast = ChartColorMath.ContrastRatio(Color(rgba[0]), Color(rgba[1]));
        await CaptureContractAsync(page, "paint-pinned-label-" + (dark ? "dark" : "light"), new { paint, contrast, rgba });
        Assert.True(contrast >= 4.5, "Pinned label contrast is " + contrast);
        Assert.Contains(caption, paint.GetProperty("content").GetString()!, StringComparison.Ordinal);
        Assert.Equal("900", paint.GetProperty("weight").GetString());
        Assert.Equal("uppercase", paint.GetProperty("transform").GetString());
        AssertNoConsoleErrors(session);
    }
}
