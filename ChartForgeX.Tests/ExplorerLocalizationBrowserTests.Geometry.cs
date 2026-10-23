using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class ExplorerLocalizationBrowserTests {
    [BrowserTheory]
    [InlineData("text")]
    [InlineData("position")]
    [InlineData("direction")]
    public async Task NativeComparisonRejectsCanvasDrawingFactChanges(string change) {
        if (!Enabled) return;
        await using var session = await OpenAsync("<!doctype html><html><body></body></html>", 600, 400);
        var page = session.Page;
        await ObserveNativeCanvasPaintAsync(page);
        await page.SetContentAsync("""
            <!doctype html><html><body><main>
            <canvas data-cfx-role="localization-canvas" width="400" height="200"></canvas>
            <script>
              window.drawNative=change=>{const context=document.querySelector('canvas').getContext('2d');
                context.clearRect(0,0,400,200);context.font='20px Arial';context.fillStyle='#172033';
                context.textBaseline='top';context.textAlign='start';context.direction=change==='direction'?'rtl':'ltr';
                context.fillText(change==='text'?'ABD אבג':'ABC אבג',change==='position'?100:80,40);};
              drawNative('');
            </script></main></body></html>
            """);
        var baseline = await NativeSnapshotAsync(page, "main");
        var drawing = baseline.GetProperty("surfaces")[0].GetProperty("drawing");
        Assert.Equal(System.Text.Json.JsonValueKind.Object, drawing.ValueKind);
        Assert.Contains(drawing.GetProperty("commands").EnumerateArray(),
            command => command.GetProperty("operation").GetString() == "fillText");
        AssertNativeSnapshotEqual(baseline, await NativeSnapshotAsync(page, "main"));
        await page.EvaluateAsync("change=>drawNative(change)", change);
        var changed = await NativeSnapshotAsync(page, "main");
        Assert.NotNull(Record.Exception(() => AssertNativeSnapshotEqual(baseline, changed)));
        AssertNoConsoleErrors(session);
    }

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
