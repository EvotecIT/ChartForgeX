using System.Text.Json;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidC4PresentationDepthTests {
    [BrowserTheory]
    [InlineData(false,390)] [InlineData(true,390)] [InlineData(false,1100)] [InlineData(true,1100)]
    [Trait("BrowserSuite","Compatibility")]
    public async Task NativeC4SourcePresentationKeepsCaptionsSurfacesHierarchyAndKeyboardScrolling(bool dark,int width) {
        if(!InteractiveChartBrowser.Enabled)return;
        foreach(var name in new[]{"c4-style-shapes-native","c4-layout-config-native"}) {
            var source=Fixture(name);var options=Options(dark,width);var direct=MermaidRenderer.Render(source,options);Assert.False(direct.HasErrors);
            var fenced=new MermaidVisualMarkupParser(options).Parse("```mermaid\n"+source+"\n```");Assert.False(fenced.HasErrors);
            foreach(var pair in new[]{("direct",direct.Artifact!),("fenced",Assert.Single(fenced.Artifacts))}) {
                var artifact=pair.Item2;var stem=name+"-"+pair.Item1+"-"+width+(dark?"-dark":"-light");Capture(artifact,stem);
                await using var session=await BrowserProofFonts.OpenAsync(artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing=VisualArtifactHtmlSizing.PreserveSize }),width,850);
                var host=session.Page.Locator("[data-cfx-html-sizing='preserve-size']");var output=Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
                var raw=await session.Page.EvaluateAsync<string>("""
                    () => {
                        const svg=document.querySelector('svg'),frame=svg.getBoundingClientRect(),clipped=[];
                        for(const text of svg.querySelectorAll('text')){const box=text.getBoundingClientRect();if(box.width&&box.height&&text.textContent.trim()&&(box.left<frame.left-1.5||box.top<frame.top-1.5||box.right>frame.right+1.5||box.bottom>frame.bottom+1.5))clipped.push(text.textContent)}
                        const host=document.querySelector('[data-cfx-html-sizing=preserve-size]');
                        return JSON.stringify({text:svg.textContent,clipped,scripts:svg.querySelectorAll('script,image,foreignObject').length,outerOverflow:document.documentElement.scrollWidth>innerWidth,
                            scrollable:host.scrollWidth>host.clientWidth,shapes:[...svg.querySelectorAll('[data-node-shape]')].map(node=>node.getAttribute('data-node-shape')),
                            groups:[...svg.querySelectorAll('[data-cfx-role="topology-group"]')].map(node=>node.getAttribute('data-group-id'))});
                    }
                    """);
                if(!string.IsNullOrWhiteSpace(output)){Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,stem+".observed.json"),raw);await session.Page.ScreenshotAsync(new PageScreenshotOptions {Path=Path.Combine(output,stem+".browser.png"),FullPage=true});await host.EvaluateAsync("node=>node.scrollLeft=node.scrollWidth");await session.Page.ScreenshotAsync(new PageScreenshotOptions {Path=Path.Combine(output,stem+".scrolled.browser.png"),FullPage=true});await host.EvaluateAsync("node=>node.scrollLeft=0");}
                using var document=JsonDocument.Parse(raw);var observed=document.RootElement;
                var expected=name=="c4-style-shapes-native"?new[]{"Platform","Application","Store","Queue","User","Application tier","Data tier"}:new[]{"Cloud","West","East","Alpha","Bravo","Charlie","Delta","Operator"};
                foreach(var caption in expected)Assert.Contains(caption,observed.GetProperty("text").GetString());
                Assert.Empty(observed.GetProperty("clipped").EnumerateArray());Assert.Equal(0,observed.GetProperty("scripts").GetInt32());Assert.False(observed.GetProperty("outerOverflow").GetBoolean());
                if(observed.GetProperty("scrollable").GetBoolean()){await host.FocusAsync();await session.Page.Keyboard.PressAsync("ArrowRight");await session.Page.WaitForFunctionAsync("()=>document.querySelector('[data-cfx-html-sizing=preserve-size]').scrollLeft>0");Assert.True(await host.EvaluateAsync<double>("node=>node.scrollLeft")>0);}
                if(name=="c4-style-shapes-native")foreach(var shape in new[]{"Actor","Cylinder","HorizontalCylinder","Subroutine"})Assert.Contains(observed.GetProperty("shapes").EnumerateArray(),item=>item.GetString()==shape);
                InteractiveChartBrowser.AssertNoConsoleErrors(session);
            }
        }
    }
}
