using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite","Compatibility")]
public sealed class MermaidGraphStyleSettingsBrowserTests {
    [BrowserTheory]
    [InlineData(false,390)][InlineData(true,390)][InlineData(false,1100)][InlineData(true,1100)]
    public async Task FamilyStyleFixturesRemainStaticAndKeepCaptionsInTheirViewport(bool dark,int width) {
        if(!InteractiveChartBrowser.Enabled)return;
        foreach(var family in new[]{"flowchart","agentflow"}) {
            var source=MermaidGraphStyleSettingsRenderingTests.Fixture(family);
            // Theme remains a host option so the frontmatter stays the fixture's first statement.
            var rendered=MermaidRenderer.Render(source,new MermaidRenderOptions{Presentation=new MermaidPresentationOptions{ThemeMode=dark?VisualThemeMode.Dark:VisualThemeMode.Light}});
            MermaidGraphStyleSettingsCausalTests.Capture(rendered,family+"-browser-source-"+width+(dark?"-dark":"-light"));Assert.Empty(rendered.Diagnostics);
            var artifact=rendered.Artifact!;
            await using var session=await BrowserProofFonts.OpenAsync(artifact.ToHtmlPage(new VisualArtifactRenderOptions{HtmlSizing=VisualArtifactHtmlSizing.PreserveSize}),width,700);
            var observed=await session.Page.EvaluateAsync<string>("""
                () => {
                    const svg=document.querySelector('svg'),frame=svg.getBoundingClientRect(),clipped=[];
                    for(const text of svg.querySelectorAll('text')){const b=text.getBoundingClientRect();if(b.width&&b.height&&text.textContent.trim()&&(b.left<frame.left-1.5||b.top<frame.top-1.5||b.right>frame.right+1.5||b.bottom>frame.bottom+1.5))clipped.push(text.textContent);}
                    const host=document.querySelector('[data-cfx-html-sizing=preserve-size]');
                    return JSON.stringify({clipped,text:svg.textContent,nodes:svg.querySelectorAll('[data-cfx-role="topology-node"]').length,
                        groups:svg.querySelectorAll('[data-cfx-role="topology-group"]').length,edges:svg.querySelectorAll('[data-cfx-role="topology-edge"]').length,
                        unsafe:svg.querySelectorAll('script,image,foreignObject').length,outerOverflow:document.documentElement.scrollWidth>innerWidth,scrollable:host.scrollWidth>host.clientWidth});
                }
                """);
            var output=Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
            if(!string.IsNullOrWhiteSpace(output)) {
                Directory.CreateDirectory(output);var stem=Path.Combine(output,family+"-style-settings-"+width+(dark?"-dark":"-light"));
                File.WriteAllText(stem+".observed.json",observed);File.WriteAllText(stem+".svg",artifact.ToSvg());File.WriteAllBytes(stem+".native.png",artifact.ToPng());
                await session.Page.ScreenshotAsync(new PageScreenshotOptions{Path=stem+".browser.png",FullPage=true});
            }
            using var data=JsonDocument.Parse(observed);var root=data.RootElement;
            Assert.Equal(2,root.GetProperty("nodes").GetInt32());Assert.Equal(1,root.GetProperty("groups").GetInt32());Assert.Equal(1,root.GetProperty("edges").GetInt32());
            foreach(var label in new[]{"Review","Work","Done","Reads"})Assert.Contains(label,root.GetProperty("text").GetString());
            Assert.Empty(root.GetProperty("clipped").EnumerateArray());Assert.Equal(0,root.GetProperty("unsafe").GetInt32());Assert.False(root.GetProperty("outerOverflow").GetBoolean());
            if(root.GetProperty("scrollable").GetBoolean()) {
                await session.Page.Locator("[data-cfx-html-sizing=preserve-size]").FocusAsync();await session.Page.Keyboard.PressAsync("ArrowRight");
                await session.Page.WaitForFunctionAsync("()=>document.querySelector('[data-cfx-html-sizing=preserve-size]').scrollLeft>0");
            }
            InteractiveChartBrowser.AssertNoConsoleErrors(session);
        }
    }
}
