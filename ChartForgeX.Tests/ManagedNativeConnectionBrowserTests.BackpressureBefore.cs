using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class ManagedNativeConnectionBrowserTests {
    [Theory]
    [InlineData(390, false, false)] [InlineData(390, true, false)]
    [InlineData(900, false, false)] [InlineData(900, true, false)]
    [InlineData(390, false, true)] [InlineData(390, true, true)]
    [InlineData(900, false, true)] [InlineData(900, true, true)]
    public void NativeNumericAndTypedProducerControlsRetainDetachedAssetFreeSections(int width, bool dark, bool typed) {
        var host = new NativeHost(width, dark ? VisualThemeMode.Dark : VisualThemeMode.Light, typed);
        var section = host.Section("initial"); var html = section.Html; var svg = section.PreparedVisual.ToSvg(); var png = section.PreparedVisual.ToPng();
        var output = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output); var stem = Path.Combine(output, "native-admission-control-" + (typed ? "typed-" : "numeric-") + width + (dark ? "-dark" : "-light"));
            File.WriteAllText(stem + ".html", html); File.WriteAllText(stem + ".svg", svg); File.WriteAllBytes(stem + ".native.png", png);
        }
        host.ReplaceProducerOffset(5);
        using var request = JsonDocument.Parse("{\"basis\":\"initial\",\"channel\":\"viewport\",\"kind\":\"window\",\"sourceViewport\":{\"x\":{\"minimum\":2,\"maximum\":8}}}");
        var reply = JsonSerializer.SerializeToElement(host.Prepare(request.RootElement));
        var changed = host.Section(reply.GetProperty("basis").GetString()!);
        Assert.NotEqual(svg, changed.PreparedVisual.ToSvg()); Assert.Equal(2, changed.PreparedVisual.XProjection!.Minimum);
        var mark = XDocument.Parse(changed.PreparedVisual.ToSvg()).Descendants().Single(node =>
            (string?)node.Attribute("data-cfx-role") == "point" && (string?)node.Attribute("data-cfx-series-key") == "readings" && (string?)node.Attribute("data-cfx-source-point") == "5");
        Assert.Equal("50", (string?)mark.Attribute(typed ? "data-cfx-start" : "data-cfx-y"));
        Assert.Equal(html, section.Html); Assert.Equal(svg, section.PreparedVisual.ToSvg()); Assert.Equal(png, section.PreparedVisual.ToPng());
        Assert.DoesNotContain("<script", html); Assert.Contains("readings", html);
        if (typed) Assert.Contains("reading-5", html);
    }

    [BrowserTheory]
    [Trait("BrowserSuite", "Compatibility")]
    [InlineData(390, true, false)] [InlineData(900, false, false)]
    [InlineData(390, true, true)] [InlineData(900, false, true)]
    public async Task LatestWindowCausalKeepsOneIgnoredAbortProducerAndTheLatestAcceptedNativeSource(int width, bool dark, bool typed) {
        if (!Enabled) return;
        var host = new NativeHost(width, dark ? VisualThemeMode.Dark : VisualThemeMode.Light, typed);
        await using var session = await BrowserProofFonts.OpenAsync(host.Page, width + 24, 800);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await ConnectAsync(page, host, "x");
        await page.EvaluateAsync("() => managed.connectNative(controlledNative,{basis:'initial',axes:['x'],preparationBackpressure:'latest-window'})");
        await page.EvaluateAsync("() => managed.setViewport({x:{minimum:1,maximum:9}})"); await PendingAsync(page, 1);
        for (var index = 0; index < 12; index++)
            await page.EvaluateAsync("inset => managed.setViewport({x:{minimum:inset,maximum:10-inset}})", 2 + index * .01);
        await DrainAsync(page);
        await CaptureAsync(page, "admission-causal-" + width + "-" + dark + "-" + typed);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            var stem = Path.Combine(output, "native-admission-causal-" + width + "-" + dark + "-" + typed);
            File.WriteAllText(stem + ".native.svg", host.Section("initial").PreparedVisual.ToSvg());
            File.WriteAllBytes(stem + ".native.png", host.Section("initial").PreparedVisual.ToPng());
        }
        Assert.Equal(1, await page.EvaluateAsync<int>("() => pending.length"));
        Assert.True(await page.EvaluateAsync<bool>("() => pending[0].signal.aborted"));
        Assert.True(await page.Locator("button[data-cfx-zoom]").First.EvaluateAsync<bool>("node => node.disabled"));
        await page.EvaluateAsync("() => managed.root.querySelector('[data-cfx-zoom]').dispatchEvent(new MouseEvent('click',{bubbles:true}))");
        await CaptureAsync(page, "admission-disabled-control-" + width + "-" + dark + "-" + typed);
        Assert.Equal(1, await page.EvaluateAsync<int>("() => pending.length"));
        host.ReplaceProducerOffset(7);
        await ResolveAsync(page, 0); await PendingAsync(page, 2);
        Assert.Equal("initial", await RequestFieldAsync(page, 1, "basis"));
        var basis = await ResolveAsync(page, 1); await ChangesAsync(page, 1);
        AssertWindow(await page.EvaluateAsync<JsonElement>("() => managed.getXWindow()"), 2.11, 7.89);
        Assert.Equal(host.Section(basis).PreparedVisual.XProjection!.Minimum, await page.EvaluateAsync<double>("() => managed.getXWindow().minimum"));
        var reading = page.Locator("g[data-cfx-series-key=readings][data-cfx-source-point='5'][data-cfx-point]");
        Assert.Equal("52", await reading.GetAttributeAsync(typed ? "data-cfx-start" : "data-cfx-y"));
        if (typed) { Assert.Equal("60", await reading.GetAttributeAsync("data-cfx-end")); Assert.Equal("reading-5", await reading.GetAttributeAsync("data-cfx-observation-id")); }
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }
}
