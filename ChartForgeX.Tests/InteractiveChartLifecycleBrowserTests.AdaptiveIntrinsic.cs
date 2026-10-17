using System.Text.Json;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveChartLifecycleBrowserTests {
    [BrowserTheory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task AdaptiveAutoSizedHostsRestoreTheAuthoredWideDrawingAfterNarrowTransitions(bool shadow, bool dark) {
        if (!Enabled) return;
        void Configure(HtmlChartInteractionOptions options) {
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
            options.Adaptive.NarrowWidth = 360;
            options.Interaction.ChartId = "adaptive-auto-host";
        }
        await using var session = await OpenAsync(Readings(dark).WithSize(800, 420).ToInteractiveHtmlPage(Configure), 950, 720);
        var page = session.Page;
        await BindControllerAsync(page);
        await page.EvaluateAsync("""
            shadow => {
                const root = chartController.root, outer = document.createElement('div'), inner = document.createElement('div');
                const contents = document.createElement('div');
                window.intrinsicRoot = root;
                root.before(outer);
                if (shadow) {
                    outer.attachShadow({mode:'closed'}).append(inner);
                    const style = document.querySelector('style[data-cfx-interactive-assets]') || document.querySelector('style');
                    inner.attachShadow({mode:'closed'}).append(style.cloneNode(true), contents);
                } else outer.append(contents);
                contents.style.cssText = 'display:contents;overflow:auto';
                contents.append(root);
                chartController.select({observationIds:['north']});
                window.intrinsicSelectionEvents = 0;
                root.addEventListener('cfxselectionchange', () => intrinsicSelectionEvents++);
            }
            """, shadow);
        var states = new List<JsonElement>();
        var widths = new[] { 950, 390, 950 };
        for (var transition = 0; transition < widths.Length; transition++) {
            var width = widths[transition];
            var drawing = width < 640 ? "narrow" : "wide";
            await page.SetViewportSizeAsync(width, 720);
            await page.WaitForFunctionAsync("drawing=>chartController.root.dataset.cfxAdaptiveDrawing===drawing", drawing,
                new PageWaitForFunctionOptions { Timeout = 10000 });
            var state = await IntrinsicAdaptiveHostStateAsync(page);
            states.Add(state);
            Assert.Equal(drawing, state.GetProperty("drawing").GetString());
            Assert.Equal(drawing == "narrow" ? "0 0 360 420" : "0 0 800 420", state.GetProperty("viewBox").GetString());
            Assert.True(state.GetProperty("sameController").GetBoolean());
            Assert.Equal("north", state.GetProperty("selectedObservation").GetString());
            Assert.True(state.GetProperty("rootWidth").GetDouble() > 0);
            Assert.True(state.GetProperty("rootWidth").GetDouble() <= state.GetProperty("availableWidth").GetDouble() + 1);
            Assert.True(state.GetProperty("documentWidth").GetDouble() <= width);
            if (drawing == "wide") Assert.True(state.GetProperty("rootWidth").GetDouble() >= 799,
                "The auto-sized host restores the authored wide contribution after a narrow preparation.");
            await CaptureAsync(page, "hosting-intrinsic-" + shadow + "-" + transition, state);
        }
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>intrinsicSelectionEvents"));
        await UpdateAsync(page, Readings(dark).WithSize(720, 420).ToInteractiveHtmlFragmentWithoutAssets(Configure));
        await page.WaitForFunctionAsync("()=>chartController.root.getBoundingClientRect().width>=719");
        var replaced = await IntrinsicAdaptiveHostStateAsync(page);
        Assert.Equal("wide", replaced.GetProperty("drawing").GetString());
        Assert.Equal("0 0 720 420", replaced.GetProperty("viewBox").GetString());
        Assert.InRange(replaced.GetProperty("rootWidth").GetDouble(), 719d, 721d);
        Assert.True(replaced.GetProperty("sameController").GetBoolean());
        Assert.Equal("north", replaced.GetProperty("selectedObservation").GetString());
        await CaptureAsync(page, "hosting-intrinsic-replaced-" + shadow, new { states, replaced });
        AssertNoConsoleErrors(session);
    }

    private static Task<JsonElement> IntrinsicAdaptiveHostStateAsync(IPage page) => page.EvaluateAsync<JsonElement>("""
        () => {
            const root = chartController.root, shell = document.querySelector('.cfx-shell'), style = getComputedStyle(shell);
            return {drawing:root.dataset.cfxAdaptiveDrawing, viewBox:root.querySelector('.cfx-stage svg').getAttribute('viewBox'),
                rootWidth:root.getBoundingClientRect().width,
                availableWidth:shell.clientWidth-parseFloat(style.paddingLeft)-parseFloat(style.paddingRight),
                documentWidth:document.documentElement.scrollWidth,
                sameController:root===intrinsicRoot && ChartForgeXCharts.get(root)===chartController,
                selectedObservation:chartController.getSelection()[0]?.observationId};
        }
        """);
}
