using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipDelayBrowserTests {
    [Theory]
    [InlineData("nearest")]
    [InlineData("distance")]
    [InlineData("exact")]
    public async Task ExternalOverlayCannotPublishAReadoutAtExpiry(string range) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Observations(false).ToInteractiveHtmlPage(options => ConfigureDelay(options, range)), 950, 760);
        var page = session.Page;
        await TraceAsync(page);
        await page.EvaluateAsync("""
            () => {
                const root=document.querySelector('.cfx-interactive-chart'), tip=root.querySelector('.cfx-tooltip');
                const nativeTimer=window.setTimeout.bind(window), delay=Number(root.dataset.cfxTooltipDelay);
                window.overlayExpiry=[];
                // Place the real host overlay at the timer boundary and record the callback's transient state.
                // A final hidden-state assertion alone would miss a readout shown before pointerleave arrives.
                window.setTimeout=(callback,ms,...args)=>nativeTimer((...values)=>{
                    if(ms!==delay) { callback(...values); return; }
                    const overlay=document.createElement('div');
                    overlay.id='delay-host-overlay'; overlay.textContent='Host dialog';
                    overlay.style.cssText='position:fixed;inset:0;background:#eee;z-index:5;padding:24px;';
                    document.body.append(overlay);
                    const hit=document.elementFromPoint(...window.delayTrace.pointer);
                    callback(...values);
                    window.overlayExpiry.push({hit:hit?.id,owned:!!hit&&root.contains(hit),hidden:tip.hidden,readout:tip.innerText});
                },ms,...args);
            }
            """);
        await PointerAsync(page, Point(0, 0));
        Assert.True(await TipHiddenAsync(page));
        await page.WaitForFunctionAsync("() => window.overlayExpiry.length>0", null, new PageWaitForFunctionOptions { Timeout = 5000 });
        var expiry = await page.EvaluateAsync<JsonElement>("() => window.overlayExpiry");
        var trace = await TraceStateAsync(page);
        await CaptureDelayAsync(page, "delay-overlay-expiry-" + range, new { expiry, trace });
        var observed = Assert.Single(expiry.EnumerateArray());
        Assert.Equal("delay-host-overlay", observed.GetProperty("hit").GetString());
        Assert.False(observed.GetProperty("owned").GetBoolean());
        Assert.True(observed.GetProperty("hidden").GetBoolean());
        Assert.Empty(trace.GetProperty("shown").EnumerateArray());
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData("target-paint")]
    [InlineData("root-hidden")]
    [InlineData("root-removed")]
    [InlineData("target-removed")]
    [InlineData("features")]
    public async Task HostInvalidationCannotReviveAQueuedReadout(string change) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Observations(false).ToInteractiveHtmlPage(options => ConfigureDelay(options, "exact")), 950, 760);
        var page = session.Page;
        await TraceAsync(page);
        await PointerAsync(page, Point(0, 0));
        Assert.True(await TipHiddenAsync(page));
        await page.EvaluateAsync("""
            change => {
                const root=document.querySelector('.cfx-interactive-chart'), target=root.querySelector('[data-cfx-role=point][data-cfx-series="0"][data-cfx-point="0"]');
                window.suspendedRoot=root;
                if(change==='target-paint') target.style.setProperty('opacity','0','important');
                else if(change==='root-hidden') root.hidden=true;
                else if(change==='root-removed') root.remove();
                else if(change==='target-removed') target.remove();
                else root.dataset.cfxInteractionFeatures='None';
            }
            """, change);
        if (change == "target-paint") Assert.Equal("0", await page.Locator(Point(0, 0)).EvaluateAsync<string>("node=>getComputedStyle(node).opacity"));
        await Task.Delay(Delay + 80);
        Assert.True(await page.EvaluateAsync<bool>("() => window.suspendedRoot.querySelector('.cfx-tooltip').hidden"));
        if (change == "root-hidden") {
            await page.EvaluateAsync("() => window.suspendedRoot.hidden=false");
            await Task.Delay(60);
            Assert.True(await TipHiddenAsync(page));
        }
        Assert.Empty((await TraceStateAsync(page)).GetProperty("shown").EnumerateArray());
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task BriefHostDetachCancelsBeforeReattachmentAndFreshAcquisitionCanRecover() {
        if (!Enabled) return;
        await using var session = await OpenAsync(Observations(false).ToInteractiveHtmlPage(options => ConfigureDelay(options, "exact")), 950, 760);
        var page = session.Page;
        await MountFragmentAsync(page, "light");
        await RecordHostExpiryAsync(page);
        await PointerAsync(page, Point(0, 0));
        await page.EvaluateAsync("() => { const root=window.hostedChart, parent=root.parentElement; window.detachedTimer=window.hostTimerTrace.queued.at(-1); root.remove(); parent.append(root); }");
        await Task.Delay(Delay + 80);
        var canceled = await HostReceiptAsync(page);
        await CaptureDelayAsync(page, "delay-light-detach-reattach", canceled, session);
        var timer = canceled.GetProperty("detachedTimer").GetInt32();
        Assert.Contains(timer, canceled.GetProperty("timers").GetProperty("canceled").EnumerateArray().Select(item => item.GetInt32()));
        Assert.DoesNotContain(timer, canceled.GetProperty("timers").GetProperty("fired").EnumerateArray().Select(item => item.GetInt32()));
        // A real new pointerenter after reinsertion owns a fresh request, independently of the canceled timer.
        await MoveAwayAsync(page);
        Assert.True(await TipHiddenAsync(page));
        await PointerAsync(page, Point(0, 0));
        Assert.True(await TipHiddenAsync(page));
        await WaitForTipAsync(page);
        Assert.Contains("Reading", await TooltipTextAsync(page), StringComparison.Ordinal);
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task ResetFromKeyboardCancelsPointerWorkWithoutPointerLeave() {
        if (!Enabled) return;
        await using var session = await OpenAsync(Observations(false).ToInteractiveHtmlPage(options => { ConfigureDelay(options, "exact"); options.Interaction.Enable(ChartInteractionFeatures.Zoom); }), 950, 760);
        var page = session.Page;
        await page.Locator("[data-cfx-zoom=in]").ClickAsync();
        await TraceAsync(page);
        await PointerAsync(page, Point(0, 0));
        Assert.True(await TipHiddenAsync(page));
        await page.Locator("[data-cfx-reset]").FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Task.Delay(Delay + 80);
        Assert.True(await TipHiddenAsync(page));
        Assert.True(await page.Locator("[data-cfx-reset]").IsHiddenAsync());
        Assert.Empty((await TraceStateAsync(page)).GetProperty("shown").EnumerateArray());
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task SynchronizedMuteInvalidatesPendingDataWhileLegendAccessStaysImmediate() {
        if (!Enabled) return;
        var source = Observations(false);
        var peer = Chart.Create().WithSize(800, 540).WithLegend(true).AddScatter("Different label", new[] { new ChartPoint(3, 5), new ChartPoint(7, 6) });
        source.Series[0].WithInteractionKey("readings"); peer.Series[0].WithInteractionKey("readings");
        var html = new[] { source, peer }.ToInteractiveHtmlDashboardPage(options => {
            options.Columns = 1; options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Tooltip.DelayMilliseconds = Delay; options.Tooltip.Range = HtmlChartTooltipRange.Exact;
            options.Interaction.Enable(ChartInteractionFeatures.SynchronizedCharts);
        });
        await using var session = await OpenAsync(html, 950, 1300);
        var page = session.Page;
        await TraceAsync(page);
        await PointerAsync(page, ".cfx-interactive-chart:nth-child(1) " + Point(0, 0));
        var roots = page.Locator(".cfx-interactive-chart");
        await roots.Nth(1).Locator(Legend(0)).FocusAsync();
        Assert.False(await roots.Nth(1).Locator(".cfx-tooltip").EvaluateAsync<bool>("tip=>tip.hidden"));
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await roots.Nth(0).Locator(Legend(0)).GetAttributeAsync("data-cfx-muted"));
        await Task.Delay(Delay + 80);
        Assert.True(await TipHiddenAsync(page));
        Assert.Empty((await TraceStateAsync(page)).GetProperty("shown").EnumerateArray());
        await CaptureDelayAsync(page, "delay-semantic-mute", await TraceStateAsync(page));
        AssertNoConsoleErrors(session);
    }
}
