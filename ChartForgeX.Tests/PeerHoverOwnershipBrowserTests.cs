using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using HtmlTinkerX;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Linked hover retirement belongs to the sender whose exact target the peer accepted.</summary>
[Trait("BrowserSuite", "Compatibility")]
public sealed class PeerHoverOwnershipBrowserTests {
    private const string Roots = ".cfx-interactive-chart";
    private const string Shared = "shared-readings";
    private static string Observation(string id) => "[data-cfx-role=point][data-cfx-observation-id='" + id + "']";

    [BrowserFact]
    public async Task MissingSharedObservationPreservesPeersOwnKeyboardAffordancesThroughPointerAndClear() {
        if (!Enabled) return;
        var charts = new[] {
            Model(Readings(Shared, ("source-only", 8, 5))),
            Model(Readings(Shared, ("peer-only", 2, 5)))
        };
        await using var session = await OpenAsync(Dashboard(charts, 2), 1080, 600);
        var page = session.Page;
        await Bind(page);
        var receiver = page.Locator(Roots).Nth(1);
        await FocusWithKeyboardAsync(page, receiver.Locator(Observation("peer-only")));
        var before = await NativeState(receiver);
        Assert.Equal("peer-only", before.GetProperty("focusedObservation").GetString());
        Assert.Single(before.GetProperty("hovered").EnumerateArray());
        Assert.Single(before.GetProperty("reveals").EnumerateArray());
        Assert.NotEmpty(before.GetProperty("trail").EnumerateArray());

        var positions = await NativeBackgroundPoints(page.Locator(Roots).Nth(0), "source-only");
        await Move(page, positions[0]);
        await WaitForSourceHover(page, "source-only");
        var afterHover = await NativeState(receiver);
        await page.Mouse.MoveAsync(2, 2);
        await page.WaitForFunctionAsync("()=>!peerRoots[0].querySelector('.cfx-hovered')");
        var afterClear = await NativeState(receiver);
        await Capture(session, "missing-shared-observation", new { before, afterHover, afterClear });

        Assert.Equal(before.GetRawText(), afterHover.GetRawText());
        Assert.Equal(before.GetRawText(), afterClear.GetRawText());
        var actions = await page.EvaluateAsync<string[]>("()=>peerSync[0].map(event=>event.action)");
        Assert.Contains("hover", actions);
        Assert.Contains("crosshair", actions);
        Assert.Contains("hover-clear", actions);
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task ContinuousPlotTransitionsRetireOnlyTheSendersAcceptedPeerHover() {
        if (!Enabled) return;
        var charts = new[] {
            Model(Readings(Shared, ("common", 2, 3), ("source-only", 8, 3)),
                Readings("unrepresented-series", ("unrepresented", 8, 8))),
            Model(Readings(Shared, ("common", 2, 3))),
            Model(Readings(Shared, ("common", 2, 3)))
        };
        await using var session = await OpenAsync(Dashboard(charts, 3), 1380, 620);
        var page = session.Page;
        await Bind(page);
        var roots = page.Locator(Roots);
        var positions = await NativeBackgroundPoints(roots.Nth(0), "common", "unrepresented", "source-only");
        await Move(page, positions[0]);
        await WaitForSourceHover(page, "common");
        Assert.Equal(1, await roots.Nth(1).Locator(Observation("common") + ".cfx-hovered").CountAsync());
        var accepted = await NativeState(roots.Nth(1));
        Assert.Single(accepted.GetProperty("reveals").EnumerateArray());
        var actionBoundary = await page.EvaluateAsync<int>("()=>peerSync[0].length");

        // Both coordinates hit the same native frame-card, so no native mark leave can mask the transition.
        await Move(page, positions[1]);
        await WaitForSourceHover(page, "unrepresented");
        var retired = await NativeState(roots.Nth(1));
        var transitionActions = await page.EvaluateAsync<string[]>("start=>peerSync[0].slice(start).map(event=>event.action)", actionBoundary);
        await Capture(session, "same-sender-unrepresented-transition", new { accepted, retired, transitionActions });
        Assert.Contains("hover", transitionActions);
        Assert.Contains("crosshair", transitionActions);
        Assert.DoesNotContain("hover-clear", transitionActions);
        Assert.Empty(retired.GetProperty("hovered").EnumerateArray());
        Assert.Empty(retired.GetProperty("emphasis").EnumerateArray());
        Assert.Empty(retired.GetProperty("reveals").EnumerateArray());

        // A second sender takes ownership through real keyboard focus while the pointer stays on source A's plot.
        await Move(page, positions[0]);
        await WaitForSourceHover(page, "common");
        await FocusWithKeyboardAsync(page, roots.Nth(2).Locator(Observation("common")));
        var otherSender = await NativeState(roots.Nth(1));
        var localSender = await NativeState(roots.Nth(2));
        Assert.Single(otherSender.GetProperty("hovered").EnumerateArray());
        Assert.Single(otherSender.GetProperty("reveals").EnumerateArray());
        Assert.Equal("common", localSender.GetProperty("focusedObservation").GetString());

        await Move(page, positions[2]);
        await WaitForSourceHover(page, "source-only");
        var afterAbsentItem = await NativeState(roots.Nth(1));
        var localAfterAbsentItem = await NativeState(roots.Nth(2));
        await page.Mouse.MoveAsync(2, 2);
        await page.WaitForFunctionAsync("()=>!peerRoots[0].querySelector('.cfx-hovered')");
        var afterForeignClear = await NativeState(roots.Nth(1));
        var localAfterForeignClear = await NativeState(roots.Nth(2));
        await Capture(session, "other-sender-preservation", new {
            otherSender, afterAbsentItem, afterForeignClear, localSender, localAfterAbsentItem, localAfterForeignClear
        });
        Assert.Equal(otherSender.GetRawText(), afterAbsentItem.GetRawText());
        Assert.Equal(otherSender.GetRawText(), afterForeignClear.GetRawText());
        Assert.Equal(localSender.GetRawText(), localAfterAbsentItem.GetRawText());
        Assert.Equal(localSender.GetRawText(), localAfterForeignClear.GetRawText());
        AssertNoConsoleErrors(session);
    }

    private static Chart Model(params ChartSeries[] series) {
        var chart = Chart.Create().WithSize(420, 300).WithHeader(false).WithLegend(false)
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 10);
        foreach (var item in series) chart.AddSeries(item);
        return chart;
    }

    private static ChartSeries Readings(string key, params (string Id, double X, double Y)[] observations) {
        var series = ChartSeries.CreateScatter(new ChartSeriesId(key), key,
            observations.Select(value => new ChartPoint(value.X, value.Y)));
        foreach (var (value, index) in observations.Select((value, index) => (value, index)))
            series.WithObservationId(index, new ChartObservationId(value.Id));
        return series;
    }

    private static string Dashboard(Chart[] charts, int columns) => charts.ToInteractiveHtmlDashboardPage(options => {
        options.Columns = columns;
        options.Interaction.GroupName = "peer-hover-ownership";
        options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.KeyboardNavigation
            | ChartInteractionFeatures.SynchronizedCharts | ChartInteractionFeatures.RevealLabels
            | ChartInteractionFeatures.FocusTrail | ChartInteractionFeatures.Crosshair;
        options.Tooltip.Range = HtmlChartTooltipRange.Nearest;
        options.Tooltip.Mode = HtmlChartTooltipMode.Single;
        options.Tooltip.PinOnSelect = false;
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
    });

    private static Task Bind(IPage page) => page.EvaluateAsync("""
        ()=>{window.peerRoots=[...document.querySelectorAll('.cfx-interactive-chart')];window.peerSync=peerRoots.map(()=>[]);
            window.peerPointerHits=[];
            peerRoots.forEach((root,index)=>root.addEventListener('cfxsync',event=>peerSync[index].push(event.detail)));
            peerRoots[0].querySelector('.cfx-stage').addEventListener('pointermove',event=>peerPointerHits.push({
                x:event.clientX,y:event.clientY,trusted:event.isTrusted,role:event.target.dataset.cfxRole,tag:event.target.tagName}),true);}
        """);

    private static Task<double[][]> NativeBackgroundPoints(ILocator root, params string[] observationIds) => root.EvaluateAsync<double[][]>("""
        (root,ids)=>{const surface=root.querySelector('.cfx-stage svg [data-cfx-role=frame-card]');
            if(!surface)throw Error('The typed chart must expose its native plot background.');
            return ids.map(id=>{const node=[...root.querySelectorAll('[data-cfx-role=point]')].find(node=>node.dataset.cfxObservationId===id);
                if(!node)throw Error('Missing native observation '+id);const box=node.getBoundingClientRect();
                for(const dy of [21,-21,29,-29])for(const dx of [13,-13,19,-19]){
                    const x=Math.round(box.left+box.width/2+dx),y=Math.round(box.top+box.height/2+dy);
                    if(document.elementFromPoint(x,y)===surface)return[x,y];}
                throw Error('No real background hit near '+id);});}
        """, observationIds);

    private static Task Move(IPage page, double[] point) => page.Mouse.MoveAsync((float)point[0], (float)point[1]);

    private static Task WaitForSourceHover(IPage page, string id) => page.WaitForFunctionAsync("""
        id=>peerRoots[0].querySelector('.cfx-hovered[data-cfx-observation-id="'+id+'"]')
        """, id);

    private static Task<JsonElement> NativeState(ILocator root) => root.EvaluateAsync<JsonElement>("""
        root=>{const identity=node=>[node.dataset.cfxSeriesKey,node.dataset.cfxObservationId,node.dataset.cfxTargetKind,node.dataset.cfxTargetId];
            return {focusedObservation:root.contains(root.getRootNode().activeElement)?root.getRootNode().activeElement.dataset.cfxObservationId:null,
                hovered:[...root.querySelectorAll('.cfx-hovered')].map(identity),
                emphasis:[...root.querySelectorAll('.cfx-hover-related,.cfx-hover-series,.cfx-hover-column')].map(identity),
                trail:[...root.querySelectorAll('.cfx-trail')].map(node=>[...identity(node),node.dataset.cfxTrailIndex]),
                reveals:[...root.querySelectorAll('.cfx-reveal-label')].map(node=>node.textContent)};}
        """);

    private static async Task Capture(HtmlBrowserSession session, string name, object states) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Browser captures require an absolute output path.");
        Directory.CreateDirectory(directory);
        var input = await session.Page.EvaluateAsync<JsonElement>("()=>({sync:peerSync,pointerHits:peerPointerHits})");
        await File.WriteAllTextAsync(Path.Combine(directory, "peer-hover-" + name + ".json"), JsonSerializer.Serialize(new { states, input }));
        await session.Page.ScreenshotAsync(new() { Path = Path.Combine(directory, "peer-hover-" + name + ".png"), FullPage = true });
    }
}
