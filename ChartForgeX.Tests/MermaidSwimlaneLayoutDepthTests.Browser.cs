using System.Text.Json;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidSwimlaneLayoutDepthTests {
    [BrowserTheory]
    [InlineData(false,390)] [InlineData(true,390)] [InlineData(false,1100)] [InlineData(true,1100)]
    public async Task SourceLocalDirectionsAndRankPoliciesKeepActualGeometryAcrossPlainNativeHosts(bool dark,int width) {
        var source=Source();var options=Options(dark,width);var direct=MermaidRenderer.Render(source,options);Assert.Empty(direct.Diagnostics);
        var fence=new MermaidVisualMarkupParser(options).Parse("```mermaid\n"+source+"\n```");Assert.False(fence.HasErrors);
        foreach(var pair in new[]{("direct",direct.Artifact!),("fence",Assert.Single(fence.Artifacts))}) {
            var artifact=pair.Item2;var name="swimlane-layout-depth-"+pair.Item1+"-"+width+"-"+dark;
            await using var session=await BrowserProofFonts.OpenAsync(artifact.ToHtmlPage(new VisualArtifactRenderOptions {HtmlSizing=VisualArtifactHtmlSizing.PreserveSize}),width,1000);
            MermaidNestedSwimlaneTests.Capture(artifact,name);var output=Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
            if(!string.IsNullOrWhiteSpace(output)){Directory.CreateDirectory(output);await session.Page.ScreenshotAsync(new PageScreenshotOptions {Path=Path.Combine(output,name+".browser.png"),FullPage=true});}
            var observed=await session.Page.EvaluateAsync<JsonElement>("""
                ()=>{
                    const svg=document.querySelector('svg'),box=element=>{const r=element.getBoundingClientRect();return {x:r.left,y:r.top,right:r.right,bottom:r.bottom}};
                    const nodes=[...svg.querySelectorAll('[data-cfx-role="topology-node"]')].map(node=>({id:node.getAttribute('data-node-id'),...box(node)}));
                    const groups=[...svg.querySelectorAll('[data-cfx-role="topology-group"]')].map(group=>({id:group.getAttribute('data-group-id'),parent:group.getAttribute('data-parent-group-id'),...box(group.querySelector('[data-cfx-role="topology-group-surface"]'))}));
                    return {nodes,groups,edges:svg.querySelectorAll('[data-cfx-role="topology-edge"]').length,
                        text:[...svg.querySelectorAll('text')].map(node=>node.textContent),active:svg.querySelectorAll('script,image,foreignObject').length,
                        invalid:/NaN|Infinity/.test(svg.outerHTML),documentOverflow:document.documentElement.scrollWidth>innerWidth};
                }
                """);
            if(!string.IsNullOrWhiteSpace(output))File.WriteAllText(Path.Combine(output,name+".observed.json"),observed.GetRawText());
            var nodes=observed.GetProperty("nodes").EnumerateArray().ToArray();var a=nodes.Single(node=>node.GetProperty("id").GetString()=="A");var b=nodes.Single(node=>node.GetProperty("id").GetString()=="B");
            Assert.True(b.GetProperty("y").GetDouble()>a.GetProperty("y").GetDouble());Assert.Equal(5,nodes.Length);Assert.Equal(3,observed.GetProperty("edges").GetInt32());
            var groups=observed.GetProperty("groups").EnumerateArray().ToArray();var api=groups.Single(group=>group.GetProperty("id").GetString()=="API");var product=groups.Single(group=>group.GetProperty("id").GetString()=="Product");
            Assert.Equal("Product",api.GetProperty("parent").GetString());
            Assert.True(api.GetProperty("x").GetDouble()>=product.GetProperty("x").GetDouble()-1&&api.GetProperty("right").GetDouble()<=product.GetProperty("right").GetDouble()+1
                &&api.GetProperty("y").GetDouble()>=product.GetProperty("y").GetDouble()-1&&api.GetProperty("bottom").GetDouble()<=product.GetProperty("bottom").GetDouble()+1);
            var captions=observed.GetProperty("text").EnumerateArray().Select(value=>value.GetString()).ToArray();foreach(var caption in new[]{"Plan","Accept","Build","Check","Deliver","Product delivery","API","Partner"})Assert.Contains(caption,captions);
            Assert.Equal(0,observed.GetProperty("active").GetInt32());Assert.False(observed.GetProperty("invalid").GetBoolean());Assert.False(observed.GetProperty("documentOverflow").GetBoolean());
            InteractiveChartBrowser.AssertNoConsoleErrors(session);
        }
    }
}
