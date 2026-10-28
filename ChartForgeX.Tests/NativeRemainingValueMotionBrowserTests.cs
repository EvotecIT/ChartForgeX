using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Uses actual native sibling replies through the existing direct update, passive frame, selection and export owners.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class NativeRemainingValueMotionBrowserTests {
    public static IEnumerable<object[]> Families() => RemainingValueTransitionDefaultControls.Families
        .SelectMany(kind => new[] { new object[] { kind, 390, true }, new object[] { kind, 760, false } });
    [BrowserTheory]
    [Trait("BrowserSuite", "Compatibility")]
    [MemberData(nameof(Families))]
    public async Task RingGaugeAndBulletNativeFramesPaintRealMidpointInkAndRetainAcceptedIdentityExportsAndLifetime(string kind, int width, bool dark) {
        if (!Enabled) return;
        var host = new Host(kind, width, dark);
        await using var session = await BrowserProofFonts.OpenAsync(host.Page, width + 24, 720);
        var page = session.Page;
        await page.ExposeFunctionAsync("prepareRemainingNative", (JsonElement request) => host.Prepare(request));
        await page.EvaluateAsync("""
            ()=>{window.remaining=ChartForgeXCharts.mount(document.querySelector('.cfx-interactive-chart'));window.remainingBasis='initial';
                window.commitRemaining=async value=>{const sceneRevision=remaining.sceneRevision;
                    const reply=await prepareRemainingNative({basis:remainingBasis,value});
                    remaining.update(reply.html,{basis:reply.basis,sceneRevision,transition:reply.transition,motion:{duration:1000}});
                    remainingBasis=reply.basis;return reply.basis;};}
            """);
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.NoPreference });
        var start = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        await page.Clock.InstallAsync(new ClockInstallOptions { TimeDate = start }); await page.Clock.PauseAtAsync(start.AddHours(1));
        await page.EvaluateAsync("identity=>remaining.select({targets:[remaining.resolveTargets().find(target=>target.seriesKey===identity.series&&target.observationId===identity.observation)]},{silent:true})",
            new { series = host.Series, observation = host.Observation });
        var targetCount = await page.EvaluateAsync<int>("()=>remaining.resolveTargets().length");
        var basis = await page.EvaluateAsync<string>("value=>commitRemaining(value)", 90);
        await page.Clock.RunForAsync(550);
        var selector = "[data-cfx-role=" + host.Role + "][data-cfx-series-key=" + host.Series + "][data-cfx-observation-id=" + host.Observation + "]";
        Assert.Equal("90", await page.Locator(".cfx-stage > svg " + selector).GetAttributeAsync("data-cfx-value"));
        Assert.Equal("65", await page.Locator(".cfx-native-motion-frame " + selector).GetAttributeAsync("data-cfx-value"));
        Assert.Equal("1", await page.Locator(".cfx-native-motion-frame").GetAttributeAsync("data-cfx-update-frame"));
        Assert.Equal(90, await page.EvaluateAsync<double>("()=>remaining.getSelection()[0].numericValue"));
        Assert.Equal(host.Series, await page.EvaluateAsync<string>("()=>remaining.getSelection()[0].seriesKey"));
        Assert.Equal(targetCount, await page.EvaluateAsync<int>("()=>remaining.resolveTargets().length"));
        Assert.True(await page.Locator(".cfx-native-motion-frame").EvaluateAsync<bool>("node=>node.hasAttribute('inert')&&node.getAttribute('aria-hidden')==='true'&&getComputedStyle(node).pointerEvents==='none'"));
        await NativeValueFrameBrowserProof.AssertAsync(page, selector + " [data-cfx-role=" + host.MarkRole + "]", "remaining-value-ink-" + kind + "-" + width + "-" + dark, .01);
        await host.CaptureAsync(page, basis);
        var download = await page.RunAndWaitForDownloadAsync(() => page.EvaluateAsync("()=>remaining.root.querySelector('[data-cfx-export=svg]').click()"));
        var path = await download.PathAsync() ?? throw new InvalidOperationException("The accepted native sibling did not export.");
        var exported = XDocument.Load(path);
        Assert.Contains(exported.Descendants(), element => (string?)element.Attribute("data-cfx-role") == host.Role
            && (string?)element.Attribute("data-cfx-series-key") == host.Series && (string?)element.Attribute("data-cfx-observation-id") == host.Observation
            && (string?)element.Attribute("data-cfx-value") == "90");
        Assert.DoesNotContain(exported.Descendants(), element => element.Attribute("data-cfx-update-frame") != null);
        Assert.Equal(0, await page.Locator(".cfx-native-motion-frame").CountAsync());
        await page.EvaluateAsync("()=>commitRemaining(95)"); await page.EvaluateAsync("()=>window.oldRemainingFrame=remaining.root.querySelector('.cfx-native-motion-frame')");
        await page.EvaluateAsync("()=>commitRemaining(100)");
        Assert.True(await page.EvaluateAsync<bool>("()=>!oldRemainingFrame.isConnected")); Assert.Equal(1, await page.Locator(".cfx-native-motion-frame").CountAsync());
        await page.EvaluateAsync("()=>remaining.resize()"); Assert.Equal(0, await page.Locator(".cfx-native-motion-frame").CountAsync());
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.Reduce });
        await page.EvaluateAsync("()=>commitRemaining(80)");
        Assert.Equal("80", await page.Locator(".cfx-stage > svg " + selector).GetAttributeAsync("data-cfx-value"));
        Assert.Equal(0, await page.Locator(".cfx-native-motion-frame").CountAsync());
        await page.EvaluateAsync("()=>remaining.dispose()"); await page.Clock.RunForAsync(2000);
        Assert.Equal(0, await page.Locator(".cfx-native-motion-frame,[data-cfx-update-motion]").CountAsync()); AssertNoConsoleErrors(session);
    }
    private sealed class Host {
        private readonly string _kind; private readonly int _width; private readonly bool _dark;
        private readonly HtmlInteractiveChartRenderer _renderer = new();
        private readonly Dictionary<string, HtmlChartSection> _snapshots = new();
        internal Host(string kind, int width, bool dark) { _kind = kind; _width = width; _dark = dark; _snapshots.Add("initial", Section(40)); }
        internal string Series => _kind == "rings" ? "completion" : _kind.StartsWith("gauge-", StringComparison.Ordinal) ? "capacity" : "verified";
        internal string Observation => _kind == "rings" ? "stage-1" : "current";
        internal string Role => _kind == "rings" ? "progress-ring-point" : _kind.StartsWith("gauge-", StringComparison.Ordinal) ? "gauge" : "bullet-row";
        internal string MarkRole => _kind == "rings" ? "progress-ring-ring" : _kind == "gauge-needle" ? "gauge-needle"
            : _kind.StartsWith("gauge-", StringComparison.Ordinal) ? "gauge-value" : "bullet-value";
        internal string Page => "<style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle() + "</style>" + _snapshots["initial"].Html
            + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script>";
        internal Reply Prepare(JsonElement request) {
            var before = _snapshots[request.GetProperty("basis").GetString()!]; var next = Section(request.GetProperty("value").GetDouble());
            var transition = _renderer.PrepareValueTransitionJson(before, next, 3); var basis = "remaining-" + _snapshots.Count;
            _snapshots.Add(basis, next); return new Reply(next.Html, basis, transition);
        }
        private HtmlChartSection Section(double value) => _renderer.PrepareSection(NativeRemainingValueTransitionTests.Model(_kind, _width, _dark, value, BrowserProofFonts.Family), configure: options => {
            NativeRemainingValueTransitionTests.Configure(options);
            options.Interaction.Enable(ChartInteractionFeatures.Export | ChartInteractionFeatures.Selection | ChartInteractionFeatures.KeyboardNavigation);
            options.Tooltip.PinOnSelect = false;
        });
        internal async Task CaptureAsync(IPage page, string basis) {
            var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return;
            if (!Path.IsPathRooted(output)) throw new InvalidOperationException("Capture output must be absolute.");
            Directory.CreateDirectory(output); var stem = Path.Combine(output, "remaining-value-midpoint-" + _kind + "-" + _width + "-" + _dark);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
            await File.WriteAllTextAsync(stem + ".frame.json", await page.EvaluateAsync<string>("()=>JSON.stringify({basis:remainingBasis,selection:remaining.getSelection(),frame:remaining.root.querySelector('.cfx-native-motion-frame')?.outerHTML})"));
            var section = _snapshots[basis]; File.WriteAllText(stem + ".native.svg", section.PreparedVisual.ToSvg()); File.WriteAllBytes(stem + ".native.png", section.PreparedVisual.ToPng());
            var midpoint = Section(65); File.WriteAllText(stem + ".midpoint.native.svg", midpoint.PreparedVisual.ToSvg()); File.WriteAllBytes(stem + ".midpoint.native.png", midpoint.PreparedVisual.ToPng());
            if (midpoint.NarrowPreparedVisual is { } narrow) File.WriteAllBytes(stem + ".midpoint.narrow.native.png", narrow.ToPng());
        }
        internal sealed record Reply(string html, string basis, string transition);
    }
}
