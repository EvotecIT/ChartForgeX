using Microsoft.Playwright;
using Xunit;
using Xunit.Sdk;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>CSS spelling changes must not hide differences in authored paint, alpha, or invalid inputs.</summary>
public sealed class BrowserColorComparisonTests {
    [BrowserFact]
    public async Task EquivalentPaintKeepsExactRgbaAndDifferentOrInvalidPaintStillFails() {
        if (!Enabled) return;
        await using var session = await OpenAsync("<!doctype html><html><body></body></html>");
        var page = session.Page;
        await AssertCssColorsEqualAsync(page, "rgb(0, 255, 0)", "color(srgb 0 1 0)");
        await AssertCssColorsEqualAsync(page, "rgba(255, 0, 0, .5)", "color(srgb 1 0 0 / .5)");
        await AssertCssColorsEqualAsync(page, "transparent", "color(srgb 0 0 0 / 0)");
        await AssertCssColorsDifferentAsync(page, "#00ff00", "#ff00ff");
        await Assert.ThrowsAsync<EqualException>(() => AssertCssColorsDifferentAsync(page, "#00ff00", "color(srgb 0 1 0)"));
        await Assert.ThrowsAsync<EqualException>(() => AssertCssColorsEqualAsync(page, "#00ff00", "#ff00ff"));
        await Assert.ThrowsAsync<EqualException>(() => AssertCssColorsEqualAsync(page, "rgba(255, 0, 0, .5)", "rgba(255, 0, 0, .25)"));
        await Assert.ThrowsAsync<PlaywrightException>(() => AssertCssColorsEqualAsync(page, "#010203", "not-a-color"));
        await Assert.ThrowsAsync<PlaywrightException>(() => AssertCssColorsEqualAsync(page, "#040506", "var(--undefined-paint)"));
        AssertNoConsoleErrors(session);
    }
}
