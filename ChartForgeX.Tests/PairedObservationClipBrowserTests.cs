using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Protects paired observations whose native extent, rather than its midpoint, intersects the plot.</summary>
public sealed class PairedObservationClipBrowserTests {
    [BrowserTheory]
    [InlineData(ChartSeriesKind.DifferenceArea, false)]
    [InlineData(ChartSeriesKind.DifferenceArea, true)]
    [InlineData(ChartSeriesKind.RangeArea, false)]
    [InlineData(ChartSeriesKind.RangeBand, false)]
    public async Task VisiblePairedBoundaryRetainsBothValuesAndOnePointerKeyboardIdentity(ChartSeriesKind kind, bool negative) {
        if (!Enabled) return;
        var chart = Create(kind, negative ? 1 : 100, negative ? 100 : 1);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), 800, 600);
        var page = session.Page; var point = page.Locator(Point(0, 0));
        var role = kind == ChartSeriesKind.DifferenceArea ? negative ? "difference-first" : "difference-second" : "range-lower";
        var position = await page.Locator("[data-cfx-role='" + role + "']").EvaluateAsync<double[]>("""
            path=>{const p=path.getPointAtLength(0),screen=new DOMPoint(p.x+1,p.y-1).matrixTransform(path.getScreenCTM());
              return [screen.x,screen.y];}
            """);
        Assert.True(await page.EvaluateAsync<bool>("p=>document.elementFromPoint(p[0],p[1])?.closest('[data-cfx-point]')?.dataset.cfxPoint==='0'", position));
        Assert.Equal("0", await point.GetAttributeAsync("tabindex"));
        Assert.Equal("paired:0", await point.GetAttributeAsync("data-cfx-target-id"));
        Assert.Equal("1", await point.GetAttributeAsync("data-cfx-source-count"));
        await page.EvaluateAsync("""
            ()=>{window.cfxPairSelections=[];document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect',event=>{
              if(event.detail.target?.targetKind==='point')window.cfxPairSelections.push(event.detail.target);});}
            """);
        await page.Mouse.MoveAsync(2, 2); await page.Mouse.MoveAsync((float)position[0], (float)position[1]);
        await AssertPairedReadoutAsync(page, kind, negative);
        Assert.StartsWith("point|paired:0|", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-hover-key") ?? "");
        await page.Mouse.ClickAsync((float)position[0], (float)position[1]);
        Assert.Equal("true", await point.GetAttributeAsync("aria-selected"));
        await CaptureAsync(page, kind + "-" + negative + "-pointer", position);
        await MoveAwayAsync(page); await point.FocusAsync();
        await AssertPairedReadoutAsync(page, kind, negative);
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("false", await point.GetAttributeAsync("aria-selected"));
        var selections = await page.EvaluateAsync<JsonElement>("()=>window.cfxPairSelections");
        Assert.Equal(2, selections.GetArrayLength());
        Assert.All(selections.EnumerateArray(), target => {
            Assert.Equal("paired:0", target.GetProperty("targetId").GetString());
            Assert.Equal("0", target.GetProperty("sourcePoint").ToString());
        });
        Assert.Equal(1, await page.Locator("[data-cfx-role='point'][tabindex='0']").CountAsync());
        await CaptureAsync(page, kind + "-" + negative + "-keyboard", position);
        await page.Keyboard.PressAsync("ArrowRight");
        Assert.Equal("paired:1", await page.EvaluateAsync<string>("()=>document.activeElement.dataset.cfxTargetId"));
        await AssertPairedReadoutAsync(page, kind, negative);
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task LogarithmicRangeMidlineKeepsItsNativeCapReadout() {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(600, 380).WithTitle("Logarithmic paired midpoint")
            .WithTheme(VisualTheme.Graphite()).WithLegend(false).WithDataLabels(false)
            .WithXAxisBounds(0, 1).WithYAxisBounds(10, 1000).ConfigureYAxis(axis => axis.Scale = ChartScaleKind.Logarithmic)
            .WithLineVisualStyle(ChartLineVisualStyle.Plain());
        chart.AddSeries(ChartSeries.CreateRangeArea(new ChartSeriesId("paired"), "Pair", new[] { new ChartRangeBand(.5, 1, 1800) }));
        chart.Series[0].WithStrokeWidth(6).ConfigureMarkers(markers => markers.Enabled = false);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        var html = chart.ToInteractiveHtmlPage();
        if (!string.IsNullOrWhiteSpace(directory)) {
            var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
            await File.WriteAllTextAsync(Path.Combine(directory, "log-midline.svg"), prepared.ToSvg());
            await File.WriteAllBytesAsync(Path.Combine(directory, "log-midline.native.png"), prepared.ToPng());
            await File.WriteAllTextAsync(Path.Combine(directory, "log-midline.html"), html);
        }
        await using var session = await OpenAsync(html, 800, 600);
        var page = session.Page; var point = page.Locator(Point(0, 0));
        var box = await point.BoundingBoxAsync() ?? throw new InvalidOperationException("Missing paired hit area");
        var position = new[] { (double)(box.X + box.Width / 2), box.Y + box.Height / 2 };
        await page.Mouse.MoveAsync(2, 2); await page.Mouse.MoveAsync((float)position[0], (float)position[1]);
        var pointerReadout = !await page.Locator(".cfx-tooltip").IsHiddenAsync();
        await page.Mouse.ClickAsync((float)position[0], (float)position[1]);
        await CaptureAsync(page, "log-midline-pointer", position);
        await MoveAwayAsync(page); await point.FocusAsync();
        var keyboardReadout = !await page.Locator(".cfx-tooltip").IsHiddenAsync();
        await CaptureAsync(page, "log-midline-keyboard", position);
        Assert.Equal("0", await point.GetAttributeAsync("tabindex"));
        Assert.True(pointerReadout); Assert.True(keyboardReadout);
        Assert.Equal("1", await point.GetAttributeAsync("data-cfx-lower"));
        Assert.Equal("1800", await point.GetAttributeAsync("data-cfx-upper"));
        Assert.Contains("1 - 1.8k", await page.Locator(".cfx-tooltip dd").AllTextContentsAsync());
        Assert.Equal("paired:0", await point.GetAttributeAsync("data-cfx-target-id"));
        Assert.Equal("1", await point.GetAttributeAsync("data-cfx-source-count"));
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("false", await point.GetAttributeAsync("aria-selected"));
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task OffPlotEndpointCapRetainsItsOwnPairedReadout() {
        if (!Enabled) return;
        var chart = Create(ChartSeriesKind.DifferenceArea, 100, 1, visibleSibling: true, firstX: -.001);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        var html = chart.ToInteractiveHtmlPage();
        if (!string.IsNullOrWhiteSpace(directory)) {
            var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
            await File.WriteAllTextAsync(Path.Combine(directory, "offplot-x-cap.svg"), prepared.ToSvg());
            await File.WriteAllBytesAsync(Path.Combine(directory, "offplot-x-cap.native.png"), prepared.ToPng());
            await File.WriteAllTextAsync(Path.Combine(directory, "offplot-x-cap.html"), html);
        }
        await using var session = await OpenAsync(html, 800, 600);
        var page = session.Page; var point = page.Locator(Point(0, 0));
        var position = await page.Locator("[data-cfx-role=difference-second]").EvaluateAsync<double[]>("""
            path=>{let point=path.getPointAtLength(0).matrixTransform(path.getScreenCTM());
              for(let p=path;p&&p.ownerSVGElement;p=p.parentElement){const match=/^url\(#([^)]*)\)$/.exec(p.getAttribute('clip-path')||'');if(!match)continue;
                const r=p.ownerSVGElement.getElementById(match[1]).firstElementChild.getBBox(),m=p.getScreenCTM();
                const a=new DOMPoint(r.x,r.y).matrixTransform(m),b=new DOMPoint(r.x+r.width,r.y+r.height).matrixTransform(m);
                point=new DOMPoint(Math.max(a.x+1,Math.min(b.x-1,point.x)),Math.max(a.y+1,Math.min(b.y-1,point.y)));}
              return [point.x,point.y];}
            """);
        Assert.True(await page.EvaluateAsync<bool>("p=>document.elementFromPoint(p[0],p[1])?.closest('[data-cfx-point]')?.dataset.cfxPoint==='0'", position));
        Assert.Equal("0", await point.GetAttributeAsync("tabindex"));
        Assert.Equal("paired:0", await point.GetAttributeAsync("data-cfx-target-id"));
        Assert.Equal("-0.001", await point.GetAttributeAsync("data-cfx-x"));
        await page.Mouse.MoveAsync(2, 2); await page.Mouse.MoveAsync((float)position[0], (float)position[1]);
        await AssertPairedReadoutAsync(page, ChartSeriesKind.DifferenceArea, false);
        await page.Mouse.ClickAsync((float)position[0], (float)position[1]);
        Assert.Equal("true", await point.GetAttributeAsync("aria-selected"));
        await CaptureAsync(page, "offplot-x-cap-pointer", position);
        await MoveAwayAsync(page); await point.FocusAsync();
        await AssertPairedReadoutAsync(page, ChartSeriesKind.DifferenceArea, false);
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("false", await point.GetAttributeAsync("aria-selected"));
        await CaptureAsync(page, "offplot-x-cap-keyboard", position);
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task OffPlotBoundaryCapRetainsReadoutAndKeyboardIdentityWithoutCrosshair() {
        if (!Enabled) return;
        // Both Y values are inside the plot: only the source X excludes the guide anchor.
        var chart = Create(ChartSeriesKind.DifferenceArea, 8, 2, visibleSibling: true, firstX: -.001);
        // Keep enough endpoint ink inside the clip for a native DOM hit across browser pixel rounding.
        chart.Series[0].WithStrokeWidth(6);
        var html = chart.ToInteractiveHtmlPage(options => options.Interaction.Features =
            ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Selection |
            ChartInteractionFeatures.KeyboardNavigation | ChartInteractionFeatures.Crosshair);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
            await File.WriteAllTextAsync(Path.Combine(directory, "crosshair-offplot-cap.svg"), prepared.ToSvg());
            await File.WriteAllBytesAsync(Path.Combine(directory, "crosshair-offplot-cap.native.png"), prepared.ToPng());
            await File.WriteAllTextAsync(Path.Combine(directory, "crosshair-offplot-cap.html"), html);
        }
        await using var session = await OpenAsync(html, 800, 600);
        var page = session.Page; var point = page.Locator(Point(0, 0));
        var root = page.Locator(".cfx-interactive-chart"); var guide = page.Locator(".cfx-crosshair");
        Assert.Equal(1, await guide.CountAsync());
        await MoveToAsync(page, Point(0, 1));
        Assert.False(await guide.IsHiddenAsync());
        Assert.StartsWith("point|paired:1|", await root.GetAttributeAsync("data-cfx-crosshair") ?? "");
        var position = await page.Locator("[data-cfx-role=difference-second]").EvaluateAsync<double[]>("""
            path=>{
              const matrix=path.getScreenCTM(),anchor=path.getPointAtLength(0).matrixTransform(matrix);
              let left=-Infinity,top=-Infinity,right=Infinity,bottom=Infinity;
              for(let parent=path;parent&&parent.ownerSVGElement;parent=parent.parentElement){
                const match=/^url\(#([^)]*)\)$/.exec(parent.getAttribute('clip-path')||'');if(!match)continue;
                const rect=parent.ownerSVGElement.getElementById(match[1]).firstElementChild;
                if(rect.tagName!=='rect')throw new Error('Expected actual rectangular native plot clip');
                const r=rect.getBBox(),m=parent.getScreenCTM(),a=new DOMPoint(r.x,r.y).matrixTransform(m),
                  b=new DOMPoint(r.x+r.width,r.y+r.height).matrixTransform(m);
                left=Math.max(left,a.x);top=Math.max(top,a.y);right=Math.min(right,b.x);bottom=Math.min(bottom,b.y);
              }
              if(!Number.isFinite(left)||!(anchor.x<left&&anchor.y>top&&anchor.y<bottom))
                throw new Error('Expected source X outside and endpoint Y inside actual plot clip');
              const radius=parseFloat(getComputedStyle(path).strokeWidth)*Math.hypot(matrix.a,matrix.b)/2,
                visible=anchor.x+radius-left;
              if(!(visible>0))throw new Error('Expected a real round endpoint cap inside native clip');
              const screen=new DOMPoint(left+visible/2,anchor.y),local=screen.matrixTransform(matrix.inverse());
              if(!(screen.x<right&&path.isPointInStroke(local)))throw new Error('Pointer must hit actual native boundary ink');
              return [screen.x,screen.y];
            }
            """);
        Assert.True(await page.EvaluateAsync<bool>("p=>document.elementFromPoint(p[0],p[1])?.closest('[data-cfx-point]')?.dataset.cfxPoint==='0'", position));
        Assert.Equal("0", await point.GetAttributeAsync("tabindex"));
        Assert.Equal("paired:0", await point.GetAttributeAsync("data-cfx-target-id"));
        Assert.Equal("1", await point.GetAttributeAsync("data-cfx-source-count"));
        Assert.Equal("0", await point.GetAttributeAsync("data-cfx-source-point"));
        Assert.Equal("-0.001", await point.GetAttributeAsync("data-cfx-x"));
        Assert.Equal("8", await point.GetAttributeAsync("data-cfx-first"));
        Assert.Equal("2", await point.GetAttributeAsync("data-cfx-second"));
        await page.EvaluateAsync("""
            ()=>{window.cfxCapSelections=[];document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect',event=>{
              if(event.detail.target?.targetKind==='point')window.cfxCapSelections.push(event.detail.target);});}
            """);
        async Task AssertCapReadoutAsync() {
            Assert.False(await page.Locator(".cfx-tooltip").IsHiddenAsync());
            var values = await page.Locator(".cfx-tooltip dd").AllTextContentsAsync();
            Assert.Contains("8", values); Assert.Contains("2", values);
            Assert.True(await guide.IsHiddenAsync());
            Assert.Null(await root.GetAttributeAsync("data-cfx-crosshair"));
        }
        await page.Mouse.MoveAsync(2, 2); await page.Mouse.MoveAsync((float)position[0], (float)position[1]);
        await AssertCapReadoutAsync();
        Assert.StartsWith("point|paired:0|", await root.GetAttributeAsync("data-cfx-hover-key") ?? "");
        await page.Mouse.ClickAsync((float)position[0], (float)position[1]);
        Assert.Equal("true", await point.GetAttributeAsync("aria-selected"));
        await CaptureAsync(page, "crosshair-offplot-cap-pointer", position);
        await MoveAwayAsync(page); await point.FocusAsync();
        Assert.Equal("paired:0", await page.EvaluateAsync<string>("()=>document.activeElement.dataset.cfxTargetId"));
        await AssertCapReadoutAsync();
        await CaptureAsync(page, "crosshair-offplot-cap-keyboard", position);
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("false", await point.GetAttributeAsync("aria-selected"));
        var selections = await page.EvaluateAsync<JsonElement>("()=>window.cfxCapSelections");
        Assert.Equal(2, selections.GetArrayLength());
        Assert.All(selections.EnumerateArray(), target => {
            Assert.Equal("paired:0", target.GetProperty("targetId").GetString());
            Assert.Equal("0", target.GetProperty("sourcePoint").ToString());
        });
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(ChartSeriesKind.DifferenceArea, "y-clipped")]
    [InlineData(ChartSeriesKind.RangeArea, "y-clipped")]
    [InlineData(ChartSeriesKind.RangeBand, "y-clipped")]
    [InlineData(ChartSeriesKind.DifferenceArea, "x-clipped")]
    [InlineData(ChartSeriesKind.DifferenceArea, "transparent")]
    [InlineData(ChartSeriesKind.RangeArea, "transparent")]
    [InlineData(ChartSeriesKind.RangeBand, "transparent")]
    public async Task UnpaintedPairedExtentCannotBorrowAnotherObservationsPaint(ChartSeriesKind kind, string unavailable) {
        if (!Enabled) return;
        var chart = unavailable == "y-clipped" ? Create(kind, 10.08, 10.07, visibleSibling: true)
            : Create(kind, 100, 1, visibleSibling: unavailable == "x-clipped", firstX: unavailable == "x-clipped" ? -.004 : .25);
        if (unavailable == "transparent") chart.Series[0].WithColor(ChartColor.Transparent);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
            await File.WriteAllTextAsync(Path.Combine(directory, "negative-" + kind + "-" + unavailable + ".svg"), prepared.ToSvg());
            await File.WriteAllBytesAsync(Path.Combine(directory, "negative-" + kind + "-" + unavailable + ".native.png"), prepared.ToPng());
        }
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), 800, 600);
        var page = session.Page; var point = page.Locator(Point(0, 0));
        Assert.Equal("-1", await point.GetAttributeAsync("tabindex"));
        // The source is beyond its stroke footprint; the minimum eight-unit browser hit rectangle still intersects the clip.
        var position = await point.EvaluateAsync<double[]>("""
            node=>{
              const box=node.getBoundingClientRect(),svg=node.ownerSVGElement;
              for(let parent=node;parent&&parent.ownerSVGElement;parent=parent.parentElement){
                const m=/^url\(#([^)]*)\)$/.exec(parent.getAttribute('clip-path')||'');if(!m)continue;
                const clip=svg.getElementById(m[1]).firstElementChild.getBBox(),matrix=parent.getScreenCTM();
                const a=new DOMPoint(clip.x,clip.y).matrixTransform(matrix),b=new DOMPoint(clip.x+clip.width,clip.y+clip.height).matrixTransform(matrix);
                return [Math.max(a.x+1,Math.min(b.x-1,(box.left+box.right)/2)),Math.max(a.y+1,Math.min(b.y-1,box.bottom-1))];
              }
              throw new Error('Expected native plot clip');
            }
            """);
        Assert.True(await page.EvaluateAsync<bool>("p=>document.elementFromPoint(p[0],p[1])?.closest('[data-cfx-point]')?.dataset.cfxPoint==='0'", position));
        await page.Mouse.MoveAsync(2, 2); await page.Mouse.MoveAsync((float)position[0], (float)position[1]);
        Assert.True(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        await page.Mouse.ClickAsync((float)position[0], (float)position[1]);
        Assert.NotEqual("true", await point.GetAttributeAsync("aria-selected"));
        await MoveAwayAsync(page); await point.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.NotEqual("true", await point.GetAttributeAsync("aria-selected"));
        if (unavailable == "transparent") Assert.True(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        else {
            Assert.NotEqual("paired:0", await page.EvaluateAsync<string>("()=>document.activeElement.dataset.cfxTargetId||''"));
            Assert.Equal("0", await page.Locator(Point(0, 1)).GetAttributeAsync("tabindex"));
        }
        await CaptureAsync(page, kind + "-" + unavailable, position);
        AssertNoConsoleErrors(session);
    }

    private static Chart Create(ChartSeriesKind kind, double first, double second, bool visibleSibling = false, double firstX = .25) {
        var chart = Chart.Create().WithSize(600, 380).WithTitle("Clipped paired observations")
            .WithTheme(VisualTheme.Graphite()).WithLegend(false).WithDataLabels(false)
            .WithXAxisBounds(0, 1).WithYAxisBounds(0, 10).WithLineVisualStyle(ChartLineVisualStyle.Plain());
        chart.Options.ClipMarksToPlot = true;
        var nextFirst = visibleSibling ? 8 : first; var nextSecond = visibleSibling ? 1 : second;
        if (kind == ChartSeriesKind.DifferenceArea) chart.AddSeries(ChartSeries.CreateDifferenceArea(new ChartSeriesId("paired"), "Pair", new[] {
            new ChartDifferencePoint(firstX, first, second), new ChartDifferencePoint(.75, nextFirst, nextSecond)
        }));
        else {
            var ranges = new[] { new ChartRangeBand(firstX, Math.Min(first, second), Math.Max(first, second)),
                new ChartRangeBand(.75, Math.Min(nextFirst, nextSecond), Math.Max(nextFirst, nextSecond)) };
            if (kind == ChartSeriesKind.RangeArea) chart.AddSeries(ChartSeries.CreateRangeArea(new ChartSeriesId("paired"), "Pair", ranges));
            else chart.AddSeries(ChartSeries.CreateRangeBand(new ChartSeriesId("paired"), "Pair", ranges));
        }
        chart.Series[0].ConfigureMarkers(markers => markers.Enabled = false);
        return chart;
    }

    private static async Task AssertPairedReadoutAsync(IPage page, ChartSeriesKind kind, bool negative) {
        Assert.False(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        // The paired extent paints, but its midpoint is outside the explicit plot.
        Assert.True(await page.Locator(".cfx-crosshair").IsHiddenAsync());
        Assert.Null(await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-crosshair"));
        var values = await page.Locator(".cfx-tooltip dd").AllTextContentsAsync();
        if (kind == ChartSeriesKind.DifferenceArea) {
            Assert.Contains("100", values); Assert.Contains("1", values);
            Assert.Equal(negative ? "1" : "100", await page.Locator(Point(0, 0)).GetAttributeAsync("data-cfx-first"));
            Assert.Equal(negative ? "100" : "1", await page.Locator(Point(0, 0)).GetAttributeAsync("data-cfx-second"));
        } else Assert.Contains("1 - 100", values);
    }

    private static async Task CaptureAsync(IPage page, string name, double[] position) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        var state = await page.EvaluateAsync<JsonElement>("""
            ()=>{const root=document.querySelector('.cfx-interactive-chart'),tip=root.querySelector('.cfx-tooltip');return {
              active:document.activeElement.dataset.cfxTargetId||'',tooltipVisible:!tip.hidden,readout:tip.hidden?'':tip.innerText,
              guideHidden:root.querySelector('.cfx-crosshair').hidden,crosshairTarget:root.getAttribute('data-cfx-crosshair'),
              crosshairVisible:!!root.querySelector('.cfx-crosshair')&&!root.querySelector('.cfx-crosshair').hidden,
              points:[...root.querySelectorAll('[data-cfx-role="point"]')].map(n=>({id:n.dataset.cfxTargetId,tabindex:n.getAttribute('tabindex'),selected:n.getAttribute('aria-selected')}))};}
            """);
        await File.WriteAllTextAsync(Path.Combine(directory, "paired-clip-" + name + ".json"), JsonSerializer.Serialize(new { position, state }));
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, "paired-clip-" + name + ".png") });
    }
}
