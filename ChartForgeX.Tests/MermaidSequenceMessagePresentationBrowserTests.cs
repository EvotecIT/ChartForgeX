using System.Text.Json;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite","Compatibility")]
public sealed class MermaidSequenceMessagePresentationBrowserTests {
    [BrowserTheory]
    [InlineData(false,390)] [InlineData(true,390)] [InlineData(false,1100)] [InlineData(true,1100)]
    public async Task MeasuredBadgesAndAlignedCaptionsRemainVisibleInDirectAndFenceHosts(bool dark,int width) {
        var source=SequenceMessageSourceFixture.Canonical();var options=new MermaidRenderOptions{WarningPolicy=MermaidWarningPolicy.Reject,
            Presentation=new MermaidPresentationOptions{ThemeMode=dark?VisualThemeMode.Dark:VisualThemeMode.Light,FontFamily=BrowserProofFonts.Family},
            Sequence=new MermaidSequenceRenderOptions{Width=width-32,Height=820}};
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
                    badges:[...svg.querySelectorAll('[data-cfx-role="sequence-message"]')].map(group=>{
                      const circle=group.querySelector('[data-cfx-role="sequence-number-badge"]'),number=group.querySelector('[data-cfx-role="sequence-number"] text');if(!circle||!number)return null;
                      const b=number.getBBox(),cx=circle.cx.baseVal.value,cy=circle.cy.baseVal.value,r=circle.rx.baseVal.value;
                      const inside=[b.x,b.x+b.width].every(x=>[b.y,b.y+b.height].every(y=>(x-cx)**2+(y-cy)**2<=r*r));
                      const overlap=[...group.querySelectorAll('[data-cfx-role="sequence-label"] text')].some(text=>{const q=text.getBBox();return q.width&&q.height&&q.x<cx+r&&q.x+q.width>cx-r&&q.y<cy+r&&q.y+q.height>cy-r});
                      return {text:number.textContent,size:parseFloat(getComputedStyle(number).fontSize),ink:getComputedStyle(number).fill,fill:getComputedStyle(circle).fill,inside,captionOverlap:overlap};}).filter(Boolean),
                    participants:svg.querySelectorAll('[data-cfx-role="sequence-participant"]').length,footers:[...svg.querySelectorAll('[data-cfx-role="sequence-participant-footer"]')].map(node=>({source:node.getAttribute("data-source-id"),text:node.textContent})),messages:svg.querySelectorAll('[data-cfx-role="sequence-message"]').length,
                    activationWidths:[...svg.querySelectorAll('[data-cfx-role="sequence-activation-bar"]')].map(x=>x.width.baseVal.value),
                    nativeScripts:svg.querySelectorAll('script,foreignObject').length,pageOverflow:document.documentElement.scrollWidth>innerWidth,
                    host:{width:host.clientWidth,contentWidth:host.scrollWidth},frame:{width:frame.width,height:frame.height}};
                }
                """);
            var output=Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");var stem="sequence-message-presentation-"+surface+"-"+width+(dark?"-dark":"-light");
            if(!string.IsNullOrWhiteSpace(output)) {
                Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,stem+".mmd"),source);File.WriteAllText(Path.Combine(output,stem+".svg"),artifact.ToSvg());
                File.WriteAllBytes(Path.Combine(output,stem+".native.png"),artifact.ToPng());File.WriteAllText(Path.Combine(output,stem+".native.json"),artifact.ToInterchangeJson());
                File.WriteAllText(Path.Combine(output,stem+".observed.json"),observed.GetRawText());
                await page.ScreenshotAsync(new PageScreenshotOptions{Path=Path.Combine(output,stem+".browser.png"),FullPage=true});
            }
            Assert.Equal(2,observed.GetProperty("participants").GetInt32());Assert.Equal(3,observed.GetProperty("messages").GetInt32());Assert.Empty(observed.GetProperty("footers").EnumerateArray()); // The canonical source explicitly sets mirrorActors:false.
            Assert.Equal(new[]{"1000","1001","1002"},observed.GetProperty("badges").EnumerateArray().Select(item=>item.GetProperty("text").GetString()));
            Assert.All(observed.GetProperty("badges").EnumerateArray(),item=>{Assert.True(item.GetProperty("inside").GetBoolean());Assert.False(item.GetProperty("captionOverlap").GetBoolean());Assert.Equal(9,item.GetProperty("size").GetDouble());Assert.Equal("rgb(255, 255, 255)",item.GetProperty("ink").GetString());Assert.Equal("rgb(59, 92, 204)",item.GetProperty("fill").GetString());});
            Assert.Empty(observed.GetProperty("clip").EnumerateArray());Assert.Equal(0,observed.GetProperty("nativeScripts").GetInt32());Assert.False(observed.GetProperty("pageOverflow").GetBoolean());
            AssertRole("sequence-participant","Review",14,"400");AssertRole("sequence-message","Save",12,"400");AssertRole("sequence-note","Review",12,"400");
            Assert.All(observed.GetProperty("activationWidths").EnumerateArray(),item=>Assert.Equal(10,item.GetDouble()));
            var host=page.Locator("[data-cfx-html-sizing='preserve-size']");
            if(observed.GetProperty("host").GetProperty("contentWidth").GetInt32()>observed.GetProperty("host").GetProperty("width").GetInt32()) {
                await host.FocusAsync();await page.Keyboard.PressAsync("ArrowRight",new KeyboardPressOptions{Delay=100});
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
