using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using HtmlTinkerX;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed partial class RuntimeSelectionCueBrowserTests {
    [BrowserTheory]
    [InlineData(ChartSeriesKind.Donut, true, true, 390, false, true)]
    [InlineData(ChartSeriesKind.Bar, false, false, 980, false, false)]
    [InlineData(ChartSeriesKind.Pie, false, false, 980, true, true)]
    [InlineData(ChartSeriesKind.Bar, true, true, 390, true, true)]
    public async Task PersistentSelectionAndHostHighlightsPaintWithoutFiltersAndFollowCurrentNativeGeometry(
        ChartSeriesKind kind, bool graphite, bool dark, int width, bool dim, bool keyboard) {
        if (!Enabled) return;
        var initial = Model(kind, graphite, dark);
        var replacement = Model(kind, graphite, dark, reordered: true);
        void Configure(HtmlChartInteractionOptions options) => Options(options, dim, keyboard);
        await using var session = await OpenAsync(initial.ToInteractiveHtmlPage(Configure), width, 680);
        var page = session.Page;
        // A supported host can suppress decorative effects; the per-mark cue must still paint.
        await page.AddStyleTagAsync(new() { Content = ".cfx-stage svg,.cfx-stage svg *{filter:none!important}" });
        await Bind(page);
        var native = await NativeInk(page);
        var paint = await NativePaintIdentity(page);
        await page.EvaluateAsync("()=>{cueController.select({observationIds:['a']});cueController.highlight({observationIds:['b']},{dim:false});}");
        await AssertCues(page, 2);
        Assert.Equal(native, await NativeInk(page));
        var visible = PngReader.Decode(await page.Locator(".cfx-stage svg").ScreenshotAsync());
        await page.Locator("[data-cfx-state-outline]").EvaluateAllAsync<bool>("nodes=>{nodes.forEach(n=>n.style.visibility='hidden');return true;}");
        var withoutCue = PngReader.Decode(await page.Locator(".cfx-stage svg").ScreenshotAsync());
        Assert.True(ChangedPixels(visible, withoutCue) > 8, "The native SVG cue must change actual pixels without a CSS filter.");
        await page.Locator("[data-cfx-state-outline]").EvaluateAllAsync<bool>("nodes=>{nodes.forEach(n=>n.style.removeProperty('visibility'));return true;}");
        await Capture(session, $"selection-cue-{kind}-{dark}-{width}");
        await AssertPersistentExports(page, kind + "-" + dark + "-" + width);

        await page.EvaluateAsync("()=>{window.cueHidden=cueRoot.querySelector('[data-cfx-observation-id=a] [data-cfx-role=donut-slice],[data-cfx-observation-id=a] [data-cfx-role=pie-slice],[data-cfx-observation-id=a] [data-cfx-role=bar]');window.cueHiddenStyle=cueHidden.getAttribute('style');cueHidden.style.opacity='0';}");
        await AssertCues(page, 1);
        Assert.Equal("a", Assert.Single(await page.EvaluateAsync<string[]>("()=>cueController.getSelection().map(t=>t.observationId)")));
        await page.EvaluateAsync("()=>{if(cueHiddenStyle===null)cueHidden.removeAttribute('style');else cueHidden.setAttribute('style',cueHiddenStyle);}");
        await AssertCues(page, 2);
        await page.SetViewportSizeAsync(width < 500 ? 980 : 390, 680);
        await AssertCues(page, 2);
        // Adaptive layout can change coordinates; authored paint and observation ownership must survive.
        Assert.Equal(paint, await NativePaintIdentity(page));

        await page.EvaluateAsync("fragment=>cueController.update(fragment)", replacement.ToInteractiveHtmlFragmentWithoutAssets(Configure));
        await AssertCues(page, 2);
        Assert.Equal("a", Assert.Single(await page.EvaluateAsync<string[]>("()=>cueController.getSelection().map(t=>t.observationId)")));
        Assert.Equal("2", await page.Locator("[data-cfx-observation-id=a][data-cfx-target-kind=point]").GetAttributeAsync("data-cfx-point"));
        await page.EvaluateAsync("()=>{cueController.clearSelection();cueController.clearHighlight();}");
        await AssertCues(page, 0);
        var clean = await DownloadSvg(page);
        Assert.DoesNotContain("data-cfx-state-outline", clean, StringComparison.Ordinal);
        await page.EvaluateAsync("()=>{cueController.select({observationIds:['a']});cueController.dispose();}");
        Assert.Equal(0, await page.Locator("[data-cfx-state-outline]").CountAsync());
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task CollapsedFilledMarksHaveNoCueWhileNativeStrokesAndConnectedPointCuesRemainAvailable() {
        if (!Enabled) return;
        var bar = Model(ChartSeriesKind.Bar, true, true, zero: true);
        await using var session = await OpenAsync(bar.ToInteractiveHtmlPage(options => Options(options, false, true)), 740, 600);
        var page = session.Page;
        await Bind(page);
        await page.EvaluateAsync("()=>cueController.select({observationIds:['c']})");
        await AssertCues(page, 0);
        Assert.Equal("c", Assert.Single(await page.EvaluateAsync<string[]>("()=>cueController.getSelection().map(t=>t.observationId)")));
        var line = Model(ChartSeriesKind.Line, true, true, horizontal: true);
        await page.EvaluateAsync("fragment=>cueController.update(fragment)", line.ToInteractiveHtmlFragmentWithoutAssets(options => Options(options, false, true)));
        await page.EvaluateAsync("()=>{cueController.clearSelection();cueController.select({observationIds:['a']});cueController.highlight({observationIds:['b']},{dim:false});}");
        Assert.Equal(0, await page.Locator("[data-cfx-state-outline]").CountAsync());
        var prepared = await page.Locator(".cfx-selected > .cfx-prepared-point-marker,.cfx-highlighted > .cfx-prepared-point-marker")
            .EvaluateAllAsync<bool>("nodes=>nodes.length===2&&nodes.every(n=>getComputedStyle(n).opacity==='1'&&parseFloat(getComputedStyle(n).r)>0)");
        Assert.True(prepared);
        await FocusWithKeyboardAsync(page, page.Locator("[data-cfx-observation-id=a][data-cfx-target-kind=point]"));
        Assert.Contains("Parts", await TooltipTextAsync(page), StringComparison.Ordinal);
        await Capture(session, "selection-cue-horizontal-line");
        AssertNoConsoleErrors(session);
    }

    private static Chart Model(ChartSeriesKind kind, bool graphite, bool dark, bool reordered = false, bool zero = false, bool horizontal = false) {
        var ids = reordered ? new[] { "c", "b", "a" } : new[] { "a", "b", "c" };
        var values = reordered ? new[] { 4d, 6d, 9d } : new[] { 7d, 3d, zero ? 0d : 2d };
        var series = new ChartSeries(new ChartSeriesId("parts"), "Parts", kind,
            values.Select((value, index) => new ChartPoint(index + 1, horizontal ? 3 : value)))
            .WithObservationIds(ids.Select(id => new ChartObservationId(id)).ToArray())
            .WithPointColor(1, ChartColor.FromRgb(40, 120, 200).WithOpacity(.65));
        var chart = Chart.Create().WithSize(640, 360).WithTitle("Persistent observation cues")
            .WithTheme(graphite ? VisualTheme.Graphite() : VisualTheme.Report(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithXLabels(reordered ? new[] { "C", "B", "A" } : new[] { "A", "B", "C" }).AddSeries(series);
        return kind == ChartSeriesKind.Bar ? chart.WithBarStyle(ChartBarStyle.Flat) : chart;
    }

    private static void Options(HtmlChartInteractionOptions options, bool dim, bool keyboard) {
        options.Interaction.Enable(ChartInteractionFeatures.Selection | ChartInteractionFeatures.Export);
        if (keyboard) options.Interaction.Enable(ChartInteractionFeatures.KeyboardNavigation);
        else options.Interaction.Disable(ChartInteractionFeatures.KeyboardNavigation);
        options.Interaction.Disable(ChartInteractionFeatures.CompareMarkers);
        options.Highlight.DimOthersWhileSelected = dim;
        options.Tooltip.PinOnSelect = false;
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
    }

    private static Task Bind(IPage page) => page.EvaluateAsync("()=>{window.cueRoot=document.querySelector('.cfx-interactive-chart');window.cueController=ChartForgeXCharts.get(cueRoot);}");
    private static Task<string> NativeInk(IPage page) => page.EvaluateAsync<string>("()=>JSON.stringify([...cueRoot.querySelectorAll('[data-cfx-role=donut-slice],[data-cfx-role=pie-slice],[data-cfx-role=bar]')].map(n=>n.outerHTML))");
    private static Task<string> NativePaintIdentity(IPage page) => page.EvaluateAsync<string>("""
        ()=>JSON.stringify([...cueRoot.querySelectorAll('[data-cfx-role=donut-slice],[data-cfx-role=pie-slice],[data-cfx-role=bar]')].map(n=>({
            observation:n.closest('[data-cfx-observation-id]')?.dataset.cfxObservationId,
            paint:['fill','fill-opacity','stroke','stroke-opacity','stroke-width','opacity','class','style'].map(k=>n.getAttribute(k))})))
        """);
    private static async Task AssertCues(IPage page, int count) {
        await page.WaitForFunctionAsync("count=>cueRoot.querySelectorAll('[data-cfx-state-outline]').length===count", count);
        var facts = await page.EvaluateAsync<JsonElement>("""
            ()=>({cues:[...cueRoot.querySelectorAll('[data-cfx-state-outline]')].map(p=>({identity:p.querySelectorAll('[id],[tabindex],[data-cfx-target-id],[data-cfx-point]').length,
                hidden:p.getAttribute('aria-hidden'),pointer:getComputedStyle(p).pointerEvents,
                sameGeometry:[...p.children].every(n=>['x','y','width','height','rx','ry','cx','cy','r','x1','y1','x2','y2','d','points','transform','fill-rule','clip-path'].every(k=>n.getAttribute(k)===p.nextElementSibling.getAttribute(k))),
                ink:[...p.children].filter(n=>n.hasAttribute('data-cfx-state-ink')).map(n=>({stroke:getComputedStyle(n).stroke,width:parseFloat(getComputedStyle(n).strokeWidth),fill:getComputedStyle(n).fill}))}))})
            """);
        foreach (var cue in facts.GetProperty("cues").EnumerateArray()) {
            Assert.Equal(0, cue.GetProperty("identity").GetInt32()); Assert.Equal("true", cue.GetProperty("hidden").GetString());
            Assert.Equal("none", cue.GetProperty("pointer").GetString()); Assert.True(cue.GetProperty("sameGeometry").GetBoolean());
            foreach (var ink in cue.GetProperty("ink").EnumerateArray()) {
                Assert.NotEqual("none", ink.GetProperty("stroke").GetString()); Assert.True(ink.GetProperty("width").GetDouble() > 0);
                Assert.Equal("none", ink.GetProperty("fill").GetString());
            }
        }
    }

    private static int ChangedPixels(RgbaImage first, RgbaImage second) {
        Assert.Equal(first.Width, second.Width); Assert.Equal(first.Height, second.Height);
        var changed = 0;
        for (var offset = 0; offset < first.Pixels.Length; offset += 4)
            if (!first.Pixels.AsSpan(offset, 4).SequenceEqual(second.Pixels.AsSpan(offset, 4))) changed++;
        return changed;
    }

    private static async Task Capture(HtmlBrowserSession session, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await session.Page.ScreenshotAsync(new() { Path = Path.Combine(directory, name + ".png"), FullPage = true });
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".json"), await session.Page.EvaluateAsync<string>("()=>JSON.stringify({cues:cueRoot.querySelectorAll('[data-cfx-state-outline]').length,selection:cueController.getSelection(),nativePaint:[...cueRoot.querySelectorAll('[data-cfx-role=donut-slice],[data-cfx-role=pie-slice],[data-cfx-role=bar]')].map(n=>({fill:n.getAttribute('fill'),opacity:getComputedStyle(n).opacity})),console:[]})"));
    }
}
