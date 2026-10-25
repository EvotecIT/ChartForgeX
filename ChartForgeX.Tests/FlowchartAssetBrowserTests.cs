using System.Text.Json;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class FlowchartAssetBrowserTests {
    [BrowserTheory]
    [InlineData("compact",false)] [InlineData("compact",true)] [InlineData("wide",false)] [InlineData("wide",true)]
    public async Task OrdinaryAssetWorkflowKeepsItsPaintedCaptionsAndOwnedImagesInsideTheNativeFrame(string variant,bool dark) {
        var artifact=FlowchartAssetRecipe.Create(variant,dark?VisualThemeMode.Dark:VisualThemeMode.Light,BrowserProofFonts.Family);
        var html=artifact.ToHtmlPage(new VisualArtifactRenderOptions{HtmlSizing=VisualArtifactHtmlSizing.PreserveSize});
        await using var session=await BrowserProofFonts.OpenAsync(html,variant=="compact"?640:1100,900);
        var observed=await session.Page.EvaluateAsync<JsonElement>("""
            async () => {
                await document.fonts.ready;await Promise.all([...document.querySelectorAll('svg image')].map(async image=>{
                    const probe=new Image();probe.src=image.getAttribute('href');await probe.decode();
                }));
                const svg=document.querySelector('svg'),frame=svg.getBoundingClientRect(),nodes=[...svg.querySelectorAll('[data-cfx-role="topology-node"]')];
                return {nodes:nodes.length,groups:svg.querySelectorAll('[data-cfx-role="topology-group"]').length,
                    edges:svg.querySelectorAll('[data-cfx-role="topology-edge"]').length,images:svg.querySelectorAll('image').length,
                    empty:nodes.filter(node=>{const b=node.getBBox();return b.width<=0||b.height<=0}).length,
                    clips:nodes.filter(node=>{const b=node.getBoundingClientRect();return b.left<frame.left-1.5||b.right>frame.right+1.5||b.top<frame.top-1.5||b.bottom>frame.bottom+1.5}).map(node=>node.id),
                    externalImages:[...svg.querySelectorAll('image')].filter(image=>!/^data:image\/png;base64,/.test(image.getAttribute('href')||'')).length,
                    active:svg.querySelectorAll('script,foreignObject').length,overflow:document.documentElement.scrollWidth>innerWidth};
            }
            """);
        if(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is {Length:>0} output) {
            Directory.CreateDirectory(output);var name="asset-browser-"+variant+"-"+dark;
            FlowchartAssetRecipeTests.Capture(name,artifact.ToSvg(),artifact.ToPng(),artifact.ToInterchangeJson(),html,"");
            File.WriteAllText(Path.Combine(output,name+".observed.json"),observed.GetRawText());
            await session.Page.ScreenshotAsync(new PageScreenshotOptions{Path=Path.Combine(output,name+".browser.png"),FullPage=true});
        }
        Assert.Equal(5,observed.GetProperty("nodes").GetInt32());Assert.Equal(2,observed.GetProperty("groups").GetInt32());Assert.Equal(4,observed.GetProperty("edges").GetInt32());
        Assert.Equal(1,observed.GetProperty("images").GetInt32());Assert.Equal(0,observed.GetProperty("empty").GetInt32());Assert.Empty(observed.GetProperty("clips").EnumerateArray());
        Assert.Equal(0,observed.GetProperty("externalImages").GetInt32());Assert.Equal(0,observed.GetProperty("active").GetInt32());Assert.False(observed.GetProperty("overflow").GetBoolean());
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
