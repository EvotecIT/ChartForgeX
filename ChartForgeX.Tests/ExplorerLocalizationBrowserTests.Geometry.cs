using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class ExplorerLocalizationBrowserTests {
    [BrowserTheory]
    [InlineData("translation")]
    [InlineData("glyph-width")]
    public async Task NativeComparisonRejectsVisibleSvgTextGeometryChanges(string change) {
        if (!Enabled) return;
        await using var session = await OpenAsync("""
            <!doctype html><html><body><main>
            <svg data-cfx-role="graph-scene" viewBox="0 0 200 100" width="400" height="200">
              <g><text x="20" y="40" font-size="20">M</text></g>
            </svg></main></body></html>
            """, 600, 400);
        var page = session.Page;
        var baseline = await NativeSnapshotAsync(page, "main");
        AssertNativeSnapshotEqual(baseline, await NativeSnapshotAsync(page, "main"));
        if (change == "translation")
            await page.Locator("svg g").EvaluateAsync("node=>node.setAttribute('transform','translate(20 0)')");
        else
            await page.Locator("svg text").EvaluateAsync("node=>node.setAttribute('font-size','40')");
        var changed = await NativeSnapshotAsync(page, "main");
        Assert.NotNull(Record.Exception(() => AssertNativeSnapshotEqual(baseline, changed)));
        AssertNoConsoleErrors(session);
    }
}
