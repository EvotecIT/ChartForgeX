using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipDelayBrowserTests {
    [Theory]
    [InlineData("light", "attribute")]
    [InlineData("light", "child-list")]
    [InlineData("nested-closed", "attribute")]
    [InlineData("nested-closed", "child-list")]
    public async Task UnrelatedHostUpdatesDoNotRescanPendingChartPaint(string host, string mutation) {
        if (!Enabled) return;
        await using var session = await OpenAsync(HostFragment(delay: 5000), 950, 760);
        var page = session.Page;
        await MountFragmentAsync(page, host);
        await RecordHostExpiryAsync(page);
        await page.EvaluateAsync("() => { const node=document.createElement('div'); node.id='unrelated-dashboard'; document.body.append(node); }");
        await MoveToHostTargetAsync(page, Point(0, 0));
        Assert.True(await HostedTipHiddenAsync(page));
        var scans = await page.EvaluateAsync<int>("""
            async mutation => {
                await Promise.resolve();
                const nativeStyle=window.getComputedStyle, root=window.hostedChart;
                let scans=0;
                window.getComputedStyle=(node,...args) => { if(root.contains(node)) scans++; return nativeStyle.call(window,node,...args); };
                try {
                    const unrelated=document.getElementById('unrelated-dashboard');
                    for(let frame=0;frame<20;frame++) {
                        if(mutation==='attribute') { unrelated.dataset.frame=String(frame); unrelated.style.opacity=frame%2 ? '.9' : '1'; }
                        else { const status=document.createElement('span'); document.body.append(status); status.remove(); }
                        await Promise.resolve();
                    }
                    return scans;
                } finally { window.getComputedStyle=nativeStyle; }
            }
            """, mutation);
        var pending = await HostReceiptAsync(page);
        // An ancestor's availability remains part of the lifetime boundary, including closed shadow hosts.
        await page.EvaluateAsync("() => { (window.outerChartTree?.host || window.hostedChart.parentElement).style.opacity='0'; }");
        var canceled = await HostReceiptAsync(page);
        await CaptureDelayAsync(page, "delay-observer-scope-" + host + "-" + mutation, new { PaintScans = scans, Pending = pending, Canceled = canceled }, session);
        Assert.Equal(0, scans);
        var queued = pending.GetProperty("timers").GetProperty("queued").EnumerateArray().Select(timer => timer.GetInt32()).ToArray();
        var alreadyCanceled = pending.GetProperty("timers").GetProperty("canceled").EnumerateArray().Select(timer => timer.GetInt32());
        var nowCanceled = canceled.GetProperty("timers").GetProperty("canceled").EnumerateArray().Select(timer => timer.GetInt32());
        Assert.Single(queued.Except(alreadyCanceled));
        Assert.Empty(queued.Except(nowCanceled));
        Assert.Empty(canceled.GetProperty("timers").GetProperty("fired").EnumerateArray());
        Assert.True(canceled.GetProperty("hidden").GetBoolean());
        AssertNoConsoleErrors(session);
    }
}
