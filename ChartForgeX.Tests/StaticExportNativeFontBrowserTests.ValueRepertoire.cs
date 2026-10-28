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

public sealed partial class StaticExportNativeFontBrowserTests {
    [BrowserTheory]
    [InlineData(false, 760, false, false)] [InlineData(true, 390, true, false)]
    [InlineData(false, 390, true, true)] [InlineData(true, 760, false, true)]
    public async Task ActualNativeNumericFallbackSuffixesRemainVisibleAndExportAcceptedValuesThroughTheExistingLifetime(bool donut, int width, bool dark, bool initial) {
        if (!Enabled) return;
        NativeValueRepertoireBeforeControls.Register();
        var fallbackBytes = NativeValueRepertoireMotionTests.FallbackBytes(); FontRegistry.Register(NativeFallbackClusterBeforeControls.Fallback, fallbackBytes);
        var renderer = new HtmlInteractiveChartRenderer();
        HtmlChartSection Section(double value) => renderer.PrepareSection(NativeValueRepertoireBeforeControls.Model(donut, "compact", value).WithSize(width, 420)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(NativeFallbackClusterBeforeControls.Primary + ", " + NativeFallbackClusterBeforeControls.Fallback), dark ? VisualThemeMode.Dark : VisualThemeMode.Light),
            configure: options => { options.TextPresentation = HtmlChartTextPresentation.NativeContours; options.Interaction.ChartId = "value-text-repertoire"; options.Interaction.Enable(ChartInteractionFeatures.Export); });
        try {
            var from = Section(900); var to = Section(1100); var payload = renderer.PrepareNativeTransitionJson(from, to,
                new HtmlNativeTransitionOptions(initial ? HtmlNativeTransitionEffect.InitialValues : HtmlNativeTransitionEffect.Values, 5));
            var name = "value-text-repertoire-browser-" + donut + "-" + width + "-" + initial;
            // Synthetic OpenType fixtures qualify captured native glyphs; real binary font delivery is checked separately.
            var html = "<style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle() + "</style>" + (initial ? to.Html : from.Html) + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script>";
            await using var session = await BrowserProofFonts.OpenAsync(html, width + 24, 680); var page = session.Page;
            await page.WaitForFunctionAsync("()=>!!ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'))");
            var start = new DateTime(2026, 10, 25, 10, 0, 0, DateTimeKind.Utc); await page.Clock.InstallAsync(new ClockInstallOptions { TimeDate = start }); await page.Clock.PauseAtAsync(start.AddHours(1));
            await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.NoPreference });
            await page.EvaluateAsync("()=>{window.valueTextChart=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));window.valueTextRevision=valueTextChart.sceneRevision;window.valueTextEvents=[];valueTextChart.root.addEventListener('cfxupdated',event=>valueTextEvents.push(event.detail));}");
            if (initial) await page.EvaluateAsync("payload=>valueTextChart.presentInitial(payload,{sceneRevision:valueTextChart.sceneRevision,motion:{duration:1000}})", payload);
            else await page.EvaluateAsync("reply=>valueTextChart.update(reply.html,{sceneRevision:valueTextChart.sceneRevision,transition:reply.payload,motion:{duration:1000}})", new { html = to.Html, payload });
            await page.Clock.RunForAsync(550);
            var observed = await page.EvaluateAsync<JsonElement>("()=>({before:valueTextRevision,revision:valueTextChart.sceneRevision,events:valueTextEvents,frame:valueTextChart.root.querySelector('.cfx-native-motion-frame')?.outerHTML,captions:[...valueTextChart.root.querySelectorAll('.cfx-native-motion-frame [data-cfx-caption]')].map(node=>{const b=window.cfxCaptionBox(node);return{text:window.cfxCaptionText(node),width:b.width,height:b.height}})})");
            NativeRadialValueMotionTests.Capture(from, to, VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 5, null), payload, name);
            NativeRadialMotionBeforeControls.Save(name + ".page.html", html); NativeRadialMotionBeforeControls.Save(name + ".live.html", await page.ContentAsync()); NativeRadialMotionBeforeControls.Save(name + ".observed.json", observed.GetRawText());
            if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".browser.png"), FullPage = true });
            Assert.Equal(initial ? 0 : 1, observed.GetProperty("events").GetArrayLength()); Assert.Equal(initial, observed.GetProperty("before").GetString() == observed.GetProperty("revision").GetString());
            await BrowserProofCaptions.AssertFrameInkAsync(page, payload);
            await NativeValueFrameBrowserProof.AssertAsync(page, "[data-cfx-role=radial-point][data-cfx-observation-id=primary] [data-cfx-role=" + (donut ? "donut-slice" : "pie-slice") + "]", name + "-ink");
            await page.Clock.ResumeAsync(); var exported = await CaptureExports(page, "[data-cfx-export=svg]", "[data-cfx-export=png]", name + "-accepted");
            await BrowserProofCaptions.AssertExportInkAsync(page, XDocument.Parse(exported.Svg));
            Assert.Equal("1100", XDocument.Parse(exported.Svg).Descendants().Single(row => (string?)row.Attribute("data-cfx-role") == "radial-point" && (string?)row.Attribute("data-cfx-observation-id") == "primary").Attribute("data-cfx-value")!.Value);
            Assert.True(exported.Report.GetProperty("uses").GetInt32() > 0); Assert.Equal(0, exported.Report.GetProperty("pngVersusCapturedRasterDifferentPixels").GetInt32()); Assert.Equal(0, exported.Report.GetProperty("compositionVersusForcedMonospaceDifferentPixels").GetInt32());
            await page.EvaluateAsync("()=>valueTextChart.dispose()"); Assert.Equal(0, await page.Locator(".cfx-native-motion-frame,[data-cfx-update-motion]").CountAsync()); AssertNoConsoleErrors(session);
        } finally { FontRegistry.Clear(); _ = BrowserProofFonts.Family; }
    }
}
