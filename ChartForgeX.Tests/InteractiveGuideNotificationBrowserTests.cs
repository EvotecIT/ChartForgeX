using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractiveGuideNotificationBrowserTests {
    [Theory]
    [InlineData(true, 950)]
    [InlineData(false, 340)]
    public async Task NativeGuidesNotifyChangesDeduplicateStableMovesAndRecoverWithSemanticPeers(bool tooltips, int width) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Dashboard(tooltips, width == 340), width, 1300);
        var page = session.Page;
        var source = page.Locator(".cfx-interactive-chart").Nth(0);
        var positions = await NativePositionsAsync(source, 0);
        var summary = await source.Locator("[data-cfx-role=series][data-cfx-series='0']").GetAttributeAsync("data-cfx-target-id");
        await RecordEventsAsync(page);
        var trace = new List<JsonElement>();
        await page.Mouse.MoveAsync(2, 2);
        for (var index = 0; index < 3; index++) {
            await page.Mouse.MoveAsync((float)positions[index * 2], (float)positions[index * 2 + 1]);
            var state = await AssertGuideAsync(page, "reading-source:" + index, "series", index + 1);
            Assert.Equal(tooltips ? "series" : "point", state.GetProperty("sourceHoverKind").GetString());
            Assert.Equal(tooltips ? summary : "reading-source:" + index, state.GetProperty("sourceHoverId").GetString());
            Assert.Equal(tooltips ? 1 : index + 2, state.GetProperty("hoverCount").GetInt32());
            Assert.Equal(tooltips, state.GetProperty("tipVisible").GetBoolean());
            if (tooltips) Assert.Equal(new[] { "Series" }, await source.Locator(".cfx-tooltip dt").AllTextContentsAsync());
            trace.Add(state);
            await page.Mouse.MoveAsync((float)positions[index * 2] + 1, (float)positions[index * 2 + 1]);
            var stable = await AssertGuideAsync(page, "reading-source:" + index, "series", index + 1);
            Assert.Equal(state.GetProperty("hoverCount").GetInt32(), stable.GetProperty("hoverCount").GetInt32());
            trace.Add(stable);
        }
        var background = await source.Locator(Point(0, 2)).EvaluateAsync<double[]>("n => { const b=n.getBoundingClientRect(); return [b.x+b.width/2,b.y+b.height/2+35]; }");
        await AssertEmptySourcePositionAsync(source, background);
        await page.Mouse.MoveAsync((float)background[0], (float)background[1]);
        trace.Add(await AssertGuideAsync(page, "reading-source:2", "shared", 4));
        Assert.True(await source.Locator(".cfx-tooltip").IsHiddenAsync());
        await page.Mouse.MoveAsync((float)background[0] + 1, (float)background[1]);
        trace.Add(await AssertGuideAsync(page, "reading-source:2", "shared", 4));
        await page.Mouse.MoveAsync((float)positions[4], (float)positions[5]);
        trace.Add(await AssertGuideAsync(page, "reading-source:2", "series", 5));
        Assert.Equal(tooltips ? summary : "reading-source:2", trace[^1].GetProperty("sourceHoverId").GetString());
        Assert.Equal(tooltips, trace[^1].GetProperty("tipVisible").GetBoolean());
        await page.Mouse.MoveAsync(2, 2);
        var hidden = await ReadStateAsync(page);
        Assert.False(hidden.GetProperty("guideVisible").GetBoolean());
        Assert.Null(hidden.GetProperty("sourceGuideId").GetString());
        Assert.Empty(hidden.GetProperty("peerHoveredPoints").EnumerateArray());
        Assert.Equal(5, hidden.GetProperty("events").GetArrayLength());
        trace.Add(hidden);
        await page.Mouse.MoveAsync((float)positions[0], (float)positions[1]);
        trace.Add(await AssertGuideAsync(page, "reading-source:0", "series", 6));
        await CaptureAsync(page, "guide-notifications-" + (tooltips ? "exact-wide-light" : "guide-only-compact-dark"), trace);
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task AGuideFromAnotherSeriesKeepsItsSharedModeIndependentFromTheNativeSummary() {
        if (!Enabled) return;
        var source = Frame(false, "Native line and nearby observation")
            .AddLine("Line reading", new[] { new ChartPoint(3, 5), new ChartPoint(7, 5) })
            .AddScatter("Nearby reading", new[] { new ChartPoint(5, 5.15) });
        source.Series[0].WithInteractionKey("line-source"); source.Series[1].WithInteractionKey("nearby-source");
        var peer = Frame(false, "Reordered native peer")
            .AddScatter("Peer nearby reading", new[] { new ChartPoint(5, 5.15) })
            .AddLine("Peer line reading", new[] { new ChartPoint(3, 5), new ChartPoint(7, 5) });
        peer.Series[0].WithInteractionKey("nearby-source"); peer.Series[1].WithInteractionKey("line-source");
        await using var session = await OpenAsync(RenderDashboard(source, peer, true), 950, 1300);
        var page = session.Page;
        var root = page.Locator(".cfx-interactive-chart").Nth(0);
        var native = await root.Locator("[data-cfx-role=line]").EvaluateAsync<double[]>("n => { const p=n.getPointAtLength(n.getTotalLength()/2), q=new DOMPoint(p.x,p.y).matrixTransform(n.getScreenCTM()); return [q.x,q.y]; }");
        Assert.True(await root.EvaluateAsync<bool>("(r,p) => document.elementFromPoint(p[0],p[1])?.closest('[data-cfx-target-kind]')===r.querySelector('[data-cfx-role=series]')", native));
        await RecordEventsAsync(page);
        await page.Mouse.MoveAsync(2, 2); await page.Mouse.MoveAsync((float)native[0], (float)native[1]);
        var trace = new List<JsonElement> { await AssertGuideAsync(page, "nearby-source:0", "shared", 1) };
        Assert.Equal("series", trace[0].GetProperty("sourceHoverKind").GetString());
        Assert.Equal("series", trace[0].GetProperty("sourceHoverMode").GetString());
        Assert.Equal(1, trace[0].GetProperty("hoverCount").GetInt32());
        Assert.Equal(new[] { "Series" }, await root.Locator(".cfx-tooltip dt").AllTextContentsAsync());
        Assert.Contains("Line reading", await root.Locator(".cfx-tooltip").InnerTextAsync(), StringComparison.Ordinal);
        await page.Mouse.MoveAsync((float)native[0] + 1, (float)native[1]);
        trace.Add(await AssertGuideAsync(page, "nearby-source:0", "shared", 1));
        Assert.Equal(1, trace[^1].GetProperty("hoverCount").GetInt32());
        var background = new[] { native[0], native[1] + 35 };
        await AssertEmptySourcePositionAsync(root, background);
        await page.Mouse.MoveAsync((float)background[0], (float)background[1]);
        trace.Add(await AssertGuideAsync(page, "nearby-source:0", "shared", 2));
        Assert.Equal("nearby-source:0", trace[^1].GetProperty("sourceHoverId").GetString());
        Assert.Equal("shared", trace[^1].GetProperty("sourceHoverMode").GetString());
        Assert.Equal(2, trace[^1].GetProperty("hoverCount").GetInt32());
        Assert.False(trace[^1].GetProperty("tipVisible").GetBoolean());
        await page.Mouse.MoveAsync((float)background[0] + 1, (float)background[1]);
        trace.Add(await AssertGuideAsync(page, "nearby-source:0", "shared", 2));
        Assert.Equal(2, trace[^1].GetProperty("hoverCount").GetInt32());
        await page.Mouse.MoveAsync((float)native[0], (float)native[1]);
        trace.Add(await AssertGuideAsync(page, "nearby-source:0", "shared", 3));
        Assert.Equal("line-source", trace[^1].GetProperty("sourceHoverId").GetString());
        Assert.Equal("series", trace[^1].GetProperty("sourceHoverMode").GetString());
        Assert.Equal(3, trace[^1].GetProperty("hoverCount").GetInt32());
        Assert.True(trace[^1].GetProperty("tipVisible").GetBoolean());
        await CaptureAsync(page, "guide-notifications-different-series-shared", trace);
        AssertNoConsoleErrors(session);
    }

    private static Chart Frame(bool dark, string title) => Chart.Create().WithSize(800, 540).WithTitle(title)
        .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight()).WithLineMarkers(ChartLineMarkerMode.None)
        .WithXAxisBounds(0, 10).WithYAxisBounds(0, 10);

    private static string Dashboard(bool tooltips, bool dark) {
        var points = new[] { new ChartPoint(3, 5), new ChartPoint(5, 5), new ChartPoint(7, 5) };
        var source = Frame(dark, "Independent native guide").AddLine("Reading", points);
        source.Series[0].WithInteractionKey("reading-source");
        var peer = Frame(dark, "Reordered peer").AddLine("Other", new[] { new ChartPoint(3, 2), new ChartPoint(7, 2) })
            .AddLine("Peer reading", points);
        peer.Series[0].WithInteractionKey("other-source"); peer.Series[1].WithInteractionKey("reading-source");
        return RenderDashboard(source, peer, tooltips);
    }

    private static string RenderDashboard(Chart source, Chart peer, bool tooltips) => new[] { source, peer }.ToInteractiveHtmlDashboardPage(options => {
        options.Columns = 1;
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
        options.Interaction.Features = ChartInteractionFeatures.Crosshair | ChartInteractionFeatures.SynchronizedCharts
            | (tooltips ? ChartInteractionFeatures.Tooltips : ChartInteractionFeatures.None);
        options.Tooltip.Range = HtmlChartTooltipRange.Exact;
    });

    private static async Task<double[]> NativePositionsAsync(ILocator root, int series) => await root.Locator("[data-cfx-role=series][data-cfx-series='" + series + "'] [data-cfx-role=line]")
        .EvaluateAsync<double[]>("""
            line => [[.1,.13,.17],[.33,.37,.43],[.77,.83,.87]].flatMap(fractions => {
                // Subpixel Fit strokes need a real native hit, rather than an assumed mathematical centre.
                for(const f of fractions) {
                    const a=line.getPointAtLength(line.getTotalLength()*f), p=new DOMPoint(a.x,a.y).matrixTransform(line.getScreenCTM());
                    for(const dy of [0,-.25,.25])
                        if(document.elementFromPoint(p.x,p.y+dy)?.closest('[data-cfx-target-kind]')===line.closest('[data-cfx-role=series]')) return [p.x,p.y+dy];
                }
                throw new Error('No actual native summary surface at this scale');
            })
            """);

    private static async Task AssertEmptySourcePositionAsync(ILocator root, double[] position) =>
        Assert.True(await root.EvaluateAsync<bool>("(r,p) => { const h=document.elementFromPoint(p[0],p[1]); return !!h && r.contains(h) && !h.closest('[data-cfx-target-kind]'); }", position));

    private static async Task RecordEventsAsync(IPage page) => await page.EvaluateAsync("""
        () => {
            const [root,peer]=document.querySelectorAll('.cfx-interactive-chart');
            window.guideNotifications={events:[],sync:[],hoverCount:0,clearCount:0,peerEcho:0};
            root.addEventListener('cfxcrosshair',e=>window.guideNotifications.events.push({id:e.detail.target.targetId,kind:e.detail.target.targetKind,x:e.detail.x,y:e.detail.y}));
            root.addEventListener('cfxsync',e=>{if(e.detail.action==='crosshair')window.guideNotifications.sync.push({id:e.detail.target.targetId,kind:e.detail.target.targetKind,mode:e.detail.mode});});
            root.addEventListener('cfxhover',()=>window.guideNotifications.hoverCount++);
            root.addEventListener('cfxhoverclear',()=>window.guideNotifications.clearCount++);
            peer.addEventListener('cfxsync',()=>window.guideNotifications.peerEcho++);
        }
        """);

    private static async Task<JsonElement> ReadStateAsync(IPage page) => await page.EvaluateAsync<JsonElement>("""
        () => {
            const [root,peer]=document.querySelectorAll('.cfx-interactive-chart'),hover=(root.dataset.cfxHoverKey||'').split('|');
            return {...window.guideNotifications,sourceGuideId:(root.dataset.cfxCrosshair||'').split('|')[1]||null,
                guideVisible:!root.querySelector('.cfx-crosshair').hidden,sourceHoverKind:hover[0]||null,sourceHoverId:hover[1]||null,
                sourceHoverMode:root.dataset.cfxHoverMode||null,tipVisible:!root.querySelector('.cfx-tooltip').hidden,
                peerMode:peer.dataset.cfxHoverMode||null,peerHoveredPoints:Array.from(peer.querySelectorAll('[data-cfx-point].cfx-hovered')).map(n=>n.dataset.cfxTargetId)};
        }
        """);

    private static async Task<JsonElement> AssertGuideAsync(IPage page, string id, string mode, int count) {
        var state = await ReadStateAsync(page);
        Assert.True(state.GetProperty("guideVisible").GetBoolean());
        Assert.Equal(id, state.GetProperty("sourceGuideId").GetString());
        var events = state.GetProperty("events"); var sync = state.GetProperty("sync");
        Assert.Equal(count, events.GetArrayLength()); Assert.Equal(count, sync.GetArrayLength());
        Assert.Equal("point", events[count - 1].GetProperty("kind").GetString());
        Assert.Equal(id, events[count - 1].GetProperty("id").GetString());
        Assert.Equal("point", sync[count - 1].GetProperty("kind").GetString());
        Assert.Equal(id, sync[count - 1].GetProperty("id").GetString());
        Assert.Equal(mode, sync[count - 1].GetProperty("mode").GetString());
        Assert.Equal(mode, state.GetProperty("peerMode").GetString());
        Assert.Equal(new[] { id }, state.GetProperty("peerHoveredPoints").EnumerateArray().Select(e=>e.GetString()));
        Assert.Equal(0, state.GetProperty("peerEcho").GetInt32());
        return state;
    }

    private static async Task CaptureAsync(IPage page, string name, IReadOnlyList<JsonElement> trace) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".json"), JsonSerializer.Serialize(trace));
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png"), FullPage = true });
    }
}
