using System.Text.Json;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipDelayBrowserTests {
    [BrowserTheory]
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
        await ArmQueuedHostMutationAsync(page, "detach-" + subject);
        await MoveToHostTargetAsync(page, Point(0, 0));
        var mutation = await WaitForHostMutationAsync(page);
        // Chromium can emit a fresh native pointerenter after reinsertion; only the original token is canceled.
        await MoveAwayAsync(page);
        await Task.Delay(Delay + 80);
        var canceled = await HostReceiptAsync(page);
        await CaptureDelayAsync(page, "delay-shadow-detach-" + subject, canceled, session);
        AssertQueuedHostCancellation(mutation, canceled);
        Assert.True(await HostedTipHiddenAsync(page));
        await page.EvaluateAsync("() => { window.hostExpiry=[]; }");
        await MoveToHostTargetAsync(page, Point(0, 0));
        Assert.True(await HostedTipHiddenAsync(page));
        await WaitForHostExpiryAsync(page);
        Assert.False(await HostedTipHiddenAsync(page));
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
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
        await ArmQueuedHostMutationAsync(page, change, restore: true);
        await MoveToHostTargetAsync(page, Point(0, 0));
        var mutation = await WaitForHostMutationAsync(page);
        await Task.Delay(Delay + 80);
        var receipt = await HostReceiptAsync(page);
        await CaptureDelayAsync(page, "delay-shadow-mutation-" + change, receipt, session);
        AssertQueuedHostCancellation(mutation, receipt);
        Assert.True(receipt.GetProperty("hidden").GetBoolean());
        Assert.Empty(receipt.GetProperty("expiry").EnumerateArray());
        Assert.Empty(receipt.GetProperty("trace").GetProperty("shown").EnumerateArray());
        AssertNoConsoleErrors(session);
    }
}
