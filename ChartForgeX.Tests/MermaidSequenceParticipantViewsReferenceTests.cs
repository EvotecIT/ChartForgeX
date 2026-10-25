using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidNotationReferenceRenderingTests {
    [MermaidReferenceTheory]
    [Trait("Category","MermaidReference")]
    [InlineData(false,390)] [InlineData(true,390)] [InlineData(false,1100)] [InlineData(true,1100)]
    public async Task PinnedSequenceParticipantViewSourceIsComparedWithNativeSvgAndPng(bool dark,int width) {
        var corpus=Path.Combine(TestRepository.Root,"tests","mermaid-conformance");using var manifest=JsonDocument.Parse(File.ReadAllText(Path.Combine(corpus,"compatibility.json")));
        var reference=manifest.RootElement.GetProperty("references").GetProperty("12");var bundle=Path.Combine(corpus,"node_modules",reference.GetProperty("package").GetString()!);
        using var installed=JsonDocument.Parse(File.ReadAllText(Path.Combine(bundle,"package.json")));Assert.Equal(reference.GetProperty("version").GetString(),installed.RootElement.GetProperty("version").GetString());
        var source=SequenceViewSourceFixture.Canonical();var result=MermaidRenderer.Render(source,new MermaidRenderOptions{
            Presentation=new MermaidPresentationOptions{ThemeMode=dark?VisualThemeMode.Dark:VisualThemeMode.Light,FontFamily=BrowserProofFonts.Family},
            Sequence=new MermaidSequenceRenderOptions{Width=width-32,Height=820}});Assert.Empty(result.Diagnostics);var artifact=result.Artifact!;
        var svg=artifact.ToSvg();var png=artifact.ToPng();
        var html="<style>body{margin:0;padding:16px;box-sizing:border-box;background:"+(dark?"#171b23;color:#edf2f7":"#fff;color:#182235")
            +"}h2{font:18px system-ui}section{max-width:100%;overflow:auto;margin-bottom:24px}svg,img{display:block;max-width:none}</style><h2>Pinned participant views</h2><section id='reference'></section>"
            +"<h2>Native SVG</h2><section id='native'>"+svg+"</section><h2>Native PNG</h2><section><img id='png' src='data:image/png;base64,"+Convert.ToBase64String(png)+"'></section>";
        await using var session=await BrowserProofFonts.OpenAsync(html,width,1000);
        await session.Page.AddScriptTagAsync(new PageAddScriptTagOptions{Path=Path.Combine(bundle,"dist","mermaid.min.js")});
        var observed=await ObserveAsync(session.Page,source,"",dark,new Dictionary<string,int>{{"sequence-participant",3},{"sequence-message",3},{"sequence-note",1}});
        var fonts=await session.Page.EvaluateAsync<JsonElement>("""
            () => Object.fromEntries(['reference','native'].map(id=>[id,[...document.querySelectorAll('#'+id+' svg text')].map(node=>{
              const s=getComputedStyle(node);return {text:node.textContent,size:parseFloat(s.fontSize),weight:s.fontWeight,family:s.fontFamily};})]))
            """);
        var occurrences=await session.Page.EvaluateAsync<JsonElement>("""
            () => ({reference:{top:document.querySelectorAll('#reference .actor-top').length,bottom:document.querySelectorAll('#reference .actor-bottom').length},
              native:{top:document.querySelectorAll('#native [data-cfx-role="sequence-participant"]').length,bottom:document.querySelectorAll('#native [data-cfx-role="sequence-participant-footer"]').length}})
            """);
        await CaptureAsync(session.Page,"sequence-participant-views-reference12-"+width+(dark?"-dark":"-light"),svg,png,artifact.ToInterchangeJson(),observed);
        if(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is {Length:>0} output)
            {File.WriteAllText(Path.Combine(output,"sequence-participant-views-reference12-"+width+(dark?"-dark":"-light")+".fonts.json"),fonts.GetRawText());
             File.WriteAllText(Path.Combine(output,"sequence-participant-views-reference12-"+width+(dark?"-dark":"-light")+".occurrences.json"),occurrences.GetRawText());}
        Assert.True(observed.GetProperty("referencePaintElements").GetInt32()>0);Assert.Empty(observed.GetProperty("clip").EnumerateArray());Assert.False(observed.GetProperty("horizontalOverflow").GetBoolean());
        Assert.Equal(0,observed.GetProperty("nativeScripts").GetInt32());
        foreach(var surface in new[]{"reference","native"}) {
            foreach(var caption in new[]{"Review","Archive","Audit","Store","Saved","Close","Retained note"})Assert.Contains(caption,observed.GetProperty(surface+"Text").GetString());
            AssertFont(surface,"Review",14,"400");AssertFont(surface,"Store",12,"400");
            Assert.DoesNotContain("Retained unused",observed.GetProperty(surface+"Text").GetString());Assert.DoesNotContain("empty-group",observed.GetProperty(surface+"Text").GetString());
            Assert.Equal(3,occurrences.GetProperty(surface).GetProperty("top").GetInt32());Assert.Equal(3,occurrences.GetProperty(surface).GetProperty("bottom").GetInt32());
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        void AssertFont(string surface,string caption,double size,string weight) {
            var actual=fonts.GetProperty(surface).EnumerateArray().First(item=>item.GetProperty("text").GetString()!.Contains(caption,StringComparison.Ordinal));
            Assert.Equal(size,actual.GetProperty("size").GetDouble());Assert.Equal(weight,actual.GetProperty("weight").GetString());
        }
    }
}
