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
        HtmlChartSection Section(double value) => renderer.PrepareSection(NativeNumberCultureCaseMotionTests.Model(kind, policy, value, width, dark), configure: options => { options.Interaction.ChartId = "native-number-culture-case"; options.Interaction.Enable(ChartInteractionFeatures.Export); });
        try {
            var readings = NativeNumberCultureCaseMotionTests.Readings(policy); var from = Section(readings.First); var to = Section(readings.Last); var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 5, null);
            var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.Values, 5));
            var css = "@font-face{font-family:'" + NativeFallbackClusterBeforeControls.Primary + "';src:url(data:font/otf;base64," + Convert.ToBase64String(NativeFallbackClusterBeforeControls.PrimaryBytes())
                + ") format('opentype')}@font-face{font-family:'" + NativeFallbackClusterBeforeControls.Fallback + "';src:url(data:font/otf;base64," + Convert.ToBase64String(fallbackBytes) + ") format('opentype')}";
            var html = "<style>" + css + HtmlInteractiveChartRenderer.BuildFragmentStyle() + "</style>" + from.Html + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script>";
            await using var session = await BrowserProofFonts.OpenAsync(html, width + 24, 680); var page = session.Page;
            var loaded = await page.EvaluateAsync<bool>("async family=>{const faces=await document.fonts.load('14px '+JSON.stringify(family),'k');await document.fonts.ready;return faces.length>0&&faces.every(face=>face.status==='loaded')}", NativeFallbackClusterBeforeControls.Fallback);
            await page.WaitForFunctionAsync("()=>!!ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'))");
            var start = new DateTime(2026, 10, 25, 10, 0, 0, DateTimeKind.Utc); await page.Clock.InstallAsync(new ClockInstallOptions { TimeDate = start }); await page.Clock.PauseAtAsync(start.AddHours(1));
            await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.NoPreference });
            await page.EvaluateAsync("()=>{window.numberChart=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));window.numberEvents=[];numberChart.root.addEventListener('cfxupdated',event=>numberEvents.push(event.detail));}");
            await page.EvaluateAsync("reply=>numberChart.update(reply.html,{sceneRevision:numberChart.sceneRevision,transition:reply.payload,motion:{duration:1000}})", new { html = to.Html, payload }); await page.Clock.RunForAsync(550);
            var observed = await page.EvaluateAsync<JsonElement>("()=>({revision:numberChart.sceneRevision,events:numberEvents,frame:numberChart.root.querySelector('.cfx-native-motion-frame')?.outerHTML,captions:[...numberChart.root.querySelectorAll('.cfx-native-motion-frame text')].map(node=>{const b=node.getBoundingClientRect();return{text:node.textContent,width:b.width,height:b.height}})})");
            var expected = frames[2].Scene.Nodes.OfType<VisualSceneText>().SelectMany(row => row.Text.Lines).Select(row => row.Text).ToArray(); var name = "number-culture-case-browser-" + kind + "-" + policy + "-" + width;
            NativeRadialValueMotionTests.Capture(from, to, frames, payload, name); NativeRadialMotionBeforeControls.Save(name + ".observed.json", observed.GetRawText()); NativeRadialMotionBeforeControls.Save(name + ".expected.json", JsonSerializer.Serialize(expected)); NativeRadialMotionBeforeControls.Save(name + ".page.html", html);
            if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".browser.png"), FullPage = true });
            Assert.True(loaded); Assert.Equal(1, observed.GetProperty("events").GetArrayLength()); Assert.NotEmpty(expected); Assert.Equal(expected, observed.GetProperty("captions").EnumerateArray().Select(row => row.GetProperty("text").GetString()));
            Assert.All(observed.GetProperty("captions").EnumerateArray(), row => { Assert.True(row.GetProperty("width").GetDouble() > 0); Assert.True(row.GetProperty("height").GetDouble() > 0); });
            var selector = kind == "treemap" ? "[data-cfx-node=first] [data-cfx-role=treemap-tile-mark]" : "[data-cfx-role=radial-point][data-cfx-observation-id=primary] [data-cfx-role=" + (kind == "donut" ? "donut-slice" : "pie-slice") + "]";
            await NativeValueFrameBrowserProof.AssertAsync(page, selector, name + "-ink"); await page.Clock.ResumeAsync();
            var exported = await CaptureExports(page, "[data-cfx-export=svg]", "[data-cfx-export=png]", name + "-accepted"); var svg = XDocument.Parse(exported.Svg);
            var point = svg.Descendants().Single(row => kind == "treemap" ? (string?)row.Attribute("data-cfx-node") == "first" : (string?)row.Attribute("data-cfx-role") == "radial-point" && (string?)row.Attribute("data-cfx-observation-id") == "primary");
            Assert.Equal(readings.Last.ToString("R", CultureInfo.InvariantCulture), point.Attribute(kind == "treemap" ? "data-cfx-authored-value" : "data-cfx-value")!.Value);
            Assert.True(exported.Report.GetProperty("uses").GetInt32() > 0); Assert.Equal(0, exported.Report.GetProperty("pngVersusCompletedCompositionDifferentPixels").GetInt32()); Assert.Equal(0, exported.Report.GetProperty("pngVersusForcedMonospaceDifferentPixels").GetInt32());
            await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.Reduce }); Assert.Equal(0, await page.Locator(".cfx-native-motion-frame").CountAsync()); await page.EvaluateAsync("()=>numberChart.dispose()"); AssertNoConsoleErrors(session);
        } finally { FontRegistry.Clear(); _ = BrowserProofFonts.Family; }
    }
}
