using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipPaintBrowserTests {
    [Theory]
    [InlineData("heatmap", false, true)]
    [InlineData("heatmap", true, false)]
    [InlineData("map", false, false)]
    [InlineData("progress", true, true)]
    public async Task NativeOnlyCoordinatesDoNotInferReadoutsOrGuidesInEmptyStageSpace(string family, bool bounded, bool guide) {
        if (!Enabled) return;
        var chart = NativeOnlyChart(family);
        var selector = family switch { "heatmap" => "[data-cfx-role=heatmap-cell]", "map" => "[data-cfx-role=region-map-region-source][data-cfx-point]", _ => "[data-cfx-role=progress-row]" };
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            options.Interaction.Features = ChartInteractionFeatures.Tooltips | (guide ? ChartInteractionFeatures.Crosshair : ChartInteractionFeatures.None);
            options.Tooltip.Range = bounded ? HtmlChartTooltipRange.WithinDistance(120) : HtmlChartTooltipRange.Nearest;
        }), 950, 720);
        var page = session.Page;
        Assert.Null(await page.Locator(selector).First.EvaluateAsync<string?>("n => n.closest('[data-cfx-coordinate-system]')?.dataset.cfxCoordinateSystem || null"));
        var id = await page.Locator(selector).First.GetAttributeAsync("data-cfx-target-id");
        var kind = await page.Locator(selector).First.GetAttributeAsync("data-cfx-target-kind");
        var box = await BoxAsync(page, selector);
        var native = new double[] { box.X + box.Width / 2, box.Y + box.Height / 2 };
        Assert.True(await page.Locator(selector).First.EvaluateAsync<bool>("(n,p) => n.contains(document.elementFromPoint(p[0],p[1]))", native));
        await page.Mouse.MoveAsync((float)native[0], (float)native[1]);
        Assert.False(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        await AssertHoverIdentityAsync(page, kind!, id!);
        Assert.True(await page.Locator(".cfx-crosshair").IsHiddenAsync());
        await CaptureContractAsync(page, "paint-native-only-" + family + "-" + bounded + "-native", new { Native = native, Id = id, Kind = kind });
        var empty = await EmptyStagePositionAsync(page, selector, bounded);
        if (bounded) Assert.InRange(empty[2], 40, 115);
        else Assert.True(empty[2] >= 40);
        await page.Mouse.MoveAsync((float)empty[0], (float)empty[1]);
        Assert.True(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        Assert.True(await page.Locator(".cfx-crosshair").IsHiddenAsync());
        Assert.Null(await page.Locator(Root).GetAttributeAsync("data-cfx-hover-key"));
        await CaptureContractAsync(page, "paint-native-only-" + family + "-" + bounded + "-empty", new { Empty = empty, Bounded = bounded, Guide = guide });
        await page.Mouse.MoveAsync((float)native[0], (float)native[1]);
        Assert.False(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        await AssertHoverIdentityAsync(page, kind!, id!);
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task AVisiblePointLabelCannotReplaceItsHiddenPrimaryMarker() {
        if (!Enabled) return;
        var chart = Frame(false).WithXAxisBounds(0, 10).WithYAxisBounds(0, 10).WithDataLabels()
            .AddScatter("Measured", new[] { new ChartPoint(5, 5) });
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            options.Tooltip.Range = HtmlChartTooltipRange.Nearest;
            options.Interaction.Features = ChartInteractionFeatures.Tooltips;
        }), 950, 720);
        var page = session.Page;
        Assert.True(await page.Locator("[data-cfx-role=data-label]").CountAsync() > 0);
        await page.Locator(Point(0, 0) + " [data-cfx-role=marker]").EvaluateAsync("n => n.style.opacity = '0'");
        await MoveToAsync(page, Point(0, 0), offsetY: 60);
        Assert.True(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        Assert.Null(await page.Locator(Root).GetAttributeAsync("data-cfx-hover-key"));
        AssertNoConsoleErrors(session);
    }

    private static Chart NativeOnlyChart(string family) {
        var chart = Frame(family == "progress").WithTitle("Native " + family);
        return family switch {
            "heatmap" => chart.WithSize(596, 400).WithXLabels("North", "South").WithHeatmapCellGap(20)
                .AddHeatmapRow("Requests", new[] { 37d, 48d }).AddHeatmapRow("Previous", new[] { 61d, 54d }),
            "map" => chart.WithMapLabels(false).WithMapScaleLegend(false).AddRegionMap("Coverage", new ChartMapDefinition("regions", "Regions", 100, 100, new[] {
                new ChartMapRegion("A", "First", "M20 20H40V40H20Z"), new ChartMapRegion("B", "Second", "M60 60H80V80H60Z")
            }), new[] { new ChartRegionMapItem("A", 10), new ChartRegionMapItem("B", 20) }),
            "progress" => chart.AddProgressBars("Progress", new[] { new ChartProgressItem("Ready", 37), new ChartProgressItem("Working", 61) }),
            _ => throw new ArgumentOutOfRangeException(nameof(family))
        };
    }

    private static async Task<double[]> EmptyStagePositionAsync(IPage page, string selector, bool radius) => await page.EvaluateAsync<double[]>("""
        input => {
            const r = document.querySelector('.cfx-interactive-chart'), s = r.querySelector('.cfx-stage').getBoundingClientRect();
            const targets = Array.from(r.querySelectorAll(input.selector)).map(n => {
                const b = n.getBoundingClientRect(); return [b.x+b.width/2,b.y+b.height/2];
            });
            const positions = [];
            for (let y=s.top+8; y<s.bottom-8; y+=8) for (let x=s.left+8; x<s.right-8; x+=8) {
                const hit = document.elementFromPoint(x,y);
                if (!hit || !r.contains(hit) || hit.closest('[data-cfx-target-kind]')) continue;
                const distance = Math.min(...targets.map(p => Math.hypot(p[0]-x,p[1]-y)));
                if (distance>=40 && (!input.radius || distance<=115)) positions.push([x,y,distance]);
            }
            positions.sort((a,b) => Math.abs(a[2]-95)-Math.abs(b[2]-95));
            if (!positions.length) throw new Error('No genuinely empty stage position for this policy');
            return positions[0];
        }
        """, new { selector, radius });
}
