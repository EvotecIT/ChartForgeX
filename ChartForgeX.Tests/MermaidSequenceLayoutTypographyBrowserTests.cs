using System.Text.Json;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite","Compatibility")]
public sealed class MermaidSequenceLayoutTypographyBrowserTests {
    [BrowserTheory]
    [InlineData(false,390)] [InlineData(true,390)] [InlineData(false,1100)] [InlineData(true,1100)]
    public async Task IndependentRoleTypographyAndNativeSpacingStayVisibleInDirectAndFenceHosts(bool dark,int width) {
        var source=SequenceSourcePolicyFixture.Canonical();var options=new MermaidRenderOptions{WarningPolicy=MermaidWarningPolicy.Reject,
            Presentation=new MermaidPresentationOptions{ThemeMode=dark?VisualThemeMode.Dark:VisualThemeMode.Light,FontFamily=BrowserProofFonts.Family},
            Sequence=new MermaidSequenceRenderOptions{Width=width-32,Height=620}};
        var direct=MermaidRenderer.Render(source,options);Assert.Empty(direct.Diagnostics);
        var fence=new MermaidVisualMarkupParser(options).Parse("```mermaid\n"+source+"\n```");Assert.Empty(fence.Diagnostics);
        await Observe(direct.Artifact!,"direct");await Observe(Assert.Single(fence.Artifacts),"fence");
        async Task Observe(VisualArtifact artifact,string surface) {
            var html=artifact.ToHtmlPage(new VisualArtifactRenderOptions{HtmlSizing=VisualArtifactHtmlSizing.PreserveSize});
            await using var session=await BrowserProofFonts.OpenAsync(html,width,900);var page=session.Page;
            var observed=await page.EvaluateAsync<JsonElement>("""
                () => {
                  const svg=document.querySelector('svg'),frame=svg.getBoundingClientRect(),host=document.querySelector('[data-cfx-html-sizing="preserve-size"]');
                  const text=[...svg.querySelectorAll('text')].map(node=>{const s=getComputedStyle(node),b=node.getBoundingClientRect();return {text:node.textContent,
                    roles:[...function*(e){while(e){const r=e.getAttribute&&e.getAttribute('data-cfx-role');if(r)yield r;e=e.parentElement}}(node)],
                    size:parseFloat(s.fontSize),weight:s.fontWeight,family:s.fontFamily,box:{left:b.left,top:b.top,right:b.right,bottom:b.bottom,width:b.width,height:b.height}}});
                  return {text,clip:text.filter(x=>x.box.width&&x.box.height&&(x.box.left<frame.left-1.5||x.box.top<frame.top-1.5||x.box.right>frame.right+1.5||x.box.bottom>frame.bottom+1.5)).map(x=>x.text),
                    participants:svg.querySelectorAll('[data-cfx-role="sequence-participant"]').length,messages:svg.querySelectorAll('[data-cfx-role="sequence-message"]').length,
                    activationWidths:[...svg.querySelectorAll('[data-cfx-role="sequence-activation-bar"]')].map(x=>x.width.baseVal.value),
                    nativeScripts:svg.querySelectorAll('script,foreignObject').length,pageOverflow:document.documentElement.scrollWidth>innerWidth,
                    host:{width:host.clientWidth,contentWidth:host.scrollWidth},frame:{width:frame.width,height:frame.height}};
                }
                """);
            var output=Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");var stem="sequence-layout-"+surface+"-"+width+(dark?"-dark":"-light");
            if(!string.IsNullOrWhiteSpace(output)) {
                Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,stem+".mmd"),source);File.WriteAllText(Path.Combine(output,stem+".svg"),artifact.ToSvg());
                File.WriteAllBytes(Path.Combine(output,stem+".native.png"),artifact.ToPng());File.WriteAllText(Path.Combine(output,stem+".native.json"),artifact.ToInterchangeJson());
                File.WriteAllText(Path.Combine(output,stem+".observed.json"),observed.GetRawText());
                await page.ScreenshotAsync(new PageScreenshotOptions{Path=Path.Combine(output,stem+".browser.png"),FullPage=true});
            }
            Assert.Equal(2,observed.GetProperty("participants").GetInt32());Assert.Equal(2,observed.GetProperty("messages").GetInt32());
            Assert.Empty(observed.GetProperty("clip").EnumerateArray());Assert.Equal(0,observed.GetProperty("nativeScripts").GetInt32());Assert.False(observed.GetProperty("pageOverflow").GetBoolean());
            AssertRole("sequence-participant","Review",18,"700");AssertRole("sequence-message","Save",20,"500");AssertRole("sequence-note","Note",16,"400");
            Assert.All(observed.GetProperty("activationWidths").EnumerateArray(),item=>Assert.Equal(14,item.GetDouble()));
            var host=page.Locator("[data-cfx-html-sizing='preserve-size']");
            if(observed.GetProperty("host").GetProperty("contentWidth").GetInt32()>observed.GetProperty("host").GetProperty("width").GetInt32()) {
                await host.FocusAsync();await page.Keyboard.PressAsync("ArrowRight");
                await page.WaitForFunctionAsync("""() => document.querySelector('[data-cfx-html-sizing="preserve-size"]').scrollLeft > 0""");
                Assert.True(await host.EvaluateAsync<double>("element=>element.scrollLeft")>0);
                if(!string.IsNullOrWhiteSpace(output))await page.ScreenshotAsync(new PageScreenshotOptions{Path=Path.Combine(output,stem+".scrolled.browser.png"),FullPage=true});
            }
            InteractiveChartBrowser.AssertNoConsoleErrors(session);
            void AssertRole(string role,string caption,double size,string weight) {
                var actual=observed.GetProperty("text").EnumerateArray().First(item=>item.GetProperty("text").GetString()!.Contains(caption,StringComparison.Ordinal)
                    &&item.GetProperty("roles").EnumerateArray().Any(value=>value.GetString()==role));
                Assert.Equal(size,actual.GetProperty("size").GetDouble());Assert.Equal(weight,actual.GetProperty("weight").GetString());Assert.Contains(BrowserProofFonts.Family,actual.GetProperty("family").GetString());
            }
        }
    }
}
