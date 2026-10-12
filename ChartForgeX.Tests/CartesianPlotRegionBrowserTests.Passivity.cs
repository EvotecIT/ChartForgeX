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

public sealed partial class CartesianPlotRegionBrowserTests {
    private const string RegionRoot = ".cfx-interactive-chart";
    private const string Guidance = "Background guidance";

    [Fact]
    public async Task BrushLassoAndCompareContainObservationsWithoutBackgroundRegions() {
        if (!Enabled) return;
        var html = PassiveChart().ToInteractiveHtmlPage(options => {
            options.Interaction.Features = ChartInteractionFeatures.ReportReview | ChartInteractionFeatures.Brush;
        });
        await using var session = await OpenAsync(html, 720, 560);
        var page = session.Page;
        await page.Locator(RegionRoot).EvaluateAsync("""
            root => {
                window.regionEvents=[];
                for(const name of ['cfxlasso','cfxcompare']) root.addEventListener(name,event=>window.regionEvents.push({name,detail:event.detail}));
            }
            """);
        await page.Locator("[data-cfx-mode-button=brush]").ClickAsync();
        var box = await BoxAsync(page, "[data-cfx-role=plot-region-fill]");
        await page.Mouse.MoveAsync((float)(box.X + 3), (float)(box.Y + 3));
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync((float)(box.X + box.Width - 3), (float)(box.Y + box.Height - 3), new MouseMoveOptions { Steps = 8 });
        await page.Mouse.UpAsync();
        var facts = await page.Locator(RegionRoot).EvaluateAsync<JsonElement>("""
            root => ({events:window.regionEvents,selected:Array.from(root.querySelectorAll('.cfx-selected')).map(n=>({role:n.dataset.cfxRole,id:n.dataset.cfxTargetId})),
                passiveSelected:root.querySelectorAll('[data-cfx-passive] .cfx-selected,[data-cfx-passive].cfx-selected').length,
                compareCount:Number(root.dataset.cfxCompareCount)})
            """);
        await CapturePassiveAsync(session, html, "passive-brush", facts);
        Assert.Equal(0, facts.GetProperty("passiveSelected").GetInt32());
        Assert.Equal(3, facts.GetProperty("compareCount").GetInt32());
        Assert.Equal(3, facts.GetProperty("selected").GetArrayLength());
        foreach (var notification in facts.GetProperty("events").EnumerateArray()) {
            Assert.Equal(3, notification.GetProperty("detail").GetProperty("targets").GetArrayLength());
            Assert.Equal(2, notification.GetProperty("detail").GetProperty("targets").EnumerateArray().Count(target => target.GetProperty("targetKind").GetString() == "point"));
            Assert.Single(notification.GetProperty("detail").GetProperty("targets").EnumerateArray(), target => target.GetProperty("targetKind").GetString() == "series");
        }
        Assert.Contains(facts.GetProperty("events").EnumerateArray(), e => e.GetProperty("name").GetString() == "cfxlasso");
        Assert.Contains(facts.GetProperty("events").EnumerateArray(), e => e.GetProperty("name").GetString() == "cfxcompare");
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task HostStateAndSynchronizedHoverKeepPassiveFactsOutOfSelectionAndEmphasis() {
        if (!Enabled) return;
        var html = new[] { PassiveChart().AddHorizontalLine(9, Guidance), PassiveChart().AddHorizontalLine(9, Guidance) }
            .ToInteractiveHtmlDashboardPage(options => {
                options.Columns = 1;
                options.Interaction.Features = ChartInteractionFeatures.ReportReview | ChartInteractionFeatures.StateBookmarks | ChartInteractionFeatures.SynchronizedCharts;
                options.Interaction.GroupName = "passive-guidance";
            });
        await using var session = await OpenAsync(html, 720, 1050);
        var page = session.Page;
        var roots = page.Locator(RegionRoot);
        var facts = await roots.First.EvaluateAsync<JsonElement>("""
            root => {
                const passive=Array.from(root.querySelectorAll('[data-cfx-passive]'));
                const point=root.querySelector('[data-cfx-role=point]'), annotation=root.querySelector('[data-cfx-role=annotation]');
                const real=[point,annotation].map(n=>({targetKind:n.dataset.cfxTargetKind,targetId:n.dataset.cfxTargetId}));
                const unwanted=[...passive.map(n=>({id:n.id})),{role:'plot-region',label:passive[0].dataset.cfxLabel}];
                root.dispatchEvent(new CustomEvent('cfx-apply-state',{detail:{snapshot:{selectedTargets:[...real,...unwanted]}}}));
                root.dispatchEvent(new CustomEvent('cfx-capture-state',{detail:{sync:false}}));
                return {snapshot:JSON.parse(root.dataset.cfxInteractionSnapshot),roots:Array.from(document.querySelectorAll('.cfx-interactive-chart')).map(r=>({
                    selected:Array.from(r.querySelectorAll('.cfx-selected')).map(n=>({role:n.dataset.cfxRole,id:n.dataset.cfxTargetId})),
                    passiveSelected:r.querySelectorAll('[data-cfx-passive] .cfx-selected,[data-cfx-passive].cfx-selected').length,
                    count:Number(r.dataset.cfxCompareCount)}))};
            }
            """);
        await MoveToAsync(page, RegionRoot + " [data-cfx-role=annotation-label]");
        var hover = await page.EvaluateAsync<JsonElement>("""
            () => Array.from(document.querySelectorAll('.cfx-interactive-chart')).map(root=>({
                annotationHovered:root.querySelector('[data-cfx-role=annotation]').classList.contains('cfx-hovered'),
                passiveEmphasis:Array.from(root.querySelectorAll('[data-cfx-passive], [data-cfx-passive] *')).filter(n=>
                    ['cfx-selected','cfx-hovered','cfx-hover-related','cfx-hover-series','cfx-hover-column','cfx-revealed','cfx-trail'].some(c=>n.classList.contains(c))).length}))
            """);
        await CapturePassiveAsync(session, html, "passive-state-sync", new { State = facts, Hover = hover });
        Assert.Equal(2, facts.GetProperty("snapshot").GetProperty("selectedTargets").GetArrayLength());
        foreach (var root in facts.GetProperty("roots").EnumerateArray()) {
            Assert.Equal(0, root.GetProperty("passiveSelected").GetInt32());
            Assert.Equal(2, root.GetProperty("count").GetInt32());
            Assert.Equal(new[] { "point", "annotation" }, root.GetProperty("selected").EnumerateArray().Select(n => n.GetProperty("role").GetString()));
        }
        foreach (var root in hover.EnumerateArray()) {
            Assert.True(root.GetProperty("annotationHovered").GetBoolean());
            Assert.Equal(0, root.GetProperty("passiveEmphasis").GetInt32());
        }
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PassiveFillAndCaptionPaintAllowNearbyObservationAndCrosshair(bool nearest) {
        if (!Enabled) return;
        var html = PassiveChart().ToInteractiveHtmlPage(options => {
            options.Tooltip.Range = nearest ? HtmlChartTooltipRange.Nearest : HtmlChartTooltipRange.WithinDistance(120);
        });
        await using var session = await OpenAsync(html, 720, 560);
        var page = session.Page;
        await MoveToAsync(page, Point(0, 0));
        Assert.Contains("Observed", await TooltipTextAsync(page));
        await MoveAwayAsync(page);
        var positions = await page.Locator(RegionRoot).EvaluateAsync<JsonElement>("""
            root => {
                const point=root.querySelector('[data-cfx-role=point]'), p=point.getBoundingClientRect(), px=p.x+p.width/2, py=p.y+p.height/2;
                const roles=['plot-region-fill','plot-region-label','plot-region-label-backplate'];
                return roles.map(role=>{
                    const n=root.querySelector('[data-cfx-role="'+role+'"]'), b=n.getBoundingClientRect();
                    const samples=role==='plot-region-fill' ? [[px-45,py+25],[px+45,py+25],[px-45,py-25]]
                        : [.02,.1,.3,.5,.7,.9,.98].flatMap(x=>[.1,.5,.9].map(y=>[b.x+b.width*x,b.y+b.height*y]));
                    const hit=samples.find(([x,y])=>document.elementFromPoint(x,y)?.closest('[data-cfx-role]')?.dataset.cfxRole===role);
                    if(!hit)throw new Error('No real painted hit for '+role);
                    return {role,x:hit[0],y:hit[1],distance:Math.hypot(hit[0]-px,hit[1]-py),targetId:point.dataset.cfxTargetId};
                });
            }
            """);
        var observations = new List<object>();
        var acquired = new List<JsonElement>();
        foreach (var position in positions.EnumerateArray()) {
            await MoveAwayAsync(page);
            await page.Mouse.MoveAsync((float)position.GetProperty("x").GetDouble(), (float)position.GetProperty("y").GetDouble(), new MouseMoveOptions { Steps = 4 });
            var state = await page.Locator(RegionRoot).EvaluateAsync<JsonElement>("""
                root => ({tooltip:root.querySelector('.cfx-tooltip').hidden?'':root.querySelector('.cfx-tooltip').innerText,
                    crosshairVisible:!root.querySelector('.cfx-crosshair').hidden,crosshair:root.dataset.cfxCrosshair||'',
                    hover:root.dataset.cfxHoverKey||''})
                """);
            observations.Add(new { Position = position, State = state }); acquired.Add(state);
        }
        await CapturePassiveAsync(session, html, "passive-nearby-" + (nearest ? "nearest" : "distance"), observations);
        foreach (var position in positions.EnumerateArray()) Assert.InRange(position.GetProperty("distance").GetDouble(), 5, 120);
        foreach (var state in acquired) {
            Assert.Contains("Observed", state.GetProperty("tooltip").GetString());
            Assert.DoesNotContain(Guidance, state.GetProperty("tooltip").GetString());
            Assert.True(state.GetProperty("crosshairVisible").GetBoolean());
            Assert.StartsWith("point|observations:0|", state.GetProperty("crosshair").GetString());
        }
        AssertNoConsoleErrors(session);
    }

    private static Chart PassiveChart() {
        var chart = Chart.Create().WithSize(600, 360).WithLegend(false).WithTitle("Passive planning guidance")
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddScatter("Observed", new[] { new ChartPoint(5, 6), new ChartPoint(8, 8) })
            .AddPlotRegion(new(ChartColor.FromHex("#5c9e75"), label: Guidance, opacity: .2));
        chart.Series[0].WithInteractionKey("observations");
        return chart;
    }

    private static async Task CapturePassiveAsync(HtmlBrowserSession session, string html, string name, object facts) {
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(capture)) return;
        Directory.CreateDirectory(capture);
        await File.WriteAllTextAsync(Path.Combine(capture, name + ".html"), html);
        await File.WriteAllTextAsync(Path.Combine(capture, name + ".json"), JsonSerializer.Serialize(new { Facts = facts, Console = session.ConsoleLog }, new JsonSerializerOptions { WriteIndented = true }));
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, name + ".png") });
    }
}
