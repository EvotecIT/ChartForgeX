using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite","Compatibility")]
public sealed class MermaidSurfacePaintBrowserTests {
    [BrowserTheory]
    [InlineData(false,390)][InlineData(true,390)][InlineData(false,1100)][InlineData(true,1100)]
    public async Task AuthoredSurfaceStylesRemainStaticAndKeepTheirComputedPaints(bool dark,int width) {
        if(!InteractiveChartBrowser.Enabled)return;
        foreach(var family in new[]{"flowchart","agentflow"}) {
            var rendered=MermaidRenderer.Render(MermaidSurfacePaintRenderingTests.Fixture(family),new MermaidRenderOptions{Presentation=new MermaidPresentationOptions{ThemeMode=dark?VisualThemeMode.Dark:VisualThemeMode.Light}});
            MermaidSurfacePaintCausalTests.Capture(rendered,family+"-surface-browser-"+width+(dark?"-dark":"-light"));Assert.Empty(rendered.Diagnostics);var artifact=rendered.Artifact!;
            await using var session=await BrowserProofFonts.OpenAsync(artifact.ToHtmlPage(new VisualArtifactRenderOptions{HtmlSizing=VisualArtifactHtmlSizing.PreserveSize}),width,700);
            var observed=await session.Page.EvaluateAsync<string>("""
                () => {
                    const svg=document.querySelector('svg'),frame=svg.getBoundingClientRect(),clipped=[];
                    for(const text of svg.querySelectorAll('text')){const b=text.getBoundingClientRect();if(b.width&&b.height&&text.textContent.trim()&&(b.left<frame.left-1.5||b.top<frame.top-1.5||b.right>frame.right+1.5||b.bottom>frame.bottom+1.5))clipped.push(text.textContent);}
                    const paints=[...svg.querySelectorAll('[data-cfx-role="topology-node"],[data-cfx-role="topology-group"]')].filter(g=>(g.dataset.cfxRole==='topology-node'&&g.dataset.nodeId==='work')||(g.dataset.cfxRole==='topology-group'&&g.dataset.groupId==='review')).map(g=>{
                        const mark=g.querySelector('[data-cfx-role="topology-node-surface"],[data-cfx-role="topology-group-surface"]'),style=getComputedStyle(mark);
                        return {width:parseFloat(style.strokeWidth),dash:style.strokeDasharray.split(/[ ,]+/).filter(Boolean).map(parseFloat)};
                    });
                    return JSON.stringify({paints,clipped,unsafe:svg.querySelectorAll('script,image,foreignObject').length,outerOverflow:document.documentElement.scrollWidth>innerWidth});
                }
                """);
            var output=Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
            if(!string.IsNullOrWhiteSpace(output)) {
                Directory.CreateDirectory(output);var stem=Path.Combine(output,family+"-surface-paints-"+width+(dark?"-dark":"-light"));
                File.WriteAllText(stem+".observed.json",observed);File.WriteAllText(stem+".svg",artifact.ToSvg());File.WriteAllBytes(stem+".native.png",artifact.ToPng());await session.Page.ScreenshotAsync(new PageScreenshotOptions{Path=stem+".browser.png",FullPage=true});
            }
            using var data=JsonDocument.Parse(observed);var root=data.RootElement;Assert.Equal(2,root.GetProperty("paints").GetArrayLength());
            foreach(var paint in root.GetProperty("paints").EnumerateArray()){Assert.Equal(3,paint.GetProperty("width").GetDouble());Assert.Equal(new double[]{9,4,2,9,4,2},paint.GetProperty("dash").EnumerateArray().Select(v=>v.GetDouble()));}
            Assert.Empty(root.GetProperty("clipped").EnumerateArray());Assert.Equal(0,root.GetProperty("unsafe").GetInt32());Assert.False(root.GetProperty("outerOverflow").GetBoolean());InteractiveChartBrowser.AssertNoConsoleErrors(session);
        }
    }
}
