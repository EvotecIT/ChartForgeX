using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;
namespace ChartForgeX.Tests;
public sealed partial class MermaidNotationReferenceRenderingTests {
    [MermaidReferenceTheory] [Trait("Category","MermaidReference")]
    [InlineData(false,390)] [InlineData(true,390)] [InlineData(false,1100)] [InlineData(true,1100)]
    public async Task ExplicitSelfRoutesAreComparedWithPinnedNativePathsAndPixels(bool dark,int width) {
        var corpus=Path.Combine(TestRepository.Root,"tests","mermaid-conformance");using var manifest=JsonDocument.Parse(File.ReadAllText(Path.Combine(corpus,"compatibility.json")));var reference=manifest.RootElement.GetProperty("references").GetProperty("12");var bundle=Path.Combine(corpus,"node_modules",reference.GetProperty("package").GetString()!);using var installed=JsonDocument.Parse(File.ReadAllText(Path.Combine(bundle,"package.json")));Assert.Equal(reference.GetProperty("version").GetString(),installed.RootElement.GetProperty("version").GetString());
        foreach(var rightAngles in new[]{true,false}) {
            var source=SequenceSelfSourceFixture.Source(rightAngles);var result=MermaidRenderer.Render(source,new MermaidRenderOptions{WarningPolicy=MermaidWarningPolicy.Reject,Presentation=new MermaidPresentationOptions{ThemeMode=dark?VisualThemeMode.Dark:VisualThemeMode.Light,FontFamily=BrowserProofFonts.Family},Sequence=new MermaidSequenceRenderOptions{Width=width-32,Height=820}});Assert.Empty(result.Diagnostics);var artifact=result.Artifact!;var svg=artifact.ToSvg();var png=artifact.ToPng();
            var html="<style>body{margin:0;padding:16px;box-sizing:border-box}section{max-width:100%;overflow:auto;margin-bottom:24px}svg,img{display:block;max-width:none}</style><section id='reference'></section><section id='native'>"+svg+"</section><section><img src='data:image/png;base64,"+Convert.ToBase64String(png)+"'></section>";
            await using var session=await BrowserProofFonts.OpenAsync(html,width,1000);await session.Page.AddScriptTagAsync(new PageAddScriptTagOptions{Path=Path.Combine(bundle,"dist","mermaid.min.js")});var observed=await ObserveAsync(session.Page,source,"",dark,new Dictionary<string,int>{{"sequence-participant",1},{"sequence-message",2}});
            var paths=await session.Page.EvaluateAsync<JsonElement>("""
                () => Object.fromEntries(['reference','native'].map(id=>[id,[...document.querySelectorAll(id==='reference'?'#reference path.messageLine0,#reference path.messageLine1':'#native [data-cfx-role="sequence-message-line"]')].map(p=>({d:p.getAttribute('d'),length:p.getTotalLength()}))]))
                """);var stem="sequence-self-reference12-"+rightAngles+"-"+width+(dark?"-dark":"-light");await CaptureAsync(session.Page,stem,svg,png,artifact.ToInterchangeJson(),observed);if(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is {Length:>0} output)File.WriteAllText(Path.Combine(output,stem+".paths.json"),paths.GetRawText());
            foreach(var surface in new[]{"reference","native"}){Assert.Equal(2,paths.GetProperty(surface).GetArrayLength());Assert.All(paths.GetProperty(surface).EnumerateArray(),p=>{Assert.Equal(!rightAngles,p.GetProperty("d").GetString()!.Contains("C",StringComparison.OrdinalIgnoreCase));Assert.True(p.GetProperty("length").GetDouble()>0);});foreach(var caption in new[]{"Review","Check","Save"})Assert.Contains(caption,observed.GetProperty(surface+"Text").GetString());}Assert.Empty(observed.GetProperty("clip").EnumerateArray());Assert.False(observed.GetProperty("horizontalOverflow").GetBoolean());Assert.Equal(0,observed.GetProperty("nativeScripts").GetInt32());InteractiveChartBrowser.AssertNoConsoleErrors(session);
        }
    }
}
