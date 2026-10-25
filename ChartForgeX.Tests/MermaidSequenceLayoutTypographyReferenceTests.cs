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
    public async Task PinnedSequenceRoleFontAndAllocationSourceIsComparedWithNativeSvgAndPng(bool dark,int width) {
        var corpus=Path.Combine(TestRepository.Root,"tests","mermaid-conformance");using var manifest=JsonDocument.Parse(File.ReadAllText(Path.Combine(corpus,"compatibility.json")));
        var reference=manifest.RootElement.GetProperty("references").GetProperty("12");var bundle=Path.Combine(corpus,"node_modules",reference.GetProperty("package").GetString()!);
        using var installed=JsonDocument.Parse(File.ReadAllText(Path.Combine(bundle,"package.json")));Assert.Equal(reference.GetProperty("version").GetString(),installed.RootElement.GetProperty("version").GetString());
        var source=SequenceSourcePolicyFixture.Canonical();var result=MermaidRenderer.Render(source,new MermaidRenderOptions{
            Presentation=new MermaidPresentationOptions{ThemeMode=dark?VisualThemeMode.Dark:VisualThemeMode.Light,FontFamily=BrowserProofFonts.Family},
            Sequence=new MermaidSequenceRenderOptions{Width=width-32,Height=620}});Assert.Empty(result.Diagnostics);var artifact=result.Artifact!;
        var svg=artifact.ToSvg();var png=artifact.ToPng();
        var html="<style>body{margin:0;padding:16px;box-sizing:border-box;background:"+(dark?"#171b23;color:#edf2f7":"#fff;color:#182235")
            +"}h2{font:18px system-ui}section{max-width:100%;overflow:auto;margin-bottom:24px}svg,img{display:block;max-width:none}</style><h2>Pinned sequence source typography</h2><section id='reference'></section>"
            +"<h2>Native SVG</h2><section id='native'>"+svg+"</section><h2>Native PNG</h2><section><img id='png' src='data:image/png;base64,"+Convert.ToBase64String(png)+"'></section>";
        await using var session=await BrowserProofFonts.OpenAsync(html,width,1000);
        await session.Page.AddScriptTagAsync(new PageAddScriptTagOptions{Path=Path.Combine(bundle,"dist","mermaid.min.js")});
        var observed=await ObserveAsync(session.Page,source,"",dark,new Dictionary<string,int>{{"sequence-participant",2},{"sequence-message",2},{"sequence-note",1}});
        var fonts=await session.Page.EvaluateAsync<JsonElement>("""
            () => Object.fromEntries(['reference','native'].map(id=>[id,[...document.querySelectorAll('#'+id+' svg text')].map(node=>{
              const s=getComputedStyle(node);return {text:node.textContent,size:parseFloat(s.fontSize),weight:s.fontWeight,family:s.fontFamily};})]))
            """);
        await CaptureAsync(session.Page,"sequence-layout-reference12-"+width+(dark?"-dark":"-light"),svg,png,artifact.ToInterchangeJson(),observed);
        if(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is {Length:>0} output)
            File.WriteAllText(Path.Combine(output,"sequence-layout-reference12-"+width+(dark?"-dark":"-light")+".fonts.json"),fonts.GetRawText());
        Assert.True(observed.GetProperty("referencePaintElements").GetInt32()>0);Assert.Empty(observed.GetProperty("clip").EnumerateArray());Assert.False(observed.GetProperty("horizontalOverflow").GetBoolean());
        Assert.Equal(0,observed.GetProperty("nativeScripts").GetInt32());
        foreach(var surface in new[]{"reference","native"}) {
            foreach(var caption in new[]{"Review","Store","Save","Ready","Note"})Assert.Contains(caption,observed.GetProperty(surface+"Text").GetString());
            AssertFont(surface,"Review",18,"700");AssertFont(surface,"Save",20,"500");AssertFont(surface,"Note",16,"400");
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        void AssertFont(string surface,string caption,double size,string weight) {
            var actual=fonts.GetProperty(surface).EnumerateArray().First(item=>item.GetProperty("text").GetString()!.Contains(caption,StringComparison.Ordinal));
            Assert.Equal(size,actual.GetProperty("size").GetDouble());Assert.Equal(weight,actual.GetProperty("weight").GetString());
        }
    }
}
