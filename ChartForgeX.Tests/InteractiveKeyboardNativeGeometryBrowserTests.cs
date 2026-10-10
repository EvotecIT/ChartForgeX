using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractiveKeyboardNativeGeometryBrowserTests {
    private const string DataStop = "[data-cfx-keyboard-component='data'][tabindex='0']";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RetainedNativeFactsRoveAndSelectWithoutMakingHiddenOrMutedFactsAvailable(bool compactDark) {
        if (!Enabled) return;
        var variant = compactDark ? "compact-dark" : "wide-light";
        var html = NativeFixture(compactDark);
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            File.WriteAllText(Path.Combine(capture, "keyboard-native-geometry-" + variant + ".html"), html);
        }
        await using var session = await OpenAsync(html, compactDark ? 390 : 800, compactDark ? 620 : 550);
        var page = session.Page;
        var root = page.Locator(".cfx-interactive-chart");
        await root.EvaluateAsync("root => { window.nativeSelections=[]; root.addEventListener('cfxselect', event => window.nativeSelections.push(event.detail.target)); }");
        var annotationId = await page.Locator("[data-cfx-role='annotation']").GetAttributeAsync("data-cfx-target-id");
        var available = await page.EvaluateAsync<string[]>("() => Array.from(document.querySelectorAll('[data-cfx-keyboard-component=data]')).map(node => node.dataset.cfxTargetId)");
        Assert.True(await page.EvaluateAsync<bool>("() => ['native-zero','native-collapse'].every(id => { const node=document.querySelector('[data-cfx-target-id='+id+']'); const box=node.getBoundingClientRect(); return node.childElementCount===0 && box.width===0 && box.height===0; })"));

        await page.Locator("#before-chart").FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        var tabTarget = await ActiveTargetIdAsync(page);
        var tabElement = await page.EvaluateAsync<string>("() => document.activeElement.outerHTML.slice(0, 300)");
        await page.Keyboard.PressAsync("Space");
        await page.Keyboard.PressAsync("ArrowRight");
        var arrowTarget = await ActiveTargetIdAsync(page);
        var arrowElement = await page.EvaluateAsync<string>("() => document.activeElement.outerHTML.slice(0, 300)");
        await page.Keyboard.PressAsync("Space");
        var selected = await page.EvaluateAsync<string[]>("() => window.nativeSelections.map(target => target.targetId)");
        if (!string.IsNullOrWhiteSpace(capture)) {
            File.WriteAllText(Path.Combine(capture, "keyboard-native-geometry-" + variant + ".json"),
                JsonSerializer.Serialize(new { tabTarget, tabElement, arrowTarget, arrowElement, selected, available, ConsoleErrors = session.ConsoleLog.Count(entry => entry.Type == HtmlTinkerX.HtmlConsoleMessageType.Error) }));
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, "keyboard-native-geometry-" + variant + ".png") });
        }
        Assert.Equal("native-zero", tabTarget);
        Assert.Equal("native-collapse", arrowTarget);
        Assert.Equal(new[] { "native-zero", "native-collapse" }, selected);
        Assert.Equal(new[] { "native-zero", "native-collapse", annotationId }, available);
        Assert.Equal("true", await page.Locator("[data-cfx-target-id='native-zero']").GetAttributeAsync("aria-selected"));
        Assert.Equal("true", await page.Locator("[data-cfx-target-id='native-collapse']").GetAttributeAsync("aria-selected"));
        Assert.Equal(1, await page.Locator(DataStop).CountAsync());
        await page.Keyboard.PressAsync("ArrowRight");
        Assert.Equal(annotationId, await ActiveTargetIdAsync(page));
        await page.Keyboard.PressAsync("End");
        Assert.Equal(annotationId, await ActiveTargetIdAsync(page));

        // The native fact belongs to a visible SVG, not a hidden host or an inner CSS-hidden group.
        await root.EvaluateAsync("root => { root.style.display='none'; window.dispatchEvent(new Event('resize')); }");
        await page.WaitForFunctionAsync("() => !document.querySelector('[data-cfx-keyboard-component=data][tabindex=\"0\"]')");
        await root.EvaluateAsync("root => { root.style.display=''; window.dispatchEvent(new Event('resize')); }");
        await page.WaitForFunctionAsync("() => document.querySelectorAll('[data-cfx-keyboard-component=data][tabindex=\"0\"]').length===1");
        await page.Locator("#before-chart").FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        Assert.Equal(annotationId, await ActiveTargetIdAsync(page));
        await page.Keyboard.PressAsync("Home");
        Assert.Equal("native-zero", await ActiveTargetIdAsync(page));
        await page.Keyboard.PressAsync("Shift+Tab");
        Assert.True(await page.Locator("#before-chart").EvaluateAsync<bool>("node => node===document.activeElement"));
        AssertNoConsoleErrors(session);
    }

    private static Task<string> ActiveTargetIdAsync(IPage page) => page.EvaluateAsync<string>("() => document.activeElement.dataset.cfxTargetId || ''");

    private static string NativeFixture(bool dark) {
        var chart = Chart.Create().WithSize(596, 338).WithTitle("Retained authored facts")
            .WithSubtitle("Zero and precision-collapse facts remain available by keyboard")
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight()).WithLegend(false)
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 10).AddHorizontalLine(6, "Reference guide");
        var html = chart.ToInteractiveHtmlPage(options => options.IncludeResetButton = false);
        // This branch has no zero-geometry producer. Exercise the exported native SVG metadata
        // contract in the actual prepared chart host; empty groups never invent pointer hit areas.
        var facts = NativeFact("native-zero", "zero") + NativeFact("native-collapse", "precision-collapse")
            + "<g hidden>" + NativeFact("native-hidden", "zero") + "</g>"
            + "<g aria-hidden='true'>" + NativeFact("native-aria-hidden", "precision-collapse") + "</g>"
            + "<g style='display:none'>" + NativeFact("native-css-hidden", "zero") + "</g>"
            + "<g style='visibility:hidden'>" + NativeFact("native-invisible", "precision-collapse") + "</g>"
            + "<g class='cfx-series-muted'>" + NativeFact("native-muted", "zero") + "</g>";
        var start = html.IndexOf("<svg", StringComparison.Ordinal);
        html = html.Insert(html.IndexOf('>', start) + 1, facts);
        return html.Replace("<main ", "<button id='before-chart'>Before chart</button><main ", StringComparison.Ordinal)
            .Replace("</main>", "</main><button id='after-chart'>After chart</button>", StringComparison.Ordinal);
    }

    private static string NativeFact(string id, string status) => "<g data-cfx-role='link' data-cfx-target-kind='link' data-cfx-target-id='" + id
        + "' data-cfx-geometry-status='" + status + "' data-cfx-label='" + id + "' data-cfx-value='" + (status == "precision-collapse" ? "1e-20" : "0")
        + "' aria-label='" + id + "'></g>";
}
