using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Microsoft.Playwright;
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
        Assert.Equal(caption, await tip.GetAttributeAsync("aria-description"));
        var close = tip.GetByRole(AriaRole.Button, new() { Name = "Close tooltip", Exact = true });
        await Assertions.Expect(close).ToBeVisibleAsync(new() { Timeout = 1000 });
        Assert.Equal("×", await close.InnerTextAsync());
        Assert.Equal("8", await page.Locator(Point(0, 1)).GetAttributeAsync("data-cfx-y"));
        Assert.Equal("8", await tip.Locator("dd").InnerTextAsync());
        Assert.Contains("Reading", await TooltipTextAsync(page), StringComparison.Ordinal);
        var paint = await tip.EvaluateAsync<JsonElement>("""
            tip=>{
                const close=tip.querySelector('.cfx-tooltip__close'),control=getComputedStyle(close),surface=getComputedStyle(tip);
                return {ink:control.color,surface:surface.backgroundColor,accentWidth:surface.borderInlineStartWidth,
                    description:tip.getAttribute('aria-description'),closeLabel:close.getAttribute('aria-label')};
            }
            """);
        var rgba = await ResolveCssColorsAsync(page, paint.GetProperty("ink").GetString()!, paint.GetProperty("surface").GetString()!);
        ChartColor Color(int[] channels) => ChartColor.FromRgba((byte)channels[0], (byte)channels[1], (byte)channels[2], (byte)channels[3]);
        Assert.Equal(255, rgba[1][3]);
        var contrast = ChartColorMath.ContrastRatio(Color(rgba[0]), Color(rgba[1]));
        await CaptureContractAsync(page, "paint-pinned-label-" + (dark ? "dark" : "light"), new { paint, contrast, rgba });
        Assert.True(contrast >= 4.5, "Pinned close control contrast is " + contrast);
        Assert.Equal("2px", paint.GetProperty("accentWidth").GetString());
        AssertNoConsoleErrors(session);
    }
}
