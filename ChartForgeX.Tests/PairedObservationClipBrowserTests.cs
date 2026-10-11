using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Protects paired observations whose native extent, rather than its midpoint, intersects the plot.</summary>
public sealed class PairedObservationClipBrowserTests {
    [Theory]
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
        Assert.Equal("2", await point.GetAttributeAsync("data-cfx-source-count"));
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

    [Theory]
    [InlineData(ChartSeriesKind.DifferenceArea, "y-clipped")]
    [InlineData(ChartSeriesKind.RangeArea, "y-clipped")]
    [InlineData(ChartSeriesKind.RangeBand, "y-clipped")]
    [InlineData(ChartSeriesKind.DifferenceArea, "x-clipped")]
    [InlineData(ChartSeriesKind.DifferenceArea, "transparent")]
    [InlineData(ChartSeriesKind.RangeArea, "transparent")]
    [InlineData(ChartSeriesKind.RangeBand, "transparent")]
    public async Task UnpaintedPairedExtentCannotBorrowAnotherObservationsPaint(ChartSeriesKind kind, string unavailable) {
        if (!Enabled) return;
        var chart = unavailable == "y-clipped" ? Create(kind, 10.02, 10.01, visibleSibling: true)
            : Create(kind, 100, 1, visibleSibling: unavailable == "x-clipped", firstX: unavailable == "x-clipped" ? -.001 : .25);
        if (unavailable == "transparent") chart.Series[0].WithColor(ChartColor.Transparent);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), 800, 600);
        var page = session.Page; var point = page.Locator(Point(0, 0));
        Assert.Equal("-1", await point.GetAttributeAsync("tabindex"));
        // The browser's minimum eight-unit hit rectangle can intersect the clip even when the captured extent does not.
        var position = await point.EvaluateAsync<double[]>("""
            node=>{
              const box=node.getBoundingClientRect(),svg=node.ownerSVGElement;
              for(let parent=node;parent&&parent.ownerSVGElement;parent=parent.parentElement){
                const m=/^url\(#([^)]*)\)$/.exec(parent.getAttribute('clip-path')||'');if(!m)continue;
                const clip=svg.getElementById(m[1]).firstElementChild.getBBox(),matrix=parent.getScreenCTM();
                const a=new DOMPoint(clip.x,clip.y).matrixTransform(matrix),b=new DOMPoint(clip.x+clip.width,clip.y+clip.height).matrixTransform(matrix);
                return [Math.max(a.x+.1,Math.min(b.x-.1,(box.left+box.right)/2)),Math.max(a.y+.1,Math.min(b.y-.1,box.bottom-1))];
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
            .WithTheme(ChartTheme.GraphiteLight()).WithLegend(false).WithDataLabels(false)
            .WithXAxisBounds(0, 1).WithYAxisBounds(0, 10).WithLineVisualStyle(ChartLineVisualStyle.Plain());
        chart.Options.ClipMarksToPlot = true;
        var nextFirst = visibleSibling ? 8 : first; var nextSecond = visibleSibling ? 1 : second;
        if (kind == ChartSeriesKind.DifferenceArea) chart.AddDifferenceArea("Pair", new[] {
            new ChartDifferencePoint(firstX, first, second), new ChartDifferencePoint(.75, nextFirst, nextSecond)
        });
        else {
            var ranges = new[] { new ChartRangeBand(firstX, Math.Min(first, second), Math.Max(first, second)),
                new ChartRangeBand(.75, Math.Min(nextFirst, nextSecond), Math.Max(nextFirst, nextSecond)) };
            if (kind == ChartSeriesKind.RangeArea) chart.AddRangeArea("Pair", ranges);
            else chart.AddRangeBand("Pair", ranges);
        }
        chart.Series[0].WithInteractionKey("paired").ConfigureMarkers(markers => markers.Enabled = false);
        return chart;
    }

    private static async Task AssertPairedReadoutAsync(IPage page, ChartSeriesKind kind, bool negative) {
        Assert.False(await page.Locator(".cfx-tooltip").IsHiddenAsync());
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
              points:[...root.querySelectorAll('[data-cfx-role="point"]')].map(n=>({id:n.dataset.cfxTargetId,tabindex:n.getAttribute('tabindex'),selected:n.getAttribute('aria-selected')}))};}
            """);
        await File.WriteAllTextAsync(Path.Combine(directory, "paired-clip-" + name + ".json"), JsonSerializer.Serialize(new { position, state }));
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, "paired-clip-" + name + ".png") });
    }
}
