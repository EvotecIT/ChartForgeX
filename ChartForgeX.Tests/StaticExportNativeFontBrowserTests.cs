using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Actual PNG downloads consume native glyphs; ordinary SVG and the live document remain text.</summary>
[Trait("BrowserSuite", "Compatibility")]
public sealed partial class StaticExportNativeFontBrowserTests {
    [BrowserTheory]
    [InlineData(800, VisualThemeMode.Light)]
    [InlineData(390, VisualThemeMode.Dark)]
    public async Task NativeFontPngKeepsCompletePreparedCaptionsAndEditableSvg(int width, VisualThemeMode mode) {
        if (!Enabled) return;
        var chart = Model(width, mode);
        await using var session = await BrowserProofFonts.OpenAsync(chart.ToInteractiveHtmlFragment(Options), width + 24, 850);
        var page = session.Page;
        var root = page.Locator(".cfx-interactive-chart");
        var target = page.Locator("g[data-cfx-role=funnel-stage][data-cfx-point='1']");
        await FocusWithKeyboardAsync(page, target); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        await page.Locator("[data-cfx-export=svg]").FocusAsync();
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        var before = await root.Locator(".cfx-stage svg").EvaluateAsync<string>("node=>node.outerHTML");
        var exported = await CaptureExports(page, "[data-cfx-export=svg]", "[data-cfx-export=png]", "native-font-" + width + "-" + mode);
        Assert.Equal(before, await root.Locator(".cfx-stage svg").EvaluateAsync<string>("node=>node.outerHTML"));
        AssertTextSvg(exported.Svg, "Received", "Reviewed", "Qualified");
        Assert.Contains(XDocument.Parse(exported.PngComposition).Descendants(), node => node.Name.LocalName == "use");
        Assert.DoesNotContain(XDocument.Parse(exported.PngComposition).Descendants(), node => node.Name.LocalName == "text");
        Assert.Equal(0, exported.Report.GetProperty("pngVersusCapturedRasterDifferentPixels").GetInt32());
        Assert.Equal(0, exported.Report.GetProperty("compositionVersusForcedMonospaceDifferentPixels").GetInt32());
        Assert.True(exported.Report.GetProperty("resourcePaths").GetInt32() < exported.Report.GetProperty("uses").GetInt32());
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task OrdinaryAcceptedReplacementReplacesGlyphsAndKeepsComputedTextPaint() {
        if (!Enabled) return;
        var chart = Model(800, VisualThemeMode.Light);
        var replacement = Model(800, VisualThemeMode.Light).WithTitle("A new accepted caption")
            .WithXLabels("Captured one", "Captured two", "Captured three", "Captured zero");
        replacement.ConfigureFunnel(options => options.CategoryLabel.Style.FontSize = 18);
        await using var session = await BrowserProofFonts.OpenAsync(chart.ToInteractiveHtmlFragment(Options), 824, 850);
        var page = session.Page;
        await page.EvaluateAsync("() => { window.nativeFontChart=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')); }");
        var first = await CaptureExports(page, "[data-cfx-export=svg]", "[data-cfx-export=png]", "native-font-before-replacement");
        await page.EvaluateAsync("html=>nativeFontChart.update(html)", replacement.ToInteractiveHtmlFragmentWithoutAssets(Options));
        await page.AddStyleTagAsync(new PageAddStyleTagOptions { Content = ".cfx-interactive-chart .cfx-stage text{fill:rgb(18,52,86)!important;stroke:rgb(211,17,19)!important;stroke-width:1.5px!important;paint-order:stroke fill!important}" });
        var second = await CaptureExports(page, "[data-cfx-export=svg]", "[data-cfx-export=png]", "native-font-after-replacement");
        Assert.NotEqual(first.Png, second.Png);
        AssertTextSvg(second.Svg, "A new accepted caption", "Captured one", "Captured two", "Captured three");
        Assert.DoesNotContain(XDocument.Parse(second.PngComposition).Descendants(), node => node.Name.LocalName == "text");
        Assert.Equal(0, second.Report.GetProperty("pngVersusCapturedRasterDifferentPixels").GetInt32());
        Assert.Equal(0, second.Report.GetProperty("compositionVersusForcedMonospaceDifferentPixels").GetInt32());
        Assert.Contains("rgb(18, 52, 86)", second.PngComposition, StringComparison.Ordinal);
        Assert.True(second.Report.GetProperty("authoredTextInkPixels").GetInt32() > 0);
        Assert.True(second.Report.GetProperty("authoredStrokeInkPixels").GetInt32() > 0);
        Assert.Equal(0, second.Report.GetProperty("nonTranslationGlyphs").GetInt32());
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task TopologyProducerCallbackUsesTheSameCapturedFontAndExportOwner() {
        if (!Enabled) return;
        var chart = TopologyChart.Create().WithViewport(800, 420).WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family));
        chart.Title = "Topology captured font";
        chart.Nodes.Add(new TopologyNode { Id = "node", Label = "Received Reviewed", X = 60, Y = 90, Width = 260, Height = 90 });
        var calls = 0;
        var fragment = new HtmlInteractiveTopologyRenderer().RenderPresentationFragment(chart, prepared => {
            calls++; return prepared.ToSvg();
        }, new TopologyRenderOptions { EnableHtmlExportControls = true });
        await using var session = await BrowserProofFonts.OpenAsync(fragment, 824, 650);
        var exported = await CaptureExports(session.Page, "[data-cfx-topology-export=svg]", "[data-cfx-topology-export=png]", "native-font-topology-callback");
        Assert.Equal(1, calls); AssertTextSvg(exported.Svg, "Received Reviewed");
        Assert.DoesNotContain(XDocument.Parse(exported.PngComposition).Descendants(), node => node.Name.LocalName == "text");
        Assert.Equal(0, exported.Report.GetProperty("pngVersusCapturedRasterDifferentPixels").GetInt32());
        Assert.Equal(0, exported.Report.GetProperty("compositionVersusForcedMonospaceDifferentPixels").GetInt32());
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task CapturedOutlinePngDoesNotNeedPageFontsOrBlobImagePermission() {
        if (!Enabled) return;
        var fragment = Model(800, VisualThemeMode.Light).ToInteractiveHtmlFragment(Options);
        const string policy = "default-src 'none'; script-src 'unsafe-inline'; style-src 'unsafe-inline'; img-src data:; font-src 'none'; connect-src 'none'; base-uri 'none'";
        var html = "<!doctype html><html><head><meta http-equiv=\"Content-Security-Policy\" content=\"" + policy
            + "\"></head><body><script>window.fontPolicyViolations=[];addEventListener('securitypolicyviolation',event=>fontPolicyViolations.push({directive:event.effectiveDirective,blocked:event.blockedURI}));</script>" + fragment + "</body></html>";
        await using var session = await OpenAsync(html, 824, 850);
        var exported = await CaptureExports(session.Page, "[data-cfx-export=svg]", "[data-cfx-export=png]", "native-font-strict-csp");
        Assert.DoesNotContain(XDocument.Parse(exported.PngComposition).Descendants(), node => node.Name.LocalName == "text");
        Assert.Equal(0, exported.Report.GetProperty("pngVersusCapturedRasterDifferentPixels").GetInt32());
        Assert.Equal(0, exported.Report.GetProperty("compositionVersusForcedMonospaceDifferentPixels").GetInt32());
        Assert.Equal("[]", await session.Page.EvaluateAsync<string>("() => JSON.stringify(fontPolicyViolations)"));
        Assert.Equal(0, await session.Page.EvaluateAsync<int>("() => document.fonts.size"));
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false, false, "underline", "solid")]
    [InlineData(false, true, "underline line-through", "double")]
    [InlineData(true, false, "overline", "dashed")]
    [InlineData(true, false, "line-through", "wavy")]
    [InlineData(true, true, "underline", "dotted")]
    public async Task HostTextDecorationsKeepTheExistingBrowserPaintPath(bool topology, bool ancestor, string lines, string style) {
        if (!Enabled) return;
        string html;
        if (topology) {
            var chart = TopologyChart.Create().WithViewport(800, 420).WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family));
            chart.Title = "Decoration captured font";
            chart.Nodes.Add(new TopologyNode { Id = "node", Label = DecorationCorridorCaption, X = 60, Y = 90, Width = 260, Height = 90 });
            html = new HtmlInteractiveTopologyRenderer().RenderPresentationFragment(chart, prepared => prepared.ToSvg(), new TopologyRenderOptions { EnableHtmlExportControls = true });
        } else html = Model(800, VisualThemeMode.Light).WithTitle(DecorationCorridorCaption).WithSubtitle(string.Empty).ToInteractiveHtmlFragment(Options);
        await using var session = await BrowserProofFonts.OpenAsync(html, 824, 850);
        await session.Page.AddStyleTagAsync(new PageAddStyleTagOptions { Content = "svg " + (ancestor ? "g" : "text")
            + "{text-decoration-line:" + lines + ";text-decoration-style:" + style + ";text-decoration-color:rgb(17,203,177);text-decoration-thickness:3px}" });
        var liveSvg = session.Page.Locator(topology ? ".cfx-topology-viewport > svg[role=img]" : ".cfx-stage svg");
        if (!topology && !ancestor) await liveSvg.EvaluateAsync("""
            svg => {
                const texts=svg.querySelectorAll('text'),authored=texts[0],complex=texts[1];
                authored.setAttribute('textLength',String(authored.getComputedTextLength())); authored.setAttribute('lengthAdjust','spacing');
                authored.dataset.cfxLengthBoundary='authored-length';
                const span=svg.ownerDocument.createElementNS(svg.namespaceURI,'tspan');span.textContent=complex.textContent;
                complex.replaceChildren(span);complex.dataset.cfxLengthBoundary='complex-tspan';
                const zero=svg.ownerDocument.createElementNS(svg.namespaceURI,'text');zero.dataset.cfxLengthBoundary='zero-advance';svg.appendChild(zero);
            }
            """);
        var before = await liveSvg.EvaluateAsync<string>("node=>node.outerHTML");
        using var advanceDocument = JsonDocument.Parse(await liveSvg.EvaluateAsync<string>("""
            svg => JSON.stringify([...svg.querySelectorAll('text')].map(node=>({text:node.textContent,advance:node.getComputedTextLength(),
                children:node.childElementCount,textLength:node.getAttribute('textLength'),lengthAdjust:node.getAttribute('lengthAdjust'),boundary:node.dataset.cfxLengthBoundary||null})))
            """));
        var advances = advanceDocument.RootElement.EnumerateArray().ToArray();
        var prefix = topology ? "data-cfx-topology-export" : "data-cfx-export";
        var exported = await CaptureExports(session.Page, "[" + prefix + "=svg]", "[" + prefix + "=png]",
            "native-font-decoration-" + topology + "-" + ancestor + "-" + style);
        Assert.True(exported.Report.GetProperty("editableSvgDecorationDifferentPixels").GetInt32() > 0, "The existing editable SVG text path must paint the authored decoration.");
        Assert.True(exported.Report.GetProperty("compositionDecorationDifferentPixels").GetInt32() > 0, "Native-glyph materialization must not silently drop supported host text decoration paint.");
        Assert.Equal(0, exported.Report.GetProperty("pngVersusCapturedRasterDifferentPixels").GetInt32());
        Assert.Contains(XDocument.Parse(exported.PngComposition).Descendants(), node => node.Name.LocalName == "text");
        Assert.Equal(before, await liveSvg.EvaluateAsync<string>("node=>node.outerHTML"));
        var editableText = XDocument.Parse(exported.Svg).Descendants().Where(node => node.Name.LocalName == "text").ToArray();
        var pngText = XDocument.Parse(exported.PngComposition).Descendants().Where(node => node.Name.LocalName == "text").ToArray();
        Assert.Equal(advances.Length, pngText.Length); Assert.Equal(advances.Length, editableText.Length);
        for (var index = 0; index < advances.Length; index++) {
            var source = advances[index]; var authoredLength = source.GetProperty("textLength").GetString(); var authoredAdjust = source.GetProperty("lengthAdjust").GetString();
            Assert.Equal(source.GetProperty("text").GetString(), pngText[index].Value); Assert.Equal(source.GetProperty("text").GetString(), editableText[index].Value);
            Assert.Equal(authoredLength, (string?)editableText[index].Attribute("textLength")); Assert.Equal(authoredAdjust, (string?)editableText[index].Attribute("lengthAdjust"));
            var advance = source.GetProperty("advance").GetDouble();
            if (source.GetProperty("children").GetInt32() == 0 && authoredLength == null && authoredAdjust == null && double.IsFinite(advance) && advance > 0) {
                Assert.Equal(advance, double.Parse((string?)pngText[index].Attribute("textLength") ?? throw new InvalidOperationException("Missing accepted PNG text advance."), CultureInfo.InvariantCulture), 6);
                Assert.Equal("spacingAndGlyphs", (string?)pngText[index].Attribute("lengthAdjust"));
            } else {
                Assert.Equal(authoredLength, (string?)pngText[index].Attribute("textLength")); Assert.Equal(authoredAdjust, (string?)pngText[index].Attribute("lengthAdjust"));
            }
            if (source.GetProperty("boundary").GetString() == "zero-advance") Assert.Equal(0, advance);
        }
        var corridor = await CaptureDecorationCorridorAsync(liveSvg);
        await session.Page.AddStyleTagAsync(new PageAddStyleTagOptions { Content = "svg,svg *{text-decoration:none!important}" });
        var undecorated = await CaptureExports(session.Page, "[" + prefix + "=svg]", "[" + prefix + "=png]",
            "native-font-decoration-control-" + topology + "-" + ancestor + "-" + style);
        Assert.Equal(0, undecorated.Report.GetProperty("pngVersusCapturedRasterDifferentPixels").GetInt32());
        AssertDecorationCorridor(exported, undecorated, corridor, "native-font-decoration-" + topology + "-" + ancestor + "-" + style);
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PngComparisonRejectsChangedOutputPixelsAndDimensions(bool wrongDimensions) {
        if (!Enabled) return;
        await using var session = await BrowserProofFonts.OpenAsync(Model(800, VisualThemeMode.Light).ToInteractiveHtmlFragment(Options), 824, 850);
        var page = session.Page;
        // First prove an unmodified download, then corrupt one real encoder output after
        // its pristine bitmap snapshot. Neither unrelated paint nor a frame resize may pass.
        var control = await CaptureExports(page, "[data-cfx-export=svg]", "[data-cfx-export=png]", "native-font-oracle-control-" + wrongDimensions);
        Assert.Equal(0, control.Report.GetProperty("pngVersusCapturedRasterDifferentPixels").GetInt32());
        var name = "native-font-oracle-negative-" + wrongDimensions;
        if (wrongDimensions) await Assert.ThrowsAsync<Xunit.Sdk.EqualException>(() => CaptureExports(page, "[data-cfx-export=svg]", "[data-cfx-export=png]", name, wrongDimensions));
        else {
            var exported = await CaptureExports(page, "[data-cfx-export=svg]", "[data-cfx-export=png]", name, wrongDimensions);
            Assert.Equal(1, exported.Report.GetProperty("pngVersusCapturedRasterDifferentPixels").GetInt32());
            var changed = exported.Report.GetProperty("downloadPixelDifferences")[0];
            Assert.Equal(0, changed.GetProperty("x").GetInt32()); Assert.Equal(0, changed.GetProperty("y").GetInt32());
            Assert.Equal(new[] { 255, 0, 255, 255 }, changed.GetProperty("actual").EnumerateArray().Select(channel => channel.GetInt32()));
            Assert.True(File.Exists(CapturePath(name + ".comparison-actual.png")));
            Assert.True(File.Exists(CapturePath(name + ".comparison-expected.png")));
        }
        AssertNoConsoleErrors(session);
    }

    private static Chart Model(int width, VisualThemeMode mode) =>
        FunnelPresentationRecipe.Create(width == 390 ? "compact-cone" : "cone", mode)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), mode);
    private static void Options(HtmlChartInteractionOptions options) {
        options.TextPresentation = HtmlChartTextPresentation.Browser; // Preserve the incumbent editable-browser/export contract explicitly.
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
        options.Interaction.Enable(ChartInteractionFeatures.Export); options.Tooltip.PinOnSelect = false;
    }
    private static void AssertTextSvg(string svg, params string[] captions) {
        var document = XDocument.Parse(svg);
        Assert.DoesNotContain(document.Descendants(), node => node.Name.LocalName is "script" or "foreignObject");
        Assert.DoesNotContain("data-cfx-png-", svg, StringComparison.Ordinal);
        foreach (var caption in captions) Assert.Contains(document.Descendants(), node => node.Name.LocalName == "text" && node.Value == caption);
    }
    private sealed record Exported(string Svg, string Png, string PngComposition, JsonElement Report);
}
