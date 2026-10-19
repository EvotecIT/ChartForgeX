using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Real native ribbon input preserves flow weight independently from quantitative color and whole-scene replacement.</summary>
[Trait("BrowserSuite", "Compatibility")]
public sealed class InteractiveChordColorBrowserTests {
    [BrowserTheory]
    [InlineData(360, false)]
    [InlineData(800, true)]
    public async Task IndependentFlowColorRetainsNativeReadoutsIdentityAndCurrentExportAcrossReplacement(int width, bool dark) {
        if (!Enabled) return;
        var chart = Model(width, dark); var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var html = chart.ToInteractiveHtmlPage(Configure);
        await using var session = await OpenAsync(html, width + 24, 700);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await page.EvaluateAsync("""
            () => { window.colorChord=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));
                window.colorChordEvents=[]; for(const name of ['cfxhover','cfxselect'])
                    colorChord.root.addEventListener(name,event=>colorChordEvents.push({name,target:event.detail.target})); }
            """);
        var root = page.Locator(".cfx-interactive-chart");
        Assert.Equal(9, await page.Locator("[data-cfx-target-kind=link]").CountAsync());
        Assert.Equal(4, await page.Locator("[data-cfx-target-kind=node]").CountAsync());
        Assert.Equal(0, await page.Locator("[data-cfx-role=chord-color-scale] [data-cfx-target-kind]").CountAsync());
        Assert.Equal(3, await page.Locator("[data-cfx-role=chord-color-scale-caption-source][data-cfx-caption-origin=authored]").CountAsync());
        var stops = page.Locator("[data-cfx-role=chord-color-scale-stops]");
        var scale = chart.Options.Chord.ColorScale!;
        Assert.Equal(scale.Stops.Count.ToString(CultureInfo.InvariantCulture), await stops.GetAttributeAsync("data-cfx-stop-count"));
        Assert.Equal(0, await stops.Locator("[data-cfx-target-kind], [tabindex]").CountAsync());
        for (var index = 0; index < scale.Stops.Count; index++) {
            var stop = stops.Locator("[data-cfx-stop='" + index + "']");
            Assert.Equal(scale.Stops[index].Value.ToString("R", CultureInfo.InvariantCulture), await stop.GetAttributeAsync("data-cfx-value"));
            Assert.Equal(scale.Stops[index].Color.ToHexRgba(), await stop.GetAttributeAsync("data-cfx-color"));
            Assert.Contains(scale.Stops[index].Color.ToHexRgba(), await stop.GetAttributeAsync("aria-label"));
        }
        var sourceStops = await StopFactsAsync();
        var nativePaint = await NativePaintAsync(page);
        var selector = Flow("north-south");
        var point = await NativeInteriorPointAsync(page, selector, "[data-cfx-role=chord-ribbon]", clearance: 2);
        await MoveAwayAsync(page);
        await page.Mouse.MoveAsync((float)point[0], (float)point[1], new MouseMoveOptions { Steps = 4 });
        Assert.Equal("north-south", await page.EvaluateAsync<string>("point=>document.elementFromPoint(point[0],point[1]).closest('[data-cfx-target-kind=link]').dataset.cfxTargetId", point));
        await AssertRowAsync("Value", "18"); await AssertRowAsync("Illustrative latency", "180 ms");
        await CaptureAsync("native-hover");
        await page.Mouse.ClickAsync((float)point[0], (float)point[1]);
        var selected = await ReadTargetAsync(selectedEvent: true);
        AssertTarget(selected, "north-south", 18);
        Assert.Equal("true", await root.GetAttributeAsync("data-cfx-tooltip-pinned"));
        Assert.Equal(nativePaint, await NativePaintAsync(page));
        await page.Locator(selector).FocusAsync(); await page.Keyboard.PressAsync("Space");

        await MoveAwayAsync(page);
        await page.Keyboard.PressAsync("ArrowRight");
        Assert.Equal("south-north", await page.EvaluateAsync<string>("()=>document.activeElement.dataset.cfxTargetId"));
        Assert.True(await page.Locator(Flow("south-north")).EvaluateAsync<bool>("node=>node.matches(':focus-visible')"));
        await FocusWithKeyboardAsync(page, page.Locator(Flow("support-south")));
        await AssertRowAsync("Value", "9"); await AssertRowAsync("Illustrative latency", "0 ms");
        Assert.Equal("0", await page.Locator(Flow("support-south")).GetAttributeAsync("data-cfx-color-value"));
        await page.Keyboard.PressAsync("Space");
        AssertTarget(await ReadTargetAsync(), "support-south", 9);
        await page.Keyboard.PressAsync("Space");
        await FocusWithKeyboardAsync(page, page.Locator(Flow("south-support")));
        await AssertRowAsync("Value", "14"); await AssertRowAsync("Illustrative latency", "Not sampled");
        Assert.Null(await page.Locator(Flow("south-support")).GetAttributeAsync("data-cfx-color-value"));
        Assert.Equal("true", await page.Locator(Flow("south-support")).GetAttributeAsync("data-cfx-color-missing"));
        await FocusWithKeyboardAsync(page, page.Locator(Flow("zero-transfer")));
        await AssertRowAsync("Value", "0"); await AssertRowAsync("Illustrative latency", "210 ms");
        Assert.Equal(0, await page.Locator(Flow("zero-transfer") + " [data-cfx-role=chord-ribbon]").CountAsync());
        await page.Keyboard.PressAsync("Space");
        AssertTarget(await ReadTargetAsync(), "zero-transfer", 0);
        await CaptureAsync("semantic-zero"); await page.Keyboard.PressAsync("Space");

        await FocusWithKeyboardAsync(page, page.Locator(Flow("north-priority")));
        await AssertRowAsync("Value", "5"); await AssertRowAsync("Illustrative latency", "40 ms");
        await page.Keyboard.PressAsync("Space");
        var oldPath = await page.Locator(Flow("north-priority") + " [data-cfx-role=chord-ribbon]").GetAttributeAsync("d");
        var next = Model(width, dark, updated: true);
        await page.EvaluateAsync("fragment=>colorChord.update(fragment)", next.ToInteractiveHtmlFragmentWithoutAssets(Configure));
        var priority = page.Locator(Flow("north-priority"));
        Assert.Equal("80", await priority.GetAttributeAsync("data-cfx-color-value"));
        Assert.Equal("5", await priority.GetAttributeAsync("data-cfx-value"));
        Assert.Equal("5", await priority.GetAttributeAsync("data-cfx-source-link-index"));
        Assert.Equal("true", await priority.GetAttributeAsync("aria-selected"));
        Assert.Equal(0, await page.Locator("[data-cfx-role=chord-color-scale] [data-cfx-target-kind]").CountAsync());
        Assert.Equal(sourceStops, await StopFactsAsync());
        AssertTarget(await ReadTargetAsync(), "north-priority", 5);
        Assert.NotEqual(oldPath, await priority.Locator("[data-cfx-role=chord-ribbon]").GetAttributeAsync("d"));
        // A replacement dismisses the old readout while retaining the authored focus/selection. New input inspects the new scene.
        Assert.True(await page.Locator(".cfx-tooltip").EvaluateAsync<bool>("tip=>tip.hidden"));
        await page.Keyboard.PressAsync("ArrowRight"); await page.Keyboard.PressAsync("ArrowLeft");
        Assert.Equal("north-priority", await page.EvaluateAsync<string>("()=>document.activeElement.dataset.cfxTargetId"));
        await AssertRowAsync("Value", "5"); await AssertRowAsync("Illustrative latency", "80 ms");
        await CaptureAsync("updated-selection");
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var downloadPath = await download.PathAsync() ?? throw new InvalidOperationException("Current Chord export produced no SVG.");
        var exported = await File.ReadAllTextAsync(downloadPath); var document = XDocument.Parse(exported);
        var links = document.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "chord-link").ToArray();
        Assert.Equal(9, links.Length);
        var exportedPriority = links.Single(node => (string?)node.Attribute("data-cfx-target-id") == "north-priority");
        Assert.Equal("5", (string?)exportedPriority.Attribute("data-cfx-value"));
        Assert.Equal("80", (string?)exportedPriority.Attribute("data-cfx-color-value"));
        Assert.Equal("transfer-source", (string?)exportedPriority.Attribute("data-cfx-series-key"));
        var exportedStops = document.Descendants().Where(node => node.Attribute("data-cfx-stop") != null).ToArray();
        Assert.Equal(scale.Stops.Count, exportedStops.Length);
        Assert.All(exportedStops, stop => { Assert.Null(stop.Attribute("data-cfx-target-kind")); Assert.Null(stop.Attribute("tabindex")); });
        Assert.DoesNotContain(document.Descendants(), node => node.Name.LocalName == "script");
        Assert.True(await page.EvaluateAsync<bool>("()=>document.documentElement.scrollWidth<=innerWidth+1"));
        Assert.Empty(errors); AssertNoConsoleErrors(session);
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) await File.WriteAllTextAsync(Path.Combine(capture, Name() + "-current-export.svg"), exported);

        Task AssertRowAsync(string name, string value) => page.WaitForFunctionAsync("""
            expected=>{const tip=document.querySelector('.cfx-tooltip');return tip&&!tip.hidden&&[...tip.querySelectorAll('dt')]
                .some(term=>term.textContent===expected.name&&term.nextElementSibling.textContent===expected.value);}
            """, new { name, value }, new PageWaitForFunctionOptions { Timeout = 5000 });
        Task<JsonElement> ReadTargetAsync(bool selectedEvent = false) => page.EvaluateAsync<JsonElement>("""
            selectedEvent=>{
                const target=selectedEvent?colorChordEvents.findLast(event=>event.name==='cfxselect').target:colorChord.getSelection()[0],
                    observed=[target,...colorChordEvents.filter(event=>event.target?.targetKind==='link').map(event=>event.target)];
                if(observed.some(item=>item.point!==undefined||item.sourcePoint!==undefined))
                    throw Error('Native Chord links must not invent point or source-point ordinals.');
                return target;
            }
            """, selectedEvent);
        Task<string[]> StopFactsAsync() => stops.Locator("[data-cfx-stop]").EvaluateAllAsync<string[]>("""
            nodes=>nodes.map(node=>JSON.stringify(['data-cfx-source-id','data-cfx-stop','data-cfx-value','data-cfx-color','data-cfx-label']
                .map(name=>node.getAttribute(name))))
            """);
        string Name() => "chord-color-" + width + "-" + (dark ? "dark" : "light");
        async Task CaptureAsync(string stage) {
            var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
            if (string.IsNullOrWhiteSpace(directory)) return;
            Directory.CreateDirectory(directory); var stem = Path.Combine(directory, Name());
            if (stage == "native-hover") {
                await File.WriteAllTextAsync(stem + "-native.svg", prepared.ToSvg());
                await File.WriteAllBytesAsync(stem + "-native.png", prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
            }
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + "-" + stage + ".png", FullPage = false });
            var state = await page.EvaluateAsync<JsonElement>("""
                ()=>({selection:colorChord.getSelection(),events:colorChordEvents,focus:document.activeElement.dataset.cfxTargetId,
                    pinned:colorChord.root.dataset.cfxTooltipPinned,readout:colorChord.root.querySelector('.cfx-tooltip').innerText,
                    links:[...colorChord.root.querySelectorAll('[data-cfx-role=chord-link]')].map(node=>({...node.dataset}))})
                """);
            await File.WriteAllTextAsync(stem + "-" + stage + ".json", JsonSerializer.Serialize(new {
                State = state, Errors = errors, Console = session.ConsoleLog
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    private static void AssertTarget(JsonElement target, string id, double weight) {
        Assert.Equal("link", target.GetProperty("targetKind").GetString()); Assert.Equal(id, target.GetProperty("targetId").GetString());
        Assert.Equal("transfer-source", target.GetProperty("seriesKey").GetString()); Assert.Equal(weight, target.GetProperty("numericValue").GetDouble());
        Assert.Equal("shared", target.GetProperty("seriesScope").GetString());
        // The browser transport can materialize JavaScript undefined fields as JSON null.
        foreach (var name in new[] { "point", "sourcePoint" })
            if (target.TryGetProperty(name, out var point)) Assert.Equal(JsonValueKind.Null, point.ValueKind);
    }
    private static string Flow(string id) => "[data-cfx-role=chord-link][data-cfx-target-id='" + id + "']";
    private static Task<string> NativePaintAsync(IPage page) => page.Locator("[data-cfx-role=chord-series]").EvaluateAsync<string>("""
        root=>JSON.stringify([...root.querySelectorAll('[data-cfx-role=chord-ribbon],[data-cfx-role=chord-target-cue]')]
            .filter(node=>!node.closest('[data-cfx-state-outline],[data-cfx-browser-hit-area]'))
            .map(node=>Object.fromEntries(['d','fill','stroke','stroke-width'].map(key=>[key,node.getAttribute(key)]))))
        """);
    private static Chart Model(int width, bool dark, bool updated = false) {
        var chart = V2GalleryModels.Create(ChartSeriesKind.Chord, width == 360 ? "compact-color-dimension" : "color-dimension")
            .WithSize(width, width == 360 ? 360 : 440).WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithTitle("Transfer volume and latency");
        if (updated) {
            var series = chart.Series[0];
            series.ReplaceFlowData(series.Nodes, series.FlowLinks.Reverse().Select(flow => new ChartFlowLink(flow.Id, flow.SourceId, flow.TargetId,
                flow.Value, flow.Id == "north-priority" ? 80 : flow.ColorValue)));
        }
        return chart;
    }
    private static void Configure(HtmlChartInteractionOptions options) {
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit; options.Tooltip.Mode = HtmlChartTooltipMode.Single;
        options.Interaction.Enable(ChartInteractionFeatures.Export);
    }
}
