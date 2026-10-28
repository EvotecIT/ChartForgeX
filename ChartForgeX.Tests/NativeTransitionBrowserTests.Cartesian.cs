using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class NativeTransitionBrowserTests {
    [BrowserTheory]
    [InlineData(false, 390, true, false)] [InlineData(true, 760, false, false)]
    [InlineData(false, 760, false, true)] [InlineData(true, 390, true, true)]
    [Trait("BrowserSuite", "Compatibility")]
    public async Task RealNativeLineAndBarValuesUseOneAcceptedSceneLifetimeAndCapturedMidpoint(bool bar, int width, bool dark, bool initial) {
        if (!Enabled) return;
        var renderer = new HtmlInteractiveChartRenderer();
        HtmlChartSection Section(double value) { var chart = NativeCartesianValueTransitionTests.Styled(bar ? "bar-gradient" : "line-pattern-markers", value)
                .WithSize(width, 420).WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
            return renderer.PrepareSection(chart, configure: options => { options.Interaction.ChartId = "native-cartesian"; options.Interaction.Enable(ChartInteractionFeatures.Export);
                options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive; options.Adaptive.NarrowWidth = 350; options.Adaptive.NarrowHeight = 420; }); }
        var from = Section(20); var to = Section(80); var options = new HtmlNativeTransitionOptions(initial ? HtmlNativeTransitionEffect.InitialValues : HtmlNativeTransitionEffect.Values, 3);
        var payload = renderer.PrepareNativeTransitionJson(from, to, options);
        await using var session = await BrowserProofFonts.OpenAsync(Page(initial ? to : from), width + 24, 680); var page = session.Page; await ReadyAndPauseAsync(page);
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.NoPreference });
        await page.EvaluateAsync("()=>{window.cartesian=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));window.cartesianEvents=[];window.cartesianRevision=cartesian.sceneRevision;cartesian.root.addEventListener('cfxupdated',event=>cartesianEvents.push(event.detail));}");
        if (initial) await page.EvaluateAsync("payload=>cartesian.presentInitial(payload,{sceneRevision:cartesian.sceneRevision,motion:{duration:1000}})", payload);
        else await page.EvaluateAsync("reply=>cartesian.update(reply.html,{sceneRevision:cartesian.sceneRevision,transition:reply.payload,motion:{duration:1000}})", new { html = to.Html, payload });
        await page.Clock.RunForAsync(550);
        var observed = await page.EvaluateAsync<JsonElement>("()=>({revision:cartesian.sceneRevision,originalRevision:cartesianRevision,events:cartesianEvents,targets:cartesian.resolveTargets(),frame:document.querySelector('.cfx-native-motion-frame')?.outerHTML,accepted:cartesian.root.querySelector('.cfx-stage>svg [data-cfx-observation-id=current]')?.getAttribute('data-cfx-y'),middle:document.querySelector('.cfx-native-motion-frame [data-cfx-observation-id=current]')?.getAttribute('data-cfx-y')})");
        var name = "native-cartesian-browser-" + bar + "-" + width + "-" + dark + "-" + initial;
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var prefix = Path.Combine(output, name); File.WriteAllText(prefix + ".from.html", from.Html); File.WriteAllText(prefix + ".to.html", to.Html);
            File.WriteAllText(prefix + ".transition.json", payload); File.WriteAllText(prefix + ".observed.json", observed.GetRawText()); await page.ScreenshotAsync(new PageScreenshotOptions { Path = prefix + ".browser.png", FullPage = true });
            foreach (var row in new[] { ("wide", from.PreparedVisual, to.PreparedVisual), ("narrow", from.NarrowPreparedVisual!, to.NarrowPreparedVisual!) }) {
                var frames = VisualValueCapture.NativeFrames(row.Item2, row.Item3, 3, null); for (var index = 0; index < frames.Count; index++) {
                    File.WriteAllText(prefix + "." + row.Item1 + "-" + index + ".svg", frames[index].ToSvg()); File.WriteAllBytes(prefix + "." + row.Item1 + "-" + index + ".png", frames[index].ToPng()); }
            }
        }
        Assert.Equal("80", observed.GetProperty("accepted").GetString()); Assert.Equal("50", observed.GetProperty("middle").GetString());
        Assert.Equal(initial, observed.GetProperty("revision").GetString() == observed.GetProperty("originalRevision").GetString()); Assert.Equal(initial ? 0 : 1, observed.GetProperty("events").GetArrayLength());
        Assert.Equal(1, await page.Locator(".cfx-native-motion-frame").CountAsync());
        await NativeValueFrameBrowserProof.AssertAsync(page, "[data-cfx-observation-id=current] [data-cfx-role=" + (bar ? "bar" : "marker") + "]", name + "-ink");
        await ReduceAndFlushMotionAsync(page); Assert.Equal(0, await page.Locator(".cfx-native-motion-frame").CountAsync());
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync()); var file = await download.PathAsync() ?? throw new InvalidOperationException("Accepted Cartesian export missing.");
        Assert.Contains("data-cfx-y=\"80\"", await File.ReadAllTextAsync(file));
        await page.EvaluateAsync("()=>cartesian.dispose()"); await page.Clock.RunForAsync(2000); Assert.Equal(0, await page.Locator(".cfx-native-motion-frame,[data-cfx-update-motion]").CountAsync()); AssertNoConsoleErrors(session);
    }
}
