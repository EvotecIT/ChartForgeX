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

[Trait("BrowserSuite", "Compatibility")]
public sealed class RetainedMaskBrowserTests {
    [BrowserTheory]
    [InlineData("compact", false)] [InlineData("wide", false)]
    [InlineData("compact", true)] [InlineData("wide", true)]
    public async Task TypedAlphaMaskKeepsLiveNativeVectorWordsLinksAndExplicitBakedCompanion(string variant, bool dark) {
        var mode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
        var width = variant == "compact" ? 390 : 1100;
        var source = NativeRetainedMaskExample.PrepareSource(variant, mode);
        var original = source.WithMask(NativeRetainedMaskExample.CreateMask(source));
        var saved = original.ToArtifact().ToInterchangeJson();
        var loaded = VisualArtifactInterchangeEnvelope.FromJson(saved);
        var masked = loaded.ApplyCapturedEffects(source);
        var edited = NativeRetainedMaskExample.Restore(saved, variant, mode, "Approved");
        var editedSvg = edited.ToSvg(new VisualSvgOptions(idPrefix: "retained-mask-edited",
            effectProfile: VisualSvgEffectProfile.NativeFilters, responsive: true));
        var native = masked.ToSvg(new VisualSvgOptions(idPrefix: "retained-mask-browser",
            effectProfile: VisualSvgEffectProfile.NativeFilters, responsive: true));
        var baked = masked.ToSvg(new VisualSvgOptions(idPrefix: "retained-mask-baked", responsive: true));
        var plate = RetainedMaskFixture.Plate().WithMask(RetainedMaskFixture.Mask(64, 48, 1, VisualMaskMode.Luminance));
        var plateSvg = plate.ToSvg(new VisualSvgOptions(idPrefix: "retained-mask-plate", effectProfile: VisualSvgEffectProfile.NativeFilters));
        var background = VisualTheme.Graphite().Resolve(mode).Background.ToCss();
        var html = "<style>body{margin:0;background:" + background + "}section{max-width:100%}svg{display:block}</style>" +
            "<section id='native'>" + native + "</section><section id='baked'>" + baked +
            "</section><section id='edited'>" + editedSvg + "</section><div id='plate'>" + plateSvg + "</div><canvas id='sample' width='64' height='48'></canvas><div id='guide'>Guide</div>";
        await using var session = await BrowserProofFonts.OpenAsync(html, width, 1000);
        var observed = await session.Page.EvaluateAsync<JsonElement>("""
            async () => {
              await document.fonts.ready;
              const native=document.querySelector('#native svg'),baked=document.querySelector('#baked svg');
              const frame=native.getBoundingClientRect();
              const words=[...native.querySelectorAll('text')].map(e=>e.textContent);
              const escaped=[...native.querySelectorAll('text')].filter(e=>{const r=e.getBoundingClientRect();return r.width&&r.height&&(r.left<frame.left-1||r.right>frame.right+1||r.top<frame.top-1||r.bottom>frame.bottom+1)}).map(e=>e.textContent);
              const references=[...native.querySelectorAll('[mask]')].map(e=>{const id=/#([^)'"\s]+)/.exec(e.getAttribute('mask'))?.[1];const mask=document.getElementById(id);return {id,resolves:!!mask,type:mask?.getAttribute('mask-type')}});
              const image=new Image();image.src='data:image/svg+xml;base64,'+btoa(unescape(encodeURIComponent(document.querySelector('#plate svg').outerHTML)));
              await image.decode();const context=document.querySelector('#sample').getContext('2d');context.drawImage(image,0,0);
              return {words,escaped,references,links:[...native.querySelectorAll('a')].map(e=>({href:e.getAttribute('href'),tab:e.getAttribute('tabindex')})),
                editedWords:[...document.querySelectorAll('#edited svg text')].map(e=>e.textContent),
                editedMasks:document.querySelectorAll('#edited svg mask').length,
                nativeText:native.querySelectorAll('text').length,bakedText:baked.querySelectorAll('text').length,
                bakedMasks:baked.querySelectorAll('mask').length,bakedImages:baked.querySelectorAll('image').length,
                alpha:context.getImageData(25,17,1,1).data[3],scripts:native.querySelectorAll('script,foreignObject').length,
                overflow:document.documentElement.scrollWidth>innerWidth};
            }
            """);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output);
            var stem = Path.Combine(output, "retained-mask-browser-" + variant + "-" + mode);
            File.WriteAllText(stem + ".native.svg", native);
            File.WriteAllText(stem + ".baked.svg", baked);
            File.WriteAllBytes(stem + ".native.png", masked.ToPng());
            File.WriteAllText(stem + ".observed.json", observed.GetRawText());
            File.WriteAllText(stem + ".source-facts.json", source.ToArtifact().ToInterchangeJson());
            File.WriteAllText(stem + ".json", saved);
            File.WriteAllText(stem + ".edited.svg", editedSvg);
            File.WriteAllBytes(stem + ".edited.png", edited.ToPng());
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
        }
        var words = string.Join(" ", observed.GetProperty("words").EnumerateArray());
        foreach (var word in new[] { "Review", "Archive", "Save", "Stored", "Retain source facts" }) Assert.Contains(word, words);
        Assert.Contains("Approved", string.Join(" ", observed.GetProperty("editedWords").EnumerateArray()));
        Assert.Equal(1, observed.GetProperty("editedMasks").GetInt32());
        Assert.Equal(original.ToPng(), masked.ToPng());
        Assert.Empty(observed.GetProperty("escaped").EnumerateArray());
        Assert.All(observed.GetProperty("references").EnumerateArray(), item => {
            Assert.True(item.GetProperty("resolves").GetBoolean());
            Assert.Equal("alpha", item.GetProperty("type").GetString());
        });
        Assert.Single(observed.GetProperty("references").EnumerateArray());
        Assert.True(observed.GetProperty("nativeText").GetInt32() > 0);
        Assert.Equal(0, observed.GetProperty("bakedText").GetInt32());
        Assert.Equal(0, observed.GetProperty("bakedMasks").GetInt32());
        Assert.Equal(1, observed.GetProperty("bakedImages").GetInt32());
        Assert.Single(observed.GetProperty("links").EnumerateArray());
        var link = session.Page.Locator("#native a[href='#guide']");
        await link.FocusAsync();
        Assert.True(await link.EvaluateAsync<bool>("e=>document.activeElement===e"));
        await link.PressAsync("Enter");
        Assert.Equal("#guide", await session.Page.EvaluateAsync<string>("location.hash"));
        Assert.True(observed.GetProperty("alpha").GetInt32() > 0);
        Assert.True(observed.GetProperty("alpha").GetInt32() < 123);
        Assert.Equal(0, observed.GetProperty("scripts").GetInt32());
        Assert.False(observed.GetProperty("overflow").GetBoolean());
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
