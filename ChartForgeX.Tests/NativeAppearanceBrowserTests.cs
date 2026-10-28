using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ChartForgeX.Composition;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class NativeAppearanceBrowserTests {
    [BrowserTheory]
    [InlineData("compact", VisualThemeMode.Light)] [InlineData("compact", VisualThemeMode.Dark)]
    [InlineData("wide", VisualThemeMode.Light)] [InlineData("wide", VisualThemeMode.Dark)]
    public async Task ExplicitBakedEffectsAndLocalGlassHaveSelfContainedVisibleOutputs(string variant, VisualThemeMode mode) {
        var outline = NativeAppearanceExample.Create("outline", variant, mode);
        var bloom = NativeAppearanceExample.Create("bloom", variant, mode);
        var glow = NativeAppearanceExample.Create("glow", variant, mode);
        var retainedGlow = glow.Source.WithDropShadow(NativeAppearanceExample.AlphaGlowPolicy(mode));
        var glowNativeSvg = retainedGlow.ToSvg(new VisualSvgOptions(idPrefix: "glow-native",
            effectProfile: VisualSvgEffectProfile.NativeFilters));
        var sourceTextCount = System.Xml.Linq.XDocument.Parse(glow.Source.ToSvg()).Descendants()
            .Count(item => item.Name.LocalName == "text");
        var glass = NativeGlassCompositionExample.Create(variant, mode);
        var image = glass.ToImage();
        var svg = VisualCanvas.Create(image.Width, image.Height).WithBackdrop(VisualCanvasBackdropStyle.Transparent)
            .AddImage(image, 0, 0, image.Width, image.Height).ToSvg();
        var width = variant == "compact" ? 390 : 1100;
        var html = "<style>body{margin:0}section svg{max-width:100%;height:auto}</style>"
            + "<section id='outline'>" + outline.Visual.ToSvg(new VisualSvgOptions(idPrefix: "outline")) + "</section>"
            + "<section id='bloom'>" + bloom.Visual.ToSvg(new VisualSvgOptions(idPrefix: "bloom")) + "</section>"
            + "<section id='glass'>" + svg + "</section>"
            + "<section id='glow'>" + glow.Visual.ToSvg(new VisualSvgOptions(idPrefix: "glow")) + "</section>"
            + "<section id='glow-native'>" + glowNativeSvg + "</section><canvas id='sample'></canvas>";
        await using var session = await BrowserProofFonts.OpenAsync(html, width, 1200);
        var observed = await session.Page.EvaluateAsync<JsonElement>("""
            async () => {
              const svgs=[...document.querySelectorAll('#outline svg,#bloom svg,#glass svg,#glow svg')];
              const nativeGlow=document.querySelector('#glow-native svg');
              const image=new Image();
              image.src='data:image/svg+xml;base64,'+btoa(unescape(encodeURIComponent(svgs[2].outerHTML)));
              await image.decode();
              const canvas=document.querySelector('#sample');canvas.width=image.width;canvas.height=image.height;
              const ctx=canvas.getContext('2d');ctx.drawImage(image,0,0);
              return {loaded:svgs.map(s=>({width:s.getBoundingClientRect().width,height:s.getBoundingClientRect().height,images:s.querySelectorAll('image').length})),
                pixel:[...ctx.getImageData(1,1,1,1).data],scripts:svgs.reduce((n,s)=>n+s.querySelectorAll('script,foreignObject,filter').length,0),
                profiles:svgs.map(s=>({section:s.closest('section').id,svg:s.outerHTML,
                  forbidden:[...s.querySelectorAll('script,foreignObject,filter')].map(node=>({tag:node.tagName,outerHTML:node.outerHTML}))})),

                glow:{filters:nativeGlow.querySelectorAll('filter').length,images:nativeGlow.querySelectorAll('image').length,
                  text:nativeGlow.querySelectorAll('text').length,width:nativeGlow.getBoundingClientRect().width,
                  scripts:nativeGlow.querySelectorAll('script,foreignObject').length},
                overflow:document.documentElement.scrollWidth>innerWidth};
            }
            """);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output);
            var stem = Path.Combine(output, "native-appearance-" + variant + "-" + mode);
            File.WriteAllText(stem + ".glass.svg", svg);
            File.WriteAllText(stem + ".outline.svg", outline.Visual.ToSvg());
            File.WriteAllText(stem + ".bloom.svg", bloom.Visual.ToSvg());
            File.WriteAllText(stem + ".glow.svg", glow.Visual.ToSvg());
            File.WriteAllBytes(stem + ".glow.png", glow.Visual.ToPng());
            File.WriteAllText(stem + ".glow.native-filter.svg", glowNativeSvg);
            File.WriteAllText(stem + ".glow.source.svg", NativeAppearanceExample.AlphaGlowFilterSource(mode));
            File.WriteAllText(stem + ".glow.csharp.txt", ExampleRecipeSource.EmitNativeAppearance("glow", variant, mode));
            File.WriteAllBytes(stem + ".native.png", glass.ToPng());
            File.WriteAllText(stem + ".observed.json", observed.GetRawText());
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
        }
        Assert.Equal(4, observed.GetProperty("loaded").GetArrayLength());
        Assert.All(observed.GetProperty("loaded").EnumerateArray(), item => {
            Assert.True(item.GetProperty("width").GetDouble() > 0);
            Assert.True(item.GetProperty("height").GetDouble() > 0);
            Assert.Equal(1, item.GetProperty("images").GetInt32());
        });
        var expected = AppearanceFixture.Pixel(image, 1, 1);
        for (var channel = 0; channel < 4; channel++) Assert.Equal(expected[channel], observed.GetProperty("pixel")[channel].GetInt32());
        Assert.Equal(1, observed.GetProperty("glow").GetProperty("filters").GetInt32());
        Assert.Equal(0, observed.GetProperty("glow").GetProperty("images").GetInt32());
        Assert.Equal(sourceTextCount, observed.GetProperty("glow").GetProperty("text").GetInt32());
        Assert.True(observed.GetProperty("glow").GetProperty("width").GetDouble() > 0);
        Assert.Equal(0, observed.GetProperty("glow").GetProperty("scripts").GetInt32());
        Assert.Equal(0, observed.GetProperty("scripts").GetInt32());
        Assert.False(observed.GetProperty("overflow").GetBoolean());
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
