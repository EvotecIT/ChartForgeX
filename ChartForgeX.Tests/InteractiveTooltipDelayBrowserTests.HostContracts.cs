using ChartForgeX.Core;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipDelayBrowserTests {
    [BrowserTheory]
    [InlineData("nearest", 950)]
    [InlineData("distance", 340)]
    public async Task SparseShadowPlotKeepsOneDeadlineAndTheLatestPointer(string range, int width) {
        if (!Enabled) return;
        await using var session = await OpenAsync(HostFragment(range), width, 760);
        var page = session.Page;
        await MountFragmentAsync(page, "nested-closed");
        await RecordHostExpiryAsync(page);
        var position = await MoveToHostTargetAsync(page, Point(0, 0));
        await AssertDelayedNativeEntryAsync(page);
        var offset = await page.EvaluateAsync<double>("""
            () => {
                const root=window.hostedChart, own=root.querySelector('[data-cfx-role=point][data-cfx-series="0"][data-cfx-point="0"]').getBoundingClientRect();
                const peer=root.querySelector('[data-cfx-role=point][data-cfx-series="1"][data-cfx-point="0"]').getBoundingClientRect();
                return Math.min(36,(peer.y+peer.height/2-own.y-own.height/2)/3);
            }
            """);
        Assert.False(await page.EvaluateAsync<bool>("() => window.hostedChart.querySelector('.cfx-crosshair').hidden"));
        for (var step = 0; step < 30 && await HostedTipHiddenAsync(page); step++) {
            await Task.Delay(40);
            await page.Mouse.MoveAsync((float)(position[0] + (step % 2 == 0 ? .15 : -.15)), (float)(position[1] + offset));
        }
        var receipt = await HostReceiptAsync(page);
        await CaptureDelayAsync(page, "delay-shadow-sparse-" + range + "-" + width, receipt, session);
        Assert.False(receipt.GetProperty("hidden").GetBoolean());
        Assert.Single(receipt.GetProperty("trace").GetProperty("shown").EnumerateArray());
        AssertDelay(receipt.GetProperty("trace"));
        Assert.True(await page.EvaluateAsync<bool>("""
            () => {
                const tip=window.hostedChart.querySelector('.cfx-tooltip'), p=window.delayTrace.pointer;
                return Math.abs(parseFloat(tip.style.left)-Math.max(8,Math.min(innerWidth-tip.offsetWidth-8,p[0]+14)))<.01;
            }
            """));
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ShadowMappedCaptionsAndLegendsRetainTheirDelayedNativeIdentity(bool legend) {
        if (!Enabled) return;
        var chart = legend ? Observations(false) : RadialLabelTargetBrowserTests.Create(ChartSeriesKind.Pie, true);
        await using var session = await OpenAsync(HostFragment(legend ? "nearest" : "exact", chart), 950, 760);
        var page = session.Page;
        await MountFragmentAsync(page, "closed");
        await RecordHostExpiryAsync(page);
        var selector = legend ? Legend(0) : "[data-cfx-label-for='series-0-point-0']";
        await BrowserProofCaptions.InstallNativeAsync(page);
        var position = await page.EvaluateAsync<double[]>("""
            async selector => {
                const node=window.hostedChart.querySelector(selector);
                await window.cfxCaptionPrepareRaster(node);
                const point=window.cfxCaptionPoint(node);
                return [point.x,point.y];
            }
            """, selector);
        await page.Mouse.MoveAsync((float)position[0], (float)position[1]);
        await AssertDelayedHostAcquisitionAsync(page);
        await WaitForHostExpiryAsync(page);
        var receipt = await HostReceiptAsync(page);
        await CaptureDelayAsync(page, "delay-shadow-" + (legend ? "legend" : "caption"), receipt, session);
        Assert.False(receipt.GetProperty("hidden").GetBoolean());
        Assert.Contains(legend ? "Reading" : "1200", receipt.GetProperty("text").GetString()!.Replace(",", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.Contains(legend ? "legend|" : "point|", receipt.GetProperty("hover").GetString()!, StringComparison.Ordinal);
        AssertDelay(receipt.GetProperty("trace"));
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData("default-zero")]
    [InlineData("keyboard")]
    [InlineData("pin")]
    public async Task ShadowImmediateAccessBypassesOrCancelsThePointerQueue(string access) {
        if (!Enabled) return;
        await using var session = await OpenAsync(HostFragment(delay: access == "default-zero" ? 0 : Delay), 950, 760);
        var page = session.Page;
        await MountFragmentAsync(page, "nested-closed");
        var clockStart = DateTime.UtcNow;
        await page.Clock.InstallAsync(new ClockInstallOptions { TimeDate = clockStart });
        await page.Clock.PauseAtAsync(clockStart.AddHours(1));
        await RecordHostExpiryAsync(page);
        await MoveToHostTargetAsync(page, Point(0, 0));
        if (access != "default-zero") {
            Assert.True(await HostedTipHiddenAsync(page));
            await page.EvaluateAsync("""() => window.hostedChart.querySelector('[data-cfx-role=point][data-cfx-series="0"][data-cfx-point="1"]').focus()""");
            Assert.False(await HostedTipHiddenAsync(page));
            if (access == "pin") await page.Keyboard.PressAsync("Space");
        }
        Assert.False(await HostedTipHiddenAsync(page));
        await page.Clock.RunForAsync(Delay + 80);
        var receipt = await HostReceiptAsync(page);
        await CaptureDelayAsync(page, "delay-shadow-immediate-" + access, receipt, session);
        Assert.False(receipt.GetProperty("hidden").GetBoolean());
        Assert.Empty(receipt.GetProperty("expiry").EnumerateArray());
        if (access == "pin") Assert.True(await page.EvaluateAsync<bool>("() => window.hostedChart.dataset.cfxTooltipPinned==='true'"));
        AssertNoConsoleErrors(session);
    }
}
