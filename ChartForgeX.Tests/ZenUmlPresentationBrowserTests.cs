using System.Text.Json;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ZenUmlPresentationBrowserTests {
    [BrowserTheory]
    [InlineData("zenuml-native-presentation",640,false)] [InlineData("zenuml-native-presentation",640,true)]
    [InlineData("zenuml-native-presentation",1100,false)] [InlineData("zenuml-native-presentation",1100,true)]
    [InlineData("zenuml-native-presentation-order",640,false)] [InlineData("zenuml-native-presentation-order",640,true)]
    [InlineData("zenuml-native-presentation-order",1100,false)] [InlineData("zenuml-native-presentation-order",1100,true)]
    public async Task NativeOrdinaryHtmlRetainsPresentationAndScrollableFullPaintWithoutScripts(string fixture,int width,bool dark) {
        var model=ZenUmlPresentationDepthTests.Model(fixture,width,dark);var artifact=model.ToVisualArtifact();
        await using var session=await BrowserProofFonts.OpenAsync(artifact.ToHtmlPage(new VisualArtifactRenderOptions{HtmlSizing=VisualArtifactHtmlSizing.PreserveSize}),width,980);
        var observed=await session.Page.EvaluateAsync<JsonElement>("""
            ()=>{
                const svg=document.querySelector('svg'),frame=svg.getBoundingClientRect();
                return {reference:svg.querySelectorAll('[data-cfx-role="sequence-reference"]').length,
                    divider:svg.querySelectorAll('[data-cfx-role="sequence-divider"]').length,
                    text:[...svg.querySelectorAll('text')].map(t=>t.textContent).join('\n'),
                    clips:[...svg.querySelectorAll('text')].filter(t=>{const b=t.getBoundingClientRect();return t.textContent.trim()&&b.width&&b.height&&(b.left<frame.left-1||b.right>frame.right+1||b.top<frame.top-1||b.bottom>frame.bottom+1)}).map(t=>t.textContent),
                    participants:[...svg.querySelectorAll('[data-cfx-role="sequence-participant"]')].map(g=>({label:g.getAttribute('data-label'),width:g.getBBox().width,fill:g.querySelector('rect')?.getAttribute('fill')})),
                    scripts:svg.querySelectorAll('script,image,foreignObject').length,overflow:document.documentElement.scrollWidth>innerWidth};
            }
            """);
        Assert.True(observed.GetProperty("reference").GetInt32()>0);Assert.True(observed.GetProperty("divider").GetInt32()>0);
        Assert.Equal(0,observed.GetProperty("scripts").GetInt32());Assert.Empty(observed.GetProperty("clips").EnumerateArray());Assert.False(observed.GetProperty("overflow").GetBoolean());
        var text=observed.GetProperty("text").GetString();Assert.Contains("«Service»",text);Assert.Contains("🚀",text);
        var api=Assert.Single(observed.GetProperty("participants").EnumerateArray(),item=>item.GetProperty("label").GetString()=="API service");
        Assert.True(api.GetProperty("width").GetDouble()>=176);Assert.Equal("#336699",api.GetProperty("fill").GetString());
        if(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is {Length:>0} output) {
            Directory.CreateDirectory(output);var path=Path.Combine(output,"zen-browser-"+fixture+"-"+width+(dark?"-dark":"-light"));
            File.WriteAllText(path+".svg",artifact.ToSvg());File.WriteAllBytes(path+".native.png",artifact.ToPng());File.WriteAllText(path+".json",artifact.ToInterchangeJson());File.WriteAllText(path+".observed.json",observed.GetRawText());
            await session.Page.ScreenshotAsync(new PageScreenshotOptions{Path=path+".browser.png",FullPage=true});
        }
        var host=session.Page.Locator("[data-cfx-html-sizing='preserve-size']");
        if(await host.EvaluateAsync<bool>("element=>element.scrollWidth>element.clientWidth")) {
            await host.FocusAsync();await session.Page.Keyboard.PressAsync("ArrowRight");
            await session.Page.WaitForFunctionAsync("()=>document.querySelector('[data-cfx-html-sizing]').scrollLeft>0");
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
