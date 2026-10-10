using System.Globalization;
using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveOverlayCoordinatesBrowserTests {
    [Theory]
    [InlineData(false, 0.8, false)]
    [InlineData(true, 0.8, true)]
    [InlineData(false, 1, true)]
    public async Task KeyboardNavigationRevealsNativeMarksThroughScaledHorizontalAndVerticalStageScroll(bool graphite, double scale, bool verticalScroll) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(596, 338).WithTitle("Keyboard reveal coordinates").WithLegend(false)
            .WithTheme(graphite ? ChartTheme.GraphiteLight() : ChartTheme.Light())
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddScatter("Reading", new[] { new ChartPoint(1, 9), new ChartPoint(5, 5), new ChartPoint(9, 1) })
            .AddScatter("Secondary", new[] { new ChartPoint(1, 1), new ChartPoint(5, 1), new ChartPoint(9, 9) });
        chart.Series[0].WithInteractionKey("reading-source");
        chart.Series[1].WithInteractionKey("secondary-source");
        var html = Host(graphite, HtmlChartResponsiveLayout.Readable, 340, scale, verticalScroll,
            ChartInteractionFeatures.KeyboardNavigation | ChartInteractionFeatures.Tooltips, chart)
            .Replace("<main id='host'", "<button id='before-chart'>Before chart</button><main id='host'", StringComparison.Ordinal);
        if (verticalScroll) html = html.Replace("</head>", "<style>#host .cfx-stage{height:170px;overflow-y:auto;border:5px solid #334155;padding:18px}</style></head>", StringComparison.Ordinal);
        await using var session = await OpenAsync(html, 950, 760);
        var page = session.Page;
        if (verticalScroll) await page.EvaluateAsync("() => window.scrollTo(0,150)");
        await page.Locator(Root).EvaluateAsync("r => { window.keyboardRevealEvents=[]; r.addEventListener('cfxnavigate', e => window.keyboardRevealEvents.push(e.detail)); }");
        await page.Locator("#before-chart").FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        Assert.True(await page.Locator(Point(0, 0)).EvaluateAsync<bool>("n => n === document.activeElement"));
        var measurements = new List<JsonElement>();
        foreach (var key in new[] { "End", "Home", "ArrowRight", "ArrowDown", "ArrowUp" }) {
            await page.Keyboard.PressAsync(key);
            var geometry = await KeyboardRevealGeometryAsync(page, scale);
            measurements.Add(geometry);
            if (key is "End" or "ArrowDown") await CaptureAsync(session,
                "keyboard-reveal-" + (graphite ? "graphite" : "generic") + "-scale" + scale.ToString(CultureInfo.InvariantCulture) + (verticalScroll ? "-xy" : "-x") + "-" + key,
                new { key, geometry, events = await page.EvaluateAsync<JsonElement>("() => window.keyboardRevealEvents") });
        }
        var events = await page.EvaluateAsync<JsonElement>("() => window.keyboardRevealEvents");
        Assert.Equal(new[] { "End", "Home", "ArrowRight", "ArrowDown", "ArrowUp" }, events.EnumerateArray().Select(e => e.GetProperty("key").GetString()));
        Assert.Equal(new[] { "reading-source:2", "reading-source:0", "reading-source:1", "secondary-source:1", "reading-source:1" }, measurements.Select(g => g.GetProperty("targetId").GetString()));
        foreach (var geometry in measurements) AssertKeyboardMarkVisible(geometry);
        Assert.True(measurements[0].GetProperty("scrollLeft").GetDouble() > 0);
        if (verticalScroll) {
            Assert.True(measurements[0].GetProperty("scrollTop").GetDouble() > 0);
            Assert.True(measurements[1].GetProperty("scrollTop").GetDouble() < measurements[0].GetProperty("scrollTop").GetDouble());
            Assert.Equal(5, measurements[0].GetProperty("border").GetDouble());
        }
        AssertNoConsoleErrors(session);
    }

    private static Task<JsonElement> KeyboardRevealGeometryAsync(IPage page, double scale) => page.Locator(Root).EvaluateAsync<JsonElement>("""
        (r,scale) => {
            const s=r.querySelector('.cfx-stage'), b=s.getBoundingClientRect(), n=document.activeElement, mark=n.getBoundingClientRect();
            const left=b.left+s.clientLeft*scale, top=b.top+s.clientTop*scale, margin=8*scale;
            return {targetId:n.closest('[data-cfx-target-id]').dataset.cfxTargetId,
                mark:{left:mark.left,right:mark.right,top:mark.top,bottom:mark.bottom},
                viewport:{left:left+margin,right:left+s.clientWidth*scale-margin,top:top+margin,bottom:top+s.clientHeight*scale-margin},
                paintedHit:n.contains(document.elementFromPoint((mark.left+mark.right)/2,(mark.top+mark.bottom)/2)),
                scrollLeft:s.scrollLeft,scrollTop:s.scrollTop,border:s.clientLeft,
                clientWidth:s.clientWidth,clientHeight:s.clientHeight,scrollWidth:s.scrollWidth,scrollHeight:s.scrollHeight,
                pageScroll:[window.scrollX,window.scrollY]};
        }
        """, scale);

    private static void AssertKeyboardMarkVisible(JsonElement geometry) {
        var mark = geometry.GetProperty("mark");
        var viewport = geometry.GetProperty("viewport");
        Assert.True(geometry.GetProperty("paintedHit").GetBoolean(), geometry.ToString());
        Assert.True(mark.GetProperty("left").GetDouble() >= viewport.GetProperty("left").GetDouble() - 1, geometry.ToString());
        Assert.True(mark.GetProperty("right").GetDouble() <= viewport.GetProperty("right").GetDouble() + 1, geometry.ToString());
        Assert.True(mark.GetProperty("top").GetDouble() >= viewport.GetProperty("top").GetDouble() - 1, geometry.ToString());
        Assert.True(mark.GetProperty("bottom").GetDouble() <= viewport.GetProperty("bottom").GetDouble() + 1, geometry.ToString());
    }
}
