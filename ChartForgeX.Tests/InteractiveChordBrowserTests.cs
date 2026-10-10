using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractiveChordBrowserTests {
    [Theory]
    [InlineData(false, 360)]
    [InlineData(true, 360)]
    [InlineData(false, 800)]
    [InlineData(true, 800)]
    public async Task DirectedRepeatedParallelAndSelfFlowsKeepNativePointerAndKeyboardIdentity(bool dark, int width) {
        if (!Enabled) return;
        var chart = V2GalleryModels.Create(ChartSeriesKind.Chord, "options").WithSize(width, width < 500 ? 320 : 460)
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight()).WithTitle("Directed transfers").WithLegend(true);
        chart.WithPngFont(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf"));
        chart.Series[0].WithInteractionKey("transfer-source");
        var name = "chord-styled-" + width + "-" + (dark ? "dark" : "light");
        var html = chart.ToInteractiveHtmlPage(options => options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit);
        await using var session = await OpenAsync(html, width + 24, width < 500 ? 460 : 600);
        var page = session.Page;
        await CaptureAsync(chart, page, name, html);
        Assert.Equal(4, await page.Locator("[data-cfx-target-kind=node]").CountAsync());
        Assert.Equal(9, await page.Locator("[data-cfx-target-kind=link]").CountAsync());
        Assert.Equal(0, await page.Locator("[data-cfx-target-kind=node][data-cfx-point], [data-cfx-target-kind=link][data-cfx-point]").CountAsync());
        var priority = page.Locator("[data-cfx-target-kind=link][data-cfx-target-id=north-priority]");
        var priorityRibbon = priority.Locator("[data-cfx-role=chord-ribbon]");
        Assert.Equal(ChartColorMath.WithOpacity(ChartColor.FromHex("#AF6B24"), .7).ToCss(), await priorityRibbon.GetAttributeAsync("fill"));
        Assert.Equal(ChartColor.FromHex("#764415").ToCss(), await priorityRibbon.GetAttributeAsync("stroke"));
        Assert.Equal("1", await priorityRibbon.GetAttributeAsync("stroke-width"));
        Assert.Equal("Warning", await priority.GetAttributeAsync("data-cfx-state"));
        await page.EvaluateAsync("() => { window.selections = []; window.hovers = []; const root = document.querySelector('.cfx-interactive-chart'); root.addEventListener('cfxselect', event => window.selections.push(event.detail)); root.addEventListener('cfxhover', event => window.hovers.push(event.detail)); }");
        foreach (var id in new[] { "north-standard", "north-priority", "south-north", "support-internal", "zero-transfer" }) {
            var fact = chart.Series[0].FlowLinks.Single(link => link.Id == id);
            var link = page.Locator("[data-cfx-target-kind=link][data-cfx-target-id='" + id + "']");
            // The authored zero fact follows support-internal, so it must be reached by real roving navigation.
            if (id == "zero-transfer") await page.Keyboard.PressAsync("ArrowRight");
            else await link.FocusAsync();
            Assert.True(await link.EvaluateAsync<bool>("node => node === document.activeElement"));
            Assert.Equal("0", await link.GetAttributeAsync("tabindex"));
            await page.Keyboard.PressAsync("Space");
            using var record = JsonDocument.Parse(await page.EvaluateAsync<string>("() => JSON.stringify(window.selections.at(-1).target)"));
            var selected = record.RootElement;
            Assert.Equal(id, selected.GetProperty("targetId").GetString());
            Assert.Equal("link", selected.GetProperty("targetKind").GetString());
            Assert.Equal("transfer-source", selected.GetProperty("seriesKey").GetString());
            Assert.Equal(fact.SourceId, await link.GetAttributeAsync("data-cfx-source"));
            Assert.Equal(fact.TargetId, await link.GetAttributeAsync("data-cfx-target"));
            Assert.Equal(await link.GetAttributeAsync("data-cfx-value"), selected.GetProperty("value").GetString());
            Assert.False(selected.TryGetProperty("point", out _)); Assert.False(selected.TryGetProperty("sourcePoint", out _));
            if (id == "north-priority") await CaptureAsync(chart, page, name + "-styled-keyboard", html, native: false);
            if (id == "zero-transfer") {
                Assert.Equal("zero", await link.GetAttributeAsync("data-cfx-geometry-status"));
                Assert.Equal(0, await link.Locator("[data-cfx-role=chord-ribbon]").CountAsync());
                Assert.Contains("0", await TooltipTextAsync(page));
                await CaptureAsync(chart, page, name + "-zero-keyboard", html, native: false);
            }
        }
        var zero = page.Locator("[data-cfx-target-kind=link][data-cfx-target-id=zero-transfer]");
        await zero.FocusAsync(); await page.Keyboard.PressAsync("Space"); // Unpin after the full-page capture before checking native link hover.
        await zero.EvaluateAsync("node => node.blur()");
        foreach (var id in new[] { "north-standard", "north-priority", "support-internal" }) {
            var link = page.Locator("[data-cfx-target-kind=link][data-cfx-target-id='" + id + "']");
            var endpoint = await link.EvaluateAsync<double[]>("""
                link => {
                    const svg = link.closest('svg');
                    const source = Array.from(svg.querySelectorAll('[data-cfx-target-kind=node]')).find(node => node.dataset.cfxTargetId === link.dataset.cfxSource);
                    const angle = Number(link.dataset.cfxSourceStartAngle) + Number(link.dataset.cfxSourceSweep) / 2;
                    const radius = Number(source.dataset.cfxInnerRadius) * .995;
                    const point = new DOMPoint(Number(source.dataset.cfxCenterX) + Math.cos(angle) * radius,
                        Number(source.dataset.cfxCenterY) + Math.sin(angle) * radius).matrixTransform(svg.getScreenCTM());
                    return [point.x, point.y];
                }
                """);
            await MoveAwayAsync(page);
            await page.EvaluateAsync("() => window.hovers = []");
            Assert.False(await page.Locator(".cfx-interactive-chart").EvaluateAsync<bool>("root => root.dataset.cfxTooltipPinned === 'true'"), id);
            Assert.Equal(id, await page.EvaluateAsync<string>("position => document.elementFromPoint(position[0], position[1]).closest('[data-cfx-target-kind]').dataset.cfxTargetId", endpoint));
            await page.Mouse.MoveAsync((float)endpoint[0], (float)endpoint[1], new MouseMoveOptions { Steps = 4 });
            Assert.Equal(id, await page.EvaluateAsync<string>("position => document.elementFromPoint(position[0], position[1]).closest('[data-cfx-target-kind]').dataset.cfxTargetId", endpoint));
            await page.WaitForFunctionAsync("id => { const tip = document.querySelector('.cfx-tooltip'); return tip && !tip.hidden && !tip.classList.contains('cfx-tooltip--pinned') && window.hovers.at(-1)?.target.targetId === id; }", id,
                new PageWaitForFunctionOptions { Timeout = 5000 });
            Assert.True(await page.Locator(".cfx-crosshair").EvaluateAsync<bool>("node => node.hidden"));
            await page.Mouse.ClickAsync((float)endpoint[0], (float)endpoint[1]);
            Assert.Equal(id, await page.EvaluateAsync<string>("() => window.selections.at(-1).target.targetId"));
            if (id == "north-priority") await CaptureAsync(chart, page, name + "-styled-pointer", html, native: false);
            // Selection can resize the compare tray; unpin the clicked identity before reading another hover.
            await link.FocusAsync();
            await page.Keyboard.PressAsync("Space");
            await link.EvaluateAsync("node => node.blur()");
        }
        var node = page.Locator("[data-cfx-target-kind=node][data-cfx-target-id=north-support]");
        await node.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("north-support", await page.EvaluateAsync<string>("() => window.selections.at(-1).target.targetId"));
        Assert.Equal("Support", await node.GetAttributeAsync("data-cfx-label"));
        await page.Keyboard.PressAsync("Space"); // Toggle off the keyboard-pinned tooltip before observing real pointer hover.
        await node.EvaluateAsync("node => node.blur()");
        var position = await node.EvaluateAsync<double[]>("""
            node => {
                const angle = Number(node.dataset.cfxStartAngle) + Number(node.dataset.cfxSweep) / 2;
                const radius = (Number(node.dataset.cfxInnerRadius) + Number(node.dataset.cfxOuterRadius)) / 2;
                const point = new DOMPoint(Number(node.dataset.cfxCenterX) + Math.cos(angle) * radius,
                    Number(node.dataset.cfxCenterY) + Math.sin(angle) * radius).matrixTransform(node.closest('svg').getScreenCTM());
                return [point.x, point.y];
            }
            """);
        await MoveAwayAsync(page);
        await page.EvaluateAsync("() => window.hovers = []");
        Assert.Equal("north-support", await page.EvaluateAsync<string>("position => document.elementFromPoint(position[0], position[1]).closest('[data-cfx-target-kind]').dataset.cfxTargetId", position));
        Assert.False(await page.Locator(".cfx-interactive-chart").EvaluateAsync<bool>("root => root.dataset.cfxTooltipPinned === 'true'"));
        await page.Mouse.MoveAsync((float)position[0], (float)position[1], new MouseMoveOptions { Steps = 4 });
        await page.WaitForFunctionAsync("() => { const tip = document.querySelector('.cfx-tooltip'); return tip && !tip.hidden && !tip.classList.contains('cfx-tooltip--pinned') && tip.textContent.includes('26') && window.hovers.at(-1)?.target.targetId === 'north-support'; }");
        Assert.Contains("Support", await TooltipTextAsync(page)); Assert.Contains("26", await TooltipTextAsync(page));
        await CaptureAsync(chart, page, name + "-pointer", html, native: false);
        await page.Mouse.ClickAsync((float)position[0], (float)position[1]);
        Assert.Equal("north-support", await page.EvaluateAsync<string>("() => window.selections.at(-1).target.targetId"));
        AssertNoConsoleErrors(session);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) await File.WriteAllTextAsync(Path.Combine(directory, name + "-console.json"), JsonSerializer.Serialize(session.ConsoleLog));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AllZeroChordRetainsKeyboardFactsWithoutFilledGeometry(bool dark) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(360, 320).WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight()).WithTitle("No positive transfers")
            .AddChord("Transfers", new[] { new ChartNode("first", "Support"), new ChartNode("second", "Support") }, new[] { new ChartFlowLink("none", "first", "second", 0) });
        var html = chart.ToInteractiveHtmlPage().Replace("<main ", "<button id=\"before-chart\">Before chart</button><main ", StringComparison.Ordinal);
        await using var session = await OpenAsync(html, 384, 460);
        var page = session.Page;
        Assert.Equal(0, await page.Locator("[data-cfx-role=chord-ribbon], [data-cfx-role=chord-node-mark]").CountAsync());
        Assert.Equal("0", await page.Locator("[data-cfx-target-kind=link][data-cfx-target-id=none]").GetAttributeAsync("data-cfx-value"));
        var target = page.Locator("[data-cfx-target-kind=link][data-cfx-target-id=none]");
        Assert.Equal(3, await page.Locator("[data-cfx-keyboard-component=data]").CountAsync());
        Assert.Equal(1, await page.Locator("[data-cfx-keyboard-component=data][tabindex='0']").CountAsync());
        await page.Locator("#before-chart").FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        Assert.True(await target.EvaluateAsync<bool>("node => node === document.activeElement"));
        foreach (var id in new[] { "first", "second" }) {
            await page.Keyboard.PressAsync("ArrowRight");
            Assert.Equal(id, await page.EvaluateAsync<string>("() => document.activeElement.dataset.cfxTargetId"));
        }
        await page.Keyboard.PressAsync("Home");
        Assert.True(await target.EvaluateAsync<bool>("node => node === document.activeElement"));
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        await CaptureAsync(chart, page, "chord-zero-" + (dark ? "dark" : "light"), html); AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(0, "zero")]
    [InlineData(double.Epsilon, "precision-collapse")]
    public async Task SemanticOnlyChordFactsRespectHiddenLayoutAndMutedSeries(double value, string geometryStatus) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(360, 320).WithLegend(true).AddChord("Transfers", new[] {
            new ChartNode("a", "A"), new ChartNode("b", "B"), new ChartNode("c", "C"), new ChartNode("d", "D")
        }, new[] { new ChartFlowLink("retained", "a", "b", value), new ChartFlowLink("visible", "c", "d", 1e308) });
        var html = "<!doctype html><html><body><button id=\"show-chart\" onclick=\"document.getElementById('chart-host').style.display=''\">Show chart</button>" +
            "<div id=\"chart-host\" style=\"display:none\">" + chart.ToInteractiveHtmlFragment() + "</div></body></html>";
        await using var session = await OpenAsync(html);
        var page = session.Page;
        const string dataStop = "[data-cfx-keyboard-component=data][tabindex='0']";
        var retained = page.Locator("[data-cfx-target-kind=link][data-cfx-target-id=retained]");
        var visible = page.Locator("[data-cfx-target-kind=link][data-cfx-target-id=visible]");
        Assert.Equal(geometryStatus, await retained.GetAttributeAsync("data-cfx-geometry-status"));
        Assert.Equal(0, await page.Locator(dataStop).CountAsync());
        await page.Locator("#show-chart").ClickAsync();
        await page.WaitForFunctionAsync("() => document.querySelector('[data-cfx-target-id=retained]').getAttribute('tabindex') === '0'", null,
            new PageWaitForFunctionOptions { Timeout = 5000 });
        await retained.FocusAsync();
        Assert.True(await retained.EvaluateAsync<bool>("node => node === document.activeElement"));
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await retained.GetAttributeAsync("aria-selected"));
        Assert.Equal(0, await retained.Locator("[data-cfx-role=chord-ribbon]").CountAsync());

        // CSS hiding a semantic group must skip the retained fact even while the outer SVG has layout.
        await retained.EvaluateAsync("node => node.style.display = 'none'");
        await visible.FocusAsync();
        await page.Keyboard.PressAsync("Home");
        Assert.True(await visible.EvaluateAsync<bool>("node => node === document.activeElement"));
        Assert.Equal("-1", await retained.GetAttributeAsync("tabindex"));
        await retained.EvaluateAsync("node => node.style.display = ''");
        await page.Keyboard.PressAsync("Home");
        Assert.True(await retained.EvaluateAsync<bool>("node => node === document.activeElement"));

        var series = page.Locator("[data-cfx-role=chord-series]");
        foreach (var state in new[] { "display", "hidden", "aria-hidden" }) {
            await series.EvaluateAsync("(node, state) => { if (state === 'display') node.style.display = 'none'; else node.setAttribute(state, state === 'hidden' ? '' : 'true'); }", state);
            await page.Locator(Legend(0)).FocusAsync();
            await page.Keyboard.PressAsync("Home"); // A keyboard refresh must remove the entire data component.
            Assert.Equal(0, await page.Locator(dataStop).CountAsync());
            await series.EvaluateAsync("(node, state) => { if (state === 'display') node.style.display = ''; else node.removeAttribute(state); }", state);
            await page.Locator(Legend(0)).FocusAsync();
            await page.Keyboard.PressAsync("Home");
            Assert.Equal(1, await page.Locator(dataStop).CountAsync());
        }

        await page.Keyboard.PressAsync("Space");
        Assert.Equal(0, await page.Locator(dataStop).CountAsync());
        Assert.Equal(1, await page.Locator("[data-cfx-keyboard-component=legend][tabindex='0']").CountAsync());
        await page.Keyboard.PressAsync("Space");
        await page.Keyboard.PressAsync("Shift+Tab");
        Assert.True(await retained.EvaluateAsync<bool>("node => node === document.activeElement"));
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false, 360)]
    [InlineData(true, 360)]
    [InlineData(false, 800)]
    [InlineData(true, 800)]
    public async Task HiddenNodeCaptionsPreserveVisibleRadiusAndKeyboardZeroFacts(bool dark, int width) {
        if (!Enabled) return;
        Chart Frame(Chart chart) => chart.WithSize(width, width < 500 ? 320 : 460)
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight()).WithTitle("Directed transfers");
        var baseline = Frame(PreparedChordLabelReservationTests.ZeroCaptionChart(false, false));
        var chart = Frame(PreparedChordLabelReservationTests.ZeroCaptionChart(true, true));
        chart.Series[0].WithPointLabel(2, new string('W', 180));
        var baselineNode = PreparedChordTests.Role(baseline.Prepare(VisualExportRequest.ForChart(baseline).Context), "chord-node")
            .Single(node => (string?)node.Attribute("data-cfx-target-id") == "north");
        var html = chart.ToInteractiveHtmlPage(options => options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit);
        await using var session = await OpenAsync(html, width + 24, width < 500 ? 460 : 600);
        var page = session.Page;
        var name = "chord-hidden-caption-" + width + "-" + (dark ? "dark" : "light");
        await CaptureAsync(chart, page, name, html);
        AssertNoConsoleErrors(session);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory))
            await File.WriteAllTextAsync(Path.Combine(directory, name + "-console.json"), JsonSerializer.Serialize(session.ConsoleLog));
        Assert.Equal((string?)baselineNode.Attribute("data-cfx-outer-radius"),
            await page.Locator("[data-cfx-target-kind=node][data-cfx-target-id=north]").GetAttributeAsync("data-cfx-outer-radius"));
        Assert.Equal(2, await page.Locator("[data-cfx-role=chord-node-label]").CountAsync());
        var semantic = page.Locator("[data-cfx-target-kind=node][data-cfx-target-id=semantic]");
        Assert.Equal("zero", await semantic.GetAttributeAsync("data-cfx-geometry-status"));
        Assert.Equal("0", await semantic.GetAttributeAsync("data-cfx-value"));
        var zero = page.Locator("[data-cfx-target-kind=link][data-cfx-target-id=zero]");
        await zero.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await zero.GetAttributeAsync("aria-selected"));
        Assert.Contains("0", await TooltipTextAsync(page));
        AssertNoConsoleErrors(session);
    }

    private static async Task CaptureAsync(Chart chart, IPage page, string name, string html, bool native = true) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        if (native) {
            var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
            await File.WriteAllBytesAsync(Path.Combine(directory, name + "-native.png"), prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
            await File.WriteAllTextAsync(Path.Combine(directory, name + "-native.svg"), prepared.ToSvg());
            await File.WriteAllTextAsync(Path.Combine(directory, name + ".html"), html);
        }
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png"), FullPage = true });
    }
}
