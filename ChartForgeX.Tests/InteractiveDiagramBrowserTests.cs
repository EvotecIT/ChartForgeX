using System.Text.Json;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Flow and sequence live pages: keyboard traversal, hover highlight, the readout and Escape over the gallery models.</summary>
[Trait("BrowserSuite", "Compatibility")]
public sealed class InteractiveDiagramBrowserTests {
    [BrowserTheory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public async Task FlowStepsAndConnectorsAreExploredByKeyboardAndPointer(VisualThemeMode mode) {
        if (!Enabled) return;
        var html = FlowRecipe.Create("expanded").ToInteractiveHtmlPage(Context(mode, "Assessment workflow", "Typed lanes, decisions and branched routes", 900, 600));
        await using var session = await OpenDiagramAsync(html);
        var page = session.Page; var root = page.Locator(".cfx-diagram");

        await page.Keyboard.PressAsync("Tab");
        Assert.Equal("received", await Focused(page));
        Assert.Equal(new[] { "decision", "received-decision-1" }, await Marked(root, "related"));
        Assert.True(await Opacity(root, "manual") < .5, "Steps outside the focused step's connections recede.");
        await page.Keyboard.PressAsync("ArrowRight");
        Assert.Equal("received-decision-1", await Focused(page));
        Assert.Contains("Evaluate · Request received → Automatic checks pass?", await Readout(root));
        await page.Keyboard.PressAsync("End");
        Assert.Equal("complete", await Focused(page));
        Assert.Contains("From Automatic checks pass? (Passed), Review observations (Accepted)", await Readout(root));
        await page.Keyboard.PressAsync("Escape");
        Assert.Null(await root.GetAttributeAsync("data-cfx-diagram-active"));
        Assert.True(await page.Locator(".cfx-diagram-tooltip").IsHiddenAsync());

        await Hover(page, "manual");
        Assert.Equal("manual", await root.GetAttributeAsync("data-cfx-diagram-active"));
        await AssertTooltip(page, mode, "Review observations", "To Assessment complete (Accepted)");
        await CaptureAsync(page, "flow-" + mode.ToString().ToLowerInvariant());
        AssertNoErrors(session);
    }

    [BrowserTheory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public async Task SequenceMessagesHighlightTheirParticipantsAndPlayInOrder(VisualThemeMode mode) {
        if (!Enabled) return;
        var html = SequenceRecipe.Create("expanded").ToInteractiveHtmlPage(Context(mode, "Request processing sequence", "Participant notation, self calls, fragments and activation", 960, 760));
        await using var session = await OpenDiagramAsync(html);
        var page = session.Page; var root = page.Locator(".cfx-diagram");
        var progress = root.Locator("[data-cfx-diagram-progress]");
        Assert.Equal("5 steps", await progress.TextContentAsync());

        await page.Keyboard.PressAsync("Tab");
        Assert.Equal("client", await Focused(page));
        Assert.Contains("Client · 1 sent · 1 received", await Readout(root));
        for (var index = 0; index < 4; index++) await page.Keyboard.PressAsync("ArrowRight");
        Assert.Equal("message-1", await Focused(page));
        Assert.Contains("Submit request · Message 1 of 5 · Client → Service", await Readout(root));
        Assert.Equal(new[] { "client", "service" }, await Marked(root, "related"));
        await page.Keyboard.PressAsync("Escape");
        Assert.Null(await root.GetAttributeAsync("data-cfx-diagram-active"));

        var next = root.Locator("[data-cfx-diagram-step=\"next\"]");
        await next.ClickAsync(); await next.ClickAsync();
        Assert.Equal("Step 2 / 5", await progress.TextContentAsync());
        Assert.Equal("message-2", await root.GetAttributeAsync("data-cfx-diagram-active"));
        await root.Locator("[data-cfx-diagram-step=\"previous\"]").ClickAsync();
        Assert.Equal("message-1", await root.GetAttributeAsync("data-cfx-diagram-active"));
        var play = root.Locator("[data-cfx-diagram-step=\"play\"]");
        await play.ClickAsync();
        Assert.Equal("true", await play.GetAttributeAsync("aria-pressed"));
        await Assertions.Expect(progress).ToHaveTextAsync("Step 5 / 5", new() { Timeout = 8000 });
        await Assertions.Expect(play).ToHaveAttributeAsync("aria-pressed", "false");
        Assert.Equal("message-5", await root.GetAttributeAsync("data-cfx-diagram-active"));
        await page.Keyboard.PressAsync("Escape");
        Assert.Equal("5 steps", await progress.TextContentAsync());

        await Hover(page, "message-3");
        Assert.Equal(new[] { "service", "store" }, await Marked(root, "related"));
        Assert.True(await Opacity(root, "queue") < .5, "Participants outside the hovered message recede.");
        await AssertTooltip(page, mode, "Persist result", "Service → Store");
        await CaptureAsync(page, "sequence-" + mode.ToString().ToLowerInvariant());
        AssertNoErrors(session);
    }

    private static VisualRenderContext Context(VisualThemeMode mode, string title, string subtitle, int width, int height) =>
        new(new VisualLayoutOptions(new VisualSize(width, height), fitHeightToContent: true), VisualTheme.Graphite(), mode,
            new VisualFrame(title, subtitle, showLegend: false));

    private static async Task<HtmlTinkerX.HtmlBrowserSession> OpenDiagramAsync(string html) {
        var session = await OpenAsync(html, 1280, 900);
        // Reduced motion removes the highlight fade so computed opacity is final when it is read.
        await session.Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await session.Page.WaitForSelectorAsync(".cfx-diagram[data-cfx-runtime-bound=\"true\"]");
        var facts = await session.Page.EvaluateAsync<JsonElement>("""
            () => ({ svgRole: document.querySelector('.cfx-diagram svg').getAttribute('role'),
              unnamed: [...document.querySelectorAll('.cfx-diagram [role="button"][data-cfx-diagram-item]')].filter(node => !node.getAttribute('aria-label')).length,
              readout: !document.querySelector('[data-cfx-diagram-readout]').hidden,
              heading: document.querySelector('.cfx-diagram-title')?.textContent ?? null,
              nativeHeading: !!document.querySelector('.cfx-diagram svg [data-cfx-role="frame-heading"]') })
            """);
        Assert.Equal("group", facts.GetProperty("svgRole").GetString());
        Assert.Equal(0, facts.GetProperty("unnamed").GetInt32());
        Assert.True(facts.GetProperty("readout").GetBoolean());
        // The card carries the frame title once, as HTML; the drawing does not repeat it.
        Assert.False(string.IsNullOrEmpty(facts.GetProperty("heading").GetString()));
        Assert.False(facts.GetProperty("nativeHeading").GetBoolean());
        return session;
    }

    private static Task<string> Focused(IPage page) => page.EvaluateAsync<string>("() => document.activeElement?.getAttribute('data-cfx-diagram-item') ?? null");
    private static Task<string> Readout(ILocator root) => root.Locator("[data-cfx-diagram-readout]").InnerTextAsync();
    private static Task<string[]> Marked(ILocator root, string state) => root.EvaluateAsync<string[]>(
        "(root, state) => [...new Set([...root.querySelectorAll(`[data-cfx-diagram-state=\"${state}\"]`)].map(node => node.getAttribute('data-cfx-diagram-item')))].sort()", state);
    private static Task<double> Opacity(ILocator root, string id) => root.EvaluateAsync<double>(
        "(root, id) => Number(getComputedStyle(root.querySelector(`[data-cfx-diagram-item=\"${id}\"]`)).opacity)", id);

    private static async Task Hover(IPage page, string id) {
        // Lines are hovered on their pointer band; shapes at their centre.
        var item = page.Locator($".cfx-diagram [data-cfx-source-id=\"{id}\"][data-cfx-diagram-item]").First;
        var band = item.Locator("[data-cfx-diagram-hit]");
        var box = await (await band.CountAsync() > 0 ? band.First : item).BoundingBoxAsync();
        Assert.NotNull(box);
        await page.Mouse.MoveAsync(box!.X + box.Width / 2, box.Y + box.Height / 2);
    }

    // The tooltip carries the target's own text, stays inside the stage and uses the page's light or dark chrome.
    private static async Task AssertTooltip(IPage page, VisualThemeMode mode, params string[] text) {
        var tooltip = page.Locator(".cfx-diagram-tooltip");
        await Assertions.Expect(tooltip).ToBeVisibleAsync();
        foreach (var value in text) await Assertions.Expect(tooltip).ToContainTextAsync(value);
        var facts = await tooltip.EvaluateAsync<JsonElement>("""
            tip => { const box = tip.getBoundingClientRect(), stage = tip.parentElement.getBoundingClientRect();
              const [r, g, b] = getComputedStyle(tip).backgroundColor.match(/\d+(\.\d+)?/g).map(Number);
              return { inside: box.left >= stage.left && box.right <= stage.right, luminance: (0.2126 * r + 0.7152 * g + 0.0722 * b) / 255 }; }
            """);
        Assert.True(facts.GetProperty("inside").GetBoolean(), facts.GetRawText());
        var luminance = facts.GetProperty("luminance").GetDouble();
        Assert.True(mode == VisualThemeMode.Dark ? luminance < .5 : luminance > .5, facts.GetRawText());
    }

    private static void AssertNoErrors(HtmlTinkerX.HtmlBrowserSession session) =>
        Assert.Empty(session.ConsoleLog.Where(entry => entry.Type == HtmlTinkerX.HtmlConsoleMessageType.Error).Select(entry => entry.Text));

    private static async Task CaptureAsync(IPage page, string name) {
        var folder = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(folder)) return;
        Directory.CreateDirectory(folder);
        foreach (var width in new[] { 1280, 375 }) {
            await page.SetViewportSizeAsync(width, 900);
            await page.EvaluateAsync("() => { dispatchEvent(new Event('resize')); return new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))); }");
            Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= document.documentElement.clientWidth"), name + " scrolls sideways at " + width + "px.");
            await page.ScreenshotAsync(new() { Path = Path.Combine(folder, "diagram-" + name + "-" + width + ".png"), FullPage = true });
        }
    }
}
