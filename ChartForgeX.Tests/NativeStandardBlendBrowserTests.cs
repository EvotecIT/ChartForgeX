using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ChartForgeX;
using ChartForgeX.Composition;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class NativeStandardBlendBrowserTests {
    [BrowserTheory]
    [InlineData("compact", false)]
    [InlineData("compact", true)]
    [InlineData("wide", false)]
    [InlineData("wide", true)]
    public async Task ExplicitNativeAndFiniteSourceModesKeepTheirInputsVisibleAndCompletePrograms(string variant, bool dark) {
        var cases = new (string Name, VisualBlendMode Mode, int[] Expected)[] {
            ("overlay", VisualBlendMode.Overlay, new[] { 85, 0, 170, 192 }),
            ("darken", VisualBlendMode.Darken, new[] { 85, 0, 85, 192 }),
            ("lighten", VisualBlendMode.Lighten, new[] { 170, 0, 170, 192 }),
            ("color-dodge", VisualBlendMode.ColorDodge, new[] { 85, 0, 170, 192 }),
            ("color-burn", VisualBlendMode.ColorBurn, new[] { 85, 0, 170, 192 }),
            ("hard-light", VisualBlendMode.HardLight, new[] { 170, 0, 85, 192 }),
            ("soft-light", VisualBlendMode.SoftLight, new[] { 85, 0, 170, 192 }),
            ("difference", VisualBlendMode.Difference, new[] { 170, 0, 170, 192 }),
            ("exclusion", VisualBlendMode.Exclusion, new[] { 170, 0, 170, 192 }),
            ("hue", VisualBlendMode.Hue, new[] { 116, 0, 85, 192 }),
            ("saturation", VisualBlendMode.Saturation, new[] { 85, 0, 170, 192 }),
            ("color", VisualBlendMode.Color, new[] { 116, 0, 85, 192 }),
            ("luminosity", VisualBlendMode.Luminosity, new[] { 103, 18, 170, 192 })
        };
        var foreground = dark ? "#f8fafc" : "#0f172a";
        var background = dark ? "#0f172a" : "#f8fafc";
        var html = new StringBuilder("<!doctype html><meta charset='utf-8'><style>*{box-sizing:border-box}body{margin:0;padding:12px;font:15px system-ui;background:"
            + background + ";color:" + foreground + "}h1{font-size:20px}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(160px,1fr));gap:12px}figure{margin:0;padding:8px;border:1px solid #8090a0}figcaption{margin-bottom:8px}.samples{display:flex;gap:12px}.samples svg,.samples img{display:block;width:64px;height:64px;image-rendering:pixelated}small{display:block}</style><h1>Explicit isolated standard blends</h1><div class='grid'>");
        var sourceLayer = CompositeOperatorFixture.Layer(ChartColor.FromRgba(255, 0, 0, 128));
        var nativePixels = new System.Collections.Generic.Dictionary<string, int[]>();
        foreach (var item in cases) {
            var source = NativeMaskExample.FilterSource(item.Name)
                .Replace("id='blend'", "id='blend-" + item.Name + "'")
                .Replace("url(#blend)", "url(#blend-" + item.Name + ")")
                .Replace("id='source'", "id='source-" + item.Name + "'");
            var native = ImageComposition.Create(2, 2, ChartColor.FromRgba(0, 0, 255, 128))
                .DrawLayer(sourceLayer, 0, 0, item.Mode, orderedDither: false);
            nativePixels.Add(item.Name, CompositeOperatorFixture.Pixel(native.ToImage(), 0, 0).Select(value => (int)value).ToArray());
            html.Append("<figure data-mode='").Append(item.Name).Append("'><figcaption>").Append(item.Name)
                .Append("</figcaption><div class='samples'><section>").Append(source)
                .Append("<small>SVG source</small></section><section><img alt='Native ").Append(item.Name)
                .Append("' src='data:image/png;base64,").Append(Convert.ToBase64String(native.ToPng()))
                .Append("'><small>Native pixels</small></section></div></figure>");
        }
        html.Append("</div>");
        await using var session = await BrowserProofFonts.OpenAsync(html.ToString(), variant == "compact" ? 390 : 760, 1000);
        var observed = await session.Page.EvaluateAsync<JsonElement>("""
            async () => {
              await document.fonts.ready;
              const rows=[];
              for(const figure of document.querySelectorAll('figure[data-mode]')){
                const svg=figure.querySelector('svg'),native=figure.querySelector('img');
                const blob=URL.createObjectURL(new Blob([new XMLSerializer().serializeToString(svg)],{type:'image/svg+xml'}));
                try {
                  const reference=new Image();reference.src=blob;await reference.decode();await native.decode();
                  const canvas=document.createElement('canvas');canvas.width=32;canvas.height=32;
                  const context=canvas.getContext('2d');context.drawImage(reference,0,0);
                  const source=[...context.getImageData(16,16,1,1).data];
                  context.clearRect(0,0,32,32);context.drawImage(native,0,0);
                  const pixels=[...context.getImageData(0,0,1,1).data];
                  rows.push({name:figure.dataset.mode,mode:svg.querySelector('feBlend').getAttribute('mode'),source,native:pixels,
                    words:figure.querySelector('figcaption').textContent,scripts:svg.querySelectorAll('script,foreignObject').length,
                    size:[native.naturalWidth,native.naturalHeight]});
                } finally {URL.revokeObjectURL(blob)}
              }
              return {rows,overflow:document.documentElement.scrollWidth>innerWidth};
            }
            """);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output);
            var stem = Path.Combine(output, "standard-blends-" + variant + (dark ? "-dark" : "-light"));
            File.WriteAllText(stem + ".html", html.ToString());
            File.WriteAllText(stem + ".observed.json", observed.GetRawText());
            File.WriteAllText(stem + ".native-rgba.json", JsonSerializer.Serialize(nativePixels));
            File.WriteAllText(stem + ".core.csharp.txt", ExampleRecipeSource.EmitNativeMask(variant, dark ? ChartForgeX.Themes.VisualThemeMode.Dark : ChartForgeX.Themes.VisualThemeMode.Light));
            File.WriteAllText(stem + ".visuals.csharp.txt", ExampleRecipeSource.EmitNativeMaskComposition(variant, dark ? ChartForgeX.Themes.VisualThemeMode.Dark : ChartForgeX.Themes.VisualThemeMode.Light));
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
        }
        var rows = observed.GetProperty("rows").EnumerateArray().ToArray();
        Assert.Equal(cases.Length, rows.Length);
        foreach (var row in rows) {
            var item = Assert.Single(cases, item => item.Name == row.GetProperty("name").GetString());
            Assert.Equal(item.Name, row.GetProperty("mode").GetString());
            Assert.Equal(item.Name, row.GetProperty("words").GetString());
            Assert.Equal(item.Expected, nativePixels[item.Name]);
            // Canvas image readback may quantize the PNG through a premultiplied browser surface.
            // Exact unassociated native bytes are asserted above, and both actual browser values are captured.
            Assert.Equal(192, row.GetProperty("native")[3].GetInt32());
            Assert.Equal(192, row.GetProperty("source")[3].GetInt32());
            Assert.Equal(new[] { 2, 2 }, row.GetProperty("size").EnumerateArray().Select(value => value.GetInt32()).ToArray());
            Assert.Equal(0, row.GetProperty("scripts").GetInt32());
        }
        Assert.False(observed.GetProperty("overflow").GetBoolean());
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
