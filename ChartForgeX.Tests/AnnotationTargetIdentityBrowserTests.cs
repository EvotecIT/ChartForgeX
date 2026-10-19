using System.Text.Json;
using System.Xml.Linq;
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

/// <summary>Annotation captions describe facts; returned opaque identities address individual native targets.</summary>
[Trait("BrowserSuite", "Compatibility")]
public sealed class AnnotationTargetIdentityBrowserTests {
    private const string Root = ".cfx-interactive-chart";
    private static string Annotation(int index) => "[data-cfx-role=annotation][data-cfx-source-id=annotation-" + index + "]";

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RepeatedAnnotationCaptionsAndValuesRoundTripIndividualCommandAndActivationTargets(bool dark) {
        if (!Enabled) return;
        var chart = Model(dark);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(Options), dark ? 390 : 800, 720);
        var page = session.Page;
        await Bind(page);
        var paint = await NativePaint(page);
        var targets = await page.EvaluateAsync<JsonElement>("()=>annotationController.resolveTargets()");
        Assert.Equal(5, targets.GetArrayLength());
        Assert.Equal(5, targets.EnumerateArray().Select(target => target.GetProperty("targetId").GetString()).Distinct().Count());
        Assert.All(targets.EnumerateArray(), target => Assert.Equal("local", target.GetProperty("seriesScope").GetString()));

        // Two captions and two empty-caption kind/value pairs repeat, but each returned target remains singular.
        foreach (var index in new[] { 0, 1, 2, 3, 4 }) {
            var selected = await page.EvaluateAsync<JsonElement>("""
                index=>{const target=annotationController.resolveTargets()[index];
                    annotationController.select({targets:[target]});annotationController.highlight({targets:[target]},{dim:false});
                    return {target,selected:annotationController.getSelection(),highlighted:annotationRoot.querySelectorAll('.cfx-highlighted').length};}
                """, index);
            Assert.Equal(selected.GetProperty("target").GetProperty("targetId").GetString(),
                Assert.Single(selected.GetProperty("selected").EnumerateArray()).GetProperty("targetId").GetString());
            Assert.Equal(1, selected.GetProperty("highlighted").GetInt32());
        }
        Assert.Equal(paint, await NativePaint(page));
        await page.EvaluateAsync("()=>{annotationController.clearHighlight();annotationController.clearSelection();}");
        var first = page.Locator(Annotation(0));
        await FocusWithKeyboardAsync(page, first);
        await page.Keyboard.PressAsync("Space");
        Assert.Equal(await first.GetAttributeAsync("data-cfx-target-id"), await SelectedId(page));
        Assert.Equal("keyboard", await page.EvaluateAsync<string>("()=>annotationEvents.at(-1).reason"));
        await page.EvaluateAsync("()=>annotationController.clearSelection()");
        var labelPosition = await page.Locator(Annotation(1)).EvaluateAsync<double[]>("""
            node=>{const text=node.querySelector('[data-cfx-role=annotation-label] text'),box=text.getExtentOfChar(Math.floor(text.textContent.length/2)),
                point=new DOMPoint(box.x+box.width/2,box.y+box.height/2).matrixTransform(text.getScreenCTM());return[point.x,point.y];}
            """);
        Assert.True(await page.Locator(Annotation(1)).EvaluateAsync<bool>("""
            (node,point)=>node.querySelector('[data-cfx-role=annotation-label] text')===document.elementFromPoint(Math.trunc(point[0]),Math.trunc(point[1]))
            """, labelPosition), "A real integer pointer coordinate must hit this annotation's native caption text.");
        await page.Mouse.ClickAsync((float)labelPosition[0], (float)labelPosition[1]);
        Assert.Equal(await page.Locator(Annotation(1)).GetAttributeAsync("data-cfx-target-id"), await SelectedId(page));
        Assert.Equal("click", await page.EvaluateAsync<string>("()=>annotationEvents.at(-1).reason"));

        await page.SetViewportSizeAsync(dark ? 980 : 390, 720);
        await page.WaitForFunctionAsync("drawing=>annotationRoot.dataset.cfxAdaptiveDrawing===drawing", dark ? "wide" : "narrow");
        Assert.Equal("annotation-1", await SelectedId(page));
        Assert.Equal(paint, await NativePaint(page));
        await AssertExports(page, dark);
        await Capture(session, "individual-annotation-targets-" + dark);
        await page.EvaluateAsync("()=>annotationController.clearSelection()");
        Assert.Empty(await page.EvaluateAsync<string[]>("()=>annotationController.getSelection().map(t=>t.targetId)"));
        Assert.Equal(0, await page.Locator("[data-cfx-state-outline]").CountAsync());
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task AnnotationScenarioOpaqueIdentityWinsBeforeLegacyCaptionAliases() {
        if (!Enabled) return;
        await using var session = await OpenAsync(Model(false).ToInteractiveHtmlPage(options => {
            Options(options);
            options.Interaction.AddScenario("exact", "Exact identity", route => route.AddAnnotationStep("annotation-0"));
            options.Interaction.AddScenario("caption", "Caption route", route => route.AddAnnotationStep("Target"));
        }), 800, 820);
        var page = session.Page;
        await page.Locator("[data-cfx-scenario=exact]").ClickAsync();
        Assert.Equal(new[] { "annotation-0" }, await page.Locator("[data-cfx-role=annotation].cfx-scenario-active")
            .EvaluateAllAsync<string[]>("nodes=>nodes.map(n=>n.dataset.cfxTargetId)"));
        await page.Locator("[data-cfx-scenario-step-index='1']").ClickAsync();
        Assert.Equal(new[] { "annotation-0" }, await page.Locator("[data-cfx-role=annotation].cfx-scenario-step-active")
            .EvaluateAllAsync<string[]>("nodes=>nodes.map(n=>n.dataset.cfxTargetId)"));
        // Caption routes intentionally describe all equal captions; they do not masquerade as a returned opaque identity.
        await page.Locator("[data-cfx-scenario=caption]").ClickAsync();
        Assert.Equal(new[] { "annotation-0", "annotation-1" }, await page.Locator("[data-cfx-role=annotation].cfx-scenario-active")
            .EvaluateAllAsync<string[]>("nodes=>nodes.map(n=>n.dataset.cfxTargetId)"));
        await Capture(session, "annotation-scenario-identity-and-caption");
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AutomaticAnnotationIdentitiesStayLocalToPeersAndClearOnFullReplacement(bool dark) {
        if (!Enabled) return;
        var charts = new[] { PeerModel(dark), PeerModel(dark, reversed: true) };
        var html = charts.ToInteractiveHtmlDashboardPage(options => {
            options.Columns = 1;
            options.Interaction.GroupName = "annotation-local-identity";
            options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Selection
                | ChartInteractionFeatures.KeyboardNavigation | ChartInteractionFeatures.SynchronizedCharts | ChartInteractionFeatures.StateBookmarks;
        });
        await using var session = await OpenAsync(html, dark ? 390 : 800, 980);
        var page = session.Page;
        var roots = page.Locator(Root);
        await roots.Nth(0).EvaluateAsync("""
            root=>{window.annotationRoot=root;window.annotationController=ChartForgeXCharts.get(root);window.annotationEvents=[];
                root.addEventListener('cfxselectionchange',event=>annotationEvents.push(event.detail));}
            """);
        var first = roots.Nth(0).Locator(Annotation(0));
        await FocusWithKeyboardAsync(page, first);
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("annotation-0", await SelectedId(page));
        Assert.Equal(0, await roots.Nth(1).Locator(".cfx-selected,.cfx-highlighted,.cfx-hovered").CountAsync());
        await page.EvaluateAsync("()=>annotationController.highlight({targets:[annotationController.resolveTargets().find(t=>t.targetKind==='annotation')]},{sync:true,dim:true})");
        Assert.Equal(0, await roots.Nth(1).Locator(".cfx-highlighted,.cfx-linked-dim").CountAsync());

        var transport = await page.EvaluateAsync<JsonElement>("""
            ()=>{
                const source=annotationController,peer=ChartForgeXCharts.get(document.querySelectorAll('.cfx-interactive-chart')[1]),
                    targets=source.resolveTargets(),annotation=targets.find(t=>t.targetKind==='annotation'),
                    local=targets.find(t=>t.observationId==='local-east'),shared=targets.find(t=>t.observationId==='shared-east');
                const peerHighlights=()=>[...peer.root.querySelectorAll('.cfx-highlighted')].map(n=>({
                    kind:n.dataset.cfxTargetKind,observation:n.dataset.cfxObservationId,category:n.dataset.cfxXLabel}));
                const cleared=[
                    {targets:[{targetKind:annotation.targetKind,targetId:annotation.targetId,seriesScope:'shared'}]},
                    {targets:[{...local,seriesScope:'shared'}]},
                    {observationIds:['local-east']}, {seriesKeys:[local.seriesKey]},
                    {sourcePoints:[{seriesKey:local.seriesKey,index:Number(local.sourcePoint)}]},
                    {seriesKey:local.seriesKey,observationIds:['local-east']},
                    {seriesKey:local.seriesKey,ranges:[[0,10]]}, {targets:[]}
                ].map(match=>{
                    source.highlight({targets:[shared]},{sync:true,dim:true});
                    const before=peerHighlights().length;source.highlight(match,{sync:true,dim:true});
                    return {match,before,after:peerHighlights().length,dimmed:peer.root.querySelectorAll('.cfx-linked-dim').length};
                });
                const highlight=match=>{source.highlight(match,{sync:true,dim:true});return peerHighlights();};
                const sharedObservation=highlight({observationIds:['shared-east']}),
                    sharedPoint=highlight({sourcePoints:[{seriesKey:shared.seriesKey,index:Number(shared.sourcePoint)}]}),
                    sharedSeries=highlight({seriesKeys:[shared.seriesKey]}),
                    categories=highlight({targets:[annotation],categories:['East']}),ranges=highlight({ranges:[[0,10]]});
                source.clearHighlight({sync:true});
                source.root.dispatchEvent(new CustomEvent('cfx-apply-state',{detail:{snapshot:{selectedTargets:[
                    {targetKind:annotation.targetKind,targetId:annotation.targetId,seriesScope:'shared'},
                    {targetKind:local.targetKind,targetId:local.targetId,seriesScope:'shared'},
                    {targetKind:shared.targetKind,targetId:shared.targetId}, {targetKind:'annotation',targetId:'missing'}]}}}));
                return {cleared,sharedObservation,sharedPoint,sharedSeries,categories,ranges,
                    sourceState:source.getSelection(),peerState:peer.getSelection()};
            }
            """);
        foreach (var result in transport.GetProperty("cleared").EnumerateArray()) {
            Assert.Equal(1, result.GetProperty("before").GetInt32());
            Assert.Equal(0, result.GetProperty("after").GetInt32());
            Assert.Equal(0, result.GetProperty("dimmed").GetInt32());
        }
        foreach (var field in new[] { "sharedObservation", "sharedPoint" })
            Assert.Equal("shared-east", Assert.Single(transport.GetProperty(field).EnumerateArray()).GetProperty("observation").GetString());
        Assert.Equal(2, transport.GetProperty("sharedSeries").GetArrayLength());
        Assert.Equal(2, transport.GetProperty("categories").GetArrayLength());
        Assert.All(transport.GetProperty("categories").EnumerateArray(), target => Assert.Equal("East", target.GetProperty("category").GetString()));
        Assert.Equal(5, transport.GetProperty("ranges").GetArrayLength());
        Assert.Equal(3, transport.GetProperty("sourceState").GetArrayLength());
        Assert.Equal("shared-east", Assert.Single(transport.GetProperty("peerState").EnumerateArray()).GetProperty("observationId").GetString());

        var replacement = Model(dark, reversed: true).ToInteractiveHtmlFragmentWithoutAssets(Options);
        await page.EvaluateAsync("fragment=>annotationController.update(fragment)", replacement);
        Assert.Empty(await page.EvaluateAsync<string[]>("()=>annotationController.getSelection().map(t=>t.targetId)"));
        Assert.Equal(0, await roots.Nth(0).Locator(".cfx-highlighted,[data-cfx-state-outline]").CountAsync());
        Assert.True(await roots.Nth(0).EvaluateAsync<bool>("root=>root.getRootNode().activeElement===root"));
        await Capture(session, "local-annotation-replacement-" + dark);
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task SharedObservationHighlightFollowsItsCurrentAggregateAcrossPeersAndRepeatedReplacement() {
        if (!Enabled) return;
        await using var session = await OpenAsync(new[] { Parts(2), Parts(3) }.ToInteractiveHtmlDashboardPage(options => {
            options.Columns = 1;
            options.Interaction.Features = ChartInteractionFeatures.Selection | ChartInteractionFeatures.SynchronizedCharts;
            options.Interaction.GroupName = "aggregate-observation-intent";
        }), 700, 900);
        var page = session.Page;
        await page.EvaluateAsync("""
            ()=>{const roots=document.querySelectorAll('.cfx-interactive-chart');window.annotationController=ChartForgeXCharts.get(roots[0]);
                annotationController.highlight({observationIds:['part-b']},{sync:true});}
            """);
        Assert.Equal(new[] { 60d, 30d }, await HighlightedValues());
        foreach (var limit in new[] { 4, 2 }) {
            await page.EvaluateAsync("fragment=>annotationController.update(fragment)", Parts(limit).ToInteractiveHtmlFragmentWithoutAssets(Options));
            Assert.Equal(new[] { limit == 4 ? 30d : 60d, 30d }, await HighlightedValues());
        }
        await Capture(session, "shared-observation-aggregate-intent");
        AssertNoConsoleErrors(session);

        Task<double[]> HighlightedValues() => page.EvaluateAsync<double[]>("""
            ()=>[...document.querySelectorAll('.cfx-interactive-chart')].map(root=>{
                const controller=ChartForgeXCharts.get(root),expected=controller.resolveTargets({observationIds:['part-b']}),
                    actual=[...root.querySelectorAll('.cfx-highlighted')];
                if(expected.length!==1||actual.length!==1||actual[0].dataset.cfxTargetId!==expected[0].targetId)
                    throw new Error('Observation intent must highlight exactly its current raw or aggregated target.');
                return expected[0].numericValue;
            })
            """);
    }

    private static Chart Model(bool dark, bool reversed = false) {
        var chart = Chart.Create().WithSize(640, 360).WithHeader(false).WithLegend(false)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 10);
        chart.AddHorizontalLine(reversed ? 7 : 3, "Target").AddHorizontalLine(reversed ? 3 : 7, "Target")
            .AddVerticalLine(5).AddVerticalLine(5).AddHorizontalBand(4, 6, "annotation-0");
        return chart;
    }

    private static Chart PeerModel(bool dark, bool reversed = false) {
        var chart = Model(dark, reversed).WithXLabels("East", "West");
        chart.AddSeries(ChartSeries.CreateScatter(new ChartSeriesId("shared-readings"), "Shared readings",
            new[] { new ChartPoint(1, 2), new ChartPoint(2, 8) })
            .WithObservationId(0, new ChartObservationId("shared-east")).WithObservationId(1, new ChartObservationId("shared-west")));
        chart.AddScatter("Local readings", new[] { new ChartPoint(1, 3), new ChartPoint(2, 7), new ChartPoint(3, 5) });
        chart.Series[^1].WithObservationId(0, new ChartObservationId("local-east")).WithObservationId(1, new ChartObservationId("local-west"))
            .WithObservationId(2, new ChartObservationId("shared-east"));
        return chart;
    }

    private static Chart Parts(int maximumSlices) {
        var series = new ChartSeries(new ChartSeriesId("parts"), "Parts", ChartSeriesKind.Pie,
            new[] { new ChartPoint(1, 40), new ChartPoint(2, 30), new ChartPoint(3, 20), new ChartPoint(4, 10) });
        foreach (var (id, index) in new[] { "part-a", "part-b", "part-c", "part-d" }.Select((id, index) => (id, index)))
            series.WithObservationId(index, new ChartObservationId(id));
        var chart = Chart.Create().WithSize(520, 320).WithHeader(false).WithLegend(false).AddSeries(series);
        chart.Options.MaximumPieSlices = maximumSlices;
        return chart;
    }

    private static void Options(HtmlChartInteractionOptions options) {
        options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Selection
            | ChartInteractionFeatures.KeyboardNavigation | ChartInteractionFeatures.Export | ChartInteractionFeatures.Scenarios;
        options.Tooltip.PinOnSelect = false;
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
    }

    private static Task Bind(IPage page) => page.EvaluateAsync("""
        ()=>{window.annotationRoot=document.querySelector('.cfx-interactive-chart');window.annotationController=ChartForgeXCharts.get(annotationRoot);
            window.annotationEvents=[];annotationRoot.addEventListener('cfxselectionchange',event=>annotationEvents.push(event.detail));}
        """);

    private static Task<string> SelectedId(IPage page) => page.EvaluateAsync<string>("""
        ()=>{const selected=annotationController.getSelection();if(selected.length!==1)throw new Error('Expected exactly one annotation target.');return selected[0].targetId;}
        """);

    private static Task<string> NativePaint(IPage page) => page.EvaluateAsync<string>("""
        ()=>JSON.stringify([...annotationRoot.querySelectorAll('[data-cfx-role=annotation-line],[data-cfx-role=annotation-band]')].map(n=>({
            source:n.closest('[data-cfx-role=annotation]').dataset.cfxSourceId,
            paint:['fill','fill-opacity','stroke','stroke-opacity','stroke-width','stroke-dasharray','opacity'].map(k=>n.getAttribute(k))})))
        """);

    private static async Task AssertExports(IPage page, bool dark) {
        var svgDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").EvaluateAsync("button=>button.click()"));
        var svg = await File.ReadAllTextAsync(await svgDownload.PathAsync() ?? throw new InvalidOperationException("No annotation SVG export."));
        var document = XDocument.Parse(svg);
        var annotations = document.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "annotation").ToArray();
        Assert.Equal(5, annotations.Length);
        Assert.Equal(5, annotations.Select(node => (string?)node.Attribute("data-cfx-target-id")).Distinct().Count());
        Assert.Equal("annotation-1", (string?)Assert.Single(annotations, node => ((string?)node.Attribute("class") ?? "").Split(' ').Contains("cfx-selected")).Attribute("data-cfx-source-id"));
        Assert.DoesNotContain("data-cfx-keyboard-focus-outline", svg, StringComparison.Ordinal);
        var pngDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=png]").EvaluateAsync("button=>button.click()"));
        var png = await File.ReadAllBytesAsync(await pngDownload.PathAsync() ?? throw new InvalidOperationException("No annotation PNG export."));
        var image = PngReader.Decode(png);
        Assert.True(image.Width > 0 && image.Height > 0);
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(capture)) return;
        Directory.CreateDirectory(capture);
        await File.WriteAllTextAsync(Path.Combine(capture, "annotation-export-" + dark + ".svg"), svg);
        await File.WriteAllBytesAsync(Path.Combine(capture, "annotation-export-" + dark + ".png"), png);
    }

    private static async Task Capture(HtmlBrowserSession session, string name) {
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(capture)) return;
        Directory.CreateDirectory(capture);
        await session.Page.ScreenshotAsync(new() { Path = Path.Combine(capture, name + ".png"), FullPage = true });
    }
}
