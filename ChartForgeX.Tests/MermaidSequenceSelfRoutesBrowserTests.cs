using System.Text.Json;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;
namespace ChartForgeX.Tests;
[Trait("BrowserSuite","Compatibility")]
public sealed class MermaidSequenceSelfRoutesBrowserTests {
    [BrowserTheory] [InlineData(false,390)] [InlineData(true,390)] [InlineData(false,1100)] [InlineData(true,1100)]
    public async Task BothExplicitNativeSelfRoutesRemainCompleteInDirectAndFenceHosts(bool dark,int width) {
        foreach(var rightAngles in new[]{true,false}) {
            var source=SequenceSelfSourceFixture.Source(rightAngles);var options=new MermaidRenderOptions{WarningPolicy=MermaidWarningPolicy.Reject,Presentation=new MermaidPresentationOptions{ThemeMode=dark?VisualThemeMode.Dark:VisualThemeMode.Light,FontFamily=BrowserProofFonts.Family},Sequence=new MermaidSequenceRenderOptions{Width=width-32,Height=820}};
            var direct=MermaidRenderer.Render(source,options);Assert.Empty(direct.Diagnostics);var fence=new MermaidVisualMarkupParser(options).Parse("```mermaid\n"+source+"\n```");Assert.Empty(fence.Diagnostics);
            foreach(var pair in new[]{("direct",direct.Artifact!),("fence",Assert.Single(fence.Artifacts))}) {
                await using var session=await BrowserProofFonts.OpenAsync(pair.Item2.ToHtmlPage(new VisualArtifactRenderOptions{HtmlSizing=VisualArtifactHtmlSizing.PreserveSize}),width,900);var page=session.Page;
                var observed=await page.EvaluateAsync<JsonElement>("""
                    () => {const svg=document.querySelector('svg'),f=svg.getBoundingClientRect();return {paths:[...svg.querySelectorAll('[data-cfx-role="sequence-message-line"]')].map(p=>({d:p.getAttribute('d'),length:p.getTotalLength()})),text:svg.textContent,scripts:svg.querySelectorAll('script,foreignObject').length,clip:[...svg.querySelectorAll('text')].filter(t=>{const b=t.getBoundingClientRect();return b.width&&b.height&&(b.left<f.left-1.5||b.top<f.top-1.5||b.right>f.right+1.5||b.bottom>f.bottom+1.5)}).map(t=>t.textContent),overflow:document.documentElement.scrollWidth>innerWidth};}
                    """);
                var output=Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");var stem="sequence-self-"+pair.Item1+"-"+rightAngles+"-"+width+(dark?"-dark":"-light");if(!string.IsNullOrWhiteSpace(output)){Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,stem+".mmd"),source);File.WriteAllText(Path.Combine(output,stem+".svg"),pair.Item2.ToSvg());File.WriteAllBytes(Path.Combine(output,stem+".native.png"),pair.Item2.ToPng());File.WriteAllText(Path.Combine(output,stem+".native.json"),pair.Item2.ToInterchangeJson());File.WriteAllText(Path.Combine(output,stem+".observed.json"),observed.GetRawText());await page.ScreenshotAsync(new PageScreenshotOptions{Path=Path.Combine(output,stem+".browser.png"),FullPage=true});}
                Assert.Equal(2,observed.GetProperty("paths").GetArrayLength());Assert.All(observed.GetProperty("paths").EnumerateArray(),p=>{Assert.Equal(!rightAngles,p.GetProperty("d").GetString()!.Contains("C",StringComparison.Ordinal));Assert.True(p.GetProperty("length").GetDouble()>0);});foreach(var caption in new[]{"Review","Check","Save"})Assert.Contains(caption,observed.GetProperty("text").GetString());Assert.Empty(observed.GetProperty("clip").EnumerateArray());Assert.False(observed.GetProperty("overflow").GetBoolean());Assert.Equal(0,observed.GetProperty("scripts").GetInt32());InteractiveChartBrowser.AssertNoConsoleErrors(session);
            }
        }
    }
}
