using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveSelectionDockBrowserTests {
    [BrowserTheory]
    [Trait("BrowserSuite", "Compatibility")]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task ReadableCompareInspectionRevealsEachSelectedSourceBeforeShowingItsReadout(bool dark, bool pinOnSelect) {
        if (!Enabled) return;
        await using var session = await OpenAsync(StatusLines(dark).ToInteractiveHtmlPage(options => {
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Readable;
            options.Tooltip.PinOnSelect = pinOnSelect;
        }), 390, 844);
        var page = session.Page;
        for (var point = 0; point <= 6; point += 6) {
            await page.Locator(Point(1, point)).FocusAsync();
            await page.Keyboard.PressAsync("Space");
        }
        var stage = page.Locator(".cfx-stage");
        var lateScroll = await stage.EvaluateAsync<double>("node=>node.scrollLeft");
        Assert.True(lateScroll > 0);
        var early = await BoxAsync(page, Point(1, 0));
        Assert.True(early.X + early.Width < (await BoxAsync(page, ".cfx-stage")).X);
        await page.Locator(".cfx-interactive-chart").EvaluateAsync("""
            root=>{
                window.readableInspection={selection:[],pins:[]};
                root.addEventListener('cfxselectionchange',event=>readableInspection.selection.push(event.detail));
                root.addEventListener('cfxtooltip',event=>readableInspection.pins.push({detail:event.detail,pinned:root.dataset.cfxTooltipPinned,hidden:root.querySelector('.cfx-tooltip').hidden}));
            }
            """);
        await page.Locator(".cfx-compare-chip").First.ClickAsync();
        Assert.False(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        Assert.Contains("Mon", await TooltipTextAsync(page));
        Assert.True(await stage.EvaluateAsync<double>("node=>node.scrollLeft") < lateScroll);
        await CaptureAsync(page, "selection-dock-readable-early-" + dark + "-" + pinOnSelect);
        await FocusWithKeyboardAsync(page, page.Locator(".cfx-compare-chip").Last);
        await page.Keyboard.PressAsync("Enter");
        Assert.False(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        Assert.Contains("Sun", await TooltipTextAsync(page));
        Assert.True(await stage.EvaluateAsync<double>("node=>node.scrollLeft") > 0);
        Assert.Equal("2", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-compare-count"));
        Assert.Equal(2, await page.Locator("[aria-selected=true]").CountAsync());
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>readableInspection.selection.length"));
        var pins = await page.EvaluateAsync<JsonElement>("()=>readableInspection.pins");
        Assert.Equal(pinOnSelect ? 2 : 0, pins.GetArrayLength());
        foreach (var pin in pins.EnumerateArray()) {
            Assert.True(pin.GetProperty("detail").GetProperty("pinned").GetBoolean());
            Assert.Equal("true", pin.GetProperty("pinned").GetString());
            Assert.False(pin.GetProperty("hidden").GetBoolean());
        }
        await CaptureAsync(page, "selection-dock-readable-late-" + dark + "-" + pinOnSelect);
        if (pinOnSelect) {
            // A host can clip the drawing while leaving the comparison controls available.
            await stage.EvaluateAsync("node=>{node.style.height='0';node.style.overflow='clip'}");
            await page.Locator(".cfx-compare-chip").First.ClickAsync();
            Assert.True(await page.Locator(".cfx-tooltip").IsHiddenAsync());
            Assert.Null(await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-tooltip-pinned"));
            Assert.Equal(2, await page.EvaluateAsync<int>("()=>readableInspection.pins.length"));
            Assert.Equal(0, await page.EvaluateAsync<int>("()=>readableInspection.selection.length"));
            await CaptureAsync(page, "selection-dock-readable-unavailable-source-" + dark);
        }
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [Trait("BrowserSuite", "Compatibility")]
    [InlineData(false, 320, true)]
    [InlineData(true, 1000, true)]
    [InlineData(false, 1000, false)]
    [InlineData(true, 320, false)]
    public async Task CompareActivationInspectsTheRequestedObservationWithoutChangingSelectionOrCoveringTheDock(
        bool dark, int width, bool pinOnSelect) {
        if (!Enabled) return;
        await using var session = await OpenAsync(StatusLines(dark).ToInteractiveHtmlPage(options => {
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Tooltip.PinOnSelect = pinOnSelect;
        }), width, 560);
        var page = session.Page;
        for (var point = 0; point < 6; point++) {
            await page.Locator(Point(1, point)).FocusAsync();
            await page.Keyboard.PressAsync("Space");
        }
        if (pinOnSelect) Assert.Contains("Sat", await TooltipTextAsync(page));
        await page.Locator(".cfx-interactive-chart").EvaluateAsync("""
            root=>{
                window.dockSelectionEvents=[];
                root.addEventListener('cfxselectionchange',event=>dockSelectionEvents.push(event.detail));
            }
            """);
        var requested = page.Locator(".cfx-compare-chip").Nth(3);
        await requested.ClickAsync();
        Assert.Contains("Thu", await TooltipTextAsync(page));
        await CaptureAsync(page, "selection-dock-inspect-pointer-" + dark + "-" + width + "-" + pinOnSelect, fullPage: false);
        Assert.Contains("Thu", await TooltipTextAsync(page));
        Assert.Equal(pinOnSelect ? "true" : null, await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-tooltip-pinned"));
        await AssertReadoutDoesNotCoverDockAsync(page);
        if (!pinOnSelect) {
            await MoveAwayAsync(page);
            Assert.False(await page.Locator(".cfx-tooltip").IsVisibleAsync());
            await requested.ClickAsync();
        }
        await FocusWithKeyboardAsync(page, requested);
        await page.Keyboard.PressAsync("Enter");
        Assert.Contains("Thu", await TooltipTextAsync(page));
        Assert.Equal(pinOnSelect ? "true" : null, await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-tooltip-pinned"));
        await page.Locator(".cfx-compare-chip").Nth(4).FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        Assert.Contains("Fri", await TooltipTextAsync(page));
        Assert.Equal("6", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-compare-count"));
        Assert.Equal(6, await page.Locator("[aria-selected=true]").CountAsync());
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>dockSelectionEvents.length"));
        await AssertReadoutDoesNotCoverDockAsync(page);
        await CaptureAsync(page, "selection-dock-inspect-keyboard-" + dark + "-" + width + "-" + pinOnSelect, fullPage: false);
        Assert.Contains("Fri", await TooltipTextAsync(page));
        if (!pinOnSelect) {
            await page.EvaluateAsync("""
                ()=>{
                    const button=document.createElement('button');button.textContent='Following host control';document.body.appendChild(button);
                    button.focus({preventScroll:true});
                }
                """);
            Assert.False(await page.Locator(".cfx-tooltip").IsVisibleAsync());
        }
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [Trait("BrowserSuite", "Compatibility")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PointerPressOnAPartlyVisibleChipKeepsItsClickWhileKeyboardFocusRevealsTheWholeChip(bool dark) {
        if (!Enabled) return;
        var chart = StatusLines(dark).WithXLabels("Monday reading", "Tuesday reading", "Wednesday reading",
            "Thursday reading", "Friday reading", "Saturday reading", "Sunday reading");
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options =>
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit), 320, 560);
        var page = session.Page;
        for (var point = 0; point < 6; point++) {
            await page.Locator(Point(1, point)).FocusAsync();
            await page.Keyboard.PressAsync("Space");
        }
        var tray = page.Locator("[data-cfx-compare-tray]");
        var chip = page.Locator(".cfx-compare-chip").Nth(3);
        var press = await chip.EvaluateAsync<JsonElement>("""
            chip=>{
                const tray=chip.parentElement,box=tray.getBoundingClientRect();
                const target=chip.getBoundingClientRect(),right=box.left+tray.clientLeft+tray.clientWidth;
                tray.scrollLeft+=target.left-right+12;
                const after=chip.getBoundingClientRect();
                window.dockClicks=[];
                tray.closest('.cfx-interactive-chart').addEventListener('cfxhover',event=>dockClicks.push(event.detail));
                return {x:Math.min(right-3,after.right-3),y:(after.top+after.bottom)/2,scroll:tray.scrollLeft,
                    partlyVisible:after.left<right&&after.right>right};
            }
            """);
        Assert.True(press.GetProperty("partlyVisible").GetBoolean(), "The click starts on a genuinely clipped chip.");
        await page.Mouse.MoveAsync((float)press.GetProperty("x").GetDouble(), (float)press.GetProperty("y").GetDouble());
        await page.Mouse.DownAsync();
        await CaptureAsync(page, "selection-dock-partial-press-" + (dark ? "dark" : "light"));
        Assert.Equal(press.GetProperty("scroll").GetDouble(), await tray.EvaluateAsync<double>("node=>node.scrollLeft"), 1);
        await page.Mouse.UpAsync();
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>dockClicks.length"));
        Assert.Equal("Thursday reading", await page.EvaluateAsync<string>("()=>dockClicks[0].label"));

        var last = page.Locator(".cfx-compare-chip").Last;
        await FocusWithKeyboardAsync(page, last);
        var dock = await tray.BoundingBoxAsync() ?? throw new InvalidOperationException("No comparison dock.");
        var bounds = await last.BoundingBoxAsync() ?? throw new InvalidOperationException("No comparison chip.");
        Assert.True(bounds.X >= dock.X - 1 && bounds.X + bounds.Width <= dock.X + dock.Width + 1,
            "Keyboard focus reveals the complete comparison chip.");
        await CaptureAsync(page, "selection-dock-keyboard-reveal-" + (dark ? "dark" : "light"));
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false, HtmlChartResponsiveLayout.Readable, 700, 460)]
    [InlineData(true, HtmlChartResponsiveLayout.Readable, 700, 360)]
    [InlineData(false, HtmlChartResponsiveLayout.Readable, 340, 560)]
    [InlineData(true, HtmlChartResponsiveLayout.Readable, 340, 360)]
    [InlineData(false, HtmlChartResponsiveLayout.Fit, 340, 360)]
    [InlineData(true, HtmlChartResponsiveLayout.Fit, 700, 460)]
    public async Task SelectedTargetsRemainBelowTheNativeViewportWithoutResizingIt(bool dark, HtmlChartResponsiveLayout layout, int width, int height) {
        if (!Enabled) return;
        await using var session = await OpenAsync(StatusLines(dark).ToInteractiveHtmlPage(options => options.ResponsiveLayout = layout), width, height);
        var page = session.Page;
        var before = await BoxAsync(page, ".cfx-stage svg");
        var hostBefore = await BoxAsync(page, ".cfx-interactive-chart");
        Assert.False(await page.Locator("[data-cfx-compare-tray]").IsVisibleAsync());

        for (var point = 0; point < 7; point++) {
            await page.Locator(Point(1, point)).FocusAsync();
            await page.Keyboard.PressAsync("Space");
        }
        Assert.Equal("7", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-compare-count"));
        Assert.Equal(6, await page.Locator(".cfx-compare-chip").CountAsync());
        Assert.Equal("1–6 of 7", await page.Locator(".cfx-compare-tray__range").InnerTextAsync());
        var after = await BoxAsync(page, ".cfx-stage svg");
        Assert.Equal(before.Width, after.Width, 1);
        Assert.Equal(before.Height, after.Height, 1);
        Assert.Equal(hostBefore.Height, (await BoxAsync(page, ".cfx-interactive-chart")).Height, 1);
        await CaptureAsync(page, "selection-dock-" + (dark ? "dark" : "light") + "-" + layout + "-" + width + "x" + height);
        await AssertDockBoundsAsync(page, ".cfx-interactive-chart");

        await page.Locator("[data-cfx-compare-clear]").FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        Assert.False(await page.Locator("[data-cfx-compare-tray]").IsVisibleAsync());
        Assert.Equal(0, await page.Locator(".cfx-selected").CountAsync());
        Assert.Equal("cfx-interactive-chart", await page.EvaluateAsync<string>("() => document.activeElement.className"));
        await page.Keyboard.PressAsync("Tab");
        await CaptureAsync(page, "selection-dock-after-clear-tab-" + dark + "-" + layout + "-" + width + "x" + height);
        Assert.Equal("data", await page.EvaluateAsync<string>("() => document.activeElement.dataset.cfxKeyboardComponent"));
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [Trait("BrowserSuite", "Compatibility")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompareControlsStayInsideAnEmbeddedHostAndPreserveTheFollowingContent(bool dark) {
        if (!Enabled) return;
        var fragment = StatusLines(dark).ToInteractiveHtmlFragment(options => options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit);
        var html = "<!doctype html><html><head><style>body{margin:0;background:#ececec}.report{width:280px;padding:8px}button{font:inherit}</style></head><body>"
            + "<main class='report'>" + fragment + "<button id='following-content'>Continue reading</button></main></body></html>";
        await using var session = await OpenAsync(html, 320, 560);
        var page = session.Page;
        for (var point = 0; point < 6; point++) {
            await page.Locator(Point(1, point)).FocusAsync();
            await page.Keyboard.PressAsync("Space");
        }
        await CaptureAsync(page, "selection-dock-fragment-" + (dark ? "dark" : "light"));
        await AssertDockBoundsAsync(page, ".cfx-interactive-chart");
        var dock = await BoxAsync(page, "[data-cfx-compare-tray]");
        var next = await BoxAsync(page, "#following-content");
        Assert.True(next.Y >= dock.Y + dock.Height, "The host's following content must move below the selection controls.");
        var style = await page.Locator("[data-cfx-compare-tray]").EvaluateAsync<string[]>("node => { const s = getComputedStyle(node); return [s.backgroundColor, s.color]; }");
        var expected = await page.Locator(".cfx-tooltip").EvaluateAsync<string[]>("node => { const s = getComputedStyle(node); return [s.backgroundColor, s.color]; }");
        Assert.Equal(expected, style);
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task SynchronizedDashboardDocksPreserveBothChartViewports() {
        if (!Enabled) return;
        var html = new[] { StatusLines(false), StatusLines(true) }.ToInteractiveHtmlDashboardPage(options => {
            options.Columns = 2;
            options.Interaction.Enable(ChartInteractionFeatures.SynchronizedCharts);
        });
        await using var session = await OpenAsync(html, 1100, 560);
        var page = session.Page;
        await page.Locator(".cfx-interactive-chart").Nth(0).Locator(Point(1, 2)).FocusAsync();
        await page.Keyboard.PressAsync("Space");
        for (var i = 0; i < 2; i++) {
            var root = ".cfx-interactive-chart:nth-child(" + (i + 1) + ")";
            Assert.Equal("1", await page.Locator(root).GetAttributeAsync("data-cfx-compare-count"));
            await AssertDockBoundsAsync(page, root);
            var last = page.Locator(root).Locator(Point(1, 6));
            await last.FocusAsync();
            await CaptureAsync(page, "selection-dock-dashboard-focus-" + i);
            Assert.True(await last.EvaluateAsync<bool>("node => { const stage = node.closest('.cfx-stage'); const b = stage.getBoundingClientRect(); const target = node.getBoundingClientRect(); const center = (target.left + target.right) / 2; return stage.scrollLeft > 0 && center >= b.left + stage.clientLeft && center <= b.left + stage.clientLeft + stage.clientWidth; }"), "Readable charts must scroll inside narrow dashboard columns even when the window is wide.");
        }
        await CaptureAsync(page, "selection-dock-dashboard");
        AssertNoConsoleErrors(session);
    }

    private static async Task AssertDockBoundsAsync(IPage page, string selector) {
        var root = page.Locator(selector);
        var dock = await root.Locator("[data-cfx-compare-tray]").BoundingBoxAsync() ?? throw new InvalidOperationException("No selection dock.");
        var stage = await root.Locator(".cfx-stage").BoundingBoxAsync() ?? throw new InvalidOperationException("No chart viewport.");
        Assert.True(dock.Y >= stage.Y + stage.Height, "Selection controls must not cover chart labels, marks or viewport scrollbars.");
        var box = await root.BoundingBoxAsync() ?? throw new InvalidOperationException("No chart host.");
        Assert.True(dock.X >= box.X - 1 && dock.X + dock.Width <= box.X + box.Width + 1, "The dock must remain inside its host width.");
        var controls = await root.Locator(".cfx-compare-chip,.cfx-compare-page:enabled,[data-cfx-compare-clear]").AllAsync();
        foreach (var control in controls) {
            // The dock is one stable row; keyboard focus scrolls each complete control into view.
            await control.FocusAsync();
            dock = await root.Locator("[data-cfx-compare-tray]").BoundingBoxAsync() ?? throw new InvalidOperationException("No selection dock.");
            var bounds = await control.BoundingBoxAsync() ?? throw new InvalidOperationException("No compare control.");
            Assert.True(bounds.X >= dock.X - 1 && bounds.X + bounds.Width <= dock.X + dock.Width + 1);
            Assert.True(bounds.Y >= dock.Y && bounds.Y + bounds.Height <= dock.Y + dock.Height + 1);
        }
        Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"), "Chart selection must not introduce horizontal page overflow.");
    }

    private static async Task AssertReadoutDoesNotCoverDockAsync(IPage page) {
        var dock = await BoxAsync(page, "[data-cfx-compare-tray]");
        var tip = await BoxAsync(page, ".cfx-tooltip");
        var viewport = page.ViewportSize ?? throw new InvalidOperationException("No browser viewport.");
        Assert.True(tip.X >= 7 && tip.Y >= 7 && tip.X + tip.Width <= viewport.Width - 7 && tip.Y + tip.Height <= viewport.Height - 7,
            "An inspected readout must remain within its visible browser viewport.");
        var text = await page.Locator(".cfx-tooltip").EvaluateAsync<JsonElement>("""
            tip=>{
                const box=tip.getBoundingClientRect(),walker=document.createTreeWalker(tip,NodeFilter.SHOW_TEXT),texts=[];
                for(let node=walker.nextNode();node;node=walker.nextNode())if(node.textContent.trim()){
                    const range=document.createRange();range.selectNodeContents(node);
                    for(const rect of range.getClientRects())texts.push({text:node.textContent,box:rect.toJSON(),inside:rect.left>=box.left&&rect.right<=box.right&&rect.top>=box.top&&rect.bottom<=box.bottom});
                }
                return texts;
            }
            """);
        Assert.All(text.EnumerateArray(), item => Assert.True(item.GetProperty("inside").GetBoolean(), item.GetRawText()));
        Assert.True(tip.Y + tip.Height <= dock.Y - 1 || tip.Y >= dock.Y + dock.Height + 1
            || tip.X + tip.Width <= dock.X || tip.X >= dock.X + dock.Width,
            "An inspected readout must leave comparison controls readable when it fits above them.");
    }

    private static async Task CaptureAsync(IPage page, string name, bool fullPage = true) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png"), FullPage = fullPage });
        var facts = await page.EvaluateAsync<object>("""
            () => Array.from(document.querySelectorAll('.cfx-interactive-chart')).map(root => {
                const box = selector => root.querySelector(selector).getBoundingClientRect().toJSON(),stage=root.querySelector('.cfx-stage');
                const tip=root.querySelector('.cfx-tooltip'),walker=document.createTreeWalker(tip,NodeFilter.SHOW_TEXT),texts=[];
                for(let node=walker.nextNode();node;node=walker.nextNode())if(node.textContent.trim()){
                    const range=document.createRange();range.selectNodeContents(node);texts.push({text:node.textContent,boxes:[...range.getClientRects()].map(rect=>rect.toJSON())});
                }
                return {chart:box('.cfx-stage'),dock:box('[data-cfx-compare-tray]'),count:root.dataset.cfxCompareCount,
                    readout:{hidden:tip.hidden,box:tip.getBoundingClientRect().toJSON(),texts},
                    stage:{scroll:stage.scrollLeft,width:stage.clientWidth,extent:stage.scrollWidth},
                    active:{tag:document.activeElement.localName,data:{...document.activeElement.dataset},html:document.activeElement.outerHTML.slice(0,1200)},
                    targets:[...root.querySelectorAll('[data-cfx-keyboard-component]')].map(node=>({data:{...node.dataset},tabIndex:node.getAttribute('tabindex'),
                        focused:node===document.activeElement,focusVisible:node.matches(':focus-visible'),box:node.getBoundingClientRect().toJSON()}))};
            })
            """);
        File.WriteAllText(Path.Combine(directory, name + ".json"), JsonSerializer.Serialize(facts));
    }
}
