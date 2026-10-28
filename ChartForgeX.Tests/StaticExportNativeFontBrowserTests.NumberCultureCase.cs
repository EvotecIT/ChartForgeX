using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class StaticExportNativeFontBrowserTests {
    [BrowserTheory]
    [InlineData("pie", "fr-number", 760, false)] [InlineData("donut", "sv-currency", 390, true)]
    [InlineData("pie", "ar-percent", 760, true)] [InlineData("donut", "fr-compact", 390, false)]
    [InlineData("treemap", "sentence", 390, true)] [InlineData("treemap", "toggle", 760, false)]
    [InlineData("pie", "literal-ms", 390, false)] [InlineData("donut", "sections", 760, true)]
    [InlineData("treemap", "scaled", 390, true)] [InlineData("donut", "scientific", 760, false)]
    public async Task ActualNativeNumberCulturesAndCompositeCaseKeepCapturedGlyphExportsAndOneSceneLifetime(string kind, string policy, int width, bool dark) {
        if (!Enabled) return; var fallbackBytes = NativeNumberCultureCaseMotionTests.Register(); var renderer = new HtmlInteractiveChartRenderer();
        HtmlChartSection Section(double value) => renderer.PrepareSection(NativeNumberCultureCaseMotionTests.Model(kind, policy, value, width, dark), configure: options => { options.TextPresentation = HtmlChartTextPresentation.NativeContours; options.Interaction.ChartId = "native-number-culture-case"; options.Interaction.Enable(ChartInteractionFeatures.Export); });
        try {
            var readings = NativeNumberCultureCaseMotionTests.Readings(policy); var from = Section(readings.First); var to = Section(readings.Last); var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 5, null);
            var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.Values, 5));
            // Synthetic OpenType fixtures qualify captured native glyphs; real binary font delivery is checked separately.
            var html = "<style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle() + "</style>" + from.Html + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script>";
            await using var session = await BrowserProofFonts.OpenAsync(html, width + 24, 680); var page = session.Page;
            await page.WaitForFunctionAsync("()=>!!ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'))");
            var start = new DateTime(2026, 10, 25, 10, 0, 0, DateTimeKind.Utc); await page.Clock.InstallAsync(new ClockInstallOptions { TimeDate = start }); await page.Clock.PauseAtAsync(start.AddHours(1));
            await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.NoPreference });
            await page.EvaluateAsync("()=>{window.numberChart=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));window.numberEvents=[];numberChart.root.addEventListener('cfxupdated',event=>numberEvents.push(event.detail));}");
            await page.EvaluateAsync("reply=>numberChart.update(reply.html,{sceneRevision:numberChart.sceneRevision,transition:reply.payload,motion:{duration:1000}})", new { html = to.Html, payload }); await page.Clock.RunForAsync(550);
            var observed = await page.EvaluateAsync<JsonElement>("()=>({revision:numberChart.sceneRevision,events:numberEvents,frame:numberChart.root.querySelector('.cfx-native-motion-frame')?.outerHTML,captions:[...numberChart.root.querySelectorAll('.cfx-native-motion-frame [data-cfx-caption]')].map(node=>{const b=window.cfxCaptionBox(node);return{text:window.cfxCaptionText(node),width:b.width,height:b.height}})})");
            var expected = frames[2].Scene.Nodes.OfType<VisualSceneText>().SelectMany(row => row.Text.Lines).Select(row => row.Text).ToArray(); var name = "number-culture-case-browser-" + kind + "-" + policy + "-" + width;
            NativeRadialValueMotionTests.Capture(from, to, frames, payload, name); NativeRadialMotionBeforeControls.Save(name + ".observed.json", observed.GetRawText()); NativeRadialMotionBeforeControls.Save(name + ".expected.json", JsonSerializer.Serialize(expected)); NativeRadialMotionBeforeControls.Save(name + ".page.html", html);
            if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".browser.png"), FullPage = true });
            Assert.Equal(1, observed.GetProperty("events").GetArrayLength()); Assert.NotEmpty(expected); await BrowserProofCaptions.AssertFrameInkAsync(page, payload);

            var selector = kind == "treemap" ? "[data-cfx-node=first] [data-cfx-role=treemap-tile-mark]" : "[data-cfx-role=radial-point][data-cfx-observation-id=primary] [data-cfx-role=" + (kind == "donut" ? "donut-slice" : "pie-slice") + "]";
            await NativeValueFrameBrowserProof.AssertAsync(page, selector, name + "-ink"); await page.Clock.ResumeAsync();
            var exported = await CaptureExports(page, "[data-cfx-export=svg]", "[data-cfx-export=png]", name + "-accepted"); var svg = XDocument.Parse(exported.Svg);
            var point = svg.Descendants().Single(row => kind == "treemap" ? (string?)row.Attribute("data-cfx-node") == "first" : (string?)row.Attribute("data-cfx-role") == "radial-point" && (string?)row.Attribute("data-cfx-observation-id") == "primary");
            Assert.Equal(readings.Last.ToString("R", CultureInfo.InvariantCulture), point.Attribute(kind == "treemap" ? "data-cfx-authored-value" : "data-cfx-value")!.Value);
            Assert.True(exported.Report.GetProperty("uses").GetInt32() > 0); Assert.Equal(0, exported.Report.GetProperty("pngVersusCapturedRasterDifferentPixels").GetInt32()); Assert.Equal(0, exported.Report.GetProperty("compositionVersusForcedMonospaceDifferentPixels").GetInt32());
            await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.Reduce }); Assert.Equal(0, await page.Locator(".cfx-native-motion-frame").CountAsync()); await page.EvaluateAsync("()=>numberChart.dispose()"); AssertNoConsoleErrors(session);
        } finally { FontRegistry.Clear(); _ = BrowserProofFonts.Family; }
    }
}
