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
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExactNativeSummaryDelayKeepsGuideEventsAndSemanticPeerImmediate(bool tooltips) {
        if (!Enabled) return;
        var points = new[] { new ChartPoint(3, 5), new ChartPoint(5, 5), new ChartPoint(7, 5) };
        var source = NativeLine("Source", points);
        source.Series[0].WithInteractionKey("readings");
        var peer = NativeLine("Peer", points);
        peer.Series[0].WithInteractionKey("readings");
        var html = new[] { source, peer }.ToInteractiveHtmlDashboardPage(options => {
            options.Columns = 1; options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Tooltip.DelayMilliseconds = Delay; options.Tooltip.Range = HtmlChartTooltipRange.Exact;
            options.Interaction.Features = ChartInteractionFeatures.Crosshair | ChartInteractionFeatures.SynchronizedCharts
                | (tooltips ? ChartInteractionFeatures.Tooltips : ChartInteractionFeatures.None);
        });
        await using var session = await OpenAsync(html, 950, 1300);
        var page = session.Page;
        var roots = page.Locator(".cfx-interactive-chart");
        await TraceAsync(page);
        await roots.Nth(1).EvaluateAsync("root => { window.delayPeerSync=[]; root.addEventListener('cfxsync',e=>window.delayPeerSync.push(e.detail)); }");
        var positions = await roots.Nth(0).Locator("[data-cfx-role=series] [data-cfx-role=line]").EvaluateAsync<double[]>("""
            line => [.1,.43,.9].flatMap(f => {
                const p=line.getPointAtLength(line.getTotalLength()*f), s=new DOMPoint(p.x,p.y).matrixTransform(line.getScreenCTM());
                for(const dy of [0,-.25,.25])
                    if(document.elementFromPoint(s.x,s.y+dy)?.closest('[data-cfx-target-kind]')===line.closest('[data-cfx-role=series]')) return [s.x,s.y+dy];
                throw new Error('Missing actual native line hit');
            })
            """);
        for (var point = 0; point < 3; point++) {
            await page.Mouse.MoveAsync((float)positions[point * 2], (float)positions[point * 2 + 1]);
            // These guide positions share one native series target and one delay deadline.
            // Later movement may occur after that deadline without queuing another readout.
            if (!tooltips) Assert.True(await TipHiddenAsync(page));
            var peerPoints = await roots.Nth(1).Locator("[data-cfx-role=point].cfx-hovered").EvaluateAllAsync<string[]>("nodes=>nodes.map(n=>n.dataset.cfxTargetId)");
            Assert.Equal(new[] { "readings:" + point }, peerPoints);
            Assert.Equal(point + 1, (await TraceStateAsync(page)).GetProperty("guide").GetArrayLength());
            await Task.Delay(75);
        }
        if (tooltips) {
            for (var step = 0; step < 15 && await TipHiddenAsync(page); step++) {
                await Task.Delay(40);
                await page.Mouse.MoveAsync((float)(positions[4] + step % 2 * .1), (float)positions[5]);
            }
            Assert.False(await TipHiddenAsync(page));
            Assert.Contains("Source", await TooltipTextAsync(page), StringComparison.Ordinal);
            Assert.StartsWith("series|readings|", (await roots.Nth(0).GetAttributeAsync("data-cfx-hover-key"))!, StringComparison.Ordinal);
        } else {
            await Task.Delay(Delay + 40);
            Assert.True(await TipHiddenAsync(page));
        }
        var trace = await TraceStateAsync(page);
        Assert.Equal(3, trace.GetProperty("guide").GetArrayLength());
        Assert.Equal(3, trace.GetProperty("sync").EnumerateArray().Count(e => e.GetProperty("action").GetString() == "crosshair"));
        Assert.Empty((await page.EvaluateAsync<JsonElement>("() => window.delayPeerSync")).EnumerateArray());
        if (tooltips) {
            Assert.True(trace.GetProperty("entry")[0].GetProperty("hidden").GetBoolean());
            AssertDelay(trace);
            Assert.Equal(1, trace.GetProperty("shown").GetArrayLength());
        }
        await CaptureDelayAsync(page, "delay-exact-native-guide-tooltips-" + tooltips, trace);
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NativeTextAndLegendMovementKeepOneDelayedSemanticTarget(bool legend) {
        if (!Enabled) return;
        var chart = legend ? Observations(false) : Chart.Create().WithSize(800, 540).WithTitle("Text readout pacing")
            .AddWordCloud("Topics", new[] { new ChartWordCloudItem("Visible", 100) });
        var selector = legend ? Legend(0) : "[data-cfx-role=word-cloud-term]";
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            ConfigureDelay(options, "exact"); options.Interaction.Disable(ChartInteractionFeatures.Crosshair);
        }), 950, 760);
        var page = session.Page;
        await TraceAsync(page);
        var positions = await page.Locator(selector).EvaluateAsync<double[]>("""
            node => {
                const text=node.querySelector('text'), positions=[];
                for(let i=0;i<text.getNumberOfChars();i++) {
                    const b=text.getExtentOfChar(i), p=new DOMPoint(b.x+b.width/2,b.y+b.height/2).matrixTransform(text.getScreenCTM());
                    if(document.elementFromPoint(p.x,p.y)?.closest('[data-cfx-target-kind]')===node) positions.push(p.x,p.y);
                }
                if(positions.length<4) throw new Error('No distinct native text positions');
                return [positions[0],positions[1],positions[positions.length-2],positions[positions.length-1]];
            }
            """);
        await page.Mouse.MoveAsync((float)positions[0], (float)positions[1]);
        Assert.True(await TipHiddenAsync(page));
        await Task.Delay(100);
        await page.Mouse.MoveAsync((float)positions[2], (float)positions[3]);
        await WaitForTipAsync(page);
        Assert.Contains(legend ? "Reading" : "Visible", await TooltipTextAsync(page), StringComparison.Ordinal);
        var trace = await TraceStateAsync(page);
        AssertDelay(trace);
        Assert.Equal(1, trace.GetProperty("shown").GetArrayLength());
        await CaptureDelayAsync(page, "delay-native-" + (legend ? "legend" : "text"), trace);
        AssertNoConsoleErrors(session);
    }

    private static Chart NativeLine(string name, ChartPoint[] points) => Chart.Create().WithSize(800, 540)
        .WithLineMarkers(ChartLineMarkerMode.None).WithXAxisBounds(0, 10).WithYAxisBounds(0, 10).AddLine(name, points);
}
