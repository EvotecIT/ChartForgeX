using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractivePreparedIdentityBrowserTests {
    [Fact]
    public async Task MarkerFreeDecimatedObservationEmitsOneOriginalSourceSelectionAndSupportsKeyboard() {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(596, 338).WithLineMarkers(ChartLineMarkerMode.None)
            .AddDecimatedLine("Latency", Enumerable.Range(0, 100).Select(index => new ChartPoint(index, Math.Sin(index / 4d))), 12);
        chart.Series[0].WithInteractionKey("latency-source");
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage());
        var page = session.Page;
        await page.EvaluateAsync("() => { window.cfxSelections = []; document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.cfxSelections.push(event.detail)); }");
        var point = page.Locator(Point(0, 3));
        Assert.Equal("latency-source:" + chart.Series[0].SourcePointIndices[3], await point.GetAttributeAsync("data-cfx-target-id"));
        Assert.Equal(1, await point.Locator("[data-cfx-browser-hit-area]").CountAsync());
        await point.ClickAsync();
        Assert.Equal("true", await point.GetAttributeAsync("aria-selected"));
        Assert.Equal(1, await page.EvaluateAsync<int>("() => window.cfxSelections.length"));
        Assert.Equal("point", await page.EvaluateAsync<string>("() => window.cfxSelections[0].target.targetKind"));
        Assert.Equal(chart.Series[0].SourcePointIndices[3].ToString(), await page.EvaluateAsync<string>("() => window.cfxSelections[0].target.sourcePoint"));
        await point.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("false", await point.GetAttributeAsync("aria-selected"));
        Assert.Equal(2, await page.EvaluateAsync<int>("() => window.cfxSelections.length"));
        AssertNoConsoleErrors(session);
    }
}
