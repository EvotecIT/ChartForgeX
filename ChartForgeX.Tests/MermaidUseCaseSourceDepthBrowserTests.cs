using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class MermaidUseCaseSourceDepthBrowserTests {
    [BrowserTheory]
    [InlineData(390, false)] [InlineData(390, true)] [InlineData(900, false)] [InlineData(900, true)]
    public async Task WholeUseCaseSourceShowsActualStereotypesNoteAndNamedAssociationPaint(int width, bool dark) {
        if (!InteractiveChartBrowser.Enabled) return;
        var source = UseCaseSourceDepthFixture.Read(width == 390);
        var result = MermaidRenderer.Render(source, MermaidUseCaseSourceDepthTests.Options(width, dark));
        var artifact = result.Artifact!;
        await using var session = await BrowserProofFonts.OpenAsync(artifact.ToHtmlPage(new VisualArtifactRenderOptions {
            HtmlSizing = VisualArtifactHtmlSizing.PreserveSize }), width + 24, 900);
        var errors = new List<string>(); session.Page.PageError += (_, message) => errors.Add(message);
        var observed = await session.Page.EvaluateAsync<JsonElement>("""
            () => {
              const svg=document.querySelector('svg'),frame=svg.getBoundingClientRect();
              const texts=[...svg.querySelectorAll('text')];
              const actor=svg.querySelector('[data-cfx-role=topology-node][data-node-id=Operator]');
              const request=svg.querySelector('[data-cfx-role=topology-edge][data-edge-id=request]');
              const glyphPath=use=>document.getElementById((use.getAttribute('href')||'').slice(1));
              const ink=[...svg.querySelectorAll('[data-cfx-native-text-ink] use')].filter(use=>glyphPath(use)?.getAttribute('d')?.trim());
              const bounds=node=>{const b=node.getBoundingClientRect();return {left:b.left,top:b.top,right:b.right,bottom:b.bottom,width:b.width,height:b.height}};
              const clipped=[...texts.filter(text=>text.textContent.length>0),...ink].filter(node=>{const b=bounds(node);
                return b.left<frame.left-1||b.top<frame.top-1||b.right>frame.right+1||b.bottom>frame.bottom+1})
                .map(node=>({tag:node.localName,text:node.textContent,caption:node.closest('[data-cfx-caption]')?.dataset.cfxCaption,box:bounds(node)}));
              const actorCaptions=[...actor.querySelectorAll('[data-cfx-caption]')].map(caption=>{
                const uses=[...caption.querySelectorAll('[data-cfx-native-text-ink] use')],style=getComputedStyle(caption);
                return {role:caption.dataset.cfxRole,caption:caption.dataset.cfxCaption,box:bounds(caption),
                  glyphs:uses.length,unresolved:uses.filter(use=>!glyphPath(use)).length,
                  inkGlyphs:uses.filter(use=>glyphPath(use)?.getAttribute('d')?.trim()).length,
                  visible:style.display!=='none'&&style.visibility==='visible'&&Number(style.opacity)>0};});
              return {text:texts.map(text=>text.textContent).join(' '),clipped,
                actorText:[...actor.querySelectorAll('text')].map(text=>text.textContent).join(' '),
                actorCaptions,
                nodeCount:svg.querySelectorAll('[data-cfx-role=topology-node]').length,
                noteTarget:svg.querySelector('[data-cfx-role=topology-annotation]').getAttribute('data-target-id'),
                requestPaint:[...request.querySelectorAll('[data-cfx-role=topology-edge-line]')].map(path=>({stroke:path.getAttribute('stroke'),dash:path.getAttribute('stroke-dasharray')})),
                fonts:texts.map(text=>getComputedStyle(text).fontFamily),active:svg.querySelectorAll('script,image,foreignObject').length};
            }
            """);
        var visible = await session.Page.ScreenshotAsync(new PageScreenshotOptions { FullPage = true });
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        string? stem = null;
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output); stem = Path.Combine(output, "usecase-source-" + width + (dark ? "-dark" : "-light"));
            File.WriteAllBytes(stem + ".browser.png", visible);
            File.WriteAllText(stem + ".observed.json", JsonSerializer.Serialize(new { observed, errors, session.ConsoleLog }));
            File.WriteAllText(stem + ".source.mmd", source);
            File.WriteAllText(stem + ".svg", artifact.ToSvg()); File.WriteAllBytes(stem + ".native.png", artifact.ToPng());
            File.WriteAllText(stem + ".json", artifact.ToInterchangeJson());
        }
        var actorInk = new Dictionary<string, int>();
        foreach (var role in new[] { "topology-node-label", "topology-node-subtitle" }) {
            var caption = session.Page.Locator("[data-cfx-role=topology-node][data-node-id=Operator] [data-cfx-role=" + role + "]");
            await caption.EvaluateAsync("node=>node.style.visibility='hidden'");
            byte[] hidden;
            try { hidden = await session.Page.ScreenshotAsync(new PageScreenshotOptions { FullPage = true }); }
            finally { await caption.EvaluateAsync("node=>node.style.removeProperty('visibility')"); }
            actorInk[role] = DifferentPixels(visible, hidden);
            if (stem != null) File.WriteAllBytes(stem + "." + role + ".hidden.png", hidden);
        }
        if (stem != null) File.WriteAllText(stem + ".ink.json", JsonSerializer.Serialize(new { actorInk, errors, session.ConsoleLog }));

        Assert.Empty(result.Diagnostics);
        Assert.Equal(3, observed.GetProperty("nodeCount").GetInt32()); Assert.Equal("Publish", observed.GetProperty("noteTarget").GetString());
        var captions = observed.GetProperty("actorCaptions").EnumerateArray().ToArray(); Assert.Equal(2, captions.Length);
        Assert.Equal("Release coordinator", captions.Single(caption => caption.GetProperty("role").GetString() == "topology-node-label").GetProperty("caption").GetString());
        Assert.Equal("«primary»", captions.Single(caption => caption.GetProperty("role").GetString() == "topology-node-subtitle").GetProperty("caption").GetString());
        Assert.All(captions, caption => {
            Assert.True(caption.GetProperty("visible").GetBoolean()); Assert.Equal(0, caption.GetProperty("unresolved").GetInt32());
            Assert.True(caption.GetProperty("inkGlyphs").GetInt32() > 0);
            Assert.True(caption.GetProperty("box").GetProperty("width").GetDouble() > 0); Assert.True(caption.GetProperty("box").GetProperty("height").GetDouble() > 0);
        });
        Assert.All(actorInk, caption => Assert.True(caption.Value > 0, caption.Key + " must paint visible captured glyph ink."));
        foreach (var word in new[] { "business", "Approval", "delivery", "requests" }) Assert.Contains(word, observed.GetProperty("text").GetString());
        Assert.NotEmpty(observed.GetProperty("requestPaint").EnumerateArray());
        Assert.All(observed.GetProperty("requestPaint").EnumerateArray(), paint => { Assert.Equal(ChartColor.FromHex("#2563EB").WithOpacity(.8).ToCss(), paint.GetProperty("stroke").GetString()); Assert.NotNull(paint.GetProperty("dash").GetString()); });
        Assert.All(observed.GetProperty("fonts").EnumerateArray(), family => Assert.Contains(BrowserProofFonts.Family, family.GetString()));
        Assert.Empty(observed.GetProperty("clipped").EnumerateArray()); Assert.Equal(0, observed.GetProperty("active").GetInt32()); Assert.Empty(errors);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    private static int DifferentPixels(byte[] visible, byte[] hidden) {
        var on = PngReader.Decode(visible); var off = PngReader.Decode(hidden);
        if (on.Width != off.Width || on.Height != off.Height) return -1;
        return Enumerable.Range(0, on.Width * on.Height).Count(pixel =>
            Math.Abs(on.Pixels[pixel * 4] - off.Pixels[pixel * 4]) + Math.Abs(on.Pixels[pixel * 4 + 1] - off.Pixels[pixel * 4 + 1])
                + Math.Abs(on.Pixels[pixel * 4 + 2] - off.Pixels[pixel * 4 + 2]) > 24);
    }
}
