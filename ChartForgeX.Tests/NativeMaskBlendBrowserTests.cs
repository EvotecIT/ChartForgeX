using System.Text.Json;
using ChartForgeX.Composition;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class NativeMaskBlendBrowserTests {
    [BrowserTheory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public async Task NativeMasksAdditiveCompositionAndStandardsSourceMasksStayVisible(bool dark, bool compact) {
        if (!Enabled) return;
        var mode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
        var native = NativeMaskExample.Create(compact ? "compact" : "wide", mode);
        var report = NativeMaskBlendCompositionExample.Create(compact ? "compact" : "wide", mode);
        var builder = new VisualSceneBuilder(new VisualSize(8, 6), FontSpec.SystemSans());
        builder.Rect(new ChartRect(0, 0, 8, 6), ChartColor.FromRgba(255, 0, 0, 128));
        var red = new PreparedVisual(builder.Build()).CaptureLayer(new VisualLayerBlur(0), MaskBlendFixture.Options);
        var additive = ImageComposition.Create(8, 6, ChartColor.FromRgba(0, 0, 255, 128)).DrawLayer(red, 0, 0, VisualBlendMode.PlusLighter, orderedDither: false);
        var mask = MaskBlendBeforeFixture.Mask("#ff0000", "luminance", "1", null);
        var linear = MaskBlendBeforeFixture.Mask("#808080", "luminance", "1", "linearRGB");
        var background = dark ? "#0f172a" : "#f8fafc"; var foreground = dark ? "#f8fafc" : "#0f172a";
        var html = "<!doctype html><html><head><meta charset='utf-8'><style>body{margin:0;padding:16px;background:" + background
            + ";color:" + foreground + ";font:16px system-ui}section{margin:16px 0;overflow:auto}.probes{display:flex;gap:16px;flex-wrap:wrap}.probes svg,.probes img{width:160px;height:160px;image-rendering:pixelated}h1{font-size:20px}</style></head><body>"
            + "<h1>Native alpha fade and explicit report blending</h1><section id='native'>" + native.Visual.ToSvg()
            + "</section><section><img id='report' alt='Native screen composition' src='data:image/png;base64," + Convert.ToBase64String(report.ToPng())
            + "'></section><div class='probes'><section id='mask'>" + mask + "</section><section id='linear'>" + linear
            + "</section><section><img id='additive' alt='Additive premultiplied composition' src='data:image/png;base64," + Convert.ToBase64String(additive.ToPng())
            + "'></section><section id='source-blend'>" + NativeMaskExample.FilterSource() + "</section></div></body></html>";
        var errors = new List<string>();
        await using var session = await OpenAsync(html, compact ? 390 : 1100, 1100,
            configurePage: configuredPage => configuredPage.PageError += (_, error) => errors.Add(error));
        var page = session.Page; await page.WaitForSelectorAsync("#native svg image");
        var observed = await page.EvaluateAsync<JsonElement>("""
            async () => {
              async function svgPixel(selector) {
                const svg=document.querySelector(selector),copy=svg.cloneNode(true);copy.setAttribute('xmlns','http://www.w3.org/2000/svg');
                const uri=URL.createObjectURL(new Blob([new XMLSerializer().serializeToString(copy)],{type:'image/svg+xml'}));
                try {const img=new Image();await new Promise((resolve,reject)=>{img.onload=resolve;img.onerror=reject;img.src=uri});
                  const c=document.createElement('canvas');c.width=32;c.height=32;const x=c.getContext('2d');x.drawImage(img,0,0);return [...x.getImageData(16,16,1,1).data];
                }finally{URL.revokeObjectURL(uri)}
              }
              const image=document.querySelector('#additive');await image.decode();const c=document.createElement('canvas');c.width=8;c.height=6;
              const x=c.getContext('2d');x.drawImage(image,0,0);const chart=document.querySelector('#native svg'),b=chart.getBoundingClientRect();
              const report=document.querySelector('#report');await report.decode();
              return {mask:await svgPixel('#mask svg'),linear:await svgPixel('#linear svg'),sourceBlend:await svgPixel('#source-blend svg'),
                additive:[...x.getImageData(4,3,1,1).data],chart:{width:b.width,height:b.height,images:chart.querySelectorAll('image').length,
                scripts:chart.querySelectorAll('script,foreignObject').length,targets:chart.querySelectorAll('[data-cfx-target-kind]').length},report:[report.naturalWidth,report.naturalHeight]};
            }
            """);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory); var stem = Path.Combine(directory, "native-mask-blend-" + (compact ? "compact" : "wide") + (dark ? "-dark" : "-light"));
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
            File.WriteAllText(stem + ".native.svg", native.Visual.ToSvg()); File.WriteAllBytes(stem + ".native.png", native.Visual.ToPng());
            File.WriteAllBytes(stem + ".report.png", report.ToPng()); File.WriteAllText(stem + ".mask.source.svg", mask); File.WriteAllText(stem + ".linear.source.svg", linear);
            File.WriteAllText(stem + ".blend.source.svg", NativeMaskExample.FilterSource());
            File.WriteAllText(stem + ".observed.json", JsonSerializer.Serialize(new { observed, errors, session.ConsoleLog }));
        }
        Assert.Equal(new[] { 255, 255, 255, 54 }, observed.GetProperty("mask").EnumerateArray().Select(item => item.GetInt32()));
        Assert.Equal(new[] { 255, 255, 255, 55 }, observed.GetProperty("linear").EnumerateArray().Select(item => item.GetInt32()));
        Assert.Equal(new[] { 128, 0, 128, 255 }, observed.GetProperty("additive").EnumerateArray().Select(item => item.GetInt32()));
        var sourceBlend = observed.GetProperty("sourceBlend").EnumerateArray().Select(item => item.GetInt32()).ToArray();
        Assert.InRange(sourceBlend[0], 169, 171); Assert.Equal(0, sourceBlend[1]); Assert.InRange(sourceBlend[2], 169, 171); Assert.Equal(192, sourceBlend[3]);
        Assert.Equal(1, observed.GetProperty("chart").GetProperty("images").GetInt32()); Assert.Equal(0, observed.GetProperty("chart").GetProperty("scripts").GetInt32());
        Assert.Equal(0, observed.GetProperty("chart").GetProperty("targets").GetInt32()); Assert.True(observed.GetProperty("chart").GetProperty("width").GetDouble() > 0);
        Assert.Equal(new[] { (compact ? 320 : 740) + 64, 304 }, observed.GetProperty("report").EnumerateArray().Select(item => item.GetInt32()));
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }
}
