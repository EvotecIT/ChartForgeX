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
        Assert.Equal(0, exported.Report.GetProperty("pngVersusCompletedCompositionDifferentPixels").GetInt32());
        Assert.Equal(0, exported.Report.GetProperty("pngVersusForcedMonospaceDifferentPixels").GetInt32());
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
        Assert.Equal(0, second.Report.GetProperty("pngVersusCompletedCompositionDifferentPixels").GetInt32());
        Assert.Equal(0, second.Report.GetProperty("pngVersusForcedMonospaceDifferentPixels").GetInt32());
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
        Assert.Equal(0, exported.Report.GetProperty("pngVersusCompletedCompositionDifferentPixels").GetInt32());
        Assert.Equal(0, exported.Report.GetProperty("pngVersusForcedMonospaceDifferentPixels").GetInt32());
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
        Assert.Equal(0, exported.Report.GetProperty("pngVersusCompletedCompositionDifferentPixels").GetInt32());
        Assert.Equal(0, exported.Report.GetProperty("pngVersusForcedMonospaceDifferentPixels").GetInt32());
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
            chart.Nodes.Add(new TopologyNode { Id = "node", Label = "Received Reviewed", X = 60, Y = 90, Width = 260, Height = 90 });
            html = new HtmlInteractiveTopologyRenderer().RenderPresentationFragment(chart, prepared => prepared.ToSvg(), new TopologyRenderOptions { EnableHtmlExportControls = true });
        } else html = Model(800, VisualThemeMode.Light).ToInteractiveHtmlFragment(Options);
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
        Assert.True(exported.Report.GetProperty("pngDecorationDifferentPixels").GetInt32() > 0, "Native-glyph materialization must not silently drop supported host text decoration paint.");
        Assert.Equal(0, exported.Report.GetProperty("pngVersusCompletedCompositionDifferentPixels").GetInt32());
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
    private static async Task<Exported> CaptureExports(IPage page, string svgButton, string pngButton, string name) {
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(output) || !Path.IsPathRooted(output)) throw new InvalidOperationException("Font proof requires an absolute capture path.");
        Directory.CreateDirectory(output); var stem = Path.Combine(output, name);
        await page.EvaluateAsync("""
            () => {
                window.nativeFontPngInputs=[];
                window.nativeFontObjectUrlOwner=URL.createObjectURL;
                URL.createObjectURL=function(blob){if(blob.type.startsWith('image/svg+xml'))nativeFontPngInputs.push(blob.text());return nativeFontObjectUrlOwner.call(URL,blob);};
                window.nativeFontImageSrcOwner=Object.getOwnPropertyDescriptor(HTMLImageElement.prototype,'src');
                Object.defineProperty(HTMLImageElement.prototype,'src',{...nativeFontImageSrcOwner,set(value){
                    const prefix='data:image/svg+xml;charset=utf-8,';
                    if(String(value).startsWith(prefix))nativeFontPngInputs.push(Promise.resolve(decodeURIComponent(String(value).slice(prefix.length))));
                    return nativeFontImageSrcOwner.set.call(this,value);
                }});
            }
            """);
        string svg; byte[] png; string composition;
        try {
            var vector = await page.RunAndWaitForDownloadAsync(() => page.Locator(svgButton).ClickAsync());
            svg = await File.ReadAllTextAsync(await vector.PathAsync() ?? throw new InvalidOperationException("Missing SVG download."));
            await vector.SaveAsAsync(stem + ".svg");
            await page.EvaluateAsync("() => { nativeFontPngInputs.length=0; }");
            var raster = await page.RunAndWaitForDownloadAsync(() => page.Locator(pngButton).ClickAsync());
            png = await File.ReadAllBytesAsync(await raster.PathAsync() ?? throw new InvalidOperationException("Missing PNG download."));
            await raster.SaveAsAsync(stem + ".png");
            composition = await page.EvaluateAsync<string>("async () => { const inputs=await Promise.all(nativeFontPngInputs);if(inputs.length!==1)throw new Error('Expected one completed PNG SVG input.');return inputs[0]; }");
            await File.WriteAllTextAsync(stem + ".png-composition.svg", composition);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
        } finally {
            await page.EvaluateAsync("() => { URL.createObjectURL=window.nativeFontObjectUrlOwner;delete window.nativeFontObjectUrlOwner;Object.defineProperty(HTMLImageElement.prototype,'src',nativeFontImageSrcOwner);delete window.nativeFontImageSrcOwner; }");
        }
        var raw = await page.EvaluateAsync<string>(ComparePixels, new { svg = composition, editableSvg = svg, png = Convert.ToBase64String(png) });
        await File.WriteAllTextAsync(stem + ".json", raw);
        using var report = JsonDocument.Parse(raw);
        return new Exported(svg, Convert.ToBase64String(png), composition, report.RootElement.Clone());
    }

    private const string ComparePixels = """
        async ({svg,editableSvg,png})=>{
            const documentSvg=new DOMParser().parseFromString(svg,'image/svg+xml'),root=documentSvg.documentElement;
            const width=Math.round(Number(root.getAttribute('width'))),height=Math.round(Number(root.getAttribute('height'))),scale=Math.max(1,Math.ceil(devicePixelRatio||1));
            // Both sides cross the PNG codec: Firefox can quantize translucent edge colors
            // during encoding, even when the source canvas and downloaded PNG are correct.
            const load=(source,encodePng=false)=>new Promise((resolve,reject)=>{const image=new Image();
                image.onload=()=>{try{
                    const canvas=document.createElement('canvas');canvas.width=width*scale;canvas.height=height*scale;
                    const context=canvas.getContext('2d');context.setTransform(scale,0,0,scale,0,0);context.drawImage(image,0,0,width,height);
                    if(!encodePng){resolve(context.getImageData(0,0,canvas.width,canvas.height).data);return;}
                    canvas.toBlob(blob=>{
                        if(!blob){reject(new Error('Expected PNG comparison encoding.'));return;}
                        const reader=new FileReader();reader.onerror=()=>reject(reader.error);
                        reader.onload=()=>load(String(reader.result)).then(resolve,reject);reader.readAsDataURL(blob);
                    },'image/png');
                }catch(error){reject(error);}};
                image.onerror=()=>reject(new Error('Font export image decode failed.'));
                image.src=source.startsWith('data:')?source:'data:image/svg+xml;charset=utf-8,'+encodeURIComponent(source);});
            const different=(a,b)=>{let count=0;for(let i=0;i<a.length;i+=4)if(a[i]!==b[i]||a[i+1]!==b[i+1]||a[i+2]!==b[i+2]||a[i+3]!==b[i+3])count++;return count;};
            const actual=await load('data:image/png;base64,'+png),same=await load(svg,true),editable=await load(editableSvg,true);
            const undecorated=source=>{const d=new DOMParser().parseFromString(source,'image/svg+xml');
                for(const n of [d.documentElement,...d.querySelectorAll('*')])n.style.setProperty('text-decoration','none','important');
                return new XMLSerializer().serializeToString(d);};
            const editableNone=await load(undecorated(editableSvg),true),pngNone=await load(undecorated(svg),true);
            const decorationInk=pixels=>{let count=0;for(let i=0;i<pixels.length;i+=4)if(pixels[i]===17&&pixels[i+1]===203&&pixels[i+2]===177&&pixels[i+3]===255)count++;return count;};
            for(const node of root.querySelectorAll('*'))node.style.setProperty('font-family','monospace','important');
            const fallback=await load(new XMLSerializer().serializeToString(documentSvg),true);let ink=0,stroke=0;
            for(let i=0;i<actual.length;i+=4){if(actual[i]===18&&actual[i+1]===52&&actual[i+2]===86&&actual[i+3]===255)ink++;
                if(actual[i]===211&&actual[i+1]===17&&actual[i+2]===19&&actual[i+3]===255)stroke++;}
            const nonTranslationGlyphs=Array.from(root.querySelectorAll('use')).filter(node=>{const matrix=node.transform.baseVal.consolidate().matrix;return matrix.a!==1||matrix.b!==0||matrix.c!==0||matrix.d!==1;}).length;
            return JSON.stringify({width,height,scale,resourcePaths:root.querySelectorAll('defs path[id]').length,uses:root.querySelectorAll('use').length,
                decorationInkPixels:decorationInk(actual),editableSvgDecorationInkPixels:decorationInk(editable),
                editableSvgDecorationDifferentPixels:different(editable,editableNone),pngDecorationDifferentPixels:different(actual,pngNone),
                pngVersusCompletedCompositionDifferentPixels:different(actual,same),pngVersusForcedMonospaceDifferentPixels:different(actual,fallback),authoredTextInkPixels:ink,authoredStrokeInkPixels:stroke,nonTranslationGlyphs});
        }
        """;
}
