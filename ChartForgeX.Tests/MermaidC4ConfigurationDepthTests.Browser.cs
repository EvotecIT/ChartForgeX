using System.Text.Json;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidC4ConfigurationDepthTests {
    [BrowserTheory]
    [Trait("BrowserSuite","Compatibility")]
    [InlineData(false,390)] [InlineData(true,390)] [InlineData(false,1100)] [InlineData(true,1100)]
    public async Task ConfiguredC4FontsWrappingPaletteAndNestedFramesReachActualBrowserPaint(bool dark,int width) {
        if(!InteractiveChartBrowser.Enabled)return;
        _=BrowserProofFonts.Family;var source=Fixture();var options=Options(dark,width);
        var direct=MermaidRenderer.Render(source,options);Assert.False(direct.HasErrors);
        var fence=new MermaidVisualMarkupParser(options).Parse("```mermaid\n"+source+"\n```");Assert.False(fence.HasErrors);
        foreach(var item in new[]{("direct",direct.Artifact!),("fence",Assert.Single(fence.Artifacts))}) {
            var name="c4-config-"+item.Item1+"-"+width+(dark?"-dark":"-light");Capture(item.Item2,name);
            await using var session=await BrowserProofFonts.OpenAsync(item.Item2.ToHtmlPage(new VisualArtifactRenderOptions {HtmlSizing=VisualArtifactHtmlSizing.PreserveSize}),width,900);
            var raw=await session.Page.EvaluateAsync<string>("""
                () => {
                    const svg=document.querySelector('svg'),frame=svg.getBoundingClientRect(),clipped=[],fonts=[];
                    for(const text of svg.querySelectorAll('text')) {
                        const box=text.getBoundingClientRect(),style=getComputedStyle(text);
                        if(!text.textContent.trim())continue;
                        if(box.left<frame.left-1.5||box.top<frame.top-1.5||box.right>frame.right+1.5||box.bottom>frame.bottom+1.5)clipped.push(text.textContent);
                        fonts.push({role:text.closest('[data-cfx-role]')?.getAttribute('data-cfx-role'),text:text.textContent,family:style.fontFamily,size:style.fontSize,weight:style.fontWeight,width:box.width,height:box.height});
                    }
                    const host=document.querySelector('[data-cfx-html-sizing=preserve-size]');
                    return JSON.stringify({text:svg.textContent,clipped,fonts,frame:{width:frame.width,height:frame.height},scripts:svg.querySelectorAll('script,image,foreignObject').length,
                        outerOverflow:document.documentElement.scrollWidth>innerWidth,scrollable:host.scrollWidth>host.clientWidth,
                        groups:[...svg.querySelectorAll('[data-cfx-role=topology-group]')].length});
                }
                """);
            var output=Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
            if(!string.IsNullOrWhiteSpace(output)) {
                if(!Path.IsPathRooted(output))throw new InvalidOperationException("Capture root must be absolute.");
                Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,name+".observed.json"),raw);
                await session.Page.ScreenshotAsync(new PageScreenshotOptions {Path=Path.Combine(output,name+".browser.png"),FullPage=true});
                await session.Page.Locator("[data-cfx-html-sizing=preserve-size]").EvaluateAsync("node=>node.scrollLeft=node.scrollWidth");
                await session.Page.ScreenshotAsync(new PageScreenshotOptions {Path=Path.Combine(output,name+".scrolled.browser.png"),FullPage=true});
            }
            using var json=JsonDocument.Parse(raw);var observed=json.RootElement;
            Assert.Empty(observed.GetProperty("clipped").EnumerateArray());Assert.Equal(0,observed.GetProperty("scripts").GetInt32());Assert.False(observed.GetProperty("outerOverflow").GetBoolean());
            Assert.True(observed.GetProperty("groups").GetInt32()>0);
            foreach(var word in new[]{"Service platform","Operations","coordinator","Publication","orchestration","service","External","delivery","validated","documents"})Assert.Contains(word,observed.GetProperty("text").GetString());
            var text=observed.GetProperty("fonts").EnumerateArray().Where(font=>font.GetProperty("role").GetString() is "topology-node-label" or "topology-node-subtitle" or "topology-group-label" or "topology-edge-label-text").ToArray();
            Assert.NotEmpty(text);Assert.All(text,font=>Assert.Contains(BrowserProofFonts.Family,font.GetProperty("family").GetString()));
            InteractiveChartBrowser.AssertNoConsoleErrors(session);
        }
    }
}
