using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed partial class StaticExportNativeFontBrowserTests {
    [BrowserTheory]
    [InlineData(false, 760, false, false)] [InlineData(true, 390, true, false)]
    [InlineData(false, 390, true, true)] [InlineData(true, 760, false, true)]
    public async Task CapturedFallbackMotionKeepsNativeGlyphExportsAndAcceptedInitialLifetime(bool donut, int width, bool dark, bool initial) {
        if (!Enabled) return;
        NativeFallbackClusterBeforeControls.Register(); var renderer = new HtmlInteractiveChartRenderer();
        HtmlChartSection Section(double value) => renderer.PrepareSection(NativeFallbackClusterBeforeControls.Model(donut, true, value).WithSize(width, 420)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(NativeFallbackClusterBeforeControls.Primary + ", " + NativeFallbackClusterBeforeControls.Fallback), dark ? VisualThemeMode.Dark : VisualThemeMode.Light),
            configure: options => { options.TextPresentation = HtmlChartTextPresentation.NativeContours; options.Interaction.ChartId = "native-fallback-motion"; options.Interaction.Enable(ChartInteractionFeatures.Export); });
        var from = Section(60); var to = Section(80); var payload = renderer.PrepareNativeTransitionJson(from, to,
            new HtmlNativeTransitionOptions(initial ? HtmlNativeTransitionEffect.InitialValues : HtmlNativeTransitionEffect.Values, 5));
        var name = "fallback-browser-" + donut + "-" + width + "-" + initial;
        // Synthetic OpenType fixtures qualify captured native glyphs; real owned-face loading has separate browser contracts.
        var html = "<style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle() + "</style>" + (initial ? to.Html : from.Html)
            + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script>";
        await using var session = await BrowserProofFonts.OpenAsync(html, width + 24, 680); var page = session.Page;
        await page.WaitForFunctionAsync("()=>!!ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'))");
        var start = new DateTime(2026, 10, 25, 10, 0, 0, DateTimeKind.Utc); await page.Clock.InstallAsync(new ClockInstallOptions { TimeDate = start }); await page.Clock.PauseAtAsync(start.AddHours(1));
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.NoPreference });
        await page.EvaluateAsync("()=>{window.fallbackChart=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));window.fallbackBefore=fallbackChart.sceneRevision;window.fallbackEvents=[];fallbackChart.root.addEventListener('cfxupdated',event=>fallbackEvents.push(event.detail));}");
        if (initial) await page.EvaluateAsync("payload=>fallbackChart.presentInitial(payload,{sceneRevision:fallbackChart.sceneRevision,motion:{duration:1000}})", payload);
        else await page.EvaluateAsync("reply=>fallbackChart.update(reply.html,{sceneRevision:fallbackChart.sceneRevision,transition:reply.payload,motion:{duration:1000}})", new { html = to.Html, payload });
        await page.Clock.RunForAsync(300);
        var observed = await page.EvaluateAsync<JsonElement>("()=>({before:fallbackBefore,revision:fallbackChart.sceneRevision,events:fallbackEvents,frame:fallbackChart.root.querySelector('.cfx-native-motion-frame')?.outerHTML,fonts:[...document.fonts].map(face=>({family:face.family,status:face.status})),captionBounds:[...fallbackChart.root.querySelectorAll('.cfx-native-motion-frame [data-cfx-caption]')].map(node=>{const box=window.cfxCaptionBox(node);return{text:window.cfxCaptionText(node),x:box.x,y:box.y,width:box.width,height:box.height};})})");
        NativeRadialValueMotionTests.Capture(from, to, VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 5, null), payload, name);
        NativeRadialMotionBeforeControls.Save(name + ".observed.json", observed.GetRawText());
        NativeRadialMotionBeforeControls.Save(name + ".page.html", html);
        NativeRadialMotionBeforeControls.Save(name + ".live.html", await page.ContentAsync());
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".motion.png"), FullPage = true });
        Assert.Equal(initial ? 0 : 1, observed.GetProperty("events").GetArrayLength()); Assert.Equal(initial, observed.GetProperty("before").GetString() == observed.GetProperty("revision").GetString());
        await BrowserProofCaptions.AssertFrameInkAsync(page, payload);
        await NativeValueFrameBrowserProof.AssertAsync(page, "[data-cfx-role=radial-point][data-cfx-observation-id=primary] [data-cfx-role=" + (donut ? "donut-slice" : "pie-slice") + "]", name + "-ink");
        await page.Clock.ResumeAsync();
        var exported = await CaptureExports(page, "[data-cfx-export=svg]", "[data-cfx-export=png]", name + "-accepted-export");
        await BrowserProofCaptions.AssertExportInkAsync(page, XDocument.Parse(exported.Svg));
        Assert.Equal("80", XDocument.Parse(exported.Svg).Descendants().Single(row => (string?)row.Attribute("data-cfx-role") == "radial-point" && (string?)row.Attribute("data-cfx-observation-id") == "primary").Attribute("data-cfx-value")!.Value);
        Assert.True(exported.Report.GetProperty("resourcePaths").GetInt32() >= 3); Assert.True(exported.Report.GetProperty("uses").GetInt32() > 0);
        Assert.Equal(0, exported.Report.GetProperty("pngVersusCapturedRasterDifferentPixels").GetInt32()); Assert.Equal(0, exported.Report.GetProperty("compositionVersusForcedMonospaceDifferentPixels").GetInt32());
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.Reduce }); await page.EvaluateAsync("()=>fallbackChart.dispose()");
        Assert.Equal(0, await page.Locator(".cfx-native-motion-frame,[data-cfx-update-motion]").CountAsync()); AssertNoConsoleErrors(session);
    }
}
