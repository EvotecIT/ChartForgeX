using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipDelayBrowserTests {
    [Theory]
    [InlineData(0)]
    [InlineData(Delay)]
    public async Task PointerHandoffReturnsToTheFocusedShadowLegendWithoutQueuingItsReadout(int delay) {
        if (!Enabled) return;
        await using var session = await OpenAsync(HostFragment(delay: delay), 950, 760);
        var page = session.Page;
        await MountFragmentAsync(page, "nested-closed");
        await RecordHostExpiryAsync(page);
        await page.EvaluateAsync("() => window.hostedChart.querySelector('[data-cfx-role=legend-item][data-cfx-series=\"0\"]').focus()");
        var focusedText = (await HostReceiptAsync(page)).GetProperty("text").GetString();
        Assert.Contains("Reading", focusedText!, StringComparison.Ordinal);
        Assert.False(await HostedTipHiddenAsync(page));

        await MoveToHostTargetAsync(page, Point(1, 1));
        if (delay > 0) {
            Assert.True(await HostedTipHiddenAsync(page));
            await WaitForHostExpiryAsync(page);
        }
        var pointer = await HostReceiptAsync(page);
        Assert.False(pointer.GetProperty("hidden").GetBoolean());
        Assert.StartsWith("point|", pointer.GetProperty("hover").GetString()!, StringComparison.Ordinal);
        Assert.NotEqual(focusedText, pointer.GetProperty("text").GetString());
        await MoveAwayAsync(page);
        var focus = await HostReceiptAsync(page);
        Assert.False(focus.GetProperty("hidden").GetBoolean());
        Assert.Equal(focusedText, focus.GetProperty("text").GetString());
        Assert.StartsWith("legend|", focus.GetProperty("hover").GetString()!, StringComparison.Ordinal);
        Assert.True(await page.EvaluateAsync<bool>("() => window.hostedChart.querySelector('.cfx-crosshair').hidden"));
        Assert.Equal(pointer.GetProperty("timers").GetProperty("queued").GetArrayLength(), focus.GetProperty("timers").GetProperty("queued").GetArrayLength());

        await page.Keyboard.PressAsync("Space");
        await page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");
        Assert.True(await page.EvaluateAsync<bool>("() => window.hostedChart.querySelector('[data-cfx-role=legend-item][data-cfx-series=\"0\"]').dataset.cfxMuted === 'true'"));
        Assert.False(await HostedTipHiddenAsync(page));
        var recovered = await HostReceiptAsync(page);
        await CaptureDelayAsync(page, "delay-focused-shadow-legend-handoff-" + delay, recovered, session);
        await page.Keyboard.PressAsync("Space");
        await page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");
        Assert.Equal(focusedText, (await HostReceiptAsync(page)).GetProperty("text").GetString());
        await page.EvaluateAsync("() => window.hostedChart.querySelector('[data-cfx-role=legend-item][data-cfx-series=\"0\"]').blur()");
        Assert.True(await HostedTipHiddenAsync(page));
        AssertNoConsoleErrors(session);
    }
}
