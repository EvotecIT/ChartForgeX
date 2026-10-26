using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite","Compatibility")]
public sealed class MermaidFlowchartCatalogBrowserTests {
    [BrowserTheory]
    [InlineData(false,390)] [InlineData(true,390)] [InlineData(false,1100)] [InlineData(true,1100)]
    public async Task CompleteCatalogKeepsVisibleCaptionsAndStaticScrollHosts(bool dark,int width) {
        if(!InteractiveChartBrowser.Enabled)return;
        foreach(var suffix in new[]{"a","b","c","d"}) {
            var source=MermaidFlowchartCatalogCausalTests.Fixture(suffix);
            if(dark)source="%%{init: {'theme':'dark'}}%%\n"+source;
            var rendered=MermaidRenderer.Render(source);Assert.Empty(rendered.Diagnostics);var artifact=rendered.Artifact!;
            await using var session=await BrowserProofFonts.OpenAsync(artifact.ToHtmlPage(new VisualArtifactRenderOptions{HtmlSizing=VisualArtifactHtmlSizing.PreserveSize}),width,850);
            var observed=await session.Page.EvaluateAsync<string>("""
                () => {
                    const svg=document.querySelector('svg'),frame=svg.getBoundingClientRect(),clipped=[],lost=[],hidden=[];
                    const suppressed=new Set(['FilledCircle','FramedCircle','SynchronizationBar','Hourglass','LightningBolt','CrossedCircle']);
                    for(const node of svg.querySelectorAll('[data-cfx-role="topology-node"]')) {
                        const labels=[...node.querySelectorAll('[data-cfx-role="topology-node-label"]')];
                        if(suppressed.has(node.dataset.nodeShape)){if(labels.length)hidden.push(node.dataset.nodeLabel);continue;}
                        const actual=labels.map(label=>label.textContent).join('').replace(/\s/g,'');
                        if(actual!==node.dataset.nodeLabel.replace(/\s/g,''))lost.push(node.dataset.nodeLabel);
                    }
                    for(const text of svg.querySelectorAll('text')) {
                        const box=text.getBoundingClientRect();
                        if(box.width&&box.height&&text.textContent.trim()&&(box.left<frame.left-1.5||box.top<frame.top-1.5||box.right>frame.right+1.5||box.bottom>frame.bottom+1.5))clipped.push(text.textContent);
                    }
                    const host=document.querySelector('[data-cfx-html-sizing=preserve-size]');
                    return JSON.stringify({clipped,lost,hidden,nodes:svg.querySelectorAll('[data-cfx-role="topology-node"]').length,
                        edges:svg.querySelectorAll('[data-cfx-role="topology-edge"]').length,scripts:svg.querySelectorAll('script,image,foreignObject').length,
                        outerOverflow:document.documentElement.scrollWidth>innerWidth,scrollable:host.scrollWidth>host.clientWidth});
                }
                """);
            var output=Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
            if(!string.IsNullOrWhiteSpace(output)) {
                Directory.CreateDirectory(output);var stem=Path.Combine(output,"flowchart-catalog-"+suffix+"-"+width+(dark?"-dark":"-light"));
                File.WriteAllText(stem+".observed.json",observed);File.WriteAllText(stem+".svg",artifact.ToSvg());File.WriteAllBytes(stem+".native.png",artifact.ToPng());
                await session.Page.ScreenshotAsync(new PageScreenshotOptions{Path=stem+".browser.png",FullPage=true});
            }
            using var data=JsonDocument.Parse(observed);var root=data.RootElement;
            Assert.Equal(suffix=="a"?14:13,root.GetProperty("nodes").GetInt32());Assert.Equal(suffix=="a"?13:12,root.GetProperty("edges").GetInt32());
            Assert.Empty(root.GetProperty("clipped").EnumerateArray());Assert.Empty(root.GetProperty("lost").EnumerateArray());Assert.Empty(root.GetProperty("hidden").EnumerateArray());
            Assert.Equal(0,root.GetProperty("scripts").GetInt32());Assert.False(root.GetProperty("outerOverflow").GetBoolean());
            if(root.GetProperty("scrollable").GetBoolean()) {
                var host=session.Page.Locator("[data-cfx-html-sizing=preserve-size]");await host.FocusAsync();await session.Page.Keyboard.PressAsync("ArrowRight",new KeyboardPressOptions{Delay=100});
                await session.Page.WaitForFunctionAsync("()=>document.querySelector('[data-cfx-html-sizing=preserve-size]').scrollLeft>0");
            }
            InteractiveChartBrowser.AssertNoConsoleErrors(session);
        }
    }
}
