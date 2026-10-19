using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Raster;
using HtmlTinkerX;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class AnnotationTargetIdentityBrowserTests {
    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AuthoredAnnotationsRetainStateAcrossReplacementAndRejectInvalidCatalogsAtomically(bool dark) {
        if (!Enabled) return;
        var html = AuthoredModel(dark).ToInteractiveHtmlPage(AuthoredOptions);
        await using var session = await OpenAsync(html, dark ? 390 : 900, 800);
        var page = session.Page;
        await Bind(page);
        await page.Locator("[data-cfx-scenario=authored-band]").ClickAsync();
        Assert.Equal(new[] { "annotation-0" }, await page.Locator("[data-cfx-role=annotation].cfx-scenario-active")
            .EvaluateAllAsync<string[]>("nodes=>nodes.map(n=>n.dataset.cfxAnnotationId)"));
        var warning = page.Locator("[data-cfx-role=annotation][data-cfx-annotation-id=warning]");
        await FocusWithKeyboardAsync(page, warning);
        await page.Keyboard.PressAsync("Space");
        var original = await page.EvaluateAsync<JsonElement>("""
            ()=>{const target=annotationController.getSelection()[0];
                annotationController.highlight({targets:[target]},{dim:false});
                annotationRoot.addEventListener('cfxstate',event=>window.authoredSnapshot=JSON.parse(JSON.stringify(event.detail.snapshot)),{once:true});
                annotationRoot.dispatchEvent(new CustomEvent('cfx-capture-state',{detail:{sync:false}}));
                annotationController.clearSelection();
                annotationRoot.dispatchEvent(new CustomEvent('cfx-apply-state',{detail:{snapshot:authoredSnapshot,sync:false}}));
                return annotationController.getSelection()[0];}
            """);
        Assert.Equal("warning", original.GetProperty("annotationId").GetString());
        var replacement = AuthoredModel(dark, reversed: true).ToInteractiveHtmlFragmentWithoutAssets(AuthoredOptions);
        await page.EvaluateAsync("fragment=>annotationController.update(fragment)", replacement);
        var retained = await page.EvaluateAsync<JsonElement>("""
            ()=>({selected:annotationController.getSelection(),
                highlighted:[...annotationRoot.querySelectorAll('[data-cfx-role=annotation].cfx-highlighted')].map(n=>n.dataset.cfxAnnotationId),
                focused:annotationRoot.getRootNode().activeElement.dataset.cfxAnnotationId})
            """);
        var selected = Assert.Single(retained.GetProperty("selected").EnumerateArray());
        Assert.Equal("warning", selected.GetProperty("annotationId").GetString());
        Assert.Equal("Localized limit", selected.GetProperty("label").GetString());
        Assert.Equal(6, selected.GetProperty("numericValue").GetDouble());
        Assert.Equal(original.GetProperty("targetId").GetString(), selected.GetProperty("targetId").GetString());
        Assert.Equal(new[] { "warning" }, retained.GetProperty("highlighted").EnumerateArray().Select(item => item.GetString()));
        Assert.Equal("warning", retained.GetProperty("focused").GetString());

        await page.SetViewportSizeAsync(dark ? 900 : 390, 800);
        await page.WaitForFunctionAsync("drawing=>annotationRoot.dataset.cfxAdaptiveDrawing===drawing", dark ? "wide" : "narrow");
        Assert.Equal(original.GetProperty("targetId").GetString(), await SelectedId(page));
        Assert.True(await warning.EvaluateAsync<bool>("node=>node.getRootNode().activeElement===node"));
        var atomic = await page.EvaluateAsync<JsonElement>("""
            fragment=>[{role:'catalog'},...['annotation','annotation-line','annotation-band',
                'annotation-label','annotation-label-backplate'].flatMap(role=>
                    ['data-cfx-source-id','data-cfx-id','data-cfx-target-id'].map(field=>({role,field})))].map(({role,field})=>{
                const template=document.createElement('template');template.innerHTML=fragment;
                const next=template.content.firstElementChild,metadata=JSON.parse(next.dataset.cfxPreparedChart);
                if(role==='catalog'){
                    metadata.annotations[1].id=metadata.annotations[0].id;next.dataset.cfxPreparedChart=JSON.stringify(metadata);
                }else next.querySelector('[data-cfx-role="'+role+'"]'+(role==='annotation'?':not([data-cfx-annotation-id])':''))
                    .setAttribute(field,next.querySelector('[data-cfx-annotation-id=warning]').dataset.cfxSourceId);
                const before=annotationRoot.innerHTML,revision=annotationController.sceneRevision,focus=annotationRoot.getRootNode().activeElement;
                let rejected=false;try{annotationController.update(next);}catch(error){rejected=error instanceof TypeError;}
                return{field:role+'/'+field,rejected,unchanged:before===annotationRoot.innerHTML,revision:revision===annotationController.sceneRevision,
                    focus:focus===annotationRoot.getRootNode().activeElement,selected:annotationController.getSelection()};})
            """, replacement);
        Assert.All(atomic.EnumerateArray(), outcome => {
            Assert.True(outcome.GetProperty("rejected").GetBoolean(), outcome.GetProperty("field").GetString());
            Assert.True(outcome.GetProperty("unchanged").GetBoolean());
            Assert.True(outcome.GetProperty("revision").GetBoolean());
            Assert.True(outcome.GetProperty("focus").GetBoolean());
            Assert.Equal("warning", Assert.Single(outcome.GetProperty("selected").EnumerateArray()).GetProperty("annotationId").GetString());
        });
        await AuthoredExports(page, dark);
        await CaptureAuthored(session, "authored-annotation-retained-" + dark, html);

        var missing = AuthoredModel(dark, reversed: true, warningId: "replacement")
            .AddHorizontalLine(8, original.GetProperty("targetId").GetString()!).ToInteractiveHtmlFragmentWithoutAssets(AuthoredOptions);
        await page.EvaluateAsync("fragment=>annotationController.update(fragment)", missing);
        Assert.Empty(await page.EvaluateAsync<string[]>("()=>annotationController.getSelection().map(t=>t.annotationId)"));
        Assert.Equal(0, await page.Locator("[data-cfx-role=annotation].cfx-highlighted").CountAsync());
        Assert.True(await page.Locator(Root).EvaluateAsync<bool>("root=>root.getRootNode().activeElement===root"));
        await page.Locator("[data-cfx-scenario=authored-warning]").ClickAsync();
        Assert.Equal(0, await page.Locator("[data-cfx-role=annotation].cfx-scenario-active").CountAsync());
        await CaptureAuthored(session, "authored-annotation-removed-" + dark);
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AuthoredAnnotationPeersShareOnlyInsideTheExistingEnabledGroup(bool dark) {
        if (!Enabled) return;
        var fragments = Enumerable.Range(0, 5).Select(index => AuthoredModel(dark, reversed: index == 1,
            warningId: index == 2 ? "different-warning" : "warning").ToInteractiveHtmlFragmentWithoutAssets(options => {
                AuthoredOptions(options);
                options.Interaction.ChartId = "authored-peer-" + index;
                options.Interaction.GroupName = index == 3 ? "other-thresholds" : "thresholds";
                if (index != 4) options.Interaction.Features |= ChartInteractionFeatures.SynchronizedCharts;
            }));
        var html = "<!doctype html><html><head><style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle()
            + "</style></head><body>" + string.Join("\n", fragments) + "<script>"
            + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script></body></html>";
        await using var session = await OpenAsync(html, dark ? 390 : 900, 980);
        var page = session.Page;
        var shared = await page.EvaluateAsync<JsonElement>("""
            ()=>{const roots=[...document.querySelectorAll('.cfx-interactive-chart')],controllers=roots.map(root=>ChartForgeXCharts.get(root));
                window.authoredControllers=controllers;window.authoredSync=roots.map(()=>0);
                roots.forEach((root,index)=>root.addEventListener('cfxsync',()=>authoredSync[index]++));
                const source=controllers[0],target=source.resolveTargets().find(t=>t.annotationId==='warning');
                source.select({targets:[target]},{sync:true});source.highlight({targets:[target]},{sync:true});
                return{selected:controllers.map(c=>c.getSelection().map(t=>t.annotationId)),
                    highlighted:roots.map(root=>[...root.querySelectorAll('.cfx-highlighted')].map(n=>n.dataset.cfxAnnotationId)),sync:authoredSync};}
            """);
        foreach (var field in new[] { "selected", "highlighted" }) {
            var states = shared.GetProperty(field).EnumerateArray().ToArray();
            Assert.Equal(new[] { "warning" }, states[0].EnumerateArray().Select(item => item.GetString()));
            Assert.Equal(new[] { "warning" }, states[1].EnumerateArray().Select(item => item.GetString()));
            Assert.All(states.Skip(2), state => Assert.Empty(state.EnumerateArray()));
        }
        Assert.Equal(new[] { 2, 0, 0, 0, 0 }, shared.GetProperty("sync").EnumerateArray().Select(item => item.GetInt32()));
        var forged = await page.EvaluateAsync<JsonElement>("""
            ()=>{const source=authoredControllers[0],local=source.resolveTargets().find(t=>t.targetKind==='annotation'&&!t.annotationId),
                target={...local,annotationId:'warning',seriesScope:'shared'};
                source.select({targets:[target]},{sync:true});source.highlight({targets:[target]},{sync:true});
                return authoredControllers.map(c=>({selected:c.getSelection().length,highlighted:c.root.querySelectorAll('.cfx-highlighted').length}));}
            """);
        Assert.All(forged.EnumerateArray(), state => {
            Assert.Equal(0, state.GetProperty("selected").GetInt32());
            Assert.Equal(0, state.GetProperty("highlighted").GetInt32());
        });
        await CaptureAuthored(session, "authored-annotation-peer-scope-" + dark, html);
        AssertNoConsoleErrors(session);
    }

    private static Chart AuthoredModel(bool dark, bool reversed = false, string warningId = "warning") {
        var chart = Model(dark);
        chart.Annotations.Clear();
        chart.AddVerticalLine(5, "Limit");
        if (reversed) chart.AddHorizontalLine(new ChartAnnotationId("critical"), 7, "Limit");
        chart.AddHorizontalLine(new ChartAnnotationId(warningId), reversed ? 6 : 3, reversed ? "Localized limit" : "Limit");
        if (!reversed) chart.AddHorizontalLine(new ChartAnnotationId("critical"), 7, "Limit");
        return chart.AddHorizontalBand(new ChartAnnotationId("annotation-0"), 4, 5, "Range");
    }

    private static void AuthoredOptions(HtmlChartInteractionOptions options) {
        Options(options);
        options.Interaction.ChartId = "authored-thresholds";
        options.Interaction.Features |= ChartInteractionFeatures.StateBookmarks;
        options.Interaction.AddScenario("authored-warning", "Warning limit", route => route.AddAnnotationStep(new ChartAnnotationId("warning")));
        options.Interaction.AddScenario("authored-band", "Threshold range", route => route.AddAnnotationStep(new ChartAnnotationId("annotation-0")));
    }

    private static async Task AuthoredExports(IPage page, bool dark) {
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").EvaluateAsync("button=>button.click()"));
        var svg = await File.ReadAllTextAsync(await download.PathAsync() ?? throw new InvalidOperationException("No authored annotation SVG export."));
        var warning = Assert.Single(XDocument.Parse(svg).Descendants(), node => (string?)node.Attribute("data-cfx-annotation-id") == "warning");
        Assert.Equal("6", (string?)warning.Attribute("data-cfx-value"));
        Assert.Contains("cfx-selected", (string?)warning.Attribute("class") ?? "", StringComparison.Ordinal);
        var pngDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=png]").EvaluateAsync("button=>button.click()"));
        var png = await File.ReadAllBytesAsync(await pngDownload.PathAsync() ?? throw new InvalidOperationException("No authored annotation PNG export."));
        Assert.True(PngReader.Decode(png).Width > 0);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } capture) return;
        Directory.CreateDirectory(capture);
        await File.WriteAllTextAsync(Path.Combine(capture, "authored-annotation-export-" + dark + ".svg"), svg);
        await File.WriteAllBytesAsync(Path.Combine(capture, "authored-annotation-export-" + dark + ".png"), png);
    }

    private static async Task CaptureAuthored(HtmlBrowserSession session, string name, string? fixture = null) {
        await Capture(session, name);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } capture) return;
        var state = await session.Page.EvaluateAsync<JsonElement>("""
            ()=>[...document.querySelectorAll('.cfx-interactive-chart')].map(root=>({
                chartId:root.dataset.cfxChartId,targets:ChartForgeXCharts.get(root).resolveTargets(),
                selection:ChartForgeXCharts.get(root).getSelection(),
                focus:root.getRootNode().activeElement?.dataset.cfxAnnotationId}))
            """);
        await File.WriteAllTextAsync(Path.Combine(capture, name + ".runtime.json"), state.GetRawText());
        if (fixture != null) await File.WriteAllTextAsync(Path.Combine(capture, name + ".fixture.html"), fixture);
    }
}
