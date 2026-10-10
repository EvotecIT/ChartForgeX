using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveOverlayCoordinatesBrowserTests {
    [Theory]
    [InlineData(HtmlChartResponsiveLayout.Fit)]
    [InlineData(HtmlChartResponsiveLayout.Readable)]
    public async Task AVisibleGuideCannotExpandTheScrollExtentWhenItsHostShrinks(HtmlChartResponsiveLayout layout) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Host(false, layout, 650, 0.8, false, ChartInteractionFeatures.Crosshair), 950, 760);
        var page = session.Page;
        var mark = await BoxAsync(page, Point(0, 0));
        await page.Mouse.MoveAsync(mark.X + mark.Width / 2, mark.Y + mark.Height / 2);
        Assert.True(await page.Locator(".cfx-crosshair").IsVisibleAsync());
        await page.Locator("#host").EvaluateAsync("h => h.style.width = '340px'");
        await page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");
        var visible = await page.Locator(".cfx-crosshair").IsVisibleAsync();
        var withGuide = await ScrollExtentAsync(page);
        await page.Locator(".cfx-crosshair").EvaluateAsync("c => c.hidden = true");
        var withoutGuide = await ScrollExtentAsync(page);
        await page.Locator(".cfx-crosshair").EvaluateAsync("(c,visible) => c.hidden = !visible", visible);
        await CaptureAsync(session, "overlay-resize-" + layout, new { visible, withGuide, withoutGuide });
        Assert.Equal(withoutGuide, withGuide);
        Assert.False(visible);
        mark = await BoxAsync(page, Point(0, 0));
        var native = new double[] { mark.X + mark.Width / 2, mark.Y + mark.Height / 2 };
        await page.Mouse.MoveAsync((float)native[0], (float)native[1]);
        var guide = await GuideGeometryAsync(page);
        await CaptureAsync(session, "overlay-resize-rehover-" + layout, new { native, guide });
        AssertGuideGeometry(guide, native);
        AssertNoConsoleErrors(session);
    }
}
