using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>POST-only actual source/native sheet; independent noise algorithm selection and exact pixel laws stay in their reference owner.</summary>
[Trait("BrowserSuite", "Compatibility")]
public sealed class SvgRasterTurbulenceAffineBrowserTests {
    [BrowserTheory]
    [InlineData(390, false)]
    [InlineData(390, true)]
    [InlineData(760, false)]
    [InlineData(760, true)]
    public async Task NativeAffineSheetKeepsActualSourceCoverageAndCompleteDownloadAtBothWidths(int width, bool dark) {
        var mode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
        var colors = VisualTheme.Graphite().Resolve(mode);
        var names = new[] { "rotate", "skew", "reflect", "quarter-turn" };
        var html = "<style>body{margin:0;padding:16px;box-sizing:border-box;background:" + colors.Background.ToCss() +
            ";color:" + colors.Foreground.ToCss() + ";font:14px sans-serif}main{display:grid;grid-template-columns:repeat(" +
            (width < 700 ? "2" : "4") + ",minmax(0,1fr));gap:12px}figure{margin:0}figure>svg,figure>img{display:block;width:96px;height:72px;max-width:100%}figcaption{margin-bottom:8px}</style>" +
            "<h1>Affine procedural source</h1><main>";
        foreach (var name in names) {
            var image = NativeTurbulenceExample.RasterizeAffine(name);
            html += "<figure data-case='" + name + "'><figcaption>" + name + "</figcaption>" +
                NativeTurbulenceExample.AffineSource(name) + "<img alt='Native " + name + "' src='data:image/png;base64," +
                Convert.ToBase64String(image.ToPng()) + "'></figure>";
        }
        html += "</main>";
        await using var session = await BrowserProofFonts.OpenAsync(html, width, 520);
        var observed = await session.Page.EvaluateAsync<JsonElement>("""
            async () => {
              await document.fonts.ready;
              const rows=[];
              for(const figure of document.querySelectorAll('figure')) {
                const native=figure.querySelector('img');await native.decode();
                const original=new Image();original.src='data:image/svg+xml;base64,'+btoa(unescape(encodeURIComponent(figure.querySelector('svg').outerHTML)));await original.decode();
                function sample(image) {
                  const canvas=document.createElement('canvas');canvas.width=96;canvas.height=72;
                  const context=canvas.getContext('2d');context.drawImage(image,0,0);
                  const data=context.getImageData(0,0,96,72).data;let visible=0;
                  for(let i=3;i<data.length;i+=4)if(data[i]>0)visible++;
                  return {visible,first:[...data.slice(0,4)],outside:[...data.slice((60*96+90)*4,(60*96+90)*4+4)]};
                }
                rows.push({name:figure.dataset.case,caption:figure.querySelector('figcaption').textContent,
                  primitives:figure.querySelectorAll('feTurbulence').length,native:sample(native),reference:sample(original)});
              }
              return {rows,overflow:document.documentElement.scrollWidth>innerWidth,scripts:document.querySelectorAll('svg script,svg foreignObject').length};
            }
            """);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output);
            var stem = Path.Combine(output, "turbulence-affine-sheet-" + width + "-" + mode);
            File.WriteAllText(stem + ".observed.json", observed.GetRawText());
            File.WriteAllText(stem + ".csharp.txt", ExampleRecipeSource.EmitNativeTurbulence(width < 700 ? "compact" : "wide", mode));
            foreach (var name in names) {
                File.WriteAllText(stem + "." + name + ".source.svg", NativeTurbulenceExample.AffineSource(name));
                File.WriteAllBytes(stem + "." + name + ".native.png", NativeTurbulenceExample.RasterizeAffine(name).ToPng());
            }
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
        }
        Assert.Equal(4, observed.GetProperty("rows").GetArrayLength());
        foreach (var row in observed.GetProperty("rows").EnumerateArray()) {
            Assert.Equal(row.GetProperty("name").GetString(), row.GetProperty("caption").GetString());
            Assert.Equal(1, row.GetProperty("primitives").GetInt32());
            foreach (var kind in new[] { "native", "reference" }) {
                var pixels = row.GetProperty(kind);
                Assert.True(pixels.GetProperty("visible").GetInt32() > 0);
                Assert.Equal(new[] { 0, 0, 0, 0 }, pixels.GetProperty("first").EnumerateArray().Select(value => value.GetInt32()));
                Assert.Equal(new[] { 0, 0, 0, 0 }, pixels.GetProperty("outside").EnumerateArray().Select(value => value.GetInt32()));
            }
        }
        Assert.False(observed.GetProperty("overflow").GetBoolean());
        Assert.Equal(0, observed.GetProperty("scripts").GetInt32());
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
