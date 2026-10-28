using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ChartForgeX;
using ChartForgeX.Composition;
using ChartForgeX.Effects;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class FiniteCompositingBrowserTests {
    [BrowserTheory]
    [InlineData("compact", false)] [InlineData("wide", false)]
    [InlineData("compact", true)] [InlineData("wide", true)]
    public async Task ExplicitSourceOperatorsAndOptionalNativeStencilHaveVisibleIndependentInputs(string variant, bool dark) {
        var mode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
        var width = variant == "compact" ? 390 : 1100;
        var operators = new[] { "over", "in", "out", "atop", "xor" };
        var background = VisualTheme.Graphite().Resolve(mode).Background.ToCss();
        var html = "<style>body{margin:0;background:" + background + "}section{max-width:100%}svg,img{display:block;max-width:100%;height:auto}</style>";
        foreach (var operation in operators) {
            var source = NativeCompositeSourceExample.Source(variant, mode, operation);
            var native = NativeCompositeSourceExample.Rasterize(variant, mode, operation).Image.ToPng();
            html += "<section id='source-pane-" + operation + "'>" + source + "</section><img id='native-" + operation
                + "' alt='Native " + operation + "' src='data:image/png;base64," + Convert.ToBase64String(native) + "'>";
        }
        var opacitySource = NativeCompositeSourceExample.SourceWithOpacity(variant, mode);
        var opacityResult = NativeCompositeSourceExample.RasterizeWithOpacity(variant, mode);
        html += "<section id='nested-opacity'>" + opacitySource + "</section><img id='nested-native' alt='Completed group opacity' src='data:image/png;base64,"
            + Convert.ToBase64String(opacityResult.Image.ToPng()) + "'>";
        var composition = NativeCompositeCompositionExample.Create(variant, mode, VisualCompositeOperator.SourceIn);
        var image = composition.ToImage();
        var canvas = VisualCanvas.Create(image.Width, image.Height).WithBackdrop(VisualCanvasBackdropStyle.Transparent)
            .WithTitle("Explicit native stencil").AddImage(image, 0, 0, image.Width, image.Height);
        var stencil = canvas.ToSvg();
        html += "<section id='stencil'>" + stencil + "</section>";
        await using var session = await BrowserProofFonts.OpenAsync(html, width, 900);
        var observed = await session.Page.EvaluateAsync<JsonElement>("""
            async () => {
              await document.fonts.ready;
              const rows=[];
              for(const operation of ['over','in','out','atop','xor']){
                const svg=document.querySelector('#source-pane-'+operation+' svg');
                const filter=svg.querySelector('filter'),primitive=filter.querySelector('feComposite');
                const preview=new Image();preview.src='data:image/svg+xml;base64,'+btoa(unescape(encodeURIComponent(svg.outerHTML)));
                await preview.decode();
                const native=document.querySelector('#native-'+operation);await native.decode();
                const surface=document.createElement('canvas');surface.width=native.naturalWidth;surface.height=native.naturalHeight;
                const context=surface.getContext('2d');context.drawImage(preview,0,0);
                const browser=[...context.getImageData(Math.floor(surface.width/2),120,1,1).data];
                context.clearRect(0,0,surface.width,surface.height);context.drawImage(native,0,0);
                const pixels=[...context.getImageData(Math.floor(surface.width/2),120,1,1).data];
                rows.push({operation,browser,native:pixels,reference:primitive.getAttribute('in2'),
                  resolves:!!document.getElementById(filter.id),words:svg.querySelector('text').textContent,
                  width:native.naturalWidth,height:native.naturalHeight,scripts:svg.querySelectorAll('script,foreignObject').length});
              }
              const opacitySvg=document.querySelector('#nested-opacity svg');
              const opacityNative=document.querySelector('#nested-native');await opacityNative.decode();
              const opacityReference=new Image();
              opacityReference.src='data:image/svg+xml;base64,'+btoa(unescape(encodeURIComponent(opacitySvg.outerHTML)));
              await opacityReference.decode();
              const surface=document.createElement('canvas');surface.width=opacityNative.naturalWidth;surface.height=opacityNative.naturalHeight;
              const context=surface.getContext('2d');context.drawImage(opacityReference,0,0);
              const opacityBrowser=[...context.getImageData(Math.floor(surface.width/2),120,1,1).data];
              context.clearRect(0,0,surface.width,surface.height);context.drawImage(opacityNative,0,0);
              const opacityPixels=[...context.getImageData(Math.floor(surface.width/2),120,1,1).data];
              return {rows,opacityBrowser,opacityNative:opacityPixels,opacity:opacitySvg.querySelector('g[opacity]').getAttribute('opacity'),
                images:document.querySelectorAll('#stencil svg image').length,
                liveStencilText:document.querySelectorAll('#stencil svg text').length,
                overflow:document.documentElement.scrollWidth>innerWidth};
            }
            """);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output);
            var stem = Path.Combine(output, "finite-composite-browser-" + variant + "-" + mode);
            File.WriteAllText(stem + ".observed.json", observed.GetRawText());
            File.WriteAllText(stem + ".source-opacity.svg", opacitySource);
            File.WriteAllBytes(stem + ".source-opacity.native.png", opacityResult.Image.ToPng());
            File.WriteAllText(stem + ".program.csharp.txt", ExampleRecipeSource.EmitNativeCompositeSource(variant, mode));
            File.WriteAllText(stem + ".stencil.svg", stencil);
            File.WriteAllBytes(stem + ".native.png", composition.ToPng());
            foreach (var operation in operators) {
                File.WriteAllText(stem + "-" + operation + ".svg", NativeCompositeSourceExample.Source(variant, mode, operation));
                File.WriteAllBytes(stem + "-" + operation + ".native.png", NativeCompositeSourceExample.Rasterize(variant, mode, operation).Image.ToPng());
            }
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
        }
        var rows = observed.GetProperty("rows").EnumerateArray().ToArray();
        Assert.Equal(5, rows.Length);
        foreach (var row in rows) {
            Assert.True(row.GetProperty("resolves").GetBoolean());
            Assert.Equal("backdrop", row.GetProperty("reference").GetString());
            Assert.Contains("Source " + row.GetProperty("operation").GetString(), row.GetProperty("words").GetString());
            Assert.Equal(variant == "compact" ? 390 : 760, row.GetProperty("width").GetInt32());
            Assert.Equal(280, row.GetProperty("height").GetInt32());
            Assert.Equal(0, row.GetProperty("scripts").GetInt32());
            foreach (var key in new[] { "browser", "native" }) {
                var pixels = row.GetProperty(key).EnumerateArray().Select(item => item.GetInt32()).ToArray();
                Assert.True(pixels[3] > 0);
                if (row.GetProperty("operation").GetString() is "in" or "out") {
                    Assert.Equal(255, pixels[0]);
                    Assert.Equal(0, pixels[2]);
                    Assert.True(pixels[3] < 128);
                } else {
                    Assert.True(pixels[0] > 0 && pixels[2] > 0);
                }
            }
        }
        Assert.Equal(".5", observed.GetProperty("opacity").GetString());
        foreach (var key in new[] { "opacityBrowser", "opacityNative" })
            Assert.Equal(new[] { 255, 0, 0, 32 }, observed.GetProperty(key).EnumerateArray().Select(item => item.GetInt32()).ToArray());
        Assert.Empty(opacityResult.Diagnostics);
        Assert.Equal(1, observed.GetProperty("images").GetInt32());
        Assert.Equal(0, observed.GetProperty("liveStencilText").GetInt32()); // Explicit captured-image composition, not a vector identity claim.
        Assert.False(observed.GetProperty("overflow").GetBoolean());
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
