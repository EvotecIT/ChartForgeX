using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Paired facts need their own marker, boundary ink or connected fill to become interactive paint.</summary>
public sealed class PairedSegmentPaintBrowserTests {
    [BrowserTheory]
    [InlineData(ChartSeriesKind.DifferenceArea, "opposite")]
    [InlineData(ChartSeriesKind.DifferenceArea, "opposite-reversed")]
    [InlineData(ChartSeriesKind.RangeBand, "opposite")]
    [InlineData(ChartSeriesKind.RangeArea, "opposite")]
    [InlineData(ChartSeriesKind.DifferenceArea, "connected-transparent")]
    [InlineData(ChartSeriesKind.RangeBand, "connected-transparent")]
    [InlineData(ChartSeriesKind.RangeArea, "connected-transparent")]
    [InlineData(ChartSeriesKind.DifferenceArea, "connected-fill")]
    [InlineData(ChartSeriesKind.RangeBand, "connected-fill")]
    [InlineData(ChartSeriesKind.DifferenceArea, "clipped-cap")]
    [InlineData(ChartSeriesKind.RangeBand, "clipped-cap")]
    [InlineData(ChartSeriesKind.RangeArea, "clipped-cap")]
    public async Task ClippedBoundariesRequireLocalInkRatherThanAnotherSpansStroke(ChartSeriesKind kind, string scenario) {
        if (!Enabled) return;
        var connected = scenario.StartsWith("connected", StringComparison.Ordinal);
        var cap = scenario == "clipped-cap";
        var first = cap ? 10.02 : scenario == "opposite-reversed" ? 12 : -2;
        var second = cap ? 10.04 : scenario == "opposite-reversed" ? -2 : 12;
        var chart = Create(kind, false, true);
        chart.Series[0].WithStrokeWidth(6);
        if (kind == ChartSeriesKind.DifferenceArea) chart.Series[0].UpdateDifferencePoint(2, new ChartDifferencePoint(2, first, second, !connected));
        else chart.Series[0].UpdateRange(2, new ChartRangeBand(2, first, second, !connected));
        if (connected) {
            if (kind == ChartSeriesKind.DifferenceArea) chart.Series[0].UpdateDifferencePoint(3, new ChartDifferencePoint(3, 8, 2));
            else chart.Series[0].UpdateRange(3, new ChartRangeBand(3, 2, 8));
        }
        var transparent = scenario == "connected-transparent";
        if (transparent && kind == ChartSeriesKind.DifferenceArea) chart.Series[0].ConfigureDifferenceArea(options => {
            options.PositiveFillOpacity = 0; options.NegativeFillOpacity = 0;
        });
        var html = chart.ToInteractiveHtmlPage();
        if (transparent && kind != ChartSeriesKind.DifferenceArea)
            html = html.Replace("</head>", "<style>[data-cfx-role=range-area],[data-cfx-role=range-band]{fill-opacity:0}[data-cfx-role=range-midline]{stroke-dasharray:none}</style></head>", StringComparison.Ordinal);
        var name = "local-boundary-" + kind + "-" + scenario;
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
            await File.WriteAllTextAsync(Path.Combine(directory, name + ".svg"), prepared.ToSvg());
            await File.WriteAllBytesAsync(Path.Combine(directory, name + ".native.png"), prepared.ToPng());
            await File.WriteAllTextAsync(Path.Combine(directory, name + ".html"), html);
        }
        await using var session = await OpenAsync(html, 800, 600);
        var page = session.Page; var point = page.Locator(Point(0, 2));
        await page.EvaluateAsync("()=>{window.cfxSegmentSelections=[];document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect',event=>{if(event.detail.target?.targetKind==='point')window.cfxSegmentSelections.push(event.detail.target.targetId);});}");
        var position = await point.EvaluateAsync<double[]>("""
            node=>{const box=node.getBoundingClientRect();let x=(box.left+box.right)/2,y=(box.top+box.bottom)/2;
              for(let p=node;p&&p.ownerSVGElement;p=p.parentElement){const match=/^url\(#([^)]*)\)$/.exec(p.getAttribute('clip-path')||'');if(!match)continue;
                const r=p.ownerSVGElement.getElementById(match[1]).firstElementChild.getBBox(),m=p.getScreenCTM();
                const a=new DOMPoint(r.x,r.y).matrixTransform(m),b=new DOMPoint(r.x+r.width,r.y+r.height).matrixTransform(m);
                x=Math.max(a.x+1,Math.min(b.x-1,x));y=Math.max(a.y+1,Math.min(b.y-1,y));}
              return [x,y];}
            """);
        Assert.True(await page.EvaluateAsync<bool>("p=>document.elementFromPoint(p[0],p[1])?.closest('[data-cfx-point]')?.dataset.cfxPoint==='2'", position));
        await page.Mouse.MoveAsync(2, 2); await page.Mouse.MoveAsync((float)position[0], (float)position[1]);
        var pointer = await StateAsync(page);
        await page.Mouse.ClickAsync((float)position[0], (float)position[1]); var clicked = await StateAsync(page);
        await CaptureAsync(page, name + "-pointer", new { position, pointer, clicked });
        await MoveAwayAsync(page); await page.Locator("[data-cfx-reset]").FocusAsync(); await point.FocusAsync();
        var focused = await StateAsync(page); await page.Keyboard.PressAsync("Space"); var keyboard = await StateAsync(page);
        await CaptureAsync(page, name + "-keyboard", new { position, focused, keyboard });
        var available = kind == ChartSeriesKind.RangeArea || cap || scenario == "connected-fill";
        Assert.Equal(available, pointer.GetProperty("tooltipVisible").GetBoolean());
        Assert.Equal(available, clicked.GetProperty("isolatedSelected").GetBoolean());
        Assert.Equal(available, focused.GetProperty("active").GetString() == "paired:2");
        Assert.False(keyboard.GetProperty("isolatedSelected").GetBoolean());
        if (available) {
            Assert.True(focused.GetProperty("tooltipVisible").GetBoolean());
            Assert.Contains(first.ToString(System.Globalization.CultureInfo.InvariantCulture), pointer.GetProperty("readout").GetString());
            Assert.Contains(second.ToString(System.Globalization.CultureInfo.InvariantCulture), pointer.GetProperty("readout").GetString());
            Assert.Equal(new[] { "paired:2", "paired:2" }, keyboard.GetProperty("selections").EnumerateArray().Select(id => id.GetString()));
        } else Assert.DoesNotContain(keyboard.GetProperty("selections").EnumerateArray(), id => id.GetString() == "paired:2");
        Assert.Equal(first, double.Parse((await point.GetAttributeAsync(kind == ChartSeriesKind.DifferenceArea ? "data-cfx-first" : "data-cfx-lower"))!, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(second, double.Parse((await point.GetAttributeAsync(kind == ChartSeriesKind.DifferenceArea ? "data-cfx-second" : "data-cfx-upper"))!, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal("1", await point.GetAttributeAsync("data-cfx-source-count"));
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(ChartSeriesKind.DifferenceArea, "none")]
    [InlineData(ChartSeriesKind.RangeArea, "none")]
    [InlineData(ChartSeriesKind.RangeBand, "none")]
    [InlineData(ChartSeriesKind.DifferenceArea, "markers")]
    [InlineData(ChartSeriesKind.RangeArea, "markers")]
    [InlineData(ChartSeriesKind.RangeBand, "markers")]
    [InlineData(ChartSeriesKind.DifferenceArea, "boundary")]
    [InlineData(ChartSeriesKind.RangeArea, "boundary")]
    [InlineData(ChartSeriesKind.RangeBand, "boundary")]
    public async Task IsolatedPairCannotBorrowDisconnectedFillButKeepsItsOwnInk(ChartSeriesKind kind, string ink) {
        if (!Enabled) return;
        var markers = ink == "markers"; var available = ink != "none";
        var chart = Create(kind, markers, ink == "boundary");
        var name = kind + "-" + ink;
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
            await File.WriteAllTextAsync(Path.Combine(directory, "paired-segment-" + name + ".svg"), prepared.ToSvg());
            await File.WriteAllBytesAsync(Path.Combine(directory, "paired-segment-" + name + ".native.png"), prepared.ToPng());
        }
        var html = chart.ToInteractiveHtmlPage();
        if (kind != ChartSeriesKind.DifferenceArea && ink != "boundary") {
            // A public host stylesheet can hide boundary ink while retaining the independently painted envelope.
            html = html.Replace("</head>", "<style>[data-cfx-role=range-upper],[data-cfx-role=range-lower],[data-cfx-role=range-midline]{stroke:transparent}</style></head>", StringComparison.Ordinal);
        }
        if (!string.IsNullOrWhiteSpace(directory)) await File.WriteAllTextAsync(Path.Combine(directory, "paired-segment-" + name + ".html"), html);
        await using var session = await OpenAsync(html, 800, 600);
        var page = session.Page; var isolated = page.Locator(Point(0, 2));
        await page.EvaluateAsync("""
            ()=>{window.cfxSegmentSelections=[];document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect',event=>{
              if(event.detail.target?.targetKind==='point')window.cfxSegmentSelections.push(event.detail.target.targetId);});}
            """);
        // The same series has real filled spans on both sides of the isolated observation.
        await MoveToAsync(page, Point(0, 0));
        Assert.False(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        Assert.Contains("Pair", await TooltipTextAsync(page));
        Assert.Equal("paired:0", await page.Locator(Point(0, 0)).GetAttributeAsync("data-cfx-target-id"));
        await MoveAwayAsync(page);
        await MoveToAsync(page, Point(0, 2));
        var pointer = await StateAsync(page);
        var box = await isolated.BoundingBoxAsync() ?? throw new InvalidOperationException("Missing isolated paired hit area");
        if (markers) await isolated.Locator("[data-cfx-role$='-marker']").First.ClickAsync();
        else await page.Mouse.ClickAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        var clicked = await StateAsync(page);
        await CaptureAsync(page, name + "-pointer", new { pointer, clicked });
        await MoveAwayAsync(page); await page.Locator("[data-cfx-reset]").FocusAsync(); await isolated.FocusAsync();
        var focused = await StateAsync(page);
        await page.Keyboard.PressAsync("Space");
        var keyboard = await StateAsync(page);
        await CaptureAsync(page, name + "-keyboard", new { focused, keyboard });
        Assert.Equal("1", await isolated.GetAttributeAsync("data-cfx-source-count"));
        Assert.Equal("2", await isolated.GetAttributeAsync("data-cfx-source-point"));
        Assert.Equal("true", await isolated.GetAttributeAsync("data-cfx-break-before"));
        Assert.Equal(kind == ChartSeriesKind.DifferenceArea ? "8" : "2", await isolated.GetAttributeAsync(kind == ChartSeriesKind.DifferenceArea ? "data-cfx-first" : "data-cfx-lower"));
        Assert.Equal(kind == ChartSeriesKind.DifferenceArea ? "2" : "8", await isolated.GetAttributeAsync(kind == ChartSeriesKind.DifferenceArea ? "data-cfx-second" : "data-cfx-upper"));
        Assert.Equal(available, pointer.GetProperty("tooltipVisible").GetBoolean());
        Assert.Equal(available, clicked.GetProperty("isolatedSelected").GetBoolean());
        if (available) {
            Assert.Equal("paired:2", keyboard.GetProperty("active").GetString());
            Assert.True(focused.GetProperty("tooltipVisible").GetBoolean());
            Assert.False(keyboard.GetProperty("isolatedSelected").GetBoolean());
        } else {
            Assert.NotEqual("paired:2", keyboard.GetProperty("active").GetString());
            Assert.DoesNotContain(keyboard.GetProperty("selections").EnumerateArray(), id => id.GetString() == "paired:2");
            Assert.False(keyboard.GetProperty("isolatedSelected").GetBoolean());
        }
        AssertNoConsoleErrors(session);
    }

    private static Chart Create(ChartSeriesKind kind, bool markers, bool boundary) {
        var chart = Chart.Create().WithSize(600, 380).WithTitle("Disconnected paired observations")
            .WithTheme(VisualTheme.Graphite()).WithLegend(false).WithDataLabels(false)
            .WithXAxisBounds(0, 4).WithYAxisBounds(0, 10).WithLineVisualStyle(ChartLineVisualStyle.Plain());
        if (kind == ChartSeriesKind.DifferenceArea) {
            chart.AddSeries(ChartSeries.CreateDifferenceArea(new ChartSeriesId("paired"), "Pair", Enumerable.Range(0, 5).Select(x => new ChartDifferencePoint(x, 8, 2, x is 2 or 3))));
            chart.Series[0].WithColor(boundary ? ChartColor.FromHex("#DF3040") : ChartColor.Transparent).ConfigureDifferenceArea(options => {
                options.PositiveFill = ChartColor.FromHex("#DF3040"); options.PositiveFillOpacity = .6;
            });
        } else {
            var pairs = Enumerable.Range(0, 5).Select(x => new ChartRangeBand(x, 2, 8, x is 2 or 3));
            if (kind == ChartSeriesKind.RangeArea) chart.AddSeries(ChartSeries.CreateRangeArea(new ChartSeriesId("paired"), "Pair", pairs));
            else chart.AddSeries(ChartSeries.CreateRangeBand(new ChartSeriesId("paired"), "Pair", pairs));
        }
        chart.Series[0].WithPointColor(2, ChartColor.FromHex("#DF3040"))
            .ConfigureMarkers(options => options.Enabled = markers);
        return chart;
    }

    private static Task<JsonElement> StateAsync(IPage page) => page.EvaluateAsync<JsonElement>("""
        ()=>{const root=document.querySelector('.cfx-interactive-chart'),tip=root.querySelector('.cfx-tooltip');return {
          active:document.activeElement.dataset.cfxTargetId||'',tooltipVisible:!tip.hidden,readout:tip.hidden?'':tip.innerText,
          hover:root.dataset.cfxHoverKey||'',crosshairVisible:!!root.querySelector('.cfx-crosshair')&&!root.querySelector('.cfx-crosshair').hidden,isolatedSelected:root.querySelector('[data-cfx-point="2"]').getAttribute('aria-selected')==='true',
          selections:window.cfxSegmentSelections,
          points:[...root.querySelectorAll('[data-cfx-role="point"]')].map(n=>({id:n.dataset.cfxTargetId,tabindex:n.getAttribute('tabindex'),
            selected:n.getAttribute('aria-selected'),first:n.getAttribute('data-cfx-first')??n.getAttribute('data-cfx-lower'),second:n.getAttribute('data-cfx-second')??n.getAttribute('data-cfx-upper'),
            breakBefore:n.getAttribute('data-cfx-break-before')}))};}
        """);

    private static async Task CaptureAsync(IPage page, string name, object state) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        await File.WriteAllTextAsync(Path.Combine(directory, "paired-segment-" + name + ".json"), JsonSerializer.Serialize(state));
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, "paired-segment-" + name + ".browser.png") });
    }
}
