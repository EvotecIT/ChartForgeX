using System.Globalization;
using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Protects native map keys, weighted readouts and authored point identity through real browser interactions.</summary>
public sealed class DottedMapPointLegendBrowserTests {
    private const string Root = ".cfx-interactive-chart";

    [BrowserTheory]
    [InlineData(false, 380)]
    [InlineData(false, 800)]
    [InlineData(true, 380)]
    [InlineData(true, 800)]
    public async Task PointKeysKeepWeightedFactsAndPointOnlyLegendStateAcrossNativeReplacement(bool dark, int width) {
        if (!Enabled) return;
        var initial = DottedMapPointLegendTests.Create(dark, width: width);
        var reversed = DottedMapPointLegendTests.Create(dark, reversed: true, width: width);
        var northFacts = initial.Series[0].MapPoints[0];
        await using var session = await OpenAsync(initial.ToInteractiveHtmlPage(Fit), width + 24, 650);
        var page = session.Page;
        var pageErrors = new List<string>();
        page.PageError += (_, error) => pageErrors.Add(error);
        await page.EvaluateAsync("""
            () => {
                const root=document.querySelector('.cfx-interactive-chart');
                window.mapController=ChartForgeXCharts.mount(root);
                window.mapSelections=[];
                root.addEventListener('cfxselect',event=>mapSelections.push(event.detail.target));
            }
            """);

        Assert.Equal(new[] { "North", "South", "Offscreen" },
            await page.Locator("[data-cfx-role='legend-label']").AllTextContentsAsync());
        foreach (var id in new[] { "north", "south", "offscreen" }) {
            var legend = page.Locator(Key(id));
            Assert.Equal(1, await legend.CountAsync());
            Assert.Equal("locations", await legend.GetAttributeAsync("data-cfx-series-key"));
            Assert.Equal(JsonSerializer.Serialize(new[] { "locations", id }), await legend.GetAttributeAsync("data-cfx-target-id"));
        }
        Assert.Equal(0, await page.Locator(Mark("offscreen") + " [data-cfx-role='dotted-map-point']").CountAsync());
        Assert.Equal(0, await page.Locator(Mark("offscreen") + " [data-cfx-browser-hit-area]").CountAsync());
        Assert.Equal("north", await page.Locator(Mark("north") + " [data-cfx-role='dotted-map-point']").EvaluateAsync<string>("""
            node=>{
                const box=node.getBoundingClientRect();
                return document.elementFromPoint(box.x+box.width/2,box.y+box.height/2)
                    ?.closest('[data-cfx-observation-id]')?.dataset.cfxObservationId||'';
            }
            """));
        await CaptureAsync(page, dark, width, "initial", initial);

        await MoveToAsync(page, Key("north"));
        Assert.Equal("North", await page.Locator(".cfx-tooltip__title").InnerTextAsync());
        Assert.Equal(new[] { "Value", Number(northFacts.Value!.Value) }, await TooltipRowsAsync(page));
        Assert.Equal(1d, await PaintOpacityAsync(page, "north"));
        Assert.InRange(await PaintOpacityAsync(page, "south"), 0, .99);
        await CaptureAsync(page, dark, width, "key-readout");
        await MoveToAsync(page, Key("offscreen"));
        Assert.Equal("Offscreen", await page.Locator(".cfx-tooltip__title").InnerTextAsync());
        Assert.Equal(new[] { "Value", "12" }, await TooltipRowsAsync(page));

        var north = page.Locator(Mark("north"));
        Assert.Equal(Number(northFacts.Longitude), await north.GetAttributeAsync("data-cfx-longitude"));
        Assert.Equal(Number(northFacts.Latitude), await north.GetAttributeAsync("data-cfx-latitude"));
        Assert.Equal(Number(northFacts.Value!.Value), await north.GetAttributeAsync("data-cfx-value"));
        await MoveToAsync(page, Mark("north") + " [data-cfx-role='dotted-map-point']");
        await CaptureAsync(page, dark, width, "mark-readout");
        Assert.Contains("North", await TooltipTextAsync(page));
        Assert.Contains("7", await TooltipTextAsync(page));
        await north.FocusAsync();
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await north.GetAttributeAsync("aria-selected"));
        var selected = await page.EvaluateAsync<JsonElement>("() => mapSelections.at(-1)");
        Assert.Equal("locations", selected.GetProperty("seriesKey").GetString());
        Assert.Equal("north", selected.GetProperty("observationId").GetString());
        Assert.Equal("7", selected.GetProperty("value").GetString());
        Assert.Equal("0", selected.GetProperty("sourcePoint").GetString());

        await page.Locator(Key("north")).FocusAsync();
        await page.Keyboard.PressAsync("Space");
        await LeaveReadoutAsync(page);
        Assert.Equal("true", await page.Locator(Key("north")).GetAttributeAsync("data-cfx-muted"));
        Assert.InRange(await PaintOpacityAsync(page, "north"), 0, .99);
        Assert.Equal(1d, await PaintOpacityAsync(page, "south"));
        await ReplaceAsync(page, reversed);
        Assert.Equal("2", await page.Locator(Key("north")).GetAttributeAsync("data-cfx-point"));
        Assert.Equal("2", await page.Locator(Mark("north")).GetAttributeAsync("data-cfx-source-point"));
        Assert.Equal("true", await page.Locator(Key("north")).GetAttributeAsync("data-cfx-muted"));
        Assert.Equal("true", await page.Locator(Mark("north")).GetAttributeAsync("aria-selected"));
        Assert.InRange(await PaintOpacityAsync(page, "north"), 0, .99);
        Assert.Equal(1d, await PaintOpacityAsync(page, "south"));
        await CaptureAsync(page, dark, width, "reordered-muted");

        await page.Locator(Key("north")).FocusAsync();
        await page.Keyboard.PressAsync("Space");
        await page.Keyboard.PressAsync("i");
        await LeaveReadoutAsync(page);
        Assert.Equal("true", await page.Locator(Key("north")).GetAttributeAsync("data-cfx-isolated"));
        Assert.Equal(1d, await PaintOpacityAsync(page, "north"));
        Assert.InRange(await PaintOpacityAsync(page, "south"), 0, .99);
        await ReplaceAsync(page, initial);
        Assert.Equal("0", await page.Locator(Key("north")).GetAttributeAsync("data-cfx-point"));
        Assert.Equal("true", await page.Locator(Key("north")).GetAttributeAsync("data-cfx-isolated"));
        Assert.Equal(1d, await PaintOpacityAsync(page, "north"));
        Assert.InRange(await PaintOpacityAsync(page, "south"), 0, .99);
        await CaptureAsync(page, dark, width, "reordered-isolated");

        await ReplaceAsync(page, DottedMapPointLegendTests.Create(dark, reversed: true, removeNorth: true, width: width));
        Assert.Equal(0, await page.Locator("[data-cfx-observation-id='north']").CountAsync());
        Assert.Equal(0, await page.Locator("[aria-selected='true'],[data-cfx-muted='true'],[data-cfx-isolated='true']").CountAsync());
        Assert.True(string.IsNullOrEmpty(await page.Locator(Root).GetAttributeAsync("data-cfx-isolated-series")));
        Assert.Equal(1d, await PaintOpacityAsync(page, "south"));
        await CaptureAsync(page, dark, width, "removed");
        Assert.Empty(pageErrors);
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task UnweightedLocationAndZeroWeightHaveDistinctLegendReadouts() {
        if (!Enabled) return;
        var series = ChartSeries.CreateDottedMap(new ChartSeriesId("locations"), "Locations", new[] {
            new ChartMapPoint("North", 21, 52), new ChartMapPoint("South", -3.7, 40.4, 0)
        }).WithObservationId(0, new ChartObservationId("north")).WithObservationId(1, new ChartObservationId("south"));
        var chart = Chart.Create().WithSize(380, 420).WithMapViewport(ChartMapViewport.Europe())
            .WithPointLegend().WithDataLabels(false).AddSeries(series);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(Fit), 404, 650);
        var page = session.Page;
        var pageErrors = new List<string>();
        page.PageError += (_, error) => pageErrors.Add(error);
        Assert.Equal("", await page.Locator(Mark("north")).GetAttributeAsync("data-cfx-value"));
        Assert.Equal("0", await page.Locator(Mark("south")).GetAttributeAsync("data-cfx-value"));
        await MoveToAsync(page, Key("north"));
        await CaptureAsync(page, false, 380, "unweighted-key-readout", chart);
        Assert.Equal("North", await TooltipTextAsync(page));
        Assert.Equal(0, await page.Locator(".cfx-tooltip__meta").CountAsync());
        await MoveToAsync(page, Key("south"));
        Assert.Equal("South", await page.Locator(".cfx-tooltip__title").InnerTextAsync());
        Assert.Equal(new[] { "Value", "0" }, await TooltipRowsAsync(page));
        await CaptureAsync(page, false, 380, "zero-weight-key-readout");
        Assert.Empty(pageErrors);
        AssertNoConsoleErrors(session);
    }

    private static string Key(string id) => "[data-cfx-role='legend-item'][data-cfx-observation-id='" + id + "']";
    private static string Mark(string id) => "[data-cfx-role='dotted-map-point-source'][data-cfx-observation-id='" + id + "']";
    private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    private static void Fit(HtmlChartInteractionOptions options) => options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;

    private static Task<string[]> TooltipRowsAsync(IPage page) => page.Locator(".cfx-tooltip__meta")
        .EvaluateAsync<string[]>("node=>Array.from(node.children,child=>child.textContent)");

    private static async Task LeaveReadoutAsync(IPage page) {
        await MoveAwayAsync(page);
        await page.EvaluateAsync("() => document.activeElement?.blur()");
    }

    // Ancestor opacity also contributes to the visible mark; its own computed opacity alone can conceal a broken isolation container.
    private static Task<double> PaintOpacityAsync(IPage page, string id) => page.Locator(Mark(id) + " [data-cfx-role='dotted-map-point']")
        .EvaluateAsync<double>("""
            node=>{
                let opacity=1;
                for(let current=node;current;current=current.parentElement) opacity*=Number(getComputedStyle(current).opacity);
                return opacity;
            }
            """);

    private static Task ReplaceAsync(IPage page, Chart chart) => page.EvaluateAsync("""
        html=>{
            const root=mapController.root;
            mapController.update(html);
            if(mapController.root!==root || !root.isConnected) throw new Error('Native replacement lost the host root.');
        }
        """, chart.ToInteractiveHtmlFragmentWithoutAssets(Fit));

    private static async Task CaptureAsync(IPage page, bool dark, int width, string state, Chart? native = null) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Capture output must be an absolute path.");
        Directory.CreateDirectory(directory);
        var stem = Path.Combine(directory, "dotted-map-point-keys-" + width + "-" + (dark ? "dark" : "light") + "-" + state);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
        await File.WriteAllTextAsync(stem + ".json", await page.EvaluateAsync<string>("""
            () => {
                const north=document.querySelector('[data-cfx-role=dotted-map-point-source][data-cfx-observation-id=north] [data-cfx-role=dotted-map-point]');
                const box=north?.getBoundingClientRect(),hit=box&&document.elementFromPoint(box.x+box.width/2,box.y+box.height/2);
                return JSON.stringify({
                legend:[...document.querySelectorAll('[data-cfx-role=legend-item]')].map(node=>({...node.dataset})),
                points:[...document.querySelectorAll('[data-cfx-role=dotted-map-point-source]')].map(node=>({
                    ...node.dataset,browserHits:[...node.querySelectorAll('[data-cfx-browser-hit-area]')].map(hit=>hit.outerHTML)
                })),
                northHit:hit?{tag:hit.tagName,data:{...hit.dataset},owner:{...hit.parentElement?.dataset}}:null,
                selected:window.mapSelections||[],tooltip:document.querySelector('.cfx-tooltip')?.innerText
                },null,2);
            }
            """));
        if (native is null) return;
        var prepared = native.Prepare(VisualExportRequest.ForChart(native).Context);
        await File.WriteAllTextAsync(stem + ".svg", prepared.ToSvg());
        await File.WriteAllBytesAsync(stem + ".native.png", prepared.ToPng());
    }
}
