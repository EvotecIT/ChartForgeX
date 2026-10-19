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

public sealed partial class InteractiveLegendOverflowBrowserTests {
    private const string Panel = ".cfx-legend-overflow";
    private const string Rows = Panel + " .cfx-legend-overflow__entry";
    private const string Omitted = ".cfx-stage svg [data-cfx-role=legend-entry-omitted]";

    [BrowserTheory]
    [Trait("BrowserSuite", "Compatibility")]
    [InlineData(false, 360)]
    [InlineData(true, 980)]
    public async Task OmittedLegendEntriesExposeEveryFactAndCanonicalActionsWithoutChangingNativeExports(bool dark, int width) {
        if (!Enabled) return;
        void Configure(HtmlChartInteractionOptions options) {
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Interaction.Enable(ChartInteractionFeatures.Export | ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.Scenarios);
            options.Interaction.AddScenario("route", "Selected route", scenario => scenario
                .WithColor(dark ? "#22C55E" : "#F97316").WithDescription("Review the measured series.").WithPlayback(60000)
                .AddSeriesStep("series-0", "First measurement").AddSeriesStep("series-1", "Second measurement"));
            options.Localization.Direction = dark ? HtmlChartTextDirection.RightToLeft : HtmlChartTextDirection.LeftToRight;
            options.Localization[HtmlChartText.LegendMore] = "Dalsze klucze ({count})";
            options.Localization[HtmlChartText.LegendPrevious] = "Poprzednie klucze";
            options.Localization[HtmlChartText.LegendNext] = "Następne klucze";
            options.Localization[HtmlChartText.LegendRange] = "{first}–{last} z {count}";
        }
        var chart = DenseSeries(dark, longLabel: true);
        var staticSvg = chart.ToSvg();
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(Configure), width, 900);
        var page = session.Page;
        Assert.Equal(13, await page.Locator(Omitted).CountAsync());
        Assert.Equal(0, await page.Locator(Omitted + "[tabindex]," + Omitted + "[data-cfx-target-kind]," + Omitted + " [data-cfx-browser-hit-area]").CountAsync());
        Assert.False(await page.Locator(Panel).EvaluateAsync<bool>("node=>node.open"));
        await page.Locator(Panel + " summary").ClickAsync();
        Assert.Contains(new string('W', 80), await page.Locator(Rows).First.InnerTextAsync());
        Assert.True(await page.Locator(Rows + " [data-cfx-legend-action=mute]").First.EvaluateAsync<bool>("""
            button=>{
                const span=button.querySelector('span'),bounds=button.getBoundingClientRect(),range=document.createRange();
                range.selectNodeContents(span);
                return button.scrollWidth<=button.clientWidth+1&&[...range.getClientRects()].every(rect=>
                    rect.left>=bounds.left-1&&rect.right<=bounds.right+1&&rect.top>=bounds.top-1&&rect.bottom<=bounds.bottom+1);
            }
            """));
        await AssertScenarioPanelContrastAsync(page, dark, "normal");
        await AssertReadableControlStatesAsync(page, page.Locator("[data-cfx-zoom=in]"), "normal-toolbar-" + dark);
        await AssertReadableControlStatesAsync(page, page.Locator(".cfx-scenario[aria-pressed=true]").First, "normal-scenario-" + dark);
        await AssertReadableControlStatesAsync(page, page.Locator(Rows + " [data-cfx-legend-action=mute]").First, "normal-legend-" + dark);
        var original = await DownloadLegendSvgAsync(page);
        await page.EvaluateAsync("""
            ()=>{
                window.legendEvents=[];
                document.querySelector('.cfx-interactive-chart').addEventListener('cfxseries',event=>legendEvents.push(event.detail));
            }
            """);
        var seen = new List<int>();
        for (var group = 0; group < 3; group++) {
            seen.AddRange(await page.Locator(Rows).EvaluateAllAsync<int[]>("rows=>rows.map(row=>Number(row.dataset.cfxLegendIndex))"));
            Assert.InRange(await page.Locator(Rows).CountAsync(), 1, 6);
            Assert.Equal(group == 2 ? "13–13 z 13" : group == 0 ? "1–6 z 13" : "7–12 z 13",
                await page.Locator(Panel + " .cfx-legend-overflow__range").InnerTextAsync());
            await CaptureAsync(page, "legend-overflow-" + dark + "-page-" + group);
            if (group < 2) {
                await FocusWithKeyboardAsync(page, page.GetByRole(AriaRole.Button, new() { Name = "Następne klucze", Exact = true }));
                await page.Keyboard.PressAsync("Enter");
                Assert.Equal("mute", await page.EvaluateAsync<string>("()=>document.activeElement.dataset.cfxLegendAction"));
            }
        }
        Assert.Equal(Enumerable.Range(0, 13), seen);
        Assert.Equal(original, await DownloadLegendSvgAsync(page));
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>legendEvents.length"));
        var mute = page.Locator(Rows + " [data-cfx-legend-action=mute]").First;
        await FocusWithKeyboardAsync(page, mute); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await mute.GetAttributeAsync("aria-pressed"));
        await AssertReadableControlAsync(page, mute, "normal-muted-" + dark);
        Assert.Equal("series-12", await page.EvaluateAsync<string>("()=>legendEvents[0].seriesKey"));
        Assert.True(await page.Locator(".cfx-stage [data-cfx-series='12'].cfx-series-muted").CountAsync() > 0);
        var isolate = page.Locator(Rows + " [data-cfx-legend-action=isolate]").First;
        await isolate.ClickAsync();
        Assert.Equal("true", await isolate.GetAttributeAsync("aria-pressed"));
        await AssertReadableControlAsync(page, isolate, "normal-isolated-" + dark);
        var committed = await DownloadLegendSvgAsync(page);
        Assert.Contains("cfx-series-muted", committed, StringComparison.Ordinal);
        Assert.Contains("cfx-series-isolated-in", committed, StringComparison.Ordinal);
        await AssertReadableControlStatesAsync(page, page.Locator("[data-cfx-reset]"), "normal-reset-" + dark);
        await page.Locator("[data-cfx-reset]").ClickAsync();
        Assert.Equal("false", await mute.GetAttributeAsync("aria-pressed"));
        Assert.Equal("false", await isolate.GetAttributeAsync("aria-pressed"));
        Assert.Equal(staticSvg, chart.ToSvg());
        Assert.True(await page.EvaluateAsync<bool>("()=>document.documentElement.scrollWidth<=innerWidth+1"));
        await page.Locator("[data-cfx-legend-page=previous]").ClickAsync();
        await page.Locator("[data-cfx-legend-page=previous]").ClickAsync();
        await page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active, ReducedMotion = ReducedMotion.Reduce });
        await page.EvaluateAsync("()=>document.documentElement.style.zoom='1.5'");
        await AssertScenarioPanelContrastAsync(page, dark, "forced");
        await AssertReadableControlStatesAsync(page, page.Locator("[data-cfx-zoom=in]"), "forced-toolbar-" + dark);
        await AssertReadableControlStatesAsync(page, page.Locator(".cfx-scenario[aria-pressed=true]").First, "forced-scenario-" + dark);
        await AssertReadableControlStatesAsync(page, mute, "forced-legend-" + dark);
        Assert.True(await mute.EvaluateAsync<bool>("node=>node.matches(':focus-visible')"));
        await CaptureAsync(page, "legend-overflow-" + dark + "-forced-zoom");
        await page.EmulateMediaAsync(new() { Media = Media.Print });
        Assert.Equal("none", await page.Locator(Panel).EvaluateAsync<string>("node=>getComputedStyle(node).display"));
        Assert.Contains("legend-entry-omitted", await DownloadLegendSvgAsync(page), StringComparison.Ordinal);
        await CaptureAsync(page, "legend-overflow-" + dark + "-print");
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task ThousandPointKeysMaterializeOneBoundedPageAndRetainEveryObservationIdentity() {
        if (!Enabled) return;
        var series = ChartSeries.CreateBar(new ChartSeriesId("measurements"), "Measurements",
            Enumerable.Range(0, 1000).Select(index => new ChartPoint(index, index % 11)));
        for (var index = 0; index < 1000; index++) series.WithObservationId(index, new ChartObservationId("item-" + index));
        var chart = Chart.Create().WithSize(680, 380).WithLegend().WithPointLegend().WithLegendPosition(ChartLegendPosition.Right)
            .WithLegendBudget(maximumRows: 1).AddSeries(series);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit), 390, 900);
        var page = session.Page;
        await page.Locator(Panel + " summary").ClickAsync();
        var report = await page.EvaluateAsync<JsonElement>("""
            ()=>{
                const panel=document.querySelector('.cfx-legend-overflow'),seen=[];let maximumRows=0;
                do{
                    const rows=[...panel.querySelectorAll('.cfx-legend-overflow__entry')];
                    maximumRows=Math.max(maximumRows,rows.length);seen.push(...rows.map(row=>Number(row.dataset.cfxLegendIndex)));
                    const next=panel.querySelector('[data-cfx-legend-page=next]');
                    if(!next||next.disabled)break;next.click();
                }while(seen.length<=1000);
                return{seen,maximumRows,omitted:document.querySelectorAll('.cfx-stage [data-cfx-role=legend-entry-omitted]').length,
                    identities:[...document.querySelectorAll('.cfx-stage g[data-cfx-role=point][data-cfx-observation-id]')].map(node=>node.dataset.cfxObservationId)};
            }
            """);
        Assert.Equal(6, report.GetProperty("maximumRows").GetInt32());
        Assert.Equal(1000, report.GetProperty("omitted").GetInt32());
        Assert.Equal(Enumerable.Range(0, 1000), report.GetProperty("seen").EnumerateArray().Select(value => value.GetInt32()));
        Assert.Equal(Enumerable.Range(0, 1000).Select(index => "item-" + index), report.GetProperty("identities").EnumerateArray().Select(value => value.GetString()));
        Assert.Equal(4, await page.Locator(Rows).CountAsync());
        Assert.True(await page.Locator("[data-cfx-legend-page=next]").IsDisabledAsync());
        await CaptureAsync(page, "legend-overflow-thousand-last");
        AssertNoConsoleErrors(session);
    }

    private static Chart DenseSeries(bool dark, int count = 13, bool reverse = false, bool longLabel = false) {
        var chart = Chart.Create().WithSize(680, 380).WithTitle("Service observations").WithLegend()
            .WithLegendPosition(ChartLegendPosition.Right).WithLegendBudget(maximumRows: 1)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        var indices = Enumerable.Range(0, count);
        if (reverse) indices = indices.Reverse();
        foreach (var index in indices) chart.AddSeries(ChartSeries.CreateLine(new ChartSeriesId("series-" + index),
            "Service " + (index + 1).ToString("00") + (longLabel && index == 0 ? " · " + new string('W', 80) + " · Complete native caption" : ""), new[] { new ChartPoint(0, index + 2), new ChartPoint(1, index + 4) }));
        return chart;
    }

    private static async Task<string> DownloadLegendSvgAsync(IPage page) {
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").EvaluateAsync("button=>button.click()"));
        var path = await download.PathAsync() ?? throw new InvalidOperationException("No native SVG export.");
        var svg = await File.ReadAllTextAsync(path);
        Assert.DoesNotContain("cfx-legend-overflow", svg, StringComparison.Ordinal);
        return svg;
    }
    private static async Task AssertReadableControlStatesAsync(IPage page, ILocator target, string state) {
        await target.HoverAsync();
        await AssertReadableControlAsync(page, target, state + "-hover");
        await FocusWithKeyboardAsync(page, target);
        await AssertReadableControlAsync(page, target, state + "-focus");
    }

    private static async Task AssertReadableControlAsync(IPage page, ILocator target, string state, string? pseudo = null, bool nonTextFill = false) {
        var paints = await target.EvaluateAsync<string[]>("""
            (node,options)=>{
                const style=getComputedStyle(node,options.pseudo),paints=[options.nonTextFill?style.backgroundColor:style.color];
                if(options.pseudo)paints.push(style.backgroundColor);
                for(let parent=options.nonTextFill?node.parentElement:node;parent;parent=parent.parentElement)paints.push(getComputedStyle(parent).backgroundColor);
                return paints;
            }
            """, new { pseudo, nonTextFill });
        var channels = await ResolveCssColorsAsync(page, paints);
        ChartColor Color(int[] value) => ChartColor.FromRgba((byte)value[0], (byte)value[1], (byte)value[2], (byte)value[3]);
        ChartColor? backdrop = null;
        for (var index = channels.Length - 1; index > 0; index--) {
            var color = Color(channels[index]);
            if (backdrop.HasValue) backdrop = ChartColorMath.CompositeOverOpaque(color, backdrop.Value);
            else if (color.A == 255) backdrop = color;
        }
        var surface = backdrop ?? throw new InvalidOperationException("The fixture requires a known opaque backing surface.");
        var foreground = ChartColorMath.CompositeOverOpaque(Color(channels[0]), surface);
        var contrast = ChartColorMath.ContrastRatio(foreground, surface);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(Path.Combine(directory, "legend-control-" + state + ".json"),
                JsonSerializer.Serialize(new { state, paints, channels, contrast }));
        }
        Assert.True(contrast >= (nonTextFill ? 3 : 4.5), "Host captions, step badges and progress retain contrast against the composed surface: " + contrast);
    }

    private static async Task CaptureAsync(IPage page, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new() { Path = Path.Combine(directory, name + ".png"), FullPage = true });
        var receipt = await page.EvaluateAsync<JsonElement>("""
            ()=>[...document.querySelectorAll('.cfx-interactive-chart')].map(root=>{
                const panel=root.querySelector('.cfx-legend-overflow'),svg=root.querySelector('.cfx-stage svg');
                return{chart:root.dataset.cfxChartId,direction:getComputedStyle(root).direction,
                    viewBox:svg.getAttribute('viewBox'),omitted:svg.querySelectorAll('[data-cfx-role=legend-entry-omitted]').length,
                    panel:panel&&{open:panel.open,display:getComputedStyle(panel).display,box:panel.getBoundingClientRect().toJSON(),
                        rows:[...panel.querySelectorAll('.cfx-legend-overflow__entry')].map(row=>({text:row.textContent,
                            index:row.dataset.cfxLegendIndex,box:row.getBoundingClientRect().toJSON(),width:row.clientWidth,extent:row.scrollWidth,
                            controls:[...row.querySelectorAll('button')].map(button=>({action:button.dataset.cfxLegendAction,
                                pressed:button.getAttribute('aria-pressed'),name:button.getAttribute('aria-label'),
                                box:button.getBoundingClientRect().toJSON(),color:getComputedStyle(button).color,
                                background:getComputedStyle(button).backgroundColor,focused:button===document.activeElement}))}))},
                    pageWidth:document.documentElement.scrollWidth,viewport:innerWidth};
            })
            """);
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".json"), JsonSerializer.Serialize(receipt));
    }

}
