using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using HtmlTinkerX;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractiveCrosshairStylesBrowserTests {
    [BrowserTheory]
    [InlineData(false, false, 950, HtmlChartCrosshairLineStyle.Solid)]
    [InlineData(false, true, 340, HtmlChartCrosshairLineStyle.Dashed)]
    [InlineData(true, false, 340, HtmlChartCrosshairLineStyle.Dotted)]
    [InlineData(true, true, 950, HtmlChartCrosshairLineStyle.Solid)]
    public async Task ExplicitPaintPreservesNativeAndInferredReadoutIdentityAndNotifications(bool graphite, bool dark, int width, HtmlChartCrosshairLineStyle lineStyle) {
        if (!Enabled) return;
        var chart = Observation(graphite, dark);
        var showLabel = width == 950;
        await using var session = await OpenAsync(Render(chart, showLabel: showLabel), width, 720);
        var page = session.Page;
        var baseline = await ObserveAsync(page);
        var defaultPaint = await PaintAsync(page);
        var defaultX = defaultPaint[0];
        Assert.Equal(graphite ? "dashed" : "none", defaultX.GetProperty("pattern").GetString());
        if (graphite) {
            Assert.Equal("1px", defaultX.GetProperty("width").GetString());
            Assert.Equal("none", defaultX.GetProperty("shadow").GetString());
        } else {
            Assert.Equal("1px", defaultX.GetProperty("height").GetString());
            Assert.NotEqual("none", defaultX.GetProperty("shadow").GetString());
        }
        await CaptureAsync(session, "crosshair-default-" + Variant(graphite, dark, width), new { baseline, defaultPaint });

        // A fresh document exercises the configured renderer, rather than patching runtime style by hand.
        await page.GotoAsync("about:blank");
        await page.SetContentAsync(Render(chart, crosshair => {
            crosshair.Stroke = ChartColor.FromArgb(128, 37, 99, 235);
            crosshair.StrokeWidth = 2;
            crosshair.LineStyle = lineStyle;
        }, showLabel));
        var configured = await ObserveAsync(page);
        var paint = await PaintAsync(page);
        var expectedStroke = await page.EvaluateAsync<string>("""
            color => {
                const reference=document.createElement('span');
                reference.style.color=color;
                document.body.append(reference);
                const computed=getComputedStyle(reference).color;
                reference.remove();
                return computed;
            }
            """, ChartColor.FromArgb(128, 37, 99, 235).ToCss());
        Assert.Equal(baseline.GetRawText(), configured.GetRawText());
        Assert.Equal(!showLabel, await page.Locator(".cfx-crosshair__label").IsHiddenAsync());
        foreach (var line in paint) {
            Assert.Equal(lineStyle.ToString().ToLowerInvariant(), line.GetProperty("pattern").GetString());
            Assert.Equal("2px", line.GetProperty("width").GetString());
            await AssertCssColorsEqualAsync(page, expectedStroke, line.GetProperty("color").GetString()!);
            Assert.Equal("none", line.GetProperty("shadow").GetString());
            await AssertCssColorsEqualAsync(page, "rgba(0, 0, 0, 0)", line.GetProperty("background").GetString()!);
        }
        await CaptureAsync(session, "crosshair-styled-" + Variant(graphite, dark, width) + "-" + lineStyle, new { configured, paint });
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task StyledLinesDoNotEnableADisabledGuideOrPreventTooltips() {
        if (!Enabled) return;
        var chart = Observation(true, false);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            options.Interaction.Features = ChartInteractionFeatures.Tooltips;
            options.Tooltip.Mode = HtmlChartTooltipMode.Single;
            options.Crosshair.Stroke = ChartColor.FromRgb(37, 99, 235);
            options.Crosshair.StrokeWidth = 2;
            options.Crosshair.LineStyle = HtmlChartCrosshairLineStyle.Dotted;
        }), 950, 720);
        var page = session.Page;
        await MoveToAsync(page, Point(0, 0));
        Assert.Contains("Reading", await TooltipTextAsync(page), StringComparison.Ordinal);
        Assert.True(await page.Locator(".cfx-crosshair").IsHiddenAsync());
        Assert.Null(await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-crosshair"));
        await CaptureAsync(session, "crosshair-styled-disabled", new { TargetId = await page.Locator(Point(0, 0)).GetAttributeAsync("data-cfx-target-id") });
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(true, false)]
    public async Task SparseInvisiblePaintRetainsGuideLabelAndPointerReadout(bool graphite, bool zeroWidth) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Render(Observation(graphite, graphite), crosshair => {
            if (zeroWidth) crosshair.StrokeWidth = 0;
            else crosshair.Stroke = ChartColor.Transparent;
        }), 700, 720);
        var contract = await ObserveAsync(session.Page);
        var paint = await PaintAsync(session.Page);
        foreach (var line in paint) {
            Assert.Equal(graphite ? "dashed" : "solid", line.GetProperty("pattern").GetString());
            Assert.Equal(zeroWidth ? "0px" : "1px", line.GetProperty("width").GetString());
            if (!zeroWidth) await AssertCssColorsEqualAsync(session.Page, "rgba(0, 0, 0, 0)", line.GetProperty("color").GetString()!);
            Assert.Equal("none", line.GetProperty("shadow").GetString());
        }
        Assert.False(await session.Page.Locator(".cfx-crosshair__label").IsHiddenAsync());
        Assert.True(await session.Page.Locator(".cfx-crosshair").EvaluateAsync<bool>("""
            c => { const b=c.getBoundingClientRect(), l=c.querySelector('[data-cfx-crosshair-label]').getBoundingClientRect();
                return l.left>=b.left-1 && l.right<=b.right+1 && l.top>=b.top-1 && l.bottom<=b.bottom+1; }
            """));
        await CaptureAsync(session, "crosshair-sparse-" + (graphite ? "graphite" : "ordinary") + (zeroWidth ? "-zero" : "-transparent"), new { contract, paint });
        AssertNoConsoleErrors(session);
    }

    private static Chart Observation(bool graphite, bool dark) {
        var chart = Chart.Create().WithSize(800, 440).WithLegend(false).WithHeader(false)
            .WithTheme(graphite ? VisualTheme.Graphite() : VisualTheme.Report(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddSeries(ChartSeries.CreateScatter(new ChartSeriesId("reading-source"), "Reading", new[] { new ChartPoint(5, 5) }));
        return chart;
    }

    private static string Render(Chart chart, Action<HtmlChartCrosshairOptions>? configure = null, bool showLabel = true) => chart.ToInteractiveHtmlPage(options => {
        options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Crosshair | ChartInteractionFeatures.SynchronizedCharts;
        options.Interaction.GroupName = "crosshair-style-proof";
        options.Tooltip.Mode = HtmlChartTooltipMode.Single;
        options.Tooltip.Range = HtmlChartTooltipRange.WithinDistance(64);
        options.Crosshair.ShowLabel = showLabel;
        configure?.Invoke(options.Crosshair);
    });

    private static async Task<JsonElement> ObserveAsync(IPage page) {
        await page.Locator(Point(0, 0)).EvaluateAsync("node => { const stage=node.closest('.cfx-stage'), mark=node.getBoundingClientRect(), viewport=stage.getBoundingClientRect(); stage.scrollLeft+=mark.x+mark.width/2-viewport.x-viewport.width/2; }");
        await MoveAwayAsync(page);
        await page.EvaluateAsync("""
            () => {
                window.crosshairStyleTrace=[];
                const root=document.querySelector('.cfx-interactive-chart');
                for(const name of ['cfxhover','cfxcrosshair','cfxsync']) root.addEventListener(name,event=>{
                    const detail=event.detail, target=detail.target;
                    if(target) window.crosshairStyleTrace.push({name,action:detail.action||'',kind:target.targetKind,id:target.targetId});
                });
            }
            """);
        // Acquire the native mark directly; intermediate approach positions can legitimately acquire an inferred guide.
        var nativeBox = await BoxAsync(page, Point(0, 0));
        await page.Mouse.MoveAsync((float)(nativeBox.X + nativeBox.Width / 2), (float)(nativeBox.Y + nativeBox.Height / 2));
        var native = await ReadContractAsync(page);
        Assert.Equal("reading-source:0", native.GetProperty("guideId").GetString());
        Assert.Equal("reading-source:0", native.GetProperty("hoverId").GetString());
        Assert.Contains("Reading", native.GetProperty("tooltip").GetString()!, StringComparison.Ordinal);
        await MoveToAsync(page, Point(0, 0), offsetX: 1);
        Assert.Equal(native.GetRawText(), (await ReadContractAsync(page)).GetRawText());
        await MoveToAsync(page, Point(0, 0), offsetY: 24);
        var inferred = await ReadContractAsync(page);
        Assert.Equal("reading-source:0", inferred.GetProperty("guideId").GetString());
        Assert.Equal(native.GetProperty("tooltip").GetString(), inferred.GetProperty("tooltip").GetString());
        Assert.False(await page.Locator(".cfx-crosshair").IsHiddenAsync());
        var guideEvents = inferred.GetProperty("events").EnumerateArray().Where(entry => entry.GetProperty("name").GetString() == "cfxcrosshair").ToArray();
        Assert.Equal(2, guideEvents.Length);
        Assert.All(guideEvents, entry => Assert.Equal("reading-source:0", entry.GetProperty("id").GetString()));
        Assert.Contains(inferred.GetProperty("events").EnumerateArray(), entry => entry.GetProperty("name").GetString() == "cfxsync" && entry.GetProperty("action").GetString() == "crosshair");
        return JsonSerializer.SerializeToElement(new { native, inferred });
    }

    private static Task<JsonElement> ReadContractAsync(IPage page) => page.EvaluateAsync<JsonElement>("""
        () => {
            const root=document.querySelector('.cfx-interactive-chart');
            return {guideId:(root.dataset.cfxCrosshair||'').split('|')[1],hoverId:(root.dataset.cfxHoverKey||'').split('|')[1],
                label:root.querySelector('.cfx-crosshair__label').textContent,tooltip:root.querySelector('.cfx-tooltip').innerText,
                events:window.crosshairStyleTrace.slice()};
        }
        """);

    private static async Task<JsonElement[]> PaintAsync(IPage page) {
        using var paint = JsonDocument.Parse(await page.Locator(".cfx-crosshair__line").EvaluateAllAsync<string>("""
        lines => JSON.stringify(lines.map((line,index)=>{
            const style=getComputedStyle(line);
            return {pattern:index?style.borderLeftStyle:style.borderTopStyle,width:index?style.borderLeftWidth:style.borderTopWidth,
                color:index?style.borderLeftColor:style.borderTopColor,shadow:style.boxShadow,background:style.backgroundColor,height:style.height};
        }))
        """));
        return paint.RootElement.EnumerateArray().Select(line => line.Clone()).ToArray();
    }

    private static string Variant(bool graphite, bool dark, int width) => (graphite ? "graphite" : "ordinary") + (dark ? "-dark-" : "-light-") + width;

    private static async Task CaptureAsync(HtmlBrowserSession session, string name, object observations) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".json"), JsonSerializer.Serialize(new { observations, Console = session.ConsoleLog.Select(entry => entry.Type.ToString()) }));
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png") });
    }
}
