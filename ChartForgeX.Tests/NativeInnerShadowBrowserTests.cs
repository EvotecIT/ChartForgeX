using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class NativeInnerShadowBrowserTests {
    [BrowserTheory]
    [InlineData("compact", VisualThemeMode.Light)]
    [InlineData("compact", VisualThemeMode.Dark)]
    [InlineData("wide", VisualThemeMode.Light)]
    [InlineData("wide", VisualThemeMode.Dark)]
    public async Task ExplicitNativeInsetKeepsVectorWordsAndSourceAlphaBesideItsBakedCompanion(string variant, VisualThemeMode mode) {
        var source = NativeAppearanceExample.PrepareSource(variant, mode);
        var layer = NativeAppearanceExample.Create("inner", variant, mode);
        var retained = source.WithInnerShadow(NativeAppearanceExample.InnerShadowPolicy(mode));
        var svg = retained.ToSvg(new VisualSvgOptions(idPrefix: "inner-native", effectProfile: VisualSvgEffectProfile.NativeFilters));
        var sourceWords = XDocument.Parse(source.ToSvg()).Descendants().Count(item => item.Name.LocalName == "text");
        var simple = InnerShadowFixture.Source(128).WithInnerShadow(new VisualInnerShadow(
            ChartColor.FromRgb(0, 0, 255), 0, 0, 0, orderedDither: false));
        var simpleSvg = simple.ToSvg(new VisualSvgOptions(idPrefix: "inner-sample", effectProfile: VisualSvgEffectProfile.NativeFilters));
        var background = mode == VisualThemeMode.Dark ? "#0f172a" : "#f8fafc";
        var html = "<style>body{margin:0;background:" + background + "}section svg{max-width:100%;height:auto}</style>"
            + "<section id='native'>" + svg + "</section><section id='baked'>" + layer.Visual.ToSvg() + "</section>"
            + "<section id='specimen'>" + simpleSvg + "</section><canvas id='sample'></canvas>";
        await using var session = await BrowserProofFonts.OpenAsync(html, variant == "compact" ? 390 : 800, 750);
        var observed = await session.Page.EvaluateAsync<JsonElement>("""
            async () => {
              const native=document.querySelector('#native svg'),baked=document.querySelector('#baked svg');
              const specimen=document.querySelector('#specimen svg'),image=new Image();
              image.src='data:image/svg+xml;base64,'+btoa(unescape(encodeURIComponent(specimen.outerHTML)));
              await image.decode();
              const canvas=document.querySelector('#sample');canvas.width=image.width;canvas.height=image.height;
              const ctx=canvas.getContext('2d');ctx.drawImage(image,0,0);
              return {filters:native.querySelectorAll('filter').length,images:native.querySelectorAll('image').length,
                text:native.querySelectorAll('text').length,bakedImages:baked.querySelectorAll('image').length,
                scripts:document.querySelectorAll('script,foreignObject').length,
                width:native.getBoundingClientRect().width,height:native.getBoundingClientRect().height,
                interior:[...ctx.getImageData(8,8,1,1).data],exterior:[...ctx.getImageData(3,8,1,1).data],
                overflow:document.documentElement.scrollWidth>innerWidth};
            }
            """);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output);
            var stem = Path.Combine(output, "native-inner-shadow-" + variant + "-" + mode);
            File.WriteAllText(stem + ".native-filter.svg", svg);
            File.WriteAllBytes(stem + ".native.png", layer.Visual.ToPng(InnerShadowFixture.Unit));
            File.WriteAllText(stem + ".csharp.txt", ExampleRecipeSource.EmitNativeAppearance("inner", variant, mode));
            File.WriteAllText(stem + ".observed.json", observed.GetRawText());
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
        }
        Assert.Equal(1, observed.GetProperty("filters").GetInt32());
        Assert.Equal(0, observed.GetProperty("images").GetInt32());
        Assert.Equal(sourceWords, observed.GetProperty("text").GetInt32());
        Assert.Equal(1, observed.GetProperty("bakedImages").GetInt32());
        Assert.Equal(0, observed.GetProperty("scripts").GetInt32());
        Assert.True(observed.GetProperty("width").GetDouble() > 0 && observed.GetProperty("height").GetDouble() > 0);
        Assert.Equal(128, observed.GetProperty("interior")[3].GetInt32());
        // Browser premultiplied readback has its own RGB quantization; native raw RGB is checked separately.
        Assert.True(observed.GetProperty("interior")[0].GetInt32() > 0);
        Assert.True(observed.GetProperty("interior")[2].GetInt32() > 0);
        Assert.All(observed.GetProperty("exterior").EnumerateArray(), channel => Assert.Equal(0, channel.GetInt32()));
        Assert.False(observed.GetProperty("overflow").GetBoolean());
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
