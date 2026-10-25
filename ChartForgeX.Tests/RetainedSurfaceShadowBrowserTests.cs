using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class RetainedSurfaceShadowBrowserTests {
    [BrowserTheory]
    [InlineData(390, false)] [InlineData(1100, false)]
    [InlineData(390, true)] [InlineData(1100, true)]
    public async Task BothProfilesRetainSingleLiveCaptionsSafeLinksAndActualShadowPixels(int width, bool dark) {
        var model = RetainedSurfaceFixture.Model(width, dark);
        var prepared = model.Prepare(RetainedSurfaceFixture.Context(model));
        foreach (var profile in new[] { VisualSvgEffectProfile.Baked, VisualSvgEffectProfile.NativeFilters }) {
            var svg = prepared.ToSvg(new VisualSvgOptions(idPrefix: "surface-browser", effectProfile: profile, responsive: true));
            var plate = RetainedSurfaceFixture.Plate(25, true).ToSvg(new VisualSvgOptions(idPrefix: "plate-browser", effectProfile: profile));
            var html = "<style>body{margin:0}#scene{width:100%}</style><main id='scene'>" + svg +
                "</main><div id='plate'>" + plate + "</div><canvas id='sample' width='32' height='32'></canvas>";
            await using var session = await BrowserProofFonts.OpenAsync(html, width, 1100);
            var observed = await session.Page.EvaluateAsync<JsonElement>("""
                async () => {
                  await document.fonts.ready;
                  const svg=document.querySelector('#scene svg');
                  const texts=[...svg.querySelectorAll('text')];
                  const root=svg.getBoundingClientRect();
                  const bad=texts.filter(t=>{const b=t.getBoundingClientRect();return b.width&&b.height&&(b.left<root.left-1||b.top<root.top-1||b.right>root.right+1||b.bottom>root.bottom+1)}).map(t=>t.textContent);
                  const labelledEffects=texts.filter(t=>{for(let e=t;e&&e!==svg;e=e.parentElement)if(e.hasAttribute('filter'))return true;return false}).map(t=>t.textContent);
                  const filters=[...svg.querySelectorAll('[filter]')].map(e=>{const id=/#([^)'"\s]+)/.exec(e.getAttribute('filter'))?.[1];return {id,exists:!!document.getElementById(id)}});
                  const image=new Image();
                  image.src='data:image/svg+xml;base64,'+btoa(unescape(encodeURIComponent(document.querySelector('#plate svg').outerHTML)));
                  await image.decode();
                  const ctx=document.querySelector('#sample').getContext('2d');ctx.drawImage(image,0,0);
                  return {text:texts.map(t=>t.textContent),bad,labelledEffects,filters,images:svg.querySelectorAll('image').length,
                    uses:svg.querySelectorAll('use').length,links:[...svg.querySelectorAll('a')].map(a=>({href:a.getAttribute('href'),tab:a.getAttribute('tabindex')})),
                    source:[...ctx.getImageData(10,6,1,1).data],shadow:[...ctx.getImageData(18,8,1,1).data],
                    scripts:svg.querySelectorAll('script,foreignObject').length,overflow:document.documentElement.scrollWidth>innerWidth};
                }
                """);
            var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
            if (!string.IsNullOrWhiteSpace(output)) {
                Directory.CreateDirectory(output);
                var stem = Path.Combine(output, "retained-shadow-" + profile + "-" + width + "-" + dark);
                File.WriteAllText(stem + ".svg", svg);
                File.WriteAllBytes(stem + ".native.png", prepared.ToPng());
                File.WriteAllText(stem + ".native.json", prepared.ToArtifact(model.Id, VisualArtifactKind.Sequence).ToInterchangeJson());
                File.WriteAllText(stem + ".observed.json", observed.GetRawText());
                await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
            }
            Assert.Contains(observed.GetProperty("text").EnumerateArray(), item => item.GetString() == "Review");
            Assert.Contains(observed.GetProperty("text").EnumerateArray(), item => item.GetString() == "Archive");
            Assert.Contains(observed.GetProperty("text").EnumerateArray(), item => item.GetString() == "Retain facts");
            Assert.Empty(observed.GetProperty("bad").EnumerateArray());
            Assert.Empty(observed.GetProperty("labelledEffects").EnumerateArray());
            Assert.All(observed.GetProperty("filters").EnumerateArray(), item => Assert.True(item.GetProperty("exists").GetBoolean()));
            Assert.Equal(profile == VisualSvgEffectProfile.Baked, observed.GetProperty("images").GetInt32() > 0);
            Assert.Equal(0, observed.GetProperty("uses").GetInt32());
            var link = Assert.Single(observed.GetProperty("links").EnumerateArray());
            Assert.Equal("#guide", link.GetProperty("href").GetString());
            await session.Page.Locator("#scene a[href='#guide']").FocusAsync();
            Assert.True(await session.Page.Locator("#scene a[href='#guide']").EvaluateAsync<bool>("e=>document.activeElement===e"));
            Assert.Equal(25, observed.GetProperty("source")[3].GetInt32());
            Assert.True(observed.GetProperty("shadow")[3].GetInt32() > 0);
            Assert.True(observed.GetProperty("shadow")[3].GetInt32() < 25);
            Assert.Equal(0, observed.GetProperty("scripts").GetInt32());
            Assert.False(observed.GetProperty("overflow").GetBoolean());
            InteractiveChartBrowser.AssertNoConsoleErrors(session);
        }
    }
}
