using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class RetainedAppearanceBrowserTests {
    [BrowserTheory]
    [InlineData("compact", false)] [InlineData("wide", false)]
    [InlineData("compact", true)] [InlineData("wide", true)]
    public async Task OrderedRootFiltersKeepTheSingleLiveNativeSourceAndTerminalReplayKeepsTheExplicitBakedBoundary(string variant, bool dark) {
        var mode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
        var width = variant == "compact" ? 390 : 760;
        var source = NativeRetainedAppearanceExample.PrepareSource(variant, mode);
        var original = NativeRetainedAppearanceExample.Create(variant, mode);
        var saved = original.ToArtifact().ToInterchangeJson();
        var replayed = NativeRetainedAppearanceExample.RestoreRoot(saved, variant, mode);
        var edited = NativeRetainedAppearanceExample.RestoreRoot(saved, variant, mode, "Approved");
        var terminal = NativeRetainedAppearanceExample.Capture(variant, mode);
        var terminalSaved = terminal.ToArtifact().ToInterchangeJson();
        var terminalReplay = NativeRetainedAppearanceExample.RestoreLayer(terminalSaved, variant, mode);
        var native = replayed.ToSvg(new VisualSvgOptions("appearance-native", responsive: true,
            effectProfile: VisualSvgEffectProfile.NativeFilters));
        var sourceSvg = source.ToSvg(new VisualSvgOptions("appearance-source", responsive: true));
        var baked = replayed.ToSvg(new VisualSvgOptions("appearance-baked", responsive: true));
        var editedSvg = edited.ToSvg(new VisualSvgOptions("appearance-edited", responsive: true,
            effectProfile: VisualSvgEffectProfile.NativeFilters));
        var terminalSvg = terminalReplay.Visual.ToSvg(new VisualSvgOptions("appearance-terminal", responsive: true));
        var plate = RetainedAppearanceFixture.Wrap(RetainedAppearanceFixture.Source(), "outline");
        var plateSvg = plate.ToSvg(new VisualSvgOptions("appearance-plate", effectProfile: VisualSvgEffectProfile.NativeFilters));
        var background = ChartForgeX.Themes.VisualTheme.Graphite().Resolve(mode).Background.ToCss();
        var html = "<style>body{margin:0;background:" + background + "}section{max-width:100%}svg{display:block}</style>" +
            "<section id='native'>" + native + "</section><section id='source' hidden>" + sourceSvg + "</section>" +
            "<section id='edited'>" + editedSvg + "</section><section id='baked'>" + baked + "</section>" +
            "<section id='terminal'>" + terminalSvg + "</section><div id='plate'>" + plateSvg +
            "</div><canvas id='sample' width='8' height='6'></canvas><div id='guide'>Guide</div>";
        await using var session = await BrowserProofFonts.OpenAsync(html, width, 1200);
        var observed = await session.Page.EvaluateAsync<JsonElement>("""
            async () => {
              await document.fonts.ready;
              const native=document.querySelector('#native svg'), source=document.querySelector('#source svg');
              const words=root=>[...root.querySelectorAll('text')].map(node=>node.textContent);
              const frame=native.getBoundingClientRect();
              const escaped=[...native.querySelectorAll('text')].filter(node=>{
                const r=node.getBoundingClientRect();
                return r.width&&r.height&&(r.left<frame.left-1||r.right>frame.right+1||r.top<frame.top-1||r.bottom>frame.bottom+1);
              }).map(node=>node.textContent);
              const references=[...native.querySelectorAll('[filter]')].map(node=>{
                const match=/^url\(#([^()\s]+)\)$/.exec(node.getAttribute('filter'));
                const target=match?document.getElementById(match[1]):null;
                return {id:match?.[1],resolves:target?.localName==='filter',space:target?.getAttribute('color-interpolation-filters')};
              });
              const image=new Image();
              image.src='data:image/svg+xml;base64,'+btoa(unescape(encodeURIComponent(document.querySelector('#plate svg').outerHTML)));
              await image.decode();
              const context=document.querySelector('#sample').getContext('2d');context.drawImage(image,0,0);
              return {words:words(native),sourceWords:words(source),editedWords:words(document.querySelector('#edited svg')),
                escaped,references,filters:native.querySelectorAll('filter').length,
                links:native.querySelectorAll('a[href="#guide"]').length,sourceLinks:source.querySelectorAll('a[href="#guide"]').length,
                nativeImages:native.querySelectorAll('image').length,bakedText:document.querySelectorAll('#baked text').length,
                bakedImages:document.querySelectorAll('#baked image').length,terminalText:document.querySelectorAll('#terminal text').length,
                terminalImages:document.querySelectorAll('#terminal image').length,pixel:[...context.getImageData(1,3,1,1).data],
                scripts:native.querySelectorAll('script,foreignObject').length,overflow:document.documentElement.scrollWidth>innerWidth};
            }
            """);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output);
            var stem = Path.Combine(output, "retained-appearance-" + variant + "-" + mode);
            File.WriteAllText(stem + ".native.svg", native); File.WriteAllText(stem + ".baked.svg", baked);
            File.WriteAllText(stem + ".edited.svg", editedSvg); File.WriteAllText(stem + ".terminal.svg", terminalSvg);
            File.WriteAllText(stem + ".json", saved); File.WriteAllText(stem + ".terminal.json", terminalSaved);
            File.WriteAllText(stem + ".source.json", source.ToArtifact().ToInterchangeJson());
            File.WriteAllText(stem + ".observed.json", observed.GetRawText());
            File.WriteAllBytes(stem + ".native.png", replayed.ToPng());
            File.WriteAllBytes(stem + ".terminal.png", terminalReplay.Visual.ToPng());
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
        }
        Assert.Equal(original.ToPng(), replayed.ToPng());
        Assert.Equal(terminal.ToImage().Pixels, terminalReplay.ToImage().Pixels);
        Assert.Equal(observed.GetProperty("sourceWords").GetRawText(), observed.GetProperty("words").GetRawText());
        var allWords = string.Join(" ", observed.GetProperty("words").EnumerateArray());
        foreach (var word in new[] { "Review", "Archive", "Save", "Stored", "Facts stay editable" }) Assert.Contains(word, allWords);
        Assert.Contains("Approved", string.Join(" ", observed.GetProperty("editedWords").EnumerateArray()));
        Assert.Equal(3, observed.GetProperty("filters").GetInt32());
        Assert.Equal(3, observed.GetProperty("references").GetArrayLength());
        Assert.All(observed.GetProperty("references").EnumerateArray(), row => {
            Assert.True(row.GetProperty("resolves").GetBoolean());
            Assert.Equal("sRGB", row.GetProperty("space").GetString());
        });
        Assert.Equal(observed.GetProperty("sourceLinks").GetInt32(), observed.GetProperty("links").GetInt32());
        Assert.Equal(1, observed.GetProperty("links").GetInt32());
        Assert.Empty(observed.GetProperty("escaped").EnumerateArray());
        Assert.Equal(0, observed.GetProperty("nativeImages").GetInt32());
        Assert.Equal(0, observed.GetProperty("bakedText").GetInt32()); Assert.Equal(1, observed.GetProperty("bakedImages").GetInt32());
        Assert.Equal(0, observed.GetProperty("terminalText").GetInt32()); Assert.Equal(1, observed.GetProperty("terminalImages").GetInt32());
        var pixel = observed.GetProperty("pixel");
        Assert.True(pixel[2].GetInt32() > pixel[0].GetInt32()); Assert.True(pixel[3].GetInt32() > 0);
        Assert.Equal(0, observed.GetProperty("scripts").GetInt32()); Assert.False(observed.GetProperty("overflow").GetBoolean());
        var link = session.Page.Locator("#native a[href='#guide']");
        await link.FocusAsync(); Assert.True(await link.EvaluateAsync<bool>("node=>document.activeElement===node"));
        await link.PressAsync("Enter"); Assert.Equal("#guide", await session.Page.EvaluateAsync<string>("location.hash"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
