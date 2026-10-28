using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class NativePolarCoordinateBrowserTests {
    [Theory]
    [Trait("Category", "Browser")]
    [InlineData(ChartSeriesKind.Pie)]
    [InlineData(ChartSeriesKind.Donut)]
    [InlineData(ChartSeriesKind.Radar)]
    [InlineData(ChartSeriesKind.Polar)]
    [InlineData(ChartSeriesKind.PolarArea)]
    [InlineData(ChartSeriesKind.ProgressRing)]
    [InlineData(ChartSeriesKind.Sunburst)]
    [InlineData(ChartSeriesKind.Gauge)]
    public async Task CommonPolarProducerRetainsNativeHitAndLeavesNearbyEmptySpaceWithoutCrosshair(ChartSeriesKind kind) {
        var chart = Fixture(kind);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Metadata.TryGetValue("data-cfx-coordinate-system", out var system) && system == "polar");
        if (!Enabled) return;
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage());
        var page = session.Page;
        const string observationSelector = "[data-cfx-point],[data-cfx-target-kind=node]";
        var (target, painted) = await NativePaintedObservationAsync(page);
        var expected = await target.GetAttributeAsync("data-cfx-target-id");
        await page.Mouse.MoveAsync((float)painted[0], (float)painted[1]);
        Assert.Equal(expected, await page.Locator(".cfx-hovered:is(" + observationSelector + ")").GetAttributeAsync("data-cfx-target-id"));
        Assert.NotEqual(string.Empty, await TooltipTextAsync(page));
        Assert.True(await page.Locator(".cfx-crosshair").EvaluateAsync<bool>("node => node.hidden"));
        var empty = await target.EvaluateAsync<double[]>("node => { const box=node.getBoundingClientRect(), svg=node.ownerSVGElement, view=svg.getBoundingClientRect(), cx=box.left+box.width/2, cy=box.top+box.height/2; for(let y=-110;y<=110;y+=10) for(let x=-110;x<=110;x+=10) { if(Math.hypot(x,y)>115) continue; const px=cx+x, py=cy+y; if(px<view.left || px>view.right || py<view.top || py>view.bottom) continue; const hit=document.elementFromPoint(px,py); if(hit && svg.contains(hit) && !hit.closest('[data-cfx-point],[data-cfx-target-kind=node],[data-cfx-role=legend-item]')) return [px,py]; } throw new Error('No nearby empty polar space'); }");
        await page.Mouse.MoveAsync((float)empty[0], (float)empty[1]);
        Assert.Equal(string.Empty, await TooltipTextAsync(page));
        Assert.Null(await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-hover-key"));
        Assert.True(await page.Locator(".cfx-crosshair").EvaluateAsync<bool>("node => node.hidden"));
        await page.Mouse.ClickAsync((float)painted[0], (float)painted[1]);
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [Trait("Category", "Browser")]
    [InlineData(ChartSeriesKind.Radar)]
    [InlineData(ChartSeriesKind.Polar)]
    public async Task PaintedObservationRequiresAnUnoccludedNativeMark(ChartSeriesKind kind) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Fixture(kind).ToInteractiveHtmlPage());
        var page = session.Page;
        var (first, _) = await NativePaintedObservationAsync(page);
        var firstId = await first.GetAttributeAsync("data-cfx-target-id");
        // A host overlay can cover a native vertex. Its semantic metadata does not make it a painted hit.
        await first.EvaluateAsync("node => { const box=node.getBoundingClientRect(), cover=document.createElement('div'); cover.id='proof-occluder'; cover.style.cssText='position:fixed;z-index:2147483647;background:white;left:'+(box.left-2)+'px;top:'+(box.top-2)+'px;width:'+(box.width+4)+'px;height:'+(box.height+4)+'px'; document.body.appendChild(cover); }");
        Assert.Empty(await NativePaintedPointAsync(first));
        var (visible, _) = await NativePaintedObservationAsync(page);
        Assert.NotEqual(firstId, await visible.GetAttributeAsync("data-cfx-target-id"));
        await page.EvaluateAsync("() => { const cover=document.getElementById('proof-occluder'); cover.style.cssText='position:fixed;z-index:2147483647;background:white;inset:0'; }");
        await Assert.ThrowsAsync<InvalidOperationException>(() => NativePaintedObservationAsync(page));
        await page.Locator("#proof-occluder").EvaluateAsync("node => node.remove()");
        Assert.NotEmpty(await NativePaintedPointAsync(first));
        AssertNoConsoleErrors(session);
    }

    // Axis captions and host overlays can occlude a vertex on a different font/layout host.
    // The family contract requires a real visible observation, not a particular metadata ordinal.
    private static async Task<(Microsoft.Playwright.ILocator Target, double[] Point)> NativePaintedObservationAsync(Microsoft.Playwright.IPage page) {
        var observations = page.Locator("[data-cfx-coordinate-system='polar']").Locator("[data-cfx-point],[data-cfx-target-kind=node]");
        for (var index = 0; index < await observations.CountAsync(); index++) {
            var target = observations.Nth(index);
            var point = await NativePaintedPointAsync(target);
            if (point.Length == 2) return (target, point);
        }
        var detail = await observations.EvaluateAllAsync<string>("nodes => JSON.stringify(nodes.map(node=>({owner:node.outerHTML.slice(0,6000),box:node.getBoundingClientRect().toJSON()})))");
        throw new InvalidOperationException("No unoccluded native painted observation: " + detail);
    }

    [Fact]
    public void LinearGaugeAndScalarCircleDoNotDeclarePolarObservationGeometry() {
        Assert.DoesNotContain("data-cfx-coordinate-system=\"polar\"", Chart.Create().AddLinearGauge("Linear", 80).ToSvg(), StringComparison.Ordinal);
        var circle = Chart.Create().AddCircle("Scalar", 80).Prepare(VisualExportRequest.ForChart(Chart.Create()).Context).Scene;
        Assert.DoesNotContain(circle.Nodes.OfType<VisualSceneGroup>(), group => group.Metadata.ContainsKey("data-cfx-point"));
    }

    // Sample the actual native geometry before the broad owner box. Small circular markers
    // can fall between a screen-space grid's samples on a different font/layout host.
    private static Task<double[]> NativePaintedPointAsync(Microsoft.Playwright.ILocator target) => target.EvaluateAsync<double[]>("""
        node => {
            const marks=[...node.querySelectorAll('circle,ellipse,rect,path,polygon')].filter(mark=>
                !mark.hasAttribute('data-cfx-browser-hit-area')&&!mark.classList.contains('cfx-prepared-point-marker'));
            const candidates=[],box=node.getBoundingClientRect();
            for(const mark of marks.filter(()=>['radar','polar'].includes(node.dataset.cfxKind))){
                const matrix=mark.getScreenCTM(),box=mark.getBBox(),screen=mark.getBoundingClientRect();if(!matrix)continue;
                candidates.push({x:(screen.left+screen.right)/2,y:(screen.top+screen.bottom)/2});
                candidates.push(new DOMPoint(box.x+box.width/2,box.y+box.height/2).matrixTransform(matrix));
                if(typeof mark.getTotalLength==='function'){
                    const length=mark.getTotalLength();
                    for(let i=0;i<64;i++)candidates.push(mark.getPointAtLength(length*i/64).matrixTransform(matrix));
                }
            }
            for(let y=1;y<20;y++)for(let x=1;x<20;x++)candidates.push({x:box.left+box.width*x/20,y:box.top+box.height*y/20});
            for(const {x,y} of candidates){
                const hit=document.elementFromPoint(x,y);
                if(!hit||hit.closest('[data-cfx-point],[data-cfx-target-kind=node]')!==node)continue;
                const painted=marks.some(mark=>{
                    const matrix=mark.getScreenCTM(),style=getComputedStyle(mark);
                    if(!matrix||style.display==='none'||style.visibility==='hidden'||Number(style.opacity)===0)return false;
                    // A direct native SVG hit is already the browser's physical painted geometry.
                    if(hit===mark)return style.fill!=='none'&&style.fill!=='transparent'&&Number(style.fillOpacity)>0
                        ||style.stroke!=='none'&&Number(style.strokeOpacity)>0;
                    const point=new DOMPoint(x,y).matrixTransform(matrix.inverse());
                    return style.fill!=='none'&&style.fill!=='transparent'&&Number(style.fillOpacity)>0&&mark.isPointInFill(point)
                        ||style.stroke!=='none'&&Number(style.strokeOpacity)>0&&mark.isPointInStroke(point);
                });
                if(painted)return [x,y];
            }
            return [];
        }
        """);

    private static Chart Fixture(ChartSeriesKind kind) {
        var chart = Chart.Create().WithSize(600, 360).WithLegend(false).WithDataLabels(false).WithXLabels("North", "South", "East", "West");
        var points = new[] { new ChartPoint(1, 60), new ChartPoint(2, 80), new ChartPoint(3, 50), new ChartPoint(4, 70) };
        switch (kind) {
            case ChartSeriesKind.Pie: chart.AddPie("Observed", points); break;
            case ChartSeriesKind.Donut: chart.AddDonut("Observed", points); break;
            case ChartSeriesKind.Radar: chart.AddRadar("Observed", points); break;
            case ChartSeriesKind.Polar: chart.AddPolar("Observed", points); break;
            case ChartSeriesKind.PolarArea: chart.AddPolarArea("Observed", points); break;
            case ChartSeriesKind.ProgressRing: chart.AddProgressRing("Observed", points); break;
            case ChartSeriesKind.Sunburst: chart.AddSunburst("Observed", new[] { new ChartHierarchyItem("All", "All"), new ChartHierarchyItem("North", "North", "All", 60), new ChartHierarchyItem("South", "South", "All", 80), new ChartHierarchyItem("East", "East", "All", 50) }); break;
            case ChartSeriesKind.Gauge: chart.AddGauge("Observed", 80); break;
        }
        return chart;
    }
}
