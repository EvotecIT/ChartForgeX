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

/// <summary>Disconnected paired facts need their own marker or connected span to become interactive paint.</summary>
public sealed class PairedSegmentPaintBrowserTests {
    [Theory]
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
        Assert.Equal("2", await isolated.GetAttributeAsync("data-cfx-source-count"));
        Assert.Equal("4", await isolated.GetAttributeAsync("data-cfx-source-0-index"));
        Assert.Equal("5", await isolated.GetAttributeAsync("data-cfx-source-1-index"));
        Assert.Equal("true", await isolated.GetAttributeAsync("data-cfx-source-0-break"));
        Assert.Equal("true", await isolated.GetAttributeAsync("data-cfx-source-1-break"));
        Assert.Equal(kind == ChartSeriesKind.DifferenceArea ? "8" : "2", await isolated.GetAttributeAsync("data-cfx-source-0-y"));
        Assert.Equal(kind == ChartSeriesKind.DifferenceArea ? "2" : "8", await isolated.GetAttributeAsync("data-cfx-source-1-y"));
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
            .WithTheme(ChartTheme.GraphiteLight()).WithLegend(false).WithDataLabels(false)
            .WithXAxisBounds(0, 4).WithYAxisBounds(0, 10).WithLineVisualStyle(ChartLineVisualStyle.Plain());
        if (kind == ChartSeriesKind.DifferenceArea) {
            chart.AddDifferenceArea("Pair", Enumerable.Range(0, 5).Select(x => new ChartDifferencePoint(x, 8, 2, x is 2 or 3)));
            chart.Series[0].WithColor(boundary ? ChartColor.FromHex("#DF3040") : ChartColor.Transparent).ConfigureDifferenceArea(options => {
                options.PositiveFill = ChartColor.FromHex("#DF3040"); options.PositiveFillOpacity = .6;
            });
        } else {
            var pairs = Enumerable.Range(0, 5).Select(x => new ChartRangeBand(x, 2, 8));
            if (kind == ChartSeriesKind.RangeArea) chart.AddRangeArea("Pair", pairs);
            else chart.AddRangeBand("Pair", pairs);
            foreach (var raw in new[] { 4, 5, 6, 7 }) {
                var point = chart.Series[0].Points[raw];
                chart.Series[0].Points[raw] = new ChartPoint(point.X, point.Y, true);
            }
        }
        chart.Series[0].WithInteractionKey("paired").WithPointColor(2, ChartColor.FromHex("#DF3040"))
            .ConfigureMarkers(options => options.Enabled = markers);
        return chart;
    }

    private static Task<JsonElement> StateAsync(IPage page) => page.EvaluateAsync<JsonElement>("""
        ()=>{const root=document.querySelector('.cfx-interactive-chart'),tip=root.querySelector('.cfx-tooltip');return {
          active:document.activeElement.dataset.cfxTargetId||'',tooltipVisible:!tip.hidden,readout:tip.hidden?'':tip.innerText,
          hover:root.dataset.cfxHoverKey||'',isolatedSelected:root.querySelector('[data-cfx-point="2"]').getAttribute('aria-selected')==='true',
          selections:window.cfxSegmentSelections,
          points:[...root.querySelectorAll('[data-cfx-role="point"]')].map(n=>({id:n.dataset.cfxTargetId,tabindex:n.getAttribute('tabindex'),
            selected:n.getAttribute('aria-selected'),first:n.getAttribute('data-cfx-source-0-y'),second:n.getAttribute('data-cfx-source-1-y'),
            breakBefore:n.getAttribute('data-cfx-source-0-break')}))};}
        """);

    private static async Task CaptureAsync(IPage page, string name, object state) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        await File.WriteAllTextAsync(Path.Combine(directory, "paired-segment-" + name + ".json"), JsonSerializer.Serialize(state));
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, "paired-segment-" + name + ".browser.png") });
    }
}
