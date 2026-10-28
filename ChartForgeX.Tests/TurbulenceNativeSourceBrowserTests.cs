using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ChartForgeX.Effects;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>POST-only actual source/profile paint and native frame contracts, not universal browser pixel identity.</summary>
[Trait("BrowserSuite", "Compatibility")]
public sealed class TurbulenceNativeSourceBrowserTests {
    [BrowserTheory]
    [InlineData("compact", false)] [InlineData("wide", false)]
    [InlineData("compact", true)] [InlineData("wide", true)]
    public async Task FiniteProceduralSourceProfilesRetainOneSurfaceAndUnfilteredCommonFrameWords(string variant, bool dark) {
        var mode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
        var width = variant == "compact" ? 390 : 760;
        var saved=NativeTurbulenceExample.Save(variant,mode);
        var prepared = NativeTurbulenceExample.Load(saved,variant,mode);
        var native = prepared.ToSvg(new VisualSvgOptions("turbulence-native", responsive: true,
            effectProfile: VisualSvgEffectProfile.NativeFilters));
        var baked = prepared.ToSvg(new VisualSvgOptions("turbulence-baked", responsive: true));
        var plate = new VisualTurbulence(.071, .113, 2, 13, VisualTurbulenceType.FractalNoise)
            .Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(64, 48), 0),
                frame: new VisualFrame(transparentBackground: true)))
            .ToSvg(new VisualSvgOptions("turbulence-plate", effectProfile: VisualSvgEffectProfile.NativeFilters));
        var html = "<style>body{margin:0;background:" + VisualTheme.Graphite().Resolve(mode).Background.ToCss() +
            "}section{max-width:100%}section>svg{display:block}</style><section id='native'>" + native +
            "</section><section id='baked'>" + baked + "</section><div id='plate'>" + plate +
            "</div><canvas id='probe' width='64' height='48'></canvas>";
        await using var session = await BrowserProofFonts.OpenAsync(html, width, 800);
        var observed = await session.Page.EvaluateAsync<JsonElement>("""
            async () => {
              await document.fonts.ready;
              const native=document.querySelector('#native>svg'),baked=document.querySelector('#baked>svg');
              const primitive=native.querySelector('feTurbulence'),surface=native.querySelector('[data-cfx-role="procedural-source"]');
              const target=/^url\(#([^()\s]+)\)$/.exec(surface.getAttribute('filter'))?.[1];
              const labels=[...native.querySelectorAll('text')];
              const image=new Image();image.src='data:image/svg+xml;base64,'+btoa(unescape(encodeURIComponent(document.querySelector('#plate svg').outerHTML)));
              await image.decode();const context=document.querySelector('#probe').getContext('2d');context.drawImage(image,0,0);
              return {userAgent:navigator.userAgent,words:labels.map(node=>node.textContent),
                filteredWords:labels.filter(node=>node.closest('[filter]')).map(node=>node.textContent),
                sourceSurfaces:native.querySelectorAll('[data-cfx-role="procedural-source"]').length,
                resolves:document.getElementById(target)?.localName==='filter',octaves:primitive.getAttribute('numOctaves'),
                type:primitive.getAttribute('type'),space:native.querySelector('filter').getAttribute('color-interpolation-filters'),
                nativeImages:native.querySelectorAll('image').length,bakedImages:baked.querySelectorAll('image').length,
                bakedPrimitives:baked.querySelectorAll('feTurbulence').length,
                pixels:[[7,11],[23,19],[41,29]].map(([x,y])=>[...context.getImageData(x,y,1,1).data]),
                scripts:native.querySelectorAll('script,foreignObject').length,overflow:document.documentElement.scrollWidth>innerWidth};
            }
            """);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output);
            var stem = Path.Combine(output, "turbulence-native-source-" + variant + "-" + mode);
            File.WriteAllText(stem + ".source.json",saved);
            File.WriteAllText(stem + ".native.svg", native); File.WriteAllText(stem + ".baked.svg", baked);
            File.WriteAllText(stem + ".observed.json", observed.GetRawText());
            File.WriteAllBytes(stem + ".native.png", prepared.ToPng());
            File.WriteAllBytes(stem + ".generated.png", NativeTurbulenceExample.Generate(variant).ToPng());
            File.WriteAllText(stem + ".csharp.txt", ExampleRecipeSource.EmitNativeTurbulence(variant, mode));
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
        }
        Assert.Equal(NativeTurbulenceExample.Create(variant,mode).ToPng(),prepared.ToPng());
        var words = string.Join(" ", observed.GetProperty("words").EnumerateArray());
        Assert.Contains("Noise as a source", words); Assert.Contains("One retained four-channel procedural surface", words);
        Assert.Empty(observed.GetProperty("filteredWords").EnumerateArray());
        Assert.Equal(1, observed.GetProperty("sourceSurfaces").GetInt32()); Assert.True(observed.GetProperty("resolves").GetBoolean());
        Assert.Equal("3", observed.GetProperty("octaves").GetString()); Assert.Equal("fractalNoise", observed.GetProperty("type").GetString());
        Assert.Equal("sRGB", observed.GetProperty("space").GetString());
        Assert.Equal(0, observed.GetProperty("nativeImages").GetInt32()); Assert.Equal(1, observed.GetProperty("bakedImages").GetInt32());
        Assert.Equal(0, observed.GetProperty("bakedPrimitives").GetInt32());
        var pixels = observed.GetProperty("pixels"); Assert.NotEqual(pixels[0].GetRawText(), pixels[1].GetRawText());
        Assert.All(pixels.EnumerateArray(), pixel => Assert.True(pixel[3].GetInt32() > 0));
        Assert.Equal(0, observed.GetProperty("scripts").GetInt32()); Assert.False(observed.GetProperty("overflow").GetBoolean());
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
