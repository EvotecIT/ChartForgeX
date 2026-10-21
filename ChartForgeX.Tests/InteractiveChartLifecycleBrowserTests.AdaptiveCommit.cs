using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveChartLifecycleBrowserTests {
    private const string AdaptiveCommitSelected = "[data-cfx-series-key=reference][data-cfx-observation-id=north-central]:not([data-cfx-label-for])";

    [BrowserTheory]
    [InlineData("horizontal")]
    [InlineData("vertical-secondary")]
    [Trait("BrowserSuite", "Compatibility")]
    public async Task AdaptiveUpdateCommitsItsActiveDrawingBeforeHostNotificationAndKeepsFocusAndExportControlsUntilResize(string layout) {
        if (!Enabled) return;
        var pageErrors = new List<string>();
        await using var session = await OpenAsync(SupportedCombinationRecipe.Create(layout, VisualThemeMode.Dark)
            .ToInteractiveHtmlPage(options => ConfigureAdaptiveCommit(options)), 390, 760,
            configurePage: page => page.PageError += (_, error) => pageErrors.Add(error));
        var page = session.Page;
        await BeginAdaptiveCommitProofAsync(page, "narrow");
        await page.Locator("[data-cfx-role=legend-item][data-cfx-series-key=observed]").ClickAsync();
        await FocusWithKeyboardAsync(page, page.Locator(AdaptiveCommitSelected));

        var committed = await ApplyAdaptiveCommitAsync(page, layout, theme: VisualThemeMode.Dark);
        await CaptureAsync(page, "adaptive-commit-" + layout + "-accepted", committed);
        AssertAdaptiveCommit(committed, "narrow", focused: true);
        Assert.Equal(360, committed.GetProperty("nativeWidth").GetDouble());
        await TwoAdaptiveCommitFramesAsync(page);
        var stable = await ReadAdaptiveCommitAsync(page);
        AssertAdaptiveCommit(stable, "narrow", focused: true);
        Assert.Equal(committed.GetProperty("revision").GetString(), stable.GetProperty("revision").GetString());
        Assert.Equal(0, stable.GetProperty("layoutChanges").GetInt32());
        Assert.True(stable.GetProperty("acceptedControlConnected").GetBoolean());
        Assert.True(stable.GetProperty("sameAcceptedControl").GetBoolean());
        await AssertAdaptiveCommitExportAsync(page, layout, capturedControl: true);

        await page.SetViewportSizeAsync(980, 760);
        await page.WaitForFunctionAsync("()=>chartController.root.dataset.cfxAdaptiveDrawing==='wide'");
        var resized = await ReadAdaptiveCommitAsync(page);
        AssertAdaptiveCommit(resized, "wide", focused: true);
        Assert.Equal(1, resized.GetProperty("layoutChanges").GetInt32());
        Assert.False(resized.GetProperty("acceptedControlConnected").GetBoolean());
        Assert.NotEqual(committed.GetProperty("revision").GetString(), resized.GetProperty("revision").GetString());
        await AssertAdaptiveCommitExportAsync(page, layout, capturedControl: false);
        await CaptureAsync(page, "adaptive-commit-" + layout + "-resized", resized);
        Assert.Empty(pageErrors); AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    [Trait("BrowserSuite", "Compatibility")]
    public async Task AdaptiveUpdateUsesTheCurrentPositiveHostWidthAndNewBreakpointWhileHiddenHostsKeepTheirActiveDrawing() {
        if (!Enabled) return;
        var pageErrors = new List<string>();
        await using var session = await OpenAsync(SupportedCombinationRecipe.Create("horizontal")
            .ToInteractiveHtmlPage(options => ConfigureAdaptiveCommit(options)), 980, 760,
            configurePage: page => page.PageError += (_, error) => pageErrors.Add(error));
        var page = session.Page;
        await BeginAdaptiveCommitProofAsync(page, "wide");
        await FocusWithKeyboardAsync(page, page.Locator(AdaptiveCommitSelected));

        // Resize and update in the same JavaScript task, before a ResizeObserver can switch the old drawing.
        var resizedBeforeUpdate = await ApplyAdaptiveCommitAsync(page, "horizontal", beforeUpdate: "chartController.root.style.width='390px';");
        AssertAdaptiveCommit(resizedBeforeUpdate, "narrow", focused: true);
        Assert.InRange(resizedBeforeUpdate.GetProperty("hostWidth").GetDouble(), 1, 639);
        await TwoAdaptiveCommitFramesAsync(page);
        Assert.Equal(0, (await ReadAdaptiveCommitAsync(page)).GetProperty("layoutChanges").GetInt32());

        // A zero-width host has no measured layout. Retain its current drawing until it becomes visible.
        var hidden = await ApplyAdaptiveCommitAsync(page, "horizontal", breakpoint: 300,
            beforeUpdate: "chartController.root.style.display='none';");
        AssertAdaptiveCommit(hidden, "narrow", focused: false);
        Assert.Equal(0, hidden.GetProperty("hostWidth").GetDouble());
        await page.EvaluateAsync("()=>chartController.root.style.display=''");
        await page.WaitForFunctionAsync("()=>chartController.root.dataset.cfxAdaptiveDrawing==='wide'");
        var revealed = await ReadAdaptiveCommitAsync(page);
        Assert.Equal("wide", revealed.GetProperty("drawing").GetString());
        Assert.Equal("north-central", revealed.GetProperty("selectedObservation").GetString());
        Assert.Equal(1, revealed.GetProperty("layoutChanges").GetInt32());

        await FocusWithKeyboardAsync(page, page.Locator(AdaptiveCommitSelected));
        var newPolicy = await ApplyAdaptiveCommitAsync(page, "horizontal", breakpoint: 640);
        AssertAdaptiveCommit(newPolicy, "narrow", focused: true);
        await TwoAdaptiveCommitFramesAsync(page);
        var settled = await ReadAdaptiveCommitAsync(page);
        AssertAdaptiveCommit(settled, "narrow", focused: true);
        Assert.Equal(1, settled.GetProperty("layoutChanges").GetInt32());
        Assert.True(settled.GetProperty("acceptedControlConnected").GetBoolean());
        await AssertAdaptiveCommitExportAsync(page, "horizontal", capturedControl: true);
        await CaptureAsync(page, "adaptive-commit-host-policy-hidden", new { resizedBeforeUpdate, hidden, revealed, newPolicy, settled });
        Assert.Empty(pageErrors); AssertNoConsoleErrors(session);
    }

    private static void ConfigureAdaptiveCommit(HtmlChartInteractionOptions options, int breakpoint = 640) {
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
        options.Adaptive.Breakpoint = breakpoint;
        options.Adaptive.NarrowWidth = 360;
        options.Interaction.ChartId = "adaptive-update-commit";
        options.Interaction.Enable(ChartInteractionFeatures.Export);
        options.Tooltip.PinOnSelect = false;
    }

    private static async Task BeginAdaptiveCommitProofAsync(IPage page, string drawing) {
        await BindControllerAsync(page);
        await page.WaitForFunctionAsync("drawing=>chartController.root.dataset.cfxAdaptiveDrawing===drawing", drawing);
        await page.EvaluateAsync("""
            () => {
                chartController.select({seriesKey:'reference',observationIds:['north-central']});
                window.adaptiveCommitAccepted = null;
                window.adaptiveCommitControl = null;
                window.adaptiveCommitLayoutChanges = 0;
                window.adaptiveCommitFacts = () => {
                    const root=chartController.root, point=root.querySelector('[data-cfx-series-key=reference][data-cfx-observation-id=north-central]:not([data-cfx-label-for])');
                    return {drawing:root.dataset.cfxAdaptiveDrawing, revision:chartController.sceneRevision,
                        nativeWidth:root.querySelector('.cfx-stage svg').viewBox.baseVal.width, hostWidth:root.getBoundingClientRect().width,
                        selectedObservation:chartController.getSelection()[0]?.observationId,
                        focused:point===root.getRootNode().activeElement, layoutChanges:adaptiveCommitLayoutChanges,
                        acceptedControlConnected:!!adaptiveCommitControl?.isConnected,
                        sameAcceptedControl:adaptiveCommitControl===root.querySelector('[data-cfx-export=svg]')};
                };
                chartController.root.addEventListener('cfxlayoutchange',()=>adaptiveCommitLayoutChanges++);
                chartController.root.addEventListener('cfxupdated',()=>{
                    adaptiveCommitControl=chartController.root.querySelector('[data-cfx-export=svg]');
                    adaptiveCommitAccepted=adaptiveCommitFacts();
                });
            }
            """);
    }

    private static Task<JsonElement> ApplyAdaptiveCommitAsync(IPage page, string layout, int breakpoint = 640, string beforeUpdate = "", VisualThemeMode theme = VisualThemeMode.Light) =>
        page.EvaluateAsync<JsonElement>("html=>{" + beforeUpdate + "chartController.update(html);return {...adaptiveCommitAccepted,returnedDrawing:chartController.root.dataset.cfxAdaptiveDrawing};}",
            SupportedCombinationRecipe.Create("updated-" + layout, theme).ToInteractiveHtmlFragmentWithoutAssets(options => ConfigureAdaptiveCommit(options, breakpoint)));

    private static Task<JsonElement> ReadAdaptiveCommitAsync(IPage page) => page.EvaluateAsync<JsonElement>("()=>adaptiveCommitFacts()");
    private static Task TwoAdaptiveCommitFramesAsync(IPage page) => page.EvaluateAsync("()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))");

    private static void AssertAdaptiveCommit(JsonElement value, string drawing, bool focused) {
        Assert.Equal(drawing, value.GetProperty("drawing").GetString());
        if (value.TryGetProperty("returnedDrawing", out var returned)) Assert.Equal(drawing, returned.GetString());
        Assert.Equal("north-central", value.GetProperty("selectedObservation").GetString());
        if (focused) Assert.True(value.GetProperty("focused").GetBoolean(), "The accepted scene retains the source observation's keyboard focus.");
    }

    private static async Task AssertAdaptiveCommitExportAsync(IPage page, string layout, bool capturedControl) {
        var download = await page.RunAndWaitForDownloadAsync(() => capturedControl
            ? page.EvaluateAsync("()=>adaptiveCommitControl.click()")
            : page.Locator("[data-cfx-export=svg]").EvaluateAsync("button=>button.click()"));
        var document = XDocument.Parse(await File.ReadAllTextAsync(await download.PathAsync() ?? throw new InvalidOperationException("No adaptive-update SVG export.")));
        var point = Assert.Single(document.Descendants(), node => (string?)node.Attribute("data-cfx-series-key") == "reference"
            && (string?)node.Attribute("data-cfx-observation-id") == "north-central" && node.Attribute("data-cfx-label-for") == null);
        Assert.Equal("true", (string?)point.Attribute("aria-selected"));
        Assert.Equal(layout == "vertical-secondary" ? .94 : 48,
            double.Parse(((string?)point.Attribute("data-cfx-y") ?? (string?)point.Attribute("data-cfx-value"))!, System.Globalization.CultureInfo.InvariantCulture));
    }
}
