using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipPaintBrowserTests {
    [Theory]
    [InlineData("line", false)]
    [InlineData("line", true)]
    [InlineData("area", false)]
    [InlineData("range-band", true)]
    [InlineData("range-area", false)]
    public async Task ExactNativeConnectedSeriesSummarySurvivesStageMovementWithoutInventingAnObservation(string family, bool guide) {
        if (!Enabled) return;
        await using var session = await OpenAsync(ConnectedSeries(family).ToInteractiveHtmlPage(options => {
            options.Tooltip.Range = HtmlChartTooltipRange.Exact;
            options.Interaction.Features = ChartInteractionFeatures.Tooltips | (guide ? ChartInteractionFeatures.Crosshair : ChartInteractionFeatures.None);
        }), 950, 720);
        var page = session.Page;
        var positions = await NativeSeriesPositionsAsync(page, family);
        var id = await page.Locator("[data-cfx-role=series][data-cfx-series='0']").GetAttributeAsync("data-cfx-target-id");
        await page.EvaluateAsync("() => { window.nativeEnter = null; const r = document.querySelector('.cfx-interactive-chart'); r.querySelector('[data-cfx-role=series]').addEventListener('pointerenter', () => { const t = r.querySelector('.cfx-tooltip'); window.nativeEnter = { visible: !t.hidden, text: t.hidden ? '' : t.innerText }; }); }");
        await page.Mouse.MoveAsync(2, 2);
        await page.Mouse.MoveAsync((float)positions[0], (float)positions[1]);
        Assert.True(await page.EvaluateAsync<bool>("() => window.nativeEnter.visible"));
        Assert.Contains("Reading", await TooltipTextAsync(page), StringComparison.Ordinal);
        Assert.Equal(new[] { "Series" }, await page.Locator(".cfx-tooltip dt").AllTextContentsAsync());
        await AssertHoverIdentityAsync(page, "series", id!);
        await page.Mouse.MoveAsync((float)positions[2], (float)positions[3]);
        Assert.Contains("Reading", await TooltipTextAsync(page), StringComparison.Ordinal);
        Assert.Equal(new[] { "Series" }, await page.Locator(".cfx-tooltip dt").AllTextContentsAsync());
        await AssertHoverIdentityAsync(page, "series", id!);
        Assert.Equal(!guide, await page.Locator(".cfx-crosshair").IsHiddenAsync());
        if (guide) {
            var guideIdentity = (await page.Locator(Root).GetAttributeAsync("data-cfx-crosshair"))!.Split('|');
            Assert.Equal("point", guideIdentity[0]);
            Assert.NotEqual(id, guideIdentity[1]);
        }
        await CaptureContractAsync(page, "paint-exact-native-" + family + "-guide-" + guide, new { Family = family, Guide = guide, Positions = positions, NativeSummaryId = id });
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task NativeMarkerFreeLineCanAcquireARealObservationUnderNearestAndBoundedPolicies(bool bounded, bool guide) {
        if (!Enabled) return;
        await using var session = await OpenAsync(ConnectedSeries("line").ToInteractiveHtmlPage(options => {
            options.Tooltip.Mode = HtmlChartTooltipMode.Single;
            options.Tooltip.Range = bounded ? HtmlChartTooltipRange.WithinDistance(120) : HtmlChartTooltipRange.Nearest;
            options.Interaction.Features = ChartInteractionFeatures.Tooltips | (guide ? ChartInteractionFeatures.Crosshair : ChartInteractionFeatures.None);
        }), 950, 720);
        var page = session.Page;
        var positions = await NativeSeriesPositionsAsync(page, "line");
        var expected = await page.EvaluateAsync<string>("p => Array.from(document.querySelectorAll('[data-cfx-point]')).map(n => { const b = n.getBoundingClientRect(); return { id:n.dataset.cfxTargetId, d:Math.hypot(b.x+b.width/2-p[0], b.y+b.height/2-p[1]) }; }).sort((a,b) => a.d-b.d)[0].id", positions);
        await page.Mouse.MoveAsync(2, 2); await page.Mouse.MoveAsync((float)positions[0], (float)positions[1]);
        Assert.Equal(new[] { "Series", "X", "Y" }, await page.Locator(".cfx-tooltip dt").AllTextContentsAsync());
        Assert.Contains("5", await TooltipTextAsync(page), StringComparison.Ordinal);
        await AssertHoverIdentityAsync(page, "point", expected);
        Assert.Equal(!guide, await page.Locator(".cfx-crosshair").IsHiddenAsync());
        await CaptureContractAsync(page, "paint-native-line-" + (bounded ? "bounded" : "nearest"), new { Positions = positions, ObservationId = expected, Guide = guide });
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AuthoredPlotClipsKeepCroppedFactsOutOfPointerLookupWhileInPlotObservationsRemainUsable(bool hasVisibleSibling) {
        if (!Enabled) return;
        var chart = Frame(hasVisibleSibling).WithTitle("Cropped observations").WithLineMarkers(ChartLineMarkerMode.None)
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 10).AddLine("Outside", new[] { new ChartPoint(5, 11), new ChartPoint(6, hasVisibleSibling ? 9 : 12) });
        chart.Options.ClipMarksToPlot = true;
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            options.Tooltip.Range = hasVisibleSibling ? HtmlChartTooltipRange.WithinDistance(30) : HtmlChartTooltipRange.Nearest;
            options.Interaction.Features = ChartInteractionFeatures.Tooltips | (hasVisibleSibling ? ChartInteractionFeatures.Crosshair : ChartInteractionFeatures.None);
        }), 950, 720);
        var page = session.Page;
        var outside = await page.Locator(Point(0, 0)).GetAttributeAsync("data-cfx-target-id");
        var position = await page.Locator(Point(0, 0)).EvaluateAsync<double[]>("n => { const b = n.getBoundingClientRect(); return [b.x+b.width/2,b.y+b.height/2+20]; }");
        Assert.Equal("11", await page.Locator(Point(0, 0)).GetAttributeAsync("data-cfx-y"));
        Assert.True(await page.EvaluateAsync<bool>("p => { const h=document.elementFromPoint(p[0],p[1]); return !!h && !h.closest('[data-cfx-target-kind]'); }", position));
        await page.Mouse.MoveAsync((float)position[0], (float)position[1]);
        Assert.True(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        Assert.DoesNotContain("point|" + outside + "|", await page.Locator(Root).GetAttributeAsync("data-cfx-hover-key") ?? "", StringComparison.Ordinal);
        await CaptureContractAsync(page, "paint-cropped-" + (hasVisibleSibling ? "partial-dark" : "entire-light"), new { Position = position, SourceValue = 11, HasVisibleSibling = hasVisibleSibling });
        if (hasVisibleSibling) {
            await MoveToAsync(page, Point(0, 1));
            Assert.Contains("9", await TooltipTextAsync(page), StringComparison.Ordinal);
            await AssertHoverIdentityAsync(page, "point", (await page.Locator(Point(0, 1)).GetAttributeAsync("data-cfx-target-id"))!);
            await CaptureContractAsync(page, "paint-cropped-positive-in-plot", new { SourceValue = 9, Guide = true });
        } else Assert.True(await page.Locator(".cfx-crosshair").IsHiddenAsync());
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task NativeSvgRoundingKeepsAPaintedBoundaryObservationAvailableAtItsScreenScale() {
        if (!Enabled) return;
        await using var session = await OpenAsync(StatusLines(false).ToInteractiveHtmlPage(options => {
            ExactFeatures(options);
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
        }), 340, 720);
        var page = session.Page;
        var target = page.Locator(Point(1, 0));
        var boundary = await target.EvaluateAsync<double[]>("n => { const b=n.getBoundingClientRect(), line=n.closest('[data-cfx-role=series]').querySelector('[data-cfx-role=line]'), owner=line.parentElement, id=owner.getAttribute('clip-path').match(/#([^)]*)/)[1], clip=n.ownerSVGElement.getElementById(id).firstElementChild.getBBox(), matrix=owner.getScreenCTM(), edge=new DOMPoint(clip.x,clip.y).matrixTransform(matrix); return [edge.x-(b.x+b.width/2),matrix.a]; }");
        Assert.InRange(boundary[0], double.Epsilon, .001 * boundary[1]);
        Assert.InRange(boundary[1], .1, .9);
        await MoveToAsync(page, Point(1, 0), offsetX: 1);
        Assert.Contains("120", await TooltipTextAsync(page), StringComparison.Ordinal);
        await AssertHoverIdentityAsync(page, "point", (await target.GetAttributeAsync("data-cfx-target-id"))!);
        await page.Mouse.DownAsync(); await page.Mouse.UpAsync();
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        Assert.Equal("true", await page.Locator(Root).GetAttributeAsync("data-cfx-tooltip-pinned"));
        await page.Locator("[data-cfx-reset]").ClickAsync();
        await target.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        await CaptureContractAsync(page, "paint-rounded-boundary-fit-compact-light", new { OutsideRoundedClipCssPixels = boundary[0], Scale = boundary[1], SourceValue = 120 });
        AssertNoConsoleErrors(session);
    }

    private static Chart ConnectedSeries(string family) {
        var chart = Frame(false).WithTitle("Native marker-free " + family).WithLineMarkers(ChartLineMarkerMode.None)
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 10);
        var points = new[] { new ChartPoint(3, 5), new ChartPoint(5, 5), new ChartPoint(7, 5) };
        var ranges = new[] { new ChartRangeBand(3, 4, 6), new ChartRangeBand(5, 4, 6), new ChartRangeBand(7, 4, 6) };
        return family switch {
            "line" => chart.AddLine("Reading", points), "area" => chart.AddArea("Reading", points),
            "range-band" => chart.AddRangeBand("Reading", ranges), "range-area" => chart.AddRangeArea("Reading", ranges),
            _ => throw new ArgumentOutOfRangeException(nameof(family))
        };
    }

    private static async Task<double[]> NativeSeriesPositionsAsync(IPage page, string role) {
        var positions = await page.Locator("[data-cfx-role='" + role + "']").First.EvaluateAsync<double[]>("path => { const length=path.getTotalLength(), owner=path.closest('[data-cfx-role=series]'); for (const f of [.13,.23,.37,.61,.77]) { const a=path.getPointAtLength(length*f), b=path.getPointAtLength(length*f+2), p=new DOMPoint(a.x,a.y).matrixTransform(path.getScreenCTM()), q=new DOMPoint(b.x,b.y).matrixTransform(path.getScreenCTM()); const valid=t => { const hit=document.elementFromPoint(t.x,t.y); return !!hit && owner.contains(hit) && hit.closest('[data-cfx-target-kind]')===owner; }; if (valid(p)&&valid(q)) return [p.x,p.y,q.x,q.y]; } throw new Error('No native series path position outside observation hit areas'); }");
        return positions;
    }

    private static async Task AssertHoverIdentityAsync(IPage page, string kind, string id) =>
        Assert.Equal(new[] { kind, id }, await page.Locator(Root).EvaluateAsync<string[]>("n => (n.dataset.cfxHoverKey || '').split('|').slice(0,2)"));
}
