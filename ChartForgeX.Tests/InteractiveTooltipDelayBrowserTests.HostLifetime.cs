using System.Text.Json;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipDelayBrowserTests {
    [Theory]
    [InlineData("root")]
    [InlineData("target")]
    [InlineData("inner-host")]
    [InlineData("outer-host")]
    public async Task BriefDetachAcrossShadowBoundariesCancelsQueuedWorkAndFreshEntryRecovers(string subject) {
        if (!Enabled) return;
        await using var session = await OpenAsync(HostFragment(), 950, 760);
        var page = session.Page;
        await MountFragmentAsync(page, "nested-closed");
        await RecordHostExpiryAsync(page);
        await MoveToHostTargetAsync(page, Point(0, 0));
        await page.EvaluateAsync("""
            subject => {
                const root=window.hostedChart;
                window.detachedTimer=window.hostTimerTrace.queued.at(-1);
                const node=subject==='root'?root:subject==='target'?root.querySelector('[data-cfx-role=point][data-cfx-series="0"][data-cfx-point="0"]'):
                    subject==='inner-host'?root.getRootNode().host:document.getElementById('outer-chart-host');
                const parent=node.parentNode, next=node.nextSibling;
                node.remove(); parent.insertBefore(node,next);
            }
            """, subject);
        await Task.Delay(Delay + 80);
        var canceled = await HostReceiptAsync(page);
        await CaptureDelayAsync(page, "delay-shadow-detach-" + subject, canceled, session);
        var timer = canceled.GetProperty("detachedTimer").GetInt32();
        Assert.Contains(timer, canceled.GetProperty("timers").GetProperty("canceled").EnumerateArray().Select(item => item.GetInt32()));
        Assert.DoesNotContain(timer, canceled.GetProperty("timers").GetProperty("fired").EnumerateArray().Select(item => item.GetInt32()));
        // Chromium can emit a fresh native pointerenter after reinsertion; it may queue a new deadline.
        await MoveAwayAsync(page);
        Assert.True(await HostedTipHiddenAsync(page));
        await page.EvaluateAsync("() => { window.hostExpiry=[]; }");
        await MoveToHostTargetAsync(page, Point(0, 0));
        Assert.True(await HostedTipHiddenAsync(page));
        await WaitForHostExpiryAsync(page);
        Assert.False(await HostedTipHiddenAsync(page));
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData("root-hidden")]
    [InlineData("target-paint")]
    [InlineData("features")]
    [InlineData("shadow-host-opacity")]
    public async Task ShadowMutationCancelsBeforeTheHostRestoresAvailability(string change) {
        if (!Enabled) return;
        await using var session = await OpenAsync(HostFragment(), 950, 760);
        var page = session.Page;
        await MountFragmentAsync(page, "nested-closed");
        await RecordHostExpiryAsync(page);
        await MoveToHostTargetAsync(page, Point(0, 0));
        await page.EvaluateAsync("""
            async change => {
                const root=window.hostedChart, target=root.querySelector('[data-cfx-role=point][data-cfx-series="0"][data-cfx-point="0"]');
                const host=root.getRootNode().host, features=root.dataset.cfxInteractionFeatures;
                if(change==='root-hidden') root.hidden=true;
                else if(change==='target-paint') target.style.setProperty('opacity','0','important');
                else if(change==='shadow-host-opacity') host.style.opacity='0';
                else root.dataset.cfxInteractionFeatures='None';
                const unavailable=change==='root-hidden'?root.hidden:change==='features'?root.dataset.cfxInteractionFeatures==='None':
                    getComputedStyle(change==='target-paint'?target:host).opacity==='0';
                window.hostMutation={change,unavailable};
                if(!unavailable) throw new Error('Host fixture remained available');
                // Let pending mutation observers see the unavailable state before it is restored.
                await Promise.resolve();
                if(change==='root-hidden') root.hidden=false;
                else if(change==='target-paint') target.style.removeProperty('opacity');
                else if(change==='shadow-host-opacity') host.style.removeProperty('opacity');
                else root.dataset.cfxInteractionFeatures=features;
            }
            """, change);
        await Task.Delay(Delay + 80);
        var receipt = await HostReceiptAsync(page);
        await CaptureDelayAsync(page, "delay-shadow-mutation-" + change, receipt, session);
        Assert.True(receipt.GetProperty("hidden").GetBoolean());
        Assert.Empty(receipt.GetProperty("expiry").EnumerateArray());
        Assert.Empty(receipt.GetProperty("trace").GetProperty("shown").EnumerateArray());
        AssertNoConsoleErrors(session);
    }
}
