using ChartForgeX.VisualArtifacts;
using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualBlocks;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class MermaidIshikawaDepthBrowserTests {
    [BrowserTheory]
    [InlineData(false, 390)] [InlineData(true, 390)] [InlineData(false, 1100)] [InlineData(true, 1100)]
    public async Task CompleteCauseCaptionsKeepNativeBoundsAndNaturalScrollingAcrossHostResize(bool dark, int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        var result = MermaidRenderer.Render(MermaidIshikawaDepthTests.Fixture(dark), MermaidIshikawaDepthTests.Options(dark, width, BrowserProofFonts.Family)); Assert.Empty(result.Diagnostics);
        var artifact = result.Artifact!; var block = (FishboneDiagramBlock)artifact.Model!; var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var html = "<style>body{margin:0;padding:12px;box-sizing:border-box;background:" + (dark ? "#171b23" : "#fff")
            + "}section{max-width:100%;overflow:auto}svg,img{display:block;max-width:none!important}</style><section id='native' tabindex='0'>" + svg
            + "</section><section><img id='pixels' src='data:image/png;base64," + Convert.ToBase64String(png) + "'></section>";
        await using var session = await BrowserProofFonts.OpenAsync(html, width + 24, 1000); var page = session.Page;
        var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await page.EvaluateAsync("async()=>{await document.fonts.ready;await document.querySelector('#pixels').decode()}");
        var first = await Observe(page); await Capture(page, svg, png, first, "ishikawa-browser-" + width + (dark ? "-dark" : "-light"));
        Assert.Equal(28, first.GetProperty("causes").GetArrayLength()); Assert.Empty(first.GetProperty("clipped").EnumerateArray()); Assert.Empty(first.GetProperty("overlaps").EnumerateArray()); Assert.Empty(first.GetProperty("hidden").EnumerateArray());
        Assert.Equal(block.Options.Size.Width, first.GetProperty("pngWidth").GetInt32()); Assert.Equal(block.Options.Size.Height, first.GetProperty("pngHeight").GetInt32());
        Assert.Equal(0, first.GetProperty("activeElements").GetInt32()); Assert.True(first.GetProperty("fitsDocument").GetBoolean());
        var expected = FishboneNaturalLayoutTests.Facts(block);
        var actual = first.GetProperty("causes").EnumerateArray().ToArray();
        for (var index = 0; index < expected.Count; index++) {
            Assert.Equal(expected[index].Id, actual[index].GetProperty("id").GetString()); Assert.Equal(expected[index].Parent, actual[index].GetProperty("parent").GetString()); Assert.Equal(expected[index].Text, actual[index].GetProperty("text").GetString());
        }
        if (width == 390) Assert.True(first.GetProperty("scrolls").GetBoolean());
        foreach (var hostWidth in new[] { width == 390 ? 1124 : 414, width + 24 }) {
            await page.SetViewportSizeAsync(hostWidth, 1000); var resized = await Observe(page);
            await Capture(page, svg, png, resized, "ishikawa-browser-" + width + (dark ? "-dark" : "-light") + "-resized-" + hostWidth);
            Assert.Equal(first.GetProperty("causes").GetRawText(), resized.GetProperty("causes").GetRawText()); Assert.True(resized.GetProperty("fitsDocument").GetBoolean()); Assert.Empty(resized.GetProperty("clipped").EnumerateArray());
        }
        await page.Locator("#native").FocusAsync();
        await page.Locator("#native").EvaluateAsync("element=>{element.scrollLeft=element.scrollWidth}");
        var scrolled = await Observe(page); if (width == 390) Assert.True(scrolled.GetProperty("scrollLeft").GetDouble() > 0);
        Assert.True(scrolled.GetProperty("effectInsideHost").GetBoolean());
        await Capture(page, svg, png, await Observe(page), "ishikawa-browser-" + width + (dark ? "-dark" : "-light") + "-scrolled");
        Assert.Empty(errors); InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
    private static Task<JsonElement> Observe(IPage page) => page.EvaluateAsync<JsonElement>("""
        () => {
          const svg=document.querySelector('#native svg'),frame=svg.getBoundingClientRect(),clipped=[],overlaps=[];
          const text=[...svg.querySelectorAll('text')].filter(t=>t.textContent.trim()&&getComputedStyle(t).display!=='none');
          for(const t of text){const b=t.getBoundingClientRect();if(b.width<=0||b.height<=0||b.left<frame.left-.5||b.top<frame.top-.5||b.right>frame.right+.5||b.bottom>frame.bottom+.5)clipped.push(t.textContent)}
          for(let i=0;i<text.length;i++)for(let j=i+1;j<text.length;j++){const a=text[i].getBoundingClientRect(),b=text[j].getBoundingClientRect();if(Math.min(a.right,b.right)-Math.max(a.left,b.left)>.01&&Math.min(a.bottom,b.bottom)-Math.max(a.top,b.top)>.01)overlaps.push([text[i].textContent,text[j].textContent])}
          const host=document.querySelector('#native'),png=document.querySelector('#pixels');
          return {causes:[...svg.querySelectorAll('[data-cfx-role=fishbone-cause]')].map(g=>({id:g.dataset.causeId,parent:g.getAttribute('data-parent-id'),text:[...g.querySelectorAll('[data-cfx-role=fishbone-cause-caption] text')].map(t=>t.textContent).join(' ')})),
            clipped,overlaps,hidden:[...svg.querySelectorAll("text")].filter(t=>getComputedStyle(t).display==='none'||t.closest('[display="none"]')).map(t=>t.textContent),activeElements:svg.querySelectorAll('script,image,foreignObject,button,[tabindex]').length,pngWidth:png.naturalWidth,pngHeight:png.naturalHeight,
            scrolls:host.scrollWidth>host.clientWidth,scrollLeft:host.scrollLeft,effectInsideHost:svg.querySelector('[data-cfx-role=fishbone-effect]').getBoundingClientRect().right<=host.getBoundingClientRect().right+.5,fitsDocument:document.documentElement.scrollWidth<=innerWidth};
        }
        """);
    private static async Task Capture(IPage page, string svg, byte[] png, JsonElement observed, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        if (!Path.IsPathRooted(output)) throw new InvalidOperationException("Captures require an absolute task-owned directory.");
        Directory.CreateDirectory(output); var path = Path.Combine(output, name);
        File.WriteAllText(path + ".json", observed.GetRawText()); File.WriteAllText(path + ".svg", svg); File.WriteAllBytes(path + ".native.png", png);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = path + ".browser.png", FullPage = true });
    }
}
