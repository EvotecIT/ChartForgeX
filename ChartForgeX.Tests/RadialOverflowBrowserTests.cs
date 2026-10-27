using System.Globalization;
using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class RadialOverflowBrowserTests {
    private const string Bound = "> 1.7976931348623157E+308";

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OverflowingAggregateKeepsBoundedLocalizedReadoutsAndFiniteContributorIdentity(bool donut) {
        if (!Enabled) return;
        var series = new ChartSeries(new ChartSeriesId("parts"), "Parts", donut ? ChartSeriesKind.Donut : ChartSeriesKind.Pie,
            new[] { new ChartPoint(1, 1e308), new ChartPoint(2, 1e308), new ChartPoint(3, 1e308) });
        for (var index = 0; index < 3; index++) series.WithObservationId(index, new ChartObservationId("part-" + index));
        var chart = Chart.Create().WithSize(640, 400).WithXLabels("First", "Second", "Third").AddSeries(series)
            .WithTheme(VisualTheme.Graphite(), donut ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithDataLabels().WithLegend();
        chart.Options.MaximumPieSlices = 2;
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(Configure),
            donut ? 390 : 760, 720);
        var page = session.Page;
        var aggregate = page.Locator("[data-cfx-role=radial-point][data-cfx-point='-1']");
        Assert.Null(await aggregate.GetAttributeAsync("data-cfx-value"));
        Assert.Equal("true", await aggregate.GetAttributeAsync("data-cfx-value-overflow"));
        Assert.Equal(Bound, await aggregate.GetAttributeAsync("data-cfx-formatted-value"));
        Assert.Equal("1,2", await aggregate.GetAttributeAsync("data-cfx-source-points"));
        var raw = await aggregate.GetAttributeAsync("data-cfx-source-values") ?? throw new InvalidOperationException("No aggregate source values.");
        Assert.All(raw.Split(','), value => Assert.Equal(1e308, double.Parse(value, CultureInfo.InvariantCulture)));
        Assert.Equal(2, raw.Split(',').Length);
        var pointer = await aggregate.EvaluateAsync<float[]>("""
            node=>{
                const box=node.getBoundingClientRect();
                // A donut's bounding-box centre is its unpainted hole. Let native hit testing
                // locate a visible part of this exact slice rather than forcing a synthetic event.
                for(let row=1;row<10;row++)for(let column=1;column<10;column++){
                    const x=box.left+box.width*column/10,y=box.top+box.height*row/10,hit=document.elementFromPoint(x,y);
                    if(hit&&node.contains(hit)&&!hit.hasAttribute('data-cfx-browser-hit-area')&&hit.getAttribute('fill')!=='transparent'
                        &&getComputedStyle(hit).fill!=='none'&&getComputedStyle(hit).fill!=='transparent')return[x,y];
                }
                throw new Error('The aggregate slice has no visible native painted hit point.');
            }
            """);
        await page.Mouse.MoveAsync(pointer[0], pointer[1], new() { Steps = 4 });
        await CaptureAsync(page, donut ? "donut-pointer" : "pie-pointer", JsonSerializer.SerializeToElement(Array.Empty<object>()));
        Assert.Contains(Bound, await TooltipTextAsync(page));
        Assert.Contains("Wartość", await page.Locator(".cfx-tooltip dt").AllTextContentsAsync());
        await FocusWithKeyboardAsync(page, aggregate);
        Assert.Contains(Bound, await TooltipTextAsync(page));
        await page.Keyboard.PressAsync("Space");
        var selected = await page.EvaluateAsync<JsonElement>("()=>ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')).getSelection()");
        await CaptureAsync(page, donut ? "donut-selected" : "pie-selected", selected);
        var target = Assert.Single(selected.EnumerateArray());
        Assert.Equal(new[] { 1, 2 }, target.GetProperty("sourcePoints").EnumerateArray().Select(value => value.GetInt32()));
        Assert.Equal(new[] { "part-1", "part-2" }, target.GetProperty("observationIds").EnumerateArray().Select(value => value.GetString()));
        Assert.False(target.TryGetProperty("numericValue", out _));
        Assert.False(target.TryGetProperty("observationId", out _));
        await AssertLegendColumnsAsync(page);
        if (donut) Assert.Equal(Bound, await page.Locator("[data-cfx-center-value]").GetAttributeAsync("data-cfx-center-value"));
        await CaptureAsync(page, donut ? "donut" : "pie", selected);
        if (donut) {
            chart.WithDonutCenterText("Authored capacity", "Explicit total");
            await page.EvaluateAsync("html=>ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')).update(html)",
                chart.ToInteractiveHtmlFragmentWithoutAssets(Configure));
            Assert.Equal("Authored capacity", await page.Locator("[data-cfx-center-value]").GetAttributeAsync("data-cfx-center-value"));
            await CaptureAsync(page, "donut-authored-center", selected);
        }
        AssertNoConsoleErrors(session);
    }

    private static async Task AssertLegendColumnsAsync(IPage page) {
        await BrowserProofCaptions.InstallNativeAsync(page);
        var rows = await page.Locator("[data-cfx-role=legend-item]").EvaluateAllAsync<JsonElement>("""
            entries=>entries.map(entry=>{
                const box=role=>{const node=entry.querySelector('[data-cfx-role='+role+']');if(!node)return null;
                    const rect=node.getBoundingClientRect(),style=getComputedStyle(node);
                    return {text:window.cfxCaptionText(node),left:rect.left,right:rect.right,top:rect.top,bottom:rect.bottom,font:style.font,fontFamily:style.fontFamily};};
                return {label:box('legend-label'),value:box('legend-value'),percent:box('legend-percentage')};
            })
            """);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output))
            await File.WriteAllTextAsync(Path.Combine(output, "radial-legend-columns-" + await page.Locator("[data-cfx-role=radial-point]").First.GetAttributeAsync("data-cfx-kind") + ".json"), rows.GetRawText());
        Assert.Equal(2, rows.GetArrayLength());
        foreach (var row in rows.EnumerateArray()) {
            var label = row.GetProperty("label"); var value = row.GetProperty("value"); var percent = row.GetProperty("percent");
            Assert.False(string.IsNullOrWhiteSpace(label.GetProperty("text").GetString()));
            Assert.True(label.GetProperty("right").GetDouble() <= value.GetProperty("left").GetDouble() + .5,
                "The native legend label and value must remain separate under the browser's actual font.");
            Assert.True(value.GetProperty("right").GetDouble() + 1 <= percent.GetProperty("left").GetDouble(),
                "The native legend value and percentage need visible separation.");
        }
    }

    private static void Configure(HtmlChartInteractionOptions options) {
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
        options.Localization[HtmlChartText.Value] = "Wartość";
    }

    private static async Task CaptureAsync(IPage page, string name, JsonElement selection) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        var facts = await page.Locator("[data-cfx-role=radial-point][data-cfx-point='-1']").EvaluateAsync<object>(
            "node=>({attributes:Object.fromEntries([...node.attributes].map(item=>[item.name,item.value])),readout:document.querySelector('.cfx-tooltip').textContent})");
        await File.WriteAllTextAsync(Path.Combine(directory, "radial-overflow-" + name + ".json"), JsonSerializer.Serialize(new { facts, selection }));
        await page.ScreenshotAsync(new() { Path = Path.Combine(directory, "radial-overflow-" + name + ".png"), FullPage = false });
    }
}
