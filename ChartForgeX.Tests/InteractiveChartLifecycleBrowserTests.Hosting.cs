using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveChartLifecycleBrowserTests {
    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PointerReadoutsCloseWithoutPinningAndKeyboardFocusKeepsItsReadout(bool dark) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Readings(dark).ToInteractiveHtmlPage(options => {
            Fit(options); options.Tooltip.PinOnSelect = false;
        }), 820, 700);
        var page = session.Page;
        Assert.True(await page.EvaluateAsync<bool>("()=>getComputedStyle(document.body).backgroundColor===getComputedStyle(document.querySelector('main')).backgroundColor"));
        await page.Locator(North).ClickAsync();
        Assert.NotEqual("true", await page.Locator(Root).GetAttributeAsync("data-cfx-tooltip-pinned"));
        Assert.False(await page.Locator(North).EvaluateAsync<bool>("node=>node.matches(':focus-visible')"));
        Assert.Equal("none", await page.Locator(North).EvaluateAsync<string>("node=>getComputedStyle(node).outlineStyle"));
        await MoveAwayAsync(page);
        Assert.True(await page.Locator(".cfx-tooltip").EvaluateAsync<bool>("tip=>tip.hidden"));
        await page.Keyboard.PressAsync("Tab");
        await page.Locator(North).FocusAsync();
        Assert.True(await page.Locator(North).EvaluateAsync<bool>("node=>node.matches(':focus-visible')"));
        Assert.NotEmpty(await TooltipTextAsync(page));
        Assert.Equal("solid", await page.Locator(North).EvaluateAsync<string>("node=>getComputedStyle(node).outlineStyle"));
        await page.Keyboard.PressAsync("Space");
        Assert.NotEqual("true", await page.Locator(Root).GetAttributeAsync("data-cfx-tooltip-pinned"));
        await MoveAwayAsync(page);
        Assert.NotEmpty(await TooltipTextAsync(page));
        await CaptureAsync(page, "hosting-keyboard-" + (dark ? "dark" : "light"));
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task PublicUnpinCommandsCloseDefaultPinnedReadoutsWithoutBookmarkFeatures() {
        if (!Enabled) return;
        await using var session = await OpenAsync(Readings().ToInteractiveHtmlPage(Fit), 820, 700);
        var page = session.Page;
        await BindControllerAsync(page);
        await page.Locator(North).ClickAsync();
        Assert.Equal("true", await page.Locator(Root).GetAttributeAsync("data-cfx-tooltip-pinned"));
        await page.EvaluateAsync("()=>chartController.unpinTooltip()");
        Assert.Null(await page.Locator(Root).GetAttributeAsync("data-cfx-tooltip-pinned"));
        Assert.True(await page.Locator(".cfx-tooltip").EvaluateAsync<bool>("tip=>tip.hidden"));
        await page.Locator(North).ClickAsync();
        Assert.Equal("true", await page.Locator(Root).GetAttributeAsync("data-cfx-tooltip-pinned"));
        await page.Locator(Root).EvaluateAsync("root=>root.dispatchEvent(new CustomEvent('cfx-unpin-tooltip'))");
        Assert.Null(await page.Locator(Root).GetAttributeAsync("data-cfx-tooltip-pinned"));
        Assert.True(await page.Locator(".cfx-tooltip").EvaluateAsync<bool>("tip=>tip.hidden"));
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task AdaptiveNativeDrawingsRetainSelectionMuteAndHighlightAcrossContainerChanges(bool dark, bool explicitIdentity) {
        if (!Enabled) return;
        var chart = Readings(dark, explicitIdentity: explicitIdentity);
        var selector = "g[data-cfx-role=point][data-cfx-series-key=samples][data-cfx-point='0']";
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
            options.Toolbar = HtmlChartToolbarLayout.Compact;
            options.Interaction.Enable(ChartInteractionFeatures.Zoom);
        }), 1440, 800);
        var page = session.Page;
        await BindControllerAsync(page);
        await page.Locator(selector).ClickAsync();
        await page.Locator("[data-cfx-role=legend-item][data-cfx-series-key=reference]").ClickAsync();
        await page.EvaluateAsync("()=>chartController.highlight({sourcePoints:[{seriesKey:'samples',index:0}]},{dim:true,source:'linked-grid'})");
        await page.Locator(Root).EvaluateAsync("root=>{window.adaptiveRoot=root;window.layoutEvents=[];root.addEventListener('cfxlayoutchange',event=>layoutEvents.push(event.detail));}");
        for (var transition = 0; transition < 4; transition++) {
            var narrow = transition % 2 == 0;
            await page.SetViewportSizeAsync(narrow ? 390 : 1440, 800);
            await page.WaitForFunctionAsync("drawing=>document.querySelector('.cfx-interactive-chart').dataset.cfxAdaptiveDrawing===drawing", narrow ? "narrow" : "wide");
            Assert.Equal("true", await page.Locator(selector).GetAttributeAsync("aria-selected"));
            Assert.Equal("true", await page.Locator(selector).GetAttributeAsync("data-cfx-highlighted"));
            Assert.Equal("true", await page.Locator("[data-cfx-role=legend-item][data-cfx-series-key=reference]").GetAttributeAsync("data-cfx-muted"));
            Assert.Equal(1, await page.EvaluateAsync<int>("()=>chartController.getSelection().length"));
            Assert.True(await page.Locator(Root).EvaluateAsync<bool>("root=>root===adaptiveRoot&&ChartForgeXCharts.get(root)===chartController&&root.querySelectorAll('.cfx-stage svg').length===1"));
            var size = await page.Locator(".cfx-stage svg").GetAttributeAsync("viewBox");
            Assert.Equal(narrow ? "0 0 360 360" : "0 0 640 360", size);
            Assert.True(await page.Locator(".cfx-stage").EvaluateAsync<bool>("stage=>stage.scrollWidth<=stage.clientWidth+1"));
            Assert.True(await page.Locator(Root).EvaluateAsync<bool>("root=>Math.abs(root.querySelector('.cfx-toolbar').getBoundingClientRect().right-root.querySelector('.cfx-frame').getBoundingClientRect().right)<1"));
            await CaptureAsync(page, "hosting-adaptive-" + (dark ? "dark" : "light") + "-" + transition);
        }
        Assert.Equal(4, await page.EvaluateAsync<int>("()=>layoutEvents.length"));
        await page.SetViewportSizeAsync(390, 800);
        await page.WaitForFunctionAsync("()=>adaptiveRoot.dataset.cfxAdaptiveDrawing==='narrow'");
        await page.EvaluateAsync("()=>{chartController.dispose();window.chartController=ChartForgeXCharts.mount(adaptiveRoot);}");
        Assert.Equal("narrow", await page.Locator(Root).GetAttributeAsync("data-cfx-adaptive-drawing"));
        await page.SetViewportSizeAsync(1440, 800);
        await page.WaitForFunctionAsync("()=>adaptiveRoot.dataset.cfxAdaptiveDrawing==='wide'");
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ToolbarDensityUsesTheChartContainerInsteadOfTheWindow(bool dark) {
        if (!Enabled) return;
        var chart = Readings(dark).WithTransparentBackground(false);
        await using var session = await OpenAsync(RuntimePage(), 1440, 800);
        var page = session.Page;
        string Section(HtmlChartToolbarLayout toolbar) => chart.ToInteractiveHtmlFragmentWithoutAssets(options => {
            Fit(options); options.Toolbar = toolbar; options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Brush);
        });
        await InsertAsync(page, Section(HtmlChartToolbarLayout.Band));
        await BindControllerAsync(page);
        await page.Locator(Root).EvaluateAsync("root=>root.style.width='360px'");
        await page.WaitForFunctionAsync("()=>getComputedStyle(document.querySelector('.cfx-toolbar .cfx-tool')).minHeight==='36px'");
        var band = await page.Locator(".cfx-toolbar .cfx-tool").First.EvaluateAsync<JsonElement>("button=>({height:button.getBoundingClientRect().height,grow:getComputedStyle(button).flexGrow})");
        Assert.Equal("1", band.GetProperty("grow").GetString());
        await UpdateAsync(page, Section(HtmlChartToolbarLayout.Compact));
        var compact = await page.Locator(".cfx-toolbar .cfx-tool").First.EvaluateAsync<JsonElement>("button=>({height:button.getBoundingClientRect().height,grow:getComputedStyle(button).flexGrow})");
        Assert.Equal("0", compact.GetProperty("grow").GetString());
        Assert.Equal(28, compact.GetProperty("height").GetDouble());
        Assert.True(compact.GetProperty("height").GetDouble() < band.GetProperty("height").GetDouble());
        var background = chart.Options.ResolvedTheme.Background;
        Assert.Equal($"rgb({background.R}, {background.G}, {background.B})", await page.Locator(Root).EvaluateAsync<string>("root=>getComputedStyle(root).backgroundColor"));
        Assert.Equal("rgba(0, 0, 0, 0)", await page.Locator("body").EvaluateAsync<string>("body=>getComputedStyle(body).backgroundColor"));
        Assert.Equal("rgba(0, 0, 0, 0)", await page.Locator(".cfx-toolbar").EvaluateAsync<string>("toolbar=>getComputedStyle(toolbar).backgroundColor"));
        await CaptureAsync(page, "hosting-compact-toolbar-" + (dark ? "dark" : "light"));
        chart.WithTransparentBackground();
        await UpdateAsync(page, Section(HtmlChartToolbarLayout.Compact));
        Assert.Equal("rgba(0, 0, 0, 0)", await page.Locator(Root).EvaluateAsync<string>("root=>getComputedStyle(root).backgroundColor"));
        var surface = chart.Options.ResolvedTheme.CardBackground;
        Assert.Equal($"rgb({surface.R}, {surface.G}, {surface.B})", await page.Locator(".cfx-toolbar .cfx-tool").First.EvaluateAsync<string>("button=>getComputedStyle(button).backgroundColor"));
        await CaptureAsync(page, "hosting-compact-transparent-" + (dark ? "dark" : "light"));
        chart.WithTransparentBackground(false);
        chart.Options.HostOwnsFrame = true;
        await UpdateAsync(page, Section(HtmlChartToolbarLayout.Compact));
        Assert.Equal("rgba(0, 0, 0, 0)", await page.Locator(Root).EvaluateAsync<string>("root=>getComputedStyle(root).backgroundColor"));
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task InvalidAdaptiveReplacementRetainsTheWorkingSceneAndItsObserver() {
        if (!Enabled) return;
        await using var session = await OpenAsync(Readings().ToInteractiveHtmlPage(options => options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive), 1440, 800);
        var page = session.Page;
        await BindControllerAsync(page);
        await page.Locator(North).ClickAsync();
        var result = await page.EvaluateAsync<JsonElement>("""
            () => {
                const root=chartController.root,original=root.innerHTML,next=root.cloneNode(true);
                next.removeAttribute('data-cfx-runtime-bound');
                next.querySelector('template[data-cfx-adaptive-narrow]').content.firstElementChild.dataset.cfxPreparedChart='{}';
                let rejected=false;try{chartController.update(next);}catch{rejected=true;}
                return {rejected,unchanged:root.innerHTML===original};
            }
            """);
        Assert.True(result.GetProperty("rejected").GetBoolean());
        Assert.True(result.GetProperty("unchanged").GetBoolean());
        await page.SetViewportSizeAsync(390, 800);
        await page.WaitForFunctionAsync("()=>chartController.root.dataset.cfxAdaptiveDrawing==='narrow'");
        Assert.Equal("true", await page.Locator(North).GetAttributeAsync("aria-selected"));
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task AdaptiveNativeXRequestsUseTheCapturedDrawingAndCarryItsImmutableBasisAcrossTransitions() {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(640, 360).WithTitle("Native adaptive window")
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 12)
            .AddSeries(ChartSeries.CreateLine(new ChartSeriesId("measurements"), "Measurements",
                Enumerable.Range(0, 6).Select(index => new ChartPoint(index * 2, index + 2))));
        void Configure(HtmlChartInteractionOptions options) {
            options.Interaction.ChartId = "adaptive-native-window";
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
            options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan);
        }
        var renderer = new HtmlInteractiveChartRenderer();
        var initial = renderer.PrepareSection(chart, configure: Configure);
        var sections = new Dictionary<string, HtmlChartSection> { ["initial"] = initial };
        var drawings = new List<string>();
        HtmlChartSection? response = null;
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(Configure), 1440, 800);
        var page = session.Page;
        await page.ExposeFunctionAsync("prepareAdaptiveWindow", (JsonElement request) => {
            var token = request.GetProperty("basis").GetString()!;
            var drawing = request.GetProperty("drawing").GetString()!;
            var basis = drawing == "narrow" ? sections[token].NarrowPreparedVisual! : sections[token].PreparedVisual;
            var window = request.GetProperty("kind").GetString() == "reset" ? null
                : basis.XProjection!.ConstrainViewport(request.GetProperty("left").GetDouble(), request.GetProperty("right").GetDouble());
            response = renderer.PrepareSection(chart, window, Configure);
            var nextToken = "candidate-" + sections.Count;
            sections[nextToken] = response;
            drawings.Add(drawing);
            return new { token = nextToken, html = response.Html };
        });
        await page.EvaluateAsync("""
            () => {
              const root=document.querySelector('.cfx-interactive-chart');
              window.chartController=ChartForgeXCharts.get(root);
              window.adaptiveNativeChanges=[];window.adaptiveNativeErrors=[];
              const bases=new Map([[chartController.sceneRevision,'initial']]),candidates=new Map();
              root.addEventListener('cfxlayoutchange',({detail})=>bases.set(detail.sceneRevision,bases.get(detail.previousSceneRevision)));
              root.addEventListener('cfxviewportchange',({detail})=>{
                bases.set(detail.sceneRevision,candidates.get(detail.requestId));candidates.delete(detail.requestId);
                adaptiveNativeChanges.push(detail);
              });
              root.addEventListener('cfxviewporterror',({detail})=>adaptiveNativeErrors.push(detail));
              chartController.connectXViewport(async(request,signal)=>{
                const basis=bases.get(request.sceneRevision);
                if(!basis)throw new Error('No immutable native basis for this scene revision.');
                const result=await prepareAdaptiveWindow({...request,basis});
                if(signal.aborted)throw new DOMException('Canceled','AbortError');
                candidates.set(request.requestId,result.token);return result.html;
              });
            }
            """);
        await page.SetViewportSizeAsync(390, 800);
        await page.WaitForFunctionAsync("()=>chartController.root.dataset.cfxAdaptiveDrawing==='narrow'");
        await page.Locator("[data-cfx-zoom=in]").ClickAsync();
        await page.WaitForFunctionAsync("()=>adaptiveNativeChanges.length===1");
        Assert.Equal("narrow", Assert.Single(drawings));
        Assert.NotNull(response);
        Assert.Equal(1, response.PreparedVisual.XProjection!.Minimum, 8);
        Assert.Equal(9, response.PreparedVisual.XProjection.Maximum, 8);
        await page.SetViewportSizeAsync(1440, 800);
        await page.WaitForFunctionAsync("()=>chartController.root.dataset.cfxAdaptiveDrawing==='wide'");
        await page.Locator("[data-cfx-zoom=in]").ClickAsync();
        await page.WaitForFunctionAsync("()=>adaptiveNativeChanges.length===2");
        Assert.Equal(new[] { "narrow", "wide" }, drawings);
        Assert.Equal(1.8, response!.PreparedVisual.XProjection!.Minimum, 8);
        Assert.Equal(8.2, response.PreparedVisual.XProjection.Maximum, 8);
        Assert.Empty(await page.EvaluateAsync<JsonElement[]>("()=>adaptiveNativeErrors"));
        await CaptureAsync(page, "hosting-adaptive-native-window");
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData("series", false)]
    [InlineData("series", true)]
    [InlineData("local-series", false)]
    [InlineData("local-series", true)]
    [InlineData("pie", false)]
    [InlineData("pie", true)]
    [InlineData("hierarchy", false)]
    [InlineData("hierarchy", true)]
    public async Task AdaptiveLegendBudgetsRetainCanonicalMuteAndIsolationWhenControlsAreOmitted(string family, bool isolated) {
        if (!Enabled) return;
        var labels = new[] { "Service request measures North", "Service request measures South", "Service request measures West" };
        var chart = Chart.Create().WithSize(900, 360).WithTitle("Adaptive legend state").WithLegend().WithLegendBudget(maximumRows: 1);
        string legend, mark;
        if (family is "series" or "local-series") {
            for (var index = 0; index < labels.Length; index++) {
                var points = new[] { new ChartPoint(0, index + 2), new ChartPoint(1, index + 4) };
                chart.AddSeries(family == "series" ? ChartSeries.CreateLine(new ChartSeriesId("area-" + index), labels[index], points)
                    : new ChartSeries(labels[index], ChartSeriesKind.Line, points));
            }
            legend = "[data-cfx-role=legend-item][data-cfx-series='2']";
            mark = "[data-cfx-role=series][data-cfx-series='2']";
        } else {
            var series = family == "hierarchy"
                ? ChartSeries.CreateTreemap(new ChartSeriesId("areas"), "Areas", labels.Select((label, index) => new ChartHierarchyItem("area-" + index, label, value: 4 - index)))
                : new ChartSeries(new ChartSeriesId("areas"), "Areas", ChartSeriesKind.Pie,
                    Enumerable.Range(0, 3).Select(index => new ChartPoint(index + 1, 4 - index)));
            for (var index = 0; index < 3; index++) series.WithObservationId(index, new ChartObservationId("area-" + index));
            chart.WithXLabels(labels).WithPointLegend().WithDataLabels(false).AddSeries(series);
            legend = family == "hierarchy" ? "[data-cfx-role=legend-item][data-cfx-legend-target-id=area-2]"
                : "[data-cfx-role=legend-item][data-cfx-observation-id=area-2]";
            mark = family == "hierarchy" ? "[data-cfx-target-kind=node][data-cfx-target-id=area-2]"
                : "g[data-cfx-target-kind=point][data-cfx-observation-id=area-2]";
        }
        void Configure(HtmlChartInteractionOptions options) => options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(Configure), 1440, 800);
        var page = session.Page;
        await BindControllerAsync(page);
        await page.Locator(legend).FocusAsync();
        await page.Keyboard.PressAsync(isolated ? "i" : "Space");
        var stateClass = isolated ? "cfx-series-isolated-in" : "cfx-series-muted";
        for (var transition = 0; transition < 4; transition++) {
            var narrow = transition % 2 == 0;
            await page.SetViewportSizeAsync(narrow ? 390 : 1440, 800);
            await page.WaitForFunctionAsync("drawing=>chartController.root.dataset.cfxAdaptiveDrawing===drawing", narrow ? "narrow" : "wide");
            Assert.True(await page.Locator(mark).EvaluateAsync<bool>("(node,state)=>node.classList.contains(state)", stateClass));
            if (narrow) Assert.Equal(0, await page.Locator(legend).CountAsync());
            else Assert.Equal("true", await page.Locator(legend).GetAttributeAsync(isolated ? "data-cfx-isolated" : "data-cfx-muted"));
        }
        if (family == "local-series") {
            var replacement = Chart.Create().WithSize(900, 360).WithLegend().AddLine("Unrelated measurements", new[] { new ChartPoint(0, 5), new ChartPoint(1, 7) });
            await UpdateAsync(page, replacement.ToInteractiveHtmlFragmentWithoutAssets(Configure));
            Assert.Equal(0, await page.Locator(".cfx-series-muted,.cfx-series-isolated-in,.cfx-series-isolated-out").CountAsync());
        }
        await CaptureAsync(page, "hosting-legend-budget-" + family + "-" + isolated);
        AssertNoConsoleErrors(session);
    }
}
