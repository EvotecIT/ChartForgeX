using System.Globalization;
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

public sealed class InteractiveCrosshairTickCaptionBrowserTests {
    private static readonly DateTime Start = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    // The value pills sit on the axes they read, where the tick captions are drawn. A caption the pill
    // touches must not show around it, and every caption returns once the guide leaves.
    [BrowserTheory]
    [InlineData("daily-bars", 0, false, 950)]
    [InlineData("daily-bars", -45, true, 420)]
    [InlineData("time-line", 0, true, 950)]
    [InlineData("time-line", 0, false, 420)]
    public async Task ValuePillsHideOnlyTheTickCaptionsTheyCoverUntilTheGuideLeaves(string shape, double labelAngle, bool dark, int width) {
        if (!Enabled) return;
        var chart = shape == "daily-bars" ? DailyBars(labelAngle, dark) : TimeLine(dark);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Crosshair;
        }), width, 560);
        var page = session.Page;
        Assert.Equal(0, (await ObserveAsync(page)).GetProperty("hidden").GetArrayLength());

        // Hover the observation aligned with a middle x caption, so the x pill lands on that caption.
        var aligned = await page.EvaluateAsync<JsonElement>("""
            () => {
                const captions=[...document.querySelectorAll('[data-cfx-role="axis-x-label"]')].filter(node=>node.getBoundingClientRect().width);
                const caption=captions[Math.floor(captions.length/2)], box=caption.getBoundingClientRect(), x=box.left+box.width/2;
                const centre=node=>{const r=node.getBoundingClientRect();return {x:r.left+r.width/2,y:r.top+r.height/2};};
                const point=[...document.querySelectorAll('[data-cfx-point]')].map(centre).sort((a,b)=>Math.abs(a.x-x)-Math.abs(b.x-x))[0];
                return {caption:caption.textContent,x:point.x,y:point.y};
            }
            """);
        await page.Mouse.MoveAsync((float)aligned.GetProperty("x").GetDouble(), (float)aligned.GetProperty("y").GetDouble(), new MouseMoveOptions { Steps = 4 });
        await SettlePresentationAsync(page);
        var hovered = await ObserveAsync(page);
        Assert.Equal(2, hovered.GetProperty("pills").GetInt32());
        Assert.Empty(hovered.GetProperty("collisions").EnumerateArray());
        Assert.Contains(aligned.GetProperty("caption").GetString(), hovered.GetProperty("hidden").EnumerateArray().Select(caption => caption.GetString()));
        Assert.True(hovered.GetProperty("visible").GetInt32() > hovered.GetProperty("hidden").GetArrayLength());
        await CaptureAsync(session, "crosshair-ticks-" + shape + (dark ? "-dark-" : "-light-") + width, hovered);

        await MoveAwayAsync(page);
        var left = await ObserveAsync(page);
        Assert.Equal(0, left.GetProperty("pills").GetInt32());
        Assert.Equal(0, left.GetProperty("hidden").GetArrayLength());
        AssertNoConsoleErrors(session);
    }

    private static Task<JsonElement> ObserveAsync(IPage page) => page.EvaluateAsync<JsonElement>("""
        () => {
            const root=document.querySelector('.cfx-interactive-chart');
            const pills=[...root.querySelectorAll('.cfx-crosshair__axis-value')].map(node=>node.getBoundingClientRect()).filter(box=>box.width&&box.height);
            const captions=[...root.querySelectorAll('[data-cfx-role^="axis-"][data-cfx-role$="-label"]')]
                .filter(node=>!node.dataset.cfxRole.includes('title')).map(node=>({node,box:node.getBoundingClientRect()})).filter(item=>item.box.width||item.box.height);
            const shown=captions.filter(item=>getComputedStyle(item.node).visibility==='visible');
            return {pills:pills.length,visible:shown.length,
                hidden:captions.filter(item=>!shown.includes(item)).map(item=>item.node.textContent),
                collisions:shown.filter(({box})=>pills.some(pill=>box.left<pill.right&&box.right>pill.left&&box.top<pill.bottom&&box.bottom>pill.top)).map(item=>item.node.textContent)};
        }
        """);

    private static Chart DailyBars(double labelAngle, bool dark) {
        var chart = Frame(dark);
        for (var day = 0; day < 90; day++)
            chart.Options.XAxisLabels.Add(new ChartAxisLabel(day + 1, Start.AddDays(day).ToString("d MMM", CultureInfo.InvariantCulture)));
        if (labelAngle != 0) chart.Options.XAxis.LabelAngle = labelAngle;
        return chart.AddSeries(ChartSeries.CreateBar(new ChartSeriesId("changes"), "Changes",
            Enumerable.Range(0, 90).Select(day => new ChartPoint(day + 1, 8 + day * 7 % 30))));
    }

    private static Chart TimeLine(bool dark) => Frame(dark).WithXAxisTimeScale().AddSeries(ChartSeries.CreateLine(new ChartSeriesId("latency"), "Latency",
        Enumerable.Range(0, 90).Select(step => new ChartPoint(Start.AddHours(step * 8), 20 + step * 11 % 30))));

    private static Chart Frame(bool dark) => Chart.Create().WithSize(900, 380).WithLegend(false).WithHeader(false)
        .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);

    private static async Task CaptureAsync(HtmlBrowserSession session, string name, JsonElement observation) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".json"), observation.GetRawText());
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png") });
    }
}
